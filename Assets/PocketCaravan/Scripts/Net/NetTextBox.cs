using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 한글이 쳐지는 한 줄 입력 칸 (2026-10-30, feature/online). 채팅 칸과 접속 창의 이름 칸이 쓴다.
    /// 이 프로젝트는 입력 방식이 Input System만 켜져 있어서, IMGUI TextField가 한글 입력기(IME)를 켜지 못한다
    /// (영어는 되는데 한글이 안 쳐짐). 그래서 칸이 켜져 있는 동안 직접 입력기를 켜고(SetIMEEnabled),
    /// 확정된 글자(onTextInput)와 아직 조합 중인 글자(onIMECompositionChange)를 받아 그린다.
    /// 지우기는 Backspace 한 글자씩. 커서 옮기기는 없다(끝에만 쓴다).
    /// </summary>
    public class NetTextBox
    {
        public string Text = "";
        public readonly int Max;
        public bool Active { get; private set; }
        /// <summary>아직 조합 중인 글자 (예: "ㅎ", "하")</summary>
        public string Composing { get; private set; } = "";
        /// <summary>확정된 글 + 조합 중인 글자</summary>
        public string Full => Fit(Text + Composing);

        static GUIStyle composingStyle;
#if ENABLE_INPUT_SYSTEM
        Keyboard hooked;
#endif

        public NetTextBox(int max) { Max = max; }

        public void Begin()
        {
            if (Active) return;
            Active = true;
            Composing = "";
            GUIUtility.keyboardControl = 0;   // IMGUI 칸이 같은 글자를 또 받지 않게
#if ENABLE_INPUT_SYSTEM
            hooked = Keyboard.current;
            if (hooked == null) return;
            hooked.onTextInput += OnText;
            hooked.onIMECompositionChange += OnComposition;
            hooked.SetIMEEnabled(true);
#endif
        }

        /// <summary>칸을 끈다. 조합 중이던 글자는 확정해서 남긴다</summary>
        public void End()
        {
            if (!Active) return;
            Active = false;
            Text = Full;
            Composing = "";
#if ENABLE_INPUT_SYSTEM
            if (hooked != null)
            {
                hooked.onTextInput -= OnText;
                hooked.onIMECompositionChange -= OnComposition;
                hooked.SetIMEEnabled(false);
                hooked = null;
            }
#endif
        }

        void OnText(char c)
        {
            if (!Active) return;
            if (c == '\b' || c == (char)127)
            {
                if (Text.Length > 0) Text = Text.Substring(0, Text.Length - (Text.Length >= 2 && char.IsLowSurrogate(Text[Text.Length - 1]) ? 2 : 1));
                return;
            }
            if (char.IsControl(c)) return;   // Enter · Tab · Esc
            if (Text.Length < Max) Text += c;
        }

#if ENABLE_INPUT_SYSTEM
        void OnComposition(IMECompositionString s) => Composing = Active ? s.ToString() : "";
#endif

        string Fit(string s) => s.Length > Max ? s.Substring(0, Max) : s;

        /// <summary>
        /// 칸을 그린다. 칸을 누르면 켜지고, 칸 밖을 누르면 꺼진다(clickToToggle일 때).
        /// 글이 칸보다 길면 끝 쪽이 보이게 민다.
        /// </summary>
        public void Draw(Rect r, GUIStyle style, bool clickToToggle = false)
        {
            var e = Event.current;
            if (clickToToggle && e.type == EventType.MouseDown)
            {
                if (r.Contains(e.mousePosition)) { Begin(); e.Use(); }
                else End();
            }
            GUI.Box(r, GUIContent.none, style);
            if (composingStyle == null || composingStyle.font != style.font || composingStyle.fontSize != style.fontSize)
            {
                composingStyle = new GUIStyle(style) { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0), border = new RectOffset(0, 0, 0, 0), richText = false, clipping = TextClipping.Overflow };
                foreach (var st in new[] { composingStyle.normal, composingStyle.hover, composingStyle.focused, composingStyle.active }) st.background = null;
            }
            var plain = composingStyle;
            var inner = new Rect(r.x + 3f, r.y, r.width - 6f, r.height);
            float textW = plain.CalcSize(new GUIContent(Text)).x;
            float compW = Composing.Length > 0 ? plain.CalcSize(new GUIContent(Composing)).x : 0f;
            float total = textW + compW + 4f;
            float shift = Mathf.Min(0f, inner.width - total);

            GUI.BeginGroup(inner);
            var old = plain.normal.textColor;
            plain.normal.textColor = new Color(0.97f, 0.95f, 0.9f);
            GUI.Label(new Rect(shift, 0f, textW + 2f, inner.height), Text, plain);
            if (compW > 0f)
            {
                plain.normal.textColor = new Color(1f, 0.87f, 0.35f);   // 조합 중인 글자는 노랗게
                GUI.Label(new Rect(shift + textW, 0f, compW + 2f, inner.height), Composing, plain);
                var ink = GUI.color;
                GUI.color = new Color(1f, 0.87f, 0.35f, 0.8f);
                GUI.DrawTexture(new Rect(shift + textW, inner.height - 3f, compW, 1f), Texture2D.whiteTexture);
                GUI.color = ink;
            }
            if (Active && (Time.unscaledTime * 2f) % 2f < 1.2f)
                GUI.DrawTexture(new Rect(shift + textW + compW + 1f, 3f, 1f, inner.height - 6f), Texture2D.whiteTexture);
            plain.normal.textColor = old;
            GUI.EndGroup();

#if ENABLE_INPUT_SYSTEM
            // 입력기 후보 창이 칸 근처에 뜨게 (화면 픽셀, 왼쪽 위 기준)
            if (Active && hooked != null && e.type == EventType.Repaint)
            {
                Vector2 p = GUIUtility.GUIToScreenPoint(new Vector2(inner.x + Mathf.Min(inner.width, total), r.yMax));
                hooked.SetIMECursorPosition(p);
            }
#endif
        }
    }
}
