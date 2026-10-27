# 풀밭 연못 깊이 수정 시안

사용자 수정 지시로 평평한 타원 물을 흙둑·풀 가장자리·접촉 그늘·얕은 물·안쪽 물로 나눴다. 물가를 약하게 비대칭으로 만들고 먼 쪽 물색을 그늘과 이었다. 장식은 이전 시안의 완전 개체를 그대로 사용한다.

- `pond.aseprite`: 이전 참고 레이어(숨김), 흙둑, 수면 레이어. `pond.png`: A5 결과80×48. 후보9색(실제 연못8색), 이진 알파. D3 확정 아님.
- `pieces/`: 렌더용16×16 전체15조각. 항상 고정5×3 전체를 놓는다. 자유 조립·크기 확장 미검사. `inner-corner.*`는 별도 제작한 안쪽 모서리로 이번 볼록 프리셋에는 배치하지 않는다.
- `water-mask.png`: 물 표면 위치 검사용, 본편 렌더용 아님.
- 재현: Aseprite 배치 모드에서 `Tools/ArtTrials/c7_sunken_pond.lua`에 root=이 폴더, basis=이전 RoundPond/pond.aseprite를 전달한 뒤 `Tools/ArtTrials/c7_sunken_pond_review.py`로 A5/A2 및 제출판을 만든다. Unity 캡처는 `C7SunkenPondReview.Run`.
- 실제 게임 카메라 전후: [비교](../C7-TeamStyle-20261027/PondDepth-v12/comparison.png). 화면은 검토 씬 Play이며 보행 검사는 아니다.

그림 승인 대기. 본편과 개경 A4 승인본은 변경하지 않았다.
