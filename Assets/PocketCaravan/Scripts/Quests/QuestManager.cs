using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    public enum QuestMarker { None, Available, InProgress, TurnIn }

    /// <summary>
    /// 퀘스트 진행 상태. GameManager가 들고 있고, GameEvents를 듣고 단계를 넘긴다.
    /// </summary>
    public class QuestManager
    {
        public class Progress
        {
            public QuestData quest;
            public int step;
            public int amount;   // 현재 단계 진행 수치
            public bool rewardCaptured;
            public int acceptedRewardMoney, acceptedRewardCredit;
            public int RewardMoney => rewardCaptured ? acceptedRewardMoney : quest.rewardMoney;
            public int RewardCredit => rewardCaptured ? acceptedRewardCredit : quest.rewardCredit;
            public bool Done => step >= quest.objectives.Count;
            public QuestObjective Current => Done ? null : quest.objectives[step];
        }

        readonly List<QuestData> all;
        readonly Dictionary<QuestData, Progress> active = new Dictionary<QuestData, Progress>();
        readonly HashSet<QuestData> finished = new HashSet<QuestData>();

        public IEnumerable<Progress> Active => active.Values;

        /// <summary>수첩에서 선택한 길 안내 퀘스트. 새 수락 시 그 의뢰를 선택한다.</summary>
        public Progress Tracked { get; private set; }
        public bool IsFinished(QuestData q) => finished.Contains(q);
        public void Track(Progress progress)
        {
            if (progress != null && active.TryGetValue(progress.quest, out var current) && current == progress) Tracked = progress;
        }

        public QuestManager(List<QuestData> quests)
        {
            all = quests ?? new List<QuestData>();
            GameEvents.MapEntered += id => Advance(ObjectiveType.VisitMap, id, null, 1);
            GameEvents.ItemBought += (item, town, price) => Advance(ObjectiveType.Buy, town != null ? town.id : null, item, 1);
            GameEvents.ItemSold += (item, town, price, profit) =>
            {
                Advance(ObjectiveType.Sell, town != null ? town.id : null, item, 1);
                if (profit > 0) Advance(ObjectiveType.Profit, null, null, profit);
            };
            GameEvents.VehicleBought += v => AdvanceVehicle(v);
            GameEvents.ContractFinished += (c, ok) => { if (ok && c != null) Advance(ObjectiveType.CompleteContract, c.fromTown, null, 1); };
        }

        // ---------- 저장 ----------
        [System.Serializable]
        public class Save
        {
            public string id; public int step, amount;
            public bool rewardCaptured;
            public int acceptedRewardMoney, acceptedRewardCredit;
        }

        public void Export(SaveData d)
        {
            foreach (var p in active.Values) d.quests.Add(new Save { id = p.quest.id, step = p.step, amount = p.amount, rewardCaptured = true, acceptedRewardMoney = p.RewardMoney, acceptedRewardCredit = p.RewardCredit });
            foreach (var q in finished) d.finishedQuests.Add(q.id);
            d.trackedQuest = Tracked != null ? Tracked.quest.id : null;
        }

        public void Import(SaveData d)
        {
            active.Clear(); finished.Clear(); Tracked = null;
            QuestData Find(string id) => all.Find(q => q != null && q.id == id);
            foreach (var id in d.finishedQuests) { var q = Find(id); if (q != null) finished.Add(q); }
            foreach (var s in d.quests)
            {
                var q = Find(s.id);
                if (q == null) continue;
                var p = new Progress { quest = q, step = s.step, amount = s.amount, rewardCaptured = true,
                    acceptedRewardMoney = s.rewardCaptured ? s.acceptedRewardMoney : q.hasLegacyReward ? q.legacyRewardMoney : q.rewardMoney,
                    acceptedRewardCredit = s.rewardCaptured ? s.acceptedRewardCredit : q.hasLegacyReward ? q.legacyRewardCredit : q.rewardCredit };
                active[q] = p;
                if (q.id == d.trackedQuest || Tracked == null) Tracked = p;
            }
        }

        // ---------- NPC ----------

        public bool CanStart(QuestData q)
        {
            if (q == null || active.ContainsKey(q) || finished.Contains(q)) return false;
            foreach (var r in q.requires) if (r != null && !finished.Contains(r)) return false;
            if (q.requiresAny != null && q.requiresAny.Count > 0 && !q.requiresAny.Exists(r => r != null && finished.Contains(r))) return false;
            if (!string.IsNullOrEmpty(q.choiceGroup))
                foreach (var other in all)
                    if (other != null && other != q && other.choiceGroup == q.choiceGroup && (active.ContainsKey(other) || finished.Contains(other))) return false;
            return true;
        }

        public QuestData AvailableFrom(string npcId)
        {
            // MarkerFor가 매 프레임 호출하므로 전체 제안 목록은 대화창을 열 때만 만든다.
            foreach (var q in all) if (q != null && q.giverNpc == npcId && CanStart(q)) return q;
            return null;
        }

        /// <summary>비용이 부족한 의뢰도 열람 가능. 열람과 수락 조건은 별도로 판정한다.</summary>
        public List<QuestData> AvailableAllFrom(string npcId)
        {
            var offers = new List<QuestData>();
            foreach (var q in all)
                if (q != null && q.giverNpc == npcId && CanStart(q) && !offers.Contains(q)) offers.Add(q);
            return offers;
        }

        /// <summary>이 NPC에게 말을 걸면 끝나는 단계가 있는 진행 중 퀘스트</summary>
        public Progress WaitingAt(string npcId)
        {
            Progress missingCargo = null;
            foreach (var p in active.Values)
            {
                var o = p.Current;
                if (o == null || (o.type != ObjectiveType.Talk && o.type != ObjectiveType.Deliver) || o.targetId != npcId) continue;
                if (o.type == ObjectiveType.Talk || HasDeliveryItems(o)) return p;
                if (missingCargo == null) missingCargo = p;
            }
            return missingCargo;
        }

        public QuestMarker MarkerFor(string npcId)
        {
            var w = WaitingAt(npcId);
            if (w != null) return w.Current.type == ObjectiveType.Deliver && !HasDeliveryItems(w.Current) ? QuestMarker.InProgress : QuestMarker.TurnIn;
            if (AvailableFrom(npcId) != null) return QuestMarker.Available;
            foreach (var p in active.Values) if (p.quest.giverNpc == npcId) return QuestMarker.InProgress;
            return QuestMarker.None;
        }

        /// <summary>수락할 수 없는 까닭 (돈·짐). 없으면 null</summary>
        public string AcceptBlocker(QuestData q)
        {
            var p = GameManager.I.Player;
            if (q.offerItem != null && q.offerCount > 0)
            {
                if (p.money < q.offerPrice * q.offerCount) return $"{q.offerPrice * q.offerCount}냥이 있어야 넘겨줄 수 있네. 돈을 좀 더 모아 오게.";
                if (!p.CanCarry(q.offerItem, q.offerCount)) return "짐이 꽉 찼구먼. 짐을 좀 덜고 오게.";
            }
            if (q.giveItem != null && q.giveCount > 0 && !p.CanCarry(q.giveItem, q.giveCount)) return "짐이 꽉 찼구먼. 짐을 좀 덜고 오게.";
            return null;
        }

        public void Accept(QuestData q)
        {
            if (!CanStart(q) || AcceptBlocker(q) != null) return;
            if (q.offerItem != null && q.offerCount > 0)
            {
                var pl = GameManager.I.Player;
                pl.money -= q.offerPrice * q.offerCount;
                pl.cargo.Add(q.offerItem, q.offerCount, q.offerPrice);   // 산 값으로 기록해 이익이 제대로 계산되게
                Sfx.Play("coin");
            }
            var p = new Progress { quest = q, rewardCaptured = true, acceptedRewardMoney = q.rewardMoney, acceptedRewardCredit = q.rewardCredit };
            active[q] = p;
            Tracked = p;
            if (q.giveItem != null && q.giveCount > 0) GameManager.I.Player.cargo.Add(q.giveItem, q.giveCount);
            Notify.Show($"퀘스트 수락: {q.title}");
            Sfx.Play("accept");
            CheckPassive(p);
        }

        /// <summary>Talk/Deliver 단계를 이 NPC에게서 끝낸다. 끝낸 단계의 대사를 돌려준다 (실패하면 null).</summary>
        public string TryCompleteAt(string npcId, out string failReason)
        {
            failReason = null;
            var p = WaitingAt(npcId);
            if (p == null) return null;
            var o = p.Current;
            if (o.type == ObjectiveType.Deliver)
            {
                if (!HasDeliveryItems(o))
                {
                    failReason = $"{o.item.displayName} {o.count}개가 필요하네. (지금 {GameManager.I.Player.cargo.Count(o.item)}개)";
                    return null;
                }
                GameManager.I.Player.cargo.Remove(o.item, o.count);
            }
            string line = string.IsNullOrEmpty(o.completeLine) ? "고맙네." : o.completeLine;
            NextStep(p);
            return line;
        }

        bool HasDeliveryItems(QuestObjective o) => o.item == null || GameManager.I.Player.cargo.Count(o.item) >= o.count;

        // ---------- 진행 ----------

        void Advance(ObjectiveType type, string targetId, ItemData item, int amount)
        {
            foreach (var p in new List<Progress>(active.Values))
            {
                var o = p.Current;
                if (o == null || o.type != type) continue;
                if (!string.IsNullOrEmpty(o.targetId) && o.targetId != targetId) continue;
                if (o.item != null && o.item != item) continue;
                p.amount += amount;
                if (p.amount >= o.count) NextStep(p);
            }
        }

        void AdvanceVehicle(VehicleData v)
        {
            foreach (var p in new List<Progress>(active.Values))
            {
                var o = p.Current;
                if (o != null && o.type == ObjectiveType.BuyVehicle && (o.vehicle == null || o.vehicle == v)) NextStep(p);
            }
        }

        /// <summary>돈 모으기, 이미 가진 이동 수단처럼 상태로 판정하는 단계</summary>
        public void Tick()
        {
            foreach (var p in new List<Progress>(active.Values)) CheckPassive(p);
        }

        void CheckPassive(Progress p)
        {
            var o = p.Current;
            if (o == null) return;
            var player = GameManager.I.Player;
            if (o.type == ObjectiveType.HaveMoney && player.money >= o.count) NextStep(p);
            else if (o.type == ObjectiveType.BuyVehicle && o.vehicle != null && player.vehicle == o.vehicle) NextStep(p);
            else if (o.type == ObjectiveType.VisitMap && MapRoot.Current != null && MapRoot.Current.mapId == o.targetId) NextStep(p);
        }

        void NextStep(Progress p)
        {
            p.step++;
            p.amount = 0;
            if (p.Done) Finish(p);
            else CheckPassive(p);
        }

        void Finish(Progress p)
        {
            var q = p.quest;
            active.Remove(q);
            finished.Add(q);
            if (Tracked == p) { Tracked = null; foreach (var other in active.Values) Tracked = other; }
            var player = GameManager.I.Player;
            player.money += p.RewardMoney;
            player.credit += p.RewardCredit;
            string reward = "";
            if (p.RewardMoney > 0) reward += $" 돈 +{p.RewardMoney}";
            if (p.RewardCredit > 0) reward += $" 신용 +{p.RewardCredit}";
            Notify.Show($"퀘스트 완료: {q.title}{reward}");
            Sfx.Play("success");
            if (!string.IsNullOrEmpty(q.finishNote)) Notify.Show(q.finishNote, 6f);
        }

        /// <summary>
        /// 이 단계를 하려면 어디로 가야 하는지. kind: "npc"(npc id), "market"(마을 id), "map"(맵 id). 없으면 null.
        /// </summary>
        public (string kind, string id)? TargetOf(QuestObjective o)
        {
            if (o == null) return null;
            switch (o.type)
            {
                case ObjectiveType.Talk:
                case ObjectiveType.Deliver:
                    // 전할 물건이 모자라면, 그 물건을 파는 곳 먼저
                    if (o.type == ObjectiveType.Deliver && o.item != null && !o.item.questItem
                        && GameManager.I.Player.cargo.Count(o.item) < o.count && o.item.originTown != null)
                        return ("market", o.item.originTown.id);
                    return ("npc", o.targetId);
                case ObjectiveType.VisitMap:
                    return ("map", o.targetId);
                case ObjectiveType.CompleteContract:
                    var cs = GameManager.I.Player.contracts;
                    return cs.Count > 0 ? ("board", cs[0].toTown) : ("board", o.targetId);
                case ObjectiveType.Buy:
                    if (!string.IsNullOrEmpty(o.targetId)) return ("market", o.targetId);
                    return o.item != null && o.item.originTown != null ? ("market", o.item.originTown.id) : ((string, string)?)null;
                case ObjectiveType.Sell:
                    return !string.IsNullOrEmpty(o.targetId) ? ("market", o.targetId) : ((string, string)?)null;
                case ObjectiveType.BuyVehicle:
                    foreach (var t in GameManager.I.Db.towns)
                        if (t != null && t.vehiclesForSale.Contains(o.vehicle)) return ("market", t.id);
                    return null;
                default:
                    return null;
            }
        }

        public string ProgressText(Progress p)
        {
            var o = p.Current;
            if (o == null) return "";
            switch (o.type)
            {
                case ObjectiveType.Buy:
                case ObjectiveType.Sell:
                case ObjectiveType.Profit:
                case ObjectiveType.CompleteContract:
                    return $"{o.text} ({p.amount}/{o.count})";
                case ObjectiveType.Deliver:
                    return $"{o.text} ({Mathf.Min(GameManager.I.Player.cargo.Count(o.item), o.count)}/{o.count})";
                case ObjectiveType.HaveMoney:
                    return $"{o.text} ({Mathf.Min(GameManager.I.Player.money, o.count)}/{o.count})";
                default:
                    return o.text;
            }
        }
    }
}
