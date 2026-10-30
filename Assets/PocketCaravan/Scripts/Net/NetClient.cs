using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 온라인 접속 (2026-10-30, feature/online 시험). 서버는 저장소의 Server/ (Cloudflare Workers + Durable Objects).
    /// - O 키: 접속 창 (서버 주소 · 이름 · 접속/끊기). 주소와 이름은 이 컴퓨터 설정(PlayerPrefs)에만 남는다. 게임 저장 파일과는 무관하다.
    /// - 접속 중이면 화면 왼쪽 아래에 "온라인 · p1 · 2명 접속".
    /// - 받은 메시지는 백그라운드에서 줄에 넣고 Update(메인 스레드)에서 꺼내 처리한다. 보내기도 한 줄로 차례대로 보낸다.
    /// - 접속하지 않으면 아무것도 하지 않는다(혼자 플레이 그대로). 공용 코드는 읽기만 한다.
    /// - 4단계: 접속하면 지금 맵을 알리고(enter), 움직이는 동안 1초에 10번·가만히 있으면 1초에 1번 위치를 보낸다(pos).
    ///   같은 맵 사람 목록(Peers)은 서버가 보내 주는 room · join · pos · leave로 맞춘다. 화면에는 RemotePlayers가 그린다.
    /// - 5단계: 접속할 때 내 출신(생김새)을 함께 보낸다(look).
    /// 메시지는 JSON 한 줄이고 "t"가 종류다 (Server/src/world.ts와 같은 약속).
    /// </summary>
    public class NetClient : MonoBehaviour
    {
        public const int Protocol = 1;
        /// <summary>팀 시험 서버 (Cloudflare, 2026-10-30 배포). 내 컴퓨터 서버로 개발할 때는 접속 창에서 ws://127.0.0.1:8787/ws 로 바꾼다</summary>
        public const string DefaultAddress = "wss://pocket-caravan-server.baribari.workers.dev/ws";
        public enum State { Offline, Connecting, Online }

        /// <summary>서버에서 받는 메시지 한 개. 종류마다 쓰는 칸만 채워진다(JsonUtility는 없는 칸을 기본값으로 둔다)</summary>
        [Serializable]
        public class Msg
        {
            public string t;
            public int v;
            public string id;
            public string name;
            public string look;
            public int online;
            public string why;
            public string map;
            public float x, y;
            public int fx, fy;
            public bool m;
            public List<Peer> players;
        }

        /// <summary>같은 맵에 있는 다른 플레이어 (서버가 알려 준 마지막 상태)</summary>
        [Serializable]
        public class Peer
        {
            public string id;
            public string name;
            public string look;     // 출신 아이디 (venice · goryeo · viking · arabia) → 캐릭터 그림
            public float x, y;
            public int fx, fy;
            public bool m;
            [NonSerialized] public float heardAt;
        }

        // 보내는 메시지 (필요한 칸만)
        [Serializable] class HelloOut { public string t = "hello"; public int v = Protocol; public string name; public string look; }
        [Serializable] class EnterOut { public string t = "enter"; public string map; }
        [Serializable] class PosOut { public string t = "pos"; public float x, y; public int fx, fy; public bool m; }

        public static NetClient I { get; private set; }
        /// <summary>서버에서 메시지가 올 때마다 (메인 스레드). 다음 단계(위치·다른 사람 표시)가 듣는다</summary>
        public static event Action<Msg> Received;

        public State Status { get; private set; }
        public string MyId { get; private set; } = "";
        public int OnlineCount { get; private set; }
        public string LastError { get; private set; } = "";
        /// <summary>지금 맵에 같이 있는 다른 플레이어 (아이디 → 상태). 다음 단계에서 화면에 그린다</summary>
        public IReadOnlyDictionary<string, Peer> Peers => peers;
        /// <summary>서버에 알린 지금 맵 (맵을 옮기는 중이면 서버 답을 기다린다)</summary>
        public string CurrentMap => sentMap;

        readonly Dictionary<string, Peer> peers = new Dictionary<string, Peer>();
        string sentMap;
        bool waitingRoom;
        float lastPosAt;
        Vector2 lastPos;
        Vector2Int lastFacing;
        bool lastMoving;

        ClientWebSocket socket;
        CancellationTokenSource cts;
        readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> outbox = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<string> problems = new ConcurrentQueue<string>();
        SemaphoreSlim outSignal;

        bool window;
        string address, playerName;
        GUIStyle field;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("NetClient");
            DontDestroyOnLoad(go);
            I = go.AddComponent<NetClient>();
        }

        void Awake()
        {
            address = PlayerPrefs.GetString("net.address", DefaultAddress);
            playerName = PlayerPrefs.GetString("net.name", "");
        }

        // ---------------- 접속 · 끊기 ----------------

        public async void Connect()
        {
            if (Status != State.Offline) return;
            string url = (address ?? "").Trim();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "ws" && uri.Scheme != "wss"))
            {
                LastError = "주소는 ws:// 또는 wss://로 시작해야 합니다";
                return;
            }
            PlayerPrefs.SetString("net.address", url);
            PlayerPrefs.SetString("net.name", (playerName ?? "").Trim());
            PlayerPrefs.Save();

            Status = State.Connecting;
            LastError = "";
            var ws = new ClientWebSocket();
            var c = new CancellationTokenSource();
            socket = ws; cts = c;
            outSignal = new SemaphoreSlim(0);
            try
            {
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(c.Token))
                {
                    timeout.CancelAfter(8000);
                    await ws.ConnectAsync(uri, timeout.Token);
                }
            }
            catch (Exception e)
            {
                if (socket == ws) Drop("접속 실패: " + Short(e));
                return;
            }
            if (socket != ws) return;   // 기다리는 사이에 끊기를 눌렀다
            _ = Task.Run(() => ReceiveLoop(ws, c.Token));
            _ = Task.Run(() => SendLoop(ws, outSignal, c.Token));
            var origin = GameManager.I != null && GameManager.I.Player != null ? GameManager.I.Player.origin : null;
            Send(new HelloOut { name = (playerName ?? "").Trim(), look = origin != null ? origin.id : "" });
        }

        public void Disconnect() => Drop(null);

        /// <summary>연결을 정리한다. reason이 있으면 화면에 남긴다</summary>
        void Drop(string reason)
        {
            if (Status == State.Offline && socket == null) return;
            Status = State.Offline;
            MyId = "";
            OnlineCount = 0;
            peers.Clear();
            sentMap = null;
            waitingRoom = false;
            if (!string.IsNullOrEmpty(reason)) LastError = reason;
            var ws = socket; var c = cts;
            socket = null; cts = null;
            while (outbox.TryDequeue(out _)) { }
            if (ws != null) _ = CloseQuietly(ws, c);
        }

        static async Task CloseQuietly(ClientWebSocket ws, CancellationTokenSource c)
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    using (var t = new CancellationTokenSource(2000))
                        await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", t.Token);
            }
            catch { /* 이미 끊김 */ }
            finally
            {
                c?.Cancel();
                ws.Dispose();
            }
        }

        public void Send(object msg)
        {
            if (socket == null || outSignal == null) return;
            outbox.Enqueue(JsonUtility.ToJson(msg));
            outSignal.Release();
        }

        // ---------------- 백그라운드 (Unity API를 쓰지 않는다) ----------------

        async Task ReceiveLoop(ClientWebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[16 * 1024];
            try
            {
                while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult r;
                        do
                        {
                            r = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                            if (r.MessageType == WebSocketMessageType.Close) { problems.Enqueue("서버가 연결을 닫았습니다"); return; }
                            ms.Write(buffer, 0, r.Count);
                        } while (!r.EndOfMessage);
                        inbox.Enqueue(Encoding.UTF8.GetString(ms.ToArray()));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { problems.Enqueue("연결이 끊겼습니다: " + Short(e)); }
        }

        async Task SendLoop(ClientWebSocket ws, SemaphoreSlim signal, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await signal.WaitAsync(ct);
                    while (outbox.TryDequeue(out var json))
                    {
                        var bytes = Encoding.UTF8.GetBytes(json);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { problems.Enqueue("보내기 실패: " + Short(e)); }
        }

        static string Short(Exception e)
        {
            while (e.InnerException != null) e = e.InnerException;
            return e.Message;
        }

        // ---------------- 메인 스레드 ----------------

        void Update()
        {
            while (problems.TryDequeue(out var why))
                if (Status != State.Offline) Drop(why);
            while (inbox.TryDequeue(out var json))
            {
                Msg msg;
                try { msg = JsonUtility.FromJson<Msg>(json); }
                catch { continue; }
                if (msg == null) continue;
                Handle(msg);
            }
            if (Status == State.Online) Report();
            WindowKeys();
        }

        /// <summary>맵이 바뀌었으면 알리고, 위치를 보낸다 (움직이면 0.1초, 가만히 있으면 1초마다)</summary>
        void Report()
        {
            var gm = GameManager.I;
            var map = MapRoot.Current;
            if (map == null || (gm != null && gm.IsTravelling)) return;
            if (map.mapId != sentMap)
            {
                sentMap = map.mapId;
                waitingRoom = true;
                peers.Clear();
                Send(new EnterOut { map = sentMap });
                lastPosAt = -10f;
            }
            var pc = PlayerController.Instance;
            if (pc == null) return;
            Vector2 pos = pc.transform.position;
            var facing = pc.anim != null ? pc.anim.facing : lastFacing;
            bool moving = pc.Motor != null && pc.Motor.IsMoving;
            float since = Time.unscaledTime - lastPosAt;
            bool changed = (pos - lastPos).sqrMagnitude > 0.0004f || facing != lastFacing || moving != lastMoving;
            if (since < 0.1f || (!changed && since < 1f)) return;
            lastPosAt = Time.unscaledTime;
            lastPos = pos; lastFacing = facing; lastMoving = moving;
            Send(new PosOut { x = pos.x, y = pos.y, fx = facing.x, fy = facing.y, m = moving });
        }

        void Handle(Msg msg)
        {
            switch (msg.t)
            {
                case "welcome":
                    Status = State.Online;
                    MyId = msg.id;
                    OnlineCount = msg.online;
                    Notify.Show($"온라인 접속: {msg.id} · 지금 {msg.online}명", 3f);
                    break;
                case "online":
                    OnlineCount = msg.online;
                    break;
                case "room":
                    if (msg.map != sentMap) break;           // 그새 다른 맵으로 옮겼다
                    waitingRoom = false;
                    peers.Clear();
                    if (msg.players != null)
                        foreach (var p in msg.players) { p.heardAt = Time.unscaledTime; peers[p.id] = p; }
                    break;
                case "join":
                case "pos":
                    if (waitingRoom || string.IsNullOrEmpty(msg.id) || msg.id == MyId) break;   // 맵을 옮기는 중엔 예전 맵 소식을 버린다
                    if (!peers.TryGetValue(msg.id, out var peer)) peers[msg.id] = peer = new Peer { id = msg.id };
                    if (!string.IsNullOrEmpty(msg.name)) peer.name = msg.name;
                    if (!string.IsNullOrEmpty(msg.look)) peer.look = msg.look;
                    peer.x = msg.x; peer.y = msg.y; peer.fx = msg.fx; peer.fy = msg.fy; peer.m = msg.m;
                    peer.heardAt = Time.unscaledTime;
                    break;
                case "leave":
                    peers.Remove(msg.id ?? "");
                    break;
                case "error":
                    LastError = "서버: " + msg.why;
                    break;
            }
            Received?.Invoke(msg);
        }

        void OnDestroy() => Drop(null);
        void OnApplicationQuit() => Drop(null);

        // ---------------- 접속 창 (O) ----------------

        static bool OPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.O);
#endif
        }

        void WindowKeys()
        {
            if (window)
            {
                bool typing = GUIUtility.keyboardControl != 0;
                if (InputReader.CancelPressed() || (OPressed() && !typing)) CloseWindow();
                return;
            }
            if (!OPressed() || UIState.Blocking || UIState.JustClosed) return;
            if (GameManager.I != null && GameManager.I.IsTravelling) return;
            window = true;
            UIState.Blocking = true;
            Sfx.Play("open", .5f);
        }

        void CloseWindow()
        {
            window = false;
            GUIUtility.keyboardControl = 0;
            UIState.Release();
        }

        void OnGUI()
        {
            if (Status == State.Offline && !window) return;
            UIStyle.Begin();
            if (Status != State.Offline) DrawStatus();
            if (window) DrawWindow();
        }

        void DrawStatus()
        {
            string here = sentMap != null && !waitingRoom ? $" · 이 맵 {peers.Count + 1}명" : "";
            string text = Status == State.Online
                ? $"<color=#8fe08f>●</color> 온라인 · {MyId} · 전체 {OnlineCount}명{here}"
                : "<color=#ffde59>●</color> 접속 중…";
            GUI.depth = -6;
            float w = UIStyle.Rich.CalcSize(new GUIContent(text)).x + 12f;
            var r = new Rect(6f, UIStyle.VirtualHeight - 60f, w, 16f);
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            GUI.Label(new Rect(r.x + 6f, r.y, w, r.height), text, UIStyle.Rich);
        }

        void DrawWindow()
        {
            GUI.depth = -11;
            if (field == null) field = new GUIStyle(GUI.skin.textField) { fontSize = UIStyle.BodySize, alignment = TextAnchor.MiddleLeft };
            float w = 300f, h = 176f;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, (UIStyle.VirtualHeight - h) / 2f, w, h);
            GUI.Box(r, "", UIStyle.Box);
            float x = r.x + 14f, fw = w - 28f, y = r.y + 8f;
            GUI.Label(new Rect(r.x, y, w, 16f), "<b>온라인 접속 (시험)</b>", UIStyle.CenterRich);
            y += 22f;

            bool editable = Status == State.Offline;
            GUI.enabled = editable;
            GUI.Label(new Rect(x, y, fw, 14f), "서버 주소", UIStyle.Rich);
            address = GUI.TextField(new Rect(x, y + 15f, fw, 18f), address ?? "", 160, field);
            y += 38f;
            GUI.Label(new Rect(x, y, fw, 14f), "이름 (다른 사람에게 보임)", UIStyle.Rich);
            playerName = GUI.TextField(new Rect(x, y + 15f, fw, 18f), playerName ?? "", 16, field);
            y += 40f;
            GUI.enabled = true;

            var b = new Rect(x, y, (fw - 8f) / 2f, 20f);
            if (Status == State.Offline)
            {
                if (GUI.Button(b, "접속", UIStyle.Button)) { GUIUtility.keyboardControl = 0; Connect(); }
            }
            else if (Status == State.Connecting)
            {
                GUI.enabled = false; GUI.Button(b, "접속 중…", UIStyle.Button); GUI.enabled = true;
            }
            else if (GUI.Button(b, "끊기", UIStyle.Button)) Disconnect();
            if (GUI.Button(new Rect(b.xMax + 8f, y, b.width, 20f), "닫기 (O · Esc)", UIStyle.Button)) CloseWindow();
            y += 26f;

            string line = !string.IsNullOrEmpty(LastError) ? $"<color=#ff9a8a>{LastError}</color>"
                : Status == State.Online ? $"<color=#8fe08f>접속됨 · 내 번호 {MyId} · 지금 {OnlineCount}명</color>"
                : Status == State.Connecting ? "서버에 연결하는 중…"
                : "<color=#c9c3d6>이름을 넣고 접속을 누르세요 (주소는 팀 시험 서버)</color>";
            GUI.Label(new Rect(x, y, fw, 16f), $"<size={UIStyle.SmallSize}>{line}</size>", UIStyle.Rich);
        }
    }
}
