"""Evidence from native art and isolated inputs. No art approval, no live writes."""
from pathlib import Path
import hashlib,json,subprocess,sys,tempfile
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/E3-E2-Art-20261103'
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from asset_manifest import validate_catalogue
SIZES={'fallen_log':(48,48),'river_flood_high':(80,48),'river_flood_low':(80,48),'transit_spice':(16,16),'transit_wool':(16,16)}
def run():
 (OUT/'Cells').mkdir(exist_ok=True);assets=[];evidence=[];palette=set()
 with tempfile.TemporaryDirectory()as td:
  for name,size in SIZES.items():
   file=OUT/'Native'/(name+'.png')
   with Image.open(file)as im:im=im.convert('RGBA')
   assert im.size==size;assert set(im.getchannel('A').getdata())<={0,255}
   palette.update(c[:3]for c in im.getdata()if c[3]);tiles=[]
   for y in range(im.height//16):
    for x in range(im.width//16):
     p=OUT/'Cells'/f'{name}_{x}_{y}.png';im.crop((x*16,y*16,x*16+16,y*16+16)).save(p);tiles.append(dict(dx=x,dy=y,layer='obj',tile=f'e3_{name}_{x}_{y}',image=str(p.relative_to(ROOT))))
   assets.append(dict(id='e3_'+name,purpose='trade_item'if name.startswith('transit_')else 'road_hazard',kind='object',profile='native16',size=[im.width//16,im.height//16],image=str(file.relative_to(ROOT)),tiles=tiles,source=dict(kind='ai_processed',path='Tools/ArtTrials/e3_e2_native.lua'),review=dict(status='pending')))
   back=Path(td)/(name+'.png');subprocess.run(['/Applications/Aseprite.app/Contents/MacOS/aseprite','--batch',str(file.with_suffix('.aseprite')),'--save-as',str(back)],check=True)
   with Image.open(back)as rebuilt:assert rebuilt.convert('RGBA').tobytes()==im.tobytes()
   evidence.append(dict(id=name,size=size,colors=len({c for c in im.getdata()if c[3]}),binaryAlpha=True,asepriteRGBAEqual=True,rgba=hashlib.sha256(im.tobytes()).hexdigest()))
 doc=dict(schemaVersion=1,profiles=dict(native16=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=['#%02x%02x%02x'%c for c in sorted(palette)])),assets=assets)
 (OUT/'catalogue.json').write_text(json.dumps(doc,ensure_ascii=False,indent=2)+'\n');errors,_=validate_catalogue(doc,ROOT);assert not errors,errors
 with Image.open(OUT/'Native/river_flood_high.png')as hi,Image.open(OUT/'Native/river_flood_low.png')as lo:
  hi=hi.convert('RGBA');lo=lo.convert('RGBA');assert sum(c[3]>0 for c in lo.getdata())<sum(c[3]>0 for c in hi.getdata())
  assert all(not lo.getpixel((x,y))[3]or hi.getpixel((x,y))[3]for y in range(48)for x in range(80))
  for im in [hi,lo]:assert all(im.getpixel((79,y))==(127,185,175,255)for y in range(5,43))
  for im in [hi,lo]:assert all(im.getpixel((x,0))[3]==im.getpixel((x,47))[3]==0 for x in range(80)) and all(im.getpixel((0,y))[3]==0 for y in range(48))
  assert sum(c[3]>0 for c in hi.getdata())>=2700
 (OUT/'native-checks.json').write_text(json.dumps(dict(status='pending user approval',assets=evidence,a2Errors=errors,a2PlacementsChecked=False,floodEastPortMatchesRiver=True,recededMaskSubset=True),ensure_ascii=False,indent=2)+'\n')
 print('Native PNG/ASE5 pairs RGBA equal; A2 catalogue0 errors; right water ports/receded subset valid')
if __name__=='__main__':run()
