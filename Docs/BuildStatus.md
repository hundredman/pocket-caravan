# 소스·빌드·발표 파일 현황

확인: 2026-10-22. 최신 Continuous ZIP은 실제 연속 지형19구역·지도 일치·풀밭 경계 이동·저장 위치 보정을 포함한다. 이전 ZIP과 구분한다. **날짜가 같아도 다른 시험장/본편 빌드이며, main에 반영됐다고 공개 ZIP이 갱신되는 것은 아니다.**

## 공유할 때 구분할 파일

| 구분 | 내용 | 위치/배포 상태 | 포함하지 않은 것 |
|---|---|---|---|
| 현재 main 소스 | 고려13+중간권6구역(19구역), 공통 실제 지형/지도·해안/하구4구역·풀밭 경계 이동·저장 높이 보정·내륙 초원, 제작 라이브러리, 게시판 지역/경로 제한(#7), 고려·중간권 지형 재배치·여정 수첩/NPC 의뢰 선택·고려 배달/귀환 교역·수량 거래/구매 예산·화물 판매처/장부 연결·저장 참조 검사/실패 안내 | GitHub main. Unity6000.3.26f1로 확인 | 온라인, 북유럽 Draft 콘텐츠, 새 출신 배율·퀘스트 지역/장 제한 |
| 공개10/15 본편 프리릴리스 | 코드 `3ad1d5c` 기준 이전 고려 프로토타입 | [GitHub Release](https://github.com/hundredman/pocket-caravan/releases/tag/goryeo-prototype-2026-10-15) | 이후 마을/시설·낙하 제거·중간권·게시판 패치. 최신 main 검증용으로 쓰지 않음 |
| 로컬10/22 Continuous ZIP | 실제 해안·강·길·숲과 지도 일치,19구역 경계 연결, 구 저장 위치 보정 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-Continuous-2026-10-22.zip`. 소스0bb0175, 미공개 | Windows 실제 실행·다른 Mac·사람 체감·온라인 미검증. 씬 전환은 남아 있음 |
| 로컬10/22 Peninsula ZIP | 고려 반도·바다·대륙 지도, 송악 북서쪽 대륙 진입, 여러 경유 지역과 건조 초원, 고려 확대 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-Peninsula-2026-10-22.zip`. 소스7ddf9df, 미공개 | Windows 실제 실행/다른 Mac·전 여정 재미·온라인 미검증. 남부 반도 배경은 미구현 |
| 로컬10/21 SaveGuard ZIP | Ledger 개선 + 저장 참조 사전 검사·원본 보존/실패 안내 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-SaveGuard-2026-10-21.zip`. 코드d277308, 미공개 | Windows 실제 실행·사람 반복 재미·온라인 미검증. Mac 기존 프리뷰 초기 복원 확인 |
| 로컬10/21 Ledger ZIP | Market 개선 + 장터 L 연결·화물 판매처/경유 비교·안내 문구 정리 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-Ledger-2026-10-21.zip`. 코드946dcbb, 미공개 | Windows 실제 실행·사람 반복 재미·온라인 미검증. Mac 초기 실행만 확인 |
| 로컬10/20 Market ZIP | Economy 패치 + 수량별 변동 총액/원가 손익·수레 구매 후 잔금 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-Market-2026-10-20.zip`. 코드1064d73, 미공개 | Windows 실제 실행·사람 반복 재미·온라인 미검증. Mac 초기 실행만 확인 |
| 로컬10/20 Economy ZIP | 고려9+중간권4, 수첩·고려 배달 보수·땔감 대안 입문·종이 귀환 거래·구 약속 보존 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-Economy-2026-10-20.zip`. 코드efb04cb, 미공개 | Windows 실제 실행·사람 반복 재미·온라인 미검증. Mac 초기 실행만 확인 |
| 로컬10/18 Progression ZIP | 고려9+중간권4, 지역 공급/수요·선택 성장·산록 순환 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-Progression-2026-10-18.zip`. 코드4234c13, 미공개 | Windows 실제 실행·사람 플레이·온라인 미검증 |
| 로컬10/17 고려 ZIP | 고려6맵·로컬 시설·운반 개선 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-2026-10-17.zip`. 미공개 | 중간권/후속 공용 수정은 이후 별도 빌드 |
| 로컬10/17 Transit ZIP | 고려6+중간권4맵 연결 | 같은 폴더의 `PocketCaravan-*-Transit-2026-10-17.zip`. 미공개 | 10/18 게시판·지형 재배치 |
| 로컬10/17 ModuleLibrary ZIP | 환경 원형40·환경13·날씨8 비교/조합 시험장 | 같은 폴더의 `PocketCaravan-ModuleLibrary-*-2026-10-17.zip`. 미공개 | 본편 플레이 전체·10/18 게시판·지형 재배치 |
| 로컬10/17 ObstacleLibrary ZIP | 장애물48종을 선택하는 별도 걷기 시험장 | 같은 폴더의 `PocketCaravan-ObstacleLibrary-*-2026-10-17.zip`. 미공개 | 본편 자동 장애 배치·장애 해결 저장·멀티·10/18 게시판·지형 재배치 |
| 10/19 발표 제출물 | PPTX+PDF, 대본 | `Presentations/2026-10-19/`의 지정 한 쌍 | 10/18 구현 현황·화면·대본 동기화, 8번은 고려9맵 확대 지형과 새 연결 반영. PPTX 본편14+숨긴 보충4, PDF18쪽. 이후 온라인 시험 결과는 발표 전 재확인 |

위 로컬 ZIP은 작업 컴퓨터의 산출물이다. 저장소 clone만으로 받아지지 않는다. GitHub에 새 릴리스로 업로드한 상태도 아니다. 로컬 경로를 팀 다운로드 링크로 안내하지 않는다.

Windows ZIP은 exe와 `_Data`/DLL 폴더를 모두 유지한다. Mac은 Intel/Apple Silicon Universal이며 Developer ID 서명·Apple 공증 완료 빌드가 아니다. 빌드 성공, 이 컴퓨터에서 초기화 성공, 다른 사람의 실제 플레이 성공은 각각 구분해서 기록한다. 상세 실행은 [팀 확인 안내](TeamPlaytest.md)를 따른다.

## 근거와 앞으로 남길 배포 정보

[고려 검사](Validation/Goryeo-2026-10-17.json), [중간권 검사](Validation/Transit-2026-10-17.json), [환경 라이브러리 검사](Validation/ModuleLibrary-2026-10-17.json), [장애물 검사/해시](Validation/Obstacles-2026-10-17.json), [게시판 소스 검사](Validation/RegionalContracts-2026-10-18.json). 로컬 `Builds/Packages/deliverables/`의 체크섬 파일도 확인한다.

다음 팀 공유 빌드부터 `종류/플랫폼/파일명/소스 전체 커밋 SHA/생성 시각/포함 기능/실행 검증 환경/검사·해시/공유 URL`을 함께 남긴다. 기존 산출물 중 정확한 소스 SHA가 기록되지 않은 것은 날짜만 보고 추정하지 않는다. 현재 보유 ZIP을 최신 main 전체 빌드라고 이름만 바꾸지 않는다.

## 10/20 검토 빌드 검증

Windows 성공(오류0·경고18), Mac Universal 성공(오류0·경고0). Windows 경고를 모두 무해하다고 분류한 것은 아니다. Mac 실행 파일의Intel/Apple Silicon 포함과 headless 초기화/실행 예외0/종료0을 확인하고 기존 프리뷰 저장을 바이트 기준 복구했다. 사람이 직접 조작한 플레이나 다른 Mac의 Gatekeeper 통과 검증과 다르다. 빌드가 바꾼 기존 프로젝트/렌더 설정은 작업 전 값으로 복구했다.

패키지 SHA256·빌드 원본 전체 커밋·검사·제한은 [10/20 검증 기록](Validation/GoryeoTradeBalance-2026-10-20.json). ZIP 무결성·README 중복 없음·플랫폼 실행 파일 포함을 확인했다. 읽을 안내는 같은 폴더 `README-Economy-2026-10-20.txt`, 체크섬은 `SHA256-Economy-2026-10-20.txt`다. GitHub 새 Release 업로드는 하지 않았다.

## 10/20 교역 수량 검토 빌드

Market ZIP 원본은1064d7397d5f486ff1da0d90f468a64df96fe054다. Windows/Mac 모두 성공(각 오류0·경고18). Mac Universal 두 아키텍처와 headless 초기화/종료0/실행 예외0·기존 프리뷰 저장 바이트 복구를 확인했다. 빌드가 바꾼 프로젝트/렌더 설정을 복구했고 Editor는 정지한 Route01로 돌아갔다. [소스·검사·플랫폼별 해시/제한](Validation/MarketPlanning-2026-10-20.json). 이전 Economy ZIP에 수량 UI가 포함됐다고 안내하지 않는다. 공개 릴리스는 여전히10/15다.

## 10/21 장부 검토 빌드

Ledger ZIP 소스는946dcbb3dde6ff025fddc5d0087d2f5a6d5290ec다. Windows 성공(오류0·경고18), Mac 성공(오류0·경고0). Mac Universal/headless 초기화·예외0·기존 프리뷰 저장 복구와 ZIP 구조/해시를 확인했다. [검증 기록](Validation/CargoLedger-2026-10-21.json). 경고를 모두 무해하다고 분류한 것은 아니며 실제 Windows/다른 Mac 플레이·서명/공증은 별도다. 공개 릴리스는 여전히10/15다.

## 10/21 저장 보호 검토 빌드

SaveGuard ZIP 소스는d277308이다. Windows 성공(오류0·경고18), Mac 성공(오류0·경고0). Mac Universal 초기 실행·기존 호환 프리뷰의 복원 차단 없음·실행 예외0·종료0·저장 바이트 복구를 확인했다. 프로젝트/렌더 설정도 원래 값으로 복구했다. [전체 소스·검사·해시·한계](Validation/SaveCompatibility-2026-10-21.json). Ledger ZIP에는 이 후속 보호가 없으며 공개10/15 릴리스도 갱신되지 않았다.

## 10/22 반도·대륙 검토 빌드

소스7ddf9dfcbbc2bd6c0024b8f8b3e881ff35be9435. Windows 성공(오류0·경고18), Mac 성공(오류0·경고0). Mac Universal 두 아키텍처·headless 초기화/실행 예외0/종료0·기존 프리뷰 저장 바이트 복구를 확인했다. Windows는 빌드/PE x64 형식까지 확인했으며 실제 실행은 별도다. ZIP 무결성/안내문/소스/해시는 [검증 기록](Validation/CentralConnection-2026-10-22.json)을 따른다. 프로젝트/렌더 설정은 빌드 전 값으로 복구했다. 이전 중간 배치 원시 빌드는 이 ZIP으로 대체하며 공개 Release는 여전히10/15다.

## 10/22 실제 연속 지형 검토 빌드

소스0bb01751ac556a16e500aa96617e742a8ae4d0ae. Windows 성공(오류0·경고19), Mac 성공(오류0·경고0). Mac Universal Intel/Apple Silicon, headless 초기화/종료0·실행 예외0·호환 프리뷰 저장 복원과 원본 바이트 복구를 확인했다. Windows는 빌드/PE x64 형식까지 확인했으며 실제 실행은 미검증이다. ZIP 무결성·README·소스/해시는 [검증 기록](Validation/ContinuousTerrain-2026-10-22.json)을 따른다. 프로젝트/렌더 설정5개는 빌드 전 바이트로 복구했다. 로컬ZIP이며 공개10/15 Release는 갱신하지 않았다. Mac Developer ID 서명/공증은 미완료다.
