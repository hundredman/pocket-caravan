# 아라비아 자동 보행·화면 캡처 시험 2026-10-26 21:26:29 · Unity 6000.3.26f1
본편 저장 파일: /Users/euninjun/Library/Application Support/DefaultCompany/PocketCaravan/pocket_caravan_save.json (시작 해시 E5BB55D861DC)
기존 아라비아 시험 저장을 .manual-backup으로 보관 후 새 시험 시작

## 자동 보행 (실제 이동·충돌 처리)
- [PASS] 아라비아 시험 세션·전용 저장
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/01_ahsa_start.png
- [PASS] 마을 사람 거닐기 (12초) — 7/8명 이동 · 걷는 방향 4가지
- [PASS] NPC 4방향 그림 (앞·뒤·옆이 서로 다름) — 대추야자 농부
- [PASS] 걸어갈 경로 찾기: S0 → 힌드바드 — 21칸
- [PASS] 걸어서 도착: S0 → 힌드바드 — 21.3칸 · 5.5초 · 평균 속도 3.98칸/초 · 짐 0/10
- [FAIL] 힌드바드가 Space 대화 대상
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/02_hindbad_offer.png
  (화면) /Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-team/Docs/Arabia/Reports/Screens/03_q1_accepted.png
- [PASS] 적재량에 따른 감속 (같은 칸 기준) — 빈 등짐 3.80 → 의뢰품 4/10 3.34칸/초
- [PASS] 걸어갈 경로 찾기: 힌드바드 → 알 아흐사 동쪽 출구 — 23칸
- [FAIL] 시험 진행 — 시간 초과: 힌드바드 → 알 아흐사 동쪽 출구 출구 판정
- [PASS] 본편 저장 파일 변경 없음 (시험 전후 해시) — E5BB55D861DC → E5BB55D861DC
이전 아라비아 시험 저장을 복원

결과: 실패 항목 있음
