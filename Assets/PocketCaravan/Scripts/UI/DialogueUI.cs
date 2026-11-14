using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 아래 대화창. DialogueUI.Show(이름, 대사, 선택지...)로 연다.
    /// 실제 글꼴·창 크기로 줄과 쪽을 나눈다 (WrapLines·Layout, 한 쪽에 글 두 줄까지). Space로 다음 쪽, 마지막 쪽에서만 선택/닫기.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public struct Choice
        {
            public string label;
            public Action action;
            public Choice(string label, Action action) { this.label = label; this.action = action; }
        }

        static DialogueUI instance;
        string speaker, text;
        readonly List<Choice> choices = new List<Choice>();
        bool open;
        sealed class Pending { public string speaker, text; public Choice[] choices; public int scene, version; }
        readonly Queue<Pending> pending = new Queue<Pending>();
        int openedFrame;
        readonly List<string> pages = new List<string>();
        readonly List<int> pageSeen = new List<int>();   // 쪽마다 그 앞까지 나온 글자 수 (빈칸 빼고)
        int pageIndex;
        float pageWidth;
        GUIStyle bodyStyle;
        Vector2 choiceScroll;
        Rect lastWindow, choiceViewport;
        readonly List<Rect> choiceRects = new List<Rect>();
        static Rect shownRect;
        static int shownFrame = -9;
        /// <summary>11/08 대화창이 지금 화면에 그려져 있으면 그 자리 (알림이 그 위로 비킨다)</summary>
        public static Rect? ShownRect => Time.frameCount - shownFrame <= 1 ? shownRect : (Rect?)null;
        GUIStyle wrappedChoice, wrappedChoiceOn;
        /// <summary>11/08 키보드로 고른 선택지 (←→·↑↓로 옮기고 Space로 정한다). 예전엔 Space가 늘 첫 번째라 가운데 선택지는 마우스로만 골랐다</summary>
        int focus;
        const float BodyHeight = 52f; // 기본 글꼴 3줄 + 레이블 패딩

        public int PageCount => pages.Count;
        public int PageIndex => pageIndex;
        public string CurrentPage => pages.Count > 0 ? pages[pageIndex] : "";
        public bool ChoicesVisible => open && pages.Count > 0 && pageIndex == pages.Count - 1;

        void Awake() => instance = this;

        public static void Show(string speaker, string text, params Choice[] choices)
        {
            if (instance == null) return;
            var d = instance;
            if (!UIState.TryOpen(d, d.Close))
            {
                d.pending.Enqueue(new Pending { speaker=speaker, text=text, choices=choices,
                    version=UIState.ResetVersion, scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle });
                return;
            }
            d.speaker = speaker;
            d.text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            d.pages.Clear(); d.pageSeen.Clear();
            d.pageIndex = 0;
            d.pageWidth = 0;
            d.choices.Clear(); d.choiceScroll = Vector2.zero; d.focus = 0;
            if (choices != null) d.choices.AddRange(choices);
            d.open = true;
            d.openedFrame = Time.frameCount;
        }

        void Close()
        {
            open = false;
            UIState.Release(this);
        }

        void OnDisable() { pending.Clear(); if (open) Close(); }

        void Pick(Choice c)
        {
            Close();
            c.action?.Invoke(); // 다음 대화를 열 수 있으니 닫은 뒤에 부른다
        }

        void Update()
        {
            if (!open && pending.Count > 0 && !UIState.Blocking && !UIState.JustClosed)
            {
                var next=pending.Dequeue();
                if(next.version==UIState.ResetVersion && next.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle) Show(next.speaker,next.text,next.choices);
                return;
            }
            if (!open || pages.Count == 0 || Time.frameCount == openedFrame) return;
            if (!ChoicesVisible)
            {
                if (InputReader.InteractPressed() || InputReader.ConfirmPressed() || InputReader.CancelPressed()) NextPage();
                return;
            }
            if (choices.Count == 0)
            {
                if (InputReader.InteractPressed() || InputReader.ConfirmPressed() || InputReader.CancelPressed()) Close();
                return;
            }
            // 11/08 ←→·↑↓로 고르기 (가로 단추는 왼쪽부터, 세로 목록은 위부터) · 숫자 1~9로 바로 고르기 · Space = 고른 것 · Esc = 마지막 선택지
            var mv = InputReader.MovePressed();
            int step = mv.x != 0 ? mv.x : -mv.y;
            if (step != 0) { focus = (focus + step + choices.Count) % choices.Count; Sfx.Play("open", .25f); }
            focus = Mathf.Clamp(focus, 0, choices.Count - 1);
            int number = InputReader.NumberPressed();
            if (number > 0 && number <= choices.Count) Pick(choices[number - 1]);
            else if (InputReader.CancelPressed()) Pick(choices[choices.Count - 1]);
            else if (InputReader.InteractPressed() || InputReader.ConfirmPressed()) Pick(choices[focus]);
        }

        void NextPage()
        {
            if (pageIndex >= pages.Count - 1) return;
            pageIndex++;
            openedFrame = Time.frameCount;
        }

        /// <summary>미리 나눈 한 줄. start = 대사 한 줄(원문의 줄)의 첫 줄, unit = 원문의 몇 번째 줄에서 나왔는지</summary>
        public struct Line { public string text; public bool start; public int unit; }

        /// <summary>
        /// 11/11 은샘: 대사를 창 너비에 맞춰 미리 줄을 나눈다. Unity 글자 배치는 한글을 글자마다 줄바꿈해서
        /// "주고받 / 고", "비 / 요른"처럼 단어가 갈리고, 바뀐 줄 앞에 빈칸이 남았다.
        /// 원문의 한 줄(\n 사이)을 한 덩어리로 보고, 그 안에서
        /// 1) 다음 문장이 이 줄에 다 안 들어가면(그 문장이 한 줄에는 들어갈 때) 다음 줄에서 문장을 시작한다.
        /// 2) 한 줄보다 긴 문장은 이어 쓰되 띄어쓰기에서 줄을 바꾼다. 한 줄보다 긴 단어만 글자 단위로 자른다.
        /// 원문의 빈 줄은 덩어리 사이 구분으로만 쓴다 (쪽 안의 여백은 Layout이 정한다).
        /// </summary>
        public static List<Line> WrapLines(string source, GUIStyle style, float width)
        {
            var lines = new List<Line>();
            if (string.IsNullOrEmpty(source) || style == null || width <= 0f) return lines;
            var plain = new GUIStyle(style) { wordWrap = false };
            float limit = width - 2f;   // 재는 너비와 실제 줄바꿈의 반올림 차이로 한 줄이 두 줄이 되지 않게
            System.Func<string, bool> fits = line => plain.CalcSize(new GUIContent(line)).x <= limit;
            int unit = -1;
            foreach (string para in source.Split('\n'))
            {
                if (para.Trim().Length == 0) continue;
                unit++;
                bool first = true;
                string cur = "";
                System.Action<string> push = t => { lines.Add(new Line { text = t, start = first, unit = unit }); first = false; };
                foreach (string sentence in Sentences(para))
                {
                    string joined = cur.Length == 0 ? sentence : cur + " " + sentence;
                    if (fits(joined)) { cur = joined; continue; }
                    if (cur.Length > 0 && fits(sentence)) { push(cur); cur = sentence; continue; }   // 1) 다음 줄에서 문장 시작
                    foreach (string word in sentence.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))   // 2) 단어 묶음
                    {
                        string next = cur.Length == 0 ? word : cur + " " + word;
                        if (fits(next)) { cur = next; continue; }
                        if (cur.Length > 0) push(cur);
                        cur = word;
                        while (cur.Length > 1 && !fits(cur))   // 한 줄보다 긴 단어
                        {
                            int[] cs = StringInfo.ParseCombiningCharacters(cur);
                            int n = cs.Length - 1;
                            while (n > 1 && !fits(cur.Substring(0, cs[n]))) n--;
                            push(cur.Substring(0, cs[n]));
                            cur = cur.Substring(cs[n]);
                        }
                    }
                }
                if (cur.Length > 0) push(cur);
            }
            return lines;
        }

        /// <summary>미리 나눈 줄을 한 글로 (확인·시험용)</summary>
        public static string Wrap(string source, GUIStyle style, float width)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var l in WrapLines(source, style, width)) { if (sb.Length > 0) sb.Append('\n'); sb.Append(l.text); }
            return sb.ToString();
        }

        /// <summary>
        /// 11/11 은샘: 쪽 나누기. 한 쪽에 글은 두 줄까지 (세 줄이 붙어 있으면 읽기 어렵다).
        /// - 원문의 다른 줄 두 개면 가운데 빈 줄을 두고, 원문 한 줄이 두 줄로 이어지면 붙여 둔다.
        /// - 지금 쪽에 한 줄이 있는데 다음 덩어리가 두 줄 이상이면 그 덩어리는 다음 쪽에서 시작한다 (덩어리가 쪽 사이로 갈리지 않게).
        /// - 그리는 쪽에서 글 높이에 맞춰 위아래 가운데에 놓는다.
        /// seen = 쪽마다 그 앞까지 나온 글자 수 (창 크기가 바뀌어도 보던 쪽을 찾는다)
        /// </summary>
        public static List<string> Layout(List<Line> lines, List<int> seen = null)
        {
            var pages = new List<string>();
            seen?.Clear();
            int i = 0, shown = 0;
            while (i < lines.Count)
            {
                var a = lines[i];
                seen?.Add(shown);
                string page = a.text; int used = 1;
                if (i + 1 < lines.Count)
                {
                    var b = lines[i + 1];
                    bool sameSentence = b.unit == a.unit;
                    int bLen = 1; while (i + 1 + bLen < lines.Count && lines[i + 1 + bLen].unit == b.unit) bLen++;
                    if (sameSentence) { page = a.text + "\n" + b.text; used = 2; }
                    else if (!a.start) { }   // 긴 덩어리의 마지막 줄이면 혼자 둔다
                    else if (bLen == 1) { page = a.text + "\n\n" + b.text; used = 2; }
                }
                for (int k = 0; k < used; k++) foreach (char c in lines[i + k].text) if (!char.IsWhiteSpace(c)) shown++;
                pages.Add(page);
                i += used;
            }
            if (pages.Count == 0) { pages.Add(""); seen?.Add(0); }
            return pages;
        }

        /// <summary>문단을 문장으로 나눈다: . ? ! 뒤에 띄어쓰기가 오면 문장 끝 (문장 부호는 앞 문장에 붙는다)</summary>
        static IEnumerable<string> Sentences(string para)
        {
            int start = 0;
            for (int i = 0; i < para.Length; i++)
            {
                char c = para[i];
                bool end = (c == '.' || c == '?' || c == '!') && i + 1 < para.Length && para[i + 1] == ' ';
                if (!end) continue;
                string s = para.Substring(start, i + 1 - start).Trim();
                if (s.Length > 0) yield return s;
                start = i + 1;
            }
            string rest = para.Substring(start).Trim();
            if (rest.Length > 0) yield return rest;
        }


        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -10;   // 다른 글자들 위에
            if (bodyStyle == null || bodyStyle.font != UIStyle.RegularFont)
            {
                bodyStyle = new GUIStyle(CuteUI.PaperWrap) { richText = false, clipping = TextClipping.Clip };   // 11/05 UI B안: 종이 위 진한 글씨
                pageWidth = -1;
            }
            float w = Mathf.Min(460, UIStyle.VirtualWidth - 20), h = 104;
            float bodyWidth = w - 20;
            if (pages.Count == 0 || !Mathf.Approximately(pageWidth, bodyWidth))
            {
                // 보던 쪽 앞까지의 글자 수 (빈칸·줄바꿈 빼고). 창 크기가 바뀌어 줄을 다시 나눠도 그 쪽을 이어 본다
                int seen = pageIndex < pageSeen.Count ? pageSeen[pageIndex] : 0;
                pages.Clear();
                pages.AddRange(Layout(WrapLines(text, bodyStyle, bodyWidth), pageSeen));
                pageIndex = 0;
                while (pageIndex < pages.Count - 1 && pageSeen[pageIndex + 1] <= seen) pageIndex++;
                pageWidth = bodyWidth;
            }
            if (wrappedChoice == null || wrappedChoice.font != UIStyle.RegularFont)
            {
                wrappedChoice = new GUIStyle(CuteUI.PaperButton) { wordWrap = true, clipping = TextClipping.Clip };
                wrappedChoiceOn = new GUIStyle(CuteUI.PaperButtonOn) { wordWrap = true, clipping = TextClipping.Clip };
            }
            var labels = new List<string>(); var widths = new List<float>();
            float totalWidth = 0; bool multiline = false;
            for (int i = 0; i < choices.Count; i++)
            {
                // 11/08 Space는 지금 고른 선택지에 붙는다. 셋 이상이면 숫자도 붙여 바로 고를 수 있게
                string label = (choices.Count >= 3 ? (i + 1) + ". " : "") + choices[i].label
                    + (i == focus ? " (Space)" : i == choices.Count - 1 ? " (Esc)" : "");
                labels.Add(label); multiline |= label.IndexOf('\n') >= 0 || label.IndexOf('\r') >= 0;
                float width = Mathf.Max(60f, CuteUI.PaperButton.CalcSize(new GUIContent(label)).x + 14f);
                widths.Add(width); totalWidth += width + (i > 0 ? 6 : 0);
            }
            bool stacked = ChoicesVisible && (totalWidth > bodyWidth || multiline);
            float pageReserve = stacked && pages.Count > 1 ? 16 : 0;
            var heights = new List<float>(); float totalHeight = 0;
            float maxHeight = Mathf.Max(104, UIStyle.VirtualHeight - 48), contentWidth = bodyWidth;
            if (stacked)
            {
                foreach (string label in labels) { float height = Mathf.Max(18, wrappedChoice.CalcHeight(new GUIContent(label), contentWidth)); heights.Add(height); totalHeight += height + 6; }
                totalHeight -= 6;
                if (104 + totalHeight - 18 + pageReserve > maxHeight)
                {
                    contentWidth -= 18; heights.Clear(); totalHeight = 0;
                    foreach (string label in labels) { float height = Mathf.Max(18, wrappedChoice.CalcHeight(new GUIContent(label), contentWidth)); heights.Add(height); totalHeight += height + 6; }
                    totalHeight -= 6;
                }
                h = Mathf.Min(maxHeight, 104 + totalHeight - 18 + pageReserve);
            }
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), UIStyle.VirtualHeight - h - 26, w, h);
            shownRect = r; shownFrame = Time.frameCount;
            choiceRects.Clear(); choiceViewport = default;
            // 11/05 UI B안: 종이 창 + 왼쪽 위에 걸친 주황 이름표
            CuteUI.Window(r);
            if (!string.IsNullOrEmpty(speaker))
            {
                float nw = CuteUI.Width(speaker, false, true) + 22;
                var tag = new Rect(r.x + 12, r.y - 9, nw, 18);
                CuteUI.Box(tag, 7, CuteUI.Frame, CuteUI.Ink, 2, 2);
                CuteUI.LabelCenter(tag.center.x, tag.center.y + 1, speaker, CuteUI.Ink, false, true);
                CuteUI.LabelCenter(tag.center.x, tag.center.y, speaker, CuteUI.White, false, true);
            }
            // 11/11 은샘: 글 높이에 맞춰 위아래 가운데 (한 줄이면 위아래 여백, 두 줄이면 고르게)
            float textH = Mathf.Min(BodyHeight, bodyStyle.CalcHeight(new GUIContent(CurrentPage), bodyWidth));
            GUI.Label(new Rect(r.x + 10, r.y + 15 + Mathf.Round((BodyHeight - textH) * 0.5f), bodyWidth, textH + 2f), CurrentPage, bodyStyle);   // 이름이 창 위 이름표로 올라가서 본문도 위로

            if (pages.Count > 1)
                GUI.Label(new Rect(r.x + 10, stacked ? r.y + 72 : r.yMax - 22, 60, 16), $"<color=#6a5464>{pageIndex + 1} / {pages.Count}</color>", CuteUI.PaperSmall);

            float bx = r.xMax - 10;
            if (!ChoicesVisible)
            {
                if (GUI.Button(new Rect(bx - 104, r.yMax - 26, 104, 18), "다음 (Space)", CuteUI.PaperButtonOn)) NextPage();
                return;
            }
            if (choices.Count == 0)
            {
                // 11/08 마우스로도 닫게 단추로 (예전엔 글씨라 Space로만 닫혔다)
                if (GUI.Button(new Rect(bx - 104, r.yMax - 26, 104, 18), "닫기 (Space)", CuteUI.PaperButtonOn)) Close();
                return;
            }
            if (stacked)
            {
                float shownHeight = h - 104 + 18 - pageReserve;
                choiceViewport = new Rect(r.x + 10, r.yMax - 8 - shownHeight, bodyWidth, shownHeight);
                choiceScroll.y = Mathf.Round(choiceScroll.y);
                choiceScroll = GUI.BeginScrollView(choiceViewport, choiceScroll, new Rect(0, 0, contentWidth, totalHeight));
                float y = 0;
                for (int i = 0; i < choices.Count; i++)
                {
                    var row = new Rect(0, y, contentWidth, heights[i]); choiceRects.Add(row);
                    if (GUI.Button(row, labels[i], i == focus ? wrappedChoiceOn : wrappedChoice))   // 11/08 키보드로 고른 것을 강조
                    {
                        GUI.EndScrollView(); Pick(choices[i]); return;
                    }
                    y += heights[i] + 6;
                }
                GUI.EndScrollView();
            }
            else
            {
                choiceViewport = new Rect(r.x + 10, r.yMax - 26, bodyWidth, 18);
                for (int i = choices.Count - 1; i >= 0; i--)
                {
                    bx -= widths[i]; var row = new Rect(bx, r.yMax - 26, widths[i], 18); choiceRects.Add(row);
                    if (GUI.Button(row, labels[i], i == focus ? CuteUI.PaperButtonOn : CuteUI.PaperButton)) { Pick(choices[i]); return; }   // 11/08 키보드로 고른 것을 강조
                    bx -= 6;
                }
            }
        }
    }
}
