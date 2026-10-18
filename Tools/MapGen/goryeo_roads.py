"""고려의 산록 순환·들판 접속. 가격용 좌표와 독립적인 실제 지형 원본."""
from tiles import Map


def ridge():
    m = Map('goryeo_ridge', '송악 구릉길', 61, 28, seed=171)
    m.hours = 1
    m.link('W', 14, 3, 'goryeo_songak', 'E', 'W')
    m.link('S', 27, 3, 'goryeo_descent', 'N', 'S')
    m.road_path([(0,15),(16,15),(28,20),(28,27)],width=2,grade=1)
    m.road_path([(16,15),(18,8),(39,8)],width=1,grade=1)
    m.plateau(3,0,13,7)
    m.plateau(43,2,57,13)
    m.stamp('bench',38,6)
    m.sign(4,13,'송악 구릉길\n서쪽은 산록 마을, 남쪽 내리막은 닥나무 마을입니다.\n수레는 개경 큰길과 이 산길의 이동 시간을 비교해 보세요.')
    m.sign(38,9,'구릉 위 쉼터\n아래쪽은 종이를 뜨는 마을과 들판으로 이어집니다.\n여기는 쉬어 둘러보는 길입니다. 교역하려면 남쪽 내리막으로 가세요.')
    m.water_blob(49,22,4,2)
    m.add('ambience',47,22,id='river',radius=6)
    m.forest_border(thickness=2,big=('pinebig','roundbig'))
    m.scatter('pinebig',17,area=(3,3,53,21),margin=1)
    m.flowers(4,12,53,24,12)
    m.finish();return m


def descent():
    m = Map('goryeo_descent', '닥목 산기슭 길', 34, 12, seed=172)
    m.hours = 1
    m.link('N',0,3,'goryeo_ridge','S','N')
    m.link('S',19,3,'Dakmok','N','S')
    m.road_path([(1,0),(1,6),(20,6),(20,11)],width=2,grade=1)
    m.sign(12,4,'산기슭 갈림길\n북서쪽은 송악 산록, 남쪽은 닥나무 마을입니다.\n종이와 생활재를 실었다면 장터에서 귀환품도 살펴보세요.')
    m.stamp('house_thatch',26,3)
    m.road_path([(27,7),(20,7)],width=1,grade=1)
    m.bushes([(8,8),(10,8),(24,1)])
    m.forest_border(thickness=1,big=('pinebig','roundbig'))
    m.finish();return m


def fieldroad():
    m = Map('goryeo_fieldroad', '장단 들길', 34, 44, seed=173)
    m.hours = 1
    m.river_h(21,24,wiggle=1,fixed=range(18,28))
    m.link('N',19,3,'Dakmok','S','N')
    m.link('S',19,3,'goryeo_jangdan','N','S')
    m.road_path([(20,0),(20,8)],width=3,grade=3)
    m.road_path([(20,8),(25,8),(25,35),(20,35),(20,43)],width=3,grade=3)
    m.road_path([(20,8),(20,35)],width=1,grade=1)
    m.bridge(19,20,20,25)
    for y in range(20,26):
        for x in range(19,21):m.road[y][x]=1
    m.bridge(24,20,26,25)
    m.sign(17,6,'장단으로 가는 두 길\n곧장: 좁은 밭둑과 작은 다리\n동쪽: 수레 큰길과 넓은 다리. 짐과 수단에 맞춰 골라 보세요.')
    m.sign(18,37,'들마을 앞\n남쪽 장단은 곡식·땔감 거래처, 북쪽 닥목은 종이 거래처입니다.\n빈손으로 돌아가기 전에 시세와 남은 적재 공간을 확인하세요.')
    for x,y in [(3,4),(7,4),(11,4),(3,12),(7,12),(29,4),(3,31),(7,31),(3,37),(7,37),(29,30)]:m.stamp('wheat',x,y)
    m.stamp('cart',28,36)
    m.stamp('sheaf',10,35)
    m.add('worker',10,16,name='들판 일꾼',character='villager_dog_tall',path=[(10,16),(14,16)],lines=['마을 장터로 보낼 곡식을 묶고 있어요.','장터에서 값을 확인하고 돌아올 물건도 골라 보세요.'])
    m.add('ambience',14,22,id='river',radius=8)
    m.forest_border(thickness=1,big=('roundbig','fruitbig'))
    m.flowers(2,2,30,40,10)
    m.finish();return m
