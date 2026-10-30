# C7 객주용 큰 기와집 외형 후보

시전 가게 승인 후 다음 건물로 제작했다. 기존 큰 기와집6×4 그림은 `goryeo_manor` 여러 곳이 공유하므로, 객주에 독점 연결된 기존 에셋이라고 해석하지 않는다. 이번에는 객주용 별도 외형 후보를 만들며 본편 역할 연결은 하지 않는다.

- 이전 비교 원본: `Assets/PocketCaravan/Art/Baked/Sprites/house_6_4.png`,96×64, 불투명범위(0,1)~(92,63).
- 후보: 동일96×64/6×4칸·하단63 유지. 온전한개체 균등 최근접 샘플링. 새 범위(2,13)~(94,63), 폭92px는 같고 가시높이62→50px로 비례는 검토 대상이다. 비균등 늘이기 없음.
- 가운데 문·양옆 창·앞마루·짧은 계단·작은 빈 간판으로 시전 가게의 열린 판매대와 구분했다. 간판에 새 시설명/문자를 넣거나 실제 객주 기능·NPC·저장 ID를 추가하지 않았다.
- 팀/승인기와집/기존큰집 참고imagegen→Aseprite 네이티브→A5후보21색→Aseprite1px외곽선 별도레이어. 역할과출처는 AI/provenance.json. 알파224 기준으로 생성 가장자리 흐림 제거. D3 공통 팔레트 확정 아님.
- 전체24칸은 렌더분할이며 고정6×4 전체로만 놓는다. 공용큰집 전체일괄교체/객주연결/충돌 적용은 이 시안의 범위가 아니다.

## 재현

Pillow가 있는Python으로 저장소루트에서 실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Gaekju-20261027" --script Tools/ArtTrials/c7_gaekju.lua
python3 Tools/ArtTrials/c7_gaekju.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Gaekju-20261027" --script-param finish=true --script Tools/ArtTrials/c7_gaekju.lua
python3 Tools/ArtTrials/c7_gaekju.py register
```

이worktree에디터가 닫힌 상태에서Unity6000.3.26f1 `C7GaekjuReview.Run`(Burst꺼짐/worker2)으로Play촬영. 캡처가 생기면 `python3 Tools/ArtTrials/c7_gaekju.py board`로 제출판을 만든다.

## 제출과 검사

[공용 기존집/후보/팀 비교](style-comparison.png), [실제Unity 전후](game-comparison.png). 가운데 이웃건물은 승인된시전가게, 오른쪽은 승인궁궐이며 같은인물·원래땅·v13연못검토안을 양쪽에 뒀다. 실제마을배치가 아닌 검토시험장이다.

새후보4+시전승인4+기존팀5=관련13검사통과. A2검토0오류·미승인본편거부/24칸RGBA재조합·누락거부/후보색·이진알파·발밑·외곽선/실제Aseprite재출력/Unity후보밖차이0·최신원본불투명픽셀과화면RGB오차≤1 확인. PNG는크기+디코딩RGBA로비교. Unity기존SearchDatabase예외1회·종료0.

2026-10-27 사용자 “좋아.”로 이 외형 그림 승인. `ArtReview/Approved/C7-Gaekju-v1/`에 원본·크기/RGBA·범위 보관. 제출판 대기 문구는 당시 이력이다. 본편·기존승인본·타팀그림·충돌·시장/시설/NPC/퀘스트ID·저장데이터미변경. 보행·수동플레이·저장회귀·전체통합·게임빌드미실행.
