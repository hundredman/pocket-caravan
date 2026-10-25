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
    /// 자리는 NorthLife.json "craft" [x, y, 종류]. 진행은 NorthSave ("craft:키:n" 맡긴 횟수, "craft:키:t" 다 되는 시각).
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
        }

        void Rebuild(string map)
        {
            // 공용 이벤트는 새 게임에서 비워질 수 있어 맵마다 다시 건다 (두 번 걸리지 않게 먼저 뺌)
            GameEvents.ItemSold -= OnSold; GameEvents.ItemSold += OnSold;
            if (root != null) Destroy(root.gameObject);
            root = null; idlers.Clear();
            if (map == null || !map.StartsWith("north_") || GameManager.I == null) return;
            root = new GameObject("North Craft").transform;

            // 큰 장 알림: 중앙·국경 고개에 들어서면 하루 한 번, 큰 장 마을에서는 들어설 때마다
            if (map == FairMap) Notify.Show($"큰 장이 섰다! 오늘 {FairName} 장터에서 팔면 {Mathf.RoundToInt(FairBonus * 10)}할을 더 받는다.", 4f);
            else if ((map.StartsWith("north_c_") || map == "north_pass") && Mathf.RoundToInt(NorthSave.Get("fairnote", -1f)) != Today)
            {
                NorthSave.Set("fairnote", Today);
                Notify.Show(FairNews, 4f);
            }
            if (map == FairMap) BuildFair(map);

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

        void BuildFair(string map)
        {
            var mk = NorthData.Get(map, "market");
            if (mk.Length < 2) return;
            int x = mk[0], y = mk[1];
            Put(map, "fair_stall", x - 7, y + 3, 9); Put(map, "fair_stall", x + 7, y + 3, 9);
            Put(map, "fair_flags", x - 3, y - 5, 12); Put(map, "fair_flags", x + 3, y - 5, 12);
            string[] crowd = { "fair_a", "fair_b", "fair_c", "fair_d" };
            int[,] at = { { -4, 4 }, { 4, 4 }, { -9, 1 }, { 9, 1 } };
            for (int i = 0; i < crowd.Length; i++)
            {
                var go = new GameObject("Fair Crowd");
                go.transform.SetParent(root, false);
                go.transform.position = NorthUtil.Cell(map, x + at[i, 0], y + at[i, 1]);
                Animate(go, crowd[i], i * 0.5f);
            }
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
