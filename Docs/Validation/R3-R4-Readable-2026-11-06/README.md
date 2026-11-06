# 기존 테마 가독성 보완 검증

사용자가 승인한 팀 공용 UI B안의 창/리본/종이색/팔레트/폰트를 유지한다. 검토 브랜치에서 본문·보조 글자·기본/청록 버튼 글자의 대비를 보완하고, 게시판의 목적지/기한/보증금 줄을 정리했다. 회복 창에는 보유금과 기부 불가 이유를 보이며 장터의 수량 문구를 짧게 했다. 거래·평판·저장 처리 콜백/수치/ID는 변경하지 않았다.

## 실행 결과

- Unity 자동 Play100확인·실제480×270 11캡처. 흥정 수량/회복/속죄 수락·납품/저장 실패 복원/시작 씬 Space 이어하기·중복 방지 시나리오 포함. 마지막 캐시 보완 후에도 재실행했다.
- Unity 재컴파일·Console error0. 전체 Python403/23skip·실패0 (`python-full.log`). Pillow만 있는 별도 환경/Editor8종은 이번 변경에서 재실행하지 않았으며 이전 결과는 앞 작업 기록이다.
- `stroke-comparison.json`: 이전/보완 후 같은 회복 제목 영역의 배경 RGB는255,253,249. 가장 진한 글자 획 RGB는145,130,146→111,107,110, 해당 핵심 획의 대비 참고치3.556→5.162. 모든 글자/상태/화면이 WCAG를 통과한다는 뜻은 아니다.
- `color-conversion.txt`/`cached-color-check.txt`: Linear 프로젝트에서 기존 팔레트 글자색 보정, 태그 반복 적용의 안정성/멱등성1000회 확인. 변환색은 캐시한다. 종이·프레임 텍스처 색/생성은 고치지 않았다.
- 기존 사용자 저장9파일 SHA256 불변·임시 저장/백업/임시 파일0 (`save-preservation.json`). 네이티브 마우스/사람 플레이·온라인·앱 재시작·게임 빌드/FPS는 미검사.

## 표시와 기준

현재 캡처/전후 그림: `ArtReview/Goryeo-R3-R4-Readable-20261106`. 이전 종이 테마는 사용자 승인 상태이며, 이번 가독성 보완안은 별도 그림 확인 대기다. GitHub integration에는 미반영/push 없음.

글자 대비의 참고 기준은 [W3C Contrast Minimum](https://www.w3.org/WAI/WCAG21/Understanding/contrast-minimum/)의 일반 글자4.5:1이다. 웹 접근성 인증을 이 게임에 적용/선언한 것은 아니다. Unity의 [GUIStyleState.textColor](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/guistylestate)는 표시 상태별 글자색이다. 색 공간에 대한 이번 보정 판단은 문서의 보편적 보장으로 주장하지 않고 이 프로젝트 실제 캡처 실험에 근거한다.

전체 UI 1차 점검은 별도 [검토 기록](../../Goryeo/UI-Audit-2026-11-06.md)에 있다.
