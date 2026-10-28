# M2 마을 구역도 (검토용)

M1② 지형(ArtReview/M1-Tiles) 위에 마을 기능 자리를 잡은 안이다. 건물 그림은 M3에서 승인된 C7 에셋으로 넣는다.

- 원본: `Tools/WorldLayout/m2_zones.py` (위치는 직접 잡고, 스크립트는 겹침·물·길·마을 범위만 검사)
- 나루: `naru.json`, `naru.png`(전체), `naru-half.png`, `naru-zoom.png`(마을 부분 1:1)
- 색: 노랑 광장·마당, 주황 건물 자리, 파랑 물가, 흰색 시설 점, 분홍 NPC, 초록 성장 자리, 갈색 골목
- 건물 자리에 걸친 나무는 M3에서 치운다.
- 기존 ID: market/board naru, npc naru_chief·naru_well_woman, pastime goryeo_fishing. 집 수는 정해진 값이 없어 5채로 잡았다.
