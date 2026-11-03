# 승인 범람 지형 런타임 연결 검증

그림 승인: `../E3-FloodTerrain-20261103/approval.json`. 이번에는 Editor 전용 F6 대신 실제 `RiverFloodTerrain` 컴포넌트를 선택 맵 메타데이터로 생성해 기존 사건 일정에 따라 자동 전환했다. 이 검증 이후 사용자 담당 변경에 따라 Codex가 실제 본편 연결까지 완료했다(아래 후속 적용 참조).

- `runtime-comparison.png`: 실제 Unity480×270 캡처 세 장(비범람일 / 범람일 물 참 / 같은 범람일 물 빠짐).
- `terrain-binding.json`: 승인 지형의80개 변경 칸 쌍. 물62칸 확대/길22칸 침수. 최신 맵 원본에서 재생성할 것.
- `Unity/checks.json`: 자동 검사3725확인, 반복 칸 검사 포함.
- 재현: 기존 `Tools/ArtTrials/e3_flood_terrain.py`로 타일/검토 입력 준비 → `e3_flood_runtime.py` → Unity 메뉴 `Pocket Caravan/Tests/E3 범람 자동 연결 검토 만들기` → `실행`. 자동 테스트는 `RiverFloodRuntimePlayReview`를 검토 Play에만 붙여 실행한다.

본편 Maps/Scenes/원본 지형/저장/DB는 변경하지 않았다. 다음 담당 작업은 `Docs/Goryeo/E3-Flood-Binding-2026-11-03.md`에 정리했다. 정적357(16skip)·최종 대상4개·월드0오류/1437출구·Unity3725확인/3캡처·저장9파일 보존. 실제 본편 씬/사람 플레이/서버 동기화는 미검사다.

2026-11-03 후속 적용: 사용자 담당 변경에 따라 Codex가 승인 범람의 본편 JSON/씬 및 원본 내보내기 연결을 완료했다. 이 폴더는 당시 검토/검증 기록이다. 최신 본편 결과는 `../E3-FloodProduction-20261103/README.md` 및 `Docs/Goryeo/E3-Flood-Binding-2026-11-03.md` 참조.
