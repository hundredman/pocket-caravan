"""본토 서부 시골 풍경 덧칠 (2026-10-26, 제안). venice_mainland.finish()가 길·건물을 다 놓은 뒤 부른다.
    - village   : 장터 둘레에 크림·황토색 시골 집(vh_*)을 몇 채 (나루터·양조장·대상 숙소)
    - avenue    : 큰길(등급 3) 양쪽에 사이프러스 가로수를 줄지어
    - farmsteads: 빈 들판에 농가 한 채 + 채소밭·건초·수레·벌통 묶음
    모든 배치는 venice_dress.Guard로 길이 끊기지 않을 때만 남긴다. 사람·출구 둘레는 비운다."""
import tiles
import venice_dress
from venice_dress import Guard, _keep

VILLAGE = {   # 맵: (벽색, 종류 비율, 남길 빈 땅 비율, 마을 반경)
    "venice_c_landing": (["cream", "ochre", "salmon", "brick"], dict(house=3, narrow=1, wide=2, shop=1), .82, 16),
    "venice_c_vineyard": (["ochre", "cream", "salmon"], dict(house=3, wide=2), .88, 13),
    "venice_c_post": (["cream", "ochre", "salmon", "brick", "sage"], dict(house=3, narrow=2, wide=2, shop=2, wide_shop=1), .8, 18),
}
FARMS = {"venice_c_landing": 2, "venice_c_post": 2, "venice_c_gate": 3, "venice_c_bridge": 2, "venice_c_pass": 1}


def _open(m, x, y, keep):
    return (m.inside(x, y) and m.zone[y][x] == "meadow" and not m.water[y][x] and not m.blocked[y][x]
            and m.obj[y][x] is None and m.high[y][x] is None and not m.reserved[y][x] and not m.road[y][x]
            and (x, y) not in keep)


def avenue(m, guard, keep, step=3, off=2):
    """등급 3 큰길 가장자리에서 off칸 떨어진 풀밭에 step칸마다 사이프러스"""
    placed = 0
    for y in range(m.h):
        for x in range(m.w):
            if m.road[y][x] < 3:
                continue
            for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
                nx, ny = x + dx * off, y + dy * off
                edge = (x + dx, y + dy)
                if not m.inside(*edge) or m.road[edge[1]][edge[0]] >= 3:
                    continue
                if (nx if dy else ny) % step:
                    continue
                if not all(_open(m, nx, ny - k, keep) for k in range(3)):
                    continue
                if guard.place("vg_cypress", nx, ny - 2, high=2) is not None:
                    placed += 1
    return placed


def farmsteads(m, guard, keep, n):
    """농가 묶음: 집(문은 남쪽) 앞 마당 오른쪽에 채소밭 둘, 왼쪽에 건초·수레·벌통"""
    rng = m.rng
    done = 0
    for _ in range(300):
        if done >= n:
            break
        house = rng.choice(["vg_n_orc_farmhouse", "vg_n_orc_cottage", "vh_house_cream", "vh_wide_ochre"])
        w, h = tiles.STAMPS[house][3:5]
        x, y = rng.randrange(2, m.w - w - 6), rng.randrange(2, m.h - h - 5)
        area = [(x + dx, y + dy) for dy in range(-1, h + 4) for dx in range(-3, w + 4)]
        if not all(_open(m, *c, keep) for c in area):
            continue
        if guard.place(house, x, y, high=1) is None:
            continue
        for name, px, py in (("vg_n_orc_vegbed", x + w + 1, y + h - 1), ("vg_n_orc_vegbed", x + w + 1, y + h + 1),
                             ("vg_n_orc_hay", x - 2, y + h - 1), ("vg_n_orc_hay", x - 2, y + h),
                             (rng.choice(["vg_orc_cart", "vg_well", "vg_px_orc_drying_rack"]), x - 1, y + h + 2),
                             ("vg_px_orc_beehive", x + w + 3, y + h - 2)):
            if all(_open(m, px + dx, py + dy, keep) for dy in range(tiles.STAMPS[name][4]) for dx in range(tiles.STAMPS[name][3])):
                guard.place(name, px, py, high=tiles.STAMPS[name][6] or 0)
        for dx in range(-1, w + 1):          # 집 앞은 비워 둔다
            keep.add((x + dx, y + h)); keep.add((x + dx, y + h + 1))
        m.cluster_deco(["vg_tuft", "vg_tuft", "vg_rocks"], x + w // 2, y + h + 1, 3, 4)
        done += 1
    return done


def dress(m):
    out = {}
    if m.id in VILLAGE:
        colors, kinds, ratio, radius = VILLAGE[m.id]
        out["village"] = venice_dress.dress(m, (colors, kinds, ratio), zones=("meadow", "dirt"), radius=radius)
    keep = _keep(m)
    guard = Guard(m)
    out["avenue"] = avenue(m, guard, keep)
    out["farms"] = farmsteads(m, guard, keep, FARMS.get(m.id, 0))
    return out
