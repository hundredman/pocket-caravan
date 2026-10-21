"""베네치아 맵 배치 확인용 그림 (저장소에 올리지 않는 미리보기 생성).
    사용: python Tools/MapGen/venice_debug.py <생성 함수 이름> [카메라]
    - preview_<mapId>_layout.png : 막힌 칸(빨강), 문 앞 칸(하늘색, 막혀 있으면 노랑), 2칸 격자, 객체 위치
    - '카메라'를 주면 기본 카메라 크기(24×13.5칸)로 주요 지점을 잘라 preview_<mapId>_cam_N.png로 저장
    검사: 문 앞 막힘, 길 끊김, 객체 접근을 출력한다."""
import os, sys
from PIL import Image, ImageDraw
import venice
from export_venice import preview
from validate import reachable

HERE = os.path.dirname(os.path.abspath(__file__))


def check(m):
    roads = {(x, y) for y in range(m.h) for x in range(m.w) if m.road[y][x] and not m.blocked[y][x]}
    start = next(iter(roads))
    acc = reachable(m, start)
    problems = []
    iso = roads - reachable(m, start, road_only=True)
    if iso:
        problems.append(f"끊긴 길 {len(iso)}칸 예: {sorted(iso)[:5]}")
    by_building = {}
    for name, cell in m.doors:
        by_building.setdefault((name, id(cell)), []).append(cell)
    seen = {}
    for name, cell in m.doors:
        seen.setdefault(name, []).append(cell)
    for o in m.objects:
        k = o["kind"]
        if k in ("market", "board", "talker", "sign", "npc"):
            if not any((o["x"] + dx, o["y"] + dy) in acc for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1))):
                problems.append(f"{k} 접근 불가 {o['x']},{o['y']}")
        if k == "exit" and (o["x"], o["y"]) not in acc:
            problems.append(f"출구 접근 불가 {o['target']}")
        if k == "spawn" and (o["x"], o["y"]) not in acc:
            problems.append(f"스폰 접근 불가 {o['id']}")
    return problems, acc


def door_groups(m):
    """같은 건물(같은 y, 붙어 있는 x)끼리 묶은 문 앞 칸 목록"""
    groups = []
    for name, (x, y) in m.doors:
        if groups and groups[-1][0] == name and groups[-1][1][-1][1] == y and x - groups[-1][1][-1][0] <= 1:
            groups[-1][1].append((x, y))
        else:
            groups.append((name, [(x, y)]))
    return groups


def render(m, acc, scale=2):
    path = os.path.join(HERE, f"preview_{m.id}_layout.png")
    preview(m, path, markers=True)
    img = Image.open(path).convert("RGBA")
    over = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(over)
    for y in range(m.h):
        for x in range(m.w):
            if m.blocked[y][x] and not m.water[y][x]:
                d.rectangle((x * 16, y * 16, x * 16 + 15, y * 16 + 15), fill=(255, 0, 0, 60))
    for name, cells in door_groups(m):
        ok = any(c in acc for c in cells)
        for x, y in cells:
            if m.inside(x, y):
                d.rectangle((x * 16 + 4, y * 16 + 4, x * 16 + 11, y * 16 + 11), fill=(0, 220, 255, 170) if ok else (255, 230, 0, 220))
    for x in range(0, m.w, 2):
        d.line((x * 16, 0, x * 16, m.h * 16), fill=(0, 0, 0, 40))
    for y in range(0, m.h, 2):
        d.line((0, y * 16, m.w * 16, y * 16), fill=(0, 0, 0, 40))
    img.alpha_composite(over)
    img.resize((img.width * scale, img.height * scale), Image.NEAREST).save(path)
    return path


def cameras(m, points):
    clean = os.path.join(HERE, f"preview_{m.id}.png")
    preview(m, clean, markers=False)
    img = Image.open(clean)
    outs = []
    for i, (cx, cy) in enumerate(points):
        x0 = min(max(0, cx * 16 - 192), img.width - 384)
        y0 = min(max(0, cy * 16 - 108), img.height - 216)
        p = os.path.join(HERE, f"preview_{m.id}_cam_{i}.png")
        img.crop((x0, y0, x0 + 384, y0 + 216)).resize((768, 432), Image.NEAREST).save(p)
        outs.append(p)
    return outs


if __name__ == "__main__":
    m = getattr(venice, sys.argv[1])()
    problems, acc = check(m)
    blocked_doors = [(n, c) for n, c in door_groups(m) if not any(x in acc for x in c)]
    print(m.id, "문제:", problems or "없음", "| 막힌 문:", [(n, c[0]) for n, c in blocked_doors] or "없음")
    print(render(m, acc))
    if len(sys.argv) > 2:
        pts = getattr(venice, "CAMERA_POINTS", {}).get(m.id, [])
        for p in cameras(m, pts):
            print(p)
