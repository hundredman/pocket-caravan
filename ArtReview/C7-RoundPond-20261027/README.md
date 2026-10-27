# 풀밭용 둥근 연못 수정안 · 승인 대기

2026-10-27 사용자: “좋긴 한데 연못 디자인 개선도 필요하겠다. 삐죽 튀어나온 디자인이 별로네.”

[실제 Unity 전후·확대 비교](../C7-TeamStyle-20261027/PondShape-v10/comparison.png).

기존 물 조합은 가운데 줄에만 양옆 끝 조각이 붙는11칸 배치라 돌출부가 생겼다. 같은5×3칸 범위에 완만한 타원형을 새로 그렸다. 물색·얕은 물·물가의 원래3색을 유지하며, 포장이나 돌을 덧붙이지 않았다. 원래 풀/길과 승인 건물·나무·인물은 같다. 개경용 A4 승인본/원본과 본편은 변경하지 않았다.

## 제작·사용 범위

- 기존 A4의 편집 가능한 네이티브 설계에 대한 국소 수정으로 Aseprite Lua에서80×48을 실제 도트로 그렸다. `pond.aseprite`가 수정 원본, `pond-source.png`가 첫 출력, A5를 통과한 `pond.png`가 제출 그림이다. AI 재생성·전체 그림 늘이기·부드러운 보간을 쓰지 않았다.
- `pieces/oval_*`는 새 타원 전체를 구성하는16×16 조립 조각15개다. `layout.json` 배치 그대로 한 번 놓는 **고정 타원 프리셋**이다. 임의로 넓히거나 다른 호수/강과 연결하는 범용 물 세트가 아니다.
- A2 목록에는 가운데/가장자리/바깥·안쪽 모서리 정의를 포함했다. 오목 모서리는 기존 A4의 온전한 물 조각을 원래 용도로 참조하며, 볼록한 타원 프리셋에는 배치하지 않는다. 새15조각은 정확히 조합할 때 전체 PNG와 픽셀 일치한다. 다른 자유 조합의 접합/범용성은 검사하지 않았다.
- `review.status=pending`, 풀 위·길 금지 조건이다. 미승인 본편 배치 검사는 실패하도록 확인했다. 물결 애니메이션은 추가하지 않았다.

## 실행한 검사

- A5 명시 원래3색/이진알파, A2 검토배치0오류.
- 새4개+기존팀스타일5개=관련9개 통과([결과](../../Docs/Validation/C7-TeamStyle-2026-10-27/round-pond-tests.txt)).15조각 재조합RGBA일치, 줄마다 끊기지 않은 타원·완만한 폭 변화, 실제 Aseprite 재열기/PNG 출력 일치, 미승인 본편 거부, 실제 전후 카메라 변화가 연못80×48영역 안에만 있음 포함.
- 실제 Unity previewOnly Play480×270 전후 새촬영, 종료0. 기존 SearchDatabase 인덱싱 예외1회. 원래 지형과 승인 그림은 그대로다. 전체 통합/실제보행/수동플레이/저장회귀/게임빌드/새연못 충돌 적용은 미실행.

아트 승인 뒤 풀밭 배치가 필요한 곳에 연결할 때 물칸/충돌·길 이격·실제 접근을 따로 확인한다. 현재는 그림 검토본만 만들었다.

```sh
/Applications/Aseprite.app/Contents/MacOS/aseprite -b --script-param "root=$PWD/ArtReview/C7-RoundPond-20261027" --script Tools/ArtTrials/c7_round_pond.lua
python Tools/ArtTrials/c7_round_pond.py
```

Pillow12.3 이상이 필요하다. Unity 전용 폴더에서 `PocketCaravan.EditorTools.C7RoundPondReview.Run`, 이후 `c7_round_pond_review.py`를 실행해 캡처/비교한다. 최종 제출 경로는 `PondShape-v10/`이며 새 제출 때 버전명을 올린다.
