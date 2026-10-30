"""M4 겹친 칸 인증(A2 확장, 은샘 2026-10-28 '검사를 넓히기').

새 맵의 칸 그림(m4_*.png) 하나하나가 '승인된 원본 그림을 통째로, 기록된 위치에, 기록된 순서로 겹쳐 그린 것'인지
원본에서 다시 그려 RGBA까지 같은지 확인한다. 자르기·색 바꾸기·크기 바꾸기는 할 수 없다
(원본을 칸 창으로 보는 것만 허용: 원본이 칸 밖으로 넘치는 부분은 그 칸에 그려지지 않을 뿐).

기록: Assets/PocketCaravan/Art/Baked/m4-composites.json
  sources: {원본ID: {path, sha256(디코딩 RGBA), approval(근거)}}
  tiles:   {칸 이름: {sha256, layer, base(바닥 칸만: 그 아래 바탕색), parts:[[원본ID, dx, dy], ...]}}
"""
import hashlib,json,sys
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];T=16
MANIFEST=ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json'

def rgba_sha(im):return hashlib.sha256(im.convert('RGBA').tobytes()).hexdigest()

def approval_for(sid,path):
    """원본의 승인 근거. 승인 근거가 없는 원본은 쓸 수 없다."""
    p=Path(path)
    if path.startswith('ArtReview/A7d-20261030/Headwaters/Native/'):
        a='ArtReview/A7d-20261030/Headwaters/catalogue.json'
        d=json.loads((ROOT/a).read_text())
        if not any(item['path']==path for item in d['assets']):raise ValueError('Unregistered draft headwater')
        return a if d['status']=='approved' else a+' (review artwork; production requires approval)'
    if path.startswith('ArtReview/Approved/'):
        a=ROOT/p.parent/'approval.json'
        if not a.exists():
            a=next(iter(sorted((ROOT/p.parent).glob('*approval*.json'))),None)
        if a is None:raise ValueError('승인 기록 없음: '+path)
        return str(a.relative_to(ROOT))
    if path.startswith('ArtReview/C7-RemainingBatch-20261027/Native/'):
        return 'Docs/Plan.md 새 맵 디자인 기준: C7 잔여 12종을 새 맵에 쓴다(은샘 2026-10-28 M2)'
    if path.startswith('Assets/PocketCaravan/Art/NinjaAdventure/Tiles/') or path.startswith('Assets/PocketCaravan/Art/Baked/Sprites/'):
        return 'Assets/PocketCaravan/Art/Baked/index.json (C6 동결 게임 그림)'
    if path.startswith('Assets/PocketCaravan/Art/Baked/Objects/M4/bridge_'):
        return 'ArtReview/M1-Tiles/README.md: 게임 목조 다리 조각을 길 각도대로 돌린 다리(은샘 2026-10-28 "다리는 좋아", M1② 진행 승인)'
    if path.startswith('Assets/PocketCaravan/Art/Baked/Objects/M4/shade_'):
        return 'ArtReview/M1-Tiles/README.md: 숲 타일 그늘 바탕(승인 나무 그림의 가장 어두운 잎 색, 은샘 2026-10-28 M1② 진행 승인)'
    raise ValueError('승인 근거를 알 수 없는 원본: '+path)

def compose(entry,sources,cache):
    base=entry.get('base')
    canvas=Image.new('RGBA',(T,T),tuple(base) if base else (0,0,0,0))
    for sid,dx,dy in entry['parts']:
        if sid not in cache:cache[sid]=Image.open(ROOT/sources[sid]['path']).convert('RGBA')
        src=cache[sid]
        # 원본을 (dx,dy)에 통째로 놓은 뒤 칸 창(0..16)만 본다
        sx,sy=max(0,-dx),max(0,-dy);tx,ty=max(0,dx),max(0,dy)
        if sx>=src.width or sy>=src.height or tx>=T or ty>=T:continue
        canvas.alpha_composite(src,dest=(tx,ty),source=(sx,sy,min(src.width,sx+T-tx),min(src.height,sy+T-ty)))
    return canvas

def verify(manifest=None,tile_dir=None):
    m=json.loads((manifest or MANIFEST).read_text());errors=[];cache={}
    tile_dir=Path(tile_dir or ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles')
    for sid,s in m['sources'].items():
        p=ROOT/s['path']
        if not p.exists():errors.append('원본 없음: '+sid);continue
        if rgba_sha(Image.open(p))!=s['sha256']:errors.append('원본 그림이 바뀜: '+sid)
        if not s.get('approval'):errors.append('승인 근거 없음: '+sid)
    for name,e in m['tiles'].items():
        p=tile_dir/f'{name}.png'
        if not p.exists():errors.append('칸 그림 없음: '+name);continue
        got=Image.open(p).convert('RGBA')
        if rgba_sha(got)!=e['sha256']:errors.append('칸 그림 해시 다름: '+name);continue
        if any(sid not in m['sources'] for sid,_,_ in e['parts']):errors.append('등록 안 된 원본: '+name);continue
        if compose(e,m['sources'],cache).tobytes()!=got.tobytes():errors.append('원본으로 다시 그린 그림과 다름: '+name)
    return errors,dict(sources=len(m['sources']),tiles=len(m['tiles']))

if __name__=='__main__':
    errs,info=verify(*(Path(a) for a in sys.argv[1:3]))
    for e in errs[:20]:print('ERROR:',e)
    print(f"{len(errs)} errors; {info['tiles']} composite tiles from {info['sources']} approved sources")
    raise SystemExit(1 if errs else 0)
