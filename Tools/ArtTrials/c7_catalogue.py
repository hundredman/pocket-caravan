"""Register isolated C7 trials; engine cells always represent the complete object.

PNG evidence is dimensions + decoded RGBA, never encoded PNG file bytes.
Nothing here changes a production catalogue, map, scene or approval record.
"""
from pathlib import Path
import hashlib
import json
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtReview/C7-Trial4-20261026'
sys.path.insert(0, str(ROOT / 'Tools/MapGen'))
from asset_manifest import validate_catalogue


def rgba_evidence(path):
    with Image.open(path) as source:
        im = source.convert('RGBA')
        return dict(size=list(im.size), rgbaSHA256=hashlib.sha256(im.tobytes()).hexdigest())


def path(p):
    return str(p.relative_to(ROOT))


def shape(name, layer='obj'):
    whole = OUT / 'Native' / (name + '.png')
    im = Image.open(whole).convert('RGBA')
    w, h = im.width // 16, im.height // 16
    assert im.size == (w * 16, h * 16)
    tiles = []
    for y in range(h):
        for x in range(w):
            key = f'c7_{name.replace("-", "_")}_{x}_{y}'
            tile = OUT / 'Cells' / (key + '.png')
            # Complete, lossless render subdivision only. Every cell is declared.
            im.crop((x*16, y*16, (x+1)*16, (y+1)*16)).save(tile)
            tiles.append(dict(dx=x, dy=y, layer=layer, tile=key, image=path(tile)))
    return dict(size=[w, h], image=path(whole), tiles=tiles)


def build():
    assets = []
    for name in ('thatch', 'tile-house', 'pine'):
        assets.append(dict(id='c7_'+name.replace('-', '_'), purpose=name, kind='object', profile='c7_native16',
                           source=dict(kind='ai_processed', path=path(OUT/'Native'/(name+'.aseprite'))),
                           review=dict(status='pending'), **shape(name)))
    pieces = {p.stem: shape(p.stem, 'ground') for p in sorted((OUT/'Native').glob('grass-*.png'))}
    # Semantic core names point to real authored topology masks, not partial objects.
    aliases = dict(center='grass-255', edge='grass-110', outer_corner='grass-38', inner_corner='grass-127')
    pieces.update({k: pieces[v] for k, v in aliases.items()})
    pieces['dirt'] = shape('dirt', 'ground')
    assets.append(dict(id='c7_grass_dirt', purpose='grass_dirt_boundary', kind='modular',
                       moduleType='ground_boundary', profile='c7_native16',
                       source=dict(kind='ai_processed', path='Tools/ArtTrials/c7_finish.lua'),
                       review=dict(status='pending'), requiredPieces=list(aliases), pieces=pieces))
    palette = [l.strip() for l in (OUT/'trial-palette.txt').read_text().splitlines() if l.startswith('#')]
    doc = dict(schemaVersion=1, profiles=dict(c7_native16=dict(pixelsPerCell=[16,16], binaryAlpha=True, palette=palette)), assets=assets)
    (OUT/'catalogue.json').write_text(json.dumps(doc, ensure_ascii=False, indent=2)+'\n')
    errors, _ = validate_catalogue(doc, ROOT)
    (OUT/'a2-result.json').write_text(json.dumps(dict(errors=errors, approval='pending; catalogue validation is not art approval'), indent=2)+'\n')
    evidence = {path(p): rgba_evidence(p) for p in sorted((OUT/'Native').glob('*.png'))}
    (OUT/'native-rgba.json').write_text(json.dumps(evidence, indent=2)+'\n')
    if errors:
        raise RuntimeError(errors)
    print(f'A2 catalogue: {len(assets)} assets, {len(pieces)} boundary definitions, 0 errors; pending approval')


if __name__ == '__main__':
    build()
