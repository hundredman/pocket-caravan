# C7 팀 그림체 수정안 · 건물2개 승인 / 땅 미확정

2026-10-27 초가집·기와집 그림 승인: [동결 기록](../Approved/C7-Buildings-v5/approval.md). 땅은 미확정이다. 처음 본편의 경계 표현은 [원래 지형 재확인](GroundOrigin-v7/README.md)을 본다. 앞선 [땅만 비교](GroundCompare-v6/comparison.png)의 “이전”은 C7 중간안이었다.

2026-10-27 사용자 정정: “특징”은 **베네치아·아라비아 팀 아트의 표현 방식**이다. Figma 원본을 읽기 전용으로 확인하고 초가집·기와집·풀/흙을 다시 만들었다. 나무·기존 승인 캐릭터·작은 연못·본편은 유지했다.

- [직전 시안 / 이번 수정안 / Figma 팀 원본](Submission-v5/style-comparison.png)
- [새로 촬영한 Unity 전후](Submission-v5/game-comparison.png), [게임 원래 크기](Submission-v5/game-native.png), [3배 확대](Submission-v5/game-3x.png)
- [생성 요청 원문](AI/prompts.json), [A2 목록](catalogue.json), [검증 기록](../../Docs/Validation/C7-TeamStyle-2026-10-27/README.md)

## Figma에서 확인한 것

`77:5`와 `92:2`는 제목 프레임이었다. 같은 페이지 아래 실제 이미지 노드를 확인했다.

| 원본 | 확인 노드 | 관찰 |
|---|---|---|
| 베네치아 집·소품 | `77:9`, `78:11` | 진한 바깥 윤곽, 넓은 지붕/벽 색면, 몇 개의 굵은 장식·벽돌 모양 |
| 아라비아 집 | `92:31`, `92:32` | 넓은 밝은 벽과 한쪽 면 그림자, 단순한 문·창, 소수의 강조색 |
| 아라비아 길·풀 | `92:102`, `92:107` | 얼룩 대신 짧은 풀잎·흙 모양을 평평한 바탕에 배치 |

Figma 파일과 팀 에셋을 고치지 않았다. 캡처6개와 노드/크기 기록은 `Reference/`에 있다. 표시된 Figma 크기는 캡처 크기이며 업로드 원본의 실제 도트 배율을 확정한 값은 아니다. 비교용 참고 그림을 게임 그림으로 복사하지 않았다.

## 이번 수정

- 두 집은 새 AI 원본 전체를 종횡비 유지·최근접으로 실제 격자에 옮겼다. 큰 지붕 색면, 소수의 장식, 단순한 문·창, 한쪽 벽 그림자로 팀 표현에 맞춘다. A5 후 Aseprite에서 외곽선을 정리했으며 지붕을 떼거나 벽체를 별도로 다시 그리지 않았다.
- 맵상64×48 캔버스·발밑은 유지한다. **실제 보이는 높이는 초가39px·기와37px로 직전보다 낮다.** 새 집 비례도 이번 그림 검토 대상이다. 엔진 배율/맵 배치를 바꾸지 않았다.
- 풀/흙은 명확한 짧은 모양을 실제16px에 그렸다. 경계47종·흙 중심, 가운데 변형6종이 있으며 변형은 별도 완성 그림으로 등록해 고른다. 맵 생성 중 색·크기 변환이나 자르기를 하지 않는다.
- 집16색·지형6색은 시안용 후보다. 팀 공통 D3 팔레트 확정은 아니다. 나무는 이전 원본/PNG 그대로 사용한다.

## 기존 승인 에셋 함께 보기

플레이어·아낙·촌장·작은 연못22칸을 전후 같은 위치에 배치했다. 현재 제작자 의견은 **수정 불필요**다. 캐릭터의 윤곽/얼굴과 밝은 색 덩어리가 새 환경 옆에서도 읽히며, 연못의 단순한 물색과 낮은 돌 경계가 이어진다. 사용자 판단에 따라 수정이 필요하면 승인본/수정안/팀 비교와 게임 캡처를 별도로 제출한다. 승인본·해시를 바꾸지 않았다.

## 재현

저장소 루트에서 Pillow12.3 이상인 Python을 쓴다. 나무 `Native/pine.*`는 기존 첫 시험의 원본2개를 그대로 복사한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-TeamStyle-20261027" --script Tools/ArtTrials/c7_rebuild_seed.lua
python Tools/ArtTrials/c7_team_prepare.py
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-TeamStyle-20261027" --script Tools/ArtTrials/c7_team_finish.lua
python -c "import sys;sys.path.insert(0,'Tools/ArtTrials');import c7_rebuild_catalogue as c;c.run(c.ROOT/'ArtReview/C7-TeamStyle-20261027',c.OLD,'c7ts','Tools/ArtTrials/c7_team_finish.lua')"
```

Unity 전용 작업 폴더에서 `PocketCaravan.EditorTools.C7TeamStyleReview.Run`을 실행하고 `c7_team_review.py`로 비교 그림을 만든다. 최종 경로는 `Screens-v5/`, `Submission-v5/`다. 재제출 때 경로 버전을 올려 이전 제출을 덮어쓰지 않는다. 이번 화면은 previewOnly 아트 시험장으로 실제 B3 마을 배치·수동 플레이를 보여 주는 자료는 아니다.
