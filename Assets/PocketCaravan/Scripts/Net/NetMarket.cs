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
    /// - 접속하지 않은 동안의 거래 (11/08): 새 서버는 접속할 때 공유 장터 재고(적정 재고와의 차이)를 통째로 보내 준다.
    ///   그동안의 거래와 세상 시간에 따른 회복이 모두 들어 있어 아무리 오래 쉬어도 모두 같은 재고다.
    ///   예전 서버면 놓친 거래(24시간·300건까지)를 다시 받아 더한다. 받은 마지막 번호는 서버 주소마다 기억한다.
    /// - 혼자 하는 동안(접속 안 함·잠깐 끊김) 공유 장터에서 한 거래 (11/08 윤채): 진행 메모 "net:off:마을|물건"에 재고 변화를 모아 두고,
    ///   다음에 새 서버에 접속하면 서버 재고를 받은 뒤 내 장터에 다시 더하고 서버에도 올린다.
    ///   예전엔 접속할 때 서버 재고로 덮여 혼자 몰아 판 값 하락이 사라졌다 (끊었다 붙으면 또 비싸게 팔 수 있었다).
    /// 공용 코드(MarketState · GameEvents · GameManager)는 공개된 것만 쓰고 고치지 않는다.
    /// </summary>
    public class NetMarket : MonoBehaviour
    {
        /// <summary>공유 장터: 베네치아 리알토 · 고려 개경 · 북유럽 비르카 · 아라비아 제다 (제안, 2026-10-30)</summary>
        public static readonly string[] SharedTowns = { "venice_rialto", "gaegyeong", "north_birka", "arabia_jeddah" };
        const float FlushSeconds = 0.5f;
        /// <summary>혼자 한 거래를 올릴 때 0.5초에 보내는 수 (서버는 1초에 30개를 넘으면 버린다)</summary>
        const int OfflinePerFlush = 8;
        const string OfflinePrefix = "net:off:";

        [System.Serializable] class TradeOut { public string t = "trade"; public string town, item; public int n; public float d; public bool sell; }
        class Pending { public string town, item; public int n; public float d; public bool sell; }

        readonly Dictionary<string, Pending> pending = new Dictionary<string, Pending>();
        readonly Queue<TradeOut> offlineOut = new Queue<TradeOut>();
        float nextFlush;
        bool toldTowns;

        static string OfflineKey(string town, string item) => OfflinePrefix + town + "|" + item;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetMarket>() != null) return;
            var go = new GameObject("NetMarket");
            DontDestroyOnLoad(go);
            go.AddComponent<NetMarket>();
        }

        public static bool IsShared(TownData town) => town != null && System.Array.IndexOf(SharedTowns, town.id) >= 0;

        /// <summary>이번 접속에서 서버가 공유 장터 재고를 통째로 줬다 (11/08). 예전 서버면 false: 지난 거래를 다시 받아 더한다.</summary>
        public static bool ServerShared { get; private set; }
        /// <summary>
        /// 공유 장터 크기 배율 (11/08): 최근 1시간 접속한 사람이 많으면 서버가 키운다(1~3). 공유 장터의 적정 재고에 곱한다.
        /// 사람이 많아도 한 사람의 거래가 값에 주는 영향이 비슷하다. 혼자 하기·예전 서버는 1.
        /// </summary>
        public static float Scale { get; private set; } = 1f;
        public static float ScaleFor(TownData town) => IsShared(town) ? Scale : 1f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetShared() { ServerShared = false; Scale = 1f; }

        /// <summary>배율을 바꾼다. 적정 재고와의 차이는 그대로 두어 값이 갑자기 튀지 않게 한다.</summary>
        static void ChangeScale(float scale)
        {
            scale = Mathf.Clamp(float.IsNaN(scale) ? 1f : scale, 1f, 10f);
            if (Mathf.Approximately(scale, Scale)) return;
            var gm = GameManager.I;
            var shift = new List<(MarketState market, ItemData item, float dev)>();
            if (gm != null && gm.Db != null)
                foreach (var id in SharedTowns)
                {
                    var town = gm.Db.towns.Find(t => t != null && t.id == id);
                    if (town == null) continue;
                    var market = gm.GetMarket(town);
                    foreach (var it in market.KnownItems) shift.Add((market, it, market.Stock(it) - market.Optimal(it)));
                }
            Scale = scale;
            foreach (var (market, item, dev) in shift) market.SetStock(item, market.Optimal(item) + dev);
        }

        // ---------- 받은 마지막 거래 번호 (서버 주소마다) ----------

        static string SeqKey(string address) => "net.tradeSeq:" + (address ?? "");
        /// <summary>이 서버에서 마지막으로 받은 거래 번호. 처음이면 -1 (예전 서버가 놓친 거래를 보내 준다)</summary>
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
            if (item == null || !IsShared(town)) return;
            if (!Online)
            {
                // 혼자 하는 동안: 다음 접속 때 올리려고 모아 둔다 (저장에 들어가는 진행 메모)
                var gm = GameManager.I;
                if (gm != null) { string memo = OfflineKey(town.id, item.id); gm.SetMemo(memo, gm.GetMemo(memo) + d); }
                return;
            }
            string k = town.id + "|" + item.id + "|" + (sell ? "s" : "b");
            if (!pending.TryGetValue(k, out var p)) pending[k] = p = new Pending { town = town.id, item = item.id, sell = sell };
            p.n++;
            p.d += d;
            if (nextFlush <= Time.unscaledTime) nextFlush = Time.unscaledTime + FlushSeconds;
        }

        void Update()
        {
            // 끊겨서 혼자 하기로 돌아가면 공유 장터도 원래 크기로 (잠깐 끊겨 다시 붙는 중에는 그대로)
            if (ServerShared && !WorldClock.Active) { ChangeScale(1f); ServerShared = false; }
            if (offlineOut.Count > 0 && Time.unscaledTime >= nextFlush && Online)
            {
                for (int i = 0; i < OfflinePerFlush && offlineOut.Count > 0; i++) NetClient.I.Send(offlineOut.Dequeue());
                nextFlush = Time.unscaledTime + FlushSeconds;
            }
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
                    ServerShared = m.marketOk;
                    if (m.marketOk) { ChangeScale(m.marketScale > 0f ? m.marketScale : 1f); ApplySnapshot(m.market); SendOffline(); }
                    if (!toldTowns)
                    {
                        toldTowns = true;
                        NetChat.System($"공유 장터: {TownList()} · 여기서 사고팔면 접속한 모두의 값이 같이 바뀌어요");
                    }
                    break;
                case "online":
                    if (ServerShared && m.marketScale > 0f) ChangeScale(m.marketScale);
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

        /// <summary>
        /// 공유 장터 재고를 서버 값으로 맞춘다 (11/08): 재고 = 내 적정 재고 + 서버의 차이. 서버에 없는 물건은 적정 재고(차이 0).
        /// 오래 접속하지 않았어도, 그동안의 거래와 회복이 모두 반영된 같은 재고를 받는다.
        /// </summary>
        static void ApplySnapshot(List<NetClient.MarketRow> rows)
        {
            var gm = GameManager.I;
            if (gm == null || gm.Db == null) return;
            foreach (var id in SharedTowns)
            {
                var town = gm.Db.towns.Find(t => t != null && t.id == id);
                if (town == null) continue;
                var market = gm.GetMarket(town);
                foreach (var it in new List<ItemData>(market.KnownItems)) market.SetStock(it, market.Optimal(it));
                if (rows == null) continue;
                foreach (var r in rows)
                {
                    if (r == null || r.town != id) continue;
                    var it = gm.ItemById(r.item);
                    if (it != null) market.SetStock(it, market.Optimal(it) + r.dev);
                }
            }
        }

        /// <summary>혼자 하는 동안 모아 둔 공유 장터 거래를 서버 재고 위에 다시 더하고, 나눠서 서버에 올린다 (11/08)</summary>
        void SendOffline()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Db == null) return;
            int kinds = 0;
            foreach (var id in SharedTowns)
                foreach (var it in gm.Db.items)
                {
                    if (it == null) continue;
                    string k = OfflineKey(id, it.id);
                    if (!gm.HasMemo(k)) continue;
                    float d = gm.GetMemo(k);
                    gm.ClearMemo(k);
                    if (Mathf.Abs(d) < 0.001f || !Apply(id, it.id, d, out _, out _)) continue;
                    offlineOut.Enqueue(new TradeOut { town = id, item = it.id, n = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(d)), 1, 1000), d = Mathf.Clamp(d, -10000f, 10000f), sell = d > 0f });
                    kinds++;
                }
            if (kinds > 0) NetChat.System($"혼자 하는 동안 공유 장터에서 사고판 것 {kinds}가지를 모두의 장터에 반영했어요");
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

            // 처음 보는 물건이면 적정 재고에서 시작한다 (11/08: Stock()이 재고표에 올리지 않게 바뀐 뒤 남의 거래가 빠지던 것 고침)
            var market = gm.GetMarket(town);
            market.SetStock(item, market.Stock(item) + d);
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
