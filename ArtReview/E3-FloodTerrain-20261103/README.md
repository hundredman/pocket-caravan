# E3 범람 지형 검토 — 본편 미적용

2026-11-03 사용자 지시: 기존 강을 자연스럽게 넓혀 길을 물로 끊는다. 폐기한 별도 범람 스프라이트는 사용하지 않는다.

원본: `Tools/ArtTrials/e3_flood_terrain.py`. 실제 `transit_wetroad.json`을 읽어 기존 강·물가 조립 타일만으로 평상시/범람 상태를 만든다. 원본 맵/씬/에셋과 ID·저장은 변경하지 않는다. 새 PNG 제작·자르기·색 바꾸기 없이 기존 타일을 그대로 배치한다. 수면 내부가 된 기존 강둑과 대각 안쪽 모서리까지 함께 다시 계산한다. 검토 씬 양쪽의 옛 ripples 참조는 기존 고려용 goryeo_ripples로 맞춰 불투명한 파란 사각 바탕을 피했다(기존 PNG/알파·본편 참조 유지).

범람: 새 물62칸, 그중 길22칸 침수. 숲·다리·기능물/출입구/도착 위치는 그대로다. 그림 승인 전 확정 배치가 아니다. 경계 너머 다른 맵 지형은 바꾸지 않는 내부 구간이다.

## 확인할 그림

- `terrain-comparison-static.png`: 기존 타일로 그린 정적 전후 비교(게임 캡처 아님).
- `unity-comparison.png`: 실제 Unity480×270 평상시/범람 전후 묶음.
- `Unity/restored.png`: 물이 빠진 뒤 원래 강·길 복원 화면.
- `normal-map.png`, `flood-map.png`: 전체 맵 정적 렌더.

## Unity에서 직접 확인

1. Python: `/tmp/pocket-market-py/bin/python Tools/ArtTrials/e3_flood_terrain.py` (numpy/scipy/Pillow 필요).
2. Unity 메뉴 `Pocket Caravan > Tests > E3 범람 지형 검토 만들기`.
3. `Pocket Caravan > Tests > E3 범람 지형 검토 실행`.
4. WASD로 움직이며 **F6 또는 좌상단 버튼**으로 평상시/범람 전환.

생성 위치: `Assets/PocketCaravan/Editor/B2Preview/E3FloodTerrain/Scenes/normal.unity` (검토 전용, git 제외). 본편 BuildSettings/WorldIndex에는 넣지 않았다. 검토 범위 밖 본편으로 이동하는 출구는 검토 씬에서만 비활성화했다. previewOnly로 기존 저장 불러오기/자동 저장을 끈다.

전환은 같은 씬의 Ground/Deco/Blocked/RoadGrid를 한 번에 바꾸고, 해제하면 기존 타일 참조/길 등급/충돌을 복원한다. 캐릭터가 잠길 칸에 서 있으면 검토용 전환은 보류한다. 이는 검토 안전 장치이며, 본편 사건의 경고/유예 정책은 후속 연결 때 확인한다.

실제 E3 일정 자동 발생, 게임 시간에 따른 물 빠짐, 네트워크/저장 연결은 아직 하지 않았다. 이번에는 수동 전환 가능한 지형·통행 검토를 구현했다. 승인 후 기존 `river_flood` ID/일정과 연결하며 별도 범람 PNG를 다시 사용하지 않는다.

검증: 정적354개(16skip) 실패0, 최종 대상4개 통과. 실제 Unity Play 자동 확인523항목/3캡처 완료. PlayerMotor 침수 길 차단·복원 뒤 통과, 타일/충돌/등급 복원·캐릭터 위치 안전 보류·반복 전환 확인. 저장9파일 동일. 사람 수동 플레이는 하지 않았다. Docs/Validation/E3-FloodTerrain-2026-11-03 참조.
