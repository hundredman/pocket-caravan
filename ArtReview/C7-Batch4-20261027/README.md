# C7 묶음4: 수레·우물·석등

묶음3 나무 승인 후 작은소품3개를 묶음으로제출한다. 동결목록에있는기존전체그림과 STAMPS의역할/크기를참고해각개체를새로그렸다. 수레·우물은색테마가다른여러동결변형중아래지정한원본을비교기준으로선택했으며모든변형을자동교체하지않는다.

| 후보 | 비교 원본 | 캔버스 | 이전 불투명범위 | 새 불투명범위 |
|---|---|---|---|---|
| 수레 |Objects/c6_cart_e1a728ea.png|32×32| (2,3)~(29,30) | (2,9)~(29,30) |
| 우물 |Objects/c6_well_a3de50d0.png|16×32| (0,11)~(16,32) | (0,12)~(16,32) |
| 석등 |Sprites/lantern_1_2.png|16×32| (1,1)~(15,32) | (2,1)~(14,32) |

원본경로는 Assets/PocketCaravan/Art/Baked/ 기준. 캔버스·하단30/32/32는유지한다. 가시높이수레27→21/우물21→20, 석등가시폭14→12는그림검토대상이다. 비균등늘이기없이전체개체를균등최근접으로맞췄다.

수레는손잡이·적재함·바퀴, 우물은작은지붕·물입구·붙은두레박, 석등은돌받침·기둥·등실·작은지붕의목적대로만쓴다. 새상품/시설기능/불빛효과·충돌을추가하지않았으며, 석등을종이등롱으로바꾸지않았다.

## 제작과 재현

팀베네치아/아라비아기준표·승인시전가게·개별기존그림을역할별로참고해imagegen을개체별1회사용했다. AI/provenance.json/old-source-evidence.json에출처와기존RGBA증거보관. Aseprite전체개체균등최근접→A5후보21색/이진알파→1px외곽선별도레이어. D3공통팔레트확정아님.

Pillow가있는Python으로저장소루트에서실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch4-20261027" --script Tools/ArtTrials/c7_batch4.lua
python3 Tools/ArtTrials/c7_batch4.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Batch4-20261027" --script-param finish=true --script Tools/ArtTrials/c7_batch4.lua
python3 Tools/ArtTrials/c7_batch4.py register
```

검토worktree에디터가닫힌상태에서Unity6000.3.26f1 `C7Batch4Review.Run`(Burst꺼짐/worker2)실행. 실제Play480×270촬영후 `python3 Tools/ArtTrials/c7_batch4.py board`로제출판을만든다. style단계는정적비교만생성한다. 각그림승인은review-decisions.json에서개별기록하며승인픽셀일치도확인한다.

전체8렌더조각(4+2+2)은각개체고정범위전체로만쓴다. 잘라붙이기/부분배치/런타임변환없음.

## 제출·검사

[기존/수정/팀자료와실제크기](style-comparison.png)는작은소품이므로6배로비교한다. [실제Unity전후](game-comparison.png)는승인집앞에소품을배치해실제크기로본다. 집·인물·승인버드나무/가게·원래지형·v13연못검토안은양쪽동일하다. 실제마을배치·보행검사가아닌검토시험장이다.

새4+묶음3승인4+기존팀5=관련13검사통과. A2검토0오류·미승인본편거부/8칸RGBA재조합·부분누락거부/각크기·발밑·후보색·이진알파·외곽선/Aseprite3원본실제재출력/세범위밖차이0·최신불투명원본과화면RGB오차≤1.

각소품사용자그림승인대기. 본편/기존승인본·시장/시설/NPC/퀘스트ID·저장·충돌미변경. 보행·수동플레이·저장회귀·전체통합·게임빌드미실행. C7나머지소품·벚나무원본연결/성문/적용검토는남음.

2026-10-27 사용자수레만비례수정요청. 기존수레는changes_requested, 우물/석등원본은그대로다. 별도수정원본·같은장면전후는 `../C7-CartRevision-20261027/`에보관한다.

후속 C7 성문 묶음에서 사용자 “좋아. 계속 진행해.”로 같은 픽셀의 우물·석등 그림 승인을 받았다. Approved/C7-GateProps-v1에 원본/RGBA 보존. 수레는 기존 게임 원본 유지로 결정되어 이 묶음의 새 수레는 수정 요청 상태를 유지한다.
