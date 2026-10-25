# 현재 상태

기준: 2026-10-24, 로컬 `main`. 코드와 파일을 직접 확인해 적었다. 계획이 아니라 **지금 있는 것**만 적는다.
구현이 바뀌면 이 문서를 고친다. 오래된 상세 기록은 `Archive/DevelopmentStatus.md`에 있다.

## 1. 환경

- Unity 6000.3.26f1 (6.3 LTS), URP 2D, Input System.
- 코드: `Assets/PocketCaravan/Scripts/` C# 83개, 약 7,000줄. 에디터 도구: `Assets/PocketCaravan/Editor/`, 약 3,300줄.
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
- C1에서 본편 NPC를 승인된 사람 그림으로 일괄 교체했다. 동물 NPC176프레임/meta는 `Art/Characters/ArchivedAnimals/`에 보관(꾸미기 후보)했고 플레이어 너구리는 유지했다.

### A2 에셋 목록·배치 검사 도구 (2026-10-24)

- 형식: `Tools/MapGen/ASSET_MANIFEST.md`. 구현: `Tools/MapGen/asset_manifest.py`, 실행: `Tools/Integration/validate_assets.py`.
- 목록·출처·사용자 승인 기록, 조립 세트 필수 조각, 온전한 PNG/렌더링 타일 일치, 최종 맵의 배치 완전성·등록 여부·용도·명시적 지형/길 규칙을 검사한다. 미정 D2/D3 규격은 선택 프로파일이며 도구가 값을 정하지 않는다.
- 정적/PNG/CLI 테스트: A2 26개 포함 전체 71개 통과. `Docs/Validation/A2-AssetManifest-2026-10-24.json`.
- 기존 본편 맵은 새 목록/배치 메타데이터로 이관하지 않았다. 새 검사기는 기존 맵을 새 규칙 통과로 인정하지 않으며, 본편 생성기의 새 형식 연결은 B2에서 한다.
- Claude의 Direction 2절 규칙 교차 검토는 대기다. 미술 판정·사용자 아트 승인·Unity 씬·실제 플레이는 이번 검사 범위가 아니다.

### A2b 검사 보완 (2026-10-24)

- `separateFromSame`: 같은 개체·층의 최소 빈 간격 검사. 지형 조립 세트는 moduleType별 최소 역할 조각을 requiredPieces와 실제 pieces 양쪽에서 확인한다.
- 기존 A2 사례를 유지한 전용 32개, 전체 Python 77개·등록 출구 92개 검사 통과. `Validation/A2b-2026-10-24.json`.
- 현재 맵·아트를 새 형식으로 바꾸거나 승인하지 않았다. 실제 이음매·맵 사이 개체 간격·Unity·플레이는 미검증이다.

### A3a 에셋 사용 감사 도구 (2026-10-25)

- `Tools/Audit/audit_assets.py`가 Python/C# 변형 후보와 등록 11맵의 실제 이미지·성장 소품 사용 좌표를 자동 추출한다. `Validation/A3a-2026-10-25/`에 파일/줄/사각형/사용처·변형 종류와 누락 가능성을 기록했다.
- 타일/성장 소품645종·스탬프634개·정적 후보65곳·관찰 호출879회·동일 개체 접촉54쌍. 후보를 에셋 규칙 위반이라고 확정하지 않으며 유지/교체/삭제는 Claude A3b 판정이다.
- Python100개·등록 출구92개 통과, 목록과 저장된 11맵의 모든 지형 사용 좌표 일치·재생성 결과 일치. 본편/아트/맵/씬/저장 미변경. Unity/실제 이동/새 화면 검토는 실행하지 않았다. 나무 가림 C3a는 별도 후속 점검이다.

### A4 연못·호수 조립 시험 (2026-10-25, 호수 미채택·작은 연못 검토 대기)

- 본편 밖 `ArtReview/A4-Water-v1/`:16px 연결47종·물결4프레임, Aseprite2개, A2 목록/배치4개, 비교·실제Unity1920×1080/480×270·물결GIF. 원본은 `Tools/ArtTrials/water_modules_a4.py`와 Lua다.
- A2 오류0, 원본 재열기 일치·32,768쌍 경계 일치, Python105개·출구92개 통과. Unity 개경22셀/호수시험164셀 메모리 표시, 물결5개×4상태 선택·PPU16/Point·Console error0. 개경 발칸(35,24) blocked0/reachable1/Unity보행가능 확인.
- 사용자 피드백: 작은 연못은 개선 평가, 큰 호수는 기존이 더 좋음. 새 호수 시안은 미채택하고 기존 물 조립 타일 표현을 유지한다. 작은 연못도 최종 승인으로 간주하지 않는다. `feedback-comparison.png`/`review-decision.json`에 기록했다.
- 저장false/씬dirtyfalse 확인 후Play종료. 본편/맵/씬/ID/저장 미변경. 아트승인·D3·Unity47형태 전체·실제 이동/충돌·본편 애니메이터 미확인. A4 전체 완료가 아니며 나머지 강/해안/길 등은 미착수. `Validation/A4-Water-v1-2026-10-25.md` 참고.

## 4. 게임 시스템 (유지 대상)

| 기능 | 위치 |
|---|---|
| 이동, 달리기 체력, 짐 무게·노면에 따른 감속, 흔들림 연출 | `Scripts/Player/` (`PlayerController`, `CarryVisual`) |
| 시세(재고 기반), 사고팔기, 수량 거래, 교역 장부(L) | `Scripts/Economy/` (`MarketMath`, `MarketState`, `TownMarket`, `TradeLedger`) |
| 배달 의뢰 게시판(마을당 3개, 기한·신용) | `Scripts/Contracts/` |
| 퀘스트(GameDatabase 등록 17개), 대화창 | `Scripts/Quests/`, `UI/DialogueUI` |
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

### R1 평판 데이터·저장 (2026-10-24)

- `Core/ReputationState`: 캐릭터 소유의 문명/중앙 마을별 평판. 출신 문명 우호, 미기록 대상 중립. 단계와 단계 내 진행점만 저장하고 D10 점수 경계·보상·가격 효과는 미구현이다.
- `PlayerState.reputation`을 새 게임·독립 시험장 초기화와 `GameManager` 저장/복원에 연결했다. 새 실제 저장은 v3, 평판 블록은 schemaVersion 1. v1/v2는 현재 설정 출신으로 한 번 초기화하고, 이후 저장된 고향/평판을 유지한다. 기존 돈·신용·화물·퀘스트 ID는 바꾸지 않는다.
- Unity가 구 JSON의 없는 inline 객체를 빈 객체로 복원하는 경우도 처리한다. 새 저장의 누락/미지원/중복/잘못된 평판은 원본을 덮어쓰지 않도록 검증한다. 유효한 미등록 미래 대상 ID는 보존한다.
- .NET 데이터 45개, 실제 Unity 평판·JSON·임시 파일 36개와 기존 저장 호환 27개 검사 통과. `Editor/ReputationChecks`와 `Tools/Integration/ReputationProbe/README.md` 참고. 검사 기록은 `Validation/R1-*.json`에 있다.
- 거래·의뢰·선행 보상 연결(R2/R6), 회복(R4), UI, 출신 선택, 실제 플레이/이동은 이번 작업 범위가 아니다. Claude 검토는 대기다.

### A6 Aseprite 연결 (2026-10-24, 클라이언트 확인 대기)

- Mac의 Aseprite `1.3.18.6-arm64`를 절대 경로로 실행하고 Lua 기술 시험 PNG·`.aseprite` 생성, 원본 재열기/PNG 재출력을 확인했다. `Tools/Aseprite/smoke.lua` 참고. 게임 아트나 팔레트 승인 자료가 아니다.
- 커뮤니티 MCP 두 후보의 코드를 비교한 뒤 `mattt/aseprite-mcp` 커밋 `0cd6aac4`를 저장소 밖에 고정 설치했다. 선택 이유와 Lua의 파일·명령·외부 전송 위험을 설치 전에 `Tools/Aseprite/MCP_REVIEW.md`와 WorkLog에 기록했다.
- MCP 자체의 도구 두 개 나열, 실제 Lua 실행과 PNG 이미지 응답을 확인했다. upstream 일반 검사 37개·실제 Aseprite 검사 1개 통과. `Docs/Validation/A6-2026-10-24.json` 참고.
- Codex 설정과 사용자 승인 후 Claude 데스크톱 설정에 등록했다. **현재 Codex 대화에는 새 도구가 아직 로드되지 않았으며 두 앱의 실제 도구 목록 확인은 대기**다. Claude 조작 검사는 다음 세션에 한다. 따라서 A6는 진행 중이다. 재연결과 확인 방법은 `Tools/Aseprite/README.md` 참고.

### R2 가격·흥정 수치 계산 (2026-10-25, D10 미적용)

- `Tools/Balance/R2Probe`가 본편 가격/재고/상품·마을·출신 C#을 연결해 미확정 가격 후보를 계산한다. Unity1680기준 일치, 일반8상품·7마을·4출신, 후보5160960조합(기준선 재사용)/1344단가·할인9구간을 `Validation/R2-2026-10-25/`에 남겼다.
- 작은 보정/문서 흥정 예시/매입·판매 양쪽 보정 세 후보에서 동일 장터 왕복 이익 사례가 있었다. 재고·성장 변화/현금·적재 한도 생략 때문에 무한 반복 수익이라고 단정하지 않는다. README에서 Claude·사용자 D10 판단 항목을 구분했다.
- Python109개/출구92개, Unity 읽기/Console error0. 실제Play/거래/저장회귀/빌드는 미실행. 본편·기존ID/저장·R6 점수0 유지, 품목/시설 잠금·단계 점수·최종 가격/흥정률을 정하거나 적용하지 않았다. 수치 계산만 완료이며 R2 구현은 대기다.

### R6 신용·평판 기여 분리 (2026-10-24)

- Standing 2절 결정에 따라 신규 수락 계약성 퀘스트 6개는 신용 +1, 거래 연습·성장 안내·게시판 보고 6개는 신용 0/고려 평판 기여(작음)로 분류했다. 구 저장의 캡처 보상과 미캡처 legacy 약속은 유지하며 이미 쌓인 신용도 바꾸지 않는다.
- `ReputationAwards`와 `GameEvents.ReputationContributed`: 대상·크기 이름·방향·원인만 전달한다. 점수표는 전부 0이고 단계/진행점을 바꾸지 않는다. R2/D10 전 기여를 저장해 나중에 소급 지급하지 않는다.
- 행인 돕기는 명시한 관계 대상에 기여하고 신용을 주지 않는다. 배달 기여는 발주지 한 곳만 대상으로 하며 중앙 교역권은 마을별, 고려는 문명별이다. 성공·부분 납품·실패·기한 종료·분실 경로와 중복 호출을 검사했다.
- Unity 6000.3.26f1 에디터 로직/실제 에셋/JsonUtility 87개, .NET 평판 45개, 기존 퀘스트 진단 10개 관찰, Python 77개·출구 92개 확인. 퀘스트 진단에는 알려진 정의 변경 결함 재현도 포함된다. `Validation/R6-Validation-2026-10-24.json` 참고. 플레이·게임 빌드는 미실행이다.

### Q1 고려 한 바퀴 (2026-10-24)

- 새 ID `q1_naru`→`q1_jangdan`→`q1_songak`→`q1_dakmok`→`q1_gaegyeong` 5개를 등록했다. 옛 `tutorial_1`·`tutorial_2`·`naru_board`·`dakmok_hanji`는 신규 제안만 막고 진행 중이면 옛 목표대로 완료한다. 사이드 5개는 설계의 새 선행으로 연결했다.
- 구 완료 대응은 보상 없이 기록만 추가한다. 이주 전 완료 이력과 옛 활성 퀘스트 표시를 별도 저장하여, 새로 끝낸 사이드 퀘스트가 완료 대응표로 메인을 건너뛰지 않는다. 이미 수락한 새/옛 퀘스트의 진행·보상·화물을 건드리지 않고, 뒤 단계 완료 기록이 있어도 가장 앞의 빈 단계부터 제안한다. 반복 이주·재로드에 보상을 지급하지 않는다.
- 원본/생성기는 `Tools/QuestGen/generate_q1.py`, 시작 데이터 재생성 연계는 `Editor/Q1QuestUpgrade`다. 기존 12개 목표 블록·ID·돈/신용 보상은 R6와 같다.
- 실제 Unity 에디터 로직으로 장단 곡식 3개 매입 15냥→송악 판매 17냥(순익 2냥), 100냥 시작 체인 완료 시 240냥을 확인했다. NPC 상인·시간 흐름·실제 이동에 따라 시세는 달라질 수 있다. 시장/배달은 메인 미수락 상태에서도 동작한다.
- .NET 기존 진단 10관찰+Q1 41개, Unity Q1 25개·R6 회귀 87개·저장 회귀 27개, Python 77개·출구92개 확인. `Validation/Q1-Validation-2026-10-24.json`. 실제 이동/사용자 플레이·게임 빌드는 미실행이다.

### A5 아트 정리 도구 (2026-10-24)

- `Tools/ArtPrep/prepare_art.py`: 크기·팔레트를 명시하고 격자 추정 또는 수동 간격으로 최근접 축소한다. 불확실한 격자·종횡비/전체 크기 불일치는 거절한다. 팔레트 고정·알파 0/255 정리, 원본/팔레트 덮어쓰기 방지와 보고서를 지원한다.
- A5 12개 포함 Python 전체 89개 확인. 기술 시험 전/후는 `Validation/A5-sample-comparison.png`. 도구 개발은 끝났으며 Claude의 샘플 화면 검토와 D3 팔레트 승인은 대기다. 사용자 아트 승인으로 처리하지 않는다.

### C1 사람 NPC 초기 시험 (2026-10-24, 과한 단순화로 반려)

- 본편 밖 `ArtReview/C1/`에 나루 촌장·아낙·행상 3명 시안, 전/후·실루엣 비교, 480×270 화면 크기 합성, 4방향×4프레임·Aseprite 시트 원본을 만들었다. 직접 도트 원본 코드는 `Tools/ArtTrials/npc_sample.py`다.
- 48개 프레임의 16×24·후보 팔레트·알파0/255·발밑 기준, 원본3개 재열기 일치, A2 목록/PNG 미리보기 검사를 확인했다. 초기 시안은 사용자 반려 상태다. 실제 Unity 교체/플레이는 하지 않았다. 기존 동물 그림·본편 Assets/·맵/씬은 바꾸지 않았다.

### C2·C3 기와집·소나무 초기 시험 (2026-10-24, 과한 단순화로 반려)

- `ArtReview/C2-C3/`에 기와집·소나무 각 1개를 직접 도트로 다시 그렸다. 전체 64×48, 기존 비투명 외곽 범위와 발밑 위치 유지. 기와집 28→12색, 소나무 16→8색. 소나무는 요청서의 대략 12~16색보다 적은 색을 쓴 시안이며 단순화 정도는 사용자 검토 대상이다.
- 전/후 같은 배율 비교와 480×270 게임 화면 크기 합성, 온전한 Aseprite 원본·PNG·A2 시험 목록을 제출했다. 원본 재열기·픽셀/알파/후보 팔레트·전체 렌더링 조각 일치 검사 통과. 초기 시안은 사용자 반려 상태이고, 배치·깊이·충돌·실제 플레이는 미검증이다.
- C1과 C2·C3는 본편 밖 시안이다. 최종 Assets/ 내용은 실제 Unity로 검증한 Q1 브랜치와 동일하며 기존 동물 그림·맵·씬·시설/상품/NPC ID는 보존했다.

### 원본 보존 보정 v2 (2026-10-24, 환경 긍정 피드백·사람 재수정)

- 사용자 지시: 비교 왼쪽 기존 게임 아트가 기준. 초기 단순화 시안은 반려했고, 원본의 색·명암·디테일을 보존하는 약한 보정으로 바꿨다. Direction/ArtBrief/ArtGuide에 반영했다.
- `ArtReview/Refine-v2/`: 기와집/나무 원본·아주 약하게·약하게 비교, 기존 캐릭터 렌더러 기반 사람 NPC 정면3명, 480×270 검토용 합성. 기와집 픽셀 98/96%, 나무 99.78/97.26% 유지(불투명 기준). 환경의 모든 알파·실루엣·64×48 크기·원래 색은 유지했다.
- AI 보정 검토안은 형태 변화나 네이티브 크기에서의 손실로 최종본 미채택. 환경은 Aseprite 국소 픽셀 보정, NPC는 기존 캐릭터 코드의 색/음영/몸을 살린 직접 도트다. 실제 7개 원본 재열기 픽셀 일치·A2 목록/PNG 검사 확인. 원본/도구/프롬프트 기록은 해당 폴더 README/PROCESS에 있다.
- NPC v2는 정면 그림체 확인용이며 4방향/걷기는 갱신하지 않았다. 본편·Assets/·맵/씬·R6/Q1 코드는 바꾸지 않았다. 사용자 미술 승인은 대기다.

### 사람 NPC 얼굴 v3 (2026-10-24, 긍정 피드백·선/색 보완)

- 사용자: 동물 캐릭터는 괜찮고 사람 변환이 어색함. 건물·나무 v2는 긍정 피드백이므로 이미지 바이트 그대로 유지하고 사람만 수정했다. `ArtReview/NPC-v3/`에 동물/이전 사람/수정 사람 비교와 480×270 합성을 제출했다.
- 네이티브 16×24에서 얼굴 윤곽·귀·턱, 눈동자/눈 위치, 머리카락, 수염과 손/물동이 접점을 수정했다. 실제 해상도의 3개 PNG/Aseprite 원본·알파0/255·발밑·재열기 일치 및 A2 목록/PNG 검사 확인. 사용자 판정은 대기다.
- 정면 시안만 갱신했다. 걷기·다른 방향·실제 Unity 임포트/플레이는 미검증이고 본편 Assets/·저장/ID/맵/씬은 바꾸지 않았다.

### 사람 NPC 선·색 정리 v4 (2026-10-24, 아낙 정면 스타일 확인)

- 사용자 피드백을 받아 얼굴/소품 위치와 몸 비율을 유지한 채, 윤곽선 색을 재질별로 맞추고 비슷한 중간색을 정리했다. 촌장37→28색, 아낙36→27색, 행상37→26색. `ArtReview/NPC-v4/`에 전/후·480×270 합성·네이티브 PNG/Aseprite 원본을 제출했다.
- 선/색 정리 시 v3의 알파·실루엣을 유지했고, 후속 요청으로 아낙 왼쪽 세로선과 연결 잔여물11픽셀만 제거했다. 추가 요청으로 화면 오른쪽 눈 위의 머리카락1픽셀을 피부색으로 바꿔 눈과 분리했다. 나머지 아낙 픽셀과 촌장/행상은 수정 직전과 동일하다. 16×24·발밑·원본 재열기 일치·A2 목록/PNG 검사를 확인했다. 실제 사용 색 목록은 시안용이며 D3를 확정하지 않는다.
- 정면만 갱신했다. 걷기·다른 방향·Unity/플레이·사용자 미술 승인은 대기이며, 동물·건물·나무 원본과 본편 Assets/·저장/ID/맵/씬은 바꾸지 않았다.

- 사용자가 마지막 아낙 정면에 “좋다. 이런 느낌이야.”라고 확인했다. `ArtReview/Approved/woman-front-v4.*`에 PNG·Aseprite 표본과 해시/범위 기록을 동결했다. 이후 사람 NPC 스타일 기준이며, 나머지 인물/방향·걷기·본편 적용은 완료로 처리하지 않았다.

### C1 확대 1단계 (2026-10-24, 촌장·행상 정면 승인 대기)

- 최신 main `a1ed0f9b`에서 `codex/c1-expand`로 시작했다. `ArtReview/C1-Expand-1/`에 촌장·행상 정면 PNG/Aseprite, 전후/아낙 기준 비교, 480×270 검토 합성을 제출했다. 생성 원본은 `Tools/ArtTrials/npc_expand_c1.py`다.
- 촌장 눈 위 틈·흰 도포 37픽셀, 행상 입 주변 3픽셀을 정리했다. 16×24·알파·실루엣·발밑은 유지했고 Aseprite 재열기 픽셀 일치 및 A2 기술 미리보기 오류0을 확인했다. 아낙 승인본·본편 Assets/·동물 원본은 변경하지 않았다.
- 사용자 요청으로 실제 Unity 장단 씬 Play 모드에 정면 비교를 임시 표시하고 `unity-game-review-1920x1080.png`를 촬영했다. 동물/사람 PPU16·pivot·scale 일치, 저장 꺼짐·씬 변경 저장 없음·Console error0 확인. 정지 외형 비교이며 이동/상호작용/걷기 검사는 아니다.
- 사용자 승인 대기이므로 나머지 7명·4방향/걷기·본편 영구 교체는 아직 진행하지 않았다. 상세는 `Validation/C1-Expand-1-2026-10-24.md`.

### C1 확대 2단계 (2026-10-24, 나머지 7명 정면 승인)

- 사용자 “좋다. 사이즈도 좋은 것 같아. 이어서 진행하자.”로 촌장·행상 정면과 게임 내 크기를 확인했다. `ArtReview/Approved/`에 촌장·행상 PNG/Aseprite·해시를 동결하고 두 목록만 approved로 변경했다.
- `ArtReview/C1-Expand-2/`에 농부·약재상·닥목 촌장·객주·아이·뱃사공·도적 정면 PNG/Aseprite7쌍, 기존 동물/사람/아낙 비교2장과 실제 Unity 장단 Game 화면 전후를 제출했다. 원본은 `Tools/ArtTrials/npc_expand_remaining.py`다.
- 16×24·알파0/255·발밑·원본 재열기 픽셀 일치 및 A2 기술 미리보기 오류0 확인. Unity 정지 외형 비교(PPU16·pivot·scale 일치), Console error0 확인. PNG/씬은 저장하지 않았고 실제 이동/걷기는 미검증이다.
- 사용자 “좋아. 이대로 가자.”로 새7명 정면을 승인했고 PNG/Aseprite·해시를 동결했다. 이후 4방향·걷기·본편 교체는 별도다. 요청서의 주민 역할과 현행 주민 직업5종 연결은 본편 교체 전에 Claude 검토가 필요하다. 이번에 이름/ID/대사/스프라이트 참조를 바꾸지 않았다. `Validation/C1-Expand-2-2026-10-24.md` 참고.

### C1 4방향·걷기 (2026-10-24, 미술 승인·본편 역할 배정 검토 대기)

- `ArtReview/C1-Walk/`에 10명×4방향×4프레임(160 PNG), 태그·125ms 시간을 가진 Aseprite10개, 방향 비교2장·걷기 GIF·실제 Unity Game16캡처/GIF를 만들었다. 원본은 `Tools/ArtTrials/npc_animate_c1.py/.lua`다.
- 승인 정면과 아래0/2 프레임이 픽셀 일치하며 나머지도 인물별 승인 정면의 실제 색만 쓴다. 크기16×24·알파0/255·발밑·원본 재열기 시트/태그/시간 및 A2 미리보기 오류0 확인.
- 실제 Unity `CharacterAnimator`에 임시로 연결해 방향별 선택160건·반복/정지80건 일치, Console error0 확인. 제자리 걷기 검토이며 실제 경로 이동/충돌·상호작용은 미검증이다. PNG/씬/저장을 변경하지 않았고 Play 종료 시 임시 표시가 사라진다.
- 사용자 옆머리 수정 요청으로 관자놀이·뒤통수 면을 연결했다. 좌우80프레임만 변경, 앞뒤80프레임·전체 알파/실루엣 동일. `ArtReview/C1-SideHair/`에 수정 전후와 검사 기록, C1-Walk의 원본/걷기/실제 Unity GIF를 갱신했다. 기술 검사 및 Unity 선택/전환240건 재확인.
- 사용자 “훨씬 낫다. 이대로 하면 좋을 것 같아.”로 옆머리 보완 후 방향/걷기를 승인했다. `ArtReview/Approved/C1-Walk/`에160 PNG·Aseprite10개와 해시를 동결했고 목록160개를 approved로 변경했다.
- 본편 역할 배정은 최신 main의 `Docs/C1-Binding.md`로 답변을 받았다. 새 역할5명 정면 승인 → 걷기 → 역할별 경로로 전체 일괄 교체/동물 보관 → 확인 순서다. 기존22참조 조사 문서는 근거로 보존하며 C1 전체는 완료하지 않았다. 상세는 `Validation/C1-Walk-2026-10-24.md`.

### C1 추가 역할 정면 (2026-10-24, 새5명 정면 승인)

- 최신 main `bd609476`을 병합해 C1-Binding을 받았다. `ArtReview/C1-Binding-Fronts/`에 porter·merchant·merchant_silk·scholar·woman_ricecake 정면 PNG/Aseprite5쌍, 승인10명과 같은 배율 비교 한 장, 소품 변형 비교, 실제 Unity Game 화면을 제출했다. 원본은 `Tools/ArtTrials/npc_binding_fronts.py`다.
- 16×24·알파0/255·발밑·원본 재열기와 몸 유지 범위 확인. A2 기술 미리보기 오류0, Unity PPU/pivot/scale 일치·Console error0 확인. 본편/기존 ID/이름/대사/저장은 그대로다.
- 사용자 “그래.”로 새5명 정면을 승인했고 `ArtReview/Approved/C1-Binding-Fronts/`에 PNG/Aseprite·해시를 동결했다. 본편 적용은 이후 단계다. `Validation/C1-Binding-Fronts-2026-10-24.md` 참고.

### C1 추가 역할 걷기 (2026-10-24, 새5명 방향·걷기 승인 대기)

- `ArtReview/C1-Binding-Walk/`에5명×dulr×4=80프레임, Aseprite5개, 승인 정면/방향 비교·걷기 GIF·실제 Unity Game16캡처/GIF 제출. 원본은 `Tools/ArtTrials/npc_binding_walk.py`다. 기존 승인10명 출력/본편은 유지했다.
- 16×24·알파0/255·발밑·승인색 범위·아래0/2 정면 동일·원본 재열기/태그/125ms·A2 오류0 확인. Unity CharacterAnimator 선택80/루프·정지40건·Console error0 확인. 메모리 제자리 걷기 검토이며 실제 경로 이동/말걸기는 미검증이다.
- 새5명 방향/걷기 승인 후 역할별 경로로 전체 일괄 교체·동물 보관·새 우물가 아낙 추가·맵 이동/말걸기를 확인한다. 지금은 본편을 일부만 바꾼 상태가 아니다. 상세는 `Validation/C1-Binding-Walk-2026-10-24.md`.

### C1 본편 교체 (2026-10-24, 완료)

- 추가5명 걷기 승인 “좋아.”를 동결하고 C1-Binding대로34개 역할 경로·544 PNG/역할별Aseprite를 등록했다. 지역/맵 원본과 빌더 연결을 수정해11맵 JSON/씬을 재생성했다. 새 우물가 아낙1명만 별도 ID로 추가했고 기존 이름/ID/대사/지형/저장은 유지했다.
- Python94개·출구92개·A2 본편/보관 오류0, Unity36NPC×16프레임 사람 경로/PPU/발밑 및11씬45,000칸 일치 확인. 실제 PlayerMotor6828틱/WorkingNpc30틱과25대화 열기를 검사했다. 대화 NPC 없는 transit_wetroad는 이동만 확인했다. Q1 25/R6 87/저장27 회귀 통과.
- 동물176프레임·meta/해시/A2 목록 보관, 플레이어16프레임 불변 확인. `Tools/ArtGen/npc_roles.json`이 역할 연결 원본, `install_human_npcs.py`가 승인 그림 설치 원본, `MapGen/npc_bindings.py`가 최종 맵 연결 원본이다.
- 상세/한계는 `Validation/C1-Production-2026-10-24.md`. 실제 Game 캡처도 보존했다. 사람의 수동 플레이 완주는 별도이며 기존 저장은 쓰지 않았다.

### C2·C3 확대 v2 (2026-10-25, 색 보정 미채택·기존 그림 유지)

- 최신 main71d56cf5에서 `codex/c2-c3-expand` 시작, 완료 C1 worktree 정리. `ArtReview/Environment-Expand-v2/`에 궁궐·초가집2크기·버드나무·숲 나무6종의 전체 PNG/Aseprite, 전후 비교·실제 Unity 캡처8장·게임크기 비교를 제출했다.
- 기존 v2 gentle 보정을 그대로 사용해 원본 불투명 픽셀96% 이상·전체 알파/실루엣/크기/원래 색 유지. 원본10개 재열기·A2 194셀/10개체 오류0 확인. source는 `Tools/ArtTrials/environment_expand_v2.py`다.
- 개경/나루 Play의 메모리로만7종21개체를 표시해 실제 게임에서 확인했다. 본편 Assets/·맵·씬은 저장하지 않았고 사용하지 않은 숲3종 배치/실제 이동은 미검증이다. Console error0. 사용자 결정으로 약한 보정은 미채택하며 기존 본편을 유지한다. 원본 등록/사용 감사는 A3a에서 이어간다. 상세는 `Validation/C2-C3-Expand-2026-10-25.md`.

### C3a 겹침/가림 후속 점검 (2026-10-25)

- 사용자가 숲 비교 화면의 캐릭터가 나무 위처럼 보인다고 지적했다. 검토용 플레이어 발 셀(개경7,50)을 확인하니 blocked=1/reachable=0, 나무 그림 내부였다. 검토자의 강제 배치 오류이며 정상 플레이 재현으로 기록하지 않는다.
- CUSTOM 나무의 high_rows=0, Objects5/플레이어11 고정 순서는 별도 점검 대상으로 남겼다. 정상 보행 칸에서 앞뒤/좌우와 발밑 기준 가림을 확인한 뒤 필요시 수정한다. 본편은 바꾸지 않았다. `Validation/Environment-Decision-2026-10-25.md` 참고.

### B1 원본 이관 준비 (2026-10-25, 승인 면적 재현 불일치)

- `Tools/WorldLayout/export_world_layout.py`는 승인v2/seed7의 원래 셀을 한 JSON으로 묶는 준비 도구다. 두 라이브러리 환경에서 개경5001/나루3997/장단4298/북동길3050/남동길3051을 재현했으나 문서의4999/4000/4297/3048/3053과 달라 원본 파일 생성 전에 중단했다. 합계20000/25000과 육지 면적은 같다.
- `Validation/B1-2026-10-25/`에 차이표/실행 환경/Claude 요청을 남겼다. 경계/승인 수치/본편/ID/저장을 바꾸지 않았다. 준비 도구의 성공 경로·최종 원본·Unity/Play는 미검증이다. Python112개/출구92개는 통과했지만 실제B1이관은 종료1이며 완료가 아니다.

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
