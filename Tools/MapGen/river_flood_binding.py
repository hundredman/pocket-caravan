"""Attach approved paired terrain metadata to a generated map; asset placement is owned by Codex (user 2026-11-03).
Usage: python river_flood_binding.py INPUT.json OUTPUT.json [--id e3_wetroad_flood]
attach() is called by m4_export's source pipeline before writing the map. No PNG edits.
"""
import argparse,copy,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/ArtTrials'))
from e3_flood_terrain import states
ID='e3_wetroad_flood'
def attach(data,placement_id=ID):
 if data['id']!='transit_wetroad':return copy.deepcopy(data)
 if not placement_id.startswith('e3_'):raise ValueError('Existing schedule requires e3_ placement ID')
 if data.get('boundaryLayoutVersion')==3:
  sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
  from t3_history import data as baseline
  b=baseline()['maps']['transit_wetroad'];expected=attach(b,placement_id)['terrainFloods'];expected=copy.deepcopy(expected)
  for p in expected:
   for c in p['cells']:
    c['x']+=b['terrainX']-data['terrainX'];c['y']+=b['terrainY']-data['terrainY']
    x,y=c['x'],c['y']
    if not(0<=x<data['w']and 0<=y<data['h']):raise ValueError('Flood crosses approved scene border')
    i=y*data['w']+x
    if data['palette'][data['ground'][i]]!=c['dryGround']or data['palette'][data['deco'][i]]!=c['dryDeco']or bool(data['blocked'][i])!=c['dryBlocked']or data['road'][i]!=c['dryGrade']:raise ValueError('T3 flood dry geometry changed')
  old=data.get('terrainFloods',[])
  if old and old!=expected:raise ValueError('T3 flood recipe changed')
  result=copy.deepcopy(data);result['terrainFloods']=expected;return result
 if (data['w'],data['h'],data.get('terrainX'),data.get('terrainY'))!=(78,64,86,-82):raise ValueError('Map bounds changed: revalidate approved flood geometry')
 if any(p.get('definitionId')=='river_flood'for p in data.get('obstacles',[])):raise ValueError('Existing sprite flood placement: owner must resolve duplication first')
 old=data.get('terrainFloods',[])
 if any(p.get('id')!=placement_id or p.get('definitionId')!='river_flood' for p in old):raise ValueError('Existing different terrain flood: owner must resolve overlap/ID first')
 wet,report=states(data)
 if(report['newWaterCells'],report['submergedRoadCells'])!=(62,22):raise ValueError('Approved geometry/road changed: revalidate before binding')
 cells=[];pal=wet['palette'];w=data['w']
 for y in range(data['h']):
  for x in range(w):
   i=y*w+x
   if all(data[k][i]==wet[k][i]for k in ['ground','deco','blocked','road']):continue
   cells.append(dict(x=x,y=y,dryGround=pal[data['ground'][i]],wetGround=pal[wet['ground'][i]],dryDeco=pal[data['deco'][i]],wetDeco=pal[wet['deco'][i]],dryBlocked=bool(data['blocked'][i]),wetBlocked=bool(wet['blocked'][i]),dryGrade=data['road'][i],wetGrade=wet['road'][i]))
 result=copy.deepcopy(data);result['terrainFloods']=[dict(id=placement_id,definitionId='river_flood',phaseOffset=old[0].get('phaseOffset',0) if old else 0,cells=cells)]
 return result
if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('input',type=Path);ap.add_argument('output',type=Path);ap.add_argument('--id',default=ID);a=ap.parse_args()
 data=attach(json.loads(a.input.read_text()),a.id);a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print('Terrain flood records:',len(data.get('terrainFloods',[])))
