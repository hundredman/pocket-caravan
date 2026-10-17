using System;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 북유럽 데이터(지역·마을·물건)를 만들고, 테스트용으로 내 컴퓨터의 GameDatabase 목록 끝에 덧붙인다.
    /// 맵 씬은 공용 메뉴 "2. 맵 씬 다시 만들기"가 World/Regions/north.json을 읽어 만든다.
    /// 팀 규칙: GameDatabase.asset 변경은 커밋하지 않는다 (통합 담당자가 모아서 반영). Docs/TeamDevelopment.md
    /// </summary>
    public static class NorthBuilder
    {
        const string Root = "Assets/PocketCaravan";
        const string DataDir = Root + "/Data/North";
        const string DbPath = Root + "/Resources/GameDatabase.asset";

        [MenuItem("Pocket Caravan/북유럽/데이터 만들기 + 테스트용 DB 등록", priority = 30)]
        public static void BuildData()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
            if (db == null) { Debug.LogError("GameDatabase가 없습니다."); return; }

            var north = Upsert<RegionData>($"{DataDir}/Region_North.asset", r => { r.id = "north"; r.displayName = "북유럽"; r.note = "D구역. 한랭 지형·피오르·숲. 특산: 모피, 호박"; });

            TownData Town(string file, string id, string name, Vector2Int coord, int level) =>
                Upsert<TownData>($"{DataDir}/{file}.asset", t =>
                {
                    t.id = id; t.displayName = name; t.region = north; t.worldCoord = coord; t.startLevel = level;
                    t.stock.Clear(); t.vehiclesForSale.Clear(); t.defaultOptimal = 4f;
                });
            // 좌표는 north.json의 worldX, worldY와 같게 (경제 거리 계산에 쓰임)
            var birka = Town("Town_Birka", "north_birka", "비르카", new Vector2Int(30, 0), 1);

            ItemData Item(string id, string name, float weight, float price, TownData origin) =>
                Upsert<ItemData>($"{DataDir}/Item_{id}.asset", i =>
                {
                    i.id = id; i.displayName = name; i.weight = weight; i.basePrice = price; i.originTown = origin;
                    i.pricePerMap = 0.5f; i.questItem = false;
                    i.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Items/{id}.png");
                });
            // 산지 마을은 그 맵이 생기면 바꾼다 (모피: 사냥꾼 마을, 호박: 호박 해안)
            var mead = Item("north_mead", "꿀술", 2f, 12f, birka);
            var fur = Item("north_fur", "모피", 2f, 30f, null);
            var amber = Item("north_amber", "호박", 1f, 50f, null);
            var meat = Item("north_dried_meat", "말린 고기", 2f, 8f, null);

            birka.stock.Add(S(mead, 25)); birka.stock.Add(S(fur, 6)); birka.stock.Add(S(amber, 4)); birka.stock.Add(S(meat, 20));
            EditorUtility.SetDirty(birka);

            // 테스트용 등록 (북유럽 것만 지우고 다시 넣음, 고려 목록은 그대로)
            db.towns.RemoveAll(t => t == null || t.id.StartsWith("north_"));
            db.items.RemoveAll(i => i == null || i.id.StartsWith("north_"));
            db.towns.Add(birka);
            db.items.AddRange(new[] { mead, fur, amber, meat });
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[북유럽] 데이터를 만들고 GameDatabase에 덧붙였습니다. 이제 메뉴 2(맵 씬 다시 만들기)를 실행하세요. GameDatabase.asset 변경은 커밋하지 마세요.");
        }

        static TownData.StockEntry S(ItemData item, float optimal) => new TownData.StockEntry { item = item, optimal = optimal };

        static T Upsert<T>(string path, Action<T> apply) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            bool create = a == null;
            if (create) a = ScriptableObject.CreateInstance<T>();
            apply(a);
            if (create) AssetDatabase.CreateAsset(a, path);
            EditorUtility.SetDirty(a);
            return a;
        }
    }
}
