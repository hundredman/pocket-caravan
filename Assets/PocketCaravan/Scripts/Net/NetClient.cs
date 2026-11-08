using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 온라인 접속 (2026-10-30, feature/online). 서버는 저장소의 Server/ (Cloudflare Workers + Durable Objects).
    /// - 이 파일은 연결과 메시지만 맡는다. 로그인 화면(게임 첫 화면)과 게임 중 온라인 창(O)은 NetLogin이 그리고 Connect를 부른다 (2026-10-31).
    /// - 접속 중이면 화면 왼쪽 아래에 "온라인 · p1 · 2명 접속". 시작 화면이 떠 있는 동안은 숨기고 위치도 보내지 않는다.
    /// - 받은 메시지는 백그라운드에서 줄에 넣고 Update(메인 스레드)에서 꺼내 처리한다. 보내기도 한 줄로 차례대로 보낸다.
    /// - 접속하지 않으면 아무것도 하지 않는다(혼자 플레이 그대로). 공용 코드는 읽기만 한다.
    /// - 4단계: 접속하면 지금 맵을 알리고(enter), 움직이는 동안 1초에 10번·가만히 있으면 1초에 1번 위치를 보낸다(pos).
    ///   같은 맵 사람 목록(Peers)은 서버가 보내 주는 room · join · pos · leave로 맞춘다. 화면에는 RemotePlayers가 그린다.
    /// - 5단계: 접속할 때 내 출신(생김새)을 함께 보낸다(look).
    /// - 6단계: 끊기면(서버를 다시 올릴 때·와이파이가 끊길 때) 2·4·8·16·30초 간격으로 저절로 다시 접속한다. 끊기를 누르면 멈춘다.
    ///   게임을 켤 때마다 만드는 비밀 글자(key)를 보내서, 다시 붙으면 서버가 예전 번호를 그대로 준다.
    ///   5초마다 ping을 보내고(가만히 있어도), 20초 넘게 서버 소식이 없으면 끊긴 것으로 보고 다시 붙는다.
    /// - 7단계: 채팅(Say). 입력 칸과 말풍선은 NetChat이 그린다.
    /// - 8단계: 접속키 로그인(이름 + 접속키 → 계정, 돈·짐 서버 저장은 NetSave), 공유 장터(NetMarket), 리액션 전달(NetEmote → RemotePlayers).
    ///   접속키는 이 컴퓨터 설정(PlayerPrefs)에 남는다. 서버는 접속키를 그대로 두지 않고 확인용 값만 저장한다.
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
            public string veh;      // 11/08 운반 수단 (join · room에만 온다)
            public bool s;          // 11/08 앉기 (pos · join · room)
            public float sy, sq;
            public int ld;          // 11/08 짐 상자 수
            public int online;
            public string why;
            public string map;
            public float x, y;
            public int fx, fy;
            public bool m;
            public string text;
            public List<Peer> players;
            // 8단계
            public bool account, hasSave;
            public SaveData save;
            public int seq;
            public long at;
            public string town, item, e;
            public int n;
            public float d;
            public bool sell;
            public List<TradeRec> list;
            // 9단계 (11/05): 관리자가 지운 접속키를 이번에 새로 정했다
            public bool newPass;
            // 10단계 (11/07): 게임 저장 통째로. fullOk = 이 서버가 받는다, full = 서버 저장 요약(없으면 h가 빈 글), h·zip = 받은 저장
            public bool fullOk, extras;
            public FullInfo full;
            public string h, zip;
            // 11단계 (11/07): 순위표(board·value·best·rank·boards) · 선물(gid·from·to·money·cost·ok)
            public string board, gid, from, to;
            public int value, best, rank, money;
            public float cost;
            public bool ok;
            public List<Board> boards;
            // 11/08 공용 세상 시계: welcome에 서버 시각(now)·기준 시각(epoch, ms)·하루 길이(secondsPerDay)
            public ClockInfo clock;
            // 11/08 공유 장터 재고: marketOk = 이 서버가 재고를 들고 있다, market = 물건마다 적정 재고와의 차이
            public bool marketOk;
            public List<MarketRow> market;
            // 공유 장터 크기 배율 (welcome·online). 0이면 예전 서버
            public float marketScale;
        }

        [Serializable]
        public class MarketRow { public string town, item; public float dev; }

        [Serializable]
        public class ClockInfo { public long now, epoch; public float secondsPerDay; }

        /// <summary>순위표 한 판 (id: "money" 부자 상인 · "gondola" 곤돌라 경주 …)</summary>
        [Serializable]
        public class Board
        {
            public string id;
            public List<BoardRow> rows = new List<BoardRow>();
        }

        [Serializable]
        public class BoardRow
        {
            public string name;
            public int value;
        }

        /// <summary>서버에 저장된 게임 저장 요약 (at: 그 컴퓨터에서 저장 파일을 쓴 시각 ms, h: 저장 글의 SHA-256)</summary>
        [Serializable]
        public class FullInfo
        {
            public long at;
            public string h;
            public int day, money;
            public string map, origin;
            public int size;
            public long saved;
        }

        /// <summary>서버에 저장된 돈·짐 (costs는 물건마다 산 값의 합)</summary>
        [Serializable]
        public class SaveData
        {
            public int money;
            public List<string> items = new List<string>();
            public List<int> counts = new List<int>();
            public List<float> costs = new List<float>();
            public long at;
        }

        /// <summary>공유 장터 거래 한 건 (d: 재고 변화)</summary>
        [Serializable]
        public class TradeRec
        {
            public int seq;
            public long at;
            public string id, name, town, item;
            public int n;
            public float d;
            public bool sell;
        }

        /// <summary>같은 맵에 있는 다른 플레이어 (서버가 알려 준 마지막 상태)</summary>
        [Serializable]
        public class Peer
        {
            public string id;
            public string name;
            public string look;     // 출신 아이디 (venice · goryeo · viking · arabia) → 캐릭터 그림
            public string veh;      // 11/08 운반 수단 아이디 (handcart · north_sled · arabia_camel, 걷기면 "") → VehicleLook이 그린다
            public float x, y;
            public int fx, fy;
            public bool m;
            public bool s;          // 11/08 앉아 있음
            public float sy, sq;    // 11/08 앉은 모습: 그림이 오르내린 높이 · 세로 눌림 비율 (지역마다 자리 모양이 달라서)
            public int ld;          // 11/08 손수레 짐 상자 수 (0~3)
            [NonSerialized] public float heardAt;
        }

        // 보내는 메시지 (필요한 칸만)
        [Serializable] class HelloOut { public string t = "hello"; public int v = Protocol; public string name; public string look; public string key; public string pass; public int since; public bool snap = true; }   // snap: 공유 장터 재고를 통째로 받는다(11/08)
        /// <summary>11/08: look도 싣는다. 시작 화면에서 로그인하면 hello의 look은 아직 고르기 전 기본 출신이라, 맵에 들어갈 때 고친다</summary>
        [Serializable] class EnterOut { public string t = "enter"; public string map; public string look; public string veh; }   // veh: 11/08 운반 수단
        [Serializable] class PosOut { public string t = "pos"; public float x, y; public int fx, fy; public bool m; public bool s; public float sy, sq; public int ld; }   // s·sy·sq: 11/08 앉기, ld: 짐 상자 수
        [Serializable] class SayOut { public string t = "say"; public string text; }
        [Serializable] class EmoteOut { public string t = "emote"; public string e; public string name; }

        public static NetClient I { get; private set; }
        /// <summary>서버에서 메시지가 올 때마다 (메인 스레드). 다음 단계(위치·다른 사람 표시)가 듣는다</summary>
        public static event Action<Msg> Received;

        public State Status { get; private set; }
        public string MyId { get; private set; } = "";
        /// <summary>서버가 정해 준 내 이름 (겹치면 "윤채(2)"처럼 바뀐다)</summary>
        public string MyName { get; private set; } = "";
        /// <summary>다시 접속하려고 한 횟수 (Waiting · Connecting 중)</summary>
        public int Retries => retries;
        /// <summary>접속키로 들어온 계정이면 true (돈·짐이 서버에 저장된다)</summary>
        public bool IsAccount { get; private set; }
        /// <summary>서버가 순위표·선물(11단계)을 안다 (welcome.extras). 예전 서버면 false</summary>
        public bool ServerExtras { get; private set; }
        /// <summary>접속한 서버 주소 (공유 장터가 서버마다 받은 거래 번호를 따로 기억한다)</summary>
        public string Address => target != null ? target.ToString() : "";
        public int OnlineCount { get; private set; }
        public string LastError { get; private set; } = "";
        /// <summary>지금 맵에 같이 있는 다른 플레이어 (아이디 → 상태). 다음 단계에서 화면에 그린다</summary>
        public IReadOnlyDictionary<string, Peer> Peers => peers;
        /// <summary>서버에 알린 지금 맵 (맵을 옮기는 중이면 서버 답을 기다린다)</summary>
        public string CurrentMap => sentMap;

        readonly Dictionary<string, Peer> peers = new Dictionary<string, Peer>();
        string sentMap;
        /// <summary>서버에 알린 내 생김새(출신 아이디). 출신이 바뀌면(새 게임·저장 불러오기) 같은 맵이어도 다시 알린다</summary>
        string sentLook, sentVeh;
        bool lastSit; int lastLd; float restBodyY, restBodyScaleY = 1f;   // 11/08 앉기
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

        /// <summary>로그인 때 넣은 이름·접속키 (다시 접속할 때도 쓴다)</summary>
        string askedName = "", pass = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("NetClient");
            DontDestroyOnLoad(go);
            I = go.AddComponent<NetClient>();
        }

        // ---------------- 접속 · 끊기 ----------------

        /// <summary>
        /// 접속한다 (NetLogin이 부른다). 이름 + 접속키면 계정, 접속키가 없으면 손님.
        /// Disconnect를 부를 때까지 끊겨도 다시 붙는다. 주소가 이상하면 false
        /// </summary>
        public bool Connect(string address, string name, string accessKey)
        {
            if (Status != State.Offline) return false;
            string url = (address ?? "").Trim();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "ws" && uri.Scheme != "wss"))
            {
                LastError = "서버 주소는 ws:// 또는 wss://로 시작해야 합니다";
                return false;
            }
            askedName = (name ?? "").Trim();
            pass = accessKey ?? "";
            wantOnline = true;
            retries = 0;
            LastError = "";
            target = uri;
            Open();
            return true;
        }

        /// <summary>접속한 주소 (다시 접속할 때도 이 주소로)</summary>
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
            Send(new HelloOut { name = askedName, look = origin != null ? origin.id : "", key = sessionKey, pass = pass, since = NetMarket.Since(Address) });
        }

        /// <summary>끊기 버튼 · 게임 끝. 다시 접속도 멈춘다</summary>
        public void Disconnect()
        {
            wantOnline = false;
            WorldClock.Leave();   // 혼자 하기: 세상 시간은 지금 값에서 이 컴퓨터로 이어 흐른다
            retries = 0;
            Drop(null);
        }

        /// <summary>
        /// 뜻하지 않게 끊겼다. 서버가 일부러 닫은 경우(너무 많이 보냄 · 버전 다름 · 다른 곳에서 다시 접속)만 빼고 다시 접속을 잡는다
        /// </summary>
        void Lost(string reason, int code)
        {
            bool final = code == 4001 || code == 4002 || code == 4004 || code == 4005 || code == 4006;
            if (final) wantOnline = false;
            Drop(reason);
            if (!wantOnline) { retries = 0; WorldClock.Leave(); return; }   // 잠깐 끊겨 다시 붙는 동안은 세상 시계를 그대로 쓴다
            retries++;
            Status = State.Waiting;
            retryAt = Time.unscaledTime + Mathf.Min(MaxRetryDelay, 2f * (1 << Mathf.Min(retries - 1, 5)));
        }

        static string CloseReason(int code, string text) => code switch
        {
            4001 => "메시지를 너무 많이 보내서 서버가 끊었습니다",
            4002 => "서버와 게임 버전이 다릅니다. 브랜치를 다시 받아 주세요",
            4003 => "서버가 응답 없는 접속으로 보고 끊었습니다",
            4004 => "같은 이름으로 다른 곳에서 접속해서 여기는 끊었습니다",
            4005 => null,   // 로그인 실패: 서버가 먼저 보낸 이유(error)를 그대로 둔다
            4006 => null,   // 관리자가 끊음(내보내기·계정 지움): 이유는 서버가 먼저 보냈다
            _ => $"서버가 연결을 닫았습니다 ({code}{(string.IsNullOrEmpty(text) ? "" : " " + text)})",
        };

        /// <summary>연결을 정리한다. reason이 있으면 화면에 남긴다</summary>
        void Drop(string reason)
        {
            if (Status == State.Offline && socket == null) return;
            Status = State.Offline;
            Interlocked.Exchange(ref lastHeardTicks, 0);
            MyId = "";
            IsAccount = false;
            ServerExtras = false;
            OnlineCount = 0;
            peers.Clear();
            sentMap = null;
            sentLook = null; sentVeh = null;
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

        /// <summary>리액션을 같은 맵 사람들에게 알린다 (NetEmote.Send로 부른다)</summary>
        public void Emote(string id, string name)
        {
            if (Status != State.Online || string.IsNullOrEmpty(id)) return;
            Send(new EmoteOut { e = id, name = name ?? "" });
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
        }

        /// <summary>맵이 바뀌었으면 알리고, 위치를 보낸다 (움직이면 0.1초, 가만히 있으면 1초마다)</summary>
        void Report()
        {
            if (StartScreen.IsOpen) return;   // 아직 시작 화면: 고른 뒤 들어갈 맵에서부터 알린다
            var gm = GameManager.I;
            var map = MapRoot.Current;
            if (map == null || (gm != null && gm.IsTravelling)) return;
            string look = gm != null && gm.Player != null && gm.Player.origin != null ? gm.Player.origin.id : "";
            // 11/08 기우: 운반 수단도 enter에 싣는다. 바뀌면 같은 맵에 enter를 다시 보내 같은 맵 사람들이 join으로 새로 그린다
            string veh = gm != null && gm.Player != null && gm.Player.vehicle != null && gm.Player.vehicle.id != "walk" ? gm.Player.vehicle.id : "";
            if (map.mapId != sentMap || look != sentLook || veh != sentVeh)
            {
                if (map.mapId != sentMap) { waitingRoom = true; peers.Clear(); }   // 생김새만 바뀌면 같은 맵: 다른 사람 목록은 그대로
                sentMap = map.mapId;
                sentLook = look; sentVeh = veh;
                Send(new EnterOut { map = sentMap, look = look, veh = veh });
                lastPosAt = -10f;
            }
            var pc = PlayerController.Instance;
            if (pc == null) return;
            Vector2 pos = pc.transform.position;
            var facing = pc.anim != null ? pc.anim.facing : lastFacing;
            bool moving = pc.Motor != null && pc.Motor.IsMoving;
            // 11/08 기우: 앉기. 서 있을 때의 그림 자리·크기를 기억해 두고, 앉으면 그 차이를 함께 보낸다
            bool sit = SeatRest.Active;
            float sy = 0f, sq = 1f;
            var body = pc.anim != null ? pc.anim.transform : null;
            if (body != null)
            {
                if (!sit) { restBodyY = body.localPosition.y; restBodyScaleY = Mathf.Max(0.01f, body.localScale.y); }
                else { sy = body.localPosition.y - restBodyY; sq = body.localScale.y / restBodyScaleY; }
            }
            int ld = gm != null && gm.Player != null ? VehicleLook.Crates(gm.Player.LoadRatio) : 0;   // 11/08 짐 상자 수
            float since = Time.unscaledTime - lastPosAt;
            bool changed = (pos - lastPos).sqrMagnitude > 0.0004f || facing != lastFacing || moving != lastMoving || sit != lastSit || ld != lastLd;
            if (since < 0.1f || (!changed && since < 1f)) return;
            lastPosAt = Time.unscaledTime;
            lastPos = pos; lastFacing = facing; lastMoving = moving; lastSit = sit; lastLd = ld;
            Send(new PosOut { x = pos.x, y = pos.y, fx = facing.x, fy = facing.y, m = moving, s = sit, sy = sit ? sy : 0f, sq = sit ? sq : 1f, ld = ld });
        }

        void Handle(Msg msg)
        {
            switch (msg.t)
            {
                case "welcome":
                    Status = State.Online;
                    // 공용 세상 시계: 예전 서버(clock 없음)면 이 컴퓨터의 세계 표준시로 같은 계산을 한다
                    WorldClock.Join(msg.clock != null ? msg.clock.now : 0, msg.clock != null ? msg.clock.epoch : 0, msg.clock != null ? msg.clock.secondsPerDay : 0);
                    MyId = msg.id;
                    MyName = string.IsNullOrEmpty(msg.name) ? msg.id : msg.name;
                    IsAccount = msg.account;
                    ServerExtras = msg.extras;
                    OnlineCount = msg.online;
                    LastError = "";
                    string asked = askedName;
                    if (msg.newPass) Notify.Show("접속키를 새로 정했어요. 다음부터 이 접속키로 들어와요", 5f);
                    else if (StartScreen.IsOpen || NetLogin.IsOpen) { }   // 첫 화면: 로그인 화면이 알아서 넘어간다
                    else if (retries > 0) Notify.Show($"다시 연결됐어요 · 지금 {msg.online}명", 2.5f);
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
                    if (msg.t == "join") peer.veh = msg.veh ?? "";   // 운반 수단은 join(들어옴·바꿈)에만 실려 온다
                    peer.x = msg.x; peer.y = msg.y; peer.fx = msg.fx; peer.fy = msg.fy; peer.m = msg.m;
                    peer.s = msg.s; peer.sy = msg.sy; peer.sq = msg.sq > 0f ? msg.sq : 1f;   // 11/08 앉기 (예전 서버면 늘 서 있음)
                    peer.ld = msg.ld;   // 11/08 짐 상자 수
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

        // ---------------- 화면 왼쪽 아래 접속 표시 ----------------

        void OnGUI()
        {
            // 11/08 혼자 할 때도 '혼자 하기' 알약을 띄워, 눌러서 온라인 창을 열 수 있게 (예전엔 O 키로만)
            if (StartScreen.IsOpen || MapRoot.Current == null || GameManager.I == null) return;
            UIStyle.Begin();
            DrawStatus();
        }

        /// <summary>
        /// 11/05 B안: 오른쪽 위 단추 왼쪽에 "사람 3명" 알약. 마우스를 올리면 내 이름·이 맵 사람 수.
        /// 끊겨서 다시 붙는 중이면 빨간 점(알림), 처음 접속 중이면 노란 점.
        /// </summary>
        void DrawStatus()
        {
            GUI.depth = -6;
            bool online = Status == State.Online;
            string label = Status switch
            {
                State.Online => $"{OnlineCount}명",
                State.Offline => "혼자 하기",
                State.Waiting => $"다시 연결 {Mathf.CeilToInt(Mathf.Max(0f, retryAt - Time.unscaledTime))}초",
                _ => retries > 0 ? "다시 연결 중…" : "접속 중…",
            };
            float w = Mathf.Round(6 + 11 + 5 + CuteUI.Width(label, false, true) + 9);
            var r = new Rect(Mathf.Round(HUD.ButtonsLeft - 7 - w), 9, w, 18);
            ScreenZones.Mark("online", r);   // 11/08 위 가운데 알약이 이 왼쪽에서 멈춘다
            CuteUI.Pill(r);
            if (online) CuteUI.Icon(CuteUI.Pic.People, r.x + 6, r.y + 5);
            else CuteUI.Circle(r.x + 8, r.y + 5, 8, Status == State.Offline ? CuteUI.ZeroSoft : Status == State.Waiting || retries > 0 ? CuteUI.Alert : CuteUI.Deco, CuteUI.Ink);
            CuteUI.Label(r.x + 22, r.y + 9, label, Status == State.Offline ? CuteUI.Sub : CuteUI.Text, false, true);
            if (!UIState.Blocking && GUI.Button(r, GUIContent.none, GUIStyle.none)) NetLogin.OpenGame();   // 11/08 눌러도 O처럼 온라인 창
            if (online && r.Contains(Event.current.mousePosition))
            {
                string here = sentMap != null && !waitingRoom ? $"이 맵 {peers.Count + 1}명" : "이 맵 확인 중";
                string line = $"{MyName} · 전체 {OnlineCount}명 · {here}";
                float tw = CuteUI.Width(line, true) + 16;
                var tip = new Rect(Mathf.Round(r.xMax - tw), r.yMax + 4, tw, 17);
                CuteUI.Box(tip, 7, CuteUI.Paper, CuteUI.Ink, 1, 2);
                CuteUI.Label(tip.x + 8, tip.y + 8.5f, line, CuteUI.Text, true);
            }
        }
    }
}
