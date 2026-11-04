# 길게 만든 통나무 v2 — 그림 승인 대기

사용자 요청: “통나무같은 경우는 조금 더 길어야 할 것 같아.” 기존 전체 개체를 참고해 내장 image_gen edit로 몸통 길이를 추가했다. prompt.txt가 실제 프롬프트이며 Raw/fallen_log.png는 반환 원본이다. Tools/ArtTrials/long_log_native.lua로 전체 개체를 균일 축소/기존 팔레트/이진 알파로 정리했다. 가로로 늘여 찌그러뜨리거나 조각을 이어 붙이지 않았다.

Native PNG/ASE 캔버스64×48, 기존48×48. 불투명 경계는 기존46×25→62×22이며 새 그림의 가지/윤곽 때문에 전체 높이는 조금 달라졌다. native-comparison.png는 같은 정수 배율+실제 픽셀 크기 비교, game-comparison.png는 실제 게임 전후960×540 두 장이다.

LongLogTrialReview/LongLogTrialPlayReview 검토 전용 previewOnly 씬에서 PPU16·transform scale1로 확인했다(Play7확인). 저장9파일 불변. PNG/ASE 재내보내기는 파일 바이트 대신 디코딩 RGBA로 일치 검증(native-checks.json). 원본 팔레트·이진 알파 유지.

본편 obstacle 정의3×3·Sprite·충돌·발생/제거 동작은 바꾸지 않았다. 현재 새 시안은4×3칸 그림이므로 승인 후 본편 연결 시 그림 비율과 충돌/길 차단 범위를 따로 확인해야 한다. 이번 시안의 제거 기능/사람 플레이/온라인은 미검사다.
