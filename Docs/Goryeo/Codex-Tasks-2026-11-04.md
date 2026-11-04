# Codex 작업 지시 (2026-11-04, 고려·중앙 동쪽)

은샘 지시(11/04): 웬만한 작업은 Codex가 한다. Claude는 최종 확인·검토만 한다.
작업 순서는 표의 번호대로. 각 작업이 끝나면 커밋하고, 그림이 바뀐 작업은 실제 게임 화면 캡처를 남긴다(은샘 승인용).

## 공통 규칙

- **브랜치:** `merge/integration-goryeo`에서 작업한다(integration 최신을 합친 고려 브랜치, Mac 저장소에 체크아웃되어 있음). **push·PR은 하지 않는다**(은샘 지시 때 Claude가 함).
- **다른 지역은 고치지 않는다.** 공용 파일(`Scripts/Core·Economy·Quests·UI`, `GameDatabase`, `WorldIndex`, `World/Connections`, `EditorBuildSettings`)을 고치면 커밋 메시지에 적는다. 기우가 만든 공용 기능(`Social.cs`, `Crafting.cs`, `CentralFair.cs`, `Inn.cs`, `NightFestival.cs`, `StoryGuide.cs`)은 **고려 항목만** 고친다.
- **아이디·저장 형식 유지.** 바꾸면 옛 저장 이주를 함께 만든다.
- **맵 JSON을 다시 내보내지 않는다**(`m4_export` 전체 재생성 금지). 맵에 사람·이정표·장터를 더할 때는 `Tools/WorldLayout/goryeo_life.py`(설정 `goryeo_life.json`)를 쓰고, 좌판 그림 배치는 `market_production.py` + 검토 배치 파일을 쓴다.
- **검사(커밋 전):**
  - `python Tools/Integration/validate_world.py`
  - `python -m unittest discover -s Tools/Integration -p 'test_*.py'` (numpy·scipy 있는 환경과 Pillow만 있는 환경 둘 다 통과해야 함. GitHub 검사는 Pillow만 깐다)
  - Unity: `MapJsonBuilder.ValidateWorld`, `SceneTerrainChecks.CheckGeneratedScenes`, `GoryeoRegionChecks.Run`, `TransitRegionChecks.Run`, `Q1Checks.Run`, `GoryeoExperienceChecks.RunData`, `GoryeoTradeBalanceChecks.RunData`, `SaveCompatibilityChecks.RunData`
  - 맵을 바꿨으면 `MapJsonBuilder.BuildSelected(...)`로 씬을 다시 만든다.
  - Play 시험 전에 저장 파일을 백업하고, 시험 뒤 저장 파일이 바뀌지 않았는지 확인한다.
- 하지 않은 검사를 했다고 쓰지 않는다.

## 작업 목록

| 번호 | 작업 | 할 일 | 끝난 기준 |
|---|---|---|---|
| 1 | 고려 친구 8명 대사·기념품 | `Scripts/UI/Social.cs`의 고려(G) 8명 초안을 다듬는다. 아래 "1번 메모" 참고 | 대사 수정안을 표로 은샘에게 보여 주고 승인받은 뒤 반영. Play에서 친구 1명에게 말 걸기·선물(G)이 되는지 확인 |
| 2 | 중앙 동쪽 마을 소개 글 | `Data/Transit/Town_Woodtown·Ferry·Southport·Crossroads.asset`의 `journeyNote`(여정 수첩 문구). 거래 역할·귀환 힌트만, 시세·이익 보장 문구 금지 | 4곳 모두 채움. Woodtown·Ferry는 R2 고정 해시 검사(`test_r2_safety.py`) 대상이라, 이미 있는 `STALE_INFO_FIXED` 방식처럼 바꾼 문장만 되돌려 비교하도록 검사를 맞춘다 |
| 3 | 고려 공방 1~2개 | `Scripts/Economy/Crafting.cs`의 `Recipes`에 고려 항목(예: 한지 뜨기·나전 장식). 재료·결과는 GameDatabase에 있는 상품만. 공방 자리 그림·배치(`WorkshopSpot`)도 | 레시피 이익이 교역보다 크지 않은지 값 표로 보여 줌. 공방 자리 그림은 화면 캡처로 은샘 승인 |
| 4 | 중앙 동쪽 큰 장 좌판 그림 | 기우 요청. `CentralFair` 동쪽(솔재 길목·버들나루)에 큰 장 날 장식 그림. 새 마을 2곳(남동 포구·내륙 교역 길목)을 동쪽 순환에 넣을지는 **기우 확인 후**(공용 파일) | 그림 시안 → 은샘 승인 → 배치 |
| 5 | 새 장터 2곳 전용 좌판과 배치 | 포구: 생선 덕장·소금 가마니 좌판 / 길목: 피륙·양모 좌판·향신료 자루 좌판(`Codex-Requests-2026-11-03.md` 6·7번). 그 그림을 섞어 `ArtReview/MarketCourts-E2-20261103/layout.json`(지금은 곡물 좌판·잡화 판매대만, status review) 배치를 다시 잡는다 | 두 마을 장터 광장·이벤트 마당 화면 캡처로 은샘 승인. 출구·기능 객체가 막히지 않음(`test_market_production`) |
| 6 | 승인 대기 그림 연결 | 통나무(`fallen_log` 48×48), 향신료·양모 아이콘, 남동 포구 모서리·버들나루 잔물결 수정(`ArtReview/E3-E2-Art-20261103`) | **은샘 승인을 받은 것만** 연결. 아이콘은 새 상품(`transit_spice`·`transit_wool`) 등록 + 두 새 마을 재고(E2 초안: 포구 향신료 0.65, 길목 양모 0.7) |

## 1번 메모 (Claude 검토)

- 친구는 **말 거는 사람의 화면 이름과 정확히 같아야** 연결된다(`Social.FriendIndex`). 지금 고려·중앙 동쪽 사람 이름: 나루 마을 촌장, 나루 우물가 아낙, 개경 객주 돈돌, 닥나무 마을 촌장, 산록 약재 장수, 장단 곡물 상인, 뱃사공(나루·버들나루·남동 포구 세 곳에 같은 이름), 땔감 장수(솔재 길목), 여관·객주 주인들, 길손, 비단 상인, 선비, 떡 장수 등.
- **뱃사공:** 중앙 동쪽(transit)도 고려 지역으로 치기 때문에 세 마을의 뱃사공이 한 친구로 묶인다. 나루 뱃사공만 친구로 하려면 다른 두 곳 이름을 바꾸거나(예: '포구 뱃사공') 친구 이름을 구분한다.
- **땔감 장수:** 좋아하는 물건이 송진인데, 솔재 길목 마을이 송진 산지라 어색하다. 다른 곳 물건(예: 소금·곡식)으로.
- **배우는 리액션:** 고려 고유 리액션(큰절·부채질)은 다른 방법으로 배운다(개경 첫 방문·객주에서 처음 쉬기). 친구가 가르치는 것은 공통 리액션으로 둬도 된다.
- 대사는 다른 지역처럼 짧게, 구현되지 않은 기능은 말하지 않는다.

## 이미 끝난 것 (참고)

- integration(기우 통합) 합치기, 고려 맵 6개 여관 주인 충돌 해결, 새 마을 2곳 객주 주인.
- 맵 전환: 창호지 문(은샘 A안, `Scripts/Map/GoryeoTravelTransition.cs`). 카메라가 이웃 맵 배경까지 따라감(`Scripts/Player/CameraFollow.cs`).
- 남은 결정: 메시지 창이 많이 뜨는 것(기우 공용 기능)은 은샘이 기우에게 전달.
