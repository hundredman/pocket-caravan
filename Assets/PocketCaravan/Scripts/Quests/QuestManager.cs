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
            public bool legacyMainMigrationEligible;
            public int acceptedRewardMoney, acceptedRewardCredit;
            public ReputationScope acceptedReputationScope;
            public string acceptedReputationTarget;
            public ReputationSize acceptedReputationSize;
            public int RewardMoney => rewardCaptured ? acceptedRewardMoney : quest.rewardMoney;
            public int RewardCredit => rewardCaptured ? acceptedRewardCredit : quest.rewardCredit;
            public bool Done => step >= quest.objectives.Count;
            public QuestObjective Current => Done ? null : quest.objectives[step];
        }

        readonly List<QuestData> all;
        readonly Dictionary<QuestData, Progress> active = new Dictionary<QuestData, Progress>();
        readonly HashSet<QuestData> finished = new HashSet<QuestData>();
        readonly HashSet<string> legacyMainFinished = new HashSet<string>();

        public IEnumerable<Progress> Active => active.Values;

        /// <summary>수첩에서 선택한 길 안내 퀘스트. 새 수락 시 그 의뢰를 선택한다.</summary>
        public Progress Tracked { get; private set; }
        public bool IsFinished(QuestData q) => finished.Contains(q);
        /// <summary>11/11 은샘: 퀘스트를 받았을 때 (길 안내 대상을 이 퀘스트로 바꾸는 데 쓴다)</summary>
        public static event System.Action<Progress> Accepted;
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
            public bool legacyMainMigrationEligible;
            public int acceptedRewardMoney, acceptedRewardCredit;
            public ReputationScope acceptedReputationScope;
            public string acceptedReputationTarget;
            public ReputationSize acceptedReputationSize;
        }

        public void Export(SaveData d)
        {
            foreach (var p in active.Values) d.quests.Add(new Save { id = p.quest.id, step = p.step, amount = p.amount, rewardCaptured = true, legacyMainMigrationEligible = p.legacyMainMigrationEligible, acceptedRewardMoney = p.RewardMoney, acceptedRewardCredit = p.RewardCredit, acceptedReputationScope = p.acceptedReputationScope, acceptedReputationTarget = p.acceptedReputationTarget, acceptedReputationSize = p.acceptedReputationSize });
            foreach (var q in finished) d.finishedQuests.Add(q.id);
            d.trackedQuest = Tracked != null ? Tracked.quest.id : null;
            d.q1MigrationVersion = 1;
            d.legacyMainFinishedIds = new List<string>(legacyMainFinished);
            d.legacyMainFinishedIds.Sort(System.StringComparer.Ordinal);
        }

        public void Import(SaveData d)
        {
            active.Clear(); finished.Clear(); legacyMainFinished.Clear(); Tracked = null;
            legacyMainFinished.UnionWith(d.q1MigrationVersion == 0 ? d.finishedQuests : d.legacyMainFinishedIds);
            QuestData Find(string id) => all.Find(q => q != null && q.id == id);
            foreach (var id in d.finishedQuests) { var q = Find(id); if (q != null) finished.Add(q); }
            foreach (var s in d.quests)
            {
                var q = Find(s.id);
                if (q == null) continue;
                var p = new Progress { quest = q, step = s.step, amount = s.amount, rewardCaptured = true,
                    legacyMainMigrationEligible = d.q1MigrationVersion == 0 || s.legacyMainMigrationEligible,
                    acceptedRewardMoney = s.rewardCaptured ? s.acceptedRewardMoney : q.hasLegacyReward ? q.legacyRewardMoney : q.rewardMoney,
                    acceptedRewardCredit = s.rewardCaptured ? s.acceptedRewardCredit : q.hasLegacyReward ? q.legacyRewardCredit : q.rewardCredit,
                    acceptedReputationScope = s.acceptedReputationScope, acceptedReputationTarget = s.acceptedReputationTarget,
                    acceptedReputationSize = s.rewardCaptured ? s.acceptedReputationSize : ReputationSize.None };
                active[q] = p;
                if (q.id == d.trackedQuest || Tracked == null) Tracked = p;
            }
            MigrateMainProgress();
        }

        static readonly string[] MainQuestIds = { "q1_naru", "q1_jangdan", "q1_songak", "q1_dakmok", "q1_gaegyeong" };
        bool FinishedId(string id) => all.Exists(q => q != null && q.id == id && finished.Contains(q));
        bool LegacyCondition(int step)
        {
            switch (step)
            {
                case 0: return legacyMainFinished.Contains("tutorial_1");
                case 1: return legacyMainFinished.Contains("goryeo_jangdan_trade") || legacyMainFinished.Contains("tutorial_3");
                case 2: return legacyMainFinished.Contains("naru_board");
                case 3: return legacyMainFinished.Contains("tutorial_2") || legacyMainFinished.Contains("goryeo_return_trade");
                case 4: return (legacyMainFinished.Contains("tutorial_2") && legacyMainFinished.Contains("dakmok_hanji")) || legacyMainFinished.Contains("tutorial_5");
                default: return false;
            }
        }
        void GrantMainRecord(int index)
        {
            var q = all.Find(value => value != null && value.id == MainQuestIds[index]);
            // Preserve an already accepted new quest and its promised cargo/reward too.
            if (q != null && !active.ContainsKey(q)) finished.Add(q);
        }
        public void MigrateMainProgress(string completedLegacyId = null)
        {
            if (completedLegacyId != null && !legacyMainFinished.Contains(completedLegacyId)) return;
            bool allOld = legacyMainFinished.Contains("tutorial_2") && legacyMainFinished.Contains("tutorial_5");
            for (int i = 0; i < MainQuestIds.Length; i++)
            {
                if (allOld || FinishedId(MainQuestIds[i])) { GrantMainRecord(i); continue; }
                if (!LegacyCondition(i)) break;
                GrantMainRecord(i);
            }
            // A legacy quest finishing now may add a later record across a gap (design 7.6).
            if (completedLegacyId == "tutorial_1") GrantMainRecord(0);
            if (completedLegacyId == "goryeo_jangdan_trade" || completedLegacyId == "tutorial_3") GrantMainRecord(1);
            if (completedLegacyId == "naru_board") GrantMainRecord(2);
            if (completedLegacyId == "tutorial_2" || completedLegacyId == "goryeo_return_trade") GrantMainRecord(3);
            if ((completedLegacyId == "tutorial_2" || completedLegacyId == "dakmok_hanji" || completedLegacyId == "tutorial_5") && LegacyCondition(4)) GrantMainRecord(4);
        }

        // ---------- NPC ----------

        public bool CanStart(QuestData q)
        {
            if (q == null || q.retired || active.ContainsKey(q) || finished.Contains(q)) return false;
            int mainIndex = System.Array.IndexOf(MainQuestIds, q.id);
            for (int i = 0; i < mainIndex; i++) if (!FinishedId(MainQuestIds[i])) return false;
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
                if (p.money < q.offerPrice * q.offerCount) return Speech(q, $"{q.offerPrice * q.offerCount}냥이 있어야 넘겨줄 수 있네. 돈을 좀 더 모아 오게.", $"{q.offerPrice * q.offerCount}냥이 있어야 넘겨줄 수 있어요. 돈을 좀 더 모아 오세요.");
                if (!p.CanCarry(q.offerItem, q.offerCount)) return Speech(q, "짐이 꽉 찼구먼. 짐을 좀 덜고 오게.", "짐이 꽉 찼네요. 짐을 좀 덜고 오세요.");
            }
            if (q.giveItem != null && q.giveCount > 0 && !p.CanCarry(q.giveItem, q.giveCount)) return Speech(q, "짐이 꽉 찼구먼. 짐을 좀 덜고 오게.", "짐이 꽉 찼네요. 짐을 좀 덜고 오세요.");
            return ExtraAcceptBlocker?.Invoke(q);   // 11/01 지역별 추가 조건 (예: 북유럽 동시 퀘스트 3개). 없으면 null
        }

        /// <summary>
        /// 11/08 공용 기본 대사의 말투: 퀘스트 주는 사람이 고려에 있으면 고려 말투, 다른 지역(베네치아·아라비아·북유럽)이면 보통 말투.
        /// 예전엔 모든 지역 주민이 "…넘겨줄 수 있네", "고맙네" 같은 고려 말투를 썼다
        /// </summary>
        static string Speech(QuestData q, string goryeo, string plain) => !StoryGuide.RegionOf(q, out var r) || r == Reactions.Region.Goryeo ? goryeo : plain;

        /// <summary>11/01 지역이 수락 조건을 더할 수 있는 자리. 막을 이유(대사)를 돌려주면 수락이 막힌다. 공용 규칙은 그대로.</summary>
        public static System.Func<QuestData, string> ExtraAcceptBlocker;

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
            var p = new Progress { quest = q, rewardCaptured = true, acceptedRewardMoney = q.rewardMoney, acceptedRewardCredit = q.rewardCredit,
                acceptedReputationScope = q.rewardReputationScope, acceptedReputationTarget = q.rewardReputationTarget, acceptedReputationSize = q.rewardReputationSize };
            active[q] = p;
            Tracked = p;
            Accepted?.Invoke(p);   // 11/11 은샘: 가장 최근에 받은 것을 길 안내 (QuestFocus)
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
                    failReason = Speech(p.quest, $"{o.item.displayName} {o.count}개가 필요하네.", $"{o.item.displayName} {o.count}개가 필요해요.") + $" (지금 {GameManager.I.Player.cargo.Count(o.item)}개)";
                    return null;
                }
                GameManager.I.Player.cargo.Remove(o.item, o.count);
            }
            string line = string.IsNullOrEmpty(o.completeLine) ? Speech(p.quest, "고맙네.", "고마워요.") : o.completeLine;
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
                if (!string.IsNullOrEmpty(o.targetId) && (type==ObjectiveType.VisitMap ? GoryeoLayout.Resolve(o.targetId)!=GoryeoLayout.Resolve(targetId) : o.targetId!=targetId)) continue;
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
            else if (o.type == ObjectiveType.VisitMap && MapRoot.Current != null && MapRoot.Current.mapId == GoryeoLayout.Resolve(o.targetId)) NextStep(p);
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
            if (p.legacyMainMigrationEligible)
            {
                legacyMainFinished.Add(q.id);
                MigrateMainProgress(q.id);
            }
            var player = GameManager.I.Player;
            player.money += p.RewardMoney;
            player.credit += p.RewardCredit;
            ReputationAwards.Emit(p.acceptedReputationScope, p.acceptedReputationTarget, p.acceptedReputationSize, 1, "quest:" + q.id);
            string reward = "";
            if (p.RewardMoney > 0) reward += $" 돈 +{p.RewardMoney}";
            if (p.RewardCredit > 0) reward += $" 신용 +{p.RewardCredit}";
            Notify.Show($"퀘스트 완료: {q.title}{reward}");
            Sfx.Play("success");
            if (!string.IsNullOrEmpty(q.finishNote)) Notify.Story(q.finishNote, 6f);   // 11/11 "다음 이야기" 모양 (책 그림 + 머리글)
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
                    if (!string.IsNullOrEmpty(o.targetId)) return ("market", o.targetId);   // 11/08 손수레처럼 여러 지역이 파는 수단은 의뢰가 장터를 정해 준다
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
