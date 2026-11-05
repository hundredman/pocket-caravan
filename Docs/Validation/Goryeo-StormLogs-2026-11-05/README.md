# ATM-B5 장맛비·통나무 연결과 연출 시안

사용자 “추천안으로 진행” 후 기존 승인 위치 한 곳에 장맛비 조건만 추가했다. GoryeoLogVisuals.Start(-150)에서도 충돌 생성 전 해당 한 곳을 도착 시각으로 한 번만 확정한다. GoryeoStormLogs는 승인 A1 설정을 읽고 원래8시간구간50%를 통과시킬지 결정한다. RoadHazards.cs의 fallen_log 조건1곳을 연결했다. 다른 통나무·범람은 같은 FNV/원래 값, 지형/씬/원본그림/ID·확률/무료3초/재산·저장 불변. 기존 진입시 판정과 방문중 해결/재방문 때 원래 일정 재판정도 유지한다.

## 실제 실행

- ArtReview/Goryeo-StormLogs-20261105/Unity/play.json: complete=true/본편24확인/일정16425사례(365일×3구간×3시각×대상5). 대상은 장맛비&&원래50%, 다른맵/다른배치 통나무3종과 범람1종은 원래 일정과 같음. 비관리/잘못된 시각도 검사.
- 실제 본편 장면: 맑음·봄비에서 옛확률양성이어도 통나무 없음, 장맛비에도 확률음성이면 없음, 장맛비·양성은 실제길막음. 혼자3초 제거/2초직전 미해결·통행복원·같은방문 재적용 해결유지/재방문 원래일정·돈/짐 불변·저장OFF. 실제480×2704캡처. 최종 치운후 캡처는 상세창을 접어 통로를 보이도록 다시 촬영했다.
- Unity/thunder-play.json: complete=true/24확인/480×27012캡처. 새 효과는 검토모듈로만 실행: 자동재생0·명시적트리거·최대7%빛/지연실제소리·음소거/효과음음량·본편/맑음차단·소스정리. 원작합성PCM5.5초·피크.24/RMS.0332/양끝0(audio-check.json). 청감/그림 승인과는 별개.
- Python385개: numpy환경OK(23skip), Pillow환경OK(32skip), 순서대로 실행. validate_world0오류1437출구.
- Unity 필수8종 완료: ValidateWorld/SceneTerrain/GoryeoRegion/TransitRegion/Q1/GoryeoExperience/TradeBalance/SaveCompat. 각 결과파일 기록. 최종콘솔error0.
- Play 전9저장 백업 후 SHA256 불변(save-preservation.json).

최종 사진 재촬영에서 장맛비 확률음성 구간이 활성인 Play1실패를 발견했다(recapture-failure.json). 진입 시각/판정 로그를 추가한 재검사에서는 정상이라 원인을 확정하지 못했다. 콜백 누락/순서에 의존하지 않도록 native log Start에서 충돌 생성 전 최종 시각 판정을 보강했다. 콜백이 누락된 것처럼 초기 enabled=true를 강제로 주고 두 컴포넌트의 실제 Start를 실행하는 회귀 검사를 추가했다. 보강 뒤 같은 Play24검사를 두 번 반복해 통과했다. 첫 최종결과는 guarded-play-first.json, 두 번째는 ArtReview의 Unity/play.json/entry 기록으로 구분한다.

QA 코드에서 Runtime→Editor 사용자 타입 상수 참조로 첫 컴파일1실패, 경로리터럴로 수정 후 error0/최종검사 통과. MCP 재로드/인스턴스 재검색/Play 응답시간초과는 실제 편집기/QA개수를 확인해 중복실행 없이 복구했다. 이미지 묶기 첫 시도는 기본Python에Pillow없음으로 실패, 기존 전체환경을 사용해 성공했다. 새 패키지를 설치하지 않았다.

사람 직접/스피커청감/온라인/빌드·목표PC FPS/장시간은 미검사. 번개/천둥/세션당1회 빈도는 승인 전 본편 미구현이며 자동 설치/Resources 새음원 없음. 본편 조건 연결과 연출 승인 상태를 구분했다. 최신main 반영 후 로컬커밋만·push/PR/빌드 없음.
