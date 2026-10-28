"""C1: real production identities, complete approved frames and animal preservation."""
import os,sys as _sys;_sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)));from m4_superseded import superseded
import hashlib,json,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/MapGen'))
from npc_bindings import roles
from PIL import Image
from occlusion_baseline import restore_before_c3

def read(path):return json.loads((ROOT/path).read_text())
def digest(d):return hashlib.sha256(json.dumps(d,sort_keys=True,ensure_ascii=False).encode()).hexdigest()
class HumanNpcBindings(unittest.TestCase):
    def test_old_region_ids_names_dialogue_and_villager_order_preserved(self):
        fixture=read('Tools/Integration/Fixtures/c1_legacy_identity.json')
        for rid,old in fixture['regions'].items():
            now=read(f'Assets/PocketCaravan/World/Regions/{rid}.json')
            for sec in ['npcs','villagers']:
                existing=[{k:v for k,v in n.items() if k!='sprite'} for n in now[sec] if n.get('id')!='naru_well_woman']
                self.assertEqual(existing,old[sec])
    @superseded
    def test_old_map_objects_and_terrain_unchanged_except_approved_pond(self):
        for mid,old in read('Tools/Integration/Fixtures/c1_legacy_identity.json')['maps'].items():
            now=restore_before_c3(read(old['jsonPath']));terrain={k:v for k,v in now.items() if k!='objects'}
            if mid=='Gaegyeong':
                # Preserve the C1 identity baseline while permitting only the approved A4 cells.
                prior=read('Docs/Validation/A4-Pond-Production-2026-10-25/Gaegyeong-before.json')
                sites={(p['x'],p['y']):p['piece'] for p in read('Tools/MapGen/pond_layout.json')['placements']}
                old_palette={name:index for index,name in enumerate(prior['palette'])}
                for layer in ('ground','deco','obj','high'):
                    restored=[]
                    for index,pi in enumerate(now[layer]):
                        name=now['palette'][pi];cell=(index%now['w'],index//now['w'])
                        if layer=='ground' and cell in sites:
                            self.assertEqual(name,'pond_'+sites[cell])
                            name=prior['palette'][prior[layer][index]]
                        self.assertIn(name,old_palette)
                        restored.append(old_palette[name])
                    terrain[layer]=restored
                terrain['palette']=prior['palette']
            self.assertEqual(digest(terrain),old['terrainSHA256'],mid)
            objects=[{k:v for k,v in o.items() if k!='character'} for o in now['objects'] if o.get('npc')!='naru_well_woman']
            self.assertEqual(objects,old['objects'],mid)
    def test_role_paths_unique_and_all_frames_equal_approved_sources(self):
        rows=roles();self.assertEqual(len({r['path'] for r in rows.values()}),len(rows))
        for row in rows.values():
            name=row['art'];new=name in ['porter','merchant','merchant_silk','scholar','woman_ricecake']
            src=ROOT/('ArtReview/Approved/C1-Binding-Walk' if new else 'ArtReview/Approved/C1-Walk')
            for direction in 'dulr':
                for frame in range(4):
                    dst=ROOT/f'Assets/PocketCaravan/Art/Characters/{row["path"]}_{direction}{frame}.png'
                    self.assertEqual(dst.read_bytes(),(src/'frames'/f'{name}_{direction}{frame}.png').read_bytes())
    def test_animal_archive_hashes_and_no_npc_animal_path_in_registered_data(self):
        archive=ROOT/'Assets/PocketCaravan/Art/Characters/ArchivedAnimals'
        for a in json.loads((archive/'archive-index.json').read_text())['assets']:
            self.assertEqual(hashlib.sha256((archive/a['file']).read_bytes()).hexdigest(),a['sha256'])
        for r in ['goryeo','transit']:
            doc=read(f'Assets/PocketCaravan/World/Regions/{r}.json')
            for n in doc['npcs']+doc['villagers']:self.assertTrue(n['sprite'].startswith('NPC/'))
            for m in doc['maps']:
                for o in read(m['jsonPath'])['objects']:
                    if o['character']:self.assertTrue(o['character'].startswith('NPC/'),(m['mapId'],o))
        old=read('Tools/Integration/Fixtures/c1_legacy_identity.json')
        for name,h in old['originalAnimalSHA256'].items():self.assertEqual(hashlib.sha256((archive/name).read_bytes()).hexdigest(),h)
        for name,h in old['playerSHA256'].items():self.assertEqual(hashlib.sha256((ROOT/'Assets/PocketCaravan/Art/Characters'/name).read_bytes()).hexdigest(),h)

    def test_new_well_woman_exists_once_and_is_on_reachable_ground(self):
        doc=read('Assets/PocketCaravan/Maps/Naru.json');rows=[o for o in doc['objects'] if o.get('npc')=='naru_well_woman'];self.assertEqual(len(rows),1)
        o=rows[0];i=o['y']*doc['w']+o['x'];self.assertEqual(doc['reachable'][i],1);self.assertEqual(doc['blocked'][i],0)

if __name__=='__main__':unittest.main()
