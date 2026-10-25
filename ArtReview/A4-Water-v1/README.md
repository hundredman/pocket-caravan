# A4 물·연못 조립 시안 v1 — 작은 연못 승인·적용 / 큰 호수 미채택

최초 제출 시안과 검토 이력이다. 작은 연못22칸/11조합은2026-10-25 사용자 “작은 연못 좋은 것 같아. 승인할게.” 이후 본편에 적용했다. 큰 호수는 미채택이다. 환경 원본 유지 결정에 따라 건물/나무는 보정하지 않고 물가의 조립 방식만 시험했다. A4 전체(강·해안·길·성벽 등) 완료나 공통 팔레트(D3) 승인을 뜻하지 않는다.

2026-10-25 사용자 피드백: 작은 연못은 조금 나아졌으나 큰 호수는 기존 버전이 더 낫다. 새 호수 시안은 채택하지 않고 기존 조립 타일 표현을 유지한다. 이 피드백 당시에는 작은 연못도 최종 승인 전이었으며 이후 별도 승인/본편 적용을 마쳤다. `review-decision.json`에 범위를 기록했고 원래 전후 그림은 이력으로 보존했다. 추가 호수 재제작/본편 적용은 하지 않는다. 작은 연못의 승인 동결은 `ArtReview/Approved/A4-Pond/`, 적용 검사는 `Docs/Validation/A4-Pond-Production-2026-10-25/`를 본다.

## 볼 그림

- `feedback-comparison.png`: 사용자 피드백을 반영한 현재 검토 방향. 작은 연못은 기존/시안, 호수는 미채택 시안/유지할 기존 표현. 이전 Unity 캡처 재배열이며 새 촬영이 아니다.

- `game-comparison.png`: 실제 Unity 전/후, 패널마다480×270. 위 개경 연못 두 곳, 아래 같은 형태의 호수 조립 시험장(본편 맵 아님).
- `pond-detail-comparison.png`: 같은 위치/4배 확대. 연꽃·물 영역을 유지하고 녹색 네모 바닥/흰 테두리 대신 돌마당에 연결했다.
- `module-sheet-review.png`: 조립51개와 기존 플레이어를 같은4배로 비교.
- `native-lake-ripples.gif`: Unity에서 물결4상태를 선택한480×270 반복 보기. 본편 애니메이터 연결/자동 재생 검사는 아니다.
- `game-pond-*.png`, `game-lake-*.png`: 실제Game1920×1080 원본8장. 비교용 정수 축소 외에 화면을 잘라 바꾸거나 그림을 덧붙이지 않았다.

## 원본과 사용

- `water-sheet.aseprite`/PNG: 이웃 연결47형태와 물결4상태를 각각16×16 조립 조각으로 직접 그렸다. `piece-index.json`은 원본 사각형/이웃 비트를 기록한다. 온전한 호수 그림을 잘라 재사용하지 않았다.
- `ripple.aseprite`:16×16/180ms×4/`ripple` 태그. 두 원본을 다시 열어 PNG 일치를 확인했다.
- `pieces/*.png`: 가운데·가장자리4방향·바깥/안쪽 모서리 각4방향과 단독/끝/좁은 연결 조각51개. 배치 중 회전/확대/색 변환을 하지 않는다.
- 시안 색4개는 기존 개경 물 타일의 물/그늘/흙 RGB와 `goryeo_ripples` RGB를 참고했다. 새 조각은 알파0/255이며 D3를 확정하지 않는다.
- 초기 합본 `catalogue.json`은 `changes_requested`(호수 미채택)를 보존한다. 승인된 작은 연못의 생산 목록은 `Assets/PocketCaravan/Art/Goryeo/Terrain/Pond/catalogue.json`에 별도로 등록했다. `*-preview.json`은 A2 조립 시험 전용이며 본편/세계 배치가 아니다. 충돌·정상 보행 검사는 별도다.
- `reference-tiles`는 기존 Ninja Adventure(CC0) 물 경로의 비교용 렌더 결과이며 새 조각의 원본으로 잘라 쓰지 않았다.

## 재실행

Pillow/Aseprite 필요. 본편 파일을 쓰지 않는다.

```sh
python Tools/ArtTrials/water_modules_a4.py
python Tools/ArtTrials/review_a4_water.py
python Tools/Integration/validate_assets.py ArtReview/A4-Water-v1/catalogue.json --map ArtReview/A4-Water-v1/pond-preview.json --map ArtReview/A4-Water-v1/lake-preview.json --map ArtReview/A4-Water-v1/island-preview.json --map ArtReview/A4-Water-v1/narrow-preview.json --preview
```

촬영용 `.cs.txt` 세 개는 MCP 메서드 본문이며 프로젝트 스크립트로 컴파일하지 않는다. 이 Mac의 시안 폴더 절대 경로를 담았다. 저장 꺼진 시험용 에디터의 Play에서만 실행한다. 개경 정상 발칸 확인 → 동일 카메라 전후 → 호수 시험장 전후 → 물결4상태 촬영 → Play 종료 순서다. 에셋/씬 생성·저장 명령은 없다. `unity-checks.json`에 조건/결과를 기록했다.

## 검사와 남은 일

- Python105개·출구92개 오류0. 새5개 검사는47형태/방향 역할/32,768쌍 경계 및 변조 검출/원본 셀 일치/미변형 배치를 확인한다.
- A2: 연못·호수·섬·좁은 연결4배치 오류0. 팔레트/알파/16px/원본 셀 일치. 기술 미리보기이며 사용자 승인을 대신하지 않는다.
- Unity: 개경22셀(11형태), 호수시험164셀(13형태), PPU16/Point·물결5개×4상태 선택. 개경 발칸(35,24) blocked0/reachable1 및 Unity 보행 가능. Console error0, 저장false/씬dirtyfalse 확인 후Play종료.
- Unity47형태 전체/섬·좁은 물길/실제 이동·충돌/본편 애니메이터/사람 플레이/저장회귀/빌드는 미실행이다.
- 작은 연못은 후속 사용자 승인/동결/본편 적용을 마쳤다. A4 전체나 물결 애니메이션 승인을 뜻하지 않는다. 다음 E1/B2 판단과 C3a 나무 가림·C6 굽기는 별도다.
