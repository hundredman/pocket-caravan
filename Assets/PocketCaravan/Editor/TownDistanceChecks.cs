using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>Edit-mode checks; no scene generation or persistent player save writes.</summary>
    public static class TownDistanceChecks
    {
        [Serializable] class Report { public string unity; public int checks, prices, quotes; public List<string> passed = new List<string>(); }
        [Serializable] class Oracle { public string town, item; public int level, buy, sell; public float ratio, demand; }
        [Serializable] class Snapshot { public Oracle[] oracle; }
        public static void RunBatch()
        {
            if (Application.isPlaying || GameManager.I != null) throw new InvalidOperationException("Use isolated Edit mode.");
            var report = new Report { unity = Application.unityVersion };
            void Check(bool ok, string name) { if (!ok) throw new Exception(name); report.checks++; }
            var db = Resources.Load<GameDatabase>("GameDatabase"); var world = WorldIndex.Get();
            var config = JsonUtility.FromJson<TownDistance.Settings>(Resources.Load<TextAsset>("TownRoutes").text);
            Check(config.mode == "current", "production mode");
            foreach (var a in db.towns) foreach (var b in db.towns)
            {
                var delta = a.worldCoord - b.worldCoord;
                Check(ContractManager.Distance(a, b) == Mathf.Abs(delta.x) + Mathf.Abs(delta.y), "current distance");
            }
            var source = JsonUtility.FromJson<Snapshot>(File.ReadAllText("Docs/Validation/R2-2026-10-25/source-snapshot.json"));
            foreach (var row in source.oracle)
            {
                var town = db.towns.Find(t => t.id == row.town); var item = db.items.Find(i => i.id == row.item);
                float opt = town.OptimalFor(item) * (1f + .5f * (row.level - 1));
                Check(MarketMath.Price(town.BasePriceFor(item), opt, opt * row.ratio, row.demand) == row.buy, "buy oracle");
                Check(MarketMath.Buyback(MarketMath.Price(town.BasePriceFor(item), opt, opt * row.ratio + 1, row.demand)) == row.sell, "sell oracle");
                report.prices++;
            }
            var active = new Contract { id = 9000, fromTown = "naru", toTown = "gaegyeong", reward = 777,
                hours = 33, acceptedAt = 5, deadline = 38, finalDeadline = 54.5f, condition = .8f };
            var saved = new SaveData(); saved.contracts.Add(active);
            string json = JsonUtility.ToJson(saved);
            var restored = JsonUtility.FromJson<SaveData>(json);
            Check(json == JsonUtility.ToJson(restored), "Unity old contract JSON roundtrip");
            var player = new PlayerState(); player.contracts.Add(restored.contracts[0]);
            var cm = new ContractManager(db, new GameClock(), player, world, 43);
            cm.boards.Add(new ContractManager.Board { town = "naru", list = new List<Contract> { restored.contracts[0] } });
            cm.InitBoards(); Check(json == JsonUtility.ToJson(restored), "accepted contract current untouched");
            var cache = typeof(TownDistance).GetField("cached", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                config.mode = "layoutD"; cache.SetValue(null, config);
                foreach (var a in db.towns) foreach (var b in db.towns)
                {
                    Check(ContractManager.Distance(a, b) == DeliveryReward.RouteSteps(world, a.id, b.id), "layoutD unified wrappers");
                    foreach (var item in db.items.Where(i => i.originTown == a)) Check(item.MapsFrom(b) == ContractManager.Distance(a, b), "layoutD item wrapper");
                    foreach (int weight in Enumerable.Range(2, 11)) foreach (bool premium in new[] { false, true })
                    {
                        int d = ContractManager.Distance(a, b);
                        Check(DeliveryReward.Quote(DeliveryReward.Rules(a), d, d, weight, premium) >= 1, "valid reward"); report.quotes++;
                    }
                }
                cm.InitBoards(); Check(json == JsonUtility.ToJson(restored), "accepted contract layoutD untouched");
            }
            finally { cache.SetValue(null, null); }
            report.passed.Add("current price oracle, all town distances, layoutD wrappers, Unity contract serialization and accepted terms");
            string folder = "Docs/Validation/E1-Unified-2026-10-25";
            File.WriteAllText(folder + "/unity.json", JsonUtility.ToJson(report, true));
            File.WriteAllText(folder + "/q1.json", JsonUtility.ToJson(Q1Checks.Run(), true));
            File.WriteAllText(folder + "/r6.json", JsonUtility.ToJson(R6Checks.Run(), true));
            File.WriteAllText(folder + "/save.json", JsonUtility.ToJson(SaveCompatibilityChecks.RunData(), true));
            Debug.Log("E1 checks completed: " + report.checks);
        }
    }
}
