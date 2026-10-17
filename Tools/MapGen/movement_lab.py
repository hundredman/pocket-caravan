"""튜토리얼과 독립된 이동·운반 비교 맵. python3 Tools/MapGen/movement_lab.py"""
import json
from pathlib import Path
from tiles import Map, K, tile_image, STAMPS


def build():
    m = Map('MovementLab', '이동·운반 시험장', 90, 42, seed=52)
    m.river_v(58, 60, wiggle=1, fixed_rows=list(range(6, 11)) + list(range(17, 23)) + list(range(27, 33)))
    m.road_rect(3, 23, 13, 34, grade=3)
    m.road_rect(76, 23, 86, 34, grade=3)
    m.road_path([(9, 26), (9, 7), (79, 7), (79, 26)], width=3, grade=3)
    m.road_path([(12, 26), (21, 19), (34, 18), (45, 21), (58, 19), (69, 21), (78, 26)], width=2, grade=1)
    # 일직선 풀밭 지름길: 길 등급은 0으로 유지해 운반 조건을 비교한다.
    m.reserve(13, 28, 75, 31)
    for bounds, grade in (((57, 7, 61, 9), 3), ((57, 19, 61, 21), 1)):
        m.bridge(*bounds)
        x0, y0, x1, y1 = bounds
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                m.road[y][x] = grade
    # 얕은 여울: 통행 가능한 풀밭 등급, 돌과 물 그림으로 표시한다.
    for y in range(28, 32):
        for x in range(57, 62):
            m.water[y][x] = False
            m.ground[y][x] = K('water', 1, 7)
            m.blocked[y][x] = False
            m.reserved[y][x] = True
    for x in (57, 59, 61):
        for dy in range(2):
            for dx in range(2):
                m.deco[29 + dy][x + dx] = K('nature', 14 + dx, 8 + dy)
    # 길 사이 숲은 묶음 단위로 배치. 경로를 침범하지 않는다.
    m.scatter('pinebig', 10, area=(17, 11, 72, 15), margin=0)
    m.scatter('tree', 12, area=(17, 23, 71, 26), margin=0)
    m.scatter('roundbig', 7, area=(15, 34, 72, 37), margin=0)
    m.stamp('house_thatch', 4, 18)
    m.stamp('house_tile', 80, 18)
    m.forest_border(thickness=2)
    m.add('spawn', 8, 28, id='S0')
    m.finish()
    return m


def export():
    m = build()
    root = Path(__file__).resolve().parents[2]
    dest = root / 'Assets/CaravanMovementLab/Maps'
    dest.mkdir(parents=True, exist_ok=True)
    palette = ['']; ids = {'': 0}; used = set()
    def flat(layer):
        result = []
        for row in layer:
            for key in row:
                name = key.replace(':', '_').replace('@', '_') if key else ''
                if name not in ids:
                    ids[name] = len(palette); palette.append(name)
                if key: used.add(key)
                result.append(ids[name])
        return result
    data = dict(id=m.id, name=m.name, w=m.w, h=m.h, hours=0,
                ground=flat(m.ground), deco=flat(m.deco), obj=flat(m.obj), high=flat(m.high),
                road=[v for row in m.road for v in row], blocked=[int(v) for row in m.blocked for v in row],
                objects=[dict(kind='spawn', x=8, y=28, w=1, h=1, id='S0')], palette=palette)
    (dest / 'MovementLab.json').write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':')))
    tiles = root / 'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    for key in used:
        path = tiles / (key.replace(':', '_').replace('@', '_') + '.png')
        if not path.exists(): tile_image(key).save(path)
    output = root / 'output/movement-lab'; output.mkdir(parents=True, exist_ok=True)
    m.render(str(output / 'map-preview.png'))
    return m


if __name__ == '__main__':
    m = export()
    print(f'{m.id}: {m.w} x {m.h}')
