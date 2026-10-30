"""Read-only approved nature binding audit. No production regeneration."""
from pathlib import Path
import hashlib,json,sys,tempfile
import numpy as np
from PIL import Image
from scipy.signal import correlate2d
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Validation/Nature-Readiness-2026-10-29'
sys.path[:0]=[str(ROOT/'Tools/WorldLayout'),str(ROOT/'Tools/MapGen')]
import m4_certify,asset_manifest,terrain_v3 as terrain
def rgba(p):return Image.open(p).convert('RGBA')
def sha(im):return hashlib.sha256(im.tobytes()).hexdigest()
def main():
    OUT.mkdir(parents=True,exist_ok=True)
    assets=[];results=[]
    for family,folder in [('mountain','MountainRange-Final-v2'),('rice','RiceField-v5')]:
        base=ROOT/'ArtReview/Approved'/folder;approval=json.loads((base/'approval.json').read_text());assert approval['status']=='approved'
        for a in approval['assets']:
            p=base/(a['name']+'.png');s=p.with_suffix('.aseprite');im=rgba(p)
            assert list(im.size)==a['size'] and sha(im)==a['rgbaSha256'],p
            assert hashlib.sha256(s.read_bytes()).hexdigest()==a['sourceSha256'],s
            assets.append((family,a['name'],p,s,base/'approval.json'))
    for folder in ['C7-Batch2-v1','C7-Batch3-v1']:
        base=ROOT/'ArtReview/Approved'/folder
        for a in json.loads((base/'approval.json').read_text())['assets']:
            p=ROOT/a['image'];s=ROOT/a['source'];im=rgba(p)
            assert a['status']=='approved' and list(im.size)==a['rgbaEvidence']['size'] and sha(im)==a['rgbaEvidence']['rgbaSHA256'] and s.exists(),p
            assets.append(('forest',p.stem,p,s,base/'approval.json'))
    production=json.loads(m4_certify.MANIFEST.read_text());errors,production_info=m4_certify.verify();assert not errors,errors[:5]
    # Exercise A2 full-image registration and the M4 decoded-pixel compositor in disposable output.
    with tempfile.TemporaryDirectory(prefix='scratch-',dir=OUT) as td:
        td=Path(td);entries=[];manifest={'sources':{},'tiles':{}};unique_new=set()
        existing={sha(rgba(p)) for p in (ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles').glob('*.png')}
        for family,name,p,s,evidence in assets:
            aid=family+'_'+name.replace('-','_');im=rgba(p);w,h=im.size;assert w%16==h%16==0
            sid='readiness:'+aid;manifest['sources'][sid]={'path':str(p.relative_to(ROOT)),'sha256':sha(im),'approval':str(evidence.relative_to(ROOT))}
            tiles=[]
            for y in range(h//16):
                for x in range(w//16):
                    key=f'{aid}_{x}_{y}';piece=im.crop((x*16,y*16,x*16+16,y*16+16));file=td/(key+'.png');piece.save(file)
                    if family!='forest' and sha(piece) not in existing:unique_new.add(sha(piece))
                    tiles.append({'tile':key,'image':str(file.relative_to(ROOT)),'dx':x,'dy':y,'layer':'obj'})
                    manifest['tiles'][key]={'sha256':sha(piece),'layer':'obj','base':None,'parts':[[sid,-x*16,-y*16]]}
            entries.append({'id':aid,'kind':'object','purpose':'readiness: whole approved '+family+' role '+name,'source':{'kind':'ai_processed','path':str(s.relative_to(ROOT))},'review':{'status':'approved','evidence':str(evidence.relative_to(ROOT))},'profile':'native16','size':[w//16,h//16],'image':str(p.relative_to(ROOT)),'tiles':tiles})
            uses=[k for k,v in production['sources'].items() if v['path']==str(p.relative_to(ROOT))]
            certified_uses=sum(any(part[0] in uses for part in v['parts']) for v in production['tiles'].values())
            results.append({'family':family,'name':name,'sizePixels':[w,h],'renderFootprintCells':[w//16,h//16],'approvalRGBAUnchanged':True,'asepriteExists':True,'asepriteFrozenHashChecked':family!='forest','currentProductionSourceIds':uses,'currentCertifiedCompositeUses':certified_uses})
        doc={'schemaVersion':1,'profiles':{'native16':{'pixelsPerCell':[16,16],'binaryAlpha':True}},'assets':entries}
        errors,_=asset_manifest.validate_catalogue(doc,ROOT);assert not errors,errors[:5]
        mp=td/'manifest.json';mp.write_text(json.dumps(manifest));errors,info=m4_certify.verify(mp,td);assert not errors,errors[:5]
        dry={'registeredWholeAssets':len(entries),'renderCells':info['tiles'],'a2Errors':0,'m4ComposeErrors':0,'distinctNewSourceCellImagesUpperBound':len(unique_new),'rawRGBAUpperBoundBytes':len(unique_new)*16*16*4,'note':'Source-cell upper bound only; map overlaps, actual placements, Unity overhead and atlas not measured. No production catalogue written.'}
    base=ROOT/'ArtReview/Approved/MountainRange-Final-v2';cat=json.loads((base/'catalogue.json').read_text())['assets'];joins=0
    for an,a in cat.items():
        for side,p in a['ports'].items():
            assert p['x']%16==p['y']%16==0
            if side not in ('E','S'):continue
            other='W' if side=='E' else 'N'
            for bn,b in cat.items():
                if other not in b['ports'] or p['template']!=b['ports'][other]['template']:continue
                q=b['ports'][other];dx,dy=p['x']-q['x'],p['y']-q['y'];ai=rgba(base/a['path']);bi=rgba(base/b['path']);ox,oy=-min(0,dx),-min(0,dy)
                combined=Image.new('RGBA',(max(ai.width,dx+bi.width)+ox,max(ai.height,dy+bi.height)+oy));combined.alpha_composite(ai,(ox,oy));combined.alpha_composite(bi,(dx+ox,dy+oy))
                template=rgba(base/'Templates'/(p['template']+'.png'));x,y=p['x']+ox,p['y']+oy
                actual=combined.crop((x-8,y,x+8,y+template.height) if side=='E' else (x,y-8,x+template.width,y+8))
                assert actual.size==template.size and actual.tobytes()==template.tobytes(),(an,bn,side);joins+=1
    # Conservative fit only: whole visible mountain cells must remain inside the old mountain zone.
    m=np.load(ROOT/'ArtReview/M1-Tiles/masks.npz');allowed=np.isin(m['terrain'],[terrain.MOUNTAIN,terrain.PEAK])&m['own']&~m['village'];fit={};map_hashes={}
    from m4_export import MAPID
    for key,mid in MAPID.items():
        path=ROOT/f'Assets/PocketCaravan/Maps/{mid}.json';d=json.loads(path.read_text());map_hashes[mid]=hashlib.sha256(path.read_bytes()).hexdigest()
        y0,x0=d['terrainY']-terrain.Y0,d['terrainX']-terrain.X0;h,w=d['h'],d['w'];owner=m['mapid'][y0:y0+h,x0:x0+w]==list(m['keys']).index(key)
        exclude=(np.array(d['road']).reshape(h,w)>0)|(np.array(d['water']).reshape(h,w)>0)
        for obj in d['objects']:
            if obj['kind'] in ('anim','exit','spawn'):continue
            x,y=obj.get('x',0),obj.get('y',0);exclude[max(0,y-1):min(h,y+obj.get('h',1)+1),max(0,x-1):min(w,x+obj.get('w',1)+1)]=True
        allowed[y0:y0+h,x0:x0+w]&=~(exclude&owner)
    for name,a in cat.items():
        im=np.array(rgba(base/a['path']))[:,:,3];h,w=im.shape;foot=(im.reshape(h//16,16,w//16,16).max((1,3))>0).astype(np.int32)
        n=correlate2d(allowed.astype(np.int32),foot,mode='valid');fit[name]=int((n==int(foot.sum())).sum())
    # Existing Jangdan free rectangle capacity is not a placement approval or a packing count.
    d=json.loads((ROOT/'Assets/PocketCaravan/Maps/goryeo_jangdan.json').read_text());h,w=d['h'],d['w'];arr=lambda k:np.array(d[k]).reshape(h,w)
    free=(arr('ground')>0)&(arr('blocked')==0)&(arr('water')==0)&(arr('road')==0)&(arr('obj')==0)&(arr('high')==0)
    for o in d['objects']:
        x,y=o.get('x',0),o.get('y',0);free[max(0,y-1):min(h,y+o.get('h',1)+1),max(0,x-1):min(w,x+o.get('w',1)+1)]=False
    count=int((correlate2d(free.astype(np.int32),np.ones((8,12),np.int32),mode='valid')==96).sum())
    report={'scope':'Read-only registration/composition/ports and conservative footprint feasibility; no production placement or Unity gameplay','assets':results,'productionCertificate':production_info,'dryRun':dry,'mountainCompatiblePortPairs':joins,'mountainWholeVisibleCellFitsInExistingMountainZone':fit,'mountainFitScope':'M1 mountain/peak only, village removed; current 13-map roads/water/function objects excluded. Counts overlap; not a connected placement or capacity proof. Water pieces need actual water route validation separately.','jangdanUnoccupied12x8RectangleCandidates':count,'mapInputSha256':map_hashes,'warnings':['Mountain port compatibility is not global geography/height/walkability validation.','Rice logical 8x4 field has 12x8 rendering footprint including canopy margins.','Current cliff modular minimum is not a ridge port schema; dry run registers each whole role as object only.','Rice location, crossing/collision and new layout still require an explicit preview decision.'],'productionWrites':0}
    (OUT/'audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({'assets':len(results),'dryRun':dry,'mountainPortPairs':joins,'fitCounts':fit,'riceRectangleCandidates':count},ensure_ascii=False))
if __name__=='__main__':main()
