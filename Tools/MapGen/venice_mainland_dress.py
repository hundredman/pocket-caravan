"""본토 서부 7맵 꾸미기 (2026-10-30, 배치·이야기는 제안).
    석호 마을에 비해 본토가 넓은 풀밭에 집·나무만 흩어져 심심해 보여서, 맵 JSON 위에 시골 풍경 묶음을 덧놓는다.
    통합 브랜치에서는 맵 생성기가 돌지 않고, 본토 JSON에는 다른 문명으로 가는 출구(link_)가 있어 생성기 결과로 덮을 수 없다.
    그래서 처음 한 번 지금 JSON을 Tools/MapGen/venice_mainland_base/에 떠 두고, 매번 그 바탕에서 다시 꾸민다(여러 번 돌려도 쌓이지 않는다).
    묶음: 농가(집·밀밭·허수아비·건초 더미) · 양 우리(나무 울타리·양) · 과수원(올리브·무화과·레몬·벌통) · 길가 성모상 · 이정석 ·
          건초 마당 · 포도 압착 마당 · 물가 갈대·통발·작은 배, 바닥은 꽃밭·흙 얼룩.
    - 출구·사람·장터·런타임 장소(VeniceLife.json) 둘레는 비운다. 하나 놓을 때마다 모든 출구·사람에게 걸어서 닿는지 확인하고, 막히면 되돌린다.
    - 새 그림: Tools/ArtGen/venice_mainland_art.py (vml_*). 쓰는 venice_atlas 칸 그림이 Tiles에 없으면 잘라서 쓴다.
    사용: python Tools/MapGen/venice_mainland_dress.py  (끝에 venice_extras.py가 사람·이정표를 다시 붙인다)
          그다음 Unity '베네치아 씬 12개 다시 만들기 (데이터 제외)', python Tools/MapGen/venice_tilemap.py"""
import json, math, os, random, shutil, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import venice_extras as vx                                     # noqa: E402

ROOT = vx.ROOT
MAPDIR = os.path.join(ROOT, "Assets/PocketCaravan/Maps/VeniceMainland")
BASE = os.path.join(HERE, "venice_mainland_base")
TILES = os.path.join(ROOT, "Assets/PocketCaravan/Art/NinjaAdventure/Tiles")
ATLAS = os.path.join(ROOT, "Tools/ArtGen/venice_atlas")

with open(ATLAS + ".json", encoding="utf-8") as fp:
    CELLS = json.load(fp)
_atlas = None
_png = {}


def png(name):
    """칸 그림 (없으면 venice_atlas에서 잘라 Tiles에 쓴다)"""
    global _atlas
    if name not in _png:
        p = os.path.join(TILES, name + ".png")
        if not os.path.exists(p) and name.startswith("vgpt_"):
            if _atlas is None:
                _atlas = Image.open(ATLAS + ".png").convert("RGBA")
            _, x, y = name.split("_")
            _atlas.crop((int(x) * 16, int(y) * 16, int(x) * 16 + 16, int(y) * 16 + 16)).save(p)
        _png[name] = Image.open(p).convert("RGBA") if os.path.exists(p) else None
    return _png[name]


def cover(name):
    im = png(name)
    return 0.0 if im is None else sum(1 for a in im.getchannel("A").getdata() if a > 128) / 256


def atlas(name, high=0):
    """venice_atlas 묶음 → [(칸 그림, dx, dy, 위층?)]"""
    x, y, w, h = CELLS[name]
    return [(f"vgpt_{x + i}_{y + j}", i, j, j < high) for j in range(h) for i in range(w)]


def art(base, w, h, high=0):
    """venice_mainland_art 그림 → [(칸 그림, dx, dy, 위층?)]"""
    return [(f"vml_{base}_{i}_{j}", i, j, j < high) for j in range(h) for i in range(w)
            if os.path.exists(os.path.join(TILES, f"vml_{base}_{i}_{j}.png"))]


FENCE_H, FENCE_V = art("fence_h", 1, 1), art("fence_v", 1, 1)
SHEEP = (art("sheep", 1, 1), art("sheepl", 1, 1))
TREES = ("n_orc_olive", "n_orc_fig", "n_orc_lemon", "olive")
# 묶음 자리에 걸리면 통째로 치워도 되는 흩어 심은 나무·돌·풀 (원래 맵의 흩뿌림)
CLEAR = ("cypress", "olive", "tree", "n_orc_olive", "n_orc_fig", "n_orc_lemon", "rocks", "tuft", "reeds", "n_orc_hay")
CLEAR_AT = {f"vgpt_{CELLS[n][0] + i}_{CELLS[n][1] + j}": (n, i, j) for n in CLEAR for i in range(CELLS[n][2]) for j in range(CELLS[n][3])}


# ---------------- 묶음 (각 항목: ("s", 칸들, dx, dy) 그림 / ("g", 바닥 무늬, dx, dy, w, h)) ----------------
def farm(rng):
    house = rng.choice(["n_orc_farmhouse", "n_orc_cottage", "orc_house"])
    hw, hh = CELLS[house][2:]
    fx = hw + 2
    items = [("s", atlas(house, 1), 3, 0),
             ("s", art("haystack", 2, 2, 1), 0, 1), ("s", atlas("n_orc_hay"), 1, hh), ("s", atlas("n_orc_hay"), 0, hh + 1),
             ("g", "wheat", 3 + fx, 0, 6, 4), ("s", art("scarecrow", 1, 2, 1), 3 + fx + 3, 1),
             ("s", atlas("n_orc_vegbed"), 3 + fx, 5), ("s", atlas("px_orc_beehive"), 3 + fx + 4, 5)]
    return items, [(3 + dx, hh + dy) for dx in range(hw) for dy in (0, 1)]       # 집 앞은 비운다


def pasture(rng):
    w, h = rng.choice([(9, 6), (8, 5), (10, 6)])
    gap = rng.randrange(2, w - 2)
    items = []
    for x in range(w):
        items.append(("s", FENCE_H, x, 0))
        if x != gap:
            items.append(("s", FENCE_H, x, h - 1))
    for y in range(1, h - 1):
        items += [("s", FENCE_V, 0, y), ("s", FENCE_V, w - 1, y)]
    inside = [(x, y) for x in range(2, w - 2) for y in range(1, h - 2)]
    rng.shuffle(inside)
    for i, (x, y) in enumerate(inside[:rng.choice([3, 4])]):
        items.append(("s", SHEEP[i % 2], x, y))
    items.append(("s", art("haybale", 1, 1), w - 2, h - 2))
    return items, [(gap, h), (gap, h - 1)] + [(x, y) for x in range(1, w - 1) for y in range(1, h - 1)]


def orchard(rng):
    spots = [(0, 0), (4, 1), (8, 0), (2, 4), (6, 4)]
    items = [("s", atlas(rng.choice(TREES), 2), x, y) for x, y in spots[:rng.choice([4, 5])]]
    items += [("s", atlas("px_orc_beehive"), 10, 4), ("s", atlas("px_orc_beehive"), 11, 5), ("s", atlas("px_orc_ladder_basket"), 10, 6)]
    return items, []


def shrine(rng):
    return [("s", art("shrine", 1, 2, 1), 1, 0), ("s", atlas(rng.choice(["flower_pot", "flower_pot2"])), 0, 1),
            ("s", atlas(rng.choice(["flower_pot", "pot_plant"])), 2, 1)], [(1, 2)]


def hay_yard(rng):
    return [("s", art("haystack", 2, 2, 1), 0, 0), ("s", art("haystack", 2, 2, 1), 3, 0), ("s", atlas("n_orc_hay"), 2, 1),
            ("s", atlas(rng.choice(["orc_cart", "w_cart"])), 1, 3), ("s", art("haybale", 1, 1), 5, 3)], []


def press_yard(rng):
    return [("s", atlas("orc_pergola", 2), 0, 0), ("s", atlas("px_orc_press", 1), 5, 1), ("s", atlas("barrel"), 6, 2),
            ("s", atlas("barrel"), 7, 2), ("s", atlas("barrel"), 6, 3), ("s", atlas("px_orc_drying_rack", 1), 8, 1)], []


def flock(rng):
    """울타리 없이 풀 뜯는 양 몇 마리 (고갯길·들판)"""
    spots = [(0, 1), (2, 0), (3, 2), (5, 1), (1, 3)]
    rng.shuffle(spots)
    return [("s", SHEEP[i % 2], x, y) for i, (x, y) in enumerate(spots[:rng.choice([3, 4])])], []


def fisher(rng):
    """물가 어부 자리: 통발·유목·그물 널기"""
    return [("s", atlas("n_fsh_traps"), 0, 1), ("s", atlas("n_fsh_driftwood"), 3, 0), ("s", atlas(rng.choice(["px_fsh_net_mend", "n_fsh_netpile"])), 2, 2)], []


def milestone(rng):
    return [("s", art("milestone", 1, 1), 0, 0)], []


KINDS = {"farm": farm, "pasture": pasture, "orchard": orchard, "shrine": shrine, "hay": hay_yard, "press": press_yard,
         "milestone": milestone, "flock": flock, "fisher": fisher}
NEAR_ROAD = {"shrine": (1, 2), "milestone": (1, 1)}             # 길에서 이만큼(최소, 최대) 떨어진 곳에만

# 맵마다 놓을 묶음 수 (제안)
PLAN = {
    "venice_c_gate":     dict(flock=1, farm=3, pasture=2, orchard=2, shrine=2, hay=3, milestone=4, meadow=.1),
    "venice_c_post":     dict(farm=2, pasture=1, orchard=1, shrine=1, hay=2, milestone=2, meadow=.09),
    "venice_c_pass":     dict(pasture=2, flock=3, shrine=2, hay=2, milestone=2, meadow=.07),
    "venice_c_vineyard": dict(press=2, hay=2, shrine=1, pasture=1, milestone=2, meadow=.06),
    "venice_c_landing":  dict(farm=2, pasture=1, orchard=1, shrine=1, hay=2, milestone=2, meadow=.08),
    "venice_c_bridge":   dict(flock=1, fisher=1, farm=1, pasture=2, orchard=1, shrine=1, hay=2, milestone=1, meadow=.09),
    "venice_c_marsh":    dict(fisher=2, shrine=1, milestone=1, meadow=.04, marsh=1),
}
ORDER = ("farm", "orchard", "press", "pasture", "hay", "flock", "fisher", "shrine", "milestone")


class Dresser:
    def __init__(self, m):
        self.m, self.d = m, m.d
        self.rng = random.Random("dress:" + m.d["id"])
        pad = vx.busy_cells(m, 3)
        self.busy = set(pad)
        for o in m.d["objects"]:                              # 출구는 더 넓게 비운다
            if o["kind"] == "exit":
                for dx in range(-6, o["w"] + 6):
                    for dy in range(-6, o["h"] + 6):
                        self.busy.add((o["x"] + dx, o["y"] + dy))
        self.used = set()
        self.road_d = self.road_distance()

    def idx(self, name):
        pal = self.d["palette"]
        if name not in pal:
            pal.append(name)
        return pal.index(name)

    def water(self, x, y):
        g = self.m.tile("ground", x, y)
        return "_W_" in g

    def clearable(self, x, y):
        return all(not n or n in CLEAR_AT for n in (self.m.tile("obj", x, y), self.m.tile("high", x, y)))

    def land_free(self, x, y):
        m = self.m
        if not m.inside(x, y) or (x, y) in self.busy or (x, y) in self.used:
            return False
        i = m.i(x, y)
        if self.water(x, y) or self.d["road"][i] or "rock" in m.tile("ground", x, y):
            return False
        if self.d["obj"][i] or self.d["high"][i]:
            return self.clearable(x, y)
        return not self.d["blocked"][i]

    def clear(self, cells, undo):
        """cells에 걸친 흩뿌림 나무·돌을 한 그루 단위로 치운다"""
        m, d = self.m, self.d
        origins = set()
        for x, y in cells:
            if not m.inside(x, y):
                continue
            for layer in ("obj", "high"):
                n = m.tile(layer, x, y)
                if n in CLEAR_AT:
                    name, i, j = CLEAR_AT[n]
                    origins.add((name, x - i, y - j))
        for name, ox, oy in origins:
            cx, cy, w, h = CELLS[name]
            for j in range(h):
                for i in range(w):
                    x, y = ox + i, oy + j
                    if not m.inside(x, y):
                        continue
                    k = m.i(x, y)
                    want = f"vgpt_{cx + i}_{cy + j}"
                    hit = False
                    for layer in ("obj", "high"):
                        if m.tile(layer, x, y) == want:
                            if not hit:
                                undo.append((k, d["obj"][k], d["high"][k], d["blocked"][k], d["deco"][k], d["ground"][k]))
                            d[layer][k] = 0; hit = True
                    if hit:
                        d["blocked"][k] = 0

    def road_distance(self):
        m = self.m
        dist = {}
        q = [(x, y) for y in range(m.h) for x in range(m.w) if self.d["road"][m.i(x, y)]]
        for c in q:
            dist[c] = 0
        k = 0
        while k < len(q):
            c = q[k]; k += 1
            if dist[c] >= 8:
                continue
            for p in ((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)):
                if m.inside(*p) and p not in dist:
                    dist[p] = dist[c] + 1; q.append(p)
        return dist

    def put_cells(self, cells, x, y, undo):
        m, d = self.m, self.d
        for name, dx, dy, high in cells:
            c = cover(name)
            if c == 0:
                continue
            i = m.i(x + dx, y + dy)
            undo.append((i, d["obj"][i], d["high"][i], d["blocked"][i], d["deco"][i], d["ground"][i]))
            d["deco"][i] = 0
            if high or (dy == 0 and c < .3 and len(cells) > 1):
                d["high"][i] = self.idx(name)
            else:
                d["obj"][i] = self.idx(name)
                if c >= .3:
                    d["blocked"][i] = 1

    def try_place(self, kind):
        m, rng = self.m, self.rng
        for _ in range(500):
            items, keep_open = KINDS[kind](rng)
            foot = set()
            for it in items:
                if it[0] == "s":
                    foot.update((it[2] + dx, it[3] + dy) for _, dx, dy, _ in it[1])
                else:
                    foot.update((it[2] + i, it[3] + j) for i in range(it[4]) for j in range(it[5]))
            foot.update(keep_open)
            w = max(c[0] for c in foot) + 1; h = max(c[1] for c in foot) + 1
            x, y = rng.randrange(1, max(2, m.w - w - 1)), rng.randrange(1, max(2, m.h - h - 1))
            margin = 1
            area = [(x + c[0] + dx, y + c[1] + dy) for c in foot for dx in range(-margin, margin + 1) for dy in range(-margin, margin + 1)]
            if kind in NEAR_ROAD:
                lo, hi = NEAR_ROAD[kind]
                if not all(self.land_free(x + c[0], y + c[1]) for c in foot):
                    continue
                rd = min(self.road_d.get((x + c[0], y + c[1]), 99) for c in foot)
                if not lo <= rd <= hi:
                    continue
            elif not all(self.land_free(*c) for c in area):
                continue
            undo = []
            self.clear([(x + c[0] + dx, y + c[1] + dy) for c in foot for dx in (-1, 0, 1) for dy in (-1, 0, 1)], undo)
            for it in items:
                if it[0] == "s":
                    self.put_cells(it[1], x + it[2], y + it[3], undo)
                else:
                    for i in range(it[4]):
                        for j in range(it[5]):
                            cx, cy = x + it[2] + i, y + it[3] + j
                            k = m.i(cx, cy)
                            undo.append((k, self.d["obj"][k], self.d["high"][k], self.d["blocked"][k], self.d["deco"][k], self.d["ground"][k]))
                            self.d["ground"][k] = self.idx(f"vml_{it[1]}_{cx % 2}_{cy % 2}")
                            self.d["deco"][k] = 0
            if m.connected(set()):
                self.used.update((x + c[0] + dx, y + c[1] + dy) for c in foot for dx in (-1, 0, 1) for dy in (-1, 0, 1))
                return True
            for k, o, hi, b, de, g in reversed(undo):
                self.d["obj"][k], self.d["high"][k], self.d["blocked"][k], self.d["deco"][k], self.d["ground"][k] = o, hi, b, de, g
        return False

    def meadow(self, share):
        """얼룩진 꽃밭·흙 바닥 (값 잡음). 그냥 풀 칸만 바꾼다."""
        m, d, rng = self.m, self.d, self.rng
        grids = [(g, {(i, j): rng.random() for i in range(m.w // g + 2) for j in range(m.h // g + 2)}) for g in (7, 3)]
        def smooth(t):
            return t * t * (3 - 2 * t)
        def noise(x, y):                                       # 값 잡음 두 겹 (굵은 얼룩 + 잔 얼룩)
            v = 0.0
            for (g, z), wgt in zip(grids, (.7, .3)):
                i, j = x // g, y // g; u, w = smooth((x % g) / g), smooth((y % g) / g)
                a = z[(i, j)] * (1 - u) + z[(i + 1, j)] * u
                b = z[(i, j + 1)] * (1 - u) + z[(i + 1, j + 1)] * u
                v += (a * (1 - w) + b * w) * wgt
            return v
        grass = [(x, y) for y in range(m.h) for x in range(m.w) if m.tile("ground", x, y).startswith("vq_L_g_orc_grass")
                 and m.tile("ground", x, y).endswith("_0_--------")]
        if not grass:
            return 0
        vals = sorted(noise(x, y) for x, y in grass)
        cut_p = vals[int(len(vals) * (1 - share))]
        cut_d = vals[int(len(vals) * .02)]
        n = 0
        for x, y in grass:
            v = noise(x, y)
            if v >= cut_p:
                d["ground"][m.i(x, y)] = self.idx(f"vq_L_g_orc_petal_0_{x % 2}_{y % 2}_0_--------"); n += 1
                if not d["deco"][m.i(x, y)] and not d["obj"][m.i(x, y)] and rng.random() < .08:   # 꽃밭엔 풀포기도 조금
                    d["deco"][m.i(x, y)] = self.idx("vgpt_3_25")
            elif v <= cut_d and self.road_d.get((x, y), 99) <= 2 and not d["obj"][m.i(x, y)]:
                d["ground"][m.i(x, y)] = self.idx(f"vq_L_g_orc_dirt_0_{x % 2}_{y % 2}_0_--------"); n += 1
        return n

    def marsh(self):
        """물가: 땅 가장자리에 키 큰 갈대, 물 위 작은 배·통발, 물가 땅에 유목"""
        m, d, rng = self.m, self.d, self.rng
        n = 0
        shore = [(x, y) for y in range(1, m.h - 1) for x in range(1, m.w - 1) if self.land_free(x, y)
                 and any(self.water(x + a, y + b) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
        rng.shuffle(shore)
        for x, y in shore[:40]:
            if (x, y) in self.used or not self.land_free(x, y - 1):
                continue
            undo = []
            self.put_cells(atlas("reeds_tall", 1), x, y - 1, undo)
            if m.connected(set()):
                self.used.add((x, y)); n += 1
            else:
                for k, o, hi, b, de, g in reversed(undo):
                    d["obj"][k], d["high"][k], d["blocked"][k], d["deco"][k], d["ground"][k] = o, hi, b, de, g
        open_water = [(x, y) for y in range(2, m.h - 2) for x in range(2, m.w - 3)
                      if all(self.water(x + a, y + b) and not d["obj"][m.i(x + a, y + b)] and (x + a, y + b) not in self.busy
                             for a in (-1, 0, 1, 2) for b in (-1, 0, 1))]
        rng.shuffle(open_water)
        boats = 0
        for x, y in open_water:
            if boats >= 6:
                break
            if any(abs(x - u) + abs(y - v) < 6 for u, v in self.used):
                continue
            undo = []
            self.put_cells(atlas(rng.choice(["fsh_dinghy", "fishing_boat"])), x, y, undo)
            self.used.update((x + a, y) for a in range(3)); boats += 1
        for x, y in water_edge(self, 45):
            undo = []
            self.put_cells(atlas("reeds"), x, y, undo)
        return n + boats


def water_edge(dr, k):
    """물 칸 중 땅에 닿은 칸 몇 개 (물속 갈대 바닥 장식)"""
    m = dr.m
    out = [(x, y) for y in range(1, m.h - 1) for x in range(1, m.w - 1) if dr.water(x, y) and not dr.d["obj"][m.i(x, y)]
           and (x, y) not in dr.busy and any(not dr.water(x + a, y + b) for a, b in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    dr.rng.shuffle(out)
    return out[:k]


def snapshot():
    """바탕 JSON을 처음 한 번만 떠 둔다 (앞서 붙인 사람·이정표는 걷어 낸 상태)"""
    os.makedirs(BASE, exist_ok=True)
    for mid in PLAN:
        dst = os.path.join(BASE, mid + ".json")
        if os.path.exists(dst):
            continue
        m = vx.Map(os.path.join(MAPDIR, mid + ".json"))
        with open(dst, "w", encoding="utf-8", newline="\n") as fp:
            json.dump(m.d, fp, ensure_ascii=False, separators=(",", ":"))


def main():
    snapshot()
    for mid, plan in PLAN.items():
        m = vx.Map(os.path.join(BASE, mid + ".json"))
        m.path = os.path.join(MAPDIR, mid + ".json")
        dr = Dresser(m)
        got = {}
        for kind in ORDER:
            for _ in range(plan.get(kind, 0)):
                got[kind] = got.get(kind, 0) + (1 if dr.try_place(kind) else 0)
        if plan.get("marsh"):
            got["marsh"] = dr.marsh()
        got["meadow"] = dr.meadow(plan.get("meadow", .06))
        m.save()
        print(mid, " · ".join(f"{k} {v}/{plan.get(k, v)}" for k, v in got.items()))
    vx.main()


if __name__ == "__main__":
    main()
