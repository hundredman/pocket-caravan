"""A5 nearest-neighbor grid reduction, explicit palette and binary alpha. No art approval."""
import argparse
from collections import Counter
import json
from pathlib import Path
import re
from PIL import Image


def load_palette(path):
    colors = []
    for line in Path(path).read_text().splitlines():
        value = line.strip()
        if not value or value.startswith('//'):
            continue
        if not re.fullmatch(r'#[0-9a-fA-F]{6}', value):
            raise ValueError('Palette must contain one #RRGGBB color per line')
        color = tuple(bytes.fromhex(value[1:]))
        if color not in colors:
            colors.append(color)
    if not colors or len(colors) > 256:
        raise ValueError('Palette must contain 1..256 distinct RGB colors')
    return colors


def estimate_grid(image):
    """Repeated exact pixel edges only; uncertain AI texture requires explicit step."""
    rgba = image.convert('RGBA')
    width, height = rgba.size
    px = rgba.load()
    edges = Counter()
    def visible(value):
        return value if value[3] else (0, 0, 0, 0)
    for y in range(height):
        for x in range(width):
            here = visible(px[x, y])
            if x and here != visible(px[x-1, y]):
                edges[x] += 1
            if y and here != visible(px[x, y-1]):
                edges[y] += 1
    total = sum(edges.values())
    if not total:
        return dict(step=None, confidence=0, reason='No visible edges; specify grid step manually')
    for step in range(min(width, height), 1, -1):
        if width % step or height % step:
            continue
        support = sum(count for at, count in edges.items() if at % step == 0) / total
        if support >= .95:
            return dict(step=step, confidence=round(support, 4), reason='Repeated aligned edges')
    return dict(step=None, confidence=0, reason='No reliable repeated grid; specify grid step manually')


def prepare(image, size, palette, grid_step=None, alpha_threshold=128):
    width, height = size
    if width <= 0 or height <= 0 or not 1 <= alpha_threshold <= 255:
        raise ValueError('Positive output dimensions and alpha threshold 1..255 required')
    if not palette or len(palette) > 256 or any(len(c) != 3 or any(not isinstance(v,int) or not 0 <= v <= 255 for v in c) for c in palette):
        raise ValueError('Explicit RGB palette required')
    source = image.convert('RGBA')
    estimate = estimate_grid(source) if grid_step is None and source.size != size else None
    step = grid_step if grid_step is not None else 1 if source.size == size else estimate['step']
    if not isinstance(step, int) or step < 1:
        raise ValueError(estimate['reason'] if estimate else 'Positive integer grid step required')
    if source.width != width * step or source.height != height * step:
        raise ValueError('Grid step/output size must preserve the whole image and its aspect ratio')
    small = source.resize(size, Image.Resampling.NEAREST)
    cache = {}
    pixels = []
    for red, green, blue, alpha in small.get_flattened_data():
        if alpha < alpha_threshold:
            pixels.append((0, 0, 0, 0))
            continue
        rgb = (red, green, blue)
        if rgb not in cache:
            cache[rgb] = min(palette, key=lambda c: sum((a-b)**2 for a,b in zip(rgb,c)))
        pixels.append((*cache[rgb], 255))
    result = Image.new('RGBA', size)
    result.putdata(pixels)
    report = dict(sourceSize=list(source.size), outputSize=list(size), gridStep=step,
        gridMode='manual' if grid_step is not None else 'native' if source.size == size else 'estimated',
        estimate=estimate, alphaThreshold=alpha_threshold,
        usedOpaqueColors=len({p[:3] for p in pixels if p[3]}),
        paletteValid=all(p[:3] in palette for p in pixels if p[3]),
        binaryAlpha=all(p[3] in (0,255) for p in pixels), approval='pending user visual review')
    return result, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--size', nargs=2, type=int, required=True, metavar=('WIDTH','HEIGHT'))
    parser.add_argument('--palette', type=Path, required=True)
    parser.add_argument('--grid-step', type=int)
    parser.add_argument('--alpha-threshold', type=int, default=128)
    parser.add_argument('--report', type=Path)
    args = parser.parse_args()
    protected = {args.source.resolve(), args.palette.resolve()}
    if args.output.resolve() in protected or (args.report and (args.report.resolve() in protected or args.report.resolve() == args.output.resolve())):
        parser.error('Source, palette, image output and report must use separate paths')
    try:
        with Image.open(args.source) as image:
            result, report = prepare(image, tuple(args.size), load_palette(args.palette), args.grid_step, args.alpha_threshold)
    except (ValueError, OSError) as error:
        parser.error(str(error))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.output, format='PNG')
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n')
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()
