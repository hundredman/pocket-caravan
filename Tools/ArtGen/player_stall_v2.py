"""11/19 은샘 "내 좌판 디자인이 너무 튀어. 주변 환경에 자연스럽게": 고려 장터 곡식 좌판(grain_stall)을 바탕으로
진열칸의 곡식만 지우고 빈 진열칸(판자 벽)으로 바꾼 그림. 진열한 물건은 게임에서 실제 짐 그림을 올려 보인다 (StallSpot).
작은 나무 패(엽전 무늬)를 차양 아래 달아 내 좌판임을 살짝 표시.
python Tools/ArtGen/player_stall_v2.py  → Assets/PocketCaravan/Resources/Stall/stall_0.png
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'Assets/PocketCaravan/Art/Goryeo/Markets/grain_stall.png'
OUT = ROOT / 'Assets/PocketCaravan/Resources/Stall/stall_0.png'

def hexc(h): return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)
INK, DARK, MID, LO, LIGHT, HI = map(hexc, ['2e241d', '5b432f', '69452f', '68452d', '86513b', 'b77c3e'])
GOLD, GOLDHI = hexc('daa34a'), hexc('f0c365')

a = Image.open(SRC).convert('RGBA')
px = a.load()
# 진열칸(곡식) 자리를 판자 벽으로: y 28..32, 기둥 안쪽 x 9..38
for y in range(28, 33):
    for x in range(9, 39):
        if px[x, y][3] == 0: continue
        seam = (x - 9) % 7 == 6
        px[x, y] = INK if seam else (MID if y in (28, 31) else (DARK if y == 32 else LO))
# 판자 벽 위 그늘 한 줄
for x in range(9, 39):
    if px[x, 28][3]: px[x, 28] = DARK
# 차양 끝에 작은 나무 패 (엽전 무늬), x 21..27, y 20..23 (아래 진열 물건과 안 겹치게)
for y in range(20, 24):
    for x in range(21, 28):
        edge = y in (20, 23) or x in (21, 27)
        px[x, y] = INK if edge else LIGHT
for (x, y) in ((23, 21), (24, 21), (25, 21), (23, 22), (24, 22), (25, 22)): px[x, y] = GOLD
px[23, 21] = GOLDHI; px[24, 21] = INK   # 엽전 빛 · 구멍
a.save(OUT)
print('saved', OUT)
