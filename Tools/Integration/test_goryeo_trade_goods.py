"""Task6: append approved goods without resetting any pre-existing stock or registry IDs."""
from pathlib import Path
import re,json,hashlib,unittest
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
class ApprovedGoods(unittest.TestCase):
 def test_approved_pixels_and_registered_goods_specs(self):
  a=json.loads((ROOT/'ArtReview/ItemIcons-v3-20261104/approval.json').read_text());self.assertEqual(a['status'],'approved')
  rows={r['id']:r for r in a['assets']}
  override=json.loads((ROOT/'ArtReview/WoolWidth-v4-20261104/approval.json').read_text());self.assertEqual(override['status'],'approved');self.assertEqual([r['id']for r in override['assets']],['transit_wool'])
  rows.update({r['id']:r for r in override['assets']})
  for row in rows.values():
   with Image.open(ROOT/row['source'])as src,Image.open(ROOT/'Assets/PocketCaravan/Art/Goryeo/Items'/(row['id']+'.png'))as dest:
    src=src.convert('RGBA');dest=dest.convert('RGBA');self.assertEqual(dest.size,(16,16));self.assertEqual(src.tobytes(),dest.tobytes());self.assertEqual(hashlib.sha256(dest.tobytes()).hexdigest(),row['rgbaSha256'])
  for file,id,price,weight in [('Item_Spice','transit_spice',22,1),('Item_Wool','transit_wool',14,2)]:
   text=(ROOT/'Assets/PocketCaravan/Data/Transit'/(file+'.asset')).read_text();self.assertIn('  id: '+id+'\n',text);self.assertIn('  basePrice: '+str(price)+'\n',text);self.assertIn('  weight: '+str(weight)+'\n',text)
 def test_preexisting_items_stocks_and_grain_values_preserved(self):
  before=json.loads((ROOT/'Docs/Validation/TradeGoods-Task6-2026-11-04/before-inputs.json').read_text())
  db=(ROOT/'Assets/PocketCaravan/Resources/GameDatabase.asset').read_text().split('  items:\n',1)[1].split('  towns:\n',1)[0];items=re.findall(r'guid: ([0-9a-f]{32})',db)
  self.assertEqual(items[:-2],before['preexistingItemGuids']);self.assertEqual(len(items),len(set(items)))
  for path,old in before['stocks'].items():
   rows=re.findall(r'  - item: .*\n    optimal: .*\n    localPriceMultiplier: .*\n',(ROOT/path).read_text());self.assertEqual(rows[:len(old)],old)
  grain=re.sub(r'^  icon: .*\n','',(ROOT/'Assets/PocketCaravan/Data/Goryeo/Item_grain.asset').read_text(),flags=re.M);self.assertEqual(grain,before['grainBeforeWithoutIcon'])
if __name__=='__main__':unittest.main()
