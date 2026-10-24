# 현재 상태

기준: 2026-10-24, 로컬 `main`. 코드와 파일을 직접 확인해 적었다. 계획이 아니라 **지금 있는 것**만 적는다.
구현이 바뀌면 이 문서를 고친다. 오래된 상세 기록은 `Archive/DevelopmentStatus.md`에 있다.

## 1. 환경

- Unity 6000.3.26f1 (6.3 LTS), URP 2D, Input System.
- 코드: `Assets/PocketCaravan/Scripts/` C# 82개, 약 7,000줄. 에디터 도구: `Assets/PocketCaravan/Editor/`, 약 3,300줄.
- 맵 제작 흐름: `Tools/MapGen`(Python) → `Assets/PocketCaravan/Maps/*.json` → `Editor/MapJsonBuilder` → `Scenes/`. 씬만 고치면 다음 생성 때 사라진다.
- 시작 씬: `Scenes/Goryeo/Naru.unity` (`World/settings.json`의 `startMapId`).

## 2. 맵 (새 방향과 다름 — [세계 배치](WorldLayout.md) 5절)

등록된 본편 맵은 11개, 45,000칸이다.

| 지역 | 맵 | 크기 |
|---|---|---|
| 고려 | 송악 60×60, 개경 70×60, 나루 80×60, 장단 80×55, 닥목 50×60 | 20,000칸 |
| 중앙 동쪽 | transit_pass, woodtown, wetroad, ferry, central_approach, crossroads | 25,000칸 |

- 지형 원본: `Tools/MapGen/broad_regions.py`. 마을 배치는 `maps.py`, `gaegyeong.py`, `goryeo_villages.py`.
- 옛 고려 씬 8개(Route01, goryeo_descent 등)는 폴더에 남아 있지만 등록에서 빠졌다. 옛 저장 위치는 `Resources/GoryeoLayout.json`으로 새 맵에 옮긴다.
- 마지막 실제 이동 검사: 920개 항목 통과 (`Validation/BroadGoryeo-Motor-2026-10-24.json`).

## 3. 아트 (새 방향과 다름 — [아트 기준](ArtGuide.md) 5절)

- 그림 출처 세 가지가 섞여 있다: Ninja Adventure 팩 타일, AI 시트를 줄여 만든 고려 건물·자연, 코드로 그린 16×24 캐릭터.
- 16px 타일, PPU 16, Point 필터. 카메라 기준 480×270.
- 캐릭터: 너구리(플레이어), 촌장, 객주, 상인, 도적, 마을 주민(곰·개·여우·토끼·너구리) 등.
- 현재 동물 NPC는 새 확정 기준(플레이어는 2.5등신 동물, NPC는 사람)과 다르다. C1 교체 대상이며, 아직 에셋·코드를 바꾸지 않았다.

### A2 에셋 목록·배치 검사 도구 (2026-10-24)

- 형식: `Tools/MapGen/ASSET_MANIFEST.md`. 구현: `Tools/MapGen/asset_manifest.py`, 실행: `Tools/Integration/validate_assets.py`.
- 목록·출처·사용자 승인 기록, 조립 세트 필수 조각, 온전한 PNG/렌더링 타일 일치, 최종 맵의 배치 완전성·등록 여부·용도·명시적 지형/길 규칙을 검사한다. 미정 D2/D3 규격은 선택 프로파일이며 도구가 값을 정하지 않는다.
- 정적/PNG/CLI 테스트: A2 26개 포함 전체 71개 통과. `Docs/Validation/A2-AssetManifest-2026-10-24.json`.
- 기존 본편 맵은 새 목록/배치 메타데이터로 이관하지 않았다. 새 검사기는 기존 맵을 새 규칙 통과로 인정하지 않으며, 본편 생성기의 새 형식 연결은 B2에서 한다.
- Claude의 Direction 2절 규칙 교차 검토는 대기다. 미술 판정·사용자 아트 승인·Unity 씬·실제 플레이는 이번 검사 범위가 아니다.

## 4. 게임 시스템 (유지 대상)

| 기능 | 위치 |
|---|---|
| 이동, 달리기 체력, 짐 무게·노면에 따른 감속, 흔들림 연출 | `Scripts/Player/` (`PlayerController`, `CarryVisual`) |
| 시세(재고 기반), 사고팔기, 수량 거래, 교역 장부(L) | `Scripts/Economy/` (`MarketMath`, `MarketState`, `TownMarket`, `TradeLedger`) |
| 배달 의뢰 게시판(마을당 3개, 기한·신용) | `Scripts/Contracts/` |
| 퀘스트(GameDatabase 등록 12개), 대화창 | `Scripts/Quests/`, `UI/DialogueUI` |
| 마을 시설: 쉼터·길 안내소·개경 보관소(신용 조건) | `Economy/TownFacility`, `UI/TownFacilitiesUI` |
| 마을 등급 성장 | `MarketState.Level`, `World/TownGrowthProp` |
| 도적, 길 위 사건, NPC 상인 시뮬레이션 | `Scripts/World/` |
| 게임 시계, 낮밤, 장날(5일마다 **모든 마을**에서 수요 상승) | `Core/GameClock`, `Economy/MarketState` |
| 나루 낚시(짧은 놓아주기 놀이, 보상 없음) | `World/GoryeoPastime` |
| 길 장애물 48종(통나무·범람·산적 등). 별도 시험장에만 있고 본편에는 없음 | `Scripts/Obstacles/`, `Docs/Reference/ObstacleLibrary.md` |
| 저장(로컬 JSON, 옛 저장 이주, 원본 보호) | `Core/SaveSystem`, `Core/GameManager` |
| 지도(M), 상업 백과(Esc) | `UI/WorldMapUI`, `UI/CommerceGuideUI` |
| 소리 | `Audio/AudioManager` |

- **온라인 기능은 없다.** 패키지 목록에 Multiplayer Center는 있지만, 실제 이동·공유 거래를 구현하는 네트워크 패키지·게임 코드는 없다.
- 출신은 `GameDatabase.startOrigin`으로 정해지고 저장 파일에는 출신 ID가 없다. 아라비아 적재 보너스는 미적용이며, 기존 장거리 판매 +5%와 베네치아 재고 증가 0.8 규칙이 남아 있다. `Reference/ImplementationBacklog.md` ORG-01의 미완료 사항이다.
- 별도 실험장: 이동 시험장, 환경 원형 라이브러리 40종, 장애물 48종. 본편에는 배치하지 않았다.

## 5. 검사

```sh
python Tools/Integration/validate_world.py
python -m unittest discover -s Tools/Integration -p 'test_*.py'   # Pillow 필요
```

Unity 안 검사(에디터 메뉴 / MCP): `SceneTerrainChecks`, `CentralConnectionChecks`, `Pocket Caravan > Tests` 메뉴.
이 검사들은 현재 맵 기준이다. 맵을 다시 만들면 새 배치에 맞게 고쳐야 한다(작업 B5).

## 6. Git과 로컬 파일

- 원격: https://github.com/hundredman/pocket-caravan. 로컬 `main`이 원격보다 앞서 있다. push하지 않았다.
- Git에 넣지 않는 로컬 파일: `Builds/`, `output/`(옛 검사 결과), `Tools/MapGen/preview_*.png`.
- Git에 올리지 않은 실험: `Assets/Caravan3DPrototype/`, `Assets/GoryeoJourneyPrototype/`, `Tools/Prototype3D/`. 보존만 한다.
- 다른 팀 작업: 북유럽 Draft PR #2, 아라비아 PR #22. 병합하거나 고치지 않는다.
