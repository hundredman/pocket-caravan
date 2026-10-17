# 팀원이 프로토타입 확인하기

> 10월17일 기획은 주행 낙하 제외·정보/적재/판매/이동 특화로 갱신됐다. 아래 기존 실행 파일과 현재 코드는 아직 이전 운반 규칙이다. 최신 기획을 적용한 빌드가 배포됐다는 뜻은 아니다.

2026-10-15. 게임 코드 기준 `3ad1d5c`의 고려 프로토타입이다. 실행 파일은 아래의 10월15일 버전이며, 최신 소스는 PR #1을 반영한 `main` 브랜치에서 확인한다. 저장소는 비공개이므로 접근 권한을 받은 GitHub 계정으로 로그인한다.

## 실행 파일로 확인

[팀 검토용 Windows·Mac 릴리스](https://github.com/hundredman/pocket-caravan/releases/tag/goryeo-prototype-2026-10-15)의 Assets에서 자기 운영체제에 맞는 ZIP을 받는다.

| 파일 | 대상 | 실행 |
|---|---|---|
| `PocketCaravan-Windows-x64-2026-10-15.zip` | Windows x64 | 전체 압축 해제 후 `PocketCaravan.exe`. 데이터/DLL/런타임 폴더도 함께 유지 |
| `PocketCaravan-macOS-universal-2026-10-15.zip` | Intel / Apple Silicon Mac | 압축 해제 후 `PocketCaravan.app` |
| `SHA256SUMS.txt` | 두 ZIP의 SHA-256 | 받은 파일의 무결성 대조용 |

Mac 빌드는 Developer ID 서명·Apple 공증이 없는 팀 검토용이다. 우리 팀이 보낸 파일임을 확인한 경우 실행 차단창에서 ‘완료’를 누르고, 시스템 설정 → 개인정보 보호 및 보안 → ‘그래도 열기’ → 인증 후 ‘열기’를 선택할 수 있다. [Apple 공식 안내](https://support.apple.com/ko-kr/102445). GitHub 다운로드도 이 확인 절차를 대신하지 않는다.

두 빌드는 본편과 별도의 `pocket_caravan_preview_save.json`을 사용한다. 이전 프리뷰 진행은 이어질 수 있고 Esc 메뉴에서 처음부터 시작할 수 있다. 빌드 성공·압축 검사와 다른 사람의 컴퓨터에서 실제 실행 성공은 구분한다. Windows 실제 실행과 다른 Mac에서의 실행은 팀 확인 대기다. 온라인 기능은 아직 없다.

## Unity에서 확인·수정

1. 저장소를 받거나 fetch하고 `main` 브랜치의 최신 내용을 확인한다. 자신의 미커밋 작업이 있다면 전환 전에 보존한다.
2. Unity Hub에서 **6000.3.26f1 (Unity 6.3 LTS)** 로 프로젝트를 연다.
3. 전체 교역은 `Assets/PocketCaravan/Scenes/Goryeo/Naru.unity`를 열어 Play한다. 기존 본편 저장을 이어 갈 수 있다.
4. 최신 개경 배치만 보려면 `Assets/PocketCaravan/Scenes/Goryeo/Gaegyeong.unity`를 열어 Play한다. 비시작 씬 직접 실행은 기존 저장 불러오기/자동 저장을 끈다.

기존 씬·데이터가 저장소에 있으므로 단순 Play를 위해 생성 메뉴를 다시 실행할 필요는 없다. 맵 생성과 수작업 보존의 현재 제약은 [지역 제작안](RegionAuthoringPlan.md), 이번 변경 원본은 [프로토타입 기록](GoryeoPrototypeReview.md)을 참고한다. 지역 배치의 원본은 Python이며 생성 씬만 편집하면 재생성 때 유실될 수 있다.

## 짧은 검토 동선

WASD/방향키 이동, Shift 달리기, Space 상호작용, Q 퀘스트, M 지도, L 장부, Esc 메뉴.

예성 나루의 장터에서 물건/가격 확인 → 예성 길의 숲길 또는 큰길 → 개경 남문·시전·동문 짐 마당 → 닥나무 마을 → 귀환 화물 선택. 처음에는 자유롭게 플레이하고, 다른 회차에서 도보/수레·화물량을 바꾸어 비교한다. 동문 작업자는 생활 연출이며 실제 생산 경제가 아니다.

피드백에는 받은 ZIP 이름 또는 소스 커밋, 운영체제/장비, 방문 맵, 수행 행동, 기대한 결과와 실제 결과, 화면/로그를 남긴다. 재미 검토는 첫 장사 이해, 길 선택 이유, 반복 이동 피로, 수도 도착감, 화물/손익 가독성으로 나누어 기록한다. 인증정보·개인정보는 로그에서 제외한다. 자세한 팀 검토 항목은 [고려 프로토타입 확인](GoryeoPrototypeReview.md)에 있다.

기존 `goryeo-preview-2026-10-15`는 과거 Mac 프리뷰다. 이번 릴리스는 개경 개선·현재 교역/장부·최근 로컬 오류 수정이 포함된 새 버전이다. 최종 미술/지역 완성본과 온라인 MVP 완성을 뜻하지 않는다.
