"""11/08 (은샘): 호수 연꽃을 물 경계 안쪽 깊은 물에, 자연스럽게 떠 있게 다시 놓는다.

예전 연꽃(승인 그림 lotus 2x1)은 물가 가장자리 타일(물거품·얕은 물 띠)에 걸치거나 칸 격자에 딱 맞춰 한 줄로 놓여 있었다.
- 놓는 자리: 깊은 물(pond_center) 칸이고 둘레 8칸도 모두 깊은 물인 칸만. 그림의 보이는 부분이 그 칸들 안에만 든다.
- 모양: 물가에서 2~5칸 떨어진 곳에 1~3장씩 무리 지어, 칸 격자에서 몇 픽셀씩 비켜 놓는다(겹친 그림은 아래 것이 앞).
- 무리마다 옆 깊은 물 칸에 기존 물결 움직임(goryeo_ripples)을 하나 둔다.
- 칸 그림은 승인된 연꽃 그림을 통째로 겹친 것(m4_certify 규칙). 막힘·물 칸은 바뀌지 않는다(물은 원래 막힘).
"""
import json
import numpy as np
from PIL import Image
from scipy import ndimage
import t3_forest
from t3_forest import T,ROOT

LOTUS='sprite:lotus:2:1'
VISIBLE=(0,5,32,16)   # 그림에서 보이는 부분(x0,y0,x1,y1, 픽셀)

def render(tile_world,water,owners,boxes,ids,oy,seed=11,groups=8,near=(2.0,5.0),spacing=7):
    m=json.loads(t3_forest.MANIFEST.read_text());tiles=m['tiles'];sources=m['sources']
    sprites={}
    def spr(k):
        if k not in sprites:sprites[k]=Image.open(ROOT/sources['sprite:'+k]['path']).convert('RGBA')
        return sprites[k]
    namer=t3_forest.Namer(m,spr)
    H,W=water.shape
    inbox=np.zeros((H,W),bool)
    for idx,mid in enumerate(ids):
        x0,y0,x1,y1=boxes[mid];inbox[y0-oy:y1-oy,x0:x1]|=owners[y0-oy:y1-oy,x0:x1]==idx
    obj,high=tile_world['obj'],tile_world['high']
    # 1) 예전 연꽃을 걷는다(같은 칸의 다른 그림 조각은 그대로 다시 겹친다)
    old=np.zeros((H,W),bool);removed=0
    for layer in('obj','high'):
        arr=tile_world[layer]
        for r,c in zip(*np.nonzero(inbox)):
            n=arr[r,c];e=tiles.get(n)if n else None
            if not e or not any(p[0]==LOTUS for p in e['parts']):continue
            arr[r,c]=namer.name(layer,[p for p in e['parts']if p[0]!=LOTUS]);old[r,c]=True;removed+=1
    if not old.any():return dict(tiles={},entries={},removedCells=0,lotus=[],ripples=[])
    # 2) 연꽃이 있던 호수(물 덩어리)만
    lab,_=ndimage.label(water);lakes=np.isin(lab,np.unique(lab[old&water]))
    deep=lakes&(tile_world['ground']=='pond_center')&inbox
    inner=ndimage.binary_erosion(deep,np.ones((3,3),bool))&(obj=='')&(high=='')
    dist=ndimage.distance_transform_edt(water)
    rng=np.random.default_rng(seed)
    cand=[(int(r),int(c))for r,c in zip(*np.nonzero(inner&(dist>=near[0])&(dist<=near[1])))]
    rng.shuffle(cand)
    vx0,vy0,vx1,vy1=VISIBLE
    def cells(px,py):
        ys=range((py+vy0)//T,(py+vy1-1)//T+1);xs=range((px+vx0)//T,(px+vx1-1)//T+1)
        return [(y,x)for y in ys for x in xs]
    taken=np.zeros((H,W),bool);placed=[];anchors=[];ripples=[]
    for r,c in cand:
        if len(anchors)>=groups:break
        if any(abs(r-a)+abs(c-b)<spacing for a,b in anchors):continue
        n=int(rng.choice([1,2,2,3]))
        offs=[(int(rng.integers(-5,6)),int(rng.integers(-4,4)))]
        for k in range(1,n):
            bx,by=offs[0];offs.append((bx+int(rng.choice([-1,1]))*int(rng.integers(14,22)),by+int(rng.integers(5,11))))
        group=[];ok=True
        for dx,dy in offs:
            px,py=c*T+dx,r*T+dy;cs=cells(px,py)
            if not all(0<=y<H and 0<=x<W and inner[y,x]and not taken[y,x]for y,x in cs):ok=False;break
            group.append((px,py,cs))
        if not ok or not group:continue
        anchors.append((r,c))
        for px,py,cs in group:
            placed.append((py,px))
            for y,x in cs:taken[y,x]=True
        # 물결: 무리 옆(아래·양옆) 깊은 물 칸 하나
        around=sorted({(y+dy_,x+dx_)for _,_,cs in group for y,x in cs for dy_,dx_ in((1,0),(0,-1),(0,1))},key=lambda p:(-p[0],p[1]))
        for y,x in around:
            if 0<=y<H and 0<=x<W and inner[y,x]and not taken[y,x]:ripples.append((int(x),int(y+oy)));taken[y,x]=True;break
    # 3) 칸마다 겹쳐 그린다(밑동 줄=아래 것이 앞)
    pieces={}
    for py,px in sorted(placed):
        for y,x in cells(px,py):pieces.setdefault((y,x),[]).append([LOTUS,px-x*T,py-y*T])
    for (y,x),parts in pieces.items():obj[y,x]=namer.name('obj',parts)
    return dict(tiles=namer.tiles,entries=namer.entries,removedCells=removed,
                lotus=[[px/T,py/T+oy]for py,px in sorted(placed)],ripples=ripples)
