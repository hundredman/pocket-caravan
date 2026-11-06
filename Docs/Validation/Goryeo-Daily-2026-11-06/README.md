# ATM-B4 우물 검토 검사

- Python 전체399개 두 환경 OK(23/32skip). full은 numpy/scipy/Pillow, pillow는 Pillow만 설치했다. 일부 기존 테스트의 ResourceWarning/DeprecationWarning은 로그에 남아 있다.
- 월드0오류1437출구. Unity8종: World, SceneTerrain(54씬/263099칸), GoryeoRegion, TransitRegion, Q1, Experience, TradeBalance, SaveCompatibility. 각각 결과 파일을 보존했다.
- Play42확인/480×2704캡처: `play.json`. 실제 엔진 Play 안에서 QA 함수/날짜/위치 주입을 사용했다. 저장OFF 검토에서 활동하고, QA 임시슬롯만 수동 저장ON으로 바꿔 SaveNow/Read/Apply를 검사한 뒤 저장OFF/슬롯 삭제했다. 사람 입력/앱 재시작 검사가 아니다.
- 기존 사용자 저장9파일 해시 불변, 임시파일0. `save-preservation.json`.
- Unity 컴파일/콘솔error0. 그림은 ArtReview/Goryeo-Daily-20261106에 있으며 본편 연결 승인은 아직 없다.
- 최신 origin/main `f5e39581bf59d797b9072e544874de533c7c2c23`을 fetch/merge: Already up to date. push/PR/빌드 미실행.

최초 캡처는 우물이 대화창에 가려져 QA 카메라만5칸 아래로 바꾼 뒤 동일42확인과4캡처를 재실행했다. 본편 카메라/맵은 바꾸지 않았다. 상세 범위와 미확인 사항은 Docs/Goryeo/Daily-ATM-B4-2026-11-06.md 참조.
