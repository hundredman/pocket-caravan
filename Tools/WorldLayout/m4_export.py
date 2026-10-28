"""M4 ② 새 맵 13개를 게임 맵 JSON 형식으로 내보낸다(검토용, 본편은 바뀌지 않음).

입력: `m1_tiles_preview.py --zones`를 M4_LAYERS=<폴더> 로 실행해 얻은 층 그림(ground·deco·obj·high)과 masks.npz.
맵 하나 = 그 맵 구역을 감싸는 직사각형. 구역 밖 칸은 이웃 맵 그림을 그대로 보여 주되 막는다.
16px 칸으로 나눈 그림은 같은 것끼리 묶고, 이미 있는 타일(Art/NinjaAdventure/Tiles)과 같으면 그 이름을 쓴다.
새 칸 그림은 'm4_<해시>'. 승인된 그림을 통째로 놓은 뒤 렌더링 칸으로 나눈 것이다(자르기·변형 아님).

출력(검토 전용):
  Assets/PocketCaravan/Editor/B2Preview/M4/Maps/<mapId>.json   (MapJsonBuilder.BuildSingleMap 미리보기용)
  Assets/PocketCaravan/Editor/B2Preview/M4/Tiles/m4_*.png
  ArtReview/M4-Maps/<mapId>.png(JSON에서 다시 그린 그림), report.json
"""
import argparse,hashlib,json,sys
from pathlib import Path
import numpy as np
from PIL import Image
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];sys.path.insert(0,str(HERE))
import terrain_v3 as v
T=16
PREV=ROOT/'Assets/PocketCaravan/Editor/B2Preview/M4'
REVIEW=ROOT/'ArtReview/M4-Maps'
TILES=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
# M0 대응표(Docs/Validation/M0-2026-10-28.md)
MAPID={'goryeo/나루':'Naru','goryeo/장단':'goryeo_jangdan','goryeo/송악':'goryeo_songak','goryeo/닥목':'Dakmok','goryeo/개경':'Gaegyeong',
       'east/동문 마을':'transit_woodtown','east/북동 포구':'transit_ferry','east/중앙 길목 마을':'transit_crossroads','east/남동 포구':'transit_southport',
       'east/북동 길':'transit_wetroad','east/남동 길':'transit_pass','east/북쪽 내륙길':'transit_central_approach','east/남쪽 내륙길':'transit_south_inland'}
NAMES={'transit_southport':'남동 포구','transit_south_inland':'남쪽 내륙길'}

def existing_tiles():
    h={}
    for p in sorted(TILES.glob('*.png')):
        im=Image.open(p).convert('RGBA')
        if im.size!=(T,T):continue
        h.setdefault(hashlib.sha256(im.tobytes()).hexdigest(),p.stem)
    return h

def main():
    ap=argparse.ArgumentParser();ap.add_argument('layers');a=ap.parse_args()
    L=Path(a.layers);m=np.load(L/'masks.npz');keys=[str(k) for k in m['keys']]
    lay={k:Image.open(L/f'{k}.png').convert('RGBA') for k in('ground','deco','obj','high')}
    arr={k:np.array(im) for k,im in lay.items()}
    known=existing_tiles();newt={};names={}
    (PREV/'Maps').mkdir(parents=True,exist_ok=True);(PREV/'Tiles').mkdir(parents=True,exist_ok=True);REVIEW.mkdir(parents=True,exist_ok=True)
    def cell_name(layer,y,x):
        a_=arr[layer][y*T:(y+1)*T,x*T:(x+1)*T]
        if a_[:,:,3].max()==0:return ''
        b=a_.tobytes();hh=hashlib.sha256(b).hexdigest()
        if hh in names:return names[hh]
        n=known.get(hh) or 'm4_'+hh[:12]
        if n.startswith('m4_') and n not in newt:newt[n]=Image.fromarray(a_)
        names[hh]=n;return n
    rep={}
    # ③ 맵 객체: 경계 출입구는 모든 맵을 함께 보고 만든다
    import m4_objects
    mids=[MAPID.get(k,'') for k in keys]
    gwalk=(~m['blocked'])&m['own']&(m['mapid']>=0)
    seam=m4_objects.seams(m['mapid'],gwalk,mids,v.X0,v.Y0)   # 세계 좌표로
    zones=json.loads((ROOT/'ArtReview/M2-Zones/zones.json').read_text())['villages']
    oldobj=json.loads((ROOT/'ArtReview/M0/old-objects.json').read_text())
    for key,mid in MAPID.items():
        mm=m['mapid']==keys.index(key);ys,xs=np.nonzero(mm)
        y0,y1,x0,x1=ys.min(),ys.max()+1,xs.min(),xs.max()+1;w,h=int(x1-x0),int(y1-y0)
        pal=[''];idx={'':0}
        def P(n):
            if n not in idx:idx[n]=len(pal);pal.append(n)
            return idx[n]
        layers={k:[] for k in('ground','deco','obj','high')};blocked=[];road=[];water=[]
        for y in range(y0,y1):
            for x in range(x0,x1):
                for k in layers:layers[k].append(P(cell_name(k,y,x)) if m['own'][y,x] else 0)
                inside=bool(mm[y,x])
                blocked.append(int(m['blocked'][y,x] or not inside or not m['own'][y,x]))
                road.append(int(m['grade'][y,x]) if inside else 0)
                water.append(int(m['water'][y,x]))
        # 걸어서 닿는 칸: 길에서 이어진 걸을 수 있는 칸
        from scipy import ndimage
        walk=(np.array(blocked).reshape(h,w)==0)
        lab,_=ndimage.label(walk);g=np.array(road).reshape(h,w)>0
        ok=set(np.unique(lab[g&walk]))-{0}
        reach=np.isin(lab,list(ok)).astype(int).ravel().tolist()
        inside_=mm[y0:y1,x0:x1]
        vz=[z for z in zones.values() if any(it.get('kind')=='plaza' and not it.get('missing') and inside_[v.idx(it['x'],it['y'])[0]-y0,v.idx(it['x'],it['y'])[1]-x0] for it in z['items'] if 'x' in it and y0<=v.idx(it['x'],it['y'])[0]<y1 and x0<=v.idx(it['x'],it['y'])[1]<x1)]
        objects,orep=m4_objects.build(mid,key,(int(x0+v.X0),int(y0+v.Y0)),walk,np.array(road).reshape(h,w),np.array(water,bool).reshape(h,w),inside_,
                                      vz,[o for o in oldobj if o['mapId']==mid and o['kind'] not in('spawn','exit','anim','sign')],seam.get(mid,[]))
        old=ROOT/f'Assets/PocketCaravan/Maps/{mid}.json'
        oj=json.loads(old.read_text()) if old.exists() else {}
        data=dict(id=mid,name=oj.get('name') or NAMES.get(mid,key.split('/')[1]),w=w,h=h,hours=oj.get('hours',0),
                  ground=layers['ground'],deco=layers['deco'],obj=layers['obj'],high=layers['high'],road=road,blocked=blocked,
                  objects=objects,legacyHeight=h,palette=pal,reachable=reach,water=water,
                  terrainX=int(x0+v.X0),terrainY=int(y0+v.Y0),previewOnly=True)   # 미리보기: 저장 보호(④ 본편 전환 때 뺀다)
        (PREV/'Maps'/f'{mid}.json').write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')))
        # JSON만으로 다시 그려 원래 그림과 비교
        img=Image.new('RGBA',(w*T,h*T),(28,34,40,255))
        cache={}
        def tile_img(n):
            if n not in cache:cache[n]=newt[n] if n in newt else Image.open(TILES/f'{n}.png').convert('RGBA')
            return cache[n]
        for k in('ground','deco','obj','high'):
            for i,pv in enumerate(layers[k]):
                if pv:img.alpha_composite(tile_img(pal[pv]),((i%w)*T,(i//w)*T))
        ref=Image.alpha_composite(Image.alpha_composite(Image.alpha_composite(lay['ground'].crop((x0*T,y0*T,x1*T,y1*T)),lay['deco'].crop((x0*T,y0*T,x1*T,y1*T))),
              lay['obj'].crop((x0*T,y0*T,x1*T,y1*T))),lay['high'].crop((x0*T,y0*T,x1*T,y1*T)))
        bg=Image.new('RGBA',ref.size,(28,34,40,255));ref=Image.alpha_composite(bg,ref)
        ownpx=np.kron(m['own'][y0:y1,x0:x1],np.ones((T,T),bool))   # 담당 밖(다른 팀 구역)은 그리지 않으므로 비교에서 뺀다
        diff=int(((np.array(img)!=np.array(ref)).any(axis=2)&ownpx).sum())
        # 검토 그림: 막힌 칸은 붉게 살짝
        rv=img.copy();ov=np.zeros((h*T,w*T,4),np.uint8)
        bl=np.array(blocked).reshape(h,w)
        for yy,xx in np.argwhere(bl&mm[y0:y1,x0:x1]):ov[yy*T:(yy+1)*T,xx*T:(xx+1)*T]=(255,40,40,60)
        for yy,xx in np.argwhere(~mm[y0:y1,x0:x1]):ov[yy*T:(yy+1)*T,xx*T:(xx+1)*T]=(0,0,0,150)
        img.convert('RGB').save(REVIEW/f'{mid}.png')
        # 객체 표시판: 주황 출구, 하늘 도착 자리, 분홍 NPC·주민, 노랑 시장·게시판, 흰 S0
        ob=img.copy();from PIL import ImageDraw;dr=ImageDraw.Draw(ob)
        COL={'exit':(255,140,0),'spawn':(80,200,255),'market':(255,220,60),'board':(255,220,60),'npc':(255,80,200),'talker':(255,120,220),
             'worker':(255,120,220),'villager':(255,160,230),'pastime':(255,120,220),'walker':(255,160,230),'bandit':(255,0,0)}
        for o in objects:
            c_=COL.get(o['kind'])
            if not c_:continue
            if o.get('id')=='S0':c_=(255,255,255)
            dr.rectangle([o['x']*T,o['y']*T,(o['x']+o['w'])*T-1,(o['y']+o['h'])*T-1],outline=c_,width=3 if o['kind'] in('exit','spawn') else 4)
        ob.resize((w*T//2,h*T//2),Image.BOX).convert('RGB').save(REVIEW/f'{mid}-objects.png')
        rv.alpha_composite(Image.fromarray(ov));rv.resize((w*T//2,h*T//2),Image.BOX).convert('RGB').save(REVIEW/f'{mid}-blocked.png')
        rep[mid]=dict(key=key,w=w,h=h,cells=int(mm.sum()),walkable=int(walk[mm[y0:y1,x0:x1]].sum()),reachable=int(sum(reach)),
                      palette=len(pal),pixelDiff=diff,terrain=[int(x0+v.X0),int(y0+v.Y0)],
                      villages=[z['slug'] for z in vz],exits=sum(o['kind']=='exit' for o in objects),spawns=sum(o['kind']=='spawn' for o in objects),
                      objects={k_:sum(o['kind']==k_ for o in objects) for k_ in sorted({o['kind'] for o in objects})},moved=orep['moved'],missing=orep['missing'])
        print(mid,rep[mid])
    for n,im in newt.items():im.save(PREV/'Tiles'/f'{n}.png')
    # 미리보기 빌더는 미리보기 폴더의 Tiles만 읽으므로, 다시 쓴 기존 타일도 복사해 둔다(본편 전환 때는 기존 위치를 그대로 씀)
    import shutil
    for n in set(names.values()):
        if not n.startswith('m4_'):shutil.copyfile(TILES/f'{n}.png',PREV/'Tiles'/f'{n}.png')
    rep['_tiles']=dict(newTiles=len(newt),reusedExisting=len([n for n in names.values() if not n.startswith('m4_')]))
    (REVIEW/'report.json').write_text(json.dumps(rep,ensure_ascii=False,indent=1))
    print(rep['_tiles'])

if __name__=='__main__':main()
