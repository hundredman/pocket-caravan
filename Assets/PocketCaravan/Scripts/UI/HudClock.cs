using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 위 가운데 작은 시계 (2026-11-18 은샘 C안). 그리기만 하고 진행·저장과 상관없다.
    /// - 날짜 "3월 12일"(한 달 30일 · 열두 달 달력, 서버 날이 4000일을 넘어 "N일째"는 읽기 힘들었다) + "오후 2:20"(10분 단위).
    ///   시각이 바뀌면 옛 글이 위로 밀려 올라가며 옅어지고 새 글이 아래에서 올라온다 (0.25초, 분 단위는 정신없어서 10분 단위로).
    /// - 왼쪽 반원: 해가 새벽 5시에 수평선 아래에서 떠올라 반원을 따라 지나고 저녁 7시에 진다 (줄 아래는 잘려 안 보임).
    ///   반원과 해·달 자리는 칸에 맞춰 반올림하지 않아 화면 점 단위로 부드럽게 움직인다 (11/18 은샘 "매끄럽지 않다").
    ///   밤(19시~5시)에는 보름달이 같은 길로, 시계 칸도 어두운 밤 색으로 바뀐다.
    /// - 시계를 누르면 아래로 다음 장날(날짜 · D-n), 이번 장 돌이 5일, 오늘·내일 큰 장 마을이 펼쳐진다. 다시 누르면 접힌다.
    ///   튜토리얼 사기 단계 처음에 시계 → 펼친 달력 설명 → 닫기까지 안내한다 (TutorialSpot "clock"). 큰 장 마을을 누르면 지도에서 그 마을 카드가 펼쳐진다.
    /// 다른 화면 위에 그리지 않도록 깊이는 상태 카드(HUD)와 같고, 창이 열리면 누를 수 없다. 기념 사진 동안 감춘다.
    /// </summary>
    public class HudClock : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<HudClock>() != null) return;
            var go = new GameObject("HudClock"); DontDestroyOnLoad(go); go.AddComponent<HudClock>();
        }

        const float W = 108f, H = 30f, Rise = 5f, Set = 19f, ArcR = 15f;
        static Texture2D sunTex, moonTex;
        bool open; float openK;
        int shownMinute = -1, shownDay = -1; string dayText = "", timeText = "", prevTime, fullDate = ""; float changedAt = -9f;
        /// <summary>달력이 펼쳐져 있다 (튜토리얼: 펼치고 → 설명 → 닫으면 다음)</summary>
        public static bool IsOpen { get; private set; }

        float[] timeX, prevX;   // 글자마다 x (바뀔 때만 잰다)
        /// <summary>11/18 시각 줄: 바뀐 글자만 0.25초 동안 위로 밀려 올라가고 새 글자가 아래에서 올라온다 (줄 칸 밖은 잘린다)</summary>
        void DrawTime(Rect row, Color32 col)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - changedAt) / .25f), e = 1f - (1f - k) * (1f - k);
            bool rolling = k < 1f && prevTime != null && prevTime.Length == timeText.Length;
            GUI.BeginGroup(row);
            float cy = row.height / 2f;
            var oc = GUI.color;
            for (int i = 0; i < timeText.Length; i++)
            {
                string ch = Chars(timeText[i]);
                if (!rolling || prevTime[i] == timeText[i]) { CuteUI.Label(1 + timeX[i], cy, ch, col, true); continue; }
                GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * (1f - e));
                CuteUI.Label(1 + prevX[i], cy - e * row.height, Chars(prevTime[i]), col, true);
                GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * e);
                CuteUI.Label(1 + timeX[i], cy + (1f - e) * row.height, ch, col, true);
                GUI.color = oc;
            }
            GUI.color = oc;
            GUI.EndGroup();
        }
        static readonly System.Collections.Generic.Dictionary<char, string> charText = new System.Collections.Generic.Dictionary<char, string>();
        static string Chars(char c) { if (!charText.TryGetValue(c, out var s)) charText[c] = s = c.ToString(); return s; }
        static float[] Xs(string t)
        {
            var xs = new float[t.Length];
            for (int i = 1; i < t.Length; i++) xs[i] = CuteUI.Width(t.Substring(0, i), true);
            return xs;
        }

        static readonly Color32 NightFill = new Color32(0x1E, 0x21, 0x44, 255), NightHover = new Color32(0x2A, 0x2E, 0x58, 255),
            NightText = new Color32(0xF3, 0xEE, 0xDC, 255), NightSub = new Color32(0xB9, 0xBE, 0xDF, 255),
            NightArc = new Color32(0x6A, 0x6F, 0xA0, 255), NightLine = new Color32(0x9A, 0xA0, 0xCF, 255);
        static bool IsNight(GameClock c) { float h = c.minuteOfDay / 60f; return h < Rise || h >= Set; }

        void Update()
        {
            if (UIState.Blocking || UIState.PhotoMode) open = false;
            IsOpen = open;
            openK = Mathf.MoveTowards(openK, open ? 1f : 0f, Time.unscaledDeltaTime / .18f);
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null || UIState.PhotoMode || gm.Clock == null) return;
            UIStyle.Begin();
            GUI.depth = openK > 0f ? -5 : -2;   // 달력을 펼치면 할 일 카드보다 위
            var clock = gm.Clock;
            float vw = UIStyle.VirtualWidth, cx = Mathf.Round(vw / 2f);
            float y = CuteUI.TopStack(0, H + 98f * openK) - 4f;   // 위 가운데 맨 위. 다른 위 알약(상인회·축제 등)은 이 아래로 쌓인다
            var r = new Rect(cx - W / 2f, y, W, H);
            // 펼치면 달력 판(204) 너비까지 (튜토리얼 테두리가 시계와 달력을 함께 감싼다)
            float zw = Mathf.Lerp(W + 4f, 208f, openK);
            ScreenZones.Mark("hud.clock", new Rect(Mathf.Round(cx - zw / 2f), r.y, Mathf.Round(zw), r.height + 2 + (openK > 0f ? 98f * openK : 0f)));
            bool hover = !UIState.Blocking && r.Contains(Event.current.mousePosition);
            bool night = IsNight(clock);
            // 11/18 밤에는 시계 칸이 어두운 밤 색 (색은 두 가지로만 바꾼다: 색마다 그림이 생기므로 서서히 섞지 않는다)
            CuteUI.Box(r, 9, night ? (hover || open ? NightHover : NightFill) : hover || open ? CuteUI.Paper2 : CuteUI.Paper, open ? CuteUI.Interact : CuteUI.Ink, 1, 1, CuteUI.SoftShadow);

            DrawSky(r, clock, night);

            // 날짜 · 시각 (10분 단위. 글은 바뀔 때만 새로 만든다)
            int step = (int)clock.minuteOfDay / 10;
            if (step != shownMinute || clock.day != shownDay)
            {
                bool first = shownMinute < 0;
                if (!first) { prevTime = timeText; prevX = timeX; changedAt = Time.unscaledTime; }
                shownMinute = step; shownDay = clock.day;
                int hour = step / 6, mm = step % 6 * 10, h12 = (hour + 11) % 12 + 1;
                dayText = DateText(clock.day);
                timeText = (hour < 12 ? "오전 " : "오후 ") + h12 + ":" + mm.ToString("00");
                timeX = Xs(timeText);
                fullDate = YearText(clock.day) + " " + dayText + " " + timeText;
            }
            CuteUI.Label(r.x + 45, r.y + 10, dayText, night ? NightText : CuteUI.Text, false, true);
            DrawTime(new Rect(r.x + 44, r.y + 16, W - 48, 12), night ? NightSub : CuteUI.Sub);
            if (!UIState.Blocking && GUI.Button(r, GUIContent.none, GUIStyle.none)) { open = !open; Sfx.Play(open ? "open" : "move", .4f); }
            ScreenZones.MarkSpot("hud.clock.btn", r);   // 튜토리얼이 밝히는 자리
            if (openK > 0f) DrawCalendar(r, gm, clock);
        }

        /// <summary>몇 년인지 (360일 = 1년, 1일째 = 1년)</summary>
        public static string YearText(int day) => (Mathf.Max(0, day - 1) / 360 + 1) + "년";

        /// <summary>게임 날짜: 한 달 30일 · 열두 달 (1일째 = 1월 1일). 해는 YearText</summary>
        public static string DateText(int day)
        {
            int d = Mathf.Max(0, day - 1);
            return (d / 30 % 12 + 1) + "월 " + (d % 30 + 1) + "일";
        }
        static int DayOfMonth(int day) => Mathf.Max(0, day - 1) % 30 + 1;

        /// <summary>장날까지 남은 날 (오늘이 장날이면 0)</summary>
        static int DaysToMarket(GameClock clock)
        {
            int n = Mathf.Max(1, clock.marketDayInterval);
            int rem = clock.day % n;
            return rem == 0 ? 0 : n - rem;
        }

        // ---------------- 해 · 달 ----------------

        void DrawSky(Rect r, GameClock clock, bool night)
        {
            float baseY = r.y + 24f, ax = r.x + 22f;   // 수평선 높이 · 반원 가운데
            float h = clock.minuteOfDay / 60f;
            // 0~1: 떠서 질 때까지. 밤은 19시~다음 날 5시 (10시간)
            float p = !night ? (h - Rise) / (Set - Rise) : ((h - Set + 24f) % 24f) / (24f - (Set - Rise));
            // 반원: 11/18 은샘 "매끄럽지 않다" → 칸에 맞춰 반올림하지 않고 잘게(반 칸 점 90개) 찍는다
            var arcCol = night ? NightArc : new Color32(0xD2, 0xB2, 0x84, 255);
            for (int i = 0; i <= 90; i++)
            {
                float a = Mathf.PI * i / 90f;
                Dot(new Rect(ax - Mathf.Cos(a) * ArcR - .45f, baseY - Mathf.Sin(a) * ArcR - .45f, .9f, .9f), arcCol);
            }
            // 해·달: 양 끝에서는 수평선 아래에서 올라오고 내려간다 (줄 아래는 잘려 뜨고 지는 모습).
            // 자리는 반올림하지 않아 화면 점 단위로 부드럽게 움직인다
            float t = Mathf.Clamp01(p);
            float x = ax - Mathf.Cos(t * Mathf.PI) * ArcR;
            float dip = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f - t) / .07f);
            float yy = baseY - Mathf.Sin(t * Mathf.PI) * ArcR + dip * 5f;
            var clip = new Rect(r.x + 2, r.y + 2, 40f, baseY - (r.y + 2));   // 수평선 위만 보인다
            GUI.BeginGroup(clip);
            GUI.DrawTexture(new Rect(x - 4.5f - clip.x, yy - 4.5f - clip.y, 9, 9), night ? Moon() : Sun());
            GUI.EndGroup();
            // 수평선
            CuteUI.Fill(new Rect(ax - ArcR - 2f, baseY, ArcR * 2f + 5f, 1), night ? NightLine : CuteUI.Ink);
        }

        /// <summary>반올림하지 않는 작은 점 (흰 그림 + 색, 그림이 새로 생기지 않는다)</summary>
        static void Dot(Rect r, Color32 c)
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color; GUI.color = (Color)c * old;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static Texture2D Sun()
        {
            if (sunTex != null) return sunTex;
            sunTex = Make((x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(4f, 4f));
                return d <= 2.2f ? new Color32(0xFF, 0xE2, 0x7A, 255) : d <= 4.3f ? new Color32(0xFF, 0xB0, 0x20, 255) : new Color32(0, 0, 0, 0);
            });
            return sunTex;
        }

        /// <summary>보름달: 둥근 달 + 테두리 + 작은 얼룩 둘 (11/18 은샘: 초승달은 파인 곳이 줄을 가려 보였다)</summary>
        static Texture2D Moon()
        {
            if (moonTex != null) return moonTex;
            moonTex = Make((x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(4f, 4f));
                if (d > 4.3f) return new Color32(0, 0, 0, 0);
                if (d > 3.4f) return new Color32(0xC9, 0xB0, 0x5A, 255);
                if ((x == 3 && y == 5) || (x == 5 && y == 3) || (x == 6 && y == 5)) return new Color32(0xE2, 0xD3, 0x96, 255);
                return new Color32(0xFB, 0xF1, 0xC6, 255);
            });
            return moonTex;
        }

        static Texture2D Make(System.Func<int, int, Color32> px)
        {
            var t = new Texture2D(9, 9, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var c = new Color32[81];
            for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++) c[y * 9 + x] = px(x, y);
            t.SetPixels32(c); t.Apply();
            return t;
        }

        // ---------------- 5일 달력 ----------------

        void DrawCalendar(Rect top, GameManager gm, GameClock clock)
        {
            float ease = 1f - (1f - openK) * (1f - openK);
            const float bw = 204f, bh = 92f;   // 왼쪽 할 일 카드(오른쪽 끝 214)와 겹치지 않는 너비
            var b = new Rect(Mathf.Round(top.center.x - bw / 2f), Mathf.Round(top.yMax + 4f - (1f - ease) * 6f), bw, bh);
            ScreenZones.MarkSpot("hud.clock.panel", b);   // 튜토리얼이 펼친 달력을 밝힌다
            var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * ease);
            CuteUI.Box(b, 8, CuteUI.Paper, CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
            int n = Mathf.Max(1, clock.marketDayInterval), left = DaysToMarket(clock);
            // 1) 오늘 날짜 (해까지)  2) 다음 장날
            CuteUI.LabelCenter(b.center.x, b.y + 10, fullDate, CuteUI.Text, true, true);
            if (calDay != clock.day) { calDay = clock.day; nextText = left == 0 ? "오늘 장날!" : $"다음 장날 {DateText(clock.day + left)} · D-{left}"; }
            CuteUI.LabelCenter(b.center.x, b.y + 22, nextText, CuteUI.Sell, true, true);
            // 3) 이번 장 돌이 5일
            int start = clock.day - ((clock.day - 1) % n);   // 이번 장 돌이 첫날 (장날이 끝날)
            float cw = (bw - 16f - (n - 1) * 4f) / n;
            for (int i = 0; i < n; i++)
            {
                int d = start + i;
                bool today = d == clock.day, market = d % n == 0, past = d < clock.day;
                var cell = new Rect(Mathf.Round(b.x + 8 + i * (cw + 4f)), b.y + 31, Mathf.Floor(cw), 32);
                CuteUI.Box(cell, 5, today ? new Color32(0xE6, 0xF6, 0xF3, 255) : market ? CuteUI.SellSoft : CuteUI.Paper2,
                    today ? CuteUI.Interact : market ? CuteUI.Sell : CuteUI.Line, today ? 2 : 1);
                CuteUI.LabelCenter(cell.center.x, cell.y + 9, DayLabel(d), today ? CuteUI.Interact : past ? CuteUI.Sub : CuteUI.Text, true, true);
                string sub = market ? "장날" : today ? "오늘" : "";
                if (sub.Length > 0) CuteUI.LabelCenter(cell.center.x, cell.y + 22, sub, market ? CuteUI.Sell : CuteUI.Interact, true, market || today);
                if (past) CuteUI.Fill(new Rect(cell.x + 6, cell.y + 22, cell.width - 12, 1), CuteUI.Line);   // 지난 날
            }
            // 4) 큰 장: 오늘 · 내일 (누르면 지도에서 오늘 큰 장 마을)
            string mapNow = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (fairDay != clock.day || fairMap != mapNow)
            {
                fairDay = clock.day; fairMap = mapNow; fairText = FairText(clock.day, false, out fairMapId);
                if (fairText != null && CuteUI.Width(fairText, true, true) + 22 > bw - 12f) fairText = FairText(clock.day, true, out fairMapId);   // 길면 오늘 것만
            }
            if (fairText != null)
            {
                float fw = CuteUI.Width(fairText, true, true) + 22f;
                var fr = new Rect(Mathf.Round(b.center.x - fw / 2f), b.y + 68, Mathf.Round(fw), 17);
                bool fh = !UIState.Blocking && fr.Contains(Event.current.mousePosition);
                CuteUI.Box(fr, 6, fh ? CuteUI.Paper2 : CuteUI.Paper, fh ? CuteUI.Interact : CuteUI.Line, 1);
                CuteUI.Icon(CuteUI.Pic.Pin, fr.x + 5, fr.center.y - 4, 1, CuteUI.Interact);
                CuteUI.Label(fr.x + 15, fr.center.y, fairText, CuteUI.Interact, true, true);
                ScreenZones.MarkSpot("hud.clock.fair", fr);
                if (ease >= 1f && !UIState.Blocking && GUI.Button(fr, GUIContent.none, GUIStyle.none))
                {
                    // 지도를 열고 그 마을 카드부터 (11/18 은샘)
                    MapTownCard.Pending = fairMapId; open = false; IsOpen = false; InputReader.Press('m'); Sfx.Play("open", .5f);
                }
            }
            GUI.color = oc;
        }

        int calDay = -1, fairDay = -1; string nextText = "", fairText, fairMap, fairMapId;
        readonly System.Collections.Generic.Dictionary<int, string> dayLabels = new System.Collections.Generic.Dictionary<int, string>();
        string DayLabel(int d) { int m = DayOfMonth(d); if (!dayLabels.TryGetValue(m, out var s)) dayLabels[m] = s = m + "일"; return s; }

        /// <summary>오늘·내일 큰 장 마을 한 줄 (지금 지역 쪽 중앙 조각). 고려는 동쪽, 베네치아는 서쪽, 북유럽은 북쪽, 아라비아는 남쪽</summary>
        static string FairText(int day, bool todayOnly, out string todayMap)
        {
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            string chunk = CentralFair.ChunkOfMap(map);
            if (chunk == null)
                switch (Reactions.RegionOfMap(map))
                {
                    case Reactions.Region.Venice: chunk = "west"; break;
                    case Reactions.Region.North: chunk = "north"; break;
                    case Reactions.Region.Arabia: chunk = "south"; break;
                    default: chunk = "east"; break;
                }
            var a = CentralFair.On(chunk, day); var b = CentralFair.On(chunk, day + 1);
            todayMap = a.mapId;
            if (string.IsNullOrEmpty(a.name)) return null;
            return todayOnly || a.name == b.name ? $"오늘 큰 장: {a.name}" : $"큰 장: 오늘 {a.name} · 내일 {b.name}";
        }
    }
}
