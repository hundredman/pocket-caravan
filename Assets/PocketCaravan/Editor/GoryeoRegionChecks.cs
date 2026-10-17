using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class GoryeoRegionChecks
    {
        [Serializable] public class Report { public List<string> passed = new List<string>(); }
        [MenuItem("Pocket Caravan/Tests/고려 거점·시설·저장 이주 검사")]
        public static void RunMenu() => Debug.Log(JsonUtility.ToJson(Run(), true));
        public static Report Run()
        {
            var report = new Report();
            void Check(string label, bool ok) { if (!ok) throw new Exception(label); report.passed.Add(label); }
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/PocketCaravan/Resources/GameDatabase.asset");
            var goryeo = db.towns.FindAll(t => t != null && t.region != null && t.region.id == "goryeo");
            Check("고려 수도 포함 독립5거점", goryeo.Count == 5 && new HashSet<string>(goryeo.ConvertAll(t => t.id)).Count == 5);
            Check("모든 고려 거점 기본 시설 보유", goryeo.TrueForAll(t => t.facilities.Count > 0));
            var town = ScriptableObject.CreateInstance<TownData>(); town.id = "test_town"; town.settlementTier = 3;
            var other = ScriptableObject.CreateInstance<TownData>(); other.id = "test_other"; other.settlementTier = 3;
            var item = ScriptableObject.CreateInstance<ItemData>(); item.id = "test_goods"; item.weight = 2;
            var questItem = ScriptableObject.CreateInstance<ItemData>(); questItem.id = "test_parcel"; questItem.questItem = true;
            var vehicle = ScriptableObject.CreateInstance<VehicleData>(); vehicle.capacity = 10;
            var facility = new TownFacility { id = "test_warehouse", kind = FacilityKind.Warehouse, minTier = 3, minCredit = 3, storageCapacity = 6 };
            town.facilities.Add(facility);
            var p = new PlayerState { vehicle = vehicle, credit = 2 };
            var b = new PlayerState { vehicle = vehicle, credit = 3 };
            ItemData Find(string id) => id == item.id ? item : null;
            try
            {
                p.cargo.Add(item, 3, 13.5f);
                Check("문턱 직전 시설 거절·재산 불변", p.warehouse.Transfer(town,facility,p,item,true,Find) != null && p.cargo.Count(item)==3);
                Check("다른 플레이어 신용은 공유 안 함", FacilityAccess.Denial(town,facility,b)==null && FacilityAccess.Denial(town,facility,p)!=null);
                p.credit = 3;
                Check("정확한 문턱에서 해금", FacilityAccess.Denial(town,facility,p)==null);
                Check("다른 마을의 시설 참조 거절", FacilityAccess.Denial(other,facility,p)!=null);
                town.settlementTier=2;
                Check("개인 신용 충족해도 마을 등급 부족 거절", FacilityAccess.Denial(town,facility,p)!=null);
                town.settlementTier=3;
                Check("일반 상품 입고", p.warehouse.Transfer(town,facility,p,item,true,Find)==null && p.cargo.Count(item)==2);
                Check("원가를 유지한 입고/출고", p.warehouse.Transfer(town,facility,p,item,false,Find)==null && p.cargo.Count(item)==3 && Mathf.Approximately(p.cargo.TotalCost(item),40.5f));
                Check("빈 창고 재출고 거절", p.warehouse.Transfer(town,facility,p,item,false,Find)!=null && p.cargo.Count(item)==3);
                Check("의뢰/퀘스트 물품 입고 거절", p.warehouse.Transfer(town,facility,p,questItem,true,Find)!=null);
                p.warehouse.Transfer(town,facility,p,item,true,Find);
                p.warehouse.Transfer(town,facility,p,item,true,Find);
                p.warehouse.Transfer(town,facility,p,item,true,Find);
                p.cargo.Add(item,1,7);
                Check("창고 한도 초과 입고 거절", p.warehouse.Transfer(town,facility,p,item,true,Find)!=null && p.cargo.Count(item)==1);
                p.cargo.Add(item,4,7);
                Check("적재 초과 출고 거절", p.warehouse.Transfer(town,facility,p,item,false,Find)!=null && p.cargo.Count(item)==5);
                var saved = new SaveData { storedCargo = p.warehouse.Export() };
                var json = JsonUtility.ToJson(saved);
                var loaded = JsonUtility.FromJson<SaveData>(json);
                var restored = new WarehouseStore(); restored.Import(loaded.storedCargo);
                var rows = restored.Export();
                Check("저장 왕복 수량/원가/마을 보존", rows.Count==1 && rows[0].count==3 && rows[0].townId==town.id && Mathf.Approximately(rows[0].totalCost,40.5f));
                Check("두 캐릭터의 창고 분리", b.warehouse.Export().Count==0);
                restored.Import(new List<StoredCargo> { new StoredCargo {townId="missing_town",itemId="missing_item",count=2,totalCost=8} });
                Check("알 수 없는 창고 ID 보존", restored.Export()[0].count==2);
                bool rejected=false;
                try { restored.Import(new List<StoredCargo> { new StoredCargo { townId="a",itemId="b",count=-1 } }); } catch(InvalidOperationException) { rejected=true; }
                Check("손상 창고 거절 후 기존 화물 보존", rejected && restored.Export()[0].itemId=="missing_item");
                var legacy = JsonUtility.FromJson<SaveData>("{\"version\":1,\"money\":9}");
                SaveSystem.Validate(legacy);
                Check("버전1 저장의 보관함 누락 호환", legacy.storedCargo==null || legacy.storedCargo.Count==0);
                var drops = new List<DropRecord> { new DropRecord {itemId=item.id,unitCost=11}, new DropRecord {itemId="missing"} };
                var migrated = new PlayerState {vehicle=vehicle}; migrated.cargo.Add(item,5,3);
                Check("기존 낙하 화물 적재초과도 복구", LegacyCargoMigration.Restore(migrated,drops,Find)==1 && migrated.cargo.Count(item)==6);
                Check("낙하 화물 원가 보존", Mathf.Approximately(migrated.cargo.TotalCost(item),26));
                Check("이주 재호출 중복 없음·미지 ID 보존", LegacyCargoMigration.Restore(migrated,drops,Find)==0 && drops.Count==1 && migrated.cargo.Count(item)==6);
                var contract = new Contract {id=91,dropped=true,condition=.6f}; migrated.contracts.Add(contract); drops.Add(new DropRecord {contractId=91});
                Check("의뢰 화물 이주·상태 보존", LegacyCargoMigration.Restore(migrated,drops,Find)==1 && !contract.dropped && Mathf.Approximately(contract.condition,.6f));
                string path = "output/goryeo-development-2026-10-17/save-fixture.json";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                Check("새 저장 파일 쓰기", SaveSystem.WriteAt(path,new SaveData { money=7, storedCargo=p.warehouse.Export() }));
                Check("원자 교체와 이전 저장 백업", SaveSystem.WriteAt(path,new SaveData { money=8 }) && JsonUtility.FromJson<SaveData>(File.ReadAllText(path+".bak")).money==7);
                bool future=false;try { SaveSystem.Validate(new SaveData { version=999 }); } catch(InvalidDataException) { future=true; }
                Check("미지원 미래 저장 버전 거절",future);
            }
            finally { UnityEngine.Object.DestroyImmediate(town); UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(item); UnityEngine.Object.DestroyImmediate(questItem); UnityEngine.Object.DestroyImmediate(vehicle); }
            Directory.CreateDirectory("output/goryeo-development-2026-10-17");
            File.WriteAllText("output/goryeo-development-2026-10-17/facility-checks.json",JsonUtility.ToJson(report,true));
            return report;
        }
    }
}
