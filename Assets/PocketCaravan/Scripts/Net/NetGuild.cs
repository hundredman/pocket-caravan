using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 만국 상인회 이야기 「만국 큰 장으로」 (2026-11-09, 11/10 다시 만듦. 1차 시연: 여러 사람이 함께 어울리는 두 번째 튜토리얼).
    /// 1) 초대장: 지역 첫걸음 안내(튜토리얼)를 마치면 봉인 찍힌 만국 상인회 초대장이 펼쳐진다.
    ///    11/10 은인·은샘 의견: 마차로 바로 보내지 않고 직접 걸어간다 (가는 길에 세상을 둘러보는 맛). 길은 퀘스트와 같은
    ///    길 안내 화살표 + 아래 "목표 · ○○ 지나 만국 장터 방면" 줄로 알려 준다 (Navigator.Tutorials). 시험용 순간 이동은 "/상인회 마차".
    ///    11/10 윤채: 가는 길이 심심하지 않게 "고향 특산품 꾸러미 배달"로 (출신마다 무라노 유리 · 고려 인삼 · 북쪽 호박 · 유향).
    ///    짐·데이터는 늘리지 않고 이야기 단계로만 센다 (초대장 받음 ~ 라일라 첫인사 = 배달 중).
    /// 2) 도착: 만국 장터에 들어서면 남쪽 가운데 만국 상인회 광장으로 안내하고, 접수원 라일라 가까이 가면 꾸러미를 받고(배달 삯 30냥) 수첩을 펼쳐 보여 준다.
    /// 3) 상인회 수첩 (11/10 윤채: 늘 떠 있는 카드가 화면을 가려서, 도장을 받을 때만 가운데에 펼쳐지는 도장판으로 바꿈.
    ///    언제든 N 또는 위 알약을 눌러 볼 수 있다). 네 칸 (11/10 윤채: 인사·거래는 재미없어서, 다른 사람과 같이 하거나 놀이로):
    ///    - 합동 공연: 누군가 옆에서 리액션. 접속한 사람과 6초 안에 같이 하면 합동 공연, 혼자면 상인회 상인이 따라 춘다
    ///    - 선물 주고받기: 접속한 사람에게 선물이 닿거나(NetGift), 상인회 상인에게 건네고 답례를 받기(NetMeet)
    ///    - 짐 쌓기 5단: 마당 놀이판 미니게임(NetStackGame). 기록은 온라인 순위표 "guild_stack"
    ///    - 다 같이 기념 사진: 사진 자리에서 Space → 셋 둘 하나 찰칵, 옆에 있는 사람(접속한 사람·장터 사람)이 다 같이 찍힌 사진 한 장
    ///    순서와 상관없이 채워지고, 찾아갈 곳은 머리 위 노란 화살표(화면 밖이면 가장자리 화살표). 만국 장터 안에서 한 것만 센다.
    /// 4) 답장: 다 채우면 상인회장의 답장 → 라일라에게 가서 이름 올리기 → 상인패 수여식(메달 · 꽃가루). 보상 50냥,
    ///    온라인 이름표에 금빛 "★ 상인회"(서버 tag "guild").
    /// 상인회 마당은 만국 장터 남쪽 가운데 만국 상인회 광장(맵 JSON 칸 18~43, 34~50 (11/11 기우: 안뜰을 위로 3줄 넓히고 춤·사진 자리를 맨 위 귀퉁이로). 큰길 서쪽 안뜰, 네 귀퉁이)에 실행 중에만 세운다.
    ///   11/11 은인: 동쪽 4줄 빈터가 비좁아 광장으로 옮김. 칸은 arabia_central.py bazaar()가 비워 둔다 (거기 주석과 같이 고칠 것).
    ///   (옛 자리: 동쪽 빈터 칸 70~90, 23~26)
    ///   접수처·노점·놀이판·사진 자리는 아라비아 큰 장 그림(Resources/Arabia/Fair)을 빌려 쓰고, 아라비아 씬·데이터는 고치지 않는다.
    ///   노점은 제다 공유 장터 시세 (만국 장터엔 장터가 없어서, 11/09 윤채 결정). 선물할 물건을 여기서 산다.
    /// 진행은 공용 진행 메모 "net:guild:step"(0 없음 · 1 초대장 받음 · 2 수첩 다 채움 · 3 가입), "net:guild:duet|gift|stack|photo"(1 = 함),
    ///   "net:guild:welcomed"(라일라 첫인사를 들음). 누구와 했는지는 이번 실행에만 기억한다.
    /// 지역 Intro는 읽기만 한다 (끝났는지: 각 지역 저장의 intro 단계 11).
    /// </summary>
    public class NetGuild : MonoBehaviour
    {
        public const string Bazaar = "arabia_central_bazaar";
        public const string BadgeId = "guild";
        const string StallTown = "arabia_jeddah";
        const string ArriveSpawn = "arabia_central_bazaar_spawn_seam_east_3";
        const int BazaarHeight = 56, Reward = 50, MaxCast = 6, DeliveryPay = 30;
        const float LetterDelay = 2.5f, ReplyDelay = 1.0f, MarkRange = 14f, DuetWindow = 6f, DuetRange = 4f, PhotoRange = 4.5f, CountSeconds = 3.6f;
        const string Clerk = "상인회 접수원 라일라", Chief = "만국 상인회장 아지즈 드림";
        const string StepKey = "net:guild:step", WelcomeKey = "net:guild:welcomed";

        public enum Task { Duet, Gift, Stack, Photo }
        static readonly string[] TaskKeys = { "net:guild:duet", "net:guild:gift", "net:guild:stack", "net:guild:photo" };
        static readonly string[] OldKeys = { "net:guild:hello", "net:guild:emote", "net:guild:trade" };   // 11/09 첫 수첩 (시험용 되돌리기에서 같이 지운다)
        static readonly string[] TaskTitle = { "합동 공연", "선물 주고받기", $"짐 쌓기 {NetStackGame.Goal}단", "다 같이 기념 사진" };
        static readonly string[] TaskHow =
        {
            "상인 앞에서 Space → 리액션! 상인이 따라 춰요. 접속한 사람과 6초 안에 같이 해도 돼요",
            "상인이나 접속한 사람 앞에서 Space → 선물. 상인회 상인은 답례를 줘요",
            $"마당 놀이판에서 {NetStackGame.Goal}단 넘기. 기록은 온라인 순위표에 올라가요",
            "사진 자리에서 Space. 옆에 있는 사람이 다 같이 찍혀요",
        };
        static readonly CuteUI.Pic[] TaskPic = { CuteUI.Pic.Star4, CuteUI.Pic.Sack, CuteUI.Pic.Basket, CuteUI.Pic.People };

        static string[] InvitePara => new[]
        {
            "바다 건너, 사막 건너. 네 지방 상인이 모두 모이는 만국 장터에 큰 장이 섰습니다.",
            "상인회 패는 장부가 아니라 사람으로 얻는 법이지요. 장터 사람들과 함께 춤추고, 선물을 나누고, 놀이판에서 솜씨를 겨루고, 기념 사진도 남겨 보세요. 할 일은 수첩에 적어 두었습니다.",
            $"첫 일감도 하나 맡깁니다. 고향 상인회가 보내는 {Parcel}를 만국 장터 상인회 마당의 라일라에게 배달해 주세요. 만국 장터는 지도 한가운데. 가는 길의 마을과 사람들도 천천히 둘러보며 오세요.",
        };

        /// <summary>배달할 고향 특산품 꾸러미 (출신마다)</summary>
        static string Parcel
        {
            get
            {
                var o = GM != null ? GM.Player.origin : null;
                switch (o != null ? o.id : "")
                {
                    case "venice": return "무라노 유리 꾸러미";
                    case "goryeo": return "고려 인삼 꾸러미";
                    case "viking": return "북쪽 호박 꾸러미";
                    case "arabia": return "아라비아 유향 꾸러미";
                    default: return "고향 특산품 꾸러미";
                }
            }
        }
        static readonly string[] ReplyPara =
        {
            "수첩을 다 채웠다는 소식은 장터 사람들에게 먼저 들었습니다. 벌써 당신 이야기로 떠들썩하더군요.",
            "함께 춤추고, 나누고, 겨루고, 추억을 남기는 상인. 상인회가 기다리던 사람입니다.",
            "상인회 마당의 라일라에게 가서 이름을 올리세요. 상인패를 준비해 두었습니다.",
        };

        static GameManager GM => GameManager.I;
        static int Step
        {
            get => GM != null ? Mathf.RoundToInt(GM.GetMemo(StepKey)) : 0;
            set { if (GM != null) GM.SetMemo(StepKey, value); }
        }
        static bool Has(int t) => GM != null && GM.GetMemo(TaskKeys[t]) >= 1f;
        static int DoneCount { get { int n = 0; for (int i = 0; i < TaskKeys.Length; i++) if (Has(i)) n++; return n; } }
        /// <summary>지금 할 일 (아직 안 한 첫 칸, 다 했으면 -1)</summary>
        static int Current { get { for (int i = 0; i < TaskKeys.Length; i++) if (!Has(i)) return i; return -1; } }

        /// <summary>온라인 이름표 표시 아이디 (가입했으면 "guild", 아니면 ""). NetClient가 enter에 싣는다</summary>
        public static string Badge => Step >= 3 ? BadgeId : "";
        /// <summary>초대장을 받고 아직 가입하기 전</summary>
        public static bool Active => Step == 1 || Step == 2;
        static bool AtBazaar => MapRoot.Current != null && MapRoot.Current.mapId == Bazaar;
        static bool InGame => GM != null && MapRoot.Current != null && PlayerController.Instance != null && !StartScreen.IsOpen && !NetLogin.IsOpen;
        static string PlayerName
        {
            get
            {
                var net = NetClient.I;
                return net != null && net.Status == NetClient.State.Online && !string.IsNullOrEmpty(net.MyName) ? net.MyName : "새내기 상인";
            }
        }

        /// <summary>이번 실행에서 칸마다 누구와 했는지</summary>
        static readonly string[] partners = new string[4];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { for (int i = 0; i < 4; i++) partners[i] = null; }

        enum Letter { None, Invite, Reply }
        enum Book { None, Stamp, View }
        enum Shot { None, Count, Snap, Show }   // Snap = 찰칵하는 순간 (11/11: 진짜 게임 화면을 찍는 동안 뷰파인더를 감춘다)

        static NetGuild inst;
        Transform yard, clerkTf, stallTf, boardTf, photoTf;
        readonly List<NetMeet.Npc> merchants = new List<NetMeet.Npc>();
        readonly Dictionary<Transform, (string text, float until)> bubbles = new Dictionary<Transform, (string, float)>();
        readonly Dictionary<Transform, float> ambientAt = new Dictionary<Transform, float>();
        readonly List<Transform> expired = new List<Transform>();
        /// <summary>깡총 뛰는 그림 (원래 높이 · 시작 시각)</summary>
        readonly List<(Transform body, float baseY, float start)> hops = new List<(Transform, float, float)>();
        const float HopSeconds = 1.1f;
        float readyAt = -1f, nextCheck, lastStamp = -99f;
        bool riding, wantBook;
        Letter letter, pendingLetter;
        float letterAt; int letterFrame;
        // 11/11 기우: 편지를 한 문단 = 한 장으로 넘겨 읽는다. 글자는 차례로 찍힌다 (Space: 찍는 중이면 한 번에 → 다음 장 → 마지막 장에서 출발/닫기)
        const float TypeSpeed = 32f;   // 1초에 찍히는 글자 수
        int letterPage; float pageAt;
        List<string>[] letterLines; float letterLinesW = -1f; Letter letterLinesOf;
        bool ceremony; float ceremonyAt; int ceremonyFrame;
        // 11/11 기우: 가입할 때 명부에 마우스로 서명 (Space: 서명했으면 완료, 안 했으면 이름으로 대신. Esc: 다음에). 서명은 수여식 상인패 아래에 나온다.
        //   그림은 이 컴퓨터 설정(PlayerPrefs "pc.guild.sign")에만 남는다. 서버로는 안 보낸다.
        const int SignW = 150, SignH = 44; const string SignPref = "pc.guild.sign";
        bool signing, signDown; float signAt; int signFrame, signInk;
        Texture2D signTex; Vector2 signLast;
        bool Signed => signTex != null && signInk >= 12;
        NetGuildArt.Bit[] confetti;
        // 찾아갈 상대 (다른 플레이어 아이디 또는 사람 자리) · 머리 위 높이
        Transform markTf; string markPeer; float markLift;
        // 수첩 도장판: 열린 모양 · 연 시각 · 도장 찍을 칸 · 칸마다 도장이 내려앉는 시각(-1 = 이번에 찍지 않음)
        Book book; float bookAt; int bookFrame;
        readonly List<int> pendingStamps = new List<int>();
        readonly float[] slamAt = { -1f, -1f, -1f, -1f };
        readonly bool[] slamPlayed = new bool[4];
        // 합동 공연: 내가 · 다른 사람이 리액션한 시각
        float myEmoteAt = -99f, duetHintAt = -99f;
        readonly Dictionary<string, float> peerEmoteAt = new Dictionary<string, float>();
        // 기념 사진
        Shot shot; float shotAt; int shotFrame, shotTick;
        readonly List<(Sprite sprite, string name)> cast = new List<(Sprite, string)>();
        string castLine;
        // 뷰파인더(흰 모서리 네 개) 자리: 화면 너비·높이에 대한 비율. 사진은 딱 이 안쪽만 찍힌다 (11/11 기우: 화면 통째가 아니라 뷰파인더만큼)
        const float FinderX = .22f, FinderY = .2f, FinderW = .56f, FinderH = .58f;
        Texture2D photoTex;   // 11/11 기우: 사진 = 찰칵한 순간의 게임 화면 그대로 (전에는 사람 그림을 한 줄로 세운 그림이었다). 못 찍었으면 null이고 옛 그림으로 보여 준다

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetGuild>() != null) return;
            var go = new GameObject("NetGuild");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetGuild>();
            go.AddComponent<WorldLayer>();
        }

        /// <summary>마당 위 그림(간판 · 말풍선 · 화살표)은 창보다 아래 층에 그린다</summary>
        class WorldLayer : MonoBehaviour { void OnGUI() { if (inst != null) inst.DrawWorld(); } }

        void Awake() => inst = this;

        void OnEnable()
        {
            GameEvents.MapEntered += OnMap;
            NetEmote.Sent += OnEmote;
            NetGift.Delivered += OnGift;
            NetClient.Received += OnReceived;
            if (!Navigator.Tutorials.Contains(NavHint)) Navigator.Tutorials.Add(NavHint);
        }

        void OnDisable()
        {
            GameEvents.MapEntered -= OnMap;
            NetEmote.Sent -= OnEmote;
            NetGift.Delivered -= OnGift;
            NetClient.Received -= OnReceived;
            Navigator.Tutorials.Remove(NavHint);
            UIState.PhotoMode = false;
        }

        // ---------------- 꾸러미 (진짜 짐) ----------------
        // 11/11 기우: 꾸러미를 배달하는데 소지품(I)에 아무것도 없으면 이상해서, 배달하는 동안 짐에 '맡은 물건' 하나로 들어 있게 했다 (무게 1).
        //   물건 데이터(.asset)는 만들지 않는다: 실행 중에만 있는 물건이고, 저장 파일에는 남지 않는다 (불러올 때 없는 물건으로 빠진다).
        //   그래서 짐을 이야기 단계에 맞춰 매번 다시 맞춘다 (SyncParcel): 배달 중이면 1개, 아니면 0개.
        //   맡은 물건(questItem)이라 장터에 못 팔고, 선물·서버 저장에도 끼지 않는다.
        const string ParcelId = "guild_parcel";
        static ItemData parcelItem;

        static ItemData ParcelItem
        {
            get
            {
                if (parcelItem == null)
                {
                    parcelItem = ScriptableObject.CreateInstance<ItemData>();
                    parcelItem.hideFlags = HideFlags.HideAndDontSave;
                    parcelItem.id = ParcelId; parcelItem.weight = 1f; parcelItem.basePrice = 0f; parcelItem.questItem = true;
                }
                parcelItem.name = parcelItem.displayName = Parcel;
                if (parcelItem.icon == null || parcelIconFor != OriginId) { parcelIconFor = OriginId; parcelItem.icon = ParcelIcon(); }
                return parcelItem;
            }
        }
        static string parcelIconFor;
        static string OriginId => GM != null && GM.Player != null && GM.Player.origin != null ? GM.Player.origin.id : "";

        /// <summary>꾸러미 그림: 고향 특산품(유리·인삼·호박·유향) 그림을 빌린다. 못 찾으면 비워 두고 창들이 자루 그림을 대신 쓴다</summary>
        static Sprite ParcelIcon()
        {
            string key = OriginId == "venice" ? "유리" : OriginId == "goryeo" ? "인삼" : OriginId == "viking" ? "호박" : OriginId == "arabia" ? "유향" : null;
            if (key == null || GM == null || GM.Db == null) return null;
            var like = GM.Db.items.Find(i => i != null && i.icon != null && !i.questItem && i.displayName != null && i.displayName.Contains(key));
            return like != null ? like.icon : null;
        }

        /// <summary>배달 중(초대장을 받고 출발 ~ 라일라에게 전하기 전)이면 짐에 꾸러미 1개, 아니면 0개</summary>
        void SyncParcel()
        {
            if (GM == null || GM.Player == null || GM.Player.cargo == null || !InGame) return;
            bool carry = Step == 1 && GM.GetMemo(WelcomeKey) < 1f && letter != Letter.Invite && pendingLetter != Letter.Invite;
            var item = ParcelItem; int n = GM.Player.cargo.Count(item);
            if (carry && n < 1) GM.Player.cargo.Add(item, 1, 0f);
            else if (!carry && n > 0) GM.Player.cargo.Remove(item, n);
        }

        /// <summary>
        /// 11/10 길 안내 (퀘스트와 같은 화살표 + 아래 "목표" 줄): 장터 밖이면 만국 장터로 가는 출구, 장터 안이면 지금 찾아갈 곳.
        /// 상인회 이야기 동안 장터 안에서는 퀘스트 화살표가 끼어들지 않게 "이미 도착한 맵"으로 둔다
        /// </summary>
        Navigator.Hint? NavHint()
        {
            int s = Step;
            if ((s != 1 && s != 2) || MapRoot.Current == null) return null;
            if (QuestFocus.Current != QuestFocus.Kind.Event) return null;   // 11/11 은샘: 수첩에서 다른 것을 길 안내 중이면 비킨다
            if (!AtBazaar) return new Navigator.Hint { kind = "map", id = Bazaar, label = "만국 장터" };
            Interactable it = markTf != null ? markTf.GetComponent<Interactable>() : markPeer != null ? RemotePlayers.TargetOf(markPeer) : null;
            if (it != null && it.isActiveAndEnabled) return new Navigator.Hint { target = it, label = markPeer != null ? PeerName(markPeer) + " 님" : null };
            return new Navigator.Hint { kind = "map", id = Bazaar };
        }

        void Start() { if (AtBazaar) OnMap(Bazaar); }

        bool AnyWindow => letter != Letter.None || ceremony || signing || book != Book.None || shot != Shot.None;

        // ---------------- 흐름 ----------------

        /// <summary>네 지역 중 한 곳이라도 첫걸음 안내를 끝까지 마쳤다 (각 지역 저장의 intro 11)</summary>
        static bool IntroDone()
        {
            if (GM == null) return false;
            return NorthSave.Get("intro") >= 11f || VeniceSave.Get("intro") >= 11f
                || GM.GetMemo("goryeo:intro") >= 11f || GM.GetMemo("arabia:intro") >= 11f;
        }

        static bool IntroActive => NorthIntro.Active || VeniceIntro.Active || GoryeoIntro.Active || ArabiaIntro.Active;

        void Update()
        {
            AnimateHops();
            HandleWindows();
            TickPhoto();
            UIState.PhotoMode = shot == Shot.Count || shot == Shot.Snap;   // 사진 찍는 동안 이름표·화살표 감추기 (WorldLabels · ActivityHints)
            SyncParcel();
            float now = Time.unscaledTime;
            bool calm = InGame && !UIState.Blocking && !UIState.JustClosed && !GM.IsTravelling && !AnyWindow;
            // 도장판: 도장 받을 게 있거나(라일라 안내 뒤 · N · 위 알약) 열어 달라고 했으면
            if (calm && Active && (pendingStamps.Count > 0 || wantBook)) { OpenBook(pendingStamps.Count > 0 ? Book.Stamp : Book.View); wantBook = false; }
            else if (calm && Active && NotebookKey()) OpenBook(Book.View);

            if (now < nextCheck) return;
            nextCheck = now + 0.2f;
            expired.Clear();
            foreach (var kv in bubbles) if (kv.Key == null || now > kv.Value.until) expired.Add(kv.Key);
            foreach (var t in expired) bubbles.Remove(t);
            FindMark();
            Ambient(now);

            calm = InGame && !UIState.Blocking && !GM.IsTravelling && !AnyWindow;
            if (!calm) { if (Step == 0) readyAt = -1f; return; }
            if (pendingLetter != Letter.None) { var l = pendingLetter; pendingLetter = Letter.None; OpenLetter(l); return; }
            int s = Step;
            if (s == 0)
            {
                if (!IntroDone() || IntroActive) { readyAt = -1f; return; }
                if (readyAt < 0f) readyAt = now + LetterDelay;   // 튜토리얼 마지막 말이 끝나고 잠깐 뒤에
                if (now >= readyAt) { Step = 1; QuestFocus.Set(QuestFocus.Kind.Event); OpenLetter(Letter.Invite); }   // 11/11 가장 최근에 받은 것을 길 안내
                return;
            }
            if (s == 1 && AtBazaar && GM.GetMemo(WelcomeKey) < 1f)
            {
                // 걸어서 도착: 상인회 마당의 라일라 가까이 오면 맞아 준다
                var pc = PlayerController.Instance;
                if (clerkTf != null && pc != null && ((Vector2)clerkTf.position - (Vector2)pc.transform.position).sqrMagnitude < 4.5f * 4.5f) Welcome();
                return;
            }
            if (s == 1 && DoneCount >= TaskKeys.Length && pendingStamps.Count == 0 && now - lastStamp > ReplyDelay) { Step = 2; QuestFocus.Set(QuestFocus.Kind.Event); Sfx.Play("levelup", .6f); OpenLetter(Letter.Reply); }
        }

        static bool NotebookKey()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.nKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.N);
#endif
        }

        /// <summary>초대장 [만국 장터로 출발!]: 걸어서 간다. 길은 길 안내 화살표와 아래 목표 줄이 알려 준다</summary>
        static void Depart()
        {
            if (inst != null) inst.CloseLetter();
            Sfx.Play("accept");
            Notify.Event(AtBazaar ? $"{Parcel}를 받았다! 남쪽 상인회 안뜰의 라일라에게 전해요"
                : $"{Parcel}를 받았다! 만국 장터의 라일라에게 배달해요 · 화살표와 아래 '목표' 줄을 따라가요", 5f);
        }

        /// <summary>시험용 (채팅 "/상인회 마차", 개발 키): 만국 장터 동쪽 어귀로 바로 간다 (상인회 광장까지는 야시장 길로 조금 걷는다)</summary>
        static void Ride()
        {
            if (GM == null || inst == null || AtBazaar || GM.IsTravelling) return;
            inst.CloseLetter();
            inst.CloseBook();
            inst.riding = true;
            GM.TravelTo(Bazaar, ArriveSpawn, 0f, Vector2Int.left);
        }

        /// <summary>라일라 첫인사 = 꾸러미 배달 끝 (배달 삯) → 수첩 설명 → 수첩 펼치기</summary>
        void Welcome()
        {
            GM.SetMemo(WelcomeKey, 1f);
            GM.Player.money += DeliveryPay;
            Sfx.Play("coin");
            Notify.Event($"배달 완료! 배달 삯 {DeliveryPay}냥을 받았다", 3.5f);
            Say(clerkTf, "어서 와요!");
            Hop(clerkTf);
            DialogueUI.Show(Clerk,
                $"어서 와요, {PlayerName} 님! 만국 상인회 접수원 라일라예요.\n" +
                $"어머, {Parcel}! 먼 길 배달해 줘서 고마워요. 여기 배달 삯 {DeliveryPay}냥이에요.\n\n" +
                "상인회 패를 받으려면 이 수첩에 도장 네 개를 모아 와요. 함께 춤추고, 선물을 주고받고, 놀이판에서 짐을 쌓고, 다 같이 사진도 찍는 거예요.\n\n" +
                "찾아갈 곳은 노란 화살표가, 수첩은 N 키가 보여 줘요. 혼자 왔어도 괜찮아요. 길보, 비요른, 파리드가 같이 놀아 줄 거예요!",
                new DialogueUI.Choice("수첩 볼게요!", () => { Say(clerkTf, "화이팅!"); wantBook = true; }));
        }

        // ---------------- 수첩 ----------------

        /// <summary>수첩 한 칸에 도장을 찍는다 (만국 장터에서, 초대장을 받은 뒤에만). partner = 누구와 했는지 (짧게). 도장판은 창이 다 닫히면 펼쳐진다</summary>
        public static void Mark(Task t, string partner = null)
        {
            int i = (int)t;
            if (Step != 1 || !AtBazaar || GM == null || Has(i)) return;
            GM.SetMemo(TaskKeys[i], 1f);
            partners[i] = partner;
            if (inst != null) { inst.lastStamp = Time.unscaledTime; if (!inst.pendingStamps.Contains(i)) inst.pendingStamps.Add(i); }
        }

        void OnReceived(NetClient.Msg m)
        {
            // 합동 공연: 다른 사람이 리액션했다 (내 것은 OnEmote)
            var net = NetClient.I;
            if (m.t != "emote" || net == null || string.IsNullOrEmpty(m.id) || m.id == net.MyId) return;
            float now = Time.unscaledTime;
            peerEmoteAt[m.id] = now;
            if (Step != 1 || !AtBazaar || Has((int)Task.Duet) || !PeerNear(m.id, DuetRange)) return;
            string who = PeerName(m.id);
            if (now - myEmoteAt <= DuetWindow) { Duet(who + " 님"); return; }
            if (now - duetHintAt > 8f)
            {
                duetHintAt = now;
                Notify.Show($"{who} 님이 {(string.IsNullOrEmpty(m.name) ? "리액션" : m.name)}! 6초 안에 같이 리액션하면 합동 공연", 3.5f);
            }
        }

        void OnEmote(string id)
        {
            float now = Time.unscaledTime;
            myEmoteAt = now;
            if (Step != 1 || !AtBazaar || Has((int)Task.Duet)) return;
            // 접속한 사람이 6초 안에 리액션했으면 합동 공연
            foreach (var kv in peerEmoteAt)
                if (now - kv.Value <= DuetWindow && PeerNear(kv.Key, DuetRange)) { Duet(PeerName(kv.Key) + " 님"); return; }
            // 혼자면 옆의 상인회 상인이 따라 춘다
            var pc = PlayerController.Instance;
            if (pc == null) return;
            NetMeet.Npc best = null; float bd = 3.5f * 3.5f;
            foreach (var n in merchants)
            {
                if (n.anchor == null) continue;
                float d = ((Vector2)n.anchor.position - (Vector2)pc.transform.position).sqrMagnitude;
                if (d <= bd) { bd = d; best = n; }
            }
            if (best == null) return;
            Hop(best.anchor);
            Say(best.anchor, "♪ 나도 같이!");
            Duet(best.shortName);
        }

        void Duet(string who)
        {
            Sfx.Play("gold", .6f);
            Mark(Task.Duet, who);
        }

        static bool PeerNear(string id, float range)
        {
            var pc = PlayerController.Instance;
            return pc != null && RemotePlayers.TryGetPosition(id, out var pos) && ((Vector2)pos - (Vector2)pc.transform.position).sqrMagnitude <= range * range;
        }

        static string PeerName(string id)
        {
            var net = NetClient.I;
            return net != null && net.Peers.TryGetValue(id, out var p) && !string.IsNullOrEmpty(p.name) ? p.name : "접속한 상인";
        }

        void OnGift(string to) => Mark(Task.Gift, to + " 님");

        // ---------------- 수첩 도장판 (가운데에 펼쳐진다) ----------------

        void OpenBook(Book mode)
        {
            if (AnyWindow || !UIState.TryOpen(this, CloseBook)) return;
            book = mode; bookAt = Time.unscaledTime; bookFrame = Time.frameCount;
            for (int i = 0; i < 4; i++) { slamAt[i] = -1f; slamPlayed[i] = false; }
            if (mode == Book.Stamp)
                for (int k = 0; k < pendingStamps.Count; k++) slamAt[pendingStamps[k]] = bookAt + .45f + k * .4f;
            pendingStamps.Clear();
            Sfx.Play("open", .7f);
        }

        void CloseBook()
        {
            if (book == Book.None) return;
            var was = book;
            book = Book.None;
            UIState.Release(this);
            if (was != Book.Stamp) return;
            lastStamp = Time.unscaledTime;
            int cur = Current;
            if (cur >= 0) Notify.Event($"다음: {TaskTitle[cur]} · 노란 화살표를 따라가요", 3f);
        }

        float LastSlam { get { float t = -1f; foreach (var s in slamAt) t = Mathf.Max(t, s); return t; } }

        // ---------------- 상인회 마당 (만국 장터 남쪽 가운데 상인회 광장, 11/11) ----------------

        class Person : Interactable
        {
            public System.Func<string> label; public System.Action act; public string verb;
            // 11/11 기우: 말풍선이 떠 있는 동안에는 그 사람 이름표를 감춘다 (말풍선이 자기 이름표를 덮어 글자가 겹쳤다)
            public override string DisplayName => label == null || (inst != null && inst.Speaking(transform)) ? "" : label();
            public override string ActionVerb => verb;
            public override Kind Category => Kind.Person;
            public override void Interact(PlayerController player) => act?.Invoke();
        }

        /// <summary>상인회 노점: 제다 공유 장터 시세로 사고판다 (TownMarket 그대로, 이름만)</summary>
        class Stall : TownMarket
        {
            public override string DisplayName => "상인회 노점";
        }

        void OnMap(string map)
        {
            if (yard != null) Destroy(yard.gameObject);
            yard = clerkTf = stallTf = boardTf = photoTf = null;
            if (shot == Shot.Count || shot == Shot.Snap) shot = Shot.None;   // 사진을 세다가 맵을 나감
            merchants.Clear(); bubbles.Clear(); ambientAt.Clear(); hops.Clear(); peerEmoteAt.Clear();
            markTf = null; markPeer = null;
            if (map != Bazaar) { riding = false; return; }
            BuildYard();
            riding = false;
            if (Step == 1 && GM != null && GM.GetMemo(WelcomeKey) < 1f)
                Notify.Event($"만국 장터에 도착했다! 남쪽 상인회 안뜰의 라일라에게 {Parcel}를 전해요", 5f);
        }

        void BuildYard()
        {
            var pc = PlayerController.Instance;
            if (pc == null || MapRoot.Current == null || GM == null) return;
            yard = new GameObject("만국 상인회 마당").transform;

            clerkTf = Spawn("arabia", 31, 44, () => Clerk + (Step == 0 || Step == 2 ? " !" : ""), TalkClerk, true, pc).transform;
            Booth(clerkTf.position, true);

            var town = GM.Db != null ? GM.Db.towns.Find(t => t != null && t.id == StallTown) : null;
            if (town != null)
            {
                var go = Body("venice", 40, 49, pc);
                go.AddComponent<Stall>().town = town;
                stallTf = go.transform;
                Booth(stallTf.position, false);
            }

            // 짐 쌓기 놀이판 (안뜰 서쪽 아래, 춤 양탄자 밑)
            boardTf = Place(19, 49, "짐 쌓기 놀이판", "한 판 하기", NetStackGame.Open, true).transform;
            Deco("prop_carpet", boardTf.position, 0f, -0.35f, 4);
            Deco("prop_spiceSack", boardTf.position, -0.55f, 0.3f, 9);
            Deco("prop_pepperSack", boardTf.position, 0.55f, 0.3f, 9);
            Deco("prop_dates", boardTf.position, 0f, 1.05f, 10);

            // 기념 사진 자리 (11/11 기우: 안뜰 북동 귀퉁이 포토존. 아치 문·대추야자·장미·양탄자 석 장은 맵에 그려져 있다 — arabia_central.py와 같이 고칠 것)
            //   옛 자리 40,43 → 41,41 → 41,38. 여기서 깔던 양탄자·등불 둘은 맵 그림과 겹쳐서 뺐다
            photoTf = Place(41, 38, "기념 사진 자리", "사진 찍기", TakePhoto, false).transform;
            Deco("bunting", photoTf.position, -1f, 2.1f, 21);
            Deco("bunting", photoTf.position, 1f, 2.1f, 21);

            // 11/11 은샘: 43,41 → 45,43 (화분과 한 칸 띄움). 사진 자리에서 자리를 고를 때 길보 말풍선이 사진 자리를 거의 다 덮었다 (팀 시연)
            Merchant("goryeo", 45, 43, new NetMeet.Npc
            {
                name = "고려 보부상 길보", shortName = "길보",
                hello = "어이쿠, 반갑소! 개경에서 등짐 지고 왔다오.",
                cheer = "허허, 흥이 나는구먼!",
                thanks = "이리 귀한 걸! 받은 정은 꼭 갚으리다.",
                near = "어이, 새 얼굴이구먼! 사진 자리에서 같이 한 장 찍겠소?",
            }, pc);
            Merchant("viking", 19, 36, new NetMeet.Npc
            {
                name = "북쪽 상인 비요른", shortName = "비요른",
                hello = "하하! 피오르에서 배 타고 왔지. 반갑다!",
                cheer = "좋아, 나도 발 구르기다!",
                thanks = "오, 고맙다! 오늘은 꿀술로 건배다!",
                near = "이 춤 양탄자에서 같이 춤 한판 어때? 리액션하면 나도 따라 한다!",
            }, pc);
            Merchant("arabia", 23, 47, new NetMeet.Npc
            {
                name = "대상 파리드", shortName = "파리드",
                hello = "평안을 빕니다, 친구여. 잘 왔어요.",
                cheer = "하하, 사막 바람보다 시원하군요!",
                thanks = "선물은 길동무를 만든다지요. 고마워요!",
                near = "옆 놀이판 짐 쌓기, 내 기록은 7단이라오!",
            }, pc);
        }

        /// <summary>접수처(guild)·노점 꾸밈: 좌판 지붕, 앞 양탄자, 접수처는 깃발과 등불까지 (아라비아 큰 장과 같은 자리 잡기)</summary>
        void Booth(Vector3 at, bool guild)
        {
            Deco("building_stall", at, 0f, 1.4f, 8);
            if (guild)
            {
                Deco("prop_carpet", at, -1.2f, -1.0f, 4);
                Deco("prop_carpet", at, 1.2f, -1.0f, 4);
                Deco("bunting", at, -1f, 2.6f, 21);
                Deco("bunting", at, 1f, 2.6f, 21);
                Deco("prop_lantern", at, -2.4f, 0.75f, 10);
                Deco("prop_lantern", at, 2.4f, 0.75f, 10);
            }
            else
            {
                Deco("prop_carpet", at, 0f, -1.0f, 4);
                Deco("prop_spiceSack", at, -2.2f, 0.75f, 9);
                Deco("prop_dates", at, 2.2f, 0.75f, 9);
            }
        }

        void Deco(string sprite, Vector3 at, float dx, float dy, int order)
        {
            var sp = Resources.Load<Sprite>("Arabia/Fair/" + sprite);
            if (sp == null) return;
            var go = new GameObject(sprite);
            go.transform.SetParent(yard, false);
            go.transform.position = at + new Vector3(dx, dy, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sp; r.sortingOrder = order;
        }

        void Merchant(string look, int x, int y, NetMeet.Npc npc, PlayerController pc)
        {
            var go = Spawn(look, x, y, () => npc.name, () => NetMeet.OpenNpc(npc), false, pc);
            Deco("prop_carpet", go.transform.position, 0f, -0.3f, 4);
            npc.anchor = go.transform;
            npc.react = _ => Hop(go.transform);
            npc.say = text => Say(go.transform, text);
            merchants.Add(npc);
        }

        GameObject Spawn(string look, int x, int y, System.Func<string> label, System.Action act, bool blocks, PlayerController pc)
        {
            var go = Body(look, x, y, pc);
            var p = go.AddComponent<Person>();
            p.blocksMovement = blocks;
            p.label = label;
            p.act = act;
            return go;
        }

        /// <summary>사람이 아닌 자리 (놀이판 · 사진 자리). 그림은 Deco로 따로 놓는다</summary>
        GameObject Place(int x, int y, string name, string verb, System.Action act, bool blocks)
        {
            var go = new GameObject(name);
            go.transform.SetParent(yard, false);
            go.transform.position = MapRoot.Current.CellCenter(new Vector3Int(x, BazaarHeight - 1 - y, 0));
            var p = go.AddComponent<Person>();
            p.blocksMovement = blocks;
            p.label = () => name;
            p.verb = verb;
            p.act = act;
            return go;
        }

        /// <summary>맵 JSON 칸(위가 0)에 사람 하나를 세운다. 그림은 그 출신의 플레이어 캐릭터 (아래를 보고 서 있다)</summary>
        GameObject Body(string look, int x, int y, PlayerController pc)
        {
            var root = new GameObject(look + " " + x + "," + y);
            root.transform.SetParent(yard, false);
            root.transform.position = MapRoot.Current.CellCenter(new Vector3Int(x, BazaarHeight - 1 - y, 0));
            var body = new GameObject("그림");
            body.transform.SetParent(root.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            var anim = body.AddComponent<CharacterAnimator>();
            var mine = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            if (mine != null)
            {
                body.transform.localPosition = pc.anim.transform.localPosition;
                sr.sortingLayerID = mine.sortingLayerID;
                sr.sortingOrder = mine.sortingOrder;
                sr.sharedMaterial = mine.sharedMaterial;
                sr.spriteSortPoint = mine.spriteSortPoint;
            }
            var origin = StartChoices.FindOrigin(look);
            anim.frames = PlayerAppearance.HasFrames(origin) ? (Sprite[])origin.playerFrames.Clone()
                : pc.anim != null && pc.anim.frames != null ? (Sprite[])pc.anim.frames.Clone() : new Sprite[16];
            anim.facing = Vector2Int.down;
            anim.walking = false;
            if (anim.frames.Length > 0) sr.sprite = anim.frames[0];
            return root;
        }

        bool Speaking(Transform who) => who != null && bubbles.TryGetValue(who, out var b) && Time.unscaledTime < b.until;
        bool AnyoneSpeaking() { float now = Time.unscaledTime; foreach (var kv in bubbles) if (kv.Key != null && now < kv.Value.until) return true; return false; }

        void Say(Transform who, string text)
        {
            if (who == null || string.IsNullOrEmpty(text)) return;
            bubbles[who] = (text, Time.unscaledTime + Mathf.Clamp(2.5f + text.Length * .07f, 2.5f, 6f));
        }

        void Hop(Transform root)
        {
            if (root == null || root.childCount == 0) return;
            var body = root.GetChild(0);
            int i = hops.FindIndex(h => h.body == body);
            float baseY = i >= 0 ? hops[i].baseY : body.localPosition.y;
            if (i >= 0) hops.RemoveAt(i);
            hops.Add((body, baseY, Time.unscaledTime));
        }

        /// <summary>깡총: 두 번 (RemotePlayers 깡총과 같은 높이, 16분의 1칸 단위)</summary>
        void AnimateHops()
        {
            for (int i = hops.Count - 1; i >= 0; i--)
            {
                var (body, baseY, start) = hops[i];
                if (body == null) { hops.RemoveAt(i); continue; }
                float k = (Time.unscaledTime - start) / HopSeconds;
                var pos = body.localPosition;
                if (k >= 1f) { body.localPosition = new Vector3(pos.x, baseY, pos.z); hops.RemoveAt(i); continue; }
                float lift = Mathf.Sin(Mathf.Repeat(k * 2f, 1f) * Mathf.PI) * .38f;
                body.localPosition = new Vector3(pos.x, baseY + Mathf.Round(lift * 16f) / 16f, pos.z);
            }
        }

        /// <summary>가까이 가면 상인들이 먼저 말을 건다 (사람마다 40초에 한 번)</summary>
        void Ambient(float now)
        {
            var pc = PlayerController.Instance;
            if (pc == null || yard == null || UIState.Blocking) return;
            Vector2 me = pc.transform.position;
            foreach (var n in merchants) Near(n.anchor, n.near, me, now);
            int s = Step;
            Near(clerkTf, s == 0 ? "상인회에 들고 싶다면 이리 와요!" : s == 1 && GM.GetMemo(WelcomeKey) < 1f ? "초대장 받고 왔군요? 이리 와요!" : s == 1 ? "수첩은 잘 채우고 있어요? N으로 볼 수 있어요" : s == 2 ? "다 채웠군요! 이리 와요!" : "어서 와요, 상인회 동료!", me, now);
        }

        void Near(Transform t, string line, Vector2 me, float now)
        {
            if (t == null || ((Vector2)t.position - me).sqrMagnitude > 3.5f * 3.5f) return;
            if (ambientAt.TryGetValue(t, out var at) && now - at < 40f) return;
            if (AnyoneSpeaking()) return;   // 11/11 먼저 말 거는 건 한 번에 한 사람만 (말풍선 둘이 겹쳐 떴다). 차례는 다음에 온다
            ambientAt[t] = now;
            Say(t, line);
        }

        void TalkClerk()
        {
            int s = Step;
            Hop(clerkTf);
            if (s == 0)
            {
                DialogueUI.Show(Clerk,
                    "어서 와요! 여기는 만국 상인회 마당이에요. 상인회에 들고 싶다면 이 수첩에 도장 네 개를 모아 와요. 장터 사람들과 어울려 보는 게 첫 장사랍니다.",
                    new DialogueUI.Choice("수첩 받기", () =>
                    {
                        Step = 1; GM.SetMemo(WelcomeKey, 1f);
                        Sfx.Play("accept");
                        Say(clerkTf, "화이팅!");
                        wantBook = true;
                    }),
                    new DialogueUI.Choice("다음에요", null));
                return;
            }
            if (s == 1 && GM.GetMemo(WelcomeKey) < 1f) { Welcome(); return; }
            if (s == 1)
            {
                int cur = Current;
                DialogueUI.Show(Clerk, cur < 0 ? "수첩을 다 채웠군요! 잠깐만요, 상인회장님 답장이 곧 올 거예요."
                    : $"도장이 {DoneCount}/{TaskKeys.Length}개 모였네요. 다음은 '{TaskTitle[cur]}'예요.\n\n{TaskHow[cur]}\n\n" +
                      "노란 화살표를 따라가요. 수첩은 N으로 언제든 볼 수 있어요.");
                return;
            }
            if (s == 2)
            {
                DialogueUI.Show(Clerk,
                    "수첩 잘 봤어요! 같이 웃고, 나누고, 겨루고, 추억을 남기는 것. 그게 만국 장터의 법이에요.\n\n자, 여기 이름을 적어요.",
                    new DialogueUI.Choice("이름 올리기", OpenSign));
                return;
            }
            DialogueUI.Show(Clerk, "상인패가 잘 어울려요! 바다 건너 어느 장터에서든 그 패를 보면 다들 반길 거예요. 좋은 장사길 되세요!");
        }

        // ---------------- 화살표 (찾아갈 상대) ----------------

        void FindMark()
        {
            markTf = null; markPeer = null;
            var pc = PlayerController.Instance;
            if (!AtBazaar || yard == null || pc == null) return;
            int s = Step;
            markLift = 1.55f;
            if (s == 2 || (s == 1 && GM.GetMemo(WelcomeKey) < 1f)) { markTf = clerkTf; return; }
            if (s != 1) return;
            int cur = Current;
            if (cur < 0) return;
            if (cur == (int)Task.Stack) { markTf = boardTf; markLift = 2.1f; return; }
            if (cur == (int)Task.Photo) { markTf = photoTf; markLift = 1.3f; return; }
            // 합동 공연·선물: 가까운 다른 플레이어가 먼저, 없으면 가까운 상인회 상인
            Vector2 me = pc.transform.position;
            float best = MarkRange * MarkRange;
            var net = NetClient.I;
            if (net != null && net.Status == NetClient.State.Online)
                foreach (var p in net.Peers.Values)
                {
                    if (!RemotePlayers.TryGetPosition(p.id, out var pos)) continue;
                    float d = ((Vector2)pos - me).sqrMagnitude;
                    if (d < best) { best = d; markPeer = p.id; }
                }
            if (markPeer != null) { markLift = 2.05f; return; }
            best = float.MaxValue;
            foreach (var n in merchants)
            {
                if (n.anchor == null) continue;
                float d = ((Vector2)n.anchor.position - me).sqrMagnitude;
                if (d < best) { best = d; markTf = n.anchor; }
            }
        }

        // ---------------- 기념 사진 ----------------

        void TakePhoto()
        {
            if (Step == 0) { Notify.Show("상인회 수첩을 받은 뒤에 찍어요 · 접수원 라일라에게 말을 걸어 보세요", 3f); return; }
            // 11/11 기우: 셋 세는 동안에도 움직일 수 있다 (자리 잡고 리액션하며 찍게). 창은 찍힌 사진을 보여 줄 때만 잡는다
            if (AnyWindow || UIState.Blocking) return;
            shot = Shot.Count; shotAt = Time.unscaledTime; shotFrame = Time.frameCount; shotTick = 4;
            Sfx.Play("open", .5f);
            // 같은 맵에 접속한 사람이 있으면 채팅 말풍선으로 부른다 (셋 셀 동안 모이면 같이 찍힌다)
            var net = NetClient.I;
            if (net != null && net.Status == NetClient.State.Online && net.Peers.Count > 0) net.Say("기념 사진 찍어요! 사진 자리로 모여요~");
        }

        void TickPhoto()
        {
            if (shot != Shot.Count) return;
            // 세는 동안 움직일 수 있으니, 사진 자리에서 멀리 가 버리면 그만둔다
            var pc = PlayerController.Instance;
            if (photoTf == null || pc == null || ((Vector2)pc.transform.position - (Vector2)photoTf.position).sqrMagnitude > 7f * 7f)
            { shot = Shot.None; Notify.Show("사진 자리에서 멀어져서 그만 찍었어요", 2.5f); return; }
            float t = Time.unscaledTime - shotAt;
            int n = 3 - Mathf.FloorToInt(t / (CountSeconds / 3f));
            if (n != shotTick && n >= 1) { shotTick = n; Sfx.Play("step", .8f); }
            if (t >= CountSeconds) Snap();
        }

        void Snap()
        {
            if (UIState.Blocking) { shot = Shot.None; Notify.Show("창을 닫고 다시 찍어요", 2.5f); return; }   // 세는 동안 다른 창(소지품·지도)을 열었으면
            shot = Shot.Snap;
            StartCoroutine(SnapRoutine());
        }

        /// <summary>찰칵: 다 같이 기쁨 리액션 → 잠깐 뒤 그 순간의 게임 화면을 그대로 찍는다 → 사진을 보여 준다</summary>
        System.Collections.IEnumerator SnapRoutine()
        {
            NetEmote.Send("joy", "기쁨");
            RemotePlayers.PlaySelf("joy", "기쁨");
            var pc = PlayerController.Instance;
            if (pc != null)
                foreach (var n in merchants)
                    if (n.anchor != null && Vector2.Distance(n.anchor.position, pc.transform.position) < PhotoRange + 1f) Hop(n.anchor);
            yield return new WaitForSecondsRealtime(.3f);   // 리액션·깡총이 화면에 보일 때
            yield return new WaitForEndOfFrame();
            if (shot != Shot.Snap) yield break;   // 그 사이 맵이 바뀌었거나 취소됨
            if (photoTex != null) { Destroy(photoTex); photoTex = null; }
            try
            {
                var raw = ScreenCapture.CaptureScreenshotAsTexture();
                if (raw != null)
                {
                    // PNG로 한 번 거쳐야 색이 화면과 같게 나온다 (Social 사진과 같은 방법). Photos 폴더에도 남긴다
                    // 뷰파인더 안쪽만 잘라 낸다 (그림은 아래가 0이라 위아래를 뒤집어 센다)
                    int cx = Mathf.Clamp(Mathf.RoundToInt(raw.width * FinderX), 0, raw.width - 1), cw = Mathf.Clamp(Mathf.RoundToInt(raw.width * FinderW), 1, raw.width - cx);
                    int cy = Mathf.Clamp(Mathf.RoundToInt(raw.height * (1f - FinderY - FinderH)), 0, raw.height - 1), ch = Mathf.Clamp(Mathf.RoundToInt(raw.height * FinderH), 1, raw.height - cy);
                    var cut = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
                    cut.SetPixels(raw.GetPixels(cx, cy, cw, ch)); cut.Apply();
                    byte[] png = cut.EncodeToPNG();
                    Destroy(cut);
                    Destroy(raw);
                    photoTex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    photoTex.LoadImage(png);
                    string dir = System.IO.Path.Combine(Application.persistentDataPath, "Photos");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"guild_{System.DateTime.Now:yyyyMMdd_HHmmss}.png"), png);
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[상인회] 기념 사진을 찍지 못했습니다: " + e.Message); }
            FinishSnap();
        }

        void FinishSnap()
        {
            var names = GatherCast(out int players);
            UIState.TryOpen(this, ClosePhoto);   // 사진을 보는 동안은 멈춘다
            shot = Shot.Show; shotAt = Time.unscaledTime; shotFrame = Time.frameCount;
            Sfx.Play("gold");
            string partner = names.Count == 0 ? "혼자 찰칵" : names.Count == 1 ? names[0] : $"{names[0]} 외 {names.Count - 1}명";
            castLine = names.Count == 0 ? "혼자서 찰칵! 다음엔 친구와 함께" : "함께: " + string.Join(", ", names);
            Mark(Task.Photo, partner);
            var net = NetClient.I;
            if (players > 0 && net != null && net.Status == NetClient.State.Online) net.Say("찰칵! 만국 장터에서 다 같이 기념 사진!");
        }

        void ClosePhoto()
        {
            if (shot == Shot.None) return;
            shot = Shot.None;
            UIState.Release(this);
        }

        /// <summary>사진에 찍히는 사람: 나 + 옆의 접속한 사람 + 상인회 사람 + 장터 사람 (나를 가운데, 최대 6명). 나 말고 찍힌 사람 이름을 돌려준다</summary>
        List<string> GatherCast(out int players)
        {
            cast.Clear(); players = 0;
            var names = new List<string>();
            var pc = PlayerController.Instance;
            if (pc == null) return names;
            Vector2 at = pc.transform.position;
            var others = new List<(Sprite sprite, string name, float x)>();
            var near = new List<(Sprite sprite, string name)>();
            RemotePlayers.Collect(at, PhotoRange, near);
            foreach (var p in near) { others.Add((p.sprite, p.name + " 님", 0f)); players++; }
            void Add(Transform t, string name, bool always)
            {
                if (t == null || others.Count >= MaxCast - 1 || (!always && ((Vector2)t.position - at).sqrMagnitude > PhotoRange * PhotoRange)) return;
                var sp = Front(t);
                if (sp != null) others.Add((sp, name, t.position.x));
            }
            // 11/11 기우: 진짜 화면을 찍으니, 상인회 사람도 가까이 있는 사람만 "함께"에 적는다 (전에는 다 달려와 찍히는 걸로 쳤다)
            foreach (var n in merchants) Add(n.anchor, n.shortName, false);
            Add(clerkTf, "라일라", false);
            // 장터 사람 (아라비아 큰 장 이야기꾼 등, 사람 모습이 있는 것만)
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || it is RemotePlayers.Target || (yard != null && it.transform.IsChildOf(yard))) continue;
                if (it.Category != Interactable.Kind.Person && it.Category != Interactable.Kind.Quest) continue;
                string nm = WorldDisplayNames.ShortName(it.DisplayName);
                if (string.IsNullOrEmpty(nm)) continue;
                Add(it.transform, nm, false);
            }
            if (others.Count > MaxCast - 1) others.RemoveRange(MaxCast - 1, others.Count - (MaxCast - 1));
            others.Sort((a, b) => a.x.CompareTo(b.x));
            var me = pc.anim != null && pc.anim.frames != null && pc.anim.frames.Length > 0 && pc.anim.frames[0] != null ? pc.anim.frames[0] : Front(pc.transform);
            int mid = others.Count / 2;
            for (int i = 0; i <= others.Count; i++)
            {
                if (i == mid && me != null) cast.Add((me, PlayerName));
                if (i < others.Count) { cast.Add((others[i].sprite, others[i].name)); names.Add(others[i].name); }
            }
            return names;
        }

        /// <summary>사진 속 이름표: 접속한 사람은 "○○ 님" 그대로, 긴 직함은 맨 끝 이름만 ("불 재주꾼 자이드" → "자이드")</summary>
        static string PhotoName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.EndsWith(" 님")) return name;
            int sp = name.LastIndexOf(' ');
            return sp >= 0 && sp < name.Length - 1 ? name.Substring(sp + 1) : name;
        }

        /// <summary>정면 서 있는 그림 (걷는 그림 첫 장, 없으면 지금 그림)</summary>
        static Sprite Front(Transform t)
        {
            var an = t.GetComponentInChildren<CharacterAnimator>();
            if (an != null && an.frames != null && an.frames.Length > 0 && an.frames[0] != null) return an.frames[0];
            var sr = t.GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }

        // ---------------- 창: 편지 · 수여식 ----------------

        void OpenLetter(Letter kind)
        {
            if (AnyWindow || !UIState.TryOpen(this, CloseLetter)) { pendingLetter = kind; return; }
            letter = kind; letterAt = Time.unscaledTime; letterFrame = Time.frameCount;
            letterPage = 0; pageAt = letterAt + .35f;   // 창이 다 펼쳐진 뒤부터 찍힌다
            letterLines = null;
            Sfx.Play("open");
        }

        /// <summary>편지 한 장을 줄로 나눈다. 글자가 찍히는 동안 줄이 다시 꺾여 흔들리지 않게 미리 나눠 둔다 (한글이라 글자 단위로 꺾는다)</summary>
        static List<string> LetterWrap(string s, float w)
        {
            var lines = new List<string>(); int start = 0;
            for (int i = 1; i <= s.Length; i++)
            {
                bool end = i == s.Length;
                if (!end && (".,!?".IndexOf(s[i]) >= 0 || CuteUI.Width(s.Substring(start, i - start + 1)) <= w)) continue;   // 문장 부호는 줄 끝에 붙여 둔다
                lines.Add(s.Substring(start, i - start).Trim(' '));
                start = i;
            }
            return lines;
        }

        List<string>[] LetterLines(float w)
        {
            if (letterLines != null && letterLinesOf == letter && Mathf.Approximately(letterLinesW, w)) return letterLines;
            var paras = letter == Letter.Invite ? InvitePara : ReplyPara;
            letterLines = new List<string>[paras.Length];
            for (int i = 0; i < paras.Length; i++) letterLines[i] = LetterWrap(paras[i], w);
            letterLinesOf = letter; letterLinesW = w;
            return letterLines;
        }

        static int LetterLength(List<string> lines) { int n = 0; foreach (var l in lines) n += l.Length; return n; }
        int LetterShown(List<string> lines) => Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - pageAt) * TypeSpeed), 0, LetterLength(lines));

        /// <summary>편지에서 Space·버튼: 찍는 중이면 한 번에 다 보이고, 다 보였으면 다음 장, 마지막 장이면 출발(초대장)·닫기(답장)</summary>
        void LetterNext()
        {
            if (letter == Letter.None) return;
            var pages = letterLines;
            if (pages == null || pages.Length == 0) { if (letter == Letter.Invite) Depart(); else CloseLetter(); return; }   // 아직 한 번도 안 그려졌으면 예전처럼
            int page = Mathf.Clamp(letterPage, 0, pages.Length - 1);
            if (LetterShown(pages[page]) < LetterLength(pages[page])) { pageAt = Time.unscaledTime - 999f; return; }
            if (page < pages.Length - 1) { letterPage = page + 1; pageAt = Time.unscaledTime; Sfx.Play("move"); return; }
            if (letter == Letter.Invite) Depart(); else CloseLetter();
        }

        void CloseLetter()
        {
            if (letter == Letter.None) return;
            var was = letter;
            letter = Letter.None;
            UIState.Release(this);
            if (was == Letter.Reply)
            {
                Say(clerkTf, "이리 와요!");
                Notify.Event("상인회 마당의 라일라에게 가서 이름을 올리세요 · 노란 화살표를 따라가요", 4.5f);
            }
        }

        // ---------------- 명부 서명 (가입 직전) ----------------

        void OpenSign()
        {
            if (AnyWindow || !UIState.TryOpen(this, CancelSign)) { OpenCeremony(); return; }   // 창을 못 열면 서명 없이 예전처럼
            signing = true; signDown = false; signAt = Time.unscaledTime; signFrame = Time.frameCount;
            ClearSign();
            Sfx.Play("open");
        }

        void ClearSign()
        {
            if (signTex == null) signTex = new Texture2D(SignW, SignH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color32[SignW * SignH];
            signTex.SetPixels32(clear); signTex.Apply();
            signInk = 0;
        }

        /// <summary>Esc · 다른 창에 밀림: 가입하지 않고 닫는다 (라일라에게 다시 말 걸면 된다)</summary>
        void CancelSign()
        {
            if (!signing) return;
            signing = signDown = false;
            UIState.Release(this);
        }

        void ConfirmSign()
        {
            if (!signing) return;
            signing = signDown = false;
            UIState.Release(this);
            if (Signed)
            {
                try { PlayerPrefs.SetString(SignPref, System.Convert.ToBase64String(signTex.EncodeToPNG())); } catch (System.Exception) { }
                Sfx.Play("accept");
            }
            OpenCeremony();
        }

        /// <summary>서명 칸 안 (a → b) 한 획. 칸 좌표(왼쪽 위가 0,0), 굵기 2</summary>
        void SignStroke(Vector2 a, Vector2 b)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y))));
            for (int i = 0; i <= n; i++)
            {
                var q = Vector2.Lerp(a, b, i / (float)n);
                int px = Mathf.FloorToInt(q.x), py = Mathf.FloorToInt(q.y);
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                    {
                        int x = px + dx, y = SignH - 1 - (py + dy);
                        if (x < 0 || y < 0 || x >= SignW || y >= SignH) continue;
                        if (signTex.GetPixel(x, y).a < .5f) signInk++;
                        signTex.SetPixel(x, y, (Color)CuteUI.Ink);
                    }
            }
            signTex.Apply();
        }

        void DrawSign()
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, ease = Ease((Time.unscaledTime - signAt) / .3f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            CuteUI.Dim();
            float w = SignW + 40f, h = 22f + 16f + SignH + 8f + 22f + 22f + 6f;
            var r = new Rect(Mathf.Round((W - w) / 2f), Mathf.Round((H - h) / 2f + (1f - ease) * 14f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, "만국 상인회 명부");
            float x = r.x + 20f, y = r.y + 22f;
            CuteUI.Label(x, y + 6f, PlayerName + " 님, 여기에 서명해 주세요", CuteUI.Text, true);
            y += 16f;
            // 서명 칸: 흰 종이 + 밑줄
            var pad = new Rect(x, y, SignW, SignH);
            CuteUI.Box(new Rect(pad.x - 2f, pad.y - 2f, pad.width + 4f, pad.height + 4f), 4, CuteUI.White, CuteUI.Ink);
            CuteUI.Fill(new Rect(pad.x + 8f, pad.yMax - 9f, pad.width - 16f, 1f), CuteUI.Line);
            if (signTex != null && Event.current.type == EventType.Repaint) GUI.DrawTexture(pad, signTex);
            var e = Event.current;
            if (ease >= 1f && signTex != null)
            {
                var at = e.mousePosition - pad.position;
                if (e.type == EventType.MouseDown && e.button == 0 && pad.Contains(e.mousePosition)) { signDown = true; signLast = at; SignStroke(at, at); e.Use(); }
                else if (e.type == EventType.MouseDrag && signDown) { SignStroke(signLast, at); signLast = at; e.Use(); }
                else if (e.type == EventType.MouseUp) signDown = false;
            }
            y += SignH + 8f;
            float half = Mathf.Floor((SignW - 6f) / 2f);
            if (CuteUI.SoftButton(new Rect(x, y, half, 22f), "다시 쓰기")) ClearSign();
            if (CuteUI.Button(new Rect(x + half + 6f, y, SignW - half - 6f, 22f), Signed ? "서명 완료" : "이름으로 대신")) ConfirmSign();
            CuteUI.LabelCenter(r.center.x, y + 33f, Signed ? "Space 서명 완료 · Esc 다음에" : "마우스로 서명해요 · Space 이름으로 대신", CuteUI.Sub, true);
            GUI.color = old;
        }

        void OpenCeremony()
        {
            Step = 3;
            GM.Player.money += Reward;
            NetEmote.Send("joy", "기쁨");
            RemotePlayers.PlaySelf("joy", "기쁨");
            Hop(clerkTf);
            if (AnyWindow || !UIState.TryOpen(this, CloseCeremony)) { Finish(); return; }
            ceremony = true; ceremonyAt = Time.unscaledTime; ceremonyFrame = Time.frameCount;
            confetti = NetGuildArt.MakeConfetti(80);
            Sfx.Play("levelup");
        }

        void CloseCeremony()
        {
            if (!ceremony) return;
            ceremony = false;
            UIState.Release(this);
            Finish();
        }

        void Finish()
        {
            Sfx.Play("gold");
            Say(clerkTf, "좋은 장사길 되세요!");
            Notify.Event("만국 상인회 상인이 되었다! 이제 어디로든, 나만의 장사길로", 5f);
            NetChat.System("만국 상인회에 가입했어요 · 다른 사람 화면의 내 이름표에 '★ 상인회'가 붙어요");
        }

        void HandleWindows()
        {
            float now = Time.unscaledTime;
            bool ok = InputReader.InteractPressed(), no = InputReader.CancelPressed();
            if (letter != Letter.None && Time.frameCount > letterFrame && now - letterAt > .3f)
            {
                if (no) { if (letter == Letter.Invite) Depart(); else CloseLetter(); }   // Esc: 읽지 않고 바로 (전과 같다)
                else if (ok) LetterNext();
            }
            if (signing && Time.frameCount > signFrame && now - signAt > .3f)
            {
                if (no) CancelSign();
                else if (ok) { ConfirmSign(); return; }   // 같은 Space로 수여식까지 닫히지 않게
            }
            if (ceremony && Time.frameCount > ceremonyFrame && now - ceremonyAt > .8f && (ok || no))
                CloseCeremony();
            if (book != Book.None && Time.frameCount > bookFrame)
            {
                // 도장이 쾅 내려앉을 때 소리 · 다 찍고 잠시 뒤 저절로 닫힌다
                for (int i = 0; i < 4; i++)
                    if (slamAt[i] > 0f && !slamPlayed[i] && now >= slamAt[i]) { slamPlayed[i] = true; Sfx.Play("success"); }
                float last = LastSlam;
                if (book == Book.Stamp && last > 0f && now > last + 2.2f) CloseBook();
                else if (now - bookAt > .35f && (ok || no || NotebookKey())) CloseBook();
            }
            if ((shot == Shot.Count || shot == Shot.Snap) && no) ClosePhoto();
            if (shot == Shot.Show && Time.frameCount > shotFrame && now - shotAt > .6f && (ok || no)) ClosePhoto();
        }

        // ---------------- 그리기 ----------------

        static bool Showing => Active && InGame && !UIState.Blocking && !HUD.Expanded && !GM.IsTravelling;
        /// <summary>11/11 은샘: 위 알약은 이벤트를 길 안내 중일 때만 (안내 대상 하나만 위에)</summary>
        static bool PillShowing => Showing && QuestFocus.Current == QuestFocus.Kind.Event;

        void OnGUI()
        {
            if (!AnyWindow && !Showing) return;
            UIStyle.Begin();
            if (signing) { GUI.depth = -30; DrawSign(); return; }
            if (ceremony) { GUI.depth = -30; DrawCeremony(); return; }
            if (letter != Letter.None) { GUI.depth = -30; DrawLetter(); return; }
            if (shot != Shot.None) { GUI.depth = -30; DrawPhoto(); return; }
            if (book != Book.None) { GUI.depth = -30; DrawBook(); return; }
            GUI.depth = -4;
            if (!PillShowing) return;
            var pill = DrawPill();
            if (GUI.Button(pill, GUIContent.none, GUIStyle.none)) wantBook = true;   // 위 알약을 누르면 수첩
        }

        static float Ease(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t) * (1f - t); }

        void DrawLetter()
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, ease = Ease((Time.unscaledTime - letterAt) / .35f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            CuteUI.Dim();
            bool invite = letter == Letter.Invite;
            float w = Mathf.Min(330f, W - 30f), inner = w - 40f;
            var pages = LetterLines(inner);
            int page = Mathf.Clamp(letterPage, 0, pages.Length - 1); bool last = page == pages.Length - 1;
            float lineH = CuteUI.WrapHeight("가", inner), body = 0f;
            foreach (var pg in pages) body = Mathf.Max(body, pg.Count * lineH);   // 가장 긴 장에 맞춰 창 높이를 고정 (장을 넘겨도 창이 안 움직이게)
            float h = 26f + 18f + body + 6f + 22f + 26f + 14f + 8f;
            var r = new Rect(Mathf.Round((W - w) / 2f), Mathf.Round((H - h) / 2f + (1f - ease) * 18f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, invite ? "만국 상인회 초대장" : "만국 상인회 답장");
            float x = r.x + 20f, y = r.y + 22f;
            if (page == 0) CuteUI.Label(x, y + 7f, PlayerName + " 님께", CuteUI.Text, false, true);
            CuteUI.LabelRight(r.xMax - 20f, y + 7f, $"{page + 1} / {pages.Length}", CuteUI.Sub, true);
            y += 19f;
            // 글자가 차례로 찍힌다 (줄은 미리 나눠 둔 그대로)
            int shown = LetterShown(pages[page]), left = shown; bool typed = shown >= LetterLength(pages[page]);
            for (int i = 0; i < pages[page].Count && left > 0; i++)
            {
                string line = pages[page][i];
                CuteUI.Label(x, y + i * lineH + lineH / 2f, left >= line.Length ? line : line.Substring(0, left), CuteUI.Text);
                left -= line.Length;
            }
            y += body + 6f;
            // 서명 + 봉인 (마지막 장을 다 읽으면)
            if (last && typed)
            {
                float sigW = CuteUI.Width(Chief, true);
                CuteUI.LabelRight(r.xMax - 20f, y + 8f, Chief, CuteUI.Sub, true);
                NetGuildArt.Seal(r.xMax - 20f - sigW - 18f, y + 8f);
            }
            y += 24f;
            if (CuteUI.Button(new Rect(x, y, inner, 22f), !last ? "다음 장" : invite ? "꾸러미 들고 출발!" : "라일라에게 가기")) LetterNext();
            CuteUI.LabelCenter(r.center.x, y + 33f, !typed ? "Space 한 번에 보기" : !last ? "Space 다음 장" : invite ? "Space 출발 · 길 안내 화살표를 따라 걸어가요" : "Space 닫기", CuteUI.Sub, true);
            GUI.color = old;
        }

        void DrawCeremony()
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, now = Time.unscaledTime, age = now - ceremonyAt, ease = Ease(age / .4f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            CuteUI.Dim();
            bool signed = Signed;   // 11/11 명부에 서명했으면 이름 아래에 그 서명
            float w = Mathf.Min(300f, W - 30f), h = 244f + (signed ? SignH + 2f : 0f);
            var r = new Rect(Mathf.Round((W - w) / 2f), Mathf.Round((H - h) / 2f + 6f + (1f - ease) * 20f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, "만국 상인회 상인패");
            float cx = r.center.x, y = r.y + 20f;
            NetGuildArt.Medal(cx, y, now);
            y += 72f;
            CuteUI.LabelCenter(cx, y + 7f, PlayerName, CuteUI.Text, false, true);
            y += 16f;
            var origin = GM != null ? GM.Player.origin : null;
            CuteUI.LabelCenter(cx, y + 6f, (origin != null ? origin.displayName + " · " : "") + "만국 상인회 상인", CuteUI.Sub, true);
            y += 15f;
            if (signed)
            {
                if (Event.current.type == EventType.Repaint) GUI.DrawTexture(new Rect(Mathf.Round(cx - SignW / 2f), y, SignW, SignH), signTex);
                y += SignH + 2f;
            }
            CuteUI.Fill(new Rect(r.x + 24f, y, w - 48f, 1f), CuteUI.Line);
            y += 7f;
            // 받은 것: 돈 + 이름표 표시
            string money = $"+{Reward}냥";
            float cw = Mathf.Round(6f + 7f + 4f + CuteUI.Width(money, false, true) + 8f);
            var chip = new Rect(cx - 8f - cw, y, cw, 18f);
            CuteUI.Box(chip, 6, CuteUI.SellSoft, CuteUI.Sell);
            CuteUI.Icon(CuteUI.Pic.Coin7, chip.x + 6f, chip.y + 5f);
            CuteUI.Label(chip.x + 17f, chip.center.y, money, CuteUI.Sell, false, true);
            RemotePlayers.DrawBadge(cx + 32f, y + 14f);
            CuteUI.Label(cx + 58f, y + 9f, "이름표", CuteUI.Sub, true);
            y += 26f;
            CuteUI.WrapCenter(r.x + 18f, y, w - 36f, "이제 어느 장터에서든 이 패가 당신을 소개해요.\n세상의 장터로, 나만의 장사길을 떠나 볼까요?", CuteUI.Text);
            if (CuteUI.Button(new Rect(cx - 70f, r.yMax - 34f, 140f, 22f), "모험 떠나기!") && age > .8f) CloseCeremony();
            GUI.color = old;
            NetGuildArt.DrawConfetti(confetti, age);
        }

        /// <summary>위 가운데 알약: 지금 할 일 + 수첩 몇 개 (누르면 수첩). 그린 자리를 돌려준다</summary>
        /// <summary>11/11 은샘: 지금 할 일 한 줄과 덧붙임 (위 알약 · Q 수첩 이벤트 카드가 같이 쓴다)</summary>
        public static (string main, string where, bool pin) Summary()
        {
            int s = Step, cur = Current;
            if (s == 2) return ("라일라에게 가서 상인회 가입하기", "만국 장터 상인회 마당", true);
            if (!AtBazaar) return ($"{Parcel} 배달하기", "만국 장터 라일라 · 지도(M) 한가운데", true);
            if (GM.GetMemo(WelcomeKey) < 1f) return ("라일라에게 꾸러미 전하기", "만국 장터 남쪽 상인회 안뜰", true);
            return (cur >= 0 ? TaskTitle[cur] : "상인회 수첩 다 채움", $"수첩(N) {DoneCount}/{TaskKeys.Length}", false);
        }

        Rect DrawPill()
        {
            var (main, where, pin) = Summary();
            // 11/11 은샘: 이벤트 퀘스트는 금빛 알약 + "이벤트" 머리말로 다른 퀘스트와 구분
            CuteUI.EventStyle = true;
            var r = CuteUI.StackPill(12, false, CuteUI.Pic.OpenBook, "이벤트", main, where, false);
            CuteUI.EventStyle = false;
            return r;
        }

        /// <summary>
        /// 상인회 수첩 도장판 (11/10): 화면 가운데에 펼쳐지는 네 칸 도장판. 도장을 받을 때(Stamp)는 새 칸에 "완료" 도장이 쾅 내려앉고
        /// 2초쯤 뒤 저절로 닫힌다. N · 위 알약으로 열면(View) 칸마다 하는 법을 보여 주고, 장터 밖이면 가는 길을 한 줄 붙인다
        /// </summary>
        void DrawBook()
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, now = Time.unscaledTime, ease = Ease((now - bookAt) / .3f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            CuteUI.Dim();
            const float pad = 14f, gap = 8f, circle = 36f;
            float w = Mathf.Min(380f, W - 24f), slotW = Mathf.Floor((w - pad * 2f - gap) / 2f), textW = slotW - circle - 20f;
            float slotH = 58f;
            for (int i = 0; i < 4; i++) slotH = Mathf.Max(slotH, 9f + 15f + Mathf.Ceil(CuteUI.WrapHeight(SlotText(i), textW, true)) + 8f);
            bool away = Step == 1 && !AtBazaar;
            bool stamping = book == Book.Stamp;
            float h = 24f + 14f + slotH * 2f + gap + (away && !stamping ? 30f : 0f) + 24f;
            // 도장이 내려앉는 순간 판이 살짝 흔들린다
            float shake = 0f;
            foreach (var t in slamAt) if (t > 0f && now >= t && now - t < .14f) shake = (Mathf.PerlinNoise(now * 60f, 0f) - .5f) * 5f;
            var r = new Rect(Mathf.Round((W - w) / 2f + shake), Mathf.Round((H - h) / 2f - 8f + (1f - ease) * 16f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, "만국 상인회 수첩");
            int shown = 0;
            for (int i = 0; i < 4; i++) if (Has(i) && (slamAt[i] < 0f || now >= slamAt[i])) shown++;
            string sub = shown < 4 ? $"도장 {shown}/4 · 다 모으면 상인패를 받아요" : Step >= 2 ? "다 모았어요! 라일라에게 가서 가입해요" : "도장 4개 다 모았어요! 상인회장님 답장이 곧 와요";
            CuteUI.LabelCenter(r.center.x, r.y + 21f, sub, shown >= 4 ? CuteUI.Sell : CuteUI.Sub, true, shown >= 4);
            int cur = Current;
            for (int i = 0; i < 4; i++)
            {
                var slot = new Rect(r.x + pad + (i % 2) * (slotW + gap), r.y + 34f + (i / 2) * (slotH + gap), slotW, slotH);
                bool has = Has(i), landed = has && (slamAt[i] < 0f || now >= slamAt[i]);
                bool next = !has && i == cur && !stamping;
                CuteUI.Box(slot, 7, landed ? new Color32(255, 247, 240, 255) : CuteUI.White, next ? CuteUI.Interact : CuteUI.Line, next ? 2 : 1);
                float cx = slot.x + 8f + circle / 2f, cy = slot.center.y;
                CuteUI.Circle(cx - circle / 2f, cy - circle / 2f, (int)circle, CuteUI.Paper2, CuteUI.KeyLine, 1);
                var pic = TaskPic[i]; var ps = CuteUI.PicSize(pic);
                CuteUI.Icon(pic, cx - ps.x, cy - ps.y, 2, landed ? CuteUI.KeyLine : (Color32?)null);
                if (has) NetGuildArt.Stamp(cx, cy, slamAt[i] > 0f ? now - slamAt[i] : 9f, 34);
                float tx = slot.x + circle + 16f;
                CuteUI.Label(tx, slot.y + 13f, TaskTitle[i], landed ? CuteUI.Sub : CuteUI.Text, false, true);
                CuteUI.Wrap(tx, slot.y + 22f, textW, SlotText(i), landed ? CuteUI.Sell : CuteUI.Sub, true);
            }
            float by = r.y + 34f + slotH * 2f + gap + 6f;
            if (away && !stamping)
            {
                var note = new Rect(r.x + pad, by, w - pad * 2f, 22f);
                CuteUI.Box(note, 6, CuteUI.Paper2, CuteUI.Line, 1);
                CuteUI.Icon(CuteUI.Pic.Compass, note.x + 7f, note.center.y - 4f);
                CuteUI.Label(note.x + 20f, note.center.y, $"{Parcel}를 들고 만국 장터 라일라에게 · 화살표와 '목표' 줄을 따라", CuteUI.Text, true);
                by += 30f;
            }
            CuteUI.LabelCenter(r.center.x, by + 7f, stamping ? "Space 계속" : "N · Esc 닫기", CuteUI.Sub, true);
            GUI.color = old;
        }

        /// <summary>칸 아래 글: 했으면 누구와 했는지, 안 했으면 하는 법</summary>
        string SlotText(int i)
        {
            if (!Has(i)) return TaskHow[i];
            string p = partners[i];
            return string.IsNullOrEmpty(p) ? "완료!" : $"{p} · 완료!";
        }

        /// <summary>기념 사진: 셋 둘 하나 (뷰파인더) → 찰칵 (번쩍) → 사진 한 장</summary>
        void DrawPhoto()
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, now = Time.unscaledTime, t = now - shotAt;
            if (shot == Shot.Count)
            {
                // 뷰파인더 모서리 + 큰 숫자
                var f = new Rect(W * FinderX, H * FinderY, W * FinderW, H * FinderH);
                Color32 white = CuteUI.White;
                foreach (var (cx0, cy0, dx, dy) in new[] { (f.x, f.y, 1, 1), (f.xMax, f.y, -1, 1), (f.x, f.yMax, 1, -1), (f.xMax, f.yMax, -1, -1) })
                {
                    CuteUI.Fill(new Rect(dx > 0 ? cx0 : cx0 - 18, dy > 0 ? cy0 : cy0 - 3, 18, 3), white);
                    CuteUI.Fill(new Rect(dx > 0 ? cx0 : cx0 - 3, dy > 0 ? cy0 : cy0 - 18, 3, 18), white);
                }
                int n = Mathf.Clamp(3 - Mathf.FloorToInt(t / (CountSeconds / 3f)), 1, 3);
                float k = Mathf.Repeat(t, CountSeconds / 3f) / (CountSeconds / 3f);
                float d = Mathf.Round(Mathf.Lerp(46f, 38f, k));
                CuteUI.Circle(W / 2f - d / 2f, f.y + 14f, (int)d, CuteUI.Paper, CuteUI.Ink, 2, 2);
                CuteUI.LabelCenter(W / 2f, f.y + 14f + d / 2f, n.ToString(), CuteUI.Alert, false, true);
                CuteUI.LabelCenter(W / 2f, f.y + 14f + d + 10f, "다들 모여요! 이 네모 안이 그대로 찍혀요 · 움직여서 자리 잡아요", CuteUI.White, true, true);
                return;
            }
            if (shot == Shot.Snap) return;   // 찍는 순간: 아무것도 그리지 않는다 (사진에 뷰파인더가 안 찍히게)
            float ease = Ease(t / .35f);
            var old = GUI.color;
            CuteUI.Dim();
            float pw = Mathf.Min(268f, W - 40f), ph = 222f;
            var r = new Rect(Mathf.Round((W - pw) / 2f), Mathf.Round((H - ph) / 2f - 4f + (1f - ease) * 24f), pw, ph);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(-2.5f, r.center * UIStyle.Scale);
            CuteUI.Box(r, 3, CuteUI.White, CuteUI.Line, 1, 3, CuteUI.SoftShadow);
            var photo = new Rect(r.x + 10f, r.y + 10f, r.width - 20f, 150f);
            if (photoTex != null)
            {
                if (Event.current.type == EventType.Repaint) GUI.DrawTexture(photo, photoTex, ScaleMode.ScaleAndCrop);   // 찍힌 게임 화면
            }
            else
            {
                CuteUI.Fill(photo, new Color32(190, 226, 238, 255));
                CuteUI.Fill(new Rect(photo.x, photo.y + photo.height * .58f, photo.width, photo.height * .42f), new Color32(236, 212, 158, 255));
                CuteUI.Fill(new Rect(photo.x, photo.y + photo.height * .58f, photo.width, 1f), new Color32(214, 186, 128, 255));
                var bunting = Resources.Load<Sprite>("Arabia/Fair/bunting");
                if (bunting != null) for (float bx = photo.x; bx < photo.xMax; bx += 64f) CuteUI.Sprite(bunting, new Rect(bx, photo.y + 2f, Mathf.Min(64f, photo.xMax - bx), 16f), new Rect(0, 0, Mathf.Min(1f, (photo.xMax - bx) / 64f), 1));
                var lantern = Resources.Load<Sprite>("Arabia/Fair/prop_lantern");
                if (lantern != null) { CuteUI.Sprite(lantern, new Rect(photo.x + 2f, photo.y + 40f, 36f, 36f)); CuteUI.Sprite(lantern, new Rect(photo.xMax - 38f, photo.y + 40f, 36f, 36f)); }
                // 사람들 (11/10: 그림마다 크기가 달라 사막여우만 컸고, 긴 이름이 서로 겹쳤다)
                // - 사람마다 정수 배율을 따로 골라 키를 비슷하게 맞춘다 (픽셀이 번지지 않게 정수만)
                // - 한 사람 자리 = 그림 너비와 이름 너비 중 큰 것. 사진에 안 들어가면 키를 줄이고, 그래도 넘치면 이름은 아래 글로만
                int people = cast.Count;
                var scale = new int[people]; var label = new string[people];
                for (int i = 0; i < people; i++) label[i] = cast[i].name == PlayerName ? "나" : PhotoName(cast[i].name);
                float feet = photo.yMax - 16f, gap = 5f, total = 0f;
                bool names = true;
                for (float want = 84f; ; want -= 12f)
                {
                    total = 0f;
                    for (int i = 0; i < people; i++)
                    {
                        var rc = cast[i].sprite.rect;
                        scale[i] = Mathf.Max(1, Mathf.RoundToInt(want / rc.height));
                        while (scale[i] > 1 && rc.height * scale[i] > 108f) scale[i]--;
                        total += Mathf.Max(rc.width * scale[i], names ? CuteUI.Width(label[i], true) + 4f : 0f) + (i > 0 ? gap : 0f);
                    }
                    if (total <= photo.width - 12f) break;
                    if (want <= 36f) { if (!names) break; names = false; want = 96f; }
                }
                float gx = photo.center.x - total / 2f;
                for (int i = 0; i < people; i++)
                {
                    var c = cast[i];
                    float cw = c.sprite.rect.width * scale[i], ch = c.sprite.rect.height * scale[i];
                    float slot = Mathf.Max(cw, names ? CuteUI.Width(label[i], true) + 4f : 0f);
                    CuteUI.Sprite(c.sprite, new Rect(Mathf.Round(gx + (slot - cw) / 2f), Mathf.Round(feet - ch), cw, ch));
                    if (names) CuteUI.LabelCenter(gx + slot / 2f, feet + 8f, label[i], label[i] == "나" ? CuteUI.Text : CuteUI.Sub, true, label[i] == "나");
                    gx += slot + gap;
                }
            }
            // 사진 아래 글
            float y = photo.yMax + 8f;
            CuteUI.Label(r.x + 12f, y + 6f, "만국 장터에서", CuteUI.Text, false, true);
            CuteUI.LabelRight(r.xMax - 12f, y + 6f, "★ 만국 상인회", CuteUI.Frame, true, true);
            CuteUI.Wrap(r.x + 12f, y + 15f, r.width - 24f, castLine ?? "", CuteUI.Sub, true);
            GUI.matrix = m;
            CuteUI.LabelCenter(W / 2f, Mathf.Min(H - 10f, r.yMax + 14f), "Space 닫기", CuteUI.White, true, true);
            // 번쩍
            if (t < .45f)
            {
                GUI.color = new Color(1f, 1f, 1f, 1f - t / .45f);
                CuteUI.Fill(new Rect(0, 0, W, H), CuteUI.White);
            }
            GUI.color = old;
        }

        /// <summary>11/11 은샘(팀 시연 의견): 짐 쌓기 놀이판 옆 최고 기록 표. 온라인이면 순위표 1위, 아니면 내 최고</summary>
        void BoardRecord(Camera cam)
        {
            if (boardTf == null) return;
            NetScores.Peek();
            var top = NetScores.Top(NetStackGame.Board);
            int mine = NetStackGame.MyBest;
            string text = top != null && top.value > 0 ? $"최고 기록 {top.value}단 · {top.name}" : mine > 0 ? $"내 최고 {mine}단" : null;
            // 놀이판 오른쪽 옆 (아래에 두면 놀이판 앞에 선 사람을 가린다)
            if (text == null || !NetGuildArt.ToGui(cam, boardTf.position + new Vector3(1.3f, 0.2f, 0f), out var p) || !NetGuildArt.OnScreen(p, -40f)) return;
            float w = Mathf.Round(CuteUI.Width("★ " + text, true) + 14f);
            var r = new Rect(Mathf.Round(p.x), Mathf.Round(p.y - 7f), w, 15f);
            CuteUI.Box(r, 7, CuteUI.EventPaper, CuteUI.Deco, 1, 1);
            CuteUI.LabelCenter(r.center.x, r.center.y, "★ " + text, CuteUI.Text, true);
        }

        /// <summary>마당 위: 접수처 간판 · 상인 말풍선 · 찾아갈 상대 화살표 (창이 열려 있으면 숨긴다)</summary>
        void DrawWorld()
        {
            if (yard == null || !InGame || UIState.Blocking || GM.IsTravelling) return;
            var cam = Camera.main;
            if (cam == null) return;
            UIStyle.Begin();
            GUI.depth = -2;
            float now = Time.unscaledTime;
            if (clerkTf != null && NetGuildArt.ToGui(cam, clerkTf.position + new Vector3(0f, 3.9f, 0f), out var sign) && NetGuildArt.OnScreen(sign, -60f))
                CuteUI.TitlePlate(sign.x, sign.y, "만국 상인회");
            if (UIState.PhotoMode) return;   // 11/11 사진 찍는 중에는 말풍선·찾아갈 사람 화살표를 감춘다
            BoardRecord(cam);
            foreach (var kv in bubbles)
                if (kv.Key != null && now < kv.Value.until && NetGuildArt.ToGui(cam, kv.Key.position + new Vector3(0f, 1.45f, 0f), out var p))
                    NetGuildArt.Bubble(p, kv.Value.text);

            // 찾아갈 상대가 다른 플레이어면 머리 위 화살표 (화면 밖이면 가장자리). 사람·자리는 길 안내(Navigator)가 이름 위에 띄운다.
            // 바로 앞이면 Space 안내가 대신한다
            Vector3 at;
            if (markPeer == null || !RemotePlayers.TryGetPosition(markPeer, out at)) return;
            var pc = PlayerController.Instance;
            if (pc != null && ((Vector2)at - (Vector2)pc.transform.position).sqrMagnitude < 1.7f * 1.7f) return;
            float lift = markLift + (markTf != null && bubbles.ContainsKey(markTf) ? .9f : 0f);
            if (!NetGuildArt.ToGui(cam, at + new Vector3(0f, lift, 0f), out var head)) return;
            if (NetGuildArt.OnScreen(head, 12f)) NetGuildArt.DownMarker(head, now);
            else NetGuildArt.EdgeArrow(head);
        }

        // ---------------- 시험용 (에디터·개발 빌드에서 채팅 /상인회) ----------------

        /// <summary>채팅 "/상인회": 초대장을 바로 받는다 (시험용). "/상인회 처음"이면 진행을 지우고, "/상인회 마차"면 상인회 마당 옆으로 바로 간다. "/상인회 서명"이면 명부 서명 창부터</summary>
        public static bool TryCommand(string word)
        {
            if (!word.StartsWith("상인회")) return false;
            if (!InputReader.DevKeys) { NetChat.System("상인회 초대장은 첫걸음 안내를 마치면 와요"); return true; }
            if (GM == null) return true;
            if (word.Contains("처음"))
            {
                GM.ClearMemo(StepKey); GM.ClearMemo(WelcomeKey);
                foreach (var k in TaskKeys) GM.ClearMemo(k);
                foreach (var k in OldKeys) GM.ClearMemo(k);
                ResetStatics();
                if (inst != null) inst.pendingStamps.Clear();
                NetChat.System("상인회 이야기를 처음으로 되돌렸어요 (시험용)");
                return true;
            }
            if (word.Contains("마차")) { Ride(); return true; }   // 시험용 순간 이동 (상인회 마당 옆)
            if (word.Contains("서명")) { Step = 2; if (inst != null) inst.OpenSign(); return true; }   // 시험용: 수첩을 다 채운 걸로 치고 명부 서명 → 수여식 (11/11)
            if (Step == 0 && inst != null) { Step = 1; inst.pendingLetter = Letter.Invite; }
            else NetChat.System($"상인회 이야기 {Step}단계 · 수첩 {DoneCount}/{TaskKeys.Length}");
            return true;
        }
    }
}
