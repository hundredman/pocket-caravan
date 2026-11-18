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
    /// - 짝이 맵을 떠나거나 25초 동안 놓지 않으면 끝.
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
        string lastDrop;      // 내가 마지막으로 보낸 놓음 신호 (짝이 놓을 때까지 1.5초마다 다시 보낸다: 서버가 0.4초 안에 겹친 리액션을 버릴 수 있어서)
        float lastDropSent;

        // 받은 "같이 하자" (창이 닫혀 있어도 기억한다)
        static string invFrom, heardId;
        static float invAt = -99f, heardAt = -99f;
        // 보낼 신호 줄
        static readonly Queue<string> outq = new Queue<string>();
        static float lastSent = -9f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { invFrom = heardId = null; invAt = heardAt = -99f; outq.Clear(); lastSent = -9f; }   // 플레이 모드를 다시 켜도 (도메인 다시 읽기 꺼짐)

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

        /// <summary>내 옆(6칸 안)에서 가장 가까운 접속한 사람</summary>
        static string NearestPeer()
        {
            var net = NetClient.I; var pc = PlayerController.Instance;
            if (net == null || pc == null || net.Status != NetClient.State.Online) return null;
            string best = null; float bd = InviteRange * InviteRange;
            foreach (var kv in net.Peers)
            {
                if (!RemotePlayers.TryGetPosition(kv.Key, out var pos)) continue;
                float d = ((Vector2)pos - (Vector2)pc.transform.position).sqrMagnitude;
                if (d <= bd) { bd = d; best = kv.Key; }
            }
            return best;
        }

        // ---------------- 받기 ----------------

        static void OnNet(NetClient.Msg m)
        {
            var net = NetClient.I;
            if (m.t != "emote" || net == null || !IsSignal(m.e) || string.IsNullOrEmpty(m.id) || m.id == net.MyId) return;
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
            else if (kind == 'D' && inst.state == State.Play && !inst.MyTurn && part.Length >= 4
                     && int.TryParse(part[2], out int idx) && int.TryParse(part[3], out int x10) && idx == inst.tower.Count - 1)
            {
                inst.moverX = Mathf.Clamp(x10 / 10f, 0f, AreaW - inst.moverW);
                inst.Drop();
            }
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
            endWhy = null; lastDrop = null;
            Reset(State.Play);
            Pop(first ? "내가 먼저!" : $"{partnerName} 님 먼저");
            Sfx.Play("accept", .6f);
        }

        void ChooseSolo() { mode = Mode.Solo; Reset(State.Ready); Sfx.Play("move", .6f); }

        void ChooseDuo()
        {
            endWhy = null;
            if (InviteFresh) { JoinIfInvited(); return; }
            var peer = NearestPeer();
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
                var peer = NearestPeer();
                small = peer != null ? $"둘이서: {PeerName(peer)} 님에게 같이 하자고 해요" : $"둘이서: 상인회 {NpcName}와 번갈아 쌓아요";
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
            string t = mine ? "내 차례" : $"{partnerName} 차례";
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
            NameChip(4f, y, "나", mineN, MineCol, false);
            NameChip(AreaW - 4f, y, partnerName, theirN, PartnerCol, true);
        }

        static void NameChip(float x, float cy, string name, int n, Color32 col, bool right)
        {
            string t = $"{name} {n}";
            float w = CuteUI.Width(t, true) + 18f;
            var r = new Rect(Mathf.Round(right ? x - w : x), Mathf.Round(cy - 7f), Mathf.Round(w), 14f);
            CuteUI.Box(r, 5, CuteUI.Paper, CuteUI.Ink, 1);
            CuteUI.Circle(r.x + 4f, r.y + 3f, 8, col, CuteUI.Ink, 1);
            CuteUI.Label(r.x + 14f, r.center.y, t, CuteUI.Text, true);
        }
    }
}
