using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 한글이 쳐지는 한 줄 입력 칸 (2026-10-30, feature/online). 채팅 칸과 접속 창의 이름 칸이 쓴다.
    /// 이 프로젝트는 입력 방식이 Input System만 켜져 있어서 Unity 입력 칸이 운영체제 한글 입력기를 쓰지 못한다
    /// (영어만 쳐짐. 입력기를 직접 켜 봐도 에디터 게임 화면에서는 안 됐다). 그래서 게임이 직접 두벌식으로 한글을 조합한다.
    /// - 영어 키(r k s …)가 들어오면 '가' 상태일 때 자모(ㄱ ㅏ ㄴ …)로 바꿔 글자를 만든다. 운영체제 한/영은 영어로 두면 된다.
    /// - 운영체제가 이미 자모나 완성된 한글을 보내 주면(맥 등) 그대로 받는다.
    /// - 가/A 바꾸기: 오른쪽 Alt(한/영 키) · Shift+Space · 칸 오른쪽 '가/A'를 누르기. 고른 것은 이 컴퓨터에 기억한다.
    /// - Backspace는 조합 중이면 자모 하나씩, 아니면 커서 앞 글자 하나씩 지운다. Delete는 커서 뒤 글자.
    /// - 11/17 ← → 로 커서를 옮겨 가운데를 고칠 수 있다 (Home·End 처음·끝). 옮기면 조합 중이던 글자는 확정된다.
    /// 글자는 IMGUI 키 이벤트로 받는다(그리는 동안만 받으므로 Draw를 매 OnGUI에 불러야 한다).
    /// 접속키 칸처럼 Masked면 글자를 *로 가리고, AllowKorean이 false면 한글 조합과 가/A 표시를 끈다. 글자색은 TextColor.
    /// </summary>
    public class NetTextBox
    {
        public readonly int Max;
        public bool Active { get; private set; }
        /// <summary>글자를 *로 가린다 (접속키)</summary>
        public bool Masked;
        /// <summary>false면 영어 키를 한글로 바꾸지 않고 가/A 표시도 없다</summary>
        public bool AllowKorean = true;
        /// <summary>글자·커서 색 (밝은 바탕 칸이면 어둡게)</summary>
        public Color TextColor = new Color(0.97f, 0.95f, 0.9f);
        /// <summary>확정된 글. 바꾸면 조합 중이던 글자는 버린다</summary>
        public string Text { get => text; set { text = value ?? ""; hangul = default; caret = text.Length; } }
        /// <summary>11/17 비어 있을 때 흐리게 보이는 안내 (예: "할 말 · /로 리액션")</summary>
        public string Placeholder = "";
        /// <summary>아직 조합 중인 글자 (예: "ㅎ", "한")</summary>
        public string Composing => hangul.Render();
        /// <summary>확정된 글 + 조합 중인 글자</summary>
        public string Full { get { var s = text.Insert(caret, hangul.Render()); return s.Length > Max ? s.Substring(0, Max) : s; } }

        /// <summary>true면 영어 키를 한글로 조합한다 (모든 칸이 같이 쓴다)</summary>
        public static bool Korean
        {
            get { if (korean < 0) korean = PlayerPrefs.GetInt("net.hangul", 1); return korean == 1; }
            set { korean = value ? 1 : 0; PlayerPrefs.SetInt("net.hangul", korean); }
        }
        static int korean = -1;

        string text = "";
        int caret;   // 11/17 확정된 글 안에서 커서 자리 (조합 중인 글자는 이 자리에 끼어 있다)
        Hangul hangul;
        int altCheckedFrame = -1;
        static GUIStyle plainStyle, badgeStyle;

        public NetTextBox(int max) { Max = max; }

        public void Begin()
        {
            if (Active) return;
            Active = true;
            GUIUtility.keyboardControl = 0;   // IMGUI 입력 칸이 같은 글자를 또 받지 않게
        }

        /// <summary>칸을 끈다. 조합 중이던 글자는 확정해서 남긴다</summary>
        public void End()
        {
            if (!Active) return;
            Active = false;
            Text = Full;
        }

        void Toggle()
        {
            if (!AllowKorean) return;
            Commit();
            Korean = !Korean;
        }

        /// <summary>조합 중인 글자를 커서 자리에 확정한다</summary>
        void Commit()
        {
            string d = hangul.Flush();
            if (d.Length == 0) return;
            if (text.Length + d.Length > Max) return;
            text = text.Insert(caret, d);
            caret += d.Length;
        }

        void Insert(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            text = text.Insert(caret, s);
            caret += s.Length;
        }

        void MoveCaret(int to)
        {
            Commit();
            caret = Mathf.Clamp(to, 0, text.Length);
        }

        void Type(char c)
        {
            char j = Hangul.IsJamo(c) ? c : AllowKorean && Korean ? Hangul.FromKey(c) : '\0';
            if (j != '\0')
            {
                var saved = hangul;
                string done = hangul.Put(j);
                if (text.Length + done.Length + hangul.Render().Length > Max) { hangul = saved; return; }
                Insert(done);
                return;
            }
            Commit();
            if (text.Length < Max) Insert(c.ToString());
        }

        void Backspace()
        {
            if (hangul.Back()) return;
            if (caret <= 0) return;
            int n = caret >= 2 && char.IsLowSurrogate(text[caret - 1]) ? 2 : 1;
            text = text.Remove(caret - n, n);
            caret -= n;
        }

        void Delete()
        {
            Commit();
            if (caret >= text.Length) return;
            int n = caret + 1 < text.Length && char.IsHighSurrogate(text[caret]) ? 2 : 1;
            text = text.Remove(caret, n);
        }

        /// <summary>
        /// 칸을 그리고, 켜져 있으면 키를 받는다. clickToToggle이면 칸을 눌러 켜고 칸 밖을 눌러 끈다.
        /// 글이 칸보다 길면 끝 쪽이 보이게 민다.
        /// </summary>
        public void Draw(Rect r, GUIStyle style, bool clickToToggle = false)
        {
            var e = Event.current;
            var badge = new Rect(r.xMax - 18f, r.y + 1f, 17f, r.height - 2f);
            GameCursor.HoverText(r);   // 11/18 글 쓰는 칸 위에서는 글 막대 커서 (가/A 표시 위는 손가락)
            if (AllowKorean) GameCursor.Hover(badge);
            if (AllowKorean && e.type == EventType.MouseDown && badge.Contains(e.mousePosition)) { Toggle(); Begin(); e.Use(); }
            else if (clickToToggle && e.type == EventType.MouseDown)
            {
                if (r.Contains(e.mousePosition)) { Begin(); e.Use(); }
                else End();
            }
            if (Active) Keys(e);

            GUI.Box(r, GUIContent.none, style);
            if (plainStyle == null || plainStyle.font != style.font || plainStyle.fontSize != style.fontSize)
            {
                plainStyle = new GUIStyle(style) { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0), border = new RectOffset(0, 0, 0, 0), richText = false, clipping = TextClipping.Overflow };
                foreach (var st in new[] { plainStyle.normal, plainStyle.hover, plainStyle.focused, plainStyle.active }) st.background = null;
                badgeStyle = new GUIStyle(plainStyle) { alignment = TextAnchor.MiddleCenter };
            }
            var plain = plainStyle;
            var inner = new Rect(r.x + 3f, r.y, r.width - (AllowKorean ? 24f : 6f), r.height);
            string comp = Masked ? "" : hangul.Render();
            string shown = Masked ? new string('*', text.Length) : text;
            int cut = Mathf.Clamp(caret, 0, shown.Length);
            string left = shown.Substring(0, cut), right = shown.Substring(cut);
            float leftW = left.Length > 0 ? plain.CalcSize(new GUIContent(left)).x : 0f;
            float compW = comp.Length > 0 ? plain.CalcSize(new GUIContent(comp)).x : 0f;
            float rightW = right.Length > 0 ? plain.CalcSize(new GUIContent(right)).x : 0f;
            float caretX = leftW + compW;
            float shift = Mathf.Min(0f, inner.width - (caretX + 4f));   // 커서가 늘 보이게 민다
            bool lightBg = TextColor.grayscale < .5f;

            var old = plain.normal.textColor;
            GUI.BeginGroup(inner);
            if (shown.Length == 0 && comp.Length == 0 && !string.IsNullOrEmpty(Placeholder))
            {
                plain.normal.textColor = plain.hover.textColor = new Color(TextColor.r, TextColor.g, TextColor.b, .38f);
                GUI.Label(new Rect(4f, 0f, inner.width, inner.height), Placeholder, plain);
            }
            plain.normal.textColor = plain.hover.textColor = TextColor;
            if (left.Length > 0) GUI.Label(new Rect(shift, 0f, leftW + 2f, inner.height), left, plain);
            if (right.Length > 0) GUI.Label(new Rect(shift + caretX, 0f, rightW + 2f, inner.height), right, plain);
            if (compW > 0f)
            {
                // 11/17 조합 중인 글자: 흰 칸에서는 노란 글자가 안 보였다 → 글자색 그대로 + 연한 바탕 + 진한 밑줄
                var accent = lightBg ? new Color(.16f, .55f, .52f) : new Color(1f, 0.87f, 0.35f);
                var ink = GUI.color;
                GUI.color = new Color(accent.r, accent.g, accent.b, .18f);
                GUI.DrawTexture(new Rect(shift + leftW, 2f, compW + 1f, inner.height - 4f), Texture2D.whiteTexture);
                GUI.color = accent;
                GUI.DrawTexture(new Rect(shift + leftW, inner.height - 3f, compW + 1f, 1.5f), Texture2D.whiteTexture);
                GUI.color = ink;
                plain.normal.textColor = plain.hover.textColor = lightBg ? TextColor : accent;
                GUI.Label(new Rect(shift + leftW, 0f, compW + 2f, inner.height), comp, plain);
            }
            if (Active && (Time.unscaledTime * 2f) % 2f < 1.2f)
            {
                var ink = GUI.color;
                GUI.color = TextColor;
                GUI.DrawTexture(new Rect(shift + caretX + 1f, 3f, 1f, inner.height - 6f), Texture2D.whiteTexture);
                GUI.color = ink;
            }
            plain.normal.textColor = plain.hover.textColor = old;
            GUI.EndGroup();
            if (!AllowKorean) return;

            // 가/A 표시 (누르면 바뀜)
            var c = GUI.color;
            bool lightField = TextColor.grayscale < .5f;   // 밝은 바탕 칸(글자가 어두움)
            GUI.color = Korean ? (lightField ? new Color(.23f, .44f, .33f) : new Color(1f, 0.87f, 0.35f))
                               : (lightField ? new Color(.5f, .42f, .33f) : new Color(0.75f, 0.8f, 0.9f));
            badgeStyle.normal.textColor = badgeStyle.hover.textColor = Color.white;
            GUI.Label(badge, Korean ? "가" : "A", badgeStyle);
            GUI.color = c;
        }

        void Keys(Event e)
        {
#if ENABLE_INPUT_SYSTEM
            // 한/영 키는 키보드에 따라 오른쪽 Alt로 들어온다. 한 프레임에 한 번만 본다
            if (AllowKorean && altCheckedFrame != Time.frameCount)
            {
                altCheckedFrame = Time.frameCount;
                var k = Keyboard.current;
                if (k != null && k.rightAltKey.wasPressedThisFrame) Toggle();
            }
#endif
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.Backspace) { Backspace(); e.Use(); return; }
            if (e.keyCode == KeyCode.Delete) { Delete(); e.Use(); return; }
            // 11/17 커서 옮기기 (가운데 글자 고치기). 조합 중이던 글자는 먼저 확정한다
            bool cmd = e.command || e.control;
            if (e.keyCode == KeyCode.Home || (cmd && e.keyCode == KeyCode.LeftArrow)) { MoveCaret(0); e.Use(); return; }
            if (e.keyCode == KeyCode.End || (cmd && e.keyCode == KeyCode.RightArrow)) { MoveCaret(int.MaxValue); e.Use(); return; }
            if (e.keyCode == KeyCode.LeftArrow) { Commit(); MoveCaret(caret - 1); e.Use(); return; }
            if (e.keyCode == KeyCode.RightArrow) { bool had = hangul.Render().Length > 0; Commit(); if (!had) MoveCaret(caret + 1); e.Use(); return; }
            char ch = e.character;
            if (ch == ' ' && e.shift && AllowKorean) { Toggle(); e.Use(); return; }
            if (ch == '\0' || char.IsControl(ch)) return;   // Enter · Tab · Esc · 글자 없는 키
            if (e.alt || e.control || e.command) return;
            Type(ch);
            e.Use();
        }

        /// <summary>두벌식 한글 조합 (첫소리 · 가운뎃소리 · 끝소리). 자모는 호환용 자모(ㄱ U+3131 ~ ㅣ U+3163)로 다룬다</summary>
        struct Hangul
        {
            const string Cho = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
            const string Jong = "\0ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";
            const string Keys = "qwertyuiopasdfghjklzxcvbnm";
            const string KeyJamo = "ㅂㅈㄷㄱㅅㅛㅕㅑㅐㅔㅁㄴㅇㄹㅎㅗㅓㅏㅣㅋㅌㅊㅍㅠㅜㅡ";
            const string ShiftKeys = "QWERTOP";
            const string ShiftJamo = "ㅃㅉㄸㄲㅆㅒㅖ";
            const char FirstVowel = 'ㅏ';   // U+314F, 가운뎃소리 21개가 이 차례로 이어진다

            static readonly Dictionary<(char, char), char> VowelPair = new Dictionary<(char, char), char>
            {
                { ('ㅗ', 'ㅏ'), 'ㅘ' }, { ('ㅗ', 'ㅐ'), 'ㅙ' }, { ('ㅗ', 'ㅣ'), 'ㅚ' },
                { ('ㅜ', 'ㅓ'), 'ㅝ' }, { ('ㅜ', 'ㅔ'), 'ㅞ' }, { ('ㅜ', 'ㅣ'), 'ㅟ' }, { ('ㅡ', 'ㅣ'), 'ㅢ' },
            };
            static readonly Dictionary<(char, char), char> FinalPair = new Dictionary<(char, char), char>
            {
                { ('ㄱ', 'ㅅ'), 'ㄳ' }, { ('ㄴ', 'ㅈ'), 'ㄵ' }, { ('ㄴ', 'ㅎ'), 'ㄶ' }, { ('ㄹ', 'ㄱ'), 'ㄺ' },
                { ('ㄹ', 'ㅁ'), 'ㄻ' }, { ('ㄹ', 'ㅂ'), 'ㄼ' }, { ('ㄹ', 'ㅅ'), 'ㄽ' }, { ('ㄹ', 'ㅌ'), 'ㄾ' },
                { ('ㄹ', 'ㅍ'), 'ㄿ' }, { ('ㄹ', 'ㅎ'), 'ㅀ' }, { ('ㅂ', 'ㅅ'), 'ㅄ' },
            };
            static Dictionary<char, (char a, char b)> finalSplit;
            static Dictionary<char, char> vowelSplit;

            char cho, jung, jong;   // '\0'이면 없음

            public static bool IsJamo(char c) => c >= 'ㄱ' && c <= 'ㅣ';
            static bool IsVowel(char c) => c >= FirstVowel && c <= 'ㅣ';

            /// <summary>영어 키 → 자모 (두벌식). 해당 없으면 '\0'</summary>
            public static char FromKey(char c)
            {
                int i = ShiftKeys.IndexOf(c);
                if (i >= 0) return ShiftJamo[i];
                i = Keys.IndexOf(char.ToLowerInvariant(c));
                return i >= 0 ? KeyJamo[i] : '\0';
            }

            /// <summary>자모 하나를 넣는다. 다 만들어져 밀려난 글자를 돌려준다</summary>
            public string Put(char j)
            {
                string done;
                if (!IsVowel(j))
                {
                    if (cho != '\0' && jung != '\0')
                    {
                        if (jong == '\0') { if (Jong.IndexOf(j) > 0) { jong = j; return ""; } }
                        else if (FinalPair.TryGetValue((jong, j), out var both)) { jong = both; return ""; }
                    }
                    done = Flush();
                    cho = j;
                    return done;
                }
                if (jong != '\0')   // 끝소리가 다음 글자 첫소리로 넘어간다 (닭 + ㅏ → 달가)
                {
                    char moved;
                    if (Split().TryGetValue(jong, out var parts)) { jong = parts.a; moved = parts.b; }
                    else { moved = jong; jong = '\0'; }
                    done = Flush();
                    cho = moved;
                    jung = j;
                    return done;
                }
                if (jung != '\0')
                {
                    if (VowelPair.TryGetValue((jung, j), out var v)) { jung = v; return ""; }
                    done = Flush();
                    jung = j;
                    return done;
                }
                if (cho != '\0' && Cho.IndexOf(cho) < 0)   // ㄳ처럼 첫소리가 될 수 없는 자모
                {
                    done = Flush();
                    jung = j;
                    return done;
                }
                jung = j;
                return "";
            }

            /// <summary>조합 중인 것을 확정해 돌려주고 비운다</summary>
            public string Flush()
            {
                string s = Render();
                cho = jung = jong = '\0';
                return s;
            }

            /// <summary>마지막에 넣은 자모 하나를 지운다. 지울 게 없으면 false</summary>
            public bool Back()
            {
                if (jong != '\0') { jong = Split().TryGetValue(jong, out var p) ? p.a : '\0'; return true; }
                if (jung != '\0') { jung = VowelSplit().TryGetValue(jung, out var first) ? first : '\0'; return true; }
                if (cho != '\0') { cho = '\0'; return true; }
                return false;
            }

            public string Render()
            {
                int c = Cho.IndexOf(cho);
                if (cho != '\0' && jung != '\0' && c >= 0)
                {
                    int f = jong == '\0' ? 0 : Jong.IndexOf(jong);
                    return ((char)(0xAC00 + (c * 21 + (jung - FirstVowel)) * 28 + f)).ToString();
                }
                if (cho != '\0' && jung != '\0') return cho.ToString() + jung;
                if (cho != '\0') return cho.ToString();
                if (jung != '\0') return jung.ToString();
                return "";
            }

            static Dictionary<char, (char a, char b)> Split()
            {
                if (finalSplit != null) return finalSplit;
                finalSplit = new Dictionary<char, (char, char)>();
                foreach (var kv in FinalPair) finalSplit[kv.Value] = kv.Key;
                return finalSplit;
            }

            static Dictionary<char, char> VowelSplit()
            {
                if (vowelSplit != null) return vowelSplit;
                vowelSplit = new Dictionary<char, char>();
                foreach (var kv in VowelPair) vowelSplit[kv.Value] = kv.Key.Item1;
                return vowelSplit;
            }
        }
    }
}
