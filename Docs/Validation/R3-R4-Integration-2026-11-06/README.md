# R3·R4 통합 검증

[구현/범위/한계](../../Goryeo/R3-R4-Integration-2026-11-06.md)를 참고한다.

- `python-full.log`: 403검사/23skip, `python-pillow.log`:403/32skip, 실패0.
- `dotnet-live.json`/`dotnet-live.log`/`probe-environment.json`: 원본 소스를 설치된 SDK10에 임시 연결한62규칙 결과/소스 지문. net8 원본 통과라는 뜻이 아니다.
- `terrain.json`(54씬), `goryeo.json`, `transit.json`, `q1.json`, `experience.json`, `balance.json`, `compatibility.json`, `world-editor.txt`: Editor8종. Q1 인자 회귀는 수정 후 통과.
- `save-preservation.json`: 기존 저장9파일 해시 불변·일회용 슬롯 잔여0.
- `../../../ArtReview/Goryeo-R3-R4-Integration-20261106/play.json`: 최종 실제 등록 씬 Play99확인/10캡처. 콜백·가상 키·QA 위치 배치이며 사람 직접 플레이가 아니다.
- `fixture-history/continue-nonstart-scene-failed.json`: 잘못된 도착지 Continue 시험 실패 기록. 최종은 실제 build0 시작 씬→Space Continue로 고쳤다.
- `fixture-history/q1-one-argument-entry.txt`: 기존 Q1 호출 호환 회귀와 게임 진입점 복원 기록.

별도 `python3 Tools/Integration/validate_world.py`: 0오류·1437등록출구. Unity Console error0 확인. 실제 저장 매체 장애/앱 재시작/온라인/FPS/게임 빌드는 미검사. 화면 승인·원격 통합은 아직 하지 않았다.
