"""환경 외형만 교체. 도로 등급/충돌/연결부 좌표는 그대로."""
import colorsys
from PIL import Image
# id, label, ground, road, foliage, water. Alpha and sprite geometry are preserved.
SKINS=[
 ('meadow','온대 초원','#a5b94d','#bd8754','#65a355','#60c7d9'),
 ('pine','침엽수림','#71835b','#9c8060','#37695b','#64a8b7'),
 ('autumn','가을 숲','#ad9f50','#a47a52','#cf8a43','#7cb8c2'),
 ('mistwood','이끼 숲','#66866d','#8d8064','#477b6b','#719e9c'),
 ('wetland','습지','#829461','#a29b73','#6d8862','#669d9d'),
 ('coast','모래 해안','#cabe87','#ba9e69','#7c9f66','#4eabbf'),
 ('steppe','건조 초원','#b7ae6e','#b89b6d','#8b9258','#70adb2'),
 ('alpine','고산 지대','#979b8a','#8a8477','#526d61','#77a8b5'),
 ('desert','사막','#debf83','#ba935c','#82a469','#73b8c0'),
 ('canyon','붉은 협곡','#b97e61','#d4a074','#8c8a5b','#739b9d'),
 ('snow','설원','#dce8e7','#a6b7bb','#a1bdb6','#9bbdcf'),
 ('tundra','툰드라','#a6b0a0','#939b91','#7d9488','#9ab6bf'),
 ('volcanic','화산암','#5b5558','#81716a','#79765d','#de7446')
]


def rgb(s):return tuple(int(s[i:i+2],16) for i in (1,3,5))


def recolor(im,key,skin):
    if skin[0]=='meadow':return im.copy() # Original art reference, not another tint pass.
    ground,road,leaf,water=map(rgb,skin[2:]);im=im.copy();pixels=im.load()
    sheet=key.split(':')[0]
    for y in range(im.height):
        for x in range(im.width):
            r,g,b,a=pixels[x,y]
            if not a:continue
            h,s,v=colorsys.rgb_to_hsv(r/255,g/255,b/255)
            target=None
            if .40<h<.64 and s>.18 and sheet in ('water','floor','detail'):target=water
            elif .14<h<.43 and s>.16:
                target=ground if sheet in ('floor','water','field','detail') else leaf
            elif sheet=='floor' and (h<.14 or h>.94) and s>.18:target=road
            elif sheet=='module' and skin[0] in ('snow','tundra','volcanic','canyon','alpine') and s<.25:target=ground
            if target:
                # Preserve highlight/shadow hierarchy; do not flood transparent pixels.
                l=.40+.65*v
                pixels[x,y]=tuple(min(255,round(c*l)) for c in target)+(a,)
    return im
