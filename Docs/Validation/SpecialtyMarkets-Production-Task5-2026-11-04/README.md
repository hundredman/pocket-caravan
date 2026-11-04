# Task5 승인 좌판 본편 검사

사용자 “그래.” 승인4종/기존10개 자리 그대로 본편 연결. 승인 RGBA/배치 해시를 기록한 ArtReview/SpecialtyMarkets-Task5-20261104/approval.json에서 MarketCourts-E2/layout.json 원본을 갱신했다. market_production.py로 전체 PNG4종/MarketLayout 생성. 다른 마을 배치·모든 좌표/크기/통행 범위·Maps JSON·시장/NPC/퀘스트ID·저장 형식 유지. ImportMarketArt 및 BuildSelected로 transit_southport/transit_crossroads 두 씬만 재생성했다. 공용 변경 파일은 이 transit 두 씬, DB/WorldIndex/BuildSettings/Core/Economy/UI는 유지했다.

실제 본편 두 씬의 전용 소품5개씩·크기/PPU16·trial 컴포넌트 없음·안전한 촬영 위치·공용 큰 장 순환 미추가·자동 저장 차단을 자동 Play29확인, 실제4캡처. 앞선 승인 검토의250회 통행 비교에 더해, 전체 test_market_production의 출구/기능 연결 검사가 통과했다. 저장9파일 백업/불변, 사람이 직접 걷거나 온라인으로 확인한 결과는 아니다.

Python358개 두 환경 실패0(numpy/scipy23skip/Pillow전용32skip), 월드0오류/1437출구·Unity 지정8종 통과. PNG 비교는 디코딩 RGBA만 쓴다.
