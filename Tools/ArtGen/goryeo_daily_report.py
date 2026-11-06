"""Collate unchanged, native-sized Unity captures; never edit game assets."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtReview/Goryeo-Daily-20261106'


def build():
    board = Image.new('RGB', (984, 648), '#eee9df')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc', 19)
    draw.text((12, 8), 'ATM-B4 · 나루 우물 물 긷기 검토 (실제 게임 480×270)', font=font, fill='#302e2a')
    for i, (name, label) in enumerate([
        ('01-offer', '① 시작 선택'), ('02-working', '② 3초 동안 물 긷기 · Esc 취소'),
        ('03-complete', '③ 완료 · 돈/평판/아이템 보상 없음'), ('04-done-today', '④ 같은 날 다시 방문'),
    ]):
        x, y = 8 + i % 2 * 488, 42 + i // 2 * 302
        draw.text((x, y), label, font=font, fill='#302e2a')
        with Image.open(OUT / f'{name}.png') as image:
            assert image.size == (480, 270)
            board.paste(image, (x, y + 26))
    board.save(OUT / 'review-board.png')


if __name__ == '__main__':
    build()
