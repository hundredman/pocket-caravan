# 승인 통나무 숲길 본편 연결 검사

최종 Python368개: 전체환경23skip/Pillow 전용32skip, 실패/오류0. validate_world 0오류/1437출구. Unity 지정8종 모두 실패 항목0, 콘솔 error0. 각 원문 로그 첨부. 승인 원본/A2는5개 정적 검사가 담당하며 PNG 동일성은 디코딩 RGBA로 비교한다.

ArtReview/LogBinding-20261104/Unity/play.json은 검토4방향132확인(전 칸 충돌·PlayerMotor 차단/제거 후 이동·이동 취소·3초무료 제거·재입장·겹침 발생 탈출·기존 사각/그림 유지). 그 장소는 배치 반려되어 본편 미적용이다. ChokeUnity는 최종 숲길 검토15확인/3캡처, ProductionUnity는 실제 본편15확인/3캡처(길목 전/중/후 연결, 어느 편에서도 한 사람의 상호작용 가능·3초 제거·재산 불변·빈 때/해결 후 유령 이름표 없음). 통행 API와 Interactable.Interact를 사용한 Play이며 사람이 키보드로 플레이한 검사는 아니다. 더 먼 전체 맵 우회 연결은 남아 있다.

기존 지형/ID/출구/길·JSON 모든 원래 필드 불변, 승인 obstacles/nativeLogs 두 메타데이터만 추가(map-preservation.json). source→MapJsonBuilder.BuildSelected로 transit_central_approach 씬1개만 재생성. 본편4방향 동시 배치가 아니라 승인 사선1개다. 본편 데이터가 바뀐 뒤 지정8종을 다시 실행했다. 사용자 저장9파일 사전 백업/불변(save-preservation.json).

중간 수정: A2 source kind generated를 ai_processed로 수정, 미세 가지 때문에9칸을 차단하던 초기 점유를 몸통 면적 기준으로 보완. 새 길목 시험에서 반대편 상호작용 접근 실패를 찾아 가장 가까운 몸통 칸으로 단일 표시를 옮기도록 수정. 본편 새 메타데이터로 과거 경계 회귀 해시1실패→승인 delta만 엄격 역변환하며 별도 원본/멱등성 검사 추가. 승인 상태 파일을 갱신하던 중 실행 중인 검사 프로세스가 이전 조건을 읽어1오류 발생, 파일 갱신 완료 후 양 환경 전체 재실행해 위 최종368통과. 실행하지 않은 검사로 대체하지 않았다.

사람 수동 플레이·온라인·빌드·날씨/벼락 연동 미검사/미구현. 기존 fallen_log ID·8시간창50%·무료3초·방문 단위 해결·저장 형식 유지. 새 재산 손실/수치 조정 없음. 다른 지역/씬/DB/WorldIndex/BuildSettings·사용자 기존 실험 변경 미포함. push/PR 없음.
