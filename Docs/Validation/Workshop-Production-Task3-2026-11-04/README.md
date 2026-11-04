# Task3 승인 공방 본편 검사

닥목 한지 공방 (37,53), goryeo_hanji/paper. 승인한 기존 작업장 그림과 배치 유지. goryeo_life --workshops-only와 MapJsonBuilder.BuildSelected("Dakmok")로 생성했다. 다른 맵 데이터/지역 레시피·DB·WorldIndex·BuildSettings는 변경하지 않았다.

실제 본편 씬에서 입력 이벤트 Space로 주문·대기·수령, 재진입 중 주문/비용 보존, 기존 SaveData v2의 메모 필드만으로 JSON 복원, 하루2회·다음날·만재 보관 등33확인 통과(play.json). 검토 부트스트랩에서 자동 저장을 끄고 기존 저장9파일 백업/불변 확인. 사람이 직접 조작한 플레이·온라인 검사는 하지 않았다.

Python356개: numpy/scipy 환경23skip, Pillow전용32skip, 모두 실패0. Pillow 첫 실행은 Aseprite 내보내기 프로세스 멈춤으로 중단/오류1, 순차 재실행24초 통과. 월드0오류/1437출구, 지정 Unity8종 실패0. PNG는 디코딩한 RGBA로 비교한다.
