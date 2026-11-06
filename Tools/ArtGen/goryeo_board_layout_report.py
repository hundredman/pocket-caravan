"""Collate native Unity screenshots; no game asset editing."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtReview/Goryeo-Trade-Quest-20261106'


def build():
    board = Image.new('RGB', (984, 334), '#eee9df')
    draw = ImageDraw.Draw(board)
    font = ImageFont.truetype('/System/Library/Fonts/AppleSDGothicNeo.ttc', 19)
    draw.text((8, 5), '의뢰 게시판 · 실제 게임 480×270 · 오른쪽은 검토 전용', font=font, fill='#302e2a')
    for i, (file, label) in enumerate([('songak-board', '현재: 위·아래가 화면 밖'), ('songak-board-proposed', '수정안: 창 안에 제목·닫기, 목록 스크롤')]):
        x = 8 + i * 488
        draw.text((x, 32), label, font=font, fill='#302e2a')
        with Image.open(OUT / (file + '.png')) as image:
            assert image.size == (480, 270)
            board.paste(image, (x, 58))
    board.save(OUT / 'board-comparison.png')


if __name__ == '__main__':
    build()
