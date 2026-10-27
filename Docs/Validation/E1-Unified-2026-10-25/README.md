# E1 거리 서비스 통일 완료

Decisions 8절 기준. 본편은 `current`이며 `layoutD` 전환은 B5다. 종전 쌍별 ±10%는 철회됐다. 이전 검토 자료는 당시 수치와 판단 이력으로 보존했다.

- `TownDistance.Between` 한 진입점에 세 사용처를 연결했다. `current`는 상품·기한용 기존 맨해튼 거리와 지역 배달 보수용 기존 BFS를 용도별로 보존한다. 이 호환 구분 없이 기존 결과를 모두 유지할 수 없으므로, `layoutD`에서만 용도와 무관하게 같은 환산 정수를 반환한다. 같은 마을은 기존처럼 0, 서로 다른 마을은 최소 1이다.
- B1 원본 → `Tools/Routing/export_routes.py` → `Resources/TownRoutes.json`. 9마을/12길, Decisions 5절의 ID 대응과 비활성 외부 자리 3개. 마을 ID·가격·보수 공식·현재 맵·저장 형식은 바꾸지 않았다. 새 TownData 2개는 아직 등록하지 않았다.
- 예상 보행 초에 최소제곱 k=0.105348794943을 곱하고 가장 가까운 정수로 반올림한다(Unity ties-to-even). 미등록 마을·끊긴 새 경로는 조용히 기존 거리로 대체하지 않는다.

## 검증

- 본편 C#을 연결한 .NET 검사: 실제 이전 ItemData/ContractManager/DeliveryReward 코드와 새 코드를 각각 컴파일했다. 기준가 77행·가격 oracle 1,680행·배달 견적 6,468행·고정 난수 의뢰 2,100개의 직렬화 결과가 바이트 단위로 일치한다. `regression.json`에 해시/범위를 기록했다. 입력 에셋 44개의 해시도 이전 Unity snapshot과 일치한다. 전체 재생성 결과는 `/tmp/pocket-e1-legacy.json`, `/tmp/pocket-e1-current.json`이며 커밋에는 요약을 넣었다.
- .NET 추가 3,613개 검사: layoutD 모든 81개 방향 포함 조합, 대칭·최소 거리·세 래퍼 일치, 기수락 의뢰의 보수·기한 보존. .NET 직렬화는 Unity 저장 검사를 대신하지 않는다.
- **Unity 6000.3.26f1 Edit mode 4,582개 통과**: 현재 가격 oracle/거리, layoutD 세 사용처, Unity JSON 왕복, 진행 중 의뢰를 current/layoutD 게시판 초기화 후에도 보존. Q1 25개·R6 87개·저장 호환 27개 통과. 각 JSON에 기록했다. 플레이·정상 보행·수동 완주·게임 빌드는 미실행이다.
- 전체 Python 136개 통과. 등록 출구 92개 오류 0. layoutD 81개 C# 결과를 독립 Python 경로 계산과 대조했다.
- 첫 Unity 실행은 첫 가져오기 중 Burst 내부 네이티브 종료(138)로 검사 미도달. 로그는 `/tmp/pocket-e1-unity-first-crash.log`. 두 번째는 `--burst-disable-compilation -job-worker-count 2` 실행 옵션만 사용해 종료 0으로 완료했다. 프로젝트 설정/패키지 버전을 변경하지 않았다. 기존 로컬 MCP 패키지를 작업 폴더로 복사해 잠금 파일을 보존했다(ignored).

## layoutD 계산 표

보수는 비교 가능한 **무게 4·일반 의뢰·난수 배율 1·고려 보수 규칙** 예시다. 모든 상품의 새 거리/기준가 77행 및 모든 마을 조합 81행은 `current.layoutD.json`에 있다. 실제 B5 수치 확정이나 새 맵 보행 검사를 뜻하지 않는다.

| 구간 | current 거리 | current 보수 거리(BFS) | current 기한 | current 보수 | D 보행 초 | D 거리 | D 기한 | D 보수 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 나루–장단 | 4 | 1 | 44 | 4 | 10.055 | 1 | 17 | 4 |
| 나루–송악 | 1 | 1 | 17 | 4 | 26.965 | 3 | 35 | 6 |
| 나루–닥목 | 3 | 2 | 35 | 5 | 28.661 | 3 | 35 | 6 |
| 나루–개경 | 2 | 1 | 26 | 4 | 14.514 | 2 | 26 | 5 |
| 장단–송악 | 5 | 2 | 53 | 5 | 16.910 | 2 | 26 | 5 |
| 장단–닥목 | 1 | 1 | 17 | 4 | 31.015 | 3 | 35 | 6 |
| 장단–개경 | 2 | 1 | 26 | 4 | 24.570 | 3 | 35 | 6 |
| 송악–닥목 | 4 | 2 | 44 | 5 | 14.105 | 1 | 17 | 4 |
| 송악–개경 | 3 | 1 | 35 | 4 | 21.619 | 2 | 26 | 5 |
| 닥목–개경 | 1 | 1 | 17 | 4 | 14.147 | 1 | 17 | 4 |

B2 실제 길로 재계산해야 한다. B5 전환 때 평균 기한·평균 보수 ±15%, 실제 보행의 기한 70% 이내 도착, Q1② 이익, 옛 진행 중 의뢰 보존을 확인해야 한다. 이 후속 기준은 이번 E1의 완료 판정과 구분한다.

## 재현

```sh
python Tools/Routing/export_routes.py
dotnet run --project Tools/Routing/DistanceProbe -p:Legacy=true -- . /tmp/e1-legacy.json
dotnet run --project Tools/Routing/DistanceProbe -- . /tmp/e1-current.json
cmp /tmp/e1-legacy.json /tmp/e1-current.json
python -m unittest discover -s Tools/Integration -p 'test_*.py'
python Tools/Integration/validate_world.py
```

Unity 별도 작업 폴더의 Edit mode에서 `PocketCaravan.EditorTools.TownDistanceChecks.RunBatch`. 실제 플레이 저장을 쓰거나 씬을 다시 만들지 않는다.
