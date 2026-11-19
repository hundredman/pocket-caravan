using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 상인회 짐 쌓기 (2026-11-10, 만국 상인회 수첩 미니게임). 상인회 마당 놀이판 앞에서 Space.
    /// - 좌우로 오가는 짐을 Space(또는 클릭)로 떨어뜨려 수레 위에 탑을 쌓는다. 아래 짐에서 벗어난 만큼 잘려 떨어지고, 다 벗어나면 끝.
    /// - 딱 맞추면(2픽셀 안) "딱!" 너비 그대로, 세 번 잇달아 맞추면 조금 넓어진다. 높이 쌓을수록 빨라진다.
    /// - 5단을 넘기면 수첩 도장 (NetGuild.Mark). 끝나면 온라인 순위표 "guild_stack"에 올린다 (NetScores. 서버는 새 판도 받는다).
    /// 최고 기록은 공용 진행 메모 "net:guild:stack:best". 창(OnGUI) 안에서만 하고 맵·씬은 건드리지 않는다.
    /// 11/18 "둘이서" (팀 UT 1번 같이 하는 미니게임): 열면 혼자 / 둘이서를 고른다. 둘이서는 번갈아 하나씩 놓는다 → NetStackDuo.cs
    /// </summary>
    public partial class NetStackGame : MonoBehaviour
    {
        public const string Board = "guild_stack";
        public const int Goal = 5;
        const string BestKey = "net:guild:stack:best";
        const float AreaW = 200f, AreaH = 178f, BlockH = 12f, StartW = 64f, BaseW = 84f, Ground = 16f, Perfect = 2f, Gravity = 520f;
        // 11/18 놓을 짐은 탑 위 한 뼘(Hover) 떠서 오가고, 놓으면 FallTime 동안 툭 떨어진 뒤 SquashTime 동안 살짝 눌린다
        const float Hover = 9f, FallTime = .12f, SquashTime = .1f;
        // 둘이서는 놓은 뒤 잠깐(Beat) 쉬고 다음 사람 짐이 나온다 ("딱!"이 다음 짐·이름표와 겹치지 않게, 차례가 넘어가는 느낌)
        const float Beat = .75f;

        struct Block { public float x, w; public int look; }
        struct Chip { public float x, y, w, vx, vy; public int look; }
        enum State { Choose, Wait, Ready, Play, Over }

        static NetStackGame inst;
        bool open;
        State state;
        readonly List<Block> tower = new List<Block>();
        readonly List<Chip> chips = new List<Chip>();
        float moverX, moverW, dir = 1f, speed, view, openedAt, overAt, popAt = -9f, popY, landAt = -9f;
        int combo, openedFrame, lastScore;
        string popText;
        bool marked, newBest;

        static readonly Color32 Sky = new Color32(205, 234, 240, 255), SkyLo = new Color32(240, 226, 190, 255), Sand = new Color32(222, 190, 128, 255);

        public static bool IsOpen => inst != null && inst.open;
        static int Score => Mathf.Max(0, (inst != null ? inst.tower.Count : 1) - 1);
        static int Best => GameManager.I == null ? 0 : Mathf.RoundToInt(GameManager.I.GetMemo(BestKey, 0f));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetStackGame>() != null) return;
            var go = new GameObject("NetStackGame");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetStackGame>();
        }

        void Awake() { inst = this; Hook(); }
        void OnDisable() => Close();

        /// <summary>놀이판 앞 Space</summary>
        public static void Open()
        {
            if (inst == null || inst.open || !UIState.TryOpen(inst, inst.Close)) return;
            inst.open = true;
            inst.openedAt = Time.unscaledTime; inst.openedFrame = Time.frameCount;
            inst.mode = Mode.Solo;
            KeyNav.Clear(inst);   // 지난번 끝난 화면에서 고른 단추가 남지 않게
            // 11/18 둘이서는 혼자 5단(수첩 도장)을 넘긴 뒤에 열린다. 그 전에는 예전처럼 바로 혼자 판
            inst.Reset(DuoUnlocked ? State.Choose : State.Ready);
            if (DuoUnlocked) inst.JoinIfInvited();   // 누가 같이 하자고 했으면 바로 둘이서
            Sfx.Play("open");
        }

        void Close()
        {
            if (!open) return;
            DuoQuit();
            open = false;
            UIState.Release(this);
        }

        void Reset(State s)
        {
            state = s;
            tower.Clear(); chips.Clear();
            tower.Add(new Block { x = (AreaW - BaseW) / 2f, w = BaseW, look = -1 });
            moverW = StartW; dir = 1f; moverX = 0f; speed = 72f; view = 0f; combo = 0;
            marked = false; newBest = false; popAt = -9f; landAt = -9f; sliding = false;
            lockedAtStart = !DuoUnlocked;   // 이 판으로 둘이서가 열리는지 (도장은 판 중간에 찍힌다)
            TurnStarted();
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
            // 11/17 끝난 화면에서 화살표로 단추를 골랐으면 Space는 그 단추가 받는다 (KeyNav)
            bool drop = InputReader.InteractPressed() && !(state == State.Over && KeyNav.HasFocus(this));
            if (InputReader.CancelPressed()) { if (state == State.Play && Score > 0) End(); Close(); return; }
            DuoUpdate();
            if (!open) return;

            if (state == State.Choose) { if (drop && !KeyNav.HasFocus(this)) ChooseDuo(); }   // Space = 둘이서 (단추는 OnGUI, ← →로 고르기)
            else if (state == State.Wait) { }
            else if (state == State.Ready) { if (drop) { state = State.Play; TurnStarted(); Sfx.Play("accept", .6f); } }
            else if (state == State.Play && !InBeat)
            {
                if (mode == Mode.Online && !MyTurn) PartnerOnline();   // 11/19 접속한 짝 차례: 짐이 짝 화면처럼 오간다 (NetStackDuo)
                else
                {
                    moverX += dir * speed * dt;
                    if (moverX < 0f) { moverX = 0f; dir = 1f; }
                    if (moverX > AreaW - moverW) { moverX = AreaW - moverW; dir = -1f; }
                    if (MyTurn) { if (drop) LocalDrop(); }
                    else PartnerTurn(dt);
                }
            }
            else if (state == State.Play) { }   // 둘이서: 놓은 뒤 잠깐 쉰다
            else if (drop && Time.unscaledTime - overAt > .6f) Again();

            for (int i = chips.Count - 1; i >= 0; i--)
            {
                var c = chips[i];
                c.vy += Gravity * dt; c.y += c.vy * dt; c.x += c.vx * dt;
                if (c.y > AreaH + view + 40f) chips.RemoveAt(i); else chips[i] = c;
            }
            float want = Mathf.Max(0f, (tower.Count - 9) * BlockH);
            view = Mathf.Lerp(view, want, 1f - Mathf.Exp(-dt * 8f));
        }

        bool InBeat => mode != Mode.Solo && Time.unscaledTime - landAt < Beat;

        /// <summary>층의 아래쪽 y (놀이판 안, 위가 0). level 0 = 수레</summary>
        float LevelY(int level) => AreaH - Ground - (level + 1) * BlockH + view;

        void Drop()
        {
            var top = tower[tower.Count - 1];
            float left = Mathf.Max(moverX, top.x), right = Mathf.Min(moverX + moverW, top.x + top.w), overlap = right - left;
            int look = LookOf(tower.Count, tower.Count % 5);
            float y = LevelY(tower.Count) - Hover;   // 떠 있던 높이에서 잘려 떨어진다
            string who = mode != Mode.Solo && !IsMine(tower.Count) ? partnerName + " " : "";   // 짝이 맞추면 "루나 딱!"
            if (overlap <= 0f)
            {
                chips.Add(new Chip { x = moverX, y = y, w = moverW, vx = dir * 30f, vy = 0f, look = look });
                Sfx.Play("fail");
                End();
                return;
            }
            Block b;
            if (Mathf.Abs(moverX - top.x) <= Perfect)
            {
                combo++;
                float w = top.w;
                if (combo >= 3 && w < StartW) { w = Mathf.Min(StartW, w + 6f); combo = 0; Pop(who + "딱! 넓어졌다"); }
                else Pop(who + (combo >= 2 ? $"딱! x{combo}" : "딱!"));
                b = new Block { x = top.x - (w - top.w) / 2f, w = w, look = look };
                b.x = Mathf.Clamp(b.x, 0f, AreaW - b.w);
                Sfx.Play("coin", .7f);
            }
            else
            {
                combo = 0;
                b = new Block { x = left, w = overlap, look = look };
                if (moverX < top.x) chips.Add(new Chip { x = moverX, y = y, w = top.x - moverX, vx = -40f, vy = -20f, look = look });
                else chips.Add(new Chip { x = right, y = y, w = moverX + moverW - right, vx = 40f, vy = -20f, look = look });
                Sfx.Play("drop", .8f);
            }
            tower.Add(b);
            landAt = Time.unscaledTime;
            int score = Score;
            if (score >= Goal && !marked && mode == Mode.Solo)
            {
                marked = true;
                Pop($"{Goal}단 돌파! 수첩 도장");
                NetGuild.Mark(NetGuild.Task.Stack, $"{score}단");
            }
            moverW = b.w;
            dir = tower.Count % 2 == 0 ? 1f : -1f;
            moverX = dir > 0f ? 0f : AreaW - moverW;
            speed = Mathf.Min(200f, 72f + score * 7f);
            TurnStarted();
        }

        void Pop(string text) { popText = text; popAt = Time.unscaledTime; popY = LevelY(tower.Count) - 6f; }

        void End()
        {
            if (state == State.Over) return;
            state = State.Over; overAt = Time.unscaledTime;
            lastScore = Score;
            var gm = GameManager.I;
            if (mode == Mode.Solo)
            {
                if (gm != null && lastScore > Best) { gm.SetMemo(BestKey, lastScore); newBest = lastScore > 0; }
                justUnlocked = lockedAtStart && DuoUnlocked;
                if (lastScore > 0) NetScores.Report(Board, lastScore);
            }
            else DuoEnd(gm);
            if (newBest) Sfx.Play("levelup", .6f);
        }

        // ---------------- 그리기 ----------------

        static readonly Color32[] CargoFill = { new Color32(214, 140, 72, 255), new Color32(238, 226, 196, 255), new Color32(80, 178, 168, 255), new Color32(232, 104, 96, 255), new Color32(150, 120, 200, 255) };
        static readonly Color32[] CargoBand = { new Color32(160, 96, 48, 255), new Color32(214, 180, 92, 255), new Color32(46, 120, 112, 255), new Color32(176, 64, 64, 255), new Color32(104, 80, 152, 255) };

        /// <summary>짐 한 덩이: 0 나무 상자 · 1 자루 · 2 청록 궤짝 · 3 향신료 상자 · 4 양탄자 말이</summary>
        static void Cargo(Rect r, int look)
        {
            if (r.width < 1f) return;
            var f = CargoFill[look]; var b = CargoBand[look];
            CuteUI.Box(r, 2, f, CuteUI.Ink, 1);
            CuteUI.Fill(new Rect(r.x + 2, r.y + 2, r.width - 4, 1), new Color32(255, 255, 255, 90));
            if (look == 0 || look == 2)
                for (float x = r.x + 9; x < r.xMax - 4; x += 10) CuteUI.Fill(new Rect(x, r.y + 3, 1, r.height - 5), b);
            else if (look == 1) CuteUI.Fill(new Rect(r.x + 2, r.y + r.height / 2f, r.width - 4, 2), b);
            else if (look == 3) { CuteUI.Fill(new Rect(r.x + 2, r.y + 4, r.width - 4, 1), b); CuteUI.Fill(new Rect(r.x + 2, r.y + 8, r.width - 4, 1), b); }
            else for (float x = r.x + 5; x < r.xMax - 3; x += 6) CuteUI.Fill(new Rect(x, r.y + 2, 2, r.height - 4), b);
        }

        /// <summary>바닥 수레 (탑의 받침)</summary>
        static void Cart(Rect r)
        {
            CuteUI.Box(r, 2, new Color32(176, 112, 64, 255), CuteUI.Ink, 1);
            CuteUI.Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, 1), new Color32(255, 220, 170, 160));
            CuteUI.Circle(r.x + 10, r.yMax - 4, 10, new Color32(120, 80, 52, 255), CuteUI.Ink, 1);
            CuteUI.Circle(r.xMax - 20, r.yMax - 4, 10, new Color32(120, 80, 52, 255), CuteUI.Ink, 1);
        }

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -12;
            float now = Time.unscaledTime, W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01((now - openedAt) / .25f), 3f);
            CuteUI.Dim();
            float w = AreaW + 36f, h = AreaH + 78f;
            var r = new Rect(Mathf.Round((W - w) / 2f), Mathf.Round((H - h) / 2f + 8f + (1f - ease) * 14f), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11f, mode == Mode.Solo ? "상인회 짐 쌓기" : "상인회 짐 쌓기 · 둘이서");

            // 머리: 지금 단 · 최고 · 목표
            int score = state == State.Over ? lastScore : Score;
            CuteUI.Label(r.x + 18f, r.y + 24f, $"{score}단", CuteUI.Text, false, true);
            int best = mode == Mode.Solo ? Best : DuoBest;
            CuteUI.LabelRight(r.xMax - 18f, r.y + 24f, mode == Mode.Solo ? $"최고 {Mathf.Max(best, score)}단" : $"둘이 최고 {Mathf.Max(best, score)}단", CuteUI.Sub, true);
            bool stamped = mode == Mode.Solo && (score >= Goal || marked);
            string goal = mode != Mode.Solo ? "번갈아 놓기" : stamped ? "수첩 도장 받았어요!" : state == State.Choose ? "혼자 또는 둘이서" : $"{Goal}단을 넘기면 수첩 도장";
            CuteUI.LabelCenter(r.center.x, r.y + 24f, goal, stamped ? CuteUI.Sell : CuteUI.Sub, true, true);

            // 놀이판
            var a = new Rect(r.x + 18f, r.y + 36f, AreaW, AreaH);
            CuteUI.Box(new Rect(a.x - 2, a.y - 2, a.width + 4, a.height + 4), 4, Sky, CuteUI.Ink, 1);
            GUI.BeginGroup(a);
            CuteUI.Fill(new Rect(0, AreaH * .55f, AreaW, AreaH * .45f), SkyLo);
            float gy = AreaH - Ground + view;
            if (gy < AreaH) CuteUI.Fill(new Rect(0, gy, AreaW, AreaH - gy + 1), Sand);
            // 높이 눈금: 목표 줄
            float goalY = LevelY(Goal);
            if (mode == Mode.Solo && state != State.Choose && goalY > -2f && goalY < AreaH) { for (float x = 2; x < AreaW; x += 8) CuteUI.Fill(new Rect(x, goalY, 4, 1), CuteUI.Sell); CuteUI.Label(AreaW - 30f, goalY - 6f, $"{Goal}단", CuteUI.Sell, true); }
            for (int i = 0; i < tower.Count; i++)
            {
                var b = tower[i];
                float y = LevelY(i);
                if (y > AreaH || y < -BlockH * 2) continue;
                if (b.look < 0) { Cart(new Rect(b.x, y - 2f, b.w, BlockH + 6f)); continue; }
                float bh = BlockH, lt = now - landAt;
                if (i == tower.Count - 1 && lt < FallTime + SquashTime)
                {
                    if (lt < FallTime) { float k = lt / FallTime; y -= Hover * (1f - k * k); }     // 툭 떨어진다
                    else { bh -= 1f; y += 1f; }                                                    // 닿으면 한 칸 눌린다
                }
                Cargo(new Rect(Mathf.Round(b.x), Mathf.Round(y), Mathf.Round(b.w), bh), LookOf(i, b.look));
            }
            foreach (var c in chips) Cargo(new Rect(Mathf.Round(c.x), Mathf.Round(c.y), Mathf.Round(c.w), BlockH), c.look);
            if ((state == State.Ready || state == State.Play) && !InBeat)
            {
                float my = LevelY(tower.Count) - Hover;
                // 떨어질 자리 안내: 옅은 세로줄
                var guide = new Color32(43, 37, 51, 40);
                CuteUI.Fill(new Rect(Mathf.Round(moverX), my + BlockH, 1, Mathf.Max(0, LevelY(tower.Count - 1) - my - BlockH)), guide);
                CuteUI.Fill(new Rect(Mathf.Round(moverX + moverW - 1), my + BlockH, 1, Mathf.Max(0, LevelY(tower.Count - 1) - my - BlockH)), guide);
                if (now - landAt >= FallTime) Cargo(new Rect(Mathf.Round(moverX), Mathf.Round(my), Mathf.Round(moverW), BlockH), LookOf(tower.Count, tower.Count % 5));
                DrawTurn(my);
            }
            DrawDuoNames();
            if (now - popAt < .9f)
            {
                float k = (now - popAt) / .9f;
                var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, 1f - k * k);
                CuteUI.LabelCenter(AreaW / 2f, popY - k * 14f, popText, CuteUI.Alert, false, true);
                GUI.color = old;
            }
            GUI.EndGroup();

            // 시작 · 끝 안내
            if (state == State.Choose) ChooseBanner(a);
            else if (state == State.Wait) WaitBanner(a);
            else if (state == State.Ready) Banner(a, "짐을 수레 위에 쌓아요!", "Space · 클릭으로 놓기");
            else if (state == State.Over)
            {
                if (mode == Mode.Solo) Banner(a, newBest ? $"{lastScore}단! 새 최고 기록" : $"{lastScore}단!", DuoUnlocked && InviteFresh ? $"{PeerName(invFrom)} 님이 같이 하재요! 다시를 누르면 둘이서" : justUnlocked ? "이제 둘이서도 쌓을 수 있어요!" : lastScore >= Goal ? "잘 쌓았어요! 기록은 순위표에" : $"{Goal}단까지 다시 해 볼까요?");
                else DuoOverBanner(a);
            }
            if ((state == State.Ready || state == State.Play) && Event.current.type == EventType.MouseDown && a.Contains(Event.current.mousePosition)) { InputReader.Press(' '); Event.current.Use(); }

            // 아래 단추 · 키
            float by = r.yMax - 22f;
            if (state == State.Choose || state == State.Wait) DuoButtons(a, by);
            else if (state == State.Over)
            {
                // 11/10 단추 셋을 놀이판 너비에 고르게 (순위표·닫기가 겹쳤다)
                float bw = Mathf.Floor((AreaW - 12f) / 3f), bx = a.x;
                KeyNav.Begin(this);   // 11/17 ← →로 다시·순위표·닫기 고르기
                try
                {
                    if (CuteUI.Button(new Rect(bx, by - 6f, bw, 20f), "다시")) { KeyNav.Clear(this); Again(); }
                    if (CuteUI.SoftButton(new Rect(bx + bw + 6f, by - 6f, bw, 20f), "순위표")) { KeyNav.Clear(this); Close(); NetScores.Open(); }
                    if (CuteUI.SoftButton(new Rect(bx + (bw + 6f) * 2f, by - 6f, bw, 20f), "닫기")) { KeyNav.Clear(this); Close(); }
                }
                finally { KeyNav.End(); }
            }
            else CuteUI.LabelCenter(r.center.x, by + 4f, mode == Mode.Solo ? "Space 놓기 · Esc 그만" : "Space 놓기 · 1 박수 2 하트 · Esc 그만", CuteUI.Sub, true);
        }

        static void Banner(Rect a, string big, string small)
        {
            // 11/10 글에 맞춰 너비를 정한다 (작은 글이 상자 밖으로 삐져나왔다). 놀이판보다 넓으면 작은 글을 감싼다
            float bw = Mathf.Clamp(Mathf.Max(CuteUI.Width(big, false, true), CuteUI.Width(small, true)) + 28f, 120f, a.width - 8f);
            float sh = Mathf.Ceil(CuteUI.WrapHeight(small, bw - 20f, true));
            var b = new Rect(Mathf.Round(a.center.x - bw / 2f), a.y + 36f, bw, 26f + sh + 8f);
            CuteUI.Box(b, 8, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            CuteUI.LabelCenter(b.center.x, b.y + 14f, big, CuteUI.Text, false, true);
            if (CuteUI.Width(small, true) <= bw - 20f) CuteUI.LabelCenter(b.center.x, b.y + 28f + sh / 2f - 4f, small, CuteUI.Sub, true);
            else CuteUI.Wrap(b.x + 10f, b.y + 24f, bw - 20f, small, CuteUI.Sub, true);
        }
    }
}
