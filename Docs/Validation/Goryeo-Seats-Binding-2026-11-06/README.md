# ATM-B3 승인 본편 연결

- production.json: 최종 자동Play128확인/실제480×27011캡처. 초기116확인은 initial-production.json.
- 저장ON: 일회용 codex_seat_qa_* 슬롯에서 네 출신×세 자리 JSON의 물리 좌표·출신/씬/돈/짐 확인·기본 회복/해제·컨트롤러 비활성·중복·생성 불/입자/빛 확인.
- 복원: 실제 저장 JSON의 위치를 pendingExact에 넣어 실제 씬을 다시 로드. 앉기 상태가 남지 않는 것과 불판 겹친 좌표 유지·발바닥 전체 탈출/재진입 차단을 검사. 앱 재시작/시작화면 버튼을 사람이 누른 것은 아님.
- 픽셀: 검토 raw와 본편 import Sprite를 같은 시간/불꽃 위상으로 고정. 캐릭터 영역 x228~252/y91~125(아래 원점) RGBA 차이 네 출신 모두0. 전체 화면차이0은 아님.
- Naru/transit_ferry/venice_rialto/arabia_ahsa/north_naru5씬은 자리·불·필드0. 모터로 불 진입 차단/겹친 저장 탈출 확인.
- Python398 두환경 OK(23/32skip). 최초 정적1실패는 Unity JSON 기본값/float32 비교 정규화 뒤 정정. 마지막 목록 보강은 final-native-and-route.txt에서2검사 재통과.
- Unity8종/54씬·월드0오류1437출구·최종 콘솔error0. 사용자저장9해시 불변/임시슬롯0.

사람 키보드/온라인/시작화면 이어하기·장시간/목표PC FPS·빌드 미검사. 저장형식/공용Core/Economy/Quests/UI·DB/WorldIndex/Connections/BuildSettings·맵/씬 미변경.
