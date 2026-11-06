# 고려 A4 걷기 느낌 v3 — 승인 대기

## 현재 상태

흙·풀·돌·나무·물에 같은 원본을 EQ만 달리 쓰던 v2를 바꿨다. 흙은 Kenney RPG Audio 발걸음, 풀·단단한 바닥·나무는 Kenney Impact Sounds의 해당 재질 원본, 물은 rubberduck의 물 튀김 원본에서 파생된 wade 파일을 쓴다. 모두 CC0이고 원본 페이지/미러/고정 커밋/검증 Git blob hash는 Sources/provenance.json·CREDITS.md에 있다. 원 제작자 zip 내려받기가 불가능해 공개 미러의 음원5개만 받았으며, 미러의 코드/다른 에셋은 가져오지 않았다. 물 파일은 미러 가공본이며 원본 splash_12/14 중 어느 것인지는 별도 확인하지 못했다.

원본 전체를 유지해 mono/22.05kHz·약한 저역통과/DC 제거·끝점 페이드·부드러운 피크 제한만 적용했다. 음높이를 만드는 합성 타격음은 넣지 않았다. Tools/Atmosphere/goryeo_stride_trial.py가 재현 원본이며, v1/v2 생성 코드는 같은 폴더의 _v1/_v2 파일로 남겼다. 원본 파일을 내려받는 URL과 해시는 Sources/provenance.json에 고정했다. generatedAudio는 가공 방법의 기록이며 사람의 음질 승인이 아니다.

## 제출물

- footsteps-review.mp3:흙→풀→돌→나무→물, 각각5걸음. source.json timeline에 실제 시작/길이.
- dirt/grass/stone/wood/splash-demo.mp3:재질별 따로 들어보기.
- Native/*.wav:Unity 검토에 가져온 단일 발걸음. 소리 길이를 자르지 않고 전체 원본을 가공했다. 반복 재생은 단일 클립을 겹쳐 재생해 만들었다.
- walking-review.png:실제480×270 비교3장과 발밑4배 정수 확대. Unity/는 원본4장/Play30확인.

## 표면·효과

13맵의 현재 ground/deco와 m4-composites 인증 기록에서 흙/풀/나무다리를 구분한다. 길 등급은 바닥 재질이 아니다. 다리는 bridge 조립 원본이 들어간 deco와 실제 길/통행 칸을 함께 확인한다. 현재 돌포장은0칸이라 돌 소리는 준비만 했고 흙길에서 사용하지 않는다. 표면 문자열은 검토용 source.json에만 있고 실제 맵을 바꾸지 않았다.

공용 PlayerController/AudioManager를 고치지 않고 별도 GoryeoStride에서 실제 발 이동거리1.15칸/최소0.16초에 발소리. 기존 음소거/효과음 음량을 따르며 제자리·순간이동·UI 잠금/이동 중은 새 소리 없음. 달릴 때 흙먼지, A1 비/장맛비 때 발밑 물 튀김. native16/Point/scale1의 작은 절차 FX, 화면 안에서만/풀24개. 실제 이동/충돌/속도/짐/경제/저장은 읽기만 한다. 현재는 자동 본편 설치 훅 없이 previewOnly 검토 씬만 적용한다.

## 한계·다음

승인 전 본편 미적용. API/캡처 검사는 음질 승인과 다르다. 실제 스피커 청감·사람 키보드 플레이·빌드/장시간/온라인 미검사. 구형 v1은 날카롭고 부자연스러워 반려, v2는 부드러워졌지만 재질 차이 보강 요청. 최신 사용자 검토 대상은 이 v3와 동일한 먼지·물 튀김이다. 승인하면 본편 연결 검사 후 A3 생활 소리로 진행한다.

## 승인·본편 연결

2026-11-05 사용자 “이 버전으로 승인”. approval.json에 원문/5개 Native SHA256 기록. 승인된 소리와 먼지/물 튀김을 본편13맵에 연결했다. Unity/production.json124확인·Naru-production-rain.png. source.json은 검토 원본 그대로 보존하고 승인상태는 별도 기록/생성 Resources로 구분한다. 자세한 검사는 Docs/Validation/Goryeo-Atmosphere-A4-Production-2026-11-05.
