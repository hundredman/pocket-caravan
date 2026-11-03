"""Isolated E3 terrain-state review. Existing river tiles, never sprite overlays.
Only this source generates the candidate states; production inputs are read-only.
"""
import copy,json,sys,shutil
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
from m1_tiles_preview import autorel,tileready,fill_diagonals
OUT=ROOT/'ArtReview/E3-FloodTerrain-20261103'
PREVIEW=ROOT/'Assets/PocketCaravan/Editor/B2Preview/E3FloodTerrain'
TILES=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
# Map-local JSON coordinates (downward y). Wide smooth expansion along north bank.
LEFT_BANK={13:51,14:47,15:44,16:42,17:41,18:40,19:40,20:42,21:46,22:49}

def states(before):
 after=copy.deepcopy(before);w,h=before['w'],before['h'];shape=(h,w)
 old=np.array(before['water'],bool).reshape(shape);water=old.copy()
 for y,left in LEFT_BANK.items():
  river=[x for x in range(left,w)if old[y,x]]
  assert river,'Expansion must join the existing river'
  water[y,left:river[0]+1]=True
 allowed=np.ones(shape,bool)
 for _ in range(4):
  water=fill_diagonals(tileready(water,allowed),allowed)
  # Restrict repair to the flood neighborhood: unchanged map elsewhere stays exact.
  water[:12]=old[:12];water[24:]=old[24:];water[:,:39]=old[:,:39];water[:,58:]=old[:,58:]
 new=water&~old
 assert not (np.array(before['obj']).reshape(shape)[new]>0).any(),'Flood would cut an existing object'
 assert not (np.array(before['high']).reshape(shape)[new]>0).any(),'Flood would cut a canopy'
 for o in before['objects']:
  if o['kind'] in ('spawn','exit'):assert not new[o['y'],o['x']],'Flood would cover a seam arrival/exit'
 road=np.array(before['road']).reshape(shape).copy();road[new]=0
 edit=ndimage.binary_dilation(new,structure=np.ones((3,3),bool),iterations=1)
 pal=after['palette'];changed=[]
 def index(name):
  assert (TILES/(name+'.png')).exists(),f'Missing approved assembly tile: {name}'
  if name not in pal:pal.append(name)
  return pal.index(name)
 def rel(mask,y,x):
  def neighbor(dy,dx):
   yy,xx=y+dy,x+dx
   return bool(mask[yy,xx])if 0<=yy<h and 0<=xx<w else True
  value=autorel(neighbor,y,x);assert value is not None,('unsupported narrow terrain',x,y)
  return value
 for y,x in np.argwhere(edit):
  i=int(y*w+x);x=int(x);y=int(y)
  if water[y,x]:
   a,b=rel(water,y,x);after['ground'][i]=index(f'water_{a}_{b+6}_goryeo_jade')
  if new[y,x]:
   after['blocked'][i]=1;after['water'][i]=1;after['road'][i]=0
   after['deco'][i]=0
  if after['ground'][i]!=before['ground'][i] or new[y,x]:changed.append([x,y])
 floodroad=new&(np.array(before['road']).reshape(shape)>0)
 assert floodroad.sum()>0
 report=dict(sourceMap='transit_wetroad',newWaterCells=int(new.sum()),submergedRoadCells=int(floodroad.sum()),changedCells=changed,newWater=[[int(x),int(y)]for y,x in np.argwhere(new)],existingWaterPreserved=bool(water[old].all()),objectsPreserved=before['objects']==after['objects'],bounds=[39,12,58,24],camera=[47,46],productionChanged=False)
 return after,report

def render(d):
 w,h=d['w'],d['h'];im=Image.new('RGBA',(w*16,h*16),'#1c2228');cache={}
 for layer in ['ground','deco','obj','high']:
  for i,n in enumerate(d[layer]):
   if not n:continue
   name=d['palette'][n]
   if name not in cache:cache[name]=Image.open(TILES/(name+'.png')).convert('RGBA')
   im.alpha_composite(cache[name],(i%w*16,i//w*16))
 return im

def run():
 OUT.mkdir(parents=True,exist_ok=True);(PREVIEW/'Maps').mkdir(parents=True,exist_ok=True);(PREVIEW/'Tiles').mkdir(exist_ok=True)
 before=json.loads((ROOT/'Assets/PocketCaravan/Maps/transit_wetroad.json').read_text());after,report=states(before)
 for stage,d in [('normal',before),('flood',after)]:
  render(d).save(OUT/(stage+'-map.png'))
  d['previewOnly']=True
  # Existing jade ripple frames; avoid the known opaque blue legacy backgrounds in both views.
  for obj in d['objects']:
   if obj['kind']=='anim' and obj.get('sheet')=='ripples':obj['sheet']='goryeo_ripples'
  (PREVIEW/'Maps'/(stage+'.json')).write_text(json.dumps(d,ensure_ascii=False)+'\n')
 for name in after['palette']:
  if name:shutil.copyfile(TILES/(name+'.png'),PREVIEW/'Tiles'/(name+'.png'))
 (OUT/'terrain-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 # Full native-tile render at review camera. This is a static reference, not Unity.
 board=Image.new('RGB',(960,310),'#263035');draw=ImageDraw.Draw(board);font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',21)
 for i,stage in enumerate(['normal','flood']):
  im=Image.open(OUT/(stage+'-map.png'));cx,cy=47*16,18*16;im=im.crop((cx-240,cy-135,cx+240,cy+135));board.paste(im,(480*i,40));draw.text((480*i+10,8),'평상시'if i==0 else'범람 · 기존 강 지형 확장',font=font,fill='white')
 board.save(OUT/'terrain-comparison-static.png');print(json.dumps({k:v for k,v in report.items()if k not in ['changedCells','newWater']}))
def unity_board():
 board=Image.new('RGB',(960,310),'#263035');draw=ImageDraw.Draw(board);font=ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc',21)
 for i,name in enumerate(['normal','flood']):
  im=Image.open(OUT/'Unity'/(name+'.png')).convert('RGB');assert im.size==(480,270)
  board.paste(im,(i*480,40));draw.text((i*480+10,8),'평상시'if i==0 else'범람 · 실제 Unity 지형 전환',font=font,fill='white')
 board.save(OUT/'unity-comparison.png')
if __name__=='__main__':
 if '--unity-board' in sys.argv:unity_board()
 else:run()
