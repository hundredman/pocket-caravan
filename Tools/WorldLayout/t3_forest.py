"""11/08 v4 (은샘): 새로 채운 숲·숲 사이 길 둘레를 기존 숲과 같은 방식으로 다시 그린다.

기존 숲(m1_tiles_preview.forest_block, FOREST_BLOCK, seed 3)은 승인된 나무 그림을 6x6 칸 격자 자리에 통째로 놓아 그렸다.
그래서 가장자리에서는 나무가 잘리지 않고 풀밭 위로 잎과 밑동이 보인다. 빈칸 채우기(16x16 조각 반복)와 길을 낸 자리는
네모나게 잘려 있었으므로, 그 둘레만 같은 격자·같은 나무·같은 층 규칙으로 다시 그린다.
- 숲 칸(막힘)은 그대로 막힘. 걷는 칸은 그대로 걷는 칸(그림만 바뀜).
- 칸 그림은 원본 나무 그림을 통째로 겹친 것(m4_certify 규칙)이고, 새 칸은 m4_<해시> 이름과 겹친 기록을 함께 낸다.
"""
import hashlib,json
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT=Path(__file__).resolve().parents[2];T=16;P=6
MANIFEST=ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json'
KINDS=['forest_round:3:4','forest_bent:3:4','broadleaf:4:4','round_west:4:3','round_east:4:3','forest_wide:4:3','light_tree:3:4']
SHADE='m4_e541e99dbe9a';GRASS='floor_0_12_goryeo_jade'

def lattice():
    r=np.random.default_rng(3);lat={}
    for gy in range(0,P,2):
        for gx in range(0,P,2):
            for oy,ox in((0,0),(1,1)):lat[((gy+oy)%P,(gx+ox)%P)]=KINDS[int(r.integers(len(KINDS)))]
    return lat

class Namer:
    """Compose a 16px cell from whole approved sprites at offsets (m4_certify rule); reuse an existing tile name by hash."""
    def __init__(self,manifest,sprite):
        self.by_sha={e['sha256']:n for n,e in manifest['tiles'].items()};self.spr=sprite;self.cache={};self.tiles={};self.entries={}
    def image(self,parts):
        canvas=Image.new('RGBA',(T,T),(0,0,0,0))
        for sid,dx,dy in parts:
            src=self.spr(sid[7:]);sx,sy=max(0,-dx),max(0,-dy);tx,ty=max(0,dx),max(0,dy)
            if sx>=src.width or sy>=src.height or tx>=T or ty>=T:continue
            canvas.alpha_composite(src,dest=(tx,ty),source=(sx,sy,min(src.width,sx+T-tx),min(src.height,sy+T-ty)))
        return canvas
    def name(self,layer,parts):
        key=(layer,tuple(tuple(p)for p in parts))
        if key in self.cache:return self.cache[key]
        canvas=self.image(parts)
        if canvas.getbbox()is None:self.cache[key]='';return ''
        sha=hashlib.sha256(canvas.tobytes()).hexdigest();n=self.by_sha.get(sha)
        if not n:
            n='m4_'+sha[:12];self.by_sha[sha]=n;self.tiles[n]=canvas
            self.entries[n]=dict(sha256=sha,layer=layer,base=None,parts=[list(p)for p in parts])
        self.cache[key]=n;return n

def render(tile_world,walk,water,owners,boxes,ids,oy,changed,radius=4):
    m=json.loads(MANIFEST.read_text());tiles=m['tiles'];sources=m['sources']
    forest_src={'sprite:'+k for k in KINDS}
    def forest_only(name):
        e=tiles.get(name)
        return bool(e)and bool(e['parts'])and all(p[0]in forest_src for p in e['parts'])
    H,W=walk.shape;lat=lattice()
    inbox=np.zeros((H,W),bool)
    for idx,mid in enumerate(ids):
        x0,y0,x1,y1=boxes[mid];inbox[y0-oy:y1-oy,x0:x1]|=owners[y0-oy:y1-oy,x0:x1]==idx
    obj,high,ground=tile_world['obj'],tile_world['high'],tile_world['ground']
    cache={}
    def ok_pieces(r,c):return all(not n or forest_only(n)for n in(obj[r,c],high[r,c]))
    win=ndimage.binary_dilation(changed,iterations=radius+4)
    forest=np.zeros((H,W),bool)
    for r,c in zip(*np.nonzero(win)):
        if walk[r,c]or water[r,c]:continue
        if changed[r,c]or ground[r,c]==SHADE or(obj[r,c]and forest_only(obj[r,c])):forest[r,c]=True
    # Past the outer edge of the world (no scene) the forest is taken to go on, so the edge stays dense, not a row of trunks.
    # Not where a road or walkable land runs out of the scene (the road keeps going off-map).
    beyond=~inbox&win&ndimage.binary_dilation(forest&inbox,iterations=3)&~ndimage.binary_dilation(walk&inbox,iterations=3)&~water
    forest|=beyond
    region=ndimage.binary_dilation(changed,iterations=radius)&inbox&~water
    for r,c in zip(*np.nonzero(region)):
        if not(forest[r,c]or(walk[r,c]and ok_pieces(r,c))):region[r,c]=False
    below=np.zeros_like(forest);below[:-2]=forest[1:-1]&forest[2:]
    under=ndimage.binary_erosion(forest)&below
    sprites={}
    def spr(k):
        if k not in sprites:sprites[k]=Image.open(ROOT/sources['sprite:'+k]['path']).convert('RGBA')
        return sprites[k]
    # trees: trunk on a forest lattice cell
    trees=[]
    rr,cc=np.nonzero(ndimage.binary_dilation(region,iterations=4)&forest)
    for r,c in zip(rr,cc):
        k=lat.get(((r+oy+130)%P,c%P))
        if not k:continue
        sw,sh=int(k.split(':')[1]),int(k.split(':')[2]);trees.append((int(r),int(c-sw//2),int(r-sh+1),k))
    trees.sort()
    pieces={}
    for ty,x0,y0,k in trees:
        sw,sh=spr(k).width//T,spr(k).height//T
        for yy in range(y0,y0+sh):
            for xx in range(x0,x0+sw):
                if 0<=yy<H and 0<=xx<W and region[yy,xx]:
                    layer='obj'if(forest[yy,xx]or ty<=yy)else'high'
                    pieces.setdefault((layer,yy,xx),[]).append(('sprite:'+k,(x0-xx)*T,(y0-yy)*T))
    namer=Namer(m,spr)
    def name_for(layer,parts):return namer.name(layer,parts)
    changed_cells=0
    for r,c in zip(*np.nonzero(region)):
        before=tuple(tile_world[k][r,c]for k in('ground','deco','obj','high'))
        if forest[r,c]:
            ground[r,c]=SHADE if under[r,c]else GRASS;tile_world['deco'][r,c]=''
        elif ground[r,c]==SHADE:ground[r,c]=GRASS
        obj[r,c]=name_for('obj',pieces.get(('obj',r,c),[]))
        high[r,c]=name_for('high',pieces.get(('high',r,c),[]))
        if before!=tuple(tile_world[k][r,c]for k in('ground','deco','obj','high')):changed_cells+=1
    return dict(tiles=namer.tiles,entries=namer.entries,cells=int(region.sum()),changedCells=changed_cells)
