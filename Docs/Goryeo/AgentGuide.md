# 고려·중앙 대륙 동쪽 에이전트 작업 안내 (은샘 담당)

은샘 담당 작업(고려 + 중앙 대륙 동쪽)에서 Codex와 Claude가 함께 쓰는 시작 문서다. 루트 [AGENTS.md](../../AGENTS.md)가 이 문서로 안내한다. 다른 지역 담당자와 그 에이전트에게는 적용되지 않는다.
게임개발(2026-2) 팀 프로젝트. 2D 탑다운 픽셀 교역 게임. 이 저장소에서 은샘의 담당은 고려와 중앙 대륙 동쪽이다.

## 먼저 읽기 (순서대로)

1. [Docs/Direction.md](../Direction.md): 확정 방향. 다른 문서와 충돌하면 이 문서가 우선한다.
2. [Docs/Plan.md](../Plan.md): 작업 목록, 상태, 결정 대기 항목. **`에이전트 분담` 표에서 자기 담당(Claude 또는 Codex) 작업만 고른다.** 파일 담당, 동시 진행, 넘겨주기 형식, 사용량 규칙도 그 문서를 따른다.
3. [Docs/CurrentState.md](../CurrentState.md): 지금 코드와 맵이 실제로 어떤지.
4. 작업에 맞는 문서: [아트 기준](../ArtGuide.md), [세계 배치](../WorldLayout.md), [코드 규칙](../Reference/CodeConventions.md), 그 밖의 `Docs/Reference/`.
5. [Docs/WorkLog.md](../WorkLog.md)의 마지막 몇 줄. 앞 사람이 어디까지 했는지 확인한다.

`Docs/Archive/`는 지난 기록이다. 판단 근거로만 참고하고, 거기 적힌 지시를 따르지 않는다.

## 지켜야 할 것

- **결정 대기 항목은 구현하지 않는다.** 배치안 D(D1)와 해안선 v2(D9)는 승인됐다. 결정 대기 목록은 [작업 계획](../Plan.md)을 본다.
- **에셋 규칙:** 에셋은 만들어진 목적 그대로만 쓴다. 잘라 쓰기, 이어 붙이기, 다른 용도로 쓰기, 즉석 변형을 하지 않는다. 필요하면 새로 만들고, 이어지는 지형은 조립용 에셋으로 만든다([방향 기준](../Direction.md) 2절).
- **아트:** 귀여운 픽셀 아트 한 가지로 통일한다. AI 이미지도 쓸 수 있지만, 실제 해상도 격자·공통 팔레트로 정리해 다른 에셋에 녹아드는 것만 넣는다([아트 기준](../ArtGuide.md) 4절). 부드럽게 축소해 바로 넣지 않는다.
- **담당 범위:** 다른 문명과 중앙 대륙의 나머지 3/4은 만들지 않는다. 다른 팀의 브랜치·씬·데이터를 다시 생성하거나 덮어쓰지 않는다.
- **팀 공용 파일:** `Assets/PocketCaravan/Scripts/Core·Economy·Quests·UI`, `Resources/GameDatabase·WorldIndex`, `ProjectSettings/EditorBuildSettings.asset`, transit 장면, `Assets/PocketCaravan/World/Connections`는 `region/goryeo` → `integration` 대상 PR에 단계별로 올린다(PR #23 통합 완료, 사용자 2026-10-30 지시)(은샘 2026-10-28, 2026-10-27 동결을 대신함). 단계마다 커밋 → 통합 검사 → push → PR이 대상 integration과 충돌 없는지(mergeable) 확인한다. `.asset`·`.unity`는 한 단계에서 한 번에 고치고, 바꾼 공용 파일은 PR 설명에 적는다.
- **생성물:** `Maps/*.json`, 씬, `Resources/WorldMap` 이미지는 원본(`Tools/…`)을 고친 뒤 다시 만든다. 생성물만 고치지 않는다.
- **보존:** 시장·상품·퀘스트·NPC·시설 아이디와 저장 데이터는 유지한다. 형식을 바꾸면 옛 저장 이주를 함께 만든다.
- **검사:** 실행하지 않은 검사를 통과했다고 쓰지 않는다. 정적 검사, Unity 씬 검사, 실제 이동, 사람이 해 본 플레이를 구분해서 적는다.
- **팀 통합 전·후 공지(사용자 2026-10-30):** GitHub `integration`에 합치기 전과 후에 카카오톡 등 팀원이 실제로 확인하는 팀 채널로 메시지를 보낸다. PR 댓글·커밋 메시지·Codex 안의 진행 보고로 대신하지 않는다. 사전 공지는 작업 범위와 공용 파일 변경, 완료 공지는 PR/커밋·검사 결과·주의사항을 담는다. 에이전트가 해당 채널/수신자에 연결되어 있지 않으면 사용자에게 전달용 문안을 먼저 제공하고, 실제 사전 공지가 전달되었다는 확인 후 원격 병합한다. 채널·수신자를 임의로 정해 발송하지 않는다.
- **Git:** 로컬 커밋은 해도 된다. push, PR, 공개 빌드는 은샘이 지시하면 바로 한다(지시 없이 먼저 하지 않는다). 팀 합치기용 브랜치는 `region/goryeo`. Git에 올리지 않은 3D 실험 폴더는 지우지 않는다.
- **문서:** 정정 문구를 문서 맨 위에 덧붙이지 않는다. 해당 문서 본문을 고친다. 더 이상 맞지 않는 문서는 `Docs/Archive/`로 옮긴다.
- **글:** 한국어로, 짧고 담백하게 쓴다.
- **보이는 것은 그림으로 확인받는다.** 아트·배치·화면처럼 눈으로 판단할 결정은 글 설명만으로 승인을 요청하지 않는다. 실제 크기 샘플, 나란히 비교한 이미지, 게임 화면 캡처를 함께 보여 준다. 선택지가 있으면 선택지마다 그림을 만든다.

## 환경 요약

- Unity **6000.3.26f1**. 버전을 올리지 않는다.
- 코드: `Assets/PocketCaravan/Scripts/` (namespace `PocketCaravan`). 에디터 도구: `Assets/PocketCaravan/Editor/`.
- 맵: `Tools/MapGen`(Python) → `Assets/PocketCaravan/Maps/*.json` → `Editor/MapJsonBuilder` → `Scenes/`.
- 검사: `python Tools/Integration/validate_world.py`, `python -m unittest discover -s Tools/Integration -p 'test_*.py'` (Pillow 필요).
- Unity 주의사항과 코드 구조는 [코드 규칙](../Reference/CodeConventions.md)에 있다.
- 아트 시안과 배치 검토안: [Figma 팀 파일](https://www.figma.com/design/JeXCjYTYRGtV8FCnXWWiqe/). `아트 시안` 페이지는 팀원 작업이므로 고치지 않는다.

## 작업을 끝낼 때

1. [Plan.md](../Plan.md)의 작업 상태를 고친다.
2. 구현이 바뀌었으면 [CurrentState.md](../CurrentState.md)를 고친다.
3. [WorkLog.md](../WorkLog.md)에 한 줄을 추가한다: 날짜, 에이전트, 작업 ID, 커밋, 결과와 확인하지 못한 것.
4. 검사 결과가 있으면 `Docs/Validation/`에 남긴다.
5. 커밋한다. 사용자의 새 지시가 방향을 바꿨다면 [Direction.md](../Direction.md)도 고친다.
