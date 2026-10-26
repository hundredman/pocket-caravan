# C7 시험 4개 · 사용자 승인 대기

2026-10-26. 초가집·기와집·소나무 각1개, 풀·흙 조립 세트1개.

- 제출 그림: [팀 스타일 비교](Submission-v2/style-comparison.png), [실제 Unity 전후](Submission-v2/game-comparison.png), [게임 원래 크기](Submission-v2/game-native.png), [정수3배 확대](Submission-v2/game-3x.png).
- 초가집·기와집·소나무는 기존과 같은64×48 캔버스·발밑. 땅은16×16 조각47종과 흙 중심. A2 목록의4개 에셋은 모두 `pending`이다.
- 그림 비교용 별도 시험장이다. 실제 Unity Play 카메라480×270/PPU16/Point로 촬영했고, 실제 B3 마을 배치나 수동 플레이를 보여 주는 자료는 아니다. 본편 그림·씬·배치·저장·승인 기록은 바꾸지 않았다.
- 팀 그림은 `Docs/Images/team-style-reference.png`를 참고했다. 오른쪽 참고 그림은 기준표의 표시 배율이며 Figma 원본 픽셀 배율은 확인하지 않았다. 팀 그림의 일부를 게임 에셋으로 등록하거나 본편에 넣지 않았다.

## 제작과 원본

내장 imagegen으로 팀 기준표를 참고한4개 초안을 생성했다. 원본은 `AI/`, 생성 요청은 [AI/prompts.json](AI/prompts.json)에 남겼다. API/CLI 이미지 생성은 사용하지 않았다.

`c7_seed.lua`는 AI 개체의 전체 불투명 영역을 종횡비 유지·최근접 방식으로 실제 격자에 옮겼다. 부분 개체를 재료로 쓰지 않았다. A5로 시험용19색·이진 알파를 정리했고, `c7_finish.lua`를 Aseprite에서 실행해 실제 해상도로 손질했다. 건물은 AI 디자인을 기존 외곽 크기에 직접 다시 그렸으며, 소나무는 전체 개체와 세 덩어리 잎색을 유지하며 외곽선을 정리했다. 풀·흙은 생성 초안의 색 덩어리를 바탕으로 연결용 경계 전체를 새로 그렸다.

`Native/`에51개 `.aseprite`와 PNG, `Prepared/`에 A5 입력/출력/보고서, `Cells/`에 전체 개체의 렌더링용 분할만 보관한다. 분할은 전 칸을 함께 놓는 용도다. 이번19색 후보는 팀 공통 D3 팔레트 확정이 아니다.

## 기존 승인 에셋 함께 보기

플레이어·아낙·촌장·작은 연못22칸은 전후 화면에 같은 승인본을 썼다. 별도 수정안은 만들지 않았다.

- 플레이어·사람 NPC: **수정 불필요**라는 제작자 의견. 작은 화면에서도 얼굴·윤곽이 또렷하고 밝은 색 덩어리가 새 건물 옆에서 구분된다.
- 작은 연못: **수정 불필요**라는 제작자 의견. 단순한 물색과 낮은 돌 경계가 새 그림과 이어진다. 전체 물·해안 C7 제작 때 다시 점검한다.

위 의견은 사용자 승인을 대신하지 않는다. 이번 시험 방향 승인을 받은 뒤 나머지 C7을 제작하며, 본편 교체는 별도 승인 범위를 따른다. B3 닥목 배치 검토는 기존 `codex/b3` 브랜치에 보존돼 있고 이번 그림 승인과 구분한다.

## 검사·재현

검사 결과와 제한은 [검증 기록](../../Docs/Validation/C7-Trial4-2026-10-26/README.md)에 남겼다. PNG 동일성은 크기와 디코딩 RGBA로 비교한다. `Screens-v1/`은 초기에 잘못 선택한 바닥 비교 이력으로 보존하며 제출용이 아니다. 최종 제출은 `Screens-v2/`와 `Submission-v2/`만 사용한다.

저장소 루트에서:

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-Trial4-20261026" --script Tools/ArtTrials/c7_seed.lua
```

A5의 명시 팔레트는 `trial-palette.txt`이며 `Prepared/*-a5.json`이 실제 정리 결과다. 이후 Aseprite `c7_finish.lua` → Python `c7_catalogue.py` 순서로 생성한다. Unity 검토는 `PocketCaravan.EditorTools.C7TrialReview.Run`을 **이 전용 작업 폴더에서만** 실행한다. 재촬영할 때는 제출 경로에 새 버전명을 지정해 기존 제출 그림을 덮어쓰지 않는다.
