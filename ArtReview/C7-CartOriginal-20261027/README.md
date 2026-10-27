# 수레 복원 대상 정정

사용자 “수레는 이것보다 더 처음 버전을 말한거야.”에 따라 첫 C7 소품 시안으로 잘못 해석했던 선택을 정정했다. 선택 대상은 C7 이전 기존 게임의 `Assets/PocketCaravan/Art/Baked/Objects/c6_cart_e1a728ea.png`다. 이를 픽셀 그대로 검토 화면에 복원했다. 원래 본편 수레는 변경되지 않았으므로 본편 재가공·교체는 필요 없다. 새 그림을 만든 것이 아니다.

기와집은 직전 v2의 청회색 용마루를 양쪽 화면에서 유지한다. 지붕의 최종 사용자 그림 승인은 아직 대기다. 다른 건물·우물·석등·인물·지형·연못은 그대로다.

[비교판](comparison.png): 왼쪽 잘못 복원했던 첫 C7 시안, 오른쪽 기존 게임 수레. 각8배/실제1배와 최신 실제 Unity Play480×270을 함께 제시했다. 원본은 `cart.png`에 복사하고 크기·디코딩RGBA 일치를 확인했다. 런타임 수정·축소·색 정리 없음. 본편/ID·저장·충돌 미변경.

재현: 이 worktree Unity6000.3.26f1에서 `C7CartOriginalReview.Run`을 실행(Burst 비활성·worker2) → Pillow가 있는 Python으로 `Tools/ArtTrials/c7_cart_original.py` 실행. 실제 마을 보행 검사는 아니다.

새2+직전 기록5=7검사 통과. 캡처 before는 직전trial과 크기·RGBA 일치, 차이는 수레 영역에만 존재, 최신 불투명 원본/화면RGB 오차≤1. Unity 종료0·C# 오류0/SearchDatabase 예외0. 보행·수동 플레이·저장 회귀·전체 통합·게임 빌드는 실행하지 않았다.
