using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 아래 대화창. DialogueUI.Show(이름, 대사, 선택지...)로 연다.
    /// 실제 글꼴·창 크기로 페이지를 나눈다. Space로 다음 페이지, 마지막 페이지에서만 선택/닫기.
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
        sealed class Pending { public string speaker, text; public Choice[] choices; public int scene; }
        readonly Queue<Pending> pending = new Queue<Pending>();
        int openedFrame;
        readonly List<string> pages = new List<string>();
        int pageIndex;
        float pageWidth;
        GUIStyle bodyStyle;
        Vector2 choiceScroll;
        Rect lastWindow, choiceViewport;
        readonly List<Rect> choiceRects = new List<Rect>();
        GUIStyle wrappedChoice, wrappedChoiceOn;
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
                    scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle });
                return;
            }
            d.speaker = speaker;
            d.text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            d.pages.Clear();
            d.pageIndex = 0;
            d.pageWidth = 0;
            d.choices.Clear(); d.choiceScroll = Vector2.zero;
            if (choices != null) d.choices.AddRange(choices);
            d.open = true;
            d.openedFrame = Time.frameCount;
        }

        void Close()
        {
            open = false;
            UIState.Release(this);
        }

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
                if(next.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle) Show(next.speaker,next.text,next.choices);
                return;
            }
            if (!open || pages.Count == 0 || Time.frameCount == openedFrame) return;
            if (!ChoicesVisible)
            {
                if (InputReader.InteractPressed() || InputReader.ConfirmPressed() || InputReader.CancelPressed()) NextPage();
                return;
            }
            if (choices.Count == 0 && (InputReader.InteractPressed() || InputReader.ConfirmPressed() || InputReader.CancelPressed())) Close();
            else if (choices.Count > 0 && InputReader.CancelPressed()) Pick(choices[choices.Count - 1]);   // Esc = 마지막 선택지
            else if (choices.Count > 0 && (InputReader.InteractPressed() || InputReader.ConfirmPressed())) Pick(choices[0]);
        }

        void NextPage()
        {
            if (pageIndex >= pages.Count - 1) return;
            pageIndex++;
            openedFrame = Time.frameCount;
        }

        /// <summary>문자 누락 없이 분할. 결합 문자·이모지를 나누지 않고, 들어가는 범위에서 문장 끝을 우선한다.</summary>
        public static List<string> Paginate(string source, GUIStyle style, float width, float height)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(source)) { result.Add(""); return result; }
            int[] chars = StringInfo.ParseCombiningCharacters(source);
            int start = 0;
            while (start < chars.Length)
            {
                int lo = start + 1, hi = chars.Length, fit = start;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    int end = mid < chars.Length ? chars[mid] : source.Length;
                    string candidate = source.Substring(chars[start], end - chars[start]);
                    if (style.CalcHeight(new GUIContent(candidate), width) <= height - 1f)
                    { fit = mid; lo = mid + 1; }
                    else hi = mid - 1;
                }
                // 최소 한 글자도 못 담는 잘못된 레이아웃에서도 무한 루프는 막는다.
                fit = Mathf.Max(start + 1, fit);
                int split = fit;
                if (fit < chars.Length)
                    for (int i = fit; i > start + (fit - start) / 2; i--)
                    {
                        char last = source[chars[i] - 1];
                        if (last == '\n' || last == '.' || last == '?' || last == '!' || last == '。')
                        { split = i; break; }
                    }
                int lastChar = split < chars.Length ? chars[split] : source.Length;
                result.Add(source.Substring(chars[start], lastChar - chars[start]));
                start = split;
            }
            return result;
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
                int offset = 0;
                for (int i = 0; i < pageIndex; i++) offset += pages[i].Length;
                pages.Clear();
                pages.AddRange(Paginate(text, bodyStyle, bodyWidth, BodyHeight));
                pageIndex = 0;
                for (int consumed = 0; pageIndex < pages.Count - 1 && consumed + pages[pageIndex].Length <= offset; pageIndex++)
                    consumed += pages[pageIndex].Length;
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
                string label = i == 0 ? choices[i].label + " (Space)" : i == choices.Count - 1 ? choices[i].label + " (Esc)" : choices[i].label;
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
            GUI.Label(new Rect(r.x + 10, r.y + 15, bodyWidth, BodyHeight), CurrentPage, bodyStyle);   // 이름이 창 위 이름표로 올라가서 본문도 위로

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
                GUI.Label(new Rect(bx - 120, r.yMax - 22, 120, 16), "<color=#6a5464>계속 (Space)</color>", CuteUI.PaperRight);
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
                    if (GUI.Button(row, labels[i], i == 0 ? wrappedChoiceOn : wrappedChoice))
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
                    if (GUI.Button(row, labels[i], i == 0 ? CuteUI.PaperButtonOn : CuteUI.PaperButton)) { Pick(choices[i]); return; }
                    bx -= 6;
                }
            }
        }
    }
}
