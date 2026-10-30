"""A7d: complete map names, whole-source artwork and cross-map paddy safety."""
import hashlib,json,sys,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/WorldLayout'))
import m4_nature
PREVIEW=ROOT/'Assets/PocketCaravan/Editor/B2Preview/A7d/Maps'
class A7dSources(unittest.TestCase):
    def test_new_headwater_sources_match_decoded_pixels(self):
        d=json.loads((ROOT/'ArtReview/A7d-20261030/Headwaters/catalogue.json').read_text())
        self.assertEqual(len(d['assets']),3)
        for asset in d['assets']:
            with Image.open(ROOT/asset['path'])as source:im=source.convert('RGBA')
            self.assertEqual(list(im.size),asset['size'])
            self.assertEqual(hashlib.sha256(im.tobytes()).hexdigest(),asset['rgbaSha256'])
    def test_unapproved_layout_cannot_enter_production(self):
        with self.assertRaisesRegex(ValueError,'approval'):
            m4_nature.load_layout(ROOT/'Tools/WorldLayout/nature_a7d.json',production=True)
    def test_all_map_names_match_canonical_source(self):
        names=json.loads((ROOT/'Tools/WorldLayout/map_labels.json').read_text())
        for stem,count in [('GoryeoLayout',13),('GoryeoRegionalLayout',5)]:
            d=json.loads((ROOT/f'Assets/PocketCaravan/Resources/WorldMap/{stem}.json').read_text())
            self.assertEqual(len(d['maps']),count)
            for area in d['maps']:self.assertEqual(area['name'],names['maps'][area['mapId']])
        d=json.loads((ROOT/'Assets/PocketCaravan/Resources/WorldMap/GoryeoLayout.json').read_text())
        self.assertEqual({s['text']for s in d['settlements']},set(names['hamlets'].values()))

@unittest.skipUnless((PREVIEW/'Naru.json').exists(),'Generate A7d review maps first')
class A7dMaps(unittest.TestCase):
    def setUp(self):
        self.maps={p.stem:json.loads(p.read_text())for p in PREVIEW.glob('*.json')}
    def test_paddies_are_complete_walkable_and_do_not_cross_map_voids(self):
        total=0
        for d in self.maps.values():
            for c in d.get('natureCrops',[]):
                total+=1;self.assertEqual((c['w'],c['h']),(12,8))
                for y in range(c['y'],c['y']+c['h']):
                    for x in range(c['x'],c['x']+c['w']):
                        i=y*d['w']+x
                        self.assertGreater(d['ground'][i],0,(d['id'],x,y))
                        self.assertEqual((d['blocked'][i],d['water'][i],d['road'][i]),(0,0,0),(d['id'],x,y))
        config=json.loads((ROOT/'Tools/WorldLayout/nature_a7d.json').read_text())
        self.assertEqual(total,sum(p['role']=='rice'for p in config['placements']))
        self.assertGreaterEqual(total,8)
    def test_all_thirteen_maps_are_preview_only_and_roads_unchanged(self):
        self.assertEqual(len(self.maps),13)
        for mid,d in self.maps.items():
            self.assertTrue(d['previewOnly'])
            old=json.loads((ROOT/f'Assets/PocketCaravan/Maps/{mid}.json').read_text())
            self.assertEqual(d['road'],old['road'],mid)
    def test_four_springs_feed_their_existing_streams(self):
        water=set()
        for d in self.maps.values():
            for i,v in enumerate(d['water']):
                if v:water.add((d['terrainX']+i%d['w'],d['terrainY']+i//d['w']))
        config,assets=m4_nature.load_layout(ROOT/'Tools/WorldLayout/nature_a7d.json')
        for h in config['headwaters']:
            p=next(p for p in config['placements']if p['id']==h['placement']);mask=assets[p['asset']]['mask']['water']
            targets={(p['x']+x,p['y']+y)for y,row in enumerate(mask)for x,v in enumerate(row)if v=='1'and p['y']+y<=h['y']-2}
            # The approved large outlet shifts the old center by one tile; verify downstream of the art.
            connection=tuple(h.get('connection',[h['x'],h['y']]))
            seen=set();todo=[connection]
            while todo:
                x,y=todo.pop()
                if (x,y)in seen or(x,y)not in water or not(p['x']-2<=x<=p['x']+len(mask[0])+2 and p['y']-2<=y<=max(h['y'],connection[1])+2):continue
                seen.add((x,y));todo.extend([(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
            self.assertTrue(seen&targets,h)
if __name__=='__main__':unittest.main()
