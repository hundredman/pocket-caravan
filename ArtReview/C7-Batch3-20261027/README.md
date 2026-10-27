# C7 묶음3: 큰 활엽수·밝은 줄기·노란 잎

묶음2 숲나무3개 승인 후 woodland4/6/7의 세그림을 다음묶음으로제출한다. 기존 각개체의 전체캔버스/발밑과 초록·밝은줄기·노란잎 구분을 기준으로 새로그렸다. 구체나무종/계절시스템/맵식생분포를 새로정하지않는다.

| 후보 | 이전 전체 그림 | 캔버스 | 이전 불투명범위 | 새 불투명범위 |
|---|---|---|---|---|
| 큰 활엽수 |woodland4_4_4|64×64| (1,5)~(63,63) | (1,6)~(63,63) |
| 밝은 줄기 나무 |woodland6_3_4|48×64| (1,8)~(47,63) | (3,8)~(45,63) |
| 노란 잎 나무 |woodland7_3_4|48×64| (1,10)~(47,63) | (1,12)~(47,63) |

각 PNG의불투명하단63은같다. 가시높이는58→57/55유지/53→51, 밝은줄기나무폭46→42px이며 이새비례도그림검토대상이다. 원래색을런타임에서바꾼변형이아니라 각원본을별도로제작했다.

## 제작과 색

imagegen개체별1회(팀기준표·승인숲나무·각기존원본참고)→온전한개체균등최근접/A5→Aseprite1px외곽선별도레이어. 원본/역할은AI/provenance.json, 이전이미지의크기/RGBA는old-source-evidence.json. 출처원본은 Assets/PocketCaravan/Art/Baked/Sprites/.

후보팔레트는각7색이다. 초록은승인버드나무/숲과같은색, 밝은줄기는승인건물의연한회갈색·크림색계열, 노란잎은승인초가지붕의황금색계열을참고했다. 색참고만하며기존에셋조각을재사용하지않았다. D3공통팔레트확정아님. 계절변경이나즉석색바꾸기기능을추가하지않는다.

전체40렌더칸(16+12+12)은각개체의고정범위전체로만쓴다. 일부만놓기/붙여쓰기없음.

## 재현

Pillow가있는Python으로저장소루트에서실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch3-20261027" --script Tools/ArtTrials/c7_batch3.lua
python3 Tools/ArtTrials/c7_batch3.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch3-20261027" --script-param finish=true --script Tools/ArtTrials/c7_batch3.lua
python3 Tools/ArtTrials/c7_batch3.py register
```

검토worktree에디터가닫힌상태에서Unity6000.3.26f1 `C7Batch3Review.Run`(Burst꺼짐/worker2)실행. 실제Play480×270캡처후 `python3 Tools/ArtTrials/c7_batch3.py board`로묶음제출판을만든다. style단계로정적비교만먼저볼수있다. 새수정은버전분리, 승인은review-decisions.json에그림별로기록한다.

## 제출·검사

[기존/수정/팀자료와실제크기](style-comparison.png), [실제Unity전후](game-comparison.png). 아래승인버드나무·가게/인물/원래땅/v13연못검토안은양쪽동일하다. 실제마을배치/충돌점검이아닌 검토시험장이다.

새4+묶음2승인4+기존팀5=관련13검사통과. A2검토0오류·미승인본편거부/40칸RGBA재조합·부분누락거부/각크기·발밑·후보색·이진알파·외곽선/Aseprite3원본실제재출력/세영역밖차이0·최신불투명원본과화면RGB오차≤1.

2026-10-27 사용자 “좋아.”로 세그림승인. Approved/C7-Batch3-v1에개별원본/RGBA/범위보관. 제출판의검토대기문구는당시이력이다. 본편/기존승인본·충돌·ID·저장미변경. 보행·수동플레이·저장회귀·전체통합·게임빌드미실행. 벚나무연결확인·나머지소품/성문등은계속남아있다.
