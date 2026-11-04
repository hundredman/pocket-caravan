# 사용자 요청 일시 중지 — 양모 v4 적용 후 검사 재개

브랜치 merge/integration-goryeo, 저장소 /Users/kim/Documents/2026-2/Game Development/팀 프로젝트/PocketCaravan. push/PR 금지. 최신 main 합치기는 재개 후 보고 전에 한다.

완료: 사용자 “좋다.”로 폭14→12px 양모 v4 승인. ArtReview/WoolWidth-v4-20261104/approval.json 기록. item_icons_promote.py가 v4 승인 override를 선택하도록 갱신하고, 본편 Art/Goryeo/Items의 양모 PNG/Aseprite·A2 catalogue만 교체. 기존 Sprite GUID/상품 데이터/가격/기능/다른 그림 유지. test_goryeo_trade_goods.py의 승인 원본 비교도 v4를 따르도록 갱신. Unity 재임포트 완료.

멈춘 위치: 실제 등록 아이콘의 소지품 창 확인을 위해 SocialGoryeoReview.Run으로 previewOnly/자동저장 꺼진 검토 Play를 시작했으나, 사용자 중지 요청으로 소지품 준비·촬영 전에 종료했다. 적용 후 새 Play/정적/Unity 검사 통과를 기록하지 않았다. 저장9파일은 중지 뒤 백업 해시와 일치 확인.

재개할 일:
1. 승인 Native와 본편 양모 PNG의 디코딩 RGBA/16×16·내용 폭12px/높이12px 및 A2 catalogue를 확인한다.
2. 실제 등록 상품의 InventoryUI를 안전한 preview 부트스트랩에서 열어 새 본편 양모를 촬영한다. 자동 저장을 계속 끄고 저장 파일을 백업/불변 확인한다. 검토용 복사본 캡처를 본편 검사로 대신하지 않는다.
3. Python360개 전체환경/Pillow전용, validate_world 및 지시문의 Unity8종을 실행한다. 아직 실행하지 않은 검사다. 필요 없는 데이터 등록/맵 재생성은 하지 않는다.
4. 결과/Plan/CurrentState/WorkLog 갱신·로컬 커밋. 최신 main을 합친 뒤 보고한다.

별도 대기: 통나무/포구 그림 승인, 신규 품목 장거리 수익 Claude 검토(Docs/Goryeo/Task6-Goods-Review-2026-11-04.md). 임의 수치 조정/반려 범람 PNG 연결 금지.

사용자 기존 변경은 보존: Packages/packages-lock.json, Assets/Caravan3DPrototype·GoryeoJourneyPrototype 및 meta, Presentations/2026-10-26, Tools/Prototype3D. 이번 체크포인트에 포함하지 않는다.
