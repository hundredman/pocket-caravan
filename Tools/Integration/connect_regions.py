"""문명 사이 연결을 맵 JSON에 넣는다 (2026-10-27 통합 브랜치).

World/Connections/region_links.json의 각 연결에서, "by"가 없는 쪽(다른 담당자 맵)에 출구·스폰을 넣고 표지판 글을 바꾼다.
"by"가 있는 쪽은 그 생성기가 직접 만들므로 확인만 한다.
같은 연결은 지우고 다시 넣으므로 여러 번 실행해도 된다. 각 지역 생성기로 맵을 다시 만들었으면 이 스크립트를 다시 실행한다.
씬 반영: Unity 메뉴 Pocket Caravan > 통합 > 지역 연결 맵 씬 다시 만들기
"""
import glob, json, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
A = os.path.join(ROOT, "Assets", "PocketCaravan")


def registry():
    reg = {}
    for f in sorted(glob.glob(os.path.join(A, "World", "Regions", "*.json"))):
        for m in json.load(open(f, encoding="utf-8"))["maps"]:
            reg[m["mapId"]] = os.path.join(ROOT, m["jsonPath"])
    return reg


def place(m, edge, start, n):
    """출구 칸(가장자리)과 들어올 때 서는 칸(한 칸 안쪽)."""
    W, H = m["w"], m["h"]
    if edge in "EW":
        x = W - 1 if edge == "E" else 0
        return dict(x=x, y=start, w=1, h=n), dict(x=x - 1 if edge == "E" else 1, y=start, w=1, h=n)
    y = H - 1 if edge == "S" else 0
    return dict(x=start, y=y, w=n, h=1), dict(x=start, y=y - 1 if edge == "S" else 1, w=n, h=1)


def blank(kind, **kw):
    o = dict(w=1, h=1, id="", npc="", town="", target="", spawn="", edge="", text="", name="", character="", lines=[],
             variant=0, idx=0, sheet="", frames=0, fps=0.0, radius=0.0, type="", path=[], kind=kind, x=0, y=0)
    o.update(kw)
    return o


def main():
    spec = json.load(open(os.path.join(A, "World", "Connections", "region_links.json"), encoding="utf-8"))
    reg = registry(); n = spec.get("width", 4)
    maps, errors = {}, []
    def load(mid):
        if mid not in maps:
            maps[mid] = json.load(open(reg[mid], encoding="utf-8"))
        return maps[mid]
    for link in spec["links"]:
        for me, other in ((link["a"], link["b"]), (link["b"], link["a"])):
            mid, oid = me["map"], other["map"]
            if mid not in reg or oid not in reg:
                errors.append(f"등록 안 된 맵: {mid} / {oid}"); continue
            m = load(mid)
            if me.get("by"):
                ok = any(o["kind"] == "exit" and o["target"] == oid and o["spawn"] == "link_" + mid for o in m["objects"]) \
                     and any(o["kind"] == "spawn" and o["id"] == "link_" + oid for o in m["objects"])
                if not ok: errors.append(f"{mid}: {me['by']}에 {oid} 연결이 없음 (생성기 다시 실행)")
                continue
            m["objects"] = [o for o in m["objects"] if not (o["kind"] == "exit" and o["target"] == oid and o["spawn"].startswith("link_"))
                            and not (o["kind"] == "spawn" and o["id"] == "link_" + oid)]
            ex, sp = place(m, me["edge"], me["start"], n)
            W = m["w"]
            for c in (ex, sp):
                for dy in range(c["h"]):
                    for dx in range(c["w"]):
                        if m["blocked"][(c["y"] + dy) * W + c["x"] + dx]:
                            errors.append(f"{mid}: 막힌 칸 ({c['x'] + dx},{c['y'] + dy})")
            m["objects"].append(blank("exit", target=oid, spawn="link_" + mid, edge=me["edge"], **ex))
            m["objects"].append(blank("spawn", id="link_" + oid, edge=me["edge"], **sp))
    for s in spec.get("signs", []):
        m = load(s["map"])
        hit = [o for o in m["objects"] if o["kind"] == "sign" and o["x"] == s["x"] and o["y"] == s["y"]]
        if not hit: errors.append(f"{s['map']}: 표지판 ({s['x']},{s['y']}) 없음"); continue
        hit[0]["text"] = s["text"]
    if errors:
        print("\n".join("ERROR: " + e for e in errors)); return 1
    for mid, m in maps.items():
        if any(mid == l[k]["map"] and not l[k].get("by") for l in spec["links"] for k in "ab") or any(s["map"] == mid for s in spec.get("signs", [])):
            with open(reg[mid], "w", encoding="utf-8") as fp:
                json.dump(m, fp, ensure_ascii=False, separators=(",", ":"))
            print("updated", os.path.relpath(reg[mid], ROOT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
