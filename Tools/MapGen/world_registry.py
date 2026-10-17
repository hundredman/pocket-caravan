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
    for entry in map_entries(region_id):
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
