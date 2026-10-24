# C1 새 역할 5명 정면 검사

## 시작과 병합

최신 main `bd609476`의 `Docs/C1-Binding.md`를 `codex/c1-expand`에 병합했다(`9ae6a62e`). WorkLog의 충돌은 양쪽 기록을 모두 보존해 해결했다. 연결표 원문은 수정하지 않았다.

## 실행한 검사

- `PYTHONDONTWRITEBYTECODE=1 python Tools/ArtTrials/npc_binding_fronts.py`: 5개16×24 PNG·알파0/255·발밑24px·Aseprite 저장/재열기 픽셀 일치 확인.
- merchant_silk는 장부/비단 소품 범위 밖에서 merchant와 픽셀 동일. woman_ricecake는 물동이/쟁반 범위 밖에서 승인된 아낙과 픽셀 동일.
- `python Tools/Integration/validate_assets.py ArtReview/C1-Binding-Fronts/catalogue.json --preview --report ArtReview/C1-Binding-Fronts/manifest-report.json`: 오류0. 기술 미리보기이며 배치·미술 승인 아님.
- 승인10명과 새5명 비교 한 장을 실제로 열어 확인했다. 5개를 별도 시안 에셋으로 등록했다.
- 실제 Unity 장단 Play에15정면을 메모리 Sprite로 임시 표시. 새5명까지 PPU16·pivot(8,6)·scale1 일치, autosave=false·sceneDirty=false·paused=true 확인. 실제 Game1920×1080 캡처를 열어 확인했다.
- Unity Console error 조회0개. 기존 승인/동물 파일·main 추적파일 변경0개 확인. 미추적 3D 실험 보존.
- `git diff --check`: 통과.

## 아직 확인하지 않은 것

새5명 미술 승인, 걷기·다른 방향, 역할별 본편 일괄교체·신규 우물가 아낙 추가, 실제 이동/충돌·말걸기, 저장/기존 ID 회귀. 기존 이름/대사/ID/본편 데이터는 변경하지 않았다. C1-Binding의 첫 승인 단계에서 멈췄다. 빌드/전체 시스템 회귀 검사는 실행하지 않았다.
