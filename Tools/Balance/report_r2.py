"""Summarize unapproved R2 candidates. Never writes runtime data or design decisions."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/Validation/R2-2026-10-25'
STAGES=['적대','경계','중립','우호','신뢰','존경']
LABELS={'baseline':'현재 기준선','small':'작은 보정 후보','example':'큰 보정/문서 흥정 예시 후보','dual':'매입 할인+판매 가산 후보'}

def summarize():
    d=json.loads((OUT/'calculations.json').read_text());s=json.loads((OUT/'source-snapshot.json').read_text())
    goods={i['id']:i for i in s['items'] if not i['questItem']};towns={t['id']:t for t in s['towns']}
    rows=['# R2 가격·흥정 수치 계산 — Claude D10 검토용','','초기 후보 비교를 지역 참조 오류 수정 후 다시 계산한 자료다. 본편에 적용하지 않았다. D10 승인 수치와 안전 규칙의 후속 검사는 [R2 안전 검산](../R2-Safety-2026-10-25/README.md)을 본다.','',
          '## 계산 근거','','- 이전 도구는 마을마다 RegionData 객체를 따로 만들어 같은 문명 물건에도 베네치아의 외국 물건 보정이 적용됐다. 지역 ID별 객체 공유로 고치고 아래 집계 전체를 재실행했다. 단가 oracle만으로 이 오류를 잡을 수 없었으며 후속 검사에는 문명 동일/외국 판정을 추가했다.',f'- Unity {s["unity"]} 실제 GameDatabase: 일반 상품{len(goods)}종/퀘스트 화물{len(s["items"])-len(goods)}종(제외), 마을{len(towns)}곳, 출신{len(s["origins"])}종.',
          f'- Unity 실제 가격 공식 {d["oracleCases"]:,}건과 .NET에서 연결한 본편 MarketMath의 결과가 전부 일치했다. MarketState/ItemData/TownData/OriginData/GameDatabase도 본편 C#을 그대로 연결했다. Unity 플랫폼 속성·이벤트/플레이어는 검사 전용 대체이며 게임을 실행한 것이 아니다.',
          f'- 후보 조합 {d["cycleCases"]:,}개(같은 기준선 결과 재사용 포함), 그룹{len(d["groups"])}개, 정상 재고/일반일의 단계별 단가{len(d["quotes"]):,}행. 별도 할인 민감도9구간도 계산했다.',
          '- 재고=적정의0.25/0.5/1/2/4배, 마을1/최대3등급, 보통날/장날1.15배, 출신4종, 수량1/5/20/100개, 매입→판매/판매→매입을 비교했다. 거래 성장 켬/끔을 분리했고 성장 켬은 거래액0에서 시작했다.',
          '- 매 단위 거래마다 본편 가격/재고/거래 성장 재계산. 후보 적용 순서는 본편 단가 → 평판 비율/반올림/최소1 → 흥정 비율/반올림/최소1이다. 흥정 성공률은 정하지 않고 최대 폭의 성공 상황만 시험했다. 순서/반올림도 채택 전 검토 항목이다.',
          '- 출신 보정: 베네치아의 타 문명 물건 판매 재고0.8배, 아라비아의 거리3 이상 판매+5%를 본편 공식 그대로 포함했다. E1 통합 서비스의 current 설정이므로 현재 경제 좌표의 맨해튼 거리를 유지한다. layoutD 전환은 B5다.','',
          '## 후보 수치 (채택 안 됨)','','매입은 플레이어가 사는 값이다. 음수는 할인/양수는 할증. 판매 가산은 플레이어가 파는 값에 적용한다. 흥정은 없음/매입만/매입·판매 양쪽을 별도 비교했다.','',
          '| 후보 | 단계 | 매입 보정 | 판매 가산 | 흥정 최대 폭 |','|---|---|---:|---:|---:|']
    for p in d['policies']:
        if p['Id']=='baseline':continue
        for n in range(6):rows.append(f'| {LABELS[p["Id"]]} | {STAGES[n]} | {p["BuyDelta"][n]*100:+.0f}% | {p["SellBonus"][n]*100:.0f}% | {p["Haggle"][n]*100:.0f}% |')
    rows+=['','## 같은 장터 왕복 위험 비교','','아래는 **존경 가정/성장 끔/매입→판매**, 후보마다 동일한 재고 유효10,344상황이다. 이미 가진 재고를 파는 판매→매입과 성장 켬은 calculations.json의 별도 그룹에 있다. 최댓값 사례는 자금/적재 한도를 검사하지 않은 스트레스 사례다.','',
           '| 후보 | 흥정 범위 | 이익 발생/유효 | 기준선에서 새로 이익으로 바뀜 | 최대 이익 |','|---|---|---:|---:|---:|']
    for p in d['policies']:
        for h in ['none','buy_only','both']:
            r=next(g for g in d['groups'] if g['policy']==p['Id'] and g['stage']==5 and g['haggle']==h and not g['growth'] and g['order']=='buy_then_sell')
            rows.append(f'| {LABELS[p["Id"]]} | '+{'none':'없음','buy_only':'매입만','both':'양쪽'}[h]+f' | {r["profitable"]:,}/{r["valid"]:,} | {r["newProfitable"]:,} | {r["worst"]["profit"]:,}냥 |')
    rows+=['','### 대표 사례','']
    for policy,h in [('small','none'),('small','buy_only'),('example','buy_only'),('dual','both')]:
        g=next(g for g in d['groups'] if g['policy']==policy and g['stage']==5 and g['haggle']==h and not g['growth'] and g['order']=='buy_then_sell');r=g['worst']
        rows.append(f'- {LABELS[policy]}/{h}: {towns[r["town"]]["displayName"]}에서 {goods[r["item"]]["displayName"]}{r["quantity"]}개, 출신{r["origin"]}, 시작{r["startLevel"]}등급/적정재고{r["stockRatio"]}배/장날{r["marketDay"]}. 매입{r["buyTotal"]:,} → 판매{r["sellTotal"]:,}냥, 순익{r["profit"]:+,}냥(기존{r["baselineProfit"]:+,}냥). 재고{r["initialStock"]:g}→{r["finalStock"]:.2f}.')
    rows+=['','**이익 발생을 무한 반복 수익이라고 단정하지 않는다.** 베네치아는 왕복 후 재고가 줄고, 성장 켬은 등급이 바뀌므로 시장 상태가 소비된다. 모든 조합은 현금/적재 한도·실제 품목 표시/구매 가능 상태·흥정 재시도 제한을 생략했다. 현재 재고로 살 수 없는 조합은 제외했다. 다만 여러 가격 보정의 중첩을 검토할 근거다.','',
           '## 흥정 없이 할인만 적용한 민감도','','성장 끔/매입→판매/전체 출신·재고·장날 조합. 특정 구간에 사례가 없더라도 전 범위 안전이 증명된 것은 아니다.','',
           '| 할인 | 유효 상황 | 이익 발생 | 기준선 대비 새 이익 | 최대 이익 |','|---:|---:|---:|---:|---:|']
    for r in d['discountSweep']:rows.append(f'| {r["percent"]}% | {r["eligible"]:,} | {r["positive"]:,} | {r["introduced"]:,} | {r["worst"]["profit"]:,}냥 |')
    rows+=['','## 우호 시작의 단가 체감 예','','작은 보정 후보(-2%/흥정최대2%), 고려 출신/일반일/각 마을 시작 등급/정상 재고. 판매는 가산 없는 동일 단가다. 한 개 단가만 보며 여러 개 매입하면 재고 감소로 값이 올라간다.','',
           '| 마을 | 상품 | 현재 매입 | 평판만 | 평판+최대 흥정 | 현재 판매 |','|---|---|---:|---:|---:|---:|']
    for r in d['quotes']:
        if r['policy']=='small' and r['stage']==3 and r['town'] in ('naru','gaegyeong'):
            rows.append(f'| {towns[r["town"]]["displayName"]} | {goods[r["item"]]["displayName"]} | {r["buyBase"]} | {r["buyRep"]} | {r["buyMaxHaggle"]} | {r["sellBase"]} |')
    for p in ['small','example','dual']:
        q=[r for r in d['quotes'] if r['policy']==p and r['stage']==3]
        noop=sum(r['buyRep']==r['buyBase'] for r in q)
        rows.append(f'\n- {LABELS[p]} 우호의 매입 보정은 정상재고56단가 중 {noop}곳에서 반올림으로 효과가 없다. 주문 단위 합계 반올림을 채택하면 주문 쪼개기 유인이 생길 수 있어, 이번 비교는 본편처럼 개당 계산했다.')
    rows+=['','## Claude·사용자가 결정할 것 (D10)','','1. 할인/판매 가산/흥정을 어느 방향에 얼마나 겹칠지. 참고 후보 모두 위험 조합이 있어 그대로 확정할 근거가 아니다. 흥정 매입 전용·합산 할인 한도·장터 재판매 보호 또는 비가격 보상 중심안을 비교할 수 있지만 여기서 채택하지 않는다.',
           '2. 희귀품 분류/단계와 수도권 고급 시설 목록. 현재 상품에는 평판 잠금 필드/희귀 분류가 없다. 나전칠기처럼 값이 비싸다는 이유만으로 잠그지 않았다. 중립 필수 거래를 보장하고 우호 시작만으로 희귀 보상이 모두 열리지 않아야 한다.',
           '3. 현행 보관소 신용3 등 계약 조건은 유지한다. 기존 시설의 신용 조건을 평판으로 대체하지 않았다. 별도 고급 시설의 평판 자격은 미설계다.',
           '4. 평판 기여 점수/반복 매매 제한/단계 필요점수·기부/속죄 수치. R6 기여 점수0과 기존 저장을 유지했다. 가격 후보 비교로 이 수치를 추정/확정하지 않는다.',
           '5. 상인 성격/거래량/성공확률·실패/재시도 제한의 실제 체감. 이 자료는 가격 상한의 비교이며 실제 이동/플레이 또는 흥정 시스템 구현이 아니다.','',
           '## 재실행과 범위','','```sh','dotnet run --project Tools/Balance/R2Probe -- Docs/Validation/R2-2026-10-25/source-snapshot.json Docs/Validation/R2-2026-10-25/calculations.json','python Tools/Balance/report_r2.py','```','',
           'snapshot은 Tools/Balance/export_r2_snapshot.cs.txt를 읽기 전용 Unity Edit mode에서 실행해 내보냈다. 실제 에셋이 달라지면 먼저 snapshot을 다시 만든다. input-hashes.json은 이번 입력/생산 코드 해시다. `.NET`은 검사 프로그램 실행이며 Unity 게임 빌드가 아니다. 본편/ID/저장·Claude 담당 설계 문서는 수정하지 않았다. R2 수치 계산 단계만 완료, R2 런타임 구현은 대기다. D10 3절 수치는 수신했고 안전 검산 결과에 따른 보완 판단은 후속 자료에 있다.']
    (OUT/'README.md').write_text('\n'.join(rows)+'\n')

if __name__=='__main__':summarize()
