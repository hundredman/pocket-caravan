# Task6 승인 아이콘/신규 상품 본편 연결

승인 v3 전체16×16 아이콘3종 복사·Aseprite 원본 보관·A2 catalogue 검증0오류. grain 기존 상품은 아이콘 참조만 변경하고 공용 grain.png/상품ID·가격·무게는 유지했다. 새 두 품목은 기존 ItemData 기본 거리계수0.25 및 E2의 기준가/무게/공급 재고·배율을 썼다. 수요의 미지정 재고는 해당 마을 defaultOptimal. 기존 항목/재고값 유지 검사. 변경 공용 파일은 Resources/GameDatabase.asset(두 참조 추가)이다. 맵·씬·WorldIndex·BuildSettings·Core/Economy/UI 코드는 바꾸지 않았다.

실제 본편 두 장터에서 TownMarket.Interact/Buy/Sell 경로를 호출했다(가상 키 입력이나 사람이 한 플레이가 아니다). 공급지→개경의 새 두 경로 양수, 8회 같은 장터 매입→재계산→판매 모두0이하(최대−2), 기존 v2 및 신규품목/시장재고 v2 메모리 JSON 왕복 복원 등 Play32확인. 실제3캡처. 저장9파일 백업/불변, 온라인/장거리 실제 보행·경제 밸런스 최종 검사 없음. 신규 품목 이익54/35는 옛 E2 예상과 달라 Claude 검토 메모를 남겼다. 게임 빌드/원격 반영 없음.

정적360개 두 환경 실패0(numpy/scipy23skip/Pillow전용32skip), 월드0오류/1437출구·Unity 지정8종 통과. 첫358개 실행은 E1 calibration 입력해시 갱신 누락으로1실패, 보고서 원본 make_report로 입력해시만 갱신(모든 거리/결과 불변) 후360개 재실행 통과. R2 역사 자료는 승인된 grain 아이콘 참조와 개경 새 재고 블록만 되돌리는 exact delta로 원래 해시를 계속 검사한다.

양모 폭 조정 v4는 별도 검토 전용(14→12/높이 유지/원본 팔레트)으로 실제 소지품 창12확인/2캡처. 본편에 미적용. ArtReview/WoolWidth-v4-20261104 참조.
