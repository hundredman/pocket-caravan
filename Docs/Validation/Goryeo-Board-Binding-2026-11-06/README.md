# 승인 게시판 레이아웃 본편 적용

- 사용자 “게시판 수정안 승인” 후 공용 Contracts/ContractBoard.cs 레이아웃4줄만 반영했다. 승인 원문/원본·검토 코드 지문은 ArtReview/Goryeo-Trade-Quest-20261106/approval.json, 적용 지문은 ArtReview/Goryeo-Board-Binding-20261106/binding.json이다.
- 자동Play54확인/네이티브9캡처: 고려480×270/960×540, 수락 메시지/납품 목록·완료 메시지, 베네치아·아라비아·북유럽640×360. 각 캡처를 눈으로 확인해 제목/닫기와 메시지가 창 안에 보임을 점검했다.
- 승인 검토본/본편의 불투명 패널456×246에서 RGBA 픽셀 차이0. PNG 파일 바이트 비교나 전체 화면 동일성을 주장하지 않는다. 원래 제안한 창/목록 높이·너비와 같다.
- 창은 Interact 함수로 열었고 Esc는 가상 Keyboard→일반 입력이다. 계약 상태/메시지는 실제 Accept/Deliver 결과로 준비했다. 사람 직접 입력·네이티브 클릭·실제 저장/디스크 오류·재시작/온라인/FPS·빌드는 미검사다.
- Python400 두환경 OK(23/32skip), 월드0오류1437출구·Unity8종54씬/콘솔error0. 사용자저장9파일 백업/해시불변·임시슬롯0. 화면 시험은 저장OFF다.
- 기존 Packages/ProjectSettings/3D/발표 변경을 보존하고 커밋하지 않는다. 최신main을 합친 로컬커밋만 하며 push/PR/빌드는 하지 않는다.
- R3/R4 병합/기능 검증은 별도 미완료다. 이번 승인은 게시판 레이아웃만이며 흥정/회복을 대신 승인한 것이 아니다.

승인 레이아웃 적용 후 R3/R4 dry merge도 다시 확인했다. ContractBoard.cs·TownMarket.cs·Plan/WorkLog 내용 충돌이며 실제 병합/인덱스 변경은 하지 않았다. 충돌 결과를 r3-r4-merge-preview.txt에 보존했다.
