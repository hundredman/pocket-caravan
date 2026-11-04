# Task5 새 장터 전용 좌판4종 — 승인·본편 연결 완료

포구: 생선 덕장 fish_rack48×32, 소금 천막 좌판 salt_stall48×48. 내륙 교역 길목: 피륙·양모 천막 cloth_stall48×48, 향신료 자루 판매대 spice_stall48×32. 내장 image_gen 사용, 전체 프롬프트/참고는 prompts.json, 생성 원본 Raw, 픽셀 정리한 전체 PNG/Aseprite는 Native에 있다. Tools/ArtTrials/specialty_market_native.lua로 승인된 장터 색과 같은 도트 격자에서 전체 개체를 정리했다. 기존 그림의 일부를 잘라 쓰거나 늘이지 않았다.

원래 장터의 규모·자리·각 물체의 캔버스/통행 범위는 유지했다. 각 마을의 시장과 마당마다 기존 곡물 좌판 하나를 소금/피륙으로 바꾸고, 기존 잡화 판매대 일부를 덕장/향신료로 바꾼다. 두 마을 각각 전용 천막2곳·낮은 전용 판매대3곳, 합계10개 시험이다. 기존 곡물 좌판/잡화 판매대도 남겨 일반 물건이 함께 거래되는 구성을 유지했다.

사용자 “그래.” 승인 후 ArtReview/MarketCourts-E2-20261103/layout.json 원본에 연결하고 market_production으로 본편에 반영했다. 승인 전 배치는 before-layout.json, 승인 픽셀/배치는 approval.json에 보존했다. 두 transit 씬만 다시 만들었고 DB/공용 기능은 유지했다. ignored B2Preview는 과거 비교 재현용이다. 큰 장 순환/보너스에는 새 마을을 추가하지 않았다.

native-comparison.png: 위 기존 크기 기준/아래 새 그림5배 확대 및1배. transit_*comparison.png: 위 장터/아래 마당, 왼쪽 기존/오른쪽 새 좌판. 실제 Unity960×540 캡처8장을 변형 없이 묶었다.

검사: 네이티브 크기·이진 알파·Aseprite 재내보내기 RGBA 동일, 모든 위치/통행 범위 원본 일치. 실제 preview Play281확인(주변250회 통행 비교 포함), 저장9파일 백업/불변. 최초 촬영 도구의 카메라 z좌표가 보행 칸에 섞인 오류를 수정 후 재촬영했다. 본편 지형/배치 오류는 아니다. 정적358개 두 환경 실패0(23/32skip), 월드0오류/1437출구, Unity 지정8종은 Docs/Validation/SpecialtyMarkets-Task5-2026-11-04 참조. 사람/온라인 검사는 하지 않았다.

본편 검사: Docs/Validation/SpecialtyMarkets-Production-Task5-2026-11-04. 실제 본편 Play29확인/4캡처·저장9파일 불변·정적358 두 환경/Unity8종 통과.
