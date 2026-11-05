# ATM-B2 승인 나룻배 본편·저장 연결 (2026-11-05)

게임시간 설명 뒤 사용자 “일단 좋은 것 같아.”를 승인으로 기록하고 원본 source지문/8냥·1시간을 검증해 Resources/Goryeo/Ferry/Config로 연결했다. 기존3사공/배/위치·ID·그림/지형은 그대로다. 사용자 질문으로 정박 배3곳도 구역도/실제 캡처와 대조했다(moored-boats.json). 버들 배는 사공 오른쪽 아래, 메뉴에 가려져 있었다.

## 실행한 검사

- ArtReview/Goryeo-Ferry-20261105/Unity/production.json complete=true/138확인. 실제 저장ON인 별도 일회용 SaveSystem.UseSlot에서6방향을 기존 GameManager.TravelTo/창호지·지역명 전환으로 이동했다. 저장ON은 QA에서만 private autosave 필드를 설정했고, 실제 코드에는 reflection이나 공용 저장 설정 변경을 넣지 않았다.
- 기존 한지/90%이상 적재손수레로 모든 직항: 원래 NPC·이야기/취소·돈부족/원격/미등록/중복 거절, 출항전 현재 진행 저장·돈/가산시간 미변경·같은 출항파일, 도착Spawn 선등록/통행가능/출구아님·정확한1회8냥/운항1시간·짐/수레/출신 보존.
- 공유 SaveNow가 실제 쓴 JSON을 매 도착 읽어 목적지/차감액/시각/위치/운송수단을 확인했다. 수치 분리는 Time.timeScale=0에서 운항가산분을 검사했으며, 일반 시계가 전환 중 진행하는 기존 동작은 바꾸지 않는다.
- 저장 차단과 목적지 높이 불일치를 주입해 출항/돈/시간 변경 없음. pending요청의 owner/slot을 변조한 실제 이동2건은 요금/가산시간/완료카운터를 적용하지 않고 저장도 무차감. 요청 guard주입 검사이며 실제 프로세스 재시작/다른 계정 플레이를 했다고 쓰지 않는다.
- 정상 도착 칸을 메모리상 잠시 막고 초기화 함수를 실행해 NPC 인접한 안전한 비출구 칸을 고르는지 검사·즉시 원복했다. Maps/씬 파일은 고치지 않았다.
- 본편 실제480×27013캡처(오리지널 검토는102확인/14캡처). 예전 문서의 검토15장 집계는14로 정정했다. 초기 본편129확인은 initial-production.json, 최종138은 production.json이다.
- Python389개 두환경 OK(numpy23skip/Pillow32skip). 승인 원본/Resource정책/정적 안전 도착점 검사 포함, 순차 실행했다.
- validate_world0오류/1437출구, Unity8종 ValidateWorld/SceneTerrain/GoryeoRegion/TransitRegion/Q1/GoryeoExperience/TradeBalance/SaveCompat 완료·최종콘솔error0.
- 사용자9저장 백업/후 SHA256 불변. 일회용 codex_ferry_qa_* .json/.bak/.tmp는 검사후 제거했고 남은 파일0(save-preservation.json).

초기 목적지 검증에서 존재하지 않는 WorldIndex.MapInfo.width를 참조해1컴파일실패, 실제 제공되는 height API로 정정 후 통과했다. 폭은 도착한 실제 RoadGrid에서 검증한다. 사용자 파일/기존 미커밋 변경은 포함하지 않는다. 공용Core/Economy/Quests/UI·DB/WorldIndex/Connections/BuildSettings·맵/씬은 미변경이다.

실제 Unity 엔진의 로드 오류/프로세스 강제종료·사람 키보드/온라인·빌드/목표PC FPS/장시간 검사는 하지 않았다. 최신main 반영 뒤 로컬커밋만·push/PR/빌드 없음.
