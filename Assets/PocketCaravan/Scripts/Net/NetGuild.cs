using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 만국 상인회 이야기 「만국 큰 장으로」 (2026-11-09, 11/10 다시 만듦. 1차 시연: 여러 사람이 함께 어울리는 두 번째 튜토리얼).
    /// 1) 초대장: 지역 첫걸음 안내(튜토리얼)를 마치면 봉인 찍힌 만국 상인회 초대장이 펼쳐진다.
    ///    [상인회 마차 타기]면 만국 장터 동쪽 어귀(상인회 마당 옆)로 바로 간다. 걸어가도 되고, 수첩의 마차 단추로 언제든 탄다.
    /// 2) 도착: 상인회 접수원 라일라가 맞아 주고 수첩을 설명한다.
    /// 3) 상인회 수첩 (오른쪽 위 카드): 인사 → 리액션 → 선물 → 노점 거래. 순서와 상관없이 해도 칸은 채워지고,
    ///    카드는 "지금 할 일" 한 칸만 크게 펼쳐 방법을 늘 보여 준다(마우스를 올릴 필요 없음). 찍을 때마다 "완료" 도장이 쾅 내려앉고
    ///    누구와 했는지(길보 · 은샘 님 …)가 남는다. 찾아갈 상대 머리 위에는 노란 화살표, 화면 밖이면 가장자리 화살표.
    ///    - 인사: Space 창(NetMeet)의 인사 또는 채팅 한마디 (서버가 돌려준 내 말)
    ///    - 리액션: 다른 플레이어·말 걸 수 있는 사람 4칸 안에서 리액션 (Space 창 · 채팅 /춤 · 베네치아·북유럽 R)
    ///    - 선물: 다른 플레이어에게 선물이 닿거나(NetGift) 상인회 상인에게 건네기
    ///    - 거래: 상인회 노점에서 사거나 팔기. 노점은 제다 공유 장터 시세를 쓴다 (만국 장터엔 장터가 없어서, 11/09 윤채 결정)
    ///    혼자 온 사람도 상인회 상인 셋(고려 보부상 길보 · 북쪽 상인 비요른 · 대상 하킴)과 다 채울 수 있다. 만국 장터 안에서 한 것만 센다.
    /// 4) 답장: 다 채우면 상인회장의 답장 → 라일라에게 가서 이름 올리기 → 상인패 수여식(메달 · 꽃가루). 보상 50냥,
    ///    온라인 이름표에 금빛 "★ 상인회"(서버 tag "guild").
    /// 상인회 마당은 만국 장터 동쪽 빈터(맵 JSON 칸 74~88, 24~26. 27~28줄은 건물 지붕 밑이라 피했다)에 실행 중에만 세운다.
    ///   접수처·노점은 아라비아 큰 장 그림(Resources/Arabia/Fair: 좌판 · 양탄자 · 깃발 · 등불)을 빌려 쓰고, 아라비아 씬·데이터는 고치지 않는다.
    /// 진행은 공용 진행 메모 "net:guild:step"(0 없음 · 1 초대장 받음 · 2 수첩 다 채움 · 3 가입), "net:guild:hello|emote|gift|trade"(1 = 함),
    ///   "net:guild:welcomed"(라일라 첫인사를 들음). 누구와 했는지는 이번 실행에만 기억한다.
    /// 지역 Intro는 읽기만 한다 (끝났는지: 각 지역 저장의 intro 단계 11).
    /// </summary>
    public class NetGuild : MonoBehaviour
    {
        public const string Bazaar = "arabia_central_bazaar";
        public const string BadgeId = "guild";
        const string StallTown = "arabia_jeddah";
        const string ArriveSpawn = "arabia_central_bazaar_spawn_seam_east_3";
        const int BazaarHeight = 56, Reward = 50;
        const float LetterDelay = 2.5f, NearCells = 4f, ReplyDelay = 1.4f, MarkRange = 14f;
        const string Clerk = "상인회 접수원 라일라", Chief = "만국 상인회장 아지즈 드림";
        const string StepKey = "net:guild:step", WelcomeKey = "net:guild:welcomed";

        public enum Task { Hello, Emote, Gift, Trade }
        static readonly string[] TaskKeys = { "net:guild:hello", "net:guild:emote", "net:guild:gift", "net:guild:trade" };
        static readonly string[] TaskTitle = { "반갑게 인사하기", "리액션으로 마음 전하기", "작은 선물 건네기", "상인회 노점에서 사고팔기" };
        /// <summary>지금 할 일 아래 방법 줄: 앞말 [키] 뒷말</summary>
        static readonly (string pre, string key, string post)[] TaskHow =
        {
            ("상인 앞에서", "Space", "인사하기"), ("상인 앞에서", "Space", "리액션 고르기"), ("상인 앞에서", "Space", "선물 고르기"), ("노점 앞에서", "Space", "하나 사 보기"),
        };
        static readonly CuteUI.Pic[] TaskPic = { CuteUI.Pic.People, CuteUI.Pic.Star4, CuteUI.Pic.Sack, CuteUI.Pic.Coin7 };

        static readonly string[] InvitePara =
        {
            "바다 건너, 사막 건너. 네 지방 상인이 모두 모이는 만국 장터에 큰 장이 섰습니다.",
            "상인회 패는 장부가 아니라 사람으로 얻는 법이지요. 장터의 상인들과 인사하고, 마음을 나누고, 거래해 보세요. 할 일은 함께 보내는 수첩에 적어 두었습니다.",
            "상인회 마차가 마을 어귀에서 기다립니다.",
        };
        static readonly string[] ReplyPara =
        {
            "수첩을 다 채웠다는 소식은 장터 사람들에게 먼저 들었습니다. 벌써 당신 이야기로 떠들썩하더군요.",
            "인사하고, 웃고, 나누고, 사고파는 상인. 상인회가 기다리던 사람입니다.",
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

        /// <summary>이번 실행에서 칸마다 누구와 했는지 · 도장 찍은 시각</summary>
        static readonly string[] partners = new string[4];
        static readonly float[] stampAt = { -99f, -99f, -99f, -99f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { for (int i = 0; i < 4; i++) { partners[i] = null; stampAt[i] = -99f; } }

        enum Letter { None, Invite, Reply }

        static NetGuild inst;
        Transform yard, clerkTf, stallTf;
        readonly List<NetMeet.Npc> merchants = new List<NetMeet.Npc>();
        readonly Dictionary<Transform, (string text, float until)> bubbles = new Dictionary<Transform, (string, float)>();
        readonly Dictionary<Transform, float> ambientAt = new Dictionary<Transform, float>();
        readonly List<Transform> expired = new List<Transform>();
        /// <summary>깡총 뛰는 그림 (원래 높이 · 시작 시각)</summary>
        readonly List<(Transform body, float baseY, float start)> hops = new List<(Transform, float, float)>();
        const float HopSeconds = 1.1f;
        float readyAt = -1f, welcomeAt = -1f, nextCheck, lastStamp = -99f, cardSince = -1f;
        bool riding;
        Letter letter, pendingLetter;
        float letterAt; int letterFrame;
        bool ceremony; float ceremonyAt; int ceremonyFrame;
        NetGuildArt.Bit[] confetti;
        // 찾아갈 상대 (다른 플레이어 아이디 또는 사람 자리) · 머리 위 높이
        Transform markTf; string markPeer; float markLift;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetGuild>() != null) return;
            var go = new GameObject("NetGuild");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetGuild>();
            go.AddComponent<WorldLayer>();
        }

        /// <summary>마당 위 그림(간판 · 말풍선 · 화살표)은 상태 카드·창보다 아래 층에 그린다</summary>
        class WorldLayer : MonoBehaviour { void OnGUI() { if (inst != null) inst.DrawWorld(); } }

        void Awake() => inst = this;

        void OnEnable()
        {
            GameEvents.MapEntered += OnMap;
            GameEvents.ItemBought += OnBought;
            GameEvents.ItemSold += OnSold;
            NetEmote.Sent += OnEmote;
            NetGift.Delivered += OnGift;
            NetClient.Received += OnReceived;
        }

        void OnDisable()
        {
            GameEvents.MapEntered -= OnMap;
            GameEvents.ItemBought -= OnBought;
            GameEvents.ItemSold -= OnSold;
            NetEmote.Sent -= OnEmote;
            NetGift.Delivered -= OnGift;
            NetClient.Received -= OnReceived;
        }

        void Start() { if (AtBazaar) OnMap(Bazaar); }

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
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.2f;
            float now = Time.unscaledTime;
            expired.Clear();
            foreach (var kv in bubbles) if (kv.Key == null || now > kv.Value.until) expired.Add(kv.Key);
            foreach (var t in expired) bubbles.Remove(t);
            FindMark();
            Ambient(now);

            bool calm = InGame && !UIState.Blocking && !GM.IsTravelling;
            if (!calm) { if (Step == 0) readyAt = -1f; return; }
            if (pendingLetter != Letter.None) { var l = pendingLetter; pendingLetter = Letter.None; OpenLetter(l); return; }
            int s = Step;
            if (s == 0)
            {
                if (!IntroDone() || IntroActive) { readyAt = -1f; return; }
                if (readyAt < 0f) readyAt = now + LetterDelay;   // 튜토리얼 마지막 말이 끝나고 잠깐 뒤에
                if (now >= readyAt) { Step = 1; OpenLetter(Letter.Invite); }
                return;
            }
            if (s == 1 && AtBazaar && GM.GetMemo(WelcomeKey) < 1f)
            {
                if (welcomeAt < 0f) welcomeAt = now + 0.6f;
                else if (now >= welcomeAt) { welcomeAt = -1f; Welcome(); }
                return;
            }
            if (s == 1 && DoneCount >= TaskKeys.Length && now - lastStamp > ReplyDelay) { Step = 2; Sfx.Play("levelup", .6f); OpenLetter(Letter.Reply); }
        }

        /// <summary>상인회 마차: 만국 장터 동쪽 어귀(상인회 마당 옆)로 바로 간다</summary>
        public static void Ride()
        {
            if (GM == null || inst == null) return;
            inst.CloseLetter();
            if (AtBazaar) { Notify.Show("여기가 만국 장터예요! 노란 화살표를 따라 상인회 마당으로 가 보세요.", 3.5f); return; }
            if (GM.IsTravelling) return;
            inst.riding = true;
            Sfx.Play("accept");
            GM.TravelTo(Bazaar, ArriveSpawn, 0f, Vector2Int.left);
        }

        static void Walk()
        {
            if (inst != null) inst.CloseLetter();
            Notify.Show("만국 장터는 지도(M) 한가운데예요. 수첩의 '상인회 마차 타기'로 언제든 탈 수 있어요.", 5f);
        }

        void Welcome()
        {
            GM.SetMemo(WelcomeKey, 1f);
            Say(clerkTf, "어서 와요!");
            Hop(clerkTf);
            DialogueUI.Show(Clerk,
                $"어서 와요, {PlayerName} 님! 만국 상인회 접수원 라일라예요. 초대장 받고 왔군요?\n\n" +
                "상인회 패를 받으려면 수첩 네 칸을 채워 와요. 지금 할 일은 오른쪽 수첩에, 찾아갈 상대는 머리 위 노란 화살표로 알려 줄게요.\n\n" +
                "다른 상인 앞에서 Space를 누르면 인사·리액션·선물을 고를 수 있어요. 채팅(Enter)으로 말을 걸어도 좋고요. 혼자 왔어도 괜찮아요. 길보, 비요른, 하킴도 상인회 사람들이에요!",
                new DialogueUI.Choice("좋아요!", () => Say(clerkTf, "화이팅!")));
        }

        // ---------------- 수첩 ----------------

        /// <summary>수첩 한 칸에 도장을 찍는다 (만국 장터에서, 초대장을 받은 뒤에만). partner = 누구와 했는지 (짧게)</summary>
        public static void Mark(Task t, string partner = null)
        {
            int i = (int)t;
            if (Step != 1 || !AtBazaar || GM == null || Has(i)) return;
            GM.SetMemo(TaskKeys[i], 1f);
            partners[i] = partner;
            stampAt[i] = Time.unscaledTime;
            if (inst != null) inst.lastStamp = Time.unscaledTime;
            Sfx.Play("success");
            int next = Current;
            Notify.Show(next < 0 ? "수첩을 다 채웠다! 상인회에서 소식이 올 거예요" : $"수첩에 도장 쾅! 다음: {TaskTitle[next]}", 3.5f);
        }

        void OnReceived(NetClient.Msg m)
        {
            // 채팅으로 한마디 = 인사 (서버가 받아서 나에게도 돌려준 말만)
            if (m.t == "say" && NetClient.I != null && m.id == NetClient.I.MyId && !string.IsNullOrEmpty(m.text)) Mark(Task.Hello, "채팅");
        }

        void OnEmote(string id)
        {
            if (Step != 1 || !AtBazaar) return;
            string who = NearestName();
            if (who != null) Mark(Task.Emote, who);
        }

        void OnGift(string to) => Mark(Task.Gift, to + " 님");
        void OnBought(ItemData item, TownData town, int price) { if (town != null && town.id == StallTown) Mark(Task.Trade, "노점"); }
        void OnSold(ItemData item, TownData town, int price, int profit) { if (town != null && town.id == StallTown) Mark(Task.Trade, "노점"); }

        /// <summary>4칸 안 가장 가까운 상대의 짧은 이름 (다른 플레이어 · 상인회 상인 · 말 걸 수 있는 사람). 없으면 null</summary>
        string NearestName()
        {
            var pc = PlayerController.Instance;
            if (pc == null) return null;
            Vector2 me = pc.transform.position;
            float best = NearCells * NearCells; string who = null;
            var net = NetClient.I;
            if (net != null)
                foreach (var p in net.Peers.Values)
                {
                    float d = (new Vector2(p.x, p.y) - me).sqrMagnitude;
                    if (d <= best) { best = d; who = (string.IsNullOrEmpty(p.name) ? p.id : p.name) + " 님"; }
                }
            foreach (var n in merchants)
            {
                if (n.anchor == null) continue;
                float d = ((Vector2)n.anchor.position - me).sqrMagnitude;
                if (d <= best) { best = d; who = n.shortName; }
            }
            if (who != null) return who;
            foreach (var it in Interactable.All)
                if (it != null && it.isActiveAndEnabled && it.Category == Interactable.Kind.Person
                    && ((Vector2)it.transform.position - me).sqrMagnitude <= NearCells * NearCells) return WorldDisplayNames.ShortName(it.DisplayName) ?? "상인";
            return null;
        }

        // ---------------- 상인회 마당 (만국 장터 동쪽) ----------------

        class Person : Interactable
        {
            public System.Func<string> label; public System.Action act;
            public override string DisplayName => label != null ? label() : "";
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
            yard = clerkTf = stallTf = null;
            merchants.Clear(); bubbles.Clear(); ambientAt.Clear(); hops.Clear();
            markTf = null; markPeer = null;
            if (map != Bazaar) { riding = false; return; }
            BuildYard();
            if (riding)
            {
                riding = false;
                Notify.Show("상인회 마차를 타고 만국 장터에 도착했다!", 4f);
            }
        }

        void BuildYard()
        {
            var pc = PlayerController.Instance;
            if (pc == null || MapRoot.Current == null || GM == null) return;
            yard = new GameObject("만국 상인회 마당").transform;

            clerkTf = Spawn("arabia", 84, 24, () => Clerk + (Step == 0 || Step == 2 ? " !" : ""), TalkClerk, true, pc).transform;
            Booth(clerkTf.position, true);

            var town = GM.Db != null ? GM.Db.towns.Find(t => t != null && t.id == StallTown) : null;
            if (town != null)
            {
                var go = Body("venice", 78, 24, pc);
                go.AddComponent<Stall>().town = town;
                stallTf = go.transform;
                Booth(stallTf.position, false);
            }

            Merchant("goryeo", 74, 26, new NetMeet.Npc
            {
                name = "고려 보부상 길보", shortName = "길보",
                hello = "어이쿠, 반갑소! 개경에서 등짐 지고 왔다오.",
                cheer = "허허, 흥이 나는구먼!",
                thanks = "이리 귀한 걸! 받은 정은 꼭 갚으리다.",
                near = "어이, 새 얼굴이구먼! 이리 와 보시오.",
            }, pc);
            Merchant("viking", 81, 26, new NetMeet.Npc
            {
                name = "북쪽 상인 비요른", shortName = "비요른",
                hello = "하하! 피오르에서 배 타고 왔지. 반갑다!",
                cheer = "좋아, 나도 발 구르기다!",
                thanks = "오, 고맙다! 오늘은 꿀술로 건배다!",
                near = "너도 큰 장 구경 왔나? 하하!",
            }, pc);
            Merchant("arabia", 88, 26, new NetMeet.Npc
            {
                name = "대상 하킴", shortName = "하킴",
                hello = "평안을 빕니다, 친구여. 잘 왔어요.",
                cheer = "하하, 사막 바람보다 시원하군요!",
                thanks = "선물은 길동무를 만든다지요. 고마워요!",
                near = "평안을, 친구여. 여기가 상인회 마당이에요.",
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
            Near(clerkTf, s == 0 ? "상인회에 들고 싶다면 이리 와요!" : s == 1 ? "수첩은 잘 채우고 있어요?" : s == 2 ? "다 채웠군요! 이리 와요!" : "어서 와요, 상인회 동료!", me, now);
        }

        void Near(Transform t, string line, Vector2 me, float now)
        {
            if (t == null || ((Vector2)t.position - me).sqrMagnitude > 3.5f * 3.5f) return;
            if (ambientAt.TryGetValue(t, out var at) && now - at < 40f) return;
            if (bubbles.TryGetValue(t, out var b) && now < b.until) return;
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
                    "어서 와요! 여기는 만국 상인회 마당이에요. 상인회에 들고 싶다면 이 수첩부터 채워 와요. 장터 사람들과 어울려 보는 게 첫 장사랍니다.",
                    new DialogueUI.Choice("수첩 받기", () =>
                    {
                        Step = 1; GM.SetMemo(WelcomeKey, 1f);
                        Sfx.Play("accept");
                        Say(clerkTf, "화이팅!");
                        Notify.Show("상인회 수첩을 받았다! 오른쪽 위 수첩을 채워 보세요.", 4f);
                    }),
                    new DialogueUI.Choice("다음에요", null));
                return;
            }
            if (s == 1)
            {
                int cur = Current;
                DialogueUI.Show(Clerk, cur < 0 ? "수첩을 다 채웠군요! 잠깐만요, 상인회장님 답장이 곧 올 거예요."
                    : $"수첩이 {DoneCount}/{TaskKeys.Length}칸 찼네요. 다음은 '{TaskTitle[cur]}'예요.\n\n" +
                      "노란 화살표가 가리키는 상인 앞에서 Space를 눌러 봐요. 다른 사람이 접속해 있으면 그 사람과 해도 돼요!");
                return;
            }
            if (s == 2)
            {
                DialogueUI.Show(Clerk,
                    "수첩 잘 봤어요! 인사하고, 웃고, 나누고, 사고파는 것. 그게 만국 장터의 법이에요.\n\n자, 여기 이름을 적어요.",
                    new DialogueUI.Choice("이름 올리기", OpenCeremony));
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
            if (s == 2) { markTf = clerkTf; markLift = 1.55f; return; }
            if (s != 1 || GM.GetMemo(WelcomeKey) < 1f) return;
            int cur = Current;
            if (cur < 0) return;
            if (cur == (int)Task.Trade) { markTf = stallTf; markLift = 1.55f; return; }
            // 인사·리액션·선물: 가까운 다른 플레이어가 먼저, 없으면 가까운 상인회 상인
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
            markLift = 1.55f;
        }

        // ---------------- 창: 편지 · 수여식 ----------------

        void OpenLetter(Letter kind)
        {
            if (!UIState.TryOpen(this, CloseLetter)) { pendingLetter = kind; return; }
            letter = kind; letterAt = Time.unscaledTime; letterFrame = Time.frameCount;
            Sfx.Play("open");
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
                Notify.Show("상인회 마당의 라일라에게 가서 이름을 올리세요 · 노란 화살표를 따라가요", 4.5f);
            }
        }

        void OpenCeremony()
        {
            Step = 3;
            GM.Player.money += Reward;
            NetEmote.Send("joy", "기쁨");
            RemotePlayers.PlaySelf("joy", "기쁨");
            Hop(clerkTf);
            if (!UIState.TryOpen(this, CloseCeremony)) { Finish(); return; }
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
            Notify.Show("만국 상인회 상인이 되었다! 이제 어디로든, 나만의 장사길로", 5f);
            NetChat.System("만국 상인회에 가입했어요 · 다른 사람 화면의 내 이름표에 '★ 상인회'가 붙어요");
        }

        void HandleWindows()
        {
            float now = Time.unscaledTime;
            if (letter != Letter.None && Time.frameCount > letterFrame && now - letterAt > .3f)
            {
                if (InputReader.InteractPressed()) { if (letter == Letter.Invite) Ride(); else CloseLetter(); }
                else if (InputReader.CancelPressed()) { if (letter == Letter.Invite) Walk(); else CloseLetter(); }
            }
            if (ceremony && Time.frameCount > ceremonyFrame && now - ceremonyAt > .8f && (InputReader.InteractPressed() || InputReader.CancelPressed()))
                CloseCeremony();
        }

        // ---------------- 그리기 ----------------

        static bool Showing => Active && InGame && !UIState.Blocking && !HUD.Expanded && !GM.IsTravelling;

        void OnGUI()
        {
            if (letter == Letter.None && !ceremony && !Showing) return;
            UIStyle.Begin();
            if (ceremony) { GUI.depth = -30; DrawCeremony(); return; }
            if (letter != Letter.None) { GUI.depth = -30; DrawLetter(); return; }
            GUI.depth = -4;
            DrawPill();
            DrawCard();
        }

        static float Ease(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t) * (1f - t); }

        void DrawLetter()
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, ease = Ease((Time.unscaledTime - letterAt) / .35f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            CuteUI.Dim();
            bool invite = letter == Letter.Invite;
            var paras = invite ? InvitePara : ReplyPara;
            float w = Mathf.Min(330f, W - 30f), inner = w - 40f, body = 0f;
            foreach (var p in paras) body += CuteUI.WrapHeight(p, inner) + 6f;
            float h = 26f + 18f + body + 22f + 26f + 14f + 8f;
            var r = new Rect(Mathf.Round((W - w) / 2f), Mathf.Round((H - h) / 2f + (1f - ease) * 18f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, invite ? "만국 상인회 초대장" : "만국 상인회 답장");
            float x = r.x + 20f, y = r.y + 22f;
            CuteUI.Label(x, y + 7f, PlayerName + " 님께", CuteUI.Text, false, true);
            y += 19f;
            foreach (var p in paras) y += CuteUI.Wrap(x, y, inner, p, CuteUI.Text) + 6f;
            // 서명 + 봉인
            float sigW = CuteUI.Width(Chief, true);
            CuteUI.LabelRight(r.xMax - 20f, y + 8f, Chief, CuteUI.Sub, true);
            NetGuildArt.Seal(r.xMax - 20f - sigW - 18f, y + 8f);
            y += 24f;
            if (invite)
            {
                float bw = Mathf.Round(inner * .58f);
                if (CuteUI.Button(new Rect(x, y, bw, 22f), "상인회 마차 타기")) Ride();
                if (CuteUI.SoftButton(new Rect(x + bw + 6f, y, inner - bw - 6f, 22f), "걸어서 갈게요")) Walk();
            }
            else if (CuteUI.Button(new Rect(x, y, inner, 22f), "라일라에게 가기")) CloseLetter();
            CuteUI.LabelCenter(r.center.x, y + 33f, invite ? "Space 마차 타기 · Esc 걸어서 가기" : "Space 닫기", CuteUI.Sub, true);
            GUI.color = old;
        }

        void DrawCeremony()
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, now = Time.unscaledTime, age = now - ceremonyAt, ease = Ease(age / .4f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            CuteUI.Dim();
            float w = Mathf.Min(300f, W - 30f), h = 244f;
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

        void DrawPill()
        {
            int s = Step, cur = Current;
            string main, where; bool pin;
            if (s == 2) { main = "라일라에게 가서 상인회 가입하기"; where = "만국 장터 상인회 마당"; pin = true; }
            else if (!AtBazaar) { main = "만국 장터로 가기"; where = "수첩의 상인회 마차"; pin = false; }
            else { main = cur >= 0 ? TaskTitle[cur] : "상인회 수첩 다 채움"; where = $"수첩 {DoneCount}/{TaskKeys.Length}"; pin = false; }
            CuteUI.TopPill(CuteUI.TopStack(12), s == 2, CuteUI.Pic.OpenBook, main, where, pin);
        }

        /// <summary>
        /// 상인회 수첩 카드 (오른쪽 위). 지금 할 일 한 칸만 크게 펼쳐 방법을 보여 주고, 한 칸은 도장 + 누구와 했는지, 남은 칸은 흐리게.
        /// 처음 뜰 때 오른쪽에서 미끄러져 들어온다. 길 장애 알약 등 오른쪽 위에 이미 있는 것 밑으로 내려간다
        /// </summary>
        void DrawCard()
        {
            const float w = 206f;
            float now = Time.unscaledTime;
            int s = Step;
            bool away = s == 1 && !AtBazaar;
            int cur = s == 1 && !away ? Current : -1;
            float rows = 0f;
            for (int i = 0; i < TaskKeys.Length; i++) rows += i == cur ? 34f : 19f;
            float h = 7f + 18f + 6f + rows + (away ? 28f : s == 2 ? 28f : 0f) + 5f;

            if (cardSince < 0f) cardSince = now;
            float slide = Mathf.Round((1f - Ease((now - cardSince) / .4f)) * 50f);
            float x = Mathf.Round(UIStyle.VirtualWidth - 6f - w), top = 40f;
            ScreenZones.Mark("guild.card", default);   // 지난 프레임의 내 자리는 빼고 잰다
            for (int i = 0; i < 80 && ScreenZones.FreeRow(top, top + h, 4f).right < x + w; i++) top += 4f;
            ScreenZones.Mark("guild.card", new Rect(x, top, w, h));
            var r = new Rect(x + slide, top, w, h);
            CuteUI.Card(r);

            // 머리: 수첩 + 네 칸 진행 점
            float y = r.y + 7f;
            CuteUI.Icon(CuteUI.Pic.OpenBook, r.x + 10f, y + 4f);
            CuteUI.Label(r.x + 28f, y + 9f, "만국 상인회 수첩", CuteUI.Text, false, true);
            for (int i = 0; i < TaskKeys.Length; i++)
                CuteUI.Circle(r.xMax - 12f - (TaskKeys.Length - i) * 10f + 2f, y + 5f, 8, Has(i) ? CuteUI.Interact : CuteUI.White, CuteUI.Ink);
            y += 21f;
            CuteUI.Fill(new Rect(r.x + 8f, y, w - 16f, 1f), CuteUI.Line);
            y += 5f;

            for (int i = 0; i < TaskKeys.Length; i++)
            {
                bool done = Has(i), here = i == cur;
                float rh = here ? 34f : 19f;
                var row = new Rect(r.x + 6f, y, w - 12f, rh - 2f);
                if (here)
                {
                    float glow = .5f + .5f * Mathf.Sin(now * 3f);
                    CuteUI.Box(row, 6, CuteUI.Paper2, glow > .5f ? CuteUI.Interact : CuteUI.InteractHi, 2);
                }
                float cy = row.y + 8.5f;
                var pic = TaskPic[i]; var ps = CuteUI.PicSize(pic);
                CuteUI.Icon(pic, row.x + 10f - ps.x / 2f, cy - ps.y / 2f, 1, done || here ? (Color32?)null : CuteUI.KeyLine);
                Color32 col = done ? CuteUI.Sub : here ? CuteUI.Text : CuteUI.Zero;
                float tx = CuteUI.Label(row.x + 21f, cy, TaskTitle[i], col, false, here);
                if (done && !string.IsNullOrEmpty(partners[i]))
                {
                    string with = "· " + partners[i];
                    if (tx + 4f + CuteUI.Width(with, true) < row.xMax - 26f) CuteUI.Label(tx + 4f, cy + .5f, with, CuteUI.Sub, true);
                }
                if (here)
                {
                    var (pre, key, post) = TaskHow[i];
                    float hy = row.y + 17f;
                    float hx = CuteUI.Label(row.x + 21f, hy + 6.5f, pre, CuteUI.Sub, true);
                    hx += 4f + CuteUI.Keycap(hx + 4f, hy, key, true);
                    CuteUI.Label(hx + 4f, hy + 6.5f, post, CuteUI.Sub, true);
                }
                if (done) NetGuildArt.Stamp(row.xMax - 13f, cy, now - stampAt[i]);
                y += rh;
            }

            if (away)
            {
                y += 3f;
                if (CuteUI.Button(new Rect(r.x + 8f, y, w - 16f, 22f), "상인회 마차 타기")) Ride();
            }
            else if (s == 2)
            {
                y += 3f;
                var b = new Rect(r.x + 6f, y, w - 12f, 22f);
                CuteUI.Box(b, 6, CuteUI.AlertSoft, CuteUI.Alert, 1);
                CuteUI.Label(b.x + 8f, b.center.y, "다 채웠어요! 라일라에게 가서 가입하기", CuteUI.Alert, true, true);
            }
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
            if (clerkTf != null && NetGuildArt.ToGui(cam, clerkTf.position + new Vector3(0f, 3.35f, 0f), out var sign) && NetGuildArt.OnScreen(sign, -60f))
                CuteUI.TitlePlate(sign.x, sign.y, "만국 상인회");
            foreach (var kv in bubbles)
                if (kv.Key != null && now < kv.Value.until && NetGuildArt.ToGui(cam, kv.Key.position + new Vector3(0f, 1.45f, 0f), out var p))
                    NetGuildArt.Bubble(p, kv.Value.text);

            // 찾아갈 상대: 화면 안이면 머리 위 화살표, 밖이면 가장자리 화살표. 바로 앞이면 Space 안내가 대신한다
            Vector3 at;
            if (markPeer != null) { if (!RemotePlayers.TryGetPosition(markPeer, out at)) return; }
            else if (markTf != null) at = markTf.position;
            else return;
            var pc = PlayerController.Instance;
            if (pc != null && ((Vector2)at - (Vector2)pc.transform.position).sqrMagnitude < 1.7f * 1.7f) return;
            float lift = markLift + (markTf != null && bubbles.ContainsKey(markTf) ? .9f : 0f);
            if (!NetGuildArt.ToGui(cam, at + new Vector3(0f, lift, 0f), out var head)) return;
            if (NetGuildArt.OnScreen(head, 12f)) NetGuildArt.DownMarker(head, now);
            else NetGuildArt.EdgeArrow(head);
        }

        // ---------------- 시험용 (에디터·개발 빌드에서 채팅 /상인회) ----------------

        /// <summary>채팅 "/상인회": 초대장을 바로 받는다 (시험용). "/상인회 처음"이면 진행을 지운다</summary>
        public static bool TryCommand(string word)
        {
            if (!word.StartsWith("상인회")) return false;
            if (!InputReader.DevKeys) { NetChat.System("상인회 초대장은 첫걸음 안내를 마치면 와요"); return true; }
            if (GM == null) return true;
            if (word.Contains("처음"))
            {
                GM.ClearMemo(StepKey); GM.ClearMemo(WelcomeKey);
                foreach (var k in TaskKeys) GM.ClearMemo(k);
                ResetStatics();
                NetChat.System("상인회 이야기를 처음으로 되돌렸어요 (시험용)");
                return true;
            }
            if (Step == 0 && inst != null) { Step = 1; inst.pendingLetter = Letter.Invite; }
            else NetChat.System($"상인회 이야기 {Step}단계 · 수첩 {DoneCount}/{TaskKeys.Length}");
            return true;
        }
    }
}
