# C1 사람 NPC 3명 시험 · 사용자 승인 대기

- `comparison.png`: 현재 동물 NPC와 신규 사람 시안, 같은 배율. 실루엣과 기존 플레이어 기준도 함께 표시했다.
- `game-comparison.png`: 각 480×270. 실제 기존 게임 캡처를 원래 픽셀 크기로 되돌리고 같은 위치에 기존/후보 NPC를 합성한 비교다. 교체 후 Unity 화면이나 실제 플레이 검사가 아니다.
- `directions.png`, `walk.gif`: dulr 방향과 4프레임 걷기. 48개 프레임은 `frames/`, 세 원본은 `*.aseprite`에 있다. Aseprite 원본은 4열×4행 시트 형태로 편집할 수 있으며 셀마다 16×24다.
- `catalogue.json`/`manifest-preview-report.json`: A2 목록·PNG 기술 검사만 통과했다. 맵 배치·Unity 임포트/충돌·실제 이동은 미검증이다. 검토 상태는 모두 pending이다.

ArtBrief에 허용된 직접 도트 제작 방식으로 `Tools/ArtTrials/npc_sample.py`에서 그렸다. 같은 몸을 복사해 모자만 바꾸지 않고, 촌장의 좁은 도포·지팡이, 아낙의 넓은 치마·물동이, 행상의 큰 비대칭 지게 짐을 따로 만들었다. `review_npc.py`로 비교 그림을 생성한다. A5로 현 팔레트 후보에 맞추고 알파를 0/255로 정리했다. 팔레트 승인이나 미술 적합성은 자동 판정하지 않았다.

재생성(저장소 루트):

```sh
python Tools/ArtTrials/review_npc.py
# 각 이름은 naru_chief, woman, trader
/Applications/Aseprite.app/Contents/MacOS/aseprite -b ArtReview/C1/naru_chief-sheet.png --save-as ArtReview/C1/naru_chief.aseprite
python Tools/Integration/validate_assets.py ArtReview/C1/catalogue.json --preview
```

**본편 미반영.** Assets/ 캐릭터, 기존 동물 그림, 맵, 씬, NPC ID를 수정하지 않았다. 보관 폴더 이관과 나머지 NPC 제작도 승인 뒤다. 승인되면 이 시안 기준으로 나머지 인물을 제작하고 Unity 임포트·실제 화면을 별도로 확인한다.
