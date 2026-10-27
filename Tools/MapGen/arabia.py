"""아라비아 맵 원본: 수도1(사나)+마을4(알 아흐사·자비드·타이즈·제다)+길5 = 10개 씬.

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

# 연결표 (Docs/Arabia/02 §1·§3): (맵, 경계, 스폰 키) ↔ (맵, 경계, 스폰 키). 경계 폭은 모두 4칸.
# 2026-10-26: 사나 북문은 중앙 대륙 남쪽 구역(arabia_central)으로 나가는 관문.
#   안전한 큰길: 제다 북문 → 큰길(남→동) → 사나 서문 / 험한 지름길: 제다 동문 → 지름길(서→북) → 사나 남문.
CONNECTIONS = [
    ((AHSA, 'E', 'e_01'), (FARMROAD, 'W', 'w_01')),
    ((FARMROAD, 'E', 'e_01'), (ZABID, 'W', 'w_01')),
    ((ZABID, 'E', 'e_01'), (WORKSHOP, 'W', 'w_01')),
    ((WORKSHOP, 'E', 'e_01'), (TAIZ, 'W', 'w_01')),
    ((TAIZ, 'E', 'e_01'), (PORTROAD, 'W', 'w_01')),
    ((PORTROAD, 'E', 'e_01'), (JEDDAH, 'W', 'w_01')),
    ((JEDDAH, 'E', 'e_01'), (SHORTCUT, 'W', 'w_01')),
    ((SHORTCUT, 'N', 'n_01'), (SANAA, 'S', 's_01')),
    ((JEDDAH, 'N', 'n_01'), (HIGHROAD, 'S', 's_01')),
    ((HIGHROAD, 'E', 'e_01'), (SANAA, 'W', 'w_01')),
    ((SANAA, 'N', 'n_01'), (C_DUNES, 'S', 's_01')),          # 사나 북문 → 중앙 대륙 남부
    ((HIGHROAD, 'N', 'n_01'), (C_DUNES, 'S', 's_02')),       # 큰길 옛길 (낙석을 치우면 열리는 지름길)
    ((C_DUNES, 'E', 'e_01'), (C_CANYON, 'W', 'w_01')),       # 와디 길
    ((C_DUNES, 'E', 'e_02'), (C_CANYON, 'W', 'w_02')),       # 좁은 틈 → 협곡 아랫길
    ((C_DUNES, 'N', 'n_01'), (C_WEST, 'S', 's_01')),
    ((C_DUNES, 'N', 'n_02'), (C_BAZAAR, 'S', 's_01')),       # 큰 대상로 → 장터 남문
    ((C_CANYON, 'N', 'n_01'), (C_BAZAAR, 'S', 's_02')),
    ((C_CANYON, 'N', 'n_02'), (C_EAST, 'S', 's_01')),
    ((C_WEST, 'E', 'e_01'), (C_BAZAAR, 'W', 'w_01')),
    ((C_EAST, 'W', 'w_01'), (C_BAZAAR, 'E', 'e_01')),
    ((C_BAZAAR, 'N', 'n_01'), (C_STEPPE, 'S', 's_02')),      # 장터 북문 → 한가운데 초원
    ((C_WEST, 'N', 'n_01'), (C_STEPPE, 'S', 's_01')),        # 숨은 고갯길 (가시덤불)
    ((C_EAST, 'N', 'n_01'), (C_STEPPE, 'S', 's_03')),        # 숨은 고갯길 (바위)
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


def _ahsa():
    import arabia_ahsa
    return arabia_ahsa.build(FARMROAD, SPAWN[(FARMROAD, 'W')], SPAWN[(AHSA, 'E')])


def _farmroad():
    import arabia_farmroad
    return arabia_farmroad.build((AHSA, SPAWN[(AHSA, 'E')], SPAWN[(FARMROAD, 'W')]),
                                 (ZABID, SPAWN[(ZABID, 'W')], SPAWN[(FARMROAD, 'E')]))


def _zabid():
    import arabia_zabid
    L = links(ZABID)
    return arabia_zabid.build(L['w_01'][1:], L['e_01'][1:])


def _region(module, map_id):
    def f():
        mod = __import__(module)
        return mod.build(links(map_id))
    f.__name__ = '_' + module.split('_', 1)[1]
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


ahsa, farmroad, zabid = _patched(_ahsa), _patched(_farmroad), _patched(_zabid)
workshoproad = _region('arabia_workshoproad', WORKSHOP)
taiz = _region('arabia_taiz', TAIZ)
portroad = _region('arabia_portroad', PORTROAD)
jeddah = _region('arabia_jeddah', JEDDAH)
shortcut = _region('arabia_shortcut', SHORTCUT)
sanaa = _region('arabia_sanaa', SANAA)
highroad = _region('arabia_highroad', HIGHROAD)


def _central(map_id):
    def f():
        import arabia_central
        return _patched(lambda: arabia_central.build_piece(map_id, links(map_id)))()
    f.__name__ = '_' + map_id.split('_', 2)[2]
    return f


central_dunes, central_canyon = _central(C_DUNES), _central(C_CANYON)
central_westvale, central_bazaar, central_eastvale = _central(C_WEST), _central(C_BAZAAR), _central(C_EAST)
central_steppe = _central(C_STEPPE)
MAPS = [ahsa, farmroad, zabid, workshoproad, taiz, portroad, jeddah, shortcut, sanaa, highroad,
        central_dunes, central_canyon, central_westvale, central_bazaar, central_eastvale, central_steppe]
