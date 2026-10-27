# 아라비아 자동 플레이 시험 2026-10-27 09:22:46 · Unity 6000.3.26f1
본편 저장 파일: /Users/euninjun/Library/Application Support/DefaultCompany/PocketCaravan/pocket_caravan_save.json (시작 해시 E5BB55D861DC)
기존 아라비아 시험 저장을 .manual-backup으로 보관 후 새 시험 시작

## 1단계 — 새 세션, Q1 배달, 저장
- [PASS] 아라비아 시험 세션 시작
- [PASS] 전용 저장 파일 사용 — pocket_caravan_arabia_test_save.json
- [PASS] 자동 저장 켜짐 (전용 파일)
- [PASS] 시작 자금 = 세션 설정 — 100
- [PASS] 출신 = 아라비아, 등짐 — arabia/walk 적재 10
- [PASS] S0 스폰에서 시작
- [PASS] 시작 직후 본편 저장 파일 그대로
- [PASS] 힌드바드 머리 위 표식 = 받을 의뢰(!)
- [PASS] Q1 완료 전 마리암은 Q2를 제안하지 않음
- [PASS] 짐이 가득하면 Q1 수락을 막고 이유 안내 — 짐이 꽉 찼구먼. 짐을 좀 덜고 오게.
- [PASS] 짐꾼 힌드바드 옆에 설 자리
- [PASS] 짐꾼 힌드바드 대화 대상 인식 (Space 대상) — 짐꾼 힌드바드
- [PASS] 힌드바드 대화창에 Q1 제안·보상 표시 — 의뢰 1/1 · [힌드바드의 반쪽 짐]
- [PASS] 대화창 '수락' 선택
- [PASS] Q1 진행 중·맡긴 대추야자 2개 받음 — 보유 2
- [PASS] 힌드바드 표식 = 진행 중
- [PASS] 의뢰품 적재로 짐 비율 증가 (감속 반영) — 짐 4/10
- [PASS] 맡긴 짐은 장터에 팔 수 없음 — 맡은 물건은 팔 수 없습니다.
- [PASS] arabia_ahsa에 arabia_farmroad 출구 있음
- [PASS] 이동 arabia_ahsa → arabia_farmroad: 도착 스폰 arabia_farmroad_spawn_seam_ahsa_1 — 발 위치 (1, 19, 0)
- [PASS] arabia_farmroad 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_farmroad 길 통과 시간 +3시간 — 3.1시간
- [PASS] arabia_farmroad에 arabia_jeddah 출구 있음
- [PASS] 이동 arabia_farmroad → arabia_jeddah: 도착 스폰 arabia_jeddah_spawn_seam_farmroad_0 — 발 위치 (1, 37, 0)
- [PASS] arabia_jeddah 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_jeddah에 arabia_zabid 출구 있음
- [PASS] 이동 arabia_jeddah → arabia_zabid: 도착 스폰 arabia_zabid_spawn_seam_jeddah_1 — 발 위치 (29, 1, 0)
- [PASS] arabia_zabid 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] 부엌 담당 표식 = 완료 가능(?)
- [PASS] 공동 부엌 담당 옆에 설 자리
- [PASS] 공동 부엌 담당 대화 대상 인식 (Space 대상) — 공동 부엌 담당
- [PASS] 짐이 모자라면 납품 거절·이유 안내 — 맡긴 대추야자 2개가 필요하네. (지금 1개)
- [PASS] 납품 대사 — 기다리던 짐이구나. 네 이름도 장부에 적어 둘게.
- [PASS] Q1 완료: 보수 +20, 신용 +1, 의뢰품 제거 — 돈 100→120, 신용 0→1
- [PASS] 다시 말 걸어도 보수 중복 지급 없음
- [PASS] Q1 완료 후 마리암이 Q2 제안 가능
- [PASS] 저장하기 (전용 파일)
- [PASS] 전용 저장에 Q1 완료·현재 씬 기록
1단계 종료: Play를 멈춘 뒤 알 아흐사에서 다시 Play해 이어 불러오기를 확인

## 2단계 — 이어 불러오기, Q2 구매·납품
- [PASS] 이어 불러오기: 저장된 씬(자비드)·돈·신용·Q1 완료 복원 — 돈 120, 신용 1
- [PASS] 이어 불러오기도 전용 저장 파일 사용
- [PASS] arabia_zabid에 arabia_jeddah 출구 있음
- [PASS] 이동 arabia_zabid → arabia_jeddah: 도착 스폰 arabia_jeddah_spawn_seam_zabid_0 — 발 위치 (8, 42, 0)
- [PASS] arabia_jeddah 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_jeddah에 arabia_farmroad 출구 있음
- [PASS] 이동 arabia_jeddah → arabia_farmroad: 도착 스폰 arabia_farmroad_spawn_seam_jeddah_3 — 발 위치 (62, 6, 0)
- [PASS] arabia_farmroad 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_farmroad 길 통과 시간 +3시간 — 3.1시간
- [PASS] arabia_farmroad에 arabia_ahsa 출구 있음
- [PASS] 이동 arabia_farmroad → arabia_ahsa: 도착 스폰 arabia_ahsa_spawn_seam_farmroad_0 — 발 위치 (62, 39, 0)
- [PASS] arabia_ahsa 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] 마리암 표식 = 받을 의뢰(!)
- [PASS] 여관 주인 마리암 옆에 설 자리
- [PASS] 여관 주인 마리암 대화 대상 인식 (Space 대상) — 여관 주인 마리암
- [PASS] 마리암 대화창에 Q2 제안·보상 — 의뢰 1/1 · [여관의 저녁 준비]
- [PASS] 대화창 '수락' 선택
- [PASS] Q2 진행 중 (수락 시 돈 변화 없음)
- [PASS] 알 아흐사 상인 옆에 설 자리
- [PASS] 알 아흐사 상인 대화 대상 인식 (Space 대상) — 알 아흐사 상인
- [PASS] 다른 장터(알 아흐사) 구매는 Q2 구매 단계로 세지 않음 — 곡물 꾸러미를 8에 샀습니다.
  (참고) 알 아흐사 곡물 1개 매입/재판매: 120→118냥 · 곡물 꾸러미를 6냥에 팔았습니다. 손익 -2냥.
- [PASS] arabia_ahsa에 arabia_farmroad 출구 있음
- [PASS] 이동 arabia_ahsa → arabia_farmroad: 도착 스폰 arabia_farmroad_spawn_seam_ahsa_1 — 발 위치 (1, 19, 0)
- [PASS] arabia_farmroad 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_farmroad 길 통과 시간 +3시간 — 3.1시간
- [PASS] arabia_farmroad에 arabia_jeddah 출구 있음
- [PASS] 이동 arabia_farmroad → arabia_jeddah: 도착 스폰 arabia_jeddah_spawn_seam_farmroad_0 — 발 위치 (1, 37, 0)
- [PASS] arabia_jeddah 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_jeddah에 arabia_zabid 출구 있음
- [PASS] 이동 arabia_jeddah → arabia_zabid: 도착 스폰 arabia_zabid_spawn_seam_jeddah_1 — 발 위치 (29, 1, 0)
- [PASS] arabia_zabid 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] 자비드 장터 = 자비드 마을
- [PASS] 자비드 상인 옆에 설 자리
- [PASS] 자비드 상인 대화 대상 인식 (Space 대상) — 자비드 상인
- [PASS] 돈 부족: 구매 안 됨·이유 안내 — 돈이 모자랍니다.
- [PASS] 짐 가득: 구매 안 됨·이유 안내 — 더 실을 수 없습니다.
- [PASS] 실패한 구매는 Q2 진행에 반영되지 않음
- [PASS] 자비드에서 곡물 2개 구매 → Q2 납품 단계로 — 곡물 꾸러미를 5에 샀습니다. / 곡물 꾸러미를 5에 샀습니다. · 구매비 10냥
- [PASS] arabia_zabid에 arabia_jeddah 출구 있음
- [PASS] 이동 arabia_zabid → arabia_jeddah: 도착 스폰 arabia_jeddah_spawn_seam_zabid_0 — 발 위치 (8, 42, 0)
- [PASS] arabia_jeddah 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_jeddah에 arabia_farmroad 출구 있음
- [PASS] 이동 arabia_jeddah → arabia_farmroad: 도착 스폰 arabia_farmroad_spawn_seam_jeddah_3 — 발 위치 (62, 6, 0)
- [PASS] arabia_farmroad 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] arabia_farmroad 길 통과 시간 +3시간 — 3.1시간
- [PASS] arabia_farmroad에 arabia_ahsa 출구 있음
- [PASS] 이동 arabia_farmroad → arabia_ahsa: 도착 스폰 arabia_ahsa_spawn_seam_farmroad_0 — 발 위치 (62, 39, 0)
- [PASS] arabia_ahsa 도착 칸이 출구가 아님 (즉시 재이동 없음)
- [PASS] 마리암 표식 = 완료 가능(?)
- [PASS] 여관 주인 마리암 옆에 설 자리
- [PASS] 여관 주인 마리암 대화 대상 인식 (Space 대상) — 여관 주인 마리암
- [PASS] Q2 납품 대사 — 이제 한 줄을 네 힘으로 채웠구나. 다음에 무엇을 나를지는 네가 골라 보렴.
- [PASS] Q2 완료: 곡물 2개 제거, 보수 +30, 신용 +1 — 돈 108→138, 신용 1→2
- [PASS] Q2 실제 이익 = 보수 − 구매비 > 0 — 30 − 10 = 20냥
- [PASS] 다시 말 걸어도 Q2 보수 중복 없음
- [PASS] 선지출 없는 재기 통로: 알 아흐사 게시판 배달 의뢰 (아라비아 안 목적지만) — 3건 → arabia_taiz, arabia_sanaa
- [PASS] 저장하기 (전용 파일)
- [PASS] 전용 저장에 Q2 완료 기록
- [PASS] 본편 저장 파일 그대로 (Play 중)
- [PASS] 본편 저장 파일 변경 없음 (시험 전후 해시) — E5BB55D861DC → E5BB55D861DC
이전 아라비아 시험 저장을 복원

결과: 모든 항목 통과
