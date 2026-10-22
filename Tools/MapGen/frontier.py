"""문명 사이 여정 비교용 가상 중간권. 역사적 국경/실제 거리 재현이 아니다."""
from tiles import Map


def pass_road():
    m = Map('transit_pass', '솔바람 고개', 56, 38, seed=101)
    m.link('E',4,3,'goryeo_jangdan','W','E')
    m.link('W',19,3,'transit_woodtown','E','W')
    m.road_path([(0,19),(7,19)],width=3,grade=3)
    m.road_path([(48,19),(50,19),(50,5),(55,5)],width=3,grade=3)
    m.road_path([(7,19),(48,19)], width=2, grade=1)
    m.road_path([(7,19),(7,24),(48,24),(48,19)], width=3, grade=3)
    m.plateau(18,5,39,13)
    for x,y in [(20,7),(26,6),(33,8)]:m.stamp('pinebig',x,y,force=True)
    m.road_path([(10,24),(10,34),(18,34)],width=2,grade=2)
    m.stamp('house_thatch',17,31)
    m.sign(5,17,'솔바람 고개\n곧장: 짧은 숲 오솔길 / 남쪽: 긴 수레 큰길.\n등짐과 손수레로 같은 짐을 싣고 비교해 보세요.')
    m.sign(43,17,'동쪽: 장단 들마을·고려 / 서쪽: 솔재 길목 마을·중앙 방면\n이곳은 다른 문명 사이를 잇는 가상 중간 구역입니다.')
    m.talker(12,34,'길손','villager_dog_tall','수레는 남쪽 큰길로 돌아갑니다. 가볍게 걸을 때는 숲길이 짧지요.','짐이 많을수록 느려집니다. 흔들려도 화물이 떨어지지는 않아요.')
    m.sign(31,28,'남쪽 큰길\n노면은 수레에 유리하지만 고개에는 도적이 나타날 수 있습니다. 북쪽 샛길·귀환을 함께 비교하세요.')
    m.bandit(40,28,radius=5)
    m.forest_border(thickness=2,big=('pinebig','pinebig','roundbig'))
    m.scatter('pinebig',28,area=(3,3,49,32),margin=1)
    m.flowers(3,15,50,34,12)
    m.finish(); return m


def woodtown():
    m = Map('transit_woodtown','솔재 길목 마을',40,30,seed=102)
    m.link('E',19,3,'transit_pass','W','E')
    m.link('W',19,3,'transit_wetroad','E','W')
    m.road_path([(39,20),(0,20)],width=3,grade=2)
    m.road_path([(5,19),(7,19)],width=3,grade=2)
    m.road_path([(7,19),(32,19)],width=3,grade=2)
    m.road_blob(21,19,7,4,grade=2)
    for house,x,y in [('house_thatch',8,10),('store',19,9),('house_plain',29,11)]:
        m.stamp(house,x,y)
        m.road_path([(x+1,y+3),(x+1,19)],width=1,grade=1)
    m.stamp('cart',28,24);m.stamp('well',16,15)
    m.board(25,17,'transit_woodtown');m.add('market',21,20,town='transit_woodtown')
    m.sign(5,17,'솔재 길목 마을\n고개를 넘은 뒤 쉬어 가는 작은 장터. 서쪽은 버들 물가 길, 동쪽은 고려로 돌아가는 고갯길입니다.')
    m.talker(23,15,'땔감 장수','villager_bear_tall','숲에서 모은 땔감을 내놓고 종이와 소금을 삽니다.','강변의 버들나루와 시세를 비교해 보세요. 싼 물건도 너무 많이 팔면 가격이 내려갑니다.')
    m.forest_border(thickness=2,big=('pinebig','roundbig'))
    m.scatter('pinebig',5,area=(3,3,32,24),margin=1)
    m.finish(); return m


def wetroad():
    m = Map('transit_wetroad','버들 물가 길',56,38,seed=103)
    m.river_v(26,29,wiggle=1,fixed_rows=range(9,23))
    m.link('E',19,3,'transit_woodtown','W','E')
    m.link('N',4,3,'transit_ferry','S','N')
    m.road_path([(5,0),(5,19),(7,19)],width=3,grade=3)
    m.road_path([(48,19),(55,19)],width=3,grade=3)
    m.road_path([(7,19),(48,19)],width=2,grade=1)
    m.road_path([(7,19),(7,13),(48,13),(48,19)],width=3,grade=3)
    m.bridge(24,13,31,15)
    m.bridge(24,19,31,20)
    for y in range(19,21):
        for x in range(24,32):m.road[y][x]=1
    m.water_blob(15,29,7,4)
    m.water_blob(42,28,6,4)
    m.sign(5,17,'버들 물가 길\n곧장: 좁은 물가 오솔길 / 북쪽: 돌아가는 큰 다리.\n물가 노면에서는 수레가 느려집니다. 화물 낙하는 없습니다.')
    m.sign(43,17,'북쪽: 버들나루·중앙 방면 / 동쪽: 솔재 길목 마을·고려\n다리 두 곳 모두 통행 가능합니다. 날씨에 따른 폐쇄는 아직 없습니다.')
    m.sign(33,11,'큰 다리\n적재 수레의 우회로. 물가 샛길보다 길지만 노면이 좋습니다.')
    m.add('ambience',28,26,id='river')
    m.forest_border(thickness=2,big=('roundbig','fruitbig'))
    m.scatter('roundbig',10,area=(3,3,48,32),margin=1)
    m.bushes([(12,25),(16,24),(39,24),(46,24)])
    m.finish(); return m


def ferry():
    m=Map('transit_ferry','버들나루 마을',40,32,seed=104)
    m.river_h(25,31,wiggle=1)
    m.road_path([(0,20),(20,20),(31,20)],width=3,grade=2)
    m.link('S',30,3,'transit_wetroad','N','S')
    m.link('W',19,3,'transit_central_approach','E','W')
    m.road_path([(31,31),(31,20)],width=3,grade=2)
    m.bridge(30,24,33,31)
    m.road_blob(21,19,7,3,grade=2)
    m.road_path([(24,19),(24,26)],width=3,grade=2)
    m.bridge(23,24,27,29,dock=True)
    m.stamp('boat',35,28)
    for house,x,y in [('house_thatch',7,9),('store',18,7),('house_plain',29,10)]:
        m.stamp(house,x,y);m.road_path([(x+1,y+3),(x+1,19)],width=1,grade=1)
    m.board(25,17,'transit_ferry');m.add('market',21,20,town='transit_ferry')
    m.stamp('cart',29,22);m.stamp('well',15,14)
    m.sign(5,17,'버들나루 마을\n서쪽: 중앙 진입로·중앙 길목 / 남쪽: 물가 길·고려\n선착장에는 승선할 수 없습니다. 중앙까지는 육로로 이동하세요.')
    m.talker(23,15,'나루 상인','villager_tanuki_tall','이곳에서는 곡식과 소금을 구할 수 있어요. 솔재의 땔감을 싣고 오는 이도 있지요.','선착장은 둘러볼 수 있지만 배를 타는 기능은 아직 없어요.')
    m.add('ambience',24,27,id='river')
    m.forest_border(thickness=2,big=('roundbig','fruitbig'))
    m.flowers(3,3,36,22,12)
    m.finish();return m


def central_approach():
    """East/Goryeo to west/central; road choices within an independently editable area."""
    m = Map('transit_central_approach', '중앙 진입로', 54, 38, seed=105)
    m.link('E', 19, 3, 'transit_ferry', 'W', 'E')
    m.link('W', 19, 3, 'transit_crossroads', 'E', 'W')
    m.road_path([(0,20),(7,20),(7,24),(35,24),(35,20),(53,20)], width=3, grade=3)
    m.road_path([(7,20),(35,20)], width=2, grade=1)
    m.road_path([(35,20),(35,13)],width=2,grade=2)
    m.road_blob(35,13,4,3,grade=3)
    m.water_blob(46,30,4,3)
    m.stamp('house_thatch',41,8)
    m.road_path([(43,11),(43,13),(35,13)],width=2,grade=2)
    m.stamp('cart',41,15)
    m.stamp('bench',41,12)
    m.sign(6,17,'중앙 진입로\n동쪽: 버들나루·고려 / 서쪽: 중앙 길목\n남쪽으로 굽은 큰길은 수레길, 곧장 난 오솔길은 도보 지름길입니다.')
    m.sign(29,6,'중앙 길목 방면\n서쪽으로 가면 중앙 권역에 도착합니다. 다른 문명의 교역로는 아직 연결되지 않았습니다. 고려는 동쪽 버들나루 방향입니다.')
    m.talker(43,15,'길손','villager_dog_tall','서쪽 길을 따라가면 중앙 길목의 쉼터가 나옵니다. 고려에서 여러 고개와 마을을 지나 여기까지 오셨군요.','짐수레는 단단한 큰길로 돌아갑니다. 가볍다면 오솔길을 살펴보세요.')
    m.forest_border(thickness=2,big=('pinebig','roundbig'))
    m.scatter('pinebig',12,area=(3,3,48,32),margin=1)
    m.flowers(30,19,48,32,12)
    m.finish(); return m


def central_crossroads():
    """Central region foothold, not a literal cross. Other chains are future content.

    The stable map id is historical; geographic direction is defined by exits/atlas,
    never inferred from economic coordinates or the internal id.
    """
    m = Map('transit_crossroads', '중앙 길목', 72, 56, seed=106)
    m.atlas_label = {'x': 33, 'y': 30}
    m.link('E',19,3,'transit_central_approach','W','E')
    # An arrival bend opens onto a sheltered stopping place; no fake foreign roads.
    m.road_path([(71,20),(57,20),(57,30),(33,30)],width=3,grade=3)
    m.road_blob(33,30,6,4,grade=2)
    m.road_path([(33,30),(24,30),(24,21),(32,21)],width=2,grade=1)
    m.stamp('house_thatch',29,16)
    m.stamp('cart',39,32)
    m.stamp('well',36,25)
    m.stamp('bench',29,25)
    m.water_blob(16,42,7,5)
    for x,y in [(8,7),(12,5),(16,7),(10,10),(18,10)]:
        m.stamp('pinebig',x,y)
    m.sign(59,17,'중앙 길목\n동쪽: 중앙 진입로 → 버들나루 → 솔재 길목 → 장단·고려\n고려는 이곳에서 동쪽에 있습니다.')
    m.sign(39,25,'교역로 안내\n현재는 동쪽 고려 교역로만 연결되었습니다. 북·서·남 권역은 여러 중간 지역으로 이어질 예정이며 아직 이동할 수 없습니다.')
    m.talker(34,26,'길목 안내인','villager_tanuki_tall','고려에서 고개와 물가 마을을 지나 중앙 권역에 도착하셨군요.','여기는 길손이 쉬는 작은 길목입니다. 다른 문명으로 가는 여정은 앞으로 이 주변의 여러 지역을 거쳐 이어집니다.')
    m.forest_border(thickness=2,big=('pinebig','roundbig'))
    m.scatter('roundbig',24,area=(3,3,66,50),margin=1)
    m.flowers(42,35,65,48,20)
    m.finish(); return m
