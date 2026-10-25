using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>Editor data and logic tests only. No live saves, scene generation or play.</summary>
    public static class R6Checks
    {
        [Serializable] public class Report { public string unityVersion; public List<string> passed = new List<string>(); }
        static void Check(Report r, string name, bool ok) { if (!ok) throw new Exception(name); r.passed.Add(name); }
        static void Set(GameManager gm, string name, object value) => typeof(GameManager).GetProperty(name).SetValue(gm, value);

        public static void RunBatch()
        {
            var report = Run();
            File.WriteAllText("Docs/Validation/R6-Unity-2026-10-24.json", JsonUtility.ToJson(report, true));
        }
        public static Report Run()
        {
            if (Application.isPlaying || GameManager.I != null) throw new InvalidOperationException("Use isolated editor; no active game session.");
            var r = new Report { unityVersion = Application.unityVersion };
            var db = Resources.Load<GameDatabase>("GameDatabase");
            var civilian = new HashSet<string> { "tutorial_3", "tutorial_5", "naru_board", "goryeo_jangdan_trade", "goryeo_songak_trade", "goryeo_return_trade" };
            Check(r, "all 12 existing quests retained", db.quests.Count(q => !q.id.StartsWith("q1_")) == 12);
            foreach (var q in db.quests.Where(q => !q.id.StartsWith("q1_")))
            {
                bool civil = civilian.Contains(q.id);
                Check(r, q.id + " new credit classification", q.rewardCredit == (civil ? 0 : 1));
                Check(r, q.id + " reputation metadata", civil ? q.rewardReputationScope == ReputationScope.Civilization
                    && q.rewardReputationTarget == "goryeo" && q.rewardReputationSize == ReputationSize.Small
                    : q.rewardReputationSize == ReputationSize.None);
                if (civil) Check(r, q.id + " original legacy promise", q.hasLegacyReward && q.legacyRewardCredit == (q.id == "tutorial_5" ? 2 : 1));
            }
            foreach (ReputationSize size in Enum.GetValues(typeof(ReputationSize))) Check(r, "approved point table " + size, ReputationAwards.PointsFor(size) == (size==ReputationSize.None?0:size==ReputationSize.VerySmall?10:size==ReputationSize.Small?25:size==ReputationSize.Medium?75:250));
            var go = new GameObject("R6 isolated logic test"); go.SetActive(false);
            var gm = go.AddComponent<GameManager>();
            typeof(GameManager).GetProperty("I").SetValue(null, gm);
            var player = new PlayerState { credit = 73, money = 100 };
            var clock = new GameClock(); Set(gm, "Player", player); Set(gm, "Clock", clock); Set(gm, "Db", db);
            var test = ScriptableObject.CreateInstance<QuestData>();
            test.id = "r6_test"; test.rewardMoney = 20; test.rewardCredit = 0; test.hasLegacyReward = true;
            test.legacyRewardCredit = 2; test.legacyRewardMoney = 60;
            test.rewardReputationScope = ReputationScope.Civilization; test.rewardReputationTarget = "goryeo"; test.rewardReputationSize = ReputationSize.Small;
            test.objectives.Add(new QuestObjective { type = ObjectiveType.Talk, targetId = "r6_npc" });
            var events = new List<ReputationContribution>();
            void Subscribe() { GameEvents.ClearAll(); GameEvents.ReputationContributed += events.Add; events.Clear(); }
            try
            {
                Subscribe(); var qm = new QuestManager(new List<QuestData> { test });
                qm.Accept(test); var accepted = new SaveData(); qm.Export(accepted);
                Check(r, "new reward target captured", accepted.quests[0].acceptedReputationTarget == "goryeo");
                test.rewardReputationTarget = "changed_after_acceptance";
                var restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(accepted)); qm.Import(restored);
                qm.TryCompleteAt("r6_npc", out _);
                Check(r, "noncontract new completion never changes credit", player.credit == 73);
                Check(r, "new contribution uses captured target and Small size once", events.Count == 1 && events[0].targetId == "goryeo" && events[0].size == ReputationSize.Small && events[0].Points == 25);
                qm.TryCompleteAt("r6_npc", out _); Check(r, "repeat completion cannot replay contribution", events.Count == 1);
                var finished = new SaveData(); qm.Export(finished); qm.Import(finished);
                Check(r, "reload completed reward does not replay", events.Count == 1 && !qm.CanStart(test));
                foreach (bool captured in new[] { true, false })
                {
                    Subscribe(); qm = new QuestManager(new List<QuestData> { test });
                    var old = new SaveData { version = 2, credit = -7 };
                    old.quests.Add(new QuestManager.Save { id = test.id, rewardCaptured = captured, acceptedRewardMoney = 31, acceptedRewardCredit = 1 });
                    string json = JsonUtility.ToJson(old); var data = JsonUtility.FromJson<SaveData>(json);
                    qm.Import(data); int before = player.credit; int cash = player.money;
                    qm.TryCompleteAt("r6_npc", out _);
                    Check(r, "legacy " + captured + " promised credit granted", player.credit == before + (captured ? 1 : 2));
                    Check(r, "legacy " + captured + " promised money granted", player.money == cash + (captured ? 31 : 60));
                    Check(r, "legacy " + captured + " gains no retrospective reputation", events.Count == 0);
                    Check(r, "legacy " + captured + " saved accumulated credit unchanged", data.credit == -7 && JsonUtility.ToJson(data) == json);
                }
                Subscribe(); var road = go.AddComponent<RoadEvent>(); road.type = RoadEvent.Type.FallenTraveler;
                road.reputationScope = ReputationScope.Civilization; road.reputationTarget = "goryeo"; road.reputationSize = ReputationSize.Small;
                int credit = player.credit; float time = clock.TotalHours;
                Check(r, "road help executes", road.HelpTraveler());
                Check(r, "road goodwill leaves credit unchanged", player.credit == credit);
                Check(r, "road help spends one hour", Mathf.Abs(clock.TotalHours - time - 1) < .01f);
                Check(r, "road explicit civilization contribution", events.Count == 1 && events[0].targetId == "goryeo" && events[0].scope == ReputationScope.Civilization && events[0].size == ReputationSize.Small);
                Check(r, "road repeated help no reward", !road.HelpTraveler() && events.Count == 1);
                var goryeo = db.towns.Find(t => t.id == "naru");
                var central = db.towns.Find(t => t.id == "transit_woodtown");
                Check(r, "central region configured per town", central.region.townReputation && !goryeo.region.townReputation);
                foreach (var from in new[] { goryeo, central })
                foreach (string kind in new[] { "success", "partial", "failed", "expired", "lost" })
                {
                    Subscribe(); var p = new PlayerState { credit = -3, money = 100 }; var cclock = new GameClock();
                    var cm = new ContractManager(db, cclock, p);
                    var c = new Contract { id = 999, fromTown = from.id, toTown = "transit_ferry", reward = 10, weight = 1,
                        acceptedAt = 7, deadline = kind == "partial" ? 7 : 10, finalDeadline = kind == "expired" ? 7 : 12, condition = kind == "failed" ? .2f : 1f };
                    p.contracts.Add(c);
                    if (kind == "expired") { cm.OnHour(); cm.OnHour(); }
                    else if (kind == "lost") { cm.Lose(c); cm.Lose(c); }
                    else { cm.Deliver(c, "transit_ferry"); cm.Deliver(c, "transit_ferry"); }
                    var scope = from == central ? ReputationScope.Town : ReputationScope.Civilization;
                    string target = from == central ? from.id : "goryeo";
                    Check(r, from.id + " " + kind + " one issuer-only contribution", events.Count == 1 && events[0].scope == scope && events[0].targetId == target);
                    Check(r, from.id + " " + kind + " credit retained", p.credit == -3 + (kind == "success" ? 1 : kind == "partial" ? 0 : -1));
                    Check(r, from.id + " " + kind + " size/direction/approved points", events[0].size == (kind == "lost" ? ReputationSize.Small : kind == "partial" ? ReputationSize.VerySmall : ReputationSize.Medium)
                        && events[0].direction == (kind == "success" ? 1 : -1) && events[0].Points == events[0].direction * ReputationAwards.PointsFor(events[0].size));
                }
                Subscribe(); ReputationAwards.Emit(ReputationScope.Town, "central_a", ReputationSize.Small, 1, "probe");
                Check(r, "explicit central target distinct from other town", events.Count == 1 && events[0].targetId == "central_a" && events[0].scope == ReputationScope.Town);
                GameEvents.ClearAll(); ReputationAwards.Emit(ReputationScope.Town, "central_b", ReputationSize.Small, 1, "probe");
                Check(r, "event reset clears subscribers", events.Count == 1);
            }
            finally { GameEvents.ClearAll(); typeof(GameManager).GetProperty("I").SetValue(null, null); UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(test); }
            return r;
        }
    }
}
