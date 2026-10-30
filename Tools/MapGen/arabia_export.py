"""아라비아 첫 구간 맵 JSON만 내보낸다 (공용 export.py와 별도).

사용법 (저장소 루트):  python3 Tools/MapGen/arabia_export.py [--check] [--preview <폴더>]

- 이미 프로젝트에 있는 타일 PNG(+.meta)만 사용한다. 없는 타일이 나오면 실패하고 아무것도 쓰지 않는다.
- 공용 타일 PNG, 지형 약도(Resources/WorldMap), 다른 지역 JSON은 쓰지 않는다.
- 통행 검사: 각 도착 스폰에서 모든 출구·NPC·장터·게시판 앞 칸까지 걸어갈 수 있어야 한다.
- --check: 파일을 쓰지 않고 현재 JSON이 생성 결과와 같은지만 확인한다.
"""
import argparse
import json
import os
import sys
from collections import deque

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import arabia  # noqa: E402
import arabia_art  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
TILE_DIR = os.path.join(ROOT, 'Assets/PocketCaravan/Art/NinjaAdventure/Tiles')
TILE_ASSET_DIR = os.path.join(ROOT, 'Assets/PocketCaravan/Art/NinjaAdventure/TileAssets')
MAP_DIR = os.path.join(ROOT, 'Assets/PocketCaravan/Maps')

DEFAULTS = dict(w=1, h=1, id="", npc="", town="", target="", spawn="", edge="", text="", name="", character="",
                lines=[], variant=0, idx=0, sheet="", frames=0, fps=0.0, radius=0.0, type="", path=[])
BLOCKING_KINDS = {'npc', 'market', 'talker', 'villager', 'sign', 'board'}


def name_of(key):
    return key.replace(":", "_").replace("@", "_")


def obj_json(o):
    d = dict(DEFAULTS)
    d.update(o)
    d["path"] = [v for pt in o.get("path", []) for v in pt]
    return d


def to_json(m):
    palette, index = [""], {"": 0}

    def flat(layer):
        out = []
        for row in layer:
            for k in row:
                n = name_of(k) if k else ""
                if n not in index:
                    index[n] = len(palette)
                    palette.append(n)
                out.append(index[n])
        return out
    d = dict(id=m.id, name=m.name, w=m.w, h=m.h, hours=m.hours,
             ground=flat(m.ground), deco=flat(m.deco), obj=flat(m.obj), high=flat(m.high),
             road=[g for row in m.road for g in row],
             blocked=[1 if b else 0 for row in m.blocked for b in row],
             objects=[obj_json(o) for o in m.objects])
    d["palette"] = palette
    if getattr(m, 'obstacles', None):
        d["obstacles"] = m.obstacles          # 팀 장애물 시스템 배치 (MapJsonBuilder → ObstacleField)
    return d


def check_tiles(d, errors, new_tile_assets):
    for name in d["palette"][1:]:
        png = os.path.join(TILE_DIR, name + '.png')
        if name.startswith('arabia_'):
            if not os.path.isfile(png) and name not in arabia_art.USED:
                errors.append(f'{d["id"]}: 아라비아 조각 없음 {name}')
            elif not os.path.isfile(png + '.meta'):
                new_tile_assets.add(name)
            continue
        if not (os.path.isfile(png) and os.path.isfile(png + '.meta')):
            errors.append(f'{d["id"]}: 프로젝트에 없는 타일 {name}')
        elif not os.path.isfile(os.path.join(TILE_ASSET_DIR, name + '.asset')):
            new_tile_assets.add(name)


def walk_check(d, errors):
    w, h = d['w'], d['h']
    walk = [[d['ground'][y * w + x] != 0 and not d['blocked'][y * w + x] for x in range(w)] for y in range(h)]
    for o in d['objects']:
        if o['kind'] in BLOCKING_KINDS:
            walk[o['y']][o['x']] = False
    starts = [o for o in d['objects'] if o['kind'] == 'spawn']
    for s in starts:
        seen = set()
        q = deque()
        for yy in range(s['y'], s['y'] + s['h']):
            for xx in range(s['x'], s['x'] + s['w']):
                if not walk[yy][xx]:
                    errors.append(f"{d['id']}: 스폰 {s['id']} 칸 ({xx},{yy})이 막힘")
                else:
                    q.append((xx, yy)); seen.add((xx, yy))
        while q:
            x, y = q.popleft()
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < w and 0 <= ny < h and walk[ny][nx] and (nx, ny) not in seen:
                    seen.add((nx, ny)); q.append((nx, ny))
        for o in d['objects']:
            if o['kind'] == 'exit':
                cells = [(xx, yy) for yy in range(o['y'], o['y'] + o['h']) for xx in range(o['x'], o['x'] + o['w'])]
                if not all(c in seen for c in cells):
                    errors.append(f"{d['id']}: 스폰 {s['id']}에서 출구 →{o['target']} 전체 폭에 닿지 못함")
            if o['kind'] in ('npc', 'market', 'board'):
                adj = [(o['x'] + dx, o['y'] + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
                if not any(c in seen for c in adj):
                    errors.append(f"{d['id']}: 스폰 {s['id']}에서 {o['kind']} {o.get('npc') or o.get('town')} 앞에 닿지 못함")
    # 출구 바로 안쪽 스폰과 출구가 같은 폭인지
    exits = [o for o in d['objects'] if o['kind'] == 'exit']
    for e in exits:
        if e['w'] != 1 and e['h'] != 1:
            errors.append(f"{d['id']}: 출구는 한 줄이어야 함")


def render(d, path):
    from PIL import Image
    cache = {}

    def tile(name):
        if name not in cache:
            path = os.path.join(TILE_DIR, name + '.png')
            cache[name] = arabia_art.USED[name] if name in arabia_art.USED else Image.open(path).convert('RGBA')
        return cache[name]
    w, h = d['w'], d['h']
    img = Image.new('RGBA', (w * 16, h * 16), (0, 0, 0, 255))
    for layer in ('ground', 'deco', 'obj', 'high'):
        for i, v in enumerate(d[layer]):
            if v:
                img.alpha_composite(tile(d['palette'][v]), ((i % w) * 16, (i // w) * 16))
    colors = {"spawn": (60, 140, 255, 200), "exit": (255, 140, 0, 160), "npc": (255, 230, 60, 230),
              "market": (255, 80, 80, 230), "board": (180, 80, 255, 230), "villager": (120, 255, 120, 230),
              "talker": (120, 255, 120, 230), "sign": (255, 255, 255, 230)}
    for o in d['objects']:
        c = colors.get(o['kind'])
        if not c:
            continue
        for yy in range(o['y'], o['y'] + o['h']):
            for xx in range(o['x'], o['x'] + o['w']):
                img.alpha_composite(Image.new('RGBA', (8, 8), c), (xx * 16 + 4, yy * 16 + 4))
    img = img.resize((img.width * 2, img.height * 2), Image.NEAREST)
    img.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true')
    ap.add_argument('--preview')
    args = ap.parse_args()
    errors, new_assets, outputs = [], set(), []
    import arabia_paint
    arabia_paint.build_all()          # 직접 그린 나무·덤불 (drawn/)
    arabia_art.rescan()
    warnings = []
    for fn in arabia.MAPS:
        m = fn()
        warnings += getattr(m, 'warnings', [])
        d = to_json(m)
        check_tiles(d, errors, new_assets)
        walk_check(d, errors)
        outputs.append(d)
    for wmsg in warnings:
        print('WARN:', wmsg)
    if errors:
        for e in errors:
            print('ERROR:', e)
        return 1
    if not args.check:
        n = arabia_art.export_tiles()
        print(f'아라비아 조각 {len(arabia_art.USED)}개 (새로 쓴 것 {n}개)')
        print(f'아라비아 캐릭터 프레임 새로 쓴 것 {arabia_art.export_characters()}개')
    changed = []
    for d in outputs:
        path = os.path.join(MAP_DIR, d['id'] + '.json')
        text = json.dumps(d, ensure_ascii=False, separators=(",", ":"))
        old = open(path, encoding='utf-8').read() if os.path.exists(path) else None
        if old != text:
            changed.append(d['id'])
            if not args.check:
                with open(path, 'w', encoding='utf-8') as fp:
                    fp.write(text)
        if args.preview:
            os.makedirs(args.preview, exist_ok=True)
            render(d, os.path.join(args.preview, 'preview_' + d['id'] + '.png'))
    print(f"{len(outputs)} maps OK; {'differs' if args.check else 'written'}: {changed or 'none'}")
    if new_assets:
        print(f'Unity가 처음 가져올 타일(.meta 없음) {len(new_assets)}개')
    return 1 if (args.check and changed) else 0


if __name__ == '__main__':
    sys.exit(main())
