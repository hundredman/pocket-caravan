using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 아래 대화창. DialogueUI.Show(이름, 대사, 선택지...)로 연다.
    /// 선택지가 없으면 E/Space로 닫힌다. 임시 UI(OnGUI)다.
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

        void Awake() => instance = this;

        public static void Show(string speaker, string text, params Choice[] choices)
        {
            if (instance == null) return;
            var d = instance;
            d.speaker = speaker;
            d.text = text;
            d.choices.Clear();
            d.choices.AddRange(choices);
            d.open = true;
            d.openedFrame = Time.frameCount;
            UIState.Blocking = true;
        }

        void Close()
        {
            open = false;
            UIState.Blocking = false;
        }

        void Pick(Choice c)
        {
            Close();
            c.action?.Invoke(); // 다음 대화를 열 수 있으니 닫은 뒤에 부른다
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            if (choices.Count == 0 && (InputReader.InteractPressed() || InputReader.ConfirmPressed() || InputReader.CancelPressed())) Close();
            else if (choices.Count > 0 && InputReader.CancelPressed()) Close();
            else if (choices.Count > 0 && (InputReader.InteractPressed() || InputReader.ConfirmPressed())) Pick(choices[0]);
        }

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            float w = Mathf.Min(460, UIStyle.VirtualWidth - 20), h = 92;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, UIStyle.VirtualHeight - h - 26, w, h);
            GUI.Box(r, "");
            GUI.Box(r, "");
            GUI.Label(new Rect(r.x + 10, r.y + 6, w - 20, 16), $"<b>{speaker}</b>", UIStyle.Rich);
            GUI.Label(new Rect(r.x + 10, r.y + 24, w - 20, 44), text, UIStyle.Wrap);

            float bx = r.xMax - 10;
            if (choices.Count == 0)
            {
                GUI.Label(new Rect(bx - 120, r.yMax - 20, 120, 16), "E 계속", UIStyle.Right);
                return;
            }
            for (int i = choices.Count - 1; i >= 0; i--)
            {
                var c = choices[i];
                float bw = 74;
                bx -= bw;
                string label = i == 0 ? c.label + " (E)" : c.label;
                if (GUI.Button(new Rect(bx, r.yMax - 24, bw, 18), label)) { Pick(c); return; }
                bx -= 6;
            }
        }
    }
}
