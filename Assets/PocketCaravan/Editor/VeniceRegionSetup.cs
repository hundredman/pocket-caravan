using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 베네치아(제안 초안): 수도 1 + 마을 4가 한 맵(venice_lagoon)에서 걸어서 이어진다. 씬 전환·암전이 없다.
    /// 지명·상품·수치는 팀 확정 전 임시값이다. 무라노·산테라스모·부라노는 제안 지명이다.
    /// 다른 지역 에셋·DB 항목은 지우거나 바꾸지 않는다. 공용 GameDatabase에는 추가만 한다(커밋 여부는 통합 담당과 정한다).
    /// </summary>
    public static class VeniceRegionSetup
    {
        const string Root = "Assets/PocketCaravan";
        const string Data = Root + "/Data/Venice";
        const string MapId = "venice_lagoon";

        [MenuItem("Pocket Caravan/베네치아/데이터·맵 만들기")]
        public static void Build()
        {
            ApplyData();
            MapJsonBuilder.BuildSelected(MapId);
            AttachDistricts();
            Debug.Log("[베네치아] 데이터와 맵을 만들었습니다. Scenes/Venice/venice_lagoon에서 Play하세요.");
        }

        public static void ApplyData()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            Directory.CreateDirectory(Data); AssetDatabase.Refresh();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GameDatabase.asset");
            if (db == null) throw new InvalidOperationException("GameDatabase가 없습니다.");
            T Get<T>(string file) where T : ScriptableObject
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(Data + "/" + file + ".asset");
                if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, Data + "/" + file + ".asset"); }
                return asset;
            }
            var region = Get<RegionData>("Region_Venice");
            region.id = "venice"; region.displayName = "베네치아";
            region.note = "제안 초안. 리알토·키오자·무라노·산테라스모·부라노는 실제 지명을 빌린 게임 각색이며 실제 배치와 다르다. 좌표 x10~19는 팀 합의안 구역의 임시값, y는 미정.";
            EditorUtility.SetDirty(region);

            TownData Town(string file, string id, string label, Vector2Int coord, int tier, string note)
            {
                var t = Get<TownData>(file); t.id = id; t.displayName = label; t.region = region; t.journeyNote = note;
                t.worldCoord = coord; t.startLevel = 1; t.settlementTier = tier; t.defaultOptimal = 3;
                t.facilities = new List<TownFacility> {
                    new TownFacility { id = id + "_rest", displayName = "길손 쉼터", kind = FacilityKind.Rest, minTier = 1 }
                };
                if (!db.towns.Contains(t)) db.towns.Add(t);
                return t;
            }
            // 가격 거리용 좌표. 설계도의 상대 위치(가운데 수도, 네 모서리 마을)를 따른다.
            var rialto = Town("Town_Rialto", "venice_rialto", "리알토 장터", new Vector2Int(12, 1), 3,
                "수도. 상인회관 앞 광장 장터. 네 마을로 가는 다리가 섬의 네 모서리에 있다. 손수레를 판다.");
            var chioggia = Town("Town_Chioggia", "venice_chioggia", "키오자", new Vector2Int(13, 2), 2,
                "교역항. 석호 소금 배가 닿는 창고 앞에서 소금을 산다. 절인 생선은 부라노로.");
            var murano = Town("Town_Murano", "venice_murano", "무라노", new Vector2Int(13, 0), 1,
                "유리공방 마을. 유리그릇은 가볍지만 값이 나간다.");
            var santerasmo = Town("Town_SantErasmo", "venice_santerasmo", "산테라스모", new Vector2Int(11, 0), 1,
                "과수원 마을. 과일을 사서 수도나 어촌에 판다.");
            var burano = Town("Town_Burano", "venice_burano", "부라노", new Vector2Int(11, 2), 1,
                "어촌. 생선을 사고, 키오자 소금을 팔기 좋다.");

            ItemData Good(string file, string id, string label, float weight, int price, TownData origin, string icon)
            {
                var i = Get<ItemData>(file); i.id = id; i.displayName = label; i.weight = weight; i.basePrice = price;
                i.originTown = origin; i.pricePerMap = .5f; i.questItem = false;
                i.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/{icon}.png");
                if (!db.items.Contains(i)) db.items.Add(i);
                EditorUtility.SetDirty(i); return i;
            }
            // 기존 ID(venice_salt, venice_glass)는 유지. 유리 공급 거점만 무라노로 옮긴다.
            var salt = Good("Item_VeniceSalt", "venice_salt", "석호 소금", 2, 10, chioggia, "venice_salt");
            var glass = Good("Item_VeniceGlass", "venice_glass", "유리그릇", 1, 30, murano, "venice_glass");
            var fruit = Good("Item_VeniceFruit", "venice_fruit", "과일 상자", 2, 9, santerasmo, "venice_fruit");
            var fish = Good("Item_VeniceFish", "venice_fish", "절인 생선", 2, 12, burano, "venice_fish");

            void Stock(TownData town, params (ItemData item, float optimal)[] entries)
            {
                town.stock.Clear();
                foreach (var e in entries) if (e.item != null) town.stock.Add(new TownData.StockEntry { item = e.item, optimal = e.optimal });
                EditorUtility.SetDirty(town);
            }
            ItemData Shared(string id) => db.items.Find(i => i != null && i.id == id);
            Stock(rialto, (salt, 14), (glass, 14), (fruit, 14), (fish, 14), (Shared("grain"), 24));
            Stock(chioggia, (salt, 64), (fish, 10), (glass, 6), (Shared("grain"), 30));
            Stock(murano, (glass, 56), (salt, 8), (fruit, 6));
            Stock(santerasmo, (fruit, 56), (glass, 4), (fish, 6));
            Stock(burano, (fish, 56), (salt, 6), (fruit, 6));

            var handcart = db.vehicles.Find(v => v != null && v.id == "handcart");
            foreach (var t in new[] { rialto, chioggia, murano, santerasmo, burano }) t.vehiclesForSale.Clear();
            if (handcart != null) rialto.vehiclesForSale.Add(handcart);
            EditorUtility.SetDirty(db); AssetDatabase.SaveAssets();
        }

        [Serializable] class DistrictJson { public string name; public int x0, y0, x1, y1; }
        [Serializable] class MapJson { public string id; public int w, h; public DistrictJson[] districts; public string defaultDistrict; }

        /// <summary>맵 JSON의 구역 이름을 생성된 씬에 붙인다 (JSON은 위쪽이 0, Unity 셀은 아래쪽이 0).</summary>
        public static void AttachDistricts()
        {
            var entry = WorldRegistry.Load().Map(MapId);
            var json = JsonUtility.FromJson<MapJson>(File.ReadAllText(entry.jsonPath));
            var scene = EditorSceneManager.OpenScene(entry.scenePath, OpenSceneMode.Single);
            var go = new GameObject("베네치아 구역 이름");
            var districts = go.AddComponent<VeniceDistricts>();
            districts.defaultName = string.IsNullOrEmpty(json.defaultDistrict) ? "석호 길" : json.defaultDistrict;
            foreach (var d in json.districts ?? Array.Empty<DistrictJson>())
            {
                int yMin = json.h - 1 - d.y1, yMax = json.h - 1 - d.y0;
                districts.districts.Add(new VeniceDistricts.District { name = d.name, cells = new RectInt(d.x0, yMin, d.x1 - d.x0, yMax - yMin) });
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
