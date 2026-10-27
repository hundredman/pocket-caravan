# 수레 비례 소폭 수정

사용자 “수레 이미지만 살짝 수정해줘. 뭔가 틀어진 비율같아.”에 따라 직전묶음4수레만수정했다. 우물·석등의그림/원본과다른시안은바꾸지않았다. 이전수레는묶음4폴더에수정요청상태로보존한다.

## 바꾼 부분

- 같은왼쪽손잡이방향·빈적재함·갈색계열·32×32캔버스·하단30을유지했다.
- imagegen으로바퀴를적재함에비해작게하고축위치·손잡이/적재함투시를맞추는방향으로소폭수정했다.
- 네이티브축소후바퀴살과투명틈이검게뭉쳐Aseprite에서바퀴만8×8범위로다시정리했다. 이전A5참고레이어는숨김으로보존하고, 보정본/외곽선레이어를별도로뒀다.
- 온전한개체균등최근접·A5기존후보21색/이진알파이며D3확정아님. 새불투명범위(2,11)~(29,30), 이전(2,9)~(29,30)로폭27유지·가시높이21→19px다. 비균등늘이기/다른에셋조각재사용없음.

## 재현

Pillow가있는Python으로저장소루트에서실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-CartRevision-20261027" --script Tools/ArtTrials/c7_cart_revision.lua
python3 Tools/ArtTrials/c7_cart_revision.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-CartRevision-20261027" --script-param finish=true --script Tools/ArtTrials/c7_cart_revision.lua
python3 Tools/ArtTrials/c7_cart_revision.py register
```

이worktree에디터가닫힌상태에서Unity6000.3.26f1 `C7CartRevisionReview.Run`(Burst꺼짐/worker2)실행. 실제Play촬영후 `python3 Tools/ArtTrials/c7_cart_revision.py board`.

## 확인

[12배원본과실제Unity전후](comparison.png). 비교전그림은최초본편수레가아니라직전제출한묶음4시안이다. 이전캡처가직전묶음4trial과크기·디코딩RGBA로완전히일치하고, 전후변화는수레32×32영역안이다. 우물·석등원본도context-evidence.json의RGBA와일치한다.

새4+묶음4기록검사4=관련8검사통과. A2검토0오류·미승인본편거부/전체4렌더조각재조합·누락거부/크기·발밑·후보색·이진알파/Aseprite원본실제재출력/이웃그림동일성/최신수레원본과화면RGB오차≤1확인. 바퀴네이티브보정후Unity재촬영과관련4검사를다시실행했다.

사용자가 원본 구조·픽셀화 후 인지성을 지적해 이 수정본은 changes_requested다. 후속 재제작은 `../C7-CartRebuild-20261027/`에 보존했다. 우물·석등상태를자동승인으로올리지않았다. 본편/승인본·게임ID·저장·충돌미변경. 실제보행·수동플레이·저장회귀·전체통합·게임빌드미실행.
