using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 생활 콘텐츠 (10/24~25, 동물의 숲 오마주). 진행은 NorthSave.
    /// - 온천 목욕 (라우가르): 1시간 지나고 체력·짐 흔들림 회복, 3시간 동안 눈보라에도 덜 지침.
    /// - 순록 먹이 (순록 고원 우리): 하루 한 번. 이끼 한 줌을 내려놓으면 순록들이 걸어와 고개 숙여 뜯어 먹는다. 사흘째엔 치즈 선물.
    /// - 낚시꾼 조합 (피오르 나루): 도감·기록 상금.
    /// - 회관 전시실 (비르카): 물고기 3종·호박·룬 탁본·은빛 뿔을 기증하면 전시대가 채워진다. 다 채우면 칭호·200냥·리액션.
    /// - 떠돌이 상인: 매일 다른 장터 마을에. 리액션 책·오로라 병·황금 미끼.
    /// - 오늘의 특가: 매일 한 마을 좌판에서 물건 하나를 4할 싸게.
    /// - 떨어진 선물: 매일 북유럽 길 몇 군데에 반짝이는 주머니 (밟으면 줍기).
    /// - 룬 점쟁이 (비르카 광장): 다음 날씨와 오늘 밤 오로라를 점쳐 줌.
    /// - 나룻배 사공: 중앙 개울 나루 마을 ↔ 피오르 나루 마을 (12냥, 짐 그대로).
    /// </summary>
    public class NorthDaily : MonoBehaviour
    {
        Transform root;
        string lastMap;
        float bathFade = -1f;
        AudioSource sfx;
        AudioClip splash, munch, coin, chime;
        Sprite moss, pouch, pedestal;
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        Sprite[] reindeerWalk; Sprite reindeerGraze;

        public struct FishInfo { public string id, name; public int goal, goalReward; }
        public static readonly FishInfo[] Fish =
        {
            new FishInfo { id = "north_cod", name = "얼음 대구", goal = 60, goalReward = 50 },
            new FishInfo { id = "north_char", name = "북극 곤들매기", goal = 50, goalReward = 80 },
            new FishInfo { id = "north_salmon", name = "황금 연어", goal = 90, goalReward = 200 },
        };

        // 전시실: (키, 이름, 그림, 필요한 물건 id 또는 특별 조건)
        static readonly (string key, string name, string icon, string item)[] Exhibits =
        {
            ("cod", "얼음 대구", "fish_cod", "north_cod"), ("char", "북극 곤들매기", "fish_char", "north_char"), ("salmon", "황금 연어", "fish_salmon", "north_salmon"),
            ("amber", "얼어붙은 햇빛 (호박)", "amber", "north_amber"), ("rubbing", "룬 탁본 일곱 장", "rubbing", "#runes"), ("antler", "은빛 순록의 뿔", "antler", "#blessed"),
        };

        // 떠돌이 상인·특가가 서는 장터 마을
        static readonly string[] Towns = { "north_birka", "north_hunter", "north_naru", "north_laugar", "north_herd", "north_c_gatetown", "north_c_snowwood", "north_c_creek", "north_c_crossing" };
        static readonly string[] TownNames = { "비르카", "모피 사냥꾼 마을", "피오르 나루 마을", "라우가르 온천 마을", "순록 고원 마을", "북문 마을", "눈숲 마을", "개울 나루 마을", "북쪽 길목 마을" };
        static readonly string[] DealItems = { "north_mead", "north_fur", "north_amber", "north_dried_meat", "north_herb", "north_cheese", "north_salt" };
        static readonly string[] GiftMaps = { "north_runehill", "north_icecave", "north_c_road_nw", "north_c_road_ne", "north_c_runeway", "north_c_road_sw", "north_c_road_se" };

        // 이끼 먹이 연출
        class Grazer { public SpriteRenderer sr; public Vector3 home, spot; public float t; }
        readonly List<Grazer> grazers = new List<Grazer>();
        SpriteRenderer mossPile; float feedT = -1f;
        // 떨어진 선물
        readonly List<(SpriteRenderer sr, string key)> gifts = new List<(SpriteRenderer, string)>();
        // 움직이는 사람 그림 (상인·점쟁이) 흔들기
        readonly List<(SpriteRenderer sr, Sprite[] f)> idlers = new List<(SpriteRenderer, Sprite[])>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthDaily>() != null) return;
            var go = new GameObject("NorthDaily");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthDaily>();
        }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            splash = NorthUtil.Tone("north_bath", 0.7f, new[] { 330f, 262f, 196f }, 0.25f);
            munch = NorthUtil.Tone("north_munch", 0.12f, new[] { 180f, 140f }, 0.3f);
            coin = NorthUtil.Tone("north_coin", 0.18f, new[] { 1318f, 1760f }, 0.3f);
            chime = NorthUtil.Tone("north_museum", 0.6f, new[] { 523f, 659f, 784f, 1046f }, 0.3f);
            moss = NorthUtil.Load("moss", new Vector2(0.5f, 0.3f));
            pouch = NorthUtil.Load("pouch", new Vector2(0.5f, 0.2f));
            pedestal = NorthUtil.Load("pedestal", new Vector2(0.5f, 0.1f));
            foreach (var n in new[] { "fish_cod", "fish_char", "fish_salmon", "amber", "rubbing", "antler", "book", "bottle", "bait" })
                icons[n] = NorthUtil.Load(n, new Vector2(0.5f, 0f));
            var walk = new List<Sprite>();
            for (int i = 0; i < 4; i++)
            {
                var t = Resources.Load<Texture2D>("North/Critters/reindeer_" + i);
                if (t != null) { t.filterMode = FilterMode.Point; walk.Add(Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.1f), 16f)); }
            }
            reindeerWalk = walk.ToArray();
            var g = Resources.Load<Texture2D>("North/Critters/reindeer_graze_0");
            if (g != null) { g.filterMode = FilterMode.Point; reindeerGraze = Sprite.Create(g, new Rect(0, 0, g.width, g.height), new Vector2(0.5f, 0.1f), 16f); }
        }

        static int Today => GameManager.I != null ? GameManager.I.Clock.day : 0;
        static int Hash(int a, int b) { unchecked { uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (int)(h & 0x7fffffff); } }
        public static string WandererTown => Towns[Hash(Today, 1) % Towns.Length];
        public static string DealTown => Towns[Hash(Today, 2) % Towns.Length];
        static string DealItem => DealItems[Hash(Today, 3) % DealItems.Length];

        void Update()
        {
            string map = NorthUtil.Map;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            onBathEdge = false;
            var pcb = PlayerController.Instance;
            if (bathEdge.Count > 0 && pcb != null && pcb.Motor != null && bathFade < 0f && !UIState.Blocking && pcb.TalkTarget == null)
            {
                var c = pcb.Motor.Cell;
                int jy = NorthData.Height(map) - 1 - c.y;
                onBathEdge = bathEdge.Contains(c.x * 1000 + jy);
                if (onBathEdge && !UIState.JustClosed && InputReader.InteractPressed()) Bath();
            }
            if (bathFade >= 0f)
            {
                bathFade += Time.unscaledDeltaTime;
                if (bathFade > 2.6f) { bathFade = -1f; UIState.Release(); FinishBath(); }
            }
            UpdateFeeding();
            UpdateGifts();
            foreach (var it in idlers)
                if (it.sr != null && it.f != null && it.f.Length >= 2 && it.f[0] != null && it.f[1] != null) it.sr.sprite = it.f[(int)(Time.time * 1.5f) % 2];
        }

        Sprite[] Frames(string prefix)
        {
            var a = new Sprite[2];
            for (int i = 0; i < 2; i++) a[i] = NorthUtil.Load(prefix + "_d" + i, new Vector2(0.5f, 0.1f));
            return a;
        }

        NorthSpot Person(Vector3 pos, string prefix, System.Func<string> label, System.Action act, Interactable.Kind kind)
        {
            var s = NorthSpot.Make(root, pos, label, act, kind);
            var f = Frames(prefix);
            if (f[0] != null)
            {
                var sr = s.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = f[0]; sr.sortingOrder = 10;
                idlers.Add((sr, f));
            }
            return s;
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; grazers.Clear(); mossPile = null; feedT = -1f; gifts.Clear(); idlers.Clear();
            if (map == null || !map.StartsWith("north_")) return;
            root = new GameObject("North Daily").transform;

            // 10/27 온천 둘레 어느 칸에서나 Space로 몸 담그기 (정해진 세 자리까지 갈 필요 없게). 이름표 대신 발밑 안내
            bathEdge.Clear();
            var edge = NorthData.Get(map, "bathEdge");
            for (int i = 0; i + 1 < edge.Length; i += 2) bathEdge.Add(edge[i] * 1000 + edge[i + 1]);
            var feed = NorthData.Get(map, "feed");
            if (feed.Length >= 2)
            {
                var pos = NorthUtil.Cell(map, feed[0], feed[1]);
                NorthSpot.Make(root, pos, () => FedToday ? "순록 우리 (오늘은 배불러)" : "순록 우리 (이끼 주기)", () => Feed(pos));
            }
            var guild = NorthData.Get(map, "guild");
            if (guild.Length >= 2)
                NorthSpot.Make(root, NorthUtil.Cell(map, guild[0], guild[1]), () => "낚시꾼 조합", Guild, Interactable.Kind.Board);

            // 전시실: 회관 문 옆 기증 자리 (10/27 전시대 여섯 개는 뺌 — 마당에 책상이 늘어선 게 어색하다는 의견)
            var mus = NorthData.Get(map, "museum");
            if (mus.Length >= 2)
                NorthSpot.Make(root, NorthUtil.Cell(map, mus[0], mus[1]), () => $"회관 전시실 ({Donated}/{Exhibits.Length})", Museum, Interactable.Kind.Board);
            // 룬 점쟁이
            var fort = NorthData.Get(map, "fortune");
            if (fort.Length >= 2) Person(NorthUtil.Cell(map, fort[0], fort[1]), "fortune", () => "룬 점쟁이", Fortune, Interactable.Kind.Person);
            // 나룻배 사공 (10/26): 중앙 개울 나루 마을 ↔ 피오르 나루 마을
            var ferry = NorthData.Get(map, "ferry");
            if (ferry.Length >= 2) Person(NorthUtil.Cell(map, ferry[0], ferry[1]), "ferry", () => "나룻배 사공", Ferry, Interactable.Kind.Person);
            // 떠돌이 상인 · 오늘의 특가 (장터 옆)
            var market = NorthData.Get(map, "market");
            if (market.Length >= 2)
            {
                var mp = NorthUtil.Cell(map, market[0], market[1]);
                // 10/27 장터 사람과 이름표가 겹치지 않게, 맵 생성기가 고른 장터 옆 빈 땅 두 곳 (없으면 예전처럼 장터 양옆)
                var sides = NorthData.Get(map, "sides");
                Vector3 wp = sides.Length >= 2 ? NorthUtil.Cell(map, sides[0], sides[1]) : mp + new Vector3(1.5f, 0f, 0f);
                Vector3 dp = sides.Length >= 4 ? NorthUtil.Cell(map, sides[2], sides[3]) : mp + new Vector3(-1.5f, 0f, 0f);
                if (map == WandererTown) { Person(wp, "wanderer", () => "떠돌이 상인", Wanderer, Interactable.Kind.Market); Notify.Show("떠돌이 상인이 이 마을 장터에 와 있다!", 3f); }
                if (map == DealTown) { Person(map == WandererTown || sides.Length < 4 ? dp : wp, "dealer", () => "오늘의 특가 좌판", Deal, Interactable.Kind.Market); }
            }
            // 떨어진 선물: 오늘 고른 길 맵 세 곳에 두 개씩
            int idx = System.Array.IndexOf(GiftMaps, map);
            if (idx >= 0 && (Hash(Today, 10 + idx) % 10) < 3)
            {
                var spots = NorthData.Get(map, "glints");
                for (int k = 0; k < 2 && spots.Length >= 2; k++)
                {
                    string key = $"pouch:{Today}:{map}:{k}";
                    if (NorthSave.Has(key)) continue;
                    int j = (Hash(Today, 20 + k + idx * 3) % (spots.Length / 2)) * 2;
                    var go = new GameObject("Dropped Gift");
                    go.transform.SetParent(root, false); go.transform.position = NorthUtil.Cell(map, spots[j], spots[j + 1]);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = pouch; sr.sortingOrder = 8;
                    gifts.Add((sr, key));
                }
            }
        }

        // ---------------- 온천 ----------------

        void Bath()
        {
            if (bathFade >= 0f) return;
            bathFade = 0f;
            UIState.Blocking = true;
            if (!AudioManager.Muted) sfx.PlayOneShot(splash, 0.6f);
        }

        void FinishBath()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            gm.Clock.Advance(1f);
            gm.Player.stamina = 1f;
            gm.Player.shake = 0f;
            NorthSave.Set("warm", gm.Clock.TotalHours + 3f);
            Notify.Show("몸이 사르르 녹았다. 3시간 동안 눈보라에도 덜 지친다.", 3.5f);
        }

        // ---------------- 순록 먹이 ----------------

        static bool FedToday => NorthSave.Get("feedday", -1f) >= Today;

        void Feed(Vector3 pos)
        {
            if (FedToday) { DialogueUI.Show("순록 우리", "순록들이 배를 두드리며 누워 있다. 내일 다시 오자."); return; }
            if (feedT >= 0f) return;
            NorthSave.Set("feedday", Today);
            // 이끼 한 줌을 우리 문 앞에 내려놓는다
            var go = new GameObject("Moss Pile");
            go.transform.SetParent(root, false); go.transform.position = pos + new Vector3(0f, 1.1f, 0f);
            mossPile = go.AddComponent<SpriteRenderer>(); mossPile.sprite = moss; mossPile.sortingOrder = 8;
            feedT = 0f;
            Notify.Show("이끼 한 줌을 내려놓았다…", 2f);
            // 우리 안에서 순록 세 마리가 걸어 나온다
            var homes = new[] { new Vector3(-3.5f, 4.5f, 0f), new Vector3(1.5f, 5.5f, 0f), new Vector3(4.5f, 3.8f, 0f) };
            var spots = new[] { new Vector3(-1.1f, 1.6f, 0f), new Vector3(0.9f, 1.9f, 0f), new Vector3(0.1f, 2.6f, 0f) };
            for (int i = 0; i < 3 && reindeerWalk.Length > 0; i++)
            {
                var r = new GameObject("Hungry Reindeer");
                r.transform.SetParent(root, false); r.transform.position = pos + homes[i];
                var sr = r.AddComponent<SpriteRenderer>(); sr.sprite = reindeerWalk[0]; sr.sortingOrder = 9;
                grazers.Add(new Grazer { sr = sr, home = pos + homes[i], spot = pos + spots[i], t = -i * 0.4f });
            }
        }

        void UpdateFeeding()
        {
            if (feedT < 0f) return;
            feedT += Time.deltaTime;
            const float walkIn = 2.2f, eat = 4.5f, walkOut = 2.4f;
            foreach (var g in grazers)
            {
                if (g.sr == null) continue;
                float t = feedT + g.t;
                Vector3 from, to; float k;
                if (t < 0f) continue;
                if (t < walkIn) { from = g.home; to = g.spot; k = t / walkIn; }
                else if (t < walkIn + eat)
                {
                    g.sr.sprite = reindeerGraze != null ? reindeerGraze : reindeerWalk[0];
                    g.sr.flipX = g.sr.transform.position.x > mossPile.transform.position.x;
                    g.sr.transform.position = g.spot + new Vector3(0f, Mathf.Abs(Mathf.Sin(t * 5f)) * 0.03f, 0f);
                    if (Mathf.Repeat(t, 0.9f) < Time.deltaTime && !AudioManager.Muted) sfx.PlayOneShot(munch, 0.25f);
                    continue;
                }
                else if (t < walkIn + eat + walkOut) { from = g.spot; to = g.home; k = (t - walkIn - eat) / walkOut; }
                else { g.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - (t - walkIn - eat - walkOut))); continue; }
                g.sr.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                g.sr.flipX = to.x < from.x;
                g.sr.sprite = reindeerWalk[(int)(t * 7f) % reindeerWalk.Length];
            }
            if (mossPile != null)
            {
                float eaten = Mathf.Clamp01((feedT - walkIn) / eat);
                mossPile.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, eaten);
                if (eaten >= 1f) { Destroy(mossPile.gameObject); mossPile = null; FedDone(); }
            }
            if (feedT > walkIn + eat + walkOut + 1.5f)
            {
                foreach (var g in grazers) if (g.sr != null) Destroy(g.sr.gameObject);
                grazers.Clear(); feedT = -1f;
            }
        }

        void FedDone()
        {
            int n = Mathf.RoundToInt(NorthSave.Add("feedcount", 1f));
            if (n >= 3 && !NorthSave.Has("reindeerfriend"))
            {
                NorthSave.Set("reindeerfriend", 1f);
                var gm = GameManager.I;
                var cheese = gm != null ? gm.ItemById("north_cheese") : null;
                string gift = "";
                if (cheese != null && gm.Player.CanCarry(cheese, 2)) { gm.Player.cargo.Add(cheese, 2, 0f); gift = "\n(순록 치즈 2개를 받았다)"; }
                else if (gm != null) { gm.Player.money += 30; gift = "\n(짐이 가득해서 대신 30냥을 받았다)"; }
                DialogueUI.Show("순록 목동 할머니", "어머나, 순록들이 너만 보면 달려오는구나! 사흘이나 챙겨 줬다며? 이건 우리 순록 젖으로 만든 치즈야." + gift);
            }
            else Notify.Show($"순록들이 이끼를 깨끗이 먹었다 (먹이 준 날 {n}일)", 2.5f);
        }

        // ---------------- 낚시꾼 조합 ----------------

        public static int Caught(string id) => Mathf.RoundToInt(NorthSave.Get("fish:" + id + ":count"));
        public static int Best(string id) => Mathf.RoundToInt(NorthSave.Get("fish:" + id + ":best"));

        void Guild()
        {
            var lines = new List<string> { "얼음 낚시 도감" };
            int pay = 0;
            var claims = new List<string>();
            foreach (var f in Fish)
            {
                int c = Caught(f.id), b = Best(f.id);
                lines.Add(c > 0 ? $"· {f.name}: {c}마리, 최대 {b}cm (목표 {f.goal}cm{(b >= f.goal ? " 달성" : "")})" : "· ???: 아직 못 잡음");
                if (c > 0 && !NorthSave.Has("guild:first:" + f.id)) { pay += 20; claims.Add("guild:first:" + f.id); }
                if (b >= f.goal && !NorthSave.Has("guild:goal:" + f.id)) { pay += f.goalReward; claims.Add("guild:goal:" + f.id); }
            }
            string body = string.Join("\n", lines);
            if (pay > 0)
                DialogueUI.Show("낚시꾼 조합장", body + $"\n\n새 기록이 있구먼! 조합에서 상금 {pay}냥을 주지.",
                    new DialogueUI.Choice($"상금 받기 ({pay}냥)", () =>
                    {
                        foreach (var k in claims) NorthSave.Set(k, 1f);
                        if (GameManager.I != null) GameManager.I.Player.money += pay;
                        if (!AudioManager.Muted) sfx.PlayOneShot(coin, 0.5f);
                        Notify.Show($"낚시꾼 조합 상금 +{pay}냥", 3f);
                    }),
                    new DialogueUI.Choice("나중에", null));
            else
                DialogueUI.Show("낚시꾼 조합장", body + "\n\n처음 잡는 물고기마다 20냥, 크기 목표를 넘기면 큰 상금을 주지. 황금 연어는 오로라 뜬 밤에 잘 문다더군.");
        }

        // ---------------- 회관 전시실 ----------------

        static int Donated { get { int n = 0; foreach (var e in Exhibits) if (NorthSave.Has("museum:" + e.key)) n++; return n; } }

        void Museum()
        {
            var gm = GameManager.I;
            var choices = new List<DialogueUI.Choice>();
            var lines = new List<string>();
            foreach (var e in Exhibits)
            {
                bool done = NorthSave.Has("museum:" + e.key);
                lines.Add((done ? "■ " : "□ ") + e.name);
                if (done || choices.Count >= 3) continue;
                if (e.item == "#runes" ? NorthLegends.Found >= NorthLegends.Total : e.item == "#blessed" ? NorthLegends.Blessed :
                    gm.ItemById(e.item) != null && gm.Player.cargo.Count(gm.ItemById(e.item)) > 0)
                {
                    var ex = e;
                    choices.Add(new DialogueUI.Choice($"{e.name} 기증하기", () => Donate(ex.key, ex.name, ex.item)));
                }
            }
            choices.Add(new DialogueUI.Choice("구경만 할게요", null));
            DialogueUI.Show("회관 전시실 관리인", "북쪽 땅의 보물을 모으고 있다네. 자네가 찾은 걸 기증해 주면 길이길이 전하겠네.\n\n" + string.Join("\n", lines), choices.ToArray());
        }

        void Donate(string key, string name, string item)
        {
            var gm = GameManager.I;
            if (!item.StartsWith("#"))
            {
                var it = gm.ItemById(item);
                if (it == null || !gm.Player.cargo.Remove(it, 1)) return;
            }
            NorthSave.Set("museum:" + key, 1f);
            if (!AudioManager.Muted) sfx.PlayOneShot(chime, 0.6f);
            Rebuild(NorthUtil.Map);   // 전시대에 바로 올라가게
            if (Donated >= Exhibits.Length && !NorthSave.Has("museum:complete"))
            {
                NorthSave.Set("museum:complete", 1f);
                gm.Player.money += 200; gm.Player.credit += 1;
                string taught = NorthSocial.Teach("joy");
                DialogueUI.Show("회관 전시실 관리인", "전시실이 가득 찼어! 자네는 이제 '북쪽의 큐레이터'일세.\n(200냥 · 신용 +1" + (taught != null ? $" · 리액션 '{taught}'" : "") + ")");
            }
            else Notify.Show($"'{name}'을(를) 기증했다 ({Donated}/{Exhibits.Length})", 3f);
        }

        // ---------------- 나룻배 ----------------

        const int FerryFare = 12;

        void Ferry()
        {
            bool toNaru = NorthUtil.Map != "north_naru";
            string dest = toNaru ? "north_naru" : "north_c_creek";
            string where = toNaru ? "피오르 나루 마을" : "중앙 대륙 개울 나루 마을";
            string talk = toNaru
                ? "피오르를 따라 내려가면 금방 나루 마을이야. 비르카를 돌아가는 것보다 훨씬 빠르지. 짐도 같이 실어 줄게."
                : "남쪽 개울 나루 마을까지 태워 줄까? 중앙 대륙으로 가는 지름길이야. 짐도 같이 실어 줄게.";
            talk += "\n노는 네가 저어 봐! Space로 젓고 ←→로 얼음을 피해. 빨리 닿으면 3냥 돌려줄게.";
            DialogueUI.Show("나룻배 사공", talk,
                new DialogueUI.Choice($"{where}로 ({FerryFare}냥)", () => Buy(FerryFare, () =>
                {
                    // 10/27 노 젓기 미니게임 (NorthFerryGame): 끝나거나 Esc로 건너뛰면 건너편에 도착
                    NorthFerryGame.Begin(dest, toNaru ? Vector2Int.right : Vector2Int.left);
                })),
                new DialogueUI.Choice("다음에", null));
        }

        // ---------------- 떠돌이 상인 ----------------

        void Wanderer()
        {
            var gm = GameManager.I;
            var choices = new List<DialogueUI.Choice>
            {
                new DialogueUI.Choice("리액션 책 (80냥)", () => Buy(80, () =>
                {
                    string t = NorthSocial.TeachRandom();
                    if (t == null) { gm.Player.money += 80; DialogueUI.Show("떠돌이 상인", "이런, 자넨 이미 다 아는구먼! 돈은 돌려주지."); }
                })),
                new DialogueUI.Choice("오로라 병 (40냥) — 오늘 밤 꼭 오로라", () => Buy(40, () =>
                {
                    NorthSave.Set("aurora_day", Mathf.FloorToInt(gm.Clock.TotalHours / 24f));
                    Notify.Show("병을 열자 초록 빛이 하늘로 올라갔다. 오늘 밤엔 꼭 오로라가 뜬다!", 3.5f);
                })),
                new DialogueUI.Choice("황금 미끼 (30냥) — 다음 낚시 3번 귀한 물고기", () => Buy(30, () =>
                {
                    NorthSave.Add("bait", 3f);
                    Notify.Show("황금 미끼를 챙겼다. 다음 낚시 세 번은 귀한 물고기가 잘 문다!", 3f);
                })),
                new DialogueUI.Choice("구경만 할게요", null),
            };
            DialogueUI.Show("떠돌이 상인", $"오늘은 {TownNames[System.Array.IndexOf(Towns, WandererTown)]}에 들렀지! 내일은 또 어디로 갈지 몰라. 귀한 물건 구경하고 가게.", choices.ToArray());
        }

        void Buy(int price, System.Action then)
        {
            var gm = GameManager.I;
            if (gm.Player.money < price) { Notify.Show("돈이 모자라…", 2f); return; }
            gm.Player.money -= price;
            if (!AudioManager.Muted) sfx.PlayOneShot(coin, 0.5f);
            then();
        }

        // ---------------- 오늘의 특가 ----------------

        void Deal()
        {
            var gm = GameManager.I;
            var it = gm.ItemById(DealItem);
            if (it == null) { DialogueUI.Show("특가 좌판", "오늘은 물건이 다 떨어졌어."); return; }
            int left = 5 - Mathf.RoundToInt(NorthSave.Get("deal:" + Today));
            int price = Mathf.Max(1, Mathf.RoundToInt(it.basePrice * 0.6f));
            if (left <= 0) { DialogueUI.Show("특가 좌판", $"오늘 {it.displayName} 특가는 다 팔렸어! 내일 또 와."); return; }
            DialogueUI.Show("특가 좌판", $"오늘의 특가! {it.displayName} 한 개에 {price}냥 (평소보다 4할 싸). 오늘 남은 건 {left}개야.",
                new DialogueUI.Choice($"1개 사기 ({price}냥)", () => DealBuy(it, price, 1)),
                new DialogueUI.Choice($"{left}개 다 사기 ({price * left}냥)", () => DealBuy(it, price, left)),
                new DialogueUI.Choice("다음에", null));
        }

        void DealBuy(ItemData it, int price, int n)
        {
            var gm = GameManager.I;
            if (gm.Player.money < price * n) { Notify.Show("돈이 모자라…", 2f); return; }
            if (!gm.Player.CanCarry(it, n)) { Notify.Show("짐이 가득 차서 못 실어.", 2f); return; }
            gm.Player.money -= price * n;
            gm.Player.cargo.Add(it, n, price);
            NorthSave.Add("deal:" + Today, n);
            if (!AudioManager.Muted) sfx.PlayOneShot(coin, 0.5f);
            Notify.Show($"{it.displayName} {n}개를 특가로 샀다", 2.5f);
        }

        /// <summary>마을에 들어설 때 오늘의 특가 알림 (어느 마을인지 미리 알 수 있게 룬 점쟁이·초소 길 안내가 말해 줌)</summary>
        public static string DealNews()
        {
            var gm = GameManager.I;
            var it = gm != null ? gm.ItemById(DealItem) : null;
            int ti = System.Array.IndexOf(Towns, DealTown), wi = System.Array.IndexOf(Towns, WandererTown);
            return $"오늘의 특가: {TownNames[ti]} 좌판의 {(it != null ? it.displayName : "물건")}\n떠돌이 상인: {TownNames[wi]}\n" + NorthCraft.FairNews;
        }

        // ---------------- 떨어진 선물 ----------------

        void UpdateGifts()
        {
            if (gifts.Count == 0) return;
            var pc = PlayerController.Instance;
            for (int i = gifts.Count - 1; i >= 0; i--)
            {
                var (sr, key) = gifts[i];
                if (sr == null) { gifts.RemoveAt(i); continue; }
                sr.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 4f + i) * 0.08f);
                if (pc == null || Vector2.Distance(pc.transform.position, sr.transform.position) > 0.8f) continue;
                NorthSave.Set(key, 1f);
                Destroy(sr.gameObject); gifts.RemoveAt(i);
                PickGift(key);
            }
        }

        void PickGift(string key)
        {
            var gm = GameManager.I;
            if (!AudioManager.Muted) sfx.PlayOneShot(coin, 0.6f);
            int r = key.GetHashCode() & 0x7fffffff;
            int roll = r % 10;
            string[] small = { "north_dried_meat", "north_herb", "north_mead", "north_salt" };
            if (roll < 4) { int m = 10 + r % 31; gm.Player.money += m; Notify.Show($"반짝이는 주머니를 주웠다! (+{m}냥)", 2.5f); return; }
            if (roll < 8)
            {
                var it = gm.ItemById(small[r % small.Length]);
                if (it != null && gm.Player.CanCarry(it, 1)) { gm.Player.cargo.Add(it, 1, 0f); Notify.Show($"반짝이는 주머니를 주웠다! ({it.displayName} 1개)", 2.5f); return; }
                gm.Player.money += 15; Notify.Show("반짝이는 주머니를 주웠다! (+15냥)", 2.5f); return;
            }
            if (roll == 8) { NorthSave.Add("bait", 1f); Notify.Show("반짝이는 주머니에서 황금 미끼가 나왔다!", 2.5f); return; }
            string t = NorthSocial.TeachRandom();
            if (t == null) { gm.Player.money += 40; Notify.Show("반짝이는 주머니를 주웠다! (+40냥)", 2.5f); }
        }

        // ---------------- 룬 점쟁이 ----------------

        void Fortune()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            float h = gm.Clock.TotalHours;
            int block = Mathf.FloorToInt(h / 4f);
            var next1 = NorthAtmosphere.Peek(block + 1); var next2 = NorthAtmosphere.Peek(block + 2);
            bool aurora = NorthAtmosphere.AuroraTonight(Mathf.FloorToInt(h / 24f));
            bool prayed = NorthSave.Get("prayday", -1f) >= Today;
            string body = $"(룬 돌을 던진다… 달그락)\n\n다음 네 시간: {W(next1)}\n그다음 네 시간: {W(next2)}\n오늘 밤 오로라: {(aurora ? "뜬다" : "안 뜬다")}"
                          + (next1 == NorthAtmosphere.Weather.Blizzard || next2 == NorthAtmosphere.Weather.Blizzard ? "\n\n눈보라가 오는구먼. 썰매나 온천으로 대비하게." : "")
                          + "\n\n" + DealNews();
            if (NorthSocial.Knows(7) && !prayed && NorthAtmosphere.Shown >= NorthAtmosphere.Weather.Heavy)
                body += "\n\n지금 날씨 기도를 올리면 하늘이 조금 누그러질 게야. (R → 날씨 기도, 하루 한 번)";
            DialogueUI.Show("룬 점쟁이", body);
        }

        static string W(NorthAtmosphere.Weather w) => w == NorthAtmosphere.Weather.Clear ? "맑음" : w == NorthAtmosphere.Weather.Light ? "가랑눈" : w == NorthAtmosphere.Weather.Heavy ? "함박눈" : "눈보라";

        readonly HashSet<int> bathEdge = new HashSet<int>();
        bool onBathEdge;
        GUIStyle bathHint;

        void OnGUI()
        {
            if (bathFade < 0f)
            {
                if (!onBathEdge || UIState.Blocking) return;
                var pc = PlayerController.Instance; var cam = Camera.main;
                if (pc == null || cam == null) return;
                UIStyle.Begin();
                if (bathHint == null) bathHint = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
                var sp = cam.WorldToScreenPoint(pc.transform.position + new Vector3(0f, -0.8f, 0f));
                float sc = UIStyle.Scale;
                var r = new Rect(sp.x / sc - 60f, (Screen.height - sp.y) / sc, 120f, 14f);
                GUI.Label(r, "<color=#bfeaff>Space 온천에 몸 담그기</color>", bathHint);
                return;
            }
            float a = Mathf.Sin(Mathf.Clamp01(bathFade / 2.6f) * Mathf.PI);
            var old = GUI.color;
            GUI.color = new Color(0.95f, 0.97f, 1f, 0.9f * a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
            UIStyle.Begin();
            GUI.depth = -20;
            GUI.Label(new Rect(0, UIStyle.VirtualHeight / 2 - 10, UIStyle.VirtualWidth, 20), "따끈한 온천에 몸을 담갔다…", UIStyle.CenterRich);
        }
    }
}
