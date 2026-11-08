# 지역 표시·시작 이야기 통합 검사

2026-11-08 Codex. 사용자 “일단 깃헙으로 올리자” 지시로 region/goryeo→integration 업로드/병합 진행. 현재 제품 변경은 최신integration ff9a9ccf6와 최신main을 포함한다.

## 들어가는 변경

- 고려 상인/낚시꾼 표시, 일꾼 물 위 옛 좌표 복원·원본 리소스 내보내기.
- 고려 I/M/Q 조작 순서/옛 튜토리얼 메모 이주, 팀 TopPill/Notify/Tab 숨김.
- 고려식 이름 위·Space 아래·네모 퀘스트 배지의 공용 연결. 타 지역 긴 이름 보존/자동 축약 없음.
- 문명 선택 전 시작되던 고려 도입을 선택 후 하단 이야기로 복구·옛step1복구/진행저장보존.
- 네 문명의 구매/판매 안내 두 문구 통일. 완료 조건/교역 규칙/퀘스트ID 변경이 아니다.
- 별도 시험 저장칸의 NetSave 요약/전체 동기화 차단. Aseprite 제한환경 실행 전 확인/정상 실행 경로.

## 새로 실행한 검사

- Python431개 통과/23skip (`static.txt`), 세계883출구/오류0 (`world.txt`). Aseprite는 승인된 정상 실행 경로에서 실제 내보내기를 실행했다.
- 최종 본편 Naru의 SaveOFF/별도슬롯·NetLogin 비활성·Offline 격리 실행. 옛메모 이주/반복/대화30확인 재실행, 콘솔0/개인저장9개SHA동일 (`unity.json`). 물리 키 전체 플레이는 아니다.
- 이전에 실행한 세부4지역8캡처/첫도입·저장복구/슬롯동기화·실제거래/로컬서버 저장·선물·백업 검사는 각 이전 Validation 기록을 따른다. 이번에 전부 재실행했다고 세지 않는다.
- 실제 온라인/장시간/FPS/빌드/다른지역 전체 수동 완주는 미검사.

공용 코드 변경: UI/WorldDisplayNames.cs, UI/Navigator.cs, Quests/StoryGuide.cs, Net/NetSave.cs, 기존 GoryeoPlaceLabels/Director의 공용 연결. 다른문명 전용 변경은 NorthIntro·VeniceIntro·ArabiaIntro의 안내 문자열2개씩뿐이다. Core/Economy/DB/WorldIndex/Connections/맵/씬/빌드목록/서버 제품코드 추가 변경0. 개인 패키지·ProjectSettings·3D실험은 제외한다.

첫 도입 검사 중 자동로그인/돈짐요약 전송 사고와 제한 사항은 Goryeo-Opening-2026-11-08/README.md에 남아 있다. 이를 없던 것으로 처리하지 않는다.
