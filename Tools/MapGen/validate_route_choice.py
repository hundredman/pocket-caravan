"""예성 길의 두 통행 경로와 수단별 선택 차이. 실제 에셋 속도로 계산하며 플레이 재미 검증은 별도."""
import heapq
import json
import re
from pathlib import Path
import maps

ROOT = Path(__file__).resolve().parents[2]


def route(m, speeds, crossing):
    start, end = (10, 10), (39, 6)
    queue = [(0.0, start)]
    costs, previous = {start: 0.0}, {}
    while queue:
        cost, p = heapq.heappop(queue)
        if cost != costs[p]:
            continue
        if p == end:
            path = [p]
            while p != start:
                p = previous[p]
                path.append(p)
            return cost, list(reversed(path))
        x, y = p
        for q in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            xx, yy = q
            if not m.inside(xx, yy) or m.blocked[yy][xx] or not m.road[yy][xx]:
                continue
            # 반대 다리를 제외해 두 경로를 독립 비교. 잔디 자유 이동은 별도 플레이로 확인.
            if 27 <= xx <= 31 and ((crossing == 'forest' and yy >= 12) or (crossing == 'main' and yy <= 6)):
                continue
            next_cost = cost + .5 / speeds[m.road[y][x]] + .5 / speeds[m.road[yy][xx]]
            if next_cost < costs.get(q, float('inf')):
                costs[q], previous[q] = next_cost, p
                heapq.heappush(queue, (next_cost, q))
    raise AssertionError(crossing + ': disconnected')


def validate():
    m = maps.route01()
    checks, results = [], {}
    for vehicle, asset in [('walk', 'Vehicle_Walk.asset'), ('cart', 'Vehicle_Handcart.asset')]:
        text = (ROOT / 'Assets/PocketCaravan/Data/Goryeo' / asset).read_text()
        speeds = [float(v) for v in re.findall(r'^  - ([0-9.]+)$', text.split('speedByGrade:')[1], re.M)]
        results[vehicle] = {}
        for branch in ('forest', 'main'):
            cost, path = route(m, speeds, branch)
            assert any(27 <= x <= 31 and (y <= 6 if branch == 'forest' else y >= 12) for x, y in path)
            # 같은 무게 5. 등짐/수레 적재율은 본편 공식. 체력/가감속/낙하는 별도 확인.
            capacity = float(re.search(r'capacity: ([0-9.]+)', text)[1])
            seconds = cost / (3.8 * (1 - .3 * 5 / capacity))
            results[vehicle][branch] = {'tiles': len(path) - 1, 'seconds_estimated': round(seconds, 3), 'path': path}
        expected = 'forest' if vehicle == 'walk' else 'main'
        other = 'main' if expected == 'forest' else 'forest'
        assert results[vehicle][expected]['seconds_estimated'] < results[vehicle][other]['seconds_estimated'], results
        checks.append(vehicle + ' prefers ' + expected + ' at equal cargo 5')
    signs = [o for o in m.objects if o['kind'] == 'sign']
    assert any('등짐 권장' in o.get('text', '') for o in signs)
    assert any('노면 감속' in o.get('text', '') for o in signs)
    assert not any('짐이 흔들리면' in o.get('text', '') for o in signs)
    checks += ['fork sign explains vehicle choice', 'forest sign explains terrain slowdown']
    return {'checks': checks, 'routes': results, 'scope': '직교 노면 경로의 계산값. 체력·달리기·가감속·NPC·화물 낙하·잔디 자유 이동 제외. 실제 플레이 시간/재미 검증과 다름.'}


if __name__ == '__main__':
    result = validate()
    out = ROOT / 'output/goryeo-route'
    out.mkdir(parents=True, exist_ok=True)
    (out / 'route-choice-validation.json').write_text(json.dumps(result, ensure_ascii=False, indent=2))
    print(json.dumps({v: {b: {k: n for k, n in r.items() if k != 'path'} for b, r in choices.items()} for v, choices in result['routes'].items()}, ensure_ascii=False))
    print(len(result['checks']), 'route choice checks passed')
