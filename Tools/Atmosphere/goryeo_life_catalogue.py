"""A2 list for newly approved animal frames. Cell exports are rendering evidence only."""
from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Approved/GoryeoAtmosphereA7';TARGET=ROOT/'Assets/PocketCaravan/Resources/Goryeo/VillageLife'
def build():
 rows=[];palette=set()
 for kind in ('chicken','dog'):
  for f in range(2):
   name=f'{kind}_{f}'
   with Image.open(OUT/f'{name}.png')as im:palette.update('#%02x%02x%02x'%p[:3]for p in im.convert('RGBA').getdata()if p[3])
   rows.append(dict(id=f'goryeo_life_{name}',kind='object',purpose='village_animal',size=[2,2],image=str((TARGET/'Art'/f'{name}.png').relative_to(ROOT)),source=dict(kind='ai_processed',path=str((OUT/f'{kind}.aseprite').relative_to(ROOT))),review=dict(status='approved',evidence=str((OUT/'approval.json').relative_to(ROOT))),profile='animal',rules=dict(onTerrain=['grass','dirt'],forbidRoad=False),tiles=[dict(dx=x,dy=y,layer='obj',tile=f'goryeo_life_{name}_{x}_{y}',image=str((TARGET/'Cells'/f'{name}_{x}_{y}.png').relative_to(ROOT)))for y in range(2)for x in range(2)]))
 (OUT/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=dict(animal=dict(pixelsPerCell=[16,16],palette=sorted(palette),binaryAlpha=True)),assets=rows),ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':build()
