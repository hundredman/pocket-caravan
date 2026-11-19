"""11/19 은샘: 아라비아 4단계 운반 수단 '낙타 대상' (마차 대신, 북유럽 순록 썰매와 같은 방식).
고증: 중세 아라비아·중동은 바퀴 수레가 거의 사라지고 낙타로 짐을 날랐다 (Bulliet, The Camel and the Wheel).
대상은 낙타를 한 줄로 이어(키타르, qitar) 끌었다. 뒤 낙타 고삐는 코에서 앞 낙타 안장 뒤쪽에 묶는다.
그림: 3단계 낙타(pack_animals_v1 make_camel · camel_front · camel_back) 두 마리를 고삐로 잇는다. 두 낙타는 같은 단계 짐을 싣고 발을 엇갈려 걷는다.
- 옆 72x40 (앞 낙타가 왼쪽, 뒤 낙타는 34칸 뒤). 앞(아래로 올 때)·뒤(위로 갈 때) 23x66 (두 낙타 사이 22칸).
실행: python Tools/ArtGen/camel_caravan_v1.py → Assets/PocketCaravan/Resources/Transport/CamelCaravan (side/front/back_0~3 + _a/_b)
"""
import sys
from pathlib import Path
from PIL import Image

sys.path.insert(0, str(Path(__file__).parent))
import pack_animals_v1 as PA

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/PocketCaravan/Resources/Transport/CamelCaravan'
ROPE = (104, 70, 42, 255)   # 밧줄: 낙타 털보다 짙게 (몸 위를 지나도 보이게)
OTHER = {'': '', 'a': 'b', 'b': 'a'}   # 뒤 낙타는 걸음을 엇갈리게
D = 34                                 # 옆모습 두 낙타 사이
EH = 66                                # 앞·뒤 캔버스 높이 (두 낙타 사이 22칸)


def rope(out, pts):
    px = out.load()
    for x, y in pts: px[x, y] = ROPE


def side(stage, frame=''):
    out = Image.new('RGBA', (D + 38, 40), (0, 0, 0, 0))
    out.alpha_composite(PA.make_camel(stage, OTHER[frame]), (D, 0))     # 뒤 낙타 (먼저)
    out.alpha_composite(PA.make_camel(stage, frame), (0, 0))
    rope(out, [(D, 15), (D - 1, 16), (D - 1, 17), (D - 2, 18), (D - 2, 19), (D - 3, 20), (D - 3, 21)])   # 뒤 낙타 코 → 앞 낙타 안장 뒤
    return out


def front(stage, frame=''):
    """아래로 올 때: 앞 낙타가 아래(가까이), 뒤 낙타는 위(멀리)"""
    out = Image.new('RGBA', (23, EH), (0, 0, 0, 0))
    out.alpha_composite(PA.camel_front(stage, OTHER[frame]), (0, 0))
    rope(out, [(11, y) for y in range(26, 40)])   # 뒤 낙타 코에서 앞 낙타 안장으로 (앞 낙타 머리 뒤로 들어간다)
    out.alpha_composite(PA.camel_front(stage, frame), (0, EH - 44))
    return out


def back(stage, frame=''):
    """위로 갈 때: 앞 낙타가 위(멀리), 뒤 낙타가 아래(가까이)"""
    out = Image.new('RGBA', (23, EH), (0, 0, 0, 0))
    out.alpha_composite(PA.camel_back(stage, frame), (0, 0))
    rope(out, [(12, y) for y in range(26, 34)])   # 앞 낙타 안장 뒤에서 뒤 낙타 코로 (뒤 낙타 머리 너머)
    out.alpha_composite(PA.camel_back(stage, OTHER[frame]), (0, EH - 44))
    return out


if __name__ == '__main__':
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else OUT
    out.mkdir(parents=True, exist_ok=True)
    for view, fn in (('side', side), ('front', front), ('back', back)):
        for s in range(4):
            fn(s).save(out / f'{view}_{s}.png')
            for fr in 'ab': fn(s, fr).save(out / f'{view}_{s}_{fr}.png')
    print('ok')
