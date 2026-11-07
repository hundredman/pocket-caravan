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

def read_sources():
    config=json.loads(CONFIG.read_text())
    if config['schemaVersion']!=1 or config['status']!='review':raise ValueError('Review source only')
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
    portal_walk=walk & ndimage.binary_dilation(road>0,iterations=1)
    # Keep the terrain intact. Reject portal cells with a tree/rock immediately
    # behind either side; a point-only contact test would miss this approach wall.
    for y,x in zip(*np.nonzero(portal_walk)):
        for dy,dx in ((0,1),(1,0)):
            ny,nx=y+dy,x+dx
            if ny>=height or nx>=width or owners[y,x]==owners[ny,nx] or not portal_walk[ny,nx]:continue
            approach=[(y-dy,x-dx),(ny+dy,nx+dx)]
            if not all(0<=ay<height and 0<=ax<width and walk[ay,ax] for ay,ax in approach):
                portal_walk[y,x]=False;portal_walk[ny,nx]=False
    for mid,override in config.get('boundsOverrides',{}).items():boxes[mid]=override
    boundary=seams(owners,portal_walk,ids,0,oy)
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
        metrics.append(dict(mapId=mid,name=data['name'],oldBox=[sources[mid]['terrainX'],sources[mid]['terrainY'],sources[mid]['w'],sources[mid]['h']],box=[x0,y0,w,h],oldWalkable=sum(not b for b in sources[mid]['blocked']),walkable=sum(not b for b in data['blocked']),exits=sum(o['kind']=='exit'for o in objs),targets=sorted(newlinks),components=int(n),largestComponent=int(counts.max()),foreignPending=sum(e['source']==mid for e in foreign)))
    newlinks={(mid,o['target'])for mid,d in maps.items()for o in d['objects']if o['kind']=='exit'}
    report=dict(status='review only; no production/save migration',knownGroundCells=int(known.sum()),walkableCells=int(walk.sum()),maps=metrics,preservedFunctions=preserved,movedDecorativeObjects=dict(moved),removedLinks=sorted(oldlinks-newlinks),addedLinks=sorted(newlinks-oldlinks),foreignBaseline=foreign,externalReviewPorts=external,notes=['Existing three foreign links/target spawns retained. Other region assets untouched; preview explorer stays within the 13 review maps.','Depth-patch rectangles are intersected at scene borders; native artwork tiles are unchanged.'])
    covered=np.zeros_like(known)
    for mid,(x0,y0,x1,y1)in boxes.items():covered[y0-oy:y1-oy,x0:x1]=True
    report['trimmedReviewCells']=[[int(x),int(y+oy)]for y,x in zip(*np.nonzero(known&~covered))]
    report['trimmedWalkableCells']=int((walk&~covered).sum())
    report['trimNote']='Review-only: west tip is shortened by one column to fit two separate existing foreign portals on the same true outer face. No new land or foreign file changes.'
    return maps,report,tile_world,owners,known

def main():
    config,sources=read_sources();maps,report,*_=generate(config,sources)
    (OUT/'Maps').mkdir(parents=True,exist_ok=True);REVIEW.mkdir(parents=True,exist_ok=True);VALIDATION.mkdir(parents=True,exist_ok=True)
    for mid,d in maps.items():(OUT/'Maps'/f'{mid}.json').write_text(json.dumps(d,ensure_ascii=False,separators=(',',':')))
    report['inputs']={mid:hashlib.sha256(json.dumps(d,sort_keys=True,ensure_ascii=False).encode()).hexdigest()for mid,d in sources.items()}
    (VALIDATION/'geometry.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:report[k]for k in('knownGroundCells','walkableCells','maps','removedLinks','addedLinks')},ensure_ascii=False,indent=2))
if __name__=='__main__':main()
