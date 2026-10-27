# R2 수치 계산 검증 기록

2026-10-25 / Codex / `codex/r2`. 최신 main71d56cf5에서 새 worktree, 이전 A4 피드백57ec71ce를 합쳐 사용자 결정을 보존했다.

- 실제 Unity Edit mode의 GameDatabase에서11상품(일반8/퀘스트3),7마을,4출신과 가격 기준1680건을 읽었다. snapshot 원본/생산 입력55파일이 현재 작업트리와 엔진 프로젝트에서 동일함을 SHA256으로 확인했다(`input-hashes.json`). 에셋을 생성/저장하거나 Play에 들어가지 않았다.
- `dotnet run --project Tools/Balance/R2Probe -- Docs/Validation/R2-2026-10-25/source-snapshot.json Docs/Validation/R2-2026-10-25/calculations.json`: 실제 MarketMath/MarketState/ItemData/TownData/OriginData/GameDatabase C# 연결, Unity 기준1680건 일치, 반올림/최소가/known-answer 왕복 검사, 후보5160960조합·288그룹·1344단가·할인9구간 계산 완료. 같은 기준선 결과를 재사용하므로516만개의 독립 테스트라고 부르지 않는다. 검사 프로그램 컴파일이며 Unity 게임 빌드는 아니다.
- `python Tools/Balance/report_r2.py`: 단계별 후보/반올림 체감/동일 장터 왕복 사례/한계/Claude 결정 목록을 README로 생성.
- `python -m unittest discover -s Tools/Integration -p 'test_*.py'`:109개 통과(기존105+보고서 정합성4).
- `python Tools/Integration/validate_world.py`: 오류0·출구92개.
- Unity Console error0. Play/실제 이동·거래/사람 플레이/저장 회귀/게임 빌드는 미실행이다. 계산에 현금·적재 한도, 흥정 확률·재시도, 시간 경과·NPC 거래를 넣지 않았다. 왕복 후 재고/등급이 변할 수 있어 무한 반복 수익으로 단정하지 않는다.

결과: 작은 할인도 출신/재고/흥정 조합에서 이익을 만들 수 있어 세 후보 모두 그대로 채택하지 않는다. 흥정 없는 고정 성장 시험에서1~2%는 새 이익 사례가 없었고3%부터 있었다. 그 밖 조건의 안전을 증명하지 않는다. D10의 수치·효과·품목/시설 잠금은 Claude·사용자가 결정한다. 점수/저장·본편 코드는 바꾸지 않았다. R2 전체 구현은 대기, 수치 계산 단계만 완료다.
