# UI1 공용 글자 검증

- ArtReview/Goryeo-UI1-Text-20261107/before/play.json:원본46확인·15캡처.
- 같은 경로 after/play.json:최종48확인·15캡처. 두 결과 complete true/error 없음. 실제 입력은 가상 Keyboard이며 사람 플레이가 아니다.
- intermediate-common-only-play.json:접속창 별도 경로 보완 전 중간46확인. 최종 결과와 합산하지 않는다.
- python-full.log:403개·23skip·실패0.
- world-validation.txt:0오류·1437등록 출구.
- pixel-check.json:검토용 흰 글자 행만 디코딩 RGBA 동일.
- save-preservation.json:기존8파일 SHA256 불변·일회용0.
- map-name-audit.json:13전체/5상세 이름을 WorldIndex와 대조. 전체13개 중10개 불일치.
- 최종 Unity Console error0·Play 종료 확인.

[수정 범위/미검사](../../Goryeo/UI1-Text-2026-11-07.md). 네이티브 클릭/한글 IME/실제 온라인/전체 UI/이전 Editor8종/게임 빌드/FPS는 미검사. 새 글자 비교는 사용자 승인됐으며 원격 미반영이다.
