# 논 밀도 v4 — 간격을 조금 좁히기

**사용자2026-10-29 요청 “조금 빽빽하게 심어봐”·그림 승인 대기·본편 미적용.**

벼 그림은 RipeRice-v3의 곧은 줄기/숙인 이삭/황금색 그대로다. 크기·회전·색을 바꾸지 않고 가로·세로 식재 간격만16→14px로 좁혔다. 같은224×224px 논 면적에196→256포기, 약31% 더 심었다.

- [기존16px / 수정14px 비교](comparison.png): 같은 면적·같은2배.
- [Unity 게임 크기](unity-480x270.png), [9종 구성](module-sheet.png), [검사](checks.json), [조립 정보](catalogue.json).
- 논리 블록은7×7칸(112px), 한 블록에8×8포기다.14px 간격/행4px 엇갈림 주기와16px 엔진 격자가 모두 반복되게 맞췄다. 중앙/가장자리/바깥 모서리9종, 외곽 여백은 좌우16/위32/아래16px로 유지했다.
- 직사각형 논 최소2×2블록, 즉14×14칸부터 조립한다. 이전6×6칸 블록과 크기가 같지 않으므로 이 버전의 catalogue 오프셋/크기를 함께 사용한다. 본편 A2에는 아직 연결하지 않았다.

## 재현

Tools/ArtTrials/rice_field_tiles.lua에 revision=v4, plant=ArtReview/RipeRice-v3/rice-ripe.png, pitch=14, blockCells=7, referencePitch=16을 지정한다. Python 조립 검사도 --revision v4 --plant ArtReview/RipeRice-v3/rice-ripe.png --pitch 14 --block-cells 7 --reference-pitch 16으로 실행한다.

5크기 조립 결과가 전체 포기 직접 배치와 디코딩RGBA 일치, 원본 벼 지문·색/알파 보존. Unity Edit9모듈 연결의 논 구역도RGBA차이0·촬영후 원래씬 복귀/본편쓰기0. 실제 플레이/충돌가림/바람/본편 연결/빌드는 미검사다.
