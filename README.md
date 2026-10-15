# Pocket Caravan

대항해시대 이전의 상인이 되어 짐을 싣고, 길을 골라 나르고, 돈을 버는 2D 픽셀 교역 게임 (게임개발 2026-2 팀 프로젝트).

현재 본편 구현과 이번 제작 기준안은 **2D 탑다운 픽셀**이다. 3인칭 카툰은 이전 별도 실험으로 보존한다.
**초기 4주 MVP는 기본 시스템 구현 후 2~4명 실제 멀티플레이를 검증한다. 최종 제품 목표는 MMORPG 규모의 다중 접속 교역 세계다.** 2~4명은 최종 정원이 아니며, 장기 인원·지역별 밀집도·장비·비용은 단계적 검증으로 구체화한다. 현재 온라인 기능은 미구현이다.
네 스타팅 지역·장거리 여정·2D 품질·공유 시장·서버 저장의 기준과 계획은 [개발 문서 안내](Docs/README.md)를 참고한다.
원본과 후속 요구의 누락·차이는 [전체 요구 대조표](Docs/RequirementsAudit.md)에서 추적한다. 재현 검사는 비시작 씬에서 Play 후 **Pocket Caravan > Tests > 로컬 의뢰·회수·저장 검사**로 실행한다. 이 검사는 해당 Play를 저장 없는 시험 상태로 바꾼다.

## 시작하기

**실행만 해 볼 팀원:** [Windows·Mac 최신 프로토타입 받기](https://github.com/hundredman/pocket-caravan/releases/tag/goryeo-prototype-2026-10-15). 운영체제에 맞는 ZIP을 전체 압축 해제한다. Mac의 최초 실행 허용과 소스 브랜치 선택은 [팀 확인 안내](Docs/TeamPlaytest.md)를 참고한다.

**개발용 최신 소스:** `codex/goryeo-2d-preview` 브랜치. 아래 Unity 버전과 씬으로 확인한다.

1. Unity Hub에서 **6000.3.26f1 (Unity 6.3 LTS)** 를 설치한다. 팀 전원이 이 버전으로 맞춘다.
2. 저장소를 받아 Unity Hub → Add → 이 폴더를 연다.
3. `Assets/PocketCaravan/Scenes/Goryeo/Naru.unity`(예성 나루 마을)를 열고 Play.
   데이터나 맵을 다시 만들 때는 메뉴 **Pocket Caravan > 1. 시작 세트 만들기**.

조작: 방향키/WASD 이동 · Shift 달리기 · Space 말 걸기·확인 · Q 퀘스트 목록 · M 지도 · L 교역 장부 · Esc 메뉴(상업 백과·저장·소리·처음부터)

원본 시대 배경은 대항해시대 이전, 대략 8~15세기의 상인 문화를 모은 가상 교역 세계다. 고려를 특정 연도의 복원 게임으로 한정하지 않는다. [고려 전체 디자인·고증 검토](Docs/GoryeoDesignReview.md)에서 근거·각색·미구현 요소와 완료 기준을 구분한다. 게임의 **Esc → 상업 백과**에서도 역사적 모티브와 게임 각색을 선택해서 읽을 수 있다.

**시스템 바로 테스트:** Unity 메뉴 **Pocket Caravan > Tests > 이동·운반 시험장 열기** 후 Play.
튜토리얼 없이 1/2로 등짐·손수레, 3~6으로 적재량을 바꾸며 세 지형 경로를 비교한다.
R 재시작, Tab 전체 보기. [시험장 안내](Assets/CaravanMovementLab/README.md). 본편 저장·진행과 분리되어 있다.

## 지금 들어 있는 것 (스타팅 프로토타입)

| 기능 | 위치 |
|---|---|
| 자유 이동, 짐 무게·길 등급·달리기에 따른 속도, 달리기 체력 | `Scripts/Player/` |
| 짐 흔들림: 달리기·길 없는 곳·돌부리에서 차오르고, 가득 차면 짐이 떨어짐 (다시 줍기) | `PlayerController`, `World/DroppedCargo` |
| 맵 이동 (가장자리에서 이어서 다음 맵으로, 길 맵은 시간이 흐름) | `Scripts/Map/` |
| 퀘스트 (! · ? 표시, 길 안내 화살표, Q 목록), 튜토리얼 5개(배달 → 심부름 장보기 → 초보용 인삼으로 첫 차익 → 먼 배달 → 손수레) + 서브 3개 | `Scripts/Quests/` |
| 배달 의뢰 게시판 (마을마다 3개, 기한·짐 상태·보수·신용, 부분 성공) | `Scripts/Contracts/` |
| 재고 기반 시세, 사고팔기, 손수레, 마을 등급 1~3 (커지면 노점 짐이 늘어남) | `Scripts/Economy/`, `World/TownGrowthProp` |
| 품목별 원가·판매 손익과 방문 시세 장부(L), 방문별 판매 정산 | `Economy/TradeLedger`, `TownMarket`, `UI/TradeLedgerUI` |
| 역사적 모티브·게임 각색·여행 힌트를 나눈 선택형 상업 백과 7개 카드 | `UI/CommerceGuideUI`, `Resources/CommerceGuide/Goryeo.json` |
| 나루 짐꾼·닥목 종이 작업장과 작업 왕복 연출, 거리별 물가 주변음 | `World/WorkingNpc`, `Audio/MapAmbience`, `Tools/MapGen` |
| 개경 남문·동문 물류 마당·시전 짐꾼/행상·자유 교역 방향 안내 | `Tools/MapGen/gaegyeong.py`, [프로토타입 확인](Docs/GoryeoPrototypeReview.md) |
| 도적 (예성 길, 밤에 잦음: 달려서 따돌리거나 통행료) | `World/Bandit`, `BanditSpot` |
| 돌발 사건: 쓰러진 행인(신용 +1), 떠돌이 상인(싸게 팜) | `World/RoadEvent` |
| NPC 상인: 시장 가격·재고·거래액을 공유하는 단순 시뮬레이션, 길에서 소문을 들려줌. 별도 자금/적재/위험/의뢰 경쟁은 없음 | `World/NpcTraders`, `Walker` |
| 게임 시계 (하루 = 현실 6분, 5일마다 장날), 낮·밤 빛 | `Scripts/Core/GameClock` |
| 상태 창, 전체 지도(M), 메뉴(Esc), 자동 저장·불러오기 | `Scripts/UI/`, `Core/SaveSystem` |
| 배경 음악·효과음 (Ninja Adventure, CC0) | `Audio/AudioManager`, `Resources/Audio` |
| 맵 4개: 예성 나루 마을(시작) - 예성 길 - 개경(수도) - 닥나무 마을 | `Tools/MapGen` → `Maps/*.json` → `Editor/MapJsonBuilder` |
| 캐릭터 12종(16×24 동물)·물건 아이콘: 글자 그림으로 생성 | `Tools/ArtGen/chars.py`, `items.py` |
| 땅·건물 타일, 물결·꽃 애니메이션: Ninja Adventure 에셋 팩 (CC0). UI 폰트: 갈무리 (OFL) | `Art/NinjaAdventure`, `Resources/Fonts` |

기존 저장 불러오기는 빌드 0번 씬(현재 Naru)에서 시작할 때만 적용된다.
다른 씬에서 바로 Play하면 기존 파일 유무와 관계없이 불러오기·자동 저장을 끈다. 본편 진행을 이어가려면 Naru에서 시작한다.

UI는 임시(OnGUI)다. 정식 UI는 `MarketState`, `PlayerState` 같은 데이터는 그대로 두고 화면만 바꾸면 된다.

2026-10-15 유지보수: 나무 조각 배치를 제거하고 좁은 길 연결, 선착장·다리 그래픽을 개선했다.
M 지도는 실제 맵 배치를 축약한 지형 약도로 강·숲·길·건물·내 위치·의뢰 목적지를 표시한다.
지도 원본은 `Tools/MapGen/cartography.py`에서 맵과 함께 생성한다. 이 변경은 2D 방향 확정과 별개다.

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
3. 맵 좌표는 `MapJsonBuilder.MapCoords`, 마을 좌표는 `TownData.worldCoord`. 현재 시세·의뢰 거리는 좌표의 맨해튼 거리이며 실제 연결 경로를 세지 않는다. 우회로·분기 확장 시 거리 규칙을 별도로 정해야 한다.
4. 출구의 `target`(씬 이름)과 `spawn`, 도착 자리 `id`를 맞춘다.

씬에서 손으로 칠해도 되지만, 메뉴 2를 다시 돌리면 덮어쓰니 그 맵은 JSON 쪽을 고치거나 메뉴를 돌리지 않는다.

출신 강점은 `OriginData`의 숫자로 조절한다. 현재 시작 출신은 고려로 고정되며, 네 출신 에셋이 있다고 네 스타팅 지역과 선택 기능이 구현된 것은 아니다. 공통 교역 장부(L)는 방문한 장터의 마지막 확인 시세를 저장한다. 고려 출신의 `ledgerStaleMultiplier` 특혜는 아직 적용하지 않는다.

현재 길은 맵 제작 때 등급(오솔길·길·큰길)을 정한다. 원본의 발길 개척은 미구현이며 이번 유지보수 범위 밖이다. 영구 폐기나 고정 길로의 최종 대체는 팀 결정이 필요하다.

## 퀘스트 만들기

1. 우클릭 > Create > Pocket Caravan > Quest. `giverNpc`(주는 NPC id), 선행 퀘스트, 대사, 단계, 보상을 채운다.
2. 단계 종류: 말 걸기, 물건 전하기, 사기, 팔기, 이익 남기기, 맵 가기, 이동 수단 사기, 돈 모으기.
3. `Resources/GameDatabase`의 quests 목록에 넣는다.
4. 씬의 NPC에 `QuestNpc`를 붙이고 `npcId`를 맞춘다.

대사 작성·표시: 기본 대화창 본문은 약 3줄이다. 글자 수만으로 공간을 보장할 수 없으므로 짧은 문장 1~2개씩 작성한다.
제안 대사에는 퀘스트 제목도 포함된다. 긴 일반 텍스트는 실제 글꼴·창 너비로 자동 분할하고 Space/다음 버튼으로 넘긴다.
수락·거절 등 선택지는 마지막 페이지에서만 표시된다. 본문에 리치 텍스트 태그를 넣지 않는다.
NPC `lines` 배열의 다음 항목은 다시 말을 걸 때 나오며, 한 항목 안의 페이지 넘김과 구분한다.

상태 창의 일수·시각 표시는 제거했다. 내부 시간 흐름과 낮밤·장날·의뢰 규칙은 유지한다.

## 협업 규칙

- **씬은 한 사람만 고친다.** 씬 파일은 합치기(merge)가 거의 불가능하다. 작업 전에 팀 채팅에 "Route01 작업 중"처럼 알린다.
- 공용으로 쓰는 오브젝트는 프리팹으로 만들어 씬 충돌을 줄인다.
- 브랜치: `main`은 항상 실행되는 상태로 둔다. 기능은 `feature/이름` 브랜치에서 작업하고 PR로 합친다.
- 커밋 전 Console에 에러가 없는지 확인한다.
- Unity 버전을 올리지 않는다 (`ProjectSettings/ProjectVersion.txt`).

## 기획 문서

초기 기획서: 저장소 밖 `팀 프로젝트/기획 문서/GuildAndCargo_출신상인_성장구조계획.pdf`.
이 PDF의 길 개척·고려만 시연하는 범위 등은 현재 방향과 다르므로, 모든 내용을 최신 확정 사항으로 취급하지 않는다.
최신 기준은 [개발 기준안](Docs/GameDesign.md), [멀티플레이 설계안](Docs/MultiplayerPlan.md), [개발·검증 계획](Docs/DeliveryPlan.md)을 따른다.
구현 차이와 검토 근거는 [개발 검토 기록](Docs/DevelopmentReview.md)에 남긴다. 문서의 제안은 팀 확정·구현 완료와 구분한다.


## macOS 고려 프리뷰 빌드

Unity 메뉴 **Pocket Caravan > Build > macOS 고려 프리뷰**로 `Builds/macOS/PocketCaravan.app`을 만든다. 현재 제공 빌드는 고려 본편 네 맵이며, 나루에서 시작한다. 이전 3D 실험과 이동 시험장은 본편 빌드에 포함하지 않는다.

프리뷰는 `CARAVAN_PREVIEW` 빌드 정의로 `pocket_caravan_preview_save.json`에 저장해 에디터/기존 본편 저장과 분리한다. 빌드 결과는 Git 소스에 넣지 않고 GitHub 프리릴리스의 zip으로 배포한다. 플랫폼 서명·공증과 Windows 빌드는 별도 작업이다. Windows 지원 모듈을 설치한 팀원은 동일 소스에서 Windows 타깃 빌드를 준비해야 한다.

2026-10-15 추가 개선: 예성 길의 등짐 숲길/손수레 큰길 선택, 본편 손수레·적재량 그림, 노면·흔들림 대처 안내. 수레 그림은 운반 상태의 표현이며 별도 수레 물리·충돌체를 구현한 것은 아니다.
