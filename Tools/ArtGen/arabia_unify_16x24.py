"""아라비아 19종 16프레임(아래·왼·오른·위 × 0~3) — 북유럽·베네치아와 같은 16x24 틀.

- 정면(d)·뒤(u)·옆(l) 0번을 머리쓰개 틀 + 몸 틀로 그리고, 오른쪽(r)은 왼쪽을 뒤집는다.
- 걷기 1·3번: 북유럽 캐릭터처럼 몸이 1픽셀 내려앉고 한 발씩 든다. 2번은 0번과 같다.
- 사막여우(주인공)는 순록 주인공 틀에 맞춰 세 방향을 따로 손으로 찍었다.
- 색은 각 캐릭터의 원래 아라비아 그림(32x48)에서 가져왔다.
  python3 all_frames.py [출력 폴더]
"""
from PIL import Image, ImageOps
from pathlib import Path
import sys

OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2] / "output" / "arabia_16x24"
E16 = '................'


def hx(s):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


# ---------------- 머리 틀 (4~15줄, 12줄) ----------------
TOP_ROWS = {   # 4~10줄 (앞·옆·뒤 공통, 옆·뒤에서 10줄만 바뀜)
    'turban': ['................', '....oooooooo....', '..ooHHHHHHHHoo..', '.oHHhHHHHHHhHHo.', '.oHHHhhHHhhHHHo.', '.ohHHHHHHHHHHho.', '.ohhhhhhhhhhhho.'],
    'keffiyeh': ['................', '....oooooooo....', '..ooHHHHHHHHoo..', '.oHHHHHHHHHHHHo.', '.obbbbbbbbbbbbo.', '.oHHHHHHHHHHHHo.', '.oHHssssssssHHo.'],
    'hijab': ['................', '....oooooooo....', '..ooHHHHHHHHoo..', '.oHHHHHHHHHHHHo.', '.oHHHHHHHHHHHHo.', '.oHHbHbHHbHbHHo.', '.oHHHssssssHHHo.'],
    'cap': ['................', '................', '....oooooooo....', '..ooHHHHHHHHoo..', '.oHHhHHHHHHhHHo.', '.ohhhhhhhhhhhho.', '.oxxssssssssxxo.'],
    'longhair': ['................', '....oooooooo....', '..ooxxxxxxxxoo..', '.oxxxxxxxxxxxxo.', '.obbbbbbbbbbbbo.', '.oxxxxxxxxxxxxo.', '.oxxssssssssxxo.'],
}
SIDE_ROW10 = {'turban': '.ohhhhhhhhhhhho.', 'keffiyeh': '.oHsssssHHHHHHo.', 'hijab': '.oHssssHHHHHHHo.',
              'cap': '.osssssxxxxxxxo.', 'longhair': '.osssssxxxxxxxo.'}
BACK_FILL = {'turban': 'x', 'keffiyeh': 'H', 'hijab': 'H', 'cap': 'x', 'longhair': 'x'}

FACE_FRONT = {   # 11~15줄
    'turban': ['.oSswesssswesSo.', '.oSseesssseesSo.', '.oSysssmmsssySo.', '..oSssssssssSo..', '...oSSSSSSSSo...'],
    'keffiyeh': ['.oHswesssswesHo.', '.oHseesssseesHo.', '.oHysssmmsssyHo.', '.oHHssssssssHHo.', '.ohHoSSSSSSoHho.'],
    'hijab': ['.oHswesssswesHo.', '.oHseesssseesHo.', '.oHysssmmsssyHo.', '..oHssssssssHo..', '...oHHssssHHo...'],
    'cap': ['.oxswesssswesxo.', '.oxseesssseesxo.', '.oSysssmmsssySo.', '..oSssssssssSo..', '...oSSSSSSSSo...'],
    'longhair': ['.oxswesssswesxo.', '.oxseesssseesxo.', '.oxysssmmsssyxo.', '.oxxssssssssxxo.', '.oxxoSSSSSSoxxo.'],
}
FACE_SIDE = ['.osewssZZZZZZZo.', '.oseessZZZZZZZo.', 'osssyssZZZZZZZo.', '.omsssZZZZZZZo..', '..oSSSZZZZZZo...']
FACE_BACK = ['.oZZZZZZZZZZZZo.', '.oZZZZZZZZZZZZo.', '.oZZZZZZZZZZZZo.', '..oZZZZZZZZZZo..', '...oZZZZZZZZo...']

# 동물 머리 (0~15줄 전체)
FALCON = {
    'd': [E16] * 4 + ['................', '....oooooooo....', '..ooHHHHHHHHoo..', '.oHHHHhhHHHHHHo.', '.oHHHHHHHHHHHHo.', '.oHccHHHHHHccHo.',
                      '.oHccccccccccHo.', '.oHcYeccccYecHo.', '.oHceecBBceecHo.', '.oHycccBBcccyHo.', '..oHccccccccHo..', '...oHHccccHHo...'],
    'l': [E16] * 4 + ['................', '....oooooooo....', '..ooHHHHHHHHoo..', '.oHHHHhhHHHHHHo.', '.oHHHHHHHHHHHHo.', '.occHHHHHHHHHHo.',
                      '.occcHHHHHHHHHo.', '.ocYeHHHHHHHHHo.', 'BBceeHHHHHHHHHo.', '.BcccyHHHHHHHo..', '..occcHHHHHHo...', '...occcHHHHo....'],
    'u': [E16] * 4 + ['................', '....oooooooo....', '..ooHHHHHHHHoo..', '.oHHHHhhHHHHHHo.', '.oHHHHHHHHHHHHo.', '.oHHHHHHHHHHHHo.',
                      '.oHHHHHHHHHHHHo.', '.oHHHHHHHHHHHHo.', '.ohHHHHHHHHHHho.', '.ohHHHHHHHHHHho.', '..ohhHHHHHHhho..', '...ohhhhhhhho...'],
}
ORYX = {
    'd': ['..h..........h..', '..hh........hh..', '...hh......hh...', '...hh......hh...', '....hhoooohh....', 'oo.oWWWWWWWWo.oo', 'oEEoWWWWWWWWoEEo',
          '.oooWWWWWWWWooo.', '..oWWWWWWWWWWo..', '.oWWWWWWWWWWWWo.', '.oWWWWWWWWWWWWo.', '.oWWweWWWWweWWo.', '.oWWeeWWWWeeWWo.',
          '.oWyWWBNNBWWyWo.', '..oWWBBBBBBWWo..', '...oWWWWWWWWo...'],
    'l': ['...........h....', '..........hh....', '.........hh.....', '........hh......', '.......hh.......', '....oooooooo....', '..ooWWWWWWWWoEEo',
          '.oWWWWWWWWWWWoEo', '.oWWWWWWWWWWWWo.', '.oWWWWWWWWWWWWo.', '.oWWWWWWWWWWWWo.', '.oWewWWWWWWWWWo.', '.oWeeWWWWWWWWWo.',
          'NBBWyWWWWWWWWWo.', '.oBBWWWWWWWWWo..', '..oBWWWWWWWWo...'],
    'u': ['..h..........h..', '..hh........hh..', '...hh......hh...', '...hh......hh...', '....hhoooohh....', 'oo.oWWWWWWWWo.oo', 'oEEoWWWWWWWWoEEo',
          '.oooWWWWWWWWooo.', '..oWWWWWWWWWWo..', '.oWWWWWWWWWWWWo.', '.oWWWWWWWWWWWWo.', '.oWWWWWWWWWWWWo.', '.oWWWWWWWWWWWWo.',
          '.oWWWWWWWWWWWWo.', '..oWWWWWWWWWWo..', '...oWWWWWWWWo...'],
}

# ---------------- 몸 틀 (16~23줄) ----------------
BODY = {
    'd': ['....oCCCCCCo....', '...oRRRTTRRRo...', '..oRRTTTTTTRRo..', '..osARRRRRRAso..', '...orrrrrrrro...',
          '....okkookko....', '....okkookko....', '....oNNooNNo....'],
    'u': ['....oCCCCCCo....', '...oRRRRRRRRo...', '..oRRTTTTTTRRo..', '..osARRRRRRAso..', '...orrrrrrrro...',
          '....okkookko....', '....okkookko....', '....oNNooNNo....'],
    'l': ['......oCCCo.....', '....oRRRRRRo....', '....oTTTTTTo....', '....oRRRsARo....', '....orrrrrro....',
          '......okko......', '......okko......', '......oNNo......'],
}
# 걷기 다리 (21~23줄): 정면·뒤는 x4~11, 옆은 x3~11 자리만 바꾼다
WALK_FRONT = {1: ['....okkookko....', '....oNNookko....', '........oNNo....'],
              3: ['....okkookko....', '....okkooNNo....', '....oNNo........']}
WALK_SIDE = {1: ['....okkookko....', '...okko.okko....', '...oNNo.oNNo....'],
             3: ['....okkokko.....', '....okkokko.....', '....oNNoNNo.....']}

BEARD = {'d': {'full': {13: (5, 10), 14: (4, 11), 15: (5, 10), 16: (6, 9)}, 'short': {13: (5, 10), 14: (5, 10), 15: (6, 9)}, 'mustache': {13: (6, 9)}},
         'l': {'full': {13: (1, 6), 14: (2, 6), 15: (3, 6)}, 'short': {13: (2, 6), 14: (2, 5)}, 'mustache': {13: (1, 3)}}}

# ---------------- 사막여우 (주인공, 순록 틀) ----------------
FENNEC = {
    'd': ['.o............o.', '.oo..........oo.', '.oAo........oAo.', '.oApo......opAo.', '.oAppo....oppAo.', '.oAppAo..oAppAo.',
          '..oApAAooAApAo..', '..oAAAAAAAAAAo..', '..oAAAAaaAAAAo..', '.oAAAAAAAAAAAAo.', '.oAAAAAAAAAAAAo.', '.oAAweAAAAweAAo.',
          '.oAAeeAAAAeeAAo.', '.oayAccnnccAyao.', '..oaAccccccAao..', '...oAccccccAo...', '...BottttttoB...', '..BoVVttttVVoB..',
          '..oVVttttgtVVo..', '..oAvVVVVVVvAoGo', '...ovvvvvvvvoGGo', '....okkookkoGLLo', '....okkookko.oo.', '....oSSooSSo....'],
    'u': ['.o............o.', '.oo..........oo.', '.oAo........oAo.', '.oAao......oaAo.', '.oAaao....oaaAo.', '.oAaaAo..oAaaAo.',
          '..oAaAAooAAaAo..', '..oAAAAAAAAAAo..', '..oAAAAAAAAAAo..', '.oAAAAAAAAAAAAo.', '.oAAAAAAAAAAAAo.', '.oAAAAAAAAAAAAo.',
          '.oAAAAAAAAAAAAo.', '.oaAAAAAAAAAAao.', '..oaAAAAAAAAao..', '...oaaaaaaaao...', '...BottttttoB...', '..oBBBBBBBBBBo..',
          '..oBTBBBBBBTBo..', '..oABBBBBBBBAoGo', '...ovvvvvvvvoGGo', '....okkookkoGLLo', '....okkookko.oo.', '....oSSooSSo....'],
    'l': ['........o...o...', '........oo..oo..', '........oAo.oAo.', '.......oApooApo.', '.......oAppoAppo', '.......oAppAAppo',
          '......oAAAAAAAo.', '.....oAAAAAAAAo.', '....oAAAAaaAAAo.', '...oAAAAAAAAAAo.', '..ocAAAAAAAAAAo.', '.occewAAAAAAAAo.',
          'nccceeAAAAAAAAo.', '.occcyAAAAAAAao.', '..occcAAAAAAao..', '...occcAAAAao...', '......ottto.....', '....oVVttVBBo...',
          '....ottttgBBo...', '....oVVAVVBBoGo.', '....ovvvvvvoGGo.', '......okko.GLLo.', '......okko..oo..', '......oSSo......'],
}
FENNEC_PAL = {'o': '3b2416', 'A': 'efc98c', 'a': 'dfae66', 'p': 'f4b3a2', 'c': 'fbecd2', 'w': 'ffffff', 'e': '2a1a12', 'n': '5a3a2a',
              'y': 'f2a29a', 'V': 'd78d2c', 'v': 'b87828', 't': '2f8a87', 'T': '335561', 'B': '2f6f8a', 'g': 'f3d38a', 'k': '3b4b7e',
              'S': '6e4429', 'G': 'f4d7a7', 'L': '7a4b2e'}

BASE = {'o': '3b2416', 'w': 'ffffff', 'e': '2a1a12', 'm': '8a4a3a', 'y': 'f2a29a', 'x': '3a2620'}
SKIN_M = {'s': 'e5b088', 'S': 'c98e68'}
SKIN_F = {'s': 'ecbc94', 'S': 'c99a74', 'm': 'd8606a'}
C = {
    'npc_bedouin': dict(head='keffiyeh', beard='short', b_col='3a2620', H='efe6d2', h='d6cab0', b='2c2420', s='d9a37a', S='b5825e',
                        R='7a5236', r='6d4930', T='5a3a26', A='7a5236', k='efe3ca', N='4e3020', staff='8a6a44'),
    'npc_child': dict(head='cap', H='f3ead6', h='d6cab0', x='2e2018', **SKIN_M, R='e0b23f', r='bf9637', T='2f6f8a', A='e0b23f', k='93a971', N='6e4429'),
    'npc_guard': dict(head='keffiyeh', beard='mustache', b_col='1e1e2c', H='efe6d2', h='d6cab0', b='c43030', s='c98e68', S='a8744f',
                      R='304680', r='283c70', T='202e58', A='304680', k='efe3ca', N='4e3020', spear=True),
    'npc_merchant': dict(head='turban', beard='full', b_col='3a2620', H='f6f0e2', h='d6cab0', **SKIN_M, R='a33a32', r='8e362c', T='e9b94a', A='a33a32', k='efe3ca', N='5b3622'),
    'npc_sailor': dict(head='cap', beard='mustache', b_col='4a3020', H='c7462f', h='9a2f22', x='4a3020', s='b9805a', S='9a6a4a',
                       R='e9e0cc', r='c6baa8', T='c7462f', A='e9e0cc', k='6b91b2', N='4e3020'),
    'npc_scholar': dict(head='turban', beard='full', b_col='b8b2aa', x='b8b2aa', H='2d4a7a', h='1f3358', **SKIN_M, R='2f5d4a', r='264c3c', T='cfa64a', A='2f5d4a', k='efe3ca', N='4e3020'),
    'npc_weaver': dict(head='hijab', H='3f4f86', h='2e3a66', **SKIN_M, R='3f4f86', r='3e4670', T='b4513d', A='3f4f86', k='c77b67', N='5b3622'),
    'player_falcon': dict(head='falcon', H='6e5444', h='4f3a2f', c='f3e5c8', Y='f0c040', B='e8a838', R='2e7c7b', r='316a67', T='c7462f', A='6e5444',
                          k='a78265', N='6a4228', s='6e5444'),
    'player_lizard': dict(head='turban', H='f5efdf', h='d8ccb0', x='5e6e2c', s='9aa84e', S='5e6e2c', y='ecd27a', m='5e6e2c', R='9b3b4a', r='883640', T='2f6f8a',
                          A='9b3b4a', k='f2e6cc', N='6e4429', tail=('9aa84e', '5e6e2c')),
    'player_oryx': dict(head='oryx', W='f6f1e6', B='c9a888', Nn='5e3b25', E='8a6650', h='4a3426', R='a63f37', r='913a30', T='e0aa3c', A='a63f37',
                        k='f3e9d3', N='5e3b25', s='f6f1e6'),
    'woman_baker': dict(head='hijab', H='efe0c0', h='cfbea1', **SKIN_F, R='e0a43a', r='c08a30', T='a8443a', A='e0a43a', k='be7165', N='5b3622'),
    'woman_bedouinWoman': dict(head='hijab', H='2e2a32', h='1e1610', b='d0d4dc', **SKIN_F, R='c43d3d', r='a23733', T='e8b84a', A='c43d3d', k='625e5f', N='5b3622'),
    'woman_calligrapher': dict(head='hijab', band=True, H='2d4a7a', h='1f3358', b='e9b94a', **SKIN_F, R='3f6a4a', r='335a3e', T='e9b94a', A='3f6a4a', k='efdfb2', N='5b3622'),
    'woman_girl': dict(head='cap', H='2f8a87', h='24706e', x='3a2418', **SKIN_F, R='f08aa8', r='cc768b', T='fff0d0', A='f08aa8', k='83a6d0', N='5b3622'),
    'woman_grandma': dict(head='hijab', H='f6f2ea', h='d4cdc4', **SKIN_F, R='7a4a8a', r='653c73', T='e9d7a0', A='7a4a8a', k='9b76a1', N='5b3622'),
    'woman_perfumer': dict(head='hijab', band=True, H='f4ede0', h='d3c9bc', b='e9b94a', **SKIN_F, R='2f8a87', r='24706e', T='e9b94a', A='2f8a87', k='f4e6be', N='5b3622'),
    'woman_qayna': dict(head='longhair', x='2a1c16', b='f6d27a', **SKIN_F, R='d9607a', r='b84e66', T='f6d27a', A='d9607a', k='8f76b2', N='5b3622'),
    'woman_weaver': dict(head='hijab', band=True, H='3f4f86', h='2e3a66', b='f0c877', **SKIN_F, R='b4513d', r='983f30', T='f0c877', A='b4513d', k='c77b67', N='5b3622'),
}


def head_rows(c, view):
    t = c['head']
    if t == 'falcon':
        return FALCON[view]
    if t == 'oryx':
        return ORYX[view]
    top = list(TOP_ROWS[t])
    if c.get('band'):
        top[5] = '.oHHbbbbbbbbHHo.'
    if view == 'd':
        return [E16] * 4 + top + FACE_FRONT[t]
    z = BACK_FILL[t]
    if view == 'l':
        top[6] = SIDE_ROW10[t]
        top[5] = ''.join('H' if (ch == 'b' and i >= 7 and t == 'hijab' and not c.get('band')) else ch for i, ch in enumerate(top[5]))
        zs = 'S' if t == 'turban' else z   # 터번 옆모습: 뒤통수는 목·피부 그늘
        return [E16] * 4 + top + [r.replace('Z', zs) for r in FACE_SIDE]
    top[6] = '.o' + z * 12 + 'o.' if t not in ('turban',) else top[6]
    if t == 'hijab' and not c.get('band'):
        top[5] = '.oHHHHHHHHHHHHo.'
    return [E16] * 4 + top + [r.replace('Z', z) for r in FACE_BACK]


def palette(c):
    pal = {k: hx(v) for k, v in BASE.items()}
    for k, v in c.items():
        if isinstance(v, str) and len(k) == 1:
            pal[k] = hx(v)
    pal.setdefault('H', pal['x'])
    pal.setdefault('b', pal['H'])
    pal.setdefault('c', pal['s'])
    pal['C'] = pal['H'] if c['head'] in ('keffiyeh', 'hijab', 'falcon') else pal['R']
    return pal


def paint(rows, pal, extra=None):
    im = Image.new('RGBA', (16, 24), (0, 0, 0, 0))
    px = im.load()
    for y, r in enumerate(rows):
        assert len(r) == 16, (y, r)
        for x, ch in enumerate(r):
            if ch == '.':
                continue
            if extra and (ch, y) in extra:
                px[x, y] = extra[(ch, y)]
            else:
                px[x, y] = pal[ch]
    return im


def base_frame(c, view):
    pal = palette(c)
    rows = head_rows(c, view) + BODY[view]
    assert len(rows) == 24, (view, len(rows))
    extra = {}
    if c['head'] == 'oryx':   # 주둥이의 콧구멍 N은 신발색이 아니다
        extra = {('N', y): hx(c['Nn']) for y in range(16)}
    im = paint(rows, pal, extra)
    px = im.load()
    if c.get('beard') and view in ('d', 'l'):
        bc = hx(c['b_col'])
        for y, (a, b) in BEARD[view][c['beard']].items():
            for x in range(a, b + 1):
                if px[x, y][3] and px[x, y] not in (pal['w'], pal['e'], pal['o']):
                    px[x, y] = bc
    if c.get('spear') or c.get('staff'):
        col_top, col = (hx('d6dce8'), hx('344c88')) if c.get('spear') else (hx(c['staff']), hx(c['staff']))
        x = 15 if view == 'u' else 0
        for y in range(3 if c.get('spear') else 9, 23):
            px[x, y] = col_top if (c.get('spear') and y <= 4) else col
    if c.get('tail'):
        t, tl = map(hx, c['tail'])
        o = pal['o']
        for (x, y), col in {(13, 19): o, (14, 19): t, (15, 19): o, (13, 20): t, (14, 20): t, (15, 20): o,
                            (12, 21): t, (13, 21): tl, (14, 21): tl, (15, 21): o, (13, 22): o, (14, 22): o}.items():
            if not (view == 'l' and x < 12):
                px[x, y] = col
    return im


def walk(im, pal, side, n, shoe='N'):
    """몸을 1픽셀 내리고(23줄은 버림) 다리 줄 21~23을 걷는 모양으로"""
    out = Image.new('RGBA', (16, 24), (0, 0, 0, 0))
    out.paste(im.crop((0, 0, 16, 23)), (0, 1))
    px = out.load()
    legs = (WALK_SIDE if side else WALK_FRONT)[n]
    x0, x1 = (3, 11) if side else (4, 11)
    for i, r in enumerate(legs):
        y = 21 + i
        for x in range(x0, x1 + 1):
            ch = r[x]
            px[x, y] = (0, 0, 0, 0) if ch == '.' else pal['N' if ch == 'N' else ch]
    return out


def frames_for(name, c):
    if name == 'player_fennec':
        pal = {k: hx(v) for k, v in FENNEC_PAL.items()}
        base = {v: paint(FENNEC[v], pal) for v in ('d', 'l', 'u')}
        wpal = dict(pal, N=pal['S'])
    else:
        pal = palette(c)
        base = {v: base_frame(c, v) for v in ('d', 'l', 'u')}
        wpal = pal
    fr = {}
    for v in ('d', 'u', 'l'):
        side = v == 'l'
        fr[f'{v}0'] = fr[f'{v}2'] = base[v]
        fr[f'{v}1'] = walk(base[v], wpal, side, 1)
        fr[f'{v}3'] = walk(base[v], wpal, side, 3)
    for i in range(4):
        fr[f'r{i}'] = ImageOps.mirror(fr[f'l{i}'])
    return fr


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    names = list(C) + ['player_fennec']
    n = 0
    for name in names:
        for k, im in frames_for(name, C.get(name, {})).items():
            im.save(OUT / f'arabia_{name}_{k}.png')
            n += 1
    print('ok', n)
