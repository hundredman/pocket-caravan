"""C3a: retain geometry/pixels; upper parts of tall props occlude characters.

Two foreground rows remain below characters so a player standing in front keeps
its face visible. Existing native canopy rules and short shrubs are unchanged.
"""
BUILDINGS={'house_tile','house_thatch','house_plain','house_wall','house3','shop','store','hall',
           'goryeo_royal','goryeo_manor','goryeo_stall','goryeo_cottage','goryeo_pavilion'}


def desired_rows(name,w,h,existing):
    if existing:return existing
    if name in BUILDINGS or name.startswith('goryeo_woodland') and h>=3:
        return max(1,h-2)
    return existing


def apply(maps):
    from tiles import STAMPS,K
    from goryeo_art import REPLACE
    for m in maps:
        moves=[]
        for name,x,y in m.stamps:
            if name not in STAMPS:continue
            sheet,sx,sy,w,h,block,high,recolor=STAMPS[name]
            target=desired_rows(name,w,h,high)
            if target<=high:continue
            for dy in range(high,target):
                for dx in range(w):
                    xx,yy=x+dx,y+dy
                    if not (0<=xx<m.w and 0<=yy<m.h):continue
                    key=m.obj[yy][xx]
                    if not key:continue
                    expected=K(sheet,sx+dx,sy+dy,recolor)
                    if not m.id.startswith('transit_') and name in REPLACE:
                        expected=f'goryeo:{REPLACE[name]}:{w}:{h}:{dx}:{dy}'
                    if key.split('@')[0]!=expected.split('@')[0]:continue
                    if m.high[yy][xx]:raise ValueError(('Occlusion layer already occupied',m.id,name,xx,yy))
                    m.obj[yy][xx]=None;m.high[yy][xx]=key
                    moves.append(dict(x=xx,y=yy,key=key,name=name))
        m.occlusion_moves=moves
    return maps
