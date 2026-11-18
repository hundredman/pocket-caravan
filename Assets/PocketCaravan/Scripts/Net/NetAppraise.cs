using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 진품 감정 (2026-11-18 윤채, 미니게임 기획 "타이밍이 아니라 눈썰미로 하는 놀이").
    /// 만국 상인회 마당 감정 탁자에서 Space. 떠돌이 상인이 교역품 두 개를 장터 값 절반쯤에 내놓는다.
    /// 왼쪽 상인회 견본(진품)과 오른쪽 내놓은 물건을 15초 동안 비교해 다른 곳을 눌러 찾는다. 다른 곳이 있으면 가짜다.
    /// - 가짜를 가짜라 하면: 상인회 수고비 25냥 + 감정 기록. 다른 곳 셋을 다 찾았으면 30% 확률로 희귀품.
    /// - 진품을 사면: 그 값에 짐으로 받는다(이익) + 감정 기록. 가짜를 사면 돈만 잃는다. 진품을 거절하면 아무 일 없다.
    /// - 게임 하루에 다섯 번까지 (메모 appraise:day · appraise:n). 감정 기록 다섯 번이면 칭호 「감정의 달인」(NetCollection).
    /// 서버는 쓰지 않는다 (혼자 놀이).
    /// </summary>
    public class NetAppraise : MonoBehaviour
    {
        const int PerDay = 5, Count = 2, Reward = 25;
        const float Time0 = 15f, Miss = 1.5f, Scale = 6f;
        enum Stage { Play, Result }

        static NetAppraise inst;
        bool open;
        Stage stage;
        ItemData item;
        int price;
        bool fake;
        readonly List<Vector2Int> diffs = new List<Vector2Int>();
        readonly List<Color32> diffColor = new List<Color32>();
        readonly List<bool> found = new List<bool>();
        readonly List<(Vector2 at, float t)> misses = new List<(Vector2, float)>();
        float startAt, penalty;
        string result, detail;
        bool good;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetAppraise>() != null) return;
            var go = new GameObject("NetAppraise");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetAppraise>();
        }

        void Awake() => inst = this;
        void OnDisable() => Close();

        static GameManager GM => GameManager.I;

        static int Left
        {
            get
            {
                if (GM == null) return 0;
                int day = GM.Clock.day;
                if (Mathf.RoundToInt(GM.GetMemo("appraise:day", -1f)) != day) return PerDay;
                return Mathf.Max(0, PerDay - Mathf.RoundToInt(GM.GetMemo("appraise:n", 0f)));
            }
        }

        static void Use()
        {
            int day = GM.Clock.day;
            if (Mathf.RoundToInt(GM.GetMemo("appraise:day", -1f)) != day) { GM.SetMemo("appraise:day", day); GM.SetMemo("appraise:n", 0f); }
            GM.SetMemo("appraise:n", GM.GetMemo("appraise:n", 0f) + 1f);
        }

        /// <summary>감정 탁자 (만국 상인회 마당)</summary>
        public static void Open()
        {
            if (inst == null || inst.open || GM == null) return;
            if (Left <= 0) { Notify.Show("오늘 감정은 다 했어요. 내일 또 떠돌이 상인이 와요", 3f); return; }
            if (!inst.NewRound()) { Notify.Show("오늘은 떠돌이 상인이 안 보여요", 3f); return; }
            if (!UIState.TryOpen(inst, inst.Close)) return;
            inst.open = true;
            Sfx.Play("open");
        }

        void Close()
        {
            if (!open) return;
            open = false;
            UIState.Release(this);
        }

        bool NewRound()
        {
            var pool = new List<ItemData>();
            foreach (var i in GM.Db.items)
                if (i != null && !i.questItem && i.icon != null && i.originTown != null && i.basePrice >= 12f) pool.Add(i);
            if (pool.Count == 0) return false;
            item = pool[Random.Range(0, pool.Count)];
            price = Mathf.Max(5, Mathf.RoundToInt(item.basePrice * Count * .45f));
            fake = Random.value < .55f;
            diffs.Clear(); diffColor.Clear(); found.Clear(); misses.Clear();
            if (fake)
            {
                Color32[] pal = { new Color32(42, 26, 46, 255), CuteUI.Deco, CuteUI.Up, CuteUI.Alert, CuteUI.White, CuteUI.Interact };
                int guard = 0;
                while (diffs.Count < 3 && guard++ < 200)
                {
                    var c = new Vector2Int(Random.Range(2, 14), Random.Range(2, 14));
                    bool near = false;
                    foreach (var d in diffs) if (Mathf.Abs(d.x - c.x) + Mathf.Abs(d.y - c.y) < 5) near = true;
                    if (near) continue;
                    diffs.Add(c); diffColor.Add(pal[Random.Range(0, pal.Length)]); found.Add(false);
                }
            }
            stage = Stage.Play;
            startAt = Time.unscaledTime; penalty = 0f;
            return true;
        }

        float TimeLeft => Mathf.Max(0f, Time0 - (Time.unscaledTime - startAt) - penalty);
        int FoundCount { get { int n = 0; foreach (var f in found) if (f) n++; return n; } }

        void Update()
        {
            if (!open) return;
            if (InputReader.CancelPressed()) { Close(); return; }
            if (stage == Stage.Play && TimeLeft <= 0f) Finish(null);
        }

        /// <summary>buy = true 산다 · false 가짜라 거절 · null 시간 끝</summary>
        void Finish(bool? buy)
        {
            Use();
            stage = Stage.Result;
            var p = GM.Player;
            if (buy == null)
            {
                good = false; result = "시간이 다 됐어요"; detail = "떠돌이 상인이 슬그머니 가 버렸어요";
                Sfx.Play("cancel");
                return;
            }
            if (fake && buy == false)
            {
                good = true;
                p.money += Reward;
                NetCollection.AddStat(NetCollection.StatAppraise);
                result = "감정 성공! 가짜였어요";
                detail = $"떠돌이 상인이 줄행랑을 쳤어요 · 상인회 수고비 +{Reward}냥";
                Sfx.Play("levelup");
                if (FoundCount >= diffs.Count && Random.value < .3f) { var r = NetCollection.RandomRare(); NetCollection.Give(r.id, "감정 솜씨에 반한 상인회가 줬어요"); }
            }
            else if (fake)
            {
                good = false;
                p.money -= price;
                result = "앗, 가짜였어요…";
                detail = $"{price}냥을 잃었어요. 다음엔 더 꼼꼼히 봐요";
                Sfx.Play("fail");
            }
            else if (buy == true)
            {
                good = true;
                p.money -= price;
                p.cargo.Add(item, Count, price / (float)Count);
                NetCollection.AddStat(NetCollection.StatAppraise);
                result = "진품이었어요!";
                detail = $"{item.displayName} {Count}개를 {price}냥에 샀어요 · 장터에 팔면 남아요";
                Sfx.Play("coin");
            }
            else
            {
                good = false;
                result = "진품이었는데…";
                detail = "좋은 물건을 놓쳤어요";
                Sfx.Play("cancel");
            }
            GM.SaveNow();
        }

        void OnGUI()
        {
            if (!open || GM == null || item == null) return;
            UIStyle.Begin();
            GUI.depth = -10;
            float w = Mathf.Min(400, UIStyle.VirtualWidth - 16), h = 262;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim(); CuteUI.Window(r); CuteUI.TitlePlate(r.center.x, r.y - 11, "진품 감정");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            // 떠돌이 상인의 말
            var say = new Rect(r.x + 16, r.y + 18, w - 32, 20);
            CuteUI.Box(say, 6, CuteUI.Paper2, CuteUI.Line, 1);
            CuteUI.Label(say.x + 8, say.center.y, TradeLedgerUI.Fit($"떠돌이 상인 「{item.displayName} {Count}개, {price}냥에 드리죠!」", say.width - 16, true, false), CuteUI.Text, true);

            // 견본 · 내놓은 물건
            float box = Scale * 16 + 16, gap = 26;
            float bx = Mathf.Round(r.center.x - box - gap / 2f), by = r.y + 52;
            var left = new Rect(bx, by, box, box);
            var right = new Rect(bx + box + gap, by, box, box);
            Panel(left, "상인회 견본 · 진품", false);
            Panel(right, "내놓은 물건", true);

            if (stage == Stage.Play)
            {
                // 남은 시간 · 찾은 곳
                float t = TimeLeft / Time0;
                var bar = new Rect(r.x + 24, right.yMax + 16, w - 48, 7);
                CuteUI.Bar(bar, t, t > .3f ? CuteUI.Interact : CuteUI.Alert, CuteUI.InteractHi);
                CuteUI.LabelCenter(r.center.x, bar.yMax + 9, $"다른 곳을 눌러 찾아요 · 다른 곳이 있으면 가짜 · 찾은 곳 {FoundCount}", CuteUI.Sub, true);
                // 단추
                bool canBuy = GM.Player.money >= price && GM.Player.CanCarry(item, Count);
                float bw = (w - 48 - 10) / 2f, yb = r.yMax - 36;
                if (CuteUI.SoftButton(new Rect(r.x + 24, yb, bw, 24), "가짜예요! 안 살래요")) Finish(false);
                var buyR = new Rect(r.x + 24 + bw + 10, yb, bw, 24);
                if (canBuy) { if (CuteUI.Button(buyR, $"진품이에요, 살게요 ({price}냥)")) Finish(true); }
                else
                {
                    CuteUI.Box(buyR, 8, CuteUI.ZeroSoft, CuteUI.Line, 1);
                    CuteUI.LabelCenter(buyR.center.x, buyR.center.y, GM.Player.money < price ? "돈이 모자라요" : "짐칸이 모자라요", CuteUI.Sub, true);
                }
                return;
            }

            // 결과
            var res = new Rect(r.x + 24, right.yMax + 10, w - 48, 34);
            CuteUI.Box(res, 6, good ? new Color32(0xE3, 0xF6, 0xE8, 255) : CuteUI.AlertSoft, good ? CuteUI.Up : CuteUI.Alert, 1);
            CuteUI.LabelCenter(res.center.x, res.y + 10, result, good ? new Color32(0x16, 0x7A, 0x35, 255) : CuteUI.Down, false, true);
            CuteUI.LabelCenter(res.center.x, res.y + 24, TradeLedgerUI.Fit(detail, res.width - 12, true, false), CuteUI.Text, true);
            float bw2 = (w - 48 - 10) / 2f, yb2 = r.yMax - 36;
            int left2 = Left;
            if (left2 > 0) { if (CuteUI.Button(new Rect(r.x + 24, yb2, bw2, 24), $"한 번 더 (오늘 {left2}번 남음)")) { if (NewRound()) Sfx.Play("open"); } }
            else { var nb = new Rect(r.x + 24, yb2, bw2, 24); CuteUI.Box(nb, 8, CuteUI.ZeroSoft, CuteUI.Line, 1); CuteUI.LabelCenter(nb.center.x, nb.center.y, "오늘은 끝 (내일 또 와요)", CuteUI.Sub, true); }
            if (CuteUI.SoftButton(new Rect(r.x + 24 + bw2 + 10, yb2, bw2, 24), "닫기")) Close();
        }

        /// <summary>물건 그림 칸. offered = 오른쪽(내놓은 물건): 가짜면 다른 곳을 그리고, 누르면 찾는다. 결과 화면에서는 정답을 보여 준다</summary>
        void Panel(Rect b, string caption, bool offered)
        {
            CuteUI.Box(b, 8, CuteUI.Paper, offered ? CuteUI.Deco : CuteUI.Line, offered ? 2 : 1);
            CuteUI.LabelCenter(b.center.x, b.y - 6, caption, CuteUI.Sub, true, true);
            float ix = b.x + 8, iy = b.y + 8;
            TradeLedgerUI.ItemIcon(item, ix, iy, (int)Scale);
            if (!offered) return;
            for (int i = 0; i < diffs.Count; i++)
            {
                var c = new Rect(ix + diffs[i].x * Scale, iy + diffs[i].y * Scale, Scale, Scale);
                CuteUI.Fill(c, diffColor[i]);
                if (found[i] || stage == Stage.Result) CuteUI.Box(new Rect(c.x - 6, c.y - 6, c.width + 12, c.height + 12), 8, new Color32(0, 0, 0, 0), found[i] ? CuteUI.Up : CuteUI.Alert, 2);
            }
            // 헛짚은 곳: 잠깐 ×
            for (int i = misses.Count - 1; i >= 0; i--)
            {
                float age = Time.unscaledTime - misses[i].t;
                if (age > .6f) { misses.RemoveAt(i); continue; }
                CuteUI.LabelCenter(misses[i].at.x, misses[i].at.y, "×", CuteUI.Alert, false, true);
            }
            if (stage != Stage.Play) return;
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && b.Contains(e.mousePosition))
            {
                var m = e.mousePosition;
                int hit = -1;
                for (int i = 0; i < diffs.Count; i++)
                {
                    var c = new Vector2(ix + (diffs[i].x + .5f) * Scale, iy + (diffs[i].y + .5f) * Scale);
                    if (!found[i] && Vector2.Distance(c, m) <= Scale * 1.8f) { hit = i; break; }
                }
                if (hit >= 0) { found[hit] = true; Sfx.Play("accept", .5f); }
                else { penalty += Miss; misses.Add((m, Time.unscaledTime)); Sfx.Play("bump", .5f); }
                e.Use();
            }
            GameCursor.Hover(b);
        }
    }
}
