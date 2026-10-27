"""Read the frozen B1 source without numpy/OpenCV or game-data side effects."""
from collections import Counter,defaultdict,deque
from pathlib import Path
import argparse,json,math
DEFAULT=Path(__file__).with_name('world-layout.json')

def decode_runs(rows):
    cells=set()
    for row in rows:
        if len(row)!=3 or any(type(n) is not int for n in row):raise ValueError('Run must contain three integers')
        y,a,b=row
        if b<a:raise ValueError('Run endpoints reversed')
        line={(x,y) for x in range(a,b+1)}
        if cells&line:raise ValueError('Overlapping runs')
        cells|=line
    return cells

class WorldLayout:
    def __init__(self,data):
        if data.get('schemaVersion')!=1:raise ValueError('Unsupported layout schema')
        self.data=data;self.land=decode_runs(data['landRuns']);self.maps={};self.owners={};self.map_cells={}
        for m in data['maps']:
            key=m['key']
            if key in self.maps:raise ValueError('Duplicate map key')
            cells=decode_runs(m['runs'])
            if not cells:raise ValueError('Empty map')
            if set(self.owners)&cells:raise ValueError('Map ownership overlaps')
            xs=[x for x,y in cells];ys=[y for x,y in cells]
            if m['bounds']!=[min(xs),min(ys),max(xs)+1,max(ys)+1]:raise ValueError('Map bounds mismatch')
            counts=(len(cells),len(cells&self.land),len(cells-self.land))
            if counts!=(m['area'],m['landArea'],m['nearSeaArea']):raise ValueError('Map area mismatch')
            self.maps[key]=m;self.map_cells[key]=cells;self.owners.update({cell:key for cell in cells})
        self.towns={}
        for t in data['towns']:
            key=t['key'];p=tuple(math.floor(v) for v in t['position'])
            if key in self.towns or key not in self.maps or p not in self.map_cells[key] or p not in self.land:raise ValueError('Town must be unique on its own land')
            self.towns[key]=t
        self.roads=defaultdict(set)
        for r in data['roads']:
            a,b=r['fromKey'],r['toKey']
            if a==b or a not in self.towns or b not in self.towns:raise ValueError('Road endpoint missing')
            if r['waypoints']!=[self.towns[a]['position'],self.towns[b]['position']]:raise ValueError('Road endpoint coordinates differ')
            if b in self.roads[a]:raise ValueError('Duplicate road')
            self.roads[a].add(b);self.roads[b].add(a)
        if not self.towns:raise ValueError('No towns')
        seen=set();todo=[next(iter(self.towns))]
        while todo:
            n=todo.pop()
            if n in seen:continue
            seen.add(n);todo+=list(self.roads[n]-seen)
        if seen!=set(self.towns):raise ValueError('Disconnected town diagram')
        main=data['mainQuest']
        if any(b not in self.roads[a] for a,b in zip(main,main[1:])):raise ValueError('Main quest diagram disconnected')
        totals=Counter()
        for m in self.maps.values():totals[m['region']]+=m['area']
        if dict(totals)!={'goryeo':20000,'east':25000}:raise ValueError('Region total mismatch')
        # This only checks ownership contact, not playable coast/river crossings.
        self.shared_edges=Counter()
        for (x,y),owner in self.owners.items():
            for other in (self.owners.get((x+1,y)),self.owners.get((x,y+1))):
                if other and other!=owner:self.shared_edges[tuple(sorted((owner,other)))]+=1
        if any('goryeo/개경' in pair and any(n.startswith('east/') for n in pair) for pair in self.shared_edges):raise ValueError('Capital touches the central east region')
        for link in data['externalLinks']:
            if link['fromKey'] not in self.towns or link['agreement']!='D4/D7 pending' or link['runtimeBinding'] is not None:raise ValueError('External link agreement changed')

    @classmethod
    def read(cls,path=DEFAULT):return cls(json.loads(Path(path).read_text()))

    def owner_at(self,x,y):return self.owners.get((math.floor(x),math.floor(y)))

    def local_cell(self,key,x,y):
        if self.owner_at(x,y)!=key:raise ValueError('Point is outside assigned map')
        bx,by,_,_=self.maps[key]['bounds'];return math.floor(x)-bx,math.floor(y)-by

    def report(self):
        return dict(maps=len(self.maps),towns=len(self.towns),roads=len(self.data['roads']),assignedCells=len(self.owners),landCells=sum(m['landArea'] for m in self.maps.values()),nearSeaCells=sum(m['nearSeaArea'] for m in self.maps.values()),regions={r:sum(m['area'] for m in self.maps.values() if m['region']==r) for r in ('goryeo','east')},mapsByArea={m['label']:m['area'] for m in self.maps.values()},sharedBoundaries=[dict(maps=list(pair),edges=count) for pair,count in sorted(self.shared_edges.items())],externalRuntimeAgreements='pending D4/D7',economicDistancePolicy='not chosen',playableRoadsRiversScenes='not generated')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--source',type=Path,default=DEFAULT);p.add_argument('--report',type=Path);a=p.parse_args();report=WorldLayout.read(a.source).report()
    if a.report:a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(report,ensure_ascii=False))
