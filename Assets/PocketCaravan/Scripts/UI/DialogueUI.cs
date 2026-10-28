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
        int openedFrame;
        readonly List<string> pages = new List<string>();
        int pageIndex;
        float pageWidth;
        GUIStyle bodyStyle;
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
            d.speaker = speaker;
            d.text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            d.pages.Clear();
            d.pageIndex = 0;
            d.pageWidth = 0;
            d.choices.Clear();
            if (choices != null) d.choices.AddRange(choices);
            d.open = true;
            d.openedFrame = Time.frameCount;
            UIState.Blocking = true;
        }

        void Close()
        {
            open = false;
            UIState.Release();
        }

        void Pick(Choice c)
        {
            Close();
            c.action?.Invoke(); // 다음 대화를 열 수 있으니 닫은 뒤에 부른다
        }

        void Update()
        {
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
            if (bodyStyle == null)
                bodyStyle = new GUIStyle(UIStyle.Wrap) { richText = false, clipping = TextClipping.Clip };
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
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, UIStyle.VirtualHeight - h - 26, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 10, r.y + 6, w - 20, 16), $"<color=#FFE08A>{speaker}</color>", UIStyle.Rich);
            GUI.Label(new Rect(r.x + 10, r.y + 24, bodyWidth, BodyHeight), CurrentPage, bodyStyle);

            if (pages.Count > 1)
                GUI.Label(new Rect(r.x + 10, r.yMax - 20, 60, 16), $"{pageIndex + 1} / {pages.Count}", UIStyle.Small);

            float bx = r.xMax - 10;
            if (!ChoicesVisible)
            {
                if (GUI.Button(new Rect(bx - 104, r.yMax - 24, 104, 18), "다음 (Space)", UIStyle.Button)) NextPage();
                return;
            }
            if (choices.Count == 0)
            {
                GUI.Label(new Rect(bx - 120, r.yMax - 20, 120, 16), "계속 (Space)", UIStyle.Right);
                return;
            }
            for (int i = choices.Count - 1; i >= 0; i--)
            {
                var c = choices[i];
                string label = i == 0 ? c.label + " (Space)" : (i == choices.Count - 1 ? c.label + " (Esc)" : c.label);
                float bw = Mathf.Max(60f, UIStyle.Button.CalcSize(new GUIContent(label)).x + 14f);
                bx -= bw;
                if (GUI.Button(new Rect(bx, r.yMax - 24, bw, 18), label, UIStyle.Button)) { Pick(c); return; }
                bx -= 6;
            }
        }
    }
}
