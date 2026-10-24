# C1 본편 연결표 — 사람 NPC 배정

2026-10-24, Claude. Codex 검토 요청 `Docs/Validation/C1-Production-Binding-Review-2026-10-24.md`(codex/c1-expand)에 대한 답. 기준 그림은 은샘 승인본 10명(`ArtReview/Approved/C1-Walk/`).

## 원칙

- **역할마다 자기 그림 경로를 갖는다.** 지금처럼 여러 역할이 한 동물 파일을 같이 쓰지 않는다. 같은 승인 그림을 재사용하더라도 역할별 키(예: `npc_naru_porter`)로 연결해, 나중에 한 역할만 바꿀 수 있게 한다.
- 기존 NPC ID·이름·대사·저장 데이터는 바꾸지 않는다. 그림 연결만 바꾼다.
- 소품이 역할과 안 맞으면 재사용하지 않는다. 같은 몸에 소품만 바꾼 변형은 **별도 에셋으로 등록**한다(방향 기준 2절).
- 플레이어 너구리와 동물 원본은 보존. 동물 그림은 보관 폴더로 옮기고 목록에 "보관(꾸미기 후보)".

## 연결표

| 현재 이름 (출처) | 연결할 그림 | 구분 |
|---|---|---|
| 나루 마을 촌장 (npcs) | chief | 승인본 |
| 닥나무 마을 촌장 (npcs) | dakmok_chief | 승인본 |
| 개경 객주 돈돌 (npcs) | gaekju | 승인본 |
| 산록 약재 장수 (npcs) | herbalist | 승인본 |
| 장단 곡물 상인 (npcs) | farmer | 승인본 (곡식을 기르고 파는 사람) |
| 농부 (talker) | farmer | 승인본 |
| 뱃사공 (talker), 뱃사공 (villagers) | boatman | 승인본 |
| 나루 낚시 (pastime) | child | 승인본 (낚시하는 아이) |
| 시전 행상 (worker), 길손 ×2 (talker), 길 위 사건 Peddler·walker | trader | 승인본 (짐을 지고 다니는 사람) |
| 도적 | bandit | 승인본 |
| 나루 짐꾼, 시전 짐꾼 (worker), 짐꾼 (villagers), 종이 일꾼 (worker), 땔감 장수 (talker), 길 위 사건 FallenTraveler | **porter (새로)** | 지게꾼: 머리띠, 빈 지게. 행상(trader)과 구분되게 큰 짐 없이 |
| 나루 상인 (talker), 장터 상인 market | **merchant (새로)** | 시전 상인: 상투, 조끼, 장부 |
| 비단 장수 (villagers) | **merchant_silk (새로, merchant 변형)** | 같은 몸에 비단 두루마리 소품 |
| 선비 (villagers), 길목 안내인 (talker) | **scholar (새로)** | 유건(선비 두건), 책 |
| 떡 장수 (villagers) | **woman_ricecake (새로, 아낙 변형)** | 승인된 아낙 몸에 물동이 대신 떡 쟁반 |
| (새 주민) 나루 우물가 아낙 | woman | 승인본. 새 ID로 추가(기존 저장 영향 없음), 짧은 대사 1개 |

## 진행 순서

1. 새 그림 5개(porter, merchant, merchant_silk, scholar, woman_ricecake)를 승인된 아낙 정면 기준으로 정면만 먼저 만들어, 기존 10명과 한 장에 나란히 놓고 은샘에게 승인받는다.
2. 승인되면 4방향·걷기를 만든다.
3. 위 표대로 역할별 경로를 만들어 한 번에 교체한다. 일부만 사람으로 바꾼 중간 상태로 합치지 않는다.
4. 확인: 모든 NPC가 사람(검사로 동물 그림 참조 0개, 플레이어 제외), 기존 ID·대사·저장 유지, 맵마다 실제 이동·말 걸기 확인, 게임 화면 캡처 제출.
