# 팀원·다른 LLM을 위한 인수인계

> **2026-10-24 사용자 요청으로 작업 중단·저장.** 다음 작업은 [최신 재개 메모](Resume-2026-10-24.md)부터 읽는다. 중앙 전체100,000칸 중 담당 동쪽25,000칸이며 고려20,000칸은 별도다. **에셋을 절대로 잘라 쓰지 말고 필요하면 새로 제작한다.** 현재 크롭 구현은 교체 대상이며 최신 배치/씬 검증은 미완료다.


2026-10-24 최신: [고려 다섯 넓은 권역 개편](BroadGoryeoRegions.md). 아래19씬/13고려구역 기록은 이전 시점이다. 현재 고려5+중앙6씬, 총45,000칸.
갱신: 2026-10-23. **대화 기록 없이도 이 저장소에서 작업을 이어가기 위한 시작점**이다. 특정 모델·이 컴퓨터의 경로·현재 MCP 연결에 의존하지 않는다. 아래 구현 수치는 기준 시점의 스냅샷이며, 새 작업 전 실제 브랜치/차이/PR을 다시 확인한다.

## 사용량에 따른 작업 중단·재개

2026-10-23 로컬 체크포인트 `25f62ff`로 저장한 뒤 사용자 요청으로 작업을 재개했다. **다음 작업은 [재개 메모](Resume-2026-10-23.md)부터 읽는다.** 최종 변경·검증 시점·미확인 화면·남은 품질 작업을 기록했다. 원격 업로드는 하지 않았다.

## 10/23 회의 후 최신 수정본

먼저 [고려 아트/면적 개편](GoryeoArtRenewal.md)과 [회의 확인](Meetings/2026-10-23.md)을 읽는다. 아래 기준 스냅샷의80×64/97,280칸은 이전 버전이다. 최신 생성 파이프라인은 기존 연속 지형→`compact.py`/`area_budget.json`의20,000+25,000칸→`goryeo_art.py`의 전용 픽셀 아트다. `capital()`이 개경의 최종 배치다. 원격 업로드/빌드 상태와 이번 검증은 개편 문서 및 검증 기록을 따른다.

## 1. 처음 10분

1. 저장소 루트 `AGENTS.md` → [확정 규칙](ProjectRules.md)을 읽는다. AGENTS.md를 자동으로 읽지 않는 LLM에도 직접 읽도록 지시한다.
2. [현재 진행표](DevelopmentStatus.md) 맨 위의 최신 기록 → [연속 지형 제작](ContinuousTerrain.md)을 읽는다. 오래된 지도/맵 개수/미구현 판정을 최신 상태로 복사하지 않는다.
3. `git status --short`, `git branch --show-current`, `git log -5 --oneline`을 확인한다. 다른 사람이 수정한 파일을 초기화하거나 덮어쓰지 않는다. 필요하면 원격 상태를 확인하되 작업 중인 브랜치를 임의로 교체하지 않는다.
4. 실제 개발/검사 환경을 아래 기준과 대조한다. Unity/MCP가 없으면 가능한 정적 검사와 미실행 검사를 구분한다.
5. 최신 사용자 요청과 [다음 작업](#5-다음-작업과-완료-조건)을 대조해 한 작업 범위를 정한다. 기존 요구를 다시 추정하거나 완료된 시스템을 처음부터 다시 만들지 않는다.

## 2. 기준 스냅샷과 보존할 것

- 저장소: https://github.com/hundredman/pocket-caravan · 공통 기준 `main`.
- 직전 기능 병합: [PR #20](https://github.com/hundredman/pocket-caravan/pull/20), 커밋 `f1b1a908f58d855ad681256daf7246cd4d84053a`. 이번 인수인계는 그 이후의 문서 보완이며 기능 변경은 아니다.
- 2D 탑다운 본편: 고려13+내륙6=19구역(각80×64), 수도1+고려 마을4, 중간 시장2. 지도는 실제 타일의 축약이다. 장기 동서남북/중앙 경유 규칙은 [세계 교역망](WorldTradeNetwork.md).
- 기존 시장/상품/퀘스트ID, 시세용 논리 좌표, 시설/개인 신용 규칙, 저장 재산을 보존한다. 지리 좌표 `terrainOrigin`과 경제 좌표 `worldCoord`는 별개다.
- 신규 주행 화물 낙하는 없고 감속·시각 흔들림을 사용한다. 플레이어 간 거래는 현재 제외한다. 출신 역할과 시대 기준은 [확정 규칙](ProjectRules.md)을 따른다.
- 온라인은 미구현이다. 초기2~4명은 최종 정원이 아니며 장기 목표는 MMORPG다. 현재 씬 전환도 남아 있다.
- 10/23 확인 시 북유럽은 [Draft PR #2](https://github.com/hundredman/pocket-caravan/pull/2), `feature/north`로 미병합이다. 다른 팀의 미통합 산출물을 미개발로 단정하지 않는다. PR 상태는 다시 조회한다. [팀 통합 안내](TeamDevelopment.md)의 지역 담당/공통 파일 소유권을 따른다.

## 3. 수정 위치와 생성 결과

| 할 일 | 먼저 수정/확인할 원본 | 함께 확인할 결과 |
|---|---|---|
| 해안·강·구역 위치·연결 | `Assets/PocketCaravan/World/Terrain/continuous.json`, `Tools/MapGen/continuous.py` | 영향받은 이웃 JSON·씬·지도·경계/스폰 |
| 기존 마을/생활 배치 | `Tools/MapGen/maps.py`, `gaegyeong.py`, `goryeo_villages.py`, `frontier.py` | 공통 지형 컴파일 후 실제 오브젝트/통행 |
| 새 해안/계곡 구역 | `Tools/MapGen/landscape.py` | 마을/시장 수와 혼동하지 않기 |
| 구역 등록/생성 범위 | `World/Regions/*.json`, `World/settings.json` (World는 `Assets/PocketCaravan/World`) | 등록 ID·씬·공용DB 참조. atlasRegions 외 담당 지역 보존 |
| 지도 표시 | `Tools/MapGen/cartography.py`, `Scripts/UI/WorldMapUI.cs` | 실제 타일·현재 위치·표시 가독성. 지도만 지형을 덧그리지 않기 |
| 경계 이동 | `Scripts/Map/MapExit.cs`, `SpawnPoint.cs`, `Scripts/Player/PlayerController.cs` | 왕복 위치·방향·모서리 연속 전환·수레 폭 |
| 저장 위치/호환 | `Scripts/Core/SaveSystem.cs`, `GameManager.cs`, `Scripts/Map/WorldIndex.cs`, `Editor/MapJsonBuilder.cs` | 이전 저장·신규 저장·원본 보존. legacyHeights를 새 맵 높이로 덮어쓰지 않기 |
| 경제/의뢰/시설 | `Assets/PocketCaravan/Data/Goryeo`, 공용 Scripts와 GameDatabase | 관련 기능 명세·저장·재산 검사. 지리 수정과 수치 개편을 섞지 않기 |

표의 Scripts/Editor 상대 경로 기준은 `Assets/PocketCaravan/`다. 원본→export JSON/타일/지도→Unity 선택 씬 생성 순서다. 생성된 씬/JSON만 고치면 다음 생성 때 사라진다. 수작업 씬 변경은 원본 반영 또는 별도 보존 여부부터 확인한다. 공용 전체 재생성·‘시작 세트 만들기’를 일반 실행 방법으로 쓰지 않는다.

## 4. 재현과 검사

### 소스를 받아 실행

Unity **6000.3.26f1**과 저장소의 패키지 잠금 파일을 사용한다. `Assets/PocketCaravan/Scenes/Goryeo/Naru.unity`를 열고 Play한다. 이미 생성된 맵으로 시작할 수 있어 원본 에셋 팩이나 export가 첫 실행의 필수 조건은 아니다. 지도 M에서 실제 해안과 위치를 대조한다. 중앙 비교는 메뉴 `Pocket Caravan > 중앙 연결 > 내륙 교역 길목 열기`에서 시작하며 본편 저장과 분리된 비시작 Play다.

### Python 정적 통합 검사

Python3.10 이상. 저장소 루트에서 실행한다. 전역 환경을 바꾸기 어려우면 별도 가상환경을 사용한다.

```sh
python -m pip install "Pillow>=10,<13"
python Tools/Integration/validate_world.py
python -m unittest discover -s Tools/Integration -p 'test_*.py'
```

`validate_world.py` 자체는 표준 라이브러리로 동작한다. 전체 통합 테스트는 **Pillow가 필요**하지만 Unity와 원본 미술 팩은 필요하지 않다. 최신 CI도 Pillow를 설치한다.

지형 **재생성**에는 Ninja Adventure 원본 팩이 추가로 필요하다. 위치/`NA_PACK` 설정은 [MapGen 안내](../Tools/MapGen/README.md)를 따른다. 원본 팩이 없다고 맵/타일 결과물을 삭제하지 않는다.

```sh
python Tools/MapGen/export.py
python Tools/MapGen/validate.py
```

공유 해안/강 변경은 인접 구역도 영향을 받는다. Unity Play를 멈추고 미저장 씬을 보존한 뒤 `PocketCaravan.EditorTools.MapJsonBuilder.BuildSelected("변경_map_id", "영향받은_이웃_id")`로 해당 등록 씬만 만든다. 출력 PNG/TileAsset/JSON/씬과 각 `.meta`를 함께 전달한다.

### Unity 실제 검사

Unity 연동 도구를 사용할 수 있다면 아래 public Editor API를 호출할 수 있다. MCP 설치 여부/활성 프로젝트/편집·Play 상태는 그 컴퓨터에서 확인한다. 기존 채팅의 연결이 자동으로 이전된다고 가정하지 않는다.

- `SceneTerrainChecks.CheckGeneratedScenes()`: 멈춘 깨끗한 씬에서 실제 등록 씬의 바닥/충돌/노면/스폰 대조. 씬을 열어 검사한 뒤 원래 씬으로 복귀한다.
- `SceneTerrainChecks.CheckSavePositions()`: 메모리의 이전/신규 위치와 저장 payload 보존 검사. 실제 저장 파일을 쓰지 않는다.
- `CentralConnectionChecks.OpenCentral()` → Play 후 초기화 대기 → `CentralConnectionChecks.Begin()`: 활성 퀘스트 없는 비시작 Play에서 실행. 실제 이동 모터를 사용한 경계 왕복·도착 위치·재산/의뢰 보존 검사. 완료/error를 확인한 후 Play 종료.

클래스 전체 이름 접두어는 `PocketCaravan.EditorTools.`다. 검사 보고서는 `output/continuous-terrain/`에 생기지만 **output은 전달 원본이 아니다**. 결과 요약/한계와 필요한 증거를 `Docs/Validation/`에 기록한다. MCP가 없으면 같은 구간을 수동 왕복하고 미실행 자동 검사는 그대로 표시한다.

직전 기능 검증은 정적맵1206·Python40·출구290 오류0, 실제19씬97,280칸, 모터 경계 통과189회/관련 확인1136개다. [검증 원본](Validation/ContinuousTerrain-2026-10-22.json)을 참조하며 이 숫자를 새 변경의 재검증 결과로 재사용하지 않는다. 사람의 재미/분량·Windows 실제 실행·온라인 시험을 대신하지 않는다.

## 5. 다음 작업과 완료 조건

다음은 현재 남은 **제안 우선순위**다. 팀원의 지역 담당과 최신 사용자 요청을 먼저 적용한다. 과거 작업 목록의 ‘고려9맵’ 같은 스냅샷으로 되돌리지 않는다.

| 순서 | 남은 일 | 완료를 판단할 근거 |
|---|---|---|
| 1 | 팀원이 같은 수정본을 실제 실행하고 지형·지도·왕복을 체감 검토 | 소스/빌드 버전, OS, 구간, 재현 단계, 화면, 관찰 기록. Windows 실제 실행 포함 |
| 2 | 고려 구역 확장 이후 비어 보이는 길·반복 왕복·교역 이득/동선 점검 | 나루↔개경, 송악↔닥목 도보/손수레, 남쪽 순환의 실제 플레이 기록. 목표 분량은 측정 전 가설로 유지 |
| 3 | 관찰된 문제에 한해 이정표·환경/생활 배치·가독성을 작은 단위로 보완 | 기존 수도/4마을·해금·저장 유지, 원본/씬/지도 일치와 관련 회귀 확인. 새 마을/전체 시스템 추가를 자동 범위로 삼지 않음 |
| 병행 | 다른 지역 납품/접속 계약 확인 | 담당자 산출물과 공용 코드 변경을 분리하고 선택 통합. 새 세계 지형에 편입하기 전 방위/수계/출구 협의 |
| 별도 단계 | 퀘스트 지역/장 제한, 출신 새 배율, 네트워크 권한/접속 검증 | [작업 목록](ImplementationBacklog.md)·기능 명세의 선행조건 적용. 현재 구현 완료로 기록하지 않음 |

불확실: 정확한11월 초 마감일·공통 기능 담당·최종 아트·최종 출신 배율·장기 접속 인원. 최신 사용자/팀 결정이 생기면 해당 원본 문서와 확정 규칙을 함께 갱신한다.

## 6. 전달 범위와 작업 종료 기록

최신 실행 파일의 소스/플랫폼/공개 여부는 [빌드 현황](BuildStatus.md)이 기준이다. 최신 Continuous Windows/Mac ZIP은 소스 `0bb0175`의 **로컬 파일**이고 공개10/15 Release와 다르다. 다른 컴퓨터에서 파일이 없다면 공유받거나 같은 소스로 빌드해야 한다. `Builds/`, `output/`, 외부 원본 팩은 clone에 들어 있다고 가정하지 않는다.

이 컴퓨터에 있는 `Assets/Caravan3DPrototype/`, `Assets/GoryeoJourneyPrototype/`, `Tools/Prototype3D/`는 10/23 확인 시 Git 미추적 실험물이다. 자동 정리/삭제하거나 다른 사람이 받을 수 있다고 안내하지 않는다. 현재 본편 인수인계에 필수인 것은 아니다.

작업을 마칠 때 다음을 남긴다. 기록만으로 새 구현/검증이 완료됐다고 하지 않는다.

```text
기준 날짜 / 브랜치 / 실제 소스 커밋 / PR:
최신 요청과 이번 수정 범위:
읽은 확정 규칙 / 새 결정·미확정 사항:
수정한 원본 / 생성된 결과 / 공용 파일 영향:
실행한 검사와 결과 / 실행하지 못한 검사:
기존 저장·ID·경제·다른 담당 작업에 대한 영향:
빌드 소스·위치·공개 여부(새 빌드를 만들었을 때):
남은 문제 / 바로 다음 행동 / 필요한 팀 결정:
```

기능 변경은 DevelopmentStatus·해당 명세·Validation, 빌드 변경은 BuildStatus에 갱신한다. 이 문서는 다음 사람이 시작하는 순서/수정 위치/다음 행동이 달라졌을 때 갱신한다. 오래된 검증 기록은 당시 증거로 보존한다.

## 7. 다른 LLM에 전달할 시작 문구

```text
이 저장소의 Pocket Caravan 작업을 이어서 진행해 주세요.
먼저 AGENTS.md, Docs/Handoff.md, Docs/ProjectRules.md,
Docs/DevelopmentStatus.md의 최신 항목과 요청 기능 명세를 읽어 주세요.
현재 브랜치·미커밋 변경·원격/PR 상태를 확인하고 다른 사람 작업을 보존하세요.
대화에 없는 설정/완료 상태는 추정하지 말고 원본과 실제 코드에서 확인하세요.
이번에 할 일: [여기에 구체적인 요청 입력]
수정 원본과 생성 결과를 함께 갱신하고 필요한 검사를 실행하세요.
끝나면 실제 변경·검사·미확인 사항·다음 행동을 문서에 남겨 주세요.
```
