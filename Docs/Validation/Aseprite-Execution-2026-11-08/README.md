# Aseprite 작업 중 오류 창 방지

2026-11-08 Codex. 사용자 요청: 작업할 때 Aseprite 오류 메시지가 뜨는 이유를 해결한다.

## 원인과 수정

이 Mac Aseprite1.3.18.6 충돌 기록의 주 스레드는 `AppOSX::init → NSApplication → _RegisterApplication → abort`이며 SIGABRT로 끝난다. 앞선 동일 원본 검사는 macOS Codex seatbelt 안에서27건 중단됐고 정상 실행에서는 통과했다. `-b`도 AppKit을 초기화하므로 배치 모드만 지정해서는 이 문제를 피하지 못한다. 원본 파일을 읽기 전 실행 환경 문제로 판단했다. 근거는 `crash-evidence.json`, 이전 `Region-Consistency-Fix-2026-11-08/static-sandbox-failure.txt`/`static.txt`다.

`Tools/Aseprite/cli.py`가 알려진 macOS/seatbelt 조건을 앱 실행 전에 검사한다. 검사27개·제작15개·MCP연결1개(총43파일)를 연결했다. 기존 실행 인자/PNG RGBA 비교/실패 조건은 유지했다. `Tools/Integration/run_checks.py`는 전체 검사 전에 한 번 확인하고, 제한 환경이면 안내와 종료코드2를 반환한다. 권한 상승은 에이전트의 승인된 `require_escalated` 경로에서 같은 명령을 실행하며, Python 도구가 스스로 우회하거나 환경변수를 지우지 않는다.

Aseprite 앱/설치/OS 보안/충돌 알림/Claude·Codex 설정은 수정하지 않았다. 새 Python 제작 도구도 공통 실행 함수를 사용하도록 AgentGuide·도구 README에 기록했다. 과거 다른 원인의 모든 Aseprite 오류를 해결했다고 주장하는 것은 아니다.

## 실행한 검사

- `preflight.txt`: 실제 제한 환경에서 전체 runner는 앱 실행 없이 종료2. 성공/skip이 아니다.
- `legacy-denied.txt`: 기존 개별 unittest 실행도 Python AsepriteEnvironmentError로 멈추고 앱은 시작하지 않는다. 총8검사 중1환경오류/종료1을 예상대로 확인했다. 실패를 숨기지 않았다.
- 신규 guard 회귀5개: 제한에서 자식 실행0, 정상 인자/옵션 그대로 전달, 다른 플랫폼 차단 안 함, 실제 내보내기 실패 유지, 전체 runner의 발견/실행 전 중단.
- `static.txt`: 정상 실행 환경에서 전체431검사·실패0/23skip. 실제 Aseprite 재내보내기·RGBA 비교를 실행했다. 기존 skip은 그대로이며 이번 오류 회피를 위해 native 검사를 skip하지 않았다.
- `mcp.json`: 별도/tmp기술작업폴더에서 execute_lua/inspect_export·16×16 PNG·작업폴더 밖 접근 거부 검사 통과. 게임 아트는 생성/교체하지 않았다.
- `crash-evidence.json`: 검사 전21개/후21개, 새 Aseprite 충돌 보고서0. 보고서 자체는 삭제하지 않았다.
- `git diff --check` 통과. 게임 C#/맵/씬/에셋/저장 변경0이어서 Unity/플레이/빌드는 이번에 재검사하지 않았다. 최신main포함·로컬커밋만.

사용법: 저장소 루트에서 `python Tools/Integration/run_checks.py`. Codex의 제한 환경에서는 해당 명령을 `exec_command`의 `require_escalated`로 실행한다. 직접 CLI/추가 도구는 [도구 안내](../../../Tools/Aseprite/README.md)를 따른다.
