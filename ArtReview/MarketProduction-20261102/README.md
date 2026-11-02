# 장터 본편 연결 — 2026-11-02

검토본의 동일 그림/배치를 실제 본편 씬에 연결했다. market_production.py → Data/Goryeo/MarketLayout.json → GoryeoMarketLayout/MapJsonBuilder → 본편9씬. 재생성은 Pocket Caravan/Art/승인 장터 본편 씬 생성. 게임은 일반 나루 씬에서 Play하면 보이며 검토 메뉴가 필요 없다.

시장7곳53좌판, 게시판/이정표14개 총67개. 7시장에 더해 중앙 길목·남동 길의 이정표2맵 포함. 기능 미정4곳에 새 거래 기능을 만들지 않았고 지형·논·숲은 이번 변경에서 유지했다. 새 PNG2종은 승인 당시와 디코딩 RGBA 일치.

[전체 비교](all-markets.png)의 왼쪽은 기존 승인 검토본, 오른쪽은 실제 본편 Play 촬영이다. 원본 촬영은 Unity/에 있다. 동일640×360 카메라·1280×720 캡처, 비교판만 최근접 축소했다. 공통 도입 튜토리얼은 촬영용 QA에서만 숨겼다. 본편 튜토리얼 코드는 수정하지 않았다.

최종 QA1072확인/정적347(16skip) 실패0. 세부 검증: Docs/Validation/MarketProduction-2026-11-02. 수동보행/장시간/FPS/게임빌드 미검사. 원격 반영 상태는 WorkLog 후속 기록을 확인한다.
