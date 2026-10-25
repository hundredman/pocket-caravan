"""Exercise the complete registered-map export with live extraction/tint methods forbidden."""
from pathlib import Path
from collections import Counter
import hashlib,json,runpy,sys,tempfile
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from PIL import Image
import tiles,goryeo_art,frozen_art,world_registry

def verify():
    calls=Counter();originals=[]
    def forbid(name):
        def fail(*args,**kwargs):calls[name]+=1;raise AssertionError('Forbidden during generation: '+name)
        return fail
    frozen_art.tile.cache_clear();frozen_art.catalogue.cache_clear();frozen_art.index.cache_clear()
    for obj,name in ((Image.Image,'crop'),(tiles,'recolor'),(goryeo_art,'tone')):
        originals.append((obj,name,getattr(obj,name)));setattr(obj,name,forbid(name))
    argv=sys.argv[:]
    try:
        with tempfile.TemporaryDirectory(prefix='pocket-c6-export-') as temporary:
            target=Path(temporary);sys.argv=['export.py',temporary]
            runpy.run_path(str(ROOT/'Tools/MapGen/export.py'),run_name='__main__')
            entries,maps=world_registry.atlas_content();reports=[]
            for e,m in zip(entries,maps):
                actual=(target/e['jsonPath']).read_bytes();expected=(ROOT/e['jsonPath']).read_bytes()
                if actual!=expected:raise AssertionError('Map changed: '+m.id)
                data=json.loads(actual);bound=frozen_art.validate_export(m,data)
                reports.append(dict(map=m.id,cells=m.w*m.h,placements=len(bound['assetPlacements']),mapSHA256=hashlib.sha256(actual).hexdigest(),a2Errors=0))
            images=0
            for p in (target/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles').glob('*.png'):
                q=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'/p.name
                with Image.open(p) as a,Image.open(q) as b:
                    if a.convert('RGBA').tobytes()!=b.convert('RGBA').tobytes():raise AssertionError('Pixels changed: '+p.name)
                images+=1
            for p in (target/'Assets/PocketCaravan/Resources').rglob('*'):
                if p.is_file() and p.read_bytes()!=(ROOT/p.relative_to(target)).read_bytes():raise AssertionError('Resource changed: '+str(p))
            idx=frozen_art.index()
            for path,digest in idx['visualImages'].items():
                if hashlib.sha256((ROOT/path).read_bytes()).hexdigest()!=digest:raise AssertionError('Visual frame changed: '+path)
            return dict(maps=reports,renderTileImages=images,cropCalls=calls['crop'],recolorCalls=calls['recolor'],toneCalls=calls['tone'],catalogueAssets=len(frozen_art.catalogue()),frozenTileKeys=len(idx['tiles']),visualFrames=len(idx['visualImages']),resourcesByteIdentical=True)
    finally:
        sys.argv=argv
        for obj,name,original in reversed(originals):setattr(obj,name,original)

if __name__=='__main__':
    report=verify();output=ROOT/'Docs/Validation/C6-Production-2026-10-26/generation.json';output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
