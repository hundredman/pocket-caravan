"""Approved visual-only port water binding; no full map export and no collision/ID edits."""
import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];SPEC=Path(__file__).with_suffix('.json')
def attach(data):
 spec=json.loads(SPEC.read_text());approval=json.loads((ROOT/spec['approval']).read_text())
 if spec['status']!='approved'or approval['status']!='approved'or approval['maps']!=['transit_southport','transit_ferry']:raise ValueError('Port approval scope changed')
 settings=next((m for m in spec['maps']if m['mapId']==data['id']),None)
 report=dict(mapId=data['id'],seaCells=[],rippleObjects=[])
 if settings is None:return data,report
 if data['id']not in approval['maps']:raise ValueError('Unapproved map')
 d=copy.deepcopy(data);w,h=d['w'],d['h'];water=d['palette'].index('pond_center')
 if settings['fillEastSea']:
  for y in range(h):
   populated=[x for x in range(w)if d['ground'][y*w+x]]
   if not populated:continue
   last=max(populated)
   if not d['water'][y*w+last]:continue
   for x in range(last+1,w):
    i=y*w+x
    if not d['ground'][i]and d['blocked'][i]:d['ground'][i]=water;report['seaCells'].append([x,y])
 if settings['fixRipples']:
  for i,o in enumerate(d['objects']):
   if o['kind']=='anim'and o.get('sheet')=='ripples':o['sheet']='goryeo_ripples';report['rippleObjects'].append(i)
 return d,report
def restore_for_historical_checks(data):
 """Undo only approved visual deltas when older gameplay/area/art fixtures are compared."""
 if data['id']not in ('transit_southport','transit_ferry'):return data
 before=json.loads((ROOT/'ArtReview/PortWater-Task6-20261104/Before'/(data['id']+'.json')).read_text())
 expected,report=attach(before);d=copy.deepcopy(data)
 for x,y in report['seaCells']:
  i=y*d['w']+x
  if d['ground'][i]!=expected['ground'][i]:raise ValueError('Approved sea context changed')
  d['ground'][i]=before['ground'][i]
 for i in report['rippleObjects']:
  old=before['objects'][i];new=expected['objects'][i]
  matches=[o for o in d['objects']if o==new]
  if len(matches)!=1:raise ValueError('Approved ripple binding changed')
  matches[0]['sheet']=old['sheet']
 return d
def main():
 reports=[]
 for row in json.loads(SPEC.read_text())['maps']:
  path=ROOT/'Assets/PocketCaravan/Maps'/(row['mapId']+'.json');before=json.loads(path.read_text());after,report=attach(before)
  if before!=after:path.write_text(json.dumps(after,ensure_ascii=False,separators=(',',':')))
  reports.append(report)
 out=ROOT/'ArtReview/PortWater-Task6-20261104/applied.json';
 if not out.exists()or any(r['seaCells']or r['rippleObjects']for r in reports):out.write_text(json.dumps(reports,ensure_ascii=False,indent=2)+'\n')
 print([(r['mapId'],len(r['seaCells']),len(r['rippleObjects']))for r in reports])
if __name__=='__main__':main()
