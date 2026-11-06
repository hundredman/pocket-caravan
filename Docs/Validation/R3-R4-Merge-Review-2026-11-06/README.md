# R3·R4 병합 전 검토 근거

상세 판정/수정 순서: [Codex 검토서](../../Goryeo/R3-R4-Merge-Review-2026-11-06.md). 실제 Claude 검토/본편 병합 완료 기록이 아니다.

- original-core-probe.json: claude/r3-r4의 규칙 원본 C#5개와 기존 Stubs/Program으로56검사 통과. 원본 net8 프로젝트는 이 Mac에서 NU1100 실패, 임시 프로젝트 대상만 설치된net10으로 바꾸어 실행했다.
- bulk-counterexample.json: 같은 day1/naru/grain/Fair에서 수량1은60%·실패, 전달 수량100은약65%·성공. 실제 거래 가능1개라는 호출 시나리오를 주었으며 Unity UI/Market.Preview를 함께 실행하지 않았다. 수량 없는 HaggleDeal/호출자 소스와 함께 확인한다.
- source-manifest.json: 추출 커밋·원본/검토 프레임워크·원본 코드 지문. 게임 규칙 파일은 수정하지 않았다.
- original-target.log: 원본 net8 그대로 실행한 실패 로그. 설치된 SDK10/참조 팩10만으로는 원래 검사 프로젝트가 실행되지 않는다. 이를 통과로 바꾸어 쓰지 않는다.
- merge-preview.txt/changed-files.txt/refs.json: 현재 HEAD와 별도 브랜치의 충돌 미리보기·결과 트리 변경 목록. 작업 폴더/인덱스 실제 병합은 하지 않았다.
- save-field-audit.json: 사용자 저장9개 관련 키 읽기. 비어 있지 않은 R3/R4 기록/속죄 계약0. save-preservation.json: 이전 UI 점검 백업과9해시 불변. 실제 사용자 저장 이주/새 쓰기를 하지 않았다.

재현: `python3 Tools/Integration/review_r3_r4_core.py --ref 9c0fe9d06`. 임시 파일만 만들고 정리하며 SDK/패키지 설치는 하지 않는다. 이번에는 Unity Play·전체 unittest/Unity8종·화면/온라인/사람 플레이를 실행하지 않았다. 본편 소스/공용 파일·에셋·ID·저장 형식은 그대로다. 최신main 반영·로컬커밋만, push/PR/빌드 없음.
