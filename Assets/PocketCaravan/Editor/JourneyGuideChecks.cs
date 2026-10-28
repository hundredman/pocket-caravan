using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class JourneyGuideChecks
    {
        [Serializable] public class Report { public List<string> passed = new List<string>(); }
        static void Check(Report report, string name, bool ok)
        { if (!ok) throw new Exception(name); report.passed.Add(name); }

        public static Report RunData()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first");
            var report = new Report();
            var db = Resources.Load<GameDatabase>("GameDatabase");
            var region = db.towns.Find(t => t.id == "gaegyeong").region;
            var towns = JourneyGuide.Destinations(db, region, WorldIndex.Get(), "transit_pass");   // M4: 옛 Route01 대신 고려와 이어진 남동 길
            Check(report, "five connected regional destinations", towns.Count == 5);
            Check(report, "no foreign regional destination", towns.TrueForAll(t => t.region.id == region.id));
            Check(report, "all five authored with return trade hints", towns.TrueForAll(t => !string.IsNullOrWhiteSpace(t.journeyNote)));
            Check(report, "unknown map offers no false destination", JourneyGuide.Destinations(db, region, WorldIndex.Get(), "missing").Count == 0);
            Check(report, "missing region offers no destination", JourneyGuide.Destinations(db, null, WorldIndex.Get(), "transit_pass").Count == 0);
            var p = new PlayerState();
            Check(report, "credit zero previews info at one", JourneyGuide.NextUnlock(towns, p).Contains("다음 신용 1"));
            p.credit = 1;
            Check(report, "credit one previews warehouse at three", JourneyGuide.NextUnlock(towns, p).Contains("다음 신용 3") && JourneyGuide.NextUnlock(towns, p).Contains("객주"));
            Check(report, "premium preview mentions conditional board availability", JourneyGuide.NextUnlock(towns, p).Contains("제안이 있을 때"));
            p.credit = 3;
            Check(report, "credit three does not invent further tier", !JourneyGuide.NextUnlock(towns, p).Contains("다음 신용"));
            p.credit = -1;
            Check(report, "lost credit previews basic rest access at zero", JourneyGuide.NextUnlock(towns, p).Contains("다음 신용 0"));
            Check(report, "empty region has no phantom premium unlock", !JourneyGuide.NextUnlock(new List<TownData>(), p).Contains("다음 신용"));
            var ledger = new TradeLedger();
            Check(report, "unobserved town not treated as visited market", !JourneyGuide.MarketRecorded(ledger, "naru"));
            ledger.Import(new List<TradeLedger.Quote> { new TradeLedger.Quote { town="naru", item="salt", buy=1, sell=1 } });
            Check(report, "observed market distinguished from unknown", JourneyGuide.MarketRecorded(ledger, "naru") && !JourneyGuide.MarketRecorded(ledger, "dakmok"));
            return report;
        }

        // Isolated non-start Play session only. Synthetic quests never enter the production database/save.
        public static Report RunPlay()
        {
            var gm = GameManager.I;
            if (!EditorApplication.isPlaying || gm == null || MapRoot.Current == null || MapRoot.Current.mapId == "Naru")
                throw new InvalidOperationException("Initialized non-start Play session required");
            var report = new Report(); var player = gm.Player;
            int money=player.money, credit=player.credit;
            var saved = new List<Tuple<ItemData,int,float>>();
            foreach (var row in player.cargo.Items) saved.Add(Tuple.Create(row.Key,row.Value,player.cargo.AverageCost(row.Key)));
            var fixtures = new List<QuestData>();
            QuestData Quest(string id)
            {
                var q = ScriptableObject.CreateInstance<QuestData>(); q.id=id; q.giverNpc="test_guide_giver";
                q.objectives.Add(new QuestObjective { type=ObjectiveType.Talk, targetId="test_guide_giver" }); fixtures.Add(q); return q;
            }
            try
            {
                player.money=0; player.cargo.Clear();
                var expensive=Quest("guide_expensive"); expensive.offerItem=gm.ItemById("paper"); expensive.offerCount=1; expensive.offerPrice=999;
                var free=Quest("guide_free");
                var manager=new QuestManager(new List<QuestData> { expensive, free, free });
                Check(report,"all offers browsable despite first unaffordable", manager.AvailableAllFrom(expensive.giverNpc).Count==2);
                Check(report,"no cross NPC offers",manager.AvailableAllFrom("other").Count==0);
                Check(report,"expensive offer still blocked",manager.AcceptBlocker(expensive)!=null);
                manager.Accept(expensive);
                Check(report,"blocked accept does not add cargo or deduct funds",player.money==0 && player.cargo.Count(expensive.offerItem)==0 && manager.Tracked==null);
                manager.Accept(free);
                Check(report,"free alternative accepted independently",manager.Tracked!=null && manager.Tracked.quest==free && player.money==0);
                Check(report,"accepted quest removed from offers",manager.AvailableAllFrom(free.giverNpc).Count==1);
                var savedProgress=manager.Tracked;
                manager.Track(new QuestManager.Progress { quest=expensive });
                Check(report,"cannot track inactive progress",manager.Tracked==savedProgress);
                manager.Track(new QuestManager.Progress { quest=free });
                Check(report,"cannot forge an active tracking progress",manager.Tracked==savedProgress);
                manager.TryCompleteAt(free.giverNpc,out string failure);
                Check(report,"completed free offer stays unavailable",manager.IsFinished(free) && manager.AvailableAllFrom(free.giverNpc).Count==1);
                var blocked=Quest("guide_missing_delivery"); blocked.objectives[0]=new QuestObjective { type=ObjectiveType.Deliver, targetId="test_guide_giver", item=gm.ItemById("firewood"), count=4 };
                var ready=Quest("guide_ready_talk");
                var turnIn=new QuestManager(new List<QuestData>{blocked,ready}); turnIn.Accept(blocked); turnIn.Accept(ready);
                Check(report,"missing cargo does not hide ready turn-in",turnIn.WaitingAt(ready.giverNpc).quest==ready);
                turnIn.TryCompleteAt(ready.giverNpc,out failure);
                Check(report,"ready turn-in completes before missing cargo",turnIn.IsFinished(ready) && !turnIn.IsFinished(blocked));
                turnIn.TryCompleteAt(blocked.giverNpc,out failure);
                Check(report,"missing delivery still rejected",failure!=null && !turnIn.IsFinished(blocked));
                turnIn.Track(turnIn.Tracked);
                var export=new SaveData(); turnIn.Export(export);
                var restored=new QuestManager(new List<QuestData>{blocked,ready}); restored.Import(export);
                Check(report,"selected tracking survives export/import",export.trackedQuest==blocked.id && restored.Tracked!=null && restored.Tracked.quest==blocked && restored.Tracked.step==turnIn.Tracked.step);
                return report;
            }
            finally
            {
                player.money=money; player.credit=credit; player.cargo.Clear();
                foreach (var row in saved) player.cargo.Add(row.Item1,row.Item2,row.Item3);
                foreach(var q in fixtures) UnityEngine.Object.Destroy(q);
                Notify.Clear();
            }
        }
    }
}
