using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 떠돌이 상인 (전 지역 공용, 2026-11-05 통합: 북유럽 NorthDaily의 떠돌이 상인+특가 좌판, 베네치아 VeniceMerchant를 합침).
    /// - 지역마다 날마다 장터 마을 한 곳에 선다. 기본은 WorldIndex에서 장터 있는 맵을 모아 날짜 해시로 고름.
    ///   북유럽·베네치아는 통합 전 순서를 그대로 쓰도록 Schedule에 자기 규칙을 등록한다(시험 봇이 날짜별로 확인하므로).
    /// - 오늘의 특가: 그 지역 물건 하나를 싸게, 하루 5개까지. 산 값이 원가로 기록돼 장부·이익 계산이 맞는다. 장터 시세는 건드리지 않는다.
    ///   특가 값은 "상인의 가격 믿음"(Doran·Parberry식, 11/4 팀 결정): 처음엔 기준가, 날마다 오늘 선 마을의 시세 쪽으로 절반씩 다가간 값의 6할.
    ///   그래서 시세가 오른 물건은 특가도 따라 오르고, 폭락한 물건은 특가도 싸진다. 기준가의 3할~1배 사이로 묶는다.
    /// - 리액션 책(60냥): 아직 모르는 공통 동작 하나. 다 알면 돈을 돌려준다.
    /// - 지역 고유 상품은 Extras에 등록(북: 오로라 병·황금 미끼 / 베: 사자 문장 소문·카니발 가면 춤 비법서).
    /// 저장: 공용 메모 "peddler:deal:날짜:지역"(오늘 판 특가 수), "peddler:belief:물건"(가격 믿음), "peddler:note:지역"(오늘 알림했는지).
    /// 통합 전 북유럽 "deal:날짜"·베네치아 "deal_n_날짜"는 하루짜리라 이어받지 않는다.
    /// </summary>
    public static class Peddler
    {
        public const int MaxDeals = 5;
        public const float DealRate = 0.6f;
        public const int BookPrice = 60;
        public const string Name = "떠돌이 상인";

        /// <summary>지역별 "오늘 서는 맵" 규칙. 없으면 기본(WorldIndex 장터 맵 + 날짜 해시).</summary>
        public static readonly Dictionary<Reactions.Region, Func<string>> Schedule = new Dictionary<Reactions.Region, Func<string>>();
        /// <summary>지역별 특가 후보 물건 id. 없으면 기본(그 지역 마을 장터에 있는 물건 전부).</summary>
        public static readonly Dictionary<Reactions.Region, string[]> DealItems = new Dictionary<Reactions.Region, string[]>();
        /// <summary>지역 고유 상품(선택지). 지역 코드가 등록한다.</summary>
        public static readonly Dictionary<Reactions.Region, Func<List<DialogueUI.Choice>>> Extras = new Dictionary<Reactions.Region, Func<List<DialogueUI.Choice>>>();

        static int Day => GameManager.I != null ? GameManager.I.Clock.day : 0;
        static int Hash(int a, int b) { unchecked { uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (int)(h & 0x7fffffff); } }

        // ---------------- 오늘 어디에 ----------------

        /// <summary>이 지역의 장터 맵 (WorldIndex에 장터 마을이 적힌 맵, 이름순)</summary>
        public static List<WorldIndex.MapInfo> MarketMaps(Reactions.Region region)
        {
            var list = new List<WorldIndex.MapInfo>();
            var idx = WorldIndex.Get();
            if (idx == null) return list;
            foreach (var m in idx.maps)
                if (m != null && !string.IsNullOrEmpty(m.town) && Reactions.RegionOfMap(m.mapId) == region) list.Add(m);
            list.Sort((a, b) => string.CompareOrdinal(a.mapId, b.mapId));
            return list;
        }

        public static string TodayMap(Reactions.Region region)
        {
            if (Schedule.TryGetValue(region, out var f) && f != null) return f();
            var maps = MarketMaps(region);
            return maps.Count == 0 ? null : maps[Hash(Day, 1 + (int)region) % maps.Count].mapId;
        }

        public static TownData TodayTown(Reactions.Region region)
        {
            var map = TodayMap(region);
            var idx = WorldIndex.Get();
            var info = map != null && idx != null ? idx.Map(map) : null;
            if (info == null || GameManager.I == null) return null;
            return GameManager.I.Db.towns.Find(t => t != null && t.id == info.town);
        }

        public static string TodayMapName(Reactions.Region region)
        {
            var map = TodayMap(region);
            var idx = WorldIndex.Get();
            return map == null ? "?" : idx != null ? idx.MapName(map) : map;
        }

        public static bool IsHere => MapRoot.Current != null && TodayMap(Reactions.Here) == MapRoot.Current.mapId;

        // ---------------- 오늘의 특가 ----------------

        public static ItemData DealItem(Reactions.Region region)
        {
            var gm = GameManager.I;
            if (gm == null) return null;
            string[] ids;
            if (!DealItems.TryGetValue(region, out ids) || ids == null || ids.Length == 0)
            {
                // 기본: 이 지역 장터 마을들이 파는 물건 전부 (퀘스트 물건 제외), 이름순
                var set = new SortedSet<string>(StringComparer.Ordinal);
                var idx = WorldIndex.Get();
                foreach (var m in MarketMaps(region))
                {
                    var town = gm.Db.towns.Find(t => t != null && t.id == m.town);
                    if (town == null) continue;
                    foreach (var s in town.stock) if (s != null && s.item != null && !s.item.questItem) set.Add(s.item.id);
                }
                if (set.Count == 0) return null;
                ids = new string[set.Count]; set.CopyTo(ids);
            }
            return gm.ItemById(ids[Hash(Day, 7 + (int)region) % ids.Length]);
        }

        /// <summary>상인의 가격 믿음: 날마다 오늘 선 마을 시세 쪽으로 절반 다가감. 처음엔 기준가.</summary>
        public static float Belief(Reactions.Region region, ItemData item)
        {
            var gm = GameManager.I;
            if (gm == null || item == null) return item != null ? item.basePrice : 1f;
            string key = "peddler:belief:" + item.id, dayKey = key + ":day";
            float b = gm.GetMemo(key, -1f);
            if (b <= 0f) b = item.basePrice;
            if (Mathf.RoundToInt(gm.GetMemo(dayKey, -1f)) != Day)
            {
                var town = TodayTown(region);
                if (town != null) b = Mathf.Lerp(b, gm.GetMarket(town).BaseBuyPrice(item), 0.5f);
                gm.SetMemo(key, b); gm.SetMemo(dayKey, Day);
            }
            return b;
        }

        public static int DealPrice(Reactions.Region region, ItemData item)
        {
            if (item == null) return 1;
            float p = Belief(region, item) * DealRate;
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Clamp(p, item.basePrice * 0.3f, item.basePrice)));
        }

        static string DealKey(Reactions.Region region) => $"peddler:deal:{Day}:{(int)region}";
        public static int DealsToday(Reactions.Region region) => GameManager.I == null ? 0 : Mathf.RoundToInt(GameManager.I.GetMemo(DealKey(region)));
        public static int DealsLeft(Reactions.Region region) => Mathf.Max(0, MaxDeals - DealsToday(region));

        public static bool BuyDeal(Reactions.Region region, ItemData item, int n = 1)
        {
            var gm = GameManager.I;
            if (gm == null || item == null) return false;
            n = Mathf.Clamp(n, 1, DealsLeft(region));
            if (n <= 0) { Notify.Show("오늘 특가는 다 팔렸다.", 2.5f); return false; }
            int price = DealPrice(region, item);
            if (gm.Player.money < price * n) { Notify.Show("돈이 모자란다.", 2.5f); return false; }
            if (!gm.Player.CanCarry(item, n)) { Notify.Show("짐이 가득 찼다.", 2.5f); return false; }
            gm.Player.money -= price * n;
            gm.Player.cargo.Add(item, n, price);
            gm.SetMemo(DealKey(region), DealsToday(region) + n);
            Sfx.Play("coin");
            Notify.Show($"특가로 {item.displayName} {n}개를 {price}냥씩에 샀다 (오늘 남은 {DealsLeft(region)})", 3f);
            return true;
        }

        /// <summary>돈 내고 뭔가 하기 (지역 고유 상품이 쓴다)</summary>
        public static bool Pay(int price, Action then)
        {
            var gm = GameManager.I;
            if (gm == null) return false;
            if (gm.Player.money < price) { Notify.Show("돈이 모자란다.", 2.5f); return false; }
            gm.Player.money -= price;
            Sfx.Play("coin");
            then?.Invoke();
            return true;
        }

        static void BuyBook()
        {
            var gm = GameManager.I;
            if (gm.Player.money < BookPrice) { Notify.Show("돈이 모자란다.", 2.5f); return; }
            string learned = Reactions.TeachRandom();
            if (learned == null) { DialogueUI.Show(Name, "이런, 자넨 이미 다 아는구먼! 돈은 됐네."); return; }
            gm.Player.money -= BookPrice;
            Sfx.Play("levelup", .7f);
            Notify.Show($"비법서를 읽고 리액션 '{learned}'을(를) 배웠다! (R)", 4f);
        }

        /// <summary>안내 한 줄 (점쟁이·길 안내·알림에 그대로)</summary>
        public static string News(Reactions.Region region)
        {
            var it = DealItem(region);
            return $"떠돌이 상인: {TodayMapName(region)}" + (it != null ? $" (특가 {it.displayName} {DealPrice(region, it)}냥)" : "");
        }

        // ---------------- 말 걸기 ----------------

        public static void Open()
        {
            var region = Reactions.Here;
            var item = DealItem(region);
            int left = DealsLeft(region);
            var choices = new List<DialogueUI.Choice>();
            if (item != null && left > 0)
            {
                int price = DealPrice(region, item);
                choices.Add(new DialogueUI.Choice($"특가 {item.displayName} 1개 ({price}냥, 남은 {left})", () => BuyDeal(region, item, 1)));
                if (left > 1) choices.Add(new DialogueUI.Choice($"{left}개 다 사기 ({price * left}냥)", () => BuyDeal(region, item, left)));
            }
            choices.Add(new DialogueUI.Choice($"리액션 책 ({BookPrice}냥)", BuyBook));
            if (Extras.TryGetValue(region, out var extra) && extra != null)
            {
                var more = extra();
                if (more != null) choices.AddRange(more);
            }
            choices.Add(new DialogueUI.Choice("구경만 할게요", null));
            string deal = item != null
                ? (left > 0 ? $"오늘의 특가는 {item.displayName}, {DealPrice(region, item)}냥에 주지." : $"오늘 {item.displayName} 특가는 다 나갔어. 내일 또 보세.")
                : "오늘은 특가가 없네.";
            DialogueUI.Show(Name, $"오늘은 {TodayMapName(region)}에 짐을 풀었소. 내일은 또 다른 데로 가지.\n{deal}", choices.ToArray());
        }

        /// <summary>장터 옆에 서 있는 떠돌이 상인</summary>
        public class Spot : Interactable
        {
            public override string DisplayName => Name;
            public override Kind Category => Kind.Market;
            public override void Interact(PlayerController player) => Open();
            SpriteRenderer body;
            void Update() { if (body != null) body.transform.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 2.4f) * .02f, 1f); }   // 숨쉬기
            public void SetSprite(Sprite s) { body = gameObject.AddComponent<SpriteRenderer>(); body.sprite = s; body.sortingOrder = 10; }
        }

        static Sprite LoadSprite(Reactions.Region region)
        {
            string path = region == Reactions.Region.North ? "North/Minigame/wanderer_d0"
                        : region == Reactions.Region.Venice ? "Venice/Life/wanderer"
                        : "Common/peddler";
            var t = Resources.Load<Texture2D>(path);
            if (t == null && path != "Common/peddler") t = Resources.Load<Texture2D>("Common/peddler");
            if (t == null) return null;
            t.filterMode = FilterMode.Point;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.1f), 16f);
        }

        /// <summary>장터 상인 옆 빈 칸. 못 찾으면 null</summary>
        static Vector3? SpotNearMarket()
        {
            var map = MapRoot.Current;
            var market = UnityEngine.Object.FindAnyObjectByType<TownMarket>();
            if (map == null || market == null) return null;
            var c = map.WorldToCell(market.transform.position);
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var pc = player != null ? map.WorldToCell(player.transform.position) : new Vector3Int(int.MinValue, 0, 0);
            // 오른쪽·왼쪽·아래 순으로 가까운 칸부터
            int[] dx = { 2, -2, 1, -1, 2, -2, 3, -3, 0, 0, 3, -3 };
            int[] dy = { 0, 0, -1, -1, -1, -1, 0, 0, -2, -1, -1, -1 };
            for (int i = 0; i < dx.Length; i++)
            {
                var cell = new Vector3Int(c.x + dx[i], c.y + dy[i], c.z);
                if (cell == pc || !map.IsWalkable(cell) || Interactable.At(cell) != null) continue;
                return map.CellCenter(cell);
            }
            return null;
        }

        // ---------------- 실행 ----------------

        class Runner : MonoBehaviour
        {
            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }

            void OnLoaded(Scene s, LoadSceneMode m)
            {
                if (GameManager.I == null || MapRoot.Current == null) return;
                var region = Reactions.Here;
                string map = MapRoot.Current.mapId;
                var idx = WorldIndex.Get();
                var info = idx != null ? idx.Map(map) : null;
                if (info == null || string.IsNullOrEmpty(info.town)) return;   // 장터 마을에서만

                if (TodayMap(region) == map)
                {
                    var pos = SpotNearMarket();
                    if (pos == null) { Debug.LogWarning($"[떠돌이 상인] {map}: 장터 옆에 설 자리가 없다"); return; }
                    var go = new GameObject("떠돌이 상인");
                    go.transform.position = pos.Value;
                    var spot = go.AddComponent<Spot>();
                    spot.SetSprite(LoadSprite(region));
                    Notify.Show("떠돌이 상인이 이 마을 장터에 와 있다!", 3f);
                    return;
                }
                string key = "peddler:note:" + (int)region;
                if (Mathf.RoundToInt(GameManager.I.GetMemo(key, -1f)) == Day) return;
                GameManager.I.SetMemo(key, Day);
                Notify.Show("오늘 " + News(region), 4f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("Peddler");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
