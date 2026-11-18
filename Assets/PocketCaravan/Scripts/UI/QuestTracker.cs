using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 진행 중 퀘스트와 지금 할 일을 왼쪽 할 일 카드(TaskCard)에 올린다 (Tab이면 다른 퀘스트·배달까지). 카드를 누르면 여정 수첩.
    /// 11/13 Q 단추·Q 키는 뺐다 (InputReader.QuestLogPressed는 카드·튜토리얼이 누르는 가상 'q'만).
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
            // 11/16 팀 피드백: 할 일 카드가 늘 떠 있어 거슬린다 → Q로 접고 편다 (조작 배우기 단계는 접어도 보인다)
            if (InputReader.TaskTogglePressed() && !UIState.Blocking && MapRoot.Current != null && !GameManager.I.IsTravelling)
            { TaskCard.Hidden = !TaskCard.Hidden; Sfx.Play("move", .6f); }
            // 11/18 은샘: 튜토리얼 카드 설명 중엔 Q로 닫지 않는다 (Esc·닫기 단추는 닫는다, 다시 열면 이어서)
            if (logOpen && ((InputReader.QuestLogPressed() && !TutorialSpot.HoldsWindow) || InputReader.CancelPressed())) CloseLog();
            else if (InputReader.QuestLogPressed() && !UIState.Blocking && !UIState.JustClosed && !GameManager.I.IsTravelling
                && UIState.TryOpen(this, CloseLog)) { logOpen = true; JournalOpen = true; if (StoryGuide.IntroActive) { logTab = 0; scroll = Vector2.zero; } }   // 튜토리얼 중엔 의뢰 칸 맨 위부터
            else if (logOpen)
            {
                // 11/08 ←→ 탭 바꾸기 · ↑↓ 넘기기 (마우스는 탭 단추 · 휠 · 막대)
                var mv = InputReader.MovePressed();
                if (mv.x != 0) { logTab = (logTab + mv.x + LogTabs.Length) % LogTabs.Length; scroll = Vector2.zero; Sfx.Play("open", .25f); }
                if (mv.y != 0) scroll.y = Mathf.Max(0f, scroll.y - mv.y * 40f);
            }
        }
        static readonly string[] LogTabs = { "의뢰", "성장·시설", "주변 거래처" };
        void CloseLog() { logOpen = false; JournalOpen = false; UIState.Release(this); }
        /// <summary>11/18 여정 수첩이 열려 있다 (튜토리얼: 열고 → 목록 보고 → 닫으면 끝)</summary>
        public static bool JournalOpen { get; private set; }
        void OnDisable() { if (logOpen) CloseLog(); }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            if (UIState.PhotoMode) return;   // 11/11 은샘: 기념 사진 찍는 동안 알림·할 일 줄이 사진에 찍히지 않게 (팀 시연: 아래 대사가 플레이어를 가렸다)
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
            var focus = QuestFocus.Current;   // 11/11 은샘: 길 안내 대상 하나만 (이벤트·다음 이야기는 각자 올린다)
            if (focus == QuestFocus.Kind.Event || focus == QuestFocus.Kind.Lead) return;
            if (focus == QuestFocus.Kind.Quest && qm.Tracked != null)
            {
                int others = 0; foreach (var ap in qm.Active) if (ap != qm.Tracked) others++;
                string kicker = qm.Tracked.quest.title + (others > 0 ? $" 외 {others}개" : "");   // 11/01 진행 중인 다른 퀘스트 수 (수첩에서 안내 바꾸기)
                TaskCard.Task(TaskCard.Quest, CuteUI.Pic.Scroll, false, kicker, CuteUI.Plain(qm.ProgressText(qm.Tracked)), null);
            }
            else if (gm.Player.contracts.Count > 0)
            {
                var c = gm.Player.contracts[0];
                string kicker = "배달" + (gm.Player.contracts.Count > 1 ? $" 외 {gm.Player.contracts.Count - 1}건" : "");
                TaskCard.Task(TaskCard.Delivery, CuteUI.Pic.Sack, false, kicker, $"{c.cargo} → {gm.Contracts.TownName(c.toTown)}", CuteUI.Plain(gm.Contracts.TimeLeftText(c)));
            }
        }

        static readonly List<(string text, bool small, bool bold, Color32 col)> detailLines = new List<(string, bool, bool, Color32)>();

        /// <summary>
        /// 11/13 할 일 카드에 올린다. Tab으로 펼쳤으면 카드 아래 자세히 (카드 위 할 일 말고 다른 퀘스트·배달·다음 이야기)
        /// </summary>
        void DrawTracker(QuestManager qm)
        {
            if (MapRoot.Current == null) return;
            DrawCompact(qm);
            if (!HUD.Expanded) return;
            var gm = GameManager.I;
            var focus = QuestFocus.Current;
            bool trackedOnCard = focus == QuestFocus.Kind.Quest && qm.Tracked != null;
            detailLines.Clear();
            if (qm.Tracked != null && !trackedOnCard)
            {
                detailLines.Add((qm.Tracked.quest.title, false, true, CuteUI.Text));
                detailLines.Add(("· " + CuteUI.Plain(qm.ProgressText(qm.Tracked)), true, false, CuteUI.Sub));
            }
            foreach (var ap in qm.Active)
            {
                if (ap == null || ap == qm.Tracked) continue;
                detailLines.Add((ap.quest.title, false, true, CuteUI.Text));
                detailLines.Add(("· " + CuteUI.Plain(qm.ProgressText(ap)), true, false, CuteUI.Sub));
            }
            bool firstOnCard = !trackedOnCard && focus != QuestFocus.Kind.Event && focus != QuestFocus.Kind.Lead;
            for (int i = 0; i < gm.Player.contracts.Count; i++)
            {
                var c = gm.Player.contracts[i];
                string state = c.dropped ? "떨어뜨림!" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                if (!(i == 0 && firstOnCard)) detailLines.Add(($"{c.cargo} → {gm.Contracts.TownName(c.toTown)}", false, true, CuteUI.Text));
                detailLines.Add(($"· {CuteUI.Plain(gm.Contracts.TimeLeftText(c))} · {state} · 보수 {c.reward}냥", true, false, c.dropped ? CuteUI.Alert : CuteUI.Sub));
            }
            var next = StoryGuide.Next;
            if (next.HasValue && focus != QuestFocus.Kind.Lead)
            {
                detailLines.Add((StoryGuide.LeadTitle, true, false, CuteUI.Alert));
                detailLines.Add((next.Value.action + (string.IsNullOrEmpty(next.Value.where) ? "" : " · " + next.Value.where), false, true, CuteUI.Text));
            }
            TaskCard.Details(detailLines);
        }

        // 11/08 은샘: 여정 수첩을 장부·게시판과 같은 모양으로 (아이콘 탭 + 종이 칸 + 카드).
        // 의뢰 = 할 일·보상을 한눈에, 비면 다음에 할 만한 일. 성장 = 다음 신용까지 막대와 열리는 시설. 거래처 = 거리·특산품·시세 기록.
        float logContentH;
        static Vector2 logOrigin;   // 수첩 목록 칸 왼쪽 위 (그룹 안 자리 → 화면 자리)
        static readonly CuteUI.Pic[] LogPics = { CuteUI.Pic.Scroll, CuteUI.Pic.Star4, CuteUI.Pic.Pin };

        void DrawLog(QuestManager qm)
        {
            float w = Mathf.Min(500, UIStyle.VirtualWidth - 16), h = Mathf.Min(286, UIStyle.VirtualHeight - 34);
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "여정 수첩");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            ScreenZones.MarkSpot("journal.close", new Rect(r.xMax - 32, r.y - 9, 22, 22));   // 11/18 튜토리얼: 수첩을 연 다음 닫기 단추를 밝힌다 (TutorialSpot)
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { CloseLog(); return; }

            // 11/18 윤채: 탭 오른쪽 설명 줄 · 맨 아래 키 안내 줄은 뺐다 (칸을 더 넓게)
            var content = new Rect(r.x + 15, r.y + 50, w - 30, h - 50 - 14);
            CuteUI.Box(content, 7, CuteUI.Paper, CuteUI.Ink);
            float tx = content.x + 6;
            for (int i = 0; i < LogTabs.Length; i++)
            {
                if (CuteUI.Tab(tx, content.y, LogTabs[i], LogPics[i], logTab == i, out float tw) && logTab != i) { logTab = i; scroll = Vector2.zero; }
                tx += tw + 3;
            }
            var gm = GameManager.I;

            var area = new Rect(content.x + 6, content.y + 6, content.width - 12, content.height - 10);
            scroll.y = TradeLedgerUI.Scroll(area, scroll.y, logContentH);
            GUI.BeginGroup(area);
            logOrigin = area.position;
            float cw = area.width - (logContentH > area.height ? 7 : 0);
            float end = logTab == 0 ? DrawQuests(qm, gm, -scroll.y, cw, area.height) : logTab == 1 ? DrawGrowth(gm, -scroll.y, cw) : DrawTowns(gm, -scroll.y, cw);
            logContentH = end + scroll.y;
            GUI.EndGroup();
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
                if (CuteUI.Hit(new Rect(x, y + 4, right - x, 17), new GUIContent("", "누르면 길 안내를 끕니다"), GUIStyle.none)) click();
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
        /// <summary>11/18 은샘 "튜토리얼이 뜨는데 의뢰 목록에 없으면 이상하다": 네 지역 시작 튜토리얼의 지금 할 일</summary>
        static bool TutorialNow(out string main, out string where)
            => GoryeoIntro.Summary(out main, out where, out _) || VeniceIntro.Summary(out main, out where, out _)
            || NorthIntro.Summary(out main, out where, out _) || ArabiaIntro.Summary(out main, out where, out _);

        float DrawSpecialCards(float y, float w)
        {
            var focus = QuestFocus.Current;
            if (TutorialNow(out string tMain, out string tWhere))
            {
                // 맨 위 청록 테 카드: 튜토리얼은 늘 길 안내 중이라 단추는 없다
                float ch = 22 + 14 + 14 + 8;
                var card = new Rect(0, y, w, ch);
                CuteUI.Box(card, 7, CuteUI.Paper, CuteUI.Interact, 2, 2);
                ScreenZones.MarkSpot("journal.tutorial", new Rect(logOrigin.x + card.x, logOrigin.y + card.y, card.width, card.height));   // 11/18 튜토리얼이 목록에서 이 카드를 밝힌다
                float right = Pill(w - 7, y + 12, "튜토리얼", CuteUI.BadgeKind.On);
                CuteUI.Label(10, y + 12, TradeLedgerUI.Fit("장사 첫걸음 · 조작 배우기", right - 16, false, true), CuteUI.Text, false, true);
                CuteUI.Icon(CuteUI.Pic.Arrow, 10, y + 25, 1, CuteUI.Interact);
                CuteUI.Label(22, y + 29, TradeLedgerUI.Fit(CuteUI.Plain(tMain), w - 32, true, true), CuteUI.Text, true, true);
                if (!string.IsNullOrEmpty(tWhere))
                {
                    CuteUI.Icon(CuteUI.Pic.Pin, 22, y + 39, 1, CuteUI.Sub);
                    CuteUI.Label(30, y + 43, TradeLedgerUI.Fit(tWhere, w - 40, true, false), CuteUI.Sub, true);
                }
                y += ch + 5;
            }
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
            bool any = NetGuild.Active || StoryGuide.LeadAvailable || TutorialNow(out _, out _);
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
        /// <summary>
        /// 11/19 은샘 "마일스톤은 여정 수첩에": 운반 수단 카드. 위 줄 단계 칩(등짐 › 손수레 › 짐말 › 마차, 가진 것 ✓ · 다음 것 강조),
        /// 아래 줄 다음 목표와 돈 막대 (다 모이면 "살 수 있어요!") · 파는 장터. 마차까지 샀으면 "운반 수단을 다 모았어요".
        /// </summary>
        static float DrawVehicleGoal(GameManager gm, float y, float w)
        {
            var p = gm.Player;
            var ladder = VehicleGoal.Ladder(p, gm.Db);
            var next = VehicleGoal.Next(p, gm.Db);
            float have = Mathf.Max(p.vehicle != null ? p.vehicle.capacity : 0f, p.stowedVehicle != null ? p.stowedVehicle.capacity : 0f);
            float ch = next != null ? 64 : 44;
            var card = new Rect(0, y, w, ch);
            CuteUI.Box(card, 7, CuteUI.Paper, CuteUI.Sell, 2);
            CuteUI.Circle(10, y + 9, 26, CuteUI.Sell, CuteUI.FrameLo);
            CuteUI.Icon(CuteUI.Pic.Sack, 18, y + 17, 1, CuteUI.White);
            // 단계 칩
            float cx = 44, cy = y + 8;
            for (int i = 0; i < ladder.Count; i++)
            {
                var v = ladder[i];
                bool owned = v.capacity <= have, isNext = v == next;
                string label = (owned ? "✓ " : "") + v.displayName;
                float cw = Mathf.Round(CuteUI.Width(label, true, isNext) + 12);
                var chip = new Rect(cx, cy, cw, 15);
                CuteUI.Box(chip, 6, isNext ? CuteUI.SellSoft : owned ? CuteUI.Paper2 : CuteUI.ZeroSoft, isNext ? CuteUI.Sell : CuteUI.KeyLine);
                CuteUI.LabelCenter(chip.center.x, chip.center.y, label, owned || isNext ? CuteUI.Text : CuteUI.Sub, true, isNext);
                cx += cw;
                if (i < ladder.Count - 1) { CuteUI.Label(cx + 2, cy + 7.5f, "›", CuteUI.Sub, true); cx += 10; }
            }
            if (next == null)
            {
                CuteUI.Label(44, y + 33, "운반 수단을 다 모았어요", CuteUI.Text, true, true);
                return y + ch + 8;
            }
            int money = Mathf.Max(0, p.money), need = Mathf.Max(1, next.price);
            bool ready = money >= need;
            float lx = CuteUI.Label(44, y + 33, $"다음 목표 · {next.displayName}", CuteUI.Text, false, true);
            CuteUI.Label(lx + 6, y + 33.5f, $"짐 {p.CapacityOf(next):0}칸", CuteUI.Sub, true);
            if (ready) CuteUI.LabelRight(w - 10, y + 33, "살 수 있어요!", CuteUI.Up, true, true);
            else CuteUI.LabelRight(w - 10, y + 33, $"{money} / {need}냥", CuteUI.Sub, true, true);
            CuteUI.Bar(new Rect(44, y + 42, w - 54, 6), money / (float)need, CuteUI.Sell, CuteUI.SellSoft);
            string where = VehicleGoal.WhereSold(next, p, gm.Db);
            if (where.Length > 0)
            {
                CuteUI.Icon(CuteUI.Pic.Pin, 44, y + 51, 1, CuteUI.Sub);
                CuteUI.Label(53, y + 55, TradeLedgerUI.Fit("파는 곳 · " + where, w - 64, true, false), CuteUI.Sub, true);
            }
            return y + ch + 8;
        }

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

            y = DrawVehicleGoal(gm, y, w);   // 11/19 운반 수단 마일스톤

            // 내 신용 카드
            float chipsH = 0; { float cx = 54; foreach (var u in unlocks) { float cwid = Mathf.Round(CuteUI.Width(u, true) + 16); if (cx + cwid > w - 26) { cx = 54; chipsH += 18; } cx += cwid + 4; } if (unlocks.Count > 0) chipsH += 18; }
            float ch = next != int.MaxValue ? 40 + chipsH + 6 : 50;
            var card = new Rect(0, y, w, ch);
            CuteUI.Box(card, 7, CuteUI.Paper, CuteUI.Interact, 2);
            CuteUI.Circle(10, y + 9, 34, CuteUI.Interact, CuteUI.InteractLo);
            CuteUI.LabelCenter(27, y + 22, p.credit.ToString(), CuteUI.White, false, true);
            CuteUI.LabelCenter(27, y + 33, "신용", CuteUI.White, true);
            if (next != int.MaxValue)
            {
                // 11/18 한 줄로: "신용 N이 되면 열려요" + 막대 + 열리는 것 (설명 줄은 뺐다. 의뢰 탭·게시판이 알려 준다)
                float lx = CuteUI.Label(54, y + 14, $"신용 {next}이 되면 열려요", CuteUI.Text, false, true);
                CuteUI.Label(lx + 6, y + 14.5f, "의뢰를 마치면 올라요", CuteUI.Sub, true);
                CuteUI.Bar(new Rect(54, y + 24, w - 66, 6), next > 0 ? p.credit / (float)next : 1f, CuteUI.Interact, CuteUI.InteractHi);
                float cx = 54, cy = y + 36;
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
                y = Section(y, town.displayName);
                foreach (var f in list)
                {
                    // 11/18 한 줄: 그림 · 이름 · 하는 일(흐리게) · 오른쪽 상태
                    string denial = FacilityAccess.Denial(town, f, p);
                    CuteUI.Box(new Rect(0, y, w, 24), 7, CuteUI.Paper2, CuteUI.Line);
                    var pic = f.kind == FacilityKind.Warehouse ? CuteUI.Pic.Sack : CuteUI.Pic.Compass;
                    var ps = CuteUI.PicSize(pic);
                    CuteUI.Icon(pic, 13 - ps.x / 2f, y + 12 - ps.y / 2f);
                    float bl = Pill(w - 7, y + 12, denial == null ? "이용 가능" : TownFacilitiesUI.ShortDenial(denial), denial == null ? CuteUI.BadgeKind.Ok : CuteUI.BadgeKind.Locked);
                    float nx = CuteUI.Label(26, y + 12, f.displayName, denial == null ? CuteUI.Text : CuteUI.Sub, true, true);
                    CuteUI.Label(nx + 8, y + 12, TradeLedgerUI.Fit(CuteUI.Plain(JourneyGuide.FacilityEffect(f)), bl - nx - 16, true, false), CuteUI.Sub, true);
                    y += 27;
                }
                y += 4;
            }
            return y;
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
            // 11/18 윤채: 카드마다 두 줄 설명 + "특산" · "시세 모름" 글씨가 겹쳐 어지러웠다 →
            //   한 줄(이름 · 거리 · 특산 그림 · 시세를 본 곳만 작은 표시), 카드를 누르면 그 마을 설명이 펼쳐진다 (지금 있는 마을은 처음부터 펼침)
            ItemData hoverItem = null; Rect hoverRect = default;   // 11/11 은샘: 특산 아이콘에 마우스를 올리면 이름
            foreach (var (town, hops) in rows)
            {
                bool recorded = JourneyGuide.MarketRecorded(gm.Player.ledger, town.id);
                string note = string.IsNullOrEmpty(town.journeyNote) ? null : CuteUI.Plain(town.journeyNote);
                bool open = note != null && (openTown == town.id || (openTown == null && hops == 0));
                float nh = open ? CuteUI.WrapHeight(note, w - 20, true) + 6 : 0;
                float ch = 26 + nh;
                var card = new Rect(0, y, w, ch);
                bool hover = new Rect(0, y, w, 26).Contains(Event.current.mousePosition);
                CuteUI.Box(card, 7, open ? CuteUI.Paper : hover ? CuteUI.Paper : CuteUI.Paper2, open ? CuteUI.Interact : CuteUI.Line, open ? 2 : 1);
                float tx = CuteUI.Label(10, y + 13, town.displayName, CuteUI.Text, false, true);
                string hop = hops == 0 ? "지금 여기" : hops > 0 ? $"{hops}구간" : "길 모름";
                tx = ContractBoard.Pill(tx + 6, y + 13, hop, hops == 0 ? CuteUI.Interact : CuteUI.Paper, hops == 0 ? CuteUI.InteractLo : CuteUI.Line, hops == 0 ? CuteUI.White : CuteUI.Sub);
                // 오른쪽: 특산 그림 (최대 4) · 시세를 본 곳이면 작은 장부 표시
                float ix = w - 10;
                if (recorded)
                {
                    var bp = CuteUI.PicSize(CuteUI.Pic.OpenBook);
                    ix -= bp.x;
                    CuteUI.Icon(CuteUI.Pic.OpenBook, ix, y + 13 - bp.y / 2f, 1, CuteUI.Interact);
                    var br = new Rect(ix - 2, y + 4, bp.x + 4, 18);
                    if (br.Contains(Event.current.mousePosition)) { hoverItem = null; hoverRect = br; tipText = "시세를 장부에 적어 둔 곳"; }
                    ix -= 8;
                }
                var mine = new List<ItemData>();
                foreach (var item in gm.Db.items) if (item != null && !item.questItem && item.originTown == town && mine.Count < 4) mine.Add(item);
                for (int k = mine.Count - 1; k >= 0; k--)
                {
                    ix -= 18;
                    TradeLedgerUI.ItemIcon(mine[k], ix, y + 5, 1);
                    var ir = new Rect(ix, y + 5, 16, 16);
                    if (ir.Contains(Event.current.mousePosition)) { hoverItem = mine[k]; hoverRect = ir; tipText = null; }
                }
                if (open) CuteUI.Wrap(10, y + 25, w - 20, note, CuteUI.Sub, true);
                if (note != null && CuteUI.Hit(new Rect(0, y, w, 26), GUIContent.none, GUIStyle.none))
                {
                    openTown = open ? "" : town.id;   // "" = 다 접음 (지금 마을도)
                    Sfx.Play("open", .35f);
                }
                y += ch + 5;
            }
            if (hoverItem != null || tipText != null)   // 카드보다 위에 그리도록 다 그린 뒤
            {
                string nm = hoverItem != null ? hoverItem.displayName : tipText;
                float tw = CuteUI.Width(nm, true) + 12f;
                var tip = new Rect(Mathf.Round(Mathf.Clamp(hoverRect.center.x - tw / 2f, 2f, w - tw - 2f)), hoverRect.y - 17f, tw, 15f);
                CuteUI.Box(tip, 6, CuteUI.Paper, CuteUI.Ink, 1, 1);
                CuteUI.Label(tip.x + 6f, tip.center.y, nm, CuteUI.Text, true);
            }
            tipText = null;
            return y;
        }

        string openTown;          // 11/18 주변 거래처에서 펼친 마을 (null = 지금 마을만 펼침, "" = 다 접음)
        static string tipText;    // 11/18 장부 표시에 마우스를 올렸을 때 말
    }
}
