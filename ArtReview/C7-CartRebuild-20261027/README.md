# 수레 구조 재제작 · 검토 대기

사용자는 앞 수정본의 원본 구조와 픽셀화 후 수레 인지성을 지적했다. 앞 수정본은 `changes_requested`로 보존하고 이 폴더에 새 시안을 만들었다.

AI 초안은 구도 참고로만 썼다(`AI/provenance.json`). 최종 원본은 Aseprite의 32×32 격자에 직접 그린 3개 레이어다. 적재함의 어두운 빈 공간, 두 손잡이, 앞뒤 바퀴를 구분하고 후보 7색으로 정리했다. D3 공통 팔레트 확정은 아니다. A5 입력·출력이 모두32×32이며 축소하지 않는다. 캔버스와 하단30은 유지했다. 원본 전체를 4개 렌더 칸으로 등록하며 일부 조각만 재사용하지 않는다.

## 비교와 확인

[비교판](comparison.png)은 앞 수정본과 새 원본의12배·실제1배·Unity480×270 화면이다. Unity Play 검토용 장면이며 마을 실제 보행 검사는 아니다. before는 앞 수정본의 trial과 크기·디코딩RGBA가 완전히 같다. 전후 차이는 수레32×32 영역 안에만 있고, 새 불투명 원본과 화면 RGB 오차는1 이하다. 우물·석등과 주변 그림은 유지했다.

새 검사4개와 앞 수정본 기록 검사4개, 총8개 통과. 원본 재출력RGBA, 전체 칸 재조합/누락 거부, 후보색·이진알파·발밑, 이웃 그림 보존과 촬영 동일성을 검사했다. A2 검토0오류이며 미승인 본편 등록은 거부된다. 이는 형태 인지성에 대한 사용자 승인을 뜻하지 않는다.

## 재현

1. Aseprite 배치 실행으로 `Tools/ArtTrials/c7_cart_rebuild.lua`에 `root`를 이 폴더의 절대경로로 전달한다.
2. Pillow가 있는 Python에서 `Tools/ArtTrials/c7_cart_rebuild.py prep`, 이어 `register`를 실행한다.
3. 이 worktree의 Unity6000.3.26f1에서 `PocketCaravan.EditorTools.C7CartRebuildReview.Run`을 실행한다(Burst 비활성/worker2). 검토용 Play 촬영 후 종료한다.
4. 같은 Python으로 `Tools/ArtTrials/c7_cart_rebuild.py board`를 실행한다.

사용자가 바퀴·손잡이 위치와 작은 크기의 조잡함을 지적해 이 시안은 changes_requested다. 후속 시안은 `../C7-CartRoof-20261027/`에 보존했다. 우물·석등 자동 승인 없음. 본편·기존 승인본·ID·저장·충돌 미변경. 실제 보행·수동 플레이·저장 회귀·전체 통합·게임 빌드는 실행하지 않았다.
