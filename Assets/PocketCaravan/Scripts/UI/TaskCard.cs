using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/13 기우(시연 피드백 "글이 너무 많다 · 퀘스트를 캐릭터 UI 옆에"): 할 일 카드.
    /// 예전엔 위 가운데에 할 일 줄 · 튜토리얼 · 다음 이야기 · 이벤트 · 목표 칸 수 알약이 따로 쌓였다 → 왼쪽 위 상태 카드 바로 밑 카드 하나로.
    /// - 각 곳은 OnGUI에서 자기 할 일을 올리기만 한다 (Task · Goal · News · Details). 그리기는 HUD가 상태 카드 다음에 한 번 한다
    /// - 할 일은 한 번에 하나: 순서가 작은 것 (튜토리얼 10 · 이벤트 12 · 다음 이야기 20 · 퀘스트 22 · 배달 24)
    /// - 평소(접힘): 작은 머리말 · 굵은 할 일 · 목적지와 칸 수 한 줄. Tab(HUD.Expanded)으로 펼치면 다른 퀘스트·배달·오늘 소식까지
    /// - 카드를 누르면 여정 수첩 (Q 단추·Q 키는 뺐다). 캐릭터가 카드 뒤로 지나가면 옅어진다
    /// </summary>
    public static class TaskCard
    {
        public const int Intro = 10, EventTask = 12, Lead = 20, Quest = 22, Delivery = 24;
        const float X = 6f, Dot = 16f, ArrowW = 9f;   // ArrowW: 오른쪽 '›' 자리 (누를 수 있는 카드라는 표시)
        /// <summary>11/13 카드 너비는 늘 같다: 위 상태 카드와 같은 오른쪽 끝 (글 길이에 따라 들쭉날쭉하지 않게)</summary>
        static float W => HUD.ColumnRight - X;   // ArrowW: 오른쪽 '›' 자리 (누를 수 있는 카드라는 표시)

        struct Item { public int frame; public CuteUI.Pic? pic; public bool alert, evt; public string kicker, main, where; }
        struct GoalLine { public int frame; public string dest, sub, cells; public bool exit; }
        struct NewsLine { public int frame; public CuteUI.Pic pic; public string main, where; }

        static readonly SortedDictionary<int, Item> items = new SortedDictionary<int, Item>();
        static readonly Dictionary<string, NewsLine> news = new Dictionary<string, NewsLine>();
        static readonly List<(string text, bool small, bool bold, Color32 col)> details = new List<(string, bool, bool, Color32)>();
        static int detailsFrame = -10;
        static GoalLine goal;
        static int shownOrder = -1;
        static Rect lastRect;

        struct BadgeLine { public int frame; public CuteUI.Pic pic; public string text; public Color32 col; }
        static readonly SortedDictionary<string, BadgeLine> badges = new SortedDictionary<string, BadgeLine>();

        /// <summary>11/13 상태 카드 밑 작은 표 (푹 쉼 등). OnGUI마다 올린다</summary>
        public static void Badge(string id, CuteUI.Pic pic, string text, Color32 col)
            => badges[id] = new BadgeLine { frame = Time.frameCount, pic = pic, text = text, col = col };

        /// <summary>HUD가 상태 카드 밑(top)에 작은 표를 한 줄로 그린다. 다음 것을 놓을 기준 y를 돌려준다 (표가 없으면 top - 4: 간격이 두 번 붙지 않게)</summary>
        public static float DrawBadges(float top)
        {
            float x = X, h = 14f; bool any = false;
            foreach (var b in badges.Values)
            {
                if (Time.frameCount - b.frame > 1) continue;
                var ps = CuteUI.PicSize(b.pic);
                float bw = Mathf.Round(6f + ps.x + 3f + CuteUI.Width(b.text, true, true) + 7f);
                if (x + bw > HUD.ColumnRight) break;
                var r = new Rect(x, Mathf.Round(top), bw, h);
                CuteUI.Box(r, 6, CuteUI.Paper, CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
                CuteUI.Icon(b.pic, r.x + 6f, r.y + (h - ps.y) / 2f, 1, b.col);
                CuteUI.Label(r.x + 6f + ps.x + 3f, r.center.y, b.text, CuteUI.Text, true, true);
                x += bw + 4f; any = true;
            }
            if (!any) return top - 4f;
            ScreenZones.Mark("hud.badges", new Rect(X, top, x - X, h));
            return top + h;
        }

        static readonly Regex StepRx = new Regex(@"배우기\s*(\d+\s*/\s*\d+)");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() { badges.Clear(); items.Clear(); news.Clear(); details.Clear(); detailsFrame = -10; goal = default; shownOrder = -1; lastRect = default; }

        /// <summary>올린 것이 아직 살아 있나: 이번·지난 프레임에 올렸거나, Tab으로 펼친 동안은 펼치기 전에 있던 것 (튜토리얼 알약처럼 펼친 동안 안 올리는 곳)</summary>
        static bool Live(int frame) => Time.frameCount - frame <= 1 || (HUD.Expanded && frame >= HUD.ExpandedFrame - 2);

        /// <summary>할 일 하나 올리기 (OnGUI마다). 같은 순서는 덮어쓴다</summary>
        public static void Task(int order, CuteUI.Pic? pic, bool alert, string kicker, string main, string where)
        {
            if (string.IsNullOrEmpty(main)) return;
            items[order] = new Item { frame = Time.frameCount, pic = pic, alert = alert, evt = CuteUI.EventStyle, kicker = kicker, main = main, where = where };
        }

        /// <summary>목표 길잡이 (목적지 · 덧말 · 칸 수). 할 일 아래 한 줄</summary>
        public static void Goal(string dest, string sub, string cells, bool exit)
            => goal = new GoalLine { frame = Time.frameCount, dest = dest, sub = sub, cells = cells, exit = exit };

        /// <summary>오늘 소식 (큰 장 · 밤 축제). Tab으로 펼쳤을 때만 보인다 (처음 들어설 때는 아래 알림이 알려 준다)</summary>
        public static void News(string id, CuteUI.Pic pic, string main, string where)
            => news[id] = new NewsLine { frame = Time.frameCount, pic = pic, main = main, where = where };

        /// <summary>Tab으로 펼쳤을 때 붙일 자세히 (다른 퀘스트·배달). 두 줄씩(제목 · 자세히)</summary>
        public static void Details(List<(string text, bool small, bool bold, Color32 col)> lines)
        {
            details.Clear(); details.AddRange(lines); detailsFrame = Time.frameCount;
        }

        /// <summary>그 순서의 할 일이 지금 카드에 보이면 카드 자리 (누르면 따로 여는 곳: 상인회 수첩), 아니면 빈 칸</summary>
        public static Rect RectFor(int order) => shownOrder == order && Time.frameCount - lastFrame <= 1 ? lastRect : default;
        static int lastFrame = -10;

        /// <summary>카드가 지금 화면에 있는지 · 자리</summary>
        public static Rect Current => Time.frameCount - lastFrame <= 1 ? lastRect : default;

        /// <summary>HUD가 상태 카드 바로 밑(top)에 그린다. 그린 자리를 돌려준다 (없으면 빈 칸)</summary>
        public static Rect Draw(float top)
        {
            bool expanded = HUD.Expanded;
            Item main = default; int order = -1;
            foreach (var kv in items) if (Live(kv.Value.frame)) { main = kv.Value; order = kv.Key; break; }
            bool hasGoal = goal.dest != null && Live(goal.frame);
            var newsNow = new List<NewsLine>();
            if (expanded) foreach (var n in news.Values) if (Live(n.frame)) newsNow.Add(n);
            bool hasDetails = expanded && details.Count > 0 && Live(detailsFrame);
            if (order < 0 && !hasGoal && newsNow.Count == 0 && !hasDetails) { shownOrder = -1; return default; }

            // ---- 글 정리 ----
            string kicker = main.kicker, where = main.where, chip = null, mainText = main.main, key = null;
            int stepNow = 0, stepAll = 0;
            bool oneLine = order == Intro;   // 11/13 튜토리얼은 한 줄 (키캡 + 짧은 이름)
            if (order == Intro && string.IsNullOrEmpty(kicker)) kicker = "튜토리얼";
            if (order == EventTask && string.IsNullOrEmpty(kicker)) kicker = "이벤트";
            if (!string.IsNullOrEmpty(where))
            {
                var m = StepRx.Match(where);
                if (m.Success)
                {
                    chip = m.Groups[1].Value.Replace(" ", ""); where = null;
                    var parts = chip.Split('/');
                    if (order == Intro && parts.Length == 2 && int.TryParse(parts[0], out stepNow) && int.TryParse(parts[1], out stepAll))
                    {
                        // 11/13 "배우기 n/8" 단계: 긴 문장 대신 [키] 짧은 이름, 칩 대신 머리말 옆 진행 막대
                        var sh = ControlLesson.Short(stepNow - 1, main.main);
                        key = sh.key; mainText = sh.text; chip = null;
                    }
                    else stepNow = stepAll = 0;
                }
                else if (where.Contains("Space")) where = null;   // 말 걸 대상 머리 위 [Space] 알약이 같은 말을 한다
            }
            if (hasGoal) where = null;                              // 가는 길은 목표 줄이 알려 준다
            string goalDest = hasGoal ? goal.dest : null;
            if (hasGoal && order >= 0 && mainText.Contains(goalDest)) goalDest = null;   // "어머니께 말 걸기" + "어머니" → 칸 수만

            // ---- 크기 ----
            float w = W;
            float inner = w - 16f - Dot - 6f - ArrowW;
            string stepText = stepAll > 0 ? $"{stepNow}/{stepAll}" : null;
            float barW = stepAll > 0 ? stepAll * 6f - 1f : 0f;
            float kickW = !string.IsNullOrEmpty(kicker) ? CuteUI.Width(kicker, true) + (stepText != null ? 4f + CuteUI.Width(stepText, true, true) + 5f + barW : 0f) : 0f;
            float keyW = key != null ? CuteUI.KeycapWidth(key) + 4f : 0f;
            float chipW = chip != null ? Mathf.Round(CuteUI.Width(chip, true, true) + 10f) : 0f;
            float mainW = order >= 0 ? keyW + CuteUI.Width(mainText, false, true) : 0f;
            string goalText = hasGoal ? (goalDest ?? "") + (goal.sub != null ? (goalDest != null ? " · " : "") + goal.sub : "") : null;
            float goalW = hasGoal ? 10f + CuteUI.Width(goalText, true) + (goal.cells != null ? 6f + CuteUI.Width(goal.cells, true, true) + 10f : 0f) : 0f;
            float whereW = !string.IsNullOrEmpty(where) ? CuteUI.Width(where, true) : 0f;

            float mainH = order < 0 ? 0f : oneLine ? 15f : Mathf.Min(Mathf.Ceil(CuteUI.WrapHeight(mainText, inner + 1f)), 30f);
            float h = 6f;
            if (kickW > 0 || chipW > 0) h += 11f;
            h += mainH;
            if (hasGoal) h += 14f; else if (whereW > 0) h += 12f;

            // 펼침: 자세히 · 오늘 소식 · 수첩 안내
            float room = UIStyle.VirtualHeight - 6f - HUD.BottomReserved - top;
            var extra = new List<(string text, bool small, bool bold, Color32 col, CuteUI.Pic? pic)>();
            if (expanded)
            {
                foreach (var n in newsNow) { extra.Add((n.main, false, true, CuteUI.Text, n.pic)); if (!string.IsNullOrEmpty(n.where)) extra.Add((n.where, true, false, CuteUI.Sub, null)); }
                if (hasDetails) foreach (var d in details) extra.Add((d.text, d.small, d.bold, d.col, null));
            }
            float extraH = 0f; int extraShown = 0;
            if (extra.Count > 0)
            {
                float sep = 7f;
                for (int i = 0; i < extra.Count; i++)
                {
                    float lh = CuteUI.WrapHeight(extra[i].text, w - 20f - (extra[i].pic.HasValue ? 12f : 0f), extra[i].small) + 2f;
                    if (h + sep + extraH + lh + 16f > room) break;
                    extraH += lh; extraShown = i + 1;
                }
                if (extraShown > 0) extraH += sep;
            }
            if (expanded) h += 14f;   // "눌러서 여정 수첩" 안내
            h += 5f;
            h += extraH;

            var r = new Rect(X, Mathf.Round(top), w, Mathf.Round(h));
            lastRect = r; lastFrame = Time.frameCount; shownOrder = order;
            ScreenZones.Mark("task.card", r);

            // ---- 그리기 ----
            var old = GUI.color;
            if (PlayerBehind(r)) GUI.color = new Color(old.r, old.g, old.b, old.a * .4f);
            bool hover = !UIState.Blocking && r.Contains(Event.current.mousePosition);
            if (order >= 0 && main.evt) CuteUI.Box(r, 8, CuteUI.EventPaper, CuteUI.EventLine, 2, 2, CuteUI.SoftShadow);
            else CuteUI.Box(r, 8, hover ? CuteUI.Paper2 : CuteUI.Paper, hover ? CuteUI.Interact : CuteUI.Ink, 1, 2, CuteUI.SoftShadow);

            float tx = r.x + 8f + Dot + 6f, y = r.y + 5f;
            // 왼쪽 동그라미
            if (order >= 0)
            {
                float dy = r.y + 5f + ((kickW > 0 || chipW > 0) ? 11f : 0f) + Mathf.Max(0f, (Mathf.Min(mainH, 15f) - Dot) / 2f);
                if (main.alert) CuteUI.Medallion(r.x + 7f, dy - 1f, (int)Dot + 2, CuteUI.Alert);
                else
                {
                    CuteUI.Circle(r.x + 8f, dy, (int)Dot, main.evt ? CuteUI.Deco : CuteUI.Interact, main.evt ? CuteUI.EventLine : CuteUI.InteractLo, 1);
                    var pic = main.evt ? CuteUI.Pic.Star4 : main.pic ?? CuteUI.Pic.Scroll; var ps = CuteUI.PicSize(pic);
                    CuteUI.Icon(pic, r.x + 8f + (Dot - ps.x) / 2f, dy + (Dot - ps.y) / 2f, 1, CuteUI.White);
                }
            }
            if (kickW > 0 || chipW > 0)
            {
                if (kickW > 0)
                {
                    float kx = CuteUI.Label(tx, y + 5f, kicker, main.evt ? CuteUI.FrameLo : CuteUI.Sub, true);
                    if (stepText != null)
                    {
                        kx = CuteUI.Label(kx + 4f, y + 5f, stepText, CuteUI.Text, true, true) + 5f;
                        for (int i = 0; i < stepAll; i++)   // 진행 막대: 끝낸 칸 · 지금 칸 · 남은 칸
                            CuteUI.Fill(new Rect(kx + i * 6f, y + 3f, 5f, 4f), i < stepNow - 1 ? CuteUI.Interact : i == stepNow - 1 ? CuteUI.Deco : CuteUI.Line);
                    }
                }
                if (chipW > 0)
                {
                    var c = new Rect(r.xMax - 7f - ArrowW - chipW, y, chipW, 12f);
                    CuteUI.Box(c, 5, CuteUI.Paper2, CuteUI.Line, 1);
                    CuteUI.LabelCenter(c.center.x, c.center.y, chip, CuteUI.Text, true, true);
                }
                y += 11f;
            }
            if (order >= 0)
            {
                float mx = tx;
                if (key != null) mx += CuteUI.Keycap(mx, y + 1f, key, true) + 4f;
                if (oneLine) CuteUI.Label(mx, y + 7.5f, Fit(mainText, inner - keyW, false, true), CuteUI.Text, false, true);
                else if (mainH <= 16f) CuteUI.Label(mx, y + 7.5f, mainText, CuteUI.Text, false, true);
                else CuteUI.Wrap(mx, y, inner + 1f, mainText, CuteUI.Text, false, true);
                y += mainH;
            }
            if (hasGoal)
            {
                y += 1f;
                var pic = goal.exit ? CuteUI.Pic.Arrow : CuteUI.Pic.Pin; var ps = CuteUI.PicSize(pic);
                CuteUI.Icon(pic, tx, y + (12f - ps.y) / 2f, 1, CuteUI.Interact);
                float gx = tx + ps.x + 4f;
                float cw = goal.cells != null ? Mathf.Round(CuteUI.Width(goal.cells, true, true) + 10f) : 0f;
                if (!string.IsNullOrEmpty(goalText)) gx = CuteUI.Label(gx, y + 6f, Fit(goalText, tx + inner - gx - (cw > 0 ? cw + 6f : 0f)), CuteUI.Sub, true) + 6f;   // 길면 잘라서 칸 수 칩이 카드 안에 남게
                if (goal.cells != null)
                {
                    var c = new Rect(gx, y, cw, 12f);
                    CuteUI.Box(c, 5, CuteUI.Paper2, CuteUI.Line, 1);
                    CuteUI.LabelCenter(c.center.x, c.center.y, goal.cells, CuteUI.Text, true, true);
                }
                y += 13f;
            }
            else if (whereW > 0) { CuteUI.Label(tx, y + 6f, Fit(where, inner), CuteUI.Sub, true); y += 12f; }

            if (extraShown > 0)
            {
                y += 3f;
                CuteUI.Fill(new Rect(r.x + 8f, y, r.width - 16f, 1f), CuteUI.Line);
                y += 4f;
                for (int i = 0; i < extraShown; i++)
                {
                    var e = extra[i]; float ex = r.x + 10f;
                    if (e.pic.HasValue) { CuteUI.Icon(e.pic.Value, ex, y + 2f); ex += 12f; }
                    y += CuteUI.Wrap(ex, y, r.xMax - 10f - ex, e.text, e.col, e.small, e.bold) + 2f;
                }
            }
            if (expanded)
            {
                y += 2f;
                CuteUI.Label(r.x + 10f, y + 6f, "카드를 누르면 여정 수첩", CuteUI.Sub, true);
            }
            // 오른쪽 '›': 누를 수 있는 카드라는 표시 (시연 피드백 "길 안내가 버튼인지 몰랐다")
            float arrowY = r.y + Mathf.Min(r.height, 44f) / 2f;
            CuteUI.Label(r.xMax - 6f - CuteUI.Width("›", false, true), arrowY, "›", hover ? CuteUI.Interact : CuteUI.Sub, false, true);
            GUI.color = old;

            if (!UIState.Blocking && GUI.Button(r, GUIContent.none, GUIStyle.none)) InputReader.Press('q');
            return r;
        }

        static string Fit(string s, float w, bool small = true, bool bold = false)
        {
            if (CuteUI.Width(s, small, bold) <= w) return s;
            while (s.Length > 2 && CuteUI.Width(s + "…", small, bold) > w) s = s.Substring(0, s.Length - 1);
            return s + "…";
        }

        /// <summary>캐릭터가 이 자리 뒤에 있으면 옅게 (가린 캐릭터가 보이게)</summary>
        public static bool PlayerBehind(Rect r)
        {
            var p = PlayerController.Instance; var cam = Camera.main;
            if (p == null || cam == null) return false;
            var s = cam.WorldToScreenPoint(p.transform.position + new Vector3(0f, .5f, 0f));
            var g = new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
            var grow = new Rect(r.x - 6f, r.y - 10f, r.width + 12f, r.height + 18f);
            return grow.Contains(g);
        }
    }
}
