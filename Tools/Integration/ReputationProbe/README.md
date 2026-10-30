# R1 평판 데이터·저장 인수 계약

게임의 `ReputationState`·`SaveSystem`·`SaveCompatibility` 원본을 연결한 .NET 10 진단이다. Unity·주변 게임 타입만 대체한다.

```sh
dotnet run --project Tools/Integration/ReputationProbe/ReputationProbe.csproj --verbosity quiet
```

저장소 루트에서 실행한다. 출력은 통과 항목 JSON이며, 실제 플레이어 저장 경로/Unity 직렬화 호출은 대체 객체가 차단한다. JSON 왕복은 System.Text.Json의 DTO 검사다. 실제 Unity의 데이터·JsonUtility·임시 파일 검사는 `Pocket Caravan > Tests > Reputation data and save` 또는 `PocketCaravan.EditorTools.ReputationChecks.RunBatch`로 별도 실행한다. 둘 다 실제 플레이 검사는 아니다.

## 구현된 범위

- `PlayerState.reputation`이 캐릭터별 상태를 소유한다.
- `(ReputationScope, targetId)`를 키로 써서 스타팅 문명·중앙 마을을 분리한다. 같은 문자열 ID도 종류가 다르면 충돌하지 않는다. 마을을 어느 단위로 해석할지는 R2의 거래/의뢰 연결부에서 명시해야 하며 현재 씬·지도 담당 영역으로 자동 추정하지 않는다.
- 출신 문명 우호, 그 밖의 미기록 문명/중앙 마을은 중립. Get은 조회만 하며 새 기록/보상을 만들지 않는다.
- 값은 `standing`(6단계)과 `progress`(그 단계 안의 0 이상 진행점)로 저장한다. enum의 0~5는 직렬화 식별값이며 평판 점수 경계가 아니다. D10의 단계 길이·증감량·가격·보상은 정하지 않았다. 이름과 진행점을 저장하므로 초기 우호를 위한 임시 숫자 기준도 필요 없다.
- Set은 데이터 대입 API다. 거래/선행/의뢰에서의 획득과 점수→단계 이동 계산은 R2/R6에서 승인된 규칙으로 연결한다. 코드에 설정된 일괄 평판 획득이나 자동 단계 상승은 없다.
- 입력·출력 항목은 복제하고, 전체 입력 검증 후 상태를 바꾼다. 손상된 평판을 로드하면 진행·재산을 바꾸기 전에 기존 저장 차단 경로로 거절한다. 아직 등록되지 않은 유효 ID는 버리지 않는다.

## 저장 버전과 옛 저장

- 실제 `GameManager.SaveNow`는 `ReputationState.ExportTo`로 version 3을 기록한다. 이전 실행 파일은 이 버전을 거절하므로 새 평판을 모르고 덮어쓰지 않는다.
- SaveData의 기본 version 2는 버전 필드가 없던 구 JSON의 기존 역직렬화 동작을 보존하기 위한 것이다. 현재 저장 생성은 반드시 ExportTo를 거친다.
- v1/v2 저장에는 선택된 출신도 평판도 없었다. 문서의 '선택 화면이 없을 때 현재 설정된 출신 사용'에 따라 현재 `Db.startOrigin`에서 출신 문명을 한 번 초기화한다. 기존 신용·퀘스트로 과거 평판을 추정하지 않는다. 현행 단일 출신 설정을 사용하는 이주이며, 기록에 없는 과거 다른 출신을 복원한다는 뜻은 아니다.
- `OriginData.homeRegion.id`를 우선 사용한다. 아직 지역 에셋이 연결되지 않은 출신은 기존 origin.id 그대로 보존한다. 다른 팀의 ID를 새로 정하거나 Viking→North 같은 임의 이주표를 만들지 않는다.
- v3에는 초기화에 사용한 `homeCivilizationId`와 평판 항목을 저장한다. 이후 설정 출신이 달라져도 저장된 평판을 재초기화하거나 고향 우호를 다시 주지 않는다. 플레이어의 능력용 출신 선택/저장은 별도 후속 작업이다.
- Unity JsonUtility는 필드가 없던 inline 객체를 빈 객체로 만들 수 있다. 평판 블록 schemaVersion 기본값 0은 그 부재 표시이고, Export만 1을 쓴다. v1/v2의 빈 0 블록은 허용, 실제 평판을 가진 구 버전 표기는 거절, v3의 누락/0/미지원 블록은 거절한다.
- Initialize는 새 게임/독립 시험장 초기화이고, Import는 저장 복원이다. 시작 보정은 반복 로드 보상이 아니다.
- 기존 돈·신용·상품/퀘스트/NPC/시설 ID·장부·화물·맵 형식은 바꾸지 않는다.

## 아직 하지 않은 것

D10 수치, 가격·품목·시설·흥정 효과, 회복·선행 보상 연결, 평판 UI, 출신 선택 화면, 온라인 저장, 실제 플레이 검증. 다음 Codex 작업 순서는 최신 Plan의 A2b → R6이며, 이 작업에서 시작하지 않았다.
