# 고려 분위기 A1 — 사용자 승인·본편 연결

사용자 “아니다 그냥 승인할게.”(2026-11-05)로 맑음·봄비·장맛비·아침 안개와 실제 움직임 승인(approval.json). 안개를 더 강하게 바꾸는 제안은 취소되어 기존 시안 그대로 유지했다.

## 원본·연결

source.json은 검토 당시 설정 원본이다. GoryeoAtmosphereReview.PromoteApproved가 승인 기록을 확인해 Resources/Goryeo/Atmosphere.json으로 복사한다. GoryeoAtmosphereDirector는 GoryeoTravelTransition.RegionOf로 고려/중앙 동쪽13맵만 판별하고 승인 설정이 있을 때만 각 MapRoot에 모듈 하나를 붙인다. 다른 문명/공용 코드·맵/씬은 고치지 않았다. 검토 씬은 B2Preview/AtmosphereA1에만 생성한다.

공용 WeatherOverlay/WeatherSkin을 그대로 재사용한다. 봄비70개/장맛비128개, 안개는 공용 엷은 haze와 고려 전용 느리게 흐르는 띠3개. 최대 활성 비 입자128+배경 haze1로160개 이내다. 카메라 주변 한 풀만 재사용하며 맵 이탈 시 파괴한다. 비/안개 때 기존 AmbientFX 꽃잎·구름 그림자의 renderer 표시만 임시 보관/숨김/복원한다. 공용 AmbientFX나 다른 지역 분위기 코드는 수정하지 않는다.

날짜와 seed73의 해시가 기본 날씨를 결정한다. 기본 구간은 맑음55/봄비25/장맛비10/안개10 비중의100칸(실제 날짜 표본 분포는 약간 다름). 같은 날짜의 기본 날씨는13맵에서 같으며 안개만 물가8맵/오전4시 이상9시 미만에 표시한다. 비·안개는 실제 시간에 따라 계속 움직인다. 속도/충돌/판정/날씨 사건/저장/경제는 변경하지 않았다. 소리 추가도 없다(A4/A3/A2에서 별도 검토).

## 그림·움직임

Naru-comparison.png/goryeo_jangdan-comparison.png: 실제480×270 네 상태를 각각 그대로 배열. 안개는7:30, 다른3상태는12시, 동일 카메라다. Unity의 ClearMorning 원본은 안개와 같은 시각 비교용이다. rain-motion.gif(24프레임/약3초)·mist-motion.gif(48프레임/약6초)는 실제 Unity 프레임을8fps로 담은 움직임 참고다. GIF는 색상 양자화가 있으며 PNG 캡처가 색상 기준이다. 새 그림 합성/움직임 재현으로 만든 영상이 아니다. 중간 PNG 프레임은 무시되는 B2Preview/MotionFrames에 있다.

## 확인

- Weather data:13맵×4096날짜=53,248 사례, 동일 날짜/장소·아침 제한·4상태·타 지역/알 수 없는6ID·Unity 전역 랜덤 불변.
- Unity/play.json: 검토2맵51확인/480×27010캡처. 입자 상한·20회 전환 풀 크기 유지·표시 끄기/복원·타 지역 ID에서 정지·원래 노면/차단/돈/짐 불변.
- Unity/motion.json:80확인, 실제 비 입자/안개 띠 위치 변화·72프레임·저장 차단.
- Unity/production.json: 실제13맵67확인, 승인 모듈 자동 연결/중복 방지/장맛비128/물가 안개 적용/저장 차단. Naru-production-mist.png는 본편 연결 후 캡처다.
- 사용자 저장9파일 사전 백업/불변. 짧은 에디터 프레임 측정은 참고일 뿐, 목표 PC/빌드 FPS나 장시간 최적화 검증이 아니다.

남동 포구 시설 설명의 옛 이름 “남동 길을”만 “솔바람 고개를”로 고쳤다. 장소/시설ID/시세 값은 그대로다. 검사 결과와 미검사는 Docs/Validation/Goryeo-Atmosphere-A1-2026-11-05. 브랜치 codex/goryeo-atmosphere, push/PR 없음. 다음은 A4 지면별 걷기 느낌/발소리이며 승인 전 새 소리는 본편에 넣지 않는다.
