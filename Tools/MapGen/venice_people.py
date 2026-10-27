"""베네치아·본토 서부 맵에 사람과 공용 장치를 놓는다 (2026-10-26, 제안). 고려·아라비아가 쓰는 공용 장치를 그대로 쓴다.
    - worker  : 두 지점을 오가며 짐을 나르는 주민 (WorkingNpc)          - villager: 마을 사람 (지역 등록 villagers에서 고름)
    - walker  : 길을 오가는 NPC 상인 (NpcTraders, 말 걸면 소문)          - growth  : 장터 등급이 오르면 나타나는 짐 소품 (TownGrowthProp)
    - anim    : 흔들리는 풀 (과수원·포도밭)
    맵이 다 만들어진 뒤(finish 다음)에 걸을 수 있는 칸만 골라 놓는다. 장터 둘레에서 찾고, 출구·도착 자리·다른 사람 칸은 피한다.
    도적·길 사건·길 고르기 표지판은 venice_mainland.py가 길 위 자리를 직접 정한다. 이름·대사는 창작 제안이다."""
from collections import deque
import tiles

# 맵: [(이름, 그림, 대사...), ...] 일하는 사람 / 주민(지역 villagers 번호 목록) / 길 위 상인(trader 번호) / 성장 소품 / 흔들리는 풀 수
SPECS = {
    "venice_rialto": dict(
        workers=[("운하 짐꾼", "venice_porter_tall", "배에서 내린 짐은 이 물가에서 나눠 싣소.", "장터는 광장 남쪽이오. 많이 팔면 값이 내려가니 나눠 파시오."),
                 ("시장 일꾼", "venice_merchant_tall", "아침마다 네 마을 물건이 이 광장에 모인다네.", "비단 노점 옆이 제일 붐비지."),
                 ("곤돌라 수리공", "venice_gondolier_tall", "곤돌라 바닥은 해마다 새로 칠해야 해.", "경주용 곤돌라는 더 가볍게 깎지."), ("빵 나르는 소년", "venice_vintner_tall", "갓 구운 빵이요! 광장 끝까지 배달!", "카니발 밤엔 빵이 두 배로 팔려요.")],
        villagers=[0, 3, 5, 9], walkers=[1, 5], growth=True),
    "venice_murano": dict(
        workers=[("모래 나르는 조수", "venice_glassmaker_tall", "유리는 모래와 재로 만든다오. 하루에 몇 수레씩 들어오지.", "가마 불은 밤에도 꺼뜨리지 않는다네."),
                 ("땔감 나르는 조수", "venice_porter_tall", "가마는 하루에 장작 한 산을 먹어.", "무라노 불은 꺼지면 큰일 나.")],
        villagers=[1, 8], walkers=[6], growth=True),
    "venice_burano": dict(
        workers=[("그물 나르는 어부", "venice_fisher_tall", "새벽에 나간 배가 들어오면 그물을 말려야 해.", "소금이 있어야 생선을 절이지. 키오자 소금이 제일이야."),
                 ("그물 너는 아낙", "venice_lace_tall", "볕 좋은 날 그물을 말려야 썩지 않아.", "레이스랑 그물은 손이 같은 일을 해.")],
        villagers=[2, 7], walkers=[7], growth=True),
    "venice_chioggia": dict(
        workers=[("소금 짐꾼", "venice_porter_tall", "소금 자루는 무겁지만 어디서든 팔린다오.", "본토행 배는 동쪽 잔교에서 떠나지."),
                 ("저울 보는 서기", "venice_clerk_tall", "소금은 무게로 값을 매기지.", "본토 배가 들어오는 날은 저울이 쉬지 않아.")],
        villagers=[4, 6], walkers=[2, 8], growth=True),
    "venice_santerasmo": dict(
        workers=[("과일 나르는 일꾼", "villager_rabbit_tall", "레몬은 따자마자 상자에 담아야 무르지 않아.", "멀리 키오자 항구가 과일을 귀하게 쳐 준대."),
                 ("벌통 돌보는 사람", "venice_vintner_tall", "레몬꽃 꿀이 제일 향긋해.", "벌이 많으면 과일도 많이 열리지.")],
        villagers=[], walkers=[9], growth=True, plants=6),
    "venice_c_landing": dict(
        workers=[("하역 인부", "venice_porter_tall", "키오자 배가 들어오면 이 마당이 꽉 차지.", "직행로는 빠르지만 밤엔 도적이 나온다더군."),
                 ("밧줄 감는 뱃사람", "venice_fisher_tall", "키오자 배는 하루 두 번 들어오지.", "밧줄 잘못 감으면 짐이 물에 빠져.")],
        villagers=[1, 4], walkers=[3, 10], growth=True),
    "venice_c_vineyard": dict(
        workers=[("술통 굴리는 일꾼", "venice_vintner_tall", "통은 굴려야지, 들면 허리 나가.", "고갯길은 좁아서 수레가 고생이야."),
                 ("포도 바구니 나르는 아낙", "venice_lace_tall", "바구니가 차면 양조장으로!", "올해는 비가 적어 포도가 달아.")],
        villagers=[0, 5], walkers=[11], growth=True, plants=8),
    "venice_c_post": dict(
        workers=[("마구간지기", "villager_fox_tall", "말도 쉬어야 멀리 가지.", "동문 너머 길은 아직 닦는 중이래."),
                 ("물 긷는 하인", "venice_porter_tall", "대상 손님들 말에게 물을 줘야 해.", "우물물이 시원해. 한 바가지 줄까?")],
        villagers=[2], walkers=[0, 12], growth=True),
    "venice_c_gate": dict(workers=[("동문 경비", "venice_guard_tall", "동문 너머 중앙 대륙 길은 아직 닦는 중이오.", "짐 검사는 없소. 도적만 조심하시오.")],
                          villagers=[], walkers=[13, 14], growth=False),
    "venice_c_bridge": dict(workers=[], villagers=[], walkers=[15], growth=False),
    "venice_c_marsh": dict(workers=[("갈대 묶는 일꾼", "venice_porter_tall", "갈대는 지붕도 되고 바구니도 되지.", "진흙길은 느려도 도적은 없어.")],
                           villagers=[3], walkers=[16], growth=False),
    "venice_c_pass": dict(workers=[("돌 나르는 인부", "venice_porter_tall", "고갯길 돌을 하나씩 다지는 중이오.", "좁은 길이라 수레는 천천히 끄시오.")],
                          villagers=[], walkers=[17], growth=False),
}
GROWTH_PROPS = (("vg_crate", 2), ("vg_barrel", 2), ("vg_sack_grain", 3), ("vg_amphora", 3))


def _walk(m, x, y):
    return m.inside(x, y) and not m.blocked[y][x] and (not m.water[y][x] or (x, y) in m.decks or (x, y) in m.over_water)


def _clear(m, x, y):
    """사람이 나무 윗부분(high)에 가려지지 않는 칸: 발 칸과 머리 칸 모두 비어 있어야 한다"""
    return not m.high[y][x] and (y == 0 or not m.high[y - 1][x])


def _taken(m):
    taken = set()
    for o in m.objects:
        for dy in range(o.get("h", 1)):
            for dx in range(o.get("w", 1)):
                taken.add((o["x"] + dx, o["y"] + dy))
        if o["kind"] in ("spawn", "exit"):            # 출구·도착 자리 둘레 2칸도 비운다
            for dy in range(-2, o.get("h", 1) + 2):
                for dx in range(-2, o.get("w", 1) + 2):
                    taken.add((o["x"] + dx, o["y"] + dy))
    return taken


def _dist(m, start):
    dist = {start: 0}
    q = deque([start])
    while q:
        c = q.popleft()
        for p in ((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)):
            if p not in dist and _walk(m, *p):
                dist[p] = dist[c] + 1
                q.append(p)
    return dist


def _anchor(m):
    o = next((o for o in m.objects if o["kind"] == "market"), None) or next((o for o in m.objects if o["kind"] == "spawn"), None)
    x, y = o["x"], o["y"]
    for r in range(0, 6):                     # 장터 칸은 막혀 있으니 옆의 걸을 수 있는 칸에서 시작
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                if _walk(m, x + dx, y + dy):
                    return (x + dx, y + dy)
    return (x, y)


def _run(m, c, n, taken, horizontal):
    cells = [(c[0] + i, c[1]) if horizontal else (c[0], c[1] + i) for i in range(n)]
    return cells if all(_walk(m, *p) and _clear(m, *p) and p not in taken for p in cells) else None


def populate(m):
    spec = SPECS.get(m.id)
    if not spec:
        return
    taken = _taken(m)
    dist = _dist(m, _anchor(m))
    near = sorted((d, c) for c, d in dist.items() if c not in taken)
    rng = m.rng

    def claim(cells, pad=1):
        for x, y in cells:
            for dx in range(-pad, pad + 1):
                for dy in range(-pad, pad + 1):
                    taken.add((x + dx, y + dy))

    # 일하는 사람: 장터에서 2~12걸음 안, 가로나 세로로 5칸 곧게 걸을 수 있는 곳을 오간다
    for name, ch, *lines in spec.get("workers", []):
        for d, c in near:
            if not 2 <= d <= 12:
                continue
            path = _run(m, c, 5, taken, True) or _run(m, c, 5, taken, False)
            if path:
                m.add("worker", c[0], c[1], name=name, character=ch, lines=list(lines), path=[path[0], path[-1]])
                claim(path)
                break
    # 마을 사람: 장터에서 3~16걸음, 서로 4칸 넘게 떨어진 칸 (맵 가장자리 3칸 안은 피한다)
    placed = 0
    who = spec.get("villagers", [])            # 지역 villagers 번호. 맵마다 다른 사람이 서게 (같은 이름이 여러 섬에 겹치지 않게)
    for d, c in near:
        if placed >= len(who):
            break
        if 3 <= d <= 16 and c not in taken and _clear(m, *c) and m.road[c[1]][c[0]] < 3 and 3 <= c[0] < m.w - 3 and 3 <= c[1] < m.h - 3:
            m.add("villager", c[0], c[1], variant=0, idx=who[placed])
            claim([c], pad=3)
            placed += 1
    # 길 위 상인: 곧은 길(등급 2 이상) 8칸을 오간다
    for idx in spec.get("walkers", []):
        best = None
        for d, c in near:
            if m.road[c[1]][c[0]] < 2 or d < 4:
                continue
            for horizontal in (True, False):
                path = _run(m, c, 8, taken, horizontal)
                if path and all(m.road[p[1]][p[0]] >= 2 for p in path):
                    best = path
                    break
            if best:
                break
        if best:
            m.walker([best[0], best[len(best) // 2], best[-1]], idx=idx)
            claim(best, pad=0)
    # 장터가 커지면 늘어나는 짐 소품 (장터 옆, 사람이 지나가도 막지 않는다)
    if spec.get("growth"):
        market = next((o for o in m.objects if o["kind"] == "market"), None)
        if market:
            ring = sorted(((dx, dy) for dx in range(-3, 4) for dy in range(-3, 4) if 1 <= abs(dx) + abs(dy) <= 4),
                          key=lambda v: (abs(v[0]) + abs(v[1]), v[1] < 0))
            slots = [(market["x"] + dx, market["y"] + dy) for dx, dy in ring]
            slots = [s for s in slots if m.inside(*s) and not m.water[s[1]][s[0]] and not m.obj[s[1]][s[0]] and not m.high[s[1]][s[0]]
                     and s not in taken and (s not in m.decks)]
            for (name, level), (x, y) in zip(GROWTH_PROPS, slots):
                s, sx, sy = tiles.STAMPS[name][:3]
                m.growth(x, y, tiles.K(s, sx, sy), market["town"], level)
    # 흔들리는 풀 (공용 'plant' 그림)
    for _ in range(spec.get("plants", 0)):
        for _try in range(40):
            x, y = rng.randrange(2, m.w - 2), rng.randrange(2, m.h - 2)
            if m.zone[y][x] in ("meadow", "dirt") and not m.road[y][x] and _walk(m, x, y) and (x, y) not in taken and not m.deco[y][x]:
                m.anim("plant", x, y, frames=4, fps=2.5)
                taken.add((x, y))
                break
