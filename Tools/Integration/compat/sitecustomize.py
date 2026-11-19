"""run_checks.py가 Windows에서 검사를 돌릴 때만 먼저 읽히는 호환 설정 (2026-11-19). 테스트 파일은 그대로 둔다.

그림 테스트는 Pillow 12에서 생긴 Image.get_flattened_data를 쓴다. 파이썬 3.9에는 Pillow 12를 깔 수 없어서
Pillow 11 이하면 같은 값(픽셀을 한 줄로 편 tuple)을 내는 getdata로 이어 준다. Pillow 12 이상이면 아무것도 하지 않는다.
"""
try:
    from PIL import Image

    if not hasattr(Image.Image, 'get_flattened_data'):
        Image.Image.get_flattened_data = lambda self, band=None: tuple(self.getdata(band))
except ImportError:
    pass
