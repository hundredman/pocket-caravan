# Pocket Caravan — 작업 안내 (Claude용)

게임개발(2026-2) 팀 프로젝트. 2D 탑다운 픽셀 교역 게임. 4명 × 4주.
초기 기획서: 저장소 밖 `팀 프로젝트/기획 문서/GuildAndCargo_출신상인_성장구조계획.pdf`.
최신 요구는 `Docs/GameDesign.md`, 온라인 설계는 `Docs/MultiplayerPlan.md`, 일정·통과 기준·결정 목록은 `Docs/DeliveryPlan.md`를 먼저 확인한다. `Docs/DevelopmentReview.md`는 구현 차이와 검토 기록이다. 아래 구조 설명은 현재 로컬 2D 구현 기준이다.

## 현재 작업 범위
- 사용자 목표: 4주 내 4개 스타팅 지역과 여러 십자 연결 구역으로 MVP 구성. 최종 세계는 여러 마을·도시와 분기 교역로가 있는 대륙으로 확장
- 사용자 확인: 기본 시스템 구현 선행 → 4주 최종 2~4명 실제 멀티플레이. 추가 인원은 별도 검증. 단순 이동 동기화만으로 공유 교역/영속 저장 완료라고 기록하지 않는다
- 다른 문명은 여러 중간 구역을 거쳐 도착한다. 고려 옆에 서역을 바로 배치하는 축약은 목표 여정을 검증하지 못한다
- 고려 담당 범위: 수도와 주변 마을. 현재 본편 시작은 Naru이며, 시작 위치 변경은 팀 결정 후 데이터·빌더·튜토리얼·저장까지 함께 반영
- 동물 캐릭터 설정을 유지한다. 이번 계획은 2D 탑다운 우선으로 품질·몰입·재미를 개선한다. 이전 3인칭 실험은 보존하며 전면 전환을 이번 일정에 병행하지 않는 기준안이다. 팀의 최종 아트 승인은 별도
- 사용자는 기존 픽셀을 3인칭으로 적용하는 방향에 부정적이다. 픽셀 캐릭터만 3D 배경에 얹은 실험은 전체 아트 스타일 비교로 제시하지 않는다
- 현재 아트 프로토타입 범위는 고려풍 마을 하나에서 걷기만. 교역·퀘스트·날씨·네트워크 등 시스템을 추가하지 않는다
- 별도 시스템 시험장: `Assets/CaravanMovementLab/Scenes/MovementLab.unity`. 아트 실험과 구분한다. 튜토리얼 없이 수단·화물·체력·흔들림을 선택하고 지형별 경로를 비교한다. `GameManager.EnterTestSession`으로 본편 저장·시뮬레이션을 분리
- 3인칭 실험 시 전후진 + 좌우 회전, 뒤따르는 3인칭 카메라. 좌우 옆걸음이나 쿼터뷰 자동 전환으로 대체하지 않는다
- 대규모 접속은 미검증. 네트워크 구현·목표 동시 접속 수·공유 시세 정책 없이 지원 가능하다고 기록하지 않는다
- 풀밭 낙하가 잦아 길 이용 유도에 쓸 만하다는 사용자 관찰이 있다. 현재는 누적 흔들림+달리기 무작위 충격이다. 확률 수치나 재미 검증 완료로 해석하지 않는다
- 온라인 전환 전 개인 메뉴의 시계 정지와 맵 진입의 `Clock.Advance`를 서버 세계 시간에서 분리한다. 시장·화물·낙하·보상은 서버 명령/중복 방지/원자적 저장 기준을 따른다. 시험장 무료 재화/리셋은 온라인 권한으로 노출하지 않는다

## 환경
- Unity 6000.3.26f1 (6.3 LTS), Universal 2D (URP 2D Renderer), Input System
- 16px 타일, PPU 16, Point 필터, 압축 없음 (`Editor/PixelArtImporter`가 Art 폴더에 자동 적용)
- 카메라: Pixel Perfect Camera, 기준 해상도 384×216
- 글은 한국어, 짧고 담백하게
- 대화 본문은 기본 약 3줄. 짧은 문장 1~2개씩 쓰고, 퀘스트 제목·줄바꿈도 공간을 차지함을 고려한다. `DialogueUI`는 실제 글꼴·너비로 자동 페이지 분할하고 선택지는 마지막 페이지에서만 연다. 본문은 리치 태그 없는 일반 텍스트
- HUD에 일수·시각을 다시 추가하지 않는다. 내부 시계·낮밤·장날·의뢰 기한 규칙은 표시 제거와 별개로 유지

## 구조
- namespace `PocketCaravan`, 코드 `Assets/PocketCaravan/Scripts/`
- `GameManager`는 첫 씬 전에 자동 생성(RuntimeInitializeOnLoadMethod). 씬에 넣지 않는다
- 맵 하나 = 씬 하나. `MapRoot`(Grid에 붙음): Ground/Road/Blocked 타일맵. Blocked에 타일이 있으면 못 지나감
- 출구 `MapExit` → `GameManager.TravelTo(scene, spawnId)` → 도착 씬의 `SpawnPoint(id)`
- 시세: `MarketMath`(공식) + `MarketState`(마을별 실행 상태). 데이터는 `TownData`, `ItemData`
- 장부: `PlayerState.ledger`의 `TradeLedger`에 방문한 장터 시세만 기록하고 저장한다. `TradeLedgerUI`(L)는 미방문지를 미확인으로 표시한다. 외지 가격은 마지막 확인값이며 원격 실시간 조회/수익 보장이 아니다. `ledgerStaleMultiplier`는 미적용
- 생활 연출: `WorkingNpc`는 두 작업 지점을 오가며 짐을 들고 멈춘다. `MapAmbience`는 거리와 음소거에 따라 주변음만 조절한다. 두 기능은 생산/공유 시장 상태를 바꾸지 않는다
- 길: 맵에 미리 칠한 Road 타일의 등급만 읽는다 (`RoadGrid`). 플레이어가 길을 만드는 기능은 넣지 않는다 (MMORPG에서 맵이 망가짐)
- 퀘스트: `QuestData`(단계 목록) + `QuestManager`(GameEvents를 듣고 진행) + `QuestNpc`(!/? 표시). 대화창 `DialogueUI`, 진행 표시 `QuestTracker`
- 배달 의뢰: `ContractManager`(마을 게시판, 3개씩, 4시간마다 보충) + `ContractBoard`(게시판 창). 성공 신용 +1, 실패 −1
- 운반: `PlayerController`에 체력(달리기)·흔들림(가득 차면 짐이 떨어짐 → `DroppedCargo`로 다시 줍기)
- 위험: `BanditSpot`/`Bandit`(예성 길, 밤에 잦음, 통행료 또는 가장 비싼 짐), `RoadEventDirector`/`RoadEvent`(쓰러진 행인, 떠돌이 상인)
- NPC 상인: `NpcTraders`(시간 계산으로 사고팔아 시세·마을 거래액에 반영) + `Walker`(길을 오가는 모습, 소문)
- 마을 발전: `MarketState.Level`(거래액 기준) → 알림, `TownGrowthProp`(등급이 오르면 노점 짐이 늘어남)
- UI: `HUD`(상태·게이지), `QuestTracker`(퀘스트·의뢰), `WorldMapUI`(M), `PauseMenu`(Esc), 조사는 `Josa`
- 저장: `SaveSystem`(persistentDataPath JSON). 맵을 넘어갈 때·끌 때 자동 저장. 시작 맵(빌드 0번)에서 Play할 때만 불러옴. 다른 씬에서 직접 Play하면 기존 파일 유무와 관계없이 자동 저장을 끔. CARAVAN_PREVIEW 빌드는 별도 preview 저장 파일 사용
- 소리: `AudioManager`/`Sfx` (Resources/Audio, Ninja Adventure CC0)
- 밸런스 값은 `Resources/GameDatabase.asset`
- 맵: `Tools/MapGen`(파이썬, 오토타일·스탬프)으로 설계 → `Maps/*.json` + `Art/NinjaAdventure/Tiles/*.png` → `MapJsonBuilder`가 씬 생성
- M 지도: `Tools/MapGen/cartography.py` → `Resources/WorldMap` 지형 PNG·배치 JSON. 실제 출구로 맵 배치를 맞추며 가격용 `worldCoord`는 바꾸지 않는다
- 나무 묶음은 원자적으로 배치하며 부분 타일만 남기지 않는다. `Tools/MapGen/validate.py`로 길·다리·출구·스폰·상호작용 접근성을 확인한다
- 캐릭터: `Tools/ArtGen/chars.py`(16×24 안에 2등신 동물, 글자 그림 + 색 테두리·명암 뒤처리) → `Art/Characters/*_tall_*.png`, 발밑 그림자 `shadow.png`
- UI 폰트: 갈무리(Galmuri, OFL) `Resources/Fonts`. 창 모양은 `UIStyle.Box`/`UIStyle.Button`
- 후처리: `Settings/PostFX.asset` (빌더가 매번 다시 씀), 꽃잎·구름 그림자는 `AmbientFX`
- 레이어: Ground 0, Deco 1, Objects 5, 캐릭터 10~11, Above 20(나무 윗부분), 퀘스트 표시 25. Blocked 타일맵은 그리지 않음
- 에디터 메뉴 `1. 시작 세트 만들기`: 데이터(코드가 기준, 매번 다시 씀) + 임시 캐릭터 + 맵 씬
- 지역 구성: 예성 나루 마을(시작) → 예성 길 → 개경(수도, 성벽·남문·시전·만월대) → 동문 밖 닥나무 마을

## 주의
- MonoBehaviour·ScriptableObject 클래스는 파일 이름과 클래스 이름이 같아야 한다 (한 파일에 하나)
- 에디터가 뒤에 있으면 Play가 멈춰 있다. MCP로 확인할 때는 일시정지 후 `EditorApplication.Step()`으로 프레임을 넘긴다
- 스크립트를 바꾼 뒤 컴파일이 끝난 것을 확인하고 메뉴를 실행할 것
- "맵 씬 다시 만들기"는 씬에서 손으로 고친 내용을 지운다
- 새 씬을 만든 뒤에 에셋을 불러올 것 (NewScene이 앞서 불러온 에셋을 내린다)

- 본편 `CarryVisual`은 손수레와 적재율별 상자를 표현한다. 시험장은 자체 표시를 사용하므로 이 컴포넌트를 끈다. 수레 물리는 기존 플레이어 운반 모델 그대로다.
- 검토용 Mac 빌드는 `PreviewBuild.Mac`, 본편 네 맵만 포함. 출력은 Builds/macOS이며 Git 소스에는 넣지 않는다.
