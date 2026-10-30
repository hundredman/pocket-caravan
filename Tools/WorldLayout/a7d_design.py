"""Author A7d source data: whole headwater sprites and irrigated Goryeo paddies."""
import json,hashlib
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
ART=ROOT/'ArtReview/A7d-20261030';ART.mkdir(parents=True,exist_ok=True)
def sha(im):return hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest()
def profile(im):
    a=np.array(im);h,w=a.shape[:2];occupied=a[:,:,3].reshape(h//16,16,w//16,16).max((1,3))>0
    wet=np.isin(a[:,:,:3].dot(np.array([65536,256,1])),[0x7fb9af,0x78a099,0xedf6e7])&(a[:,:,3]>0)
    wet=wet.reshape(h//16,16,w//16,16).max((1,3))
    rows=lambda m:[''.join('1'if v else'0'for v in row)for row in m]
    return dict(sourceRGBA=sha(im),blocked=rows(occupied),above=rows(occupied),water=rows(wet),replaceWater=True)
def main():
    d=json.loads((HERE/'nature_layout.json').read_text());d['description']='A7d: all headwaters and Goryeo granary review; whole native sprites, no source transforms.'
    assets=[]
    for name in ['compact-spring-west','compact-spring-east','compact-spring-west-tiny']:
        p=ART/'Headwaters/Native'/(name+'.png');im=Image.open(p).convert('RGBA')
        assets.append(dict(name=name,path=str(p.relative_to(ROOT)),size=list(im.size),rgbaSha256=sha(im),mask=profile(im)))
    (ART/'Headwaters/catalogue.json').write_text(json.dumps(dict(status='review',scope='new whole sprites; user approval pending',assets=assets),indent=2))
    masks=json.loads((HERE/'nature_masks.json').read_text());im=Image.open(ROOT/'ArtReview/Approved/RockMountain-v1/mountain.png').convert('RGBA');masks['assets']['mountain']=profile(im);masks['assets']['mountain']['replaceWater']=False
    (HERE/'nature_masks.json').write_text(json.dumps(masks,indent=2)+'\n')
    additions=[('west-headwater','compact-spring-west',64,-58),('capital-backpeak','mountain',227,-70),('capital-headwater','compact-spring-west-tiny',225,-68),('goryeo-headwater','compact-spring-east',182,-58)]
    for ident,asset,x,y in additions:
        p=next((a for a in assets if a['name']==asset),None);w,h=p['size'] if p else (192,160)
        d['placements'].append(dict(id=ident,asset=asset,x=x,y=y,role='rock' if asset=='mountain' else 'water-rock'))
        d['clearings'].append(dict(id=ident,x=x,y=y,w=w//16,h=h//16,contour=True))
    base=ROOT/'ArtReview/BuildingNatureBinding-20261029/BuildingLayers'
    with np.load(base/'masks.npz') as z:m={k:z[k].copy()for k in z.files}
    H,W=m['own'].shape;allowed=m['own']&~m['water']&(m['grade']==0)
    mine=[i for i,k in enumerate(m['keys'])if str(k).startswith('goryeo/')];allowed&=np.isin(m['mapid'],mine)
    # Protect nonvegetation props/buildings and existing functional locations.
    ledger=json.loads((base/'parts.json').read_text());hard=np.zeros((H,W),bool)
    for key,parts in ledger['parts'].items():
        layer,y,x=key.split(':')
        if layer=='ground'and any(q[0].startswith('fill:')for q in parts):hard[int(y),int(x)]=True
        if layer in ('obj','high')and any(q[0].startswith('sprite:c7:')for q in parts):hard[int(y),int(x)]=True
    for path in (ROOT/'Assets/PocketCaravan/Maps').glob('*.json'):
        if path.stem not in ['Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan']:continue
        md=json.loads(path.read_text())
        for o in md['objects']:
            if o['kind']in('exit','spawn','anim'):continue
            x,y=md['terrainX']+o['x'],md['terrainY']+o['y']+130
            hard[max(0,y-2):min(H,y+3),max(0,x-2):min(W,x+3)]=True
    allowed&=~hard
    water_distance=ndimage.distance_transform_edt(~m['water']);road_distance=ndimage.distance_transform_edt(m['grade']==0)
    occupied=np.zeros((H,W),bool)
    for p in d['placements']:
        if p['role']=='rice':occupied[p['y']+130:p['y']+138,p['x']:p['x']+12]=True
    goals=[('jangdan',202,44,10),('naru',236,52,7),('songak',190,-12,3),('gaegyeong',253,8,3)]
    counts={};farms=[]
    for name,cx,cy,quota in goals:
        candidates=[]
        for y in range(106,221,2): # World -24..90, avoiding northern uplands.
            for x in range(156,min(W-12,284),2):
                if not allowed[y:y+8,x:x+12].all():continue
                if np.unique(m['mapid'][y:y+8,x:x+12]).size!=1:continue
                if water_distance[y:y+8,x:x+12].min()<1.5:continue
                wx,wy=x+6,y-130+4
                if abs(wx-cx)>35 or abs(wy-cy)>32:continue
                if water_distance[y+4,x+6]>20 or road_distance[y+4,x+6]>16:continue
                score=((wx-cx)**2+(wy-cy)**2)+water_distance[y+4,x+6]*4
                candidates.append((score,x,y))
        count=0
        for _,x,y in sorted(candidates):
            if count>=quota:break
            if occupied[max(0,y-1):y+9,max(0,x-1):x+13].any():continue
            ident='paddy-'+name+'-'+str(count+1);p=dict(id=ident,asset='small-plot',x=x,y=y-130,role='rice')
            d['placements'].append(p);d['clearings'].append(dict(id=ident,x=x,y=y-130,w=12,h=8))
            occupied[y:y+8,x:x+12]=True;farms.append(p);count+=1
        counts[name]=count
    d['headwaters']=[dict(x=66,y=-48,placement='west-headwater'),dict(x=226,y=-62,placement='capital-headwater'),dict(x=191,y=-48,placement='goryeo-headwater'),dict(x=117,y=-9,placement='source-spring',connection=[116,-6])]
    (HERE/'nature_a7d.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
    (ART/'design-report.json').write_text(json.dumps(dict(newPaddyCounts=counts,totalRicePlots=2+len(farms),newHeadwaterAssets=assets),indent=2))
    print(counts,'total rice',2+len(farms))
if __name__=='__main__':main()
