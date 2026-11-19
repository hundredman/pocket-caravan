using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 은샘 "좌판까지 구현": 신용 5단계 · 1000냥으로 장터 한 자리에 내 좌판을 연다 (운반 수단 마일스톤의 마지막).
    /// "더 많이 나르기"(운반 수단) 다음 목표는 "내가 없어도 장사가 돈다".
    /// - 여는 곳: 장터 창 아래 "좌판 열기". 조건: 신용 5단계 · 1000냥 · 그 장터 평판 우호 이상. 좌판은 하나 (빈 좌판이면 다른 장터로 옮겨 열 수 있다, 다시 1000냥)
    /// - 진열: 내 짐의 교역품을 올린다 (무게 30까지, 맡은 짐은 못 올림). 게임 한 시간마다 반쯤의 확률로 하나가 그 장터 지금 시세로 팔리고(장터 재고도 움직임),
    ///   판 돈은 좌판에 모인다 → 들러서 받는다. 사서 바로 그 장터에 올리면 되팔기 규칙(TradeActivity.LimitSale) 그대로라 이득이 없다.
    /// - 손님 부르기(StallRush): 좌판 앞을 지나는 사람에게 바라는 물건을 외쳐 부르는 놀이. 물건은 팔지 않고, 점수로 12시간 "북적임"(저절로 팔리는 빈도↑, 값은 그대로) + 최고 기록으로 좌판 꾸미기(StallDeco). 11/19 은샘 "미니게임은 좌판 버프로".
    /// - 맵에 내 좌판 그림(Resources/Stall/stall_0, Tools/ArtGen/player_stall_v2.py, 진열칸에 올린 물건 그림)이 장터 상인 근처 빈자리에 선다. 말 걸면 좌판 창 (StallWindow).
    /// 저장: SaveData.stall* (옛 저장 = 좌판 없음).
    /// </summary>
    public static class PlayerStall
    {
        public const int Price = VehicleGoal.StallPrice, NeedLevel = VehicleGoal.StallCredit;
        public const float Capacity = 30f, SellChance = 0.5f;

        public static bool Has(PlayerState p) => p != null && !string.IsNullOrEmpty(p.stallTown);
        public static bool At(PlayerState p, TownData t) => Has(p) && t != null && p.stallTown == t.id;
        public static bool Unlocked(PlayerState p) => Credit.Level(p) >= NeedLevel;
        public static float Weight(PlayerState p) => p != null ? p.stallStock.Weight : 0f;
        public static int Count(PlayerState p) { int n = 0; if (p != null) foreach (var kv in p.stallStock.Items) n += kv.Value; return n; }
        public static string TownName(PlayerState p, GameDatabase db) { var t = Has(p) && db != null ? db.towns.Find(x => x != null && x.id == p.stallTown) : null; return t != null ? t.displayName : ""; }

        /// <summary>여기서 좌판을 열 수 없는 까닭 (열 수 있으면 null)</summary>
        public static string Denial(PlayerState p, TownData town)
        {
            if (p == null || town == null) return "장터를 찾을 수 없어요";
            if (At(p, town)) return "이미 여기 내 좌판이 있어요";
            if (Credit.Level(p) < NeedLevel) return $"신용 {NeedLevel}단계가 되면 열 수 있어요";
            if (town.region != null && ReputationPricing.Entry(p, town).standing < ReputationStanding.Friendly) return "이 장터 평판이 우호 이상이어야 해요";
            if (Has(p) && (p.stallStock.Weight > 0f || p.stallEarned > 0)) return "지금 좌판을 비우고 판 돈을 받아야 옮길 수 있어요";
            if (p.money < Price) return $"{Price}냥이 필요해요 (지금 {p.money}냥)";
            return null;
        }

        public const string SlotKey = "stall:slot";
        public static bool Open(PlayerState p, TownData town)
        {
            if (Denial(p, town) != null) return false;
            p.money -= Price;
            p.stallTown = town.id; p.stallStock.Clear(); p.stallEarned = 0; p.stallRushDay = -1;
            if (GameManager.I != null) GameManager.I.SetMemo(BuzzLvKey, 0);   // 옮겨 열면 북적임은 새로
            // 11/19 여러 사람이 한 마을에 열 때 자리를 나누려고, 열 때 자리 번호(0~2)를 하나 골라 저장 메모에 둔다 (맵을 다시 들어와도 같은 자리)
            var gm = GameManager.I; if (gm != null) gm.SetMemo(SlotKey, Random.Range(0, 3));
            StallPlacement.Refresh();
            return true;
        }

        /// <summary>내 짐에서 좌판으로 n개 (실제로 옮긴 개수)</summary>
        public static int Put(PlayerState p, ItemData item, int n)
        {
            if (!Has(p) || item == null || item.questItem || n <= 0) return 0;
            int moved = 0;
            while (moved < n && p.cargo.Count(item) > 0 && p.stallStock.Weight + item.weight <= Capacity + 0.001f)
            {
                float cost = p.cargo.AverageCost(item);
                p.cargo.Remove(item); p.stallStock.Add(item, 1, cost); moved++;
            }
            return moved;
        }

        /// <summary>좌판에서 내 짐으로 n개</summary>
        public static int Take(PlayerState p, ItemData item, int n)
        {
            if (!Has(p) || item == null || n <= 0) return 0;
            int moved = 0;
            while (moved < n && p.stallStock.Count(item) > 0 && p.CanCarry(item))
            {
                float cost = p.stallStock.AverageCost(item);
                p.stallStock.Remove(item); p.cargo.Add(item, 1, cost); moved++;
            }
            return moved;
        }

        public static int Collect(PlayerState p)
        {
            if (p == null || p.stallEarned <= 0 || (long)p.money + p.stallEarned > int.MaxValue) return 0;
            int got = p.stallEarned; p.money += got; p.stallEarned = 0;
            return got;
        }

        /// <summary>지금 이 장터에서 좌판 물건 하나가 팔릴 값 (시세 그대로)</summary>
        public static int PriceNow(GameManager gm, ItemData item)
        {
            var t = gm != null && Has(gm.Player) ? gm.Db.towns.Find(x => x != null && x.id == gm.Player.stallTown) : null;
            return t != null && item != null ? Mathf.Max(1, gm.GetMarket(t).SellPrice(item)) : 0;
        }

        /// <summary>좌판에서 하나 판다 (값 배율). 판 값을 돌려준다. 장터 재고·시세도 움직인다</summary>
        public static int SellOne(GameManager gm, ItemData item, float mult, bool toPurse)
        {
            var p = gm.Player;
            if (!Has(p) || item == null || p.stallStock.Count(item) <= 0) return 0;
            var t = gm.Db.towns.Find(x => x != null && x.id == p.stallTown);
            if (t == null) return 0;
            var m = gm.GetMarket(t);
            int price = Mathf.Max(1, Mathf.RoundToInt(m.SellPrice(item) * mult));
            // 이 장터에서 12시간 안에 산 물건은 장터처럼 산 값 밑으로만 (장터와 같은 되팔기 규칙, TradeActivity)
            price = Mathf.Max(1, p.tradeActivity.LimitSale(t.id, item.id, (float)gm.Clock.TotalHours, price));
            // 배율이 1보다 크면 그 장터 사는 값보다 싸게 (11/19부터 배율>1로 부르는 곳은 없음. 장터 규칙 "사는 값 ≥ 파는 값+1")
            if (mult > 1f) price = Mathf.Max(1, Mathf.Min(price, m.BuyPrice(item) - 1));
            if (toPurse && (long)p.money + price > int.MaxValue) return 0;   // 장터처럼 돈 넘침은 팔지 않는다
            float cost = p.stallStock.AverageCost(item);
            p.stallStock.Remove(item);
            m.RecordSell(item, price);
            if (toPurse) p.money += price; else p.stallEarned += price;
            p.stallSold++;
            // 장터에서 판 것과 같이: 매입 기록 소진·평판 · 장부 · 판매 이벤트(의뢰·온라인 장터)
            ReputationPricing.Sold(p, t, item, price, gm.Clock.TotalHours, gm.Clock.day);
            p.ledger.Record(true, t, item, price, Mathf.RoundToInt(price - cost), gm.Clock.day);
            GameEvents.RaiseItemSold(item, t, price, Mathf.RoundToInt(price - cost));
            return price;
        }

        static readonly System.Random rng = new System.Random();

        /// <summary>게임 한 시간마다 (GameManager): 반쯤의 확률로 진열한 물건 하나가 팔린다 (많이 올린 물건일수록 먼저).
        /// 11/19 "손님 부르기"로 좌판이 북적이면(Buzz) 더 자주 팔린다. 값은 그대로 그 장터 시세</summary>
        public static void OnHour(GameManager gm)
        {
            if (gm != null && Loan.Late(gm.Player)) return;   // 11/19 외상 연체 중에는 좌판도 쉰다
            var p = gm != null ? gm.Player : null;
            if (!Has(p)) return;
            int lv = BuzzLevel(gm);
            if (rng.NextDouble() < BuzzChance[lv]) SellRandom(gm, p);
            if (lv >= 3 && rng.NextDouble() < 0.5) SellRandom(gm, p);   // 3단계: 한 시간에 하나 더 (반쯤)
        }

        static void SellRandom(GameManager gm, PlayerState p)
        {
            int total = Count(p);
            if (total <= 0) return;
            int pick = rng.Next(total);
            foreach (var kv in new List<KeyValuePair<ItemData, int>>(p.stallStock.Items))
            {
                if (pick < kv.Value) { SellOne(gm, kv.Key, 1f, false); return; }
                pick -= kv.Value;
            }
        }

        // ---------------- 11/19 북적임: 손님 부르기(StallRush) 점수로 몇 시간 동안 좌판 손님이 더 자주 온다 (은샘 "미니게임은 좌판 버프로")
        public const string BuzzLvKey = "stall:buzz", BuzzUntilKey = "stall:buzzUntil";
        public const float BuzzHours = 12f;
        /// <summary>단계별 한 시간에 하나 팔릴 확률 (0 = 보통). 3단계는 반쯤 하나 더</summary>
        public static readonly float[] BuzzChance = { SellChance, 0.75f, 1f, 1f };
        /// <summary>단계 문턱 (손님 부르기 점수)</summary>
        public static readonly int[] BuzzScore = { 0, 50, 120, 200 };
        public static int BuzzOfScore(int score) { int lv = 0; for (int i = 1; i < BuzzScore.Length; i++) if (score >= BuzzScore[i]) lv = i; return lv; }
        public static int BuzzLevel(GameManager gm)
        {
            if (gm == null || gm.Player == null || !Has(gm.Player)) return 0;
            int lv = Mathf.Clamp(Mathf.RoundToInt(gm.GetMemo(BuzzLvKey)), 0, 3);
            return lv > 0 && gm.Clock.TotalHours < gm.GetMemo(BuzzUntilKey) ? lv : 0;
        }
        public static float BuzzHoursLeft(GameManager gm) => BuzzLevel(gm) > 0 ? Mathf.Max(0f, gm.GetMemo(BuzzUntilKey) - gm.Clock.TotalHours) : 0f;
        /// <summary>새 북적임을 건다. 지금 것보다 낮으면 그대로 두고 false</summary>
        public static bool SetBuzz(GameManager gm, int lv)
        {
            if (gm == null || lv <= 0 || (lv < BuzzLevel(gm) && BuzzHoursLeft(gm) > 1f)) return false;   // 더 센 북적임이 한 시간 넘게 남았으면 그대로
            gm.SetMemo(BuzzLvKey, lv); gm.SetMemo(BuzzUntilKey, gm.Clock.TotalHours + BuzzHours);
            return true;
        }
        public static string BuzzEffect(int lv) => lv >= 3 ? "한 시간에 하나 반꼴로" : lv == 2 ? "한 시간에 꼭 하나씩" : lv == 1 ? "보통보다 1.5배 자주" : "두 시간에 하나꼴로";

        public static void Export(PlayerState p, SaveData d)
        {
            d.stallTown = p.stallTown; d.stallEarned = p.stallEarned; d.stallSold = p.stallSold; d.stallRushDay = p.stallRushDay;
            d.stallItems.Clear(); d.stallCounts.Clear(); d.stallCosts.Clear();
            foreach (var kv in p.stallStock.Items) { d.stallItems.Add(kv.Key.id); d.stallCounts.Add(kv.Value); d.stallCosts.Add(p.stallStock.TotalCost(kv.Key)); }
        }

        public static void Import(PlayerState p, SaveData d, System.Func<string, ItemData> find)
        {
            p.stallStock.Clear();
            p.stallTown = string.IsNullOrEmpty(d.stallTown) ? null : d.stallTown;
            p.stallEarned = Mathf.Max(0, d.stallEarned); p.stallSold = Mathf.Max(0, d.stallSold); p.stallRushDay = d.stallRushDay;
            if (d.stallItems == null) return;
            for (int i = 0; i < d.stallItems.Count && i < d.stallCounts.Count; i++)
            {
                var it = find(d.stallItems[i]);
                int n = d.stallCounts[i];
                if (it != null && n > 0) p.stallStock.Add(it, n, (d.stallCosts != null && i < d.stallCosts.Count ? d.stallCosts[i] : 0f) / n);
            }
        }
    }

    /// <summary>11/19 맵이 열릴 때 내 좌판이 있는 장터면 장터 상인 근처 빈자리에 좌판 그림을 세운다 (씬 파일은 그대로)</summary>
    public sealed class StallPlacement : MonoBehaviour
    {
        static StallPlacement inst;
        StallSpot spot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StallPlacement>() != null) return;
            var go = new GameObject("StallPlacement"); DontDestroyOnLoad(go); inst = go.AddComponent<StallPlacement>();
        }

        /// <summary>좌판을 열거나 옮긴 뒤 바로 다시 세운다</summary>
        public static void Refresh() { if (inst != null) inst.lastKey = null; }

        string lastKey;
        float retryUntil;
        void Update()
        {
            var gm = GameManager.I; var map = MapRoot.Current;
            string key = gm == null || map == null || gm.IsTravelling ? null : map.mapId + "|" + gm.Player.stallTown;
            // 같은 맵이면 다시 찾지 않는다 (장터가 늦게 생길 수 있어 맵이 바뀐 뒤 2초 동안만 다시 찾는다)
            if (key == lastKey && (spot != null || key == null || !PlayerStall.Has(gm.Player) || Time.unscaledTime > retryUntil)) return;
            if (key != lastKey) retryUntil = Time.unscaledTime + 2f;
            lastKey = key;
            if (spot != null) { DestroyImmediate(spot.gameObject); spot = null; }   // 바로 없애야 옛 자리가 새 자리 찾기를 막지 않는다
            if (key == null || !PlayerStall.Has(gm.Player)) return;
            TownMarket market = null;
            foreach (var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None))
                if (m.town != null && m.town.id == gm.Player.stallTown && m.gameObject.scene == map.gameObject.scene) { market = m; break; }
            if (market == null) return;
            var cell = FindPlace(map, map.WorldToCell(market.transform.position));
            if (!cell.HasValue) return;
            var go = new GameObject("내 좌판");
            go.transform.SetParent(map.transform, true);
            var c = map.CellCenter(cell.Value);
            go.transform.position = new Vector3(c.x, c.y - 0.5f, 0f);
            spot = go.AddComponent<StallSpot>();
            if (gm.Player.stallEarned > 0) Notify.Show($"내 좌판에 판 돈 {gm.Player.stallEarned}냥이 모였어요. 좌판에 말 걸어 받아요", 4f);
        }

        /// <summary>장터 상인에서 3~7칸 안, 좌판(3칸 × 3칸)과 그 앞 한 줄이 모두 걸을 수 있고 말 걸 것이 없는 자리. 가까운 곳부터.
        /// 11/19 먼저 옆 좌판·집 그림과 겹치지 않는 자리를 찾고, 없으면 칸만 보고 찾는다 (둘 다 늘 같은 자리가 나오게 움직이는 것은 안 본다)</summary>
        static Vector3Int? FindPlace(MapRoot map, Vector3Int from)
        {
            var props = new List<Bounds>();
            foreach (var sr in map.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr == null || !sr.enabled || sr.GetComponentInParent<Interactable>() != null || sr.GetComponentInParent<PlayerController>() != null) continue;
                var bd = sr.bounds;
                if (bd.size.x >= 1.5f && bd.size.y >= 1.5f) props.Add(bd);   // 좌판·집·나무처럼 큰 그림만 (풀·돌·그림자는 빼고)
            }
            var list = Search(map, from, props, true);
            if (list.Count == 0) list = Search(map, from, props, false);
            if (list.Count == 0) return null;
            // 11/19 은샘 "2명 이상의 유저가 한 마을에 좌판을 열 수도": 가까운 자리 몇 곳 중 내 이름으로 늘 같은 한 곳을 고른다
            // (열 때 고른 자리 번호, 저장 메모. 지금은 내 좌판만 보이지만, 나중에 다른 사람 좌판도 보이게 할 때 서로 덜 겹치게)
            var gm = GameManager.I;
            int slot = gm != null ? Mathf.Clamp(Mathf.RoundToInt(gm.GetMemo(PlayerStall.SlotKey)), 0, 2) : 0;
            return list[Mathf.Min(slot, list.Count - 1)];
        }

        /// <summary>좌판을 둘 수 있는 자리들 (가까운 순, 서로 3칸 넘게 떨어진 것만)</summary>
        static List<Vector3Int> Search(MapRoot map, Vector3Int from, List<Bounds> props, bool roomy)
        {
            var found = new List<KeyValuePair<float, Vector3Int>>();
            for (int dx = -7; dx <= 7; dx++)
                for (int dy = -6; dy <= 6; dy++)
                {
                    float d = Mathf.Abs(dx) + Mathf.Abs(dy) * 1.2f + (dy > 0 ? 0.5f : 0f);
                    if (d < 3f) continue;
                    var c = new Vector3Int(from.x + dx, from.y + dy, 0);
                    bool ok = true;
                    for (int x = -1; x <= 1 && ok; x++)
                        for (int y = -1; y <= 2 && ok; y++)   // 좌판 3칸 높이 + 앞 한 줄(서서 말 거는 자리)
                        {
                            var k = new Vector3Int(c.x + x, c.y + y, 0);
                            if (!map.IsWalkable(k) || Interactable.At(k) != null) ok = false;
                        }
                    if (ok && roomy)
                    {
                        var w = map.CellCenter(c);
                        var area = new Bounds(new Vector3(w.x, w.y - 0.5f + 1.4f, 0f), new Vector3(3.6f, 2.9f, 10f));
                        foreach (var bd in props) { var q = bd; q.extents = new Vector3(q.extents.x, q.extents.y, 10f); if (q.Intersects(area)) { ok = false; break; } }
                    }
                    if (ok) found.Add(new KeyValuePair<float, Vector3Int>(d, c));
                }
            found.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Value.x != b.Value.x ? a.Value.x.CompareTo(b.Value.x) : a.Value.y.CompareTo(b.Value.y));
            var picked = new List<Vector3Int>();
            foreach (var kv in found)
            {
                bool far = true;
                foreach (var q in picked) if (Mathf.Abs(q.x - kv.Value.x) < 4 && Mathf.Abs(q.y - kv.Value.y) < 4) { far = false; break; }
                if (far) picked.Add(kv.Value);
            }
            return picked;
        }
    }

    /// <summary>맵 위 내 좌판. 말 걸면 좌판 창. 11/19 지역 장터 좌판 그림(StallStyle)의 빈 진열칸에 진열한 물건 그림(많은 것 셋)을 올린다.
    /// 판 돈이 모이면 머리 위에 엽전이 깜빡인다</summary>
    public sealed class StallSpot : Interactable
    {
        SpriteRenderer sr, coin, lantern, flag;
        readonly SpriteRenderer[] goods = new SpriteRenderer[3];
        string goodsKey, goodsQuick;
        StallStyle style;
        // 진열칸 위 물건 자리 · 등롱·깃발 자리는 그림마다 다르다 (StallStyle)
        const float GoodsScale = 0.5f;   // 16픽셀 그림을 딱 8픽셀로 (점 크기가 고르게). 진열칸 자리·꾸미기 자리는 지역 그림마다 (StallStyle)

        public override string DisplayName => "내 좌판";
        public override Kind Category => Kind.Market;
        public override string ActionVerb => "좌판 보기";

        void Awake()
        {
            blocksMovement = true;
            style = StallStyle.Current();   // 11/19 은샘 "지역 그림에 맞춰야지": 좌판을 연 마을 지역의 좌판 그림
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = style.Art;
            for (int i = 0; i < goods.Length; i++)
            {
                var g = new GameObject("진열 " + i).transform; g.SetParent(transform, false); g.localScale = Vector3.one * GoodsScale;
                goods[i] = g.gameObject.AddComponent<SpriteRenderer>();
            }
            var c = new GameObject("판 돈").transform; c.SetParent(transform, false); c.localPosition = new Vector3(0f, style.CoinY, 0f);
            coin = c.gameObject.AddComponent<SpriteRenderer>();
            coin.sprite = CoinSprite();
            coin.enabled = false;
            // 11/19 손님 부르기 최고 점수로 달리는 꾸미기 (StallDeco). 지붕 오른쪽 끝에 등롱, 왼쪽 지붕 꼭지에 깃발
            lantern = Child("등롱", LanternSprite(), new Vector3(style.lantern.x / 16f, style.lantern.y / 16f, 0f));
            flag = Child("깃발", FlagSprite(), new Vector3(style.flag.x / 16f, style.flag.y / 16f, 0f));
            RefreshDeco();
        }

        SpriteRenderer Child(string name, Sprite s, Vector3 at)
        {
            var t = new GameObject(name).transform; t.SetParent(transform, false); t.localPosition = at;
            var r = t.gameObject.AddComponent<SpriteRenderer>(); r.sprite = s; return r;
        }

        /// <summary>최고 점수가 바뀌면 (손님 부르기가 끝날 때·불러올 때) 꾸미기를 다시 단다</summary>
        public void RefreshDeco()
        {
            if (lantern != null) lantern.enabled = StallDeco.HasLantern;
            if (flag != null) flag.enabled = StallDeco.HasFlag;
        }

        void LateUpdate()
        {
            var gm = GameManager.I; if (gm == null) return;
            var p = gm.Player;
            UpdateGoods(p);
            // 장터 좌판 그림처럼: 내 발이 좌판 아래 끝보다 위(좌판 안쪽)면 좌판이 앞, 아니면 뒤
            var pc = PlayerController.Instance;
            var body = pc != null && pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            int baseOrder = body != null ? PackAnimal.BodyOrder(body) : 10;   // 11/19 짐승 때문에 잠깐 올린 값이 아니라 원래 값
            if (body != null) sr.sortingLayerID = coin.sortingLayerID = body.sortingLayerID;
            bool front = pc != null && pc.transform.position.y > transform.position.y + 0.35f;
            sr.sortingOrder = front ? baseOrder + 2 : baseOrder - 2;
            foreach (var g in goods) { g.sortingLayerID = sr.sortingLayerID; g.sortingOrder = sr.sortingOrder + 1; }
            lantern.sortingLayerID = flag.sortingLayerID = sr.sortingLayerID; lantern.sortingOrder = flag.sortingOrder = sr.sortingOrder + 1;
            if (Time.frameCount % 60 == 0) RefreshDeco();
            coin.sortingOrder = baseOrder + 20;
            coin.enabled = p.stallEarned > 0;
            if (coin.enabled) coin.transform.localPosition = new Vector3(0f, style.CoinY + Mathf.Sin(Time.time * 3f) * 0.08f, 0f);
        }

        /// <summary>진열한 물건 중 많은 것 셋을 진열칸에 올린다 (바뀔 때만)</summary>
        void UpdateGoods(PlayerState p)
        {
            // 진열이 바뀔 때만 다시 고른다 (개수·무게가 같으면 그대로)
            string quick = PlayerStall.Count(p) + "/" + PlayerStall.Weight(p);
            if (quick == goodsQuick) return;
            goodsQuick = quick;
            var list = new List<KeyValuePair<ItemData, int>>();
            foreach (var kv in p.stallStock.Items) if (kv.Key != null && kv.Key.icon != null && kv.Value > 0) list.Add(new KeyValuePair<ItemData, int>(kv.Key, kv.Value));
            list.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key.id, b.Key.id));
            string key = "";
            for (int i = 0; i < list.Count && i < 3; i++) key += list[i].Key.id + ",";
            if (key == goodsKey) return;
            goodsKey = key;
            // 하나면 가운데, 둘이면 양옆, 셋이면 셋 다
            int n = Mathf.Min(3, list.Count);
            int[] slots = n == 1 ? new[] { 1 } : n == 2 ? new[] { 0, 2 } : new[] { 0, 1, 2 };
            for (int i = 0; i < goods.Length; i++) goods[i].enabled = false;
            for (int i = 0; i < n; i++)
            {
                var g = goods[slots[i]]; var icon = list[i].Key.icon;
                if (icon == null) continue;
                g.sprite = icon; g.enabled = true;
                var b = icon.bounds;   // 그림 기준점이 어디든 바닥이 진열칸에 닿게
                float px = Mathf.Round((style.goodsX[slots[i]] / 16f - b.center.x * GoodsScale) * 16f) / 16f, py = Mathf.Round((style.shelfY / 16f - b.min.y * GoodsScale) * 16f) / 16f;   // 픽셀 단위로 맞춘다
                g.transform.localPosition = new Vector3(px, py, 0f);
            }
        }

        public override void Interact(PlayerController player) => StallWindow.Show();

        protected override void OnEnable() { base.OnEnable(); Current = this; }
        protected override void OnDisable() { base.OnDisable(); if (Current == this) Current = null; }
        public static StallSpot Current { get; private set; }

        /// <summary>좌판 아래 줄 양옆 칸도 막는다 (가운데 칸은 Interactable이 막는다). MapRoot.IsWalkable이 부른다</summary>
        public static bool Blocks(MapRoot root, Vector3Int c)
        {
            var s = Current;
            if (s == null || root == null || s.gameObject.scene != root.gameObject.scene) return false;
            var b = root.WorldToCell(s.transform.position);
            return c.y == b.y && Mathf.Abs(c.x - b.x) == 1;
        }

        static Sprite lanternSprite, flagSprite;
        /// <summary>작은 붉은 등롱 (7×10, 위 가운데 매달림)</summary>
        static Sprite LanternSprite()
        {
            if (lanternSprite != null) return lanternSprite;
            string[] rows = {   // 위에서 아래
                "...k...",
                "..kkk..",
                ".krrrk.",
                "krrhrrk",
                "krrhrrk",
                "krrrrrk",
                "krrrrrk",
                ".krrrk.",
                "..kkk..",
                "...y...",
            };
            return lanternSprite = Paint(rows, new Vector2(0.5f, 1f));
        }
        /// <summary>작은 천 깃발 (8×12, 아래 왼쪽 깃대)</summary>
        static Sprite FlagSprite()
        {
            if (flagSprite != null) return flagSprite;
            string[] rows = {
                "w.......",
                "kbbbbbk.",
                "kbbbbbbk",
                "kbyybbbk",
                "kbyybbk.",
                "kbbbbbbk",
                "kkkkkkk.",
                "k.......",
                "k.......",
                "k.......",
                "k.......",
                "k.......",
            };
            return flagSprite = Paint(rows, new Vector2(0f, 0f));
        }
        static Sprite Paint(string[] rows, Vector2 pivot)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char ch = rows[h - 1 - y][x];
                    tex.SetPixel(x, y, ch == 'k' ? new Color32(74, 46, 32, 255) : ch == 'r' ? new Color32(206, 64, 48, 255) : ch == 'h' ? new Color32(250, 170, 90, 255)
                        : ch == 'y' ? new Color32(240, 196, 80, 255) : ch == 'b' ? new Color32(56, 110, 150, 255) : ch == 'w' ? new Color32(240, 196, 80, 255) : new Color32(0, 0, 0, 0));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 16f);
        }

        static Sprite coinSprite;
        static Sprite CoinSprite()
        {
            if (coinSprite != null) return coinSprite;
            const int n = 9;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var ink = new Color32(46, 36, 29, 255); var gold = new Color32(250, 200, 70, 255); var hi = new Color32(255, 236, 150, 255); var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = (x - 4) * (x - 4) + (y - 4) * (y - 4);
                    bool hole = Mathf.Abs(x - 4) <= 0 && Mathf.Abs(y - 4) <= 0;
                    tex.SetPixel(x, y, d > 16.5f ? clear : d > 10f || hole ? ink : (x < 4 && y > 4) ? hi : gold);
                }
            tex.Apply();
            return coinSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0f), 16f);
        }
    }

    /// <summary>11/19 은샘 "지역 그림에 맞춰야지": 좌판을 연 마을 지역마다 그 지역 장터 좌판을 바탕으로 한 그림 (Tools/ArtGen/player_stall_regions.py)
    /// 과 손님 부르기에 지나가는 그 지역 사람들 (Resources/Stall/StallPeople). 숫자는 그림 픽셀(아래 가운데 기준)</summary>
    public sealed class StallStyle
    {
        public string id, artName;
        public float shelfY;          // 진열칸 윗면 (아래에서)
        public float[] goodsX;        // 진열 물건 셋의 가운데 x
        public Vector2 lantern, flag; // 등롱(위 가운데 매달림) · 깃발(깃대 아래)
        public float height;          // 그림 높이

        Sprite art;
        public Sprite Art { get { if (art == null) { art = Resources.Load<Sprite>("Stall/" + artName); if (art == null) art = Resources.Load<Sprite>("Stall/stall_0"); } return art; } }
        public float CoinY => (height + 2f) / 16f;   // 판 돈 엽전은 그림 위

        static readonly StallStyle Goryeo = new StallStyle { id = "goryeo", artName = "stall_0", shelfY = 15, goodsX = new[] { -9f, 0f, 9f }, lantern = new Vector2(21, 30), flag = new Vector2(-15, 42), height = 48 };
        static readonly StallStyle Venice = new StallStyle { id = "venice", artName = "stall_venice", shelfY = 9, goodsX = new[] { -10f, 0f, 10f }, lantern = new Vector2(23, 18), flag = new Vector2(-21, 30), height = 32 };
        static readonly StallStyle Arabia = new StallStyle { id = "arabia", artName = "stall_arabia", shelfY = 13, goodsX = new[] { -9f, 0f, 9f }, lantern = new Vector2(19, 28), flag = new Vector2(-19, 36), height = 38 };
        static readonly StallStyle North = new StallStyle { id = "north", artName = "stall_north", shelfY = 15, goodsX = new[] { -10f, 0f, 10f }, lantern = new Vector2(22, 32), flag = new Vector2(-22, 44), height = 48 };

        /// <summary>지역 id → 그림. 중앙 동쪽(transit)은 고려, 중앙 서쪽은 베네치아, 중앙 북쪽은 북유럽</summary>
        public static StallStyle For(string regionId)
        {
            switch (regionId)
            {
                case "venice": case "center_venice": return Venice;
                case "arabia": return Arabia;
                case "north": case "center_north": return North;
                default: return Goryeo;
            }
        }

        /// <summary>내 좌판이 있는 마을의 지역 그림</summary>
        public static StallStyle Current()
        {
            var gm = GameManager.I;
            var t = gm != null && gm.Player != null && PlayerStall.Has(gm.Player) ? gm.Db.towns.Find(x => x != null && x.id == gm.Player.stallTown) : null;
            return For(t != null && t.region != null ? t.region.id : null);
        }

        /// <summary>손님 부르기에 지나가는 사람들: 사람마다 왼쪽으로 걷는 4장 (StallPeople 에셋)</summary>
        public Sprite[][] People()
        {
            var set = Resources.Load<StallPeople>("Stall/StallPeople");
            var flat = set != null ? set.For(id) : null;
            if (flat == null || flat.Length < 4) return null;
            var res = new List<Sprite[]>();
            for (int i = 0; i + 3 < flat.Length; i += 4)
            {
                var four = new Sprite[4]; bool ok = true;
                for (int f = 0; f < 4; f++) { four[f] = flat[i + f]; if (four[f] == null) ok = false; }   // 빠진 그림이 있는 사람은 뺀다
                if (ok) res.Add(four);
            }
            return res.Count > 0 ? res.ToArray() : null;   // 아무도 없으면 기본 사람들 (StallRush)
        }
    }
}
