using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 로그인 화면 · 온라인 창 (2026-10-31, feature/online).
    /// - 게임을 켜면 시작 화면(이어하기 · 새로 시작)보다 먼저 로그인 화면이 뜬다. 이름 + 접속키로 접속하거나 "혼자 하기".
    ///   처음 쓰는 이름이면 그 이름과 접속키로 계정이 만들어진다. 접속키가 틀리면 그 자리에서 알려 준다.
    ///   "이 컴퓨터에 기억"을 켜 두면 다음부터는 켜자마자 저절로 접속한다(실패하면 로그인 화면이 그대로 남는다).
    ///   접속되면 시작 화면으로 넘어간다. 로그인 화면이 떠 있는 동안 시작 화면은 키를 받지 않는다(StartScreen.Held).
    /// - 게임 중 O 키: 온라인 창. 접속해 있으면 내 이름·계정·접속자 수와 끊기 · 다른 이름으로 접속, 아니면 로그인 칸.
    ///   (2026-11-05 UI B안: 게임 중 창은 CuteUI 모양. 첫 화면 로그인은 시작 화면과 같은 크림·초록 그대로)
    /// - Tab 칸 바꾸기 · Enter 접속 · 한글은 NetTextBox(가/A: 오른쪽 Alt). 서버 주소는 "서버 바꾸기"를 눌러야 보인다(개발용).
    /// - 다른 씬에서 바로 Play하면 시작 화면이 없어서 로그인 화면도 안 뜬다. 편집기에서 볼 때는 Preview를 켠다(메뉴 온라인/화면 캡처).
    /// 이름·접속키·주소·기억 여부는 이 컴퓨터 설정(PlayerPrefs)에만 남는다. 게임 저장 파일과는 무관하다.
    /// </summary>
    public class NetLogin : MonoBehaviour
    {
        enum Panel { None, Start, Game }

        /// <summary>편집기 미리보기: 시작 화면이 없어도 첫 화면 로그인을 띄운다</summary>
        public static bool Preview;
        /// <summary>로그인 화면이나 온라인 창이 떠 있다</summary>
        public static bool IsOpen => inst != null && inst.panel != Panel.None;

        static NetLogin inst;
        Panel panel;
        bool startDone;      // 이번 실행에서 첫 화면 로그인을 끝냈다 (접속했거나 혼자 하기)
        bool autoTried;      // 기억한 이름·접속키로 저절로 접속해 봤다
        bool submitting;     // 접속을 눌러 결과를 기다리는 중
        bool remember, showServer;
        string address, note;
        bool noteBad;
        readonly NetTextBox nameBox = new NetTextBox(16);
        readonly NetTextBox keyBox = new NetTextBox(32) { Masked = true, AllowKorean = false };
        readonly NetTextBox addressBox = new NetTextBox(160) { AllowKorean = false };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetLogin>() != null) return;
            var go = new GameObject("NetLogin");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetLogin>();
        }

        void Awake()
        {
            inst = this;
            address = PlayerPrefs.GetString("net.address", NetClient.DefaultAddress);
            // 11/12 윤채(시연): 한 컴퓨터를 여러 사람이 돌아가며 쓰면 앞사람 이름이 채워져 있었다 →
            //   "기억하기"는 처음엔 꺼 두고, 이름도 기억하기를 켰을 때만 채운다 (켜 두면 다음 사람이 앞사람 계정으로 저절로 접속했다)
            remember = PlayerPrefs.GetInt("net.remember", 0) == 1;
            nameBox.Text = remember ? PlayerPrefs.GetString("net.name", "") : "";
            keyBox.Text = remember ? PlayerPrefs.GetString("net.pass", "") : "";
            addressBox.Text = address;
        }

        static NetClient Net => NetClient.I;
        static bool Online => Net != null && Net.Status == NetClient.State.Online;

        // ---------------- 열고 닫기 ----------------

        void Update()
        {
            bool startWanted = !startDone && (StartScreen.IsOpen || Preview);
            if (startWanted && panel != Panel.Start) OpenStart();
            if (panel == Panel.Start && !startWanted) { CloseStart(); return; }
            if (panel == Panel.Start) { UpdateStart(); return; }
            UpdateGame();
        }

        void OpenStart()
        {
            panel = Panel.Start;
            StartScreen.Held = true;
            UIState.Blocking = true;
            note = null;
            Focus(nameBox.Text.Trim().Length == 0 ? nameBox : keyBox.Text.Length == 0 ? keyBox : null);
            if (!autoTried && remember && nameBox.Text.Trim().Length > 0 && keyBox.Text.Length >= 4)
            {
                autoTried = true;
                Submit(auto: true);
            }
        }

        void CloseStart()
        {
            panel = Panel.None;
            startDone = true;
            Preview = false;
            StartScreen.Held = false;
            Focus(null);
            if (!StartScreen.IsOpen) UIState.Release();
        }

        void UpdateStart()
        {
            UIState.Blocking = true;
            StartScreen.Held = true;
            if (!submitting) return;
            var net = Net;
            if (Online) { submitting = false; Sfx.Play("accept", .6f); CloseStart(); return; }
            if (net == null || net.Status == NetClient.State.Offline || net.Status == NetClient.State.Waiting)
            {
                // 첫 화면에서는 몇 번이고 다시 붙지 않고, 이유를 보여 주고 고르게 한다
                string why = net != null && !string.IsNullOrEmpty(net.LastError) ? net.LastError : "서버에 연결하지 못했어요";
                if (net != null) net.Disconnect();
                submitting = false;
                Say(Friendly(why), true);
                Focus(keyBox);
            }
        }

        void UpdateGame()
        {
            if (panel == Panel.Game)
            {
                if (submitting && (Online || Net == null || Net.Status == NetClient.State.Offline))
                {
                    submitting = false;
                    if (!Online) Say(Friendly(Net != null ? Net.LastError : ""), true);
                    else { Say(null, false); Sfx.Play("accept", .6f); }
                }
                bool typing = nameBox.Active || keyBox.Active || addressBox.Active;
                if (InputReader.CancelPressed() || (OPressed() && !typing)) CloseGame();
                return;
            }
            bool want = OPressed() || openRequested;   // 11/08 오른쪽 위 접속 알약을 눌러도 연다
            openRequested = false;
            if (!want || UIState.Blocking || UIState.JustClosed || StartScreen.IsOpen) return;
            if (GameManager.I != null && GameManager.I.IsTravelling) return;
            panel = Panel.Game;
            UIState.Blocking = true;
            note = null;
            if (!Online) Focus(nameBox.Text.Trim().Length == 0 ? nameBox : null);
            Sfx.Play("open", .5f);
        }

        static bool openRequested;
        /// <summary>11/08 온라인 창 열기 (O와 같다): 오른쪽 위 접속 알약을 누르면 부른다</summary>
        public static void OpenGame() => openRequested = true;

        void CloseGame()
        {
            panel = Panel.None;
            Focus(null);
            UIState.Release();
        }

        static bool OPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.O);
#endif
        }

        // ---------------- 접속 ----------------

        void Submit(bool auto = false)
        {
            if (submitting || Net == null) return;
            if (Net.Status != NetClient.State.Offline) Net.Disconnect();
            Focus(null);
            string name = nameBox.Full.Trim(), key = keyBox.Full;
            if (name.Length == 0) { Say("이름을 넣어 주세요", true); Focus(nameBox); return; }
            if (key.Length > 0 && key.Length < 4) { Say("접속키는 4자 이상으로 정해 주세요", true); Focus(keyBox); return; }
            if (showServer) address = addressBox.Full.Trim();
            nameBox.Text = name;
            if (remember) PlayerPrefs.SetString("net.name", name); else PlayerPrefs.DeleteKey("net.name");   // 11/12 이름도 기억하기를 켰을 때만
            PlayerPrefs.SetString("net.address", address);
            PlayerPrefs.SetInt("net.remember", remember ? 1 : 0);
            if (remember) PlayerPrefs.SetString("net.pass", key); else PlayerPrefs.DeleteKey("net.pass");
            PlayerPrefs.Save();
            if (!Net.Connect(address, name, key)) { Say(Net.LastError, true); return; }
            submitting = true;
            Say(auto ? $"{name}(으)로 저절로 접속하는 중…" : "접속하는 중…", false);
        }

        void PlayAlone()
        {
            if (Net != null && Net.Status != NetClient.State.Offline) Net.Disconnect();
            submitting = false;
            Sfx.Play("accept", .5f);
            CloseStart();
        }

        /// <summary>끊고, 저장된 접속키를 지우고 로그인 칸을 보여 준다</summary>
        void SwitchAccount()
        {
            if (Net != null) Net.Disconnect();
            keyBox.Text = "";
            PlayerPrefs.DeleteKey("net.pass");
            PlayerPrefs.Save();
            Say("다른 이름으로 들어오려면 이름과 접속키를 넣어 주세요", false);
            Focus(nameBox);
        }

        void Say(string text, bool bad) { note = text; noteBad = bad; }

        /// <summary>서버·연결 오류를 사람 말로</summary>
        static string Friendly(string why)
        {
            if (string.IsNullOrEmpty(why)) return "서버에 연결하지 못했어요. 인터넷을 확인하고 다시 눌러 주세요";
            if (why.StartsWith("서버: ")) return why.Substring(4);
            if (why.StartsWith("접속 실패") || why.StartsWith("연결이 끊겼습니다")) return "서버에 연결하지 못했어요. 인터넷을 확인하고 다시 눌러 주세요";
            return why;
        }

        void Focus(NetTextBox box)
        {
            foreach (var b in new[] { nameBox, keyBox, addressBox }) if (b != box) b.End();
            if (box != null) box.Begin();
        }

        // ---------------- 그리기 ----------------

        // 시작 화면과 같은 색 (StartScreen)
        static readonly Color32 Bg = new Color32(0xEF, 0xE6, 0xCF, 0xFF), BgCheck = new Color32(0xE8, 0xDE, 0xC4, 0xFF);
        static readonly Color32 Green = new Color32(0x3A, 0x71, 0x55, 0xFF), GreenDark = new Color32(0x2C, 0x58, 0x42, 0xFF);
        static readonly Color32 Brown = new Color32(0x4A, 0x35, 0x24, 0xFF), BrownLight = new Color32(0x8A, 0x6A, 0x46, 0xFF);
        static readonly Color32 TitleColor = new Color32(0x6B, 0x4A, 0x2B, 0xFF), Shadow = new Color32(0xD6, 0xC8, 0xA6, 0xFF);
        static readonly Color32 Pick = new Color32(0xE0, 0x57, 0x3A, 0xFF), White = new Color32(0xFF, 0xFF, 0xFF, 0xFF), Cream = new Color32(0xFB, 0xF5, 0xE6, 0xFF);

        GUIStyle label, small, big, plainField, gameField;
        bool light;   // 지금 그리는 화면이 밝은(시작 화면) 쪽인가

        void OnGUI()
        {
            if (panel == Panel.None) return;
            UIStyle.Begin();
            Styles();
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Tab) { NextField(); e.Use(); }
                else if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && FormShown) { Submit(); e.Use(); }
            }
            if (panel == Panel.Start) DrawStart();
            else DrawGame();
        }

        bool FormShown => panel == Panel.Start || !Online;

        void NextField()
        {
            if (!FormShown) return;
            if (nameBox.Active) Focus(keyBox);
            else if (keyBox.Active && showServer) Focus(addressBox);
            else Focus(nameBox);
        }

        void Styles()
        {
            if (label != null && label.font == UIStyle.RegularFont) return;
            label = new GUIStyle(GUI.skin.label) { font = UIStyle.RegularFont, fontSize = 11, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Overflow, richText = true, wordWrap = false };
            small = new GUIStyle(label) { font = UIStyle.SmallFont, fontSize = 9 };
            big = new GUIStyle(label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            plainField = new GUIStyle(GUI.skin.label) { font = UIStyle.RegularFont, fontSize = 11, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(0, 0, 0, 0) };
            foreach (var st in new[] { plainField.normal, plainField.hover, plainField.focused, plainField.active }) st.background = null;
            gameField = new GUIStyle(GUI.skin.textField) { font = UIStyle.RegularFont, fontSize = UIStyle.BodySize, alignment = TextAnchor.MiddleLeft };
        }

        static void Fill(Rect r, Color32 c)
        {
            var old = GUI.color; GUI.color = UIStyle.Lin(c);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Frame(Rect r, Color32 c, float t)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c); Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c); Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        void Text(Rect r, string s, Color32 c, GUIStyle st, TextAnchor a = TextAnchor.MiddleLeft)
        {
            st.alignment = a;
            st.normal.textColor = UIStyle.Lin(c);
            GUI.Label(r, s, st);
        }

        bool Button(Rect r, string text, bool primary)
        {
            if (!light) return primary ? CuteUI.Button(r, text) : CuteUI.SoftButton(r, text);
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(new Rect(r.x + 2, r.y + 3, r.width, r.height), Shadow);
            Fill(r, primary ? (hover ? GreenDark : Green) : (hover ? Cream : White));
            Frame(r, primary ? GreenDark : BrownLight, 1);
            Text(r, text, primary ? White : Brown, label, TextAnchor.MiddleCenter);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>입력 칸 하나 (칸을 누르면 그 칸에 쓴다)</summary>
        void Field(Rect r, NetTextBox box)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { Focus(box); e.Use(); }
            if (light)
            {
                Fill(r, White);
                Frame(r, box.Active ? Pick : BrownLight, box.Active ? 2 : 1);
                box.TextColor = UIStyle.Lin(Brown);
                box.Draw(new Rect(r.x + 3, r.y, r.width - 4, r.height), plainField);
            }
            else
            {
                CuteUI.Box(r, 5, CuteUI.White, box.Active ? CuteUI.Interact : CuteUI.Line, box.Active ? 2 : 1);
                box.TextColor = UIStyle.Lin(CuteUI.Text);
                box.Draw(new Rect(r.x + 4, r.y, r.width - 6, r.height), plainField);
            }
        }

        /// <summary>네모 칸 + 글: 누르면 켜고 끈다</summary>
        bool Check(Rect r, bool on, string text)
        {
            if (!light) return CuteUI.Check(r, on, text);
            var box = new Rect(r.x, r.y + (r.height - 10) / 2, 10, 10);
            Fill(box, White); Frame(box, BrownLight, 1); if (on) Fill(new Rect(box.x + 2, box.y + 2, 6, 6), Green);
            Text(new Rect(box.xMax + 5, r.y, r.width - 15, r.height), text, Brown, small);
            return GUI.Button(r, GUIContent.none, GUIStyle.none) ? !on : on;
        }

        /// <summary>DrawForm이 차지하는 높이 (그리기 전에 카드 크기를 정한다)</summary>
        float FormHeight => 41 + 37 + 17 + (showServer ? 35 : 0) + 22 + 24 + (string.IsNullOrEmpty(note) ? 0 : 20);

        /// <summary>이름 · 접속키 · (서버 주소) · 기억 · 버튼 · 안내. 다 그린 아래쪽 y를 돌려준다</summary>
        float DrawForm(float x, float y, float w, bool start)
        {
            Color32 ink = light ? Brown : CuteUI.Text, soft = light ? BrownLight : CuteUI.Sub;
            Text(new Rect(x, y, w, 14), "이름", ink, label);
            Text(new Rect(x, y, w, 14), "다른 사람에게 보이는 이름 · 16자까지", soft, small, TextAnchor.MiddleRight);
            Field(new Rect(x, y + 15, w, 20), nameBox);
            y += 41;
            Text(new Rect(x, y, w, 14), "접속키", ink, label);
            Text(new Rect(x, y, w, 14), "비밀번호처럼 나만 아는 글자 · 4자 이상", soft, small, TextAnchor.MiddleRight);
            Field(new Rect(x, y + 15, w, 20), keyBox);
            y += 37;
            Text(new Rect(x, y, w, 12), "처음 쓰는 이름이면 새 계정이 만들어져요", soft, small);
            y += 17;
            if (showServer)
            {
                Text(new Rect(x, y, w, 14), "서버 주소 (개발용)", ink, small);
                Field(new Rect(x, y + 13, w, 18), addressBox);
                y += 35;
            }
            bool r = Check(new Rect(x, y, w, 14), remember, "이 컴퓨터에 기억하고 다음에는 바로 접속");
            if (r != remember) { remember = r; Sfx.Play("open", .4f); }
            y += 22;
            float bw = (w - 10) / 2;
            if (submitting)
            {
                Button(new Rect(x, y, bw, 24), "접속하는 중…", true);
            }
            else if (Button(new Rect(x, y, bw, 24), "접속하기", true)) Submit();
            if (start)
            {
                if (Button(new Rect(x + bw + 10, y, bw, 24), "혼자 하기", false)) PlayAlone();
            }
            else if (Button(new Rect(x + bw + 10, y, bw, 24), "닫기", false)) CloseGame();
            y += 24;
            if (!string.IsNullOrEmpty(note))
            {
                Text(new Rect(x - 10, y + 6, w + 20, 14), note, noteBad ? (light ? Pick : CuteUI.Alert) : (light ? Green : CuteUI.Up), small, TextAnchor.MiddleCenter);
                y += 20;
            }
            return y;
        }

        void DrawStart()
        {
            // 11/05 UI B안: 시작 화면과 같은 종이 바둑판 + 주황 머리띠, 로그인 칸은 게임 중 온라인 창과 같은 종이 창
            light = false;
            GUI.depth = -600;   // 시작 화면(-500) 위
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            CuteUI.Backdrop("온라인");
            CuteUI.LabelCenter(W / 2, 62, "함께 떠나는 상인 길", CuteUI.Text, false, true);

            float cw = 320, ch = 36 + FormHeight;
            var card = new Rect(Mathf.Round((W - cw) / 2), 90, cw, ch);
            CuteUI.Window(card);
            CuteUI.TitlePlate(card.center.x, card.y - 11, "접속");
            DrawForm(card.x + 18, card.y + 22, cw - 36, true);

            CuteUI.LabelCenter(W / 2, H - 20, "Tab 칸 바꾸기 · Enter 접속 · 한글/영어: 오른쪽 Alt", CuteUI.Sub, true);
            var server = new Rect(W - 96, H - 27, 82, 14);
            CuteUI.LabelRight(server.xMax, server.center.y, showServer ? "서버 주소 숨기기" : "서버 바꾸기", CuteUI.Interact, true);
            if (GUI.Button(server, GUIContent.none, GUIStyle.none)) { showServer = !showServer; addressBox.Text = address; }
        }

        void DrawGame()
        {
            light = false;
            GUI.depth = -11;
            CuteUI.Dim();
            float w = 320, h = Online ? 196 : 36 + FormHeight;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2f), Mathf.Round((UIStyle.VirtualHeight - h) / 2f + 6), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "온라인");
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) { CloseGame(); return; }   // 11/08 마우스로 닫기
            float x = r.x + 18, fw = w - 36, y = r.y + 22;
            if (!Online) { DrawForm(x, y, fw, false); return; }

            var net = Net;
            // 내 이름 카드: 초상 + 이름(아이디) + 계정/손님
            CuteUI.Box(new Rect(x, y, fw, 42), 8, CuteUI.Paper2, CuteUI.Line);
            CuteUI.Portrait(x + 4, y + 3, 36);
            float nx = CuteUI.Label(x + 46, y + 13, net.MyName, CuteUI.Text, false, true);
            CuteUI.Label(nx + 6, y + 13.5f, $"({net.MyId})", CuteUI.Sub, true);
            string tag = net.IsAccount ? "계정" : "손님";
            float tw = CuteUI.Width(tag, true, true) + 10;
            CuteUI.Box(new Rect(x + 46, y + 22, tw, 13), 5, net.IsAccount ? CuteUI.Up : CuteUI.Deco, CuteUI.Ink);
            CuteUI.LabelCenter(x + 46 + tw / 2f, y + 28.5f, tag, net.IsAccount ? CuteUI.White : CuteUI.Ink, true, true);
            CuteUI.Label(x + 46 + tw + 6, y + 28.5f, net.IsAccount ? "게임 진행이 서버에 저장돼요" : "접속키가 없어서 저장되지 않아요", CuteUI.Sub, true);
            y += 50;
            CuteUI.Icon(CuteUI.Pic.People, x, y + 2);
            CuteUI.Label(x + 15, y + 6, $"지금 접속한 사람 {net.OnlineCount}명 · 이 맵 {net.Peers.Count + 1}명", CuteUI.Text, true);
            y += 16;
            CuteUI.Label(x, y + 6, "Enter 채팅 · /선물 · /순위 · /리액션 도움말", CuteUI.Sub, true);
            y += 14;
            CuteUI.Label(x, y + 6, "공유 장터: 리알토 · 개경 · 비르카 · 제다", CuteUI.Sub, true);
            y += 20;
            // 11/07 순위표: 온라인 창을 닫고 연다
            float hw = (fw - 8) / 2;
            if (CuteUI.Button(new Rect(x, y, hw, 20), "순위표 보기", CuteUI.Pic.Star4)) { CloseGame(); NetScores.Open(); }
            if (CuteUI.Button(new Rect(x + hw + 8, y, hw, 20), "채팅하기", CuteUI.Pic.People)) { CloseGame(); NetChat.OpenInput(); }   // 11/08 Enter 말고 마우스로도
            y += 28;
            float bw = (fw - 16) / 3;
            if (CuteUI.SoftButton(new Rect(x, y, bw, 22), "끊기")) { net.Disconnect(); Say("접속을 끊었어요", false); }
            if (CuteUI.SoftButton(new Rect(x + bw + 8, y, bw, 22), "다른 이름으로")) SwitchAccount();
            if (CuteUI.SoftButton(new Rect(x + (bw + 8) * 2, y, bw, 22), "닫기 (O)")) CloseGame();
        }
    }
}
