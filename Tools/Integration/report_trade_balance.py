"""Unity 실제 가격/보수 시나리오에 화물별 정적 길 비용을 결합한다. 실제 플레이 시간 아님.
Unity: Pocket Caravan > Tests > 고려 배달·교역 균형 검사
python3 Tools/Integration/report_trade_balance.py output/goryeo-economy/balance-data.json > report.csv
"""
import csv,json,sys
from functools import lru_cache
from pathlib import Path
from analyze_journey import cost
MAPS={'naru':'Naru','gaegyeong':'Gaegyeong','dakmok':'Dakmok'}

@lru_cache(None)
def seconds(a,b,vehicle,weight):
    return cost(MAPS.get(a,a),MAPS.get(b,b),{'walk':'Walk','handcart':'Handcart'}[vehicle],weight)['seconds']

def rows(source):
    for original in source['rows']:
        row=dict(original)
        row['tradeProfit']=row['revenue']-row['cost']
        row['calculatedRoundTripSeconds']=round(seconds(row['from'],row['to'],row['vehicle'],row['outwardWeight'])+seconds(row['to'],row['from'],row['vehicle'],row['returnWeight']),1)
        row['calculatedProfitPerMinute']=round(row['profit']*60/row['calculatedRoundTripSeconds'],2)
        yield row

if __name__=='__main__':
    result=list(rows(json.loads(Path(sys.argv[1]).read_text())))
    writer=csv.DictWriter(sys.stdout,fieldnames=list(result[0]),lineterminator='\n');writer.writeheader();writer.writerows(result)
