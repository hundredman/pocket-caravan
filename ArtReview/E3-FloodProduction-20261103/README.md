# 승인 범람 본편 배치 완료

2026-11-03 사용자 담당 변경 지시에 따라 Codex가 승인된 범람 지형을 실제 북동 길 맵/씬에 연결했다.

- 원본: `Tools/WorldLayout/m4_export.py` → `river_flood_binding.attach()`.
- 실제 맵: `Assets/PocketCaravan/Maps/transit_wetroad.json`, 기존 모든 필드/ID/지형을 보존하고 `terrainFloods` 1개만 추가.
- 실제 씬: `Assets/PocketCaravan/Scenes/Transit/transit_wetroad.unity`. 승인된 강 확장62칸/길22칸 침수, 기존 일별40%·9초/6초 유지.
- `production-comparison.png`: 실제 본편 씬의 평상시/범람/물 빠짐480×270 3장. 물결의 옛 파란 배경은 아직 별도 승인 대기인 기존 본편 참조이며 이번 범람 지형의 테두리가 아니다.
- `Unity/checks.json`: 본편 Play3740확인(칸별 반복 포함), 물·길·충돌 전환/복원·PlayerMotor 통행·위치 보류/재시도·실제 출구Use/Travel 왕복·옛v2저장 형식 시험 복원. 사용자 파일을 바꾸지 않도록 previewOnly 세션에서 자동 저장을 끈 뒤 실제 본편 씬(previewOnly=false)을 불러 검사했다.

정적359(16skip)실패0, 월드0오류/1437출구, 저장9파일 보존. 사람 플레이·모든 구형 저장 사례·서버/온라인은 미검사. 다른 미승인 그림/포구는 아직 본편 적용하지 않았다. GitHub push/PR/통합 미실행.

최신 연결 기록: `Docs/Goryeo/E3-Flood-Binding-2026-11-03.md`. 검증: `Docs/Validation/E3-FloodProduction-2026-11-03/`.
