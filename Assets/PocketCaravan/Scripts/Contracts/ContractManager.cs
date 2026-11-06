using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 마을 게시판의 배달 의뢰. 게시판마다 의뢰 수가 정해져 있고 시간이 지나야 새로 생긴다
    /// (같은 의뢰를 끝없이 반복해 돈을 찍어내지 못하게).
    /// 보수 = 거리·무게·기한으로 정함. 성공 신용 +1, 실패 −1 (4주 범위의 단순한 규칙).
    /// </summary>
    public class ContractManager
    {
        public const int BoardSize = 3;
        public const int PremiumCredit = 3;
        public const int MaxActive = 3;
        const float RefillHours = 4f;

        [Serializable]
        public class Board
        {
            public string town;
            public float lastAdded;
            public List<Contract> list = new List<Contract>();
        }

        readonly GameDatabase db;
        readonly GameClock clock;
        readonly WorldIndex world;
        readonly PlayerState player;
        public List<Board> boards = new List<Board>();
        public int nextId = 1;
        readonly System.Random rng;

        static readonly Dictionary<string, string[]> CargoNames = new Dictionary<string, string[]>
        {
            ["naru"] = new[] { "소금 자루", "건어물 상자", "뱃사람 편지", "곡식 섬" },
            ["gaegyeong"] = new[] { "인삼 상자", "비단 꾸러미", "약재 보따리", "관청 문서" },
            ["dakmok"] = new[] { "한지 묶음", "닥나무 껍질", "붓과 먹", "종이 상자" },
            // 11/03 고려·중앙 동쪽: 다른 마을 게시판도 '짐 꾸러미' 대신 그 마을 물건 이름을 쓴다.
            ["goryeo_songak"] = new[] { "인삼 상자", "약초 보따리", "산나물 바구니" },
            ["goryeo_jangdan"] = new[] { "곡식 섬", "볏짚 묶음", "콩 자루" },
            ["transit_woodtown"] = new[] { "장작 묶음", "송진 단지", "나무 그릇 상자" },
            ["transit_ferry"] = new[] { "말린 생선 꾸러미", "그물 꾸러미", "송진 단지" },
            ["transit_southport"] = new[] { "소금 가마니", "건어물 상자", "뱃사람 짐" },
            ["transit_crossroads"] = new[] { "객주 장부", "먼 길 짐 보따리", "곡식 자루" },
        };

        public ContractManager(GameDatabase db, GameClock clock, PlayerState player, WorldIndex world = null, int? randomSeed = null)
        {
            rng = randomSeed.HasValue ? new System.Random(randomSeed.Value) : new System.Random();
            this.db = db; this.clock = clock; this.player = player; this.world = world != null ? world : WorldIndex.Get();
        }

        public void InitBoards()
        {
            foreach (var t in db.towns)
            {
                if (t == null) continue;
                var existing = BoardOf(t.id);
                if (existing != null)
                {
                    // Only unaccepted offers are replaced. Cargo already entrusted to the player is preserved.
                    int removed = existing.list.RemoveAll(c => c == null || c.fromTown != t.id || !CanOffer(t, Town(c.toTown)));
                    for (int i = 0; i < removed; i++)
                    {
                        var replacement = Generate(t);
                        if (replacement != null) existing.list.Add(replacement);
                    }
                    foreach (var offer in existing.list) RepriceUnaccepted(t, offer);
                    EnsureBasic(existing, t); continue;
                }
                var b = new Board { town = t.id, lastAdded = clock.TotalHours };
                boards.Add(b);
                for (int i = 0; i < BoardSize; i++)
                {
                    var offer = Generate(t);
                    if (offer != null) b.list.Add(offer);
                }
                EnsureBasic(b, t);
            }
        }

        // 신용이 낮은 상인도 다시 시작할 수 있도록 비어 있지 않은 게시판에 일반 의뢰 하나를 남긴다.
        void EnsureBasic(Board b, TownData town)
        {
            if (b.list.Count == 0 || b.list.Exists(c => !c.Premium && !c.atonement)) return;
            var basic = Generate(town, true);
            int last = b.list.FindLastIndex(c => c != null && !c.atonement);
            if (basic != null && last >= 0) b.list[last] = basic;
        }

        public Board BoardOf(string town) => boards.Find(b => b.town == town);
        public List<Contract> Available(string town)
        {
            var b=BoardOf(town);
            EnsureAtonement(b,Town(town));
            return b?.list.FindAll(c=>c!=null && c.fromTown==town && CanOffer(Town(town),Town(c.toTown))) ?? new List<Contract>();
        }

        /// <summary>관문 해금 전 기본 규칙. 출신이 아니라 출발 마을의 지역과 실제 연결을 확인한다.</summary>
        public bool CanOffer(TownData from, TownData to)
        {
            if (from == null || to == null || from == to || from.region == null || to.region == null
                || string.IsNullOrEmpty(from.region.id) || from.region.id != to.region.id || world == null) return false;
            string source = world.MapOf("market", from.id), target = world.MapOf("market", to.id);
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target) || world.Map(source) == null || world.Map(target) == null) return false;
            return source == target || world.NextStep(source, target) != null;
        }

        TownData Town(string id) => db.towns.Find(t => t != null && t.id == id);

        public static int Distance(TownData a, TownData b)
        {
            return TownDistance.Between(a.id, b.id, a.worldCoord, b.worldCoord);
        }

        Contract Generate(TownData from, bool basic = false)
        {
            var others = db.towns.FindAll(t => CanOffer(from, t));
            if (others.Count == 0) return null; // 새 지역의 첫 거점만 연결된 단계에도 부팅 가능.
            var to = others[rng.Next(others.Count)];
            int dist = Mathf.Max(1, Distance(from, to));
            var names = CargoNames.TryGetValue(from.id, out var n) ? n : new[] { "짐 꾸러미" };
            float weight = rng.Next(2, 7);
            bool premium = !basic && rng.NextDouble() < 0.25;
            var rules = DeliveryReward.Rules(from);
            if (rules != null && premium)
                weight = rng.Next(Mathf.Max(1, rules.premiumMinWeight), Mathf.Max(rules.premiumMinWeight, rules.premiumMaxWeight) + 1);
            int steps = rules != null ? DeliveryReward.RouteSteps(world, from.id, to.id) : 0;
            int reward = DeliveryReward.Quote(rules, dist, steps, weight, premium, 0.85f + (float)rng.NextDouble() * 0.3f);
            return new Contract
            {
                id = nextId++,
                fromTown = from.id,
                toTown = to.id,
                cargo = (premium ? "귀한 " : "") + names[rng.Next(names.Length)],
                weight = weight,
                reward = reward,
                rewardRuleVersion = rules != null ? rules.revision : 0,
                quotedRouteSteps = steps,
                minCredit = premium ? PremiumCredit : 0,
                hours = dist * 9 + 8,
            };
        }

        // 새 밸런스는 게시판에 남은 제안만 갱신. 기수락 보수·팁·무게·기한은 그대로 둔다.
        void RepriceUnaccepted(TownData from, Contract offer)
        {
            var rules = DeliveryReward.Rules(from);
            if (offer.atonement || rules == null || offer.rewardRuleVersion == rules.revision || player.contracts.Exists(c => c.id == offer.id)) return;
            var to = Town(offer.toTown);
            if (to == null) return;
            int steps = DeliveryReward.RouteSteps(world, from.id, to.id);
            if (steps < 0) return;
            // 구 제안의 랜덤 계수는 저장돼 있지 않다. 중간값으로 한 번 갱신해 재로드 재추첨을 막는다.
            offer.reward = DeliveryReward.Quote(rules, Distance(from, to), steps, offer.weight, offer.Premium);
            offer.quotedRouteSteps = steps;
            offer.rewardRuleVersion = rules.revision;
        }

        public void OnHour()
        {
            float now = clock.TotalHours;
            foreach (var b in boards)
            {
                if (b.list.FindAll(c => c != null && !c.atonement).Count < BoardSize && now - b.lastAdded >= RefillHours)
                {
                    var t = Town(b.town);
                    if (t != null)
                    {
                        var offer = Generate(t);
                        if (offer != null) b.list.Add(offer);
                        EnsureBasic(b, t);
                    }
                    b.lastAdded = now;
                }
                EnsureAtonement(b, Town(b.town));
            }
            // 최종 기한이 지난 의뢰는 실패
            foreach (var c in new List<Contract>(player.contracts))
            {
                if (now > c.finalDeadline)
                {
                    player.contracts.Remove(c);
                    player.credit -= 1;
                    Sfx.Play("fail");
                    if (c.atonement)
                    {
                        // 속죄 의뢰 실패: 보증금만 잃는다. 평판은 더 떨어지지 않는다.
                        CloseAtonement(c);
                        Notify.Show($"속죄 의뢰 실패: {c.cargo} (기한 지남) · 보증금 {c.deposit}냥 잃음 · 신용 −1", 5f);
                    }
                    else
                    {
                        Notify.Show($"의뢰 실패: {c.cargo} (기한 지남) · 신용 −1", 5f);
                        ReputationAwards.ForTown(Town(c.fromTown), ReputationSize.Medium, -1, "contract:expired");
                    }
                    GameEvents.RaiseContractFinished(c, false);
                }
            }
        }

        public string CanAccept(Contract c)
        {
            if (c == null) return "의뢰가 없습니다.";
            if (!CanOffer(Town(c.fromTown), Town(c.toTown))) return "현재 지역에서 도달 가능한 목적지의 의뢰만 맡을 수 있습니다.";
            if (player.contracts.Exists(active => active.id == c.id)) return "이미 맡은 의뢰입니다.";
            if (player.contracts.Count >= MaxActive) return $"의뢰는 한 번에 {MaxActive}개까지 맡을 수 있습니다.";
            if (c.Premium && player.credit < c.minCredit) return $"신용 {c.minCredit} 이상이어야 맡길 수 있다고 합니다.";
            if (c.atonement)
            {
                if (!StandingRecovery.Negative(player.reputation, (ReputationScope)c.atonementScope, c.atonementTarget)) return "평판이 이미 중립 이상입니다.";
                if (player.money < c.deposit) return $"보증금 {c.deposit}냥이 필요합니다.";
            }
            if (!player.CanCarryWeight(c.weight)) return "짐이 너무 많아 더 실을 수 없습니다.";
            return null;
        }

        public bool Accept(string town, Contract c)
        {
            var b = BoardOf(town);
            if (b == null || c == null || c.fromTown != town || !b.list.Contains(c) || CanAccept(c) != null) return false;
            b.list.Remove(c);
            b.lastAdded = clock.TotalHours;   // 빈자리는 시간이 지나야 채워짐
            float now = clock.TotalHours;
            c.acceptedAt = now;
            c.deadline = now + c.hours;
            c.finalDeadline = now + c.hours * 1.5f;
            c.condition = 1f;
            if (c.atonement) player.money -= c.deposit;
            player.contracts.Add(c);
            Sfx.Play("accept");
            Notify.Show($"의뢰를 맡았다: {c.cargo} → {TownName(c.toTown)}");
            return true;
        }

        public string TownName(string id) => Town(id)?.displayName ?? id;

        /// <summary>
        /// 납품. 기한 안·상태 90% 이상 = 성공(보수 전액, 기한의 70% 안이면 팁 15%, 신용 +1).
        /// 상태 30% 이상이고 최종 기한 안 = 부분 성공(상태만큼, 늦으면 60%). 그 밖은 실패(신용 −1).
        /// </summary>
        public ContractResult Deliver(Contract c, string atTown)
        {
            if (c == null || !player.contracts.Contains(c))
                return new ContractResult { message = "이미 처리했거나 맡지 않은 의뢰입니다." };
            if (c.toTown != atTown)
                return new ContractResult { message = "도착지 게시판에서 납품해 주세요." };
            if (c.dropped)
                return new ContractResult { message = "떨어뜨린 의뢰 짐을 먼저 회수해 주세요." };
            if (c.atonement && (long)player.money + c.deposit > int.MaxValue)
                return new ContractResult { message = "보증금을 돌려받을 잔고 공간이 부족합니다. 거래 후 다시 납품해 주세요." };
            float now = clock.TotalHours;
            var r = new ContractResult();
            player.contracts.Remove(c);
            if (now <= c.deadline && c.condition >= 0.9f)
            {
                bool fast = now - c.acceptedAt <= c.Window * 0.7f;
                r.paid = Mathf.RoundToInt(c.reward * (fast ? 1.15f : 1f));
                r.credit = 1;
                r.success = true;
                r.message = fast ? $"빨리 왔구먼! 수고비에 웃돈을 얹었네. (+{r.paid}, 신용 +1)" : $"틀림없이 받았네. (+{r.paid}, 신용 +1)";
            }
            else if (now <= c.finalDeadline && c.condition >= 0.3f)
            {
                float k = c.condition * (now > c.deadline ? 0.6f : 1f);
                r.paid = Mathf.RoundToInt(c.reward * k);
                r.success = true;
                string why = now > c.deadline ? "늦었고" : "";
                if (c.condition < 0.9f) why += (why != "" ? " " : "") + "짐이 상했으니";
                r.message = $"{why} 다 줄 수는 없네. (+{r.paid})";
            }
            else
            {
                r.credit = -1;
                r.message = "이걸 어떻게 받나. 없던 일로 하세. (신용 −1)";
            }
            if (c.atonement) FinishAtonement(c, ref r);
            else
            {
                ReputationAwards.ForTown(Town(c.fromTown), r.credit == 0 ? ReputationSize.VerySmall : ReputationSize.Medium,
                    r.credit > 0 ? 1 : -1, r.credit > 0 ? "contract:success" : r.success ? "contract:partial" : "contract:failed");
            }
            player.money += r.paid;
            player.credit += r.credit;
            Sfx.Play(r.success ? "success" : "fail");
            GameEvents.RaiseContractFinished(c, r.success);
            return r;
        }

        /// <summary>도적에게 의뢰 짐을 빼앗김: 실패, 보수의 20%를 물어 줌</summary>
        public void Lose(Contract c)
        {
            if (c == null || !player.contracts.Remove(c)) return;
            int penalty = Mathf.Min(player.money, Mathf.RoundToInt(c.reward * 0.2f));
            player.money -= penalty;
            player.credit -= 1;
            if (c.atonement)
            {
                CloseAtonement(c);
                Notify.Show($"속죄 의뢰 짐을 빼앗겼다: {c.cargo} · 보증금 {c.deposit}냥 잃음 · 신용 −1", 5f);
            }
            else
            {
                Notify.Show($"의뢰 짐을 빼앗겼다: {c.cargo} · 배상 {penalty} · 신용 −1", 5f);
                ReputationAwards.ForTown(Town(c.fromTown), ReputationSize.Small, -1, "contract:lost");
            }
            GameEvents.RaiseContractFinished(c, false);
        }

        // ---------- R4 속죄 의뢰 ----------
        static readonly string[] AtonementCargo = { "구호 물자", "급한 약재", "모자란 곡식" };

        bool AtonementOpen(int scope, string target) =>
            boards.Exists(b => b.list.Exists(c => c != null && c.atonement && c.atonementScope == scope && c.atonementTarget == target))
            || player.contracts.Exists(c => c.atonement && c.atonementScope == scope && c.atonementTarget == target);

        /// <summary>평판 단위가 마이너스이고 재개방 시간이 지났으면 그 단위에 하나만 연다. 중립 이상이 되면 게시만 거둔다.</summary>
        void EnsureAtonement(Board b, TownData town)
        {
            if (b == null || town == null || town.region == null) return;
            var scope = ReputationPricing.Scope(town); string target = ReputationPricing.Target(town);
            if (!StandingRecovery.Negative(player.reputation, scope, target))
            {
                b.list.RemoveAll(c => c != null && c.atonement && !player.contracts.Contains(c));
                return;
            }
            if (AtonementOpen((int)scope, target) || clock.TotalHours < player.tradeActivity.AtonementReopensAt((int)scope, target)) return;
            TownData to = null; int best = -1;
            foreach (var other in db.towns)
            {
                if (!CanOffer(town, other)) continue;
                int steps = Mathf.Max(1, DeliveryReward.RouteSteps(world, town.id, other.id));
                if (steps > best) { best = steps; to = other; }
            }
            if (to == null) return;
            b.list.Add(new Contract
            {
                id = nextId++, fromTown = town.id, toTown = to.id,
                cargo = AtonementCargo[rng.Next(AtonementCargo.Length)], weight = 3, reward = 0, quotedRouteSteps = best,
                minCredit = 0, hours = StandingRecovery.AtonementHours(best),
                atonement = true, deposit = StandingRecovery.AtonementDeposit(best),
                atonementScope = (int)scope, atonementTarget = target,
            });
        }

        void CloseAtonement(Contract c) =>
            player.tradeActivity.SetAtonementReopen(c.atonementScope, c.atonementTarget, clock.TotalHours + StandingRecovery.AtonementReopenHours);

        /// <summary>성공·부분 성공은 보증금을 돌려주고 평판을 올린다(중립까지). 실패는 보증금만 잃는다.</summary>
        void FinishAtonement(Contract c, ref ContractResult r)
        {
            CloseAtonement(c);
            var scope = (ReputationScope)c.atonementScope;
            if (!r.success) { r.message = $"이걸로는 안 되겠네. (보증금 {c.deposit}냥 잃음, 신용 −1)"; return; }
            int gained = StandingRecovery.Raise(player.reputation, scope, c.atonementTarget,
                ReputationAwards.PointsFor(r.credit > 0 ? StandingRecovery.AtonementSize : StandingRecovery.AtonementPartialSize));
            r.paid += c.deposit;
            r.message = $"덕분에 한시름 놓았네. 보증금 {c.deposit}냥을 돌려받았다. (평판 +{gained}{(r.credit > 0 ? ", 신용 +1" : "")})";
        }

        public string TimeLeftText(Contract c)
        {
            float left = c.deadline - clock.TotalHours;
            if (left >= 0) return $"{Mathf.CeilToInt(left)}시간";
            float late = c.finalDeadline - clock.TotalHours;
            return late >= 0 ? $"늦음 (마감 {Mathf.CeilToInt(late)}시간)" : "마감";
        }
    }
}
