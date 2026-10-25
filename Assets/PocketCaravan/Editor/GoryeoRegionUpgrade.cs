using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>고려만 갱신한다. 타 지역 목록/출신/가격/기존 퀘스트·GUID를 초기화하지 않는다.</summary>
    public static class GoryeoRegionUpgrade
    {
        const string Root = "Assets/PocketCaravan";
        const string Data = Root + "/Data/Goryeo";
        [MenuItem("Pocket Caravan/고려/수도와 네 마을·시설 데이터 갱신")]
        public static void ApplyData()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Play 종료 후 고려 데이터를 갱신하세요."); return; }
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GameDatabase.asset");
            var region = AssetDatabase.LoadAssetAtPath<RegionData>(Data + "/Region_Goryeo.asset");
            if (db == null || region == null) throw new System.InvalidOperationException("기본 고려 데이터가 없습니다.");
            TownData Existing(string file) => AssetDatabase.LoadAssetAtPath<TownData>(Data + "/" + file + ".asset");
            TownData Add(string file, string id, string label, Vector2Int coord)
            {
                var t = Existing(file);
                if (t == null) { t = ScriptableObject.CreateInstance<TownData>(); AssetDatabase.CreateAsset(t, Data + "/" + file + ".asset"); }
                t.id = id; t.displayName = label; t.region = region; t.worldCoord = coord;
                t.startLevel = 1; t.defaultOptimal = 4;
                if (!db.towns.Contains(t)) db.towns.Add(t);
                return t;
            }
            TownFacility Rest(string id) => new TownFacility { id = id, displayName = "길손 쉼터", kind = FacilityKind.Rest, minTier = 1, minCredit = 0 };
            TownFacility Info(string id, string text) => new TownFacility { id = id, displayName = "교역 길 안내소", kind = FacilityKind.RouteInformation, minTier = 2, minCredit = 1, description = text };
            void Configure(TownData t, int tier, params TownFacility[] facilities)
            { if (t == null) throw new System.InvalidOperationException("기존 마을 데이터 누락"); t.settlementTier = tier; t.facilities = new List<TownFacility>(facilities); EditorUtility.SetDirty(t); }
            var naru = Existing("Town_Naru"); var capital = Existing("Town_Gaegyeong"); var paper = Existing("Town_Dakmok");
            var songak = Add("Town_GoryeoSongak", "goryeo_songak", "송악 산록 마을", new Vector2Int(0,1));
            var jangdan = Add("Town_GoryeoJangdan", "goryeo_jangdan", "장단 들마을", new Vector2Int(4,0));
            ItemData Item(string id) => db.items.Find(i => i != null && i.id == id);
            void Stock(TownData t, params string[] ids)
            {
                t.stock.Clear();
                foreach (var id in ids) { var item = Item(id); if (item != null) t.stock.Add(new TownData.StockEntry { item = item, optimal = id == "grain" || id == "firewood" ? 30 : 12 }); }
            }
            Stock(songak, "ginseng", "paper", "salt", "grain", "firewood");
            Stock(jangdan, "grain", "firewood", "salt", "paper");
            Configure(naru, 1, Rest("goryeo_naru_rest"));
            Configure(paper, 2, Rest("goryeo_dakmok_rest"), Info("goryeo_dakmok_info", "남쪽 장단 들마을은 곡식과 땔감 거래처입니다. 개경에는 다양한 상품이 모입니다. 장부는 방문 당시 시세이므로 도착하면 다시 확인하세요."));
            Configure(songak, 2, Rest("goryeo_songak_rest"), Info("goryeo_songak_info", "서쪽 솔바람 고개는 대륙 내륙 교역로입니다. 남쪽 개경을 지나 예성 길과 나루로 이어집니다. 가벼운 등짐은 작은 다리 숲길, 수레는 주막 큰길을 비교하세요. 약재는 집산 상품이며 이 마을이 역사적 재배지라는 뜻은 아닙니다."));
            Configure(jangdan, 1, Rest("goryeo_jangdan_rest"));
            Configure(capital, 3, Rest("goryeo_capital_rest"), Info("goryeo_capital_info", "서남쪽 나루, 북쪽 산록 마을, 동쪽 종이 마을과 남쪽 들마을이 거래처입니다. 첫 한 개의 판매가가 전량 판매 수익은 아닙니다. 적재량과 돌아올 화물을 함께 비교하세요."), new TownFacility { id = "goryeo_capital_warehouse", displayName = "객주 보관소", kind = FacilityKind.Warehouse, minTier = 3, minCredit = 3, storageCapacity = 120 });
            EditorUtility.SetDirty(db); AssetDatabase.SaveAssets();
            GoryeoExperienceUpgrade.Apply();
            Debug.Log("고려 거점5곳/기본 시설 적용. 등급·신용1/3·보관120은 비교용 수치, 최종 밸런스 아님.");
        }
    }
}
