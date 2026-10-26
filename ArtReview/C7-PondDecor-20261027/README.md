# 둥근 연못 장식 바리에이션 · 검토 대기

2026-10-27 사용자: “더 낫긴 한데 물 위 연꽃같은 바리에이션을 추가해줘야겠나. 뭔가 애매하네.”

[실제 Unity 3안 비교·확대](../C7-TeamStyle-20261027/PondDecor-v11/comparison.png).

1. 민 연못
2. 수련잎만
3. 수련잎 + 꽃 한 송이

모두 같은 둥근 연못·원래 지형·승인 건물·나무·인물이다. 새 장식은 각16×16의 온전한 개체2개이며 기존 연꽃을 잘라 쓰지 않았다.3안에서도 장식이 차지하는 불투명 면적은 수면의 약5.5%다. 제작자 의견은 민 연못을 남겨 두고3안을 장식형으로 함께 쓰는 방향이다. **아직 연못 모양/장식 조합의 최종 사용자 승인은 받지 않았고 본편에 넣지 않았다.**

내장 imagegen으로 기존 연꽃의 색/소재와 Figma 팀 아트 표현을 참고해 새 원본2개를 생성했다([요청](AI/prompts.json)). 생성된 투명 배경의 흐린 가장자리는 알파224 기준으로 정리해 전체 개체를 종횡비 유지·최근접 방식으로 실제16px에 옮겼다. A5에서 명시8색 후보와 이진알파를 정리하고 Aseprite 원본/PNG를 저장했다. D3 공통 팔레트 결정은 아니다.

`Native/`에 Aseprite/PNG, `Prepared/`에 입력/보고서, `catalogue.json`·`layout.json`에 물 위에만 놓는 개체 정의/검토 배치가 있다. 실제 둥근 연못의 물 내부에 모든 불투명 장식 픽셀이 들어가는지 별도 확인했다. 장식을 늘이기·회전·잘라서 놓지 않았다. 둥근 물 PNG와 기존 개경 연못/승인본은 그대로다.

새3+연못4+기존팀스타일5=관련12검사 통과([결과](../../Docs/Validation/C7-TeamStyle-2026-10-27/pond-decor-tests.txt)). A2 물 위 배치/풀 위 배치 거부·미승인 본편 거부, Aseprite 실제 재출력 RGBA 동일, 실제 캡처3장의 차이가 추가한 장식16×16 영역에만 있는지 확인했다. Unity previewOnly Play480×270/PPU16/Point 새촬영, 종료0·기존SearchDatabase 예외1회. 전체 통합/실제보행/수동플레이/저장회귀/빌드는 실행하지 않았다.

재현(저장소 루트, Pillow12.3 이상 Python):

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-PondDecor-20261027" --script Tools/ArtTrials/c7_pond_decor.lua
python Tools/ArtTrials/c7_pond_decor.py --prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-PondDecor-20261027" --script-param finish=true --script Tools/ArtTrials/c7_pond_decor.lua
python Tools/ArtTrials/c7_pond_decor.py
```

Unity 전용 폴더에서 `PocketCaravan.EditorTools.C7PondDecorReview.Run`, 이후 `c7_pond_decor_review.py`로 비교 그림을 만든다. 최종 경로는 `PondDecor-v11/`이며 새 제출 때 버전명을 올린다.
