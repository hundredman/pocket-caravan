"""Fresh-clone tile references: scenes must not depend on unshared local Tile assets."""
import json
import re
import unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure'

def guid(path):
    return re.search(r'(?m)^guid: (\w+)$',path.read_text())[1]

class MapTileDependencies(unittest.TestCase):
    def test_registered_scene_tile_and_sprite_dependencies_are_present(self):
        for manifest in (ROOT/'Assets/PocketCaravan/World/Regions').glob('*.json'):
            for entry in json.loads(manifest.read_text())['maps']:
                m=json.loads((ROOT/entry['jsonPath']).read_text())
                scene=(ROOT/entry['scenePath']).read_text()
                used={i for layer in ('ground','deco','obj','high')for i in m.get(layer,[])}
                used.update(cell['tile']for cell in m.get('cameraBackdrop',[]))
                for index,name in enumerate(m['palette']):
                    if not name:continue
                    tile=BASE/'TileAssets'/(name+'.asset');png=BASE/'Tiles'/(name+'.png')
                    for p in (tile,png,Path(str(tile)+'.meta'),Path(str(png)+'.meta')):
                        self.assertTrue(p.is_file(),str(p))
                    # Unused palette entries need files, but have no scene reference.
                    if index in used:self.assertTrue(guid(Path(str(tile)+'.meta')) in scene,(entry['mapId'],name))
                    self.assertIn(guid(Path(str(png)+'.meta')),tile.read_text(),name)

if __name__=='__main__':unittest.main()
