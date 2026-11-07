"""Replay pre-T3 reviews explicitly; reject any unrecognized live change.
Current production is checked by test_t3_production and the Unity production audit.
No historical normalizer may silently hide new map/resource edits.
"""
from pathlib import Path
import copy,gzip,hashlib,json
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];BASE=ROOT/'Assets/PocketCaravan'
def data():
 raw=(HERE/'t3_baseline.json.gz').read_bytes();config=json.loads((HERE/'t3_boundaries.json').read_text())
 if hashlib.sha256(raw).hexdigest()!=config['baselineBundleSha256']:raise ValueError('Frozen approved bundle changed')
 return json.loads(gzip.decompress(raw))
def sha(d):return hashlib.sha256(json.dumps(d,ensure_ascii=False,sort_keys=True).encode()).hexdigest()
def restore_map(d):
 if d.get('boundaryLayoutVersion')!=3:return d
 b=data();p=b['paths'].get(d['id']);manifest=json.loads((HERE/'t3_installed.json').read_text())
 if not p or sha(d)!=manifest[p]:raise ValueError('T3 live map changed; historical replay refused: '+d['id'])
 return copy.deepcopy(b['maps'][d['id']])
def map_at(path):return restore_map(json.loads(Path(path).read_text()))
def resource_at(path):
 path=Path(path);p=str(path.relative_to(BASE));live=json.loads(path.read_text());b=data();manifest=json.loads((HERE/'t3_installed.json').read_text())
 if p not in b['resources']:return live
 if sha(live)!=manifest[p]:raise ValueError('T3 live resource changed; historical replay refused: '+p)
 return copy.deepcopy(b['resources'][p])
