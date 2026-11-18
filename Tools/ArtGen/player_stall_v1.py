"""11/19 은샘 "좌판까지 구현": 내 좌판 그림 (신용 5단계, 장터 한 자리). 48x44, 아래 가운데가 발 자리.
장터 좌판·손수레(handcart_v3)와 같은 나무색·짐(무명 자루·옥색 항아리·보따리·나무 상자) + 내 것임을 알리는 청록 줄무늬 차양(내 캐릭터 발밑 고리와 같은 색).
진열 단계 0~2: 0 빈 좌판(차양만) · 1 물건 조금 · 2 가득.
실행: python Tools/ArtGen/player_stall_v1.py <출력 폴더>  → stall_0.png ~ stall_2.png
"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))
import handcart_v3 as HC

HC.P.update(T=(36, 112, 110), t=(66, 156, 148), u=(132, 206, 192), y=(232, 182, 64), Y=(250, 214, 110))
W, H = 48, 44


def stall(stage):
    c = HC.C(W, H)
    # 뒤 기둥 (차양을 받친다)
    for x in (4, 42):
        c.vline(x, 6, 24, 'm'); c.vline(x + 1, 6, 24, 'd')
    # 진열대 윗면
    top = 24
    c.rect(2, top, 45, top + 3, 'h'); c.hline(2, 45, top, 'H')
    # 물건
    if stage >= 1:
        c.paste(HC.sack(), 5, top - 8)
        c.paste(HC.jar(), 16, top - 8)
        c.paste(HC.crate(False), 27, top - 7)
    if stage >= 2:
        c.paste(HC.bundle(), 34, top - 7)
        c.paste(HC.sack(), 11, top - 12)
        c.paste(HC.jar(), 22, top - 12)
    # 앞판 (나무 여러 단) + 가운데 엽전 표 (내 가게)
    for i, k in enumerate(['w', 'd', 'w', 'm', 'w', 'd']):
        c.hline(2, 45, top + 4 + i, k)
    for x in (10, 24, 38):
        c.px(x, top + 5, 'm'); c.px(x + 1, top + 8, 'd')
    cx, cy = 23, top + 6
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            if dx * dx + dy * dy <= 5: c.px(cx + dx, cy + dy, 'y')
    c.px(cx - 1, cy - 1, 'Y'); c.px(cx, cy, 'd')
    # 다리
    for x in (3, 43):
        c.vline(x, top + 10, H - 2, 'm'); c.vline(x + 1, top + 10, H - 2, 'd')
    c.hline(5, 42, top + 10, 'i')
    # 차양: 위가 좁고 아래가 넓은 청록·크림 줄무늬, 아래 끝은 물결
    for y in range(1, 10):
        inset = max(0, 4 - y // 2)
        for x in range(1 + inset, W - 1 - inset):
            stripe = ((x - 1) // 5) % 2
            k = ('u' if y < 3 else 't') if stripe == 0 else ('X' if y < 3 else 'C')
            if y == 9 and (x - 1) % 5 in (0, 4): continue   # 물결
            c.px(x, y, k)
    c.hline(5, W - 6, 1, 'u')
    for x in range(1, W - 1):
        if c.get(x, 8)[3] and ((x - 1) // 5) % 2 == 0: c.px(x, 8, 'T')
    c.outline()
    return c.im


if __name__ == '__main__':
    out = Path(sys.argv[1] if len(sys.argv) > 1 else '.'); out.mkdir(parents=True, exist_ok=True)
    for s in range(3):
        stall(s).save(out / f'stall_{s}.png')
