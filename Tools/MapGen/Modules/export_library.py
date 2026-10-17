"""수정 가능한 source → map JSON + 공유 sprite atlas + catalogue. 본편 자동 등록 없음."""
import sys,json,hashlib,math,re
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent));sys.path.insert(0,str(HERE))
from tiles import tile_image
from PIL import Image,ImageDraw,ImageFont
from catalogue import ROOT,DEST,write_sources
from layouts import build
from skins import SKINS,recolor
BASE=ROOT/'Assets/PocketCaravan/MapModules'
sys.path.insert(0,str(ROOT/'Tools/Integration'))
from obstacle_validation import validate_placements

def check_obstacles(data):
    if data.get('obstacles'):
        defs=json.loads((ROOT/'Assets/PocketCaravan/Obstacles/definitions.json').read_text())['definitions']
        errors=validate_placements(data,{d['id']:d for d in defs})
        if errors:raise ValueError('; '.join(errors))



def name(k):return k.replace(':','_').replace('@','_')


def serialize(m):
    palette=[''];index={'':0}
    def flat(layer):
        result=[]
        for row in layer:
            for key in row:
                key=name(key) if key else ''
                if key not in index:index[key]=len(palette);palette.append(key)
                result.append(index[key])
        return result
    data=dict(id=m.id,name=m.name,w=m.w,h=m.h,hours=0)
    for layer in ['ground','deco','obj','high']:data[layer]=flat(getattr(m,layer))
    data.update(palette=palette,road=[v for row in m.road for v in row],blocked=[int(v) for row in m.blocked for v in row],objects=[o for o in m.objects if o['kind']=='spawn'])
    if m.module_spec.get("obstacles"):data["obstacles"]=m.module_spec["obstacles"]
    check_obstacles(data)
    return data


def export():
    write_sources();maps=[];used=set();ids=set()
    for src in sorted(DEST.glob('*.json')):
        spec=json.loads(src.read_text())
        if not re.fullmatch(r'[a-z][a-z0-9_]*',spec['id']) or spec['id']!=src.stem or spec['id'] in ids:
            raise ValueError(f'원형 파일명/ID 중복 또는 형식 오류: {src.name}')
        if spec['environment'] not in {s[0] for s in SKINS}:raise ValueError(f'미등록 환경: {src.name}')
        ids.add(spec['id']);m=build(spec);maps.append(m)
        for layer in [m.ground,m.deco,m.obj,m.high]:used.update(k for row in layer for k in row if k)
        out=BASE/'Layouts';out.mkdir(parents=True,exist_ok=True)
        (out/(spec['id']+'.json')).write_text(json.dumps(serialize(m),ensure_ascii=False,separators=(',',':'))+'\n')
    # Existing registered terrain can use the same skin identities.
    from world_registry import atlas_content
    _,current=atlas_content()
    for m in current:
        for layer in [m.ground,m.deco,m.obj,m.high]:used.update(k for row in layer for k in row if k)
    keys=sorted(used);cols=32;rows=math.ceil(len(keys)/cols)
    images={k:tile_image(k) for k in keys}
    tilesdir=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure/Tiles'
    for k,im in images.items():
        p=tilesdir/(name(k)+'.png')
        if not p.exists():im.save(p)
    colored={skin[0]:{k:recolor(im,k,skin) for k,im in images.items()} for skin in SKINS}
    atlasdir=BASE/'Skins';atlasdir.mkdir(parents=True,exist_ok=True)
    for skin in SKINS:
        atlas=Image.new('RGBA',(cols*16,rows*16))
        for i,k in enumerate(keys):atlas.paste(colored[skin[0]][k],((i%cols)*16,(i//cols)*16))
        atlas.save(atlasdir/(skin[0]+'.png'))
    (BASE/'skin-index.json').write_text(json.dumps(dict(columns=cols,tileSize=16,tileNames=[name(k) for k in keys],environments=[dict(id=s[0],label=s[1]) for s in SKINS]),ensure_ascii=False,indent=2)+'\n')
    entries=[]
    previewdir=BASE/'Previews';previewdir.mkdir(parents=True,exist_ok=True)
    for m in maps:
        spec=m.module_spec;skin=next(s for s in SKINS if s[0]==spec['environment'])
        im=Image.new('RGBA',(m.w*16,m.h*16))
        for layer in [m.ground,m.deco,m.obj,m.high]:
            for y,row in enumerate(layer):
                for x,k in enumerate(row):
                    if k:im.alpha_composite(colored[skin[0]][k],(x*16,y*16))
        im.resize((320,320),Image.Resampling.NEAREST).save(previewdir/(spec['id']+'.png'))
        data=serialize(m)
        digest=hashlib.sha256(json.dumps({k:data[k] for k in ['road','blocked','objects']},sort_keys=True).encode()).hexdigest()
        entries.append(dict(id=spec['id'],label=spec['label'],category=spec['category'],environment=spec['environment'],ports=m.module_ports,jsonPath=f'Assets/PocketCaravan/MapModules/Layouts/{spec["id"]}.json',sourcePath=f'Assets/PocketCaravan/MapModules/Sources/{spec["id"]}.json',previewPath=f'Assets/PocketCaravan/MapModules/Previews/{spec["id"]}.png',structureHash=digest))
    (BASE/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,size=40,entries=entries),ensure_ascii=False,indent=2)+'\n')
    print(f'{len(maps)} layouts / {len(SKINS)} environment atlases / {len(keys)} tile identities')
    return maps

if __name__=='__main__':export()
