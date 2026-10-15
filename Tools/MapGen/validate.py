"""나무 묶음 보존, 실제 통행, 길 연결, 출구 및 지형 지도 배치 회귀 검사."""
import json
from collections import deque
from pathlib import Path
import maps
import gaegyeong
import cartography
from tiles import Map, STAMPS, K


def reachable(m, start, road_only=False):
    seen = {start}
    queue = deque([start])
    while queue:
        x, y = queue.popleft()
        for p in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            px, py = p
            if p in seen or not m.inside(px, py) or m.blocked[py][px]:
                continue
            if road_only and not m.road[py][px]:
                continue
            seen.add(p)
            queue.append(p)
    return seen


def validate():
    world = [maps.naru(), maps.route01(), gaegyeong.build(), maps.dakmok()]
    results = []
    def check(name, condition):
        assert condition, name
        results.append(name)

    # 재현 사례: 한 예약 칸 때문에 나머지 3칸만 남는 숲 타일이 없어야 한다.
    probe = Map("test", "test", 6, 6)
    probe.reserve(2, 2, 2, 2)
    check("예약 칸과 겹치는 나무는 전체 배치 거절", not probe.stamp_clipped("tree", 1, 1))
    check("거절 후 수관과 밑동 조각 없음", not any(k for layer in (probe.obj, probe.high) for row in layer for k in row))
    check("맵 경계의 반 그루 배치 거절", not probe.stamp_clipped("tree", -1, 1))
    probe.road_path([(0, 0), (5, 5)], width=1)
    check("폭 1 대각선 길 상하좌우 연결", (5, 5) in reachable(probe, (0, 0), road_only=True))

    positions = cartography.layout(world)
    by_id = {m.id: m for m in world}
    for m in world:
        roads = {(x, y) for y in range(m.h) for x in range(m.w) if m.road[y][x] and not m.blocked[y][x]}
        check(m.id + " 전체 통행 가능 길 연결", bool(roads) and roads <= reachable(m, next(iter(roads)), road_only=True))
        tree_ok = True
        for name, x, y in m.stamps:
            if STAMPS[name][0] != "nature":
                continue
            s, sx, sy, w, h, _, high, rc = STAMPS[name]
            for dy in range(h):
                for dx in range(w):
                    layer = m.high if dy < high else m.obj
                    tree_ok &= layer[y + dy][x + dx] == K(s, sx + dx, sy + dy, rc)
        check(m.id + " 모든 나무 묶음 타일 보존", tree_ok)
        accessible = reachable(m, next(iter(roads)))
        for o in m.objects:
            if o["kind"] == "spawn":
                cells = {(o["x"] + dx, o["y"] + dy) for dx in range(o.get("w", 1)) for dy in range(o.get("h", 1))}
                check(m.id + " 스폰 " + o["id"] + " 통행 가능", cells <= accessible)
            elif o["kind"] == "exit":
                check(m.id + " 출구 " + o["target"] + " 접근 가능", (o["x"], o["y"]) in accessible)
                target = by_id[o["target"]]
                check(m.id + " 출구의 도착 스폰 존재", any(s["kind"] == "spawn" and s["id"] == o["spawn"] for s in target.objects))
            elif o["kind"] in ("npc", "market", "board", "talker", "sign", "worker"):
                check(m.id + " " + o["kind"] + " 상호작용 접근 가능",
                      any((o["x"] + dx, o["y"] + dy) in accessible for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1))))
        for worker in (o for o in m.objects if o["kind"] == "worker"):
            path = worker.get("path", [])
            check(m.id + " " + worker["name"] + " 작업 경로 지정", len(path) >= 2)
            clear = True
            for (ax, ay), (bx, by) in zip(path, path[1:] + path[:1]):
                steps = max(abs(bx - ax), abs(by - ay)) * 4
                for i in range(steps + 1):
                    x, y = round(ax + (bx - ax) * i / max(steps, 1)), round(ay + (by - ay) * i / max(steps, 1))
                    clear &= (x, y) in accessible and not m.blocked[y][x]
            check(m.id + " " + worker["name"] + " 작업 경로 통행 가능", clear)
        check(m.id + " 다리/선착장 통행 가능", all(p in accessible and not m.water[p[1]][p[0]] for p in m.decks))
    # 물을 가로지르는 다리의 양 끝이 길과 연결되는지 확인한다.
    route = by_id["Route01"]
    check("예성 길 시내 다리 양안 연결", (33, 15) in reachable(route, (25, 15), road_only=True))
    check("지도는 남문 연결을 반영해 수도를 북쪽에 배치", positions["Gaegyeong"][1] < positions["Route01"][1])
    return results


if __name__ == "__main__":
    results = validate()
    out = Path(__file__).resolve().parents[2] / "output/2d-maintenance"
    out.mkdir(parents=True, exist_ok=True)
    (out / "map-validation.json").write_text(json.dumps(dict(passed=len(results), checks=results), ensure_ascii=False, indent=2))
    print(f"맵 회귀 검사 {len(results)}개 통과")
