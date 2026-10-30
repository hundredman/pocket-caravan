"""베네치아·본토 서부 맵에 '말 거는 사람'과 '이정표'를 더하는 마지막 단계 (2026-10-29, 이름·대사는 창작 제안).
    북유럽과 비교해 말 거는 사람(12명 → +31)·이정표(15개 → 출구마다)가 적던 것을 채운다.
    맵 생성기가 만든 맵 JSON을 읽어 덧붙인다(통합 브랜치에서는 생성기가 돌지 않아 JSON 단계에서 붙인다).
    다시 실행해도 앞서 붙인 것("type": "vx")을 먼저 걷어 내고 새로 붙이므로 겹치지 않는다.
    - 사람: 장터·출구·다른 사람에서 떨어진, 길 옆 걸을 수 있는 칸. 붙인 뒤에도 모든 출구·사람에게 걸어서 닿는지 확인한다.
    - 이정표: 출구에서 맵 안쪽으로 4~7걸음 들어온 길 옆 칸. "→ 무라노 유리공방"처럼 출구 쪽 화살표와 도착지 이름.
      장터가 있는 맵은 장터 옆에 모든 방향을 적은 큰 이정표도 하나.
    사용: python Tools/MapGen/venice_extras.py   (맵 JSON을 다시 만든 뒤, Unity에서 씬을 다시 만들기 전에)"""
import glob, json, os, random
from collections import deque

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MAPS = os.path.join(ROOT, "Assets/PocketCaravan/Maps")
SIGN_TILE = "element_0_2"
ARROW = {"W": "←", "E": "→", "N": "↑", "S": "↓"}

# 맵: [(이름, 그림, [대사…])]. 대사 한 줄은 대화창 한 쪽(약 3줄)에 들어가게 \n으로 나눈다.
TALKERS = {
    "venice_rialto": [
        ("가면 장인 루치아", "venice_maskmaker_tall", ["가면 하나에 꼬박 사흘이 걸린다오.\n종이를 겹겹이 붙이고 말린 다음 색을 올리지.",
                                                      "카니발 밤엔 누가 누군지 몰라.\n그래서 다들 그날만은 마음껏 웃는 거요."]),
        ("거리 화가 토니오", "venice_painter_tall", ["운하 물빛은 하루에도 열두 번 바뀌어.\n아침엔 은빛, 저녁엔 꿀빛이지.",
                                                     "자네 고양이 꼬리가 참 멋지군.\n가만있어 봐, 한 장 그려 줄게… 농담이야!"]),
        ("광장 악사 피에로", "venice_musician_tall", ["만돌린 줄은 둘씩 짝이야.\n그래서 소리가 반짝반짝 떨리지.",
                                                     "장터가 붐비는 낮엔 신나는 곡,\n저녁엔 느린 곡을 켜지."]),
        ("상인 집안 아가씨", "venice_lady_tall", ["아버지는 무라노 유리를 배에 실어\n멀리 동쪽까지 보내신대요.",
                                                  "레이스 손수건은 부라노 것이 제일 고와요.\n여기 리알토에선 값을 잘 쳐 주지요."]),
    ],
    "venice_murano": [
        ("유리 구슬 꿰는 아이", "venice_child_tall", ["이 구슬 다 제가 꿴 거예요!\n파란 건 바다, 노란 건 해님이에요.",
                                                     "가마 근처는 뜨거워서 가면 안 된대요.\n그래도 불빛은 정말 예뻐요."]),
        ("가마 지키는 노인", "elder_tall", ["불이 꺼지면 다시 달구는 데 사흘이야.\n그래서 이 섬은 밤에도 잠들지 않지.",
                                           "유리그릇은 여기서 사고,\n장식은 먼 장터에 가져가야 제값을 받네."]),
        ("모래 고르는 일꾼", "venice_porter_tall", ["유리는 고운 모래와 재로 만들어.\n모래에 돌이 섞이면 금이 가지.",
                                                   "키오자 배가 재를 싣고 오는 날은\n하루 종일 체로 모래를 걸러."]),
    ],
    "venice_burano": [
        ("레이스 배우는 소녀", "venice_child_tall", ["바늘땀 하나하나가 작은 꽃이 돼요.\n손수건 하나에 꽃이 천 송이래요.",
                                                    "할머니는 눈 감고도 뜨신대요.\n저는 아직 실이 자꾸 엉켜요."]),
        ("집 칠하는 아저씨", "venice_painter_tall", ["집마다 색이 다른 건\n안개 낀 날 배에서 자기 집을 찾으려고야.",
                                                     "올해는 분홍으로 칠할까, 하늘색으로 칠할까.\n자네라면 뭘 고르겠나?"]),
        ("그물 깁는 할머니", "elder_tall", ["그물코가 하나 터지면 고기 열 마리가 샌다우.\n그래서 매일 깁는 거라우.",
                                            "절인 생선은 소금이 넉넉해야 오래 가.\n키오자 소금이 제일이지."]),
    ],
    "venice_chioggia": [
        ("항구 요리사", "venice_cook_tall", ["갓 잡은 생선에 소금 한 줌, 레몬 한 방울.\n그거면 충분하지!",
                                             "본토 배가 들어오는 날은\n솥이 쉴 틈이 없어."]),
        ("배 목수 마르코", "venice_boatwright_tall", ["곤돌라 한 척에 나무가 여덟 종류 들어가.\n한쪽이 더 길어서 한 노로 곧게 가지.",
                                                      "본토행 배는 짐을 너무 실으면 느려져.\n소금 자루는 무게를 잘 봐서 싣게."]),
        ("젊은 뱃사람", "venice_sailor_tall", ["본토 나루터까지는 물길로 금방이야.\n거기서부터는 수레 길이지.",
                                              "언젠가 큰 배를 타고\n동쪽 끝 바다까지 가 보고 싶어."]),
    ],
    "venice_santerasmo": [
        ("과일 장수 로사", "venice_fruitseller_tall", ["이 섬 채소와 과일이 리알토 식탁에 다 올라가요.\n아침 배로 가야 싱싱하죠.",
                                                       "과일 상자는 무르기 전에 팔아야 해요.\n너무 먼 길은 피하는 게 좋아요."]),
        ("꿀벌 쫓는 아이", "venice_child_tall", ["벌은 레몬꽃을 제일 좋아해요.\n그래서 이 섬 꿀은 상큼해요!",
                                                "쏘이면 아파요. 가만히 있으면 괜찮대요.\n…진짜예요?"]),
    ],
    "venice_c_landing": [
        ("짐표 붙이는 서기", "venice_scholar_tall", ["짐마다 어디서 와서 어디로 가는지 적어 둬야\n잃어버려도 찾을 수 있지.",
                                                     "여기서 동쪽 큰길로 가면 대상 숙소,\n북쪽은 포도밭, 남쪽은 습지길이오."]),
        ("물새 그리는 화가", "venice_painter_tall", ["갯벌에 앉은 물새는 금방 날아가.\n그래서 빨리 그려야 해.",
                                                     "습지길은 느려도 경치가 좋아.\n바쁘지 않으면 한번 돌아가 봐."]),
        ("나루 빵집 주인", "venice_baker_tall", ["배에서 내린 손님들이 제일 먼저 찾는 게\n갓 구운 빵이야.",
                                                "포도밭 언덕 포도주랑 같이 먹으면\n여행 피로가 싹 가시지."]),
    ],
    "venice_c_vineyard": [
        ("포도 밟는 아이", "venice_child_tall", ["맨발로 포도를 밟으면 발이 보라색이 돼요!\n엄마가 웃어요.",
                                                "올해 포도는 달대요.\n비가 적어서 그렇대요."]),
        ("양조장 주인", "venice_noble_tall", ["포도주는 통에서 한 해를 쉬어야 맛이 들지.\n서두르면 시큼해져.",
                                             "고갯길 너머 눈숲 마을 사람들이\n우리 포도주를 아주 좋아한다네."]),
    ],
    "venice_c_marsh": [
        ("장어 잡는 어부", "venice_fisher_tall", ["습지 장어는 밤에 잡혀.\n낮엔 진흙 속에서 자거든.",
                                                  "이 길은 진흙이라 느려.\n대신 도적은 여기까지 안 오지."]),
        ("약초 캐는 할머니", "elder_tall", ["갯벌 풀 중엔 소금기를 먹고 자라는 것도 있다우.\n삶아 먹으면 짭조름하지.",
                                            "물이 차오르는 날엔 길 가장자리로 다니지 마우.\n발이 빠지면 빼기 힘들어."]),
    ],
    "venice_c_pass": [
        ("목동 소년", "venice_child_tall", ["양들이 고갯마루까지 올라가면\n저도 따라 올라가야 해요.",
                                            "길이 좁으니까 수레는 천천히요!\n양이 놀라요."]),
        ("순례길 나그네", "venice_scholar_tall", ["이 고개를 넘으면 북쪽 솔숲길이오.\n눈이 오면 길이 하얗게 사라지지.",
                                                  "길동무가 있으면 먼 길도 짧아진다오.\n자네는 어디까지 가시오?"]),
    ],
    "venice_c_bridge": [
        ("다리 아래 빨래꾼", "venice_lady_tall", ["이 강물은 산에서 바로 내려와서 차갑고 맑아요.\n빨래가 하얗게 돼요.",
                                                  "동쪽 다리를 건너면 다른 나라 포도밭이래요.\n말이 좀 달라요."]),
        ("석공 조수", "venice_porter_tall", ["다리 돌은 하나하나 깎아 맞춘 거야.\n틈이 없어야 물살에 버티지.",
                                             "다리 위에선 수레가 덜컹거리니\n깨지는 짐은 꼭 싸 두게."]),
    ],
    "venice_c_post": [
        ("떠돌이 악사", "venice_musician_tall", ["숙소 마당에서 노래 한 곡 부르면\n저녁 한 끼는 생기지.",
                                                 "동쪽 대륙 노래, 북쪽 노래, 사막 노래…\n여기선 다 들을 수 있어."]),
        ("대상 요리사", "venice_cook_tall", ["먼 길 온 대상들은 국물부터 찾아.\n그래서 솥은 늘 끓고 있지.",
                                             "북쪽 손님은 짠 걸, 사막 손님은 단 걸 좋아해.\n입맛도 장사 정보라네."]),
        ("통역하는 학자", "venice_scholar_tall", ["이 숙소엔 네 문명 말이 다 섞여 있소.\n숫자만 알아들어도 장사는 되지.",
                                                  "값을 깎을 땐 웃으면서.\n어느 나라든 그건 같더군."]),
    ],
    "venice_c_gate": [
        ("관문 앞 행상", "venice_fruitseller_tall", ["동문 밖으로 나가기 전에\n물이랑 빵은 챙겼어요?",
                                                    "한가운데 초원은 넓어서\n길 잃기 쉬워요. 이정표를 잘 봐요."]),
        ("귀족의 마부", "venice_boatwright_tall", ["주인 나리는 동쪽 대륙 비단을 사러 가신대.\n나는 말만 잘 몰면 되지.",
                                                  "큰길은 평평해서 수레가 잘 나가.\n대신 해 지면 조심하게."]),
    ],
}

PEOPLE_KINDS = ("npc", "talker", "worker", "villager", "walker", "market", "board", "bandit", "event", "sign")


class Map:
    def __init__(self, path):
        self.path = path
        with open(path, encoding="utf-8") as fp:
            self.d = json.load(fp)
        self.w, self.h = self.d["w"], self.d["h"]
        self.strip()

    def i(self, x, y):
        return y * self.w + x

    def inside(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    def tile(self, layer, x, y):
        v = self.d[layer][self.i(x, y)]
        return self.d["palette"][v] if v else ""

    def strip(self):
        """앞서 붙인 사람·이정표를 걷어 내고 이정표 칸을 원래대로"""
        keep = []
        for o in self.d["objects"]:
            if o.get("type") == "vx":
                if o["kind"] == "sign" and o.get("path"):
                    (obj, blocked), = o["path"]
                    self.d["obj"][self.i(o["x"], o["y"])] = obj
                    self.d["blocked"][self.i(o["x"], o["y"])] = blocked
                continue
            keep.append(o)
        self.d["objects"] = keep

    def walk(self, x, y, extra=()):
        return self.inside(x, y) and not self.d["blocked"][self.i(x, y)] and (x, y) not in extra

    def ground_ok(self, x, y):
        """사람·이정표를 세울 수 있는 칸: 물이 아니고, 나무 윗부분에 가리지 않고, 다리·소품 위가 아닌 칸"""
        g = self.tile("ground", x, y)
        if "_W_" in g or "water" in g or "sea" in g:
            return False
        if self.tile("obj", x, y) or self.tile("high", x, y) or (y > 0 and self.tile("high", x, y - 1)):
            return False
        return True

    def dist(self, starts, extra=()):
        dist = {s: 0 for s in starts if self.walk(*s, extra=extra)}
        q = deque(dist)
        while q:
            c = q.popleft()
            for p in ((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)):
                if p not in dist and self.walk(*p, extra=extra):
                    dist[p] = dist[c] + 1
                    q.append(p)
        return dist

    def spawns(self):
        return [(o["x"], o["y"]) for o in self.d["objects"] if o["kind"] == "spawn"]

    def connected(self, extra):
        """extra 칸을 막아도 모든 출구·사람·장치 옆에 걸어서 닿는가"""
        seen = self.dist(self.spawns(), extra)
        for o in self.d["objects"]:
            if o["kind"] in ("ambience", "growth", "anim"):
                continue
            cells = [(o["x"] + dx, o["y"] + dy) for dx in range(-1, o.get("w", 1) + 1) for dy in range(-1, o.get("h", 1) + 1)]
            if not any(c in seen for c in cells):
                return False
        return True

    def hub(self):
        o = next((o for o in self.d["objects"] if o["kind"] == "market"), None)
        if o:
            return (o["x"], o["y"])
        sp = self.spawns()
        return (self.w // 2, self.h // 2) if not sp else min(sp, key=lambda s: (s[0] - self.w // 2) ** 2 + (s[1] - self.h // 2) ** 2)

    def save(self):
        with open(self.path, "w", encoding="utf-8", newline="\n") as fp:
            json.dump(self.d, fp, ensure_ascii=False, separators=(",", ":"))

    def add(self, **o):
        base = dict(w=1, h=1, id="", npc="", town="", target="", spawn="", edge="", text="", name="", character="",
                    lines=[], variant=0, idx=0, sheet="", frames=0, fps=0.0, radius=0.0, type="vx", path=[])
        base.update(o)
        self.d["objects"].append(base)
        return base


def near_road(m, x, y):
    return any(m.inside(x + dx, y + dy) and m.d["road"][m.i(x + dx, y + dy)] for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2))


LIFE_KEYS = ("dock", "wander", "feed", "museum", "lion", "net", "glass", "workshop_glass", "workshop_lace", "inspector", "lost", "seats")


def life_spots(mid):
    """런타임에 놓이는 베네치아 장소(Resources/Venice/VeniceLife.json): 곤돌라 나루·벤치·공방 이름표 등"""
    path = os.path.join(ROOT, "Assets/PocketCaravan/Resources/Venice/VeniceLife.json")
    try:
        with open(path, encoding="utf-8") as fp:
            life = {m["id"]: m for m in json.load(fp)["maps"]}
    except (OSError, KeyError, ValueError):
        return []
    out = []
    for k in LIFE_KEYS:
        v = life.get(mid, {}).get(k) or []
        flat = [n for x in v for n in (x if isinstance(x, list) else [x])]
        out += [(flat[i], flat[i + 1]) for i in range(0, len(flat) - 1, 2)]
    return out


def busy_cells(m, pad):
    out = set()
    for x, y in life_spots(m.d["id"]):                     # 런타임 장소 둘레 2칸도 비운다
        out.update((x + dx, y + dy) for dx in range(-2, 3) for dy in range(-2, 3))
    for o in m.d["objects"]:
        if o["kind"] in ("ambience", "growth", "anim"):
            continue
        p = pad + (2 if o["kind"] in ("exit", "spawn") else 0)
        for dx in range(-p, o.get("w", 1) + p):
            for dy in range(-p, o.get("h", 1) + p):
                out.add((o["x"] + dx, o["y"] + dy))
    return out


CROWD_KINDS = ("worker", "villager", "walker")
CROWD_GAP = 4          # 2026-10-30 사람끼리(가로·세로 중 큰 값) 이만큼은 떨어뜨린다
MARKET_GAP = 6         # 장터에서 이만큼 안쪽은 장터 상인만


def cells_of(o):
    p = o.get("path") or []
    pts = [(p[i], p[i + 1]) for i in range(0, len(p) - 1, 2)] or [(o["x"], o["y"])]
    out = set()
    for (x0, y0), (x1, y1) in zip(pts, pts[1:] or pts):
        for t in range(max(abs(x1 - x0), abs(y1 - y0)) + 1):
            k = max(abs(x1 - x0), abs(y1 - y0)) or 1
            out.add((x0 + round((x1 - x0) * t / k), y0 + round((y1 - y0) * t / k)))
    return out


def spread_people(m):
    """장터·다른 사람에 붙어 선 일꾼·주민·걷는 사람을 장터에서 6~16걸음 떨어진 빈 길가로 옮긴다 (걷는 길은 같은 모양으로 함께 옮긴다)"""
    objs = m.d["objects"]
    market = next((o for o in objs if o["kind"] == "market"), None)
    hub = (market["x"], market["y"]) if market else m.hub()
    people = [o for o in objs if o["kind"] in PEOPLE_KINDS and o["kind"] != "sign"]
    life = set(life_spots(m.d["id"]))

    def crowded(o, cells):
        for c in cells:
            if market and max(abs(c[0] - hub[0]), abs(c[1] - hub[1])) < MARKET_GAP:
                return True
            for q in people:
                if q is o:
                    continue
                if any(max(abs(c[0] - a[0]), abs(c[1] - a[1])) < CROWD_GAP for a in cells_of(q)):
                    return True
            if any(max(abs(c[0] - a[0]), abs(c[1] - a[1])) < 3 for a in life):
                return True
        return False

    dist = m.dist([hub] + m.spawns())
    exits = [o for o in objs if o["kind"] in ("exit", "spawn")]
    moved = 0
    rng = random.Random("spread:" + m.d["id"])
    for o in [o for o in people if o["kind"] in CROWD_KINDS]:
        if not crowded(o, cells_of(o)):
            continue
        cand = [c for c, dd in dist.items() if 6 <= dd <= 16]
        rng.shuffle(cand)
        for c in cand:
            dx, dy = c[0] - o["x"], c[1] - o["y"]
            new = {(x + dx, y + dy) for x, y in cells_of(o)}
            if not all(m.walk(*p) and m.ground_ok(*p) and m.d["road"][m.i(*p)] < 3 for p in new):
                continue
            if any(abs(p[0] - e["x"]) <= e.get("w", 1) + 3 and abs(p[1] - e["y"]) <= e.get("h", 1) + 3 for p in new for e in exits):
                continue
            if crowded(o, new):
                continue
            o["x"] += dx; o["y"] += dy
            if o.get("path"):
                o["path"] = [v + (dx if i % 2 == 0 else dy) for i, v in enumerate(o["path"])]
            moved += 1
            break
    return moved


def spread_merchant(m, life):
    """떠돌이 상인 자리(VeniceLife "wander")가 장터·사람 곁이면 장터에서 7~14걸음 떨어진 길가로"""
    entry = life.get(m.d["id"])
    if not entry or not entry.get("wander"):
        return False
    hub = m.hub()
    people = [c for o in m.d["objects"] if o["kind"] in PEOPLE_KINDS for c in cells_of(o)]
    def ok(c):
        return (max(abs(c[0] - hub[0]), abs(c[1] - hub[1])) >= 7
                and all(max(abs(c[0] - p[0]), abs(c[1] - p[1])) >= CROWD_GAP for p in people))
    cur = tuple(entry["wander"][:2])
    if ok(cur):
        return False
    dist = m.dist([hub])
    cand = sorted((dd, c) for c, dd in dist.items() if 7 <= dd <= 14 and ok(c) and m.ground_ok(*c) and near_road(m, *c))
    if not cand:
        return False
    entry["wander"] = [cand[0][1][0], cand[0][1][1]]
    return True


def place_talkers(m, people):
    rng = random.Random(m.d["id"])
    dist = m.dist([m.hub()] + m.spawns())
    added, blocked = 0, set()
    for name, ch, lines in people:
        busy = busy_cells(m, 4)
        cand = [c for c, d in dist.items() if 3 <= d <= 60 and c not in busy and c not in blocked and m.ground_ok(*c)
                and 2 <= c[0] < m.w - 2 and 2 <= c[1] < m.h - 2 and m.d["road"][m.i(*c)] < 3]
        road_side = [c for c in cand if near_road(m, *c)]
        pool = road_side or cand
        rng.shuffle(pool)
        pool.sort(key=lambda c: -min([max(abs(c[0] - o["x"]), abs(c[1] - o["y"])) for o in m.d["objects"]
                                      if o["kind"] in PEOPLE_KINDS] or [99]))     # 다른 사람에게서 먼 칸부터
        for c in pool[:40]:
            if m.connected(blocked | {c}):
                m.add(kind="talker", x=c[0], y=c[1], name=name, character=ch, lines=lines)
                blocked.add(c)
                added += 1
                break
    return added


def map_names():
    out = {}
    for f in glob.glob(os.path.join(MAPS, "**", "*.json"), recursive=True):
        try:
            with open(f, encoding="utf-8") as fp:
                d = json.load(fp)
            out[d["id"]] = d.get("name", d["id"])
        except Exception:
            pass
    return out


SIGN_GAP = 8     # 2026-10-30 이정표·게시판끼리 이만큼(걸음 수, 가로+세로) 떨어뜨린다


def too_close(m, c, blocked):
    """이미 있는 이정표·게시판이나 이번에 세운 이정표와 SIGN_GAP보다 가까운가"""
    near = [(o["x"], o["y"]) for o in m.d["objects"] if o["kind"] in ("sign", "board")] + list(blocked)
    return any(abs(c[0] - p[0]) + abs(c[1] - p[1]) < SIGN_GAP for p in near)


def merge_close_signs(m):
    """원래 맵에 있던 이정표끼리 너무 붙어 있으면 한 개로 합친다 (글은 줄을 바꿔 잇고 남은 칸은 비운다)"""
    signs = [o for o in m.d["objects"] if o["kind"] == "sign" and o.get("type") != "vx"]
    gone = 0
    for a in signs:
        for b in signs:
            if a is b or a.get("_gone") or b.get("_gone"):
                continue
            if abs(a["x"] - b["x"]) + abs(a["y"] - b["y"]) < 4:
                a["text"] = a["text"] + "\n" + b["text"]
                b["_gone"] = True
                i = m.i(b["x"], b["y"])
                if m.d["palette"][m.d["obj"][i]] == SIGN_TILE:
                    m.d["obj"][i] = 0; m.d["blocked"][i] = 0
                gone += 1
    m.d["objects"] = [o for o in m.d["objects"] if not o.get("_gone")]
    return gone


def place_sign(m, start, text, blocked):
    """start에서 맵 안쪽(장터 쪽)으로 4~7걸음 들어온 길 칸 옆에 이정표를 세운다"""
    dist = m.dist([m.hub()])
    if start not in dist:
        near = [c for c in dist if abs(c[0] - start[0]) + abs(c[1] - start[1]) <= 3]
        if not near:
            return None
        start = min(near, key=lambda c: abs(c[0] - start[0]) + abs(c[1] - start[1]))
    path, c = [start], start
    while dist[c] > 0 and len(path) < 9:                      # 장터 쪽으로 한 걸음씩
        c = min(((c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)), key=lambda p: dist.get(p, 1 << 30))
        path.append(c)
    busy = busy_cells(m, 1)
    for max_road in (0, 2):                                    # 길 밖 칸 먼저, 없으면(리알토처럼 온통 포장) 큰길만 피한다
        for step in path[4:8] + path[2:4]:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1)):
                s = (step[0] + dx, step[1] + dy)
                if (m.inside(*s) and m.walk(*s) and s not in path and s not in busy and s not in blocked and m.ground_ok(*s)
                        and m.d["road"][m.i(*s)] <= max_road and not too_close(m, s, blocked) and m.connected(blocked | {s})):
                    return s
    return None


def place_signs(m, names):
    pal = m.d["palette"]
    if SIGN_TILE not in pal:
        pal.append(SIGN_TILE)
    tile = pal.index(SIGN_TILE)
    existing = [(o["x"], o["y"]) for o in m.d["objects"] if o["kind"] == "sign"]
    done, added, blocked = [], 0, set()
    exits = [o for o in m.d["objects"] if o["kind"] == "exit"]
    for o in exits:
        ex = (o["x"] + o["w"] // 2, o["y"] + o["h"] // 2)
        if any(t == o["target"] and abs(p[0] - ex[0]) + abs(p[1] - ex[1]) < 14 for t, p in done):
            continue
        if any(abs(p[0] - ex[0]) + abs(p[1] - ex[1]) < 10 for p in existing):   # 이미 이정표가 있는 출구
            done.append((o["target"], ex)); continue
        tname = names.get(o["target"], o["target"])
        if any(tname in so["text"] for so in m.d["objects"] if so["kind"] == "sign" and so.get("type") != "vx"):
            done.append((o["target"], ex)); continue                          # 원래 이정표가 이미 그 방향을 알려 준다
        text = f"{ARROW.get(o['edge'], '→')} {names.get(o['target'], o['target'])}"
        s = place_sign(m, ex, text, blocked)
        if s:
            add_sign(m, s, text, tile); blocked.add(s); added += 1
        done.append((o["target"], ex))
    market = next((o for o in m.d["objects"] if o["kind"] == "market"), None)
    if market and len({o["target"] for o in exits}) >= 2:      # 장터 옆 큰 이정표: 모든 방향
        seen, rows = set(), []
        for o in sorted(exits, key=lambda o: "NWES".index(o["edge"]) if o["edge"] in "NWES" else 4):
            if o["target"] not in seen:
                seen.add(o["target"]); rows.append(f"{ARROW.get(o['edge'], '→')} {names.get(o['target'], o['target'])}")
        text = "\n".join(rows[:3]) if len(rows) <= 3 else " · ".join(rows[:2]) + "\n" + " · ".join(rows[2:])
        busy = busy_cells(m, 1)
        dist = m.dist([(market["x"], market["y"] + 1), (market["x"], market["y"] - 1), (market["x"] + 1, market["y"]), (market["x"] - 1, market["y"])])
        spot = next((c for max_road in (0, 2) for c, dd in sorted(dist.items(), key=lambda kv: kv[1])
                     if 3 <= dd <= 10 and c not in busy and c not in blocked and m.ground_ok(*c) and not too_close(m, c, blocked)
                     and m.d["road"][m.i(*c)] <= max_road and m.connected(blocked | {c})), None)
        if spot:
            add_sign(m, spot, text, tile); blocked.add(spot); added += 1
    return added


def add_sign(m, c, text, tile):
    i = m.i(*c)
    m.add(kind="sign", x=c[0], y=c[1], text=text, path=[[m.d["obj"][i], m.d["blocked"][i]]])
    m.d["obj"][i] = tile
    m.d["blocked"][i] = 1


def main():
    names = map_names()
    total_t = total_s = 0
    life_path = os.path.join(ROOT, "Assets/PocketCaravan/Resources/Venice/VeniceLife.json")
    with open(life_path, encoding="utf-8") as fp:
        life_doc = json.load(fp)
    life = {e["id"]: e for e in life_doc["maps"]}
    life_changed = False
    for mid in TALKERS:
        path = next(iter(glob.glob(os.path.join(MAPS, "Venice*", mid + ".json"))), None)
        if not path:
            print("없음", mid); continue
        m = Map(path)
        merged = merge_close_signs(m)
        if merged:
            print(f"{mid}: 붙어 있던 이정표 {merged}개 합침")
        moved = spread_people(m)
        if moved:
            print(f"{mid}: 붙어 있던 사람 {moved}명 옮김")
        if spread_merchant(m, life):
            print(f"{mid}: 떠돌이 상인 자리 옮김 {life[mid]['wander']}"); life_changed = True
        s = place_signs(m, names)
        t = place_talkers(m, TALKERS[mid])
        m.save()
        total_t += t; total_s += s
        print(f"{mid}: 사람 +{t}/{len(TALKERS[mid])}, 이정표 +{s}")
    print("합계 사람", total_t, "이정표", total_s)
    if life_changed:
        with open(life_path, "w", encoding="utf-8", newline="\n") as fp:
            json.dump(life_doc, fp, ensure_ascii=False, separators=(",", ":"))


if __name__ == "__main__":
    main()
