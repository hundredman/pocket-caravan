"""Read-only, standard-library checks for region integration. No Unity or asset pack required."""
import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/MapGen'))
from world_registry import load_world


def validate(root=ROOT, require_scenes=True):
    root = Path(root)
    errors, connections = [], []

    def check(ok, message):
        if not ok:
            errors.append(message)
        return ok

    def asset_path(value, extension):
        return isinstance(value, str) and value.startswith('Assets/PocketCaravan/') and '\\' not in value and '..' not in value.split('/') and value.endswith(extension)

    try:
        settings, regions = load_world(root)
    except (OSError, ValueError, TypeError) as exc:
        return [f'Registry: {exc}'], []
    maps, coords, paths, region_ids, npc_ids = {}, set(), set(), set(), set()
    for region in regions:
        rid = region.get('regionId')
        if not check(isinstance(rid, str) and rid and rid.casefold() not in region_ids, f'Region ID missing/duplicate: {rid}'):
            continue
        region_ids.add(rid.casefold())
        rp = region.get('regionAssetPath')
        if check(asset_path(rp, '.asset'), f'Invalid region asset path: {rid}'):
            check((root / rp).is_file(), f'Missing RegionData: {rp}')
            if (root / rp).is_file():
                text = (root / rp).read_text(encoding='utf-8')
                check(bool(re.search(r'^  id: ' + re.escape(rid) + r'\s*$', text, re.M)), f'RegionData ID mismatch: {rid}')
        local_npcs = set()
        for npc in region.get('npcs', []):
            nid = npc.get('id')
            if check(isinstance(nid, str) and nid and nid.casefold() not in npc_ids, f'NPC ID missing/duplicate: {nid}'):
                npc_ids.add(nid.casefold()); local_npcs.add(nid)
        check(bool(region.get('maps')), f'Region has no maps: {rid}')
        for entry in region.get('maps', []):
            mid = entry.get('mapId')
            if not check(isinstance(mid, str) and mid and mid.casefold() not in {v.casefold() for v in maps}, f'Map ID missing/duplicate: {mid}'):
                continue
            coord = (entry.get('worldX'), entry.get('worldY'))
            check(all(type(v) is int for v in coord), f'Invalid coordinate: {mid}')
            check(coord not in coords, f'Duplicate coordinate: {mid} {coord}')
            coords.add(coord)
            jp, sp = entry.get('jsonPath'), entry.get('scenePath')
            if not check(asset_path(jp, '.json') and asset_path(sp, '.unity'), f'Invalid map/scene path: {mid}'):
                continue
            check(Path(sp).stem == mid, f'Scene name must match mapId: {mid}')
            for value in (jp, sp):
                check(value.casefold() not in paths, f'Duplicate content path: {value}')
                paths.add(value.casefold())
            if require_scenes:
                check((root / sp).is_file(), f'Missing generated scene: {sp}')
            try:
                data = json.loads((root / jp).read_text(encoding='utf-8'))
            except (OSError, ValueError) as exc:
                errors.append(f'{mid}: {exc}'); continue
            maps[mid] = data
            check(data.get('id') == mid, f'JSON mapId mismatch: {mid}')
            w, h = data.get('w', 0), data.get('h', 0)
            if not check(type(w) is int and type(h) is int and w > 0 and h > 0, f'Invalid dimensions: {mid}'):
                continue
            for layer in ('ground', 'deco', 'obj', 'high', 'road', 'blocked'):
                values = data.get(layer)
                if not check(isinstance(values, list) and len(values) == w*h, f'Invalid layer length: {mid}/{layer}'):
                    continue
                upper = 3 if layer == 'road' else 1 if layer == 'blocked' else len(data.get('palette', []))-1
                check(all(type(v) is int and 0 <= v <= upper for v in values), f'Invalid tile value: {mid}/{layer}')
            spawns = set()
            for obj in data.get('objects', []):
                if obj.get('kind') == 'spawn':
                    sid = obj.get('id')
                    check(bool(sid) and str(sid).casefold() not in spawns, f'Spawn ID missing/duplicate: {mid}/{sid}')
                    spawns.add(str(sid).casefold())
                if obj.get('kind') == 'npc':
                    check(obj.get('npc') in local_npcs, f'Unregistered NPC: {mid}/{obj.get("npc")}')
                if obj.get('kind') == 'villager':
                    check(bool(region.get('villagers')), f'No region villagers: {mid}')
                if obj.get('kind') in ('spawn', 'exit'):
                    x, y, ow, oh = (obj.get(k, 1 if k in ('w', 'h') else -1) for k in ('x', 'y', 'w', 'h'))
                    check(ow > 0 and oh > 0 and x >= 0 and y >= 0 and x+ow <= w and y+oh <= h, f'Port out of bounds: {mid}/{obj.get("id", "exit")}')
    check(settings.get('startMapId') in maps, 'Start map not registered')
    start = maps.get(settings.get('startMapId'), {})
    check(any(o.get('kind') == 'spawn' and o.get('id') == 'S0' for o in start.get('objects', [])), 'Start map has no S0 spawn')
    for mid, data in maps.items():
        for obj in data.get('objects', []):
            if obj.get('kind') != 'exit':
                continue
            target = maps.get(obj.get('target'))
            if not check(target is not None, f'Unknown exit target: {mid} -> {obj.get("target")}'):
                continue
            dest = next((s for s in target.get('objects', []) if s.get('kind') == 'spawn' and s.get('id') == obj.get('spawn')), None)
            if check(dest is not None, f'Unknown destination spawn: {mid} -> {obj.get("target")}/{obj.get("spawn")}'):
                check((obj.get('w', 1), obj.get('h', 1)) == (dest.get('w', 1), dest.get('h', 1)), f'Port width/axis mismatch: {mid} -> {obj.get("target")}')
            if not obj.get('oneWay', False):
                check(any(e.get('kind') == 'exit' and e.get('target') == mid for e in target.get('objects', [])), f'Missing return exit: {mid} -> {obj.get("target")}')
            connections.append(dict(source=mid, target=obj.get('target'), spawn=obj.get('spawn')))
    # Unity's text serialization: match only the top-level content ID, never nested stock IDs.
    ids = {}
    for asset in sorted((root / 'Assets/PocketCaravan/Data').rglob('*.asset')):
        text = asset.read_text(encoding='utf-8')
        kind = re.search(r'^  m_EditorClassIdentifier: .*::PocketCaravan\.(\w+)\s*$', text, re.M)
        value = re.search(r'^  id: (.+)$', text, re.M)
        if kind and value:
            key = (kind[1], value[1].strip().strip('"').casefold())
            check(key not in ids, f'Duplicate {key[0]} ID: {key[1]} ({asset.name})')
            ids[key] = asset
    return errors, connections


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--allow-unbuilt-scenes', action='store_true')
    args = parser.parse_args()
    errors, connections = validate(args.root, not args.allow_unbuilt_scenes)
    for error in errors:
        print('ERROR:', error)
    print(f'{len(errors)} errors; {len(connections)} registered exits checked. Scenes and saves unchanged.')
    return bool(errors)


if __name__ == '__main__':
    sys.exit(main())
