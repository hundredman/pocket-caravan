"""Fresh-clone tile references: scenes must not depend on unshared local Tile assets."""
import json
import re
import unittest
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/PocketCaravan/Art/NinjaAdventure'

def guid(meta_text):
    return re.search(r'(?m)^guid: (\w+)$',meta_text)[1]

def tile_files(name):
    """(path, exists, text) for the Tile asset, its png and both metas. png itself is only checked for existence."""
    tile=BASE/'TileAssets'/(name+'.asset');png=BASE/'Tiles'/(name+'.png')
    out=[]
    for p,read in ((tile,True),(png,False),(Path(str(tile)+'.meta'),True),(Path(str(png)+'.meta'),True)):
        ok=p.is_file()
        out.append((p,ok,p.read_text(encoding='utf-8') if ok and read else None))
    return out

class MapTileDependencies(unittest.TestCase):
    def test_registered_scene_tile_and_sprite_dependencies_are_present(self):
        # 11/19: same checks as before, made fast enough to run (it used to take hours on Windows):
        #   each scene's guid references are collected once into a set instead of searching the whole scene
        #   text (131MB over 54 maps) once per palette entry (~23k), and the ~10k distinct tiles' files are
        #   read once, many at a time (opening a small file costs ~13ms on Windows with real-time scanning).
        maps=[]
        for manifest in (ROOT/'Assets/PocketCaravan/World/Regions').glob('*.json'):
            for entry in json.loads(manifest.read_text(encoding='utf-8'))['maps']:
                maps.append((entry,json.loads((ROOT/entry['jsonPath']).read_text(encoding='utf-8'))))
        names=sorted({name for _,m in maps for name in m['palette'] if name})
        with ThreadPoolExecutor(32) as pool:files=dict(zip(names,pool.map(tile_files,names)))
        for entry,m in maps:
            scene=set(re.findall(r'guid: (\w+)',(ROOT/entry['scenePath']).read_text(encoding='utf-8')))
            used={i for layer in ('ground','deco','obj','high')for i in m.get(layer,[])}
            used.update(cell['tile']for cell in m.get('cameraBackdrop',[]))
            for index,name in enumerate(m['palette']):
                if not name:continue
                (_,_,tile_text),_,(_,_,tile_meta),(_,_,png_meta)=found=files[name]
                for p,ok,_ in found:
                    self.assertTrue(ok,str(p))
                # Unused palette entries need files, but have no scene reference.
                if index in used:self.assertTrue(guid(tile_meta) in scene,(entry['mapId'],name))
                self.assertIn(guid(png_meta),tile_text,name)

if __name__=='__main__':unittest.main()
