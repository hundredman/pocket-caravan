# 최신 AI 인수인계

> 2026-10-21 갱신: 첫 구간(알 아흐사·농원길·자비드, Q1·Q2)은 구현·자동 검증됐다. 최신 상태는 [STATUS.md](STATUS.md)와 [04_첫플레이_구현결과.md](04_첫플레이_구현결과.md)를 따른다. 아래는 이전 기록이다.


갱신:2026-10-21. 클로드로 이동한다.

이전 상태보다 다음 내용이 최신이다.

- 개발 복사본을 실제로 만들었다: `/Users/euninjun/Documents/ChatGPT/게임개발 수업/development/pocket-caravan-arabia/`
- 이 폴더의 `CLAUDE.md`와 `Docs/Arabia/STATUS.md`를 먼저 읽는다.
- Assets/Packages/ProjectSettings/Tools는 원본과 동일하다. 아라비아 씬·새 코드는 아직 없다.
- 원본 게임은 보존했다. 기존 Python 검사26개 통과는 아라비아 Play 검증이 아니다.
- Unity import는 라이선스/OS 접근 문제로 성공을 확인하지 못했다. 일반 Unity Hub 환경에서 다시 확인해야 한다.
- 최신 사용자 지시는 별도 아라비아 씬과 실제 게임 구현이다. 추가 외부 연결을 요구하지 않는다.
- 다음 작업은 설계 문서02에 따라3개 씬에서Q1·Q2를 구현하는 것이다.
- 기존 씬·GUID·공용 DB·저장 파일을 보존하고, 공용 변경과 신규 지역 파일을 구분한다.
- 현재 독립.git/원격기준커밋/commit/push/PR은 없다. 상위 저장소를 잘못 사용하지 않는다.

같은 컴퓨터의 Claude Code에서는 위 개발 복사본을 열면 된다. Claude 웹에서는 문서 ZIP만으로 Unity 프로젝트에 직접 접근하거나 실행할 수 없다. 소스 파일을 별도로 제공해야 한다.

바로 사용할 첫 메시지는 `CLAUDE_시작요청.md`, 상세 상태는 `CLAUDE_현재상태.md`에 있다. 작업 후 루트 CLAUDE.md와 STATUS.md를 갱신한다.
