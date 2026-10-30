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
    /// - 말은 접속한 모두에게 간다. 같은 맵 사람(나 포함)이 한 말은 머리 위 말풍선으로도 뜬다. 다른 맵 사람 말은 목록에만 (다른 맵) 표시.
    /// - 왼쪽 아래(접속 표시 위)에 최근 6줄. 12초 지나면 사라지고, 입력 칸을 열면 다시 보인다.
    /// - 서버가 꺾쇠(&lt;&gt;)를 바꿔 보내므로 남의 말에 글자 꾸미기 태그가 끼어들지 않는다.
    /// 한글 입력: 입력 칸은 NetTextBox(입력기를 직접 켬). Enter로 글자를 확정하는 프레임에 바로 보내면 마지막 글자가 빠질 수 있어
    /// 두 프레임 뒤에 보내고, 그때도 조합 중인 글자가 있으면 함께 보낸다.
    /// 공용 코드는 읽기만 한다(UIState · UIStyle · PlayerController · GameManager).
    /// </summary>
    public class NetChat : MonoBehaviour
    {
        const int MaxLines = 6, MaxChars = 80;
        const float LineSeconds = 12f, FadeSeconds = 2f, LogWidth = 230f;
        const string MeColor = "#ffde59", OtherColor = "#8fd3ff", DimColor = "#a49db5", ErrorColor = "#ff9a8a";

        class Line { public string text; public float at; }
        class Bubble { public string text; public float until; }

        readonly List<Line> lines = new List<Line>();
        readonly Dictionary<string, Bubble> bubbles = new Dictionary<string, Bubble>();
        readonly List<string> expired = new List<string>();
        bool typing, toldHowTo;
        readonly NetTextBox box = new NetTextBox(MaxChars);
        int sendFrame = -1;
        float openedAt;
        GUIStyle field, lineStyle, bubbleStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetChat>() != null) return;
            var go = new GameObject("NetChat");
            DontDestroyOnLoad(go);
            go.AddComponent<NetChat>();
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
                    if (!toldHowTo) { toldHowTo = true; Add($"<color={DimColor}>Enter를 누르면 채팅할 수 있어요</color>"); }
                    break;
                case "join":
                    if (!string.IsNullOrEmpty(m.name)) Add($"<color={DimColor}>{m.name} 님이 이 맵에 왔어요</color>");
                    break;
                case "say":
                    if (string.IsNullOrEmpty(m.text)) break;
                    bool mine = m.id == net.MyId;
                    bool here = mine || (m.map == net.CurrentMap && !string.IsNullOrEmpty(m.map));
                    string who = string.IsNullOrEmpty(m.name) ? m.id : m.name;
                    Add(here
                        ? $"<color={(mine ? MeColor : OtherColor)}>{who}</color>: {m.text}"
                        : $"<color={DimColor}>{who} (다른 맵)</color>: <color={DimColor}>{m.text}</color>");
                    if (here) bubbles[m.id ?? ""] = new Bubble { text = m.text, until = Time.unscaledTime + Mathf.Clamp(3f + m.text.Length * 0.08f, 3f, 8f) };
                    if (!mine) Sfx.Play("open", here ? .25f : .12f);
                    break;
                case "error":
                    Add($"<color={ErrorColor}>서버: {m.why}</color>");
                    break;
            }
        }

        void Add(string text)
        {
            lines.Add(new Line { text = text, at = Time.unscaledTime });
            if (lines.Count > MaxLines) lines.RemoveAt(0);
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
                    net.Say(box.Full);
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
            bool anyLine = typing || (lines.Count > 0 && Time.unscaledTime - lines[lines.Count - 1].at < LineSeconds);
            if (!anyLine && bubbles.Count == 0) return;
            UIStyle.Begin();
            Styles();
            GUI.depth = -6;

            DrawBubbles(net);
            if (UIState.Blocking && !typing) return;   // 다른 창이 열려 있으면 목록은 숨긴다

            float bottom = UIStyle.VirtualHeight - 64f;   // 접속 표시(아래에서 60) 바로 위
            if (typing) bottom = DrawInput(bottom);
            DrawLines(bottom);
        }

        void Styles()
        {
            if (lineStyle != null && lineStyle.font == UIStyle.SmallFont) return;
            lineStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.SmallFont, fontSize = UIStyle.SmallSize, richText = true, wordWrap = true, clipping = TextClipping.Overflow };
            bubbleStyle = new GUIStyle(lineStyle) { alignment = TextAnchor.MiddleCenter, richText = false };
            bubbleStyle.normal.textColor = new Color(0.97f, 0.95f, 0.9f);
            field = new GUIStyle(GUI.skin.textField) { font = UIStyle.RegularFont, fontSize = UIStyle.BodySize, alignment = TextAnchor.MiddleLeft };
        }

        /// <summary>입력 칸. 위쪽 끝 y를 돌려준다 (목록을 그 위에 쌓는다)</summary>
        float DrawInput(float bottom)
        {
            var panel = new Rect(6f, bottom - 24f, LogWidth + 40f, 22f);
            GUI.Box(panel, GUIContent.none, UIStyle.Box);
            GUI.Label(new Rect(panel.x + 6f, panel.y + 3f, 30f, 16f), "<b>말</b>", UIStyle.Rich);
            box.Draw(new Rect(panel.x + 30f, panel.y + 3f, panel.width - 36f, 16f), field);
            GUI.Label(new Rect(panel.x, panel.y - 13f, panel.width, 12f),
                $"<size={UIStyle.SmallSize}><color={DimColor}>Enter 보내기 · Esc 닫기 · 접속한 모두에게 보여요 · {box.Full.Length}/{MaxChars}</color></size>", UIStyle.Rich);
            return panel.y - 15f;
        }

        void DrawLines(float bottom)
        {
            var old = GUI.color;
            float y = bottom;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                float age = Time.unscaledTime - lines[i].at;
                float alpha = typing ? 1f : Mathf.Clamp01((LineSeconds - age) / FadeSeconds);
                if (alpha <= 0f) continue;
                var content = new GUIContent(lines[i].text);
                float h = lineStyle.CalcHeight(content, LogWidth);
                y -= h;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                UIStyle.Outlined(new Rect(8f, y, LogWidth, h), lines[i].text, lineStyle);
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
                float cx = sp.x / scale, by = (Screen.height - sp.y) / scale;
                var r = new Rect(cx - w / 2f - 5f, by - h - 6f, w + 10f, h + 6f);
                GUI.Box(r, GUIContent.none, UIStyle.Box);
                GUI.Label(new Rect(r.x + 5f, r.y + 3f, w, h), content, bubbleStyle);
            }
        }
    }
}
