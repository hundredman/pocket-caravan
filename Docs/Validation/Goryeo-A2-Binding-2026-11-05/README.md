# A2 승인 음악 본편 연결 · A6 수치 제안 (2026-11-05)

사용자 “A2 이대로 승인” 원문과 source/4WAV SHA256을 approval.json에 기록했다. PromoteApproved는 지문 검증 후 Native4곡을 변형 없이 Resources/Goryeo/TimeMusic으로 복사한다. Unity PCM/원본32kHz/미리 로드 설정으로 반복 경계를 보존한다. 전체 PCM 약24MB이며 기존 road/danger·나머지 원곡 파일은 미변경이다. 전용 Director가 소유 본편13맵에서 지속 GameManager에 모듈 하나를 연결한다. 공용 AudioManager/다른 문명/맵/씬·ID/저장 형식은 바꾸지 않았다.

## 실행한 검사

- Python unittest385개: numpy 환경 OK(23skip), Pillow 환경 OK(32skip). 순서대로 실행했다. 원작 score/PCM 특성·승인지문/본편 복사 동일성 검사 포함.
- validate_world.py:0오류,1437출구. 생성물·저장 미변경.
- Unity8종: ValidateWorld, SceneTerrain(54맵), GoryeoRegion, TransitRegion, Q1, GoryeoExperience, TradeBalance, SaveCompatibility. 각 결과를 이 폴더에 저장했다. 최종 콘솔 error0.
- 실제 Play 최종 PCM: ArtReview/Goryeo-Atmosphere-A2-20261105/Unity/production.json complete=true/89확인, 선택624조건. 실제13소유맵 설치/단일모듈/곡선택,3외부맵 원래음악 복원, 실제 재생 위치 진행·시간대 전환 두 곡 equal-power 페이드·같은 시간대 마을 이동 재시작0·기존road/danger 우선/해제·음량/음소거·누락/끄기/재활성/파괴 정리·효과음/재산 불변·저장OFF.
- Play 전9저장 백업, 이후 SHA256 모두 같음. save-preservation.json.

초기 MCP 호출 형식 누락과 스크립트 재로드 전 PromoteApproved 호출은 실행되지 않았다. 재컴파일 후 정상 실행했다. Unity importer의 옛 preloadAudioData 속성 접근은 컴파일 단계에서 거절되어 실제 변경 없이 새 sample settings API로 정정했다. 최종 PCM 가져오기 후 Play를 다시 실행했다.

사람 직접 플레이·스피커/헤드폰 청감·온라인/빌드/목표PC FPS·장시간 검사는 하지 않았다. 사용자 청감 승인을 기술 검사로 대체하지 않는다. A6는 별도 문서의 흙길 봄비97%/장맛비94% 제안이며 승인 전 코드 미구현. B단계 미착수. 최신 main 병합 확인 후 로컬 커밋만, push/PR/빌드 없음.
