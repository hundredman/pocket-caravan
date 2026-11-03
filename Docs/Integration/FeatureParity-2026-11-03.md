# 지역별 기능 통일 점검표 (2026-11-03, integration 브랜치 기준)

integration 작업트리에서 `Assets/PocketCaravan/Scripts`·`Data`·`Scenes`를 훑어 지역마다 **있는 기능 / 없는 기능**을 나란히 놓은 표. 통합 담당(기우)이 "전 지역 같은 수준"으로 맞추기 위한 기준 문서.
✔ 있음 · △ 일부/약함 · ✗ 없음 · (스크립트 이름)

## 1. 플레이 기능

| 기능 | 북유럽 | 베네치아 | 고려 | 아라비아 | 중앙 (Center·Transit·본토서부) |
|---|---|---|---|---|---|
| 시작 이야기 + 조작 튜토리얼 | ✔ NorthIntro | ✔ VeniceIntro | ✔ GoryeoIntro | ✔ ArabiaIntro | – |
| 지역 지도 (M) | ✔ NorthMapUI + NorthTerrain.png | ✔ VeniceMapUI + VeniceTerrain.png | ✔ 공용 WorldMapUI (고려 교역도) | **✗ 지도 그림 없음 → 고려 지도가 뜸** | 북쪽=North, 서부=Venice, 동쪽=고려, Transit=? |
| 맵 전환 연출 | ✔ 눈보라·순록 얼굴 (NorthTransition) | ✔ 고양이 얼굴 (VeniceTravelWave) | ✔ GoryeoTravelTransition | ✗ 기본 검은 페이드 | 지역 따라감 |
| 여관에서 쉬기 (돈 내고 4시간, 버프) | ✔ NorthInn | ✔ VeniceInn | ✗ (공용 Rest 시설만: 체력만 회복) | ✗ (〃) | △ 북문·길목 = NorthInn, 나머지 공용 Rest |
| 앉기 (벤치·모닥불) | ✔ NorthFireSeat | ✔ VeniceSeat | ✗ | ✗ | 북쪽 모닥불만 |
| 미니게임 | ✔ 6개: 얼음낚시·썰매·눈싸움·돌탑·장작패기·노젓기 | ✔ 3개: 유리불기·그물·곤돌라경주 | △ 1개: 낚시 (GoryeoPastime, 나루) | **✗ 0개** | ✗ |
| 공방 / 제작 | ✔ NorthCraft (양조·털옷·건조) | ✔ VeniceWorkshop (유리장식·레이스) | ✗ | ✗ | ✗ |
| 돌아가며 서는 큰 장 (Docs/NorthFair.md 공통 규칙) | ✔ 중앙 북쪽 4곳 (NorthCraft) | ✔ 본토 서부 3곳 (VeniceFair) | ✗ | ✗ | 북·서부만. **Transit·중앙 동쪽 없음** |
| 떠돌이 상인 (매일 다른 마을) | ✔ NorthDaily | ✔ VeniceMerchant | ✗ | ✗ | ✗ |
| 전시실 기증 (모으면 칭호·상금) | ✔ 비르카 회관 | ✔ 리알토 상인회관 | ✗ | ✗ | ✗ |
| 숨은 수집물 7개 + 전설 | ✔ 룬돌 (NorthLegends) | ✔ 사자 문장 (VeniceLion) | ✗ | ✗ | ✗ |
| 하루 한 번 일상 거리 | ✔ 온천·순록 먹이·벌통·대상 야영·나무꾼 | ✔ 비둘기 모이 | ✗ | ✗ | ✗ |
| 날씨 사건 (길 잃은 사람 데려다주기) | ✔ NorthEvents | ✔ VeniceEvents | ✗ | ✗ | ✗ |
| 밤 축제 | ✔ 오로라 축제 (판매 1.6배) | ✔ 카니발 (판매 +3할, 가면 장수) | ✗ | △ 만국장터 불꽃 (연출만, 보상 없음) | ✗ |
| 리액션 바퀴 (R) | ✔ NorthSocial | ✔ VeniceReactions | ✗ | ✗ | 북·서부만 |
| 친밀도·선물 (G) | ✔ 8명 | ✔ 8명 + 친구 수첩 (F) | ✗ | ✗ | ✗ |
| 사진 (P) | ✔ | ✔ | ✗ | ✗ | ✗ |
| 길 돌발 사건 (RoadEvent/Bandit) | ✔ | ✗ (본토서부엔 있음) | ✗ | ✗ | ✔ Center·본토서부. Transit은 Bandit만 |
| 정직 시험 | ✔ 톰테 (Q3) | ✔ 검수원 (Q3) | ✗ | ✗ | – |
| 퀘스트 연출 디렉터 | ✔ NorthQuestDirector | ✔ VeniceQuestDirector | ✗ | ✗ | – |
| "다음 이야기" 길잡이 | ✔ NorthGuide | ✔ VeniceDaily 안 | ✗ | ✗ | – |
| 퀘스트 동시 수락 제한 | ✔ NorthQuestLimit (3개) | ✗ | ✗ | ✗ | – |
| 놀거리 알림 (★ 표시·화살표) | ✗ | ✔ VeniceActivityHints | ✗ | ✗ | – |
| 세로 선택창 | ✗ (공용 가로) | ✔ VeniceMenu | ✗ | ✗ | – |
| 빠른 이동 (배) | ✔ 나룻배 미니게임 | ✔ 곤돌라 나루 6냥 | ✗ | ✗ | – |
| 살아있는 퀘스트 수 | 15 | 11 | 17 | **2** | 0 |

## 2. 연출·소리·이동 (보이기/들리기만)

| 항목 | 북유럽 | 베네치아 | 고려 | 아라비아 | 중앙 |
|---|---|---|---|---|---|
| 날씨 | ✔ 눈 4단계·오로라·바람 | ✔ 안개·비·아쿠아 알타 | ✗ | ✔ 모래바람·회오리 | 북쪽만 약하게 |
| 마을 생활 연출 | ✔ 동물·연기·불빛 (NorthLife) | ✔ 비둘기·갈매기·곤돌라·가로등 | △ 등불(GoryeoLantern)·WorkingNpc | ✔ Wanderer·Patrol·FxMarker | Walker만 |
| 지역 음악 (시간대 4곡) | ✔ NorthSound | ✔ VeniceSound | ✗ (공용 naru) | ✗ | 북·서부만 |
| 생활 소리 (장터 웅성·동물) | ✔ | ✔ + NPC 말소리 | ✗ | △ 바람 소리만 | – |
| 걷기 느낌 (발소리·발자국·먼지) | ✔ 썰매·눈 자국 (NorthTravel) | ✔ VeniceStride | ✗ | ✔ 모래 발자국·먼지 | – |
| 날씨가 속도에 영향 | ✔ | ✔ (아쿠아 알타) | ✗ | ✗ | – |
| 장소 이름표 근접 표시 | ✗ | ✔ VeniceLabels | ✗ | ✗ | – |

## 3. 마을 데이터 (Town_*.asset)

| 항목 | 북유럽 | 고려 | 베네치아 | 아라비아 | 중앙 |
|---|---|---|---|---|---|
| settlementTier | 비르카 3, **나머지 8곳 0** | 1~3 | 1~3 | 1~3 | Transit 2, **Center 4곳 0** |
| Rest 시설 | 비르카+중앙 4곳만. **Herd·Hunter·Laugar·Naru 없음** | 전부 | 전부 | 전부 | 전부 |
| RouteInfo 시설 | 비르카+중앙 4곳 | 2등급↑ | 2등급↑ | **없음** | 전부 |
| Warehouse 시설 | 비르카 | 개경 | Chioggia·Rialto | **없음** | 없음 |
| 재고 품목 수 | 9~15 | 4~6 | 4~7 | **2~4** | 6 |
| 탈것 판매 | Birka·Herd | 개경 | Rialto | **없음** | Woodtown |
| journeyNote | ✔ | ✔ | ✔ | ✔ | **Transit 2곳 비어있음** |
| ContractBoard (씬) | ✔ | ✔ | ✔ | ✔ | **venice_c_landing·vineyard 없음** |
| QuestNpc (씬) | ✔ | ✔ | ✔ | Ahsa·Zabid만 | 없음 |

기타: `Town_Naru`가 고려(`naru`)·북유럽(`north_naru`)에 같은 에셋 이름으로 둘 있음. 퀘스트 `N4_Storm`은 giverNpc 비어 있음(자동 수락용이면 OK).

## 4. 저장 방식이 지역마다 다름

| 지역 | 진행 저장 위치 |
|---|---|
| 북유럽 | GameManager 쿨다운 표에 `north:` 키 (NorthSave) |
| 베네치아 | GameManager 진행 메모에 `venice:` 키 (VeniceSave) |
| 고려·아라비아 | 지역 진행 없음 (공용 퀘스트·시장만) |

같은 구조로 쓰지만 표가 다름. 통일하려면 공용 `RegionSave` 하나로 모으고 둘 다 거기로 옮기는 게 깔끔 (저장 호환 이주 필요).

## 5. 통일 우선순위 (제안)

**A. 데이터만 고치면 되는 것 (바로)**
1. 북유럽 8개 마을 `settlementTier` 채우기, Herd·Hunter·Laugar·Naru에 Rest 시설 추가
2. 아라비아 5개 마을: RouteInfo(2등급↑)·Warehouse(Sanaa)·탈것 판매·재고 품목 늘리기
3. venice_c_landing·vineyard에 ContractBoard, Transit 2곳 journeyNote
4. Center 4곳 settlementTier

**B. 공용으로 끌어올릴 것 (북유럽·베네치아에 둘 다 있음 → 공용 코드로 올리고 고려·아라비아에 적용)**
- 여관 쉬기 → 공용 Rest 시설에 "돈 내고 4시간·버프" 합치기 (지금은 Rest 시설과 Inn이 따로 두 개)
- 리액션(R)·친밀도/선물(G)·사진(P) → 공용 Social
- 큰 장 순환 → 중앙 전체 "하루 한 곳" (NorthFair.md에 이미 그렇게 쓰여 있음)
- 떠돌이 상인, 전시실, 숨은 수집물 7개, 날씨 사건(길 잃은 사람), 다음 이야기 길잡이, 퀘스트 동시 제한, 지역 지도 오버라이드, 맵 전환 연출
- 세로 선택창(VeniceMenu)·놀거리 알림(VeniceActivityHints)은 베네치아만 있는데 공용으로 올려도 됨

**C. 지역 담당이 채워야 하는 것**
- 아라비아(은인): 미니게임 0, 퀘스트 2, 지역 지도 없음, 음악 없음, 큰 장 없음 → 가장 많이 비어 있음
- 고려(은샘): 미니게임 1, 날씨·음악·여관·사회 기능 없음. 퀘스트는 가장 많음(17)
- Transit(중앙 동쪽·고려 쪽): 큰 장·Walker·퀘스트 없음

**D. 수량 기준 맞추기 (팀 결정 필요)**
- 지역당 미니게임 최소 N개 (지금 6/3/1/0)
- 지역당 퀘스트 최소 N개 (지금 15/11/17/2)
- 친밀도 NPC 수 (지금 8/8/0/0)
