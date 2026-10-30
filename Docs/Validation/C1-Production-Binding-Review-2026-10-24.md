# C1 본편 교체 전 Claude 검토 요청

10명 미술은 옆머리 보완까지 사용자 승인됐다. `ArtReview/Approved/C1-Walk/approval.md`와 동결160 PNG/Aseprite10개가 기준이다.

## 멈춘 이유

기존 주민은 비단 장수·짐꾼·선비·뱃사공·떡 장수인데 ArtBrief의 주민 시안은 아낙·아이·뱃사공이다. 약재상과 선비가 같은 동물 파일을, 농부와 짐꾼도 같은 파일을 공유한다. 파일 단위로 일괄 교체하면 이름/역할이 맞지 않는 그림이 들어간다. 본편의 장터 상인·일꾼·행인도 요청서의10명과 단순히 대응되지 않는다. 역할 판단이 필요한 경우 멈추고 기록하라는 사용자 지시에 따라 실제 연결은 변경하지 않았다.

## 확실한 연결(아직 적용하지 않음)

| 현재 대상 | 승인 그림 |
|---|---|
| naru_chief | chief |
| goryeo_jangdan_farmer | farmer |
| goryeo_songak_merchant | herbalist |
| dakmok_chief | dakmok_chief |
| gaekju | gaekju |
| 뱃사공 주민 | boatman |
| NPC 행상/행상 사건 | trader |
| 도적 | bandit |

## 검토할 것

1. 아래 현재 역할별로 승인된 그림 재사용 여부 또는 별도 그림 필요 여부를 지정한다. 특히 비단 장수·짐꾼·선비·떡 장수와 장터 상인, 일꾼/행인을 포함한다. 그림 재사용 시 소품이 역할과 맞는지도 확인한다.
2. 승인된 아낙·아이를 기존 주민 중 어디에 표시할지, 별도 추가할지를 정한다. 기존 이름/대사 변경이 필요하면 문서로 명시한다. 기존 ID/저장 데이터는 유지한다.
3. 바꿀 생성 원본과 완료 검사 방법을 ArtBrief에 남긴다. 그림 선택과 별개로 캐릭터 원본이 같은 파일을 공유하는 대상은 **용도/대상별 새 경로**로 연결해야 한다. 플레이어 tanuki와 동물 원본은 보존한다.

## 근거 목록

등록된 고려/중앙 동쪽11맵과 지역 NPC/주민 정의를 읽은 목록이다. 아래 수량은 정의/직접 character 참조 수이며 실제 씬 인스턴스 총수가 아니다. 주민은 idx/variant로 배정되므로 추가 배치 검사가 필요하다. 상세 파일/위치는 `C1-Binding-Inventory-2026-10-24.json`.

| 현재 이름 | 현재 그림 | 출처 구분 | 목록 수 |
|---|---|---|---|
| 개경 객주 돈돌 | `gaekju_tall` | npcs | 1 |
| 길목 안내인 | `villager_tanuki_tall` | talker | 1 |
| 길손 | `villager_dog_tall` | talker | 2 |
| 나루 낚시 | `villager_rabbit_tall` | pastime | 1 |
| 나루 마을 촌장 | `elder_tall` | npcs | 1 |
| 나루 상인 | `villager_tanuki_tall` | talker | 1 |
| 나루 짐꾼 | `villager_dog_tall` | worker | 1 |
| 농부 | `villager_rabbit_tall` | talker | 1 |
| 닥나무 마을 촌장 | `dakmokchief_tall` | npcs | 1 |
| 땔감 장수 | `villager_bear_tall` | talker | 1 |
| 떡 장수 | `villager_tanuki_tall` | villagers | 1 |
| 뱃사공 | `villager_bear_tall` | talker | 1 |
| 뱃사공 | `villager_bear_tall` | villagers | 1 |
| 비단 장수 | `villager_fox_tall` | villagers | 1 |
| 산록 약재 장수 | `villager_rabbit_tall` | npcs | 1 |
| 선비 | `villager_rabbit_tall` | villagers | 1 |
| 시전 짐꾼 | `villager_dog_tall` | worker | 1 |
| 시전 행상 | `villager_fox_tall` | worker | 1 |
| 장단 곡물 상인 | `villager_dog_tall` | npcs | 1 |
| 종이 일꾼 | `villager_rabbit_tall` | worker | 1 |
| 짐꾼 | `villager_dog_tall` | villagers | 1 |

추가 고정 연결은 `Editor/MapJsonBuilder.cs`: market→merchant_tall, bandit→bandit_tall, road_event의 FallenTraveler→villager_rabbit_tall/Peddler→trader_tall, walker→trader_tall. 이 목록은 A3a 전체 에셋 감사의 완료를 대신하지 않는다.

미술 승인/문서 검토와 본편 교체 완료를 구분한다. 이번에는 정적 사용처 조사·승인 기록만 했고 Unity/플레이 검사를 다시 실행하지 않았다. 기존 검사는 앞선 C1-SideHair 기록에 있다.
