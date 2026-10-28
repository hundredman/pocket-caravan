# M3 마을 건물·소품 1차 (검토용)

M2 구역도의 건물 자리에 승인된 C7 그림을 놓고, 광장·마당·골목을 기존 흙길 조립 타일로 칠했다. 은샘 "승인 없이 모두 진행" 지시(2026-10-28)로 진행했다. 건물 그림은 나중에 바뀌어도 자리는 그대로 쓴다.

- 원본: `Tools/WorldLayout/m1_tiles_preview.py --zones` (M1② 그림 + M2 `zones.json`), 모음 `Tools/WorldLayout/m3_sheet.py`
- 결과: `world-full.png`, `world.png`, `<마을>.png`(1:1), `<마을>-labels.png`(이름 표시), `cam-<마을>.png`(게임 크기 2배), `sheet.png`(11곳 게임 크기 1배), `collision.json`, `report.json`
- 쓴 그림: cottage, thatch(v5), tile-house(v6), shop, gaekju, palace, pavilion, small_store, paper_large, paper_rack, celadon, harvest, well, stone_lantern, bench, boat, laundry. 그대로 놓기만 했다(자르기·변형 없음).
- 충돌: 건물은 지붕 맨 윗줄을 뺀 아래, 1~2칸 소품은 밑동 한 줄, 배는 물 위라 없음(`collision.json`).
- 나무: 건물·광장·골목 자리와 그 아래 3칸(나무 그림이 위로 올라오는 높이)에는 나무 밑동을 두지 않는다. 마을 밖 숲·산은 M1②와 같다.
- 흙길: 2×2 규칙을 지키게 맞췄다(조각 어긋남 0). 물 어긋남 4칸은 M1②부터 있던 것.
- 흙길 다듬기(은샘 2026-10-28): 큰길과 끊어진 광장·마당·선착장은 2칸 폭으로 잇고, 이어지지 않는 조각과 8칸 이하 막다른 돌기는 풀밭으로. 흙길에 갇힌 풀밭(60칸 이하)·거의 갇힌 풀밭(둘레 85% 이상, 40칸 이하)·1~2칸 좁은 틈은 흙길로. 우물·석등 같은 작은 소품은 흙길 위에 선다. 2칸 폭 골목이 나지 않는 건물 자리는 M2에서 고르지 않는다(닥목 집 3 빠짐). 전후 비교 `dirt-before-after-*.png`.
- 정적 검사(`report.json`의 `village`): 모든 문·게시판·NPC·시작 위치·성장 자리·소품 앞 칸이 충돌·물에 막히지 않고 길에서 걸어서 닿음(막힘 0).
- 아직 아님: 장터 노점·상인·게시판 그림·NPC(M4), 주민·꾸밈(M5), Unity 씬 확인(M4). 사람이 걸어 본 확인 없음.
