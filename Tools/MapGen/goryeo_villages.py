"""수도 밖 추가 두 마을. 실지명은 모티브이며 배치/생산은 프로토타입 각색."""
from tiles import Map, PROPS


def songak():
    m = Map('goryeo_songak', '송악 산록 마을', 40, 28, seed=71)
    m.road_path([(11, 27), (11, 19), (18, 14), (27, 14)], width=3, grade=1)
    m.road_blob(21, 14, 7, 3, grade=1)
    m.link('S', 11, 3, 'Gaegyeong', 'N', 'S')
    m.link('E',14,3,'goryeo_ridge','W','E')
    m.link('W',14,3,'transit_pass','E','W')
    m.road_path([(0,15),(11,15),(18,14)],width=3,grade=2)
    m.road_path([(27,14),(39,15)],width=2,grade=1)
    # 북쪽 높은 숲이 마을을 받치는 산록 지형. 통행로와 분리한다.
    m.plateau(3, 0, 12, 5)
    m.plateau(22, 0, 36, 3)
    for name, x, y in [('house_thatch', 5, 9), ('house_plain', 16, 5), ('house_thatch', 28, 7), ('house_plain', 29, 19)]:
        m.stamp(name, x, y)
    m.road_path([(18, 9), (18, 14)], width=1)
    m.road_path([(7, 13), (11, 13), (15, 14)], width=1)
    m.road_path([(30, 11), (30, 14), (26, 14)], width=1)
    m.road_path([(30, 23), (26, 23), (26, 16)], width=1)
    m.board(25, 12, 'goryeo_songak')
    m.add('market', 22, 15, town='goryeo_songak')
    m.sign(13, 20, '송악 산록 마을\n서쪽 고개는 대륙 내륙 방면, 남쪽은 개경, 동쪽은 닥목 산록 길이다. 종이와 생활재를 싣고 돌아갈 거래처를 살펴보자.')
    m.sign(26, 13, '약재 집산 마당\n송악산 지명을 참고한 가상 마을이다. 물품 구성과 길은 게임을 위한 각색이다.')
    m.add('npc',20,11,npc='goryeo_songak_merchant')
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
    m.road_path([(20, 0), (20, 12), (28, 14)], width=3, grade=2)
    m.road_blob(21, 14, 8, 3, grade=2)
    m.link('N', 19, 3, 'goryeo_fieldroad', 'S', 'N')
    for name,x,y in [('house_thatch',7,7),('house_plain',24,4),('house_thatch',30,7),('house_plain',29,23)]:
        m.stamp(name,x,y)
    m.road_path([(9,11),(9,16),(15,16)],width=1)
    m.road_path([(25,8),(25,13)],width=1)
    m.road_path([(32,11),(32,14),(27,14)],width=1)
    m.road_path([(30,26),(26,26),(26,16)],width=1)
    for x,y in [(4,3),(8,3),(12,3),(4,23),(8,23),(12,23)]:m.stamp('wheat',x,y)
    m.stamp('sheaf',15,8);m.stamp('hay',16,8)
    m.stamp('cart',34,23)
    m.board(24,12,'goryeo_jangdan')
    m.add('market',21,15,town='goryeo_jangdan')
    m.sign(7,20,'북쪽: 닥나무 마을 · 개경 / 대륙길: 개경 북쪽 송악 산록 경유\n곡식과 땔감은 무겁다. 적재량과 귀환 물건을 함께 정하자.')
    m.sign(27,12,'장단 들마을\n고려 지리지의 장단 지명을 참고했다. 농업 장터의 모습과 교역 역할은 각색이다.')
    m.add('npc',18,11,npc='goryeo_jangdan_farmer')
    m.stamp('well',34,16)
    m.forest_border(thickness=2,big=('roundbig','fruitbig'))
    m.flowers(3,3,36,24,10)
    m.finish()
    return m
