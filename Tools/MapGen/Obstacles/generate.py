"""장애 프리셋의 수정 원본과 임시 픽셀 소품. 본편에 자동 배치하지 않는다."""
import json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
BASE=ROOT/'Assets/PocketCaravan/Obstacles'

def defaults():
    out=[]
    def add(id,label,group,visual,mode='Barrier',action='None',**kw):
        d=dict(id=id,label=label,category=group,visual=visual,mode=mode,action=action,width=3,height=3,walkSpeed=1,cartSpeed=1,traction=1,driftX=0,driftY=0,staminaDrain=0,visibility=1,workSeconds=0,money=0,itemId='',itemCount=0,activeSeconds=6,openSeconds=5,threatSpeed=2.7,threatRadius=6,leash=8,weightLimit=12,description='')
        d.update(kw);out.append(d)
    add('fallen_log','쓰러진 통나무','잔해/식생','log',action='Clear',workSeconds=3,description='통나무를 치우거나 옆길로 우회한다.')
    add('branch_pile','부러진 가지 더미','잔해/식생','branches',action='Clear',workSeconds=1.5)
    add('boulder','길막 바위','잔해/식생','rocks',action='Clear',workSeconds=6)
    add('rockslide','낙석 더미','잔해/식생','rocks',action='Clear',workSeconds=7,width=4)
    add('landslide','산사태 토사','잔해/식생','earth',action='Clear',workSeconds=8,width=5)
    add('avalanche','눈사태 잔해','잔해/식생','snow',action='Clear',workSeconds=7,width=5)
    add('thorn_brush','가시덤불','잔해/식생','thorns',action='Clear',workSeconds=3)
    add('fallen_bamboo','쓰러진 대나무','잔해/식생','branches',action='Clear',workSeconds=2.5)
    add('broken_cart','고장 난 수레','잔해/식생','wagon',action='Clear',workSeconds=4)
    add('abandoned_cargo','버려진 짐 더미','잔해/식생','crates',action='Clear',workSeconds=2)
    add('sinkhole','꺼진 지반','잔해/식생','pit',description='영구 차단. 돌아갈 길을 찾아야 한다.')
    add('crevasse','빙하 균열','잔해/식생','icepit',width=2,height=5)
    add('river_flood','강 범람','하천/교량','flood','TimedBarrier',activeSeconds=9,openSeconds=6,width=5)
    add('flash_flood','급류 범람','하천/교량','flood','TimedBarrier',activeSeconds=4,openSeconds=3,width=4)
    add('high_tide','만조 잠김','하천/교량','flood','TimedBarrier',activeSeconds=12,openSeconds=8,width=5)
    add('broken_bridge','끊어진 다리','하천/교량','bridge',action='Repair',workSeconds=5,itemId='firewood',itemCount=2,description='땔감2개로 임시 보수하거나 우회한다. 수리 재료 사용은 명시적으로 선택한다.')
    add('rotten_planks','썩은 교량 판자','하천/교량','planks','LoadGate',action='Repair',workSeconds=3,itemId='firewood',itemCount=1,weightLimit=8)
    add('drawbridge','닫힌 도개교','하천/교량','gate','TimedBarrier',activeSeconds=6,openSeconds=7)
    add('ford_current','얕은 여울 물살','하천/교량','flood','Slow',walkSpeed=.65,cartSpeed=.4,driftX=0,driftY=-1.1)
    add('washed_road','유실된 길','하천/교량','earth',action='Repair',workSeconds=5,itemId='firewood',itemCount=1)
    add('mud','진흙탕 길','노면','mud','Slow',walkSpeed=.8,cartSpeed=.4,width=5)
    add('deep_mud','깊은 진창','노면','mud','Slow',walkSpeed=.6,cartSpeed=.22,width=5)
    add('puddle','물 고인 길','노면','flood','Slow',walkSpeed=.9,cartSpeed=.65)
    add('loose_sand','푹신한 모랫길','노면','sand','Slow',walkSpeed=.78,cartSpeed=.45,width=5)
    add('quicksand','모래 수렁','노면','quicksand','Slow',walkSpeed=.45,cartSpeed=.2,width=4)
    add('deep_snow','쌓인 눈','노면','snow','Slow',walkSpeed=.65,cartSpeed=.38,width=5)
    add('ice','얼어붙은 노면','노면','ice','Slow',walkSpeed=.95,cartSpeed=.85,traction=.08,width=6,description='가속/제동이 약해진다. 전복/화물 낙하는 없다.')
    add('rubble','자갈·돌부스러기','노면','rocks','Slow',walkSpeed=.9,cartSpeed=.55)
    add('steep_slope','가파른 오르막','노면','stairs','Slow',walkSpeed=.8,cartSpeed=.42,staminaDrain=.22)
    add('narrow_boardwalk','좁은 목도','노면','planks','VehicleGate',description='등짐은 통과. 손수레는 우회한다.')
    add('bandit','산적 습격','위협','enemy','Threat',action='Pay',money=12,walkSpeed=.5,cartSpeed=.45)
    add('water_pirate','수적 상륙 위협','위협','boat','Threat',action='Pay',money=15,walkSpeed=.55,cartSpeed=.45,threatSpeed=0,threatRadius=5,description='배는 정박 상태이며 강변 접근을 견제한다. 선박 전투/승선은 제외.')
    add('patrol_ambush','매복 순찰대','위협','enemy','Threat',action='Pay',money=18,threatSpeed=3.1,walkSpeed=.45,cartSpeed=.4)
    add('wolf_pack','들개·늑대 무리','위협','animal','Threat',threatSpeed=3,walkSpeed=.55,cartSpeed=.5)
    add('wild_boar','성난 멧돼지','위협','animal','Threat',threatSpeed=2.4,walkSpeed=.5,cartSpeed=.6,leash=5)
    add('wasp_nest','벌집 구간','위협','wasps','Slow',action='Clear',workSeconds=4,walkSpeed=.65,cartSpeed=.8,staminaDrain=.2)
    add('toll_gate','통행세 초소','사람/교통','checkpoint','Toll',action='Pay',money=8)
    add('checkpoint','검문 대기','사람/교통','checkpoint','TimedBarrier',activeSeconds=7,openSeconds=6)
    add('livestock_crossing','가축 횡단','사람/교통','animal','TimedBarrier',activeSeconds=4,openSeconds=7,width=4)
    add('caravan_queue','대상 행렬 정체','사람/교통','wagon','TimedSlow',walkSpeed=.55,cartSpeed=.3,activeSeconds=8,openSeconds=5,width=5)
    add('strong_wind','강한 맞바람','기상/자연','wind','Slow',walkSpeed=.9,cartSpeed=.8,driftX=-1.1,staminaDrain=.18,width=5)
    add('sand_gust','모래 돌풍','기상/자연','sand','TimedSlow',walkSpeed=.7,cartSpeed=.6,visibility=.5,activeSeconds=6,openSeconds=5,driftX=-.6)
    add('forest_fire','산불 구간','기상/자연','fire','TimedBarrier',activeSeconds=10,openSeconds=6,width=4,description='활성 구간을 차단하는 시험. 확산/소화 시뮬레이션과 HP 피해는 미구현.')
    add('smoke','짙은 연기','기상/자연','smoke','Slow',walkSpeed=.75,cartSpeed=.7,visibility=.45,staminaDrain=.25,width=5)
    add('falling_rocks','간헐적 낙석','기상/자연','rocks','TimedBarrier',activeSeconds=3,openSeconds=5,width=3,height=4)
    add('thin_ice','얇은 얼음 다리','기상/자연','ice','LoadGate',weightLimit=5,description='짐 무게5 이하면 통과. 파손·추락 대신 진입을 제한한다.')
    add('fishing_net','길을 가로막은 그물','시설/공사','net',action='Clear',workSeconds=2.5)
    add('roadworks','도로 공사','시설/공사','barricade','TimedBarrier',activeSeconds=10,openSeconds=4,width=4)
    assert len(out)==48
    return out

def art(kind):
    im=Image.new('RGBA',(64,48));d=ImageDraw.Draw(im)
    d.ellipse((3,32,61,46),fill='#263e342d')
    if kind in ['log','branches','thorns']:
        if kind=='log':
            d.rounded_rectangle((3,14,59,35),5,fill='#61412d',outline='#382f26',width=2);d.ellipse((1,14,17,35),fill='#d2a36a',outline='#6b5035',width=2);d.ellipse((5,18,13,31),outline='#97704a',width=2)
            for y in [18,24,30]:d.line((19,y,57,y+1),fill='#936b42',width=2)
        else:
            for x,y in [(7,17),(18,25),(34,10)]:
                d.line((x,y,x+22,y+15),fill='#65503a',width=4);d.line((x+8,y+5,x+3,y-2),fill='#69563b',width=3)
            if kind=='thorns':
                for x in range(7,54,9):d.polygon([(x,27),(x+4,9),(x+8,29)],fill='#526943')
    elif kind in ['rocks','earth','snow']:
        colors=['#81877b','#596355','#a5ae9c'] if kind=='rocks' else (['#7e6045','#644b38','#b18c61'] if kind=='earth' else ['#dce4e7','#a9bec5','#f0f1e3'])
        for x,y,s in [(7,23,20),(24,11,25),(43,25,17)]:
            d.polygon([(x,y+s//2),(x+s//4,y),(x+3*s//4,y+1),(x+s,y+s//2),(x+s-3,y+s),(x+3,y+s)],fill=colors[0],outline=colors[1]);d.line((x+4,y+s//2,x+s//3,y+3,x+2*s//3,y+4),fill=colors[2],width=2)
    elif kind in ['bridge','planks','gate','barricade','checkpoint']:
        for y in [7,35]:d.rectangle((3,y,60,y+4),fill='#4e4938')
        for x in range(6,60,8):
            if kind=='bridge' and 24<x<42:continue
            d.rectangle((x,8,x+6,35),fill='#b28a5d',outline='#71533d')
        if kind in ['barricade','checkpoint','gate']:
            d.rectangle((7,7,11,43),fill='#5d5141');d.rectangle((52,7,56,43),fill='#5d5141');d.line((8,13,54,31),fill='#c8a16c',width=5)
    elif kind in ['wagon','crates']:
        d.rectangle((9,12,54,34),fill='#ab794f',outline='#5e4535',width=2)
        for x in range(10,52,10):d.line((x,14,x,31),fill='#d1a676',width=2)
        d.line((10,31,52,13),fill='#6c4d36',width=3)
        if kind=='wagon':
            for x in [14,44]:d.ellipse((x-5,31,x+8,44),fill='#403d34',outline='#b09263',width=2)
    elif kind in ['pit','icepit','quicksand']:
        c='#8da9af' if kind=='icepit' else '#3b4040' if kind=='pit' else '#b89561'
        d.polygon([(4,16),(24,9),(41,13),(61,25),(49,41),(21,39),(3,29)],fill=c,outline='#777963',width=3)
        for r in [5,10,16]:d.arc((32-r,24-r//2,32+r,24+r//2),0,320,fill='#52574a',width=2)
    elif kind in ['flood','mud','sand','ice','stairs','wind','smoke']:
        bg={'flood':'#72b8c9b0','mud':'#795b3ca0','sand':'#ddba78a0','ice':'#a6d5e6c0','stairs':'#8c8678c0','wind':'#bccfd044','smoke':'#858e9688'}[kind]
        d.rounded_rectangle((1,7,62,43),8,fill=bg)
        for i in range(7):
            x=4+(i*11)%48;y=12+(i*7)%27
            d.line((x,y,x+10,y-2 if kind=='ice' else y),fill='#e5e0b69c' if kind!='mud' else '#b48b5090',width=2)
        if kind=='stairs':
            for y in range(9,40,6):d.line((5,y,59,y),fill='#5f675c',width=2)
    elif kind=='boat':
        d.polygon([(2,29),(61,29),(48,43),(15,43)],fill='#815735',outline='#433c32',width=2);d.rectangle((30,4,32,28),fill='#5b4a32');d.polygon([(32,5),(51,24),(32,24)],fill='#dad3a8');d.line((9,33,53,33),fill='#b88a50',width=2)
    elif kind=='enemy':
        for x,y in [(12,9),(35,16)]:
            d.ellipse((x,y,x+15,y+14),fill='#b8a67c',outline='#353a3c');d.rectangle((x,y+6,x+15,y+11),fill='#383c48');d.rectangle((x+2,y+14,x+14,y+26),fill='#6c5160');d.line((x+17,y+8,x+21,y+25),fill='#c7cdd0',width=3)
    elif kind=='animal':
        for x,y in [(4,16),(32,11)]:
            d.ellipse((x,y,x+25,y+18),fill='#8c8270',outline='#494d46');d.polygon([(x+17,y+3),(x+20,y-4),(x+23,y+3)],fill='#655b4e');d.rectangle((x+3,y+14,x+7,y+24),fill='#4d4c44');d.rectangle((x+18,y+13,x+22,y+23),fill='#4d4c44');d.point((x+23,y+8),fill='#e4b17c')
    elif kind=='wasps':
        d.ellipse((23,10,42,35),fill='#c9aa69',outline='#786342',width=2)
        for x,y in [(5,7),(46,21),(12,33)]:d.ellipse((x,y,x+9,y+5),fill='#d7b638');d.line((x+4,y,x+4,y+5),fill='#3d4140',width=2)
    elif kind=='net':
        for x in range(-20,70,8):d.line((x,7,x+35,40),fill='#a49e72',width=2);d.line((x,40,x+35,7),fill='#9a9b78',width=2)
    elif kind=='fire':
        for x,y in [(3,15),(23,4),(42,16)]:d.polygon([(x,40),(x+3,y+6),(x+10,y),(x+19,y+18),(x+18,40)],fill='#ce613d');d.polygon([(x+5,39),(x+10,y+12),(x+15,39)],fill='#edc25f')
    return im

def generate():
    BASE.mkdir(parents=True,exist_ok=True);src=BASE/'definitions.json'
    if not src.exists():src.write_text(json.dumps(dict(schemaVersion=1,definitions=defaults()),ensure_ascii=False,indent=2)+'\n')
    defs=json.loads(src.read_text())['definitions'];directory=BASE/'Art';directory.mkdir(exist_ok=True)
    for kind in sorted({d['visual'] for d in defs}):art(kind).save(directory/(kind+'.png'))
    font=ImageFont.truetype(str(ROOT/'Assets/PocketCaravan/Resources/Fonts/Galmuri11.ttf'),13)
    sheet=Image.new('RGB',(960,8*106),'#263130');draw=ImageDraw.Draw(sheet)
    for i,d in enumerate(defs):
        x=i%6*160;y=i//6*106;sheet.paste(art(d['visual']).resize((112,72),Image.Resampling.NEAREST),(x+20,y+3),art(d['visual']).resize((112,72),Image.Resampling.NEAREST));draw.text((x+5,y+79),d['label'],font=font,fill='#eeebcf')
    out=ROOT/'output/obstacles';out.mkdir(parents=True,exist_ok=True);sheet.save(out/'catalogue.png');print(len(defs),'obstacle presets')
if __name__=='__main__':generate()
