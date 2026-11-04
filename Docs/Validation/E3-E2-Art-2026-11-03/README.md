# E3/E2 그림·포구 검토 — 2026-11-03

- 새Native5종(PNG/ASE5쌍):48×48/80×48두상태/16×16두아이콘. 내장imagegen원화→AsepriteLua오프라인정리. native-checks.json에크기·팔레트색수·RGBA지문·이진알파·ASE재출력동일기록. A2 catalogue0오류, 엔진렌더분할41개가전체형상보존. 실제본편배치검사/승인은 아님.
- 범람동쪽접속RGBA가원래강(127,185,175,255)와일치, 빠짐마스크가넘침부분집합. Unity샘플두점은후처리위치차이로RGB한채널1차이였으며원본PNG연결면은동일하다.
- 포구정적계약3검사: Native/A2, 범람접속/축소, 남동바다빈칸추가가해안선/충돌/출구/NPC/다른필드를변경하지않음·반복적용동일.
- 전체Python350검사/16skip/실패0(tests.txt). 사용Python3.10.20/Pillow12.3.0. 기존파일핸들ResourceWarning은남음.
- Unity6000.3.26f1:5개독립검토씬, 최종Play45확인실패0(536TileSpritePPU16/참조는별도Editor검사), 480×270실제9장. 그림규모/일반강·범람2상태·아이콘임시UI·포구2전후. 최종콘솔오류0. 초기에임시타일PPU100/재임포트Sprite참조오류발견→검토용Importer/Tile참조재바인딩후전부재촬영. 런타임검토코드의EditorAssembly참조도로컬상수로정정. 초기결과를최종통과로사용하지않음.
- 실제사용자게임저장/백업9파일QA전후바이트동일. Unity analytics제외(save-preservation.json). 본편Maps/Scenes/Scripts/Core·Economy·Quests·UI/아이템·장애물정의/공용물결PNG/DB/색인/빌드설정미변경.
- 실제E3시작/치우기/기다림·충돌/새상품거래·사람WASD·FPS·빌드미검사. 그림/포구수정은승인전이며원격미반영. 배치·기능연결은Claude담당.

산출물 ArtReview/E3-E2-Art-20261103: review-assets.png/review-ports.png/Unity9장/정적전체맵비교/Native/Raw/프롬프트/적용메모.

## v2 사용자 수정 재검사

정적350개(16skip) 실패0: v2-tests.txt. 양쪽 포구 변경 보존 검사를 확장한 뒤 대상3개 재실행 통과: v2-targeted.txt. Unity 검토씬45확인/480×270 9장, 에러로그0. 세이브9파일 동일 및 다른3그림 디코딩RGBA 보존: v2.json. 실제 사건 일정/치우기/거래는 구현하지 않아 검사하지 않았다. 본편 미적용.
