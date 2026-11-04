# E3 범람 — 본편 연결 완료

2026-11-03 사용자 지시로 에셋 제작·배치·본편 연결의 기본 담당을 Codex로 바꿨다. 이전의 Claude 배치 전달 단계에서 이어, Codex가 원본·본편 맵·씬까지 연결했다. 품질이 부족할 때 Claude에게 넘길 수 있으며 사용자 그림 승인 원칙은 유지한다.

## 승인과 적용

승인: `ArtReview/E3-FloodTerrain-20261103/approval.json`. 기존 강62칸 확대, 길22칸 침수. 별도 범람 PNG v1/v2는 쓰지 않는다.

- 원본: `Tools/WorldLayout/m4_export.py`가 맵 JSON을 쓰기 전에 `river_flood_binding.attach()`를 호출한다. 전체 맵을 다시 생성해도 승인된 범람 메타데이터가 유지된다.
- 이번 적용은 `python Tools/WorldLayout/m4_export.py --bind-approved-flood --production`으로 기존 층/ID를 보존하며 상태만 재생성했다. 다른 맵이나 세계 지도를 다시 굽지 않았다. 이 보조 경로도 전체 내보내기와 같은 원본 함수를 쓴다. 검토 경로는 previewOnly를 설정한다.
- 본편: `Maps/transit_wetroad.json`의 `terrainFloods` 1개. ID `e3_wetroad_flood`, 정의는 기존 `river_flood`. 바뀌는 칸 쌍80개. 기존 모든 필드/ID/지형 배열/출입구/도착 위치는 동일하다.
- 씬: `Scenes/Transit/transit_wetroad.unity`. `RiverFloodProductionReview.Build()` → `MapJsonBuilder.BuildSingleMap()`으로 해당 본편 씬만 다시 만들었다. 기존 씬 meta/GUID를 유지하고 다른 문명 씬·WorldIndex·BuildSettings·DB는 건드리지 않았다.

## 동작·안전

기존 일별40% 선택과 활성9초/개방6초를 그대로 쓴다. 순환 시계는 기존 장애물과 같은 방문별 플레이 시간이며 UI 일시정지 동안 멈춘다. 같은 맵·ID·게임 날짜의 범람일 선택은 동일하고, 재진입하면 기존 방식대로 순환은 처음부터 시작한다.

Ground/Deco/Blocked/RoadGrid를 함께 바꾸고 물이 빠지면 원래 참조/등급을 복원한다. 캐릭터 발 충돌 상자가 잠길 칸에 걸치면 적용을 보류하고 벗어난 뒤 재시도한다. 강제 이동·재산 차감·사각 Sprite/영역 표시는 없다. 기준 지형과 다르면 변경하지 않고 오류로 멈춘다. 별도 그림 장애물과 중복 배치도 생성 단계에서 거부한다.

새 저장 필드는 없다. 기존 날짜와 방문별 순환을 사용한다. 온라인 서버 동기화는 별도 작업이며 이번에 구현하지 않았다.

## 실제 확인 범위

`RiverFloodProductionPlayReview`: 안전한 previewOnly 세션에서 저장을 끈 뒤, **previewOnly=false인 실제 본편 씬**을 불러 검사했다. 자동 전환/물 빠짐/재범람·실제 PlayerMotor 차단과 통과·위치 보류/재시도·비활성 복원·재진입을 확인했다. 실제 출구의 Use → GameManager.Travel 경로로 북동 포구와 왕복했다. 버전2/현 배치의 옛 저장 형식 시험 데이터를 실제 Apply/Travel 경로에 메모리에서 복원해 돈137·짐2개와 캐릭터 안전을 확인했다. 사용자 파일을 시험 데이터로 덮어쓰지 않았다.

본편 Play3740확인(칸별 반복 포함), 실제480×270 3캡처. 저장9파일 불변. 월드0오류/출구1437개. 최종 정적 검사 기록은 `Docs/Validation/E3-FloodProduction-2026-11-03/`를 따른다. 최초 정적 검사에서 예전 경계 검사가 새 범람 메타데이터를 예상하지 못해 실패했고, 원본 지형/ID 해시는 계속 검사하면서 새 상태도 승인 원본과 대조하도록 보완했다.

사람이 직접 한 플레이·모든 구형 저장 사례·네트워크/서버는 미검사다. GitHub push/PR/원격 통합은 하지 않았다. 통나무/아이콘/포구 수정의 별도 승인 대기 상태는 이 범람 승인으로 변경하지 않았다.
