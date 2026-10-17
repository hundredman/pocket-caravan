"""Optional obstacle placement contract. No Unity/Pillow dependency."""
import math


def validate_placements(data, definitions):
    errors, seen = [], set()
    placements = data.get('obstacles', [])
    if placements is None:
        return errors  # Unity JsonUtility may write a null optional array.
    if not isinstance(placements, list):
        return ['obstacles must be a list']
    for p in placements:
        if not isinstance(p, dict):
            errors.append('obstacle must be an object'); continue
        pid = p.get('id')
        if not isinstance(pid, str) or not pid.strip() or pid in seen:
            errors.append('obstacle ID missing/duplicate')
        else:
            seen.add(pid)
        d = definitions.get(p.get('definitionId'))
        if d is None:
            errors.append(f'{pid}: unknown obstacle definition'); continue
        values = [p.get(k, 0) for k in ('x', 'y', 'width', 'height')]
        if any(type(v) is not int or v < 0 for v in values):
            errors.append(f'{pid}: invalid obstacle coordinates/size'); continue
        x, y, w, h = values
        w, h = w or d['width'], h or d['height']
        if w <= 0 or h <= 0 or x+w > data['w'] or y+h > data['h']:
            errors.append(f'{pid}: obstacle out of bounds')
        phase = p.get('phaseOffset', 0)
        if type(phase) not in (int, float) or not math.isfinite(phase):
            errors.append(f'{pid}: invalid phase offset')
        if type(p.get('enabled', True)) is not bool:
            errors.append(f'{pid}: enabled must be boolean')
    return errors
