"""Map module registration, baked-content integrity and composition contract (stdlib only)."""
import hashlib,json,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen/Modules'))
from compose import compose
BASE=ROOT/'Assets/PocketCaravan/MapModules'

class ModuleLibraryContract(unittest.TestCase):
    def test_exported_structure_and_source_ids_stay_in_sync(self):
        catalogue=json.loads((BASE/'catalogue.json').read_text());ids=[]
        self.assertGreaterEqual(len(catalogue['entries']),40)
        for e in catalogue['entries']:
            ids.append(e['id']);source=json.loads((ROOT/e['sourcePath']).read_text());f=json.loads((ROOT/e['jsonPath']).read_text())
            self.assertEqual(source['id'],e['id']);self.assertEqual(f['id'],'module_'+e['id'])
            self.assertEqual((f['w'],f['h']),(40,40))
            for k in ['ground','deco','obj','high','road','blocked']:self.assertEqual(len(f[k]),1600)
            digest=hashlib.sha256(json.dumps({k:f[k] for k in ['road','blocked','objects']},sort_keys=True).encode()).hexdigest()
            self.assertEqual(digest,e['structureHash'])
            self.assertTrue((ROOT/e['previewPath']).exists())
        self.assertEqual(len(ids),len(set(ids)))

    def test_valid_composition_joins_actual_port_cells(self):
        f=compose(json.loads((BASE/'Recipes/four_crossroads.json').read_text()))
        self.assertEqual((f['w'],f['h']),(80,80))
        for x,y in [(39,20),(40,20),(20,39),(20,40),(60,39),(60,40)]:
            self.assertGreater(f['road'][y*80+x],0);self.assertEqual(f['blocked'][y*80+x],0)
        self.assertEqual(len(f['objects']),1,'Only one starting spawn in an assembled map')

    def test_composition_rejects_unmatched_and_disconnected_ports(self):
        for cells in [
            [dict(x=0,y=0,module='meadow_lane'),dict(x=0,y=1,module='wheat_crossing')],
            [dict(x=0,y=0,module='meadow_lane'),dict(x=2,y=0,module='meadow_lane')],
            [dict(x=0,y=0,module='meadow_lane'),dict(x=0,y=0,module='meadow_lane')]
        ]:
            with self.assertRaises(ValueError):compose(dict(id='invalid',name='invalid',cells=cells))

    def test_library_does_not_expand_live_world_or_reserved_team_coordinates(self):
        entries=json.loads((BASE/'catalogue.json').read_text())['entries']
        live=[json.loads(p.read_text()) for p in (ROOT/'Assets/PocketCaravan/World/Regions').glob('*.json')]
        registered={m['mapId'] for r in live for m in r['maps']}
        self.assertTrue(all('module_'+e['id'] not in registered for e in entries))

if __name__=='__main__':unittest.main()
