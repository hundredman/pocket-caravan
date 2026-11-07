"""Team-facing demo proposal, source notes in Docs/Goryeo/Demo-Scenario-2026-11-07.md."""
from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.colors import HexColor
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'output/pdf/PocketCaravan-Demo-Team-2026-11-07.pdf'
FONT='/System/Library/Fonts/Supplemental/AppleGothic.ttf'
pdfmetrics.registerFont(TTFont('KR',FONT))
c=canvas.Canvas(str(OUT),pagesize=(595.28,841.89));c.setTitle('Pocket Caravan | 5분 데모 유저 경험 제안');c.setAuthor('Pocket Caravan · 은샘')
style=ParagraphStyle('body',fontName='KR',fontSize=10,leading=16,textColor=HexColor('#293630'),wordWrap='CJK')
def para(text,x,y,w=491,size=10,color='#293630'):
 s=ParagraphStyle('p',parent=style,fontSize=size,leading=size*1.6,textColor=HexColor(color));p=Paragraph(text,s);_,h=p.wrap(w,750);p.drawOn(c,x,y-h);return y-h

def header(n,title,sub):
 c.setFillColor(HexColor('#315749'));c.rect(0,828,596,14,fill=1,stroke=0)
 para('POCKET CARAVAN  /  팀 공유 제안',52,793,size=10,color='#547467')
 para(title,52,759,size=22)
 para(sub,52,715,size=10,color='#5A655F')
 c.setStrokeColor(HexColor('#D3DDD6'));c.line(52,685,543,685)
 c.setFont('KR',9);c.setFillColor(HexColor('#69736D'));c.drawString(52,34,'은샘 · 2026.11.07 · 팀 합의 / 리허설 전');c.drawRightString(543,34,f'{n} / 2')
header(1,'첫 장사에서 다음 교역으로','직접 플레이하는 유저의 약 5분 경험 · 이후에도 같은 캐릭터로 계속 진행')
y=para('유저가 물건을 조금 사고, 가까운 마을까지 운반한 뒤 팔아 보는 흐름입니다. 한 번의 교역을 스스로 이해하고 다음 거래를 고르는 것이 목표입니다.',52,662,size=11)
y=para('지역별 퀘스트 전체를 새로 만드는 작업이나 발표자 대본이 아닙니다. 아래 시간은 설계 목표이며, 실제 처음 하는 사람의 플레이 시간은 아직 측정하지 않았습니다.',52,y-13,size=9,color='#5A655F')
rows=[('0:00 - 0:40','첫 목표를 찾기','문명 선택 후 가까운 안내 NPC를 만납니다. 새내기 상인이라는 상황과 첫 장터만 짧게 알려 줍니다.'),('0:40 - 1:30','작게 사기','장터에서 감당할 수 있는 물건 1~3개를 삽니다. 총액·남는 돈·짐 무게를 확인합니다.'),('1:30 - 2:00','목적지 고르기','지도와 길 안내로 가까운 이웃 마을을 찾습니다. 첫 이동부터 긴 우회나 위험 사건을 필수로 두지 않습니다.'),('2:00 - 3:30','직접 운반하기','길을 따라 다음 마을로 이동하며 풍경과 적재·이동의 차이를 경험합니다.'),('3:30 - 4:30','팔고 기록 보기','도착 장터의 값을 보고 판매 또는 일부 보유를 고릅니다. 장부에서 실제 거래 결과를 확인합니다.'),('4:30 - 5:00','다음 목표 남기기','다음 교역이나 기존 의뢰, 중앙 대륙으로 향하는 길을 확인합니다. 종료 팝업이나 강제 시간 제한을 두지 않습니다.')]
y-=20
for tm,title,body in rows:
 c.setFillColor(HexColor('#ECF1EC'));c.roundRect(52,y-23,98,23,4,fill=1,stroke=0)
 para(tm,59,y-4,w=90,size=9,color='#315749');para(title,166,y,w=375,size=11)
 bottom=para(body,166,y-25,w=375,size=9);y=bottom-18
c.showPage()
header(2,'문서 다음에 함께 맞출 것','기존 게임을 활용하되, 첫 플레이 경험으로 확인해야 합니다')
y=para('고려 적용 예시',52,661,size=14)
y=para('예성 나루 → 장단에서 첫 거래와 운반을 경험합니다. 이후 송악·닥목·개경의 기존 의뢰나 중앙 대륙 교역으로 이어집니다. 기존 소금 배달 의뢰(q1_naru)는 유지하며, 데모 거래와 안내가 충돌하지 않도록 맞춥니다.',52,y-10)
y=para('다른 문명도 같은 “준비 → 소량 매입 → 가까운 운반 → 거래 결과 → 다음 목표” 구조를 사용합니다. 실제 마을·품목·수량은 각 지역 담당자가 현재 구현과 시세로 확인합니다. 특정 수익을 보장하는 구성은 아닙니다.',52,y-10)
y=para('문서만 만들면 끝인가요?',52,y-24,size=14)
for title,body in [('1. 팀이 경로와 안내 범위를 정하기','각 지역의 출발·도착 마을, 추천 품목, 첫 목표를 알려 줄 NPC를 정합니다. 기존 8단계 조작 튜토리얼 중 첫 5분에 무엇을 보여 줄지도 맞춥니다.'),('2. 현재 구현과 차이만 반영하기','기존 거래·장부·지도·퀘스트를 먼저 활용합니다. 안내가 빠지거나 NPC 호칭이 다른 부분을 목록화하고 담당을 정합니다. 새 데모 퀘스트나 별도 서버 기능은 아직 추가하지 않았습니다.'),('3. 처음 하는 사람이 실제로 플레이하기','첫 매입·도착·판매 시각과 도움을 요청한 횟수, 막힌 위치를 기록합니다. 5분 이후에도 같은 캐릭터와 저장으로 이어지는지 확인합니다.')]:
 y=para(title,52,y-14,size=11);y=para(body,52,y-6,size=9)
y=para('5분 이후와 경매의 위치',52,y-23,size=14)
y=para('더 먼 교역, 게시판 의뢰, 운반 수단 업그레이드로 자연스럽게 이어 갑니다. 다른 유저와의 만남은 실제 온라인 환경에서 별도 리허설이 필요합니다. 경매는 회의에서 나온 아이디어로 남기며, 포함 여부·규칙·담당이 정해지기 전에는 구현 완료 기능으로 소개하지 않습니다.',52,y-9,size=9)
y=para('리허설 기록: 통합 커밋 / 기기 / 문명 / 첫 매입·도착·판매 시간 / 도움 횟수 / 막힌 곳 / 다음에 하고 싶은 일',52,y-19,size=9,color='#547467')
assert y>65,y
c.save();print(OUT)
