using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 튜토리얼 스포트라이트 (2026-11-18 은샘·팀 결정: 시안 spot_C_flow "동그라미와 말풍선이 남는 판").
    /// 처음 하는 유저가 지금 눌러야 할 곳·가야 할 사람을 놓치지 않게:
    /// 1) 그 단계를 처음 만나면 화면이 반쯤 어두워지고 목표만 밝게 뚫린다 (청록 테두리가 깜빡) + 짧은 말풍선 + "아무 키나 누르면 계속".
    /// 2) 아무 키나(Space·방향키 등) 누르거나 클릭하면 어둠이 걷힌다. 그 키는 원래 일도 그대로 한다 (먹지 않는다).
    ///    어둠은 단계마다 처음 한 번만 (진행 메모 "tutorial:spot:아이디"). 테두리와 말풍선은 그 단계를 끝낼 때까지 남는다.
    /// 쓰는 단계 (네 지역 시작 튜토리얼 공통. 키만 누르는 달리기·Tab은 쓰지 않는다):
    /// - 안내 인물에게 말 걸기 (Navigator.Tutorial이 같은 맵의 사람을 가리킬 때) — 사람 둘레 동그라미
    /// - 할 일 카드 누르기 — 카드 ("task.card")
    /// - 사기 / 팔기 — 창이 닫혀 있으면 장터 상인 둘레, 열려 있으면 "팔기" 칸이나 사기·팔기 단추 ("market.*")
    /// - 소지품(I) · 지도(M) · 장부(L) 보기 — 오른쪽 위 단추 ("hud.btn.i" · "hud.btn.m" · "hud.btn.l")
    /// 목표 자리는 그 화면이 그릴 때 ScreenZones에 올린 것을 쓴다 (이 파일은 그리기만 한다). 다른 창(대화·흥정 등)이 떠 있으면 감춘다.
    /// </summary>
    public class TutorialSpot : MonoBehaviour
    {
        const float DimAlpha = .58f, FadeIn = .25f, FadeOut = .22f;
        const string MemoPrefix = "tutorial:spot:";

        struct Spot { public string id; public Rect hole; public bool round; public string bubble; public bool noDim, noRing, world; }

        static TutorialSpot inst;
        string shownId;          // 지금 밝힌 것
        float dim;               // 어둠 0~1 (천천히 따라간다)
        bool dimWanted;          // 이번 단계를 처음 만나 아직 아무 키도 안 눌렀다

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<TutorialSpot>() != null) return;
            var go = new GameObject("TutorialSpot");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<TutorialSpot>();
        }

        void Awake() => inst = this;

        /// <summary>11/18 창 안 설명(장부 탭 · 수첩 튜토리얼 카드)을 보여 주는 중. 그 창을 여는 키(L·Q)로는 닫지 않는다</summary>
        public static bool HoldsWindow => Time.frameCount - holdFrame <= 1;
        static int holdFrame = -9;
        static bool journalTourShown;   // 이번 수첩 단계에서 튜토리얼 카드 설명이 떴다
        /// <summary>11/18 창 설명을 다 보지 않고 닫았다 → 그 배우기 단계는 아직 안 끝난다 (ControlLesson). 다시 열면 남은 설명부터</summary>
        public static bool TourLeft(int task)
        {
            if (task == ControlLesson.Ledger) { for (int i = 0; i < LedgerTour.Length; i++) if (!Seen("ledger.tab" + i)) return true; return false; }
            if (task == ControlLesson.Quests) return journalTourShown && !Seen("journal.tutorial");
            return false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHold() { holdFrame = -9; journalTourShown = false; }

        static bool Seen(string id) { var gm = GameManager.I; return gm != null && gm.GetMemo(MemoPrefix + id, 0f) >= 1f; }
        static readonly System.Collections.Generic.KeyValuePair<string, string>[] StallTour =
        {
            new System.Collections.Generic.KeyValuePair<string, string>("stall.cargo", "내 짐이에요. [올리기]로 좌판에 진열해요\n진열한 짐은 수레 무게에서 빠져요"),
            new System.Collections.Generic.KeyValuePair<string, string>("stall.shelf", "진열한 물건이에요. 한 시간마다 반쯤은 하나씩 팔려요\n값은 이 마을 장터 파는 값 그대로예요"),
            new System.Collections.Generic.KeyValuePair<string, string>("stall.earn", "판 돈은 여기 모여요. 들를 때 [받기]로 챙겨요"),
            new System.Collections.Generic.KeyValuePair<string, string>("stall.rush", "하루 한 번, 직접 손님을 맞을 수 있어요 (7시~21시)\n손님이 바라는 물건을 빨리 건네면 더 비싸게 팔려요"),
        };

        static void MarkSeen(string id) { var gm = GameManager.I; if (gm != null) gm.SetMemo(MemoPrefix + id, 1f); }

        static bool AnyPress()
        {
#if ENABLE_INPUT_SYSTEM
            // Esc는 빼고 (메뉴가 열리는 키라 어둠만 걷고 메뉴도 여는 일이 없게)
            var k = Keyboard.current;
            if (k != null && k.anyKey.wasPressedThisFrame && !k.escapeKey.wasPressedThisFrame) return true;
            var m = Mouse.current;
            if (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)) return true;
            var g = Gamepad.current;
            return g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame || g.dpad.left.wasPressedThisFrame || g.dpad.right.wasPressedThisFrame);
#else
            return Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        Spot? cur, last;   // last = 마지막으로 밝힌 것 (단계가 끝난 뒤 어둠이 천천히 걷히는 동안 그린다)

        void Update()
        {
            cur = Find();
            if (cur != null) last = cur;
            float dt = Time.unscaledDeltaTime;
            if (cur == null) { shownId = null; dimWanted = false; dim = Mathf.MoveTowards(dim, 0f, dt / FadeOut); return; }
            var s = cur.Value;
            if (s.id != shownId) { shownId = s.id; dimWanted = !s.noDim && !Seen(s.id); }
            if (s.noDim) dimWanted = false;   // 가까이 와서 동그라미가 사라지면 어둠도 걷는다
            // 어둠이 다 내려온 뒤(잠깐) 누른 키만 받는다 (단계가 바뀌는 그 키로 바로 지나가지 않게)
            if (dimWanted && dim > .6f && AnyPress()) { dimWanted = false; MarkSeen(s.id); }
            dim = dimWanted ? Mathf.MoveTowards(dim, 1f, dt / FadeIn) : Mathf.MoveTowards(dim, 0f, dt / FadeOut);
        }

        /// <summary>지금 밝힐 것 (없으면 null)</summary>
        static Spot? Find()
        {
            var gm = GameManager.I;
            if (gm == null || StartScreen.IsOpen || gm.IsTravelling || UIState.PhotoMode || MapRoot.Current == null) return null;
            bool blocking = UIState.Blocking;
            if (HaggleDuel.IsOpen || PackPuzzle.IsOpen || StallRush.IsOpen) return null;   // 장터·좌판 위에 뜨는 창

            // 11/19 신용 4·5단계 혜택: 장터 창에서 외상·좌판 단추를 처음 보면 설명 (아무 키나 = 다음)
            if (!Seen("market.loan") && ScreenZones.Live("market.loan"))
            { holdFrame = Time.frameCount; return Zone("market.loan", "market.loan", $"신용 4단계 혜택 외상이에요. 장터 상인이 돈을 먼저 빌려줘요 (지금 한도 {Loan.Limit(gm.Player)}냥)\n사흘 안에 아무 장터에서 갚아요. 넘기면 신용이 깎이고 1할이 붙어요", false); }
            if (!Seen("market.stall") && ScreenZones.Live("market.stall"))
            { holdFrame = Time.frameCount; return Zone("market.stall", "market.stall", $"신용 5단계: 이 마을에 내 좌판을 열 수 있어요 ({PlayerStall.Price}냥, 마을과 우호 이상)\n짐을 올려 두면 내가 떠나 있는 동안에도 조금씩 팔려요", false); }
            // 내 좌판 창을 처음 열면 하나씩 소개
            if (StallWindow.Showing)
            {
                for (int i = 0; i < StallTour.Length; i++)
                    if (!Seen(StallTour[i].Key) && ScreenZones.Live(StallTour[i].Key))
                    { holdFrame = Time.frameCount; return Zone(StallTour[i].Key, StallTour[i].Key, StallTour[i].Value, false); }
                return null;
            }

            // 11/18 은샘: 장부를 처음 열면 탭 셋을 하나씩 소개 (튜토리얼이 아니어도 처음 한 번. 아무 키나 누르면 다음 탭)
            if (TradeLedgerUI.Showing)
            {
                for (int i = 0; i < LedgerTour.Length; i++)
                    if (!Seen("ledger.tab" + i)) { holdFrame = Time.frameCount; return Zone("ledger.tab" + i, "ledger.tab" + i, LedgerTour[i], false); }
                return null;
            }
            // 11/19 은샘 "새 시스템·내용은 튜토리얼처럼": 게시판에 새 종류 의뢰가 처음 보이면 그 카드를 밝히고 설명 (아무 키나 = 다음)
            if (ContractBoard.Showing)
            {
                foreach (var key in ContractBoard.TourKeys)
                    if (!Seen("board.kind." + key) && ScreenZones.Live("board.kind." + key))
                    { holdFrame = Time.frameCount; return Zone("board.kind." + key, "board.kind." + key, ContractKind.Intro(key), false); }
            }
            // 11/19 여정 수첩 성장 탭의 운반 수단·신용 카드를 처음 보면 설명 (튜토리얼 수첩 단계가 아닐 때)
            if (QuestTracker.JournalOpen && !ControlLesson.IsActive(ControlLesson.Quests))
            {
                if (!Seen("journal.vehicle") && ScreenZones.Live("journal.vehicle"))
                { holdFrame = Time.frameCount; return Zone("journal.vehicle", "journal.vehicle", "#다음 운반 수단\n막대: 돈이 모이면 차요\n더 큰 수단: 짐을 더 많이 실어요", false); }
                if (!Seen("journal.credit") && ScreenZones.Live("journal.credit"))
                { holdFrame = Time.frameCount; return Zone("journal.credit", "journal.credit", "#신용 단계\n배달·납품: 해내면 점수가 차요\n단계가 오르면: 게시판에 새 의뢰가 열려요", false); }
            }
            if (ControlLesson.IsActive(ControlLesson.Quests))
            {
                // 11/18 은샘: 열기 → 목록에서 튜토리얼 카드 보기(어둡게, 아무 키나) → 닫기. 닫으면 이 단계가 끝난다 (ControlLesson)
                if (QuestTracker.JournalOpen)
                {
                    if (!Seen("journal.tutorial") && ScreenZones.Live("journal.tutorial"))
                    {
                        holdFrame = Time.frameCount; journalTourShown = true;
                        return Zone("journal.tutorial", "journal.tutorial", "튜토리얼 할 일은 수첩 의뢰 목록 맨 위에도 있어요", false);
                    }
                    return Zone("journal.close", "journal.close", "다 봤으면 Esc나 이 단추로 닫아요", true);
                }
                if (blocking) return null;
                if (TourLeft(ControlLesson.Quests))   // 설명 중에 닫았다 → 다시 열게
                    return Zone("quests.again", "task.card", "설명이 남았어요. Q나 이 카드로 다시 열어요", false);
                return Zone("quests", "task.card", "Q나 이 카드로 여정 수첩을 열어요", false);
            }
            // 단추는 네모 테두리 (11/18 은샘: 동그라미보다 낫다)
            if (ControlLesson.IsActive(ControlLesson.Inventory))
                return blocking ? null : Zone("inventory", "hud.btn.i", "I나 이 단추로 소지품을 열어요", false);
            if (ControlLesson.IsActive(ControlLesson.Map))
                return blocking ? null : Zone("map", "hud.btn.m", "M이나 이 단추로 지도를 펼쳐요", false);
            if (ControlLesson.IsActive(ControlLesson.Ledger))
            {
                if (blocking) return null;
                if (Seen("ledger.tab0") && TourLeft(ControlLesson.Ledger))   // 탭 설명 중에 닫았다 → 다시 열면 이어서
                    return Zone("ledger.again", "hud.btn.l", "탭 설명이 남았어요. L을 다시 눌러 이어서 봐요", false);
                return Zone("ledger", "hud.btn.l", "L이나 이 단추로 장부를 열어요", false);
            }
            bool buy = ControlLesson.IsActive(ControlLesson.Bought), sell = ControlLesson.IsActive(ControlLesson.Sold);
            // 11/18 은샘: 시계도 튜토리얼에서 (사기 단계 처음, 장터 창이 열리기 전에).
            // 시계 누르기 → 펼친 달력 설명 → 다시 눌러 닫기 → ︿로 위로 접기까지 하면 다음으로
            if (buy && IntroActive && !Seen("clock.done"))
            {
                if (!Seen("clock.seen"))
                {
                    if (HudClock.IsOpen)
                    {
                        clockOpened = true;
                        return Zone("clock.open", "hud.clock", "#시계 보는 법\n빨간 글씨: 다음 장날까지 남은 날\n큰 장 마을: 누르면 지도에서 보여요\n▶다 봤으면 시계를 다시 눌러 닫아요", false);   // 11/19 윤채: 세 줄 긴 글 → 제목 · 짧은 줄 · 할 일
                    }
                    if (clockOpened) { clockOpened = false; MarkSeen("clock.seen"); }   // 펼쳤다가 닫았다 → 접기 안내로
                    else if (blocking) return null;
                    else if (HudClock.Folded && ScreenZones.Live("hud.clock.tab"))   // 미리 접어 두었으면 먼저 펴게
                        return Zone("clock.unfold", "hud.clock.tab", "접힌 시계예요. 눌러서 펴 보세요", false);
                    else if (ScreenZones.Live("hud.clock.btn"))
                        return Zone("clock", "hud.clock.btn", "시계를 눌러 보세요. 다음 장날과 큰 장 마을이 보여요", false);
                }
                if (Seen("clock.seen"))
                {
                    if (HudClock.Folded) MarkSeen("clock.done");   // 접었다 → 다음 단계
                    else if (blocking) return null;
                    else if (ScreenZones.Live("hud.clock.fold"))
                        return Zone("clock.fold", "hud.clock.fold", "#시계 접기\n이 단추: 시계를 위로 접어요\n▶다시 볼 땐 맨 위 작은 탭을 눌러요", false);
                }
            }
            if (buy || sell)
            {
                // 튜토리얼이 이 맵의 장터를 가리킬 때만 (팔기는 산 마을이 아닌 곳이라야 한다: 그 판단은 Intro의 길 안내가 한다)
                var h = Navigator.Tutorial;
                // 11/18 은샘: 사고 나면 장터 창 닫기도 밝힌다 (팔기는 다른 마을이라 이 창에서는 할 일이 없다)
                if (sell && ScreenZones.Live("market.window") && ScreenZones.Live("market.close")
                    && (!h.HasValue || !(h.Value.target is TownMarket hm) || !ScreenZones.Live("market.window." + (hm.town != null ? hm.town.id : ""))))
                    return Zone("market.close", "market.close", "다 샀으면 Esc나 이 단추로 장터를 닫아요", true);
                if (!h.HasValue || !(h.Value.target is TownMarket m)) return null;
                if (ScreenZones.Live("market.window"))
                {
                    if (!ScreenZones.Live("market.window." + (m.town != null ? m.town.id : ""))) return null;   // 다른 장터 창 (팔기는 산 마을이 아닌 곳이라야 한다)
                }
                if (ScreenZones.Live("market.window"))
                {
                    if (buy) return Zone("buy", "market.buy", "Space나 이 단추로 하나 사 봐요", false);
                    return Zone("sell", "market.sell", "Space나 이 단추로 팔아 봐요", false) ?? Zone("sell.tab", "market.sellTab", "\"팔기\" 칸을 눌러요", false);
                }
                if (blocking) return null;
                string who = h.Value.label ?? "장터 상인";
                // 11/18 은샘: 다른 마을에서 팔기는 어둡게 하지 않는다 (동그라미·말풍선만)
                return World(buy ? "buy.market" : "sell.market", m, buy ? $"{who}에게 Space로 말 걸어 사기" : $"{who}에게 Space로 말 걸어 팔기", sell);
            }
            if (blocking || !IntroActive) return null;   // 시작 튜토리얼일 때만 (만국 상인회 이야기도 길 안내를 쓰지만 여기서는 밝히지 않는다)
            var hint = Navigator.Tutorial;
            if (hint.HasValue && hint.Value.target != null && !(hint.Value.target is TownMarket) && !string.IsNullOrEmpty(hint.Value.label))
            {
                // 배우기를 다 하고 안내 인물에게 돌아갈 때는 따로 (처음 한 번 어둡게)
                bool back = Seen("quests") || Seen("ledger");
                return World(back ? "guide.back" : "guide", hint.Value.target, back ? $"{hint.Value.label}에게 돌아가 Space로 말 걸기" : $"{hint.Value.label}에게 다가가 Space로 말 걸기", back);   // 돌아갈 때도 어둡게 하지 않는다
            }
            return null;
        }

        static readonly string[] LedgerTour =
        {
            "#내 짐 판매처\n실은 물건을 어느 장터에 팔면 이득인지 보여 줘요",   // 11/19 제목 + 한 줄 (안내 카드)
            "#전체 시세\n장터에서 직접 본 사는 값·파는 값을 비교해요",
            "#최근 거래\n최근에 사고판 기록과 남긴 돈이에요",
        };
        /// <summary>지금 밝히는 것의 아이디 (없으면 null). 장부가 소개 중인 탭을 펼치는 데 쓴다</summary>
        public static string CurrentId => inst != null && inst.cur.HasValue ? inst.cur.Value.id : null;

        static bool clockOpened;   // 튜토리얼 시계 단계: 달력을 한 번 펼쳤다 (닫으면 끝)
        static bool IntroActive => GoryeoIntro.Active || VeniceIntro.Active || NorthIntro.Active || ArabiaIntro.Active;

        static Spot? Zone(string id, string zone, string bubble, bool round)
        {
            var r = ScreenZones.Get(zone);
            if (!r.HasValue) return null;
            var h = r.Value;
            float pad = round ? 3f : 2f;
            return new Spot { id = id, hole = new Rect(h.x - pad, h.y - pad, h.width + pad * 2f, h.height + pad * 2f), round = round, bubble = bubble };
        }

        /// <summary>맵 위 사람: 그림 둘레 동그라미 (화면 밖이면 null)</summary>
        static Spot? World(string id, Component target, string bubble, bool noDim = false)
        {
            var cam = Camera.main;
            if (cam == null || target == null) return null;
            // 11/18 은샘: 동그라미 안으로 들어오면(말 걸 수 있을 만큼 가까우면) 동그라미는 사라지고 말풍선만 남는다 (발밑 노란 고리 · Space 알약이 이어 받는다)
            var pc = PlayerController.Instance;
            bool near = pc != null && (pc.TalkTarget == target || ((Vector2)(pc.transform.position - target.transform.position)).sqrMagnitude < 1.6f * 1.6f);
            Bounds b;
            var sr = target.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null) b = sr.bounds; else b = new Bounds(target.transform.position + Vector3.up * .5f, new Vector3(1f, 1.5f, 0f));
            Vector2 a = ToGui(cam, b.min), z = ToGui(cam, b.max);
            var r = Rect.MinMaxRect(Mathf.Min(a.x, z.x), Mathf.Min(a.y, z.y), Mathf.Max(a.x, z.x), Mathf.Max(a.y, z.y));
            float s = Mathf.Max(r.width, r.height) * .5f + 9f;
            var c = r.center;
            var hole = new Rect(c.x - s * 1.15f, c.y - s, s * 2.3f, s * 2f);
            if (hole.xMax < 0 || hole.yMax < 0 || hole.x > UIStyle.VirtualWidth || hole.y > UIStyle.VirtualHeight) return null;
            return new Spot { id = id, hole = hole, round = true, bubble = bubble, noDim = noDim || near, noRing = near, world = true };
        }

        static Vector2 ToGui(Camera cam, Vector3 world)
        {
            var p = cam.WorldToScreenPoint(world);
            float k = UIStyle.Scale;
            return new Vector2(p.x / k, (Screen.height - p.y) / k);
        }

        // ---------------- 그리기 ----------------

        void OnGUI()
        {
            GUI.depth = -60;   // 장터 창(-10)보다 위. 깊이는 모든 이벤트에서 정해야 순서가 맞는다
            if (Event.current.type != EventType.Repaint) return;
            var s = cur;
            if (s == null && (dim <= 0f || last == null)) return;
            UIStyle.Begin();
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight, now = Time.unscaledTime;
            var sp = (s ?? last).Value;
            var hole = new Rect(Mathf.Round(sp.hole.x), Mathf.Round(sp.hole.y), Mathf.Round(sp.hole.width), Mathf.Round(sp.hole.height));
            if (dim > 0f) Darken(hole, sp.round, W, H, new Color(CuteUI.Ink.r / 255f, CuteUI.Ink.g / 255f, CuteUI.Ink.b / 255f, DimAlpha * dim));
            if (s == null) return;   // 단계가 끝났다: 어둠만 걷히는 중

            // 깜빡이는 테두리: 흰 줄 밖 + 청록 줄 안, 조금씩 커졌다 작아진다
            float p = .5f + .5f * Mathf.Sin(now * 5f);
            float g = 1f + Mathf.Round(p * 2f);
            if (!sp.noRing)
            {
                // 11/18 은샘: 맵 위 사람 동그라미는 UI(시계·카드·단추 등)를 덮지 않는다 (UI 자리는 빼고 그린다)
                if (sp.world) ScreenZones.UiRects(clip); else clip.Clear();
                Band(hole, sp.round, g, g + 2f, Color.white);
                Band(hole, sp.round, g + .5f, g + 1.5f, (Color)CuteUI.Interact);
                clip.Clear();
            }

            Bubble(hole, sp.bubble, g + 2f, W, H);
            if (dim > .3f)
            {
                var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01((dim - .3f) / .5f));
                string t = "아무 키나 누르면 계속";
                float tw = CuteUI.Width(t, true) + 16f;
                var r = new Rect(Mathf.Round(W - tw - 8f), Mathf.Round(H - 24f), Mathf.Round(tw), 16f);
                CuteUI.Box(r, 5, CuteUI.Paper, CuteUI.Ink, 1);
                CuteUI.LabelCenter(r.center.x, r.center.y, t, CuteUI.Text, true);
                GUI.color = old;
            }
        }

        /// <summary>한 색 네모 (CuteUI.Fill은 색마다 글자 열쇠를 만들어서, 줄마다 그리는 여기서는 흰 그림 + GUI.color로)</summary>
        static void Fill(Rect r, Color c)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            var old = GUI.color; GUI.color = c * old;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>도형의 y줄 반너비 (e만큼 넓힌 도형, 그 줄에 없으면 -1). 둥근 것 = 타원, 아니면 모서리 둥근 네모</summary>
        static float HalfWidth(Rect h, bool round, float e, float y)
        {
            float cx = h.center.x, cy = h.center.y, rx = h.width / 2f + e, ry = h.height / 2f + e;
            float dy = y - cy;
            if (Mathf.Abs(dy) > ry) return -1f;
            if (round) return rx * Mathf.Sqrt(Mathf.Max(0f, 1f - dy * dy / (ry * ry)));
            float R = Mathf.Min(6f + e, ry);
            float fromEdge = ry - Mathf.Abs(dy);
            if (fromEdge >= R) return rx;
            float k = R - fromEdge;
            return rx - (R - Mathf.Sqrt(Mathf.Max(0f, R * R - k * k)));
        }

        /// <summary>목표만 남기고 어둡게: 바깥은 네모 넷, 목표 줄들은 한 줄씩 좌우만</summary>
        static void Darken(Rect h, bool round, float W, float H, Color c)
        {
            Fill(new Rect(0, 0, W, Mathf.Max(0, h.y)), c);
            Fill(new Rect(0, h.yMax, W, Mathf.Max(0, H - h.yMax)), c);
            Fill(new Rect(0, h.y, Mathf.Max(0, h.x), h.height), c);
            Fill(new Rect(h.xMax, h.y, Mathf.Max(0, W - h.xMax), h.height), c);
            for (float y = h.y; y < h.yMax; y += 1f)
            {
                float hw = HalfWidth(h, round, 0f, y + .5f);
                if (hw < 0f) { Fill(new Rect(h.x, y, h.width, 1f), c); continue; }
                float l = Mathf.Round(h.center.x - hw), r = Mathf.Round(h.center.x + hw);
                if (l > h.x) Fill(new Rect(h.x, y, l - h.x, 1f), c);
                if (r < h.xMax) Fill(new Rect(r, y, h.xMax - r, 1f), c);
            }
        }

        static readonly List<Rect> clip = new List<Rect>();
        /// <summary>한 줄(높이 1) 칠하기. clip에 든 UI 자리는 비운다</summary>
        static void RowFill(Rect r, Color c)
        {
            if (clip.Count == 0) { Fill(r, c); return; }
            float y = r.y + .5f, x0 = r.x, x1 = r.xMax;
            for (int pass = 0; pass < 8 && x1 > x0; pass++)
            {
                bool cut = false;
                foreach (var u in clip)
                {
                    if (y < u.y || y >= u.yMax || u.xMax <= x0 || u.x >= x1) continue;
                    if (u.x > x0) Fill(new Rect(x0, r.y, u.x - x0, 1f), c);
                    x0 = Mathf.Max(x0, u.xMax); cut = true; break;
                }
                if (!cut) break;
            }
            if (x1 > x0) Fill(new Rect(x0, r.y, x1 - x0, 1f), c);
        }

        /// <summary>도형 둘레 띠 (안쪽 e0 ~ 바깥 e1)</summary>
        static void Band(Rect h, bool round, float e0, float e1, Color c)
        {
            float top = h.y - e1, bot = h.yMax + e1;
            for (float y = Mathf.Floor(top); y < bot; y += 1f)
            {
                float wo = HalfWidth(h, round, e1, y + .5f);
                if (wo < 0f) continue;
                float wi = HalfWidth(h, round, e0, y + .5f);
                float cx = h.center.x;
                if (wi < 0f) { RowFill(new Rect(Mathf.Round(cx - wo), y, Mathf.Round(wo * 2f), 1f), c); continue; }
                float lo = Mathf.Round(cx - wo), li = Mathf.Round(cx - wi), ri = Mathf.Round(cx + wi), ro = Mathf.Round(cx + wo);
                if (li > lo) RowFill(new Rect(lo, y, li - lo, 1f), c); else RowFill(new Rect(lo, y, 1f, 1f), c);
                if (ro > ri) RowFill(new Rect(ri, y, ro - ri, 1f), c); else RowFill(new Rect(ro - 1f, y, 1f, 1f), c);
            }
        }

        /// <summary>목표 아래(자리가 없으면 위) 말풍선 + 목표를 가리키는 꼬리</summary>
        /// <summary>
        /// 목표 옆 말풍선. 한 줄이면 가운데 한 줄.
        /// 11/19 윤채 ("아래 뜨는 안내에 글이 너무 많아 보기 어렵다"): 여러 줄("\n")은 안내 카드로 —
        ///   "#"로 시작하는 첫 줄 = 굵은 제목(전구 그림) · 보통 줄 = 점 목록(":" 앞은 굵게) · "▶"로 시작하는 줄 = 할 일(청록 굵게). 왼쪽 맞춤
        /// </summary>
        static void Bubble(Rect hole, string text, float gap, float W, float H)
        {
            if (string.IsNullOrEmpty(text)) return;
            string[] lines = text.Split('\n');
            string title = lines.Length > 1 && lines[0].StartsWith("#") ? lines[0].Substring(1).Trim() : null;
            var rows = new List<(string t, bool act)>();
            for (int i = title != null ? 1 : 0; i < lines.Length; i++)
            {
                string l = lines[i].Trim();
                if (l.Length == 0) continue;
                bool act = l.StartsWith("▶");
                rows.Add((act ? l.Substring(1).Trim() : l, act));
            }
            bool card = lines.Length > 1;
            float tw, bh;
            if (!card) { tw = 16f + CuteUI.Width(text, false); bh = 20f; }
            else
            {
                tw = title != null ? CuteUI.Width(title, false, true) + 14f : 0f;
                foreach (var row in rows) tw = Mathf.Max(tw, CuteUI.Width(row.t, false, true) + 10f);
                tw += 24f;
                bh = 10f + (title != null ? 22f : 0f) + rows.Count * 15f + 4f;
            }
            bool below = hole.yMax + gap + 10f + bh < H - 28f;
            float by = below ? hole.yMax + gap + 9f : hole.y - gap - 9f - bh;
            float bx = Mathf.Clamp(hole.center.x - tw / 2f, 6f, W - tw - 6f);
            var r = new Rect(Mathf.Round(bx), Mathf.Round(by), Mathf.Round(tw), bh);
            CuteUI.Box(r, 6, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            float tx = Mathf.Round(Mathf.Clamp(hole.center.x, r.x + 10f, r.xMax - 10f));
            for (int i = 0; i < 6; i++)   // 꼬리: 말풍선 쪽이 넓은 세모 (테두리 잉크)
            {
                float w = (6 - i) * 2f - 1f;
                float y = below ? r.y - 1f - i : r.yMax + i;
                CuteUI.Fill(new Rect(tx - w / 2f - 1f, y, w + 2f, 1f), CuteUI.Ink);
                if (w > 1f) CuteUI.Fill(new Rect(tx - w / 2f, y, w, 1f), CuteUI.Paper);
            }
            if (!card) { CuteUI.LabelCenter(r.center.x, r.center.y, text, CuteUI.Text, false); return; }

            float x = r.x + 12f, yy = r.y + 7f;
            if (title != null)
            {
                var bp = CuteUI.PicSize(CuteUI.Pic.Bulb);
                CuteUI.Icon(CuteUI.Pic.Bulb, x, yy + 7f - bp.y / 2f, 1, CuteUI.Deco);
                CuteUI.Label(x + bp.x + 5f, yy + 7f, title, CuteUI.Text, false, true);
                yy += 17f;
                for (float dx = 0; dx < r.width - 24f; dx += 4f) CuteUI.Fill(new Rect(x + dx, yy, 2f, 1f), CuteUI.Line);
                yy += 5f;
            }
            foreach (var row in rows)
            {
                float cy = yy + 7f;
                if (row.act)
                {
                    CuteUI.Pointer(x + Mathf.Floor((Time.unscaledTime * 2.5f) % 2f), cy, CuteUI.Interact);
                    CuteUI.Label(x + 9f, cy, row.t, CuteUI.Interact, false, true);
                }
                else
                {
                    CuteUI.Fill(new Rect(x + 1f, cy - 1f, 3f, 3f), CuteUI.Frame);
                    int c = row.t.IndexOf(": ", System.StringComparison.Ordinal);
                    if (c > 0)
                    {
                        float hx = CuteUI.Label(x + 9f, cy, row.t.Substring(0, c), CuteUI.Text, false, true);
                        CuteUI.Label(hx + 4f, cy, row.t.Substring(c + 2), CuteUI.Sub, false);
                    }
                    else CuteUI.Label(x + 9f, cy, row.t, CuteUI.Text, false);
                }
                yy += 15f;
            }
        }
    }
}
