# 아라비아 자동 보행·화면 캡처 시험 2026-10-23 00:08:39 · Unity 6000.3.26f1
본편 저장 파일: /Users/euninjun/Library/Application Support/DefaultCompany/PocketCaravan/pocket_caravan_save.json (시작 해시 E5BB55D861DC)
기존 아라비아 시험 저장을 .manual-backup으로 보관 후 새 시험 시작

## 자동 보행 (실제 이동·충돌 처리)
- [PASS] 아라비아 시험 세션·전용 저장
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/01_ahsa_start.png
- [PASS] 마을 사람 거닐기 (12초) — 8/8명 이동 · 걷는 방향 4가지
- [PASS] NPC 4방향 그림 (앞·뒤·옆이 서로 다름) — 바구니 든 주민
- [PASS] 걸어갈 경로 찾기: S0 → 힌드바드 — 23칸
- [PASS] 걸어서 도착: S0 → 힌드바드 — 23.5칸 · 6.3초 · 평균 속도 3.92칸/초 · 짐 0/10
- [PASS] 힌드바드가 Space 대화 대상
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/02_hindbad_offer.png
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/03_q1_accepted.png
- [PASS] 적재량에 따른 감속 (같은 칸 기준) — 빈 등짐 3.80 → 의뢰품 4/10 3.34칸/초
- [PASS] 걸어갈 경로 찾기: 힌드바드 → 알 아흐사 동쪽 출구 — 24칸
- [PASS] 걸어서 도착: 힌드바드 → 알 아흐사 동쪽 출구 — 24.1칸 · 7.3초 · 평균 속도 3.40칸/초 · 짐 4/10
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/04_farmroad.png
- [PASS] 걸어갈 경로 찾기: 농원길 서쪽 → 동쪽 출구 (의뢰품 적재) — 59칸
- [PASS] 걸어서 도착: 농원길 서쪽 → 동쪽 출구 (의뢰품 적재) — 59.0칸 · 16.6초 · 평균 속도 3.57칸/초 · 짐 4/10
  농원길 횡단: 59.0칸 · 16.6초 (짐 4/10)
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/05_zabid_arrive.png
- [PASS] 걸어갈 경로 찾기: 자비드 입구 → 공동 부엌 담당 — 10칸
- [PASS] 걸어서 도착: 자비드 입구 → 공동 부엌 담당 — 9.9칸 · 2.8초 · 평균 속도 3.68칸/초 · 짐 4/10
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/06_delivery.png
- [PASS] 걸어서 Q1 납품 완료 — 돈 100→120
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/07_q1_done.png
- [PASS] 걸어갈 경로 찾기: 부엌 → 자비드 곡물 장터 — 3칸
- [PASS] 걸어서 도착: 부엌 → 자비드 곡물 장터 — 3.0칸 · 0.8초 · 평균 속도 4.18칸/초 · 짐 0/10
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/08_zabid_market.png
- [PASS] 걸어갈 경로 찾기: 자비드 → 공방길 — 46칸
- [PASS] 걸어서 도착: 자비드 → 공방길 — 47.4칸 · 12.4초 · 평균 속도 4.18칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_workshoproad — 47.4칸 · 12.4초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/09_workshoproad.png
- [PASS] 걸어갈 경로 찾기: 공방길 횡단 → 타이즈 — 64칸
- [PASS] 걸어서 도착: 공방길 횡단 → 타이즈 — 64.2칸 · 16.1초 · 평균 속도 4.03칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_taiz — 64.2칸 · 16.1초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/10_taiz.png
- [PASS] 걸어갈 경로 찾기: 타이즈 → 항구길 — 60칸
- [PASS] 걸어서 도착: 타이즈 → 항구길 — 60.9칸 · 16.2초 · 평균 속도 4.05칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_portroad — 60.9칸 · 16.2초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/11_portroad.png
- [PASS] 걸어갈 경로 찾기: 항구길 횡단 → 제다 — 63칸
- [PASS] 걸어서 도착: 항구길 횡단 → 제다 — 63.1칸 · 16.4초 · 평균 속도 3.86칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_jeddah — 63.1칸 · 16.4초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/12_jeddah.png
- [PASS] 걸어갈 경로 찾기: 제다 → 험한 지름길 — 61칸
- [PASS] 걸어서 도착: 제다 → 험한 지름길 — 61.2칸 · 15.3초 · 평균 속도 4.05칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_shortcut — 61.2칸 · 15.3초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/13_shortcut.png
- [PASS] 걸어갈 경로 찾기: 험한 지름길 횡단 → 사나 — 62칸
- [PASS] 걸어서 도착: 험한 지름길 횡단 → 사나 — 62.3칸 · 16.4초 · 평균 속도 3.86칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_sanaa — 62.3칸 · 16.4초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/14_sanaa.png
- [PASS] 걸어갈 경로 찾기: 사나 북문 → 안전한 큰길 — 51칸
- [PASS] 걸어서 도착: 사나 북문 → 안전한 큰길 — 51.0칸 · 12.3초 · 평균 속도 4.18칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_highroad — 51.0칸 · 12.3초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/15_highroad.png
- [PASS] 걸어갈 경로 찾기: 안전한 큰길 횡단 → 제다 북문 — 98칸
- [PASS] 걸어서 도착: 안전한 큰길 횡단 → 제다 북문 — 98.2칸 · 24.5초 · 평균 속도 4.05칸/초 · 짐 0/10
- [PASS] 탐험 일주 도착: arabia_jeddah — 98.2칸 · 24.5초
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/16_jeddah.png
- [PASS] 본편 저장 파일 그대로
- [PASS] 본편 저장 파일 변경 없음 (시험 전후 해시) — E5BB55D861DC → E5BB55D861DC
이전 아라비아 시험 저장을 복원

결과: 모든 항목 통과
