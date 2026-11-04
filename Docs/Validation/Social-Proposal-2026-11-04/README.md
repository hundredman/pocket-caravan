# 11/04 1번 고려 친구 수정안 준비 검사

구현 반영 전 후보 문서 검사다. 실제 화면 이름8개(등록 퀘스트 NPC6/말 거는 인물2종), 선물 상품의 GameDatabase 등록, 뱃사공3포구 공유를 확인했다. Social.cs 원본 SHA256은 그대로다. 기념품은 기존 수첩의 기록이며 새 물품/버프를 만들지 않는다.

- Python3.10/Pillow12.3/numpy2.2.6/scipy1.15.3: 354개 실행,23skip,실패0.
- Python3.10/Pillow12.3만 설치: 354개 실행,32skip,실패0. 선택 의존성 검사는 명시적으로 건너뜀.
- validate_world: 0오류/1437출구.
- Unity6000.3.26f1: MapJsonBuilder.ValidateWorld, SceneTerrainChecks.CheckGeneratedScenes, GoryeoRegionChecks.Run, TransitRegionChecks.Run, Q1Checks.Run, GoryeoExperienceChecks.RunData, GoryeoTradeBalanceChecks.RunData, SaveCompatibilityChecks.RunData 모두 통과. 해당 txt 원본 결과를 보존했다. 씬 검사는 기존 씬으로 복귀한다.

대사/기념품 승인 대기이므로 Social.cs/맵/씬은 미수정. Play·G 선물·화면 캡처는 아직 실행하지 않았고, 승인 반영 후 실행한다. 사용자가 작업 중인 packages-lock.json·3D 실험·발표 폴더는 이번 커밋에 포함하지 않는다.
