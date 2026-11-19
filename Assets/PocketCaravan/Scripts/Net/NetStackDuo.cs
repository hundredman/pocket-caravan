using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 상인회 짐 쌓기 · 둘이서 (2026-11-18, 팀 UT 1번 "같이 할 수 있는 미니게임").
    /// - 놀이판 앞 Space → [혼자] [둘이서]. 둘이서: 옆(6칸 안)에 접속한 사람이 있으면 그 사람에게 같이 하자고 하고,
    ///   없으면(혼자 접속·오프라인) 상인회 파리드가 짝이 되어 번갈아 놓는다 (가끔 조금 빗나간다).
    /// - 같이 하자는 말을 받은 사람은 알림을 보고 놀이판에서 Space(또는 둘이서)를 누르면 바로 시작. 서로 동시에 둘이서를 눌러도 시작.
    /// - 번갈아 하나씩 놓는다 (같이 하자고 한 사람이 먼저). 내 짐은 청록 궤짝, 짝의 짐은 붉은 상자. 놓는 사람 이름이 짐 위에 뜬다.
    /// - 둘이서는 혼자 5단을 넘긴 뒤(수첩 도장이나 혼자 최고 5단 이상)에 열린다. 그 전에는 창을 열면 바로 혼자 판.
    ///   그래서 수첩 도장은 늘 혼자 쌓아서 받는다 (둘이서 판은 도장을 찍지 않는다). 아직 안 열린 사람이 같이 하자는 말을 받으면 "아직"으로 답한다.
    /// - 돈은 주지 않는다 (11/18 검토: 하루가 현실 6분이라 하루 상한을 둬도 시간당 수입이 교역의 절반 가까이 되고, 파리드와 혼자 되풀이할 수 있다).
    ///   접속한 사람과 둘이 쌓은 기록은 순위표 "guild_stack_duo"에 (파리드와 쌓은 판은 올리지 않는다). 둘이 최고 기록은 따로 남긴다.
    /// - 통신: 서버를 고치지 않고 리액션(emote) 통로에 싣는다. 같은 맵에만 가고, 한 사람이 0.4초에 하나라 줄을 세워 0.45초 간격으로 보낸다.
    ///   신호 = "sgI_받는이"(같이 하자) · "sgA_받는이"(좋아) · "sgQ_받는이"(그만) · "sgL_받는이"(아직 안 열림) · "sgD_받는이_몇번째_x×10"(놓음).
    ///   서버가 0.4초 안에 겹친 리액션을 버릴 수 있어서, 기다리는 쪽에 닿을 때까지 "같이 하자"는 2초마다, 마지막 놓음은 1.5초마다 다시 보낸다 (받는 쪽은 겹친 것을 건너뛴다).
    ///   놓은 자리만 보내고 탑은 양쪽이 같은 계산으로 쌓는다. 리액션으로 보이지 않게 RemotePlayers·NetGuild는 IsSignal을 건너뛴다.
    /// - 짝이 맵을 떠나거나 25초 동안 놓지 않으면 끝. 10초가 넘으면 짐 위에 남은 초를 보인다.
    /// 11/18 다듬기: 접속한 짝의 차례에는 짐을 흐리게 띄워 두고 "루나 고르는 중…" (내 화면의 짐은 짝 화면과 위치가 달라 놓을 때 튀었다).
    ///   짝이 놓으면 그 자리에서 툭 떨어진다. 짝이 맞추면 "루나 딱!". 판 중 1 박수 · 2 하트 (짝 화면 이름표 위에 뜬다, 파리드는 따라 한다).
    ///   옆에 접속한 사람이 여럿이면 고르는 화면에서 Tab으로 같이 할 사람을 바꾼다 (가까운 순).
    /// 11/19 짝 차례에도 짐이 오가 보이게 (흐린 짐이 멈춰 있어 상대가 하는지 안 보였다):
    ///   짐이 오가는 길은 양쪽이 같다 (차례 시작 자리·방향·속도). 그래서 짝 차례의 짐은 차례가 시작된 뒤 지난 시간으로 그린다 (PathX).
    ///   짝 화면은 신호가 닿은 만큼 늦게 출발하므로 그만큼(lag) 늦춰 그리고, 짝이 놓은 자리로 짝이 몇 초에 놓았는지 거꾸로 셈해 lag를 맞춘다.
    ///   그래도 남는 차이는 짝이 놓았다는 신호가 오면 SlideTime 동안 놓은 자리로 미끄러진 뒤 떨어뜨려 튀지 않게 한다.
    /// </summary>
    public partial class NetStackGame
    {
        enum Mode { Solo, Npc, Online }

        public const string DuoBoard = "guild_stack_duo";
        const string DuoBestKey = "net:guild:stackduo:best";
        const float InviteRange = 6f, InviteLife = 20f, TurnLimit = 25f, SendGap = .6f, ResendGap = 1.5f, ResendAfterEnd = 6f;
        const string NpcName = "파리드";

        Mode mode;
        string partnerId, partnerName, endWhy;
        bool iFirst;
        float turnAt, waitAt, npcAt, npcErr;
        bool justUnlocked, lockedAtStart;
        int pick;                                    // 고르는 화면: 같이 하자고 할 사람 (가까운 순 몇 번째)
        string myReact, partnerReact, npcReact;      // 판 중 리액션 (이름표 위 말풍선)
        float myReactAt = -9f, partnerReactAt = -9f, npcReactAt = -9f;
        // 11/19 짝 차례 짐: 차례 시작 방향, 놓은 자리로 미끄러지기
        const float SlideTime = .15f, LagStart = .3f, LagMax = 1.2f;
        static float lag = LagStart;   // 짝 화면이 내 화면보다 늦게 출발하는 시간 (짝이 놓을 때마다 맞춘다)
        float startDir = 1f, slideFrom, slideTo, slideAt;
        bool sliding;
        string lastDrop;      // 내가 마지막으로 보낸 놓음 신호 (짝이 놓을 때까지 1.5초마다 다시 보낸다: 서버가 0.4초 안에 겹친 리액션을 버릴 수 있어서)
        float lastDropSent;

        // 받은 "같이 하자" (창이 닫혀 있어도 기억한다)
        static string invFrom, heardId;
        static float invAt = -99f, heardAt = -99f;
        // 보낼 신호 줄
        static readonly Queue<string> outq = new Queue<string>();
        static float lastSent = -9f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { invFrom = heardId = null; invAt = heardAt = -99f; outq.Clear(); lastSent = -9f; lag = LagStart; }   // 플레이 모드를 다시 켜도 (도메인 다시 읽기 꺼짐)

        static readonly Color32 MineCol = new Color32(80, 178, 168, 255), PartnerCol = new Color32(232, 104, 96, 255);

        /// <summary>짐 쌓기 신호인가 (리액션으로 보이지 않게 건너뛴다)</summary>
        public static bool IsSignal(string e) => e != null && e.Length > 3 && e[0] == 's' && e[1] == 'g' && e[3] == '_';

        /// <summary>둘이서가 열렸나: 짐 쌓기 수첩 도장을 받았거나 혼자 최고 5단 이상</summary>
        public static bool DuoUnlocked => NetGuild.StackStamped || Best >= Goal;

        static int DuoBest => GameManager.I == null ? 0 : Mathf.RoundToInt(GameManager.I.GetMemo(DuoBestKey, 0f));

        /// <summary>지금 놓을 차례가 나인가 (혼자면 늘)</summary>
        bool MyTurn => mode == Mode.Solo || IsMine(tower.Count);

        /// <summary>level 층(1 = 수레 위 첫 짐)을 내가 놓았나</summary>
        bool IsMine(int level) => ((level - 1) % 2 == 0) == iFirst;

        int LookOf(int level, int fallback) => mode == Mode.Solo || level < 1 ? fallback : IsMine(level) ? 2 : 3;

        static void Hook()
        {
            NetClient.Received -= OnNet;
            NetClient.Received += OnNet;
        }

        void LateUpdate() => Flush();

        static void Signal(string e)
        {
            var net = NetClient.I;
            if (net == null || net.Status != NetClient.State.Online) return;
            outq.Enqueue(e);
            Flush();
        }

        static void Flush()
        {
            if (outq.Count == 0 || Time.unscaledTime - lastSent < SendGap) return;
            var net = NetClient.I;
            if (net == null || net.Status != NetClient.State.Online) { outq.Clear(); return; }
            lastSent = Time.unscaledTime;
            net.Emote(outq.Dequeue(), "짐 쌓기");   // 예전 게임에서는 "짐 쌓기" 리액션으로 보인다
        }

        static bool PeerHere(string id)
        {
            var net = NetClient.I;
            return net != null && id != null && net.Peers.ContainsKey(id);
        }

        static string PeerName(string id)
        {
            var net = NetClient.I;
            return net != null && id != null && net.Peers.TryGetValue(id, out var p) && !string.IsNullOrEmpty(p.name) ? p.name : "접속한 상인";
        }

        static bool InviteFresh => invFrom != null && Time.unscaledTime - invAt < InviteLife && PeerHere(invFrom);

        /// <summary>내 옆(6칸 안)의 접속한 사람들 (가까운 순)</summary>
        static List<string> NearPeers()
        {
            var list = new List<string>();
            var net = NetClient.I; var pc = PlayerController.Instance;
            if (net == null || pc == null || net.Status != NetClient.State.Online) return list;
            var dist = new Dictionary<string, float>();
            foreach (var kv in net.Peers)
            {
                if (!RemotePlayers.TryGetPosition(kv.Key, out var pos)) continue;
                float d = ((Vector2)pos - (Vector2)pc.transform.position).sqrMagnitude;
                if (d <= InviteRange * InviteRange) { dist[kv.Key] = d; list.Add(kv.Key); }
            }
            list.Sort((x, y) => dist[x].CompareTo(dist[y]));
            return list;
        }

        /// <summary>같이 하자고 할 사람 (Tab으로 고른 사람, 없으면 null)</summary>
        string PickedPeer(out int count)
        {
            var list = NearPeers();
            count = list.Count;
            return count == 0 ? null : list[((pick % count) + count) % count];
        }

        // ---------------- 받기 ----------------

        static void OnNet(NetClient.Msg m)
        {
            var net = NetClient.I;
            if (m.t != "emote" || net == null || string.IsNullOrEmpty(m.id) || m.id == net.MyId) return;
            if (!IsSignal(m.e))
            {
                // 같이 쌓는 짝의 리액션은 창 안 이름표 위에도
                if (inst != null && inst.open && inst.mode == Mode.Online && inst.partnerId == m.id)
                { inst.partnerReact = string.IsNullOrEmpty(m.name) ? "리액션" : m.name; inst.partnerReactAt = Time.unscaledTime; }
                return;
            }
            var part = m.e.Split('_');
            if (part.Length < 2 || part[1] != net.MyId) return;
            char kind = m.e[2];
            if (kind == 'I')
            {
                float now = Time.unscaledTime;
                bool repeat = heardId == m.id && now - heardAt < InviteLife;   // 같은 사람이 다시 보낸 것 → 알림은 한 번만
                heardId = m.id; heardAt = now;
                if (!DuoUnlocked)
                {
                    Signal("sgL_" + m.id);
                    if (!repeat) Notify.Show($"{PeerName(m.id)} 님이 짐 쌓기를 같이 하재요 · 혼자 {Goal}단을 먼저 넘기면 둘이서가 열려요", 5f);
                    return;
                }
                // 이미 같이 시작했는데 다시 왔다 = 내 "좋아"가 닿지 않았다 → 다시 답한다
                if (inst != null && inst.open && inst.mode == Mode.Online && inst.partnerId == m.id && inst.state == State.Play && inst.tower.Count == 1)
                { Signal("sgA_" + m.id); return; }
                invFrom = m.id; invAt = now;
                if (inst == null || !inst.open)
                {
                    if (!repeat) { Notify.Show($"{PeerName(m.id)} 님이 짐 쌓기를 같이 하재요 · 상인회 마당 놀이판에서 Space", 5f); Sfx.Play("voice", .4f); }
                    return;
                }
                // 나도 같은 사람에게 같이 하자고 했다 → 바로 시작 (먼저 놓는 쪽은 번호 순)
                if (inst.state == State.Wait && inst.partnerId == m.id) { inst.StartOnline(m.id, string.CompareOrdinal(net.MyId, m.id) < 0); return; }
                if (repeat) return;
                Sfx.Play("voice", .4f);
                // 혼자 판을 열어 둔 채였다: 놓기 전이면 고르기 화면으로, 하는 중이면 알림 (끝나면 "다시"가 둘이서)
                if (inst.mode == Mode.Solo && inst.state == State.Ready) inst.Reset(State.Choose);
                else if (inst.state == State.Play || (inst.state == State.Over && inst.mode == Mode.Online && inst.partnerId != m.id))
                    Notify.Show($"{PeerName(m.id)} 님이 짐 쌓기를 같이 하재요 · 이 판이 끝나면 다시", 4f);
                return;
            }
            if (inst == null || !inst.open || inst.mode != Mode.Online || inst.partnerId != m.id) { if (kind == 'Q' && invFrom == m.id) invFrom = null; return; }
            if (kind == 'A' && inst.state == State.Wait) inst.StartOnline(m.id, true);
            else if (kind == 'L' && inst.state == State.Wait)
            {
                Notify.Show($"{inst.partnerName} 님은 아직 혼자 {Goal}단 전이에요 · 파리드와 해 볼까요?", 3.5f);
                inst.mode = Mode.Solo; inst.Reset(State.Choose);
            }
            else if (kind == 'Q')
            {
                if (invFrom == m.id) invFrom = null;
                if (inst.state == State.Play) { inst.endWhy = $"{inst.partnerName} 님이 그만뒀어요"; inst.End(); }
                else if (inst.state == State.Wait) { inst.state = State.Choose; Notify.Show($"{inst.partnerName} 님은 다음에 하재요", 3f); }
            }
            else if (kind == 'D' && inst.state == State.Play && !inst.MyTurn && !inst.sliding && part.Length >= 4
                     && int.TryParse(part[2], out int idx) && int.TryParse(part[3], out int x10) && idx == inst.tower.Count - 1)
                inst.PartnerLanded(Mathf.Clamp(x10 / 10f, 0f, AreaW - inst.moverW));   // 11/19 바로 떨어뜨리지 않고 놓은 자리로 미끄러진 뒤 (PartnerOnline)
        }

        // ---------------- 고르기 · 시작 ----------------

        /// <summary>창을 열 때: 누가 같이 하자고 했으면 바로 시작</summary>
        void JoinIfInvited()
        {
            if (!InviteFresh) return;
            Signal("sgA_" + invFrom);
            StartOnline(invFrom, false);
        }

        void StartOnline(string id, bool first)
        {
            mode = Mode.Online; partnerId = id; partnerName = PeerName(id); iFirst = first;
            if (invFrom == id) invFrom = null;
            endWhy = null; lastDrop = null; myReactAt = partnerReactAt = -9f; npcReact = null;
            Reset(State.Play);
            Pop(first ? "내가 먼저!" : $"{partnerName} 님 먼저");
            landAt = Time.unscaledTime;   // 첫 짐은 잠깐 쉬었다가 나온다 (알림이 이름표와 겹치지 않게)
            Sfx.Play("accept", .6f);
        }

        void ChooseSolo() { mode = Mode.Solo; Reset(State.Ready); Sfx.Play("move", .6f); }

        void ChooseDuo()
        {
            endWhy = null;
            if (InviteFresh) { JoinIfInvited(); return; }
            var peer = PickedPeer(out _);
            if (peer != null)
            {
                mode = Mode.Online; partnerId = peer; partnerName = PeerName(peer);
                Signal("sgI_" + peer);
                state = State.Wait; waitAt = lastDropSent = Time.unscaledTime;
                Sfx.Play("move", .6f);
                return;
            }
            mode = Mode.Npc; partnerId = null; partnerName = NpcName; iFirst = true;
            Reset(State.Ready);
            Sfx.Play("move", .6f);
        }

        /// <summary>끝난 화면의 "다시"·Space</summary>
        void Again()
        {
            endWhy = null; justUnlocked = false;
            if (DuoUnlocked && InviteFresh && (mode != Mode.Online || invFrom != partnerId)) { JoinIfInvited(); return; }   // 다른 사람이 같이 하자고 했다
            if (mode != Mode.Online) { Reset(State.Play); return; }
            if (InviteFresh && invFrom == partnerId) { Signal("sgA_" + partnerId); StartOnline(partnerId, false); return; }
            if (!PeerHere(partnerId)) { Reset(State.Choose); mode = Mode.Solo; return; }
            Signal("sgI_" + partnerId);
            state = State.Wait; waitAt = lastDropSent = Time.unscaledTime;
        }

        /// <summary>창을 닫을 때: 같이 하던 사람에게 그만한다고 알린다</summary>
        void DuoQuit()
        {
            if (mode == Mode.Online && state != State.Choose && partnerId != null) Signal("sgQ_" + partnerId);   // 끝난 화면에서 닫아도 알린다 (한 판 더 기다리지 않게)
        }

        void DuoUpdate()
        {
            float now = Time.unscaledTime;
            if (state == State.Choose && InputReader.StatusPressed()) { pick++; Sfx.Play("move", .5f); }   // Tab: 같이 할 사람 바꾸기
            if (mode != Mode.Solo && (state == State.Play || state == State.Over))
            {
                int n = InputReader.NumberPressed();
                if (n == 1) React("clap", "박수");
                else if (n == 2) React("love", "하트");
            }
            if (npcReact != null && now >= npcReactAt) { partnerReact = npcReact; partnerReactAt = now; npcReact = null; }
            // 짝이 내 놓음을 못 받았을 수도 있다: 짝 차례이거나 방금 끝났으면 마지막 놓음을 다시 보낸다 (받는 쪽은 이미 쌓은 번호를 건너뛴다)
            if (mode == Mode.Online && lastDrop != null && Time.unscaledTime - lastDropSent > ResendGap
                && ((state == State.Play && !MyTurn) || (state == State.Over && Time.unscaledTime - overAt < ResendAfterEnd)))
            { lastDropSent = Time.unscaledTime; Signal(lastDrop); }
            // 기다리는 동안 "같이 하자"를 2초마다 다시 보낸다 (처음 것이 버려졌을 수 있다)
            if (state == State.Wait && Time.unscaledTime - lastDropSent > 2f) { lastDropSent = Time.unscaledTime; Signal("sgI_" + partnerId); }
            if (state == State.Wait && (Time.unscaledTime - waitAt > InviteLife || !PeerHere(partnerId)))
            {
                Signal("sgQ_" + partnerId);
                Notify.Show($"{partnerName} 님이 아직 안 왔어요 · 파리드와 해 볼까요?", 3f);
                mode = Mode.Solo; Reset(State.Choose);
            }
        }

        // ---------------- 차례 ----------------

        void TurnStarted()
        {
            turnAt = Time.unscaledTime;
            startDir = dir;   // 차례 시작 방향 (짐은 오가며 dir이 바뀐다)
            if (mode == Mode.Npc && state == State.Play && !MyTurn)
            {
                npcAt = turnAt + Random.Range(.5f, 1.2f);
                npcErr = Random.value < .35f ? 0f : Random.Range(2.5f, 8f) * (Random.value < .5f ? -1f : 1f);
            }
        }

        void LocalDrop()
        {
            if (mode == Mode.Online)
            {
                // 양쪽이 같은 자리로 계산하게: 0.1 단위로 내리고(오른쪽 끝을 넘지 않게) 그 값을 보낸다
                int x10 = Mathf.FloorToInt(Mathf.Clamp(moverX, 0f, AreaW - moverW) * 10f);
                moverX = x10 / 10f;
                lastDrop = $"sgD_{partnerId}_{tower.Count - 1}_{x10}";
                lastDropSent = Time.unscaledTime;
                Signal(lastDrop);
            }
            Drop();
        }

        void PartnerTurn(float dt)
        {
            float now = Time.unscaledTime;
            if (mode == Mode.Npc)
            {
                if (now < npcAt) return;
                var top = tower[tower.Count - 1];
                float target = Mathf.Clamp(top.x + npcErr, 0f, AreaW - moverW);
                if (Mathf.Abs(moverX - target) <= speed * dt * 1.2f + .5f) { moverX = target; Drop(); }
                return;
            }
            if (mode != Mode.Online) return;
            if (!PeerHere(partnerId)) { endWhy = $"{partnerName} 님이 떠났어요"; End(); }
            else if (now - turnAt > TurnLimit) { endWhy = $"{partnerName} 님이 한참 안 놓았어요"; Signal("sgQ_" + partnerId); End(); }
        }

        /// <summary>11/19 접속한 짝 차례 (Update에서 매 프레임): 짐을 짝 화면처럼 오가게 두고, 짝이 놓았으면 그 자리로 미끄러진 뒤 떨어뜨린다</summary>
        void PartnerOnline()
        {
            float now = Time.unscaledTime;
            if (sliding)
            {
                float k = (now - slideAt) / SlideTime;
                if (k >= 1f) { moverX = slideTo; sliding = false; Drop(); return; }
                k = 1f - (1f - k) * (1f - k);
                moverX = Mathf.Lerp(slideFrom, slideTo, k);
                return;
            }
            PartnerTurn(0f);   // 떠났나 · 너무 오래 안 놓았나
            if (state != State.Play) return;
            moverX = PathX(now - (landAt + Beat) - lag, out dir);
        }

        /// <summary>차례가 시작된 뒤 t초 지난 짐의 x (양 끝에서 튕기며 오간다). 짐이 오가는 길은 양쪽 화면이 같다</summary>
        float PathX(float t, out float d)
        {
            float span = AreaW - moverW;
            d = startDir;
            if (span <= 0f) return 0f;
            float m = Mathf.Repeat(Mathf.Max(0f, t) * speed, 2f * span), a = m;
            if (m > span) { a = 2f * span - m; d = -startDir; }
            return startDir > 0f ? a : span - a;
        }

        /// <summary>짝이 놓았다는 신호가 왔다: 짝이 차례 몇 초에 놓았는지 거꾸로 셈해 lag를 맞추고, 놓은 자리로 미끄러지기 시작</summary>
        void PartnerLanded(float x)
        {
            float now = Time.unscaledTime, span = AreaW - moverW, elapsed = now - (landAt + Beat);
            if (span > .5f && speed > 0f && elapsed > 0f)
            {
                // 같은 자리를 지나는 때는 왕복마다 두 번: 그중 지금 lag로 그린 때에 가장 가까운 것
                float a = startDir > 0f ? x : span - x, period = 2f * span / speed, t1 = a / speed, t2 = (2f * span - a) / speed;
                float want = elapsed - lag, best = -1f, bestErr = float.MaxValue;
                for (int k = 0; k < 4000 && k * period <= elapsed + .05f; k++)
                    for (int j = 0; j < 2; j++)
                    {
                        float c = k * period + (j == 0 ? t1 : t2), got = elapsed - c;
                        if (got < -.05f || got > LagMax + .3f) continue;
                        float err = Mathf.Abs(c - want);
                        if (err < bestErr) { bestErr = err; best = got; }
                    }
                if (best > -1f) lag = Mathf.Clamp(Mathf.Lerp(lag, best, .5f), 0f, LagMax);
            }
            slideFrom = moverX; slideTo = x; slideAt = now; sliding = true;
        }

        void React(string id, string name)
        {
            float now = Time.unscaledTime;
            if (now - myReactAt < .5f) return;
            myReact = name; myReactAt = now;
            Sfx.Play("voice", .35f);
            if (mode == Mode.Online) NetEmote.Send(id, name);   // 맵에 있는 사람들에게도 보인다
            else if (mode == Mode.Npc) { npcReact = name; npcReactAt = now + .6f; }   // 파리드가 따라 한다
        }

        // ---------------- 끝 ----------------

        void DuoEnd(GameManager gm)
        {
            if (gm != null && lastScore > DuoBest) { gm.SetMemo(DuoBestKey, lastScore); newBest = lastScore > 0; }
            if (mode == Mode.Online && lastScore > 0) NetScores.Report(DuoBoard, lastScore);
        }

        // ---------------- 그리기 ----------------

        void ChooseBanner(Rect a)
        {
            string small;
            if (InviteFresh) small = $"{PeerName(invFrom)} 님이 같이 하재요! 둘이서를 누르면 시작";
            else
            {
                var peer = PickedPeer(out int count);
                small = peer == null ? $"둘이서: 상인회 {NpcName}와 번갈아 쌓아요"
                    : count > 1 ? $"둘이서: {PeerName(peer)} 님에게 같이 하자고 해요 · Tab 다른 사람 ({count}명)"
                    : $"둘이서: {PeerName(peer)} 님에게 같이 하자고 해요";
            }
            Banner(a, "누구랑 쌓을까요?", small);
        }

        void WaitBanner(Rect a)
        {
            int left = Mathf.Max(0, Mathf.CeilToInt(InviteLife - (Time.unscaledTime - waitAt)));
            Banner(a, $"{partnerName} 님을 기다려요", $"{partnerName} 님이 놀이판에서 Space를 누르면 시작 · {left}초");
        }

        void DuoOverBanner(Rect a)
        {
            string big = newBest ? $"둘이 {lastScore}단! 새 기록" : $"둘이 {lastScore}단!";
            string small = endWhy;
            if (mode == Mode.Online && InviteFresh && invFrom == partnerId) small = $"{partnerName} 님이 한 판 더 하재요 · 다시";
            else if (small == null) small = mode == Mode.Online ? "둘이 기록은 순위표에 · 다시 해 볼까요?" : "다시 해 볼까요?";
            Banner(a, big, small);
        }

        void DuoButtons(Rect a, float by)
        {
            KeyNav.Begin(this);
            try
            {
                if (state == State.Choose)
                {
                    float bw = Mathf.Floor((AreaW - 6f) / 2f);
                    if (CuteUI.SoftButton(new Rect(a.x, by - 6f, bw, 20f), "혼자")) { KeyNav.Clear(this); ChooseSolo(); }
                    if (CuteUI.Button(new Rect(a.x + bw + 6f, by - 6f, bw, 20f), "둘이서")) { KeyNav.Clear(this); ChooseDuo(); }
                }
                else
                {
                    float bw = 80f;
                    if (CuteUI.SoftButton(new Rect(a.center.x - bw / 2f, by - 6f, bw, 20f), "그만"))
                    { KeyNav.Clear(this); Signal("sgQ_" + partnerId); mode = Mode.Solo; Reset(State.Choose); }
                }
            }
            finally { KeyNav.End(); }
        }

        /// <summary>놓을 짐 위: 누구 차례</summary>
        void DrawTurn(float my)
        {
            if (mode == Mode.Solo || state != State.Play) return;
            bool mine = MyTurn;
            float waited = Time.unscaledTime - turnAt;   // 접속한 짝이 10초 넘게 안 놓으면 남은 초
            string t = mine ? "내 차례" : mode == Mode.Online && waited > 10f && !sliding
                ? $"{partnerName} 차례 · {Mathf.Max(0, Mathf.CeilToInt(TurnLimit - waited))}초" : $"{partnerName} 차례";
            float w = CuteUI.Width(t, true) + 12f, cx = Mathf.Clamp(moverX + moverW / 2f, w / 2f + 2f, AreaW - w / 2f - 2f);
            var r = new Rect(Mathf.Round(cx - w / 2f), Mathf.Round(my - 17f), Mathf.Round(w), 14f);
            if (r.y < 2f) r.y = Mathf.Round(my + BlockH + 3f);
            CuteUI.Box(r, 5, CuteUI.White, mine ? MineCol : PartnerCol, 1);
            CuteUI.LabelCenter(r.center.x, r.center.y, t, mine ? CuteUI.Text : CuteUI.Text, true);
        }

        /// <summary>바닥 양옆: 나 · 짝 이름과 놓은 개수</summary>
        void DrawDuoNames()
        {
            if (mode == Mode.Solo || state == State.Choose || state == State.Wait) return;
            int mineN = 0, theirN = 0;
            for (int i = 1; i < tower.Count; i++) if (IsMine(i)) mineN++; else theirN++;
            float y = AreaH - 9f + view * 0f;
            float now = Time.unscaledTime;
            var l = NameChip(4f, y, "나", mineN, MineCol, false);
            var rr = NameChip(AreaW - 4f, y, partnerName, theirN, PartnerCol, true);
            if (now - myReactAt < 1.4f) Bubble(l, myReact, now - myReactAt, MineCol);
            if (now - partnerReactAt < 1.4f) Bubble(rr, partnerReact, now - partnerReactAt, PartnerCol);
        }

        /// <summary>이름표 위 말풍선 (리액션): 톡 튀어 올랐다가 사라진다</summary>
        static void Bubble(Rect chip, string text, float t, Color32 col)
        {
            if (string.IsNullOrEmpty(text)) return;
            string s = text + "!";
            float w = CuteUI.Width(s, true) + 12f, lift = t < .15f ? 6f * (t / .15f) : 6f;
            var r = new Rect(Mathf.Round(chip.center.x - w / 2f), Mathf.Round(chip.y - 17f - lift), Mathf.Round(w), 14f);
            var old = GUI.color; if (t > 1.1f) GUI.color = new Color(1f, 1f, 1f, 1f - (t - 1.1f) / .3f);
            CuteUI.Box(r, 6, CuteUI.White, col, 1);
            CuteUI.Fill(new Rect(Mathf.Round(r.center.x) - 1, r.yMax, 3, 1), col);
            CuteUI.Fill(new Rect(Mathf.Round(r.center.x), r.yMax + 1, 1, 1), col);
            CuteUI.LabelCenter(r.center.x, r.center.y, s, CuteUI.Text, true);
            GUI.color = old;
        }

        static Rect NameChip(float x, float cy, string name, int n, Color32 col, bool right)
        {
            string t = $"{name} {n}";
            float w = CuteUI.Width(t, true) + 18f;
            var r = new Rect(Mathf.Round(right ? x - w : x), Mathf.Round(cy - 7f), Mathf.Round(w), 14f);
            CuteUI.Box(r, 5, CuteUI.Paper, CuteUI.Ink, 1);
            CuteUI.Circle(r.x + 4f, r.y + 3f, 8, col, CuteUI.Ink, 1);
            CuteUI.Label(r.x + 14f, r.center.y, t, CuteUI.Text, true);
            return r;
        }
    }
}
