# 북유럽 N3 건물·소품 그림 (확정 2026-10-29)

방식(사용자 결정): 지금 북유럽 그림을 먼저 쓰고, 없는 것만 코드로 새로 그림. 얄의 장원은 더 크게 새로 그림.

- 짝: `Tools/MapGen/northworld/n2_zones.py`의 `ASSET`(칸 크기), `ART`(그림 이름)
  - 이름 그대로 = `north_tiles.py`/`tiles.py`의 STAMPS (에셋 팩, north_art)
  - `Minigame:<이름>` = `Resources/North/Minigame/` 그림 (좌판, 대구 덕장, 털옷 작업 마당)
  - `new:<이름>` = 이 폴더 `art/`의 새 그림
- 새 그림(코드로 그림, `Tools/MapGen/northworld/n3_build_art.py`, 에셋 팩 색·외곽선): 고른 것 = A
  - 대장간 4×4 `smithy_A` (앞이 트인 판자 지붕, 화덕·모루)
  - 숯가마 3×3 `kiln_A` (흙 무덤 숯가마 + 장작)
  - 동굴 입구 5×3 `cave_A` (회색 바위 + 고드름)
  - 얄의 장원 8×6 `jarlhall_A` (긴 초가 롱하우스 + 용머리 + 방패 넷)
  - B 후보와 비교판(`art/candidates.png`)은 기록으로 남겨 둠
- 미리보기: `python3 Tools/MapGen/northworld/n3_preview.py` → 맵별 PNG, sheet_*.png (바닥은 N1 지형 색, 타일은 N4)
- N3 중 N1 수정: 폭 2칸 이하 맵 띠는 옆 맵으로(라우가르 북동 1칸 띠)
- 남은 점: 넓은 빈 땅은 N4에서 타일·나무·바위·소품으로 채움. 산 줄무늬는 N4 절벽 타일에서 다듬음.
