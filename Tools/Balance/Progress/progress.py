"""11/19 성장 속도 측정 (python Tools/Balance/Progress/progress.py): 고려 출발, 처음 하는 유저 / 보통 유저.
돈(교역 + 이야기 의뢰 보상 + 게시판 보수), 운반 수단 구입, 신용 점수·단계가 언제 오는지.
교역 모형은 11/15 sim.py (MarketMath 공식, 1맵 = 실제 45초) 그대로, 지금 맵당 비율·재고 깊이 1.5.
"""
import sys, random, statistics
sys.path.insert(0, str(__import__('pathlib').Path(__file__).parent))
import sim
from search import make

PPM = {'goryeo': 0.25, 'transit': 0.15, 'venice': 0.45, 'arabia': 0.35, 'north': 0.5}
VEH = [(150, 30), (360, 50), (800, 80), (1000, 80)]   # 손수레 · 짐말 · 마차 · 좌판(자리값, 짐 그대로)
def level(pts):   # Core/Credit.cs: 1단계 1점, 2단계부터 n×(n+1)
    if pts < 1: return 0
    n = 1
    while (n + 1) * (n + 2) <= pts: n += 1
    return n

# 고려 이야기 의뢰 (순서대로): (이름, 걸리는 이동 맵 수, 보수 냥, 신용, 손수레 필요)
STORY = [('첫 짐', 1, 30, 1, 0), ('사고팔기', 1, 15, 0, 0), ('게시판', 1, 25, 0, 0), ('특산품', 1, 10, 0, 0),
         ('수도 개경', 2, 45, 1, 0), ('손수레', 0, 0, 0, 1), ('약재', 1, 50, 1, 0), ('땔감', 1, 20, 2, 0),
         ('수도로 곡식', 1, 10, 0, 0), ('산록 약재', 1, 10, 0, 0), ('귀환 장사', 1, 6, 0, 0), ('인삼 심부름', 1, 20, 0, 0),
         ('벽란도 인삼', 2, 50, 2, 0), ('조운 곡식', 2, 90, 2, 1), ('팔관회', 3, 120, 3, 0)]

def contract_points(lv, d, rng):
    pts = 1 + (1 if d >= 2 else 0)
    kinds = ['n'] * 4 + (['e', 'e'] if lv >= 1 else []) + (['f', 'f'] if lv >= 2 else []) + (['b', 'b'] if lv >= 4 else [])
    k = rng.choice(kinds)
    if k != 'n': pts += 1
    if lv >= 3 and rng.random() < 0.25: pts += 1                 # 귀한 짐
    if rng.random() < (0.35 if k != 'f' else 0.6): pts += 1        # 빨리 옴
    if k == 'f' and rng.random() < 0.25: pts = 0                   # 달려서 상함 (처음엔 가끔)
    return pts

def run(seed, kind='casual', minutes=150):
    rng = random.Random(seed)
    e = make(PPM, 1.5)
    pl = sim.Player('Origin_Goryeo')
    towns = [t for t, v in e.towns.items() if v['region'] in ('goryeo', 'transit') and v['stock']]
    e.reset()
    money = 100; sec = 4 * 60.0; money += 20            # 튜토리얼 4분 · 노잣돈 20
    cur = 'naru'; cap = round(10 * pl.capmul); have = 0; pts = 0; known = {cur}
    story = 0; ev = {}
    cfg = dict(casual=dict(look=25, slow=1.25, story=0.6, board=0.35, frac=(0.4, 0.9), best=0.3),
               typical=dict(look=20, slow=1.2, story=0.7, board=0.55, frac=(0.6, 1.0), best=0.6))[kind]
    def mark(key):
        if key not in ev: ev[key] = sec / 60
    while sec < minutes * 60:
        sec += cfg['look']
        known.add(cur)
        near = [t for t in towns if t != cur and 1 <= sim.maps(cur, t) <= 4]
        # 이야기 의뢰 한 걸음 (그 의뢰가 요구하는 이동만큼 걷고, 짐 반쯤은 장사도 함)
        if story < len(STORY) and rng.random() < cfg['story']:
            name, hops, rew, cr, need = STORY[story]
            if need and have == 0 and name == '조운 곡식':
                pass
            else:
                if name == '손수레':
                    if have == 0 and money >= VEH[0][0]:
                        money -= VEH[0][0]; have = 1; cap = round(30 * pl.capmul); mark('손수레')
                    if have >= 1: story += 1
                    continue
                dest = rng.choice(near)
                p, w = sim.plan(e, pl, cur, dest, cap * 0.5, money * 0.5, commit=True)
                money += p + rew; pts += cr
                if name == '게시판': pts += contract_points(level(pts), sim.maps(cur, dest), rng)
                d = max(1, hops)
                sec += d * 45 * cfg['slow'] / pl.speed; e.recover(d * 3 / pl.speed + 0.5); cur = dest
                story += 1
                if story == len(STORY): mark('이야기 끝')
                if story == 5: mark('고려 한 바퀴')
        else:
            # 자유 교역 (11/15 casual/typical 규칙)
            scored = sorted(((sim.plan(e, pl, cur, t, cap, money)[0] / (sim.maps(cur, t) + 0.3), t) for t in near), reverse=True)
            r = rng.random()
            dest = scored[0][1] if r < cfg['best'] else rng.choice(scored[:3])[1] if r < 0.85 else rng.choice(near)
            frac = rng.uniform(*cfg['frac'])
            p, w = sim.plan(e, pl, cur, dest, cap * frac, money * frac, commit=True)
            money += p
            d = sim.maps(cur, dest)
            if rng.random() < cfg['board']:          # 가는 길에 게시판 의뢰 하나
                pts += contract_points(level(pts), d, rng); money += rng.randint(3, 8) + (8 if level(pts) >= 3 else 0)
            sec += d * 45 * cfg['slow'] / pl.speed; e.recover(d * 3 / pl.speed + 0.5); cur = dest
        # 운반 수단 (사고도 장사할 돈을 조금 남김)
        for i, (price, c) in enumerate(VEH):
            if have == i and money >= price + (40, 80, 150, 0)[i]:
                money -= price; have = i + 1; cap = round(c * pl.capmul); mark(['손수레', '짐말', '마차', '좌판'][i])
        for lv in range(1, 7):
            if level(pts) >= lv: mark(f'신용{lv}')
        for m in (5, 15, 30, 60, 90):
            if sec / 60 >= m and f'돈@{m}' not in ev: ev[f'돈@{m}'] = money; ev[f'신용점@{m}'] = pts
    return ev

def table(kind, n=60):
    runs = [run(s, kind) for s in range(n)]
    keys = ['신용1', '손수레', '신용2', '고려 한 바퀴', '신용3', '짐말', '이야기 끝', '신용4', '마차', '신용5', '좌판', '신용6']
    print(f'--- {kind} (가운데값, 분. 150분 안에 절반이 못 가면 -)')
    for k in keys:
        v = [r[k] for r in runs if k in r]
        got = len(v)
        print(f'{k:8s} {statistics.median(v):5.1f}분  ({got}/{n}명)' if got >= n / 2 else f'{k:8s}   -    ({got}/{n}명)')
    for m in (5, 15, 30, 60, 90):
        print(f'{m}분: 신용점수 {statistics.median([r.get(f"신용점@{m}", 0) for r in runs]):.0f}')

if __name__ == '__main__':
    table('casual'); table('typical')
