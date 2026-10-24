"""Unity/Python이 함께 읽는 지역 등록 JSON. 기존 ID·좌표는 변경하지 않는다."""
import importlib.util
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REGIONS = Path('Assets/PocketCaravan/World/Regions')
SETTINGS = Path('Assets/PocketCaravan/World/settings.json')


def load_world(root=ROOT):
    root = Path(root)
    settings = json.loads((root / SETTINGS).read_text(encoding='utf-8'))
    if settings.get('schemaVersion') != 1:
        raise ValueError('Unsupported world settings schemaVersion')
    regions = []
    for path in sorted((root / REGIONS).glob('*.json')):
        region = json.loads(path.read_text(encoding='utf-8'))
        if region.get('schemaVersion') != 1:
            raise ValueError(f'Unsupported region schemaVersion: {path.name}')
        regions.append(region)
    if not regions:
        raise ValueError('No region manifests')
    return settings, regions


def map_entries(region_id, root=ROOT):
    _, regions = load_world(root)
    matches = [r for r in regions if r['regionId'] == region_id]
    if len(matches) != 1:
        raise ValueError(f'Region not uniquely registered: {region_id}')
    return matches[0]['maps']


def generated_maps(region_id='goryeo'):
    """Only load explicitly registered generators from this source directory; never eval a string."""
    result = []
    source_entries = json.loads((ROOT/'Assets/PocketCaravan/World/Terrain/legacy_goryeo_sources.json').read_text())['maps'] if region_id=='goryeo' else map_entries(region_id)
    for entry in source_entries:
        generator = entry.get('generator', '')
        if not re.fullmatch(r'[a-z][a-z0-9_]*:[a-z][a-z0-9_]*', generator):
            raise ValueError(f'No valid Python generator for {entry["mapId"]}')
        module, function = generator.split(':')
        path = Path(__file__).resolve().parent / (module + '.py')
        spec = importlib.util.spec_from_file_location('mapgen_' + module, path)
        loaded = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(loaded)
        world_map = getattr(loaded, function)()
        if world_map.id != entry['mapId']:
            raise ValueError(f'Generator mapId mismatch: {entry["mapId"]}')
        result.append(world_map)
    return result


def atlas_content():
    """지도에 함께 표시할 제작 구역만 생성한다. 타 팀의 생성기를 자동 실행하지 않는다."""
    settings, _ = load_world()
    ids = settings.get('atlasRegions', ['goryeo'])
    if not isinstance(ids, list) or not ids or len(ids) != len(set(ids)):
        raise ValueError('atlasRegions must be a nonempty unique region list')
    entries, maps = [], []
    for rid in ids:
        entries.extend(map_entries(rid))
        maps.extend(generated_maps(rid))
    from continuous import build
    from goryeo_art import apply
    from compact import build as compact
    from broad_regions import build as broad
    final = apply(broad(compact(build(maps))))
    by_id={m.id:m for m in final}
    return entries, [by_id[e["mapId"]] for e in entries]
