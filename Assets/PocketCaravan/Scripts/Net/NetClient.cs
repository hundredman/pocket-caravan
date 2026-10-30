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
    /// - 6단계: 끊기면(서버를 다시 올릴 때·와이파이가 끊길 때) 2·4·8·16·30초 간격으로 저절로 다시 접속한다. 끊기를 누르면 멈춘다.
    ///   게임을 켤 때마다 만드는 비밀 글자(key)를 보내서, 다시 붙으면 서버가 예전 번호를 그대로 준다.
    ///   5초마다 ping을 보내고(가만히 있어도), 20초 넘게 서버 소식이 없으면 끊긴 것으로 보고 다시 붙는다.
    /// - 7단계: 채팅(Say). 입력 칸과 말풍선은 NetChat이 그린다.
    /// 메시지는 JSON 한 줄이고 "t"가 종류다 (Server/src/world.ts와 같은 약속).
    /// </summary>
    public class NetClient : MonoBehaviour
    {
        public const int Protocol = 1;
        /// <summary>팀 시험 서버 (Cloudflare, 2026-10-30 배포). 내 컴퓨터 서버로 개발할 때는 접속 창에서 ws://127.0.0.1:8787/ws 로 바꾼다</summary>
        public const string DefaultAddress = "wss://pocket-caravan-server.baribari.workers.dev/ws";
        /// <summary>Waiting: 끊겨서 다시 접속하기를 기다리는 중</summary>
        public enum State { Offline, Connecting, Online, Waiting }
        const float PingSeconds = 5f, DeadSeconds = 20f, MaxRetryDelay = 30f;
        const string PingJson = "{\"t\":\"ping\"}";   // 서버 자동 응답과 글자가 똑같아야 한다

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
            public string text;
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
        [Serializable] class HelloOut { public string t = "hello"; public int v = Protocol; public string name; public string look; public string key; }
        [Serializable] class EnterOut { public string t = "enter"; public string map; }
        [Serializable] class PosOut { public string t = "pos"; public float x, y; public int fx, fy; public bool m; }
        [Serializable] class SayOut { public string t = "say"; public string text; }

        public static NetClient I { get; private set; }
        /// <summary>서버에서 메시지가 올 때마다 (메인 스레드). 다음 단계(위치·다른 사람 표시)가 듣는다</summary>
        public static event Action<Msg> Received;

        public State Status { get; private set; }
        public string MyId { get; private set; } = "";
        /// <summary>서버가 정해 준 내 이름 (겹치면 "윤채(2)"처럼 바뀐다)</summary>
        public string MyName { get; private set; } = "";
        /// <summary>다시 접속하려고 한 횟수 (Waiting · Connecting 중)</summary>
        public int Retries => retries;
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
        // 받은 것·문제는 어느 소켓에서 왔는지 붙여 둔다 (다시 붙은 뒤 예전 소켓 소식은 버린다)
        readonly ConcurrentQueue<(ClientWebSocket ws, string json)> inbox = new ConcurrentQueue<(ClientWebSocket, string)>();
        readonly ConcurrentQueue<string> outbox = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<(ClientWebSocket ws, string why, int code)> problems = new ConcurrentQueue<(ClientWebSocket, string, int)>();
        SemaphoreSlim outSignal;
        /// <summary>서버에서 마지막으로 무언가 받은 시각 (DateTime 틱, 0이면 아직 접속 전)</summary>
        long lastHeardTicks;

        /// <summary>접속을 누른 뒤 끊기를 누르기 전까지 true. 끊기면 이 동안만 다시 접속한다</summary>
        bool wantOnline;
        int retries;
        float retryAt;
        /// <summary>이 게임 실행의 비밀 글자. 다시 붙을 때 서버가 같은 사람으로 알아본다</summary>
        readonly string sessionKey = Guid.NewGuid().ToString("N");

        bool window;
        string address;
        /// <summary>이름 칸 (한글이 쳐지는 칸, NetTextBox)</summary>
        readonly NetTextBox nameBox = new NetTextBox(16);
        string playerName { get => nameBox.Text; set => nameBox.Text = value ?? ""; }
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

        /// <summary>접속 창의 접속 버튼. 끊기를 누를 때까지 끊겨도 다시 붙는다</summary>
        public void Connect()
        {
            if (Status != State.Offline) return;
            nameBox.End();
            string url = (address ?? "").Trim();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "ws" && uri.Scheme != "wss"))
            {
                LastError = "주소는 ws:// 또는 wss://로 시작해야 합니다";
                return;
            }
            PlayerPrefs.SetString("net.address", url);
            PlayerPrefs.SetString("net.name", (playerName ?? "").Trim());
            PlayerPrefs.Save();
            wantOnline = true;
            retries = 0;
            LastError = "";
            target = uri;
            Open();
        }

        /// <summary>접속 버튼으로 정한 주소 (다시 접속할 때도 이 주소로)</summary>
        Uri target;

        async void Open()
        {
            if (target == null || Status == State.Connecting || Status == State.Online) return;
            var uri = target;
            Status = State.Connecting;
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
                if (socket == ws) Lost("접속 실패: " + Short(e), 0);
                return;
            }
            if (socket != ws) return;   // 기다리는 사이에 끊기를 눌렀다
            Interlocked.Exchange(ref lastHeardTicks, DateTime.UtcNow.Ticks);
            _ = Task.Run(() => ReceiveLoop(ws, c.Token));
            _ = Task.Run(() => SendLoop(ws, outSignal, c.Token));
            var origin = GameManager.I != null && GameManager.I.Player != null ? GameManager.I.Player.origin : null;
            Send(new HelloOut { name = (playerName ?? "").Trim(), look = origin != null ? origin.id : "", key = sessionKey });
        }

        /// <summary>끊기 버튼 · 게임 끝. 다시 접속도 멈춘다</summary>
        public void Disconnect()
        {
            wantOnline = false;
            retries = 0;
            Drop(null);
        }

        /// <summary>
        /// 뜻하지 않게 끊겼다. 서버가 일부러 닫은 경우(너무 많이 보냄 · 버전 다름 · 다른 곳에서 다시 접속)만 빼고 다시 접속을 잡는다
        /// </summary>
        void Lost(string reason, int code)
        {
            bool final = code == 4001 || code == 4002 || code == 4004;
            if (final) wantOnline = false;
            Drop(reason);
            if (!wantOnline) { retries = 0; return; }
            retries++;
            Status = State.Waiting;
            retryAt = Time.unscaledTime + Mathf.Min(MaxRetryDelay, 2f * (1 << Mathf.Min(retries - 1, 5)));
        }

        static string CloseReason(int code, string text) => code switch
        {
            4001 => "메시지를 너무 많이 보내서 서버가 끊었습니다",
            4002 => "서버와 게임 버전이 다릅니다. 브랜치를 다시 받아 주세요",
            4003 => "서버가 응답 없는 접속으로 보고 끊었습니다",
            4004 => "같은 게임이 다른 연결로 다시 접속했습니다",
            _ => $"서버가 연결을 닫았습니다 ({code}{(string.IsNullOrEmpty(text) ? "" : " " + text)})",
        };

        /// <summary>연결을 정리한다. reason이 있으면 화면에 남긴다</summary>
        void Drop(string reason)
        {
            if (Status == State.Offline && socket == null) return;
            Status = State.Offline;
            Interlocked.Exchange(ref lastHeardTicks, 0);
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

        /// <summary>채팅 한 줄 (80자까지는 서버가 자른다). 서버가 받아 주면 나에게도 "say"로 돌아온다</summary>
        public void Say(string text)
        {
            if (Status != State.Online || string.IsNullOrWhiteSpace(text)) return;
            Send(new SayOut { text = text.Trim() });
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
                            Interlocked.Exchange(ref lastHeardTicks, DateTime.UtcNow.Ticks);
                            if (r.MessageType == WebSocketMessageType.Close)
                            {
                                int code = (int)(r.CloseStatus ?? 0);
                                problems.Enqueue((ws, CloseReason(code, r.CloseStatusDescription), code));
                                return;
                            }
                            ms.Write(buffer, 0, r.Count);
                        } while (!r.EndOfMessage);
                        inbox.Enqueue((ws, Encoding.UTF8.GetString(ms.ToArray())));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { problems.Enqueue((ws, "연결이 끊겼습니다: " + Short(e), 0)); }
        }

        /// <summary>줄에 쌓인 것을 차례로 보내고, 5초마다 ping을 보낸다 (게임 화면이 멈춰 있어도)</summary>
        async Task SendLoop(ClientWebSocket ws, SemaphoreSlim signal, CancellationToken ct)
        {
            var nextPing = DateTime.UtcNow.AddSeconds(PingSeconds);
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var wait = nextPing - DateTime.UtcNow;
                    if (wait > TimeSpan.Zero) await signal.WaitAsync(wait, ct);
                    while (outbox.TryDequeue(out var json)) await SendText(ws, json, ct);
                    if (DateTime.UtcNow >= nextPing)
                    {
                        await SendText(ws, PingJson, ct);
                        nextPing = DateTime.UtcNow.AddSeconds(PingSeconds);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { problems.Enqueue((ws, "보내기 실패: " + Short(e), 0)); }
        }

        static Task SendText(ClientWebSocket ws, string json, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            return ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        static string Short(Exception e)
        {
            while (e.InnerException != null) e = e.InnerException;
            return e.Message;
        }

        // ---------------- 메인 스레드 ----------------

        void Update()
        {
            while (inbox.TryDequeue(out var got))
            {
                if (got.ws != socket) continue;   // 예전 연결에서 늦게 온 것
                Msg msg;
                try { msg = JsonUtility.FromJson<Msg>(got.json); }
                catch { continue; }
                if (msg == null) continue;
                Handle(msg);
            }
            while (problems.TryDequeue(out var p))
                if (p.ws == socket && socket != null) Lost(p.why, p.code);
            long heard = Interlocked.Read(ref lastHeardTicks);
            if (socket != null && heard != 0 && (DateTime.UtcNow.Ticks - heard) / TimeSpan.TicksPerSecond > DeadSeconds)
                Lost("서버 소식이 20초 넘게 없습니다", 0);
            if (Status == State.Waiting && Time.unscaledTime >= retryAt) Open();
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
                    MyName = string.IsNullOrEmpty(msg.name) ? msg.id : msg.name;
                    OnlineCount = msg.online;
                    LastError = "";
                    string asked = (playerName ?? "").Trim();
                    if (retries > 0) Notify.Show($"다시 연결됐어요 · 지금 {msg.online}명", 2.5f);
                    else if (asked.Length > 0 && MyName != asked) Notify.Show($"이름이 겹쳐서 '{MyName}'(으)로 들어왔어요", 4f);
                    else Notify.Show($"온라인 접속: {msg.id} · 지금 {msg.online}명", 3f);
                    retries = 0;
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

        void OnDestroy() => Disconnect();
        void OnApplicationQuit() => Disconnect();

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
                bool typing = GUIUtility.keyboardControl != 0 || nameBox.Active;
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
            nameBox.End();
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
            string text = Status switch
            {
                State.Online => $"<color=#8fe08f>●</color> 온라인 · {MyId} · 전체 {OnlineCount}명{here}",
                State.Waiting => $"<color=#ffb347>○</color> 다시 연결 중… ({retries}번째 · {Mathf.CeilToInt(Mathf.Max(0f, retryAt - Time.unscaledTime))}초 뒤)",
                _ => retries > 0 ? $"<color=#ffde59>○</color> 다시 연결하는 중… ({retries}번째)" : "<color=#ffde59>●</color> 접속 중…",
            };
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
            var addressRect = new Rect(x, y + 15f, fw, 18f);
            if (Event.current.type == EventType.MouseDown && addressRect.Contains(Event.current.mousePosition)) nameBox.End();
            address = GUI.TextField(addressRect, address ?? "", 160, field);
            y += 38f;
            GUI.Label(new Rect(x, y, fw, 14f), "이름 (다른 사람에게 보임 · 칸을 눌러 입력)", UIStyle.Rich);
            if (editable) nameBox.Draw(new Rect(x, y + 15f, fw, 18f), field, clickToToggle: true);
            else GUI.TextField(new Rect(x, y + 15f, fw, 18f), playerName, 16, field);
            y += 40f;
            GUI.enabled = true;

            var b = new Rect(x, y, (fw - 8f) / 2f, 20f);
            if (Status == State.Offline)
            {
                if (GUI.Button(b, "접속", UIStyle.Button)) { GUIUtility.keyboardControl = 0; Connect(); }
            }
            else if (Status == State.Connecting && retries == 0)
            {
                GUI.enabled = false; GUI.Button(b, "접속 중…", UIStyle.Button); GUI.enabled = true;
            }
            else if (GUI.Button(b, Status == State.Online ? "끊기" : "그만 (다시 연결 멈춤)", UIStyle.Button)) Disconnect();
            if (GUI.Button(new Rect(b.xMax + 8f, y, b.width, 20f), "닫기 (O · Esc)", UIStyle.Button)) CloseWindow();
            y += 26f;

            string line = Status == State.Waiting ? $"<color=#ffb347>{LastError} · 곧 다시 접속합니다</color>"
                : !string.IsNullOrEmpty(LastError) ? $"<color=#ff9a8a>{LastError}</color>"
                : Status == State.Online ? $"<color=#8fe08f>접속됨 · {MyName} ({MyId}) · 지금 {OnlineCount}명 · Enter 채팅</color>"
                : Status == State.Connecting ? "서버에 연결하는 중…"
                : "<color=#c9c3d6>이름을 넣고 접속을 누르세요 (주소는 팀 시험 서버)</color>";
            GUI.Label(new Rect(x, y, fw, 16f), $"<size={UIStyle.SmallSize}>{line}</size>", UIStyle.Rich);
        }
    }
}
