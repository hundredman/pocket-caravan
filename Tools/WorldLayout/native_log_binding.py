"""Promote approved whole log sprites; candidate map placements stay preview-only until reviewed."""
from pathlib import Path
import copy,json,hashlib,shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ArtReview/LogAngles-v3-20261104'
OUT=ROOT/'ArtReview/LogBinding-20261104'
ART=ROOT/'Assets/PocketCaravan/Art/Goryeo/RoadLogs'
NAMES=('horizontal','diagonal_ne','diagonal_nw','vertical')
def promote():
 approval=json.loads((REVIEW/'approval.json').read_text())
 if approval['status']!='approved':raise ValueError('Log art not approved')
 palette=set();assets=[];runtime=[]
 for name in NAMES:
  im=Image.open(REVIEW/'Native'/f'{name}.png').convert('RGBA');record=next(a for a in approval['assets']if a['name']==name)
  if im.size!=(48,48)or hashlib.sha256(im.tobytes()).hexdigest()!=record['decodedRGBAsha256']:raise ValueError('Approved log pixels changed')
  ART.mkdir(parents=True,exist_ok=True)
  for ext in ('png','aseprite'):shutil.copyfile(REVIEW/'Native'/f'{name}.{ext}',ART/f'{name}.{ext}')
  palette.update('#%02x%02x%02x'%c[:3]for c in im.getdata()if c[3]);tiles=[];occupied=[]
  for y in range(3):
   for x in range(3):
    # Whole-object A2 rendering chunks, never independently placed or repurposed.
    tile=im.crop((x*16,y*16,x*16+16,y*16+16));file=ART/'Cells'/f'{name}_{x}_{y}.png';file.parent.mkdir(exist_ok=True);tile.save(file)
    tiles.append(dict(dx=x,dy=y,layer='obj',tile=f'e3_log_{name}_{x}_{y}',image=str(file.relative_to(ROOT))))
    if sum(a>0 for a in tile.getchannel('A').tobytes())>=64:occupied.append(dict(x=x,y=2-y))
  image=str((ART/f'{name}.png').relative_to(ROOT))
  runtime.append(dict(variant=name,image=image,occupied=occupied))
  assets.append(dict(id='e3_log_'+name,purpose='road_hazard',kind='object',profile='native16',size=[3,3],image=image,tiles=tiles,source=dict(kind='ai_processed',path=str((ART/f'{name}.aseprite').relative_to(ROOT))),review=dict(status='approved',evidence=str((REVIEW/'approval.json').relative_to(ROOT)))))
 ART.joinpath('catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=dict(native16=dict(pixelsPerCell=[16,16],binaryAlpha=True,palette=sorted(palette))),assets=assets),ensure_ascii=False,indent=2)+'\n')
 target=ROOT/'Assets/PocketCaravan/Data/Goryeo/LogArt.json';target.write_text(json.dumps(dict(schemaVersion=1,status='approved',assets=runtime),indent=2)+'\n')
 return runtime
def candidate(data,variant='horizontal'):
 if data['id']!='transit_south_inland' or variant not in NAMES:raise ValueError('Placement scope requires review')
 if data.get('obstacles')or data.get('nativeLogs'):raise ValueError('Do not replace existing obstacles')
 d=copy.deepcopy(data);d['previewOnly']=True
 # Same reviewed art-trial road location; final placement still needs screenshot approval.
 d['obstacles']=[dict(id='e3_south_inland_log',definitionId='fallen_log',x=28,y=29,width=3,height=3,enabled=True,phaseOffset=0)]
 d['nativeLogs']=[dict(id='e3_south_inland_log',variant=variant)]
 return d
def revised_candidates():
 spec=json.loads((OUT/'revised-layout.json').read_text());results={}
 if spec['status']not in ('review','approved'):raise ValueError('Review placement status changed')
 for row in spec['options']:
  if row['mapId']!='transit_central_approach'or row['variant']not in NAMES:raise ValueError('Unreviewed scope')
  data=restore_for_historical_checks(json.loads((ROOT/'Assets/PocketCaravan/Maps'/(row['mapId']+'.json')).read_text()))
  if data.get('obstacles')or data.get('nativeLogs'):raise ValueError('Existing placement needs review')
  data['previewOnly']=True;data['obstacles']=[dict(id='e3_central_approach_log',definitionId='fallen_log',x=row['x'],y=row['y'],width=3,height=3,enabled=True,phaseOffset=0)];data['nativeLogs']=[dict(id='e3_central_approach_log',variant=row['variant'])]
  results[row['name']]=data
 return results
def bind_approved(data):
 spec=json.loads((Path(__file__).with_name('log_placement.json')).read_text())
 if data['id']!=spec['mapId']:return copy.deepcopy(data)
 approval=json.loads((OUT/'approval.json').read_text())
 if spec['status']!='approved'or any(approval.get(k)!=v for k,v in spec.items()):raise ValueError('Log placement approval changed')
 p={k:spec[k]for k in ['id','x','y','width','height']};p.update(definitionId='fallen_log',enabled=True,phaseOffset=0)
 b=dict(id=spec['id'],variant=spec['variant'])
 if data.get('obstacles')not in (None,[],[p])or data.get('nativeLogs')not in (None,[],[b]):raise ValueError('Existing obstacles need review')
 result=copy.deepcopy(data);result['obstacles']=[p];result['nativeLogs']=[b];return result
def restore_for_historical_checks(data):
 if data['id']!='transit_central_approach'or 'nativeLogs'not in data:return copy.deepcopy(data)
 before=json.loads((OUT/'production-source-before.json').read_text());expected=bind_approved(before)
 if data.get('obstacles')!=expected['obstacles']or data.get('nativeLogs')!=expected['nativeLogs']:raise ValueError('Approved log metadata changed')
 d=copy.deepcopy(data)
 for key in ['obstacles','nativeLogs']:
  if key in before:d[key]=before[key]
  else:d.pop(key)
 return d
if __name__=='__main__':
 promote();OUT.mkdir(parents=True,exist_ok=True)
 d=json.loads((ROOT/'Assets/PocketCaravan/Maps/transit_south_inland.json').read_text())
 for n in NAMES:(OUT/f'{n}.json').write_text(json.dumps(candidate(d,n),ensure_ascii=False,separators=(',',':')))
 if (OUT/'revised-layout.json').exists():
  for name,data in revised_candidates().items():(OUT/f'{name}.json').write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')))
 if '--apply'in __import__('sys').argv:
  path=ROOT/'Assets/PocketCaravan/Maps/transit_central_approach.json';data=bind_approved(json.loads(path.read_text()));path.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')))
 print('Approved art promoted; review maps written. Production metadata applied.'if '--apply'in __import__('sys').argv else 'Approved art promoted; review maps written. Production map unchanged.')
