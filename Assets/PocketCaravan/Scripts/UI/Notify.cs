using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 아래쪽 가운데에 잠깐 뜨는 알림 (퀘스트 수락·완료, 마을 발전 등). 11/05 전엔 위쪽 가운데.
    /// 11/11 윤채: 글만 길게 이어져 한눈에 안 들어오고, 여러 장이 쌓여 조잡해서 다시 짰다 (부르는 쪽은 그대로).
    /// - 모든 알림이 같은 카드 모양: 왼쪽 동그란 그림 + (머리글) + 글. 종류는 그림·색으로 바로 보인다
    ///   퀘스트 수락 · 완료(보상은 돈·신용 칩, 바로 뒤 다음 이야기는 아랫줄로 합침) · 다음 이야기 · 이벤트 · 오늘의 큰 장 · 떠돌이 상인 · 그 밖(전구·동전·자루)
    /// - 한 번에 한 장. 기다리는 알림이 있으면 지금 장은 조금 일찍 넘기고 귀퉁이에 "+N"
    /// - 늘 화면 가운데 (목표 줄 바로 위). 옆에 채팅 등이 있으면 옆으로 밀지 않고 좁히거나 그 위로 올린다
    /// - 맵에 들어설 때 맵 이름은 아래 알림 줄에 끼지 않고, 화면 위쪽 가운데 리본으로 잠깐 띄운다 (Arrive)
    /// 아래에서 살짝 올라오며 나타나고, 끝날 때 옅어진다.
    /// </summary>
    public static class Notify
    {
        enum Kind { Plain, Event, QuestNew, QuestDone, Story, News }
        struct Entry
        {
            public string text, key, title, note, head, chip; public float until, seconds, added; public Kind kind; public int money, credit;
            public CuteUI.Pic pic; public Color32 accent;
        }
        static readonly List<Entry> entries = new List<Entry>();
        /// <summary>11/11 은샘: 한 번에 보이는 알림 수 (윤채: 둘도 조잡해서 한 장). 나머지는 앞 알림이 사라지면 이어서 뜬다</summary>
        const int MaxVisible = 1, MaxQueued = 6;
        /// <summary>뒤에 기다리는 알림이 있을 때 한 장이 적어도 떠 있는 시간</summary>
        const float MinShown = 1.8f;
        const float MaxW = 300f, Medal = 22f, TextX = 34f, PadR = 10f, ChipGap = 4f, NoteX = 30f, ArriveSeconds = 2.6f;

        static readonly Regex DoneRx = new Regex(@"^퀘스트 완료: (.+?)(?: 돈 \+(\d+))?(?: 신용 \+(\d+))?$");
        static readonly Regex NewRx = new Regex(@"^퀘스트 수락: (.+)$");
        static readonly Regex PercentRx = new Regex(@"(\d+)%");
        static readonly Color32 CreditSoft = new Color32(0xDD, 0xF1, 0xEF, 0xFF);

        // 맵 이름 리본 (도착)
        static string arriveText; static float arriveAt = -99f;

        /// <summary>
        /// 11/11 은샘: 알림에서 띄어 쓴 괄호 안 덧붙임은 뺀다 ("(★ 표시를 찾아가 보자)", "(팔면 20% 더)" 등). 짧게 한 줄로.
        /// 낱말에 붙은 괄호("을(를)")는 그대로 둔다. 빈 줄이 되면 줄째 뺀다
        /// </summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string t = Regex.Replace(text, @"(?m)(^|[ \t])\([^()\n]*\)", "$1");
            var lines = new List<string>();
            foreach (var line in t.Split('\n'))
            {
                string l = Regex.Replace(line, @"[ \t]{2,}", " ").Trim();
                if (l.Length > 0) lines.Add(l);
            }
            return lines.Count > 0 ? string.Join("\n", lines) : text;
        }

        static void Add(Entry e)
        {
            e.text = Clean(e.text);
            if (string.IsNullOrEmpty(e.text)) return;
            // 맵 이름 = 도착 리본 (아래 알림 줄에 끼지 않는다)
            if (e.key == "location" || IsMapName(e.text)) { arriveText = CuteUI.Plain(e.text); arriveAt = Time.unscaledTime; return; }
            foreach (var x in entries) if (x.text == e.text) return;   // 같은 알림이 떠 있거나 기다리면 또 넣지 않는다
            if (e.kind == Kind.Plain) Classify(ref e);
            Style(ref e);
            // 퀘스트 완료 바로 뒤에 오는 다음 이야기는 완료 카드 아랫줄로 (두 장이 따로 뜨면 조잡하다)
            if (e.kind == Kind.Story && entries.Count > 0)
            {
                int last = entries.Count - 1; var prev = entries[last];
                if (prev.kind == Kind.QuestDone && string.IsNullOrEmpty(prev.note) && Time.unscaledTime - prev.added < 1f)
                {
                    prev.note = CuteUI.Plain(e.text);
                    prev.seconds = Mathf.Max(prev.seconds, e.seconds);
                    if (prev.until > 0f) prev.until = Mathf.Max(prev.until, Time.unscaledTime + e.seconds);
                    entries[last] = prev;
                    return;
                }
            }
            e.added = Time.unscaledTime;
            e.until = -1f;   // 자리가 나면 그때부터 시간이 간다
            entries.Add(e);
            while (entries.Count > MaxQueued) entries.RemoveAt(0);
        }

        static bool IsMapName(string t)
        {
            var map = MapRoot.Current; var index = WorldIndex.Get();
            return map != null && index != null && !t.Contains("\n") && t == index.MapName(map.mapId);
        }

        /// <summary>글로 알림 종류를 알아본다: "퀘스트 완료: 제목 돈 +20 신용 +1" · "퀘스트 수락: 제목" · "큰 장이 섰다! …" · "오늘 떠돌이 상인 …"</summary>
        static void Classify(ref Entry e)
        {
            string t = CuteUI.Plain(e.text ?? "");
            var m = DoneRx.Match(t);
            if (m.Success)
            {
                e.kind = Kind.QuestDone; e.title = m.Groups[1].Value;
                if (m.Groups[2].Success) int.TryParse(m.Groups[2].Value, out e.money);
                if (m.Groups[3].Success) int.TryParse(m.Groups[3].Value, out e.credit);
                return;
            }
            m = NewRx.Match(t);
            if (m.Success) { e.kind = Kind.QuestNew; e.title = m.Groups[1].Value; return; }
            if (t.StartsWith("큰 장이 섰다"))
            {
                e.kind = Kind.News; e.head = "오늘의 큰 장"; e.pic = CuteUI.Pic.Chart; e.accent = CuteUI.Sell;   // 동전은 흰색 한 가지로 칠하면 원판만 남아서 시세 막대
                e.title = t.Substring("큰 장이 섰다".Length).TrimStart('!', ' ', '.');
                var p = PercentRx.Match(t); if (p.Success) e.chip = "+" + p.Groups[1].Value + "%";
                return;
            }
            if (t.StartsWith("오늘 ") && t.Contains("떠돌이"))
            {
                e.kind = Kind.News; e.head = "떠돌이 상인"; e.pic = CuteUI.Pic.Sack; e.accent = CuteUI.Buy;
                e.title = t.Substring(3);
            }
        }

        /// <summary>종류마다 머리글 · 그림 · 색. 그 밖의 알림(Plain)은 글로 그림만 고른다</summary>
        static void Style(ref Entry e)
        {
            switch (e.kind)
            {
                case Kind.QuestNew: e.head = "새 퀘스트"; e.pic = CuteUI.Pic.Scroll; e.accent = CuteUI.Interact; break;
                case Kind.QuestDone: e.head = "퀘스트 완료"; e.pic = CuteUI.Pic.Check; e.accent = CuteUI.Up; break;
                case Kind.Story: e.head = "다음 이야기"; e.pic = CuteUI.Pic.OpenBook; e.accent = CuteUI.FrameLo; break;
                case Kind.Event: e.head = "이벤트"; e.pic = CuteUI.Pic.Star5; e.accent = CuteUI.EventLine; break;
                case Kind.News: break;   // Classify에서 정했다
                default:
                    string t = e.text;
                    if (t.Contains("냥")) { e.pic = CuteUI.Pic.Coin7; e.accent = CuteUI.Sell; }
                    else if (t.Contains("배웠다")) { e.pic = CuteUI.Pic.Star5; e.accent = CuteUI.Frame; }
                    else if (t.Contains("짐")) { e.pic = CuteUI.Pic.Sack; e.accent = CuteUI.FrameLo; }
                    else { e.pic = CuteUI.Pic.Bulb; e.accent = CuteUI.Frame; }
                    break;
            }
        }

        public static void Show(string text, float seconds = 3.5f) => Add(new Entry { text = text, seconds = seconds });

        /// <summary>11/11 은샘: 이벤트 퀘스트 알림 (만국 상인회 등). 금빛 종이·주황 테로 다른 알림과 구분한다</summary>
        public static void Event(string text, float seconds = 4f) => Add(new Entry { text = text, seconds = seconds, kind = Kind.Event });

        /// <summary>11/11 윤채: 퀘스트를 끝낸 뒤 "다음 이야기" 한 줄 (누가 무엇을 부탁하려 한다 등). 책 그림 + 머리글</summary>
        public static void Story(string text, float seconds = 6f) => Add(new Entry { text = text, seconds = seconds, kind = Kind.Story });

        public static void Clear() { entries.Clear(); arriveAt = -99f; }

        /// <summary>맵 이름 (고려 맵이 부른다). 11/11 화면 위쪽 가운데 리본으로</summary>
        public static void Location(string text,float seconds) => Add(new Entry{text=text,key="location",seconds=seconds});

        // ---------------- 크기 · 그리기 ----------------

        /// <summary>제목을 굵게 쓰는 종류 (퀘스트 이름)</summary>
        static bool BoldBody(Entry e) => e.kind == Kind.QuestNew || e.kind == Kind.QuestDone;
        static string Body(Entry e) => CuteUI.Plain(e.title ?? e.text);

        static string MoneyText(Entry e) => $"+{e.money}";
        static string CreditText(Entry e) => $"신용 +{e.credit}";
        static float ChipW(string s, bool icon) => Mathf.Round((icon ? 13f : 6f) + CuteUI.Width(s, true, true) + 6f);
        static float ChipsWidth(Entry e)
        {
            float w = 0f;
            if (e.money > 0) w += ChipW(MoneyText(e), true);
            if (e.credit > 0) w += (w > 0 ? ChipGap : 0f) + ChipW(CreditText(e), false);
            if (!string.IsNullOrEmpty(e.chip)) w += (w > 0 ? ChipGap : 0f) + ChipW(e.chip, false);
            return w;
        }

        /// <summary>그릴 크기. maxW 안에서 글에 맞춘다</summary>
        static Vector2 Measure(Entry e, float maxW)
        {
            string body = Body(e);
            bool head = !string.IsNullOrEmpty(e.head);
            float chips = ChipsWidth(e), chipsRoom = chips > 0f ? chips + 8f : 0f;
            float natural = TextX + Mathf.Max(head ? CuteUI.Width(e.head, true, true) : 0f, CuteUI.Width(body, false, BoldBody(e))) + chipsRoom + PadR;
            if (!string.IsNullOrEmpty(e.note)) natural = Mathf.Max(natural, TextX + NoteX + CuteUI.Width(e.note, true) + PadR);
            float w = Mathf.Max(120f, Mathf.Min(maxW, natural));
            float tw = w - TextX - PadR - chipsRoom;
            float bh = CuteUI.WrapHeight(body, tw);
            float nh = string.IsNullOrEmpty(e.note) ? 0f : 5f + CuteUI.WrapHeight(e.note, w - TextX - PadR - NoteX, true);
            float h = head ? 5f + 11f + bh + nh + 5f : 6f + bh + nh + 6f;
            return new Vector2(Mathf.Round(w), Mathf.Round(Mathf.Max(30f, h)));
        }

        static void Paint(Rect r, Entry e)
        {
            bool rich = e.kind != Kind.Plain, hasNote = !string.IsNullOrEmpty(e.note), head = !string.IsNullOrEmpty(e.head);
            if (e.kind == Kind.Event) CuteUI.Box(r, 8, CuteUI.EventPaper, CuteUI.EventLine, 2, 2);
            else CuteUI.Box(r, 8, CuteUI.Paper, CuteUI.Ink, 1, 2);
            // 왼쪽 동그란 그림: 종류가 있는 알림은 색 원판 + 흰 그림, 그 밖은 옅은 원판 + 색 그림 (덜 튀게)
            float topH = hasNote ? 32f : r.height;
            float my = Mathf.Round(r.y + topH / 2f - Medal / 2f);
            if (rich) CuteUI.Circle(r.x + 7f, my, (int)Medal, e.accent, CuteUI.Ink, 1);
            else CuteUI.Circle(r.x + 7f, my, (int)Medal, CuteUI.Paper2, CuteUI.Line, 1);
            // 작은 그림(★ 7칸 · 동전 9칸 등)은 두 배로 (한 배면 22칸 동그라미 안에서 "+"처럼 작게 보였다)
            var ps = CuteUI.PicSize(e.pic);
            int sc = Mathf.Max(ps.x, ps.y) <= 10 ? 2 : 1;
            CuteUI.Icon(e.pic, Mathf.Round(r.x + 7f + (Medal - ps.x * sc) / 2f), Mathf.Round(my + (Medal - ps.y * sc) / 2f), sc, rich ? CuteUI.White : e.accent);
            // 칩 (오른쪽): 돈 · 신용 · 덤
            float chips = ChipsWidth(e), right = r.xMax - PadR;
            if (chips > 0f)
            {
                float cx = right - chips, cy = Mathf.Round(r.y + topH / 2f - 8f);
                if (e.money > 0)
                {
                    string s = MoneyText(e); float cw = ChipW(s, true);
                    CuteUI.Box(new Rect(cx, cy, cw, 16f), 6, CuteUI.SellSoft, CuteUI.Sell, 1);
                    CuteUI.Icon(CuteUI.Pic.Coin7, cx + 4f, cy + 4.5f);
                    CuteUI.Label(cx + 13f, cy + 8f, s, CuteUI.Sell, true, true);
                    cx += cw + ChipGap;
                }
                if (e.credit > 0)
                {
                    string s = CreditText(e); float cw = ChipW(s, false);
                    CuteUI.Box(new Rect(cx, cy, cw, 16f), 6, CreditSoft, CuteUI.Interact, 1);
                    CuteUI.Label(cx + 6f, cy + 8f, s, CuteUI.InteractLo, true, true);
                    cx += cw + ChipGap;
                }
                if (!string.IsNullOrEmpty(e.chip))
                {
                    float cw = ChipW(e.chip, false);
                    CuteUI.Box(new Rect(cx, cy, cw, 16f), 6, CuteUI.SellSoft, CuteUI.Sell, 1);
                    CuteUI.Label(cx + 6f, cy + 8f, e.chip, CuteUI.Sell, true, true);
                }
                right -= chips + 8f;
            }
            // 머리글 + 글
            float tx = r.x + TextX, tw = right - tx, by;
            if (head)
            {
                CuteUI.Label(tx, r.y + 10f, e.head, e.accent, true, true);
                by = r.y + 16f + CuteUI.Wrap(tx, r.y + 16f, tw, Body(e), CuteUI.Text, false, BoldBody(e));
            }
            else
            {
                float bh = CuteUI.WrapHeight(Body(e), tw);
                float y0 = hasNote ? r.y + 6f : Mathf.Round(r.y + (r.height - bh) / 2f);
                by = y0 + CuteUI.Wrap(tx, y0, tw, Body(e), CuteUI.Text);
            }
            if (hasNote)
            {
                // 아랫줄: 옅은 구분선 + "다음 ›" + 다음 이야기
                CuteUI.Fill(new Rect(tx, by + 2f, r.xMax - PadR - tx, 1f), CuteUI.Line);
                CuteUI.Label(tx, by + 11f, "다음 ›", CuteUI.FrameLo, true, true);
                CuteUI.Wrap(tx + NoteX, by + 5f, r.xMax - PadR - tx - NoteX, e.note, CuteUI.Sub, true);
            }
        }

        /// <summary>기다리는 알림 수 (오른쪽 위 귀퉁이 "+N")</summary>
        static void Waiting(Rect r, int n)
        {
            if (n <= 0) return;
            string s = "+" + n;
            float w = Mathf.Max(16f, Mathf.Round(CuteUI.Width(s, true, true) + 8f));
            var b = new Rect(Mathf.Round(r.xMax - w + 4f), Mathf.Round(r.y - 6f), w, 13f);
            CuteUI.Box(b, 6, CuteUI.Ink, CuteUI.Ink, 1);
            CuteUI.LabelCenter(b.center.x, b.center.y, s, CuteUI.White, true, true);
        }

        /// <summary>맵 이름 리본: 화면 위쪽 가운데에 내려왔다가 옅어진다</summary>
        static void DrawArrive(float now)
        {
            float t = now - arriveAt;
            if (string.IsNullOrEmpty(arriveText) || t < 0f || t > ArriveSeconds) return;
            if (UIState.Blocking) return;
            float k = Mathf.Clamp01(t / .25f), fade = Mathf.Clamp01((ArriveSeconds - t) / .5f);
            var old = GUI.color;
            GUI.color = new Color(old.r, old.g, old.b, old.a * k * fade);
            float y = Mathf.Round(UIStyle.VirtualHeight * .2f - (1f - k) * 8f);
            CuteUI.TitlePlate(UIStyle.VirtualWidth / 2f, y, arriveText);
            GUI.color = old;
        }

        /// <summary>
        /// OnGUI 안에서 부른다 (UIStyle.Begin 이후).
        /// 11/05 B안: 종이 상자로, 화면 아래 가운데에서 위로 쌓는다 (위쪽 카드·할 일 줄을 덮지 않게).
        /// 어두운 바탕용 색 태그는 종이 위에서 안 읽혀서 뺀다.
        /// </summary>
        public static readonly List<Rect> DrawnRects=new List<Rect>();
        public static void Draw(Rect reserved=default)
        {
            bool record=!Application.isEditor||Camera.main==null||(Camera.main.pixelWidth==Screen.width&&Camera.main.pixelHeight==Screen.height);
            if(record)DrawnRects.Clear();
            float now = Time.unscaledTime;
            DrawArrive(now);
            entries.RemoveAll(e => e.until > 0f && now > e.until);
            // 11/11 앞에서부터 MaxVisible개만 보인다. 기다리던 알림은 보이기 시작할 때부터 시간을 잰다
            int shown = 0;
            for (int i = 0; i < entries.Count && shown < MaxVisible; i++)
            {
                var e = entries[i];
                if (e.until < 0f) { e.until = now + e.seconds; entries[i] = e; }
                // 뒤에 기다리는 알림이 있으면 적어도 MinShown초는 보이고 넘긴다 (하나씩 띄우니 줄이 길어지지 않게)
                if (entries.Count > MaxVisible)
                {
                    float start = e.until - e.seconds, early = start + Mathf.Max(MinShown, Mathf.Min(e.seconds, 2.6f));
                    if (early < e.until) { e.until = Mathf.Max(early, now + .35f); entries[i] = e; }
                }
                shown++;
            }
            float vw = UIStyle.VirtualWidth, cx = vw / 2f;
            float bottom = UIStyle.VirtualHeight - 8 - HUD.BottomReserved;
            var talk = DialogueUI.ShownRect;   // 11/08 대화창이 떠 있으면 그 위로 (대화창이 알림을 덮었다)
            if (talk.HasValue) bottom = Mathf.Min(bottom, talk.Value.y - 14f);   // 왼쪽 위에 걸친 이름표까지 비킨다
            var old = GUI.color;
            for (int i = Mathf.Min(entries.Count, MaxVisible) - 1; i >= 0; i--)   // 새 알림이 맨 아래 (보이는 것만)
            {
                var e = entries[i];
                var size = Measure(e, Mathf.Min(MaxW, vw - 40f));
                // 11/11 늘 화면 가운데 (목표 줄과 한 줄로). 양옆 카드(왼쪽 채팅 · 오른쪽 고려 배우기 카드) 사이가 좁으면
                //   옆으로 밀지 않고 좁히고, 그래도 안 되면 그 카드들 위로 올린다
                for (int pass = 0; pass < 4; pass++)
                {
                    var (left, right) = ScreenZones.FreeRow(bottom - size.y, bottom);
                    float room = 2f * Mathf.Min(cx - left, right - cx);
                    if (room >= size.x) break;
                    if (room >= 200f) { size = Measure(e, room); break; }
                    bottom = ScreenZones.TopOf(bottom - size.y, bottom) - 5f;
                }
                var r = new Rect(Mathf.Round(cx - size.x / 2f), Mathf.Round(bottom - size.y), size.x, size.y);
                if(reserved.width>0&&r.Overlaps(reserved))   // 고려 배우기 카드 (ScreenZones로도 비키지만 처음 한 프레임을 위해 그대로 둔다)
                    r.y=Mathf.Min(r.y,reserved.yMin-r.height-6);
                if(record)DrawnRects.Add(r);
                // 11/11 나타날 때 아래에서 살짝 올라오고, 끝날 때 옅어진다
                float since = now - (e.until - e.seconds), k = Mathf.Clamp01(since / .18f), fade = Mathf.Clamp01((e.until - now) / .35f);
                GUI.color = new Color(old.r, old.g, old.b, old.a * k * fade);
                var drawn = r; drawn.y = Mathf.Round(r.y + (1f - k) * 6f);
                Paint(drawn, e);
                if (i == 0) Waiting(drawn, entries.Count - MaxVisible);
                GUI.color = old;
                bottom = r.y - 5f;
            }
        }
    }
}
