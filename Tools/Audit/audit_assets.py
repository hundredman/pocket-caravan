"""Read-only A3a evidence extraction. Findings are review candidates, never art verdicts."""
from pathlib import Path
from collections import defaultdict,Counter
import ast,argparse,json,sys
ROOT=Path(__file__).resolve().parents[2]
METHODS={'crop':'crop','resize':'resample','quantize':'palette_reduction','rotate':'rotate','transpose':'transpose','blend':'blend','recolor':'recolor','tone':'tone','stamp_clipped':'placement_helper'}
LIMITS=[
 '정적 후보는 위반 판정이 아니다. 온전한 개체의 렌더용 분할, 스프라이트 애니메이션과 UI 배율은 정상일 수 있다.',
 '실행 관찰은 등록 atlasRegions의 지형과 growth 이미지 생성에 한한다. 캐릭터/애니메이션은 visualObjects에 참조만 기록하며 이미지 생성을 실행하지 않는다. 미등록 맵/다른 씬/미사용 분기는 미실행이다.',
 'Python AST는 임의 별칭/데이터 흐름을 해석하지 않는다. C#은 줄/패턴 검색이라 주석이나 의도된 효과도 검출한다. 파이썬 검색 범위는 Tools/MapGen이며 별도 아트 제작 도구는 제외한다.',
 '원본 사각형 이름을 추출해도 경계/용도/그림체의 적합성은 화면 검토가 필요하다. woodland의 원본 사각형은 코드/실행 관찰과 대조한다.',
 '스탬프는 생성 후처리에서 교체될 수 있다. 기록된 이름을 최종 그림이라고 단정하지 말고 finalTileFamilies와 대조한다.',
 'rowRuns는 맵별 좌상단 JSON 좌표다. 스탬프도 같은 좌표이며 Unity 좌하단 위치도 함께 적는다. growth는 그림 셀이 아닌 개체 기준점이다.',
 '접촉/겹침 추출은 같은 이름의 스탬프끼리만 비교한다. 서로 다른 개체/스탬프 밖 수작업 배치의 접촉은 놓칠 수 있다. 나무 군집 등 정상 반복도 있어 사람이 판정해야 한다.',
 '사용자 결정에 따라 현재 아트를 유지한다. 이 감사는 색 변경/교체/삭제/팔레트 결정/맵 재생성을 허가하지 않는다.',
]

def category(node):
 if isinstance(node,ast.Attribute):return node.attr
 if isinstance(node,ast.Name):return node.id
 return ''

def scan_python(text,path):
 tree=ast.parse(text,filename=path);parents={c:p for p in ast.walk(tree) for c in ast.iter_child_nodes(p)};rows=[]
 for node in ast.walk(tree):
  if not isinstance(node,ast.Call):continue
  name=category(node.func)
  if name not in METHODS:continue
  owner=node
  while owner in parents and not isinstance(owner,(ast.FunctionDef,ast.AsyncFunctionDef)):owner=parents[owner]
  context=owner.name if isinstance(owner,(ast.FunctionDef,ast.AsyncFunctionDef)) else '<module>'
  expr=ast.get_source_segment(text,node) or ast.unparse(node)
  rows.append(dict(file=path,line=node.lineno,column=node.col_offset+1,endLine=node.end_lineno,operation=METHODS[name],function=context,expression=expr,review='candidate_only',coordinates=[ast.unparse(a) for a in node.args],limitations='No automatic determination of misuse'))
 return sorted(rows,key=lambda r:(r['line'],r['column']))

def scan_sources(root):
 rows=[]
 for p in sorted((root/'Tools/MapGen').rglob('*.py')):
  rows+=scan_python(p.read_text(),p.relative_to(root).as_posix())
 import re
 patterns={'sprite_create':r'Sprite\.Create\(', 'scale':r'\.localScale\s*=', 'sort_order':r'sortingOrder\s*=', 'render_layer':r'Layer\(gridGo,', 'pixel_read':r'GetPixels(?:32)?\('}
 for base in ['Assets/PocketCaravan/Editor','Assets/PocketCaravan/Scripts']:
  for p in sorted((root/base).rglob('*.cs')):
   for line,s in enumerate(p.read_text().splitlines(),1):
    for op,pattern in patterns.items():
     if re.search(pattern,s):rows.append(dict(file=p.relative_to(root).as_posix(),line=line,column=1,endLine=line,operation=op,function='C# line candidate',expression=s.strip(),review='candidate_only',coordinates=[],limitations='Regex candidate; no C# semantic analysis'))
 return sorted(rows,key=lambda r:(r['file'],r['line'],r['column'],r['operation']))

def raster_runs(coords):
 grouped=defaultdict(list)
 for x,y in coords:grouped[y].append(x)
 result=[]
 for y,xs in sorted(grouped.items()):
  xs=sorted(set(xs));start=last=xs[0]
  for x in xs[1:]:
   if x!=last+1:result.append([y,start,last]);start=x
   last=x
  result.append([y,start,last])
 return result

def adjacent_rectangles(rows):
 out=[]
 for i,a in enumerate(rows):
  for b in rows[i+1:]:
   if a['map']!=b['map'] or a['name']!=b['name']:continue
   x,y,w,h=a['rect'];xx,yy,ww,hh=b['rect']
   if max(x,xx)<=min(x+w,xx+ww) and max(y,yy)<=min(y+h,yy+hh):
    out.append(dict(map=a['map'],asset=a['name'],first=a['rect'],second=b['rect'],review='touching_or_overlap_candidate_not_automatic_misuse'))
 return out

class Observe:
 def __init__(self,root):self.root=root.resolve();self.events={};self.restore=[];self.active_key=None
 def record(self,kind,frame,params):
  while frame:
   p=Path(frame.f_code.co_filename)
   if p.is_relative_to(self.root/'Tools/MapGen'):
    context={k:v for k,v in frame.f_locals.items() if k in ('name','key','base','theme','mode','tx','ty','w','h') and isinstance(v,(str,int,float,bool))}
    row=dict(exportKey=self.active_key,operation=kind,file=p.relative_to(self.root).as_posix(),line=frame.f_lineno,function=frame.f_code.co_name,parameters=params,context=context)
    key=json.dumps(row,sort_keys=True);self.events.setdefault(key,dict(**row,count=0))['count']+=1;return
   frame=frame.f_back
 def __enter__(self):
  from PIL import Image
  for method in ('crop','resize','quantize','transpose','rotate'):
   original=getattr(Image.Image,method)
   def wrap(im,*args,_method=method,_original=original,**kwargs):
    self.record(_method,sys._getframe(1),dict(imageSize=list(im.size),arguments=[str(a) for a in args],keywords={k:str(v) for k,v in kwargs.items()}))
    return _original(im,*args,**kwargs)
   self.restore.append((Image.Image,method,original));setattr(Image.Image,method,wrap)
  import tiles,goryeo_art
  for module,name in ((tiles,'recolor'),(goryeo_art,'tone')):
   original=getattr(module,name)
   def wrap(*args,_original=original,_name=name,**kwargs):
    self.record(_name,sys._getframe(1),dict(arguments=[str(v) for v in args if isinstance(v,(str,int,float))]))
    return _original(*args,**kwargs)
   self.restore.append((module,name,original));setattr(module,name,wrap)
  return self
 def __exit__(self,*args):
  for obj,name,original in reversed(self.restore):setattr(obj,name,original)

def dynamic_inventory(root):
 sys.path.insert(0,str(root/'Tools/MapGen'))
 import world_registry,tiles,goryeo_art as G
 # Clear image caches so repeated audits observe the same source operations.
 for fn in (G.sprite,G.image_for,G.tone):fn.cache_clear()
 placements=[];bykey=defaultdict(lambda:defaultdict(list));usage=[];objects=[]
 with Observe(root) as observed:
  entries,maps=world_registry.atlas_content()
  for m in maps:
   for index,obj in enumerate(m.objects):
    if obj.get('character') or obj.get('sheet') or obj.get('kind')=='growth':
     objects.append(dict(map=m.id,index=index,kind=obj['kind'],id=obj.get('id',''),rect=[obj.get('x',0),obj.get('y',0),obj.get('w',1),obj.get('h',1)],character=obj.get('character'),sheet=obj.get('sheet'),frames=obj.get('frames')))
    if obj.get('kind')=='growth':
     bykey[obj['id']][(m.id,'growth')].append((obj['x'],obj['y']))
   for name,x,y in m.stamps:
    if name not in tiles.STAMPS:continue
    sheet,sx,sy,w,h,block,high,recolor=tiles.STAMPS[name]
    final=Counter()
    for yy in range(y,min(y+h,m.h)):
     for xx in range(x,min(x+w,m.w)):
      if xx<0 or yy<0:continue
      for layer in ('obj','high'):
       key=getattr(m,layer)[yy][xx]
       if key:final[key.rsplit(':',2)[0]]+=1
    source=dict(stage='authored_stamp_before_final_replacement',sheet=sheet,tileOrigin=[sx,sy],pixelRect=[sx*16,sy*16,(sx+w)*16,(sy+h)*16],recolor=recolor)
    if name.startswith('goryeo_') and name[7:] in G.CUSTOM:
     asset,sw,sh=G.CUSTOM[name[7:]];source.update(nativeAsset=asset,nativeCells=[sw,sh],pixelRect=list(G.RECTS.get(asset,G.DETAIL_RECTS.get(asset,[]))),note='Legacy atlas extraction and whole native object; inspect boundary/purpose visually')
    if sheet in tiles.SHEETS:source['sourceFile']='NA_PACK/'+tiles.SHEETS[sheet]
    if source.get('nativeAsset'):
     asset=source['nativeAsset']
     source['sourceFile']='Assets/PocketCaravan/Art/Goryeo/Source/'+('PineAncient.png' if asset=='woodland0' else 'Woodland.png' if asset.startswith('woodland') else 'CourtyardStone.png' if asset=='paving' else 'VillageDetails.png' if asset in G.DETAIL_RECTS else 'Architecture.png')
     if asset.startswith('woodland') and asset!='woodland0':
      i=int(asset[8:]);col,row=i%4,i//4
      source['pixelRect']=[G.WOODLAND_COLUMNS[col],355 if i==4 else G.WOODLAND_ROWS[row],G.WOODLAND_COLUMNS[col+1],G.WOODLAND_ROWS[row+1]]
     if asset in ('woodland0','paving'):source['pixelRect']='whole_source_image'
    placements.append(dict(map=m.id,name=name,rect=[x,y,w,h],unityBottomLeft=[x,m.h-y-h],blockFrom=block,aboveRows=high,source=source,finalTileFamilies=dict(sorted(final.items()))))
   for layer in ('ground','deco','obj','high'):
    for y,row in enumerate(getattr(m,layer)):
     for x,key in enumerate(row):
      if key:bykey[key][(m.id,layer)].append((x,y))
  for key in sorted(bykey):
   observed.active_key=key
   image=tiles.tile_image(key)  # exercise the actual current export path without writing images
   usage.append(dict(key=key,exportImage='Assets/PocketCaravan/Art/NinjaAdventure/Tiles/'+key.replace(':','_').replace('@','_')+'.png',size=list(image.size),uses=[dict(map=mid,layer=layer,count=len(coords),rowRuns=raster_runs(coords)) for (mid,layer),coords in sorted(bykey[key].items())]))
 events=sorted(observed.events.values(),key=lambda r:(r['file'],r['line'],r['operation'],json.dumps(r['context'],sort_keys=True),json.dumps(r['parameters'],sort_keys=True)))
 # Retired JSONs remain visible in the report, but are never counted as current runtime maps.
 registered={e['mapId'] for e in entries};retired=[]
 for p in sorted((root/'Assets/PocketCaravan/Maps').glob('*.json')):
  d=json.loads(p.read_text())
  if d.get('id') not in registered:retired.append(dict(id=d.get('id'),file=p.relative_to(root).as_posix(),runtimeRegistered=False))
 return dict(registeredMaps=[e['mapId'] for e in entries],placements=placements,visualObjects=objects,tileUsage=usage,operations=events,adjacentObjects=adjacent_rectangles(placements),unregisteredMaps=retired)

def generate(root,out):
 static=scan_sources(root);runtime=dynamic_inventory(root)
 report=dict(schemaVersion=1,purpose='A3a evidence for Claude A3b; no keep/replace/delete decisions',staticCandidates=static,runtime=runtime,limitations=LIMITS)
 out.mkdir(parents=True,exist_ok=True);(out/'inventory.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 counts=Counter(r['operation'] for r in static);hits=Counter(r['operation'] for r in runtime['operations'])
 lines=['# A3a 에셋 사용 감사 — 자동 추출 결과','','현재 환경 아트는 유지한다는 사용자 결정을 따른다. 아래는 잘못된 사용의 확정 판정이 아니라 A3b/C3a 검토 근거다. 본편을 바꾸지 않았다.','','## 범위와 수량','',f'- 등록 맵 {len(runtime["registeredMaps"])}개, 기록 스탬프 {len(runtime["placements"])}개, 실제 사용 타일 {len(runtime["tileUsage"])}종.',f'- 정적 후보 {len(static)}곳, 현재 생성/이미지 경로에서 관찰한 변형 호출 {sum(e["count"] for e in runtime["operations"])}회.',f'- 같은 개체의 접촉/겹침 후보 {len(runtime["adjacentObjects"])}쌍. 나무 군집 등 정상 사용도 포함한다.','', '## 우선 확인할 항목','', '- goryeo_art.sprite: 고해상도 시트 crop, 크기 맞춤/양자화, image_for의 렌더용16px 분할을 각각 구분한다. 현재 그림을 유지해도 원본/등록 방식은 판정이 필요하다.', '- tiles.tile_image/recolor, goryeo_art.tone: 즉석 색 변환 경로. 실제 사용은 inventory.json의 operations/context/key와 tileUsage로 대조한다.', '- tiles.FIELD/field_rect: 언덕 계열 조각을 밭으로 사용하는 목적 적합성. 단순 crop 검출만으로 위반이라고 확정하지 않는다.', '- 연못/호수: goryeo_lotus 및 water 키의 최종 rowRuns/스탬프 접촉과 화면을 함께 확인한다. 물 타일과 장식 개체를 같은 용도로 취급하지 않는다.', '- C3a: CUSTOM woodland의 aboveRows=0 및 고정 렌더층 점검. 이전 숲 화면은 진입불가칸 강제 배치였으며 정상 이동 가림 오류로 확정하지 않는다.', '- Decisions-2026-10-25 2절에 따라 기존 아트는 유지하고 C6에서 같은 모습으로 굽고 등록한다. 이 도구는 삭제/추가 아트 승인을 하지 않는다.','','## 정적 위치 목록','','| 파일:줄 | 종류 | 함수 | 표현 |','|---|---|---|---|']
 for r in static:
  expression=r['expression'].replace('|','\\|').replace('\n',' ');lines.append(f'| `{r["file"]}:{r["line"]}` | {r["operation"]} | {r["function"]} | `{expression[:220]}` |')
 lines+=['','## 우선 검토 대상의 실제 사용 예','','모든 위치는 inventory.json의 rowRuns에서 확인한다. 아래 좌표는 사용 예 한 곳이며 위반 판정이 아니다.','','| 분류 | 사용 키 수 | 예시 키 | 맵·층·첫 좌표 |','|---|---:|---|---|']
 for title,predicate in [('고려 원본 추출',lambda k:k.startswith(('goryeo:','goryeo_'))),('즉석 색 변환',lambda k:'@' in k),('물 조립 타일',lambda k:k.startswith('water:')),('연꽃 개체',lambda k:k.startswith('goryeo_lotus:')),('성장 소품',lambda k:False)]:
  selected=[r for r in runtime['tileUsage'] if predicate(r['key']) or (title=='성장 소품' and any(u['layer']=='growth' for u in r['uses']))]
  if not selected:continue
  r=selected[0];u=r['uses'][0];y,x,_=u['rowRuns'][0]
  lines.append(f'| {title} | {len(selected)} | `{r["key"]}` | {u["map"]} / {u["layer"]} / ({x}, {y}) |')
 lines+=['','## 자동 검출 한계','']+['- '+s for s in LIMITS]+['','## 원본 데이터 읽는 법','','`inventory.json`의 staticCandidates는 파일/줄/열/식 좌표, runtime.operations의 exportKey는 관찰을 유발한 타일 키이며 캐시 공유 원본의 모든 의존 키를 뜻하지 않는다. 실제 crop 사각형/해상도/호출 문맥을 담는다. placements는 원래 스탬프의 최종 맵 좌표 사각형과 그 영역의 실제 타일 계열(원래 개체가 교체/일부 덮임일 수도 있음)이며 tileUsage.uses.rowRuns의 각 항목은 `[y, x시작, x끝]`이다. 생성 중 단계 좌표와 최종 배치를 혼동하지 않는다. unregisteredMaps는 본편 실행 목록에서 제외한다.','','Unity 씬/이동/화면 검사는 실행하지 않았다. 그림체·잘린 경계·이음매/다른 용도 사용 여부는 Claude A3b와 사용자 화면 판단이 필요하다.']
 (out/'README.md').write_text('\n'.join(lines)+'\n')
 return dict(maps=len(runtime['registeredMaps']),staticCandidates=len(static),placements=len(runtime['placements']),tileKinds=len(runtime['tileUsage']),observedCalls=sum(e['count'] for e in runtime['operations']),adjacentPairs=len(runtime['adjacentObjects']),staticOperations=dict(counts),runtimeOperations=dict(hits))

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--out',type=Path,default=ROOT/'Docs/Validation/A3a-2026-10-25');args=parser.parse_args();print(json.dumps(generate(ROOT,args.out),ensure_ascii=False))
