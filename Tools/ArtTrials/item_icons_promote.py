"""Copy approved whole item icons unchanged; data registration is an explicit editor step."""
from pathlib import Path
import json,hashlib,shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];SOURCE=ROOT/'ArtReview/ItemIcons-v3-20261104';DEST=ROOT/'Assets/PocketCaravan/Art/Goryeo/Items'
def main():
 approval=json.loads((SOURCE/'approval.json').read_text());assert approval['status']=='approved'
 expected={a['id']:a['rgbaSha256']for a in approval['assets']};assert set(expected)=={'grain','transit_spice','transit_wool'}
 selections={n:(SOURCE/'Native'/(n+'.png'),digest,SOURCE/'approval.json')for n,digest in expected.items()}
 override=ROOT/'ArtReview/WoolWidth-v4-20261104/approval.json'
 if override.exists():
  approved=json.loads(override.read_text());assert approved['status']=='approved';assert [a['id']for a in approved['assets']]==['transit_wool']
  a=approved['assets'][0];selections[a['id']]=(ROOT/a['source'],a['rgbaSha256'],override)
 assets=[];DEST.mkdir(parents=True,exist_ok=True)
 for n,(src,digest,evidence) in selections.items():
  with Image.open(src)as im:
   im=im.convert('RGBA');assert im.size==(16,16)and set(im.getchannel('A').getdata())<={0,255};assert hashlib.sha256(im.tobytes()).hexdigest()==digest
  dest=DEST/(n+'.png');shutil.copyfile(src,dest);shutil.copyfile(src.with_suffix('.aseprite'),DEST/(n+'.aseprite'))
  assets.append(dict(id='goryeo_item_'+n,purpose='inventory icon: '+n,kind='object',size=[1,1],image=dest.relative_to(ROOT).as_posix(),profile='item16',source=dict(kind='authored',path=src.with_suffix('.aseprite').relative_to(ROOT).as_posix()),review=dict(status='approved',evidence=evidence.relative_to(ROOT).as_posix()),tiles=[dict(dx=0,dy=0,layer='obj',tile='goryeo_item_'+n,image=dest.relative_to(ROOT).as_posix())]))
 (DEST/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=dict(item16=dict(pixelsPerCell=[16,16],binaryAlpha=True)),assets=assets),ensure_ascii=False,indent=2)+'\n')
 print('Approved whole icons3 copied; existing shared grain.png preserved')
if __name__=='__main__':main()
