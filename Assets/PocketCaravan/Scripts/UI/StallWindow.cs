using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 내 좌판 창 (PlayerStall). 맵 위 내 좌판에 말을 걸면 열린다.
    /// 위: 판 돈 [받기] · 진열 무게 · 지금까지 판 개수. 왼쪽: 진열한 물건(지금 이 장터 값, [내리기]). 오른쪽: 내 짐의 교역품([올리기] · [모두]).
    /// 아래: [손님 맞이] (하루 한 번, StallRush) + 도움말. 처음 열면 네 부분을 튜토리얼처럼 하나씩 밝힌다 (TutorialSpot "stall.*").
    /// </summary>
    public sealed class StallWindow : MonoBehaviour
    {
        static StallWindow inst;
        bool open; int openedFrame; string message = "";
        float scrollL, scrollR;

        public static bool Showing => inst != null && inst.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StallWindow>() != null) return;
            var go = new GameObject("StallWindow"); DontDestroyOnLoad(go); inst = go.AddComponent<StallWindow>();
        }

        public static void Show()
        {
            if (inst == null || inst.open || !PlayerStall.Has(GameManager.I?.Player)) return;
            if (!UIState.TryOpen(inst, inst.Close)) return;
            inst.open = true; inst.openedFrame = Time.frameCount; inst.message = "";
            var p = GameManager.I.Player;
            if (p.stallEarned > 0) inst.message = $"좌판에서 판 돈 {p.stallEarned}냥이 모여 있어요. [받기]를 눌러요.";
            Sfx.Play("open");
        }

        void Close() { open = false; UIState.Release(this); StallRush.ForceClose(); }
        void OnDisable() { if (open) Close(); }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame || StallRush.IsOpen || Time.frameCount == StallRush.ClosedFrame) return;   // 손님 맞이를 닫은 그 키로 좌판 창까지 닫지 않게
            if (TutorialSpot.HoldsWindow) return;   // 처음 설명 중엔 키로 닫지 않는다
            if (InputReader.CancelPressed() || InputReader.InteractPressed()) Close();
        }

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin();
            GUI.depth = -10;
            var gm = GameManager.I; var p = gm.Player;
            if (!PlayerStall.Has(p)) { Close(); return; }

            float w = Mathf.Min(520, UIStyle.VirtualWidth - 16), h = Mathf.Min(290, UIStyle.VirtualHeight - 34);
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "내 좌판 · " + PlayerStall.TownName(p, gm.Db));
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9) && !StallRush.IsOpen) { Close(); return; }

            // 위 줄: 판 돈 · 진열 무게 · 판 개수
            float hy = r.y + 26;
            var earn = new Rect(r.x + 14, hy - 10, 150, 20);
            ScreenZones.MarkSpot("stall.earn", earn);
            CuteUI.Icon(CuteUI.Pic.Coin, r.x + 18, hy - 5);
            float x = CuteUI.Label(r.x + 31, hy, "판 돈", CuteUI.Sub, true);
            x = CuteUI.Label(x + 4, hy, $"{p.stallEarned}냥", p.stallEarned > 0 ? CuteUI.Up : CuteUI.Text, false, true);
            var take = new Rect(x + 6, hy - 9, 40, 18);
            if (p.stallEarned > 0) { if (CuteUI.Button(take, "받기") && !StallRush.IsOpen) { int got = PlayerStall.Collect(p); message = $"판 돈 {got}냥을 받았어요."; Sfx.Play("coin"); gm.SaveNow(); } }
            else { CuteUI.Box(take, 7, CuteUI.ZeroSoft, CuteUI.Line); CuteUI.LabelCenter(take.center.x, take.center.y, "받기", CuteUI.Zero, true); }
            float wt = PlayerStall.Weight(p);
            string wl = $"진열 {wt:0.#}/{PlayerStall.Capacity:0}";
            float rx = r.xMax - 16 - CuteUI.Width(wl, true);
            CuteUI.Label(rx, hy, wl, CuteUI.Sub, true);
            CuteUI.Bar(new Rect(rx - 64, hy - 4, 58, 8), wt / PlayerStall.Capacity, CuteUI.Sell, CuteUI.SellSoft);
            CuteUI.Label(take.xMax + 12, hy, $"지금까지 {p.stallSold}개 팔림", CuteUI.Sub, true);

            // 두 칸: 진열 / 내 짐
            float top = r.y + 44, bottom = r.yMax - 52;
            float half = Mathf.Floor((w - 36) / 2f);
            var left = new Rect(r.x + 14, top, half, bottom - top);
            var right = new Rect(left.xMax + 8, top, half, bottom - top);
            ScreenZones.MarkSpot("stall.shelf", left); ScreenZones.MarkSpot("stall.cargo", right);
            DrawShelf(gm, p, left);
            DrawCargo(gm, p, right);

            // 아래: 손님 맞이 + 도움말
            var rush = new Rect(r.x + 14, r.yMax - 46, 150, 22);
            ScreenZones.MarkSpot("stall.rush", rush);
            string why = RushDenial(gm);
            if (why == null) { if (CuteUI.Button(rush, "손님 맞이", CuteUI.Pic.People) && !StallRush.IsOpen) StallRush.Open(); }
            else { CuteUI.Box(rush, 8, CuteUI.ZeroSoft, CuteUI.Line); CuteUI.LabelCenter(rush.center.x, rush.center.y, "손님 맞이", CuteUI.Zero, false, true); }
            string tip = why ?? "하루 한 번 손님이 몰려와요. 직접 팔면 2할 더 받아요";
            CuteUI.Label(rush.xMax + 10, rush.center.y, TradeLedgerUI.Fit(tip, r.xMax - rush.xMax - 26, true, false), CuteUI.Sub, true);
            string note = string.IsNullOrEmpty(message) ? "올려 둔 물건은 내가 없어도 한두 시간마다 이 장터 시세로 팔려요" : message;
            CuteUI.Icon(CuteUI.Pic.Bulb, r.x + 16, r.yMax - 22);
            CuteUI.Label(r.x + 27, r.yMax - 17, TradeLedgerUI.Fit(note, w - 44, true, false), string.IsNullOrEmpty(message) ? CuteUI.Sub : CuteUI.Text, true);
        }

        /// <summary>손님 맞이를 못 하는 까닭 (할 수 있으면 null)</summary>
        public static string RushDenial(GameManager gm)
        {
            var p = gm.Player;
            if (PlayerStall.Count(p) <= 0) return "물건을 올려 두면 손님이 와요";
            if (p.stallRushDay == gm.Clock.day) return "오늘 손님은 다 왔어요. 내일 또 와요";
            int hr = gm.Clock.Hour;
            if (hr < 7 || hr >= 21) return "밤에는 손님이 없어요 (아침 7시부터)";
            return null;
        }

        void DrawShelf(GameManager gm, PlayerState p, Rect a)
        {
            CuteUI.Box(a, 7, CuteUI.Paper, CuteUI.Ink);
            CuteUI.Label(a.x + 8, a.y + 9, "진열한 물건", CuteUI.Sub, true, true);
            var rows = new List<KeyValuePair<ItemData, int>>(p.stallStock.Items);
            var area = new Rect(a.x + 4, a.y + 18, a.width - 8, a.height - 22);
            const float rh = 23f;
            scrollL = TradeLedgerUI.Scroll(area, scrollL, rows.Count * rh);
            GUI.BeginGroup(area);
            float y = -scrollL, cw = area.width - (rows.Count * rh > area.height ? 7 : 0);
            if (rows.Count == 0) { CuteUI.LabelCenter(cw / 2f, 26, "비어 있어요", CuteUI.Text, true, true); CuteUI.LabelCenter(cw / 2f, 40, "오른쪽 내 짐에서 올려요", CuteUI.Sub, true); }
            foreach (var kv in rows)
            {
                var row = new Rect(0, y, cw, rh - 3);
                CuteUI.Box(row, 6, CuteUI.Paper2, CuteUI.Line);
                TradeLedgerUI.ItemIcon(kv.Key, 4, y + 2, 1);
                float lx = CuteUI.Label(24, y + 10, TradeLedgerUI.Fit(kv.Key.displayName, cw - 175, true, true), CuteUI.Text, true, true);
                CuteUI.Label(lx + 4, y + 10, $"×{kv.Value}", CuteUI.Sub, true);
                var b = new Rect(cw - 78, y + 2, 74, 16);
                CuteUI.LabelRight(b.x - 6, y + 10, $"하나 {PlayerStall.PriceNow(gm, kv.Key)}냥", CuteUI.Up, true);
                int pick = Segmented(b, "내리기", "모두", false);
                if (pick > 0 && !StallRush.IsOpen)
                {
                    int n = PlayerStall.Take(p, kv.Key, pick == 1 ? 1 : kv.Value);
                    int want = pick == 1 ? 1 : kv.Value; message = n <= 0 ? "짐 자리가 없어요." : n < want ? $"짐이 가득해 {n}개만 내렸어요." : $"{Josa.Eul(kv.Key.displayName)} {n}개 짐으로 내렸어요.";
                    Sfx.Play(n > 0 ? "coin" : "cancel");
                }
                y += rh;
            }
            GUI.EndGroup();
        }

        void DrawCargo(GameManager gm, PlayerState p, Rect a)
        {
            CuteUI.Box(a, 7, CuteUI.Paper, CuteUI.Ink);
            CuteUI.Label(a.x + 8, a.y + 9, "내 짐", CuteUI.Sub, true, true);
            var rows = new List<KeyValuePair<ItemData, int>>();
            foreach (var kv in p.cargo.Items) if (kv.Key != null && !kv.Key.questItem && kv.Value > 0) rows.Add(kv);
            var area = new Rect(a.x + 4, a.y + 18, a.width - 8, a.height - 22);
            const float rh = 23f;
            scrollR = TradeLedgerUI.Scroll(area, scrollR, rows.Count * rh);
            GUI.BeginGroup(area);
            float y = -scrollR, cw = area.width - (rows.Count * rh > area.height ? 7 : 0);
            if (rows.Count == 0) { CuteUI.LabelCenter(cw / 2f, 26, "올릴 교역품이 없어요", CuteUI.Text, true, true); CuteUI.LabelCenter(cw / 2f, 40, "장터에서 사 오면 올릴 수 있어요", CuteUI.Sub, true); }
            foreach (var kv in rows)
            {
                var row = new Rect(0, y, cw, rh - 3);
                CuteUI.Box(row, 6, CuteUI.Paper2, CuteUI.Line);
                TradeLedgerUI.ItemIcon(kv.Key, 4, y + 2, 1);
                float lx = CuteUI.Label(24, y + 10, TradeLedgerUI.Fit(kv.Key.displayName, cw - 170, true, true), CuteUI.Text, true, true);
                CuteUI.Label(lx + 4, y + 10, $"×{kv.Value}", CuteUI.Sub, true);
                var seg = new Rect(cw - 78, y + 2, 74, 16);
                CuteUI.LabelRight(seg.x - 6, y + 10, $"무게 {kv.Key.weight:0.#} · 여기 {PlayerStall.PriceNow(gm, kv.Key)}냥", CuteUI.Sub, true);
                bool room = PlayerStall.Weight(p) + kv.Key.weight <= PlayerStall.Capacity + 0.001f;
                if (room)
                {
                    int pick = Segmented(seg, "올리기", "모두", true), n = 0;
                    if (pick > 0 && !StallRush.IsOpen) n = PlayerStall.Put(p, kv.Key, pick == 1 ? 1 : kv.Value);
                    if (n > 0) { message = $"{Josa.Eul(kv.Key.displayName)} {n}개 진열했어요."; Sfx.Play("coin"); }
                }
                else { CuteUI.Box(seg, 7, CuteUI.ZeroSoft, CuteUI.Line); CuteUI.LabelCenter(seg.center.x, seg.center.y, "자리 없음", CuteUI.Zero, true); }
                y += rh;
            }
            GUI.EndGroup();
        }

        /// <summary>11/19 은샘 "내리기·올리기·모두가 대충 만든 것 같아": 한 덩어리 두 칸 단추 [하나 | 모두].
        /// primary면 청록(올리기), 아니면 흰 종이(내리기). 마우스를 올린 칸만 밝게. 누른 칸 1·2, 아니면 0</summary>
        static int Segmented(Rect r, string a, string b, bool primary)
        {
            float split = Mathf.Round(r.x + r.width * 0.58f);
            var ra = new Rect(r.x, r.y, split - r.x, r.height); var rb = new Rect(split, r.y, r.xMax - split, r.height);
            var mouse = Event.current.mousePosition;
            Color32 fill = primary ? CuteUI.Interact : CuteUI.White, hi = primary ? CuteUI.InteractHi : CuteUI.Paper2;
            Color32 line = primary ? CuteUI.InteractLo : CuteUI.KeyLine, text = primary ? CuteUI.White : CuteUI.Text;
            CuteUI.Box(r, 7, fill, line, 1, 2, primary ? CuteUI.InteractLo : CuteUI.Line);
            if (ra.Contains(mouse)) CuteUI.Box(new Rect(ra.x + 1, ra.y + 1, ra.width - 1, ra.height - 2), 6, hi, hi);
            if (rb.Contains(mouse)) CuteUI.Box(new Rect(rb.x, rb.y + 1, rb.width - 1, rb.height - 2), 6, hi, hi);
            CuteUI.Fill(new Rect(split, r.y + 3, 1, r.height - 6), line);
            CuteUI.LabelCenter(ra.center.x, ra.center.y, a, primary && !ra.Contains(mouse) ? text : primary ? CuteUI.Text : text, true, true);
            CuteUI.LabelCenter(rb.center.x, rb.center.y, b, primary && !rb.Contains(mouse) ? text : primary ? CuteUI.Text : CuteUI.Sub, true, !primary ? false : true);
            if (CuteUI.Hit(ra, GUIContent.none, GUIStyle.none)) return 1;
            if (CuteUI.Hit(rb, GUIContent.none, GUIStyle.none)) return 2;
            return 0;
        }
    }
}
