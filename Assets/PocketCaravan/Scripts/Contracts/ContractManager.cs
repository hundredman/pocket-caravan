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
        /// <summary>11/19 신용 2단계부터 한 번에 4개 (Credit.PerkText). 맡은 일이 늘어나는 게 신용의 쓰임</summary>
        public static int MaxActiveFor(PlayerState p) => Credit.Level(p) >= 2 ? MaxActive + 1 : MaxActive;
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
                for (int i = 0; i < BoardSizeFor(); i++)
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
            // 11/19 아직 못 맡는 귀한 짐(신용 3단계 전)은 게시판에 미리 보기로 하나만. 나머지 자리는 맡을 수 있는 의뢰로 (열린 것은 실제로 쓸 수 있게)
            if (Credit.Level(player) < PremiumCredit)
            {
                bool seen = false;
                for (int i = 0; i < b.list.Count; i++)
                {
                    var c = b.list[i];
                    if (c == null || !c.Premium || player.contracts.Contains(c)) continue;
                    if (!seen) { seen = true; continue; }
                    var basicOffer = Generate(town, true);
                    if (basicOffer != null) b.list[i] = basicOffer;
                }
            }
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

        /// <summary>
        /// 11/19 게시판 의뢰를 기한 안에 온전히 납품했을 때 신용 점수 (Core/Credit.cs): 기본 1 · 먼 마을(거리 2 이상) +1 · 귀한 짐 +1 · 빨리 옴(기한의 70% 안) +1.
        /// 덜 받은 납품 0 · 실패 −1은 그대로.
        /// </summary>
        public int CreditPoints(Contract c, bool fast)
        {
            if (c == null) return 0;
            int pts = 1;
            var a = Town(c.fromTown); var b = Town(c.toTown);
            if (a != null && b != null && Distance(a, b) >= 2) pts++;
            if (c.Premium) pts++;
            if (c.kind != ContractKind.Normal) pts++;   // 급한 짐·깨지기 쉬운 짐·큰 주문
            if (fast) pts++;
            return pts;
        }

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
            bool premium = forceKind == ForcePremium || (forceKind < 0 && !basic && rng.NextDouble() < 0.25);
            int kind = premium || basic ? ContractKind.Normal : PickKind(forceKind);
            forceKind = -1;
            var rules = DeliveryReward.Rules(from);
            if (rules != null && premium)
                weight = rng.Next(Mathf.Max(1, rules.premiumMinWeight), Mathf.Max(rules.premiumMinWeight, rules.premiumMaxWeight) + 1);
            if (kind == ContractKind.Bulk) weight = rng.Next(10, 17);
            // 11/19 해금된 의뢰는 실제로 맡을 수 있게: 귀한 짐·큰 주문이 지금 짐 한도(빈 몸 기준)를 넘지 않게 줄인다 (귀한 짐 8, 큰 주문 10 밑으로는 안 줄임)
            if (premium || kind == ContractKind.Bulk) weight = Mathf.Min(weight, Mathf.Max(premium ? 8f : 10f, Mathf.Floor(player.Capacity)));
            else if (kind == ContractKind.Fragile) weight = rng.Next(2, 5);
            int steps = rules != null ? DeliveryReward.RouteSteps(world, from.id, to.id) : 0;
            int reward = DeliveryReward.Quote(rules, dist, steps, weight, premium, 0.85f + (float)rng.NextDouble() * 0.3f);
            reward = Mathf.Max(1, Mathf.RoundToInt(reward * KindPay(kind)));
            string cargoName = kind == ContractKind.Fragile ? FragileName(from) : kind == ContractKind.Bulk ? BulkName(from) : names[rng.Next(names.Length)];
            return new Contract
            {
                id = nextId++,
                kind = kind,
                fromTown = from.id,
                toTown = to.id,
                cargo = (premium ? "귀한 " : "") + cargoName,
                weight = weight,
                reward = reward,
                rewardRuleVersion = rules != null ? rules.revision : 0,
                quotedRouteSteps = steps,
                minCredit = premium ? PremiumCredit : 0,
                hours = KindHours(kind, dist),
            };
        }

        // ---------------- 11/19 의뢰 종류 ----------------

        int forceKind = -1;   // 다음 Generate가 이 종류로 (새 단계에 닿았을 때 게시판에 꼭 하나 나오게)
        const int ForcePremium = 99;   // 3단계: 귀한 짐

        /// <summary>지금 신용 단계로 열린 종류 중 하나. 보통 4 : 새 종류 각 2. 아무것도 안 열렸으면 rng를 쓰지 않는다 (옛 시험의 의뢰 순서 유지)</summary>
        int PickKind(int force)
        {
            int lv = Credit.Level(player);
            if (force >= 0 && ContractKind.LevelOf(force) <= lv) return force;
            var open = new List<int>();
            foreach (int k in new[] { ContractKind.Express, ContractKind.Fragile, ContractKind.Bulk })
                if (ContractKind.LevelOf(k) <= lv) open.Add(k);
            if (open.Count == 0) return ContractKind.Normal;
            int roll = rng.Next(4 + open.Count * 2);
            return roll < 4 ? ContractKind.Normal : open[(roll - 4) / 2];
        }

        static float KindPay(int kind) => kind == ContractKind.Express ? 1.35f : kind == ContractKind.Fragile ? 1.25f : kind == ContractKind.Bulk ? 1.6f : 1f;
        static float KindHours(int kind, int dist) =>
            kind == ContractKind.Express ? Mathf.Round((dist * 9 + 8) * 0.6f) : kind == ContractKind.Bulk ? dist * 12 + 12 : dist * 9 + 8;

        static string BulkName(TownData from)
        {
            string r = from != null && from.region != null ? from.region.id : "";
            if (r == "goryeo") return "곡식 섬 더미";
            if (r == "venice" || r == "center_venice") return "소금 가마니 더미";
            if (r == "arabia") return "대추야자 자루 더미";
            if (r == "north" || r == "center_north") return "말린 대구 더미";
            return "장터 물건 더미";
        }

        static string FragileName(TownData from)
        {
            string r = from != null && from.region != null ? from.region.id : "";
            if (r == "goryeo") return "청자 그릇 상자";
            if (r == "venice" || r == "center_venice") return "유리그릇 상자";
            if (r == "arabia") return "향유 병 상자";
            if (r == "north" || r == "center_north") return "꿀술 단지";
            return "옹기 항아리";
        }

        /// <summary>게시판 크기: 신용 2단계부터 4개 (한 번에 4개 맡을 수 있으니 실제로 고를 만큼 나와야 한다), 4단계부터 5개</summary>
        public int BoardSizeFor() => BoardSize + (Credit.Level(player) >= 2 ? 1 : 0) + (Credit.Level(player) >= 4 ? 1 : 0);

        /// <summary>
        /// 신용 단계가 올랐을 때: 모든 게시판을 새 크기로 채우고, 이번 단계에 열린 종류가 게시판마다 하나는 있게 한다.
        /// 맡은 의뢰는 건드리지 않는다. GameManager가 단계 오름 알림과 함께 부른다.
        /// </summary>
        public void OnCreditLevel(int level)
        {
            int unlocked = level == 1 ? ContractKind.Express : level == 2 ? ContractKind.Fragile : level == PremiumCredit ? ForcePremium : level == 4 ? ContractKind.Bulk : -1;
            foreach (var b in boards)
            {
                var t = Town(b.town);
                if (t == null) continue;
                if (unlocked >= 0 && !b.list.Exists(c => c != null && (unlocked == ForcePremium ? c.Premium : c.kind == unlocked)))
                {
                    int i = b.list.FindIndex(c => c != null && !c.atonement && !c.Premium && c.kind == ContractKind.Normal);
                    forceKind = unlocked;
                    var offer = Generate(t);
                    forceKind = -1;
                    if (offer != null) { if (i >= 0 && b.list.FindAll(c => c != null && !c.atonement).Count >= BoardSizeFor()) b.list[i] = offer; else b.list.Add(offer); }
                }
                while (b.list.FindAll(c => c != null && !c.atonement).Count < BoardSizeFor())
                {
                    var offer = Generate(t);
                    if (offer == null) break;
                    b.list.Add(offer);
                }
                EnsureBasic(b, t);
            }
        }

        /// <summary>
        /// 깨지기 쉬운 짐: 달리면 초당 1% 상한다 (PlayerController가 부른다). 걸으면 안 상한다 — 규칙을 하나로 두어 처음 하는 사람도 알기 쉽게.
        /// 10초 넘게 달리면 90% 아래(덜 받음)로 떨어진다. 그때 한 번 알린다.
        /// </summary>
        public void TickFragile(float dt, bool moving, bool running)
        {
            if (!moving || !running || dt <= 0f) return;
            const float loss = 0.01f;
            foreach (var c in player.contracts)
            {
                if (c == null || !c.Fragile || c.dropped) continue;
                float before = c.condition;
                c.condition = Mathf.Max(0f, c.condition - loss * dt);
                if (before >= 0.9f && c.condition < 0.9f)
                    Notify.Show($"{c.cargo}이(가) 흔들려 상했다! 깨지기 쉬운 짐은 걸어서 날라요.", 4f);
            }
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
            float now = clock.TotalHours;      // 게시판 채우기: 세상 시간
            float played = clock.PlayHours;    // 의뢰 기한: 내 의뢰 시간
            foreach (var b in boards)
            {
                if (b.list.FindAll(c => c != null && !c.atonement).Count < BoardSizeFor() && now - b.lastAdded >= RefillHours)
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
                if (played > c.finalDeadline)
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
            if (player.contracts.Count >= MaxActiveFor(player)) return $"의뢰는 한 번에 {MaxActiveFor(player)}개까지 맡을 수 있습니다.";
            if (c.Premium && Credit.Level(player) < c.minCredit) return $"신용 {c.minCredit} 이상이어야 맡길 수 있다고 합니다.";
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
            float now = clock.PlayHours;      // 기한은 내 의뢰 시간으로 잰다
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
            float now = clock.PlayHours;
            var r = new ContractResult();
            player.contracts.Remove(c);
            if (now <= c.deadline && c.condition >= 0.9f)
            {
                bool fast = now - c.acceptedAt <= c.Window * 0.7f;
                r.paid = Mathf.RoundToInt(c.reward * (fast ? 1.15f : 1f));
                r.credit = CreditPoints(c, fast);
                r.success = true;
                r.message = fast ? $"빨리 왔구먼! 수고비에 웃돈을 얹었네. (+{r.paid}, 신용 +{r.credit})" : $"틀림없이 받았네. (+{r.paid}, 신용 +{r.credit})";
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
            if (r.credit > 0) r.credit = 1;   // 11/19 속죄 의뢰는 평판을 되찾는 일이라 신용은 1점만 (게시판 점수 규칙은 쓰지 않는다)
            int gained = StandingRecovery.Raise(player.reputation, scope, c.atonementTarget,
                ReputationAwards.PointsFor(r.credit > 0 ? StandingRecovery.AtonementSize : StandingRecovery.AtonementPartialSize));
            r.paid += c.deposit;
            r.message = $"덕분에 한시름 놓았네. 보증금 {c.deposit}냥을 돌려받았다. (평판 +{gained}{(r.credit > 0 ? ", 신용 +1" : "")})";
        }

        public string TimeLeftText(Contract c)
        {
            float left = c.deadline - clock.PlayHours;
            if (left >= 0) return $"{Mathf.CeilToInt(left)}시간";
            float late = c.finalDeadline - clock.PlayHours;
            return late >= 0 ? $"늦음 (마감 {Mathf.CeilToInt(late)}시간)" : "마감";
        }
    }
}
