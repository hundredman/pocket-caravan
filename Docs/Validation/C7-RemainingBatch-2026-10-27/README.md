# C7 잔여 일괄 검증

- main-c6-tests.txt: 12검사 통과. PNG 비교는 decoded RGBA.
- main-r2-tests.txt: 5검사 통과. main의 공용 GameDatabase/Origins 과거 해시 제외 수정 유지.
- batch-tests.txt: 8검사 통과. A2 검토 성공/미승인 본편 거부,87칸 재구성, Aseprite12개 재출력, 원본/이전 승인 PNG353개 보존, 세 화면 전후 범위·RGB 오차≤1, 볏단 폭.
- Unity: 독립 Editor 카메라6장/480×270. 최종 Screens/complete.json 생성. 로그에 C# error CS 및 SearchDatabase 오류 없음. 최종 실행의 종료코드 원문은 도구 출력 잘림으로 보존하지 못했으므로 종료0을 주장하지 않는다. 촬영 산출물은 별도8검사로 확인했다.
- protected-check.json: 자체 작업 후 main 갱신 전 보호100파일 변경/추가0. PNG는 크기+RGBA 비교.
- Play/수동 플레이/이동·저장·충돌/전체 통합/게임 빌드 미실행. 본편·공용 파일 연결 없음.
