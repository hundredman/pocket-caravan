using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 특산 공방 (전 지역 공용 엔진, 2026-11-04 통합: 북유럽 NorthCraft 공방 + 베네치아 VeniceWorkshop의 만드는 규칙을 합침).
    /// 재료(+삯)를 맡기면 hours 시간 뒤에 가공품을 찾아간다. hours = 0이면 그 자리에서 바로 받는다.
    /// 한 번에 최대 maxBatch번 분량, dailyMax > 0이면 하루에 그만큼만.
    /// 대체 재료(alt): 예) 북유럽 양조장은 꿀술 2 대신 벌꿀 단지 1.
    ///
    /// [지역 담당이 공방 더하는 법]
    /// 1) 아래 Recipes에 한 줄 (id는 "지역_이름", 재료·결과 물건 id는 GameDatabase에 있는 것)
    /// 2) 맵에 공방 자리: 공방 일꾼(talker 등)에 WorkshopSpot을 붙이거나, 지역 코드에서 Crafting.Open("id")를 부른다
    /// 고려 한지 공방은 현지 매입가에 작업삯을 더해 주문받는다. 아라비아 공방은 아직 없다.
    ///
    /// 저장: 공용 메모 "craft:id:n|t|c|day|cnt". 통합 전 북유럽 저장(north:craft:키:*)도 이어서 읽는다.
    /// </summary>
    public static class Crafting
    {
        public class Recipe
        {
            public string id, shop, worker, hello, product, productName, inputText, legacy;
            public string[] inputs = new string[0]; public int[] counts = new int[0];
            public int fee; public float hours;
            public int maxBatch = 5, dailyMax;
            public string[] altInputs; public int[] altCounts; public string altText, altLine;
        }

        public static readonly List<Recipe> Recipes = new List<Recipe>
        {
            // ---------- 북유럽 (기우) ----------
            new Recipe { id = "north_fish", legacy = "north:craft:fish", shop = "대구 덕장", worker = "덕장 일꾼", product = "north_stockfish", productName = "말린 대구",
                inputs = new[] { "north_cod" }, counts = new[] { 3 }, inputText = "얼음 대구 3", fee = 0, hours = 12f,
                hello = "바닷바람에 대구를 말리면 가볍고 오래가는 말린 대구가 돼. 남쪽 사람들은 이걸 금처럼 쳐 주지." },
            new Recipe { id = "north_fur", legacy = "north:craft:fur", shop = "털옷 공방", worker = "털옷 장인", product = "north_furcoat", productName = "털옷",
                inputs = new[] { "north_fur" }, counts = new[] { 2 }, inputText = "모피 2", fee = 5, hours = 8f,
                hello = "모피 두 장이면 따뜻한 털옷 한 벌이야. 눈 녹은 남쪽에서도 산 넘어갈 상인들이 줄을 서." },
            new Recipe { id = "north_brew", legacy = "north:craft:brew", shop = "양조장", worker = "양조장 할머니", product = "north_herbmead", productName = "온천 약초 꿀술",
                inputs = new[] { "north_mead", "north_herb" }, counts = new[] { 2, 1 }, inputText = "꿀술 2 + 온천 약초 1", fee = 0, hours = 6f,
                altInputs = new[] { "north_honey", "north_herb" }, altCounts = new[] { 1, 1 }, altText = "벌꿀 단지 1 + 온천 약초 1",
                altLine = "오, 그 벌꿀 단지! 그거면 꿀술 두 병 몫이야. 약초 하나랑 같이 맡기면 약초 꿀술을 빚어 주지.",
                hello = "꿀술에 온천 약초를 담가 김 오르는 통에서 익히지. 마시면 온몸이 노곤해져." },
            // ---------- 베네치아 (윤채) — 바로 받는 공방 ----------
            new Recipe { id = "venice_glassart", shop = "무라노 장식 공방", worker = "장식 공방 장인", product = "venice_glass_art", productName = "유리 장식",
                inputs = new[] { "venice_glass" }, counts = new[] { 2 }, inputText = "유리그릇 2", fee = 15, hours = 0f, maxBatch = 1,
                hello = "유리그릇 두 개를 녹여 장식을 빚어 주지. 장식은 가볍고 멀리 갈수록 값이 오르네." },
            new Recipe { id = "venice_lace", shop = "부라노 레이스 공방", worker = "레이스 뜨는 할머니", product = "venice_lace", productName = "레이스 손수건",
                fee = 25, hours = 0f, maxBatch = 1, dailyMax = 2,
                hello = "한 장 뜨는 데 손이 많이 가. 가벼워서 짐 걱정 없이 멀리 팔 수 있지." },
            // ---------- 고려 (은샘 · 2026-11-04 승인) ----------
            new Recipe { id = "goryeo_hanji", shop = "닥목 한지 공방", worker = "한지 장인", product = "paper", productName = "종이",
                fee = 18, hours = 6f, maxBatch = 1, dailyMax = 2,
                hello = "재료는 공방에서 마련합니다. 삯을 맡기면 종이 한 묶음을 떠서 말려 드리지요." },
            // ---------- 아라비아 (기우 · 2026-11-07) ----------
            new Recipe { id = "arabia_perfume", shop = "사나 향유 공방", worker = "향유 장인", product = "arabia_perfume", productName = "향유",
                inputs = new[] { "arabia_frankincense" }, counts = new[] { 2 }, inputText = "유향 덩이 2", fee = 8, hours = 6f,
                hello = "유향 두 덩이를 기름에 우리면 향유 한 병이 되오. 가볍고, 바다 건너면 값이 세 곱은 뛰지." },
            new Recipe { id = "arabia_roast", shop = "타이즈 커피 볶는 집", worker = "커피 볶는 아주머니", product = "arabia_roasted_coffee", productName = "볶은 커피",
                inputs = new[] { "arabia_coffee" }, counts = new[] { 2 }, inputText = "커피 열매 자루 2", fee = 4, hours = 4f,
                hello = "열매 그대로는 아무도 안 사. 볶아야 향이 나지. 자루 둘을 맡기면 볶은 커피 한 자루로 만들어 줄게." },
        };

        public static Recipe Find(string id) => Recipes.Find(r => r.id == id);

        // ---------------- 저장 ----------------
        static GameManager GM => GameManager.I;
        static float Now => GM != null ? GM.Clock.TotalHours : 0f;
        static float Get(Recipe r, string what)
        {
            if (GM == null) return 0f;
            string k = $"craft:{r.id}:{what}";
            if (GM.HasMemo(k)) return GM.GetMemo(k);
            return r.legacy != null ? GM.GetMemo($"{r.legacy}:{what}", 0f) : 0f;
        }
        static void Set(Recipe r, string what, float v)
        {
            if (GM == null) return;
            GM.SetMemo($"craft:{r.id}:{what}", v);
            if (r.legacy != null) GM.ClearMemo($"{r.legacy}:{what}");
        }

        public static int Batches(string id) { var r = Find(id); return r != null ? Mathf.RoundToInt(Get(r, "n")) : 0; }
        public static float ReadyAt(string id) { var r = Find(id); return r != null ? Get(r, "t") : 0f; }
        /// <summary>0 = 비어 있음, 1 = 만드는 중, 2 = 다 됨</summary>
        public static int State(string id)
        {
            if (Batches(id) <= 0) return 0;
            return ReadyAt(id) - Now > 0f ? 1 : 2;
        }
        /// <summary>이름표: 이름과 진행 상태만 짧게</summary>
        public static string Label(string id)
        {
            var r = Find(id);
            if (r == null) return id;
            int n = Batches(id);
            if (n <= 0) return r.shop;
            float left = ReadyAt(id) - Now;
            return left <= 0f ? $"{r.shop} (다 됐음!)" : $"{r.shop} ({Mathf.CeilToInt(left)}시간)";
        }
        /// <summary>하루 제한이 있는 공방이 오늘 다 찼는지 (놀거리 알림용)</summary>
        public static bool DoneForToday(string id) { var r = Find(id); return r != null && r.dailyMax > 0 && MadeToday(r) >= r.dailyMax; }
        static int MadeToday(Recipe r) => GM != null && Mathf.RoundToInt(Get(r, "day")) == GM.Clock.day ? Mathf.RoundToInt(Get(r, "cnt")) : 0;

        // ---------------- 말 걸기 ----------------
        /// <summary>공방 일꾼에게 말을 걸었을 때 (지역 코드·WorkshopSpot이 부른다)</summary>
        public static void Open(string id)
        {
            var r = Find(id);
            if (r == null || GM == null) { Notify.Show($"(공방 데이터 없음: {id})", 2f); return; }

            int n = Batches(id);
            if (n > 0)
            {
                float left = ReadyAt(id) - Now;
                if (left > 0f) { DialogueUI.Show(r.worker, $"{r.productName} {n}개, 아직 만드는 중이야. {Mathf.CeilToInt(left)}시간쯤 뒤에 다시 와."); return; }
                DialogueUI.Show(r.worker, $"{r.productName} {n}개 다 됐어! 가져가.",
                    new DialogueUI.Choice("찾아가기", () => Collect(r)), new DialogueUI.Choice("나중에", null));
                return;
            }
            if (r.dailyMax > 0 && MadeToday(r) >= r.dailyMax) { DialogueUI.Show(r.worker, $"오늘은 {r.dailyMax}개 다 만들었어. 내일 다시 와."); return; }

            int can = Can(r, r.inputs, r.counts);
            bool altOk = r.altInputs != null && Can(r, r.altInputs, r.altCounts) > 0;
            string when = r.hours > 0f ? $"{r.hours:0}시간 뒤에 찾아가." : "바로 만들어 줄게.";
            string cost = string.IsNullOrEmpty(r.inputText) ? (r.fee > 0 ? $"{r.fee}냥" : "") : r.inputText + (r.fee > 0 ? $", {r.fee}냥" : "");
            if (can <= 0 && altOk)
            {
                DialogueUI.Show(r.worker, r.altLine + "\n" + when,
                    new DialogueUI.Choice($"{r.altText} 맡기기", () => Begin(r, 1, true)), new DialogueUI.Choice("다음에", null));
                return;
            }
            if (can <= 0)
            {
                DialogueUI.Show(r.worker, r.hello + $"\n\n{(string.IsNullOrEmpty(r.inputText) ? "" : r.inputText + "개를 가져오면 ")}{r.productName} 하나를 만들어 줄게."
                    + (r.fee > 0 ? $" 삯은 {r.fee}냥." : "") + (r.hours > 0f ? $" {r.hours:0}시간 걸려." : ""));
                return;
            }
            var choices = new List<DialogueUI.Choice>();
            choices.Add(new DialogueUI.Choice($"{(r.hours > 0f ? "1번 맡기기" : "만들기")} ({cost})", () => Begin(r, 1, false)));
            if (can > 1) choices.Add(new DialogueUI.Choice($"{can}번 맡기기 ({r.productName} {can}개{(r.fee > 0 ? $", {r.fee * can}냥" : "")})", () => Begin(r, can, false)));
            if (altOk) choices.Add(new DialogueUI.Choice($"{r.altText} 맡기기", () => Begin(r, 1, true)));
            choices.Add(new DialogueUI.Choice("다음에", null));
            string left2 = r.dailyMax > 0 ? $" (오늘 {r.dailyMax - MadeToday(r)}개 더)" : "";
            DialogueUI.Show(r.worker, r.hello + "\n" + when + left2, choices.ToArray());
        }

        /// <summary>지금 재료·돈·하루 제한으로 몇 번 맡길 수 있는지</summary>
        static int Can(Recipe r, string[] inputs, int[] counts)
        {
            int can = r.maxBatch;
            for (int i = 0; i < inputs.Length; i++)
            {
                var it = GM.ItemById(inputs[i]);
                can = Mathf.Min(can, it != null ? GM.Player.cargo.Count(it) / counts[i] : 0);
            }
            if (r.fee > 0) can = Mathf.Min(can, GM.Player.money / r.fee);
            if (r.dailyMax > 0) can = Mathf.Min(can, r.dailyMax - MadeToday(r));
            return Mathf.Max(0, can);
        }

        static void Begin(Recipe r, int times, bool alt)
        {
            var inputs = alt ? r.altInputs : r.inputs; var counts = alt ? r.altCounts : r.counts;
            if (Can(r, inputs, counts) < times) { Notify.Show("재료나 돈이 모자라…", 2f); return; }
            float unit = r.fee;
            for (int i = 0; i < inputs.Length; i++)
            {
                var it = GM.ItemById(inputs[i]);
                unit += GM.Player.cargo.AverageCost(it) * counts[i];
                GM.Player.cargo.Remove(it, counts[i] * times);
            }
            GM.Player.money -= r.fee * times;
            if (r.dailyMax > 0) { Set(r, "day", GM.Clock.day); Set(r, "cnt", MadeToday(r) + times); }
            Set(r, "n", times);
            Set(r, "t", Now + r.hours);
            Set(r, "c", unit);   // 한 개당 원가 (장부 손익용)
            if (r.hours <= 0f) { Collect(r); return; }
            Sfx.Play("coin", .5f);
            Notify.Show($"{r.shop}에 맡겼다. {r.hours:0}시간 뒤에 {r.productName} {times}개를 찾아갈 수 있다.", 3f);
        }

        static void Collect(Recipe r)
        {
            var item = GM.ItemById(r.product);
            if (item == null) { Notify.Show($"(가공품 데이터가 없어요: {r.product})", 3f); return; }
            int n = Mathf.RoundToInt(Get(r, "n")), got = 0;
            float unit = Get(r, "c");
            while (got < n && GM.Player.CanCarry(item)) { GM.Player.cargo.Add(item, 1, unit); got++; }
            if (got == 0 && r.hours <= 0f)
            {
                // 바로 받는 공방은 실을 수 없으면 돈으로 (베네치아 규칙)
                GM.Player.money += Mathf.RoundToInt(item.basePrice) * n;
                Set(r, "n", 0);
                Notify.Show($"짐이 꽉 차서 {r.productName} 값 {Mathf.RoundToInt(item.basePrice) * n}냥을 받았다.", 3f);
                return;
            }
            if (got == 0) { Notify.Show("짐이 꽉 찼다. 짐을 덜고 다시 오자.", 2.5f); return; }
            Set(r, "n", n - got);
            Sfx.Play("success", .6f);
            Notify.Show(got < n ? $"{r.productName} {got}개를 챙겼다. 짐이 차서 {n - got}개는 맡겨 둠." : $"{r.productName} {got}개를 챙겼다!", 3f);
        }
    }

    /// <summary>맵에 놓는 공방 자리 (지역 담당이 공방을 더할 때 이걸 붙이고 recipeId만 적으면 된다)</summary>
    public class WorkshopSpot : Interactable
    {
        public string recipeId;
        public override string DisplayName => Crafting.Label(recipeId);
        public override Kind Category => Kind.Market;
        public override void Interact(PlayerController player) => Crafting.Open(recipeId);
    }
}
