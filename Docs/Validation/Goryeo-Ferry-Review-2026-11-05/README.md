# ATM-B2 나룻배 검토 검사 (2026-11-05)

본편 미적용·요금/시간/직항구성·화면/도착점 사용자 승인 대기. 제안은 세 나루 상호직항, 1회8냥/게임1시간/짐·수레 유지다. 기존 사공과 그림/ID/지형·Scene·WorldIndex/DB/Connections/BuildSettings는 바꾸지 않았다. Map/TalkNpc.Interact 지역 훅1줄과 Editor에서만 활성화 가능한 GoryeoFerry 검토 모듈을 만들었다. 기본 config는null이며 SafeReview(previewOnly/저장OFF) 밖에서는 기존 대화 그대로다.

## 실행한 검사

- 최종 ArtReview/Goryeo-Ferry-20261105/Unity/play.json complete=true/102확인. 90%이상 적재한 손수레/기존 한지 소지품으로 세나루 사이 여섯 방향 모두 실제 GameManager.TravelTo/기존 창호지·지역명 전환을 실행했다.
- 출항 전 돈/시간 미변경→도착 확인 후 딱8냥/1시간·1회commit, 중복출항 거절, 사공 옆 정확한feet셀/실제 통행가능/출구아님·SpawnPoint 선등록, 원래 NPC 활성/수 유지·이야기 선택 기존대화, 취소/돈부족/미등록/원격 거절·짐/수레/출신 보존. 다른4맵(Gaegyeong/베네치아/아라비아/북유럽)에 새대화/도착점 없음.
- 최종 실제480×27014캡처, native크기 비교판 game-review.png. 초기 자동100확인은 기능검사였고 버튼폭 문제를 잡지 못했다. 캡처 시각 검토에서4개 긴 버튼이 잘림을 발견해 본문에 요금/시간을 옮기고 버튼명 단축, 최종102확인/재촬영했다. Initial 그림과 initial-play.json은 옛 기록으로만 보존한다.
- Python389개 두환경 OK: numpy23skip/Pillow32skip, 순서대로 실행.2개 새검사는 검토원본 재현·기존사공/정적안전 도착점과 본편 Resources 미설치/수치 review상태를 확인한다.
- validate_world.py:0오류/1437출구. Unity8종 ValidateWorld/SceneTerrain/GoryeoRegion/TransitRegion/Q1/GoryeoExperience/TradeBalance/SaveCompat 완료, 각 결과기록·최종콘솔error0.
- Play 전9저장 백업 후 SHA256 불변(save-preservation.json).

실제 키보드 조작·사람 플레이/온라인·빌드·목표PC FPS/장시간·본편 저장ON·실제 로드 실패/중단 복구는 이번에 확인하지 않았다. SafeReview는 저장OFF일 때만 돈/시간을 바꾸며 공유 GameManager 도착/저장 순서의 실제 본편 연결은 승인 후 검사한다. UI/수치 승인을 기술 검사로 대신하지 않는다. 최신main 반영 후 로컬커밋만·push/PR/빌드 없음.
