"""수도 밖 추가 두 마을. 실지명은 모티브이며 배치/생산은 프로토타입 각색."""
from tiles import Map, PROPS


def songak():
    m = Map('goryeo_songak', '송악 산록 마을', 40, 28, seed=71)
    m.road_path([(11, 27), (11, 19), (18, 14), (27, 14)], width=3, grade=1)
    m.road_blob(21, 14, 7, 3, grade=1)
    m.link('S', 11, 3, 'Naru', 'N', 'S')
    for name, x, y in [('house_thatch', 5, 9), ('house_plain', 16, 5), ('house_thatch', 28, 7), ('house_plain', 29, 19)]:
        m.stamp(name, x, y)
    m.road_path([(18, 9), (18, 14)], width=1)
    m.road_path([(7, 13), (11, 13), (15, 14)], width=1)
    m.road_path([(30, 11), (30, 14), (26, 14)], width=1)
    m.road_path([(30, 23), (26, 23), (26, 16)], width=1)
    m.board(25, 12, 'goryeo_songak')
    m.add('market', 22, 15, town='goryeo_songak')
    m.sign(13, 20, '송악 산록 마을\n남쪽은 예성 나루. 종이와 생활재를 싣고 돌아갈 거래처를 살펴보자.')
    m.sign(26, 13, '약재 집산 마당\n송악산 지명을 참고한 가상 마을이다. 물품 구성과 길은 게임을 위한 각색이다.')
    m.talker(20, 11, '약재 장수', 'villager_rabbit_tall', '산에서 모인 짐과 장터에 들어온 약재를 골라 묶고 있어요. 개경 인삼도 이곳에서 거래하지요.', '귀한 짐을 적게 싣고 샛길을 택할지, 수레로 여러 물건을 나를지 정해 보세요.')
    for x,y in [(28,17),(29,17),(19,10)]: m.prop(PROPS[1], x,y)
    m.stamp('well', 33, 12)
    m.bushes([(5,18),(6,18),(31,5),(32,5)])
    m.forest_border(thickness=2, big=('pinebig','pinebig','roundbig'))
    m.scatter('pinebig', 4, area=(2,2,35,23), margin=1)
    m.flowers(3,3,36,24,12)
    m.finish()
    return m


def jangdan():
    m = Map('goryeo_jangdan', '장단 들마을', 40, 28, seed=81)
    m.road_path([(0, 19), (10, 19), (16, 14), (28, 14)], width=3, grade=2)
    m.road_blob(21, 14, 8, 3, grade=2)
    m.link('W', 19, 3, 'Dakmok', 'E', 'W')
    m.road_path([(28,19),(39,19)],width=3,grade=2)
    m.road_path([(28,14),(28,19)],width=3,grade=2)
    m.link('E',19,3,'transit_pass','W','E')
    for name,x,y in [('house_thatch',7,7),('house_plain',18,5),('house_thatch',30,7),('house_plain',29,23)]:
        m.stamp(name,x,y)
    m.road_path([(9,11),(9,16),(15,16)],width=1)
    m.road_path([(20,9),(20,13)],width=1)
    m.road_path([(32,11),(32,14),(27,14)],width=1)
    m.road_path([(30,26),(26,26),(26,16)],width=1)
    for x,y in [(4,3),(8,3),(12,3),(4,23),(8,23),(12,23)]:m.stamp('wheat',x,y)
    m.stamp('sheaf',15,8);m.stamp('hay',16,8)
    m.stamp('cart',34,23)
    m.board(24,12,'goryeo_jangdan')
    m.add('market',21,15,town='goryeo_jangdan')
    m.sign(7,20,'← 닥나무 마을 · 개경\n곡식과 땔감은 무겁다. 적재량과 귀환 물건을 함께 정하자.')
    m.sign(27,12,'장단 들마을\n고려 지리지의 장단 지명을 참고했다. 농업 장터의 모습과 교역 역할은 각색이다.')
    m.talker(18,11,'곡물 상인','villager_dog_tall','곡식과 땔감을 묶어 개경에 보내요. 돌아올 때 종이와 소금을 함께 실으면 좋겠지요.', '많이 싣는 것과 많이 버는 것은 다르오. 팔 곳의 시세와 재고부터 보시오.')
    m.stamp('well',34,16)
    m.forest_border(thickness=2,big=('roundbig','fruitbig'))
    m.flowers(3,3,36,24,10)
    m.finish()
    return m
