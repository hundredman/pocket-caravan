"""T3 review-only rectangular scene boundaries, reusing current frozen native tiles.

Reads the 13 production exports as a locked baseline, never writes production.
No source artwork is cropped/resized/recolored: tile references move in world space.
This review is not a production exporter or a save migration.
"""
from pathlib import Path
from collections import defaultdict, Counter
import copy, hashlib, json, sys
import numpy as np
from scipy import ndimage
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
sys.path.insert(0,str(HERE))
from m4_objects import seams, O
CONFIG=HERE/'t3_boundaries.json'
OUT=ROOT/'Assets/PocketCaravan/Editor/B2Preview/T3Boundaries'
REVIEW=ROOT/'ArtReview/T3-Boundaries-20261107'
VALIDATION=ROOT/'Docs/Validation/T3-Boundaries-2026-11-07'
LAYERS=('ground','deco','obj','high')
MOVABLE={'anim','ambience','sign','lantern'}

def filter_boundary_faces(boundary,ports,walk,oy):
    """Keep accessible faces; reserve foreign gateway strips in both directions.

    Split local runs/paired arrivals. A local crossing must not arrive opposite
    a foreign gate and then send a player to another region when walking back.
    """
    for mid,entries in list(boundary.items()):
        reserved={(p['world'][0]+dx,p['world'][1]+dy,p['edge'])
                  for p in ports if p['mapId']==mid
                  for dx in range(p['size'][0]) for dy in range(p['size'][1])}
        for exit in list(entries):
            if exit['kind']!='exit':continue
            horizontal=exit['edge']in ('N','S');length=exit['w']if horizontal else exit['h']
            x,y=exit['world']
            dy,dx={'N':(-1,0),'S':(1,0),'W':(0,-1),'E':(0,1)}[exit['edge']]
            keep=[]
            for i in range(length):
                xx,yy=x+(i if horizontal else 0),y+(0 if horizontal else i)
                approach=((yy-oy-dy,xx-dx),(yy-oy+2*dy,xx+2*dx))
                safe=all(0<=ay<walk.shape[0]and 0<=ax<walk.shape[1]and walk[ay,ax]for ay,ax in approach)
                opposite_gate=any(p['mapId']==exit['target']and {'N':'S','S':'N','W':'E','E':'W'}[p['edge']]==exit['edge']
                    and p['world'][0]<=xx+dx<p['world'][0]+p['size'][0]and p['world'][1]<=yy+dy<p['world'][1]+p['size'][1]for p in ports)
                keep.append(safe and (xx,yy,exit['edge'])not in reserved and not opposite_gate)
            if all(keep):continue
            arrivals=boundary[exit['target']]
            spawn=next(s for s in arrivals if s['kind']=='spawn'and s['id']==exit['spawn'])
            entries.remove(exit);arrivals.remove(spawn)
            i=0
            while i<length:
                if not keep[i]:i+=1;continue
                start=i
                while i<length and keep[i]:i+=1
                name=exit['spawn']+'_face_'+str(start)
                for original,destination,kind in ((exit,entries,'exit'),(spawn,arrivals,'spawn')):
                    item=copy.deepcopy(original);xx,yy=item['world']
                    item['world']=(xx+(start if horizontal else 0),yy+(0 if horizontal else start))
                    item['w'],item['h']=(i-start,1)if horizontal else (1,i-start)
                    item['spawn'if kind=='exit'else'id']=name;destination.append(item)

def read_sources():
    config=json.loads(CONFIG.read_text())
    if config['schemaVersion']!=1 or config['status']not in ('review','approved'):raise ValueError('Invalid boundary source')
    bundle=HERE/'t3_baseline.json.gz'
    if bundle.exists():
        import gzip
        sources=json.loads(gzip.decompress(bundle.read_bytes()))['maps']
        for mid,d in sources.items():
            if hashlib.sha256(json.dumps(d,sort_keys=True,ensure_ascii=False).encode()).hexdigest()!=config['sourceLocks'][mid]:raise ValueError('Frozen baseline changed: '+mid)
        return config,sources
    sources={}
    for mid in config['cuts']:
        paths=list((ROOT/'Assets/PocketCaravan/Maps').rglob(mid+'.json'))
        if len(paths)!=1:raise ValueError(mid)
        sources[mid]=json.loads(paths[0].read_text())
        expected=config.get('sourceLocks',{}).get(mid)
        got=hashlib.sha256(json.dumps(sources[mid],sort_keys=True,ensure_ascii=False).encode()).hexdigest()
        if expected and expected!=got:raise ValueError('Baseline changed; review/rebase the boundary source before regeneration: '+mid)
    return config,sources

def generate(config,sources):
    height,width=260,300;oy=-130
    known=np.zeros((height,width),bool);walk=known.copy();road=np.zeros((height,width),int)
    tile_world={k:np.full((height,width),'',object)for k in LAYERS};water=known.copy()
    for mid,d in sources.items():
        x,y=d['terrainX'],d['terrainY']-oy;h,w=d['h'],d['w'];region=np.s_[y:y+h,x:x+w]
        for layer in LAYERS:
            tiles=np.array(d['palette'],object)[np.array(d[layer]).reshape(h,w)]
            dst=tile_world[layer][region];valid=tiles!=''
            conflict=valid&(dst!='')&(dst!=tiles)
            if conflict.any():raise ValueError(f'Conflicting world tiles {mid}/{layer}: {conflict.sum()}')
            dst[valid]=tiles[valid]
        known[region]|=np.array(d['ground']).reshape(h,w)>0
        walk[region]|=np.array(d['blocked']).reshape(h,w)==0
        road[region]=np.maximum(road[region],np.array(d['road']).reshape(h,w))
        if 'water'in d:water[region]|=np.array(d['water']).reshape(h,w)>0
    ids=list(config['cuts']);owners=np.full((height,width),-1,int)
    for idx,mid in enumerate(ids):
        x0,y0,x1,y1=config['cuts'][mid];view=owners[y0-oy:y1-oy,x0:x1]
        if (view>=0).any():raise ValueError('Overlapping scene cuts')
        view[:]=idx
    if (owners[known]<0).any():raise ValueError('World coverage gap')
    # Natural silhouettes remain unchanged. Trim only empty outside margins.
    boxes={}
    for idx,mid in enumerate(ids):
        yy,xx=np.nonzero(known&(owners==idx));boxes[mid]=[int(xx.min()),int(yy.min()+oy),int(xx.max()+1),int(yy.max()+oy+1)]
    for mid,override in config.get('boundsOverrides',{}).items():boxes[mid]=override
    # 2026-11-08 v4: no black void inside a scene. Empty cells in each scene rectangle get blocked forest,
    # copied from one approved 16x16 forest patch (tiles only move; no new art). Walkable land and links are unchanged.
    fill=config.get('voidFill')
    if fill:
        px,py,pw,ph=fill['patch']
        patch={k:tile_world[k][py-oy:py-oy+ph,px:px+pw].copy()for k in LAYERS}
        if not (known[py-oy:py-oy+ph,px:px+pw].all() and not walk[py-oy:py-oy+ph,px:px+pw].any()):raise ValueError('Void fill patch is not solid forest')
        for mid,(x0,y0,x1,y1)in boxes.items():
            for wy in range(y0,y1):
                for wx in range(x0,x1):
                    r=wy-oy
                    if known[r,wx]:continue
                    for k in LAYERS:tile_world[k][r,wx]=patch[k][(wy-py)%ph,(wx-px)%pw]
                    known[r,wx]=True;walk[r,wx]=False
    # Dirt and grass use the same rule: the shared outer face must be reachable
    # from the town/start component on both sides. Do not open detached pockets.
    portal_walk=np.zeros_like(walk)
    for mid,(x0,y0,x1,y1)in boxes.items():
        region=np.s_[y0-oy:y1-oy,x0:x1]
        labels,_=ndimage.label(walk[region]);counts=np.bincount(labels.ravel());counts[0]=0
        source=sources[mid]
        refs=[o for o in source['objects']if o['kind']=='spawn'and o.get('id')=='S0']or[o for o in source['objects']if o['kind']=='market']or[o for o in source['objects']if o['kind']=='talker']
        main=0
        if refs:
            xx=refs[0]['x']+source['terrainX']-x0;yy=refs[0]['y']+source['terrainY']-y0
            if 0<=xx<x1-x0 and 0<=yy<y1-y0:main=int(labels[yy,xx])
        if not main:main=int(counts.argmax())
        portal_walk[region]=labels==main
    boundary=seams(owners,portal_walk,ids,0,oy)
    # Reject each obstructed face independently: a tree behind the north face
    # must not close an otherwise clear east/west corner crossing.
    filter_boundary_faces(boundary,config.get('externalReviewPorts',[]),portal_walk,oy)
    for entries in boundary.values():
        for entry in entries:
            key='spawn' if entry['kind']=='exit' else 'id'
            entry[key]='t3_'+entry[key]
    moved=defaultdict(list);objects=defaultdict(list);extras=defaultdict(lambda:defaultdict(list))
    preserved=[];oldlinks=set();foreign=[]
    def owner(wx,wy):
        if not (0<=wx<width and oy<=wy<height+oy):raise ValueError('Object outside known domain')
        return ids[owners[wy-oy,wx]]
    def local(o,source,target):
        result=copy.deepcopy(o);tx,ty,_,_=boxes[target]
        result['x']+=source['terrainX']-tx;result['y']+=source['terrainY']-ty
        for i in range(0,len(result.get('path',[])),2):
            result['path'][i]+=source['terrainX']-tx;result['path'][i+1]+=source['terrainY']-ty
        return result
    for mid,d in sources.items():
        for o in d['objects']:
            wx,wy=d['terrainX']+o['x'],d['terrainY']+o['y']
            if o['kind']=='exit':
                oldlinks.add((mid,o['target']))
                if o['target']not in sources:foreign.append(dict(source=mid,exit=copy.deepcopy(o),world=[wx,wy]))
                continue
            if o['kind']=='spawn' and o.get('id')!='S0':continue
            if mid=='transit_crossroads' and o['kind']=='sign' and o.get('text','').startswith('↑ 북쪽 조각'):
                o=copy.deepcopy(o);o['x']=28-d['terrainX'];o['y']=-15-d['terrainY'];wx,wy=28,-15
            target=owner(wx,wy)
            if o['kind']not in MOVABLE and target!=mid:
                raise ValueError(f'Protected function moved between maps: {mid}/{o["kind"]}/{o.get("name")or o.get("id")} -> {target}')
            objects[target].append(local(o,d,target))
            if target!=mid:moved[target].append(dict(fromMap=mid,kind=o['kind'],world=[wx,wy]))
            if o['kind']not in MOVABLE:preserved.append(dict(mapId=mid,kind=o['kind'],id=o.get('npc')or o.get('town')or o.get('id')or o.get('name'),world=[wx,wy]))
        # Metadata rectangles may meet a scene border: intersect logical depth patches only.
        for patch in d.get('natureCrops',[]):
            wx,wy=d['terrainX']+patch['x'],d['terrainY']+patch['y']
            for target,(tx,ty,ex,ey)in boxes.items():
                xa,ya=max(wx,tx),max(wy,ty);xb,yb=min(wx+patch['w'],ex),min(wy+patch['h'],ey)
                if xa<xb and ya<yb:extras[target]['natureCrops'].append(dict(x=xa-tx,y=ya-ty,w=xb-xa,h=yb-ya))
        for key in ('obstacles','terrainFloods'):
            for extra in d.get(key,[]):
                q=copy.deepcopy(extra);tx,ty,_,_=boxes[mid]
                if key=='terrainFloods':
                    for cell in q['cells']:cell['x']+=d['terrainX']-tx;cell['y']+=d['terrainY']-ty
                else:
                    q['x']+=d['terrainX']-tx;q['y']+=d['terrainY']-ty
                extras[mid][key].append(q)
        if d.get('nativeLogs'):extras[mid]['nativeLogs']=copy.deepcopy(d['nativeLogs'])
    external=[]
    for port in config.get('externalReviewPorts',[]):
        mid=port['mapId'];d=sources[mid];tx,ty,_,_=boxes[mid];target=port['target']
        old=next(e['exit']for e in foreign if e['source']==mid and e['exit']['target']==target)
        wx,wy=port['world'];w,h=port['size'];sx,sy=port['arrivalWorld']
        objects[mid].append(O('exit',wx-tx,wy-ty,w=w,h=h,target=target,spawn=old['spawn'],edge=port['edge']))
        oldspawn=next(o for o in d['objects']if o['kind']=='spawn'and o.get('id')=='link_'+target)
        if (w,h)!=(oldspawn['w'],oldspawn['h']):raise ValueError('External incoming span changed')
        objects[mid].append(O('spawn',sx-tx,sy-ty,w=w,h=h,id=oldspawn['id'],edge=port['edge']))
        external.append(dict(target=target,oldWorld=[d['terrainX']+old['x'],d['terrainY']+old['y']],world=[wx,wy],incomingSpawn=oldspawn['id'],incomingWorld=[sx,sy],size=[w,h]))
    tx,ty,_,_=boxes['transit_crossroads']
    objects['transit_crossroads'].extend([O('sign',7-tx,-4-ty,text='← 대륙 한가운데 초원\n서쪽 길 위쪽 출구'),O('sign',7-tx,4-ty,text='← 소나무 고원\n서쪽 길 아래쪽 출구')])
    maps={};metrics=[]
    for idx,mid in enumerate(ids):
        x0,y0,x1,y1=boxes[mid];w,h=x1-x0,y1-y0;region=np.s_[y0-oy:y1-oy,x0:x1]
        palette=[''];lookup={'':0}
        def P(name):
            if name not in lookup:lookup[name]=len(palette);palette.append(name)
            return lookup[name]
        data=dict(id=mid,name=sources[mid]['name'],w=w,h=h,hours=sources[mid]['hours'],legacyHeight=h,terrainX=x0,terrainY=y0,previewOnly=True)
        for layer in LAYERS:data[layer]=[P(n)for n in tile_world[layer][region].ravel()]
        data.update(palette=palette,road=road[region].ravel().tolist(),blocked=(~walk[region]).astype(int).ravel().tolist(),water=water[region].astype(int).ravel().tolist())
        objs=objects[mid]
        for o in boundary[mid]:
            xx,yy=o['world'];q={k:v for k,v in o.items()if k!='world'};q.update(x=xx-x0,y=yy-y0);objs.append(O(q.pop('kind'),q.pop('x'),q.pop('y'),**q))
        # Safe default for each editor scene without a pre-existing S0.
        label,n=ndimage.label(walk[region]);counts=np.bincount(label.ravel());counts[0]=0
        exits={(o['x']+dx,o['y']+dy)for o in objs if o['kind']=='exit'for dx in range(o.get('w',1))for dy in range(o.get('h',1))}
        refs=[o for o in objs if o['kind']=='market']or[o for o in objs if o['kind']=='talker']
        if not any(o['kind']=='spawn'and o.get('id')=='S0'for o in objs):
            main=int(label[refs[0]['y'],refs[0]['x']])if refs else int(counts.argmax())
            if not main:main=int(counts.argmax())
            yy,xx=np.nonzero((road[region]>0)&(label==main))
            if not len(xx):yy,xx=np.nonzero(label==main)
            center=np.array([refs[0]['x'],refs[0]['y']])if refs else np.array([w/2,h/2])
            candidates=sorted(zip(xx,yy),key=lambda p:sum((np.array(p)-center)**2))
            occupied=[(o['x'],o['y'])for o in objs if o['kind']in ('market','board','npc','talker','worker','villager','walker','workshop','pastime')]
            xx,yy=next((int(x),int(y))for x,y in candidates if (x,y)not in exits and all(abs(x-ox)+abs(y-oy_)>=2 for ox,oy_ in occupied))
            objs.append(O('spawn',xx,yy,id='S0'))
        data['objects']=objs;data.update(extras[mid]);maps[mid]=data
        newlinks={o['target']for o in objs if o['kind']=='exit'}
        metrics.append(dict(mapId=mid,name=data['name'],oldBox=[sources[mid]['terrainX'],sources[mid]['terrainY'],sources[mid]['w'],sources[mid]['h']],box=[x0,y0,w,h],oldWalkable=sum(not b for b in sources[mid]['blocked']),walkable=sum(not b for b in data['blocked']),exits=sum(o['kind']=='exit'for o in objs),targets=sorted(newlinks),components=int(n),largestComponent=int(counts.max()),externalExitObjects=sum(e['source']==mid for e in foreign)))
    newlinks={(mid,o['target'])for mid,d in maps.items()for o in d['objects']if o['kind']=='exit'}
    report=dict(status='review only; no production/save migration',knownGroundCells=int(known.sum()),walkableCells=int(walk.sum()),maps=metrics,preservedFunctions=preserved,movedDecorativeObjects=dict(moved),removedLinks=sorted(oldlinks-newlinks),addedLinks=sorted(newlinks-oldlinks),foreignBaseline=foreign,externalReviewPorts=external,notes=['Existing three foreign links/target spawns retained. Other region assets untouched; foreign ports load their existing registered scenes in the isolated SaveOFF preview.','Depth-patch rectangles are intersected at scene borders; native artwork tiles are unchanged.'])
    covered=np.zeros_like(known)
    for mid,(x0,y0,x1,y1)in boxes.items():covered[y0-oy:y1-oy,x0:x1]=True
    report['trimmedReviewCells']=[[int(x),int(y+oy)]for y,x in zip(*np.nonzero(known&~covered))]
    report['trimmedWalkableCells']=int((walk&~covered).sum())
    report['trimNote']='Review-only: west tip is shortened by one column to fit two separate existing foreign portals on the same true outer face. No new land or foreign file changes.'
    surfaces=Counter();samples=[];actual={}
    for mid,data in maps.items():
        for e in data['objects']:
            if e['kind']!='exit':continue
            for dy in range(e['h']):
                for dx in range(e['w']):
                    wx,wy=data['terrainX']+e['x']+dx,data['terrainY']+e['y']+dy
                    actual[mid,wx,wy,e['edge']]=e['target']
                    if e['target']not in maps:continue
                    offroad=road[wy-oy,wx]==0
                    surfaces['offRoadExitCells'if offroad else'roadExitCells']+=1
                    ddx,ddy={'W':(-1,0),'E':(1,0),'N':(0,-1),'S':(0,1)}[e['edge']]
                    clear_meadow=all(0<=xx<width and 0<=yy<height and portal_walk[yy,xx]and road[yy,xx]==0 and not water[yy,xx]and not tile_world['obj'][yy,xx]and not tile_world['high'][yy,xx]for xx,yy in ((wx-ddx*k,wy-oy-ddy*k)for k in range(3)))
                    if offroad and clear_meadow:
                        samples.append(dict(mapId=mid,target=e['target'],edge=e['edge'],x=e['x']+dx,y=e['y']+dy,spawn=e['spawn'],span=e['w']*e['h']))
    uncovered=[];reserved_local=[]
    for y,x in zip(*np.nonzero(portal_walk)):
        for edge,dy,dx in (('N',-1,0),('S',1,0),('W',0,-1),('E',0,1)):
            ny,nx=y+dy,x+dx
            if not(0<=ny<height and 0<=nx<width)or not portal_walk[ny,nx]or owners[y,x]==owners[ny,nx]:continue
            if not all(0<=ay<height and 0<=ax<width and portal_walk[ay,ax]for ay,ax in ((y-dy,x-dx),(y+2*dy,x+2*dx))):continue
            mid=ids[owners[y,x]]
            opposite_gate=any(p['mapId']==ids[owners[ny,nx]]and {'N':'S','S':'N','W':'E','E':'W'}[p['edge']]==edge
                and p['world'][0]<=nx<p['world'][0]+p['size'][0]and p['world'][1]<=ny+oy<p['world'][1]+p['size'][1]for p in config['externalReviewPorts'])
            if opposite_gate:
                reserved_local.append(dict(mapId=mid,world=[int(x),int(y+oy)],edge=edge));continue
            if (mid,int(x),int(y+oy),edge)not in actual:uncovered.append(dict(mapId=mid,world=[int(x),int(y+oy)],edge=edge))
    report.update(portalPolicy='reachable grass and dirt at actual outer faces; safe approach/arrival; foreign gateway strips excluded from local crossings in both directions',surfaceExitCells=dict(surfaces),uncoveredBoundaryCells=uncovered,reservedLocalFaces=reserved_local)
    preferred=sorted(samples,key=lambda s:(s['mapId']!='Naru',s['target']!='goryeo_jangdan',s['edge']!='W',s['span']!=1,s['y'],s['x']))
    report['grassSample']=preferred[0]if preferred else None
    return maps,report,tile_world,owners,known

def main():
    config,sources=read_sources();maps,report,*_=generate(config,sources)
    (OUT/'Maps').mkdir(parents=True,exist_ok=True);REVIEW.mkdir(parents=True,exist_ok=True);VALIDATION.mkdir(parents=True,exist_ok=True)
    for mid,d in maps.items():(OUT/'Maps'/f'{mid}.json').write_text(json.dumps(d,ensure_ascii=False,separators=(',',':')))
    report['inputs']={mid:hashlib.sha256(json.dumps(d,sort_keys=True,ensure_ascii=False).encode()).hexdigest()for mid,d in sources.items()}
    (VALIDATION/'geometry.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:report[k]for k in('knownGroundCells','walkableCells','maps','removedLinks','addedLinks')},ensure_ascii=False,indent=2))
if __name__=='__main__':main()
