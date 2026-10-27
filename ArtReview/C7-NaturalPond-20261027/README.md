# 자연 물가 연못 검토안 v13

사용자 승인한 진행 방향: Mystic Woods를 주로 참고해 지형에 난 물가로 재제작. 이전 v12는 수정 요청으로 보존한다.

- 제작: built-in imagegen 새 개념안(`Concept/generated.png`)과 별도로 네이티브80×48 Aseprite에서 물가를 새로 설계했다. 개념안을 직접 축소하거나 외부 참고작을 추적/복사하지 않았다. 폴리곤 윤곽, 위쪽 둑 단면, 바로 밑2px 이하의 물 접촉 그늘, 평평한 수면과 작은 물결2개.
- 실제 소스: `Tools/ArtTrials/c7_natural_pond.lua`. 원본은 둑과 물을 별도 레이어로 편집 가능. A5 후보7색/이진 알파, D3 전역 팔레트 확정 아님.
- 원본 생성: Aseprite -b --script-param root=이폴더절대경로 --script Tools/ArtTrials/c7_natural_pond.lua. `c7_natural_pond_review.py`로 A5/A2 등록과 제출판 생성. 캡처는 Unity `C7NaturalPondReview.Run`.
- 전체15칸 고정5×3 프리셋으로만 배치. 16×16 조각은 렌더용 전체 분할이며 임의 확장 금지. 안쪽 모서리1개는 온전한 별도 모듈이나 이번 프리셋에는 쓰지 않는다. water-mask는 위치 검사 전용.
- 장식은 기존 수련잎·꽃 완전 개체 그대로 사용. 모든 불투명 장식 픽셀이 실제 물 위에 놓이는지 검사했다.
- [실제 Unity 전후 비교](../C7-TeamStyle-20261027/PondNatural-v13/comparison.png): 왼쪽 거절된 v12, 가운데 새 연못, 오른쪽 선택 장식 추가.

사용자 “훨씬 좋다” 피드백을 받았으며 [제작 과정](../../Docs/Reference/PondArtWorkflow.md)을 정리했다. 최종 장식 선택·본편 교체 승인은 별도다. 본편/개경 A4 승인본/팀 에셋/기존 승인 인물·건물은 미변경. 실제 보행 및 수동 플레이 미실행.
