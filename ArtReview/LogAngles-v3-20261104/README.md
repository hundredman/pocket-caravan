# 통나무 각도 v3 — 그림 승인 대기

사용자는 긴 v2보다 이전 버전을 선호하며 길막음 용도에 맞게 다른 각도를 요청했다. 기존 가로48×48 PNG/ASE를 그대로 보존하고 세로/사선↗/사선↘를 각각 새로 그렸다. 긴 v2는 미채택·본편 미연결 상태로 남긴다.

- 제작: 내장 image_gen(reference 원본48×48; 각도별1회), 실제 prompt set은 prompts.json, 반환 원본은 Raw/. CLI/API fallback 미사용.
- 정리: Tools/ArtTrials/log_angles_native.lua. 전체 개체 균일축소·기존 팔레트·이진 알파·48×48. 기존 그림 회전/늘이기·부분 잘라붙이기 없음.
- Native/에 PNG/ASE4쌍. native-checks.json에서 재내보내기 디코딩 RGBA 동일·가로 원본 RGBA 동일 확인. native-comparison.png는5배 정수확대+실제48×48.
- game-comparison.png는 같은 본편 길 배경을 복제한 previewOnly에서 촬영한4장(각960×540)을 배치한 표다. Unity/에 원본 캡처·Play11확인. 실제 PPU16/scale1/rotation0이며 방향은 새 그림에 그려져 있다. 소품 뒤 지형을 수정하지 않았다.

그림 검토용 SpriteRenderer만 사용했으므로 실제 장애물 충돌·우회 가능성·길 차단·발생/제거 기능을 검사한 것은 아니다. 기존 본편 장애물 정의3×3·Sprite 바인딩·빈도/보상·ID/저장·맵/씬은 미변경. 승인 후 방향에 맞는 점유/통행 검사를 별도 수행해야 하며 이 그림 검토가 기능 완료를 뜻하지 않는다. 사용자 저장9파일 백업/불변 확인. 자동 회귀 및 Unity8종은 Docs/Validation/LogAngles-Task6-2026-11-04.
