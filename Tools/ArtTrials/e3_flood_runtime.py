"""Prepare runtime-component review only. Owner retains live placement/export responsibility."""
import sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from river_flood_binding import attach
OUT=ROOT/'ArtReview/E3-FloodRuntime-20261103';PREV=ROOT/'Assets/PocketCaravan/Editor/B2Preview/E3FloodTerrain'
def run():
 data=json.loads((ROOT/'Assets/PocketCaravan/Maps/transit_wetroad.json').read_text());bound=attach(data);OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'terrain-binding.json').write_text(json.dumps(dict(mapId=data['id'],terrainFloods=bound['terrainFloods']),ensure_ascii=False,indent=2)+'\n')
 bound['previewOnly']=True
 for o in bound['objects']:
  if o['kind']=='anim'and o.get('sheet')=='ripples':o['sheet']='goryeo_ripples'
 (PREV/'Maps/runtime.json').write_text(json.dumps(bound,ensure_ascii=False)+'\n')
 print('Runtime binding prepared:',len(bound['terrainFloods'][0]['cells']),'paired cells; production unchanged')
if __name__=='__main__':run()
