# 제작과 프롬프트 기록

## 방식과 선택

- `imagegen` 스킬의 기본 내장 도구 `image_gen` 사용. CLI/API fallback은 사용하지 않았다.
- 입력: 현재 게임의 house-before.png, tree-before.png, elder_tall_d0.png, villager_rabbit_tall_d0.png, trader_tall_d0.png. 모두 편집 대상으로 먼저 확인했다.
- AI 결과는 기존 집의 구조 변화 또는 캐릭터의 네이티브 격자 미준수로 미채택했다. 고해상도 AI 그림을 최종 게임용 그림으로 표시하지 않았다.
- 최종 환경: 원본 PNG → Aseprite `gentle_cleanup.lua`의 국소 픽셀 보정 → PNG + Aseprite 원본. 최종 NPC: 기존 프로젝트의 네이티브 캐릭터 렌더러 기반 직접 도트 수정 → Aseprite 원본. 비교판의 확대는 최근접이며 게임용 크기를 바꾸지 않는다.

## 내장 image_gen 요청 프롬프트 세트

환경 2개는 각각 별도 호출, transparent_background=true. 공통 요청:

> Use case: precise-object-edit. This is a restoration-level MICRO edit, not a redesign. The user likes this original much more than a flat simplified redraw. Preserve essentially ALL of its original silhouette, pose, proportions, layout, detail, color richness, palette feel, dark outlines, lighting, material character and shading. Only gently clean ambiguous blended edge pixels and a very small amount of isolated noisy speckling so it reads slightly more clearly as handmade pixel art. Preserve 90-95% of the existing visual information. Do NOT turn planes into big solid-color blocks. Do NOT flatten the shading or cap the palette at 8 or 12 colors. Do not add anything or move any component. Original logical canvas is 64 by 48 pixels. Render the exact same logical canvas and placement as crisp 64x48 pixel art, shown at integer enlargement on a 1024x768 transparent canvas (16 output pixels per logical pixel); keep all pixel edges perfectly hard and on that regular grid, no anti-aliasing, no interpolation, no softening. Transparent outside the existing object. No text, no border, no drop shadow added.

각 대상별 보존 요청:
- house: the Korean tiled-roof house. Keep every roof ridge, window, beam, stone and step.
- tree: the crooked mature pine tree. Keep every distinct bough, leaf cluster, crooked trunk and exposed root.

NPC 3개도 각각 별도 호출, transparent_background=true. 공통 요청:

> Use case: precise-object-edit. Edit target: attached original tiny chibi game sprite. User explicitly rejected our flat, simplified, rigid/blocky human NPC redraw and prefers the rich, soft-cute ORIGINAL attachment. Keep that exact original sprite aesthetic, detail density, tiny warm shaded clusters, large round head, short body, shiny eyes, colored soft-dark outlines, cheek expressions and original clothing material/shading. Human, no animal ears or muzzle or tail. This is a careful adaptation of the ORIGINAL, not a flat redesign. Preserve the original large head/short body proportions and existing level of detail; do not replace shading with broad solid polygons or a rectangular face. Front view only, one complete character. Original logical canvas is 16x24 pixels, feet on bottom row23, head body silhouette approximately the same size as reference, all props fit inside that canvas. Render as crisp handcrafted 16x24 logical pixel art shown at integer enlargement on a 1024x1536 transparent canvas (64 pixels per logical pixel), with a hard exact regular grid, no anti-aliasing or extra subpixel details. Retain the nuanced original colors and highlights; no extreme palette reduction. Transparent background, no text, no borders, no ground plane or added shadow.

개별 인물 지시:
- chief: a friendly elderly HUMAN Korean village chief, with the same dark gat hat, a rounded human face with big expressive shiny eyes and pink cheeks, white eyebrows and a short fluffy white beard; keep the original robe and soft blue-lavender sash, add a thin wooden cane at the side.
- woman: a friendly HUMAN Korean village woman, round human face with the same large lively shiny eyes and blush, dark hair in a traditional bun instead of rabbit ears, retain her pink jacket and dark skirt with the original shading; a small brown water jar above her head in the original transparent top margin.
- trader: a friendly HUMAN Korean traveling merchant, a rounded human face with the original large lively eyes and blush, a small moustache, woven straw hat replacing the dark animal hat, retain the weathered brown clothing with its original shading, an asymmetric green-brown wooden pack behind his shoulder.

## 재실행

원본/출력 경로를 분리해서 Aseprite에 전달한다. strength=micro 또는 gentle.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b \
  --script-param "source=$PWD/ArtReview/C2-C3/house-before.png" \
  --script-param "output=$PWD/ArtReview/Refine-v2/house-micro" \
  --script-param strength=micro --script Tools/ArtTrials/gentle_cleanup.lua
python Tools/ArtTrials/native_npc_refine.py
```
