# 통나무 길막음 본편 연결 — 승인 적용

최종 승인: 초원 진입로 숲 사이 길목 사선1개, JSON(50,32)/3×3/diagonal_nw/e3_central_approach_log. approval.json에 “이 방식으로 적용” 원문·범위를 기록했다. 혼자 어느 한쪽에서3초 치우면 사라진다. 반대편 이동/동시 작업은 필요 없다. forest-placement-review.png가 승인 당시 검토본, production-comparison.png와 ProductionUnity/가 적용 뒤 실제 본편이다.

## 원본과 본편

Tools/WorldLayout/native_log_binding.py·log_placement.json → 승인 PNG/ASE 전체 복사 및 Art/Goryeo/RoadLogs/catalogue.json(A2), Data/Goryeo/LogArt·LogPlacement, 해당 Maps JSON 두 메타데이터(obstacles/nativeLogs) → BuildSelected(transit_central_approach). m4_export에는 재현 호출만 추가, 전체 내보내기 미실행. production-source-before.json과 기존 모든 필드가 동일하며 지형·길·시장·NPC·출구는 유지한다. 다른 맵/씬은 바꾸지 않았다.

그림은48×48/PPU16/Point/무압축/scale1/rotation0. 몸통 충돌은16×16칸 중 불투명 면적64px 이상인 칸을 골라 생성한다. 세로3/가로5/양사선5칸으로 빈 모서리를 줄인다. 그림의 가는 가지를 모두 큰 사각 충돌로 막지 않는다. 고정된3×3 캔버스와 별개이며 주인공 발 충돌은 기존 PlayerMotor가 맡는다. nativeLogs 없는 기존 장애물은 원래 그림·충돌·표시를 그대로 유지한다. 승인 메타데이터와 일치하지 않는 본편 바인딩은 빌더가 거절한다.

## 검사

Unity/play.json: 검토용4방향 Play132확인·12캡처. 전체 맵의 충돌/비점유 칸 비교, PlayerMotor 차단/제거 후 이동, 이동 취소/무료3초, 방문 재입장, 겹침 발생 시 탈출, 기존 사각 장애물 유지. 이 네 방향의 열린 마을 앞 장소는 **배치 반려** 상태이며 기능 회귀 시험장으로만 남겼다.

ChokeUnity/play.json: 최종 후보1곳 Play15확인·3캡처. ProductionUnity/play.json: 실제 본편 Play15확인·3캡처. 사건 전15×15 통로 연결/활성 단절/제거 후 복원, 어느 편에서도 상호작용 대상 접근/혼자 한 번 제거·돈/짐 불변, 그림/이름표 제거. 전체 맵의 더 먼 우회 길은 남아 있다. 입력키 자체의 수동 플레이가 아니라 Interactable 대상/Interact 및 통행 API의 Play 검사다. 저장9파일 사전 백업/불변. Python/Unity8종 근거는 Docs/Validation/NativeLogBinding-Task6-2026-11-04.

## 제외·후속

남쪽 내륙길 마을 앞(placement-review.png/기존4JSON)은 길을 막지 못하고 장소 부적합으로 사용자 반려, 본편 미적용. forest-horizontal 후보는 실제 화면에서 다리 위였으므로 최종 제출에서 제외, ChokeUnity/two-sites-checks.json은 그 중간 검사 기록이다. 최종 소스 revised-layout.options에는 승인 사선1곳만 남겼다.

날씨/벼락 발생 조건·낙하 연출은 미구현. 현재는 기존8시간창50% 발생/무료3초 제거/방문 단위 해결을 유지한다. 산적 통나무 연계는 사용자 취소로 제외. 사람 플레이/온라인/빌드 미검사, push/PR 없음.
