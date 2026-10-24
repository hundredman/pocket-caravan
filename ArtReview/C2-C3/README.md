# C2·C3 기와집·소나무 시험 · 초기 시안 반려 — 최신 원본 보존 보정은 ../Refine-v2/ 참고

- `comparison.png`: 원래 개체와 새로 그린 후보를 같은 배율로 비교한다. 두 후보 모두 64×48이며 기존 개체의 비투명 외곽 범위와 발밑 위치를 유지했다. 내부 묘사를 단순화한 것이며 픽셀 윤곽 전체가 동일하다는 뜻은 아니다.
- `game-comparison.png`: 각 480×270의 기존 게임 캡처 위에 온전한 개체를 같은 위치에 합성했다. 본편 교체 후 캡처/실제 플레이 검사가 아니다.
- `house.aseprite`, `tree.aseprite`: 편집 가능한 온전한 원본. 게임 후보 PNG는 `house-after.png`, `tree-after.png`.
- `catalogue.json`: A2 시험 목록. 온전한 개체와 렌더링용 16px 조각 전체를 등록했다. `render-parts/` 조각을 일부만 다른 물체로 쓰지 않는다. 실제 맵에는 연결하지 않았다.

기와집은 28→12색, 소나무는 16→8색으로 줄였다. 소나무는 요청서의 대략 12~16색보다 더 적은 색으로 큰 잎 덩어리를 먼저 제안한다. 두 개체의 모양/단순화 정도가 현재 캐릭터와 어울리는지는 사용자 그림 검토가 필요하다. 색은 기존 후보 팔레트에서 골랐으며 D3 승인본을 뜻하지 않는다.

`Tools/ArtTrials/environment_sample.py`가 64×48 실제 격자에 직접 그린 뒤 A5로 팔레트·알파를 정리한다. 새 개체 제작은 옛 AI 시트 좌표를 사용하지 않는다. 옛 렌더링 타일 전체를 다시 모은 것은 **전 비교 자료를 위한 것뿐**이다. 전 이미지는 외부 시트를 새 게임 에셋으로 잘라 쓰는 근거가 아니다.

재생성:

```sh
python Tools/ArtTrials/environment_sample.py
/Applications/Aseprite.app/Contents/MacOS/aseprite -b ArtReview/C2-C3/house-after.png --save-as ArtReview/C2-C3/house.aseprite
/Applications/Aseprite.app/Contents/MacOS/aseprite -b ArtReview/C2-C3/tree-after.png --save-as ArtReview/C2-C3/tree.aseprite
python Tools/Integration/validate_assets.py ArtReview/C2-C3/catalogue.json --preview
```

크기·외곽 범위·후보 팔레트·알파0/255·원본 재열기 픽셀 일치, A2의 온전한 개체/렌더링 조각 일치를 확인했다. 배치·Unity 임포트·깊이 정렬·충돌·플레이는 검사하지 않았다. **승인 전에는 본편 Assets/·맵·씬·등록 코드를 바꾸지 않는다.**
