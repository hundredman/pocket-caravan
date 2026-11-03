# Claude 전달 메모 — 그림 승인 후 연결

현재는 사용자 승인 대기이며 본편 반영 허가로 해석하지 않는다. 아래 파일은 모두 이 폴더 기준이다.

| 역할 | 파일 | 규격/연결 |
|---|---|---|
| fallen_log | Native/fallen_log.png / .aseprite | 전체48×48/3×3칸. 원래3×3요청 유지, 제거 후 별도그림 없음 |
| river_flood 넘침 | Native/river_flood_high.png / .aseprite | 전체80×48/5×3칸. 오른쪽16px는 기존강과 같은물색/동쪽접속. 방향이 다르면 새 방향그림 요청 |
| river_flood 빠짐 | Native/river_flood_low.png / .aseprite | 같은캔버스/같은동쪽접속, 왼쪽길이 드러남. 넘침마스크의 부분집합 |
| transit_spice | Native/transit_spice.png / .aseprite | 기존상품과 같은16×16. 주황/적갈향신료가보이는천꾸러미 |
| transit_wool | Native/transit_wool.png / .aseprite |16×16, 끈으로묶은크림색양모 |

그림을 줄이거나 돌리거나 거울상으로 바꾸지 말고 PPU16/Point/압축없음으로 연결한다. PNG/Aseprite SHA는 native-checks.json, 원화/정리도구와 승인상태는 README/manifest 참조.

E3의 발생 일정/위치/충돌/치우기/통행 회복은 이번에 구현하지 않았다. 기존 ObstacleDefinition.visuals는 환경별 그림 목록이지 넘침/빠짐 애니메이션 계약이 아니다. high/low는 명시적으로 상태와 연결해야 한다. 실제 길/강 접속과 장애 범위를 배치 후 확인한다. 배치/게임 로직은 Claude 담당이다.

## 포구 수정 원본

Tools/ArtTrials/e3_e2_preview.py의 coast_context(map)는 남동포구에서 각 행의 마지막기존타일이 물이고 그오른쪽이 비었으며 막힌칸일 때만 pond_center를 넣는다. 검토결과1128칸. 이동가능/물마스크/길/출입구/기능물/해안선은 유지한다. 남동포구의 바다쪽 빈모서리만 다뤘고 다른담당육지경계를 임의로채우지 않았다. 승인이면 최신Claude생성원본에 반영한뒤 내보낸다. 검토JSON만 본편으로 덮어쓰지 않는다.

북동포구의 anim20개가 원본 ripples를 참조해 불투명한청색16×16바탕이 보였다. 기존 goryeo_ripples로 바꾸면 고려물색과 어울리는 반투명프레임을 쓴다. 프레임PNG/알파90은 그대로이며 Decisions10/C6예외를 바꾸지 않았다. 검토에서 objects의 sheet20개만 바꿨다. 승인이면 최신배치원본에서 고려/중앙동쪽 해당참조를 수정하고 다시생성한다. 공용/다른문명 ripples 원본은 고치지 않는다.

검토씬 재현: python Tools/ArtTrials/e3_e2_preview.py → Unity의 PocketCaravan.EditorTools.E3ArtReview.Build(). 생성은 Editor/B2Preview/E3Art 안에만 쓴다. 본편에 새 상품/장애물·시장ID를 임의등록하지 않았다.
