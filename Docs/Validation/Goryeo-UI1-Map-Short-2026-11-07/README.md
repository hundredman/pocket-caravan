# 짧은 지도 이름·양옆 배치 검증

- ArtReview/Goryeo-UI1-Map-Short-20261107/play.json:최종295확인·15캡처·complete true/error 없음.
- 모든 고려 캡처에서 이름표/지형 겹침0, 모든15캡처에서 이름표/점/핀/범례 겹침0·글자 수용·캐시 재사용 확인.
- first-attempt:초기360×270 모든 목적지 이름표 중첩 실패120확인과6캡처. 이후 열 높이 배분 보완·반복 목적지 문구 제거 후 재실행했다. 최종 결과와 합산하지 않는다.
- python-full.log:403개·23skip·통과. 마지막 C# 열 배분 보완 전 실행이며 그 뒤 배치는 Unity295확인으로 검증했다.
- world-validation.txt:최종0오류·1437출구.
- save-preservation.json:기존8해시 불변·임시0. 최종 Console error0·Play 종료.

[범위와 미검사](../../Goryeo/UI1-Map-Short-2026-11-07.md). 사람 직접 플레이/네이티브 클릭/실제 온라인/빌드/FPS는 미검사다. 수정 그림 승인 대기·원격 미반영.
