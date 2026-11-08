"""Read-only T3 distance regression using the existing M4 physical-world metric.
Measures fade/loading separately: these costs are not in TownRoutes.
"""
import sys,json
from pathlib import Path
import numpy as np
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
sys.path.insert(0,str(HERE));sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import m4_routes as r
from t3_history import data as baseline

def distances(maps):
 x0=min(d['terrainX']for d in maps.values());y0=min(d['terrainY']for d in maps.values());x1=max(d['terrainX']+d['w']for d in maps.values());y1=max(d['terrainY']+d['h']for d in maps.values());walk=np.zeros((y1-y0,x1-x0),bool)
 for d in maps.values():walk[d['terrainY']-y0:d['terrainY']-y0+d['h'],d['terrainX']-x0:d['terrainX']-x0+d['w']]|=np.array(d['blocked']).reshape(d['h'],d['w'])==0
 towns=r.town_cells(maps,x0,y0);out={}
 for a in towns:
  d=r.dijkstra(walk,towns[a])
  for b in towns:
   if a<b:out[(a,b)]=float(d[towns[b]])/3.8
 return out

def main():
 old=baseline()['maps'];maps={mid:json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())for mid in old}
 before=distances(old);after=distances(maps)
 stored=json.loads((ROOT/'Assets/PocketCaravan/Resources/TownRoutes.json').read_text());reference={frozenset((p['from'],p['to'])):p['seconds']for p in stored['walkingDistances']}
 rows=[dict(pair=list(pair),before=before[pair],after=value,delta=value-before[pair],storedReference=reference[frozenset(pair)])for pair,value in after.items()]
 report=dict(method='existing physical-world 8-direction/no-corner-cut metric; fades and load time excluded',pairs=rows,maxBoundaryDeltaSeconds=max(abs(p['delta'])for p in rows),maxExistingReferenceDeltaSeconds=max(abs(p['before']-p['storedReference'])for p in rows),note='Stored reference already differs slightly from the frozen pre-T3 maps; no economy/route calibration changed.')
 (ROOT/'Docs/Validation/T3-Production-2026-11-08/distance.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='pairs'}))
 if report['maxBoundaryDeltaSeconds']>1e-6:raise ValueError('Physical walking distances changed')
if __name__=='__main__':main()
