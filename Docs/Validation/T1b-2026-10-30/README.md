# T1b 카메라 튐 / A7e 마을 기본 표현·논 추가

사용자 2026-10-30 지시. `codex/transition-boundary` 로컬 작업이다. 앞선 “아직 반영하지 말라” 지시를 유지한다. 본편 맵·씬·공용DB·색인·빌드목록과 GitHub는 바꾸지 않았다. 카메라 코드와 기본 공간 교체 API만 작업 브랜치에 있다.

## 카메라

기존 코드를 같은 런타임 위치에 적용하면 0.02칸 이동에서 목표가 최대28칸 바뀌었다. 출구의 행 범위 .75칸을 벗어날 때 다른 출구를 고르는 불연속과, 좁은 구역에서 Facing으로 반대쪽 고정 면을 선택하는 것이 원인이다. 연속적인 경계 제한/좁은 구역의 가까운 중심으로 변경하고 실제 출구 접촉 프레임을 나가는 페이드 동안 유지한다. 다른 문명은 기존 직사각형 정책을 유지한다.

| 실행 검사 | 결과 |
|---|---|
| Unity13맵 미세 이동·방향 전환 | 23,770미세이동, 위치0.02칸 변화에서 새 목표 최대0.04473칸. 방향 전환만의 변화0 |
| 실제 PlayerMotor/Camera LateUpdate | 고정60Hz/1,560렌더 프레임, 한 프레임 최대0.13263칸. 이동/충돌/네 방향 전환을 실행했으며 실기기FPS 측정은 아님 |
| 출구/페이드/속도·FPS/실제 지역 이동 | **7182확인 통과**. 실제 접촉 코드와 동일하게 FrameExit 후 화면 끝 및 페이드 중 유지 확인. 정지·입구 칸 중앙·역방향은 이동 금지, 외국 기존 판정 확인 |
| Python | **341개 실행,16skip,실패0**. 생성하지 않은 검토 맵에 의존하는 검사는 다른 환경에서 추가skip 가능 |
| 본편 세계 연결 | 1408출구,오류0 |

런타임 보고: `ArtReview/CameraStability-20261030/checks.json`, `TransitionRegression/checks.json`. 기존 T1a 증거를 덮어쓰지 않는다. 입력 격리는 씬 로드 시 첫 Update 전에 컨트롤러를 끄며, 이동 검사는 Motor.Tick을 직접 호출한다. 초기 캡처에서 본편 씬으로 이동하는 실패가 있었고, 로드 직후 입력을 격리한 뒤 최종 재실행 결과만 위에 집계했다.

## 누락/미배치 구분

- **실제 게시판7곳:** 기능 객체는 있었지만 M2에서 asset=None이었다. 원래 게시판 목적으로 쓰던 16px 원본을 연결했다.
- **이정표7곳:** 텍스트 대화 위치만 있던 곳에 원래 이정표 원본을 붙였다. 대화 위치는 그대로, 그림/충돌은 가까운 빈 칸에 두었다.
- **장터·이벤트 마당:** 흙만 있던 기본 모습에 기존 승인 점포·수확물·벤치·수레를 용도에 맞게 배치. 합계59개. 새 그림 제작/색 변경/크기 변경/부분 추출 없음. 모든 새 PNG 동일성 검사는 디코딩 RGBA로 비교한다.
- **예정 시설4곳:** 남동 포구·중앙 길목은 E2 연결 예정, 쉼터2곳 시장/NPC는 결정 대기. 새 거래/의뢰 기능이나ID를 만들지 않고, 이용 가능한 시장처럼 보이지 않게 벤치·짐 정리 소품만 배치했다.
- **집2곳:** 닥목 집3·북쪽 쉼터 집2는 M2 `missing:true`로 위치가 정해지지 않은 항목. 에셋 이미지 누락과 다르며 이번 배치에서 임의로 집터를 정하지 않았다.
- **조건부 표현:** 성장 소품은 레벨 조건, NPC·시작 위치는 런타임 캐릭터/스폰, 부두·물가는 바닥 지형/별도 배로 표현한다.
- 넓은 수동 카메라 캡처의 남동 포구 맵 밖 검은 모서리·북동 포구 잔물결의 바탕색 차이는 추가 화면 점검 대상으로 남긴다. 이번에 해결했다고 주장하지 않는다.

상세 목록은 [asset-audit.json](asset-audit.json), 배치 원본은 `Tools/WorldLayout/village_fill.py`, 검토 묶음은 `ArtReview/VillageFill-20261030/manifest.json`이다. 입력 시점의 맵 메타데이터/객체 지문을 함께 기록해 이후 다른 팀 작업을 과거 검토본에 고정하지 않는다.

## 기본 표현과 이벤트 교체

`VillageDressing.SetEvent("event-yard", eventPrefab)`가 해당 공간의 평상시 루트와 충돌을 비활성화한다. null을 전달하면 동일 배치/충돌을 복원한다. 이벤트 자체의 충돌·내용·발생 시기는 기존 이벤트 시스템/이벤트 프리팹이 담당하며 새 일정이나 축제를 정하지 않았다. 정상 소품의 렌더러 배열은 Awake에서 캐시해 매 프레임 배열을 만들지 않는다. 한 화면에는 그 맵의 소품만 존재한다. 성능 보장은 실기기 측정이 필요하다.

11마을의 평상시/이벤트 교체·복원, 소품 충돌, 소품 밑 옛 위치의 안전 복구, 기존 게시판 열기/닫기, 캐릭터16프레임 존재 확인을 실제 Unity에서 실행했다. 추가 논3곳을 포함한14시점 전후28장(각1280×720), 게시판UI1장 보관. 정오/같은 카메라로 비교하며 사진용 UI숨김·넓은 구도는 Play 세션에만 적용했다. **1511확인 통과**. 3곳의 논은 장단(193,48), 개경(240,8), 송악(174,-5) 세계좌표. 승인된12×8칸 완성 논 원본 그대로다.

생성기 인증87원본/3294합성칸 오류0, 13맵 pixelDiff0. 기본 소품을 포함한 정적 시작 위치→모든 마을·기능물 도달 검사 통과. 추가 논으로 숲 장벽이 열리는 미리보기 경계7칸은 원본 생성기가 이웃 출구/도착 자리를 함께 추가했으며 옛 객체/ID는 모두 보존했다. 본편 적용 때 이 추가 경계도 함께 검토해야 한다. 본편에는 현재 추가하지 않았다.

## 재현

1. 기존 건물 연결 단계의 `ArtReview/BuildingNatureBinding-20261029/BuildingLayers` 원본 층을 준비한다(이전 건물/자연 검토 생성 절차 참고). `python Tools/WorldLayout/village_fill.py`로 배치 manifest·native복사·검토 자연 레시피를 생성한다.
2. `M4_LAYERS=<repo>/ArtReview/VillageFill-20261030/Layers M4_REVIEW_ROOT=<repo>/ArtReview/VillageFill-20261030/Base M4_NATURE=review M4_NATURE_LAYOUT=<repo>/Tools/WorldLayout/nature_village_review.json python Tools/WorldLayout/m1_tiles_preview.py --zones`
3. `python Tools/WorldLayout/m4_export.py ArtReview/VillageFill-20261030/Layers --preview-root Assets/PocketCaravan/Editor/B2Preview/VillageFill --review-root ArtReview/VillageFill-20261030/Maps --world-map-preview --preserve-objects`
4. Unity에서 `VillageFillReview.Build()` 실행. 파일을 손으로 고치지 않고13씬을 생성한다. `VillageFillPlayReview`는 previewOnly 씬에서 시작한 임시 Play 객체로만 실행한다. `shots.json`은 검토 구도의 좌표 원본이다. 실행 전/Play 중 Game View1280×720 설정이 필요하다.
5. `python Tools/WorldLayout/village_review_sheet.py`는 실제 PNG만 묶는다. 소스 아트는 편집하지 않는다. 카메라 검사는 같은 안전 시작에서 `CameraStabilityPlayReview`, 경계 회귀는 `BoundaryTransitionReview`에 별도 Output 경로를 설정해 실행한다.

Play 전후 자동저장 비활성화를 검사했다. 이번 세션은 실제 저장 파일의 시작 전 바이트 지문을 새로 수집하지 않았으므로 저장6파일 전후 해시 검사를 했다고 기록하지 않는다. 사람이 실제 WASD로 체감을 확인하는 것, 실기기FPS/장시간 플레이/플레이어 빌드는 미실행이다. 승인·팀 사전 공지·추가 통합 지시 전에는 원격 반영하지 않는다.
