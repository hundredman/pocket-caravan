# A6 MCP 코드 검토 (2026-10-24, 설치 전)

## 후보와 선택

| 후보 | 검토한 고정 커밋 | 판단 |
|---|---|---|
| [mattt/aseprite-mcp](https://github.com/mattt/aseprite-mcp) | `0cd6aac4420a603ec10d19c9fe49a1bb5f6bff8b` | 선택. `execute_lua`, `inspect_export` 두 도구와 작은 실행 경로. stdio 전용, 명시적 실행 파일, 호출 시간/출력/이미지 크기 제한. Apache-2.0 |
| [Vollkorn-Games/aseprite-mcp](https://github.com/Vollkorn-Games/aseprite-mcp) | `28900e757d7e5c18515ad0a95558acde1865aad2` | 미설치. 여러 전용 조작·Lua 핸들러와 선택 HTTP 경로로 검토 표면이 크다. `executeOperation`에는 시간 제한이 없고 `executeAsepriteArgs`의 제한은 선택적이다. `prepare`가 빌드 스크립트를 실행한다 |

선택 후보의 main/config/discovery/runner/tools/workspace와 go.mod/go.sum, 대안의 package.json·실행기·HTTP·빌드 코드를 읽고 위험 호출을 검색했다. 의존성 전 코드·바이너리의 완전한 보안 감사는 아니다. 대안은 실행/설치하지 않았다.

## 외부 전송·명령·파일 접근

- 선택 후보 자체의 실제 서버 경로에는 HTTP 서버, 원격 요청, 텔레메트리 코드가 없다. stdio로 클라이언트에 결과를 돌려준다. `inspect_export`의 PNG와 실행 로그는 MCP 클라이언트 및 모델 서비스에 전달될 수 있다. 비공개·개인 파일을 입력으로 쓰지 않는다.
- `exec.CommandContext`로 지정한 Aseprite만 실행하며 셸 문자열을 만들지 않는다. 버전 확인도 같은 실행 파일이다. 임시 Lua 파일은 작업 폴더에 만들고 종료 때 삭제한다.
- **Lua 실행은 컴퓨터 권한으로 실행되는 코드다.** 작업 폴더 검사는 `inspect_export`의 상대 경로·심볼릭 링크 이탈을 막지만 Lua의 `io`, `os.execute`나 Aseprite의 네트워크 API를 차단하지 않는다. Lua는 작업 폴더 밖 파일 접근·명령 실행·외부 전송도 가능하다. 따라서 작업 폴더를 OS 격리라고 부르지 않는다. 시간 초과 시 직접 자식 Aseprite만 종료하므로 Lua가 띄운 후손 프로세스까지 종료한다는 보장도 없다.
- 이미지 경로 검사 후 읽기 사이에 파일을 바꾸는 경쟁 조건까지 막지는 않는다. 협력하는 로컬 작업 폴더에만 쓴다. 악성 로컬 사용자가 쓰는 공유 폴더에는 두지 않는다.
- Go SDK와 전이 의존성은 go.sum에 고정되나 공급망 위험은 남는다. 설치 때 Go 도구와 모듈을 외부에서 내려받는다. 공식 Go 배포의 SHA-256을 확인하고, 검토 커밋을 저장소 밖에 복사해 빌드한다. 실행 때 자동 업데이트/패키지 다운로드를 하지 않는다.

## 적용 범위

설치 위치는 `~/.local/share/pocket-caravan/aseprite-mcp/<커밋>/`, 작업 폴더는 `~/.local/share/pocket-caravan/aseprite-workspace/`로 고정한다. 실제 게임 에셋/저장/맵을 자동 수정하지 않는다. Codex 등록은 사용자 지시에 포함된다. Claude 데스크톱 설정 변경은 사용자 승인 후에만 한다.

MCP 실행 결과는 기술 검증용이며 아트 승인과 다르다. 실제 아트는 사용자가 그림을 검토한 뒤 반영한다. 재설치/업데이트는 새 커밋을 다시 검토한다.
