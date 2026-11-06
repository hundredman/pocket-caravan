# ATM-B3 모닥불 움직임 검사

- 실제 Play: ArtReview/Goryeo-Seats-20261105/Motion/play.json, complete=true/150확인. 기존 앉기54회귀와 불꽃/불씨/낮밤빛·상한/카메라범위·비활성/본편거절/실제씬이탈/재산불변 포함.
- 실제480×27040움직임표본→20프레임 낮밤 GIF, 정지10캡처. GIF 재생간격120ms는 FPS 측정치가 아니다.
- motion-summary: 낮밤20표본씩4프레임/최대3불씨·빛 범위, 원래4PNG를 HEAD와 RGBA 픽셀 비교해 보존 확인, 밤 주변 바닥의 실제RGB 변화 관찰.
- 첫 화면밖 검사 실패는 initial-culling-failure.json. 직교 카메라 Transform 판정으로 바꾸고 렌더3프레임 뒤 camera/viewport 및 표시0을 실제 기록했다. 기존 실패 원인의 투영행렬/갱신 순서는 완전 분리하지 않았다.
- Python391 두환경 OK(23/32skip), 월드0오류1437출구. Unity8종/54씬 JSON·최종 콘솔error0.
- 사용자 저장9파일 백업/최종 SHA256 불변. 검사 중 저장은 OFF.

검토 전용·승인/본편 연결은 아직이다. 사람 키보드·온라인·저장ON·빌드/목표PC FPS·장시간은 하지 않았다. 소리 추가 없음.
