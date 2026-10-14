# Pocket Caravan — 작업 안내 (Claude용)

게임개발(2026-2) 팀 프로젝트. 2D 탑다운 픽셀 교역 게임. 4명 × 4주.
기획서: claude.ai 문서 "Pocket Caravan — 출신 상인과 성장 구조 계획".

## 환경
- Unity 6000.3.26f1 (6.3 LTS), Universal 2D (URP 2D Renderer), Input System
- 16px 타일, PPU 16, Point 필터, 압축 없음 (`Editor/PixelArtImporter`가 Art 폴더에 자동 적용)
- 카메라: Pixel Perfect Camera, 기준 해상도 384×216
- 글은 한국어, 짧고 담백하게

## 구조
- namespace `PocketCaravan`, 코드 `Assets/PocketCaravan/Scripts/`
- `GameManager`는 첫 씬 전에 자동 생성(RuntimeInitializeOnLoadMethod). 씬에 넣지 않는다
- 맵 하나 = 씬 하나. `MapRoot`(Grid에 붙음): Ground/Road/Blocked 타일맵. Blocked에 타일이 있으면 못 지나감
- 출구 `MapExit` → `GameManager.TravelTo(scene, spawnId)` → 도착 씬의 `SpawnPoint(id)`
- 시세: `MarketMath`(공식) + `MarketState`(마을별 실행 상태). 데이터는 `TownData`, `ItemData`
- 길: 맵에 미리 칠한 Road 타일의 등급만 읽는다 (`RoadGrid`). 플레이어가 길을 만드는 기능은 넣지 않는다 (MMORPG에서 맵이 망가짐)
- 퀘스트: `QuestData`(단계 목록) + `QuestManager`(GameEvents를 듣고 진행) + `QuestNpc`(!/? 표시). 대화창 `DialogueUI`, 진행 표시 `QuestTracker`
- 배달 의뢰: `ContractManager`(마을 게시판, 3개씩, 4시간마다 보충) + `ContractBoard`(게시판 창). 성공 신용 +1, 실패 −1
- 운반: `PlayerController`에 체력(달리기)·흔들림(가득 차면 짐이 떨어짐 → `DroppedCargo`로 다시 줍기)
- 위험: `BanditSpot`/`Bandit`(예성 길, 밤에 잦음, 통행료 또는 가장 비싼 짐), `RoadEventDirector`/`RoadEvent`(쓰러진 행인, 떠돌이 상인)
- NPC 상인: `NpcTraders`(시간 계산으로 사고팔아 시세·마을 거래액에 반영) + `Walker`(길을 오가는 모습, 소문)
- 마을 발전: `MarketState.Level`(거래액 기준) → 알림, `TownGrowthProp`(등급이 오르면 노점 짐이 늘어남)
- UI: `HUD`(상태·게이지), `QuestTracker`(퀘스트·의뢰), `WorldMapUI`(M), `PauseMenu`(Esc), 조사는 `Josa`
- 저장: `SaveSystem`(persistentDataPath JSON). 맵을 넘어갈 때·끌 때 자동 저장. 시작 맵(빌드 0번)에서 Play할 때만 불러옴
- 소리: `AudioManager`/`Sfx` (Resources/Audio, Ninja Adventure CC0)
- 밸런스 값은 `Resources/GameDatabase.asset`
- 맵: `Tools/MapGen`(파이썬, 오토타일·스탬프)으로 설계 → `Maps/*.json` + `Art/NinjaAdventure/Tiles/*.png` → `MapJsonBuilder`가 씬 생성
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
