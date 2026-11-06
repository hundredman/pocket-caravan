# ATM-B3 앉기 검토 검사

상세 범위·제한은 `Docs/Goryeo/Seats-ATM-B3-2026-11-05.md`, 실제 Play 결과/화면은 `ArtReview/Goryeo-Seats-20261105/Unity/`다. 최종 Play54확인·6캡처. previewOnly/저장OFF 검토이며 본편 연결/그림 승인은 아니다.

- python-full/pillow: 전체391개 OK, 23/32skip. 마지막 자리 보정 후 final-geometry 각2개 OK.
- world-python/world-unity:0오류1437출구.
- scene-terrain:54씬, 나머지6개 JSON 포함 Unity8종 완료.
- save-preservation: 사용자9파일 백업 후 해시 불변.
- console-errors: 최종 error0.

초기 QA의 LateUpdate 이전 Sprite 비교 실패는 검토 폴더의 별도 JSON에 보존했다. 렌더 종료 후 검사로 정정했다. 사람 키보드/온라인/빌드/실기 FPS·장시간·다른 출신 앉기는 미검사다.
