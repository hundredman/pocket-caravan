# C7 묶음2: 숲 나무 세 형태

큰초가집·정자·버드나무 승인 후 숲나무3개를 다음 묶음으로 제출한다. 벚나무는 현재등록원본·사용처 연결을 확인해야 해 이번 묶음에 넣지 않았다. 별도연결을 추측해 본편에 추가/교체하지 않는다.

| 후보 | 원래 그림 | 캔버스 | 이전 불투명범위 | 새 불투명범위 |
|---|---|---|---|---|
| 둥근형 |woodland1_3_4|48×64| (1,14)~(47,63) | (1,14)~(46,63) |
| 기울어진형 |woodland2_3_4|48×64| (1,16)~(47,63) | (1,16)~(46,63) |
| 넓은형 |woodland3_4_3|64×48| (5,1)~(58,47) | (5,9)~(58,47) |

원래3개 전체PNG는 `Assets/PocketCaravan/Art/Baked/Sprites/`에 있다. 원래캔버스/하단63·63·47은 유지한다. 처음두개는가시폭1px감소, 넓은형높이46→38px와 더큰잎덩어리로수관이풍성해진변화는 사용자그림검토대상이다. 기존성긴솔가지느낌을그대로보존한보정이아니라 새형태시안이다. 구체나무종이나맵의식생분포를확정하지않는다.

## 제작

imagegen개체별1회, 팀베네치아/아라비아 기준표·승인버드나무·각기존그림을역할별참고. whole개체균등최근접→A5→Aseprite외곽선별도레이어. 자연후보7색은승인버드나무와같으며 D3공통팔레트확정아님. AI/provenance.json과old-source-evidence.json에출처와기존RGBA증거보관.

전체36렌더칸(12+12+12)은각개체의고정범위전체로만쓴다. 잘라붙이거나한그루를쪼개숲으로쓰지않는다.

## 재현

Pillow가있는Python으로저장소루트에서실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch2-20261027" --script Tools/ArtTrials/c7_batch2.lua
python3 Tools/ArtTrials/c7_batch2.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch2-20261027" --script-param finish=true --script Tools/ArtTrials/c7_batch2.lua
python3 Tools/ArtTrials/c7_batch2.py register
```

검토worktree에디터닫힌상태에서Unity6000.3.26f1 `C7Batch2Review.Run`(Burst꺼짐/worker2)을실행한다. 실제Play480×270촬영후 `python3 Tools/ArtTrials/c7_batch2.py board`. 비교판만먼저보려면style단계를쓸수있다. 새수정은버전을분리하며 각그림승인상태는review-decisions.json의에셋ID별로읽고승인픽셀일치도확인한다.

## 검토 자료와 결과

[기존/새형태/팀자료/실제크기](style-comparison.png), [실제Unity전후](game-comparison.png). 상단세후보외에원래지형·승인버드나무/가게·인물·v13연못검토안은동일하다. 실제마을배치나충돌점검이아닌 검토시험장이다.

새4+묶음1승인4+기존팀5=관련13검사통과. A2검토0오류·미승인본편거부/36칸RGBA재조합·누락거부/각크기·발밑·후보색·이진알파·외곽선/Aseprite3원본실제재출력/카메라세범위밖차이0·최신불투명원본과화면RGB오차≤1. 최종Unity종료0/C#오류0/SearchDatabase예외0.

그림별승인대기. 본편/기존승인그림·충돌·게임ID·저장미변경. 보행·수동플레이·저장회귀·전체통합·게임빌드미실행. 남은숲그림·벚나무·소품·성문 등 C7전체완료아님.
