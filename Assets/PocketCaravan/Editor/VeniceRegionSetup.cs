using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 베네치아 최소 교역 동선(제안 초안) 데이터와 씬. 키오자 → 석호 둑길 → 리알토.
    /// 지명·상품·수치는 팀 확정 전 임시값이다. 다른 지역 에셋·DB 항목은 지우거나 바꾸지 않는다.
    /// 공용 GameDatabase에는 추가만 한다. 이 변경의 커밋 여부는 통합 담당과 정한다.
    /// </summary>
    public static class VeniceRegionSetup
    {
        const string Root = "Assets/PocketCaravan";
        const string Data = Root + "/Data/Venice";
        static readonly string[] Maps = { "venice_chioggia", "venice_lagoon_road", "venice_rialto" };

        [MenuItem("Pocket Caravan/베네치아/데이터·맵 만들기")]
        public static void Build()
        {
            ApplyData();
            MapJsonBuilder.BuildSelected(Maps);
            Debug.Log("[베네치아] 데이터와 맵 3개를 만들었습니다. Scenes/Venice/venice_chioggia에서 Play하세요.");
        }

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
            var region = Get<RegionData>("Region_Venice");
            region.id = "venice"; region.displayName = "베네치아";
            region.note = "제안 초안. 키오자 염전·리알토 장터는 실제 지명을 빌린 게임 각색. 좌표 x10~12는 팀 합의안 구역의 임시값.";
            EditorUtility.SetDirty(region);

            TownData Town(string file, string id, string label, int x, int tier, string note)
            {
                var t = Get<TownData>(file); t.id = id; t.displayName = label; t.region = region; t.journeyNote = note;
                t.worldCoord = new Vector2Int(x, 0); t.startLevel = 1; t.settlementTier = tier; t.defaultOptimal = 3;
                t.facilities = new List<TownFacility> {
                    new TownFacility { id = id + "_rest", displayName = "길손 쉼터", kind = FacilityKind.Rest, minTier = 1 }
                };
                if (!db.towns.Contains(t)) db.towns.Add(t);
                return t;
            }
            var chioggia = Town("Town_Chioggia", "venice_chioggia", "키오자", 10, 1, "염전 마을. 소금을 리알토로 실어 가고, 돌아올 때 유리그릇을 살펴보자.");
            var rialto = Town("Town_Rialto", "venice_rialto", "리알토 장터", 12, 2, "운하 옆 큰 장터. 유리 공방이 있고 손수레를 판다.");

            ItemData Shared(string id) => db.items.Find(i => i != null && i.id == id);
            ItemData Good(string file, string id, string label, float weight, int price, TownData origin, string iconFrom)
            {
                var i = Get<ItemData>(file); i.id = id; i.displayName = label; i.weight = weight; i.basePrice = price;
                i.originTown = origin; i.pricePerMap = .5f; i.questItem = false;
                var source = Shared(iconFrom); i.icon = source != null ? source.icon : null;   // 임시 아이콘
                if (!db.items.Contains(i)) db.items.Add(i);
                EditorUtility.SetDirty(i); return i;
            }
            var salt = Good("Item_VeniceSalt", "venice_salt", "석호 소금", 2, 10, chioggia, "salt");
            var glass = Good("Item_VeniceGlass", "venice_glass", "유리그릇", 1, 30, rialto, "najeon");

            void Stock(TownData town, params (ItemData item, float optimal)[] entries)
            {
                town.stock.Clear();
                foreach (var e in entries) if (e.item != null) town.stock.Add(new TownData.StockEntry { item = e.item, optimal = e.optimal });
                EditorUtility.SetDirty(town);
            }
            Stock(chioggia, (salt, 64), (glass, 6), (Shared("grain"), 20));
            Stock(rialto, (salt, 12), (glass, 48), (Shared("grain"), 24));

            var handcart = db.vehicles.Find(v => v != null && v.id == "handcart");
            rialto.vehiclesForSale.Clear();
            if (handcart != null) rialto.vehiclesForSale.Add(handcart);
            chioggia.vehiclesForSale.Clear();
            EditorUtility.SetDirty(chioggia); EditorUtility.SetDirty(rialto);
            EditorUtility.SetDirty(db); AssetDatabase.SaveAssets();
        }
    }
}
