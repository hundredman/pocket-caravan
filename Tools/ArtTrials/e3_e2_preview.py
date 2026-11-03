"""Build isolated art-review inputs; never overwrite production maps/art.
Whole native objects only. Coast context is drawn only on blocked empty east-sea cells.
"""
import copy,hashlib,json,shutil
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/E3-E2-Art-20261103';PREV=ROOT/'Assets/PocketCaravan/Editor/B2Preview/E3Art'
NAMES=['fallen_log','river_flood_high','river_flood_low','transit_spice','transit_wool']
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
def coast_context(d):
 d=copy.deepcopy(d);w,h=d['w'],d['h'];water=d['palette'].index('pond_center');changed=[]
 for y in range(h):
  populated=[x for x in range(w)if d['ground'][y*w+x]]
  if not populated:continue
  last=max(populated)
  if not d['water'][y*w+last]:continue
  for x in range(last+1,w):
   i=y*w+x
   if not d['ground'][i]and d['blocked'][i]:d['ground'][i]=water;changed.append([x,y])
 return d,changed

def run():
 (PREV/'Maps').mkdir(parents=True,exist_ok=True);(PREV/'Whole').mkdir(exist_ok=True);(PREV/'Tiles').mkdir(exist_ok=True)
 assets=[]
 for name in NAMES:
  source=OUT/'Native'/(name+'.png');shutil.copyfile(source,PREV/'Whole'/source.name)
  with Image.open(source)as im:im=im.convert('RGBA');assets.append(dict(id=name,width=im.width,height=im.height,image=str((PREV/'Whole'/source.name).relative_to(ROOT)),rgba=hashlib.sha256(im.tobytes()).hexdigest()))
 names=set();reports={}
 for mid,slug in [('transit_southport','southport'),('transit_ferry','ferry')]:
  before=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f'{mid}.json').read_text());after=copy.deepcopy(before)
  if slug=='southport':after,changes=coast_context(after);reports[slug]=dict(visualSeaCells=changes,collisionChanged=False)
  else:
   changed=0
   for o in after['objects']:
    if o['kind']=='anim'and o.get('sheet')=='ripples':o['sheet']='goryeo_ripples';changed+=1
   reports[slug]=dict(reboundAnimations=changed,frames='existing goryeo_ripples (RGBA/alpha unchanged)')
  for version,d in [('before',before),('after',after)]:
   d['previewOnly']=True;names.update(n for n in d['palette']if n);write(PREV/'Maps'/f'{slug}-{version}.json',d)
 # A neutral terrain trial, same 16px ground, with a vertical path and matching river.
 base=json.loads((ROOT/'Assets/PocketCaravan/Maps/Naru.json').read_text());w,h=30,17;pal=['','floor_0_12_goryeo_jade','pond_center'];names.update(pal[1:]);ground=[1]*(w*h)
 for y in range(h):
  for x in range(18,w):ground[y*w+x]=2
 # Copy an existing straight road column (native road+grass edges, not a new tile design).
 template=base;tw=base['w'];roadcols=None
 for y in range(2,base['h']-2):
  for x in range(2,tw-3):
   if all(base['road'][(y+dy)*tw+x+dx]>0 for dy in [-1,0,1]for dx in [0,1]):roadcols=(x,y);break
  if roadcols:break
 if roadcols:
  rx,ry=roadcols
  for y in range(h):
   for dx in range(-1,3):
    n=base['palette'][base['ground'][ry*tw+rx+dx]]
    if n not in pal:pal.append(n)
    names.add(n);ground[y*w+14+dx]=pal.index(n)
 bank='water_0_7_goryeo_jade'
 if bank not in pal:pal.append(bank)
 names.add(bank)
 for y in range(h):ground[y*w+17]=pal.index(bank)
 d=dict(id='e3_art_trial',name='E3 미술 시험',w=w,h=h,palette=pal,ground=ground,deco=[0]*(w*h),obj=[0]*(w*h),high=[0]*(w*h),blocked=[int(i%w>=18)for i in range(w*h)],road=[0]*(w*h),water=[int(i%w>=18)for i in range(w*h)],objects=[dict(kind='spawn',id='S0',x=10,y=10)],previewOnly=True,terrainX=0,terrainY=0,hours=0)
 write(PREV/'Maps/trial.json',d)
 for name in names:
  shutil.copyfile(ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/f'{name}.png',PREV/'Tiles'/f'{name}.png')
 write(OUT/'manifest.json',dict(status='pending user visual approval',productionChanged=False,assets=assets,ports=reports,preview=str(PREV.relative_to(ROOT)),roadsSource=roadcols,baseIntegration='4f00f799c'))
 print('Review inputs:',len(assets),'native assets;',len(reports['southport']['visualSeaCells']),'sea context cells;',reports['ferry']['reboundAnimations'],'ripple bindings')
if __name__=='__main__':run()
