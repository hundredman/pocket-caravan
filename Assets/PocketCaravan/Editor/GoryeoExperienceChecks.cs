using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class GoryeoExperienceChecks
    {
        [Serializable] public class Report { public List<string> passed = new List<string>(); public List<string> trades = new List<string>(); }
        public static Report RunData()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("데이터 검사는 Play 종료 후 실행하세요.");
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/PocketCaravan/Resources/GameDatabase.asset");
            var report = new Report();
            void Check(string name, bool ok) { if (!ok) throw new Exception(name); report.passed.Add(name); }
            TownData Town(string id) => db.towns.Find(t => t != null && t.id == id);
            ItemData Item(string id) => db.items.Find(i => i != null && i.id == id);
            void Trade(string source, string destination, string itemId, int count)
            {
                var item = Item(itemId); var from = new MarketState(Town(source), db); var to = new MarketState(Town(destination), db);
                int cost = 0, revenue = 0;
                for (int i = 0; i < count; i++) { int p = from.BuyPrice(item); cost += p; from.RecordBuy(item, p); }
                for (int i = 0; i < count; i++) { int p = to.SellPrice(item); revenue += p; to.RecordSell(item, p); }
                Check(source + " -> " + destination + "/" + itemId + " batch profitable", revenue > cost);
                for (int hour = 0; hour < 100; hour++) { from.RecoverOneHour(); to.RecoverOneHour(); }
                Check(source + "/" + itemId + " supply persists after stock recovery", from.BuyPrice(item) < to.SellPrice(item));
                report.trades.Add(source + " -> " + destination + " / " + itemId + " x" + count + ": cost " + cost + ", revenue " + revenue + ", profit " + (revenue - cost));
            }
            Trade("goryeo_songak", "gaegyeong", "ginseng", 2);
            Trade("goryeo_jangdan", "gaegyeong", "grain", 3);
            Trade("goryeo_jangdan", "dakmok", "firewood", 4);
            Trade("dakmok", "goryeo_jangdan", "paper", 2);
            foreach (var town in db.towns)
                foreach (var stock in town.stock)
                {
                    if (stock.item == null || stock.item.questItem) continue;
                    var m = new MarketState(town, db); int buy = m.BuyPrice(stock.item); m.RecordBuy(stock.item, buy);
                    Check(town.id + "/" + stock.item.id + " same-market resale has no profit", m.SellPrice(stock.item) <= buy);
                }
            var legacy = UnityEngine.Object.Instantiate(Town("goryeo_jangdan"));
            try
            {
                var entry = legacy.stock.Find(e => e.item == Item("grain"));
                entry.localPriceMultiplier = 0;
                Check("zero serialized factor preserves legacy base price", legacy.BasePriceFor(entry.item) == entry.item.BasePriceAt(legacy));
                entry.localPriceMultiplier = float.NaN;
                Check("invalid serialized factor preserves legacy base price", legacy.BasePriceFor(entry.item) == entry.item.BasePriceAt(legacy));
            }
            finally { UnityEngine.Object.DestroyImmediate(legacy); }
            var q2 = db.quests.Find(q => q.id == "tutorial_2");
            var cart = db.quests.Find(q => q.id == "tutorial_5");
            Check("paper purchase explicitly targets Dakmok", q2.objectives[0].targetId == "dakmok");
            Check("cart available after Q1 Dakmok step (QuestMain1)", cart.requires.Count == 1 && cart.requires[0] != null && cart.requires[0].id == "q1_dakmok");
            Check("old objective indices preserved", q2.objectives.Count == 2 && cart.objectives.Count == 2);
            foreach (var id in new[] { "goryeo_songak_trade", "goryeo_jangdan_trade", "goryeo_cart_supply" })
                Check(id + " uniquely registered", db.quests.FindAll(q => q != null && q.id == id).Count == 1);
            var supply = db.quests.Find(q => q.id == "goryeo_cart_supply");
            Check("post-cart shipment does not force legacy tutorial completion", supply.requires.Count == 1 && supply.requires[0].id == "goryeo_jangdan_trade");
            Check("cart supply exceeds backpack capacity", Item("firewood").weight * 4 > db.vehicles.Find(v => v.id == "walk").capacity);
            Check("cart supply fits handcart", Item("firewood").weight * 4 <= db.vehicles.Find(v => v.id == "handcart").capacity);
            return report;
        }
    }
}
