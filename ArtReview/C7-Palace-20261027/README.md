# C7 궁궐 첫 확대 시안

승인된 초가집·기와집 다음 대상인 궁궐을 제작했다. 팀 베네치아·아라비아의 큰 색면·윤곽·단순한 명암과 승인된 기와집의 청회색 기와를 참고했다. 첫 AI 초안은 낮고 넓어 두 번째 생성에서 지붕·기둥 비례를 재설계했다.

## 범위와 원본

- 본편 원본: `Assets/PocketCaravan/Art/Baked/Sprites/hall_10_7.png`, 160×112, 불투명 경계(4,4)~(160,112). 과거 `Environment-Expand-v2/palace-before.png`와 디코딩 RGBA가 같다.
- 새 시안: `Native/palace.aseprite`와 PNG. 동일160×112/10×7칸, 전체 개체 균등 최근접 샘플링·바닥 기준112 유지. 새 불투명 경계(2,14)~(158,112)로 폭156px는 같고 보이는 높이는108→98px다. 그림을 비균등하게 늘이지 않았다. 캔버스·앵커 유지와 실제 윤곽 변화는 구분한다.
- 생성 원본2개와 역할은 `AI/provenance.json`. 팀 자료는 이미지 생성 참고 및 비교판에만 쓰며, 픽셀을 본편이나 시안에 잘라 붙이지 않았다.
- `Prepared/`는 온전한 개체의 네이티브 샘플 및 A5 정리본이다. 알파224 미만 생성 가장자리 흐림을 버리고, 승인된 집 후보16색+붉은 기둥/청록 창살 후보5색으로 정리했다. 후보21색이며 D3 공통 팔레트 확정이 아니다.
- Aseprite는 A5 본체와1px 외곽선 별도 레이어를 보관한다. 전체70개16×16 조각은 렌더 분할이며, 일부만 사용하거나 다른 건물에 조각을 붙이지 않는다.

## 재현

저장소 루트, Pillow가 있는 Python 환경에서 실행한다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Palace-20261027" --script Tools/ArtTrials/c7_palace.lua
python3 Tools/ArtTrials/c7_palace.py prep
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param root="$PWD/ArtReview/C7-Palace-20261027" --script-param finish=true --script Tools/ArtTrials/c7_palace.lua
python3 Tools/ArtTrials/c7_palace.py register
```

이 worktree 에디터가 닫힌 상태에서 Unity6000.3.26f1의 `C7PalaceReview.Run`을 실행한다(`--burst-disable-compilation -job-worker-count 2`). 실제 Play 카메라480×270 캡처가 생긴 뒤 `python3 Tools/ArtTrials/c7_palace.py board`로 제출판을 만든다. 별도 수정안은 새 폴더/ID/촬영 파일로 버전 관리한다.

## 제출과 검증

- [기존/새 시안/팀 그림체](style-comparison.png): 고려 그림3배와 실제1배 비교. 팀 원본 이미지는 전체 비율을 유지해 표시하되 도트 배율이 같다고 주장하지 않는다.
- [실제 Unity 전후](game-comparison.png): 궁궐만 교체한 검토 시험장. 같은 원래 지형·승인된 기와집·인물, 같은 v13 연못/장식(검토용). 실제 마을 배치가 아니다.
- A2 검토0오류, 미승인 본편 거부. 전체70칸 RGBA재조합, 일부 누락 거부, 이진알파/후보색/발밑/1px외곽선, 실제 Aseprite 재출력, 실제 카메라 궁궐밖차이0 검사 통과. 기존 집·연못 검사 포함 관련24개 통과.
- Unity 기존 SearchDatabase 예외1회·종료0. 보행·수동플레이·저장회귀·전체통합·게임빌드 미실행. 기술 검사는 미술 승인 아님.

2026-10-27 사용자 “좋은 것 같아.” 응답으로 궁궐 그림 승인. `ArtReview/Approved/C7-Palace-v1/`에 크기·RGBA 증거와 원본을 보존했다. 본편 맵·시설/NPC/퀘스트/저장 ID·충돌·개경 승인본·기존 승인 아트는 미변경이다. 다음은 나머지 건물 제작이다. 제출 당시 비교판의 검토대기 문구는 이력으로 보존한다.
