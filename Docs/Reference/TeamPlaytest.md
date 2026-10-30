# 팀원이 프로토타입 확인하기

안내 갱신: 2026-10-18. **현재 main을 확인하려면 Unity 소스를 사용한다. 아래 공개 ZIP은10월15일의 이전 게임 버전이다.** 공개 파일·로컬 제작 ZIP·현재 소스 차이는 [빌드 현황](BuildStatus.md)을 먼저 확인한다. 저장소가 비공개이므로 접근 권한을 받은 GitHub 계정으로 로그인한다.

현재 본편은 고려9맵+중간권6맵이며, 주행 낙하 제거·시설·새 게시판 목적지 제한이 반영됐다. 제작용 환경/장애물 시험장은 별도다. 북유럽은 Draft PR #2이며 main에 아직 없다.

## 실행 파일로 확인

[10월15일 Windows·Mac 프리릴리스](https://github.com/hundredman/pocket-caravan/releases/tag/goryeo-prototype-2026-10-15)의 Assets에서 자기 운영체제에 맞는 ZIP을 받는다.

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

기존 씬·데이터가 저장소에 있으므로 단순 Play를 위해 생성 메뉴를 다시 실행할 필요는 없다. 맵 생성과 수작업 보존의 현재 제약은 [지역 제작안](../Archive/RegionAuthoringPlan.md), 이번 변경 원본은 [프로토타입 기록](../Archive/GoryeoPrototypeReview.md)을 참고한다. 지역 배치의 원본은 Python이며 생성 씬만 편집하면 재생성 때 유실될 수 있다.

## 짧은 검토 동선

WASD/방향키 이동, Shift 달리기, Space 상호작용, Q 퀘스트, M 지도, L 장부, Esc 메뉴.

예성 나루의 장터에서 물건/가격 확인 → 예성 길의 숲길 또는 큰길 → 개경 남문·시전·동문 짐 마당 → 닥나무 마을 → 귀환 화물 선택. 처음에는 자유롭게 플레이하고, 다른 회차에서 도보/수레·화물량을 바꾸어 비교한다. 동문 작업자는 생활 연출이며 실제 생산 경제가 아니다.

피드백에는 받은 ZIP 이름 또는 소스 커밋, 운영체제/장비, 방문 맵, 수행 행동, 기대한 결과와 실제 결과, 화면/로그를 남긴다. 재미 검토는 첫 장사 이해, 길 선택 이유, 반복 이동 피로, 수도 도착감, 화물/손익 가독성으로 나누어 기록한다. 인증정보·개인정보는 로그에서 제외한다. 자세한 팀 검토 항목은 [고려 프로토타입 확인](../Archive/GoryeoPrototypeReview.md)에 있다.

기존 `goryeo-preview-2026-10-15`는 과거 Mac 프리뷰다. 공개10월15일 릴리스는 당시 개경 개선·교역/장부·로컬 오류 수정을 담았다. 이후 main의 개선은 포함하지 않는다. 최종 미술/지역 완성본과 온라인 MVP 완성을 뜻하지 않는다.

## 10/20 고려 경제/귀환 교역 검토판

경제 패치 시점의 로컬 파일은 `Builds/Packages/deliverables/`의 `PocketCaravan-Windows-x64-Economy-2026-10-20.zip`, `PocketCaravan-macOS-Universal-Economy-2026-10-20.zip`이다. 소스efb04cb. GitHub 공개10/15 릴리스/10/18 Progression ZIP과 다르며 clone만으로 이 파일을 받는 것은 아니다.

최신 새 게임에서는 땔감 운송 입문을 둘 중 하나 선택하고, 완료 후 닥목 촌장의 종이 귀환 거래를 선택할 수 있다. 기존 저장에서 이미 맡은 두 입문 의뢰와 기수락 보상은 유지한다. ‘귀한 짐’은 적재 공간8~12를 사용한다. Q로 선택 의뢰/시설·거래처를 확인하고, 배달 제안과 매매 원가·적재 공간을 비교한다.

테스트할 때 첫 교역과 세 번째 왕복의 실제 시간, 배달/순수 매매/퀘스트 보수, 선택 이유, 수레 구입 후 매입 자금, 다음 장사가 떠오르지 않은 지점을 따로 남겨 주세요. 사람 평가·Windows 실제 실행·온라인은 자동 검사 통과와 구분한다. 구체적인 조건/계산 한계는 [경제 패치](GoryeoTradeBalancePatch.md)를 따른다.

## 10/20 교역 수량·운전자금 검토판

10/20 수량 패치 검토 ZIP은 `PocketCaravan-Windows-x64-Market-2026-10-20.zip`과 `PocketCaravan-macOS-Universal-Market-2026-10-20.zip`이다. 폴더는 `Builds/Packages/deliverables/`, 소스1064d73. Economy 패치를 포함하며 아직 GitHub Release에 올린 파일이 아니다. 일반 clone에는 ZIP이 포함되지 않는다.

장터에서 품목을 고른 뒤 수량 +/- 또는 프리셋으로 총액·잔금/매매 손익을 확인한다. 실제 거래 수량은 자금/재고/적재 여유/보유 수량으로 제한된다. 운반 수단 탭에서 수레 구매 후 남는 자금을 확인한다. 가격이 변동하므로 장부의 과거 단가를 전체 판매 수익으로 곱하지 않는다. 의뢰 보상은 매매 손익과 별도다. 사람 플레이에서는 잔금 안내를 이해했는지와 반복 왕복 선택을 기록해 주세요. [상세](MarketPlanningPatch.md).

## 10/21 화물 판매처·장부 검토판

장부 검토 시점의 로컬 파일은 `Builds/Packages/deliverables/`의 `PocketCaravan-Windows-x64-Ledger-2026-10-21.zip` / `PocketCaravan-macOS-Universal-Ledger-2026-10-21.zip`이다. 코드946dcbb, Economy/Market 개선 포함. GitHub Release에 올린 파일이 아니며 clone으로 ZIP을 받는 것은 아니다.

장터에서 L을 누르면 장부로 전환한다. ‘내 짐 판매처’에서 원가와 직접 기록한 단위 판매가, 다음 경유 맵을 비교하고 ‘전체 시세’ 탭에서 기존 표를 볼 수 있다. 높은 과거 단가가 가장 좋은 여행/전량 수익을 뜻하지 않는다. 첫 왕복과 세 번째 왕복에서 본인이 고른 판매처/귀환품과 이유를 남겨 주세요. 연결 수를 시간으로 오해했거나 안내를 보고도 다음 목적지를 정하기 어려웠던 지점도 기록합니다. [범위·검증·후속](CargoLedgerReview.md).

## 10/21 저장 보호 검토판

최신 로컬 ZIP은 `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-SaveGuard-2026-10-21.zip`이다. 코드d277308, Ledger 개선 포함. GitHub Release 업로드는 아직 하지 않았다.

저장 복원 중단 안내가 뜨면 이번 세션은 저장되지 않는다. Esc 메뉴에서 원인을 확인하고 원래 파일을 보관한 상태로 누락 ID/맵·이주를 검토한다. 파일을 자동 초기화하거나 새 ID로 바꾸지 않는다. 메뉴의 ‘처음부터 시작’은 기존 진행 초기화 선택이므로 이어서 할 의도가 있다면 선택하지 않는다. 유효한 구 저장의 기수락 배달/보수/화물 원가는 유지한다. [검토/확인 범위](SaveCompatibilityReview.md).

## 10/22 실제 해안/내륙 동선

최신19구역에서는 실제 해안을 걸으며 지도M과 대조할 수 있다. Naru 남쪽→갯벌 해안길→예성 물굽이→남쪽 해안 구릉→장단 순환, 북서쪽 서해안 둔덕→솔바람 고개→여러 경유지→내륙 길목을 비교한다. 연결된 풀밭 경계도 왕복해본다. 구역 전환은 남아 있다. 다른 문명은 미연결이다. [현재 지형](../Archive/ContinuousTerrain.md)과 [빌드 버전](BuildStatus.md)을 먼저 확인한다.
