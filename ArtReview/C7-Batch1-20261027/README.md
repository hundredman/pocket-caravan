# C7 묶음1: 큰 초가집·정자·버드나무

사용자가 “다음에는 이미지를 여러개 만든 다음 한번에 검사”하도록 지시해 세 개를 제작하고 묶음으로 제출한다. 한 묶음이어도 그림별 승인·수정 요청과 해당 픽셀 증거는 독립적으로 기록한다.

## 대상과 범위

| 그림 | 원래 전체 캔버스 | 원래 불투명 범위 | 새 불투명 범위 | 용도 |
|---|---|---|---|---|
| 큰 초가집 |80×64| (2,2)~(79,62) | (1,14)~(78,62) |5×4칸 큰 집|
| 정자 |80×80| (5,2)~(78,80) | (3,5)~(76,80) |5×5칸 열린 정자|
| 버드나무 |64×64| (4,1)~(59,63) | (4,1)~(59,63) |4×4칸 나무 한 그루|

캔버스와 불투명하단(발밑)은 각각 그대로다. 그림비례는 새설계이며 큰초가집 가시높이60→48px, 정자78→75px로 줄어든 부분도 검토 대상이다. 버드나무는55×62px 범위를 유지한다. 비균등 늘이기를 하지 않았다. 승인된 작은초가집을 확대해 쓰지 않고 큰초가집을 새로 제작했다.

이전 그림은 `Assets/PocketCaravan/Art/Baked/Sprites/`의 `thatch_5_4.png`, `pavilion_5_5.png`, `woodland5_4_4.png`. 본편에서는 교체하지 않았다. 성문·그밖의건물·벚나무·숲·소품 전체가 완료된 묶음이 아니다.

## 제작과 재현

팀 기준표와 각 기존그림·승인그림을 참고해 imagegen을 개체별1회 사용했다. AI/provenance.json에 참고역할·방법을 보관한다. 전체개체를 균등 최근접으로 네이티브격자에 맞추고 A5후보색·이진알파로 정리한 뒤 Aseprite1px외곽선 레이어를 보관했다. 후보색은 건물21/자연7색이며, 건물은 지붕 안 어두운 부분이 검은 점으로 바뀌지 않도록 기와3색(33475e/4a627a/718da3)을 사용했다. 나머지 후보는 집·궁궐과 같은 계열이다. 이 후보는 D3공통팔레트 확정 아님. Aseprite는 본체·외곽선 별도레이어로 편집 가능하다.

Pillow가있는Python으로 저장소루트에서 실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch1-20261027" --script Tools/ArtTrials/c7_batch1.lua
python3 Tools/ArtTrials/c7_batch1.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch1-20261027" --script-param finish=true --script Tools/ArtTrials/c7_batch1.lua
python3 Tools/ArtTrials/c7_batch1.py register
```

이worktree에디터가 닫힌 상태에서Unity6000.3.26f1의 `C7Batch1Review.Run`(Burst꺼짐/worker2)을 실행한다. 실제Play촬영후 `python3 Tools/ArtTrials/c7_batch1.py board`로 제출판을 만든다. 제출판을 만들기 전에 캡처가 있어야 한다. 수정본은 새버전으로 분리해 승인/기존제출그림을 덮어쓰지 않는다.

A2목록은3개 온전한개체를61개 렌더조각(20+25+16)으로 기록한다. 개체의 일부만 쓰거나 이어붙여 다른그림을 만들지 않는다. `review-decisions.json`이 생기면 에셋ID별승인상태를 읽고 승인그림은RGBA일치도확인한다. 최초세그림은모두pending이다.

## 검토 자료

[기존/수정/팀 참고](style-comparison.png)와 [실제Unity 전후](game-comparison.png)를 함께 본다. 위쪽왼쪽부터 큰초가집·정자·버드나무, 아래의승인집/가게·인물·v13연못검토안은양쪽동일하다. 지형·개체위치·카메라는고정했다. 팀이미지는참고용표시이며 게임에가져오지않았다. 실제마을배치나보행검사가아닌 검토시험장이다.

원본·조립3+화면1+객주승인4+기존팀5=관련13검사 통과. 실제Unity 세범위밖차이0·최신불투명원본/화면RGB오차≤1 확인. 보행·수동플레이·저장회귀·전체통합·게임빌드미실행.

각그림별 사용자검토대기. 본편·승인에셋·기존ID·저장·충돌 미변경. 검사 결과와 실행 범위는 `Docs/Validation/C7-Batch1-2026-10-27/`에 기록한다.
