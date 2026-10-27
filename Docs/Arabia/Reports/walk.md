# 아라비아 자동 보행·화면 캡처 시험 2026-10-27 10:06:16 · Unity 6000.3.26f1
본편 저장 파일: /Users/euninjun/Library/Application Support/DefaultCompany/PocketCaravan/pocket_caravan_save.json (시작 해시 E5BB55D861DC)
기존 아라비아 시험 저장을 .manual-backup으로 보관 후 새 시험 시작

## 자동 보행 (실제 이동·충돌 처리)
- [PASS] 아라비아 시험 세션·전용 저장
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/01_ahsa_start.png
- [PASS] 마을 사람 거닐기 (12초) — 8/8명 이동 · 걷는 방향 4가지
- [PASS] NPC 4방향 그림 (앞·뒤·옆이 서로 다름) — 바구니 든 주민
- [PASS] 걸어갈 경로 찾기: S0 → 힌드바드 — 19칸
- [PASS] 걸어서 도착: S0 → 힌드바드 — 19.2칸 · 5.1초 · 평균 속도 3.91칸/초 · 짐 0/10
- [PASS] 힌드바드가 Space 대화 대상
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/02_hindbad_offer.png
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/03_q1_accepted.png
- [PASS] 적재량에 따른 감속 (같은 칸 기준) — 빈 등짐 3.80 → 의뢰품 4/10 3.34칸/초
- [PASS] 걸어갈 경로 찾기: 힌드바드 → 알 아흐사 동쪽 출구 — 32칸
- [PASS] 걸어서 도착: 힌드바드 → 알 아흐사 동쪽 출구 — 31.9칸 · 9.6초 · 평균 속도 3.40칸/초 · 짐 4/10
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/04_farmroad.png
- [PASS] 걸어갈 경로 찾기: 나즈드 대상로 서쪽 → 동쪽 (의뢰품 적재) — 76칸
- [PASS] 걸어서 도착: 나즈드 대상로 서쪽 → 동쪽 (의뢰품 적재) — 75.9칸 · 21.8초 · 평균 속도 3.50칸/초 · 짐 4/10
  나즈드 대상로 횡단: 75.9칸 · 21.8초 (짐 4/10)
- [PASS] 걸어갈 경로 찾기: 제다 → 홍해 해안길 → 자비드 — 74칸
- [PASS] 걸어서 도착: 제다 → 홍해 해안길 → 자비드 — 74.1칸 · 21.3초 · 평균 속도 3.52칸/초 · 짐 4/10
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/05_zabid_arrive.png
- [PASS] 걸어갈 경로 찾기: 자비드 입구 → 공동 부엌 담당 — 52칸
- [PASS] 걸어서 도착: 자비드 입구 → 공동 부엌 담당 — 52.0칸 · 15.2초 · 평균 속도 3.47칸/초 · 짐 4/10
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/06_delivery.png
- [PASS] 걸어서 Q1 납품 완료 — 돈 100→120
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/07_q1_done.png
- [PASS] 걸어갈 경로 찾기: 부엌 → 자비드 곡물 장터 — 3칸
- [PASS] 걸어서 도착: 부엌 → 자비드 곡물 장터 — 3.0칸 · 0.8초 · 평균 속도 4.18칸/초 · 짐 0/10
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/08_zabid_market.png
- [PASS] 걸어갈 경로 찾기: 자비드 → 타이즈 — 23칸
- [PASS] 걸어서 도착: 자비드 → 타이즈 — 23.0칸 · 5.9초 · 평균 속도 4.00칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_taiz — 23.0칸 · 5.9초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/09_taiz.png
- [PASS] 걸어갈 경로 찾기: 타이즈 → 고원길 → 사나 남문 — 60칸
- [PASS] 걸어서 도착: 타이즈 → 고원길 → 사나 남문 — 60.6칸 · 15.8초 · 평균 속도 3.93칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_sanaa — 60.6칸 · 15.8초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/10_sanaa.png
- [PASS] 걸어갈 경로 찾기: 사나 남문 → 북문 → 모래바람 오아시스 — 44칸
- [PASS] 걸어서 도착: 사나 남문 → 북문 → 모래바람 오아시스 — 44.5칸 · 11.0초 · 평균 속도 4.11칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_dunes — 44.5칸 · 11.0초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/11_central_dunes.png
- [PASS] 걸어갈 경로 찾기: 오아시스 → 와디 건너 만국 장터 — 80칸
- [PASS] 걸어서 도착: 오아시스 → 와디 건너 만국 장터 — 80.1칸 · 20.7초 · 평균 속도 3.90칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_bazaar — 80.1칸 · 20.7초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/12_central_bazaar.png
- [PASS] 중앙 대륙 남부: 장터지기 카림 있음
- [PASS] 걸어갈 경로 찾기: 중앙 대륙 남부 → 장터지기 카림 — 54칸
- [PASS] 걸어서 도착: 중앙 대륙 남부 → 장터지기 카림 — 54.4칸 · 13.9초 · 평균 속도 3.95칸/초 · 짐 0/10
- [PASS] 중앙 대륙 남부 도착: 장터지기 카림 — 54.4칸 · 13.9초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/13_central_market.png
- [PASS] 걸어갈 경로 찾기: 장터 북문 → 한가운데 초원 — 36칸
- [PASS] 걸어서 도착: 장터 북문 → 한가운데 초원 — 36.4칸 · 8.9초 · 평균 속도 4.18칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_steppe — 36.4칸 · 8.9초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/14_central_steppe.png
- [PASS] 중앙 대륙 남부: 게르의 할아버지 있음
- [PASS] 걸어갈 경로 찾기: 중앙 대륙 남부 → 게르의 할아버지 — 57칸
- [PASS] 걸어서 도착: 중앙 대륙 남부 → 게르의 할아버지 — 57.2칸 · 15.0초 · 평균 속도 3.87칸/초 · 짐 0/10
- [PASS] 중앙 대륙 남부 도착: 게르의 할아버지 — 57.2칸 · 15.0초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/15_central_apex.png
- [PASS] 걸어갈 경로 찾기: 초원 → 장터 북문 — 58칸
- [PASS] 걸어서 도착: 초원 → 장터 북문 — 58.9칸 · 15.9초 · 평균 속도 3.84칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_bazaar — 58.9칸 · 15.9초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/16_central_bazaar.png
- [PASS] 걸어갈 경로 찾기: 장터 동문 → 소나무 고원 — 95칸
- [PASS] 걸어서 도착: 장터 동문 → 소나무 고원 — 95.4칸 · 25.3초 · 평균 속도 3.81칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_eastvale — 95.4칸 · 25.3초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/17_central_eastvale.png
- [PASS] 걸어갈 경로 찾기: 고원 → 붉은 바위 협곡 — 49칸
- [PASS] 걸어서 도착: 고원 → 붉은 바위 협곡 — 49.1칸 · 12.8초 · 평균 속도 3.87칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_canyon — 49.1칸 · 12.8초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/18_central_canyon.png
- [PASS] 걸어갈 경로 찾기: 협곡 → 장터 남동문 — 21칸
- [PASS] 걸어서 도착: 협곡 → 장터 남동문 — 20.7칸 · 5.6초 · 평균 속도 3.80칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_bazaar — 20.7칸 · 5.6초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/19_central_bazaar.png
- [PASS] 걸어갈 경로 찾기: 장터 서문 → 포도밭 언덕 — 133칸
- [PASS] 걸어서 도착: 장터 서문 → 포도밭 언덕 — 133.2칸 · 34.8초 · 평균 속도 3.85칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_westvale — 133.2칸 · 34.8초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/20_central_westvale.png
- [PASS] 걸어갈 경로 찾기: 포도밭 언덕 → 장터 서문 — 1칸
- [PASS] 걸어서 도착: 포도밭 언덕 → 장터 서문 — 0.9칸 · 0.3초 · 평균 속도 3.80칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_bazaar — 0.9칸 · 0.3초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/21_central_bazaar.png
- [PASS] 걸어갈 경로 찾기: 장터 → 와디 건너 오아시스 — 56칸
- [PASS] 걸어서 도착: 장터 → 와디 건너 오아시스 — 56.1칸 · 14.6초 · 평균 속도 3.89칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_central_dunes — 56.1칸 · 14.6초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/22_central_dunes.png
- [PASS] 걸어갈 경로 찾기: 오아시스 → 빈 사막 북쪽 모래길 — 73칸
- [PASS] 걸어서 도착: 오아시스 → 빈 사막 북쪽 모래길 — 73.4칸 · 19.4초 · 평균 속도 3.83칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_shortcut — 73.4칸 · 19.4초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/23_shortcut.png
- [PASS] 걸어갈 경로 찾기: 룹알할리 빈 사막 → 알 아흐사 — 64칸
- [PASS] 걸어서 도착: 룹알할리 빈 사막 → 알 아흐사 — 64.3칸 · 17.1초 · 평균 속도 3.81칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_ahsa — 64.3칸 · 17.1초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/24_ahsa.png
- [PASS] 본편 저장 파일 그대로
- [PASS] 본편 저장 파일 변경 없음 (시험 전후 해시) — E5BB55D861DC → E5BB55D861DC
이전 아라비아 시험 저장을 복원

결과: 모든 항목 통과
