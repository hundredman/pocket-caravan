# 지역 일관성 수정 검증

2026-11-08 Codex. [구현·범위·그림](../../Goryeo/Region-Consistency-Fix-2026-11-08.md).

- `npc-name-source-lengths.json`: 다른 지역부터 확인한 이름 원본 길이. 동적 런타임 전체 목록은 아니다. 자동 길이 제한/추가 이름 단축 없음; 화면별 개별 검토로 사용자 확정.
- `static-sandbox-failure.txt`: 첫424개 중 Aseprite27건 SIGABRT 기록(23skip). `static.txt`: 같은 검사 제한 밖 재실행424통과/23skip. `anchor-tests.txt`: 신규2개통과. 합쳐426개/23skip이며 신규2개 추가 뒤 전체426을 한 명령으로 다시 실행한 것은 아니다.
- `world.txt`:883출구/오류0. `server-typecheck.txt`:TypeScript통과.
- `unity-results.json`:SaveOFF/Offline에서 이주30·모터접근·실제UI/장터/대화 동작. Shift/Tab 물리 키로8단계 수동 완주 기록은 아니다. `migration-check.cs.txt`는 실제 실행한 이주/대화 검사 코드.
- `*-labels.json`:실제 그린 이름과 배지의 겹침0(네 시작 씬의 해당 카메라 위치 한정). 아흐사 대추야자 따기 활동명1개는 기존 겹침 생략 정책에 걸림. 배치/이름은 임의 변경하지 않음. 전체 맵 겹침0을 뜻하지 않는다.
- `preservation.json`:개인 저장9개SHA동일. 코드 편집/수신은 Unity정지 상태에서 수행했고 최종컴파일/콘솔오류0.
- `server-fullsave.txt`, `server-extras.txt`, `server-backup.txt`:팀 기존 검사 스크립트를 임시 소스사본·가짜키·별도/tmp저장소의 `127.0.0.1:18787` Wrangler local 서버에서 순서대로 실행, 모두통과. 원격 리소스 바인딩 ID/실제.dev.vars를 사용하지 않았다. 백업 시험의 전체 복원은 이 임시 저장소에만 적용됐다. 실서버/실계정/실제 매일 실행은 미검사.

검사 중 잘못된 `SupplyWeight` API를 조회한 일회성 도구 C#은 컴파일되지 않았으며 제품 코드에는 쓰지 않았다. 실제 API를 읽고 TownData.stock/실제 TownMarket Buy/Sell로 다시 검증했다.

현재 최신main/integration포함·로컬만, 그림승인/원격반영대기. 개인 설정·패키지·3D 실험 변경은 커밋하지 않는다. 서버 제품코드/타지역전용 데이터는 추가 수정하지 않았다.
