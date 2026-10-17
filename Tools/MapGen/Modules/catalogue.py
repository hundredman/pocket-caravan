"""40개 수정 가능한 지형 원형. 좌표는 40x40, 연결부는 19~21의 3칸."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
DEST=ROOT/'Assets/PocketCaravan/MapModules/Sources'


def definitions():
    out=[]
    def add(id,label,group,skin,shape='straight',**kw):
        out.append(dict(id=id,label=label,category=group,environment=skin,shape=shape,seed=300+len(out),**kw))
    add('meadow_lane','초원 큰길','초원/농경','meadow',flowers=45)
    add('flower_fork','꽃밭 갈림길','초원/농경','meadow','fork',flowers=100)
    add('wheat_crossing','밀밭 사거리','초원/농경','meadow','cross',fields=True)
    add('orchard_hamlet','과수원 마을','마을','meadow','village',trees='fruitbig',houses=4,fields=True)
    add('pine_corridor','침엽수 숲길','숲','pine','straight',trees='pinebig',density=44)
    add('deep_forest_fork','깊은 숲 갈림길','숲','mistwood','fork',trees='roundbig',density=52)
    add('logging_camp','벌목 야영지','마을','pine','camp',trees='pinebig',density=28,houses=2)
    add('forest_clearing','숲속 빈터','숲','mistwood','loop',trees='roundbig',density=40)
    add('autumn_lane','낙엽 숲 구불길','숲','autumn','bend',trees='fruitbig',density=36)
    add('hill_switchback','구릉 지그재그길','산지','steppe','switchback',hills=True,rocks=12)
    add('mountain_pass','산 고개 협로','산지','alpine','choke',hills=True,trees='pinebig',density=18,rocks=14)
    add('ridge_loop','능선 순환길','산지','alpine','loop',hills=True,rocks=14)
    add('rocky_choke','바위 협곡','산지','canyon','choke',rocks=35)
    add('river_crossing','하천 단일 다리','하천','meadow','straight',river='vertical',bridges='one')
    add('river_bends','물굽이 우회길','하천','wetland','bend',river='vertical',bridges='one',trees='roundbig',density=16)
    add('river_island','하중도 쉼터','하천','wetland','island',river='wide',bridges='two',houses=1)
    add('bridge_pair','두 다리 선택로','하천','meadow','split',river='vertical',bridges='split')
    add('wetland_boardwalk','습지 목도','습지','wetland','straight',ponds='marsh',boardwalk=True,reeds=True)
    add('reed_marsh','갈대 습지 순환길','습지','wetland','loop',ponds='center',reeds=True)
    add('lake_shore','호숫가 곡선길','호수','meadow','shore',ponds='lake',trees='roundbig',density=14)
    add('lakeside_hamlet','호반 마을','마을','meadow','shore',ponds='lake',houses=3)
    add('coastal_road','해안 절벽길','해안','coast','coast',sea=True,rocks=12)
    add('coastal_harbor','해안 항구 마을','마을','coast','harbor',sea=True,houses=4,dock=True)
    add('estuary','하구 다리길','해안','coast','straight',river='vertical',bridges='one',sea=True)
    add('fishing_hamlet','어촌','마을','coast','harbor',sea=True,houses=3,dock=True,boats=True)
    add('steppe_crossroads','초원 교차로','초원/농경','steppe','cross',rocks=5)
    add('grassland_camp','초원 야영지','마을','steppe','camp',houses=2,rocks=8)
    add('desert_oasis','사막 오아시스','건조지','desert','loop',ponds='center',houses=2,cacti=15)
    add('dune_detour','사구 우회로','건조지','desert','split',hills=True,cacti=28)
    add('canyon_bridge','협곡 가교','건조지','canyon','straight',river='vertical',bridges='one',rocks=26)
    add('canyon_switchback','붉은 협곡 굽잇길','건조지','canyon','switchback',hills=True,rocks=32)
    add('snow_pass','설산 고개','한랭지','snow','choke',trees='pinebig',density=25,rocks=18)
    add('tundra_camp','툰드라 야영지','한랭지','tundra','camp',houses=2,rocks=18)
    add('frozen_lake','빙설 호수 둘레길','한랭지','snow','loop',ponds='center',trees='pinebig',density=16)
    add('volcanic_detour','화산암 지대 우회로','화산/유적','volcanic','split',ponds='center',rocks=35)
    add('ruin_courtyard','유적 마당','화산/유적','canyon','ruin',ruins=True,rocks=15)
    add('caravanserai','대상 숙영지','마을','desert','village',houses=5,cacti=10)
    add('border_checkpoint','길목 관문','시설/길목','alpine','choke',houses=2,rocks=18)
    add('market_crossroads','교차 장터','마을','meadow','cross',houses=5,fields=True)
    add('hill_village','산록 마을','마을','autumn','village',houses=4,hills=True,trees='roundbig',density=12)
    return out


def write_sources():
    DEST.mkdir(parents=True,exist_ok=True)
    for d in definitions():
        p=DEST/(d['id']+'.json')
        # Only initialize missing source files; hand edits are the authoring source of truth.
        if not p.exists():p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')

if __name__=='__main__':write_sources()
