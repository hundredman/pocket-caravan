"""Strictly undo only the approved C3 layer moves for older C1/A4 regression baselines."""
import copy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
FOLDER=ROOT/'ArtReview/C3a-2026-10-26'

def restore_before_c3(document):
    approval=json.loads((FOLDER/'approval.json').read_text())
    assert approval['status']=='approved'
    before=json.loads((FOLDER/'before'/(document['id']+'.json')).read_text())
    moves=json.loads((FOLDER/'moves.json').read_text())[document['id']]
    expected={p['y']*document['w']+p['x']:p['key'].replace(':','_').replace('@','_') for p in moves}
    layers=('ground','deco','obj','high')
    assert {k:v for k,v in document.items() if k not in (*layers,'palette')}=={k:v for k,v in before.items() if k not in (*layers,'palette')}
    for layer in layers:
        for i,(old,now) in enumerate(zip(before[layer],document[layer])):
            a=before['palette'][old];b=document['palette'][now]
            target=a
            if i in expected and layer in ('obj','high'):
                assert (before['palette'][before['obj'][i]],before['palette'][before['high'][i]])==(expected[i],'')
                target='' if layer=='obj' else expected[i]
            assert b==target,(document['id'],layer,i,b,target)
    result=copy.deepcopy(document)
    for layer in (*layers,'palette'):result[layer]=copy.deepcopy(before[layer])
    return result
