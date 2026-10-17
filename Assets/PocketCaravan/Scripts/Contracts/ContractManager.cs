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
        readonly PlayerState player;
        public List<Board> boards = new List<Board>();
        public int nextId = 1;
        readonly System.Random rng = new System.Random();

        static readonly Dictionary<string, string[]> CargoNames = new Dictionary<string, string[]>
        {
            ["naru"] = new[] { "소금 자루", "건어물 상자", "뱃사람 편지", "곡식 섬" },
            ["gaegyeong"] = new[] { "인삼 상자", "비단 꾸러미", "약재 보따리", "관청 문서" },
            ["dakmok"] = new[] { "한지 묶음", "닥나무 껍질", "붓과 먹", "종이 상자" },
        };

        public ContractManager(GameDatabase db, GameClock clock, PlayerState player)
        {
            this.db = db; this.clock = clock; this.player = player;
        }

        public void InitBoards()
        {
            foreach (var t in db.towns)
            {
                if (t == null) continue;
                var existing = BoardOf(t.id);
                if (existing != null) { EnsureBasic(existing, t); continue; }
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
            if (b.list.Count == 0 || b.list.Exists(c => !c.Premium)) return;
            var basic = Generate(town, true);
            if (basic != null) b.list[b.list.Count - 1] = basic;
        }

        public Board BoardOf(string town) => boards.Find(b => b.town == town);
        public List<Contract> Available(string town) => BoardOf(town)?.list ?? new List<Contract>();

        TownData Town(string id) => db.towns.Find(t => t != null && t.id == id);

        public static int Distance(TownData a, TownData b)
        {
            var d = a.worldCoord - b.worldCoord;
            return Mathf.Abs(d.x) + Mathf.Abs(d.y);
        }

        Contract Generate(TownData from, bool basic = false)
        {
            var others = db.towns.FindAll(t => t != null && t != from);
            if (others.Count == 0) return null; // 새 지역의 첫 거점만 연결된 단계에도 부팅 가능.
            var to = others[rng.Next(others.Count)];
            int dist = Mathf.Max(1, Distance(from, to));
            var names = CargoNames.TryGetValue(from.id, out var n) ? n : new[] { "짐 꾸러미" };
            float weight = rng.Next(2, 7);
            bool premium = !basic && rng.NextDouble() < 0.25;
            float reward = 14 + 16 * dist + weight * 3;
            reward *= 0.85f + (float)rng.NextDouble() * 0.3f;
            if (premium) reward *= 1.8f;
            return new Contract
            {
                id = nextId++,
                fromTown = from.id,
                toTown = to.id,
                cargo = (premium ? "귀한 " : "") + names[rng.Next(names.Length)],
                weight = weight,
                reward = Mathf.RoundToInt(reward),
                minCredit = premium ? 3 : 0,
                hours = dist * 9 + 8,
            };
        }

        public void OnHour()
        {
            float now = clock.TotalHours;
            foreach (var b in boards)
            {
                if (b.list.Count < BoardSize && now - b.lastAdded >= RefillHours)
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
            }
            // 최종 기한이 지난 의뢰는 실패
            foreach (var c in new List<Contract>(player.contracts))
            {
                if (now > c.finalDeadline)
                {
                    player.contracts.Remove(c);
                    player.credit -= 1;
                    Notify.Show($"의뢰 실패: {c.cargo} (기한 지남) · 신용 −1", 5f);
                    Sfx.Play("fail");
                    GameEvents.RaiseContractFinished(c, false);
                }
            }
        }

        public string CanAccept(Contract c)
        {
            if (c == null) return "의뢰가 없습니다.";
            if (player.contracts.Exists(active => active.id == c.id)) return "이미 맡은 의뢰입니다.";
            if (player.contracts.Count >= MaxActive) return $"의뢰는 한 번에 {MaxActive}개까지 맡을 수 있습니다.";
            if (c.Premium && player.credit < c.minCredit) return $"신용 {c.minCredit} 이상이어야 맡길 수 있다고 합니다.";
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
            Notify.Show($"의뢰 짐을 빼앗겼다: {c.cargo} · 배상 {penalty} · 신용 −1", 5f);
            GameEvents.RaiseContractFinished(c, false);
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
