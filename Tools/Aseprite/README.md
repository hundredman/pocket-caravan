# Aseprite 연결 (A6)

게임 에셋 생성 전 연결을 확인하는 도구다. 실제 아트의 팔레트·모양을 정하지 않는다. 아트는 사용자에게 그림으로 검토받은 뒤 반영한다.

## 이 Mac에서 확인한 환경

- Aseprite 정식 설치: `/Applications/Aseprite.app/Contents/MacOS/aseprite`, `1.3.18.6-arm64`. PATH에는 없어 절대 경로를 사용한다.
- MCP: `mattt/aseprite-mcp` 고정 커밋 `0cd6aac4420a603ec10d19c9fe49a1bb5f6bff8b`.
- 실행 파일: `~/.local/share/pocket-caravan/aseprite-mcp/0cd6aac4420a603ec10d19c9fe49a1bb5f6bff8b/aseprite-mcp`.
- 작업 폴더: `~/.local/share/pocket-caravan/aseprite-workspace/`. 저장소/게임 에셋 폴더를 기본 작업 폴더로 쓰지 않는다.
- 도구: `execute_lua`, `inspect_export`. 한 호출 30초 제한. PNG는 10MiB, 스크립트·각 출력 스트림은 1MiB 제한(서버 기본값).
- Go는 빌드 때만 사용하며 저장소 밖 전용 폴더에 설치했다. 실행 시 Go/패키지 설치 명령을 호출하지 않는다. 바이너리를 갱신할 때는 새 소스 재검토가 필요하다.

## 연결 검사

저장소 루트에서 실행한다. `output/`은 Git에서 제외된다.

```sh
mkdir -p output/a6-cli
/Applications/Aseprite.app/Contents/MacOS/aseprite -b \
  --script-param "output=$PWD/output/a6-cli" --script Tools/Aseprite/smoke.lua
python3 Tools/Aseprite/check_connection.py \
  --binary "$HOME/.local/share/pocket-caravan/aseprite-mcp/0cd6aac4420a603ec10d19c9fe49a1bb5f6bff8b/aseprite-mcp" \
  --workspace "$HOME/.local/share/pocket-caravan/aseprite-workspace" \
  --report output/a6-mcp.json
```

`smoke.lua`는 16×16 기술 시험 PNG와 `.aseprite`를 만든다. 원본 재열기/PNG 재출력도 확인했다. `check_connection.py`는 MCP 초기화, 도구 두 개 나열, 실제 Lua 실행, 이미지 응답, 존재하는 작업 폴더 밖 PNG 거부를 검사한다. 이것은 앱에서 모델이 도구를 선택해 사용하는 검사와 다르다.

## 클라이언트 등록과 다음 세션 확인

- Codex: `~/.codex/config.toml`의 `mcp_servers.aseprite`에 등록. CLI의 설정 목록에서 활성 항목을 확인했다. 등록 이전에 시작한 현재 대화에는 도구가 아직 로드되지 않았다.
- Claude 데스크톱: 사용자에게 먼저 승인을 받고 `~/Library/Application Support/Claude/claude_desktop_config.json`의 `mcpServers.aseprite`에 추가했다. 기존 Unity·Blender 및 기타 설정은 보존했다.
- 실제 등록 내용은 `claude-mcp-entry.example.json` 참고. 해당 파일 전체를 기존 설정에 덮어쓰지 말고 `aseprite` 항목만 병합한다. 이 예시는 이 Mac 전용 절대 경로다.
- 두 설정의 변경 전 백업은 `~/.local/share/pocket-caravan/*config-before-a6-*`에 있다. 개인정보가 있을 수 있으므로 Git에 넣지 않는다.
- **완료 전 남은 확인:** 새 Codex 세션과 Claude 앱 재연결 후 각각 도구 목록에서 두 도구를 확인한다. Claude는 다음 세션에 시험 폴더 안에서 `smoke.lua` 내용으로 실제 조작하고 PNG를 확인한다. 앱 재시작·현재 작업 종료를 강제하지 않았다. 확인 전 A6는 `진행 중`이다.

## 검토와 사용 주의

[MCP_REVIEW.md](MCP_REVIEW.md)에 설치 전 후보 비교·선택 이유·파일/명령/외부 전송 위험을 기록했다. 작업 폴더는 Lua의 OS 권한을 격리하지 않는다. 검토한 Lua만 실행하고 출력 PNG/로그가 모델에 전달됨을 고려한다. 승인받지 않은 아트를 본편에 반영하지 않는다.

참고: [공식 CLI](https://www.aseprite.org/docs/cli/), [공식 Sprite API](https://aseprite.org/api/sprite), [Codex MCP 설정](https://developers.openai.com/codex/mcp/).
