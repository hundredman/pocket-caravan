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
    /// - Backspace는 조합 중이면 자모 하나씩, 아니면 글자 하나씩 지운다. 커서 옮기기는 없다(끝에만 쓴다).
    /// 글자는 IMGUI 키 이벤트로 받는다(그리는 동안만 받으므로 Draw를 매 OnGUI에 불러야 한다).
    /// </summary>
    public class NetTextBox
    {
        public readonly int Max;
        public bool Active { get; private set; }
        /// <summary>확정된 글. 바꾸면 조합 중이던 글자는 버린다</summary>
        public string Text { get => text; set { text = value ?? ""; hangul = default; } }
        /// <summary>아직 조합 중인 글자 (예: "ㅎ", "한")</summary>
        public string Composing => hangul.Render();
        /// <summary>확정된 글 + 조합 중인 글자</summary>
        public string Full { get { var s = text + hangul.Render(); return s.Length > Max ? s.Substring(0, Max) : s; } }

        /// <summary>true면 영어 키를 한글로 조합한다 (모든 칸이 같이 쓴다)</summary>
        public static bool Korean
        {
            get { if (korean < 0) korean = PlayerPrefs.GetInt("net.hangul", 1); return korean == 1; }
            set { korean = value ? 1 : 0; PlayerPrefs.SetInt("net.hangul", korean); }
        }
        static int korean = -1;

        string text = "";
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
            text = Full;
            hangul = default;
            Korean = !Korean;
        }

        void Type(char c)
        {
            char j = Hangul.IsJamo(c) ? c : Korean ? Hangul.FromKey(c) : '\0';
            if (j != '\0')
            {
                var saved = hangul;
                string done = hangul.Put(j);
                if ((text + done + hangul.Render()).Length > Max) { hangul = saved; return; }
                text += done;
                return;
            }
            text += hangul.Flush();
            if (text.Length < Max) text += c;
        }

        void Backspace()
        {
            if (hangul.Back()) return;
            if (text.Length > 0) text = text.Substring(0, text.Length - (text.Length >= 2 && char.IsLowSurrogate(text[text.Length - 1]) ? 2 : 1));
        }

        /// <summary>
        /// 칸을 그리고, 켜져 있으면 키를 받는다. clickToToggle이면 칸을 눌러 켜고 칸 밖을 눌러 끈다.
        /// 글이 칸보다 길면 끝 쪽이 보이게 민다.
        /// </summary>
        public void Draw(Rect r, GUIStyle style, bool clickToToggle = false)
        {
            var e = Event.current;
            var badge = new Rect(r.xMax - 18f, r.y + 1f, 17f, r.height - 2f);
            if (e.type == EventType.MouseDown && badge.Contains(e.mousePosition)) { Toggle(); Begin(); e.Use(); }
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
            var inner = new Rect(r.x + 3f, r.y, r.width - 24f, r.height);
            string comp = hangul.Render();
            float textW = plain.CalcSize(new GUIContent(text)).x;
            float compW = comp.Length > 0 ? plain.CalcSize(new GUIContent(comp)).x : 0f;
            float shift = Mathf.Min(0f, inner.width - (textW + compW + 4f));

            var old = plain.normal.textColor;
            GUI.BeginGroup(inner);
            plain.normal.textColor = new Color(0.97f, 0.95f, 0.9f);
            GUI.Label(new Rect(shift, 0f, textW + 2f, inner.height), text, plain);
            if (compW > 0f)
            {
                var yellow = new Color(1f, 0.87f, 0.35f);   // 조합 중인 글자는 노랗게, 밑줄
                plain.normal.textColor = yellow;
                GUI.Label(new Rect(shift + textW, 0f, compW + 2f, inner.height), comp, plain);
                var ink = GUI.color;
                GUI.color = yellow;
                GUI.DrawTexture(new Rect(shift + textW, inner.height - 3f, compW, 1f), Texture2D.whiteTexture);
                GUI.color = ink;
            }
            if (Active && (Time.unscaledTime * 2f) % 2f < 1.2f)
                GUI.DrawTexture(new Rect(shift + textW + compW + 1f, 3f, 1f, inner.height - 6f), Texture2D.whiteTexture);
            plain.normal.textColor = old;
            GUI.EndGroup();

            // 가/A 표시 (누르면 바뀜)
            var c = GUI.color;
            GUI.color = Korean ? new Color(1f, 0.87f, 0.35f) : new Color(0.75f, 0.8f, 0.9f);
            GUI.Label(badge, Korean ? "가" : "A", badgeStyle);
            GUI.color = c;
        }

        void Keys(Event e)
        {
#if ENABLE_INPUT_SYSTEM
            // 한/영 키는 키보드에 따라 오른쪽 Alt로 들어온다. 한 프레임에 한 번만 본다
            if (altCheckedFrame != Time.frameCount)
            {
                altCheckedFrame = Time.frameCount;
                var k = Keyboard.current;
                if (k != null && k.rightAltKey.wasPressedThisFrame) Toggle();
            }
#endif
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.Backspace) { Backspace(); e.Use(); return; }
            char ch = e.character;
            if (ch == ' ' && e.shift) { Toggle(); e.Use(); return; }
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
