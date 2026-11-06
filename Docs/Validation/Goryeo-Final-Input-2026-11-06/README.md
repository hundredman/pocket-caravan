# 우물·나룻배·앉기 입력 경로 점검

## 결과와 범위

일반 InputReader/각 MonoBehaviour.Update에 가상 Keyboard 이벤트를 보내 **52확인 통과**했다. 기존 검사처럼 활동 Interact/Begin이나 대화 선택 콜백을 직접 호출하지 않았다. 플레이어 위치·날짜·잔고와 저장 읽기 준비값은 테스트에서 설정했다. 사람이 직접 키보드로 플레이한 결과는 아니다. 이 세 기능 범위에서 새 게임 동작 버그를 확인하지 못해 **본편 게임 코드는 고치지 않았다.**

- 우물: Space로 대화/작업 시작, Esc 중단 시 미소모·메뉴 중복 열림 없음, 재시도→3냥 지급, 완료 Space 닫기/당일 재시도 무지급.
- 이어하기: 기존 혼자 하기 경로를 먼저 선택하고 시작 메뉴에서 Space로 이어하기. 실제 ContinueFromStartScreen→OnSceneLoaded의 저장 적용/위치 복원을 거쳐 잔고103냥/하루 기록 복원, 다시 Space를 눌러도 추가 지급 없음.
- 나룻배: 사공에게 Space→첫 행선지 Space 선택으로 예성 나루↔버들나루 왕복. 편도8냥/1게임시간, 안전한 도착·도착 저장의 목적지/잔고 확인. 왕복 후87냥과 우물 완료 기록 유지.
- 앉기: 북쪽 쉼터 평상·마루·모닥불3자리에서 Space로 앉기/일어나기·재착석/W로 일어나기. UI 잠금/추가 상호작용/돈 변경 없음.

실제480×270 캡처2장(우물 완료/이어하기 메뉴)은 ArtReview/Goryeo-Final-Input-20261106에 있다. 새 그림이나 배치를 제안하는 작업은 아니다.

## 검사 도구 보완 이력

실패 보고서도 보존한다. 아래는 게임 동작 오류로 처리하지 않았다.

1. 초기에는 시작 화면 앞 NetLogin 선행을 생략해 로그인 화면에서 멈췄다. 이후 테스트 시작 시 기존 PlayAlone 경로를 선택했다. 자동 로그인 설정/사용자 자격증명/공용 코드는 수정하지 않았다. 최초 로그인 화면 캡처는 검토 자료에서 제외했다. 온라인 플레이 검증은 하지 않았다.
2. 에디터 전용 LoadSceneAsyncInPlayMode로 열면 등록된 Naru도 buildIndex=-1이었다(editor-load-probe.txt). 실제 이어하기는 이 경우 의도대로 개발 시험 씬으로 판단해 저장을 적용하지 않는다. 테스트 로드를 실제 SceneManager.LoadSceneAsync(등록 이름)로 고쳤고 빌드 번호도 확인했다. Continue가 읽을 SaveData는 테스트가 공개 Read 후 비공개 saveToLoad 준비값에 넣었다. 실제 앱 재시작/Awake 전체 경로 검사는 아니다.
3. 왕복 직후 같은 프레임에 우물을 조회해 null이던 테스트를, 전환 잠금 해제 후 Director Update가 두 프레임 실행되도록 보완했다. 게임에서는 다음 Update에 정상 설치된다. 본편 Director는 변경하지 않았다.

## 공통 검사와 미확인 사항

- Python400개 두환경 OK(23/32skip). 기존 ResourceWarning/DeprecationWarning은 로그에 보존했다.
- 월드0오류1437출구, Unity8종(World/SceneTerrain54씬/GoryeoRegion/TransitRegion/Q1/Experience/TradeBalance/SaveCompatibility)·콘솔error0.
- 시험 전 사용자저장9파일 백업, 종료 후9해시 불변·일회용 슬롯/.bak/.tmp0. 가상 키보드는 매 시험 종료에 제거하고 원래 Keyboard.current를 복원했다.
- 사람 직접 입력·앱/프로세스 재시작·실제 디스크 고장·온라인·장시간/시연PC FPS·빌드 미검사. 화물/차량은 기본 fixture이며 이번에 적재 수레를 별도로 실험하지 않았다.
- 전체 최종 점검 완료가 아니다. 퀘스트 전 과정과 실제 거래/흥정·속죄 납품의 사람 플레이는 별도다. 그 검사를 대신했다고 주장하지 않는다.

수확 돕기는 최종 버그 수정 뒤 검토할 확장 후보로만 기록했다. 새 논/소·수확 그림/보상/공용 파일은 구현하지 않았다. 기존 Packages/ProjectSettings/3D/발표 작업은 보존하고 커밋에서 제외한다. 최신 main을 합친 로컬 커밋만 하며 push/PR/빌드는 하지 않는다.
