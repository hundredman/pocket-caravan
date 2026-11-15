"""도트 크기(칸당 픽셀) 검사 — 11/13 시연 피드백 "그림마다 픽셀 크기가 달라 보인다".

ArtGuide 2절 기준: 모든 맵 위 그림은 칸당 16픽셀(PPU 16), 확대는 정수 배율만.
읽기 전용이다. 그림·메타·코드를 고치지 않고 후보 목록만 만든다 (판정은 사람이).

1) 가져온 그림: .png.meta의 PPU와, 그림이 k×k 덩어리로 키운 것인지(업스케일) 봐서
   실제 칸당 도트 = PPU / k. 16이 아니면 후보.
2) 코드에서 만든 스프라이트: Sprite.Create(..., ppu)의 ppu가 16이 아닌 곳.
3) 코드 배율: SpriteRenderer를 단 오브젝트에 정수가 아닌 localScale을 주는 곳 (효과·애니메이션은 정상일 수 있다).

  python3 Tools/Audit/pixel_density.py            # 이어서 검사 (캐시) → Docs/Validation/PixelDensity/report.md
  python3 Tools/Audit/pixel_density.py --budget 35  # 한 번에 쓸 시간(초)
"""
from pathlib import Path
import argparse, json, re, time, sys

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'Assets/PocketCaravan'
OUT = ROOT / 'Docs/Validation/PixelDensity'
CACHE = OUT / 'cache.json'
TARGET = 16
# 맵 위에 안 그리는 곳 (화면 UI · 에디터 미리보기)
UI_PARTS = ('Resources/StartScreen', 'Resources/WorldMap', 'MapModules/Previews', 'Art/Items', 'Resources/Common/Icons')
SKIP_BLOCK = ('Art/NinjaAdventure',)   # 외부 묶음(16px 원본). PPU만 본다
PPU_RX = re.compile(r'spritePixelsToUnits: ([0-9.]+)')


def block_size(path):
    """그림이 k×k 같은 색 덩어리로만 되어 있으면 k (2~4), 아니면 1"""
    try:
        from PIL import Image
        import numpy as np
        im = np.array(Image.open(path).convert('RGBA'))
    except Exception:
        return 1
    im[im[..., 3] == 0] = 0
    if (im[..., 3] > 0).sum() < 16:
        return 1
    if len(np.unique(im.reshape(-1, 4), axis=0)) < 3:   # 한두 색뿐인 그림(물·그림자 칸)은 덩어리인지 알 수 없다
        return 1
    h, w = im.shape[:2]
    for k in (4, 3, 2):
        if h % k or w % k:
            continue
        small = im[::k, ::k]
        if np.array_equal(np.repeat(np.repeat(small, k, 0), k, 1), im):
            return k
    return 1


def scan_images(budget):
    cache = json.loads(CACHE.read_text()) if CACHE.exists() else {}
    start = time.time(); done = True
    for meta in sorted(ASSETS.rglob('*.png.meta')):
        rel = meta.relative_to(ASSETS).as_posix()[:-5]
        png = meta.with_suffix('')
        try:
            mt = int(png.stat().st_mtime)
        except FileNotFoundError:
            continue
        row = cache.get(rel)
        if row and row['mtime'] == mt:
            continue
        if time.time() - start > budget:
            done = False; break
        m = PPU_RX.search(meta.read_text(errors='ignore'))
        ppu = float(m.group(1)) if m else 100.0
        k = 1 if rel.startswith(SKIP_BLOCK) else block_size(png)
        try:
            from PIL import Image
            size = Image.open(png).size
        except Exception:
            size = (0, 0)
        cache[rel] = dict(mtime=mt, ppu=ppu, block=k, size=list(size))
    OUT.mkdir(parents=True, exist_ok=True)
    CACHE.write_text(json.dumps(cache, ensure_ascii=False))
    return cache, done


SCALE_RX = re.compile(r'localScale\s*=\s*(new Vector3\(([^;]*)\)|Vector3\.one\s*\*\s*([^;]+))')
CREATE_RX = re.compile(r'Sprite\.Create\((.*)\)')


def scan_code():
    creates, scales = [], []
    for cs in sorted((ASSETS / 'Scripts').rglob('*.cs')):
        rel = cs.relative_to(ASSETS).as_posix()
        for i, line in enumerate(cs.read_text(errors='ignore').splitlines(), 1):
            if line.strip().startswith('//'):
                continue
            for m in re.finditer(r'Sprite\.Create\(', line):
                # 마지막 인자 중 ppu (네 번째 인자) 추출
                depth, args, cur, j = 0, [], '', m.end()
                while j < len(line):
                    c = line[j]
                    if c == '(':
                        depth += 1
                    if c == ')':
                        if depth == 0:
                            break
                        depth -= 1
                    if c == ',' and depth == 0:
                        args.append(cur.strip()); cur = ''
                    else:
                        cur += c
                    j += 1
                args.append(cur.strip())
                ppu = args[3] if len(args) > 3 else '100(기본)'
                val = re.sub(r'f$', '', ppu.strip())
                if val not in ('16', '16f'):
                    creates.append(dict(file=rel, line=i, ppu=ppu, code=line.strip()[:160]))
            m = SCALE_RX.search(line)
            if m:
                expr = m.group(2) or m.group(3)
                nums = re.findall(r'(?<![\w.])(\d+(?:\.\d+)?)f?', expr)
                odd = [n for n in nums if float(n) not in (0, 1, 2, 3, 4)]
                dynamic = bool(re.search(r'Sin|Lerp|Random|age|t\b|\*', expr)) and not odd
                if odd or 'Random' in expr or 'Sin' in expr or 'Lerp' in expr:
                    scales.append(dict(file=rel, line=i, expr=expr.strip()[:90], code=line.strip()[:160]))
    return creates, scales


def code_text():
    parts = [cs.read_text(errors='ignore') for cs in (ASSETS / 'Scripts').rglob('*.cs')]
    parts += [j.read_text(errors='ignore') for j in (ASSETS / 'World').rglob('*.json')]
    return '\n'.join(parts)


def usage(rel, text, tex_dirs):
    """Resources 그림을 코드가 어떻게 쓰나: texture(Sprite.Create로 PPU를 다시 정함) · unused(코드·데이터에 경로가 없음) · sprite"""
    if not rel.startswith('Resources/'):
        return 'asset'
    sub = rel[len('Resources/'):]
    folder = sub.rsplit('/', 1)[0]
    if folder in tex_dirs:
        return 'texture'
    return 'sprite' if folder + '/' in text or '"' + folder + '"' in text else 'unused'


def report(cache, creates, scales, done):
    text = code_text()
    tex_dirs = set(m.rsplit('/', 1)[0] for m in re.findall(r'Resources\.Load<Texture2D>\("([^"]*)', text))
    rows = []
    for rel, r in cache.items():
        eff = r['ppu'] / max(1, r['block'])
        if abs(eff - TARGET) < 0.01:
            continue
        ui = rel.startswith(UI_PARTS)
        u = usage(rel, text, tex_dirs)
        if u == 'texture' and r['block'] == 1:
            ui = 'texture'   # 파일 PPU는 안 쓰고 코드가 16으로 다시 만든다 (문제 없음)
        elif u == 'unused':
            ui = 'unused'
        rows.append((rel, r, eff, ui))
    groups = {}
    for rel, r, eff, ui in rows:
        folder = rel.rsplit('/', 1)[0]
        key = (folder, r['ppu'], r['block'], ui)
        groups.setdefault(key, []).append((rel, r))
    L = ['# 도트 크기 검사 (칸당 픽셀)', '',
         f'기준: 칸당 {TARGET}픽셀 (ArtGuide 2절). 실제 칸당 도트 = PPU ÷ 업스케일 배수. 읽기 전용 후보 목록이며 판정은 사람이 한다.',
         '' if done else '> **검사가 아직 덜 끝났다.** 같은 명령을 다시 돌리면 이어서 한다.', '',
         f'- 검사한 그림: {len(cache)}개 · 기준과 다른 그림: {len(rows)}개 → **고칠 후보(맵 위) {sum(1 for x in rows if x[3] is False)}** · 코드가 PPU 16으로 다시 만듦 {sum(1 for x in rows if x[3] == "texture")} · 코드에서 안 씀 {sum(1 for x in rows if x[3] == "unused")} · 화면 UI·미리보기 {sum(1 for x in rows if x[3] is True)}', '',
         '## 1. 가져온 그림 (맵 위)', '', '| 폴더 | 개수 | PPU | 업스케일 | 칸당 도트 | 그림 크기 예 |', '|---|---|---|---|---|---|']
    for (folder, ppu, k, ui), items in sorted(groups.items(), key=lambda kv: (str(kv[0][3]), -len(kv[1]))):
        if ui is not False:
            continue
        ex = items[0][1]['size']
        L.append(f'| `{folder}` | {len(items)} | {ppu:g} | ×{k} | **{ppu / k:g}** | {ex[0]}×{ex[1]} |')
    names = {True: '화면 UI·미리보기', 'texture': '코드가 Sprite.Create(…, 16)으로 다시 만듦 → 문제 없음', 'unused': '코드·데이터에 경로가 안 보임 (안 쓰는 듯)'}
    L += ['', '## 2. 문제 없음 · 참고', '', '| 폴더 | 개수 | PPU | 업스케일 | 이유 |', '|---|---|---|---|---|']
    for (folder, ppu, k, ui), items in sorted(groups.items(), key=lambda kv: -len(kv[1])):
        if ui is not False:
            L.append(f'| `{folder}` | {len(items)} | {ppu:g} | ×{k} | {names[ui]} |')
    L += ['', '## 3. 코드에서 만든 스프라이트 (PPU ≠ 16)', '', '그림 파일의 PPU 대신 이 값이 쓰인다. 16이 아니면 같은 그림도 다른 도트 크기로 보인다.', '', '| 위치 | PPU | 코드 |', '|---|---|---|']
    for c in creates:
        L.append(f'| `{c["file"]}:{c["line"]}` | `{c["ppu"]}` | `{c["code"].replace("|", "/")}` |')
    L += ['', '## 4. 코드 배율 (localScale, 정수 아님·움직임)', '', '먼지·물보라·숨쉬기 같은 효과와 애니메이션은 정상일 수 있다. 사람·동물·소품 몸에 주는 고정 배율만 고칠 후보.', '', '| 위치 | 배율 |', '|---|---|']
    for s in scales:
        L.append(f'| `{s["file"]}:{s["line"]}` | `{s["expr"].replace("|", "/")}` |')
    (OUT / 'report.md').write_text('\n'.join(L) + '\n')
    (OUT / 'report.json').write_text(json.dumps(dict(done=done, images=[dict(path=a, ppu=r['ppu'], block=r['block'], size=r['size'], effective=e, kind=('fix' if u is False else 'ui' if u is True else u)) for a, r, e, u in rows], creates=creates, scales=scales), ensure_ascii=False, indent=1))


if __name__ == '__main__':
    ap = argparse.ArgumentParser(); ap.add_argument('--budget', type=float, default=35)
    a = ap.parse_args()
    cache, done = scan_images(a.budget)
    creates, scales = scan_code()
    report(cache, creates, scales, done)
    print(('완료' if done else '이어서 다시 실행') + f': 그림 {len(cache)}개 · 보고서 {OUT / "report.md"}')
