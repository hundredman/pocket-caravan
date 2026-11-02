# 마을 장터 — 2026-11-02 사용자 본편 통합 지시

기존 수레·벤치 분산 배치 반려를 반영했다. 천막 곡물 좌판과 낮은 잡화 판매대가 고객 통로를 둘러싼다. 원본 프롬프트는 [prompts.json](prompts.json), 제작은 내장 imagegen과 Tools/ArtTrials/market_courts_prepare.py(기존 아트 색 기반 초안 팔레트·최근접 정리), 배치 원본은 Tools/WorldLayout/market_layout.py 및 village_fill.py다.

- [곡물 좌판48×48](Native/grain_stall.png), [Aseprite](Native/grain_stall.aseprite)
- [잡화 판매대48×32](Native/wares_counter.png), [Aseprite](Native/wares_counter.aseprite)
- [장단 전후](jangdan-comparison.png), [시장7곳 모음](all-markets.png)

원화 Raw/와 native PNG/ASE는 새 버전으로 보존, 기존 승인 아트를 덮어쓰지 않았다. 소품53좌판·게시판/이정표14, 총67배치. 4개 기능 미정 장소는 장터 추가 보류. 예전 비교의 왼쪽은 반려된 소품 배치, 오른쪽은 실제 Play 촬영한 장터 시안이다. 2026-11-02 사용자 통합 지시로 새 아트/배치를 승인 기록(ArtReview/Approved/MarketCourts-v1)에 보존하고 본편 생성기·9씬에 연결했다. 실제 본편 촬영은 ArtReview/MarketProduction-20261102, 원격 병합 상태는 WorkLog에서 확인한다. 검토메뉴: Pocket Caravan → Review → 마을 공터 검토 (저장 안 함).
