"""북유럽(눈) 지역용 타일과 맵. tiles.py는 고치지 않고, 눈 바닥·눈 길·눈 물가와 눈 나무 묶음만 더한다."""
import tiles
from tiles import Map, K, autotile, PLANK

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
    "woodhouse": ("house", 25, 14, 4, 5, 0, 0, None),       # 짙은 나무 박공집
})


class SnowMap(Map):
    """바닥을 눈밭으로 칠하는 맵. 나머지 기능은 Map과 같다."""

    def snow_props(self, pts):
        for (x, y) in pts:
            if self.inside(x, y) and not self.reserved[y][x]:
                self.prop(self.rng.choice(SNOW_PROPS), x, y)

    def finish(self):
        self.smooth()

        def road_mask(a, b):
            if not self.inside(a, b):
                a = min(max(a, 0), self.w - 1); b = min(max(b, 0), self.h - 1)
            return self.road[b][a] > 0 or self.ground[b][a] == PLANK

        def water_mask(a, b):
            if not self.inside(a, b):
                return True
            return self.water[b][a] or self.ground[b][a] == PLANK

        for y in range(self.h):
            for x in range(self.w):
                if self.ground[y][x] == PLANK:
                    continue
                if self.ground[y][x] is not None and self.road[y][x] == 0 and not self.water[y][x]:
                    continue
                if self.water[y][x]:
                    self.ground[y][x] = autotile(water_mask, x, y, *SNOW_WATER_ORIGIN)
                    self.blocked[y][x] = True
                elif self.road[y][x] > 0:
                    self.ground[y][x] = autotile(road_mask, x, y, *SNOW_PATH_ORIGIN)
                else:
                    self.ground[y][x] = self.rng.choice(SNOW_VARIANTS) if self.rng.random() < 0.12 else SNOW
        # 물결 애니메이션은 넣지 않는다 (피오르가 얼어 있음)
