# 포구 승인 적용

사용자 “포구는 조항”을 “포구는 좋아”로 이해했다고 시작 때 고지했다(approval.json). 승인 검토 범위만 적용: 남동 포구1128칸·버들나루727칸의 빈 막힌 바다 배경과 버들나루 잔물결20참조. 새 그림 제작이나 통행 변경은 없다.

Tools/WorldLayout/port_water.py·port_water.json을 원본으로 최신 Before 두 맵에서 재현하며 m4_export에 후속 연결했다. 전체 내보내기는 실행하지 않고 두 JSON/BuildSelected 두 transit 씬만 재생성했다. applied.json 정확한 delta, bounds-checks.json 카메라 경계 불변. blocked/water/road/ID/좌표·기존 장터/큰 장·출입구 유지.

comparison.png는 이번 실제960×540 전후4캡처를 그대로 배열했다. Unity/play.json 자동 Play19확인(막힘/그림/잔물결/시장 연결 등), 사용자 저장9파일 불변. 사람이 직접 걸어 본 검사·온라인은 미실행. 정적363 두 환경/월드/Unity8종 결과는 Docs/Validation/PortWater-Task6-2026-11-04. 옛 회귀 검사3곳은 승인 delta만 역변환하며 별도3검사로 변경 누락·예상 밖 변경을 잡는다.
