"""Read-only forest/tile audit. Writes only Docs/Validation evidence."""
from pathlib import Path
import json,hashlib,sys
import numpy as np
from scipy import signal
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
ACTIVE=ROOT.parent/'PocketCaravan'
OUT=ROOT/'Docs/Validation/Forest-Performance-2026-10-29'
MAPS=['Naru','goryeo_jangdan','goryeo_songak','Dakmok','Gaegyeong','transit_woodtown','transit_ferry','transit_crossroads','transit_southport','transit_wetroad','transit_pass','transit_central_approach','transit_south_inland']
LAYERS=['ground','deco','obj','high']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    OUT.mkdir(exist_ok=True,parents=True)
    composites=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json').read_text())['tiles']
    tree_names=('forest_','pine_','round_','light_tree','broadleaf','golden_tree','willow')
    def trees(tile):
        return sum(1 for p in composites.get(tile,{}).get('parts',[]) if p[0].startswith('sprite:') and p[0].split(':')[1].startswith(tree_names))
    source_counts={k:trees(k) for k in composites}
    rows=[];all_textures=set();versions=[]
    for mid in MAPS:
        rel=Path(f'Assets/PocketCaravan/Maps/{mid}.json');f=json.loads((ROOT/rel).read_text());w,h=f['w'],f['h'];pal=f['palette']
        scene=next(p for p in (ROOT/'Assets/PocketCaravan/Scenes').rglob('*.unity') if p.stem==mid)
        forest=np.zeros((h,w),bool);used=set();occupied={};visible_union=np.zeros((h,w),bool)
        forest_parts=0;max_parts=0
        for layer in LAYERS:
            a=np.array(f[layer]).reshape(h,w);occupied[layer]=int((a>0).sum());visible_union|=a>0
            for idx in np.unique(a):
                if idx:used.add(pal[idx])
            lookup=np.array([source_counts.get(p,0) for p in pal]);counts=lookup[a]
            forest|=counts>0;forest_parts+=int(counts.sum());max_parts=max(max_parts,int(counts.max()))
        sums=signal.convolve2d(forest.astype(int),np.ones((17,30),int),mode='valid');flat=int(sums.argmax());y,x=np.unravel_index(flat,sums.shape)
        forest_tiles=set()
        window_counts={}
        for layer in LAYERS:
            a=np.array(f[layer]).reshape(h,w)[y:y+17,x:x+30];window_counts[layer]=int((a>0).sum())
            forest_tiles|={pal[int(i)] for i in np.unique(a) if i}
        relscene=scene.relative_to(ROOT)
        for relfile in [rel,relscene]:
            versions.append(dict(path=str(relfile),worktreeSha256=sha(ROOT/relfile),activeSha256=sha(ACTIVE/relfile),identical=sha(ROOT/relfile)==sha(ACTIVE/relfile)))
        rows.append(dict(mapId=mid,size=[w,h],scene=str(relscene),layerCells=occupied,distinctTileImages=len(used),forestCells=int(forest.sum()),
                         overlappingTreeContributions=forest_parts,maxTreePartsBakedIntoOneCell=max_parts,
                         densestWindow=dict(topLeft=[int(x),int(y)],size=[30,17],forestCells=int(sums[y,x]),tileCells=window_counts,
                                            uniqueTileImages=len(forest_tiles),cameraWorld=[float(x+15),float(h-y-8.5)])))
        all_textures|=used
    for rel in ['Assets/PocketCaravan/Editor/MapJsonBuilder.cs','Tools/WorldLayout/m4_export.py','Assets/PocketCaravan/Art/Baked/m4-composites.json']:
        versions.append(dict(path=rel,worktreeSha256=sha(ROOT/rel),activeSha256=sha(ACTIVE/rel),identical=sha(ROOT/rel)==sha(ACTIVE/rel)))
    assert all(v['identical'] for v in versions),'Active Unity checkout differs from audit source; do not conflate them'
    raw=0;sizes={}
    for n in all_textures:
        p=ROOT/f'Assets/PocketCaravan/Art/NinjaAdventure/Tiles/{n}.png'
        if p.exists():
            with Image.open(p) as im:raw+=im.width*im.height*4;sizes[str(im.size)]=sizes.get(str(im.size),0)+1
    rows.sort(key=lambda r:(r['densestWindow']['forestCells'],r['densestWindow']['uniqueTileImages']),reverse=True)
    result=dict(scope='13 production Goryeo/east maps; primary active Unity source hashes compared',maps=rows,
                uniqueTileImagesAcrossMaps=len(all_textures),imageDimensions=sizes,rawRGBAImageBytes=raw,
                bakedCompositeImages=len(composites),forestCompositeImages=sum(v>0 for v in source_counts.values()),
                globalSpriteAtlases=[str(p.relative_to(ROOT)) for p in (ROOT/'Assets').rglob('*.spriteatlasv2')],
                activeSourceMatches=True,sourceHashes=versions,notes='Contribution counts are source parts already baked per cell, not runtime tree objects. RawRGBA bytes exclude Unity/driver object overhead.')
    (OUT/'static-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    (OUT/'all-scenes.tsv').write_text(''.join(r['mapId']+'\t'+r['scene']+'\n' for r in rows))
    # Keep bounded empirical renderer tests on the two densest map windows.
    selected=rows[:2]
    (OUT/'cases.tsv').write_text(''.join('\t'.join(str(v) for v in [r['mapId'],r['scene'],*r['densestWindow']['cameraWorld']])+'\n' for r in selected))
    print(json.dumps(dict(maps=len(rows),tileImages=len(all_textures),rawRGBABytes=raw,sourceMatch=True,cases=[dict(map=r['mapId'],window=r['densestWindow']) for r in selected]),ensure_ascii=False))
if __name__=='__main__':main()
