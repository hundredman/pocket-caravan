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

## 진행 기록

- 2026-11-04 Codex 1번 완료: 사용자 승인안 Social.cs 고려8명 반영. 이름/저장키·다른 지역 유지. 실제 Space 대화/G 선물/Space 확인으로 소금 선물·친밀도 보상·첫하트 대사 검증. 저장9파일 보존, Python354(전체환경23skip/Pillow전용32skip)실패0·월드0오류·Unity 지정8종 통과. 2번 착수. push/PR 없음.
- 2026-11-04 Codex 2번 완료: 중앙 동쪽4곳 journeyNote 작성, 정확한 추가문장만 되돌리는 R2 고정 검사 보완. 정적 두 환경354개 실패0·Unity 지정8종·실제Q수첩4곳 연결 확인/2캡처·저장9파일 보존. 3번 공방 검토 착수.
- 2026-11-04 Codex 3번 검토 준비: 닥목 한지 작업장 접수(37,53), 공방 재료 제공 주문형 paper1개/6시간/하루2회/현지 매입가+5(최소18). 전후 및 주문/수령 캡처·가격 비교표 ArtReview/GoryeoWorkshop-Task3-20261104. 실제 주문/수령/진입/한도/만재 보관 Play28확인·저장9파일 불변, 정적 두 환경354개 실패0·Unity 지정8종 통과. 그림/방식 사용자 승인 대기, 본편 레시피/맵은 미반영. 4번 이후 미착수.

- 2026-11-04 Codex 3번 완료: 사용자 승인 접수(37,53)/한지 레시피 본편 연결. 기존 v2 메모 저장 복원·재진입 포함 Play33확인/저장9파일 불변. 정적356 두 환경 실패0·Unity 지정8종 통과. 4번 큰 장 시안 제작 착수, 순환/보너스 변경 없음. push/PR 없음.

- 2026-11-04 Codex 4번 검토 준비: 큰 장 남색/청록 좌판48×48 두 개, 솔재 길목/버들나루 장터·마당 기존 좌판만 대체하는 previewOnly 실제 전후6캡처. 날짜1/2 일정·복원·크기/저장 차단 Play39확인, 저장9파일 보존. 사용자 그림 승인 대기, 본편/순환/보너스 미수정·5번 이후 미착수.

- 2026-11-04 Codex 4번 완료: 사용자 승인 남색/청록 좌판 본편 두 마을8곳 연결·선택 씬 재생성. 같은 씬 날짜/활성 변경 복원·주변 통행 자동 Play291확인, 실제6캡처·저장9파일 불변. 공용 일정/보너스·신규 마을 순환은 미수정. 5번 전용 좌판4종 착수, push/PR 없음.

- 2026-11-04 Codex 5번 검토 준비: 생선 덕장/소금 좌판/피륙·양모 좌판/향신료 판매대4종. 기존 위치·크기 유지한 두 마을 시장/마당 전용 소품10개 시험·실제 전후8캡처. 자동 Play281확인(250통행 비교), 저장9파일 불변·정적358 두 환경/Unity 지정8종 통과. ArtReview/SpecialtyMarkets-Task5-20261104. 사용자 그림 승인 대기, 본편 원본/씬은 유지·6번 미착수.

- 2026-11-04 Codex 5번 완료: 사용자 “그래.” 승인4종/10자리 본편 연결, 승인 원본/해시에서 market_production 재현·두 transit 씬 선택 재생성. 본편 Play29확인/4캡처·저장9파일 불변·정적358 두 환경/Unity8종 통과. 위치/통행/기능ID·공용 큰 장 순환·DB 유지. 6번 승인 기록 점검 착수. push/PR 없음.

- 2026-11-04 Codex 6번 승인 기록 확인: 길막음 통나무/향신료·양모 아이콘/포구 빈 모서리·잔물결은 기존 문서상 별도 승인 대기. 반려된 범람 PNG 제외, 기존3종 실제크기·11/03 Unity 검토 화면/포구 비교만 묶어 재제출(ArtReview/Task6-Approval-20261104). 본편·신규 품목·사건 연결은 미실행, 그림 승인 후 진행.
