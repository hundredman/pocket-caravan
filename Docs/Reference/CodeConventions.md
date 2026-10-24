# 코드 규칙과 구조

이전 `CLAUDE.md`에 있던 코드 구조와 주의사항을 옮겼다(2026-10-24). 맵 개수, 아트 출처처럼 바뀌는 내용은 [현재 상태](../CurrentState.md)를 본다.

## Unity 주의사항

- MonoBehaviour·ScriptableObject 클래스는 파일 이름과 클래스 이름이 같아야 한다(한 파일에 하나).
- `GameManager`는 첫 씬 전에 자동 생성된다(`RuntimeInitializeOnLoadMethod`). 씬에 넣지 않는다.
- 스크립트를 바꾼 뒤 컴파일이 끝난 것을 확인하고 에디터 메뉴를 실행한다.
- 에디터 창이 뒤에 있으면 Play가 멈춰 있다. MCP로 확인할 때는 일시정지한 뒤 `EditorApplication.Step()`으로 프레임을 넘긴다.
- "맵 씬 다시 만들기"는 씬에서 손으로 고친 내용을 지운다.
- 새 씬을 만든 뒤에 에셋을 불러온다. `NewScene`이 먼저 불러온 에셋을 내린다.
- 에디터 메뉴 `1. 시작 세트 만들기`는 데이터와 공용 목록을 초기화한다. 평소 실행 방법으로 쓰지 않는다.
- 저장 불러오기는 빌드 0번 씬(Naru)에서 Play할 때만 한다. 다른 씬에서 Play하면 불러오기와 자동 저장이 꺼진다. `CARAVAN_PREVIEW` 빌드는 별도 저장 파일을 쓴다.

## 구조

- 맵 하나 = 씬 하나. `MapRoot`(Grid에 붙음) 아래 Ground / Road / Blocked 타일맵. Blocked에 타일이 있으면 지나갈 수 없다(그리지 않음).
- 출구 `MapExit` → `GameManager.TravelTo(scene, spawnId)` → 도착 씬의 `SpawnPoint(id)`.
- 지역·맵 등록: `World/Regions/<region>.json`, `World/settings.json`. 마을 경제 좌표는 `TownData.worldCoord`이며 땅 위치와 따로 관리한다.
- 시세: `MarketMath`(공식) + `MarketState`(마을별 상태). 데이터는 `TownData`, `ItemData`. 밸런스 값은 `Resources/GameDatabase.asset`.
- 장부: `PlayerState.ledger`의 `TradeLedger`는 방문한 장터 시세만 기록한다.
- 퀘스트: `QuestData`(단계 목록) + `QuestManager`(GameEvents를 듣고 진행) + `QuestNpc`(!/? 표시). 대화창 `DialogueUI`, 진행 표시 `QuestTracker`.
- 배달 의뢰: `ContractManager`(마을 게시판, 3개씩, 4시간마다 보충) + `ContractBoard`. 성공 시 신용 +1, 실패 시 −1.
- 운반: `PlayerController`의 달리기 체력과 노면·적재 감속. `CarryVisual`은 흔들림 연출만 한다. `LegacyCargoMigration`은 옛 저장 복원용이다.
- 위험: `BanditSpot`/`Bandit`, `RoadEventDirector`/`RoadEvent`.
- NPC 상인: `NpcTraders`(시간 계산으로 사고팔아 시세에 반영) + `Walker`(길을 오가는 모습).
- 생활 연출: `WorkingNpc`, `MapAmbience`. 시장 상태를 바꾸지 않는다.
- UI: `HUD`, `QuestTracker`, `WorldMapUI`(M), `PauseMenu`(Esc), `TradeLedgerUI`(L), `CommerceGuideUI`. 조사 처리는 `Josa`. 지금 UI는 임시(OnGUI)다.
- 저장: `SaveSystem`(persistentDataPath JSON). 맵 이동 때와 종료 때 자동 저장.
- 소리: `AudioManager` / `Sfx` (`Resources/Audio`).
- 폰트: 갈무리(Galmuri, OFL) `Resources/Fonts`. 창 모양은 `UIStyle.Box` / `UIStyle.Button`.
- 레이어: Ground 0, Deco 1, Objects 5, 캐릭터 10~11, Above 20(나무 윗부분), 퀘스트 표시 25.

## 글쓰기 규칙 (게임 안)

- 대화 본문은 약 3줄. 짧은 문장 1~2개씩 쓴다. 퀘스트 제목과 줄바꿈도 공간을 차지한다.
- `DialogueUI`가 실제 글꼴과 너비로 페이지를 나눈다. 선택지는 마지막 페이지에만 나온다.
- 본문에 리치 텍스트 태그를 넣지 않는다.
- HUD에 일수·시각을 다시 넣지 않는다. 내부 시계·낮밤·장날·의뢰 기한 규칙은 유지한다.

## 협업

- 씬 파일은 한 사람만 고친다. 작업 전에 팀 채팅에 알린다.
- `main`은 항상 실행되는 상태로 둔다. 기능은 브랜치에서 작업하고 PR로 합친다.
- 커밋 전에 Console에 에러가 없는지 확인한다.
