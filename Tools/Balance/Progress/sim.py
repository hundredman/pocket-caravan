import json, math, copy

D = json.load(open(__import__('pathlib').Path(__file__).with_name('data.json')))
ITEMS = {i['id']: i for i in D['items'] if not i['quest']}
TOWNS = {t['id']: t for t in D['towns']}
DIST = {k: v for k, v in D['dist'].items()}
REC = D['recover']

def civ(region):
    for c in ('goryeo', 'venice', 'arabia', 'north'):
        if region == c: return c
    return 'center'

def maps(a, b):
    return 0 if a == b else DIST[a + '|' + b]

def rnd(x):  # Unity RoundToInt (banker's)
    return int(round(x))

class Econ:
    def __init__(s, items=None, towns=None, k=1.0, kfn=None):
        s.items = items or copy.deepcopy(ITEMS)
        s.towns = towns or copy.deepcopy(TOWNS)
        s.k = k; s.kfn = kfn
        s.opt0 = {}; s.base = {}
        for t in s.towns.values():
            lv = 1.0
            st = {e[0]: (e[1], e[2]) for e in t['stock']}
            for it in s.items.values():
                o, lm = st.get(it['id'], (t['defOpt'], 1.0))
                kk = kfn(it, t, it['id'] in st) if kfn else k
                s.opt0[(t['id'], it['id'])] = o * lv * kk
                mo = maps(it['origin'], t['id']) if it['origin'] else 0
                lm = min(max(lm if lm > 0 else 1, 0.1), 3)
                s.base[(t['id'], it['id'])] = it['base'] * (1 + it['ppm'] * mo) * lm
        s.reset()
    VOL = [0, 500, 1800, 8000]
    def reset(s):
        s.level = {t: v['level'] for t, v in s.towns.items()}
        s.vol = {t: 0 for t in s.towns}
        s.opt = {k_: v * (1 + 0.5 * (s.level[k_[0]] - 1)) for k_, v in s.opt0.items()}
        s.stock = dict(s.opt)
    def add_volume(s, t, amount):
        s.vol[t] += amount
        while s.level[t] < 3 and s.vol[t] >= s.VOL[s.level[t]]:
            s.level[t] += 1
            for i in s.items: s.opt[(t, i)] = s.opt0[(t, i)] * (1 + 0.5 * (s.level[t] - 1))
    def mult(s, t, i, stock):
        o = s.opt[(t, i)]
        if stock <= 0.01: return 2.0
        return min(max(o / stock, 0.5), 2.0)
    def buy(s, t, i, pl):
        st = s.stock[(t, i)]
        p = max(1, rnd(s.base[(t, i)] * s.mult(t, i, st)))
        if civ(s.towns[t]['region']) == pl.civ: p = max(1, rnd(p * (1 - 0.03)))
        return max(p, s.sell(t, i, pl) + 1)
    def sell(s, t, i, pl):
        st = s.stock[(t, i)]
        p = max(1, rnd(s.base[(t, i)] * s.mult(t, i, st + 1)))
        p = max(1, math.floor(p * (1 - pl.fee)))
        if pl.cross > 0:
            it = s.items[i]
            ic = civ(s.towns[it['origin']]['region']) if it['origin'] else None
            if ic and ic != civ(s.towns[t]['region']): p = rnd(p * (1 + pl.cross))
        return p
    def recover(s, h):
        keep = (1 - REC) ** h
        for key in s.stock:
            o = s.opt[key]; s.stock[key] = o + (s.stock[key] - o) * keep

class Player:
    def __init__(s, origin):
        s.origin = origin
        s.civ = {'Origin_Goryeo': 'goryeo', 'Origin_Venice': 'venice', 'Origin_Arabia': 'arabia', 'Origin_Viking': 'north'}[origin]
        s.fee = 0.13 if s.civ == 'venice' else 0.15
        s.cross = 0.05 if s.civ == 'arabia' else 0
        s.capmul = 1.1 if s.civ == 'goryeo' else 1.0
        s.speed = 1.1 if s.civ == 'north' else 1.0

def plan(e, pl, a, b, cap, money, commit=False):
    items = [x[0] for x in e.towns[a]['stock'] if x[0] in e.items]
    saved = {} if commit else {(t, i): e.stock[(t, i)] for t in (a, b) for i in items}
    w = 0.0; cost = 0; prof = 0
    for _ in range(400):
        best = None; br = 0
        for i in items:
            it = e.items[i]
            if e.stock[(a, i)] < 1 or w + it['w'] > cap + 1e-3: continue
            p = e.buy(a, i, pl)
            if cost + p > money: continue
            sp = e.sell(b, i, pl)
            r = (sp - p) / it['w']
            if sp - p > 0 and r > br: br = r; best = (i, p, sp)
        if not best: break
        i, p, sp = best
        e.stock[(a, i)] -= 1; e.stock[(b, i)] += 1
        if commit:
            e.add_volume(a, p); e.add_volume(b, sp)
            ITEMLOG[i] = ITEMLOG.get(i, 0) + sp - p
        w += e.items[i]['w']; cost += p; prof += sp - p
    if not commit:
        for k_, v in saved.items(): e.stock[k_] = v
    return prof, w

HOURS_PER_MAP = 3.0
ITEMLOG = {}

def run(e, origin, region_ids, start, minutes=30, cart=150, horse=360, maxd=4, verbose=False):
    pl = Player(origin)
    towns = [t for t, v in e.towns.items() if v['region'] in region_ids and v['stock']]
    e.reset()
    money = 100; hours = 0; cur = start; cap = round(10 * pl.capmul); have = 0; earned = 0; log = []
    while hours * 15 / 60 < minutes:
        best = None; brate = 0
        for t in towns:
            if t == cur: continue
            d = maps(cur, t)
            if d < 1 or d > maxd: continue
            p, _ = plan(e, pl, cur, t, cap, money)
            rate = p / (d * HOURS_PER_MAP / pl.speed + 0.5)
            if rate > brate: brate = rate; best = t
        if best is None:
            best = min((t for t in towns if t != cur), key=lambda t: maps(cur, t))
        p, w = plan(e, pl, cur, best, cap, money, commit=True)
        money += p; earned += p
        h = maps(cur, best) * HOURS_PER_MAP / pl.speed + 0.5
        hours += h; e.recover(h); cur = best
        m = hours * 15 / 60
        if have == 0 and money >= cart + 60: money -= cart; have = 1; cap = round(30 * pl.capmul); log.append(f'cart@{m:.0f}')
        elif have == 1 and money >= horse + 150: money -= horse; have = 2; cap = round(50 * pl.capmul); log.append(f'horse@{m:.0f}')
        if verbose: log.append(f'{cur}+{p}')
    return earned, log

REG = {'goryeo': ['goryeo', 'transit'], 'venice': ['venice', 'center_venice'], 'arabia': ['arabia'], 'north': ['north', 'center_north']}
START = {'goryeo': ('Origin_Goryeo', 'naru'), 'venice': ('Origin_Venice', 'venice_rialto'), 'arabia': ('Origin_Arabia', 'arabia_ahsa'), 'north': ('Origin_Viking', 'north_birka')}

def report(e, label='', minutes=(10, 30), origin_override=None, **kw):
    out = []
    for r, (o, s) in START.items():
        res = []
        for mn in minutes:
            earned, log = run(e, origin_override or o, REG[r], s, minutes=mn, **kw)
            res.append(earned)
        out.append((r, res, log))
    print(label, ' | '.join(f"{r}:{'/'.join(map(str, res))} {' '.join(log)}" for r, res, log in out))
    return out

if __name__ == '__main__':
    e = Econ()
    report(e, 'x1 goryeo-origin all', origin_override='Origin_Goryeo')
    report(e, 'x1 own origin')
    report(Econ(k=2), 'x2 own origin')
