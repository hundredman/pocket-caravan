using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 공유 장터 (2026-10-30, feature/online 8단계). 지역마다 장터 하나씩, 온라인으로 사고판 것이 모두의 재고·값에 같이 반영된다.
    /// - 내가 공유 장터에서 사고팔면(GameEvents.ItemBought · ItemSold) 재고 변화(d)를 0.5초씩 모아 서버에 보낸다.
    ///   살 때 -1개, 팔 때는 장터가 실제로 늘린 만큼(MarketState.SellStockDelta, 출신 강점 포함).
    /// - 남의 거래를 받으면 내 장터 재고에 같은 변화를 더한다(MarketState.Export → 고쳐서 Import). 값은 재고로 정해지니 바로 바뀐다.
    ///   재고 회복(매시간)은 각자 게임 시계대로 일어난다. 장터 등급·거래액은 나누지 않는다.
    /// - 접속하지 않은 동안의 거래: 다시 접속하면 서버가 30분 안의 놓친 거래를 보내 준다. 받은 마지막 번호는 서버 주소마다 기억한다.
    /// 공용 코드(MarketState · GameEvents · GameManager)는 공개된 것만 쓰고 고치지 않는다.
    /// </summary>
    public class NetMarket : MonoBehaviour
    {
        /// <summary>공유 장터: 베네치아 리알토 · 고려 개경 · 북유럽 비르카 · 아라비아 제다 (제안, 2026-10-30)</summary>
        public static readonly string[] SharedTowns = { "venice_rialto", "gaegyeong", "north_birka", "arabia_jeddah" };
        const float FlushSeconds = 0.5f;

        [System.Serializable] class TradeOut { public string t = "trade"; public string town, item; public int n; public float d; public bool sell; }
        class Pending { public string town, item; public int n; public float d; public bool sell; }

        readonly Dictionary<string, Pending> pending = new Dictionary<string, Pending>();
        float nextFlush;
        bool toldTowns;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetMarket>() != null) return;
            var go = new GameObject("NetMarket");
            DontDestroyOnLoad(go);
            go.AddComponent<NetMarket>();
        }

        public static bool IsShared(TownData town) => town != null && System.Array.IndexOf(SharedTowns, town.id) >= 0;

        // ---------- 받은 마지막 거래 번호 (서버 주소마다) ----------

        static string SeqKey(string address) => "net.tradeSeq:" + (address ?? "");
        /// <summary>이 서버에서 마지막으로 받은 거래 번호. 처음이면 -1 (서버가 30분 안의 거래를 보내 준다)</summary>
        public static int Since(string address) => PlayerPrefs.GetInt(SeqKey(address), -1);
        static void Saw(int seq)
        {
            var net = NetClient.I;
            if (net == null || seq <= Since(net.Address)) return;
            PlayerPrefs.SetInt(SeqKey(net.Address), seq);
        }

        void OnEnable()
        {
            GameEvents.ItemBought += OnBought;
            GameEvents.ItemSold += OnSold;
            NetClient.Received += OnReceived;
        }

        void OnDisable()
        {
            GameEvents.ItemBought -= OnBought;
            GameEvents.ItemSold -= OnSold;
            NetClient.Received -= OnReceived;
        }

        static bool Online => NetClient.I != null && NetClient.I.Status == NetClient.State.Online;

        // ---------- 내 거래 → 서버 ----------

        void OnBought(ItemData item, TownData town, int price) => Queue(item, town, false, -1f);

        void OnSold(ItemData item, TownData town, int price, int profit)
        {
            if (!IsShared(town) || GameManager.I == null) return;
            Queue(item, town, true, GameManager.I.GetMarket(town).SellStockDelta(item));
        }

        void Queue(ItemData item, TownData town, bool sell, float d)
        {
            if (!Online || item == null || !IsShared(town)) return;
            string k = town.id + "|" + item.id + "|" + (sell ? "s" : "b");
            if (!pending.TryGetValue(k, out var p)) pending[k] = p = new Pending { town = town.id, item = item.id, sell = sell };
            p.n++;
            p.d += d;
            if (nextFlush <= Time.unscaledTime) nextFlush = Time.unscaledTime + FlushSeconds;
        }

        void Update()
        {
            if (pending.Count == 0 || Time.unscaledTime < nextFlush) return;
            if (Online)
                foreach (var p in pending.Values)
                    NetClient.I.Send(new TradeOut { town = p.town, item = p.item, n = p.n, d = p.d, sell = p.sell });
            pending.Clear();
        }

        // ---------- 남의 거래 → 내 장터 ----------

        void OnReceived(NetClient.Msg m)
        {
            switch (m.t)
            {
                case "welcome":
                    Saw(m.seq);
                    if (!toldTowns)
                    {
                        toldTowns = true;
                        NetChat.System($"공유 장터: {TownList()} · 여기서 사고팔면 접속한 모두의 값이 같이 바뀌어요");
                    }
                    break;
                case "trade":
                    Saw(m.seq);
                    if (m.id == NetClient.I.MyId) break;
                    if (Apply(m.town, m.item, m.d, out var town, out var item))
                        NetChat.System($"{m.name} 님이 {town.displayName}에서 {Josa.Eul(item.displayName)} {m.n}개 {(m.sell ? "팔았어요 (값 ↓)" : "샀어요 (값 ↑)")}");
                    break;
                case "trades":
                    if (m.list == null) break;
                    int applied = 0;
                    foreach (var r in m.list)
                    {
                        Saw(r.seq);
                        if (r.id != NetClient.I.MyId && Apply(r.town, r.item, r.d, out _, out _)) applied++;
                    }
                    if (applied > 0) NetChat.System($"접속하지 않은 동안 다른 사람 거래 {applied}건을 공유 장터에 반영했어요");
                    break;
            }
        }

        /// <summary>장터 재고에 변화를 더한다. 모르는 마을·물건이면 false</summary>
        static bool Apply(string townId, string itemId, float d, out TownData town, out ItemData item)
        {
            town = null; item = null;
            var gm = GameManager.I;
            if (gm == null || gm.Db == null) return false;
            town = gm.Db.towns.Find(t => t != null && t.id == townId);
            item = gm.ItemById(itemId);
            if (town == null || item == null || System.Array.IndexOf(SharedTowns, townId) < 0) return false;

            var market = gm.GetMarket(town);
            market.Stock(item);   // 처음 보는 물건이면 적정 재고로 만들어 둔다
            var save = market.Export();
            int i = save.items.IndexOf(item.id);
            if (i < 0) return false;
            save.stocks[i] = Mathf.Max(0f, save.stocks[i] + d);
            market.Import(save, gm.ItemById);
            return true;
        }

        static string TownList()
        {
            var gm = GameManager.I;
            var names = new List<string>();
            foreach (var id in SharedTowns)
            {
                var t = gm != null && gm.Db != null ? gm.Db.towns.Find(x => x != null && x.id == id) : null;
                names.Add(t != null ? t.displayName : id);
            }
            return string.Join(" · ", names);
        }
    }
}
