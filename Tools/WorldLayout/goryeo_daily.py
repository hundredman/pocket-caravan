"""Promote the approved daily policy; no scene, map, common code or art changes."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT / 'ArtReview/Goryeo-Daily-20261106'


def promote():
    source = REVIEW / 'approved-source.json'
    approval = json.loads((REVIEW / 'approval.json').read_text())
    if approval['status'] != 'approved' or hashlib.sha256(source.read_bytes()).hexdigest() != approval['sourceSha256']:
        raise ValueError('Missing/mismatched daily activity approval')
    value = json.loads(source.read_text())
    if value['status'] != 'approved' or value['reward'] != 3 or value['seconds'] != 3:
        raise ValueError('Unexpected approved policy')
    target = ROOT / 'Assets/PocketCaravan/Resources/Goryeo/Daily/Config.json'
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(source.read_bytes())
    return target


if __name__ == '__main__':
    print(promote())
