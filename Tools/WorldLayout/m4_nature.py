"""Whole approved nature overlays, called from the layer generator before M4 export.

Layout and role masks are source data; no generated map is edited. No resizing,
rotation or color conversion. The source PNG is composed at its native size.
"""
from pathlib import Path
import hashlib,json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
T=16
LAYOUT=Path(__file__).with_name('nature_layout.json')
LIVE=Path(__file__).with_name('nature_live.json')  # Created only after placement approval.
TREES=('forest_','pine_','round_','broadleaf','light_tree','golden_tree','willow','moss_rock','small_rock','shrub','fallen_log','tall_grass')
def rgba(path):return Image.open(path).convert('RGBA')

def preserve_objects(new_seams,walk,origin=(0,-130)):
    """Retain current function/exit/spawn IDs; name newly opened boundary cells separately."""
    import copy
    import m4_objects
    ox,oy=origin
    current={mid:json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text()) for mid in new_seams}
    result={mid:copy.deepcopy(d['objects']) for mid,d in current.items()}
    old_exit_cells={mid:set() for mid in current}
    relocations=[]
    def open_cell(x,y):
        iy,ix=y-oy,x-ox
        return 0<=iy<walk.shape[0] and 0<=ix<walk.shape[1] and bool(walk[iy,ix])
    def all_open(o,d):
        return all(open_cell(d['terrainX']+o['x']+dx,d['terrainY']+o['y']+dy) for dy in range(o.get('h',1)) for dx in range(o.get('w',1)))
    # User 2026-10-29: retain the large waterfall and move interfering entrances/arrivals.
    for mid,d in current.items():
        for o in result[mid]:
            if o['kind']!='exit' or o['target'] not in current:continue
            target=o['target'];td=current[target]
            arrival=next((s for s in result[target] if s['kind']=='spawn' and s.get('id')==o['spawn']),None)
            if arrival is None:raise ValueError('Missing original arrival: '+o['spawn'])
            if all_open(o,d) and all_open(arrival,td):continue
            choices=[];wx,wy=d['terrainX']+o['x'],d['terrainY']+o['y']
            for candidate in new_seams[mid]:
                if candidate['kind']!='exit' or candidate['target']!=target:continue
                cx,cy=candidate['world'];dx=min(max(wx-cx,0),candidate.get('w',1)-1);dy=min(max(wy-cy,0),candidate.get('h',1)-1)
                distance=abs(cx+dx-wx)+abs(cy+dy-wy)
                choices.append((distance,0 if candidate['edge']==o['edge'] else 1,cx+dx,cy+dy,candidate,dx,dy))
            if not choices:raise ValueError('No safe adjacent boundary for original exit: '+mid+' '+o['spawn'])
            _,_,nx,ny,candidate,dx,dy=min(choices,key=lambda q:q[:4])
            if abs(nx-wx)+abs(ny-wy)>12:raise ValueError('Entrance relocation is not local: '+mid+' '+o['spawn'])
            spawn=next(s for s in new_seams[target] if s['kind']=='spawn' and s.get('id')==candidate['spawn'])
            sx,sy=spawn['world'][0]+dx,spawn['world'][1]+dy
            old=dict(exit=[wx,wy],arrival=[td['terrainX']+arrival['x'],td['terrainY']+arrival['y']])
            o.update(x=nx-d['terrainX'],y=ny-d['terrainY'],w=1,h=1,edge=candidate['edge'])
            arrival.update(x=sx-td['terrainX'],y=sy-td['terrainY'],w=1,h=1,edge=candidate['edge'])
            relocations.append(dict(map=mid,target=target,spawnId=o['spawn'],old=old,new=dict(exit=[nx,ny],arrival=[sx,sy])))
    for mid,d in current.items():
        for o in result[mid]:
            if o['kind'] not in ('exit','spawn'):continue
            for dy in range(o.get('h',1)):
                for dx in range(o.get('w',1)):
                    x,y=d['terrainX']+o['x']+dx,d['terrainY']+o['y']+dy
                    if not open_cell(x,y):raise ValueError('Nature blocks existing exit/arrival: '+mid+' '+str((x,y)))
                    if o['kind']=='exit':old_exit_cells[mid].add((x,y,o['target']))
    added=0
    for mid,seams in new_seams.items():
        d=current[mid]
        for e in seams:
            if e['kind']!='exit':continue
            target=e['target'];td=current[target]
            spawn=next(s for s in new_seams[target] if s['kind']=='spawn' and s.get('id')==e['spawn'])
            for dy in range(e.get('h',1)):
                for dx in range(e.get('w',1)):
                    x,y=e['world'][0]+dx,e['world'][1]+dy
                    if (x,y,target) in old_exit_cells[mid]:continue
                    sx,sy=spawn['world'][0]+dx,spawn['world'][1]+dy
                    if not open_cell(sx,sy):raise ValueError('New arrival is blocked')
                    name=f'nature_{mid}_{x}_{y}_{e["edge"]}'
                    result[mid].append(m4_objects.O('exit',x-d['terrainX'],y-d['terrainY'],target=target,spawn=name,edge=e['edge']))
                    result[target].append(m4_objects.O('spawn',sx-td['terrainX'],sy-td['terrainY'],id=name,edge=e['edge']))
                    old_exit_cells[mid].add((x,y,target));added+=1
    return result,added,relocations

def load_layout(path=LAYOUT,production=False):
    config=json.loads(Path(path).read_text())
    if config.get('schemaVersion')!=1:raise ValueError('Nature layout schema')
    if production and config.get('status')!='approved':raise ValueError('Nature placement approval required before production')
    catalogue=json.loads((ROOT/'ArtReview/Approved/MountainRange-Final-v2/catalogue.json').read_text())['assets']
    assets={}
    profiles=json.loads(LAYOUT.with_name('nature_masks.json').read_text())['assets']
    for directory in ['MountainRange-Final-v2','RiceField-v5','RockMountain-v1']:
        base=ROOT/'ArtReview/Approved'/directory;approval=json.loads((base/'approval.json').read_text())
        if approval.get('status')!='approved':raise ValueError('Nature art is not approved')
        for item in approval['assets']:
            p=base/(item['name']+'.png');im=rgba(p)
            if list(im.size)!=item['size'] or hashlib.sha256(im.tobytes()).hexdigest()!=item['rgbaSha256']:raise ValueError('Approved nature PNG changed: '+str(p))
            assets[item['name']]={'image':im,'path':str(p.relative_to(ROOT)),'ports':catalogue.get(item['name'],{}).get('ports',{})}
            if item['name'] in profiles:
                profile=profiles[item['name']]
                if profile.get('sourceRGBA')!=item['rgbaSha256']:raise ValueError('Logical mask belongs to a different approved image')
                for field in ('blocked','water','above'):
                    rows=profile[field]
                    if len(rows)!=im.height//T or any(len(row)!=im.width//T or set(row)-{'0','1'} for row in rows):raise ValueError('Invalid authored nature mask')
                assets[item['name']]['mask']=profile
    draft=ROOT/'ArtReview/A7d-20261030/Headwaters/catalogue.json'
    if draft.exists():
        d=json.loads(draft.read_text())
        needed={p['asset'] for p in config['placements'] if p.get('enabled',True)}
        for item in d['assets']:
            if item['name'] not in needed:continue
            if production and d['status']!='approved':raise ValueError('Headwater artwork approval required')
            p=ROOT/item['path'];im=rgba(p)
            if list(im.size)!=item['size'] or hashlib.sha256(im.tobytes()).hexdigest()!=item['rgbaSha256']:raise ValueError('Headwater source changed')
            assets[item['name']]={'image':im,'path':item['path'],'ports':{},'mask':item['mask']}
    placements={}
    for p in config['placements']:
        if not p.get('enabled',True):continue
        if p['id'] in placements or p['asset'] not in assets or 'mask' not in assets[p['asset']] or p['role'] not in ('rock','water-rock','rice'):raise ValueError('Invalid nature placement')
        if any(type(p[k]) is not int for k in ('x','y')):raise ValueError('Native placement must align to the tile grid')
        placements[p['id']]=p
    for c in config['connections']:
        if not c.get('enabled',True):continue
        a,b=placements[c['a']],placements[c['b']];ap=assets[a['asset']]['ports'][c['portA']];bp=assets[b['asset']]['ports'][c['portB']]
        if ap['template']!=bp['template'] or c['portA']+c['portB'] not in ('EW','WE','NS','SN'):raise ValueError('Nature port type mismatch')
        if (a['x']*T+ap['x'],a['y']*T+ap['y'])!=(b['x']*T+bp['x'],b['y']*T+bp['y']):raise ValueError('Disconnected nature ports')
    return config,assets

def apply_layers(folder,forest_masks,origin=(0,-130),production=False,path=LAYOUT):
    import numpy as np
    from scipy import ndimage
    folder=Path(folder);config,assets=load_layout(path,production=production)
    record=json.loads((folder/'parts.json').read_text());parts=record['parts'];sources=record['sources'];cache={}
    with np.load(folder/'masks.npz') as z:masks={k:z[k].copy() for k in z.files}
    h,w=masks['blocked'].shape;ox,oy=origin;clear=np.zeros((h,w),bool)
    for z in config['clearings']:
        if not z.get('enabled',True):continue
        x,y=z['x']-ox,z['y']-oy
        if x<0 or y<0 or x+z['w']>w or y+z['h']>h:raise ValueError('Clearing outside world')
        if z.get('contour'):
            shape=np.zeros_like(clear)
            for p in config['placements']:
                if not p.get('enabled',True) or p['role']=='rice':continue
                a=assets[p['asset']]['image'];px,py=p['x']-ox,p['y']-oy
                alpha=np.array(a.getchannel('A')).reshape(a.height//T,T,a.width//T,T).max((1,3))>0
                shape[py:py+alpha.shape[0],px:px+alpha.shape[1]]|=alpha
            clear[y:y+z['h'],x:x+z['w']]|=ndimage.binary_dilation(shape,iterations=1)[y:y+z['h'],x:x+z['w']]
        else:clear[y:y+z['h'],x:x+z['w']]=True
    clear &= masks['own']
    # Identify original instances by source+world anchor, then remove each whole instance.
    instances={}
    for key,items in parts.items():
        layer,y,x=key.split(':');y,x=int(y),int(x)
        for sid,dx,dy in items:
            if not sid.startswith('sprite:') or not sid.split(':')[1].startswith(TREES):continue
            k=(sid,x*T+dx,y*T+dy)
            if sid not in cache:cache[sid]=rgba(ROOT/sources[sid])
            instances[k]=cache[sid]
    removed=set();affected=set();removed_trunks=np.zeros((h,w),bool);shadow_candidates=clear.copy();canopy=np.zeros((h,w),bool)
    for k,im in instances.items():
        sid,px,py=k;x,y=px//T,py//T;sw,sh=im.width//T,im.height//T
        xa,ya,xb,yb=max(0,x),max(0,y),min(w,x+sw),min(h,y+sh)
        alpha=np.array(im.getchannel('A')).reshape(sh,T,sw,T).max((1,3))>0
        if xa<xb and ya<yb and (clear[ya:yb,xa:xb]&alpha[ya-y:yb-y,xa-x:xb-x]).any():
            removed.add(k)
            shadow_candidates[ya:yb,xa:xb]=True
            foot=y+sh-1
            if 0<=foot<h:
                if sid.split(':')[1].startswith(('shrub','small_rock','moss_rock','fallen_log','tall_grass')):removed_trunks[foot,max(0,x):min(w,x+sw)]=True
                else:removed_trunks[foot,max(0,x+(sw-1)//2):min(w,x+sw//2+1)]=True
        elif xa<xb and ya<yb:
            canopy[ya:yb,xa:xb]|=alpha[ya-y:yb-y,xa-x:xb-x]
    canopy_interior=ndimage.binary_erosion(canopy,iterations=1)
    for key,items in list(parts.items()):
        layer,y,x=key.split(':');y,x=int(y),int(x)
        kept=[q for q in items if (q[0],x*T+q[1],y*T+q[2]) not in removed]
        if kept!=items:parts[key]=kept;affected.add(key)
        if layer=='ground' and (clear[y,x] or (shadow_candidates[y,x] and not canopy_interior[y,x])) and any(q[0].startswith('fill:') for q in items):
            parts[key]=[['tile:floor:0:12',0,0]];affected.add(key)
    from m1_tiles_preview import INDEX,SKIN
    sources['tile:floor:0:12']=INDEX['tiles']['floor:0:12@'+SKIN]['path']
    forest=np.zeros((h,w),bool)
    for mask in forest_masks.values():forest|=mask
    protected=masks['water']|(masks['grade']>0)|masks['deck']
    # Clear the old forest barrier only where vegetation is replaced; reinforce remaining trunks.
    masks['blocked']&=~(((clear|shadow_candidates&~canopy)&forest|removed_trunks)&~protected)
    for k,im in instances.items():
        if k in removed or k[0].split(':')[1].startswith('tall_grass'):continue
        sid,px,py=k;x,y=px//T,py//T;sw,sh=im.width//T,im.height//T;foot=y+sh-1
        if 0<=foot<h:masks['blocked'][foot,max(0,x+(sw-1)//2):min(w,x+sw//2+1)]=True
    # Authored paddy clearings contain no protected roads/water or functional props.
    rice_ids={p['id']for p in config['placements']if p.get('enabled',True)and p['role']=='rice'}
    for z in config['clearings']:
        if z['id']not in rice_ids:continue
        x,y=z['x']-ox,z['y']-oy;view=np.s_[y:y+z['h'],x:x+z['w']]
        masks['blocked'][view]&=protected[view]
    placements=[];new_water=np.zeros((h,w),bool);rock=np.zeros((h,w),bool)
    for p in config['placements']:
        if not p.get('enabled',True):continue
        a=assets[p['asset']];im=a['image'];x,y=p['x']-ox,p['y']-oy;sw,sh=im.width//T,im.height//T
        if x<0 or y<0 or x+sw>w or y+sh>h:raise ValueError('Nature outside world')
        arr=np.array(im);occupied=arr[:,:,3].reshape(sh,T,sw,T).max((1,3))>0
        if (occupied&~masks['own'][y:y+sh,x:x+sw]).any():raise ValueError('Nature touches another team region: '+p['id'])
        profile=a['mask'];mask=lambda field:np.array([[v=='1' for v in row] for row in profile[field]],bool)
        collision,wet,above=mask('blocked'),mask('water'),mask('above')
        if p['role']=='rice':
            if protected[y:y+sh,x:x+sw].any() or masks['blocked'][y:y+sh,x:x+sw].any():raise ValueError('Rice overlaps road/water/barrier: '+p['id'])
        else:
            if (occupied & (masks['grade'][y:y+sh,x:x+sw]>0)).any():raise ValueError('Mountain covers existing road: '+p['id'])
            rock[y:y+sh,x:x+sw]|=collision
        if p['role']=='water-rock':
            if profile.get('replaceWater'):
                old_water=masks['water'][y:y+sh,x:x+sw].copy()
                masks['blocked'][y:y+sh,x:x+sw]&=~(old_water&occupied&~wet)
                masks['water'][y:y+sh,x:x+sw]=np.where(occupied,wet,old_water)
            new_water[y:y+sh,x:x+sw]|=wet
        sid='nature:'+p['asset'];sources[sid]=a['path'];cache[sid]=im
        for dy in range(sh):
            for dx in range(sw):
                layer='high' if above[dy,dx] else 'obj'
                key=f'{layer}:{y+dy}:{x+dx}';parts.setdefault(key,[]).append([sid,-dx*T,-dy*T]);affected.add(key)
        placements.append(dict(p,size=[sw,sh]))
    masks['water']|=new_water;masks['blocked']|=rock|new_water
    masks['blocked']&=masks['own']
    # Vegetation overhangs that became walkable must follow the new barrier split.
    moved=[]
    for key,items in list(parts.items()):
        layer,y,x=key.split(':');y,x=int(y),int(x)
        if layer not in ('obj','high'):continue
        kept=[]
        for q in items:
            sid,dx,dy=q;instance=(sid,x*T+dx,y*T+dy)
            if instance in instances and instance not in removed:
                im=instances[instance];foot=(y*T+dy)//T+im.height//T-1
                target='obj' if masks['blocked'][y,x] or foot<=y else 'high'
                if target!=layer:
                    moved.append((f'{target}:{y}:{x}',q));affected.add(key);continue
            kept.append(q)
        parts[key]=kept
    for key,q in moved:parts.setdefault(key,[]).append(q);affected.add(key)
    # Recompose changed cells from the full approved source images, preserving the source ledger.
    layers={k:rgba(folder/(k+'.png')) for k in ('ground','deco','obj','high')}
    for key in affected:
        layer,y,x=key.split(':');y,x=int(y),int(x);cell=Image.new('RGBA',(T,T),(28,34,40,255) if layer=='ground' else (0,0,0,0))
        for sid,dx,dy in parts[key]:
            if sid not in cache:cache[sid]=rgba(ROOT/sources[sid])
            src=cache[sid];sx,sy=max(0,-dx),max(0,-dy);tx,ty=max(0,dx),max(0,dy)
            if sx<src.width and sy<src.height and tx<T and ty<T:cell.alpha_composite(src,dest=(tx,ty),source=(sx,sy,min(src.width,sx+T-tx),min(src.height,sy+T-ty)))
        layers[layer].paste(cell,(x*T,y*T))
    for k,im in layers.items():im.save(folder/(k+'.png'))
    result=Image.new('RGBA',layers['ground'].size)
    for im in layers.values():result.alpha_composite(im)
    result.save(folder/'composite.png');np.savez_compressed(folder/'masks.npz',**masks)
    (folder/'parts.json').write_text(json.dumps(record,ensure_ascii=False))
    report=dict(status=config['status'],placements=placements,removedWholeVegetation=len(removed),changedRenderCells=len(affected),newWaterCells=int(new_water.sum()),rockCells=int(rock.sum()),sourceLayout=str(Path(path).relative_to(ROOT)),layoutSha256=hashlib.sha256(Path(path).read_bytes()).hexdigest())
    (folder/'nature.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    return report
