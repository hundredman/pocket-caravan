# C1 새5명 걷기 검사

브랜치 codex/c1-expand, 사용자 “그래.”를 직전 정면5명 승인으로 기록하고 원본/해시를 동결했다. 본편 파일은 변경하지 않았다.

## 실행한 검사

- `PYTHONDONTWRITEBYTECODE=1 python Tools/ArtTrials/npc_binding_walk.py`: 80 PNG16×24, 알파0/255, 최하단24, 인물별 승인 정면의 색 범위, 아래0/2가 승인 정면과 픽셀 일치. 각 방향에 구분 프레임 최소3개 확인.
- Aseprite5개 재열기 시트와 생성80프레임 픽셀 일치, 16프레임/dulr 태그범위 각4프레임/125ms 확인.
- 새5명 정면 생성기의 layout 반환 옵션 추가 후 기본 출력이 승인본과 동일한지 확인. 기존 승인10명 PNG/걷기 원본과 기존 동결 참조 불변 확인.
- `validate_assets.py ArtReview/C1-Binding-Walk/catalogue.json --preview --report ArtReview/C1-Binding-Walk/manifest-report.json`: 기술 미리보기 오류0. 실제 배치/스타일 승인은 제외.
- 방향 비교 그림과 실제 Game 오른쪽 걷기 캡처를 열어 확인했다.
- 실제 Unity 장단 Play의 기존 정면 비교5개 오브젝트에 CharacterAnimator16프레임을 메모리로 추가했다. PPU16·pivot(8,6)·scale1 유지. 실제 LateUpdate 프레임선택80건, 루프/정지40건 모두 일치. 실제 Game16캡처/GIF 제출.
- autosave=false·sceneDirty=false·paused=true, Console error0. 임시 화면은 오른쪽을 보고 일시정지했으며 재개하면 제자리 걷기를 재생한다. Play 종료 시 원래 씬으로 돌아간다.
- main 추적 파일 변경0개(기존 미추적 3D 실험 보존), `git diff --check` 통과.

## 아직 확인하지 않은 것

새5명 방향/걷기 미술 승인, 본편 전체 역할별 경로 교체·동물 원본 보관/등록·새 우물가 아낙 추가, 맵 실제 이동·충돌·말걸기·기존 ID/대사/저장 회귀. 이번에 전체 게임 테스트나 빌드는 하지 않았다.

미술 승인 후에 C1-Binding 순서대로 전체 일괄 교체를 진행한다. 현재는 일부 NPC만 사람으로 저장한 중간 상태가 아니다.
