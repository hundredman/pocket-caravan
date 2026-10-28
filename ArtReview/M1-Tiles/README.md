# M1② 지형 타일 검토 — 기존 타일·개체만 (2026-10-28, Claude)

Codex의 새 지형 타일 14종은 은샘 지시로 모두 폐기했다(기존 타일과 느낌이 다르고 지형 경계가 부자연스러움). 이 검토는 새 타일 없이 기존 것만 쓴다.

재현: `NA_PACK=<Ninja Adventure - Asset Pack 경로> python Tools/WorldLayout/m1_tiles_preview.py [--no forest,mountain,hill,marsh,field,meadow,grass]`
원본 지형은 승인된 `terrain_v3.py --roads --variant small-hamlets`를 그대로 실행해 쓴다.

- 바닥은 세 가지 조립 타일만: 풀밭, 물(바다·강·호수), 흙길. 다리는 기존 판자. 색은 고려 맵의 goryeo_jade.
- 기존 조각만으로 그려지게 물·길 모양을 다듬는다: 모든 칸이 2×2 덩어리에 속하고, 두 옆 칸이 같으면 대각선도 같게. 이 때문에 가는 개울이 2칸으로 넓어진다(물 +148칸, 길 +191칸). 남은 어긋난 칸: 물 4, 길 2.
- 숲·산·언덕·습지·밭·꽃밭은 바닥 경계를 만들지 않고 개체로 표현한다. 개체는 팀 그림체로 다시 그려 승인된 C7 그림만 쓴다(`ArtReview/Approved/C7-*`, `ArtReview/C7-RemainingBatch-20261027/Native`): 산=소나무 무리(pine_west/east)·바위(moss_rock, small_rock)·굽은 나무, 숲=forest_round/bent/wide·broadleaf·light_tree·round_west/east·golden_tree·쓰러진 고목·덤불, 언덕=덤불·작은 바위·드문 나무, 습지=tall_grass, 호숫가=lotus, 풀밭=외딴 나무(broadleaf·willow 등). 들판의 논(3×3)과 풀포기·꽃은 기존 바닥 소품 타일이다.
- 모래 해변은 넣지 않았다(나루 등 지금 고려 맵처럼 풀밭이 바로 물에 닿음).
- 흙길 조각 9개(floor 2:7, 0:8, 2:8, 0:9, 1:9, 2:9, 6:8, 5:9, 6:9)는 goryeo_jade로 구워진 것이 없어, 기존 jade 굽기의 색 대응표(원본 30색 → jade 30색, 겹침 없음)를 그대로 적용해 검토용으로만 구웠다. 본편에 쓰려면 등록·승인이 필요하다. 해시는 report.json.
- 마을 범위·건물·출입구·NPC는 없다. Unity 씬·실제 이동은 하지 않았다(그림만).

파일: `world.png`(1/4 축소), `world-full.png`(실제 크기 16px/칸), `cam-*.png`(480×270을 2배), `cams-sheet.png`(8곳 모음), `report.json`.
