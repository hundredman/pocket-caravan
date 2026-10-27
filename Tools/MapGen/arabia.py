"""아라비아 맵 원본: 지역 7조각(마을 5 + 야생 2, 연속 지형) + 중앙 남쪽 6조각 = 13개 씬.

- 2026-10-22부터 세 맵 모두 사용자 제작 아라비아 에셋으로 만든다 (arabia_art.py, arabia_ahsa/farmroad/zabid.py).
- 실제 지리·도시 규모·거리의 재현이 아니라 게임용 압축 배치다. 지명·배치·NPC는 제안(1안)이다.
- JSON 내보내기는 arabia_export.py가 담당한다. 공용 export.py/지형 약도(atlasRegions)는 건드리지 않는다.
- 좌표는 좌상단 기준 타일 좌표(JSON y는 아래로 증가). Unity 셀 y는 빌더가 뒤집는다.
"""
from tiles import Map

AHSA, FARMROAD, ZABID = 'arabia_ahsa', 'arabia_farmroad', 'arabia_zabid'
WORKSHOP, TAIZ, PORTROAD, JEDDAH = 'arabia_workshoproad', 'arabia_taiz', 'arabia_portroad', 'arabia_jeddah'
SHORTCUT, SANAA, HIGHROAD = 'arabia_shortcut', 'arabia_sanaa', 'arabia_highroad'
# 중앙 대륙 남쪽 구역 (아라비아 담당) — 같은 틀(320×160)을 6조각으로 나눈 씬 (arabia_central.py)
C_DUNES, C_CANYON = 'arabia_central_dunes', 'arabia_central_canyon'
C_WEST, C_BAZAAR, C_EAST = 'arabia_central_westvale', 'arabia_central_bazaar', 'arabia_central_eastvale'
C_STEPPE = 'arabia_central_steppe'
CENTRAL_MAPS = [C_DUNES, C_CANYON, C_WEST, C_BAZAAR, C_EAST, C_STEPPE]

# 연결표: (맵, 경계, 스폰 키) ↔ (맵, 경계, 스폰 키). 경계 폭은 모두 4칸.
# 2026-10-27 오픈월드: 아라비아 지역은 지형 하나를 7조각으로 자른 연속 지도 (arabia_world.py).
#   조각 사이는 걸을 수 있는 경계 전부가 이음새(seam)로 자동 연결되므로 여기엔 중앙 대륙으로 가는 문만 둔다.
CONNECTIONS = [
    ((SANAA, 'N', 'n_01'), (C_DUNES, 'S', 's_01')),          # 사나 북문 → 중앙 대륙 남부 (큰 문)
    ((SHORTCUT, 'N', 'n_01'), (C_DUNES, 'S', 's_02')),       # 빈 사막 북쪽 모래길 → 모래바람 오아시스
    ((ZABID, 'N', 'n_01'), (C_CANYON, 'S', 's_01')),         # 자비드 북쪽 협곡길 → 붉은 바위 협곡
    # 중앙 남쪽 구역 6조각 사이도 오픈월드 이음새로 자동 연결 (arabia_central.add_seams)
]
SPAWN = {}
for a, b in CONNECTIONS:
    for m_, e_, k_ in (a, b):
        SPAWN[(m_, k_)] = f'{m_}_spawn_{k_}'
# 예전 키 (맵, 경계) 형식도 유지 (첫 구간 코드 호환)
for (m_, k_), v in list(SPAWN.items()):
    if k_.endswith('_01'):
        SPAWN.setdefault((m_, k_[0].upper()), v)


def links(map_id):
    """이 맵의 출구 목록: {스폰 키: (경계, 목적 맵, 목적 스폰, 내 스폰)}"""
    out = {}
    for a, b in CONNECTIONS:
        for me, other in ((a, b), (b, a)):
            if me[0] == map_id:
                out[me[2]] = (me[1], other[0], SPAWN[(other[0], other[2])], SPAWN[(me[0], me[2])])
    return out


REGION_MAPS = [AHSA, FARMROAD, SHORTCUT, TAIZ, ZABID, JEDDAH, SANAA]   # 옛 길 맵 3개(공방길·항구길·큰길)는 연속 지형에 흡수


def _chunk(map_id):
    def f():
        import arabia_world
        return _patched(lambda: arabia_world.get_chunk(map_id, links))()
    f.__name__ = '_' + map_id.split('_', 1)[1]
    return f


# 오토타일 조각 중 프로젝트에 내보내지 않은 것 → 이미 있는 비슷한 조각 (에셋 팩 없이 생성하기 위함)
FALLBACK = {
    'floor:3:0': 'floor:3:1',    # 큰길: 남쪽으로만 이어진 끝 → 세로 띠
    'floor:3:3': 'floor:1:1',    # 큰길: 외톨이 → 가운데
    'floor:3:10': 'floor:1:8',   # 흙길: 외톨이 → 가운데
    'water:3:7': 'water:1:7',    # 물: 세로 띠 → 가운데
    'water:1:9': 'water:1:7',    # 물: 가로 띠 → 가운데
    'water:3:9': 'water:1:7',    # 물: 외톨이 → 가운데
}


def _patched(fn):
    def build():
        m = fn()
        for layer in (m.ground, m.deco, m.obj, m.high):
            for row in layer:
                for i, k in enumerate(row):
                    if k in FALLBACK:
                        row[i] = FALLBACK[k]
        return m
    build.__name__ = fn.__name__
    return build


ahsa, farmroad, shortcut = _chunk(AHSA), _chunk(FARMROAD), _chunk(SHORTCUT)
taiz, zabid, jeddah, sanaa = _chunk(TAIZ), _chunk(ZABID), _chunk(JEDDAH), _chunk(SANAA)


def _central(map_id):
    def f():
        import arabia_central
        return _patched(lambda: arabia_central.get_piece(map_id, links))()
    f.__name__ = '_' + map_id.split('_', 2)[2]
    return f


central_dunes, central_canyon = _central(C_DUNES), _central(C_CANYON)
central_westvale, central_bazaar, central_eastvale = _central(C_WEST), _central(C_BAZAAR), _central(C_EAST)
central_steppe = _central(C_STEPPE)
MAPS = [ahsa, farmroad, shortcut, taiz, zabid, jeddah, sanaa,
        central_dunes, central_canyon, central_westvale, central_bazaar, central_eastvale, central_steppe]
