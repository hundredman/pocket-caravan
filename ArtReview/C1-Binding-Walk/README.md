# C1 새5명 4방향·걷기 제출

사용자 “그래.”를 앞선 새5명 정면 승인으로 기록하고, `ArtReview/Approved/C1-Binding-Fronts/`에 해당 PNG/Aseprite·해시를 동결했다. 이 폴더는 다음 단계의 방향·걷기 시안이다.

- `directions.png`: 승인 정면 / 아래 / 위 / 왼쪽 / 오른쪽. 4배 확대와 실제16×24 비교.
- `walk.gif`: 지게꾼·시전 상인·비단 장수·선비·떡 장수, 아래/위/왼쪽/오른쪽 제자리 걷기 확대 보기.
- `unity-walk.gif`: 실제 Unity 장단 Game16캡처를 묶은 GIF. 화면 오른쪽5명만 새 걷기이며 왼쪽 승인10명과 플레이어는 정면 비교용이다. 합성 이미지가 아니다.
- `frames/`: 5명×dulr×4=80개16×24 PNG. 인물별 `.aseprite`는16프레임·dulr 태그·125ms를 가진 실제 편집 원본이다. 시트는 보기/검사용이며 맵에서 잘라 쓰도록 등록한 것이 아니다.

지게꾼은 빈 지게 유지, 상인/비단 장수는 장부/비단 구분, 선비는 유건/책, 떡 장수는 물동이 없이 쟁반을 유지했다. 떡 쟁반은 뒤에서 몸에 가려지고 양끝만 보이도록 했다. 이전 보완대로 옆머리를 채우고 작은 귀·눈 위 피부 틈을 남겼다.

재현: `PYTHONDONTWRITEBYTECODE=1 python Tools/ArtTrials/npc_binding_walk.py`. 기존 렌더러의 방향/걷기 주기를 재사용하되 새5명은 별도 원본/출력 경로다. `--unity-gif`를 붙이면 이미 찍은 Game16캡처를 GIF로 인코딩한다.

80개 크기/알파/발밑/정면승인색 범위·아래0/2 정면 동일·원본5개 재열기/태그범위/시간·A2 오류0 확인. 실제 CharacterAnimator 선택80건·반복/정지40건 일치, Console error0. `checks.json`, `unity-checks.json`, `Docs/Validation/C1-Binding-Walk-2026-10-24.md` 참고.

방향·걷기 미술 승인은 대기다. 임시 Sprite/애니메이션은 실행 중 메모리에만 두었고 본편 PNG/씬/참조/저장을 바꾸지 않았다. 승인 후 C1-Binding대로 역할별 경로로 전체를 한 번에 교체하고 동물 원본을 보관한 뒤 이동·말걸기를 확인한다.

후속 사용자 “좋아.”로 방향/걷기 승인. `ArtReview/Approved/C1-Binding-Walk/`에 원본·80프레임·해시를 동결했다. C1 본편 역할별 일괄교체와 검증은 `Docs/Validation/C1-Production-2026-10-24.md` 참고.
