"""베네치아·본토 서부 사람 그림 (2026-10-26, 팀 아트 승인 전 제안). 16x24, 4방향 x 4프레임.
공용 chars.py의 글자 그림·뒤처리를 그대로 쓰고, 모자 모양과 옷 색만 이 파일에서 정한다(chars.py는 수정하지 않음).
사용: python Tools/ArtGen/venice_npc_chars.py   → Assets/PocketCaravan/Art/Characters/venice_*_tall_*.png
    곤돌라 사공(밀짚 보터)·상인(벨벳 베레)·서기·유리장이(가죽 앞치마)·어부(뜨개 모자)·레이스 아주머니·
    짐꾼·포도 농부·관문 경비(투구)"""
import os
import chars

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets/PocketCaravan/Art/Characters")

chars.EARS["v_boater"] = {    # 곤돌라 사공의 넓은 밀짚모자와 붉은 띠
    "d": ["...oooooooooo...", "..ohhbbbbbbhho..", "ohhhhhhhhhhhhhho"],
    "r": ["....ooooooo.....", "...ohhbbbbho....", ".ohhhhhhhhhhho.."],
}
chars.EARS["v_beret"] = {     # 상인·서기의 벨벳 베레 (둥근 귀가 옆으로 보인다)
    "d": ["....oooooooo....", "..ohhhhhhhHho...", ".ofohhhhhhhhofo."],
    "r": ["....ooooooo.....", "...ohhhhhHho....", "..ofohhhhhho...."],
}
chars.EARS["v_knit"] = {      # 어부의 뜨개 모자
    "d": ["...oooooooooo...", "..ohhHhhHhhHho..", "..ohhhhhhhhhho.."],
    "r": ["....ooooooo.....", "...ohhHhhHho....", "...ohhhhhhho...."],
}
chars.EARS["v_helmet"] = {    # 관문 경비의 둥근 투구
    "d": ["....oooooooo....", "..ohHhhhhhhHho..", ".ohhhhhhhhhhhho."],
    "r": ["....ooooooo.....", "...ohHhhhhho....", "..ohhhhhhhhhho.."],
}

# 이름: (색, 머리 위 모양)  —  f 털, L 밝은 털, l 주둥이, c 옷, a 옷 그늘, b 띠, k 바지, h/H 모자
PEOPLE = {
    "venice_gondolier": ({"f": (128, 86, 58), "L": (166, 120, 84), "l": (220, 186, 146),
                          "c": (240, 240, 244), "a": (54, 76, 140), "b": (196, 56, 60), "k": (40, 44, 60),
                          "h": (232, 200, 120), "H": (250, 226, 160)}, "v_boater"),
    "venice_merchant": ({"c": (138, 40, 58), "a": (104, 30, 46), "b": (226, 182, 84), "k": (60, 50, 60),
                         "h": (110, 28, 48), "H": (226, 182, 84)}, "v_beret"),
    "venice_clerk": ({"f": (228, 134, 66), "L": (246, 178, 108), "l": (252, 242, 228),
                      "c": (48, 70, 120), "a": (34, 50, 92), "b": (226, 182, 84), "k": (50, 46, 60),
                      "h": (40, 40, 52), "H": (120, 120, 140)}, "v_beret"),
    "venice_glassmaker": ({"f": (228, 134, 66), "L": (246, 178, 108), "l": (252, 242, 228),
                           "c": (122, 86, 60), "a": (92, 64, 46), "b": (232, 140, 64), "k": (70, 56, 50)}, "pointy"),
    "venice_fisher": ({"f": (224, 198, 152), "L": (242, 224, 190), "l": (252, 246, 234),
                       "c": (70, 110, 160), "a": (52, 84, 128), "b": (240, 220, 170), "k": (90, 74, 60),
                       "h": (190, 70, 70), "H": (240, 220, 200)}, "v_knit"),
    "venice_lace": ({"f": (246, 242, 236), "L": (255, 255, 255), "l": (255, 224, 230),
                     "c": (236, 196, 210), "a": (200, 150, 170), "b": (255, 255, 255), "k": (120, 90, 110)}, "long"),
    "venice_porter": ({"f": (224, 198, 152), "L": (242, 224, 190), "l": (252, 246, 234),
                       "c": (150, 104, 72), "a": (118, 80, 56), "b": (90, 120, 80), "k": (80, 64, 52)}, "pointy"),
    "venice_vintner": ({"f": (128, 86, 58), "L": (166, 120, 84), "l": (220, 186, 146),
                        "c": (118, 150, 84), "a": (88, 116, 62), "b": (110, 34, 58), "k": (90, 70, 56)}, "round"),
    "venice_guard": ({"f": (156, 146, 134), "L": (196, 188, 178), "l": (226, 220, 210),
                      "c": (120, 124, 140), "a": (90, 94, 110), "b": (196, 56, 60), "k": (60, 58, 70),
                      "h": (150, 154, 168), "H": (220, 224, 232)}, "v_helmet"),
}


def build():
    os.makedirs(OUT, exist_ok=True)
    return {name: chars.build_one(OUT, name, pal, top, False, False) for name, (pal, top) in PEOPLE.items()}


if __name__ == "__main__":
    frames = build()
    import sys
    if len(sys.argv) > 1:
        chars.preview(frames, sys.argv[1])
    print("saved", len(frames), "characters to", os.path.abspath(OUT))
