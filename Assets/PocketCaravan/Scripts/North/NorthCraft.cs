using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/26 중앙 큰 장 + 북유럽 특산 공방. 공용 코드는 고치지 않는다 (GameEvents.ItemSold를 듣기만 함).
    ///
    /// [돌아가며 열리는 큰 장] (팀 공통 규칙 Docs/NorthFair.md)
    /// - 날마다 중앙 북쪽 마을 4곳 중 한 곳에 큰 장이 선다. 차례: 북문 → 눈숲 → 개울 나루 → 길목 (게임 날짜 % 4).
    /// - 큰 장이 선 마을 장터에서 물건을 팔면 판 값의 2할을 덤으로 더 받는다.
    /// - 그 마을 광장에는 줄무늬 좌판·깃발 줄·구경꾼이 늘어난다. 중앙·국경 고개에 들어서면 하루 한 번 어디서 서는지 알려 준다.
    ///
    /// [특산 공방] 재료를 맡기면 게임 시간이 지난 뒤 가공품을 찾아간다 (한 번에 최대 5번 분량).
    /// - 대구 덕장 (피오르 나루): 얼음 대구 3 → 말린 대구 1, 12시간
    /// - 털옷 공방 (모피 사냥꾼 마을): 모피 2 → 털옷 1, 8시간, 수공 5냥
    /// - 양조장 (라우가르 온천): 꿀술 2 + 온천 약초 1 → 온천 약초 꿀술 1, 6시간
    /// 자리는 NorthLife.json "craft" [x, y, 종류], 마당 그림 기준점 "yard" [x, y, 종류]. 마당 그림은 진행에 따라 바뀐다
    /// (덕장: 빈 틀 → 막 건 대구 → 다 마름 / 털옷: 진열 → 작업 중(가죽 틀) / 양조장: 솥 김이 세짐 → 다 되면 꼭지 술통에서 꿀술이 똑똑).
    /// 진행은 NorthSave ("craft:키:n" 맡긴 횟수, "craft:키:t" 다 되는 시각).
    /// </summary>
    public class NorthCraft : MonoBehaviour
    {
        // ---------------- 큰 장 ----------------
        public const float FairBonus = 0.2f;
        static readonly string[] FairMaps = { "north_c_gatetown", "north_c_snowwood", "north_c_creek", "north_c_crossing" };
        static readonly string[] FairTownIds = { "north_post", "north_cwood", "north_ccreek", "north_cross" };
        static readonly string[] FairNames = { "북문 마을", "눈숲 마을", "개울 나루 마을", "북쪽 길목 마을" };

        static int Today => GameManager.I != null ? GameManager.I.Clock.day : 0;
        static int FairIndex => ((Today % FairMaps.Length) + FairMaps.Length) % FairMaps.Length;
        public static string FairMap => FairMaps[FairIndex];
        public static string FairName => FairNames[FairIndex];
        public static string FairTownId => FairTownIds[FairIndex];
        public static string FairNews => $"오늘 큰 장: {FairName} (팔면 {Mathf.RoundToInt(FairBonus * 10)}할 더)";

        // ---------------- 공방 ----------------
        struct Recipe
        {
            public string key, shop, worker, product, productName, hello;
            public string[] inputs; public int[] counts; public string inputText;
            public int fee; public float hours;
        }
        static readonly Recipe[] Recipes =
        {
            new Recipe { key = "fish", shop = "대구 덕장", worker = "덕장 일꾼", product = "north_stockfish", productName = "말린 대구",
                inputs = new[] { "north_cod" }, counts = new[] { 3 }, inputText = "얼음 대구 3", fee = 0, hours = 12f,
                hello = "바닷바람에 대구를 말리면 가볍고 오래가는 말린 대구가 돼. 남쪽 사람들은 이걸 금처럼 쳐 주지." },
            new Recipe { key = "fur", shop = "털옷 공방", worker = "털옷 장인", product = "north_furcoat", productName = "털옷",
                inputs = new[] { "north_fur" }, counts = new[] { 2 }, inputText = "모피 2", fee = 5, hours = 8f,
                hello = "모피 두 장이면 따뜻한 털옷 한 벌이야. 눈 녹은 남쪽에서도 산 넘어갈 상인들이 줄을 서." },
            new Recipe { key = "brew", shop = "양조장", worker = "양조장 할머니", product = "north_herbmead", productName = "온천 약초 꿀술",
                inputs = new[] { "north_mead", "north_herb" }, counts = new[] { 2, 1 }, inputText = "꿀술 2 + 온천 약초 1", fee = 0, hours = 6f,
                hello = "꿀술에 온천 약초를 담가 김 오르는 통에서 익히지. 마시면 온몸이 노곤해져." },
        };
        const int MaxBatch = 5;
        static readonly string[] Prefix = { "craft_fish", "craft_fur", "craft_brew" };

        Transform root;
        string lastMap;
        AudioSource sfx;
        AudioClip coin, done;
        readonly List<(SpriteRenderer sr, Sprite[] f, float phase)> idlers = new List<(SpriteRenderer, Sprite[], float)>();
        // 10/26 공방 마당 그림 (진행 상태에 따라 바뀜)
        readonly List<(int kind, string role, SpriteRenderer sr)> yard = new List<(int, string, SpriteRenderer)>();
        readonly Dictionary<string, Sprite> yardSprites = new Dictionary<string, Sprite>();
        float nextYard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthCraft>() != null) return;
            var go = new GameObject("NorthCraft");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthCraft>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            coin = NorthUtil.Tone("north_fair_coin", 0.2f, new[] { 1568f, 2093f }, 0.25f);
            done = NorthUtil.Tone("north_craft_done", 0.5f, new[] { 523f, 784f, 1046f }, 0.3f);
        }

        void OnDestroy() { GameEvents.ItemSold -= OnSold; }

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            foreach (var it in idlers)
                if (it.sr != null && it.f[0] != null && it.f[1] != null) it.sr.sprite = it.f[(int)(Time.time * 1.5f + it.phase) % 2];
            if (yard.Count > 0 && Time.time >= nextYard) { nextYard = Time.time + 0.33f; UpdateYard(); }
            UpdateShouts();
        }

        // ---------------- 공방 마당 ----------------
        // 마당 왼쪽 위(yard 기준점)에서 몇 칸 떨어진 곳에 그림을 놓는다. 맵 생성기(north.py)가 같은 자리 아래 줄을 막아 둠.
        static readonly (int kind, string role, int dx, int dy)[] YardLayout =
        {
            (0, "hjell", 1, 5), (0, "hjell", 6, 5), (0, "hjell", 1, 12), (0, "hjell", 6, 12),          // 로포텐 덕장: A자 틀 넷
            (1, "fur", 1, 7),                                                                         // 털옷 공방: 진열대·가죽 틀·작업대
            (2, "vat", 1, 6), (2, "kettle", 5, 6), (2, "tap", 9, 5), (2, "skeps", 10, 2), (2, "herbs", 2, 2),   // 꿀술 양조장
        };

        Sprite YardSprite(string name)
        {
            if (!yardSprites.TryGetValue(name, out var sp)) { sp = NorthUtil.Load(name, Vector2.zero); yardSprites[name] = sp; }
            return sp;
        }

        void BuildYards(string map)
        {
            var y = NorthData.Get(map, "yard");
            for (int i = 0; i + 2 < y.Length; i += 3)
            {
                int k = y[i + 2];
                foreach (var part in YardLayout)
                {
                    if (part.kind != k) continue;
                    var go = new GameObject("Yard " + part.role);
                    go.transform.SetParent(root, false);
                    go.transform.position = NorthUtil.Cell(map, y[i] + part.dx, y[i + 1] + part.dy) + new Vector3(-0.5f, -0.5f, 0f);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 9;
                    yard.Add((k, part.role, sr));
                }
            }
            UpdateYard();
        }

        /// <summary>진행 상태: 0 = 비어 있음, 1 = 만드는 중, 2 = 다 됨</summary>
        static int State(int k)
        {
            if (Batches(k) <= 0) return 0;
            return ReadyAt(k) - Now > 0f ? 1 : 2;
        }

        void UpdateYard()
        {
            int frame = Mathf.FloorToInt(Time.time * 3f);
            foreach (var p in yard)
            {
                if (p.sr == null) continue;
                int st = State(p.kind);
                string name = null;
                switch (p.role)
                {
                    case "hjell": name = st == 0 ? "yard_hjell_empty" : st == 1 ? "yard_hjell_fresh" : "yard_hjell_dried"; break;
                    case "fur": name = st == 1 ? "yard_fur_work" : "yard_fur_full"; break;
                    case "kettle": name = (st == 1 ? "yard_kettle_b" : "yard_kettle_a") + (frame % 3); break;
                    case "tap": name = st == 2 ? "yard_tap" + (frame % 2) : "yard_tap_idle"; break;
                    case "vat": name = "yard_vat"; break;
                    case "skeps": name = "yard_skeps"; break;
                    case "herbs": name = "yard_herbs"; break;
                }
                var sp = name != null ? YardSprite(name) : null;
                if (p.sr.sprite != sp) p.sr.sprite = sp;
            }
        }

        void Rebuild(string map)
        {
            // 공용 이벤트는 새 게임에서 비워질 수 있어 맵마다 다시 건다 (두 번 걸리지 않게 먼저 뺌)
            GameEvents.ItemSold -= OnSold; GameEvents.ItemSold += OnSold;
            if (root != null) Destroy(root.gameObject);
            root = null; idlers.Clear(); yard.Clear(); shouters.Clear(); shoutWho = -1;
            if (shopOpen) CloseShop();
            if (map == null || !map.StartsWith("north_") || GameManager.I == null) return;
            root = new GameObject("North Craft").transform;

            // 큰 장 알림: 중앙·국경 고개에 들어서면 하루 한 번, 큰 장 마을에서는 들어설 때마다
            if (map == FairMap) Notify.Show($"큰 장이 섰다! 오늘 {FairName} 장터에서 팔면 {Mathf.RoundToInt(FairBonus * 10)}할을 더 받는다.", 4f);
            else if ((map.StartsWith("north_c_") || map == "north_pass") && Mathf.RoundToInt(NorthSave.Get("fairnote", -1f)) != Today)
            {
                NorthSave.Set("fairnote", Today);
                Notify.Show(FairNews, 4f);
            }
            BuildMarket(map, map == FairMap);
            BuildYards(map);

            // 공방
            var c = NorthData.Get(map, "craft");
            for (int i = 0; i + 2 < c.Length; i += 3)
            {
                int k = c[i + 2];
                if (k < 0 || k >= Recipes.Length) continue;
                var pos = NorthUtil.Cell(map, c[i], c[i + 1]);
                var s = NorthSpot.Make(root, pos, () => ShopLabel(k), () => Workshop(k), Interactable.Kind.Person);
                Animate(s.gameObject, Prefix[k], 0f);
            }
        }

        void Animate(GameObject go, string prefix, float phase)
        {
            var f = new Sprite[2];
            for (int i = 0; i < 2; i++) f[i] = NorthUtil.Load(prefix + "_d" + i, new Vector2(0.5f, 0.1f));
            if (f[0] == null) return;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = f[0]; sr.sortingOrder = 10;
            idlers.Add((sr, f, phase));
        }

        // ---------------- 큰 장 ----------------

        // 10/27 장터: 모든 북유럽·중앙 북쪽 장터 마을에 좌판 둘 + 장꾼 둘 (말 걸면 그 마을 장터가 열림).
        // 큰 장 날에는 깃발 줄과 구경꾼 넷이 더 오고, 장꾼들이 더 자주 외친다.
        static readonly string[] Shouts = { "싱싱한 얼음 대구 있어요!", "털옷 보고 가요~", "꿀술 한 잔 맛보고 가!", "모피 사요, 모피!", "소금 좋은 거 들어왔어!", "골라 골라~", "Hei hei! 어서 와요!", "Takk! 또 와요~" };
        static readonly string[] FairShouts = { "큰 장 날엔 덤이 있어요!", "오늘만 이 값!", "북쪽 물건 다 모였어요~", "어서 와요, 큰 장이야!" };
        readonly List<Transform> shouters = new List<Transform>();
        int shoutWho = -1; string shoutText; float shoutT, shoutNext;
        bool fairHere;
        GUIStyle shoutStyle;

        void BuildMarket(string map, bool fair)
        {
            shouters.Clear(); shoutWho = -1; fairHere = fair; shoutNext = Time.time + 1.5f;
            var st = NorthData.Get(map, "stalls");
            var mk = NorthData.Get(map, "market");
            var keepers = KeepersFor(map);
            int n = 0;
            // 10/27 이미 있는 좌판(맵에 그려진 천 덮은 좌판) 옆에 장꾼을 세운다 (최대 넷)
            var kp = NorthData.Get(map, "keepers");
            for (int i = 0; i + 1 < kp.Length; i += 2, n++)
                Keeper(map, kp[i], kp[i + 1], keepers[n % keepers.Length], n);
            // 좌판이 없는 마을(사냥꾼·라우가르·순록 고원)이나 큰 장 날에는 줄무늬 좌판 둘 + 장꾼을 더 세운다
            if (kp.Length < 2 || fair)
                for (int i = 0; i + 1 < st.Length; i += 2, n++)
                {
                    PutAt(NorthUtil.Cell(map, st[i], st[i + 1]) + new Vector3(0f, -0.5f, 0f), "fair_stall", 9);   // 3칸 폭, 가운데 칸 기준
                    Keeper(map, st[i] + 2, st[i + 1], keepers[n % keepers.Length], n);
                }
            if (!fair || mk.Length < 2) return;
            int x = mk[0], y = mk[1];
            Put(map, "fair_flags", x - 3, y - 5, 12); Put(map, "fair_flags", x + 3, y - 5, 12);
            var cr = NorthData.Get(map, "crowd");
            string[] crowd = { "fair_a", "fair_c", "fair_a", "fair_c" };
            for (int i = 0, c = 0; i + 1 < cr.Length && c < crowd.Length; i += 2, c++)
            {
                var go = new GameObject("Fair Crowd");
                go.transform.SetParent(root, false);
                go.transform.position = NorthUtil.Cell(map, cr[i], cr[i + 1]);
                Animate(go, crowd[c], c * 0.45f);
                var sr = go.GetComponent<SpriteRenderer>(); if (sr != null) sr.flipX = c >= 2;
                shouters.Add(go.transform);
            }
        }

        void Keeper(string map, int x, int y, string sprite, int n)
        {
            var pos = NorthUtil.Cell(map, x, y);
            var go = new GameObject("Market Keeper");
            go.transform.SetParent(root, false);
            go.transform.position = pos + new Vector3(0f, -0.4f, 0f);
            Animate(go, sprite, n * 0.35f);
            shouters.Add(go.transform);
            var info = KeeperInfo(sprite);
            NorthSpot.Make(root, pos, () => $"{info.name} ({info.title})", () => OpenShop(map, n, sprite), Interactable.Kind.Market);
        }

        // ---------------- 장꾼 좌판 (10/27) ----------------
        // 장꾼마다 자기 물건 서너 가지를 장터보다 1할 5푼 싸게, 하루 4개씩 따로 판다 (장터 재고와 별도, 매일 다시 참).
        // 산 물건은 장터에서 산 것처럼 짐에 들어가고(평균 원가 기록), 공용 '산 물건' 사건도 알린다. 장터 재고·시세는 건드리지 않는다.
        const float KeeperDiscount = 0.85f;
        const int KeeperDaily = 4;
        static (string name, string title, string hello, string[] items) KeeperInfo(string sprite)
        {
            switch (sprite)
            {
                case "keeper_fish": return ("올라프", "생선 좌판", "오늘 새벽에 건진 거야. 장터보다 싸게 줄게!", new[] { "north_cod", "north_char", "north_salmon", "north_stockfish" });
                case "keeper_fur": return ("비요른", "모피 좌판", "숲에서 막 가져온 가죽이야. 몇 장 더 있어.", new[] { "north_fur", "north_furcoat", "north_dried_meat" });
                case "keeper_mead": return ("잉그리드", "꿀술 좌판", "우리 집 벌꿀로 빚은 거야. 한 모금 해 볼래?", new[] { "north_mead", "north_herbmead", "north_herb" });
                default: return ("군나르 할아범", "소금 좌판", "남쪽에서 온 소금이랑 고원 치즈야. 싸게 주지.", new[] { "north_salt", "north_cheese", "north_amber" });
            }
        }

        bool shopOpen; string shopMap, shopSprite; int shopIdx; string shopMsg = "";
        GUIStyle shopRich;

        void OpenShop(string map, int n, string sprite)
        {
            if (UIState.Blocking) return;
            shopOpen = true; shopMap = map; shopIdx = n; shopSprite = sprite; shopMsg = KeeperInfo(sprite).hello;
            UIState.Blocking = true;
            Sfx.Play("open");
        }

        void CloseShop() { shopOpen = false; UIState.Release(); }

        string ShopKey(string item) => $"kp:{shopMap}:{shopIdx}:{item}";
        int Left(string item)
        {
            if (Mathf.RoundToInt(NorthSave.Get($"kpday:{shopMap}:{shopIdx}", -1f)) != Today) return KeeperDaily;
            return Mathf.Max(0, KeeperDaily - Mathf.RoundToInt(NorthSave.Get(ShopKey(item), 0f)));
        }

        static TownData MarketTown()
        {
            foreach (var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None)) if (m != null && m.town != null) return m.town;
            return null;
        }

        int KeeperPrice(ItemData item, TownData town)
        {
            var gm = GameManager.I;
            int market = town != null ? gm.GetMarket(town).BuyPrice(item) : Mathf.RoundToInt(item.basePrice);
            return Mathf.Max(1, Mathf.RoundToInt(market * KeeperDiscount));
        }

        void BuyFromKeeper(ItemData item, TownData town)
        {
            var gm = GameManager.I; var p = gm.Player;
            if (Left(item.id) <= 0) { shopMsg = "그건 오늘 다 팔렸어. 내일 또 와!"; Sfx.Play("cancel"); return; }
            int price = KeeperPrice(item, town);
            if (p.money < price) { shopMsg = "돈이 조금 모자라네…"; Sfx.Play("cancel"); return; }
            if (!p.CanCarry(item)) { shopMsg = "짐이 꽉 찼어. 수레가 있으면 더 실을 텐데."; Sfx.Play("cancel"); return; }
            if (Mathf.RoundToInt(NorthSave.Get($"kpday:{shopMap}:{shopIdx}", -1f)) != Today)
            {
                NorthSave.Set($"kpday:{shopMap}:{shopIdx}", Today);
                foreach (var id in KeeperInfo(shopSprite).items) NorthSave.Set($"kp:{shopMap}:{shopIdx}:{id}", 0f);
            }
            NorthSave.Add(ShopKey(item.id), 1f);
            p.money -= price;
            p.cargo.Add(item, 1, price);
            GameEvents.RaiseItemBought(item, town, price);
            Sfx.Play("coin");
            shopMsg = $"{item.displayName} {price}냥! Takk, 고마워!";
        }

        void DrawShop()
        {
            var gm = GameManager.I;
            if (gm == null) { shopOpen = false; return; }
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { CloseShop(); e.Use(); return; }
            if (shopRich == null) shopRich = new GUIStyle(UIStyle.Rich) { wordWrap = false, clipping = TextClipping.Overflow };
            var info = KeeperInfo(shopSprite);
            var town = MarketTown();
            float w = 330f, h = 64f + info.items.Length * 24f + 26f;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, (UIStyle.VirtualHeight - h) / 2f, w, h);
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 18), $"<b>{info.name}</b> · {info.title}   <color=#ffe08a>돈 {gm.Player.money}냥</color>", shopRich);
            GUI.Label(new Rect(r.x + 12, r.y + 26, w - 24, 16), $"<color=#c9c3d6>{shopMsg}</color>", UIStyle.Small);
            float y = r.y + 46f;
            foreach (var id in info.items)
            {
                var item = gm.ItemById(id);
                if (item == null) continue;
                int left = Left(id);
                int market = town != null ? gm.GetMarket(town).BuyPrice(item) : Mathf.RoundToInt(item.basePrice);
                int price = KeeperPrice(item, town);
                if (item.icon != null) GUI.DrawTexture(new Rect(r.x + 12, y + 2, 16, 16), item.icon.texture);
                GUI.Label(new Rect(r.x + 34, y + 2, 120, 18), item.displayName, shopRich);
                GUI.Label(new Rect(r.x + 150, y + 2, 90, 18), $"<color=#8a8296><size=9>장터 {market}</size></color> → <color=#ffde59><b>{price}냥</b></color>", shopRich);
                GUI.Label(new Rect(r.x + 236, y + 3, 40, 18), left > 0 ? $"<size=9>{left}개 남음</size>" : "<size=9><color=#8a8296>다 팔림</color></size>", shopRich);
                GUI.enabled = left > 0;
                if (GUI.Button(new Rect(r.xMax - 52, y, 40, 20), "사기", UIStyle.Button)) BuyFromKeeper(item, town);
                GUI.enabled = true;
                y += 24f;
            }
            if (GUI.Button(new Rect(r.x + 12, r.yMax - 26, 110, 20), "장터 전부 보기", UIStyle.Button)) { CloseShop(); OpenMarket(); }
            if (GUI.Button(new Rect(r.xMax - 92, r.yMax - 26, 80, 20), "닫기 Esc", UIStyle.Button)) CloseShop();
        }

        /// <summary>마을마다 장꾼 넷의 순서 (그 마을 특산이 먼저)</summary>
        static string[] KeepersFor(string map)
        {
            switch (map)
            {
                case "north_birka": return new[] { "keeper_fish", "keeper_mead", "keeper_fur", "keeper_salt" };
                case "north_hunter": return new[] { "keeper_fur", "keeper_salt", "keeper_mead", "keeper_fish" };
                case "north_naru": return new[] { "keeper_fish", "keeper_fur", "keeper_salt", "keeper_mead" };
                case "north_laugar": return new[] { "keeper_mead", "keeper_fish", "keeper_fur", "keeper_salt" };
                case "north_herd": return new[] { "keeper_fur", "keeper_mead", "keeper_salt", "keeper_fish" };
                case "north_c_gatetown": return new[] { "keeper_salt", "keeper_fur", "keeper_fish", "keeper_mead" };
                case "north_c_snowwood": return new[] { "keeper_fur", "keeper_salt", "keeper_mead", "keeper_fish" };
                case "north_c_creek": return new[] { "keeper_fish", "keeper_salt", "keeper_fur", "keeper_mead" };
                default: return new[] { "keeper_salt", "keeper_mead", "keeper_fur", "keeper_fish" };
            }
        }

        Transform PutAt(Vector3 pos, string name, int order)
        {
            var sp = NorthUtil.Load(name, new Vector2(0.5f, 0f));
            if (sp == null) return null;
            var go = new GameObject("Market " + name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = order;
            return go.transform;
        }

        static void OpenMarket()
        {
            var pc = PlayerController.Instance;
            foreach (var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None))
                if (m != null && m.isActiveAndEnabled) { m.Interact(pc); return; }
        }

        void UpdateShouts()
        {
            if (shouters.Count == 0) return;
            if (shoutWho >= 0 && Time.time > shoutT) shoutWho = -1;
            if (Time.time < shoutNext) return;
            shoutNext = Time.time + (fairHere ? Random.Range(2f, 3.5f) : Random.Range(4.5f, 7f));
            shoutWho = Random.Range(0, shouters.Count);
            var pool = fairHere && Random.value < 0.5f ? FairShouts : Shouts;
            shoutText = pool[Random.Range(0, pool.Length)];
            shoutT = Time.time + 2.2f;
        }

        void OnGUI()
        {
            if (shopOpen) { UIStyle.Begin(); DrawShop(); return; }
            if (shoutWho < 0 || shoutWho >= shouters.Count || shouters[shoutWho] == null || UIState.Blocking) return;
            var cam = Camera.main;
            if (cam == null) return;
            var sp = cam.WorldToScreenPoint(shouters[shoutWho].position + new Vector3(0f, 1.6f, 0f));
            if (sp.z < 0f) return;
            UIStyle.Begin();
            if (shoutStyle == null) shoutStyle = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            float sc = UIStyle.Scale, x = sp.x / sc, y = (Screen.height - sp.y) / sc;
            string t = $"<color=#3a2a1e>{shoutText}</color>";
            float w = shoutStyle.CalcSize(new GUIContent(t)).x + 12f;
            var r = new Rect(x - w / 2f, y - 16f, w, 16f);
            var old = GUI.color;
            GUI.color = new Color(1f, 0.98f, 0.9f, 0.95f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(r, t, shoutStyle);
        }

        void Put(string map, string name, int cx, int cy, int order)
        {
            var sp = NorthUtil.Load(name, new Vector2(0.5f, 0f));
            if (sp == null) return;
            var go = new GameObject("Fair " + name);
            go.transform.SetParent(root, false);
            go.transform.position = NorthUtil.Cell(map, cx, cy);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = order;
        }

        void OnSold(ItemData item, TownData town, int price, int profit)
        {
            if (town == null || town.id != FairTownId || price <= 0 || GameManager.I == null) return;
            int bonus = Mathf.Max(1, Mathf.RoundToInt(price * FairBonus));
            GameManager.I.Player.money += bonus;
            Notify.Show($"큰 장 덤 +{bonus}냥", 1.5f);
            if (!AudioManager.Muted) sfx.PlayOneShot(coin, 0.4f);
        }

        // ---------------- 공방 ----------------

        static float Now => GameManager.I != null ? GameManager.I.Clock.TotalHours : 0f;
        static int Batches(int k) => Mathf.RoundToInt(NorthSave.Get("craft:" + Recipes[k].key + ":n", 0f));
        static float ReadyAt(int k) => NorthSave.Get("craft:" + Recipes[k].key + ":t", 0f);

        static string ShopLabel(int k)
        {
            var r = Recipes[k];
            int n = Batches(k);
            if (n <= 0) return $"{r.shop} ({r.inputText} → {r.productName})";
            float left = ReadyAt(k) - Now;
            return left <= 0f ? $"{r.shop} ({r.productName} {n}개 다 됐음!)" : $"{r.shop} (남은 {Mathf.CeilToInt(left)}시간)";
        }

        void Workshop(int k)
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var r = Recipes[k];
            int n = Batches(k);
            if (n > 0)
            {
                float left = ReadyAt(k) - Now;
                if (left > 0f)
                {
                    DialogueUI.Show(r.worker, $"{r.productName} {n}개, 아직 만드는 중이야. {Mathf.CeilToInt(left)}시간쯤 뒤에 다시 와.");
                    return;
                }
                DialogueUI.Show(r.worker, $"{r.productName} {n}개 다 됐어! 가져가.",
                    new DialogueUI.Choice("찾아가기", () => Collect(k)), new DialogueUI.Choice("나중에", null));
                return;
            }
            int can = MaxBatch;
            for (int i = 0; i < r.inputs.Length; i++)
            {
                var it = gm.ItemById(r.inputs[i]);
                can = Mathf.Min(can, it != null ? gm.Player.cargo.Count(it) / r.counts[i] : 0);
            }
            if (r.fee > 0) can = Mathf.Min(can, gm.Player.money / r.fee);
            if (can <= 0)
            {
                DialogueUI.Show(r.worker, r.hello + $"\n\n{r.inputText}개를 가져오면 {r.productName} 하나를 만들어 줄게." + (r.fee > 0 ? $" 수공은 {r.fee}냥." : "") + $" {r.hours:0}시간 걸려.");
                return;
            }
            var choices = new List<DialogueUI.Choice>();
            choices.Add(new DialogueUI.Choice($"1번 맡기기 ({r.inputText}{(r.fee > 0 ? $", {r.fee}냥" : "")})", () => Begin(k, 1)));
            if (can > 1) choices.Add(new DialogueUI.Choice($"{can}번 맡기기 ({r.productName} {can}개{(r.fee > 0 ? $", {r.fee * can}냥" : "")})", () => Begin(k, can)));
            choices.Add(new DialogueUI.Choice("다음에", null));
            DialogueUI.Show(r.worker, r.hello + $"\n{r.hours:0}시간 뒤에 찾아가.", choices.ToArray());
        }

        void Begin(int k, int times)
        {
            var gm = GameManager.I;
            var r = Recipes[k];
            float cost = r.fee;
            for (int i = 0; i < r.inputs.Length; i++)
            {
                var it = gm.ItemById(r.inputs[i]);
                if (it == null || gm.Player.cargo.Count(it) < r.counts[i] * times) { Notify.Show("재료가 모자라…", 2f); return; }
            }
            if (gm.Player.money < r.fee * times) { Notify.Show("돈이 모자라…", 2f); return; }
            for (int i = 0; i < r.inputs.Length; i++)
            {
                var it = gm.ItemById(r.inputs[i]);
                cost += gm.Player.cargo.AverageCost(it) * r.counts[i];
                gm.Player.cargo.Remove(it, r.counts[i] * times);
            }
            gm.Player.money -= r.fee * times;
            NorthSave.Set("craft:" + r.key + ":n", times);
            NorthSave.Set("craft:" + r.key + ":t", Now + r.hours);
            NorthSave.Set("craft:" + r.key + ":c", cost);           // 한 개당 원가 (장부 손익용)
            Notify.Show($"{r.shop}에 맡겼다. {r.hours:0}시간 뒤에 {r.productName} {times}개를 찾아갈 수 있다.", 3f);
            if (!AudioManager.Muted) sfx.PlayOneShot(coin, 0.4f);
        }

        void Collect(int k)
        {
            var gm = GameManager.I;
            var r = Recipes[k];
            var item = gm.ItemById(r.product);
            if (item == null) { Notify.Show("(가공품 데이터가 없어요: 테스트 준비를 다시 실행)", 3f); return; }
            int n = Batches(k), got = 0;
            float unit = NorthSave.Get("craft:" + r.key + ":c", 0f);
            while (got < n && gm.Player.CanCarry(item)) { gm.Player.cargo.Add(item, 1, unit); got++; }
            if (got == 0) { Notify.Show("짐이 꽉 찼다. 짐을 덜고 다시 오자.", 2.5f); return; }
            NorthSave.Set("craft:" + r.key + ":n", n - got);
            Notify.Show(got < n ? $"{r.productName} {got}개를 챙겼다. 짐이 차서 {n - got}개는 맡겨 둠." : $"{r.productName} {got}개를 챙겼다!", 3f);
            if (!AudioManager.Muted) sfx.PlayOneShot(done, 0.4f);
        }
    }
}
