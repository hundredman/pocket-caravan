using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/26 중앙 큰 장(장식) + 북유럽 특산 공방. 큰 장 규칙(날짜·덤·알림)은 2026-11-04부터 공용 CentralFair.
    ///
    /// [돌아가며 열리는 큰 장] (팀 공통 규칙 Docs/NorthFair.md)
    /// - 날마다 중앙 북쪽 마을 4곳 중 한 곳에 큰 장이 선다. 차례: 북문 → 눈숲 → 개울 나루 → 길목 (게임 날짜 % 4).
    /// - 큰 장이 선 마을 장터에서 물건을 팔면 판 값의 2할을 덤으로 더 받는다.
    /// - 그 마을 광장에는 줄무늬 좌판·구경꾼이 늘어난다 (10/27 깃발 줄은 뺌). 중앙·국경 고개에 들어서면 하루 한 번 어디서 서는지 알려 준다.
    ///
    /// [특산 공방] 재료를 맡기면 게임 시간이 지난 뒤 가공품을 찾아간다 (한 번에 최대 5번 분량).
    /// - 대구 덕장 (피오르 나루): 얼음 대구 3 → 말린 대구 1, 12시간
    /// - 털옷 공방 (모피 사냥꾼 마을): 모피 2 → 털옷 1, 8시간, 수공 5냥
    /// - 양조장 (라우가르 온천): 꿀술 2 + 온천 약초 1 → 온천 약초 꿀술 1, 6시간
    /// 자리는 NorthLife.json "craft" [x, y, 종류], 마당 그림 기준점 "yard" [x, y, 종류]. 마당 그림은 진행에 따라 바뀐다
    /// (덕장: 빈 틀 → 막 건 대구 → 다 마름 / 털옷: 진열 → 작업 중(가죽 틀) / 양조장: 솥 김이 세짐 → 다 되면 꼭지 술통에서 꿀술이 똑똑).
    /// 공방 규칙·저장은 2026-11-04부터 공용 Crafting (옛 저장 north:craft:키:*도 이어 읽음).
    /// </summary>
    public class NorthCraft : MonoBehaviour
    {
        // ---------------- 큰 장 ----------------
        // 2026-11-04 통합: 어디서 언제 서는지·덤·알림은 공용 CentralFair가 맡는다. 여기는 북쪽 장식(좌판·구경꾼·외침)만.
        public static string FairMap => CentralFair.Today("north").mapId;
        public static string FairName => CentralFair.Today("north").name;
        public static string FairNews => CentralFair.News("north");
        static int Today => GameManager.I != null ? GameManager.I.Clock.day : 0;

        // ---------------- 공방 ----------------
        // 2026-11-04 통합: 만드는 규칙(재료·시간·찾아가기·저장)은 공용 Crafting. 여기는 북유럽 공방 자리·마당 그림만.
        static readonly string[] RecipeIds = { "north_fish", "north_fur", "north_brew" };
        static readonly string[] Prefix = { "craft_fish", "craft_fur", "craft_brew" };

        Transform root;
        string lastMap;
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
        static int State(int k) => Crafting.State(RecipeIds[k]);

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
            if (root != null) Destroy(root.gameObject);
            root = null; idlers.Clear(); yard.Clear(); shouters.Clear(); stallLabels.Clear(); sideLabels.Clear(); shoutWho = -1;
            if (shopOpen) CloseShop();
            if (map == null || !map.StartsWith("north_") || GameManager.I == null) return;
            root = new GameObject("North Craft").transform;

            // 큰 장 알림은 공용 CentralFair가 한다
            BuildMarket(map, CentralFair.IsFairMap(map));
            // 10/27 고기 꼬치 화덕 옆 꼬치 장수
            var sp = NorthData.Get(map, "spitters");
            for (int i = 0; i + 1 < sp.Length; i += 2)
            {
                var pos = NorthUtil.Cell(map, sp[i], sp[i + 1]);
                var go = new GameObject("Spit Cook");
                go.transform.SetParent(root, false);
                go.transform.position = pos + new Vector3(0f, -0.4f, 0f);
                Animate(go, "keeper_spit", 0.2f);
                NorthSpot.Make(root, pos, () => "꼬치 장수 토르", SpitShop, Interactable.Kind.Person);
            }
            BuildYards(map);

            // 공방
            var c = NorthData.Get(map, "craft");
            for (int i = 0; i + 2 < c.Length; i += 3)
            {
                int k = c[i + 2];
                if (k < 0 || k >= RecipeIds.Length) continue;
                var pos = NorthUtil.Cell(map, c[i], c[i + 1]);
                // 10/29(기우): 긴 이름표가 머리 위에서 일꾼을 가려서, 공용 이름표는 비우고 일꾼 왼쪽에 오른쪽 맞춤으로 그린다(OnGUI sideLabels)
                var s = NorthSpot.Make(root, pos, () => "", () => Crafting.Open(RecipeIds[k]), Interactable.Kind.Person);
                sideLabels.Add((pos + new Vector3(-0.7f, 0.75f, 0f), () => Crafting.Label(RecipeIds[k])));
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
        // 큰 장 날에는 구경꾼 넷이 더 오고, 장꾼들이 더 자주 외친다.
        // 10/27 장꾼마다 자기 물건만 외친다 (모피 장꾼이 소금 얘기를 하지 않게)
        static string[] ShoutsFor(string sprite)
        {
            switch (sprite)
            {
                case "keeper_fish": return new[] { "싱싱한 얼음 대구 있어요!", "새벽에 건진 거야~", "Hei hei! 생선 보고 가요!" };
                case "keeper_fur": return new[] { "모피 사요, 모피!", "털옷 보고 가요~", "따뜻한 가죽 있어요!" };
                case "keeper_mead": return new[] { "꿀술 한 잔 맛보고 가!", "달콤한 꿀술 있어요~", "Skål! 한 모금 해요!" };
                default: return new[] { "소금 좋은 거 들어왔어!", "고원 치즈도 있다네~", "Takk! 또 와요~" };
            }
        }
        static readonly string[] FairShouts = { "큰 장 날엔 덤이 있어요!", "오늘만 이 값!", "북쪽 물건 다 모였어요~", "어서 와요, 큰 장이야!" };
        // 외치는 사람: 말풍선 자리(월드)와 외칠 말. 장꾼은 좌판 지붕 위 이름표 바로 위, 구경꾼은 머리 위
        readonly List<(Vector3 at, string[] lines, bool keeper)> shouters = new List<(Vector3, string[], bool)>();
        // 10/27 장꾼 이름표: 좌판 지붕 위에 직접 그린다 (머리 위에 그리면 좌판 지붕·물건을 가려서)
        readonly List<(Vector3 at, string text)> stallLabels = new List<(Vector3, string)>();
        // 10/29 공방 일꾼 이름표: 일꾼 왼쪽 (끝이 일꾼 바로 왼쪽에 오게 오른쪽 맞춤)
        readonly List<(Vector3 at, System.Func<string> text)> sideLabels = new List<(Vector3, System.Func<string>)>();
        GUIStyle sideLabelStyle, sideShadowStyle;
        GUIStyle stallLabelStyle, stallShadowStyle;
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
            // 10/27 맵에 놓인 좌판은 타일 대신 새 좌판 그림 (stallArt). 장꾼이 맡은 좌판은 그 장꾼 물건, 나머지는 돌아가며
            var kp = NorthData.Get(map, "keepers");
            var ks = NorthData.Get(map, "keeperStall");
            var art = NorthData.Get(map, "stallArt");
            var kinds = new Dictionary<int, string>();
            for (int i = 0, k = 0; i + 1 < kp.Length && i + 1 < ks.Length; i += 2, k++) kinds[ks[i] * 1000 + ks[i + 1]] = keepers[k % keepers.Length];
            for (int i = 0, j = 0; i + 1 < art.Length; i += 2)
            {
                if (kinds.ContainsKey(art[i] * 1000 + art[i + 1])) continue;
                Stall(map, art[i], art[i + 1], keepers[(j++ + 1) % keepers.Length], false);
            }
            // 이미 있는 좌판마다 장꾼 (최대 넷): 그림은 계산대 뒤, 말 거는 자리는 계산대 앞 칸
            for (int i = 0; i + 1 < kp.Length; i += 2, n++)
            {
                string who = keepers[n % keepers.Length];
                if (i + 1 < ks.Length) Stall(map, ks[i], ks[i + 1], who, true);
                Keeper(map, kp[i], kp[i + 1], who, n, i + 1 < ks.Length ? ks[i] : kp[i], i + 1 < ks.Length ? ks[i + 1] : kp[i + 1] - 1);
            }
            // 좌판이 없는 마을(사냥꾼·라우가르·순록 고원)이나 큰 장 날에는 좌판 둘 + 장꾼을 더 세운다
            if (kp.Length < 2 || fair)
                for (int i = 0; i + 1 < st.Length; i += 2, n++)
                {
                    string who = keepers[n % keepers.Length];
                    Stall(map, st[i], st[i + 1], who, true);
                    Keeper(map, st[i], st[i + 1] + 1, who, n, st[i], st[i + 1]);   // 말 거는 자리는 좌판 바로 앞 칸 (길을 막지 않게)
                }
            if (!fair || mk.Length < 2) return;
            // 10/27 깃발 줄은 뺌: 장터 기준 고정 자리라 좌판에 걸치거나 허공에 떠 보였다 (기우 피드백). 구경꾼·외침은 그대로
            var cr = NorthData.Get(map, "crowd");
            string[] crowd = { "fair_a", "fair_c", "fair_a", "fair_c" };
            for (int i = 0, c = 0; i + 1 < cr.Length && c < crowd.Length; i += 2, c++)
            {
                var go = new GameObject("Fair Crowd");
                go.transform.SetParent(root, false);
                go.transform.position = NorthUtil.Cell(map, cr[i], cr[i + 1]);
                Animate(go, crowd[c], c * 0.45f);
                var sr = go.GetComponent<SpriteRenderer>(); if (sr != null) sr.flipX = c >= 2;
                shouters.Add((go.transform.position + new Vector3(0f, 1.9f, 0f), FairShouts, false));
            }
        }

        /// <summary>10/27 새 좌판 그림 (48x48, 아랫줄 가운데 칸 기준). withKeeper면 계산대 앞판을 한 장 더 그려 장꾼 몸 아래를 가린다</summary>
        void Stall(string map, int cx, int by, string keeperSprite, bool withKeeper)
        {
            string kind = keeperSprite.Replace("keeper_", "");
            var bottom = NorthUtil.Cell(map, cx, by) + new Vector3(0f, -0.5f, 0f);
            PutAt(bottom, "stall_" + kind, 9);
            if (withKeeper) PutAt(bottom, "stall_" + kind + "_front", 11);
        }

        /// <summary>장꾼: 그림은 좌판(sx, sy) 계산대 뒤, 말 거는 자리는 (x, y), 이름표·외침은 좌판 지붕 위</summary>
        void Keeper(string map, int x, int y, string sprite, int n, int sx, int sy)
        {
            var pos = NorthUtil.Cell(map, x, y);
            var bottom = NorthUtil.Cell(map, sx, sy) + new Vector3(0f, -0.5f, 0f);
            var go = new GameObject("Market Keeper");
            go.transform.SetParent(root, false);
            go.transform.position = bottom + new Vector3(0f, 0.65f, 0f);    // 머리·몸 위쪽만 계산대 위로 보이게
            Animate(go, sprite, n * 0.35f);
            var info = KeeperInfo(sprite);
            var top = bottom + new Vector3(0f, 3.15f, 0f);                    // 지붕 꼭대기 바로 위
            stallLabels.Add((top, $"{info.name} ({info.title})"));
            shouters.Add((top, ShoutsFor(sprite), true));
            NorthSpot.Make(root, pos, () => "", () => OpenShop(map, n, sprite), Interactable.Kind.Market);   // 이름표는 지붕 위에 따로 그림
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
        GUIStyle shopRich, priceStyle;

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
            // 11/08 종이 창 (다른 창과 같은 모양) + 제목 리본 + 오른쪽 위 X. 칸 배치·사고파는 규칙은 그대로
            if (shopRich == null || shopRich.font != CuteUI.PaperRich.font) shopRich = new GUIStyle(CuteUI.PaperRich) { wordWrap = false, clipping = TextClipping.Overflow };
            var info = KeeperInfo(shopSprite);
            var town = MarketTown();
            float w = 400f, h = 64f + info.items.Length * 24f + 26f;   // 10/27 세 자리 값이 남은 수량 글자와 겹쳐서 넓힘
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2f), Mathf.Round((UIStyle.VirtualHeight - h) / 2f), w, h);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, info.name);
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) { CloseShop(); return; }
            GUI.Label(new Rect(r.x + 12, r.y + 12, w - 24, 18), CuteUI.PaperColors($"{info.title}   <color=#ffe08a>돈 {gm.Player.money}냥</color>"), shopRich);
            GUI.Label(new Rect(r.x + 12, r.y + 28, w - 24, 16), shopMsg ?? "", CuteUI.PaperSmall);
            float y = r.y + 46f;
            foreach (var id in info.items)
            {
                var item = gm.ItemById(id);
                if (item == null) continue;
                int left = Left(id);
                int market = town != null ? gm.GetMarket(town).BuyPrice(item) : Mathf.RoundToInt(item.basePrice);
                int price = KeeperPrice(item, town);
                CuteUI.Box(new Rect(r.x + 8, y - 1, w - 16, 22), 6, CuteUI.Paper2, CuteUI.Line);
                if (item.icon != null) GUI.DrawTexture(new Rect(r.x + 12, y + 2, 16, 16), item.icon.texture);
                // 칸: 이름 | 장터 값 → 좌판 값 (오른쪽 맞춤) | 남은 수량 | 사기
                GUI.Label(new Rect(r.x + 34, y + 2, 130, 18), item.displayName, shopRich);
                if (priceStyle == null || priceStyle.font != shopRich.font) priceStyle = new GUIStyle(shopRich) { alignment = TextAnchor.MiddleRight };
                GUI.Label(new Rect(r.x + 160, y + 1, 128, 20), CuteUI.PaperColors($"<color=#9a93ad><size=9>장터 {market}</size></color> → <color=#ffde59><b>{price}냥</b></color>"), priceStyle);
                GUI.Label(new Rect(r.x + 296, y + 3, 50, 18), left > 0 ? $"<size=9>{left}개 남음</size>" : CuteUI.PaperColors("<size=9><color=#9a93ad>다 팔림</color></size>"), shopRich);
                GUI.enabled = left > 0;
                if (GUI.Button(new Rect(r.xMax - 52, y, 40, 20), "사기", CuteUI.PaperButtonIf(left > 0))) BuyFromKeeper(item, town);
                GUI.enabled = true;
                y += 24f;
            }
            if (GUI.Button(new Rect(r.x + 12, r.yMax - 26, 110, 20), "장터 전부 보기", CuteUI.PaperButton)) { CloseShop(); OpenMarket(); }
            if (GUI.Button(new Rect(r.xMax - 92, r.yMax - 26, 80, 20), "닫기 Esc", CuteUI.PaperButton)) CloseShop();
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

        const int SkewerPrice = 3;   // 10/27 1시간 효과라 3냥

        /// <summary>10/27 꼬치 장수: 따끈한 고기 꼬치 하나 (3냥) → 체력 가득 + 1시간 동안 눈보라에 덜 지침 (온천·모닥불과 같은 "warm")</summary>
        static void SpitShop()
        {
            DialogueUI.Show("꼬치 장수 토르", "순록 고기를 소금 살짝 쳐서 불에 굴렸지. 하나 먹고 가!\n먹으면 몸이 확 데워져.",
                new DialogueUI.Choice($"꼬치 하나 ({SkewerPrice}냥)", BuySkewer),
                new DialogueUI.Choice("다음에", null));
        }

        static void BuySkewer()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            if (gm.Player.money < SkewerPrice) { Notify.Show("돈이 모자라…", 2f); return; }
            gm.Player.money -= SkewerPrice;
            gm.Player.stamina = 1f;
            NorthSave.Set("warm", Mathf.Max(NorthSave.Get("warm", 0f), gm.Clock.TotalHours + 1f));
            Sfx.Play("coin");
            Notify.Show("따끈한 꼬치! 체력이 가득 차고, 1시간 동안 눈보라에도 덜 지친다.", 3.5f);
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
            var pool = fairHere && Random.value < 0.4f ? FairShouts : shouters[shoutWho].lines;
            shoutText = pool[Random.Range(0, pool.Length)];
            shoutT = Time.time + 2.2f;
        }

        void OnGUI()
        {
            if (shopOpen) { UIStyle.Begin(); DrawShop(); return; }
            if (UIState.Blocking) return;
            var cam = Camera.main;
            if (cam == null) return;
            UIStyle.Begin();
            float sc = UIStyle.Scale;
            // 10/27 장꾼 이름표: 좌판 지붕 위 (다른 이름표처럼 그림자 넣은 초록 글씨)
            if (stallLabelStyle == null)
            {
                stallLabelStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.SmallFont, alignment = TextAnchor.MiddleCenter, fontSize = 9, clipping = TextClipping.Overflow, wordWrap = false };
                stallLabelStyle.normal.textColor = new Color(0.55f, 0.95f, 0.6f);
                stallShadowStyle = new GUIStyle(stallLabelStyle);
                stallShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            }
            var baseColor = GUI.color;
            // 11/13 기우(시연 피드백 "글이 너무 많다"): 좌판·공방 이름표도 다른 이름표처럼 가까이에서만 (WorldLabels.NearAlpha)
            foreach (var l in stallLabels)
            {
                float a = WorldLabels.NearAlpha(l.at);
                if (a <= 0f) continue;
                var lp = cam.WorldToScreenPoint(l.at);
                if (lp.z < 0f) continue;
                float lx = lp.x / sc, ly = (Screen.height - lp.y) / sc;
                GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * a);
                GUI.Label(new Rect(lx - 79f, ly - 7f, 160f, 12f), l.text, stallShadowStyle);
                GUI.Label(new Rect(lx - 80f, ly - 8f, 160f, 12f), l.text, stallLabelStyle);
            }
            GUI.color = baseColor;
            if (sideLabelStyle == null)
            {
                sideLabelStyle = new GUIStyle(stallLabelStyle) { alignment = TextAnchor.MiddleRight };
                sideLabelStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);
                sideShadowStyle = new GUIStyle(sideLabelStyle);
                sideShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            }
            foreach (var l in sideLabels)
            {
                float a = WorldLabels.NearAlpha(l.at);
                if (a <= 0f) continue;
                var lp = cam.WorldToScreenPoint(l.at);
                if (lp.z < 0f) continue;
                float lx = lp.x / sc, ly = (Screen.height - lp.y) / sc;
                string sideText = l.text();
                GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * a);
                GUI.Label(new Rect(lx - 199f, ly - 7f, 200f, 12f), sideText, sideShadowStyle);
                GUI.Label(new Rect(lx - 200f, ly - 8f, 200f, 12f), sideText, sideLabelStyle);
            }
            GUI.color = baseColor;
            if (shoutWho < 0 || shoutWho >= shouters.Count || UIState.PhotoMode) return;   // 11/13 사진 찍는 동안 외침 말풍선 빼기
            var sh = shouters[shoutWho];
            var pl = PlayerController.Instance;   // 11/13 외침 말풍선은 이름표 거리 + 3칸 안에서만
            if (pl != null && Vector2.Distance(pl.transform.position, sh.at) > WorldLabels.NearCells + 3f) return;
            var sp = cam.WorldToScreenPoint(sh.at);
            if (sp.z < 0f) return;
            if (shoutStyle == null) shoutStyle = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            float x = sp.x / sc, y = (Screen.height - sp.y) / sc;
            string t = $"<color=#3a2a1e>{shoutText}</color>";
            float w = shoutStyle.CalcSize(new GUIContent(t)).x + 12f;
            // 장꾼은 이름표 바로 위 한 줄, 구경꾼은 머리 위
            var r = sh.keeper ? new Rect(x - w / 2f, y - 26f, w, 16f) : new Rect(x - w / 2f, y - 16f, w, 16f);
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

    }
}
