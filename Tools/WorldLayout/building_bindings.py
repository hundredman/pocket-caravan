"""Approved whole building/boat source bindings; no image transformation."""
import hashlib,json
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
APPROVAL=json.loads((ROOT/'ArtReview/Approved/BuildingShip-Renewal-v1/approval.json').read_text())
ASSETS={a['name']:a for a in APPROVAL['assets']}
def path(name):return ROOT/ASSETS[name]['png']
def image(name):
    im=Image.open(path(name)).convert('RGBA');a=ASSETS[name]
    if APPROVAL['status']!='approved' or list(im.size)!=a['size'] or hashlib.sha256(im.tobytes()).hexdigest()!=a['rgbaSha256']:
        raise ValueError('Approved building/ship changed: '+name)
    return im
