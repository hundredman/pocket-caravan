"""PNG 프레임을 RGBA .aseprite 원본으로 쓰고 다시 읽는 작은 도구 (11/08, 고려 생활 소품 원본용).
쓰기: write(path, [Image, ...], duration_ms)  /  읽기: read(path) -> [Image, ...]
층 1개, 프레임마다 압축 셀 1개. 파일 형식: github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
"""
import struct, zlib
from PIL import Image

def _chunk(kind, data):
    return struct.pack('<IH', len(data) + 6, kind) + data

def write(path, frames, duration=500):
    frames = [f.convert('RGBA') for f in frames]
    w, h = frames[0].size
    assert all(f.size == (w, h) for f in frames)
    body = b''
    for i, f in enumerate(frames):
        chunks = []
        if i == 0:
            name = b'art'
            chunks.append(_chunk(0x2004, struct.pack('<HHHHHHB3x', 3, 0, 0, 0, 0, 0, 255) + struct.pack('<H', len(name)) + name))
        cel = struct.pack('<HhhBHh5x', 0, 0, 0, 255, 2, 0) + struct.pack('<HH', w, h) + zlib.compress(f.tobytes(), 9)
        chunks.append(_chunk(0x2005, cel))
        data = b''.join(chunks)
        body += struct.pack('<IHHH2xI', 16 + len(data), 0xF1FA, min(len(chunks), 0xFFFF), duration, len(chunks)) + data
    header = struct.pack('<IHHHHHIHIIB3xHBBhhHH84x', 128 + len(body), 0xA5E0, len(frames), w, h, 32, 1, 100, 0, 0, 0, 0, 1, 1, 0, 0, 16, 16)
    assert len(header) == 128
    with open(path, 'wb') as fh:
        fh.write(header + body)

def read(path):
    raw = open(path, 'rb').read()
    _, magic, n, w, h, depth = struct.unpack_from('<IHHHHH', raw, 0)
    assert magic == 0xA5E0 and depth == 32
    pos, out = 128, []
    for _ in range(n):
        size, fmagic, _, _, count = struct.unpack_from('<IHHH2xI', raw, pos)
        assert fmagic == 0xF1FA
        im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        p = pos + 16
        for _ in range(count):
            csize, kind = struct.unpack_from('<IH', raw, p)
            if kind == 0x2005:
                _, x, y, _, ctype, _ = struct.unpack_from('<HhhBHh', raw, p + 6)
                cw, ch = struct.unpack_from('<HH', raw, p + 6 + 16)
                assert ctype == 2
                cel = Image.frombytes('RGBA', (cw, ch), zlib.decompress(raw[p + 6 + 20:p + csize]))
                im.paste(cel, (x, y))
            p += csize
        out.append(im)
        pos += size
    return out
