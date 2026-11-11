using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 위 가운데에 진행 중 퀘스트와 지금 할 일을 보여 준다 (Tab이면 오른쪽 위 단추 아래 자세히). Q로 퀘스트 목록을 연다.
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class QuestTracker : MonoBehaviour
    {
        bool logOpen;
        int logTab;
        Vector2 scroll;

        void Update()
        {
            if (GameManager.I == null) return;
            GameManager.I.Quests.Tick();
            if (logOpen && (InputReader.QuestLogPressed() || InputReader.CancelPressed())) CloseLog();
            else if (InputReader.QuestLogPressed() && !UIState.Blocking && !UIState.JustClosed && !GameManager.I.IsTravelling
                && UIState.TryOpen(this, CloseLog)) logOpen = true;
            else if (logOpen)
            {
                // 11/08 ←→ 탭 바꾸기 · ↑↓ 넘기기 (마우스는 탭 단추 · 휠 · 막대)
                var mv = InputReader.MovePressed();
                if (mv.x != 0) { logTab = (logTab + mv.x + LogTabs.Length) % LogTabs.Length; scroll = Vector2.zero; Sfx.Play("open", .25f); }
                if (mv.y != 0) scroll.y = Mathf.Max(0f, scroll.y - mv.y * 40f);
            }
        }
        static readonly string[] LogTabs = { "의뢰", "성장·시설", "주변 거래처" };
        void CloseLog() { logOpen = false; UIState.Release(this); }
        void OnDisable() { if (logOpen) CloseLog(); }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UIStyle.Begin();
            GUI.depth = logOpen ? -10 : 0;
            Notify.Draw(GoryeoIntro.ReservedGuideRect);
            if (logOpen) DrawLog(gm.Quests); else DrawTracker(gm.Quests);
        }

        /// <summary>위 가운데 할 일 줄이 떠 있는지 (다음 이야기 줄·알림이 그 아래로 비킨다)</summary>
        public static bool LineVisible
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null || gm.Quests == null || MapRoot.Current == null || HUD.Expanded) return false;
                var focus = QuestFocus.Current;   // 11/11 DrawCompact와 같은 조건
                if (focus == QuestFocus.Kind.Event || focus == QuestFocus.Kind.Lead) return false;
                return (focus == QuestFocus.Kind.Quest && gm.Quests.Tracked != null) || gm.Player.contracts.Count > 0;
            }
        }

        /// <summary>
        /// 10/28 평소엔 할 일 한 줄. Tab(HUD.Expanded)으로 펼치면 아래 자세히.
        /// 11/05 B안: 화면 위 가운데 알약 (두루마리 + 지금 할 일 + 퀘스트 이름). 오른쪽 위는 단추 자리.
        /// </summary>
        void DrawCompact(QuestManager qm)
        {
            var gm = GameManager.I;
            var focus = QuestFocus.Current;   // 11/11 은샘: 위 알약은 길 안내 대상 하나만 (이벤트·다음 이야기는 각자 그린다)
            if (focus == QuestFocus.Kind.Event || focus == QuestFocus.Kind.Lead) return;
            if (focus == QuestFocus.Kind.Quest && qm.Tracked != null)
            {
                int others = 0; foreach (var ap in qm.Active) if (ap != qm.Tracked) others++;
                string tail = qm.Tracked.quest.title + (others > 0 ? $" 외 {others}개" : "");   // 11/01 진행 중인 다른 퀘스트 수 (Q 수첩에서 안내 바꾸기)
                CuteUI.TopPill(6, false, CuteUI.Pic.Scroll, CuteUI.Plain(qm.ProgressText(qm.Tracked)), tail, false);
            }
            else if (gm.Player.contracts.Count > 0)
            {
                var c = gm.Player.contracts[0];
                string tail = CuteUI.Plain(gm.Contracts.TimeLeftText(c)) + (gm.Player.contracts.Count > 1 ? $" 외 {gm.Player.contracts.Count - 1}건" : "");
                CuteUI.TopPill(6, false, CuteUI.Pic.Sack, $"{c.cargo} → {gm.Contracts.TownName(c.toTown)}", tail, false);
            }
        }

        /// <summary>Tab으로 펼쳤을 때: 오른쪽 위 단추 아래 카드에 퀘스트·배달 자세히</summary>
        void DrawTracker(QuestManager qm)
        {
            if (!HUD.Expanded) { DrawCompact(qm); return; }
            var gm = GameManager.I;
            const float w = 212f;
            float x = UIStyle.VirtualWidth - w - 6, inner = w - 20;
            var lines = new List<(string text, bool small, bool bold, Color32 col, float indent)>();
            bool any = false;
            if (qm.Tracked != null)
            {
                any = true;
                lines.Add((qm.Tracked.quest.title, false, true, CuteUI.Text, 0));
                lines.Add(("· " + CuteUI.Plain(qm.ProgressText(qm.Tracked)), true, false, CuteUI.Text, 6));
            }
            foreach (var c in gm.Player.contracts)
            {
                any = true;
                string state = c.dropped ? "떨어뜨림!" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                lines.Add(($"{c.cargo} → {gm.Contracts.TownName(c.toTown)}", false, true, CuteUI.Text, 0));
                lines.Add(($"· {CuteUI.Plain(gm.Contracts.TimeLeftText(c))} · {state} · 보수 {c.reward}냥", true, false, c.dropped ? CuteUI.Alert : CuteUI.Text, 6));
            }
            var next = StoryGuide.Next;
            if (next.HasValue)
            {
                any = true;
                lines.Add(("다음 이야기", true, false, CuteUI.Alert, 0));
                lines.Add((next.Value.action + (string.IsNullOrEmpty(next.Value.where) ? "" : " · " + next.Value.where), false, true, CuteUI.Text, 0));
            }
            if (!any) return;   // 11/05 보일 게 없으면 카드를 띄우지 않는다 (예전 "자유 교역 · ! 의뢰…" 안내는 뺐다)

            float y = 44 + ObstacleField.TopRightUsed;   // 길 장애 알약이 있으면 그 아래
            // 11/08 아래 조작 안내 위까지만 (좁은 고려 화면에서 카드가 화면 밖으로 나갔다). 제목·자세히 두 줄씩 통째로 넣고 남는 것은 뺀다
            float room = UIStyle.VirtualHeight - 6 - HUD.BottomReserved - y, h = 14;
            int shown = 0;
            for (int i = 0; i < lines.Count; i += 2)
            {
                float pair = 0;
                for (int k = i; k < Mathf.Min(i + 2, lines.Count); k++) pair += CuteUI.WrapHeight(lines[k].text, inner - lines[k].indent, lines[k].small) + 2;
                if (shown > 0 && h + pair > room) break;
                h += pair;
                shown = Mathf.Min(i + 2, lines.Count);
            }
            CuteUI.Card(new Rect(x, y, w, h));
            y += 8;
            for (int i = 0; i < shown; i++) { var l = lines[i]; y += CuteUI.Wrap(x + 10 + l.indent, y, inner - l.indent, l.text, l.col, l.small, l.bold) + 2; }
        }

        // 11/08 은샘: 여정 수첩을 장부·게시판과 같은 모양으로 (아이콘 탭 + 종이 칸 + 카드).
        // 의뢰 = 할 일·보상을 한눈에, 비면 다음에 할 만한 일. 성장 = 다음 신용까지 막대와 열리는 시설. 거래처 = 거리·특산품·시세 기록.
        float logContentH;
        static readonly CuteUI.Pic[] LogPics = { CuteUI.Pic.Scroll, CuteUI.Pic.Star4, CuteUI.Pic.Pin };

        void DrawLog(QuestManager qm)
        {
            float w = Mathf.Min(500, UIStyle.VirtualWidth - 16), h = Mathf.Min(286, UIStyle.VirtualHeight - 34);
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "여정 수첩");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Q") - 3, r.y - 3, "Q");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { CloseLog(); return; }

            var content = new Rect(r.x + 15, r.y + 50, w - 30, h - 50 - 24);
            CuteUI.Box(content, 7, CuteUI.Paper, CuteUI.Ink);
            float tx = content.x + 6;
            for (int i = 0; i < LogTabs.Length; i++)
            {
                if (CuteUI.Tab(tx, content.y, LogTabs[i], LogPics[i], logTab == i, out float tw) && logTab != i) { logTab = i; scroll = Vector2.zero; }
                tx += tw + 3;
            }
            var gm = GameManager.I;
            int qn = 0; foreach (var _ in qm.Active) qn++;
            string sub = logTab == 0 ? $"맡은 일 {qn + gm.Player.contracts.Count}개" : logTab == 1 ? "신용이 오르면 시설이 열려요" : "이 맵에서 갈 수 있는 거래처";
            CuteUI.LabelRight(content.xMax - 4, content.y - 9, sub, CuteUI.Sub, true);

            var area = new Rect(content.x + 6, content.y + 6, content.width - 12, content.height - 10);
            scroll.y = TradeLedgerUI.Scroll(area, scroll.y, logContentH);
            GUI.BeginGroup(area);
            float cw = area.width - (logContentH > area.height ? 7 : 0);
            float end = logTab == 0 ? DrawQuests(qm, gm, -scroll.y, cw, area.height) : logTab == 1 ? DrawGrowth(gm, -scroll.y, cw) : DrawTowns(gm, -scroll.y, cw);
            logContentH = end + scroll.y;
            GUI.EndGroup();
            CuteUI.LabelCenter(r.center.x, r.yMax - 13, "←→ 탭 · ↑↓·휠 넘기기 · Q·Esc 닫기", CuteUI.Sub, true);
        }

        static float Pill(float x, float cy, string text, CuteUI.BadgeKind k)
        {
            Color32 fill, line, ink;
            switch (k)
            {
                case CuteUI.BadgeKind.On: fill = CuteUI.Interact; line = CuteUI.InteractLo; ink = CuteUI.White; break;
                case CuteUI.BadgeKind.Ok: fill = new Color32(0xE3, 0xF6, 0xE8, 255); line = CuteUI.Up; ink = new Color32(0x16, 0x7A, 0x35, 255); break;
                case CuteUI.BadgeKind.Alert: fill = CuteUI.AlertSoft; line = CuteUI.Alert; ink = CuteUI.Down; break;
                case CuteUI.BadgeKind.Note: fill = CuteUI.Paper; line = CuteUI.Line; ink = CuteUI.Sub; break;
                default: fill = CuteUI.ZeroSoft; line = CuteUI.KeyLine; ink = CuteUI.Sub; break;
            }
            return ContractBoard.Pill(x, cy, text, fill, line, ink, true);
        }

        static float Section(float y, string label, string count = null)
        {
            float x = CuteUI.Label(3, y + 8, label, CuteUI.Sub, true, true);
            if (count != null) CuteUI.Label(x + 5, y + 8, count, CuteUI.Sub, true);
            return y + 17;
        }

        // ---------------- 의뢰 ----------------
        /// <summary>11/11 은샘: 카드 오른쪽 위 단추. 안내 중이면 청록 "안내 중"(누르면 끔), 아니면 "길 안내". 단추 왼쪽 x를 돌려준다</summary>
        static float FocusButton(float right, float y, bool on, System.Action click)
        {
            if (on)
            {
                float x = Pill(right, y + 12, "안내 중", CuteUI.BadgeKind.On);
                if (GUI.Button(new Rect(x, y + 4, right - x, 17), new GUIContent("", "누르면 길 안내를 끕니다"), GUIStyle.none)) click();
                return x;
            }
            var b = new Rect(right - 60, y + 4, 60, 17);
            if (CuteUI.SoftButton(b, "길 안내")) click();
            return b.x;
        }

        /// <summary>
        /// 11/11 은샘: 받은 퀘스트 말고도 길 안내를 고를 수 있는 카드 둘.
        /// 이벤트(만국 상인회)는 금빛 카드 + "이벤트" 표, 다음 이야기(튜토리얼 뒤 첫 일거리 등)는 받기 전 퀘스트로.
        /// </summary>
        float DrawSpecialCards(float y, float w)
        {
            var focus = QuestFocus.Current;
            if (NetGuild.Active)
            {
                var (main, where, _) = NetGuild.Summary();
                bool on = focus == QuestFocus.Kind.Event;
                float ch = 22 + 14 + 14 + 8;
                var card = new Rect(0, y, w, ch);
                CuteUI.Box(card, 7, CuteUI.EventPaper, CuteUI.EventLine, 2, 2);
                float right = FocusButton(w - 7, y, on, () => QuestFocus.Toggle(QuestFocus.Kind.Event));
                right = ContractBoard.Pill(right - 4, y + 12, "★ 이벤트", CuteUI.Deco, CuteUI.EventLine, CuteUI.Text, true);
                CuteUI.Label(10, y + 12, TradeLedgerUI.Fit("만국 큰 장으로 · 만국 상인회", right - 16, false, true), CuteUI.Text, false, true);
                CuteUI.Icon(CuteUI.Pic.Arrow, 10, y + 25, 1, CuteUI.EventLine);
                CuteUI.Label(22, y + 29, TradeLedgerUI.Fit(CuteUI.Plain(main), w - 32, true, true), CuteUI.Text, true, true);
                CuteUI.Icon(CuteUI.Pic.Pin, 22, y + 39, 1, CuteUI.Sub);
                CuteUI.Label(30, y + 43, TradeLedgerUI.Fit(where, w - 40, true, false), CuteUI.Sub, true);
                y += ch + 5;
            }
            var lead = StoryGuide.Pending;
            if (lead.HasValue && StoryGuide.LeadAvailable)
            {
                bool on = focus == QuestFocus.Kind.Lead;
                float ch = 22 + 14 + 14 + 8;
                var card = new Rect(0, y, w, ch);
                CuteUI.Box(card, 7, on ? CuteUI.Paper : CuteUI.Paper2, on ? CuteUI.Interact : CuteUI.Line, on ? 2 : 1);
                float right = FocusButton(w - 7, y, on, () => QuestFocus.Toggle(QuestFocus.Kind.Lead));
                right = Pill(right - 4, y + 12, "받기 전", CuteUI.BadgeKind.Note);
                CuteUI.Label(10, y + 12, TradeLedgerUI.Fit(StoryGuide.LeadTitle, right - 16, false, true), CuteUI.Text, false, true);
                CuteUI.Icon(CuteUI.Pic.Arrow, 10, y + 25, 1, CuteUI.Interact);
                CuteUI.Label(22, y + 29, TradeLedgerUI.Fit(lead.Value.action, w - 32, true, true), CuteUI.Text, true, true);
                if (!string.IsNullOrEmpty(lead.Value.where))
                {
                    CuteUI.Icon(CuteUI.Pic.Pin, 22, y + 39, 1, CuteUI.Sub);
                    CuteUI.Label(30, y + 43, TradeLedgerUI.Fit(lead.Value.where, w - 40, true, false), CuteUI.Sub, true);
                }
                y += ch + 5;
            }
            return y;
        }

        float DrawQuests(QuestManager qm, GameManager gm, float y, float w, float viewH)
        {
            bool any = NetGuild.Active || StoryGuide.LeadAvailable;
            y = DrawSpecialCards(y, w);
            int qn = 0; foreach (var _ in qm.Active) qn++;
            if (qn > 0) y = Section(y, "이야기 의뢰", qn + "개");
            foreach (var p in qm.Active)
            {
                any = true;
                bool on = p == qm.Tracked && QuestFocus.Current == QuestFocus.Kind.Quest;
                string progress = CuteUI.Plain(qm.ProgressText(p));
                string summary = string.IsNullOrEmpty(p.quest.summary) ? null : CuteUI.Plain(p.quest.summary);
                float inner = w - 20;
                float ph = CuteUI.WrapHeight(progress, inner - 12, true), sh = summary != null ? CuteUI.WrapHeight(summary, inner, true) : 0;
                bool reward = p.RewardMoney > 0 || p.RewardCredit > 0;
                float ch = 24 + ph + (summary != null ? sh + 3 : 0) + (reward ? 18 : 0) + 6;
                var card = new Rect(0, y, w, ch);
                CuteUI.Box(card, 7, on ? CuteUI.Paper : CuteUI.Paper2, on ? CuteUI.Interact : CuteUI.Line, on ? 2 : 1);
                // 오른쪽 위: 안내 중(누르면 끔) / 길 안내
                var pp = p;
                float right = FocusButton(w - 7, y, on, () => { if (on) QuestFocus.Set(QuestFocus.Kind.None); else { qm.Track(pp); QuestFocus.Set(QuestFocus.Kind.Quest); } });
                if (!string.IsNullOrEmpty(p.quest.chain)) right = Pill(right - 4, y + 12, p.quest.chain, CuteUI.BadgeKind.Note);
                CuteUI.Label(10, y + 12, TradeLedgerUI.Fit(p.quest.title, right - 16, false, true), CuteUI.Text, false, true);
                float yy = y + 22;
                CuteUI.Icon(CuteUI.Pic.Arrow, 10, yy + 3, 1, CuteUI.Interact);
                CuteUI.Wrap(22, yy, inner - 12, progress, CuteUI.Text, true, true);
                yy += ph + 2;
                if (summary != null) { CuteUI.Wrap(10, yy, inner, summary, CuteUI.Sub, true); yy += sh + 3; }
                if (reward)
                {
                    float rx = CuteUI.Label(10, yy + 8, "보상", CuteUI.Sub, true) + 6;
                    if (p.RewardMoney > 0)
                    {
                        CuteUI.Icon(CuteUI.Pic.Coin7, rx, yy + 4);
                        rx = CuteUI.Label(rx + 10, yy + 8, p.RewardMoney + "냥", CuteUI.Up, true, true) + 6;
                    }
                    if (p.RewardCredit > 0) ContractBoard.Pill(rx, yy + 8, "신용 +" + p.RewardCredit, CuteUI.Interact, CuteUI.InteractLo, CuteUI.White);
                }
                y += ch + 5;
            }
            if (gm.Player.contracts.Count > 0)
            {
                y = Section(y + 2, "게시판 배달", $"{gm.Player.contracts.Count}/{ContractManager.MaxActive}");
                foreach (var c in gm.Player.contracts)
                {
                    any = true;
                    CuteUI.Box(new Rect(0, y, w, 36), 7, CuteUI.Paper2, c.dropped ? CuteUI.Alert : CuteUI.Line);
                    CuteUI.Circle(6, y + 5, 26, CuteUI.Paper, CuteUI.Line);
                    CuteUI.Icon(CuteUI.Pic.Sack, 13, y + 13);
                    string pay = c.atonement ? $"보증금 {c.deposit}냥" : $"{c.reward}냥";
                    CuteUI.LabelRight(w - 8, y + 12, pay, c.atonement ? CuteUI.Text : CuteUI.Up, false, true);
                    if (!c.atonement) CuteUI.Icon(CuteUI.Pic.Coin7, w - 8 - CuteUI.Width(pay, false, true) - 10, y + 8);
                    CuteUI.LabelRight(w - 8, y + 26, "보수", CuteUI.Sub, true);
                    float tx2 = CuteUI.Label(38, y + 12, TradeLedgerUI.Fit(c.cargo, w - 160, false, true), CuteUI.Text, false, true);
                    if (c.dropped) ContractBoard.Pill(tx2 + 5, y + 12, "길에 떨어뜨림", CuteUI.AlertSoft, CuteUI.Alert, CuteUI.Down);
                    CuteUI.Icon(CuteUI.Pic.Pin, 38, y + 22, 1, CuteUI.Sub);
                    CuteUI.Label(46, y + 26, $"{gm.Contracts.TownName(c.toTown)} · 남은 시간 {CuteUI.Plain(gm.Contracts.TimeLeftText(c))} · 상태 {Mathf.RoundToInt(c.condition * 100)}%", CuteUI.Sub, true);
                    y += 40;
                }
            }
            if (!any) y = DrawNoQuests(gm, y, w, viewH);
            return y;
        }

        float DrawNoQuests(GameManager gm, float y, float w, float viewH)
        {
            CuteUI.Circle(w / 2f - 24, y + 6, 48, CuteUI.Paper2, CuteUI.Line);
            CuteUI.Icon(CuteUI.Pic.Scroll, w / 2f - 13, y + 12, 2);
            CuteUI.LabelCenter(w / 2f, y + 66, "진행 중인 의뢰가 없어요", CuteUI.Text, false, true);
            CuteUI.LabelCenter(w / 2f, y + 82, "이런 일부터 해 보세요", CuteUI.Sub, true);
            y += 96;
            var next = StoryGuide.Pending;
            if (next.HasValue) y = Idea(y, w, CuteUI.Pic.Star4, "다음 이야기", next.Value.action + (string.IsNullOrEmpty(next.Value.where) ? "" : " · " + next.Value.where), true);
            y = Idea(y, w, CuteUI.Pic.Scroll, "의뢰 게시판", "마을 게시판에서 배달을 맡으면 보수와 신용을 받아요", false);
            y = Idea(y, w, CuteUI.Pic.Coin7, "자유 교역", "싸게 사서 비싸게 팔아도 돼요 · 값은 장부(L)에 적혀요", false);
            return y;
        }

        static float Idea(float y, float w, CuteUI.Pic pic, string title, string text, bool alert)
        {
            var row = new Rect(20, y, w - 40, 26);
            CuteUI.Box(row, 7, alert ? CuteUI.AlertSoft : CuteUI.Paper2, alert ? CuteUI.Alert : CuteUI.Line);
            var ps = CuteUI.PicSize(pic);
            CuteUI.Icon(pic, row.x + 12 - ps.x / 2f, row.center.y - ps.y / 2f);
            float x = CuteUI.Label(row.x + 24, row.center.y, title, alert ? CuteUI.Down : CuteUI.Text, true, true);
            CuteUI.Label(x + 8, row.center.y, TradeLedgerUI.Fit(text, row.xMax - 8 - (x + 8), true, false), CuteUI.Sub, true);
            return y + 30;
        }

        // ---------------- 성장·시설 ----------------
        float DrawGrowth(GameManager gm, float y, float w)
        {
            var p = gm.Player;
            var map = MapRoot.Current;
            var towns = JourneyGuide.Destinations(gm.Db, map != null ? map.region : null, WorldIndex.Get(), map != null ? map.mapId : null);
            int next = int.MaxValue; var unlocks = new List<string>();
            foreach (var t in towns)
                foreach (var f in t.facilities)
                {
                    if (f == null || t.SettlementTier < f.minTier || f.minCredit <= p.credit) continue;
                    if (f.minCredit < next) { next = f.minCredit; unlocks.Clear(); }
                    if (f.minCredit == next) unlocks.Add(t.displayName + " " + f.displayName);
                }
            if (towns.Count > 0 && p.credit < ContractManager.PremiumCredit && ContractManager.PremiumCredit <= next)
            {
                if (ContractManager.PremiumCredit < next) unlocks.Clear();
                next = ContractManager.PremiumCredit; unlocks.Add("게시판 귀한 짐 의뢰");
            }

            // 내 신용 카드
            float chipsH = 0; { float cx = 54; foreach (var u in unlocks) { float cwid = Mathf.Round(CuteUI.Width(u, true) + 16); if (cx + cwid > w - 26) { cx = 54; chipsH += 18; } cx += cwid + 4; } if (unlocks.Count > 0) chipsH += 18; }
            float ch = next != int.MaxValue ? 52 + chipsH + 4 : 50;
            var card = new Rect(0, y, w, ch);
            CuteUI.Box(card, 7, CuteUI.Paper, CuteUI.Interact, 2);
            CuteUI.Circle(10, y + 9, 34, CuteUI.Interact, CuteUI.InteractLo);
            CuteUI.LabelCenter(27, y + 22, p.credit.ToString(), CuteUI.White, false, true);
            CuteUI.LabelCenter(27, y + 33, "신용", CuteUI.White, true);
            if (next != int.MaxValue)
            {
                CuteUI.Label(54, y + 14, $"신용 {next}까지 {next - p.credit} 남았어요", CuteUI.Text, false, true);
                CuteUI.Bar(new Rect(54, y + 24, w - 66, 8), next > 0 ? p.credit / (float)next : 1f, CuteUI.Interact, CuteUI.InteractHi);
                CuteUI.Label(54, y + 41, "의뢰를 마치면 신용이 올라요 · 이때 열려요", CuteUI.Sub, true);
                float cx = 54, cy = y + 52;
                foreach (var u in unlocks)
                {
                    float cwid = Mathf.Round(CuteUI.Width(u, true) + 16);
                    if (cx + cwid > w - 26) { cx = 54; cy += 18; }
                    CuteUI.Box(new Rect(cx, cy, cwid, 15), 6, CuteUI.ZeroSoft, CuteUI.KeyLine);
                    CuteUI.Icon(CuteUI.Pic.Star4, cx + 3, cy + 4, 1, CuteUI.Sub);
                    CuteUI.Label(cx + 12, cy + 7.5f, u, CuteUI.Sub, true);
                    cx += cwid + 4;
                }
            }
            else
            {
                CuteUI.Label(54, y + 17, "이 지역 시설은 다 열렸어요", CuteUI.Text, false, true);
                CuteUI.Label(54, y + 33, "물량·귀환품·길을 바꿔 더 벌어 보세요", CuteUI.Sub, true);
            }
            y += ch + 8;

            // 마을별 시설
            foreach (var town in towns)
            {
                var list = new List<TownFacility>();
                foreach (var f in town.facilities) if (f != null && f.kind != FacilityKind.Rest && town.SettlementTier >= f.minTier) list.Add(f);
                if (list.Count == 0) continue;
                y = Section(y, town.displayName, $"거점 {town.SettlementTier}등급");
                foreach (var f in list)
                {
                    string denial = FacilityAccess.Denial(town, f, p);
                    CuteUI.Box(new Rect(0, y, w, 30), 7, CuteUI.Paper2, CuteUI.Line);
                    var pic = f.kind == FacilityKind.Warehouse ? CuteUI.Pic.Sack : CuteUI.Pic.Compass;
                    var ps = CuteUI.PicSize(pic);
                    CuteUI.Circle(5, y + 4, 22, CuteUI.Paper, CuteUI.Line);
                    CuteUI.Icon(pic, 16 - ps.x / 2f, y + 15 - ps.y / 2f);
                    float bl = Pill(w - 7, y + 15, denial == null ? "이용 가능" : TownFacilitiesUI.ShortDenial(denial), denial == null ? CuteUI.BadgeKind.Ok : CuteUI.BadgeKind.Locked);
                    CuteUI.Label(33, y + 9, f.displayName, denial == null ? CuteUI.Text : CuteUI.Sub, true, true);
                    CuteUI.Label(33, y + 22, TradeLedgerUI.Fit(CuteUI.Plain(JourneyGuide.FacilityEffect(f)), bl - 40, true, false), CuteUI.Sub, true);
                    y += 33;
                }
                y += 4;
            }
            float th = CuteUI.WrapHeight("수레를 사기 전에 다음 매입 자금을 남기세요. 의뢰 짐도 짐칸을 써요. 창고에 맡긴 물건은 판 것이 아니에요.", w - 20, true);
            CuteUI.Icon(CuteUI.Pic.Bulb, 3, y + 2);
            CuteUI.Wrap(15, y, w - 20, "수레를 사기 전에 다음 매입 자금을 남기세요. 의뢰 짐도 짐칸을 써요. 창고에 맡긴 물건은 판 것이 아니에요.", CuteUI.Sub, true);
            return y + th + 4;
        }

        // ---------------- 주변 거래처 ----------------
        float DrawTowns(GameManager gm, float y, float w)
        {
            var map = MapRoot.Current;
            var world = WorldIndex.Get();
            string here = map != null ? map.mapId : null;
            var towns = JourneyGuide.Destinations(gm.Db, map != null ? map.region : null, world, here);
            var rows = new List<(TownData town, int hops)>();
            foreach (var t in towns) rows.Add((t, CargoSaleGuide.Distance(world, here, world != null ? world.MapOf("market", t.id) : null)));
            rows.Sort((a, b) => (a.hops < 0 ? 99 : a.hops).CompareTo(b.hops < 0 ? 99 : b.hops));
            if (rows.Count == 0)
            {
                CuteUI.LabelCenter(w / 2f, y + 40, "이 맵과 이어진 거래처 정보가 없어요", CuteUI.Text, true, true);
                return y + 60;
            }
            const float iconsW = 92f;
            foreach (var (town, hops) in rows)
            {
                bool recorded = JourneyGuide.MarketRecorded(gm.Player.ledger, town.id);
                string note = string.IsNullOrEmpty(town.journeyNote) ? null : CuteUI.Plain(town.journeyNote);
                float nw = w - 20 - iconsW - 8;
                float nh = note != null ? CuteUI.WrapHeight(note, nw, true) : 0;
                float ch = Mathf.Max(48, 24 + nh + 6);
                CuteUI.Box(new Rect(0, y, w, ch), 7, CuteUI.Paper2, CuteUI.Line);
                float tx = CuteUI.Label(10, y + 12, town.displayName, CuteUI.Text, false, true);
                string hop = hops == 0 ? "지금 여기" : hops > 0 ? $"{hops}구간" : "길 모름";
                ContractBoard.Pill(tx + 6, y + 12, hop, hops == 0 ? CuteUI.Interact : CuteUI.Paper, hops == 0 ? CuteUI.InteractLo : CuteUI.Line, hops == 0 ? CuteUI.White : CuteUI.Sub);
                if (note != null) CuteUI.Wrap(10, y + 22, nw, note, CuteUI.Sub, true);
                // 오른쪽: 시세 기록 + 특산품
                float ix = w - 8 - iconsW;
                Pill(w - 8, y + 12, recorded ? "시세 기록 있음" : "시세 모름", recorded ? CuteUI.BadgeKind.On : CuteUI.BadgeKind.Locked);
                int n = 0;
                foreach (var item in gm.Db.items)
                {
                    if (item == null || item.questItem || item.originTown != town || n >= 4) continue;
                    if (n == 0) CuteUI.Label(ix, y + 33, "특산", CuteUI.Sub, true);
                    TradeLedgerUI.ItemIcon(item, ix + 22 + n * 18, y + 25, 1);
                    n++;
                }
                y += ch + 5;
            }
            float th = CuteUI.WrapHeight("장부(L)의 시세는 직접 가서 본 값이에요. 가는 동안 값이 바뀔 수 있어요.", w - 20, true);
            CuteUI.Icon(CuteUI.Pic.Bulb, 3, y + 2);
            CuteUI.Wrap(15, y, w - 20, "장부(L)의 시세는 직접 가서 본 값이에요. 가는 동안 값이 바뀔 수 있어요.", CuteUI.Sub, true);
            return y + th + 4;
        }
    }
}
