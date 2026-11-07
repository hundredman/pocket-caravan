# UI 후속 수정 인수인계 — 2026-11-07

## 중단 상태와 시작 위치

사용자가 사용량 때문에 작업 중단·저장을 요청했고, 이어 인수인계 정리를 요청했다. 구현/테스트를 새로 진행하지 않고 현재 코드를 WIP 로컬 커밋으로 보존한다. **후속 수정은 완료·승인·통합 상태가 아니다.**

- 저장소: `/Users/kim/Documents/2026-2/Game Development/팀 프로젝트/PocketCaravan`
- 브랜치: `codex/hud-cart-label-fixes` (메인 체크아웃 사용, 새 worktree 아님).
- 검증된 이전 수정 커밋: `6eb3f5003`.
- 이번 WIP 커밋: 이 문서를 포함한 `Save unfinished UI refinements and handoff` 커밋. `git log -1 --oneline`으로 확인한다.
- 원격 반영 없음. 2026-11-07 이번 인수인계 시 `git fetch origin main` 후 최신 main `f5e39581b`가 HEAD 조상임을 확인했다. 추가 병합할 변경은 없었다.
- 먼저 `Docs/Goryeo/AgentGuide.md` → Direction → Plan → CurrentState → WorkLog 순으로 읽는다.

## 이미 통합된 범위와 이번 작업 구분

PR #27은 사용자 팀 공지 전달 확인 후 GitHub integration에 병합됐다. 병합 커밋 `86e46c840919e50a483367dfeb54cfebca263127`, 검증 원본 `775500b5a824902425da6a59b0229e6ef37429ed`. 날씨·생활 기능·나룻배·앉기·우물·흥정/회복·승인된 공용 UI/지도가 대상이었다. 이번 UI2와 WIP는 여기에 포함되지 않는다.

## 사용자 피드백과 현재 상태

| 항목 | 현재 상태 |
|---|---|
| 초상화 잘림/이동 중 방향 변화 | 6eb3f5003에서 정면 고정·얼굴 표시 확대. 사용자는 대체로 승인했지만 사막여우는 이전 구도가 낫다고 했다. |
| 무인 생활 수레 이동, 수레가 사람 가림 | 6eb3f5003에서 무인 수레 정지·그리기 순서 보완. 사용자 “일단 좋아” 범위에 포함. |
| 게시판이 글씨에 가림·이름표 흔들림 | 6eb3f5003에서 게시판 보호·카메라 독립 배치/픽셀 동기화. 사용자는 이후 이름표 자체의 낮은 가독성을 추가 지적. |
| 사막여우 초상화 | WIP: 정면 고정을 유지하면서 이전 프레이밍만 복원. 새 검사/캡처 없음. |
| NPC·물건·장소 위 이름표 가독성 | WIP: 기존 테마 종이 배경·진한 글자·12px 일반 폰트·고정 정면 그림 기준 머리 위 앵커. 실제 화면 검토 필요. |
| 장터·소지품·의뢰 등 큰 창 동시 열림 | 수정 전 결함 재현 완료. WIP로 창 소유자 기반 잠금/해제·대화 대기열 구현. 수정 후 검증 없음. |
| Space 게시판 보기 안내가 너무 큼 | WIP: 게시판만 작은 `Space 보기` 안내(높이13 논리 px). 동작 유지. 실제 화면 미확인. |
| 채팅하지 않으면 안내문 계속 유지 | **미착수**. 입력 여부와 무관하게 시간 후 사라지도록 해야 한다. Enter 입력 접근은 유지. |
| 이동이 막혔던 제보 | 사용자 재시작 후 정상. 원래 상태 미재현·원인 미확인, 해결 완료 아님. |

이전 제출 `ArtReview/Goryeo-HUD-Cart-Labels-20261107/`은 부분 승인으로 기록했다. 사막여우와 추가 가독성 수정까지 전체 승인된 것으로 취급하지 않는다.

## WIP 코드 범위

- `Core/UIState.cs`: Blocking을 소유자 기반 속성으로 확장. `TryOpen(owner, close)` / `Release(owner)` / `Reset()` 추가. 다른 창의 일반 Release/Blocking=false로 소유 잠금이 풀리지 않게 한다. 기존 지역 전용 코드의 legacy 접근은 남겨 둔다.
- `InventoryUI`, `TownMarket`, `ContractBoard`, `TradeLedgerUI`, `CommerceGuideUI`, `WorldMapUI`, `QuestTracker`, `PauseMenu`: 공용 큰 창의 소유권 획득/닫기 처리로 변경.
- `DialogueUI`: 다른 창이 열려 있으면 대기시킨 뒤 닫힌 후 표시. 다른 씬에서 만들어진 대화는 폐기하는 sceneHandle 검사 포함.
- `GameManager`, `GameEvents`, `StartScreen`: 생명주기 정리에 Reset 사용. `HUD`: 큰 창 중 HUD 버튼 입력 제한. `Inn`: 다른 창이 있으면 Offer 차단.
- `CuteUI`: 사막여우 이전 초상화 구도 복원, 작은 Space 안내 함수.
- `GoryeoPlaceLabels`: 읽기 쉬운 이름표와 게시판 작은 안내.
- `GoryeoHudCartLabelsReview`: 새 이름표/사막여우 기대값 추가, 출력 경로 `ArtReview/Goryeo-Nameplate-Readable-20261107/` (아직 새 실행 안 함).
- `GoryeoModalReview`: 수정 전/후 큰 창 배타성 검토 도구. **ExpectExclusive 기본 false**. 수정 후 실행 시 true를 지정해야 한다.
- `test_goryeo_labels_trial.py`: 승인/후속 게시판 안내 예외만 정규화하고 나머지 공용 안내 코드는 계속 엄격 비교.

모든 지역 전용 창/미니게임까지 배타적으로 바뀌었다고 주장하지 않는다. 특히 legacy UI와 소유자 창 혼용·대화 지연·새 게임/Continue/씬 이동에서 잠금 잔존 및 해제 누락을 확인해야 한다.

## 실제 실행한 검사와 한계

- **6eb3f5003 기준**: Unity 자동407확인/480×270 원본48캡처, Python403개/23skip, 월드1437출구/오류0, 당시 컴파일error0. 성공 체크포인트 이후9저장 불변. 자세한 근거는 `HUD-Cart-Labels-Fixes-2026-11-07.md`와 해당 Validation 폴더.
- **이번 수정 전 재현**: `Docs/Validation/Goryeo-Modal-Exclusive-2026-11-07/before/play.json`: complete=true, 11확인. 소지품+장터/대화 동시 열림과 대화 닫기 후 남은 소지품의 잠금 해제 재현. 공개 메서드와 가상 키보드로 재현한 것이며 사용자의 네이티브 조작 경로 전체를 재현한 것은 아니다.
- **이번 WIP 일부 정적 검사**: `python3 -m unittest Tools.Integration.test_goryeo_labels_trial -v` 2개 통과 기록. 전체 Python·Unity 수정 후 검사 통과로 확대 해석하지 않는다.
- **WIP 수정 후** 컴파일 정상 확정, 화면 캡처, 모달 배타성, 실제 임시 저장 R3/R4 회귀, 전체 Python, Editor 검사 모두 남아 있다. 이전407/403 결과는 WIP 결과가 아니다.
- 편집 중 사용자가 Play를 시작한 시점에 `GoryeoStride.Animate:70` NullReference 콘솔 기록이 있었다. 이후 GM/Map/Player singleton 정상 상태도 읽혔다. 원인 확정/수정 아님. 저장OFF 콜드 시작에서 재현 여부를 별도로 검사한다.
- 온라인 접속·사람의 장시간 플레이·FPS·빌드는 미검사.

## 사용자 저장과 에디터

Unity 도구 실행 이력에서 `SaveNow()` 성공 후 Play 종료 결과를 확인했다(`Current progress saved; stopping for controlled review`). 인수인계 시 `playing=False`, `compiling=False`; 활성 씬은 검토용 `Assets/PocketCaravan/Editor/B2Preview/Social20261104/woodtown.unity`. compiling=False는 오류0 판정이 아니다. 사용자 게임을 자동으로 다시 시작하지 않았다.

- 직전 백업: `/tmp/goryeo-before-readable-modal-tests-20261107/`
- 직전 해시: `/tmp/goryeo-readable-modal-hashes-20261107.json`
- 이 해시는 마지막 정상 SaveNow 전일 수 있으므로 다음 QA에서 마지막 저장 후 기준으로 새 백업/해시를 만든다. 합법적인 사용자 저장 변경을 QA 훼손으로 판정하지 않는다.
- `/tmp` 자료는 영구 자료가 아니다. 코드·기존 검토 증거·이 문서는 Git에 보존한다.

## 다음 작업 순서

1. 작업 트리/Unity 상태 확인. 사용자 저장ON 플레이 중이면 진행을 안전하게 저장·종료한 뒤에만 스크립트 수정/검사를 한다.
2. 채팅 안내 원인 조사: `Scripts/Net/NetChat.cs`의 `welcome`/toldHowTo 안내 삽입, `DrawLines`의 age/LineSeconds/FadeSeconds, `NetLogin.cs`의 접속 안내를 구분한다. DrawLines에는 이미 시간 페이드 계산이 있으므로 원인을 “페이드 없음”으로 단정하지 않는다. 서버에 실제 메시지를 보내며 시험하지 않는다.
3. 컴파일 오류 확인 후 저장OFF 검토 씬에서 모달 수정 후 검사(ExpectExclusive=true), 새 이름표/사막여우 검토 실행. 중복 검토 컴포넌트를 만들지 않는다.
4. 소유자 이외 Release로 잠금이 풀리지 않는지, I/Q/M/Esc/L 전환·대기 대화·새 게임/Continue/씬 이동·구형 창 혼용을 검사한다. 결함을 고치기 위해 경제/저장 검증을 약화하지 않는다.
5. Core 변경이 있어 R3/R4 실제 일회용 저장 회귀(기존100검사), Editor 지역/저장/거래/퀘스트 검사와 전체 Python·월드 검사를 실행한다. 옛 승인 캡처를 덮어쓰지 말고 새 Validation 경로를 사용한다.
6. 실제 전후 캡처로 사막여우·이름표·작은 Space 안내·큰 창 하나만 열림·채팅 안내 소멸을 묶어 사용자에게 제출. 승인 후에만 원격 반영 절차로 간다.
7. 완료 시 최신 main 포함 확인, 문서와 WorkLog 갱신·로컬 커밋. push/PR/원격 integration은 새 지시와 실제 팀 공지 규칙에 따른다.

## 건드리지 않을 로컬 파일

`Packages/packages-lock.json`, `ProjectSettings/ProjectSettings.asset`는 기존 도구/실행 환경 변경이라 이번 커밋에서 제외했다. 미추적 `Assets/Caravan3DPrototype*`, `Assets/GoryeoJourneyPrototype*`, `Tools/Prototype3D/`, `Presentations/2026-10-26/`도 보존·제외한다. 광범위한 git add/reset/clean을 하지 않는다.

별도 미완료 사항: 마지막 팀 회의의 사용자 역할은 “시연용 시나리오 구성”이다. 완성된 최신 시연 시나리오가 있다고 확인하지 못했으며, 현재 UI 수정을 끝내고 별도 지시 범위를 확인할 일이다.
