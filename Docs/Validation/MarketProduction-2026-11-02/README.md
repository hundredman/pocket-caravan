# 장터 본편 연결 검증 — 2026-11-02

사용자 요청: integration에는 검토본만 있었으므로 실제 본편에 같은 장터 에셋·배치를 연결한다. 대상은 기존시장7곳과 이정표만 있는2맵, 9씬/67개 배치. 승인2종과 기존 게시판/이정표 포함 PNG4개는 온전한 이미지로 복사, MapJsonBuilder가 본편 생성 시 GoryeoMarketLayout를 적용한다. 원본 배치/이미지 지문은 MarketLayout.json과 ArtReview/Approved/MarketCourts-v1/approval.json에 있다.

## 실행한 검사

- Python3.10.20 / Pillow12.3.0 / numpy·scipy 별도 venv: `/tmp/pocket-market-py/bin/python -m unittest discover -s Tools/Integration -p 'test_*.py'`: **347개,16skip,실패0**. tests.txt. 기존 파일 핸들 ResourceWarning은 남는다.
- 신규 본편 계약4검사: 승인 배치/PNG RGBA 보존, 대상맵·기능범위, 본편9씬에 실제 컴포넌트·Sprite GUID 연결, 충돌 추가 후 기존 기능/출구 도달.
- 최초 전체 실행은2실패(initial-tests.txt). 저장별칭이 기존8+북유럽2로 늘었지만 검사에8개가 고정됐고, 북유럽 눈숲의 사용하지 않는 팔레트에도 씬 GUID를 요구했다. 원격 f90cb68d4 파일에서도 별칭10/해당미사용GUID없음 확인. 테스트만 원래고려8매핑 보존·별칭중복금지/실사용타일 참조로 정정, 담당외 게임데이터 수정0.
- Unity6000.3.26f1 컴파일/콘솔 오류0. **9개 본편씬/67Sprite** 직접 확인, Editor 이미지 의존0, 원본크기/PPU16/단위배율 유지.
- 실제 본편7시장 Play 자동 **1072확인 실패0**: 요청한 맵 ID/previewOnly false, 본편Art경로, 기본충돌, 저장위치가 소품 밑일 때 안전복구, 이벤트 교체·기본충돌해제·복원, 캐릭터프레임, 게시판 열기/닫기. 최종 Unity/checks.json과 게임캡처7장·게시판UI1장은 ArtReview/MarketProduction-20261102에 있다.
- 안전검토 씬으로 시작해 자동저장을 끈 채 본편을 로드했다. 사용자 게임저장/백업6파일 실행 전후 바이트 동일(save-preservation.json). Unity analytics 캐시는 게임저장 검사에서 제외했다.
- validate_world.py: **1437출구 오류0**. git diff --check 통과.

## 범위와 한계

본편9씬만 원본으로 재생성했다. Core/Economy/Quests/UI·GameDatabase·WorldIndex·EditorBuildSettings·지역등록/연결·맵JSON·타문명씬·지형·추가논 미변경. 상태창/본편튜토리얼/기존시장ID·퀘스트ID·저장형식 미변경. 촬영용 코드에서만 도입 대사/오버레이를 숨겼다. 초기 화면 프레임이 비어 촬영된 사례는 맵ID 확인/렌더링준비 대기를 추가해 최종 캡처로 대체했다. 초기 자동확인을 화면 검증 성공으로 대신하지 않았다.

사람이 한 WASD/모든 출구 왕복·교역 손익/장시간·실기기FPS·모든 화면비·게임빌드는 이번에 하지 않았다. 원격 통합은 팀카톡 사전공지 전달 확인 후 PR로 처리한다.
