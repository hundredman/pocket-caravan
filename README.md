# Pocket Caravan

대항해시대 이전의 상인이 되어 짐을 싣고, 길을 골라 나르고, 돈을 버는 2D 픽셀 교역 게임 (게임개발 2026-2 팀 프로젝트).

## 시작하기

1. Unity Hub에서 **6000.3.26f1 (Unity 6.3 LTS)** 를 설치한다. 팀 전원이 이 버전으로 맞춘다.
2. 저장소를 받아 Unity Hub → Add → 이 폴더를 연다.
3. `Assets/PocketCaravan/Scenes/Goryeo/Naru.unity`(예성 나루 마을)를 열고 Play.
   데이터나 맵을 다시 만들 때는 메뉴 **Pocket Caravan > 1. 시작 세트 만들기**.

조작: 방향키/WASD 이동 · Shift 달리기 · Space 말 걸기·확인 · Q 퀘스트 목록 · M 지도 · Esc 메뉴(저장·소리·처음부터)

## 지금 들어 있는 것 (스타팅 프로토타입)

| 기능 | 위치 |
|---|---|
| 자유 이동, 짐 무게·길 등급·달리기에 따른 속도, 달리기 체력 | `Scripts/Player/` |
| 짐 흔들림: 달리기·길 없는 곳·돌부리에서 차오르고, 가득 차면 짐이 떨어짐 (다시 줍기) | `PlayerController`, `World/DroppedCargo` |
| 맵 이동 (가장자리에서 이어서 다음 맵으로, 길 맵은 시간이 흐름) | `Scripts/Map/` |
| 퀘스트 (! · ? 표시, 길 안내 화살표, Q 목록), 튜토리얼 5개(배달 → 심부름 장보기 → 초보용 인삼으로 첫 차익 → 먼 배달 → 손수레) + 서브 3개 | `Scripts/Quests/` |
| 배달 의뢰 게시판 (마을마다 3개, 기한·짐 상태·보수·신용, 부분 성공) | `Scripts/Contracts/` |
| 재고 기반 시세, 사고팔기, 손수레, 마을 등급 1~3 (커지면 노점 짐이 늘어남) | `Scripts/Economy/`, `World/TownGrowthProp` |
| 도적 (예성 길, 밤에 잦음: 달려서 따돌리거나 통행료) | `World/Bandit`, `BanditSpot` |
| 돌발 사건: 쓰러진 행인(신용 +1), 떠돌이 상인(싸게 팜) | `World/RoadEvent` |
| NPC 상인: 같은 규칙으로 교역해 시세와 마을 발전을 움직임, 길에서 소문을 들려줌 | `World/NpcTraders`, `Walker` |
| 게임 시계 (하루 = 현실 6분, 5일마다 장날), 낮·밤 빛 | `Scripts/Core/GameClock` |
| 상태 창, 전체 지도(M), 메뉴(Esc), 자동 저장·불러오기 | `Scripts/UI/`, `Core/SaveSystem` |
| 배경 음악·효과음 (Ninja Adventure, CC0) | `Audio/AudioManager`, `Resources/Audio` |
| 맵 4개: 예성 나루 마을(시작) - 예성 길 - 개경(수도) - 닥나무 마을 | `Tools/MapGen` → `Maps/*.json` → `Editor/MapJsonBuilder` |
| 캐릭터 12종(16×24 동물)·물건 아이콘: 글자 그림으로 생성 | `Tools/ArtGen/chars.py`, `items.py` |
| 땅·건물 타일, 물결·꽃 애니메이션: Ninja Adventure 에셋 팩 (CC0). UI 폰트: 갈무리 (OFL) | `Art/NinjaAdventure`, `Resources/Fonts` |

저장은 시작 맵(Naru)에서 Play할 때만 불러온다. 다른 씬에서 바로 Play하면 저장을 건드리지 않는다.

UI는 임시(OnGUI)다. 정식 UI는 `MarketState`, `PlayerState` 같은 데이터는 그대로 두고 화면만 바꾸면 된다.

## 폴더 구조

```
Assets/PocketCaravan/
  Art/Placeholder   임시 PNG. 같은 이름으로 덮어쓰면 실제 아트로 바뀜
  Art/Tiles         타일 에셋 (Tile Palette에 끌어다 쓰기)
  Data/<지역>        ScriptableObject 데이터
  Resources/        GameDatabase (시작 설정, 밸런스 값)
  Scenes/<지역>      맵 하나 = 씬 하나
  Scripts/          게임 코드 (namespace PocketCaravan)
  Editor/           에디터 도구 (임포트 설정, 시작 세트 생성)
```

## 새 지역·맵 만들기

지역마다 다른 것은 코드가 아니라 데이터로 둔다.

1. `Data/<지역>/`에 Region, Town, Item 에셋을 만든다 (우클릭 > Create > Pocket Caravan).
2. 맵은 `Tools/MapGen`의 파이썬 코드로 설계한다 (`Tools/MapGen/README.md`). `python3 export.py` → Unity 메뉴 2.
3. 맵 좌표는 `MapJsonBuilder.MapCoords`, 마을 좌표는 `TownData.worldCoord`. 마을 사이 거리(시세)는 이 좌표로 잰다.
4. 출구의 `target`(씬 이름)과 `spawn`, 도착 자리 `id`를 맞춘다.

씬에서 손으로 칠해도 되지만, 메뉴 2를 다시 돌리면 덮어쓰니 그 맵은 JSON 쪽을 고치거나 메뉴를 돌리지 않는다.

출신 강점은 `OriginData`의 숫자로 조절한다.

길은 플레이어가 만들지 않는다. 맵을 만들 때 길 등급(오솔길·길·큰길)을 정한다.

## 퀘스트 만들기

1. 우클릭 > Create > Pocket Caravan > Quest. `giverNpc`(주는 NPC id), 선행 퀘스트, 대사, 단계, 보상을 채운다.
2. 단계 종류: 말 걸기, 물건 전하기, 사기, 팔기, 이익 남기기, 맵 가기, 이동 수단 사기, 돈 모으기.
3. `Resources/GameDatabase`의 quests 목록에 넣는다.
4. 씬의 NPC에 `QuestNpc`를 붙이고 `npcId`를 맞춘다.

## 협업 규칙

- **씬은 한 사람만 고친다.** 씬 파일은 합치기(merge)가 거의 불가능하다. 작업 전에 팀 채팅에 "Route01 작업 중"처럼 알린다.
- 공용으로 쓰는 오브젝트는 프리팹으로 만들어 씬 충돌을 줄인다.
- 브랜치: `main`은 항상 실행되는 상태로 둔다. 기능은 `feature/이름` 브랜치에서 작업하고 PR로 합친다.
- 커밋 전 Console에 에러가 없는지 확인한다.
- Unity 버전을 올리지 않는다 (`ProjectSettings/ProjectVersion.txt`).

## 기획 문서

기획서: "Pocket Caravan — 출신 상인과 성장 구조 계획" (확정 사항, 4주 개발 계획 포함)
