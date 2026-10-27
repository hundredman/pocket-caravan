# C7 시전 가게 6×4 시안

궁궐 승인 후 다음 건물로 제작했다. 대상은 `goryeo_stall`이 사용하는6×4칸 온전한 가게 그림이다(`compact.py` 개경 장터 배치). 다른3×3/4×3 가게나 객주·시장 ID를 일괄 교체하는 작업이 아니다.

- 이전 원본: `Assets/PocketCaravan/Art/Baked/Sprites/shop_6_4.png`,96×64, 불투명범위(1,1)~(94,63).
- 새 시안: 동일96×64/6×4칸·하단63 유지. 전체 개체 균등 최근접 샘플링, 새 불투명범위(2,8)~(94,63). 보이는 폭93→92px, 높이62→55px로 비례는 검토 대상이다. 비균등 늘이기 없음.
- 열린 판매대·도자기2개·천 묶음·빈 간판·간결한 기와지붕. 진열품은 이 건물에 처음부터 포함한 그림이며 상품/NPC/시설 데이터를 새로 만들지 않았다.
- AI2회(첫 가로형 미선택)→Aseprite 네이티브 샘플→A5 집/궁궐과 같은 후보21색→별도 외곽선 레이어. 원본/역할은 AI/provenance.json. 후보색은 D3확정 아님. 원본을 제외한 흐린 반투명 가장자리는 알파224 기준으로 제거했다.
- `Native/shop.aseprite`,PNG와 전체24개 렌더 조각을 보관한다. 고정6×4 전체로만 쓴다. 잘라 붙이기·부분 배치·용도 변환 없음.

## 재현

Pillow가 있는 Python을 사용해 저장소 루트에서 실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Shop-20261027" --script Tools/ArtTrials/c7_shop.lua
python3 Tools/ArtTrials/c7_shop.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Shop-20261027" --script-param finish=true --script Tools/ArtTrials/c7_shop.lua
python3 Tools/ArtTrials/c7_shop.py register
```

이 worktree의 Unity 에디터가 닫힌 상태에서6000.3.26f1 `C7ShopReview.Run` 실행(Burst꺼짐/worker2). 실제Play 캡처 후 `python3 Tools/ArtTrials/c7_shop.py board`.

## 제출

[기존/새 시안/팀 자료](style-comparison.png), [실제Unity 전후](game-comparison.png). 전후에서 가게만 바뀐다. 같은 승인 궁궐·집·인물·원래땅과 v13연못 검토안을 두었다. 전체 팀 이미지는 비교판 참고용이며 본편으로 가져오지 않았다. 검토 시험장으로 실제 개경 배치/충돌 검사는 아니다.

신규4+궁궐승인4+기존팀5=관련13검사 통과. A2검토0오류·미승인본편거부/24칸RGBA재조합·누락거부/후보색·이진알파·발밑·외곽선/실제Aseprite재출력/실제Unity가게밖차이0 및 불투명원본과화면RGB오차≤1 확인. PNG 동일성은 크기와 디코딩RGBA로 비교한다. Unity기존SearchDatabase예외1회/종료0.

사용자 그림 승인 대기. 본편/다른 건물/기존 승인본·충돌·게임 ID·저장 데이터 미변경. 보행·수동플레이·저장회귀·전체통합·게임빌드 미실행.
