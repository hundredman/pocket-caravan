# Task4 승인 큰 장 좌판 본편 검사

market_production.py --fair-only로 승인 RGBA/48×48 전체 이미지2종을 검증·복사했다. FairLayout 원본과 GoryeoMarketLayout 연결에서 두 기존 동쪽 큰 장 맵만 GoryeoFairDressing을 생성한다. MapJsonBuilder.BuildSelected로 transit_woodtown/transit_ferry 두 씬 재생성. 원본 Markets/MarketLayout, 맵 JSON/충돌·Core/Economy/UI/DB/WorldIndex/BuildSettings는 변경하지 않았다. 변경 공용 파일은 transit 두 씬이다.

실제 본편 씬에서 날짜1/2 일정에 따른8좌판 표시, 같은 씬 다음날 원본 복원/큰 장 재표시, 비활성/재활성 원본 복원, 좌판 주변250회 보행 가능 여부 비교 등 자동 Play291확인. 실제6캡처, 저장9파일 백업/불변. 검사 부트스트랩은 previewOnly로 자동 저장을 끄고 그 뒤 본편 씬을 로드했다. 사람이 직접 조작한 플레이·온라인 검사는 하지 않았다.

Python358개: numpy/scipy 환경23skip, Pillow전용32skip, 모두 실패0. 월드0오류/1437출구·Unity 지정8종 모두 통과. PNG 동일성은 디코딩 RGBA 비교. 공용 CentralFair 일정/보너스/대상 마을 배열 및 타 지역은 변경하지 않았다.
