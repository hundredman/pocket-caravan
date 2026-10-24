# 문서 안내

정리: 2026-10-24. 에이전트와 팀원은 저장소 루트의 [AGENTS.md](../AGENTS.md)부터 읽는다.

## 지금 기준 문서

| 문서 | 내용 |
|---|---|
| [Direction.md](Direction.md) | 확정 방향: 아트 스타일, 에셋 사용 규칙, 세계 면적과 담당, 고려 구조. **가장 우선** |
| [Plan.md](Plan.md) | 작업 목록, 상태, 결정 대기 항목 |
| [ArtGuide.md](ArtGuide.md) | 아트 제작 기준, 조립용 에셋, 현재 에셋 진단 |
| [WorldLayout.md](WorldLayout.md) | 면적·좌표 약속, 고려 배치 검토안 |
| [ArtBrief.md](ArtBrief.md) | 고려 아트 개선 요청서: 사람 NPC, 건물·나무 실사감 줄이기, 깨진 연못 |
| [QuestMain1.md](QuestMain1.md) | 메인 퀘스트 ① `고려 한 바퀴` 설계: 단계·대사·옛 퀘스트 처리·옛 저장 대응표 |
| [Standing.md](Standing.md) | 돈·신용·평판의 뜻, 평판 오르내림, 흥정, 경매 (초안) |
| [CurrentState.md](CurrentState.md) | 지금 실제로 구현된 것 |
| [WorkLog.md](WorkLog.md) | 세션별 작업 기록 |

## 폴더

| 폴더 | 내용 | 읽는 법 |
|---|---|---|
| `Reference/` | 아직 유효한 시스템 명세(경제, 퀘스트, 시설, 멀티플레이, 출신, 팀 협업, 빌드) | 기준 문서와 충돌하면 기준 문서를 따른다 |
| `Archive/` | 지난 상태표, 검토, 재개 메모, 옛 맵·아트 계획 | 근거로만 참고한다. 지시로 따르지 않는다 |
| `Validation/` | 검사 결과 기록(JSON/CSV) | 당시 시점의 증거다 |
| `Meetings/` | 회의 기록 | |
| `Images/` | 문서용 화면 캡처 | |

## 최근 검토

- [문서·프로젝트 파일 재검토 (2026-10-24)](Validation/Project-Review-2026-10-24.md): 누락·불일치·Claude 크로스체크 항목과 검사 범위. 미결정 사항을 확정한 문서가 아니다.

- [퀘스트 저장 호환 검증](Validation/QuestSaveCompatibility-2026-10-24.md): 코드 분리 실행의 실제 재현 결과와 Q1/B6 이주 조건.

- [WoW 참고 평판 적용 기준](Reference/WoWReputationAdaptation.md): 공식 출처, 유지·간소화할 요소, R1~R4 구현 계약과 수치 검토 경계.
