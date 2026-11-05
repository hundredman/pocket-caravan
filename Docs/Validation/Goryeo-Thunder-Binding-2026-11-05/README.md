# ATM-B5 승인 빛·천둥 본편 연결 (2026-11-05)

사용자 “이 표현과 빈도로 승인” 원문/NativeWAV/source/승인 당시 효과 코드의 SHA256을 thunder-approval.json에 기록했다. PromoteApproved로 지문 검증 후 Resources/Goryeo/Thunder에 WAV를 온전하게 복사하고 PCM32kHz/미리 로드/압축 없음으로 가져왔다. 이름을 GoryeoThunder로 바꾸고 GUID를 보존했다. 빛.07/0.4초·음원5.5초·지연0.7초·음량비.35는 승인본 그대로다.

native log Start(-150)에서 기존 날씨/일정 판정 뒤 GoryeoThunderDirector.Apply를 호출한다. 승인배치 활성 본편맵에만 효과/Trigger/AudioSource 각1개. 실제 장맛비/미해결·대화/전환 종료를 확인하고 entryWindow마다 세션1회만 재생한다. 성공한 재생만 static HashSet<int>에 기록하고 SubsystemRegistration에서만 비운다. 같은 구간 재진입·비활성/재활성·저장불러오기로 초기화하지 않는다. 다른맵/해결/구간만료·누락클립은 재생하지 않으며 맵 이탈 때 정리한다. 효과는 음소거/실제 효과음 설정을 따른다. 저장 필드/ID·시간/재산/충돌/맵·씬/공용Core·Economy·Quests·UI·AudioManager는 미변경이다.

## 실행한 검사

- 본편 GoryeoThunderProductionReview를 두 개의 실제 Editor Play 세션에서 실행했다. 두 번 모두 complete=true/55확인. 첫 세션은 production-first-session.json, 두 번째는 ArtReview/Goryeo-StormLogs-20261105/Unity/thunder-production.json이다.
- 매 세션 시작 기록0·자동 설치/승인 클립 참조/중복설치0·대화/전환 동안 기록 미소비·첫재생/같은구간 재진입무음·다음구간 자동1회·해결/누락/만료 차단·음소거중빛1회/무음·실제 지연재생/샘플 진행·효과음음량/0음량·비활성/재활성·다른4맵 무설치/기록 미소비·맵 이탈 소스정리·재산불변/저장OFF.
- 실제480×270 thunder-production.png. 기존 검토 GIF/소리는 보존하며 새 아트를 만든 것이 아니다.
- Python 원본 지문·정책범위/승인본과 본편 디코딩 PCM 일치2개 검사를 추가했다. 최종387개 두 환경 OK(numpy23skip/Pillow32skip), python-full/pillow.txt. 가져온 실제 AudioClip352000샘플을 승인 Native의 PCM과 비교해 최대차이0(imported-pcm.json).
- validate_world.py0오류/1437출구, Unity 필수8종(ValidateWorld/SceneTerrain/GoryeoRegion/TransitRegion/Q1/GoryeoExperience/TradeBalance/SaveCompat) 완료·최종콘솔error0.9저장 백업 후 SHA256 불변(save-preservation.json).

새 코드/검사는 컴파일·Play 오류 없이 통과했다. QA 도구의 준비/정적 회귀 검사와 사람 체감/실제 스피커·온라인/빌드·목표PC FPS/장시간 검사는 구분한다. 후자는 실행하지 않았다. 이전 ATM-B5 초안 재촬영 실패와 보강 기록은 별도 Goryeo-StormLogs-2026-11-05에 그대로 보존하며 이번 최종 본편55확인 결과와 섞지 않는다. 최신main 반영 후 로컬커밋만·push/PR/빌드 없음. B단계의 다른 항목은 사용자 선택 대기다.
