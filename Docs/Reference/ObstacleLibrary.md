# 장애물 라이브러리 / 로컬 시험장

기준: 2026-10-17. 환경 원형40개·스킨13종·날씨 표현8종에 이어 **장애물 프리셋48종**을 제공한다. 종류마다 별도 AI를 만든 것이 아니라 차단·감속·추적·주기 개폐·통행료·수단/적재 제한 8개 동작 모드를 조합한다. 본편10맵 및 다른 팀 문명에 자동 배치하지 않았다.

## 바로 확인

Unity **Pocket Caravan → 맵 제작 → 장애물 걷기 시험장** → Play. 또는 `Assets/PocketCaravan/Scenes/ObstacleLab/ObstaclePreview.unity`를 연다. 두 다리 중 위쪽에 장애물을 놓고 아래쪽으로 우회한다.

- 좌측 ◀/▶ 또는 목록으로48종 선택. 선택하면 위치와 장애 상태를 초기화한다.
- WASD 이동, Shift 달리기, 장애 가까이에서 Space로 대응. 우측 상세에서 조건/진행률 확인.
- 시험 도구를 펼쳐 등짐/수레, 시험짐(땔감2·100냥), 시작 위치, 장애 켜기/끄기를 비교한다. 장애 초기화는 이미 쓴 비용을 환불하지 않는다. 보급은 시험용 짐을 지우고 새로 지급한다.
- 하단 제작 패널에서 환경/날씨 외형만 교체한다. 비 스킨을 골라도 자동으로 범람하지 않는다.
- 제작 시험장은 본편 저장을 불러오거나 덮어쓰지 않는다. 이 빌드는 본편 게임과 별도다.

## 구현한 판단과 대응

통나무/낙석은 치우는 시간과 우회 시간을 비교한다. 끊어진 다리에는 땔감2개가 필요하다. 작업 도중 이동하면 취소되고 완료 전에는 돈/재료를 쓰지 않는다. 통행료도 명시적 대응 시에만 한 번 차감한다. 진흙은 등짐보다 수레를 크게 늦추고 빙판은 제동을 약하게, 여울/강풍은 실제 밀림을 발생시킨다. 목도는 등짐만, 얇은 얼음은 무게 제한을 적용한다.

범람·도개교·가축 횡단 등은 주기적으로 닫히거나 느려진다. 길이 닫힐 때 안에 있던 플레이어는 밖으로 나올 수 있고, 나온 뒤에는 다시 막힌다. 산적/동물은 제한 범위에서 단순 추적하고 가까이 오면 감속시킨다. **새 장애물 접촉으로 돈/화물을 강제 분실하지 않는다.** 기존 본편 Bandit 약탈은 별도 시스템이며 이 작업으로 제거하지 않았다.

## 배치와 수정

1. 환경 라이브러리에서 독립 편집 씬을 복제한다. 원형을 직접 덮어쓰기보다 복제본을 사용한다.
2. **맵 제작 → 장애물 라이브러리**에서 검색/선택 후 Scene 클릭 배치를 켜고 타일에 놓는다. Undo 가능. MapRoot에 ObstacleField가 붙고 배치 ID/좌표가 저장된다.
3. ObstacleField의 placements에서 크기·활성·주기 오프셋을 수정한다. 폭/높이0은 정의 기본값이다. 배치 ID는 같은 맵 안에서 고유해야 한다.
4. **편집한 구역을 JSON으로 내보내기**로 지형과 배치를 함께 저장한다. JSON은 왼쪽 위 원점이다. 실제 타일 충돌층/노면 배열은 바꾸지 않는다. 구역 조합 시 좌표 이동과 배치 ID 접두어를 처리한다.
5. 재생성 원본에 남길 때는 `MapModules/Sources/<id>.json`의 최상위 `obstacles` 배열에 옮긴다. 내보낸 완성 지형 JSON 전체를 source 설계 파일로 대체하면 안 된다. 예: 아래 배치를 추가한다.

```json
"obstacles": [{"id":"goryeo_log_01","definitionId":"fallen_log","x":18,"y":18,"width":0,"height":0,"enabled":true,"phaseOffset":0}]
```

수치 원본은 `Assets/PocketCaravan/Obstacles/definitions.json`. 수정 후 **장애물 데이터 가져오기**를 실행한다. `.asset`에서 수치를 직접 바꾸면 다음 가져오기에서 원본 수치로 돌아간다. 정의의 `visuals` 환경 ID별 Sprite 교체 목록은 가져오기에서 보존한다. 절차 생성 그림은 64×48 임시 아트이며 여러 프리셋이 공유한다. PNG 생성 도구 `Tools/MapGen/Obstacles/generate.py`는 정의 파일이 이미 있으면 사용자 수정을 유지한다.

정의와 배치를 분리하므로 환경 그림만 교체하거나 같은 장애를 여러 맵에서 재사용할 수 있다. 임의 조합에서도 우회 가능한지는 제작자가 확인해야 한다. 시작점/출구를 막지 않고, 필수 재료를 장애 반대편에만 놓지 않으며, 유일한 길을 영구 봉쇄하지 않는다.

## 현재 한계 / 다음 연결 작업

- 해결·주기·추적 상태는 **현재 씬 세션 한정**이다. 저장·재접속·멀티 공유 상태는 아직 없다. 본편의 재산 차감 대응은 기본 잠금이다(`allowEconomyCosts=false`). 시험장은 허용된다. 저장/서버 연결 후 본편 사용을 판단한다.
- 최종 MMORPG에서는 서버가 배치 ID별 활성/해결·작업 점유·비용/재료 거래를 판정하고 클라이언트는 표현한다. 현재 로컬 작업을 온라인 완료로 보지 않는다.
- 산적/늑대는 벽을 확인하는 직접 추적이며 길찾기·전투·약탈 AI가 아니다. 수적은 정박형 강변 견제이며 배 이동/해전은 없다.
- 산불은 주기 차단이며 확산/체력 피해/진화는 없다. 연기는 화면 안개 표현이며 실제 시야 판정은 없다. 얇은 얼음도 추락/화물 유실을 발생시키지 않는다.
- 수치/대응 비용은 비교 초안이다. 장애가 많다는 이유로 항상 기다리게 만들지 않는다. 실제 길에 적용할 때는 우회 시간·처리 시간·운반 수단별 손익을 플레이테스트한다.

## 프리셋 목록

| ID | 이름 | 분류 | 동작 | 대응 |
|---|---|---|---|---|
| `fallen_log` | 쓰러진 통나무 | 잔해/식생 | Barrier | Clear |
| `branch_pile` | 부러진 가지 더미 | 잔해/식생 | Barrier | Clear |
| `boulder` | 길막 바위 | 잔해/식생 | Barrier | Clear |
| `rockslide` | 낙석 더미 | 잔해/식생 | Barrier | Clear |
| `landslide` | 산사태 토사 | 잔해/식생 | Barrier | Clear |
| `avalanche` | 눈사태 잔해 | 잔해/식생 | Barrier | Clear |
| `thorn_brush` | 가시덤불 | 잔해/식생 | Barrier | Clear |
| `fallen_bamboo` | 쓰러진 대나무 | 잔해/식생 | Barrier | Clear |
| `broken_cart` | 고장 난 수레 | 잔해/식생 | Barrier | Clear |
| `abandoned_cargo` | 버려진 짐 더미 | 잔해/식생 | Barrier | Clear |
| `sinkhole` | 꺼진 지반 | 잔해/식생 | Barrier | None |
| `crevasse` | 빙하 균열 | 잔해/식생 | Barrier | None |
| `river_flood` | 강 범람 | 하천/교량 | TimedBarrier | None |
| `flash_flood` | 급류 범람 | 하천/교량 | TimedBarrier | None |
| `high_tide` | 만조 잠김 | 하천/교량 | TimedBarrier | None |
| `broken_bridge` | 끊어진 다리 | 하천/교량 | Barrier | Repair |
| `rotten_planks` | 썩은 교량 판자 | 하천/교량 | LoadGate | Repair |
| `drawbridge` | 닫힌 도개교 | 하천/교량 | TimedBarrier | None |
| `ford_current` | 얕은 여울 물살 | 하천/교량 | Slow | None |
| `washed_road` | 유실된 길 | 하천/교량 | Barrier | Repair |
| `mud` | 진흙탕 길 | 노면 | Slow | None |
| `deep_mud` | 깊은 진창 | 노면 | Slow | None |
| `puddle` | 물 고인 길 | 노면 | Slow | None |
| `loose_sand` | 푹신한 모랫길 | 노면 | Slow | None |
| `quicksand` | 모래 수렁 | 노면 | Slow | None |
| `deep_snow` | 쌓인 눈 | 노면 | Slow | None |
| `ice` | 얼어붙은 노면 | 노면 | Slow | None |
| `rubble` | 자갈·돌부스러기 | 노면 | Slow | None |
| `steep_slope` | 가파른 오르막 | 노면 | Slow | None |
| `narrow_boardwalk` | 좁은 목도 | 노면 | VehicleGate | None |
| `bandit` | 산적 습격 | 위협 | Threat | Pay |
| `water_pirate` | 수적 상륙 위협 | 위협 | Threat | Pay |
| `patrol_ambush` | 매복 순찰대 | 위협 | Threat | Pay |
| `wolf_pack` | 들개·늑대 무리 | 위협 | Threat | None |
| `wild_boar` | 성난 멧돼지 | 위협 | Threat | None |
| `wasp_nest` | 벌집 구간 | 위협 | Slow | Clear |
| `toll_gate` | 통행세 초소 | 사람/교통 | Toll | Pay |
| `checkpoint` | 검문 대기 | 사람/교통 | TimedBarrier | None |
| `livestock_crossing` | 가축 횡단 | 사람/교통 | TimedBarrier | None |
| `caravan_queue` | 대상 행렬 정체 | 사람/교통 | TimedSlow | None |
| `strong_wind` | 강한 맞바람 | 기상/자연 | Slow | None |
| `sand_gust` | 모래 돌풍 | 기상/자연 | TimedSlow | None |
| `forest_fire` | 산불 구간 | 기상/자연 | TimedBarrier | None |
| `smoke` | 짙은 연기 | 기상/자연 | Slow | None |
| `falling_rocks` | 간헐적 낙석 | 기상/자연 | TimedBarrier | None |
| `thin_ice` | 얇은 얼음 다리 | 기상/자연 | LoadGate | None |
| `fishing_net` | 길을 가로막은 그물 | 시설/공사 | Barrier | Clear |
| `roadworks` | 도로 공사 | 시설/공사 | TimedBarrier | None |

## 검증

Unity Play 검사178개(48종 우회 포함): 실제 충돌, 진흙 수레/등짐 배율, 빙판 제동, 물살 이동, 비용/재료 부족·취소·중복 차감, 범람 탈출 유예, 저장 차단. 지형→JSON 배치 보존을 확인했다. Python 통합 검사21개에는 배치 경계/ID/참조 및 조합 좌표 보존이 포함된다. Windows/Mac 빌드는 별도 결과 기록을 따른다. 임의 다중 장애 조합, 실사용 난이도, 동시 접속 성능은 이 검사로 보증하지 않는다.
