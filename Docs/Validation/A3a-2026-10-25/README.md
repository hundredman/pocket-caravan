# A3a 에셋 사용 감사 — 자동 추출 결과

현재 환경 아트는 유지한다는 사용자 결정을 따른다. 아래는 잘못된 사용의 확정 판정이 아니라 A3b/C3a 검토 근거다. 본편을 바꾸지 않았다.

## 범위와 수량

- 등록 맵 11개, 기록 스탬프 634개, 실제 사용 타일 654종.
- 정적 후보 65곳, 현재 생성/이미지 경로에서 관찰한 변형 호출 875회.
- 같은 개체의 접촉/겹침 후보 54쌍. 나무 군집 등 정상 사용도 포함한다.

## 우선 확인할 항목

- goryeo_art.sprite: 고해상도 시트 crop, 크기 맞춤/양자화, image_for의 렌더용16px 분할을 각각 구분한다. 현재 그림을 유지해도 원본/등록 방식은 판정이 필요하다.
- tiles.tile_image/recolor, goryeo_art.tone: 즉석 색 변환 경로. 실제 사용은 inventory.json의 operations/context/key와 tileUsage로 대조한다.
- tiles.FIELD/field_rect: 언덕 계열 조각을 밭으로 사용하는 목적 적합성. 단순 crop 검출만으로 위반이라고 확정하지 않는다.
- 연못/호수: goryeo_lotus 및 water 키의 최종 rowRuns/스탬프 접촉과 화면을 함께 확인한다. 물 타일과 장식 개체를 같은 용도로 취급하지 않는다.
- C3a: CUSTOM woodland의 aboveRows=0 및 고정 렌더층 점검. 이전 숲 화면은 진입불가칸 강제 배치였으며 정상 이동 가림 오류로 확정하지 않는다.
- Decisions-2026-10-25 2절에 따라 기존 아트는 유지하고 C6에서 같은 모습으로 굽고 등록한다. 이 도구는 삭제/추가 아트 승인을 하지 않는다.

## 정적 위치 목록

| 파일:줄 | 종류 | 함수 | 표현 |
|---|---|---|---|
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:228` | render_layer | C# line candidate | `var ground = Layer(gridGo, "Ground", 0);` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:229` | render_layer | C# line candidate | `var deco = Layer(gridGo, "Deco", 1);` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:230` | render_layer | C# line candidate | `var obj = Layer(gridGo, "Objects", 5);` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:231` | render_layer | C# line candidate | `var high = Layer(gridGo, "Above", 20);   // 나무 윗부분: 플레이어 위로 그림` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:232` | render_layer | C# line candidate | `var blocked = Layer(gridGo, "Blocked", 0, renderer: false);` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:295` | sort_order | C# line candidate | `mk.sortingOrder = 25;` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:324` | sort_order | C# line candidate | `asr.sortingOrder = (o.sheet == "ripples" \|\| o.sheet == "goryeo_ripples") ? 1 : 4;   // 물결은 바닥 바로 위, 꽃은 소품과 같은 높이` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:342` | sort_order | C# line candidate | `alert.sortingOrder = 25; alert.enabled = false;` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:366` | sort_order | C# line candidate | `bsr.sprite = esr.sprite; bsr.sortingOrder = esr.sortingOrder;` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:382` | sort_order | C# line candidate | `gsr.sortingOrder = 5;` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:447` | sort_order | C# line candidate | `sr.sortingOrder = 11;` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:533` | sort_order | C# line candidate | `if (renderer) go.AddComponent<TilemapRenderer>().sortingOrder = order;` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:546` | sort_order | C# line candidate | `sr.sortingOrder = 10;` |
| `Assets/PocketCaravan/Editor/MapJsonBuilder.cs:562` | sort_order | C# line candidate | `sr.sortingOrder = 9;` |
| `Assets/PocketCaravan/Scripts/MapModules/MapAppearance.cs:72` | sprite_create | C# line candidate | `var sprite=Sprite.Create(environment.atlas,new Rect(x,y,size,size),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);` |
| `Assets/PocketCaravan/Scripts/MapModules/WeatherOverlay.cs:19` | sprite_create | C# line candidate | `sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);` |
| `Assets/PocketCaravan/Scripts/MapModules/WeatherOverlay.cs:23` | sort_order | C# line candidate | `var go=new GameObject(label);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;` |
| `Assets/PocketCaravan/Scripts/MapModules/WeatherOverlay.cs:33` | scale | C# line candidate | `if(skin!=null){r.color=skin.particleColor;r.transform.localScale=new Vector3(skin.size.x,skin.size.y,1);}` |
| `Assets/PocketCaravan/Scripts/MapModules/WeatherOverlay.cs:45` | scale | C# line candidate | `haze.transform.position=center;haze.transform.localScale=new Vector3(w,h,1);` |
| `Assets/PocketCaravan/Scripts/Obstacles/ObstacleField.cs:56` | sort_order | C# line candidate | `s.renderer=go.AddComponent<SpriteRenderer>();s.renderer.sortingOrder=12;` |
| `Assets/PocketCaravan/Scripts/Obstacles/ObstacleField.cs:57` | sprite_create | C# line candidate | `if(footprintSprite==null)footprintSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);` |
| `Assets/PocketCaravan/Scripts/Obstacles/ObstacleField.cs:58` | scale | C# line candidate | `var areaVisual=new GameObject("Declared area");areaVisual.transform.SetParent(visualRoot,false);areaVisual.transform.position=s.home;areaVisual.transform.localScale=new Vector3(rect.width,rect.height,1);` |
| `Assets/PocketCaravan/Scripts/Obstacles/ObstacleField.cs:59` | sort_order | C# line candidate | `s.footprint=areaVisual.AddComponent<SpriteRenderer>();s.footprint.sprite=footprintSprite;s.footprint.sortingOrder=3;` |
| `Assets/PocketCaravan/Scripts/Obstacles/ObstacleField.cs:154` | scale | C# line candidate | `s.renderer.transform.localScale=new Vector3(w/Mathf.Max(.01f,size.x),h/Mathf.Max(.01f,size.y),1);` |
| `Assets/PocketCaravan/Scripts/Player/CarryVisual.cs:34` | scale | C# line candidate | `crates[i].transform.localScale = Vector3.one * .42f;` |
| `Assets/PocketCaravan/Scripts/Player/CarryVisual.cs:59` | sort_order | C# line candidate | `cartSprite.sortingOrder = order;` |
| `Assets/PocketCaravan/Scripts/Player/CarryVisual.cs:63` | sort_order | C# line candidate | `crates[i].sortingOrder = order + 1;` |
| `Assets/PocketCaravan/Scripts/UI/AmbientFX.cs:48` | scale | C# line candidate | `b.t.localScale = new Vector3(Mathf.Abs(flip) < 0.3f ? 0.5f : 1f, 1f, 1f);` |
| `Assets/PocketCaravan/Scripts/UI/AmbientFX.cs:88` | scale | C# line candidate | `b.t.localScale = Vector3.one * Random.Range(1f, 1.6f);` |
| `Assets/PocketCaravan/Scripts/UI/AmbientFX.cs:100` | sort_order | C# line candidate | `sr.sortingOrder = order;` |
| `Assets/PocketCaravan/Scripts/UI/AmbientFX.cs:111` | sprite_create | C# line candidate | `return Sprite.Create(t, new Rect(0, 0, 3, 2), new Vector2(0.5f, 0.5f), 16f);` |
| `Assets/PocketCaravan/Scripts/UI/AmbientFX.cs:137` | sprite_create | C# line candidate | `return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);` |
| `Assets/PocketCaravan/Scripts/UI/WorldMapUI.cs:48` | pixel_read | C# line candidate | `var colors=source.GetPixels32();` |
| `Assets/PocketCaravan/Scripts/World/DroppedCargo.cs:37` | sort_order | C# line candidate | `d.sr.sortingOrder = 9;` |
| `Assets/PocketCaravan/Scripts/World/DroppedCargo.cs:44` | scale | C# line candidate | `if (sr != null) sr.transform.localScale = Vector3.one * (1f + Mathf.Floor(Mathf.Sin(t * 4f) * 1.5f + 1.5f) * 0.04f);` |
| `Assets/PocketCaravan/Scripts/World/WorkingNpc.cs:24` | scale | C# line candidate | `bundle.transform.localScale = Vector3.one * .45f;` |
| `Assets/PocketCaravan/Scripts/World/WorkingNpc.cs:26` | sort_order | C# line candidate | `bundle.sortingOrder = 12;` |
| `Tools/MapGen/Modules/export_library.py:66` | recolor | export | `recolor(im,k,skin)` |
| `Tools/MapGen/Modules/export_library.py:82` | resample | export | `im.resize((320,320),Image.Resampling.NEAREST)` |
| `Tools/MapGen/Modules/render_gallery.py:12` | resample | render | `Image.open(ROOT/e['previewPath']).resize((192,192),Image.Resampling.NEAREST)` |
| `Tools/MapGen/Obstacles/generate.py:130` | resample | generate | `art(d['visual']).resize((112,72),Image.Resampling.NEAREST)` |
| `Tools/MapGen/Obstacles/generate.py:130` | resample | generate | `art(d['visual']).resize((112,72),Image.Resampling.NEAREST)` |
| `Tools/MapGen/asset_manifest.py:122` | crop | shape | `full_image.crop((dx * cw, dy * ch, (dx + 1) * cw, (dy + 1) * ch))` |
| `Tools/MapGen/goryeo_art.py:37` | crop | sprite | `atlas.crop((WOODLAND_COLUMNS[col],355 if i==4 else WOODLAND_ROWS[row],WOODLAND_COLUMNS[col+1],WOODLAND_ROWS[row+1]))` |
| `Tools/MapGen/goryeo_art.py:42` | crop | sprite | `im.crop(bounds)` |
| `Tools/MapGen/goryeo_art.py:47` | palette_reduction | sprite | `im.convert('RGB').quantize(colors=18)` |
| `Tools/MapGen/goryeo_art.py:51` | crop | sprite | `atlas.crop(DETAIL_RECTS[name] if name in DETAIL_RECTS else RECTS[name])` |
| `Tools/MapGen/goryeo_art.py:53` | resample | sprite | `im.resize((w * 16, h * 16), Image.Resampling.LANCZOS)` |
| `Tools/MapGen/goryeo_art.py:62` | palette_reduction | sprite | `rgb.quantize(colors=12 if name=='paving' else 28, method=Image.Quantize.MEDIANCUT)` |
| `Tools/MapGen/goryeo_art.py:72` | crop | image_for | `sprite(name, int(sw), int(sh)).crop((int(tx)*16, int(ty)*16, (int(tx)+1)*16, (int(ty)+1)*16))` |
| `Tools/MapGen/module_art.py:22` | crop | image_for | `t.crop((tx*16,ty*16,(tx+1)*16,(ty+1)*16))` |
| `Tools/MapGen/tiles.py:55` | tone | tile_image | `tone(base, theme)` |
| `Tools/MapGen/tiles.py:78` | crop | tile_image | `t.crop((x, y, x + 16, y + 16))` |
| `Tools/MapGen/tiles.py:90` | transpose | tile_image | `t.transpose(Image.Transpose.ROTATE_270)` |
| `Tools/MapGen/tiles.py:92` | transpose | tile_image | `t.transpose(Image.Transpose.ROTATE_90)` |
| `Tools/MapGen/tiles.py:94` | transpose | tile_image | `t.transpose(Image.Transpose.FLIP_LEFT_RIGHT)` |
| `Tools/MapGen/tiles.py:101` | blend | tile_image | `Image.blend(original,skin_recolor(original,base,next(s for s in SKINS if s[0]==skin_id)),int(amount)/100)` |
| `Tools/MapGen/tiles.py:108` | crop | tile_image | `sheet(s).crop((x * 16, y * 16, x * 16 + 16, y * 16 + 16))` |
| `Tools/MapGen/tiles.py:117` | transpose | tile_image | `t.transpose(Image.Transpose.ROTATE_90)` |
| `Tools/MapGen/tiles.py:119` | recolor | tile_image | `recolor(t.copy(), rc)` |
| `Tools/MapGen/tiles.py:429` | placement_helper | forest_border | `self.stamp_clipped(self.rng.choice(big), x + self.rng.randint(0, 1), y)` |
| `Tools/MapGen/tiles.py:434` | placement_helper | forest_border | `self.stamp_clipped(self.rng.choice(kinds), x, y)` |
| `Tools/MapGen/tiles.py:638` | resample | render | `img.resize((img.width * scale, img.height * scale), Image.NEAREST)` |
| `Tools/MapGen/validate.py:37` | placement_helper | validate | `probe.stamp_clipped("tree", 1, 1)` |
| `Tools/MapGen/validate.py:39` | placement_helper | validate | `probe.stamp_clipped("tree", -1, 1)` |

## 우선 검토 대상의 실제 사용 예

모든 위치는 inventory.json의 rowRuns에서 확인한다. 아래 좌표는 사용 예 한 곳이며 위반 판정이 아니다.

| 분류 | 사용 키 수 | 예시 키 | 맵·층·첫 좌표 |
|---|---:|---|---|
| 고려 원본 추출 | 363 | `goryeo:house:4:3:0:0` | Dakmok / obj / (7, 29) |
| 즉석 색 변환 | 156 | `boat:0:0@goryeo_jade` | Naru / obj / (18, 26) |
| 물 조립 타일 | 54 | `water:0:12@wood@goryeo_jade` | Naru / ground / (31, 23) |
| 연꽃 개체 | 2 | `goryeo_lotus:0:0` | Gaegyeong / deco / (25, 20) |
| 성장 소품 | 4 | `element:0:1` | Naru / growth / (20, 16) |

## 자동 검출 한계

- 정적 후보는 위반 판정이 아니다. 온전한 개체의 렌더용 분할, 스프라이트 애니메이션과 UI 배율은 정상일 수 있다.
- 실행 관찰은 등록 atlasRegions의 지형과 growth 이미지 생성에 한한다. 캐릭터/애니메이션은 visualObjects에 참조만 기록하며 이미지 생성을 실행하지 않는다. 미등록 맵/다른 씬/미사용 분기는 미실행이다.
- Python AST는 임의 별칭/데이터 흐름을 해석하지 않는다. C#은 줄/패턴 검색이라 주석이나 의도된 효과도 검출한다. 파이썬 검색 범위는 Tools/MapGen이며 별도 아트 제작 도구는 제외한다.
- 원본 사각형 이름을 추출해도 경계/용도/그림체의 적합성은 화면 검토가 필요하다. woodland의 원본 사각형은 코드/실행 관찰과 대조한다.
- 스탬프는 생성 후처리에서 교체될 수 있다. 기록된 이름을 최종 그림이라고 단정하지 말고 finalTileFamilies와 대조한다.
- rowRuns는 맵별 좌상단 JSON 좌표다. 스탬프도 같은 좌표이며 Unity 좌하단 위치도 함께 적는다. growth는 그림 셀이 아닌 개체 기준점이다.
- 접촉/겹침 추출은 같은 이름의 스탬프끼리만 비교한다. 서로 다른 개체/스탬프 밖 수작업 배치의 접촉은 놓칠 수 있다. 나무 군집 등 정상 반복도 있어 사람이 판정해야 한다.
- 사용자 결정에 따라 현재 아트를 유지한다. 이 감사는 색 변경/교체/삭제/팔레트 결정/맵 재생성을 허가하지 않는다.

## 원본 데이터 읽는 법

`inventory.json`의 staticCandidates는 파일/줄/열/식 좌표, runtime.operations의 exportKey는 관찰을 유발한 타일 키이며 캐시 공유 원본의 모든 의존 키를 뜻하지 않는다. 실제 crop 사각형/해상도/호출 문맥을 담는다. placements는 원래 스탬프의 최종 맵 좌표 사각형과 그 영역의 실제 타일 계열(원래 개체가 교체/일부 덮임일 수도 있음)이며 tileUsage.uses.rowRuns의 각 항목은 `[y, x시작, x끝]`이다. 생성 중 단계 좌표와 최종 배치를 혼동하지 않는다. unregisteredMaps는 본편 실행 목록에서 제외한다.

Unity 씬/이동/화면 검사는 실행하지 않았다. 그림체·잘린 경계·이음매/다른 용도 사용 여부는 Claude A3b와 사용자 화면 판단이 필요하다.
