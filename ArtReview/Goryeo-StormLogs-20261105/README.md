# 장맛비 통나무·번개/천둥 검토

통나무 장맛비 연결은 사용자 “추천안으로 진행” 후 본편 적용했다. decision.json은 그 범위만 승인 기록이다. **사용자 “이 표현과 빈도로 승인” 후 빛·천둥도 본편 연결했다.** thunder-approval.json에 승인 원문/원본 지문을 보존했다.

- game-comparison.png: 실제480×270 네 장을 원본 크기로 묶음. 맑음/봄비에는 통나무 없음, 장맛비와 기존50%가 맞으면 길막음, 혼자3초 치운 뒤 통행 복원.
- thunder-review.gif: 실제 Unity 검토 장면12캡처. 최대7% 푸른 빛 한 번/0.4초 페이드. GIF 마지막 정지시간은 보기 편하게 늘렸으며 실제 빈도가 아니다.
- thunder-review.mp3: 원작 직접 합성한 낮은 천둥5.5초 들어보기. 실제 미리보기는 빛 시작0.7초 뒤 재생한다. 현장 녹음/기존 팀 음원을 썼다고 표시하지 않는다.
- Native/distant-thunder.wav, source.json, audio-check.json:32kHz stereoPCM/피크.24/RMS.0332/양끝0·고정 지문/원작 노이즈 합성 출처. 원본은 Tools/Atmosphere/goryeo_thunder_trial.py. MP3는 로컬ffmpeg 기본 인코더로 변환했다.
- 초기 BuildAudio는 Editor/B2Preview/StormLogs 검토 전용이다. 승인 후 PromoteApproved가 source/승인 당시 효과 소스/NativeWAV 지문을 검증하고 Resources/Goryeo/Thunder에 원본 PCM을 복사한다. GoryeoThunder(옛 Trial)를 전용 Director/Trigger로 연결한다. 승인된 위치의 통나무 활성 방문에만 설치한다.

본편검사 Unity/play.json:24확인/16,425일정사례. 검토연출 Unity/thunder-play.json:24확인/실제12캡처·실제 지연재생/음소거·음량·다른날씨/본편차단/소스정리. 저장9파일 불변. 그림/청감 승인과 기술 검사는 구분한다. 사람 직접/스피커/온라인/빌드FPS·장시간은 미검사.

승인 범위는 약한 빛·낮은 천둥 표현과, 해당 통나무가 켜진8시간구간에 세션당1회 재생이다. 새 확률/추가 위치/피해/영구 해결 저장은 포함하지 않는다. 본편 검사는 Unity/thunder-production.json(55확인), 첫 세션 기록은 Docs/Validation/Goryeo-Thunder-Binding-2026-11-05/production-first-session.json이다. 실제 두 Play 시작에서 기록이 비워지고, 같은 구간 재진입 무음·다음 구간 재생·UI/전환 대기·음량/음소거·다른4맵 차단/소스 정리·재산/저장OFF를 확인했다.
