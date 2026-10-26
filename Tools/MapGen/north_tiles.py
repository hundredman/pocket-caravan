"""북유럽(눈) 지역용 타일과 맵. tiles.py는 고치지 않고, 눈 바닥·눈 길·눈 물가와 눈 나무 묶음만 더한다."""
import tiles
from tiles import Map, K

# 눈 바닥 (TilesetFloor 아래쪽 흰 묶음). 배치는 풀밭 묶음(0,7)~(4,13)과 같고 7줄 아래에 있다
SNOW = K("floor", 0, 19)
SNOW_VARIANTS = [K("floor", 1, 19), K("floor", 2, 19), K("floor", 3, 19), K("floor", 4, 19)]
SNOW_PATH_ORIGIN = ("floor", 0, 14)     # 눈밭 위 다져진 길
SNOW_WATER_ORIGIN = ("water", 13, 0)    # 눈 덮인 물가 (안쪽은 얼음 무늬: 얼어붙은 피오르)
SNOW_PROPS = [K("nature", 10, 12), K("nature", 4, 12), K("nature", 5, 12), K("nature", 10, 13)]   # 눈 더미·눈 얹힌 돌

# 눈 나무와 북유럽 건물 (sheet, x0, y0, w, h, 막는 줄 시작, 위층 줄 수, 색 바꾸기)
tiles.STAMPS.update({
    "snowpine": ("nature", 10, 0, 2, 2, 1, 1, None),        # 눈 덮인 전나무
    "snowpine_g": ("nature", 8, 0, 2, 2, 1, 1, None),       # 눈이 덜 덮인 전나무
    "snowround": ("nature", 12, 0, 2, 2, 1, 1, None),       # 눈 덮인 둥근 나무
    "snowpinebig": ("nature", 8, 5, 4, 3, 2, 2, None),
    "snowpinebig_g": ("nature", 4, 5, 4, 3, 2, 2, None),
    "snowroundbig": ("nature", 8, 2, 4, 3, 2, 2, None),
    "snowboulder": ("nature", 2, 12, 2, 2, 0, 0, None),     # 눈 얹힌 큰 바위
    "longhall": ("house", 25, 7, 4, 7, 0, 0, None),         # 초가 큰 회관 (바이킹 롱하우스)
    # 10/27 회관을 지붕(위 3줄) + 문 있는 앞면(아래 2줄)으로 이어 붙여 씀 (원래 그림은 집 두 채가 위아래로 겹쳐 보였다)
    "longhall_roof": ("house", 25, 7, 4, 3, 0, 0, None),
    "longhall_front": ("house", 25, 12, 4, 2, 0, 0, None),
    "woodhouse": ("house", 25, 14, 4, 5, 0, 0, None),       # 짙은 나무 박공집
    # --- 마을 꾸미기 (10/23) ---
    "fire": ("camp", 12, 5, 2, 2, 0, 0, None),              # 돌 두른 모닥불
    "spit": ("camp", 8, 3, 2, 2, 1, 1, None),               # 고기 굽는 꼬치 화덕
    "logs": ("camp", 0, 0, 2, 1, 0, 0, None),               # 장작 더미 (가로)
    "logs_birch": ("camp", 2, 0, 2, 1, 0, 0, None),         # 자작나무 장작
    "logbench": ("camp", 0, 7, 3, 1, 0, 0, None),           # 통나무 벤치
    "stump": ("camp", 3, 5, 1, 1, 0, 0, None),              # 그루터기 의자
    "pelt": ("camp", 10, 5, 2, 2, 2, 0, None),              # 바닥에 편 모피 (안 막음)
    "tent": ("camp", 4, 0, 3, 3, 1, 1, None),               # 가죽 천막 (3칸 폭: 2칸이면 오른쪽이 잘림)
    "crate": ("camp", 4, 8, 1, 1, 0, 0, None),              # 나무 상자
    "rope": ("camp", 10, 7, 2, 2, 2, 0, None),              # 감아 둔 밧줄 (안 막음)
    "lamp": ("camp", 6, 5, 1, 2, 1, 1, None),               # 등불 기둥
    "runestone": ("northart", 0, 0, 1, 2, 1, 1, None),      # 룬 새긴 선돌 (10/24: 묘비 같던 그림 대신 north_art.py로 그림)
    "runestone_b": ("northart", 1, 0, 1, 2, 1, 1, None),    # 뱀 띠 룬돌 (이끼)
    "picturestone": ("northart", 2, 0, 2, 2, 1, 1, None),   # 배·태양 무늬 큰 그림돌
    "stall": ("house", 16, 16, 3, 2, 1, 1, None),           # 천 덮은 장터 좌판
    "fishbasket": ("house", 16, 13, 1, 1, 0, 0, None),      # 생선 바구니
    "barrels3": ("house", 16, 15, 3, 1, 0, 0, None),        # 통 세 개
    "weaponrack": ("house", 14, 12, 2, 2, 1, 1, None),      # 창·도끼 걸이 (칼끝이 윗칸까지 있는 2x2 그림: 1칸이면 잘려 보임)
    "banner": ("house", 23, 20, 1, 2, 1, 1, None),          # 붉은 깃발 천
    "snowpost": ("house", 8, 17, 1, 2, 1, 1, None),         # 눈 얹힌 돌기둥
    # --- 맵 가장자리 숲 (10/23): 같은 나무만 두르지 않고 섞는다 ---
    "deadtree": ("nature", 4, 0, 2, 2, 1, 1, None),         # 잎 진 겨울나무
    "deadtreebig": ("nature", 0, 5, 4, 3, 2, 2, None),      # 잎 진 겨울나무 덤불
    "rockbig": ("nature", 16, 5, 4, 3, 1, 1, None),         # 회색 바위 무더기
    "rock": ("nature", 16, 8, 2, 2, 0, 0, None),            # 회색 바위
    "rocksmall": ("nature", 18, 9, 1, 1, 0, 0, None),
    "stumpbig": ("nature", 0, 8, 2, 2, 0, 0, None),         # 큰 그루터기
    "baretree": ("nature", 4, 9, 1, 1, 0, 0, None),         # 작은 앙상한 나무
    # --- 10/23 마을마다 다른 건물 (모두 잘림 확인함) ---
    "h_dark": ("house", 29, 0, 4, 4, 0, 0, None),          # 짙은 목조 상인 집 (비르카·나루)
    "h_shed": ("house", 0, 7, 3, 3, 0, 0, None),           # 판자 헛간·보트 창고
    "h_turf": ("house", 3, 8, 3, 2, 0, 0, None),           # 잔디 지붕 움집 (사냥꾼·고원)
    "h_round": ("house", 29, 4, 3, 4, 0, 0, None),         # 둥근 지붕 목욕 회관 (온천)
    "h_dome": ("house", 23, 0, 3, 3, 0, 0, None),          # 돌 둥근 집 (온천)
    "h_stoneA": ("house", 29, 11, 2, 2, 0, 0, None),       # 작은 돌집 (흙색)
    "h_stoneB": ("house", 29, 13, 2, 2, 0, 0, None),       # 작은 돌집 (이끼색)
    # --- 소품 (모두 한 칸 안에 들어오는 것만) ---
    "bigbarrel": ("camp", 4, 6, 2, 2, 0, 0, None),         # 큰 통 (잘리던 통 세 개 대신)
    "barrel": ("camp", 4, 5, 1, 1, 0, 0, None),
    "barrel_s": ("camp", 3, 6, 1, 1, 0, 0, None),
    "tub": ("element", 1, 0, 1, 1, 0, 0, None),
    "chest": ("element", 3, 1, 1, 1, 0, 0, None),
    "box": ("element", 0, 0, 1, 1, 0, 0, None),
    "sack": ("element", 1, 1, 1, 1, 0, 0, None),
    "sack_big": ("house", 16, 11, 2, 2, 0, 0, None),
    "basket": ("element", 2, 1, 1, 1, 0, 0, None),
    "basket_veg": ("house", 17, 13, 1, 1, 0, 0, None),
    "basket_apple": ("house", 18, 13, 1, 1, 0, 0, None),
    "crate_berry": ("house", 19, 14, 1, 1, 0, 0, None),
    "crate_goods": ("house", 20, 14, 1, 1, 0, 0, None),
    "potion": ("house", 21, 14, 1, 1, 0, 0, None),
    "jar_red": ("element", 3, 0, 1, 1, 0, 0, None),
    "pot": ("element", 4, 0, 1, 1, 0, 0, None),
    "bucket": ("element", 5, 0, 1, 1, 0, 0, None),
    "cauldron": ("element", 4, 1, 1, 1, 0, 0, None),
    "cauldron_snow": ("element", 5, 1, 1, 1, 0, 0, None),
    "kettle": ("camp", 6, 4, 1, 1, 0, 0, None),
    "firewood": ("element", 9, 0, 1, 1, 0, 0, None),
    "helm": ("element", 0, 1, 1, 1, 0, 0, None),            # 뿔 투구 장식
    "scarecrow": ("element", 15, 0, 1, 1, 0, 0, None),
    "bedroll": ("camp", 7, 7, 2, 1, 0, 0, None),
    "sled": ("element", 4, 4, 2, 1, 0, 0, None),            # 짐 실은 썰매
    "totem": ("house", 14, 14, 1, 2, 1, 1, None),           # 나무 조각 기둥
    "totem2": ("house", 15, 14, 1, 2, 1, 1, None),
    "table": ("house", 31, 16, 2, 1, 0, 0, None),
    "lantern": ("element", 6, 3, 1, 2, 1, 1, None),
    # --- 10/23 새 장소용 (잘림 확인함) ---
    "ruinhall": ("abandon", 16, 5, 4, 6, 0, 0, None),       # 이끼 덮인 옛 회관 (룬돌 언덕)
    "ruinhut": ("abandon", 0, 0, 4, 3, 0, 0, None),         # 무너진 돌집
    "ruinwall": ("abandon", 16, 3, 4, 2, 0, 0, None),       # 무너진 돌담
    "pit": ("nature", 7, 13, 1, 1, 0, 0, None),             # 땅속으로 난 구멍 (얼음 동굴 아래층 입구)
    "crys_o": ("nature", 0, 14, 1, 1, 0, 0, None),          # 호박빛 수정 덤불
    "crys_b": ("nature", 1, 14, 1, 1, 0, 0, None),          # 푸른 수정
    "crys_p": ("nature", 2, 14, 1, 1, 0, 0, None),          # 보라 수정
    "gem_o": ("nature", 0, 16, 1, 1, 0, 0, None),           # 호박 덩어리
    "gem_b": ("nature", 1, 16, 1, 1, 0, 0, None),           # 푸른 얼음 수정 덩어리
    "gem_p": ("nature", 2, 16, 1, 1, 0, 0, None),
    "haybale": ("camp", 0, 3, 3, 2, 0, 0, None),            # 짚단 더미 (우리 안 먹이, 3칸 폭: 2칸이면 오른쪽 짚단이 잘림)
    "haybale_s": ("camp", 3, 3, 1, 2, 0, 0, None),          # 세워 둔 짚단
})
tiles.SHEETS["camp"] = "Backgrounds/Tilesets/tileset_camp.png"
import os as _os, north_art as _art
if not _os.path.exists(_art.PATH):
    _art.make()
tiles.SHEETS["northart"] = _art.PATH          # 절대 경로라 PACK과 상관없이 읽힘
tiles.SHEETS["abandon"] = "Backgrounds/Tilesets/TilesetVillageAbandoned.png"

tiles.SHEETS["relief"] = "Backgrounds/Tilesets/TilesetRelief.png"
# 눈 덮인 절벽 언덕 (TilesetRelief 왼쪽 위 묶음): 윗면 3x3 + 아래 줄은 절벽면
CLIFF = {(cx, cy): K("relief", 1 + cx, cy) for cx in range(3) for cy in range(3)}
# 얼어붙은 웅덩이 (TilesetFloor 눈 위 얼음 묶음, 3x3) — 밟고 지나갈 수 있는 바닥 장식
ICE = {(cx, cy): K("floor", cx, 21 + cy) for cx in range(3) for cy in range(3)}
# 눈밭 가장자리 풀·돌 (안 막는 바닥 장식)
TUFTS = [K("nature", 9, 13), K("nature", 8, 13)] + [K("detail", x, 3) for x in range(8)]
BONES = [K("detail", 13, 0), K("detail", 14, 0)]   # 사냥꾼 마을 바닥 뼈

# 가장자리 숲 조합 (큰 것부터 채움). 마을마다 분위기를 다르게
BORDER_THEMES = {
    "pine": (["snowpinebig", "snowpinebig_g", "snowroundbig", "snowpinebig"], ["snowpine", "snowpine_g", "snowround", "snowpine"]),
    "hunt": (["snowpinebig", "deadtreebig", "snowpinebig_g", "deadtreebig"], ["snowpine", "deadtree", "snowpine_g", "stumpbig"]),
    "coast": (["rockbig", "snowroundbig", "snowpinebig", "rockbig"], ["rock", "snowboulder", "snowround", "snowpine"]),
    "tundra": (["rockbig", "deadtreebig", "snowroundbig", "rockbig"], ["rock", "deadtree", "snowboulder", "stumpbig", "snowround"]),
    "mixed": (["snowpinebig", "snowroundbig", "deadtreebig", "rockbig", "snowpinebig_g"], ["snowpine", "snowround", "deadtree", "rock", "snowpine_g", "snowboulder"]),
}

DENSE_KINDS = {
    "pine": ["snowpine"] * 4 + ["snowpine_g"] * 3 + ["snowround"] * 2 + ["deadtree"],
    "hunt": ["snowpine"] * 3 + ["deadtree"] * 3 + ["snowpine_g"] * 2 + ["snowround"],
    "coast": ["snowround"] * 4 + ["snowpine"] * 3 + ["snowpine_g"] * 2,
    "mixed": ["snowpine"] * 3 + ["snowround"] * 3 + ["snowpine_g"] * 2 + ["deadtree"] * 2,
    "tundra": ["deadtree"] * 3 + ["snowround"] * 3 + ["snowpine"] * 2,
}

# 울타리 (TilesetHouse 나무 판자 울타리): 뒤쪽 줄은 안쪽 그늘진 면, 앞쪽 줄은 판자 앞면, 옆은 기둥
# (10/23: 가는 난간 울타리가 허술해 보여서 판자 울타리로 교체)
FENCE = dict(tl=K("house", 8, 4), t=K("house", 13, 4), tr=K("house", 9, 4),
             l=K("house", 11, 4), r=K("house", 11, 4),
             bl=K("house", 8, 5), b=K("house", 12, 5), br=K("house", 9, 5))


# 온천 물: 눈 물가 타일의 얼음 줄무늬를 맑은 청록 물로 바꾼 색 (타일 이름 끝 @thaw)
_recolor = tiles.recolor
def _recolor_thaw(tile, mode):
    if mode != "thaw":
        return _recolor(tile, mode)
    px = tile.load()
    for y in range(tile.size[1]):
        for x in range(tile.size[0]):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in ((212, 245, 250), (113, 221, 238)):
                px[x, y] = (88, 196, 206, a)   # 얼음 줄무늬도 같은 물색으로 (줄무늬가 남으면 얼어 보임)
    return tile
tiles.recolor = _recolor_thaw


class SnowMap(Map):
    """바닥을 눈밭으로 칠하는 맵. 나머지 기능은 Map과 같다."""

    def __init__(self, *a, **kw):
        super().__init__(*a, **kw)
        self._late = []   # 마무리 뒤에 칠할 바닥 장식 (얼음·풀)
        self._springs = set()   # 얼지 않은 온천 물 칸
        self.life = []          # 돌아다니는 동물: (종, x0, y0, x1, y1, 마릿수) — NorthLife.json으로 내보냄
        self.fishing = []       # 얼음 낚시 자리 (x, y): 낚시 미니게임
        self.secrets = []       # 10/24 숨은 룬돌 (x, y, 번호 1~7): 전설 조각
        self.spots = []         # 10/24 상호작용 자리 (종류, x, y): ring 룬돌 고리 중심, bath 온천, feed 순록 우리, guild 낚시 조합, festival 축제 장수

    def snow_props(self, pts):
        for (x, y) in pts:
            if self.inside(x, y) and not self.reserved[y][x]:
                self.prop(self.rng.choice(SNOW_PROPS), x, y)

    def deco_stamp(self, name, x, y):
        """꾸미기 소품: 길 위에도 놓을 수 있게 force + over_road (마당 장식용).
        소품 밑의 길(광장) 바닥은 지우지 않는다: 지우면 광장 가운데에 소품 모양대로 눈밭 구멍이 뚫려 보인다."""
        _, _, _, w, h, *_ = tiles.STAMPS[name]
        saved = {(x + dx, y + dy): self.road[y + dy][x + dx] for dx in range(w) for dy in range(h) if self.inside(x + dx, y + dy)}
        ok = self.stamp(name, x, y, force=True, over_road=True)
        if ok:
            for (cx, cy), r in saved.items():
                self.road[cy][cx] = r
        return ok

    def fence(self, x0, y0, x1, y1, gap=None, gap_w=2):
        """판자 울타리로 두른 우리. gap=(x, y)부터 gap_w칸을 비워 드나든다 (위·아래 줄). 틈 양옆은 기둥 달린 끝 판자."""
        gaps = set()
        if gap:
            gaps = {(gap[0] + i, gap[1]) for i in range(gap_w)} if gap[1] in (y0, y1) else {(gap[0], gap[1] + i) for i in range(gap_w)}
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if (x, y) in gaps or not (x in (x0, x1) or y in (y0, y1)):
                    continue
                if (x, y) == (x0, y0): k = FENCE["tl"]
                elif (x, y) == (x1, y0): k = FENCE["tr"]
                elif (x, y) == (x0, y1): k = FENCE["bl"]
                elif (x, y) == (x1, y1): k = FENCE["br"]
                elif y in (y0, y1):
                    top = y == y0
                    if (x + 1, y) in gaps: k = FENCE["tr" if top else "br"]      # 틈 왼쪽: 오른쪽에 기둥
                    elif (x - 1, y) in gaps: k = FENCE["tl" if top else "bl"]    # 틈 오른쪽: 왼쪽에 기둥
                    else: k = FENCE["t" if top else "b"]
                else:
                    k = FENCE["l"] if x == x0 else FENCE["r"]
                self.prop(k, x, y)

    def secret_rune(self, idx, x, y):
        """숨은 룬돌 (전설 조각 idx). 길에서 떨어진 곳에 이끼 낀 룬돌을 세우고, 앞(아래) 칸에서 읽는다. 밤에 은은히 빛난다."""
        for dx, dy in [(0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (1, 1), (-1, 1)]:
            X, Y = x + dx, y + dy
            if 1 < X < self.w - 2 and 1 < Y < self.h - 4 and not any(self.reserved[Y + b][X] or self.road[Y + b][X] or self.water[Y + b][X] for b in range(3)):
                if self.stamp("runestone_b", X, Y, force=True):
                    self.secrets.append((X, Y, idx))
                    self.reserve(X - 1, Y - 1, X + 1, Y + 3)
                    return True
        raise ValueError(f"{self.id}: 숨은 룬돌 {idx} 자리를 못 찾음 ({x},{y})")

    def spot(self, kind, x, y):
        """상호작용 자리 기록 (실제 서는 칸은 내보낼 때 가장 가까운 걷는 칸으로 맞춤)"""
        self.spots.append((kind, x, y))

    def critters(self, kind, area, count):
        """마을을 돌아다니는 동물 (보이기만 함, 충돌·대화 없음). kind: reindeer, goat, raven, gull"""
        self.life.append(dict(kind=kind, x0=area[0], y0=area[1], x1=area[2], y1=area[3], count=count))

    def scatter_deco(self, keys, area, count):
        """바닥 장식(안 막음)을 빈 눈밭에 흩뿌린다."""
        x0, y0, x1, y1 = area
        for _ in range(count):
            x, y = self.rng.randint(x0, x1), self.rng.randint(y0, y1)
            if self.inside(x, y) and not self.reserved[y][x] and not self.blocked[y][x]:
                self._late.append(((x, y), self.rng.choice(keys)))

    def boardwalk(self, x0, y0, x1, y1):
        """나무 판자 길·마루 (비르카·헤데비 같은 바이킹 교역 마을의 판자 거리). 걸을 수 있고 길로 친다. 난간 없음."""
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if not self.inside(x, y):
                    continue
                sx = 0 if x == x0 else 2 if x == x1 else 1
                sy = 12 if y == y0 else 14 if y == y1 else 13
                key = K("water", sx, sy, "wood")
                self.ground[y][x] = key
                self.decks[(x, y)] = key
                self.road[y][x] = max(self.road[y][x], 1)
                self.reserved[y][x] = True

    def ground_patch(self, cells, kind="dirt"):
        """10/26 눈밭 위 바닥 조각 (공방 마당). kind: dirt 다져진 흙, grass 눈 녹은 풀밭. 걸을 수 있음, 길 아님."""
        cells = set(cells)
        inside = lambda a, b: (a, b) in cells
        mode = "dirtedge" if kind == "dirt" else "grassedge"
        for (x, y) in cells:
            if self.inside(x, y) and (x, y) not in self.decks:
                self.ground[y][x] = tiles.autotile(inside, x, y, "floor", 0, 7) + "@" + mode
                self.reserved[y][x] = True

    def hot_spring(self, cx, cy, rx, ry, steam=4):
        """김 나는 온천 (얼지 않은 물). 가장자리는 눈이 녹은 흙빛 테두리, 물결과 김 애니메이션."""
        before = {(x, y) for y in range(self.h) for x in range(self.w) if self.water[y][x]}
        self.water_blob(cx, cy, rx, ry)
        cells = {(x, y) for y in range(self.h) for x in range(self.w) if self.water[y][x]} - before
        self._springs |= cells
        inner = [c for c in sorted(cells) if all((c[0] + a, c[1] + b) in cells for a in (-1, 0, 1) for b in (-1, 0, 1))]
        for (x, y) in self.rng.sample(inner, min(steam, len(inner))):
            self.objects.append(dict(kind="anim", x=x, y=y, sheet="north_steam", frames=4, fps=3.0 + self.rng.random()))

    def icefall(self, x, y0, y1):
        """얼어붙은 폭포 기둥 (한 칸 폭, 위는 눈 덮인 턱). 아래 끝은 얼어붙은 웅덩이로 이어 둔다. 막힌 칸."""
        for y in range(y0, y1 + 1):
            ry = 0 if y == y0 else 2 if y == y1 else 1
            self.prop(K("relief", 7, ry), x, y)

    def stone_ship(self, cx, cy, half, beam=3):
        """배 모양 돌무덤 (바이킹 무덤 돌 배치): 뾰족한 타원 둘레에 돌을 세운다. 양 끝은 눈 얹힌 높은 돌."""
        import math
        pts = set()
        for i in range(0, 360, 12):
            t = math.radians(i)
            x = cx + half * math.cos(t)
            y = cy + beam * math.sin(t) * abs(math.cos(t)) ** 0.0 * (1 - abs(math.cos(t)) ** 3) ** 0.5
            pts.add((int(round(x)), int(round(y))))
        for (x, y) in sorted(pts):
            if not self.inside(x, y) or self.obj[y][x] or self.high[y][x]:
                continue
            if (x, y) in ((int(round(cx - half)), cy), (int(round(cx + half)), cy)):
                self.stamp("snowpost", x, y - 1, force=True, over_road=True)
            elif (x + y) % 3:
                self.stamp("runestone", x, y, force=True, over_road=True)
            else:
                self.prop(K("nature", 18, 9), x, y)

    def flag(self, x, y):
        """펄럭이는 깃발 (Animated/north_flag_0~3.png, export_north.py가 만든다)"""
        self.anim("north_flag", x, y, frames=4, fps=5.0)
        self.blocked[y][x] = True

    def ice_patch(self, x, y):
        """3x3 얼음 웅덩이 (밟을 수 있음, 바닥 장식층). 마무리 뒤에 칠한다."""
        self._late.extend(((x + cx, y + cy), k) for (cx, cy), k in ICE.items())

    def cliff(self, x0, y0, x1, y1, trees=0):
        """눈 덮인 절벽 언덕 (전부 막힘). 아래 3줄이 절벽면(눈 턱 → 바위면 → 밑동). trees: 윗면에 심을 나무 수."""
        # 절벽면 가운데 무늬: 같은 바위면이 길게 반복되지 않게 몇 가지를 섞는다 (TilesetRelief 5·8·9·10열)
        face = {x: self.rng.choice((5, 5, 8, 9, 10)) for x in range(x0, x1 + 1)}
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if not self.inside(x, y):
                    continue
                cx = 0 if (x == x0 and x0 > 0) else 2 if (x == x1 and x1 < self.w - 1) else 1
                if y == y1 - 2:   key = K("relief", 1 + cx, 2)      # 눈 턱 + 절벽 윗부분
                elif y == y1 - 1: key = K("relief", 4 + cx if cx != 1 else face[x], 1)   # 절벽 가운데 (무늬 섞기)
                elif y == y1:     key = K("relief", 4 + cx if cx != 1 else face[x], 2)   # 절벽 밑동
                else:             key = K("relief", 1 + cx, 0 if (y == y0 and y0 > 0) else 1)
                self.ground[y][x] = key
                self.blocked[y][x] = True
                self.reserved[y][x] = True
        placed = tries = 0
        while placed < trees and tries < trees * 30:
            tries += 1
            name = self.rng.choice(["snowpine", "snowpine_g", "snowround", "deadtree"])
            _, _, _, w, h, *_ = tiles.STAMPS[name]
            x, y = self.rng.randint(x0, x1 - w + 1), self.rng.randint(y0, max(y0, y1 - 2 - h))
            if y + h - 1 < y1 - 2 and self.stamp(name, x, y, force=True):
                placed += 1

    def wild_border(self, theme="mixed", base=2, extra=2, tufts=0.18):
        """같은 나무를 두르지 않고, 두께가 들쭉날쭉한 숲·바위 가장자리. 가장자리는 빈틈 없이 막힌다."""
        rng = self.rng
        def profile(n):
            # 두께는 짝수(2칸 나무 격자에 맞춤), 4칸마다 바뀌어 들쭉날쭉하게
            knots = [rng.randint(0, extra // 2 if extra >= 2 else 1) for _ in range(n // 4 + 2)]
            return [base + 2 * knots[i // 4] for i in range(n)]
        top, bot, left, right = profile(self.w), profile(self.w), profile(self.h), profile(self.h)
        def edge(x, y):
            return y < top[x] or y >= self.h - bot[x] or x < left[y] or x >= self.w - right[y]
        mask = {(x, y) for y in range(self.h) for x in range(self.w)
                if edge(x, y) and not self.reserved[y][x] and self.road[y][x] == 0 and not self.water[y][x]
                and self.obj[y][x] is None and self.high[y][x] is None}
        big, small = BORDER_THEMES[theme]
        def fits(name, x, y):
            _, _, _, w, h, *_ = tiles.STAMPS[name]
            return all((x + dx, y + dy) in mask for dx in range(w) for dy in range(h))
        def run(x, y, dy):
            n = 0
            while (x, y) in mask:
                n += 1; y += dy
            return n
        def tidy(name, x, y):
            # 놓은 뒤 위·아래에 한 줄짜리 틈이 남으면(2칸 나무가 못 들어감) 놓지 않는다
            _, _, _, w, h, *_ = tiles.STAMPS[name]
            return all(run(x + dx, y + h, 1) != 1 and run(x + dx, y - 1, -1) != 1 for dx in range(w))
        def put(name, x, y):
            if fits(name, x, y) and tidy(name, x, y) and self.stamp(name, x, y):
                _, _, _, w, h, *_ = tiles.STAMPS[name]
                for dx in range(w):
                    for dy in range(h):
                        mask.discard((x + dx, y + dy)); self.blocked[y + dy][x + dx] = True
        cells = sorted(mask); rng.shuffle(cells)
        for (x, y) in cells:
            if rng.random() < 0.6 and x % 2 == 0 and y % 2 == 0:   # 큰 나무도 짝수 칸에서 시작
                put(rng.choice(big), x, y)
        for oy in (0, 1):                      # 2칸 나무를 격자에 맞춰 촘촘히
            for (x, y) in sorted(mask):
                if (x + y + oy) % 2 == 0:
                    put(rng.choice(small), x, y)
        # 남은 틈: 안쪽 빈 눈밭 한 칸을 함께 덮어서라도 2칸 나무로 메운다 (작은 공 모양이 줄지어 보이지 않게)
        def free(x, y):
            return self.inside(x, y) and ((x, y) in mask or (not self.reserved[y][x] and not self.blocked[y][x]
                   and self.road[y][x] == 0 and not self.water[y][x] and self.obj[y][x] is None and self.high[y][x] is None))
        for (x, y) in sorted(mask):
            if (x, y) not in mask:
                continue
            for name in rng.sample(small, len(small)):
                _, _, _, w, h, *_ = tiles.STAMPS[name]
                spots = [(x - dx, y - dy) for dx in range(w) for dy in range(h)]
                rng.shuffle(spots)
                ok = next(((sx, sy) for sx, sy in spots if all(free(sx + a, sy + b) for a in range(w) for b in range(h))), None)
                if ok and self.stamp(name, ok[0], ok[1], force=True):
                    for a in range(w):
                        for b in range(h):
                            mask.discard((ok[0] + a, ok[1] + b)); self.blocked[ok[1] + b][ok[0] + a] = True
                    break
        # 그래도 남은 한 줄짜리 틈 (집 바로 뒤 등): 가로 두 칸은 장작 더미, 나머지는 눈 더미·작은 바위·앙상한 나무
        for (x, y) in sorted(mask):
            if (x, y) in mask and (x + 1, y) in mask and rng.random() < 0.7 and self.stamp(rng.choice(["logs", "logs_birch"]), x, y, force=True):
                mask.discard((x, y)); mask.discard((x + 1, y))
                self.blocked[y][x] = self.blocked[y][x + 1] = True
        for (x, y) in sorted(mask):
            self.prop(rng.choice([K("nature", 5, 12), K("nature", 5, 12), K("nature", 18, 9), K("nature", 4, 9)]), x, y)
        # 숲 바로 앞 눈밭: 드문드문 풀·돌 (안 막음)
        for y in range(self.h):
            for x in range(self.w):
                if self.blocked[y][x] or self.reserved[y][x] or self.road[y][x] or self.water[y][x]:
                    continue
                near = any(self.inside(x + a, y + b) and self.blocked[y + b][x + a] for a in (-1, 0, 1) for b in (-1, 0, 1))
                if near and rng.random() < tufts:
                    self._late.append(((x, y), rng.choice(TUFTS)))

    def dense_border(self, theme="pine", base=3, extra=3, tufts=0.2):
        """포켓몬 숲길처럼 나무가 줄마다 한 칸씩 겹쳐 빽빽한 숲 가장자리 (절벽·울타리 같은 직선 벽 대신).
        2칸 나무를 한 줄 내릴 때마다 반 그루씩 엇갈려 심어, 아래 나무 윗부분(위층)이 윗나무 밑동을 덮는다.
        나무 종류는 테마별로 섞는다. 가장자리는 빈틈 없이 막힌다."""
        rng = self.rng
        kinds = DENSE_KINDS[theme]
        def profile(n):
            knots = [rng.randint(0, extra) for _ in range(n // 5 + 3)]
            return [base + round(knots[i // 5] + (knots[i // 5 + 1] - knots[i // 5]) * (i % 5) / 5) for i in range(n)]
        top, bot, left, right = profile(self.w), profile(self.w), profile(self.h), profile(self.h)
        def edge(x, y):
            return y < top[x] or y >= self.h - bot[x] or x < left[y] or x >= self.w - right[y]
        mask = {(x, y) for y in range(self.h) for x in range(self.w)
                if edge(x, y) and not self.reserved[y][x] and self.road[y][x] == 0 and not self.water[y][x]
                and self.obj[y][x] is None and self.high[y][x] is None}
        self._plant_dense(mask, kinds)
        # 숲 바로 앞: 눈 덮인 덤불·작은 바위·풀 (자연스러운 경계)
        for y in range(self.h):
            for x in range(self.w):
                if self.blocked[y][x] or self.reserved[y][x] or self.road[y][x] or self.water[y][x] or self.obj[y][x]:
                    continue
                near = any(self.inside(x + a, y + b) and (x + a, y + b) in mask for a in (-1, 0, 1) for b in (-1, 0, 1))
                if not near:
                    continue
                r = rng.random()
                if r < 0.03:
                    self.prop(rng.choice([K("nature", 5, 12), K("nature", 18, 9)]), x, y)
                elif r < 0.03 + tufts:
                    self._late.append(((x, y), rng.choice(TUFTS)))

    def _plant_dense(self, mask, kinds):
        """mask 칸을 2칸 나무로 줄마다 엇갈려 빽빽하게 채운다 (아래 나무 잎이 윗나무 밑동을 덮음). mask 칸은 모두 막힘."""
        rng = self.rng
        def ok(x, y):   # 맵 밖 칸은 괜찮다 (숲이 맵 밖으로 이어지는 느낌)
            return all(((x + a, y + b) in mask) or not self.inside(x + a, y + b) for a in (0, 1) for b in (0, 1)) \
                and any(self.inside(x + a, y + b) for a in (0, 1) for b in (0, 1))
        for y in range(-1, self.h):
            off = (y % 2)
            for x in range(-1 - off, self.w, 2):
                if not ok(x, y):
                    continue
                s_, sx, sy, *_ = tiles.STAMPS[rng.choice(kinds)]
                for a in (0, 1):
                    if self.inside(x + a, y) and self.high[y][x + a] is None:
                        self.high[y][x + a] = K(s_, sx + a, sy)
                    if self.inside(x + a, y + 1):
                        self.obj[y + 1][x + a] = K(s_, sx + a, sy + 1)
        for (x, y) in mask:
            self.blocked[y][x] = True; self.reserved[y][x] = True
            if self.obj[y][x] is None and self.high[y][x] is None:
                self.obj[y][x] = rng.choice([K("nature", 5, 12), K("nature", 18, 9)])

    def grove(self, cx, cy, rx, ry, theme="pine"):
        """맵 안쪽의 작은 숲 덩어리 (빈 눈밭이 너무 넓지 않게). 길·건물·물은 피한다."""
        mask = set()
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if not self.inside(x, y):
                    continue
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if d <= 1 + self.rng.uniform(-0.25, 0.1) and not self.reserved[y][x] and self.road[y][x] == 0 \
                        and not self.water[y][x] and self.obj[y][x] is None and self.high[y][x] is None \
                        and not any(self.inside(x + a, y + b) and self.road[y + b][x + a] for a in (-1, 0, 1) for b in (-1, 0, 1)):
                    mask.add((x, y))
        self._plant_dense(mask, DENSE_KINDS[theme])

    def stamp(self, name, x, y, force=False, over_road=False):
        """10/27 "longhall"은 지붕 + 문 앞면 두 조각으로 놓는다. 바닥(문 줄)은 예전 7줄짜리와 같은 자리 (y+6).
        기록은 "longhall" 하나로 남겨 굴뚝·지도·문 검사가 그대로 돌게 한다."""
        if name != "longhall":
            return Map.stamp(self, name, x, y, force, over_road)
        if not Map.stamp(self, "longhall_roof", x, y + 2, force, over_road):
            return False
        if not Map.stamp(self, "longhall_front", x, y + 5, force, over_road):
            return False
        self.stamps = [t for t in self.stamps if t not in (("longhall_roof", x, y + 2), ("longhall_front", x, y + 5))]
        self.stamps.append(("longhall", x, y))
        return True

    def fire_seats(self):
        """10/27 모닥불마다 바로 옆(왼쪽·오른쪽, 안 되면 아래 두 칸)에 앉는 자리 두 개. 런타임(NorthFireSeat)이 통나무 의자를 그리고 Space로 앉는다."""
        self._seats = set()
        objs = {(o["x"], o["y"]) for o in self.objects}
        def ok(x, y):
            return (self.inside(x, y) and 1 <= x < self.w - 1 and not self.blocked[y][x] and not self.water[y][x]
                    and self.obj[y][x] is None and (x, y) not in objs and (x, y) not in self._seats)
        for (n, x, y) in list(self.stamps):
            if n != "fire":
                continue
            got = 0
            for (sx, sy) in [(x - 1, y + 1), (x + 2, y + 1), (x, y + 2), (x + 1, y + 2), (x - 1, y), (x + 2, y)]:
                if got < 2 and ok(sx, sy):
                    self._seats.add((sx, sy)); self.spots.append(("seat", sx, sy)); self.reserved[sy][sx] = True
                    got += 1

    def break_lines(self):
        """10/27 대화창에서 한국어 낱말 가운데가 잘려 줄이 바뀌던 문제: 긴 대사는 문장마다 줄을 바꿔 둔다."""
        import re
        for o in self.objects:
            if o["kind"] != "talker":
                continue
            o["lines"] = [re.sub(r"([.!?…~])\s+(?=\S)", "\\1\n", ln) if len(ln) > 28 and "\n" not in ln else ln for ln in o.get("lines", [])]

    def settle_npcs(self):
        """10/27 서 있는 사람(talker·npc)이 길을 막지 않게, 이름표가 맵 끝에서 잘리지 않게 자리를 고른다.
        길 위나 맵 가장자리(좌우 4칸·위 3칸)에 서 있으면, 4칸 안에서 길 옆의 빈 땅(길 아님·안 막힘·물 아님)으로 옮긴다.
        길 바로 옆을 먼저 고르고, 같으면 원래 자리와 가까운 곳. 맞는 자리가 없으면 그대로 둔다."""
        taken = {(o["x"], o["y"]) for o in self.objects}
        def bad(x, y):
            return self.road[y][x] > 0 or x < 4 or x > self.w - 5 or y < 3
        def free(x, y):
            return (4 <= x <= self.w - 5 and 3 <= y <= self.h - 3 and self.road[y][x] == 0 and not self.water[y][x]
                    and not self.blocked[y][x] and self.obj[y][x] is None and (x, y) not in taken and (x, y) not in getattr(self, "decks", {})
                    and (x, y) not in getattr(self, "_seats", ()))
        def by_road(x, y):
            return any(self.inside(x + a, y + b) and self.road[y + b][x + a] > 0 for a in (-1, 0, 1) for b in (-1, 0, 1))
        self.npc_moves = []
        for o in self.objects:
            if o["kind"] not in ("talker", "npc") or not bad(o["x"], o["y"]):
                continue
            best = None
            for r in range(1, 5):
                for y in range(o["y"] - r, o["y"] + r + 1):
                    for x in range(o["x"] - r, o["x"] + r + 1):
                        if max(abs(x - o["x"]), abs(y - o["y"])) != r or not self.inside(x, y) or not free(x, y):
                            continue
                        key = (0 if by_road(x, y) else 1, abs(x - o["x"]) + abs(y - o["y"]))
                        if best is None or key < best[0]:
                            best = (key, x, y)
                if best is not None and best[0][0] == 0:
                    break
            if best is None:
                continue
            taken.discard((o["x"], o["y"]))
            self.npc_moves.append((o.get("name") or o.get("npc"), (o["x"], o["y"]), (best[1], best[2])))
            o["x"], o["y"] = best[1], best[2]
            taken.add((o["x"], o["y"]))

    def finish(self):
        """공용 Map.finish를 그대로 쓰되, 바닥·길·물가 타일만 눈 버전으로 바꿔 끼운다 (다리·판자 처리 등은 공용 코드를 따름)."""
        self.fire_seats()
        self.break_lines()
        self.settle_npcs()
        names = ("GRASS", "GRASS_VARIANTS", "DIRT_ORIGIN", "WATER_ORIGIN", "DECO")
        saved = {n: getattr(tiles, n) for n in names}
        tiles.GRASS, tiles.GRASS_VARIANTS = SNOW, SNOW_VARIANTS
        tiles.DIRT_ORIGIN, tiles.WATER_ORIGIN = SNOW_PATH_ORIGIN, SNOW_WATER_ORIGIN
        tiles.DECO = [None]          # 풀 장식은 눈밭에 안 어울려 비워 둔다
        try:
            Map.finish(self)
        finally:
            for n, v in saved.items():
                setattr(tiles, n, v)
        moved = self.npc_moves
        self.settle_npcs()          # 마무리에서 길이 다듬어진 뒤 한 번 더 (길 가장자리 메움 등)
        self.npc_moves = moved + self.npc_moves
        for (x, y), k in self._late:
            if self.inside(x, y) and not self.water[y][x] and self.road[y][x] == 0 and self.obj[y][x] is None:
                self.deco[y][x] = k
        # 온천: 얼음 무늬 대신 흙빛 테두리의 맑은 물 (TilesetWater 왼쪽 위 묶음, 눈 물가와 같은 배치)
        # (10/23 수정: 모래 테두리 묶음은 바탕이 모래색이라 눈 위에서 네모가 보였다 → 눈 물가 묶음을 쓰고,
        #  가운데 칸만 얼음 무늬 대신 맑은 물로 바꾼다: 가장자리만 살짝 얼고 가운데는 따뜻한 물)
        for (x, y) in self._springs:
            k = tiles.autotile(lambda a, b: (a, b) in self._springs, x, y, *SNOW_WATER_ORIGIN)
            self.ground[y][x] = (K("water", 1, 7) if k == K("water", 14, 1) else k) + "@thaw"
        # 물결 애니메이션은 넣지 않는다 (피오르가 얼어 있음)
        self.objects = [o for o in self.objects if not (o["kind"] == "anim" and o.get("sheet") == "ripples" and (o["x"], o["y"]) not in self._springs)]


class FitMap(SnowMap):
    """설계 좌표(design_w x design_h)로 적은 맵을 실제 크기(w x h)에 맞춰 줄여 그린다.
    10/23: 북유럽 전체를 1만 칸 이내로 맞추면서, 이미 정한 배치를 비율 그대로 유지하려고 만들었다.
    좌표·반지름만 줄이고 건물·소품 크기와 길 폭, 출구 폭(4칸)은 그대로 둔다."""

    def __init__(self, mid, name, w, h, design=None, **kw):
        super().__init__(mid, name, w, h, **kw)
        dw, dh = design or (w, h)
        self.fx, self.fy = w / dw, h / dh
        self._depth = 0

    def X(self, x): return int(round(x * self.fx))
    def Y(self, y): return int(round(y * self.fy))
    def XC(self, x): return min(self.w - 1, max(0, self.X(x)))
    def YC(self, y): return min(self.h - 1, max(0, self.Y(y)))
    def P(self, p): return (self.XC(p[0]), self.YC(p[1]))
    def A(self, a): return (self.XC(a[0]), self.YC(a[1]), self.XC(a[2]), self.YC(a[3]))


def _fit(method, conv):
    base = getattr(SnowMap, method)
    def wrapped(self, *a, **kw):
        if self._depth:               # 안쪽에서 다시 부른 것은 이미 줄인 좌표
            return base(self, *a, **kw)
        a, kw = conv(self, list(a), kw)
        self._depth += 1
        try:
            return base(self, *a, **kw)
        finally:
            self._depth -= 1
    setattr(FitMap, method, wrapped)


def _xy(i):        # a[i], a[i+1] = x, y
    def c(m, a, kw):
        a[i], a[i + 1] = m.XC(a[i]), m.YC(a[i + 1]); return a, kw
    return c

def _rect(i):
    def c(m, a, kw):
        a[i], a[i + 1], a[i + 2], a[i + 3] = m.XC(a[i]), m.YC(a[i + 1]), m.XC(a[i + 2]), m.YC(a[i + 3]); return a, kw
    return c

def _blob(m, a, kw):
    a[0], a[1], a[2], a[3] = a[0] * m.fx, a[1] * m.fy, max(1.5, a[2] * m.fx), max(1.2, a[3] * m.fy); return a, kw

def _pts(m, a, kw):
    a[0] = [m.P(p) for p in a[0]]; return a, kw

def _area_kw(m, a, kw):
    if "area" in kw and kw["area"]:
        kw["area"] = m.A(kw["area"])
    return a, kw

def _river(m, a, kw):
    a[0], a[1] = m.YC(a[0]), m.YC(a[1])
    if "fixed" in kw:
        f = kw["fixed"]; kw["fixed"] = range(m.X(f.start), m.X(f.stop))
    return a, kw

def _bridge(m, a, kw):
    x0 = m.XC(a[0]); w = a[2] - a[0]; y0, y1 = m.YC(a[1]), m.YC(a[3])
    if kw.get("dock"):
        a[0], a[2] = x0, x0 + w          # 부두 폭은 그대로
    else:
        a[0], a[2] = x0, m.XC(a[2])
    a[1], a[3] = y0, y1; return a, kw

def _door(m, a, kw):
    a[0], a[1], a[4] = m.XC(a[0]), m.YC(a[1]), m.YC(a[4]); return a, kw

def _link(m, a, kw):
    edge = a[0]
    a[1] = m.YC(a[1]) if edge in "EW" else m.XC(a[1])
    a[1] = min(a[1], (m.h if edge in "EW" else m.w) - a[2]); return a, kw

def _fence(m, a, kw):
    a, kw = _rect(0)(m, a, kw)
    if kw.get("gap"):
        kw["gap"] = m.P(kw["gap"])
    return a, kw

def _scatter(m, a, kw):
    if len(a) > 2 and a[2]:
        a[2] = m.A(a[2])
    return _area_kw(m, a, kw)

def _walker(m, a, kw):
    a[0] = [m.P(p) for p in a[0]]; return a, kw

def _critters(m, a, kw):
    a[1] = m.A(a[1]); return a, kw

def _snowprops(m, a, kw):
    a[0] = [m.P(p) for p in a[0]]; return a, kw

def _sdeco(m, a, kw):
    a[1] = m.A(a[1]); return a, kw


for _name, _conv in [
    ("road_path", _pts), ("road_blob", _blob), ("water_blob", _blob), ("hot_spring", _blob), ("grove", _blob),
    ("road_rect", _rect(0)), ("water_rect", _rect(0)), ("reserve", _rect(0)),
    ("river_h", _river), ("bridge", _bridge), ("door_path", _door), ("link", _link), ("fence", _fence),
    ("stamp", _xy(1)), ("deco_stamp", _xy(1)), ("prop", _xy(1)), ("add", _xy(1)), ("talker", _xy(0)),
    ("board", _xy(0)), ("sign", _xy(0)), ("bandit", _xy(0)), ("event", _xy(0)), ("flag", _xy(0)),
    ("anim", _xy(1)), ("laundry", _xy(0)), ("scatter", _scatter), ("walker", _walker), ("critters", _critters),
    ("snow_props", _snowprops), ("scatter_deco", _sdeco),
]:
    _fit(_name, _conv)


# 줄인 좌표에서 꾸미기 소품이 다른 것과 겹치면 바로 옆 칸으로 살짝 비켜 놓는다
_fit_deco = FitMap.deco_stamp
def _deco_nudge(self, name, x, y):
    if self._depth:
        return _fit_deco(self, name, x, y)
    X, Y = self.XC(x), self.YC(y)
    self._depth += 1
    try:
        for dx, dy in [(0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (2, 0), (-2, 0), (0, 2), (1, -1), (-1, -1)]:
            if SnowMap.deco_stamp(self, name, X + dx, Y + dy):
                return True
        return False
    finally:
        self._depth -= 1
FitMap.deco_stamp = _deco_nudge


def _icefall(m, a, kw):
    a[0], a[1], a[2] = m.XC(a[0]), m.YC(a[1]), m.YC(a[2]); return a, kw

def _ship(m, a, kw):
    a[0], a[1], a[2] = m.XC(a[0]), m.YC(a[1]), max(3, a[2] * m.fx); return a, kw

_fit("icefall", _icefall)
_fit("stone_ship", _ship)


# ---------- 10/24 중앙 경유 구역: 눈이 녹으며 지역 특색이 스며드는 맵 ----------
# 눈 위 풀 조각: 풀밭 묶음(0,7)을 색만 바꿔 씀 (바탕 풀→눈, 흙 덩어리→풀, 테두리 풀잎→서리 회색)
# 풀 위 눈 조각: 눈길 묶음(0,14)을 색만 바꿔 씀 (바탕 눈→풀, 하얀 덩어리→눈 바닥색, 회색 테두리 그대로)
_SNOWBG, _SNOWEDGE, _WHITE = (242, 234, 241), (210, 201, 201), (255, 255, 255)
_GRASS, _GRASS_D, _DIRT, _DIRT_D = (173, 188, 58), (168, 161, 41), (211, 134, 95), (189, 121, 89)
_PATCH_COLORS = {
    "snowedge": {_SNOWBG: _GRASS, _WHITE: _SNOWBG},
    "grassedge": {_GRASS: _SNOWBG, _GRASS_D: _SNOWEDGE, _DIRT: _GRASS, _DIRT_D: _GRASS_D},
    "dirtedge": {_GRASS: _SNOWBG, _GRASS_D: _SNOWEDGE},     # 10/26 눈 위 흙바닥 (공방 마당)
}
_recolor_prev = tiles.recolor
def _recolor_patch(tile, mode):
    table = _PATCH_COLORS.get(mode)
    if table is None:
        return _recolor_prev(tile, mode)
    px = tile.load()
    for y in range(tile.size[1]):
        for x in range(tile.size[0]):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in table:
                px[x, y] = table[(r, g, b)] + (a,)
    return tile
tiles.recolor = _recolor_patch

GRASS_PATCH_ORIGIN = ("floor", 0, 7)     # @grassedge 로 씀
SNOW_PATCH_ORIGIN = ("floor", 0, 14)     # @snowedge 로 씀

# 녹는 숲: 눈 덜 덮인 전나무와 초록 나무가 섞임 / 초록 숲: 중앙 가까이
DENSE_KINDS["thaw"] = ["snowpine_g"] * 3 + ["pine"] * 3 + ["snowround"] * 2 + ["tree"] * 2 + ["roundtree"]
DENSE_KINDS["green"] = ["pine"] * 4 + ["tree"] * 3 + ["roundtree"] * 2 + ["snowpine_g"]


class ThawMap(FitMap):
    """중앙 경유 구역 맵. base='snow'면 눈밭에 풀 조각이, base='grass'면 풀밭에 눈 조각이 군데군데 남는다.
    cover=(위쪽 비율, 아래쪽 비율): 조각이 덮는 정도. 위(북유럽 쪽)에서 아래(중앙 도시 쪽)로 갈수록 바뀐다.
    길·물 바로 옆은 조각을 두지 않는다 (다닌 길은 눈이 녹아 있음 / 길 가장자리 그림이 어색하지 않게)."""

    def __init__(self, mid, name, w, h, base="snow", cover=(0.1, 0.3), **kw):
        super().__init__(mid, name, w, h, **kw)
        self.base, self.cover = base, cover
        self._force_patch, self._no_patch = set(), set()

    def patch_area(self, x0, y0, x1, y1, on=True):
        """조각을 일부러 깔거나(on) 비우는(off) 네모 칸"""
        s = self._force_patch if on else self._no_patch
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                s.add((x, y))

    def _patch_mask(self):
        rng = self.rng
        top, bot = self.cover
        mask = set()
        # 크고 작은 둥근 덩어리를 높이별 비율만큼 뿌린다
        target = sum(top + (bot - top) * y / max(1, self.h - 1) for y in range(self.h)) * self.w
        tries = 0
        while len(mask) < target and tries < 4000:
            tries += 1
            cx, cy = rng.uniform(0, self.w), rng.uniform(0, self.h)
            want = top + (bot - top) * cy / max(1, self.h - 1)
            if rng.random() > want * 2.2:
                continue
            rx, ry = rng.uniform(2.5, 7), rng.uniform(2, 4.5)
            for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
                for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                    if self.inside(x, y) and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1 + rng.uniform(-0.2, 0.15):
                        mask.add((x, y))
        mask |= self._force_patch
        def bad(x, y):
            if not self.inside(x, y) or (x, y) in self._no_patch:
                return True
            return any(self.inside(x + a, y + b) and (self.road[y + b][x + a] > 0 or self.water[y + b][x + a]
                       or (x + a, y + b) in self.decks or (x + a, y + b) in self._springs)
                       for a in (-1, 0, 1) for b in (-1, 0, 1))
        mask = {c for c in mask if not bad(*c)}
        # 다듬기: 한 칸짜리 구멍은 메우고, 홀로 튀어나온 칸은 지운다
        for _ in range(3):
            n4 = lambda x, y: sum((x + a, y + b) in mask for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            add = {(x, y) for y in range(self.h) for x in range(self.w)
                   if (x, y) not in mask and not bad(x, y) and n4(x, y) >= 3}
            rem = {c for c in mask if n4(*c) <= 1}
            mask = (mask | add) - rem
        return mask

    def finish(self):
        if self.base == "snow":
            SnowMap.finish(self)
        else:
            self.fire_seats()
            self.break_lines()
            self.settle_npcs()
            Map.finish(self)
            moved = self.npc_moves
            self.settle_npcs()
            self.npc_moves = moved + self.npc_moves
            ice = set(ICE.values())
            for (x, y), k in self._late:      # 풀밭 맵에선 얼음 웅덩이만 (눈 풀 장식은 빼고)
                if k in ice and self.inside(x, y) and not self.water[y][x] and self.road[y][x] == 0 and self.obj[y][x] is None:
                    self.deco[y][x] = k
        mask = self._patch_mask()
        inside = lambda a, b: (a, b) in mask or (not self.inside(a, b) and (min(max(a, 0), self.w - 1), min(max(b, 0), self.h - 1)) in mask)
        if self.base == "snow":
            fill, origin, mode = (tiles.GRASS, tiles.GRASS_VARIANTS), GRASS_PATCH_ORIGIN, "grassedge"
        else:
            fill, origin, mode = (SNOW, SNOW_VARIANTS), SNOW_PATCH_ORIGIN, "snowedge"
        centre = K(origin[0], origin[1] + 1, origin[2] + 1)
        for (x, y) in mask:
            k = tiles.autotile(inside, x, y, *origin)
            if k == centre:
                self.ground[y][x] = self.rng.choice(fill[1]) if self.rng.random() < 0.12 else fill[0]
            else:
                self.ground[y][x] = k + "@" + mode
            if self.base == "grass" and self.deco[y][x] in tiles.DECO:
                self.deco[y][x] = None          # 눈 위엔 풀 장식 없음
            elif self.base == "snow" and self.deco[y][x] is None and self.obj[y][x] is None and self.rng.random() < 0.05:
                self.deco[y][x] = self.rng.choice([d for d in tiles.DECO if d])   # 녹은 땅엔 풀이 조금
        self.patches = mask
