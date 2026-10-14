# Pocket Caravan

대항해시대 이전의 상인이 되어 짐을 싣고, 길을 내고, 돈을 버는 2D 픽셀 교역 게임 (게임개발 2026-2 팀 프로젝트).

## 시작하기

1. Unity Hub에서 **6000.3.26f1 (Unity 6.3 LTS)** 를 설치한다. 팀 전원이 이 버전으로 맞춘다.
2. 저장소를 받아 Unity Hub → Add → 이 폴더를 연다.
3. 처음 한 번 메뉴 **Pocket Caravan > 1. 시작 세트 만들기**를 누른다 (이미 있는 에셋은 건드리지 않음).
4. `Assets/PocketCaravan/Scenes/Goryeo/Gaegyeong.unity`를 열고 Play.

조작: 방향키/WASD 이동 · Shift 달리기 · E 말 걸기(장터)

## 지금 들어 있는 것 (스타팅 프로토타입)

| 기능 | 위치 |
|---|---|
| 칸 단위 이동, 짐 무게·길 등급·달리기에 따른 속도 | `Scripts/Player/` |
| 맵 이동 (출구 칸 → 다음 씬의 도착 자리) | `Scripts/Map/MapExit`, `SpawnPoint` |
| 씬을 넘어 유지되는 상태 (돈, 짐, 시계, 장터, 발길) | `Scripts/Core/GameManager` |
| 게임 시계 (하루 = 현실 6분, 5일마다 장날) | `Scripts/Core/GameClock` |
| 재고 기반 시세, 사고팔기, 손수레 구입, 마을 등급 1~3 | `Scripts/Economy/` |
| 발길로 자라는 길 0→1→2단계, 하루마다 조금씩 줄어듦 | `Scripts/Roads/RoadGrid` |
| 데이터 (물건, 마을, 이동 수단, 출신, 밸런스 값) | `Data/`, `Resources/GameDatabase` |
| 임시 픽셀 아트와 맵 3개 (개경 - 1번 길 - 닥나무 마을) | `Editor/PlaceholderArt`, `Editor/StarterBuilder` |

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
2. 고려 씬 하나를 복사해 타일을 다시 칠한다. 레이어는 Ground(걷는 땅), Road(길), Blocked(못 지나가는 곳).
3. `Map` 오브젝트의 MapRoot에서 `mapId`(고유), `worldCoord`(맵 좌표)를 바꾼다. 마을 사이 거리는 이 좌표로 잰다.
4. 출구(`MapExit`)의 `targetScene`과 `targetSpawnId`, 도착 자리(`SpawnPoint`)의 `id`를 맞춘다.
5. 새 씬을 File > Build Profiles의 씬 목록에 넣는다.

출신 강점은 `OriginData`의 숫자로 조절한다 (바이킹 발길 1.5배 등).

## 협업 규칙

- **씬은 한 사람만 고친다.** 씬 파일은 합치기(merge)가 거의 불가능하다. 작업 전에 팀 채팅에 "Route01 작업 중"처럼 알린다.
- 공용으로 쓰는 오브젝트는 프리팹으로 만들어 씬 충돌을 줄인다.
- 브랜치: `main`은 항상 실행되는 상태로 둔다. 기능은 `feature/이름` 브랜치에서 작업하고 PR로 합친다.
- 커밋 전 Console에 에러가 없는지 확인한다.
- Unity 버전을 올리지 않는다 (`ProjectSettings/ProjectVersion.txt`).

## 기획 문서

기획서: "Pocket Caravan — 출신 상인과 성장 구조 계획" (확정 사항, 4주 개발 계획 포함)
