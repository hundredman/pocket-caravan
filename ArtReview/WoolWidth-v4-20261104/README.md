# 양모 폭 조정 v4 — 승인·본편 적용/검사 완료

사용자 추가 요청: 양모 좌우 폭을 살짝 줄임. 승인된 v3 전체 아이콘을 Aseprite 오프라인 원본에서 가로만14→12px(양쪽1px)로 줄였다. 캔버스16×16, 세로 위치/높이12px, 팔레트/이진 알파는 유지한다. 본편 생성 중 변형은 없고 별도 Native PNG/Aseprite로 저장했다. Tools/ArtTrials/wool_width_v4.lua에서 재현한다. grain/spice 두 파일은 검토 문맥을 위한 v3 원본 그대로다.

comparison.png 왼쪽 승인본/오른쪽 폭 조정안, 정수14배/실제1배. inventory-comparison.png는 실제 InventoryUI에서 임시 ItemData 복사본으로 찍은 전후960×540 2장이다. 공용 DB의 등록 여부는 그대로 두고 복사본 그림만 바꿨다. 자동 Play12확인, 실제 본편 아이콘/저장은 바꾸지 않았다.

사용자 “좋다.” 승인 후 폭 조정안 PNG/Aseprite를 본편 경로에 복사하고 A2 목록/승인 재현 도구를 갱신했다. Unity 재임포트 뒤 사용자 중지 요청으로 저장했다가 재개했고, 실제 등록 상품 소지품 창 촬영·정적360 두 환경·월드0오류·Unity8종·저장9파일 보존을 새로 확인했다. 검토용 ItemData 복사본 대신 실제 등록된 ItemData를 그대로 사용했다. 검토 PNG/Aseprite 재내보내기는 디코딩 RGBA 일치. 원본·기존 캡처를 덮어쓰지 않았다. 사람/온라인 검사는 하지 않았다.

최종 적용 검사: Docs/Validation/WoolWidth-Applied-2026-11-04.
