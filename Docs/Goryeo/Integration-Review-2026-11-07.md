# 고려 integration 통합 전 검토

사용자가 Claude 사용량 소진으로 Codex에 검토·카톡 문안·GitHub 반영을 맡겼다. 기존 PR #27(region/goryeo → integration)을 최신 승인 상태로 갱신하는 범위다. 사용자가 “팀에 전달했어”로 사전 공지 전달을 확인했다. PR #27은 integration에 병합 완료됐으며 병합 커밋은 `86e46c840919e50a483367dfeb54cfebca263127`다. GitHub 검사2개 성공, 원격 integration 포함과 검증한775500b5a 대비 전체 트리 동일을 확인했다.

## 범위

원격 integration bcb3d7280은 현재 기능 브랜치의 조상이다. 사전 조사 기준42커밋·1445경로 차이이며, 분위기·생활 기능이 들어간 기존 PR27도 아직 열려 있다. 검토 문서/로그 추가 전 기능 소스는 c540d6614다. 최신 main도 기능 브랜치에 이미 포함됐다. 변경 경로에는 다수 검토 그림/소리/검증 자료가 포함되므로1445가 게임 코드 파일 수는 아니다.

- 승인된 고려·중앙 동쪽 날씨/음악/발소리/생활 연출·장소 이름표·노면 속도·나룻배·앉기/모닥불·우물·장맛비 통나무/천둥.
- R3 흥정·R4 회복/속죄·저장 실패 복원·실제 실행 수량에 맞는 흥정 적용.
- 게시판/입력 중 미끄러짐 보완 및 승인된 공용 종이 테마·대화/문명 선택/소지품/메뉴/백과/여관·글자 대비·짧은 지도 이름과 양옆 이름표.
- UI배율 통일/최소 지원 크기 확정은 포함하지 않는다.

DB/GameDatabase·WorldIndex·씬·Connections·빌드 목록·맵 JSON·다른 문명 전용 스크립트는 이 통합 차이에서 변경하지 않았다. 시장/상품/퀘스트/NPC/시설ID를 바꾸지 않는다. 저장 선택 필드와 기존 저장 이주는 앞선 검사로 확인하며 저장 실패 시 되돌리기도 재실행했다.

공용/연결 게임 스크립트26개(고려 전용25개는 별도):

- `Contracts/Contract.cs`
- `Contracts/ContractBoard.cs`
- `Contracts/ContractManager.cs`
- `Core/SaveSystem.cs`
- `Economy/Haggle.cs`
- `Economy/Inn.cs`
- `Economy/MarketState.cs`
- `Economy/SavedEconomyChange.cs`
- `Economy/StandingRecovery.cs`
- `Economy/TownMarket.cs`
- `Economy/TradeActivity.cs`
- `Map/MapRoot.cs`
- `Map/TalkNpc.cs`
- `Net/NetLogin.cs`
- `Obstacles/GoryeoLogVisuals.cs`
- `Player/PlayerController.cs`
- `UI/CommerceGuideUI.cs`
- `UI/CuteMap.cs`
- `UI/CuteUI.cs`
- `UI/DialogueUI.cs`
- `UI/InventoryUI.cs`
- `UI/PauseMenu.cs`
- `UI/StartScreen.cs`
- `UI/TownFacilitiesUI.cs`
- `UI/WorldMapUI.cs`
- `World/RoadHazards.cs`

## 재검사

- Python403개·23skip·실패0, 월드0오류·1437출구.
- Unity Editor7종:생성54씬/263099셀 일치,고려25,중간권3, Q1 25,경험302,교역449,저장 호환27. 각 결과 JSON 보존.
- 최종 소스에서 실제 임시 저장ON R3/R4 Play100확인·10캡처. 흥정 실행 수량1/19/20/100,동일 장터 되팔기,회복/기부/속죄 수락·납품/부분·실패,사전/사후 저장 실패 복원과 이어하기를 재확인했다. UI를 열고 버튼에 연결된 실제 콜백을 호출하는 자동 검토이며 사람 직접 클릭 검사가 아니다.
- 앞선 승인 지도 최종295확인/15캡처 및 UI별 검사는 해당 단계 기록을 유지한다. 이번에 다시 실행했다고 합산하지 않는다.
- 기존 사용자 저장8파일 SHA256 불변·일회용 저장0. 본 저장은 이전부터 부재 상태이며 이전 백업을 유지했다.
- Console 컴파일error0. 로컬 embedded Unity MCP/그에 따른 lock 변경과 runInBackground 설정은 도구 환경이며 커밋/PR에서 제외한다. 3D 실험/발표 자료도 보존·제외했다.

검토 범위에서 병합을 막을 새 결함을 발견하지 못했다. 다른 팀의 전체 전용 콘텐츠·실제 온라인/채팅/서버 저장·네이티브 마우스/IME·장시간/FPS·게임 빌드/완전한 앱 재시작은 미검사다. GitHub CI와 원격 mergeable은 PR 최신 상태로 별도 확인한다.

## 카톡 사전 공지 문안

고려 작업 PR #27을 최신 승인본으로 갱신해 integration에 합치려고 합니다. 날씨·소리·생활 연출·나룻배·앉기·우물·장맛비 통나무, 흥정/평판 회복, 공용 UI 가독성과 지도 이름표 수정이 포함됩니다. 공용 경제·저장/의뢰·UI 및 이동/대화 연결 코드가 바뀌며, DB·씬·맵 연결·다른 문명 전용 파일은 그대로입니다. Python403개(23제외), Unity Editor7종과 흥정/회복/저장실패 자동100확인을 통과했습니다. 실제 온라인/FPS는 미검사입니다. PR: https://github.com/hundredman/pocket-caravan/pull/27

## 카톡 완료 공지 틀

고려 PR #27 integration 통합 완료했습니다. 병합 커밋: 86e46c840. GitHub CI: 검사2개 모두 성공. 날씨·생활 기능/흥정·회복/공용 UI·지도 수정이 반영됐습니다. DB·씬·맵 연결/다른 문명 전용 파일은 변경하지 않았고, 실제 온라인·장시간 성능은 추가 확인 대상입니다. 최신 integration을 받아 확인해 주세요.

완료 공지는 실제 병합 뒤 결과/해시를 채워 전달한다. 공지 문안 작성/PR댓글을 카톡 전송으로 간주하지 않는다. [검증 기록](../Validation/Goryeo-Integration-2026-11-07/README.md).
