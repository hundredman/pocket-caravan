# A7d 수원·곡창지대·지도/UI 검토

이 문서는 A7d 검토 단계의 기록이다. 이후 사용자 통합 지시로 본편13맵·씬·지도와 승인 live 스냅샷을 연결했다. 최신 결과는 [본편 통합 검증](../A7d-Integration-2026-10-30/README.md)을 따른다.

## 배치

- 수원 네 곳: 서쪽·고려 북부·북동 수원에 새 작은 바위 수원 원본 3개(160×160 두 방향, 96×96 하나), 승인된 독립 바위산 1개를 조합했다. 호수 수원의 승인된 큰 폭포·출입구 조정은 유지한다. 바위 주변 숲은 전체 개체 단위로만 걷어낸다.
- 논은 기존 2→**8구획**: 장단 4, 개경 2, 나루 1, 송악 1. 승인된 초원 위 192×128 완성 논을 원래 크기로 배치한다. 도로·물·건물·기능물·짙은 숲을 피한 평지에서 고른다. 농경지 아래 흙 타일을 덧대지 않는다.
- 첫 10구획 검사에서 일부 그림 여유가 맵 경계를 넘는 문제가 드러났다. 원본 선택 조건에 한 맵 소유 범위 전체 포함을 추가해 8구획으로 조정했다. 내보내기도 경계 상자의 포함만 보던 판단을 실제 소유 범위까지 확인하도록 고쳐, 다른 맵의 논을 잘못 CropDepth에 연결하지 않게 했다.
- 수원 연결은 기존 강에 이어진 물 칸의 4방향 연결로 확인했다. 큰 폭포의 옛 접속부는 승인된 출구 조각 때문에 원래 설계 중심보다 한 칸 서쪽으로 흐른다. 원래 중심(117,-9)을 물이라고 가정하지 않고, 조각 아래 실제 강(116,-6)에서 상류 접속을 검사한다. 이 물길은 이번에 바꾸지 않았다.

원본: `Tools/WorldLayout/a7d_design.py`, `nature_a7d.json`, `nature_masks.json`. 새 그림의 프롬프트·AI 원본·Aseprite 원본·RGBA 지문은 `ArtReview/A7d-20261030/Headwaters/`에 있다. imagegen → Aseprite 전체 원본 축소/팔레트 정리로 제작했으며, 맵 생성 중에는 자르기·변형·재채색하지 않는다. 새 그림과 배치 모두 승인 전 production 게이트가 막는다.

## 지도와 UI

지도는 기존에 현재 맵과 일부 대표 마을만 이름을 표시하도록 필터링되어 있었다. 필터를 없애 **13지역+쉼터 2곳**, 고려 상세는 5마을을 표시한다. 이름 원본은 `map_labels.json`이며 ID·시장/NPC/퀘스트 이름·저장은 개명하지 않는다. 지명은 양옆 목록과 연결선으로 겹치지 않게 배치한다. 의뢰 목적지는 점 색으로 구별해 긴 지명 뒤의 추가 글자가 잘리지 않게 한다.

고려·담당 이동 구역에서 UI 배율은 정수(최소 1배), 본문/보조 글자는 갈무리의 본래 크기 12/10px를 쓴다. 전용 폰트 사본의 HintedRaster·Point 설정으로 다른 지역의 기존 폰트 설정을 보존한다. 세계 카메라 줌과 UI 배율은 별개다. 작은 화면에서는 창 높이를 제한하고 장터 전체 내용을 스크롤하며, 돈·짐 안내와 하단 조작 안내는 나눠 쓴다. 다른 지역 배율은 기존 360 기준을 유지한다. 지도·이름표·알림·퀘스트 안내·대화·장터 행의 캐시도 폰트/크기 변경에 맞춰 갱신해 지역 간 이동 뒤 이전 설정이 남지 않게 한다.

빠른 QA 맵 이동에서 지붕 위로 여러 지역 이름이 겹치던 것은 지도 지명 자체가 아니라 3.5초짜리 **위치 알림이 최대 4개 쌓이는 현상**이었다. unscaledTime 사용은 원래 맞았다. 담당 지역의 위치 알림만 최신 1개로 교체하고, 의뢰 등 일반 알림은 보존한다.

참고한 1차 자료:

- [Galmuri 제작자: Galmuri11=12px, Galmuri9=10px](https://github.com/quiple/galmuri)
- [Xbox 접근성 지침: 텍스트 표시](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/101)
- [Game Accessibility Guidelines: 읽기 쉬운 글자 크기](https://gameaccessibilityguidelines.com/use-an-easily-readable-default-font-size/), [단순한 텍스트 서식](https://gameaccessibilityguidelines.com/use-simple-clear-text-formatting/)
- [Stardew Valley 공식 위키 옵션: UI 배율과 화면 줌 분리](https://stardewvalleywiki.com/Options)
- [Unity HintedRaster](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FontRenderingMode.HintedRaster.html)

## 검증과 한계

최종 확인: 정적 검사329개 실행(16개 건너뜀), 실패0. 세계 검사 오류0/출구1,398개. 검토13맵 Unity 자동 Play **194개 확인 통과**, 480×270 게임 캡처23장. UI는480×270/960×540/1280×720에서 총21장과 창 범위·배율·다른 지역의 기존 배율 보존 조건을 확인했다. 합성 인증54원본/1,670합성 칸 오류0. 원본 재생성5층 RGBA/크기·13맵 JSON 차이0. 모든 맵/기능물 정적 도달, GameDatabase·WorldIndex·Build Settings 불변. 실제 저장6파일은 09:49 체크포인트와 최종 내용/목록이 같고 수정 시각은 모두 첫 QA 캡처 전이다. 작업 시작 전 해시 스냅샷은 없으므로 전날 해시와 동일하다고 주장하지 않는다(두 저장은 오늘00:57에 변경되어 전날 값과 달랐으며 되돌리지 않았다). 최종 콘솔 오류0. 세부 실행 결과는 이 폴더의 검사 로그와 JSON을 따른다. Unity 자동 Play와 실제 렌더 캡처를 사용했으며, 사람이 모든 UI를 마우스·키보드로 조작한 장시간 플레이나 목표 기기의 성능 측정은 아니다. UI QA의 창 열기·스크롤 위치·닫기는 검토 도구가 호출했다. 큰 폭포 대표 캡처만 검토 카메라를 지형 중심에 고정했다(줌/그림 변경 없이 실제 Unity 렌더). 다른 대표 화면은 플레이어 추적 카메라다. 현재 지도 UI 캡처의 배경 지형은 본편 지도이며 이름/서식 수정 검토다. 자연 지형을 승인·통합하면 지도 배경도 같은 원본에서 함께 갱신한다.

재생성: 승인 건물 연결이 적용되는 `m1_tiles_preview.py --zones`를 자연 레이어 없이 실행해 `ArtReview/BuildingNatureBinding-20261029/BuildingLayers`를 준비한 뒤 `a7d_design.py` 실행. `M4_LAYERS=ArtReview/A7d-20261030/Layers M4_REVIEW_ROOT=ArtReview/A7d-20261030/Base M4_NATURE=review M4_NATURE_LAYOUT=Tools/WorldLayout/nature_a7d.json`으로 같은 생성기를 실행하고, `m4_export.py ArtReview/A7d-20261030/Layers --preview-root Assets/PocketCaravan/Editor/B2Preview/A7d --review-root ArtReview/A7d-20261030/Maps --world-map-preview`로 내보낸다. 경로 환경 변수는 프로젝트 절대 경로로 지정한다. Unity의 `NatureIntegrationReview.Root/Output` SessionState를 A7d로 설정하고 Build/Run한다. 빌드 설정의 임시 QA 변경은 종료 후 복원한다. 플레이어 빌드는 하지 않았다.
