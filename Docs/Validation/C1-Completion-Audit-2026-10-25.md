# C1 완료 재점검 — 2026-10-25

사용자 요청에 따라 이전 C1 완료 커밋 `14903ae8`을 재점검했다. 다음 C2·C3는 시작하지 않았다.

## 재실행

- Python 전체94개 통과, validate_world 오류0/출구92개.
- 본편544프레임 A2 생산 목록·보관176프레임 기술 미리보기 오류0.
- 실제 Unity 작업 브랜치에서 C1NpcChecks.CheckScenes: 등록11맵/36NPC 애니메이터의16프레임이 전부 사람 경로, PPU16/pivot(8,6), 동물 참조/누락0.
- SceneTerrainChecks.CheckGeneratedScenes: 11맵45,000칸/스폰이 원본 JSON과 일치.
- Q1 25·R6 87·저장 호환27개 Unity 회귀 통과, Console error0.
- 승인 원본3개 해시 목록 전부 일치, 본편544 PNG·34 Aseprite와 메타파일578쌍 존재 확인. 기존 ID/이름/대사/지형·플레이어/동물 보존 검사는 위 Python94개에 포함된다.

## 이전 실제 이동 근거 대조

`C1-Production-Unity-2026-10-24.json`의 complete=true/error 없음, 11맵 모터6,828틱·25대화 기록과 실제 Game 캡처를 확인했다. 개경 작업자는 원래 동선30틱 진행 후 접근 가능했다. transit_wetroad는 대화 NPC가 없으므로 이동만 확인했다. 이번에는 본편 변경이 없어 Play 이동 검사를 반복하지 않았다. 사람이 직접 플레이한 D-1 완주는 여전히 별도다.

## 보완

Unity가 생성한 `ArchivedAnimals/archive-index.json.meta` 1개가 이전 커밋 이후 미추적으로 남아 있었다. 보관 색인의 GUID를 다른 체크아웃에서도 유지하도록 함께 커밋한다. 기능/그림/원본/씬/저장 변경은 없다.

C1은 작업 브랜치에서 완료 상태를 유지한다. main에는 아직 반영되지 않았으며 기존 규칙대로 Claude 검토/병합 대상이다. push/PR/빌드 없음. 다음 지시를 기다린다.
