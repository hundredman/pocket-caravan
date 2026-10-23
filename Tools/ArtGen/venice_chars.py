"""베네치아 플레이어 캐릭터 시안: 흰 고양이 상인 (16x24, 4방향 x 4프레임). 팀 아트 승인 전 제안.
공용 chars.py의 글자 그림·뒤처리를 그대로 쓰고, 귀 위에 얹은 작은 모자 모양만 이 파일에서 더한다(chars.py는 수정하지 않음).
사용: python Tools/ArtGen/venice_chars.py <출력 폴더> [미리보기.png]"""
import os
import sys
from PIL import Image
import chars

# 뾰족한 고양이 귀 사이에 작은 베레모(챙 없는 상인 모자)
chars.EARS["cat_cap"] = {
    "d": ["..o...oooo...o..", ".ofo.ohHhho.ofo.", ".offohhhhhhoffo."],
    "r": ["....o.oooo......", "...ofohHhho.....", "..offohhhhho...."],
}

VENICE_CAT = {
    "f": (240, 238, 232),   # 흰 털
    "L": (255, 255, 252),
    "l": (255, 236, 236),   # 주둥이
    "n": (226, 132, 146),   # 분홍 코
    "e": (48, 92, 132),     # 푸른 눈
    "p": (246, 176, 182),
    "h": (138, 40, 58),     # 와인색 모자
    "H": (226, 182, 84),    # 금색 장식
    "c": (150, 46, 62),     # 와인색 겉옷
    "a": (116, 34, 50),
    "b": (226, 182, 84),    # 금색 띠
    "k": (56, 82, 96),      # 청록 바지
    "s": (70, 48, 40),
    "t": (214, 210, 204),   # 꼬리 무늬 (옅은 회색)
}


def build(out_dir):
    return chars.build_one(out_dir, "venice_cat", VENICE_CAT, "cat_cap", False, False)


if __name__ == "__main__":
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    frames = build(out)
    if len(sys.argv) > 2:
        chars.preview({"venice_cat": frames}, sys.argv[2])
