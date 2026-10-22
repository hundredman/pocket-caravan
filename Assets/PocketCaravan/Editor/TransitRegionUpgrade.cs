using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>중간권 비교용 데이터. 기존 지역의 목록·가격·GUID는 초기화하지 않는다.</summary>
    public static class TransitRegionUpgrade
    {
        const string Root = "Assets/PocketCaravan";
        const string Data = Root + "/Data/Transit";
        [MenuItem("Pocket Caravan/중간권/여정 시험 데이터 갱신")]
        public static void ApplyData()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Play 종료 후 실행하세요.");
            Directory.CreateDirectory(Data); AssetDatabase.Refresh();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GameDatabase.asset");
            if (db == null) throw new System.InvalidOperationException("GameDatabase가 없습니다.");
            T Get<T>(string file) where T : ScriptableObject
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(Data + "/" + file + ".asset");
                if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Data + "/" + file + ".asset"); }
                return asset;
            }
            var region = Get<RegionData>("Region_Transit");
            region.id = "transit"; region.displayName = "중간 교역권";
            region.note = "문명 사이의 긴 여정을 검증하는 가상 중간권. 실제 고려 국경·거리의 고증 아님. 임시 좌표 x5~9은 기존 고려 작업 대역 안에서만 사용.";
            EditorUtility.SetDirty(region);
            TownData Town(string file, string id, string label, int x)
            {
                var t = Get<TownData>(file); t.id = id; t.displayName = label; t.region = region;
                t.worldCoord = new Vector2Int(x,0); t.startLevel = 1; t.settlementTier = 2; t.defaultOptimal = 3;
                t.facilities = new List<TownFacility> {
                    new TownFacility { id = id + "_rest", displayName = "길손 쉼터", kind = FacilityKind.Rest, minTier = 1 },
                    new TownFacility { id = id + "_info", displayName = "길 안내", kind = FacilityKind.RouteInformation, minTier = 2, minCredit = 0,
                        description = "고개와 물가의 샛길은 짧고, 큰길은 수레에 유리합니다. 거래 후 재고와 귀환 화물을 비교하세요. 버들나루 동쪽 중앙 진입로를 지나면 중앙 십자로입니다. 현재 중앙에서 개방된 연결로는 고려 방면뿐입니다." }
                };
                if (!db.towns.Contains(t)) db.towns.Add(t);
                return t;
            }
            var wood = Town("Town_Woodtown","transit_woodtown","솔재 길목 마을",6);
            var ferry = Town("Town_Ferry","transit_ferry","버들나루 마을",8);
            ItemData Item(string id) => db.items.Find(i => i != null && i.id == id);
            ItemData Good(string file, string id, string label, int price, TownData origin, string iconId)
            {
                var i = Get<ItemData>(file); i.id=id; i.displayName=label; i.weight=2; i.basePrice=price;
                i.originTown=origin; i.pricePerMap=.35f; i.questItem=false;
                var source=Item(iconId); i.icon=source != null ? source.icon : null;
                if (!db.items.Contains(i)) db.items.Add(i); EditorUtility.SetDirty(i); return i;
            }
            var resin=Good("Item_Resin","transit_resin","송진 꾸러미",8,wood,"firewood");
            var fish=Good("Item_DriedFish","transit_driedfish","말린 생선",9,ferry,"salt");
            void Stock(TownData town, params (ItemData item, int quantity)[] entries)
            {
                town.stock.Clear(); foreach(var e in entries) if(e.item != null) town.stock.Add(new TownData.StockEntry { item=e.item, optimal=e.quantity });
                EditorUtility.SetDirty(town);
            }
            Stock(wood,(resin,64),(fish,48),(Item("firewood"),36),(Item("grain"),12),(Item("paper"),14),(Item("salt"),14));
            Stock(ferry,(resin,48),(fish,64),(Item("grain"),30),(Item("salt"),24),(Item("paper"),12),(Item("firewood"),14));
            var handcart=db.vehicles.Find(v => v != null && v.id == "handcart");
            if(handcart != null && !wood.vehiclesForSale.Contains(handcart)) wood.vehiclesForSale.Add(handcart);
            EditorUtility.SetDirty(wood); EditorUtility.SetDirty(db); AssetDatabase.SaveAssets();
        }
    }
}
