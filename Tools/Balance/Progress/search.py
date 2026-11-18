import sim, copy, itertools
from sim import ITEMS, TOWNS, Econ
def grp(it):
    o = it['origin']
    if not o: return 'staple'
    r = TOWNS[o]['region']
    return {'goryeo':'goryeo','transit':'transit','venice':'venice','center_venice':'venice','arabia':'arabia','north':'north','center_north':'north'}[r]
def make(ppm, k=2):
    it = copy.deepcopy(ITEMS)
    for x in it.values():
        if x['ppm'] >= 0.2 and grp(x) in ppm: x['ppm'] = ppm[grp(x)]
    return Econ(items=it, k=k)
