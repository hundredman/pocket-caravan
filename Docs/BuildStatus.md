# 소스·빌드·발표 파일 현황

확인: 2026-10-18. **날짜가 같아도 다른 시험장/본편 빌드이며, main에 반영됐다고 공개 ZIP이 갱신되는 것은 아니다.**

## 공유할 때 구분할 파일

| 구분 | 내용 | 위치/배포 상태 | 포함하지 않은 것 |
|---|---|---|---|
| 현재 main 소스 | 고려6+중간권4맵, 제작 라이브러리, 게시판 지역/경로 제한(#7) | GitHub main. Unity6000.3.26f1로 확인 | 온라인, 북유럽 Draft 콘텐츠, 새 출신 배율·퀘스트 지역/장 제한 |
| 공개10/15 본편 프리릴리스 | 코드 `3ad1d5c` 기준 이전 고려 프로토타입 | [GitHub Release](https://github.com/hundredman/pocket-caravan/releases/tag/goryeo-prototype-2026-10-15) | 이후 마을/시설·낙하 제거·중간권·게시판 패치. 최신 main 검증용으로 쓰지 않음 |
| 로컬10/17 고려 ZIP | 고려6맵·로컬 시설·운반 개선 | `Builds/Packages/deliverables/PocketCaravan-{Windows-x64,macOS-Universal}-2026-10-17.zip`. 미공개 | 중간권/후속 공용 수정은 이후 별도 빌드 |
| 로컬10/17 Transit ZIP | 고려6+중간권4맵 연결 | 같은 폴더의 `PocketCaravan-*-Transit-2026-10-17.zip`. 미공개 | 10/18 게시판 수정 |
| 로컬10/17 ModuleLibrary ZIP | 환경 원형40·환경13·날씨8 비교/조합 시험장 | 같은 폴더의 `PocketCaravan-ModuleLibrary-*-2026-10-17.zip`. 미공개 | 본편 플레이 전체·10/18 게시판 수정 |
| 로컬10/17 ObstacleLibrary ZIP | 장애물48종을 선택하는 별도 걷기 시험장 | 같은 폴더의 `PocketCaravan-ObstacleLibrary-*-2026-10-17.zip`. 미공개 | 본편 자동 장애 배치·장애 해결 저장·멀티·10/18 게시판 수정 |
| 10/19 발표 제출물 | PPTX+PDF, 대본 | `Presentations/2026-10-19/`의 지정 한 쌍 | 개발 이후 구현 상태와의 재동기화 필요. README만 고쳐도 슬라이드 본문/PDF는 바뀌지 않음 |

위 로컬 ZIP은 작업 컴퓨터의 산출물이다. 저장소 clone만으로 받아지지 않는다. GitHub에 새 릴리스로 업로드한 상태도 아니다. 로컬 경로를 팀 다운로드 링크로 안내하지 않는다.

Windows ZIP은 exe와 `_Data`/DLL 폴더를 모두 유지한다. Mac은 Intel/Apple Silicon Universal이며 Developer ID 서명·Apple 공증 완료 빌드가 아니다. 빌드 성공, 이 컴퓨터에서 초기화 성공, 다른 사람의 실제 플레이 성공은 각각 구분해서 기록한다. 상세 실행은 [팀 확인 안내](TeamPlaytest.md)를 따른다.

## 근거와 앞으로 남길 배포 정보

[고려 검사](Validation/Goryeo-2026-10-17.json), [중간권 검사](Validation/Transit-2026-10-17.json), [환경 라이브러리 검사](Validation/ModuleLibrary-2026-10-17.json), [장애물 검사/해시](Validation/Obstacles-2026-10-17.json), [게시판 소스 검사](Validation/RegionalContracts-2026-10-18.json). 로컬 `Builds/Packages/deliverables/`의 체크섬 파일도 확인한다.

다음 팀 공유 빌드부터 `종류/플랫폼/파일명/소스 전체 커밋 SHA/생성 시각/포함 기능/실행 검증 환경/검사·해시/공유 URL`을 함께 남긴다. 기존 산출물 중 정확한 소스 SHA가 기록되지 않은 것은 날짜만 보고 추정하지 않는다. 현재 보유 ZIP을 최신 main 전체 빌드라고 이름만 바꾸지 않는다.
