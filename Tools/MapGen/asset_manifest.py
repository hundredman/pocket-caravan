"""A2 asset catalogue and final compact-map validation. Does not modify inputs."""
from pathlib import Path
import re

LAYERS = ('ground', 'deco', 'obj', 'high')
STATUSES = ('draft', 'pending', 'approved', 'changes_requested')


def integer(value, minimum=0):
    return type(value) is int and value >= minimum


def safe_path(root, value):
    if not isinstance(value, str) or not value or Path(value).is_absolute():
        return None
    target = (Path(root) / value).resolve()
    return target if target.is_relative_to(Path(root).resolve()) else None


def validate_catalogue(document, root):
    """Return (errors, indexed assets). An invalid catalogue is never used for maps."""
    errors, assets, tile_owners = [], {}, {}

    def fail(code, location, message):
        errors.append(dict(code=code, location=location, message=message))

    if not isinstance(document, dict) or document.get('schemaVersion') != 1 or type(document.get('schemaVersion')) is not int:
        fail('catalogue_schema', '$', 'schemaVersion must be integer 1')
        return errors, assets
    entries = document.get('assets')
    if not isinstance(entries, list) or not entries:
        fail('catalogue_assets', 'assets', 'a nonempty asset list is required')
        return errors, assets
    profiles = document.get('profiles', {})
    if not isinstance(profiles, dict):
        fail('profiles', 'profiles', 'profiles must be an object')
        return errors, assets
    for name, profile in profiles.items():
        if not isinstance(profile, dict):
            fail('profile', name, 'profile must be an object')
            continue
        if set(profile) - {'pixelsPerCell', 'palette', 'binaryAlpha'}:
            fail('profile_fields', name, 'unknown pixel profile fields')
        pixels = profile.get('pixelsPerCell')
        if not isinstance(pixels, list) or len(pixels) != 2 or not all(integer(v, 1) for v in pixels):
            fail('profile_size', name, 'pixelsPerCell must be two positive integers')
        colors = profile.get('palette')
        if colors is not None and (not isinstance(colors, list) or not colors or any(not isinstance(c, str) or not re.fullmatch(r'#[0-9a-fA-F]{6}', c) for c in colors)):
            fail('profile_palette', name, 'palette must contain #RRGGBB colors')
        if profile.get('binaryAlpha', True) is not True:
            fail('profile_alpha', name, 'binaryAlpha must remain true under the current art rules')

    if errors:
        return errors, assets

    def check_file(value, where, image=False, size=None, profile=None):
        path = safe_path(root, value)
        if path is None or not path.is_file():
            fail('asset_file', where, 'file missing or outside root: ' + str(value))
            return
        if not image:
            return
        profile = profile or {}
        pixels = profile.get('pixelsPerCell')
        try:
            from PIL import Image
            with Image.open(path) as source:
                if source.format != 'PNG':
                    fail('image_format', where, 'exported images must be PNG')
                im = source.convert('RGBA')
                if size and pixels and im.size != (size[0] * pixels[0], size[1] * pixels[1]):
                    fail('image_size', where, 'image dimensions do not match configured cell size')
                if any(a not in (0, 255) for a in im.getchannel('A').tobytes()):
                    fail('image_alpha', where, 'semi-transparent pixels are not allowed')
                palette = profile.get('palette')
                if isinstance(palette, list) and palette and all(isinstance(c, str) and re.fullmatch(r'#[0-9a-fA-F]{6}', c) for c in palette):
                    allowed = {tuple(bytes.fromhex(c[1:])) for c in palette}
                    raw = im.tobytes()
                    if any(raw[i + 3] and tuple(raw[i:i + 3]) not in allowed for i in range(0, len(raw), 4)):
                        fail('image_palette', where, 'visible colors outside configured palette')
                return im
        except (ImportError, OSError, ValueError) as exc:
            fail('image_read', where, str(exc))

    def shape(value, where, owner, profile):
        if not isinstance(value, dict):
            fail('asset_shape', where, 'shape must be an object')
            return
        size = value.get('size')
        if not isinstance(size, list) or len(size) != 2 or not all(integer(v, 1) for v in size):
            fail('asset_size', where, 'size must be positive width and height in cells')
            return
        full_image = check_file(value.get('image'), where + '.image', True, size, profile)
        cell_size = None
        if full_image is not None:
            if full_image.width % size[0] or full_image.height % size[1]:
                fail('image_grid', where, 'whole PNG cannot be divided into declared cell grid')
            else:
                cell_size = (full_image.width // size[0], full_image.height // size[1])
        tiles = value.get('tiles')
        if not isinstance(tiles, list) or not tiles:
            fail('asset_tiles', where, 'nonempty tile mapping required')
            return
        cells, slots = set(), set()
        for tile in tiles:
            if not isinstance(tile, dict):
                fail('asset_tile', where, 'tile must be an object')
                continue
            dx, dy, layer, key = (tile.get(k) for k in ('dx', 'dy', 'layer', 'tile'))
            if not integer(dx) or not integer(dy) or dx >= size[0] or dy >= size[1] or layer not in LAYERS or not isinstance(key, str) or not key:
                fail('asset_tile', where, 'tile coordinate/layer/key invalid')
                continue
            if (layer, dx, dy) in slots:
                fail('duplicate_tile_slot', where, 'duplicate layer/cell mapping')
            slots.add((layer, dx, dy)); cells.add((dx, dy))
            tile_image = check_file(tile.get('image'), where + '.' + key, True)
            if tile_image is not None and cell_size:
                cw, ch = cell_size
                crop = full_image.crop((dx * cw, dy * ch, (dx + 1) * cw, (dy + 1) * ch))
                if tile_image.size != crop.size or tile_image.tobytes() != crop.tobytes():
                    fail('tile_pixels', where + '.' + key, 'exported tile differs from the whole registered image')
            if key in tile_owners and tile_owners[key] != owner:
                fail('duplicate_tile_owner', where, 'tile key reused by different asset IDs: ' + key)
            tile_owners[key] = owner
        if len(cells) != size[0] * size[1]:
            fail('incomplete_definition', where, 'every cell in full footprint needs a tile mapping, including transparent cells')

    for i, asset in enumerate(entries):
        where = 'assets[' + str(i) + ']'
        if not isinstance(asset, dict):
            fail('asset', where, 'asset must be an object')
            continue
        aid = asset.get('id')
        if not isinstance(aid, str) or not re.fullmatch(r'[a-z][a-z0-9_]*', aid) or aid in assets:
            fail('asset_id', where, 'asset ID missing, invalid or duplicate')
            continue
        assets[aid] = asset
        if not isinstance(asset.get('purpose'), str) or not asset['purpose'].strip():
            fail('asset_purpose', aid, 'purpose is required')
        source = asset.get('source')
        if not isinstance(source, dict) or source.get('kind') not in ('authored', 'ai_processed', 'external'):
            fail('asset_source', aid, 'source kind must be authored, ai_processed or external')
        else:
            check_file(source.get('path'), aid + '.source')
            if source['kind'] == 'external' and not source.get('license'):
                fail('source_license', aid, 'external source needs license information')
        review = asset.get('review')
        if not isinstance(review, dict) or review.get('status') not in STATUSES:
            fail('asset_review', aid, 'review status is required')
        elif review['status'] == 'approved':
            check_file(review.get('evidence'), aid + '.review.evidence')
        rules = asset.get('rules', {})
        if not isinstance(rules, dict):
            fail('asset_rules', aid, 'rules must be an object')
        else:
            if set(rules) - {'onTerrain', 'adjacentTo', 'forbidRoad'}:
                fail('asset_rules', aid, 'unknown placement rule fields')
            for field in ('onTerrain', 'adjacentTo'):
                if field in rules and (not isinstance(rules[field], list) or not rules[field] or any(not isinstance(t, str) or not t for t in rules[field])):
                    fail('asset_rules', aid, field + ' must be a nonempty tag list')
            if 'forbidRoad' in rules and type(rules['forbidRoad']) is not bool:
                fail('asset_rules', aid, 'forbidRoad must be boolean')
        profile = None
        if 'profile' in asset:
            profile = profiles.get(asset['profile']) if isinstance(asset['profile'], str) else None
            if not isinstance(profile, dict):
                fail('asset_profile', aid, 'unknown profile')
                profile = None
        if asset.get('kind') == 'object':
            shape(asset, aid, aid, profile)
        elif asset.get('kind') == 'modular':
            pieces, required = asset.get('pieces'), asset.get('requiredPieces')
            if not isinstance(pieces, dict) or not pieces or not isinstance(required, list) or not required or any(not isinstance(p, str) or not p for p in required):
                fail('modular_pieces', aid, 'pieces and requiredPieces must be nonempty')
            else:
                for part in required:
                    if part not in pieces:
                        fail('missing_piece', aid, 'required modular piece missing: ' + part)
                for part, definition in pieces.items():
                    shape(definition, aid + '.' + part, aid, profile)
        else:
            fail('asset_kind', aid, 'kind must be object or modular')
    return errors, assets


def validate_map(data, assets, require_approval=True):
    """Validate placement claims against every rendered cell in a compact JSON map."""
    errors = []

    def fail(code, location, message):
        errors.append(dict(code=code, location=location, message=message))

    if not isinstance(data, dict) or data.get('assetPlacementVersion') != 1 or type(data.get('assetPlacementVersion')) is not int:
        fail('placement_schema', '$', 'assetPlacementVersion 1 required; legacy maps are not certified')
        return errors
    w, h = data.get('w'), data.get('h')
    palette = data.get('palette')
    if not integer(w, 1) or not integer(h, 1) or not isinstance(palette, list) or not palette or palette[0] != '' or any(not isinstance(k, str) for k in palette):
        fail('map_schema', '$', 'positive dimensions and string palette with empty index 0 required')
        return errors
    for layer in LAYERS:
        values = data.get(layer)
        if not isinstance(values, list) or len(values) != w * h or any(not integer(v) or v >= len(palette) for v in values):
            fail('map_layer', layer, 'flat palette index layer required')
    placements = data.get('assetPlacements')
    if not isinstance(placements, list):
        fail('placements', '$', 'assetPlacements list required')
    if errors:
        return errors
    terrain = data.get('terrainTags')
    road = data.get('road')
    terrain_ok = isinstance(terrain, list) and len(terrain) == w * h and all(isinstance(v, str) and v for v in terrain)
    road_ok = isinstance(road, list) and len(road) == w * h and all(integer(v) for v in road)
    expected, ids = {}, set()
    for n, placement in enumerate(placements):
        where = 'assetPlacements[' + str(n) + ']'
        if not isinstance(placement, dict):
            fail('placement', where, 'placement must be an object')
            continue
        pid, aid = placement.get('id'), placement.get('assetId')
        if not isinstance(pid, str) or not pid or pid in ids:
            fail('placement_id', where, 'placement ID missing or duplicate')
        else:
            ids.add(pid)
        asset = assets.get(aid) if isinstance(aid, str) else None
        if asset is None:
            fail('unregistered_asset', where, 'unknown asset: ' + str(aid))
            continue
        if placement.get('purpose') != asset['purpose']:
            fail('wrong_purpose', where, 'placement purpose differs from registered purpose')
        if require_approval and asset['review']['status'] != 'approved':
            fail('approval_required', where, 'user review approval required for production use')
        # Only these fields are allowed; crop, recolor, scale, rotation are not placement operations.
        if set(placement) - {'id', 'assetId', 'x', 'y', 'purpose', 'piece'}:
            fail('placement_transform', where, 'unexpected placement fields; register variants as separate assets')
        definition = asset
        if asset['kind'] == 'modular':
            definition = asset['pieces'].get(placement.get('piece')) if isinstance(placement.get('piece'), str) else None
            if definition is None:
                fail('unknown_piece', where, 'a registered modular piece is required')
                continue
        elif 'piece' in placement:
            fail('object_piece', where, 'single objects cannot be placed as pieces')
        x, y = placement.get('x'), placement.get('y')
        sw, sh = definition['size']
        if not integer(x) or not integer(y) or x + sw > w or y + sh > h:
            fail('placement_bounds', where, 'whole footprint must fit inside map')
            continue
        footprint = {(x + dx, y + dy) for dy in range(sh) for dx in range(sw)}
        rules = asset.get('rules', {})
        if ('onTerrain' in rules or 'adjacentTo' in rules) and not terrain_ok:
            fail('missing_terrain_context', where, 'terrainTags required for declared placement rule')
        elif terrain_ok:
            if 'onTerrain' in rules and any(terrain[cy * w + cx] not in rules['onTerrain'] for cx, cy in footprint):
                fail('wrong_terrain', where, 'footprint violates onTerrain rule')
            if 'adjacentTo' in rules:
                neighbors = {(cx + dx, cy + dy) for cx, cy in footprint for dx, dy in ((0, 1), (0, -1), (1, 0), (-1, 0))}
                neighbors = {(cx, cy) for cx, cy in neighbors - footprint if 0 <= cx < w and 0 <= cy < h}
                if not any(terrain[cy * w + cx] in rules['adjacentTo'] for cx, cy in neighbors):
                    fail('wrong_adjacency', where, 'no allowed four-neighbor terrain outside footprint')
        if rules.get('forbidRoad'):
            if not road_ok:
                fail('missing_road_context', where, 'road layer required for forbidRoad rule')
            elif any(road[cy * w + cx] > 0 for cx, cy in footprint):
                fail('road_forbidden', where, 'footprint overlaps road')
        for tile in definition['tiles']:
            slot = (tile['layer'], x + tile['dx'], y + tile['dy'])
            if slot in expected:
                fail('placement_overlap', where, 'two placements own the same rendered layer/cell')
            expected[slot] = tile['tile']
    known = {tile['tile'] for asset in assets.values() for definition in ([asset] if asset['kind'] == 'object' else asset['pieces'].values()) for tile in definition['tiles']}
    for layer in LAYERS:
        for index, value in enumerate(data[layer]):
            key = palette[value]
            slot = (layer, index % w, index // w)
            if slot in expected:
                if key != expected[slot]:
                    fail('incomplete_placement', str(slot), 'expected ' + expected[slot] + ', found ' + repr(key))
            elif key:
                code = 'unrecorded_tile' if key in known else 'unregistered_tile'
                fail(code, str(slot), 'rendered tile has no registered placement: ' + key)
    return errors


def validate(document, maps, root, require_approval=True):
    errors, assets = validate_catalogue(document, root)
    if errors:
        return errors
    for name, data in maps:
        for error in validate_map(data, assets, require_approval):
            errors.append(dict(error, location=str(name) + ':' + error['location']))
    return errors
