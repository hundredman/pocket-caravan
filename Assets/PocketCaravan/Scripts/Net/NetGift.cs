using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 선물 (2026-11-07, 서버 11단계). 접속해 있는 사람에게 돈이나 짐을 보낸다.
    /// 채팅에 "/선물 은샘 비단 3" (개수를 빼면 1개) · "/선물 은샘 100냥". 물건 이름의 띄어쓰기는 상관없다.
    /// - 보낼 때 돈·짐을 먼저 빼 두고, 받는 쪽이 받았다는 답(ok)이 오면 끝이다.
    ///   못 받았거나(접속 안 함 · 짐이 꽉 참 · 그 게임에 없는 물건) 15초 안에 답이 없으면 돌려받는다. 서버는 10초에 끊으니 그 전에 답이 온다.
    /// - 받는 쪽은 저절로 받는다. 맡은 물건(questItem)은 보낼 수 없다.
    /// 공용 코드는 공개된 것만 쓴다 (PlayerState.money · cargo.Add/Remove/Count/AverageCost · GameManager.ItemById).
    /// </summary>
    public class NetGift : MonoBehaviour
    {
        const float Wait = 15f;
        const int MoneyMax = 1000000, CountMax = 1000;

        [Serializable] class GiftOut { public string t = "gift"; public string gid, to, item; public int n, money; public float cost; }
        [Serializable] class AckOut { public string t = "giftAck"; public string gid; public bool ok; public string why; }

        /// <summary>답을 기다리는 동안 맡아 둔 돈·짐</summary>
        class Held { public string gid, to, label; public ItemData item; public int n, money; public float cost, until; }

        static NetGift inst;
        readonly List<Held> held = new List<Held>();
        int nextGid;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetGift>() != null) return;
            var go = new GameObject("NetGift");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetGift>();
        }

        void Awake() => inst = this;
        void OnEnable() => NetClient.Received += OnReceived;
        void OnDisable() => NetClient.Received -= OnReceived;

        static PlayerState Player => GameManager.I != null ? GameManager.I.Player : null;
        static bool InGame => Player != null && MapRoot.Current != null && !StartScreen.IsOpen && !NetLogin.IsOpen;

        /// <summary>채팅 "/선물 …"이면 처리하고 true (NetChat이 부른다)</summary>
        public static bool TryCommand(string text)
        {
            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || (parts[0] != "선물" && parts[0].ToLowerInvariant() != "gift")) return false;
            if (inst == null) return true;
            if (parts.Length < 3) { NetChat.System("선물: /선물 이름 물건 개수 · /선물 이름 100냥"); return true; }
            inst.Send(parts);
            return true;
        }

        void Send(string[] parts)
        {
            var net = NetClient.I;
            var p = Player;
            string to = parts[1];
            if (net == null || net.Status != NetClient.State.Online) { NetChat.System("온라인일 때만 선물할 수 있어요"); return; }
            if (!net.ServerExtras) { NetChat.System("지금 서버는 아직 선물을 몰라요 (서버를 새로 올리면 쓸 수 있어요)"); return; }
            if (!InGame) { NetChat.System("게임을 시작한 뒤에 선물할 수 있어요"); return; }
            if (string.Equals(to, net.MyName, StringComparison.OrdinalIgnoreCase)) { NetChat.System("나에게는 선물할 수 없어요"); return; }

            var h = new Held { gid = "g" + (++nextGid) + UnityEngine.Random.Range(1000, 10000), to = to, until = Time.unscaledTime + Wait };
            string last = parts[parts.Length - 1];
            if (parts.Length == 3 && last.EndsWith("냥") && int.TryParse(last.Substring(0, last.Length - 1), out int money))
            {
                if (money < 1 || money > MoneyMax) { NetChat.System($"돈은 1냥부터 {MoneyMax}냥까지 보낼 수 있어요"); return; }
                if (p.money < money) { NetChat.System($"돈이 모자라요 (가진 돈 {p.money}냥)"); return; }
                p.money -= money;
                h.money = money;
                h.label = $"{money}냥";
            }
            else
            {
                int n = 1, end = parts.Length;
                if (parts.Length > 3 && int.TryParse(last, out int c)) { n = c; end--; }
                string asked = string.Join(" ", parts, 2, end - 2);
                var item = FindCargo(p, asked);
                if (item == null) { NetChat.System($"짐에 '{asked}'이(가) 없어요 (맡은 물건은 보낼 수 없어요)"); return; }
                if (n < 1 || n > CountMax) { NetChat.System($"한 번에 1~{CountMax}개 보낼 수 있어요"); return; }
                int have = p.cargo.Count(item);
                if (have < n) { NetChat.System($"{Josa.Eun(item.displayName)} {have}개밖에 없어요"); return; }
                h.cost = p.cargo.AverageCost(item) * n;
                if (!p.cargo.Remove(item, n)) { NetChat.System("짐에서 꺼내지 못했어요"); return; }
                h.item = item;
                h.n = n;
                h.label = $"{item.displayName} {n}개";
            }
            held.Add(h);
            net.Send(new GiftOut { gid = h.gid, to = to, item = h.item != null ? h.item.id : "", n = h.n, money = h.money, cost = Mathf.Round(h.cost * 100f) / 100f });
            NetChat.System($"{to} 님에게 {h.label} 보내는 중…");
        }

        /// <summary>짐에서 이름(띄어쓰기 무시)이나 아이디가 같은 교역품</summary>
        static ItemData FindCargo(PlayerState p, string name)
        {
            string want = name.Replace(" ", "").ToLowerInvariant();
            foreach (var kv in p.cargo.Items)
            {
                var it = kv.Key;
                if (it == null || it.questItem || kv.Value <= 0) continue;
                if (it.displayName.Replace(" ", "").ToLowerInvariant() == want || it.id.ToLowerInvariant() == want) return it;
            }
            return null;
        }

        void Refund(Held h)
        {
            var p = Player;
            if (p == null) return;
            if (h.money > 0) p.money += h.money;
            if (h.item != null && h.n > 0) p.cargo.Add(h.item, h.n, h.cost / h.n);
        }

        void OnReceived(NetClient.Msg m)
        {
            switch (m.t)
            {
                case "gift":
                    Receive(m);
                    break;
                case "giftResult":
                    var h = held.Find(x => x.gid == m.gid);
                    if (h == null) break;
                    held.Remove(h);
                    string who = string.IsNullOrEmpty(m.to) ? h.to : m.to;
                    if (m.ok)
                    {
                        NetChat.System($"{who} 님에게 {Josa.Eul(h.label)} 보냈어요");
                        Notify.Show($"{who} 님에게 {Josa.Eul(h.label)} 선물했어요", 3f);
                        Sfx.Play("coin");
                    }
                    else
                    {
                        Refund(h);
                        NetChat.System($"선물을 못 보냈어요: {m.why} (돌려받았어요)");
                        Sfx.Play("cancel");
                    }
                    break;
            }
        }

        /// <summary>받는 쪽: 저절로 받고 답한다</summary>
        void Receive(NetClient.Msg m)
        {
            var net = NetClient.I;
            var p = Player;
            string why = null;
            ItemData item = null;
            if (!InGame) why = "아직 게임을 시작하지 않았어요";
            else if (m.money <= 0)
            {
                item = GameManager.I.ItemById(m.item);
                if (item == null || item.questItem) why = "받는 사람 게임에 없는 물건이에요";
                else if (m.n < 1) why = "선물 모양이 이상해요";
                else if (!p.CanCarry(item, m.n)) why = "받는 사람 짐이 꽉 찼어요";
            }
            if (why == null)
            {
                string label;
                if (m.money > 0) { p.money = (int)Math.Min(int.MaxValue, (long)p.money + m.money); label = $"{m.money}냥"; }
                else { p.cargo.Add(item, m.n, m.cost / m.n); label = $"{item.displayName} {m.n}개"; }
                Notify.Show($"{m.from} 님이 {Josa.Eul(label)} 선물했어요!", 4f);
                NetChat.System($"{m.from} 님에게서 {Josa.Eul(label)} 받았어요");
                Sfx.Play("levelup", .6f);
            }
            if (net != null) net.Send(new AckOut { gid = m.gid, ok = why == null, why = why ?? "" });
        }

        void Update()
        {
            for (int i = held.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < held[i].until) continue;
                var h = held[i];
                held.RemoveAt(i);
                Refund(h);
                NetChat.System($"{h.to} 님에게 보낸 선물이 답이 없어서 돌려받았어요");
            }
        }
    }
}
