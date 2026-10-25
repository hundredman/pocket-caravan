"""Migrate approved v2 calculation to a lossless, read-only-consumer source file."""
from pathlib import Path
import argparse,contextlib,hashlib,io,json,runpy,sys,tempfile
ROOT=Path(__file__).resolve().parents[2]

def runs(mask,x0,y0):
    result=[]
    for y,row in enumerate(mask):
        start=None
        for x,on in enumerate(list(row)+[False]):
            if on and start is None:start=x
            if not on and start is not None:result.append([y+y0,start+x0,x-1+x0]);start=None
    return result

def require_approved_counts(actual,expected):
    if actual!=expected:raise ValueError(('Approved v2 per-map counts differ; Claude review required',actual,expected))

def export(out,report):
    import numpy,cv2
    oldargv=sys.argv[:]
    try:
        with tempfile.TemporaryDirectory(prefix='pocket-caravan-b1-') as temp:
            path=str(Path(temp)/'approved.json');sys.argv=['layout_proposal_d_coast_v2.py',path,'7']
            with contextlib.redirect_stdout(io.StringIO()):g=runpy.run_path(str(ROOT/'Tools/WorldLayout/layout_proposal_d_coast_v2.py'))
            sys.argv=['layout_proposal_d_coast_v2_terrain.py',path,str(Path(temp)/'terrain.json')]
            with contextlib.redirect_stdout(io.StringIO()):terrain=runpy.run_path(str(ROOT/'Tools/WorldLayout/layout_proposal_d_coast_v2_terrain.py'))
    finally:sys.argv=oldargv
    x0,y0=g['X0'],g['Y0'];maps=[]
    actual={}
    for group,source,land in [('goryeo',g['gz'],g['gl']),('east',g['ez'],g['cl'])]:
        for label,mask in source.items():
            ys,xs=mask.nonzero();key=group+'/'+label
            maps.append(dict(key=key,label=label,region=group,bounds=[int(xs.min())+x0,int(ys.min())+y0,int(xs.max())+x0+1,int(ys.max())+y0+1],runs=runs(mask,x0,y0),area=int(mask.sum()),landArea=int((mask&land).sum()),nearSeaArea=int((mask&~land).sum())))
            actual[label]=int(mask.sum())
    expected={'개경':5001,'장단':4298,'나루':3997,'닥목':3402,'송악':3302,'동문 마을':3200,'북동 포구':3200,'남동 포구':3200,'중앙 길목 마을':3200,'북동 길':3050,'남동 길':3051,'북쪽 내륙길':3047,'남쪽 내륙길':3052}
    # Never silently alter the approved geometry to meet a test.
    evidence=dict(expectedMapAreas=expected,actualMapAreas=actual,areaMatches=actual==expected,goryeoTotal=int(g['greg'].sum()),eastTotal=int(g['wedge'].sum()),goryeoLand=int(g['gl'].sum()),eastLand=int((g['wedge']&g['cl']).sum()),seed=7,numpy=numpy.__version__,opencv=cv2.__version__,rivers=terrain['report'],sourceWritten=False)
    report.parent.mkdir(parents=True,exist_ok=True);report.write_text(json.dumps(evidence,ensure_ascii=False,indent=2)+'\n')
    require_approved_counts(actual,expected)
    def key(label):return ('goryeo/' if label in g['gz'] else 'east/')+label
    towns=[dict(key=key(n),label=n,position=list(p),runtimeBinding='pending_B2_E1') for n,p in terrain['T'].items()]
    data=dict(schemaVersion=1,approval='Docs/WorldLayout.md 3절 v2 + Docs/Decisions-2026-10-25.md 1절',coordinateSystem=dict(x='east',y='south',cell='integer corner; center is +0.5',area='land plus assigned near sea; far sea excluded',bounds='half-open',runs='[y,inclusive x0,inclusive x1]'),scope=dict(goryeoArea=20000,eastArea=25000,teamBorders='D4/D7 pending; other team areas are reference only'),coastRecipe=dict(seed=7,nearSeaDistance=g['COAST'],smoothingKernel=5,minIslandArea=20,roughness=dict(goryeoDefault=3.5,goryeoSouth=4.0,goryeoEast=1.8,southBoundary='y > 40 * scale',eastBoundary='(x-radius)/scale > 105 and abs(y) < 50 * scale',amplitudeBlurSigma=6,noiseMultiplier=.75,continentAmplitude=.11,fractalScales=[48,24,12,6],fractalWeights=[1,.55,.3,.15],continentSeed=108),outline=g['GORYEO_OUTLINE'],islandSeeds=g['ISLANDS'],radius=g['r'],scale=g['s'],wedgeHalfDegrees=g['half'],note='Frozen approved cells are authoritative. Recipe/proposal retained for provenance; do not regenerate on game startup'),maps=maps,landRuns=runs(g['cl']|g['gl'],x0,y0),towns=towns,roads=[dict(fromKey=key(a),toKey=key(b),waypoints=[list(terrain['T'][a]),list(terrain['T'][b])],geometry='approved schematic segment; passable routing is B2') for a,b in terrain['ROADS']],mainQuest=[key(n) for n in terrain['MQ']],rivers=[dict(label=n,waypoints=p,validation=terrain['report'][n],geometry='approved diagram waypoints; width and playable routing not specified') for n,p in terrain['RIVERS'].items()],mountains=terrain['MOUNTAINS'],externalLinks=[dict(fromKey=key(n),target=p,agreement='D4/D7 pending',runtimeBinding=None) for n,p in [('중앙 길목 마을',[-15,0]),('북동 포구',[70,-150]),('남동 포구',[70,150])]],provenance=dict(numpy=numpy.__version__,opencv=cv2.__version__,sourceSha256={name:hashlib.sha256((ROOT/'Tools/WorldLayout'/name).read_bytes()).hexdigest() for name in ['layout_proposal_d_coast_v2.py','layout_proposal_d_coast_v2_terrain.py']}))
    out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n');evidence['sourceWritten']=True;report.write_text(json.dumps(evidence,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(maps=len(maps),areas=actual,source=str(out)),ensure_ascii=False))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--out',type=Path,default=ROOT/'Tools/WorldLayout/world-layout.json');parser.add_argument('--report',type=Path,default=ROOT/'Docs/Validation/B1-Migration-2026-10-25/reproduction.json');args=parser.parse_args();export(args.out,args.report)
