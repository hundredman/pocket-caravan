using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 온라인 채팅 (2026-10-30, feature/online 7단계).
    /// - 접속 중 Enter: 입력 칸이 열린다 → Enter 보내기 · Esc 닫기. 쓰는 동안은 다른 창처럼 이동이 멈춘다.
    /// - 말은 접속한 모두에게 간다. 같은 맵 사람(나 포함)이 한 말은 머리 위 말풍선으로도 뜬다. 다른 맵 사람 말도 목록에 똑같이 보이고, 이름 옆에 그 맵 이름이 붙는다.
    /// - 8단계: 공유 장터 거래·저장 같은 알림 줄(System)도 같은 목록에 회색으로 남긴다.
    /// - 리액션 명령 (2026-10-31): "/춤"처럼 /로 시작하면 말 대신 리액션을 한다. 모든 지역에서 된다(고려·아라비아는 R 리액션이 없어서).
    ///   내 캐릭터도 같이 움직이고(RemotePlayers.PlaySelf), 같은 맵 사람에게 보인다(NetEmote). "/리액션"이면 목록을 보여 준다.
    /// - 왼쪽 아래(접속 표시 위)에 최근 8줄. 12초 지나면 사라지고, 입력 칸을 열면 다시 보인다.
    /// - 서버가 꺾쇠(&lt;&gt;)를 바꿔 보내므로 남의 말에 글자 꾸미기 태그가 끼어들지 않는다.
    /// 한글 입력: 입력 칸은 NetTextBox(게임이 직접 두벌식으로 조합). Enter를 누르면 두 프레임 뒤에 조합 중인 글자까지 함께 보낸다.
    /// 공용 코드는 읽기만 한다(UIState · UIStyle · PlayerController · GameManager).
    /// </summary>
    public class NetChat : MonoBehaviour
    {
        const int MaxLines = 8, MaxChars = 80;
        const float LineSeconds = 12f, FadeSeconds = 2f, LogWidth = 230f;
        // 11/05 UI B안: 종이 바탕 위 글씨라 진한 색 (전엔 어두운 바탕용 밝은 색)
        const string MeColor = "#1f625e", OtherColor = "#a8541c", DimColor = "#6a5464", ErrorColor = "#d6334f", SystemColor = "#3f7a3a", NoticeColor = "#c0611a";

        class Line { public string text; public float at, seconds; public bool transient; }

        /// <summary>채팅 리액션 명령: 아이디 · 보여 줄 이름 · 받아 주는 말(한글·영어)</summary>
        static readonly (string id, string name, string[] words)[] Commands =
        {
            ("bow", "인사", new[] { "인사", "안녕", "bow", "wave", "hi" }),
            ("hop", "깡총", new[] { "깡총", "점프", "hop", "jump" }),
            ("clap", "박수", new[] { "박수", "짝짝", "clap" }),
            ("spin", "빙글", new[] { "빙글", "회전", "spin" }),
            ("dance", "춤", new[] { "춤", "댄스", "dance" }),
            ("love", "하트", new[] { "하트", "사랑", "love", "heart" }),
            ("nap", "낮잠", new[] { "낮잠", "잠", "nap", "sleep" }),
            ("surprise", "놀람", new[] { "놀람", "깜짝", "wow" }),
            ("joy", "기쁨", new[] { "기쁨", "야호", "joy" }),
            ("think", "생각", new[] { "생각", "음", "think" }),
        };
        class Bubble { public string text; public float until; }

        static NetChat inst;
        readonly List<Line> lines = new List<Line>();
        readonly Dictionary<string, Bubble> bubbles = new Dictionary<string, Bubble>();
        readonly List<string> expired = new List<string>();
        bool typing, toldHowTo;
        readonly NetTextBox box = new NetTextBox(MaxChars);
        int sendFrame = -1;
        float openedAt;
        GUIStyle field, lineStyle, bubbleStyle;
        static readonly Color32 LogFill = new Color32(0xFF, 0xFA, 0xF2, 228);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetChat>() != null) return;
            var go = new GameObject("NetChat");
            DontDestroyOnLoad(go);
            go.AddComponent<NetChat>();
        }

        void Awake() => inst = this;

        /// <summary>알림 한 줄 (공유 장터 거래, 저장 등). 채팅 목록에 연두색으로 남는다</summary>
        public static void System(string text)
        {
            if (inst != null && !string.IsNullOrEmpty(text)) inst.Add($"<color={SystemColor}>· {text}</color>");
        }

        void OnEnable() => NetClient.Received += OnReceived;
        void OnDisable()
        {
            NetClient.Received -= OnReceived;
            if (typing) Close();
        }

        void OnReceived(NetClient.Msg m)
        {
            var net = NetClient.I;
            if (net == null) return;
            switch (m.t)
            {
                case "welcome":
                    if (!toldHowTo) { toldHowTo = true; Add($"<color={DimColor}>Enter 채팅 · /리액션 도움말</color>", 6f, true); }
                    break;
                case "join":
                    if (!string.IsNullOrEmpty(m.name)) Add($"<color={DimColor}>{m.name} 님이 이 맵에 왔어요</color>");
                    break;
                case "say":
                    if (string.IsNullOrEmpty(m.text)) break;
                    bool mine = m.id == net.MyId;
                    bool here = mine || (m.map == net.CurrentMap && !string.IsNullOrEmpty(m.map));
                    string who = string.IsNullOrEmpty(m.name) ? m.id : m.name;
                    string where = here || string.IsNullOrEmpty(m.map) ? "" : $" <color={DimColor}>({MapName(m.map)})</color>";
                    Add($"<color={(mine ? MeColor : OtherColor)}>{who}</color>{where}: {m.text}");
                    if (here) bubbles[m.id ?? ""] = new Bubble { text = m.text, until = Time.unscaledTime + Mathf.Clamp(3f + m.text.Length * 0.08f, 3f, 8f) };
                    if (!mine) Sfx.Play("open", here ? .25f : .12f);
                    break;
                case "error":
                    Add($"<color={ErrorColor}>서버: {m.why}</color>");
                    break;
                case "notice":   // 11/05 관리자 알림 (npm run admin -- 알림 "…"): 채팅 목록과 가운데 아래 알림에 같이
                    if (string.IsNullOrEmpty(m.text)) break;
                    Add($"<color={NoticeColor}><b>[알림]</b> {m.text}</color>");
                    Notify.Show("알림: " + m.text, 6f);
                    Sfx.Play("open", .4f);
                    break;
            }
        }

        static string MapName(string mapId)
        {
            var index = WorldIndex.Get();
            return index != null ? index.MapName(mapId) : mapId;
        }

        void Add(string text, float seconds = LineSeconds, bool transient = false)
        {
            lines.Add(new Line { text = text, at = Time.unscaledTime, seconds = seconds, transient = transient });
            if (lines.Count > MaxLines) lines.RemoveAt(0);
        }

        float LineAlpha(Line line, float now)
        {
            if (typing && !line.transient) return 1f;
            return Mathf.Clamp01((line.seconds - (now - line.at)) / FadeSeconds);
        }

        static bool EnterPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        void Update()
        {
            var net = NetClient.I;
            bool online = net != null && net.Status == NetClient.State.Online;
            if (!online)
            {
                if (typing) Close();
                if (net == null || net.Status == NetClient.State.Offline) { bubbles.Clear(); }
                return;
            }

            if (typing)
            {
                if (sendFrame >= 0 && Time.frameCount >= sendFrame)
                {
                    string text = box.Full.Trim();
                    if (text.StartsWith("/")) Command(text.Substring(1).Trim());
                    else net.Say(text);
                    Close();
                }
                else if (InputReader.CancelPressed()) Close();
                else if (sendFrame < 0 && EnterPressed() && Time.unscaledTime - openedAt > 0.15f)
                    sendFrame = Time.frameCount + 2;   // 입력기가 마지막 글자를 확정할 시간을 준다
            }
            else if (EnterPressed() && !UIState.Blocking && !UIState.JustClosed && !(GameManager.I != null && GameManager.I.IsTravelling))
            {
                typing = true;
                box.Text = "";
                box.Begin();
                sendFrame = -1;
                openedAt = Time.unscaledTime;
                UIState.Blocking = true;
            }

            expired.Clear();
            foreach (var kv in bubbles)
                if (Time.unscaledTime > kv.Value.until || (kv.Key != net.MyId && !net.Peers.ContainsKey(kv.Key))) expired.Add(kv.Key);
            foreach (var id in expired) bubbles.Remove(id);
        }

        /// <summary>/로 시작하는 말: 리액션을 하거나 목록을 보여 준다</summary>
        void Command(string word)
        {
            string w = word.ToLowerInvariant();
            if (w == "" || w == "리액션" || w == "도움" || w == "help" || w == "?")
            {
                var names = new List<string>();
                foreach (var c in Commands) names.Add("/" + c.name);
                Add($"<color={DimColor}>리액션: {string.Join(" ", names)}</color>");
                return;
            }
            foreach (var c in Commands)
                if (global::System.Array.IndexOf(c.words, w) >= 0)
                {
                    NetEmote.Send(c.id, c.name);
                    RemotePlayers.PlaySelf(c.id, c.name);
                    return;
                }
            Add($"<color={ErrorColor}>모르는 명령이에요: /{word} · 목록은 /리액션</color>");
        }

        void Close()
        {
            typing = false;
            sendFrame = -1;
            box.End();
            box.Text = "";
            UIState.Release();
        }

        void OnGUI()
        {
            var net = NetClient.I;
            if (net == null || net.Status != NetClient.State.Online) return;
            bool anyLine = typing || lines.Exists(line => LineAlpha(line, Time.unscaledTime) > 0f);
            if (!anyLine && bubbles.Count == 0) return;
            UIStyle.Begin();
            Styles();
            GUI.depth = -6;

            DrawBubbles(net);
            if (UIState.Blocking && !typing) return;   // 다른 창이 열려 있으면 목록은 숨긴다

            float bottom = UIStyle.VirtualHeight - 10f - HUD.BottomReserved;   // 11/05 접속 표시는 오른쪽 위로 옮김: 아래 조작 안내·저장 문제 줄 바로 위
            if (typing) bottom = DrawInput(bottom);
            DrawLines(bottom);
        }

        void Styles()
        {
            if (lineStyle != null && lineStyle.font == UIStyle.SmallFont) return;
            lineStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.SmallFont, fontSize = UIStyle.SmallSize, richText = true, wordWrap = true, clipping = TextClipping.Overflow, padding = new RectOffset(0, 0, 1, 1) };
            lineStyle.normal.textColor = CuteUI.Text;
            bubbleStyle = new GUIStyle(lineStyle) { alignment = TextAnchor.MiddleCenter, richText = false };
            bubbleStyle.normal.textColor = CuteUI.Text;
            field = new GUIStyle(GUI.skin.label) { font = UIStyle.RegularFont, fontSize = UIStyle.BodySize, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(0, 0, 0, 0) };
            foreach (var st in new[] { field.normal, field.hover, field.focused, field.active }) st.background = null;
        }

        /// <summary>
        /// 입력 칸 (11/05 B안: 종이 카드 + 흰 칸 + 청록 테두리, 아래 줄에 도움말). 위쪽 끝 y를 돌려준다 (목록을 그 위에 쌓는다)
        /// </summary>
        float DrawInput(float bottom)
        {
            var panel = new Rect(6f, bottom - 38f, LogWidth + 40f, 36f);
            CuteUI.Box(panel, 8, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            CuteUI.Label(panel.x + 8f, panel.y + 11.5f, "말", CuteUI.Text, false, true);
            var fieldRect = new Rect(panel.x + 26f, panel.y + 4f, panel.width - 32f, 16f);
            CuteUI.Box(fieldRect, 4, CuteUI.White, CuteUI.Interact, 2);
            box.TextColor = (Color)CuteUI.Text;
            box.Draw(new Rect(fieldRect.x + 4f, fieldRect.y, fieldRect.width - 6f, fieldRect.height), field);
            CuteUI.Label(panel.x + 8f, panel.y + 28.5f, $"Enter 보내기 · Esc 닫기 · /리액션 · 가/A: 오른쪽 Alt · {box.Full.Length}/{MaxChars}", CuteUI.Sub, true);
            return panel.y - 4f;
        }

        /// <summary>채팅 목록 (11/05 B안: 반투명 종이 카드 위 진한 글씨, 오래되면 카드째 흐려진다)</summary>
        void DrawLines(float bottom)
        {
            var old = GUI.color;
            var shown = new List<(string text, float h, float alpha)>();
            float total = 0f, top = 0f;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                float alpha = LineAlpha(lines[i], Time.unscaledTime);
                if (alpha <= 0f) continue;
                float h = lineStyle.CalcHeight(new GUIContent(lines[i].text), LogWidth);
                shown.Add((lines[i].text, h, alpha));
                total += h;
                top = Mathf.Max(top, alpha);
            }
            if (shown.Count == 0) return;
            GUI.color = new Color(1f, 1f, 1f, top);
            CuteUI.Box(new Rect(6f, bottom - total - 8f, LogWidth + 12f, total + 8f), 8, LogFill, LogFill, 0, 2, CuteUI.SoftShadow);
            float y = bottom - 4f;
            foreach (var (text, h, alpha) in shown)
            {
                y -= h;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(12f, y, LogWidth, h), text, lineStyle);
            }
            GUI.color = old;
        }

        void DrawBubbles(NetClient net)
        {
            var cam = Camera.main;
            var pc = PlayerController.Instance;
            if (cam == null) return;
            float scale = UIStyle.Scale;
            foreach (var kv in bubbles)
            {
                Vector3 feet;
                float lift;
                if (kv.Key == net.MyId) { if (pc == null) continue; feet = pc.transform.position; lift = 1.2f; }
                else if (RemotePlayers.TryGetPosition(kv.Key, out feet)) lift = 1.45f;   // 이름표 위로
                else continue;
                var sp = cam.WorldToScreenPoint(feet + new Vector3(0f, lift, 0f));
                if (sp.z < 0f) continue;
                var content = new GUIContent(kv.Value.text);
                float w = Mathf.Min(140f, bubbleStyle.CalcSize(content).x + 4f);
                float h = bubbleStyle.CalcHeight(content, w);
                float cx = Mathf.Round(sp.x / scale), by = Mathf.Round((Screen.height - sp.y) / scale);
                // 11/05 B안 말풍선: 종이 + 테두리(내 말은 청록) + 아래 꼬리
                var r = new Rect(Mathf.Round(cx - w / 2f - 6f), by - h - 10f, Mathf.Round(w + 12f), Mathf.Round(h + 7f));
                var line = kv.Key == net.MyId ? CuteUI.Interact : CuteUI.Ink;
                CuteUI.Box(r, 6, CuteUI.Paper, line, 1, 2, CuteUI.SoftShadow);
                CuteUI.Fill(new Rect(cx - 3f, r.yMax - 1f, 7f, 1f), CuteUI.Paper);
                CuteUI.Fill(new Rect(cx - 3f, r.yMax - 1f, 1f, 2f), line);
                CuteUI.Fill(new Rect(cx + 3f, r.yMax - 1f, 1f, 2f), line);
                CuteUI.Fill(new Rect(cx - 2f, r.yMax, 5f, 1f), CuteUI.Paper);
                CuteUI.Fill(new Rect(cx - 2f, r.yMax + 1f, 1f, 1f), line);
                CuteUI.Fill(new Rect(cx + 2f, r.yMax + 1f, 1f, 1f), line);
                CuteUI.Fill(new Rect(cx - 1f, r.yMax + 1f, 3f, 1f), CuteUI.Paper);
                CuteUI.Fill(new Rect(cx - 1f, r.yMax + 2f, 3f, 1f), line);
                GUI.Label(new Rect(r.x + 6f, r.y + 3f, w, h), content, bubbleStyle);
            }
        }
    }
}
