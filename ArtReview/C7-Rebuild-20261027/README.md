# C7 초가집·기와집·풀/흙 재제작

2026-10-27 사용자 “팀 에셋처럼 형태부터 다시 제작” 지시의 수정안이다. **사용자 수정 요청, 본편 미적용**. 후속 [팀 그림체 수정안](../C7-TeamStyle-20261027/README.md)으로 이어간다.

- [이전 시안 / 수정안 / 팀 참고](Submission-v4/style-comparison.png)
- [Unity 전후](Submission-v4/game-comparison.png), [480×270 원래 크기](Submission-v4/game-native.png), [정수3배 확대](Submission-v4/game-3x.png)
- [생성 요청 원문](AI/prompts.json), [A2 목록](catalogue.json), [검증 기록](../../Docs/Validation/C7-Rebuild-2026-10-27/README.md)

## 달라진 것

초가집은 낮게 처지는 초가지붕·용마루·처마 그늘·왼쪽 문과 오른쪽 창, 기와집은 곡면 기와·처마 받침·왼쪽 넓은 창과 오른쪽 문으로 새 형태를 만들었다. 두 집 모두 원래64×48 캔버스·발밑을 유지한다. 초기에 AI 개체 전체를 균일 축소한 결과 외곽이60×34/60×39로 낮아 보였다. Aseprite에서 지붕 픽셀을 위로 옮기고 벽체·들어간 문·창·기단을 실제 해상도로 새로 그려 최종 외곽 높이44px 이상을 확보했다. 맵에서 늘이기나 렌더 배율 변경을 하지 않는다.

풀/흙은 반복 사선 점을 없애고, 색차3 이하의 낮은 대비 재질과 깊이1~3px의 불규칙한 경계47종을 만들었다. 가운데·가장자리·바깥/안쪽 모서리·흙 중심이 있는 조립 세트다. 시험장에는 같은 완만한 길 모양을 전후 양쪽에 썼다. 이는 새 B3 마을 배치나 통행 경로 승인 요청이 아니다.

나무는 이전 시안의 원본과 PNG를 보존해 그대로 가져왔다. PNG 크기·디코딩 RGBA로 같음을 검사했다. 사용자 “나무 빼고” 의견을 본편 적용 승인으로 확대하지 않았다.

## 제작

내장 imagegen으로 팀 스타일 기준표를 참고한 새 AI 원본3개를 생성했다. 고해상도 원본은 `AI/`에, 전체 개체를 종횡비 유지·최근접으로 옮긴 실제 격자와 A5 보고서는 `Prepared/`에, Aseprite 손질 결과와 원본51개는 `Native/`에 있다. AI 원본의 실측 크기와 프롬프트의 의도한 격자 크기는 다르며 실제 게임에는 손질한16px 격자 PNG를 쓴다.

건물16색과 지형4색은 이번 시안용 후보이며 D3 공통 팔레트 결정이 아니다. 나무의 기존 색을 바꾸지 않는다. 팀 기준표에서 참고 그림을 비교용으로만 표시했고, 팀 건물 조각을 게임 그림으로 쓰지 않았다. Figma 원본 도트 배율은 미확인이다.

저장소 루트에서 순서대로:

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-Rebuild-20261027" --script Tools/ArtTrials/c7_rebuild_seed.lua
python Tools/ArtTrials/c7_rebuild_prepare.py
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-Rebuild-20261027" --script Tools/ArtTrials/c7_rebuild_finish.lua
python Tools/ArtTrials/c7_rebuild_catalogue.py
```

Python은 Pillow12.3 이상이 필요하다. 나무 원본2개는 이전 시험의 `Native/pine.*`를 이 폴더에 복사해 사용하며 새로 생성하지 않는다. Unity 전용 작업 폴더에서 `PocketCaravan.EditorTools.C7RebuildReview.Run`을 실행해 격리된 previewOnly Play 시험장을 촬영한다. 비교 그림은 `c7_rebuild_review.py`로 만든다. 새 제출 시 경로 버전명을 바꿔 이전 그림을 덮어쓰지 않는다.

## 기존 승인 에셋

플레이어·아낙·촌장·작은 연못22칸은 기존 승인본을 전후에 똑같이 배치했다. 현재 제작자 의견은 **수정 불필요**다. 캐릭터의 윤곽·얼굴과 새 건물이 작은 화면에서 구분되며, 연못은 단순한 물색·낮은 돌 경계가 새 지형과 이어진다. 사용자 판단에 따라 별도 수정안이 필요하면 전/후/팀 비교와 실제 게임 화면을 다시 제출한다.

`Screens-v3/`는 집이 낮게 보이고 지형 대비가 높았던 내부 검토 이력이다. 최종 제출은 `Screens-v4/`와 `Submission-v4/`다. A2 기술 검사나 이번 수정안을 사용자 승인으로 처리하지 않는다.
