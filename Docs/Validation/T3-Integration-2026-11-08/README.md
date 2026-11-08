# T3 integration 병합 검증 (2026-11-08)

`f1dd54ce2` 본편 경계/저장 이주와 앞서 승인한 공용 UI 통일 작업에 최신 integration `c803224e3`을 로컬 병합했다(`238095b2a`). 충돌 없이 MapJsonBuilder·ActivityHints·Social 양쪽 변경을 보존했다. 다른 지역 전용 파일·DB·서버 코드는 integration과 동일하다. 팀 온라인11단계·아라비아 변경도 그대로 포함한다.

- 전체 Python424개: 실패0·skip23(`static.txt`). Aseprite 사용 가능한 승인된 제한 밖 환경에서 실행했다.
- 등록 세계883출구: 오류0(`world.txt`).
- 병합 후 Unity 컴파일 error0. SaveOFF 본편17쌍34방향·나룻배3구간 재검사201확인(`routes.json`), 합성 옛저장 이주143720확인(`save-migration.json`). 개인 파일을 쓰지 않는 격리 슬롯으로 검사했다.
- 본편635출구/모터접근·풀밭왕복·타지역3왕복·13씬/지도 캡처는 앞선 [본편 검증](../T3-Production-2026-11-08/README.md)의 결과다. 이번 병합 뒤 이 캡처 전부를 다시 촬영했다고 세지 않는다.
- 사람 체감·전체 수동 Q1·실온라인/서버 저장·장시간FPS·빌드는 미검사.

사용자 업로드 지시에 따라 region/goryeo → integration PR로 올린다. 사전 공지를 기다리지 않고 검사 후 병합하며 사후 카톡 문안을 제공한다. 개인 패키지/설정·3D실험은 제외했다.
