# 퀘스트 저장 호환 진단

.NET 10 SDK에서 실행:

```sh
dotnet run --project Tools/Integration/QuestSaveProbe/QuestSaveProbe.csproj --verbosity quiet
```

프로젝트의 QuestManager, QuestData, GameEvents, SaveCompatibility, SaveSystem 소스를 수정하지 않고 직접 연결한다. Unity와 주변 게임 객체는 Stubs.cs로 대체한다. 실제 저장 파일·씬·에셋을 열거나 쓰지 않는다.

이 도구는 현재 동작을 재현하는 진단이다. `observed: true`는 **문제가 해결됐다는 뜻이 아니다.** 정의 변경 시 목표/진행량 오적용과 마지막 단계에서 멈추는 현재 결함도 true로 보고한다. 이주 기능을 구현하면 해당 관찰과 기대 동작을 갱신해야 한다.

Unity JsonUtility·에디터·실제 게임 실행은 검사하지 않는다. 파일 직렬화 함수와 실제 저장 경로는 실수로 호출하면 예외를 내도록 막았다. 같은 퀘스트 정의의 복원, 수락 보상 보존, 완료 기록, 목표 순서 변경, 진행량 의미 변경, 단계 수 축소, 구 보상 필드, 현재 맵 방문 판정을 확인한다.

현재 저장소 퀘스트 에셋 12개의 목표 배치도 읽어, 유효한 활성 단계 21개의 타입·대상·필요량과 수락 보상이 Export/Import에서 유지되는지 확인한다. 에셋 YAML 전체를 Unity처럼 역직렬화하거나 아이템/차량 GUID를 해석하는 검사는 아니다.
