# A7 원화 제작 기록

내장 image_gen 사용, transparent_background=true. 새 PNG2개는 Raw에 복사해 보존했다. 기존 승인 그림을 수정하지 않았다. 실제 격자·크기·색 정리는 Tools/Atmosphere/goryeo_life_native.lua(Aseprite)로 수행했다. 두 셀은 처음부터 같은 동물의 두 동작으로 요청한 원화이며 새 원화에서 프레임을 추출했다. Native에는32×32 프레임4개와2프레임 ASE2개가 있다. 런타임 크기/색 변형 없음. 최종 검토는 원화가 아닌 Unity480×270 캡처다.

## chicken

Use case: stylized-concept. Asset type: production pixel-art game animation spritesheet, one white village hen. Create ONLY a perfectly crisp 64 by 32 logical-pixel sprite sheet, displayed enlarged with nearest-neighbor pixels. Two equal 32x32 cells side by side: frame 1 standing hen facing right in a 3/4 overhead RPG view; frame 2 the SAME hen pecking the ground, body and feet anchored at the same baseline. The hen silhouette fits within about 14x14 logical pixels centered in each cell; this is a tiny yard animal next to a 16x24 player, NOT a human or anthropomorphic character. Warm off-white round body, red comb and wattle, mustard beak/feet, one black eye, a single warm-grey shade, dark brown 1-pixel outline. Simple large readable pixel clusters, max 8 opaque colors. Genuine transparent background, binary alpha, no ground, no shadows, no text, no grid lines, no gradients, no noise, no anti-aliasing. Exactly two animation frames of one hen, no other objects.

## dog

Use case: stylized-concept. Asset type: tiny pixel-art game animation spritesheet, one cute Korean village dog. ONLY a 64x32 logical pixel sprite sheet enlarged with nearest-neighbor hard-edged square pixels, two equal 32x32 cells. Frame 1 at left: compact cream-and-tan Jindo-like small village dog standing on all four legs, facing right, three-quarter overhead RPG view. Frame 2 at right: the very same dog, same size and feet baseline, with head lowered sniffing, tail slightly wagged. Whole dog silhouette in each cell about 22x16 logical pixels including tail; natural animal, NOT a human character, no clothes/collar. Clear four feet, short muzzle, upright triangular ears, curled tail, single black dot eye, dark-brown single-pixel outline, warm ivory fur, ochre shadow, one lighter cream color. Max 7 solid opaque colors, simple chunky readable clusters, consistent upper-left light. Genuine transparent background, binary alpha, no scene, no shadow, no text/grid, no gradients/noise/antialiasing. Exactly two frames of this one dog, no other objects.

## 기존 에셋 재사용

source.json assets에 원본 경로를 기록했다. 베네치아 puff는 연기, pigeon은 논 위 새, gull은 물가 갈매기로 같은 목적에 온전하게 재사용한다. boat는 승인 건물·배 리뉴얼 원본, cart는 사용자가 복원을 승인한 최초 수레, lamp는 현재 승인 석등 원본이다. NPC는 본편 C1 역할별 사람 상인16프레임을 그대로 읽는다. 다른 지역의 그림·코드·데이터는 수정하지 않는다.
