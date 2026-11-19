using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 좌판 미니게임 "손님 맞이" 2판. 은샘 "미니게임은 이득을 직접 주거나 거래를 대체하지 않게. 순수 재미·해금·간접 도움으로" → 3안(순수 재미)으로 다시 만듦.
    /// - 장터 길을 손님들이 오른쪽에서 왼쪽으로 지나간다 (게임의 실제 상인 그림, 걷는 4장). 머리 위 말풍선에 바라는 물건, "…"은 구경꾼.
    /// - 손님이 좌판 앞 밝은 자리에 들어오면 그 물건 칸 번호(1~4)나 클릭으로 건넨다. 자리 한가운데서 건네면 "딱!" 점수 더, 내리 맞히면 더.
    ///   틀린 물건 · 구경꾼 부르기 · 빈자리에 누르기 · 놓치기는 연속만 끊긴다 (돈 손해는 없다).
    /// - 45초. 갈수록 손님이 빨라지고 자주 온다.
    /// - 값은 장터 시세 그대로 (PlayerStall.SellOne 배율 1). 장터에서 파는 것과 같아서 돈으로 이득은 없다. 보상은 점수 · 최고 기록 · 좌판 꾸미기(등롱·깃발).
    /// - 하루 몇 번이든 (진열한 물건이 있고 아침 7시~밤 9시). 손님과 판매는 내 화면의 NPC 손님이라, 한 마을에 좌판이 여럿이어도 서로 손님을 뺏지 않는다.
    /// 최고 기록: 저장 메모 "stall:best". 꾸미기 문턱: StallDeco.
    /// </summary>
    public sealed class StallRush : MonoBehaviour
    {
        static StallRush inst;
        public static bool IsOpen => inst != null && inst.phase != Phase.Closed;
        public static int ClosedFrame = -1;
        public static void ForceClose() { if (inst != null && inst.phase != Phase.Closed) inst.Shut(); }
        void Shut()
        {
            // 판 도중에 창이 닫혀도(불러오기·새 게임 등) 이번 판 최고 기록은 남긴다
            if (phase == Phase.Play && GameManager.I != null && score > bestBefore) { GameManager.I.SetMemo(BestKey, score); var s = StallSpot.Current; if (s != null) s.RefreshDeco(); }
            phase = Phase.Closed; ClosedFrame = Time.frameCount;
        }

        public const string BestKey = "stall:best";
        const float Duration = 45f, ZoneHalf = 26f, PerfectHalf = 8f;
        static readonly string[] People = { "trader_tall", "venice_merchant_tall", "arabia_npc_merchant_tall", "north_p_trader_tall", "arabia_woman_calligrapher_tall" };
        static Sprite[][] walk;

        enum Phase { Closed, Intro, Play, Done }
        enum Mood { Walk, Happy, Sad, Shrug }
        class Guest { public float x, speed, at, moodAt; public int look; public ItemData wish; public Mood mood; public bool tried, gone; }

        Phase phase;
        readonly List<ItemData> shelf = new List<ItemData>();
        readonly List<Guest> guests = new List<Guest>();
        float LaneW = 380f;   // 창 폭에 맞춘다 (OnGUI)
        float startedAt, nextSpawn, flashAt, flashX;
        float clock;   // 이 놀이의 시계: 한 프레임에 0.1초까지만 흐른다 (창 밖에 나갔다 와도 손님이 한꺼번에 지나가지 않게)
        int pickFrame = -1;
        static Sprite stallArt;
        int score, served, missed, earned, streak, bestStreak, bestBefore;
        string pop = ""; Color32 popCol; float popAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StallRush>() != null) return;
            var go = new GameObject("StallRush"); DontDestroyOnLoad(go); inst = go.AddComponent<StallRush>();
        }

        public static void Open()
        {
            var gm = GameManager.I;
            if (inst == null || gm == null || StallWindow.RushDenial(gm) != null) return;
            inst.Begin(gm);
        }

        public static int Best => GameManager.I != null ? Mathf.RoundToInt(GameManager.I.GetMemo(BestKey)) : 0;

        void Begin(GameManager gm)
        {
            var p = gm.Player;
            if (walk == null)
            {
                walk = new Sprite[People.Length][];
                for (int i = 0; i < People.Length; i++)
                {
                    walk[i] = new Sprite[4];
                    for (int f = 0; f < 4; f++) walk[i][f] = Resources.Load<Sprite>($"Arabia/Fair/People/{People[i]}_l{f}");
                }
            }
            shelf.Clear();
            var rows = new List<KeyValuePair<ItemData, int>>(p.stallStock.Items);
            rows.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < rows.Count && shelf.Count < 4; i++) if (rows[i].Key != null && rows[i].Value > 0) shelf.Add(rows[i].Key);
            guests.Clear();
            score = served = missed = earned = streak = bestStreak = 0; pop = "";
            bestBefore = Best;
            phase = Phase.Intro;
            Sfx.Play("open");
        }

        void StartPlay()
        {
            phase = Phase.Play; startedAt = clock; nextSpawn = 0.4f;
        }

        float T => clock - startedAt;
        float Ramp => Mathf.Clamp01(T / Duration);

        void Spawn(PlayerState p)
        {
            var have = shelf.FindAll(it => p.stallStock.Count(it) > 0);
            var g = new Guest { x = LaneW + 20f, look = Random.Range(0, People.Length), at = clock };
            g.speed = Mathf.Lerp(46f, 82f, Ramp) * Random.Range(0.9f, 1.12f);
            g.wish = have.Count > 0 && Random.value > 0.2f ? have[Random.Range(0, have.Count)] : null;   // 다섯에 하나는 구경꾼
            guests.Add(g);
            nextSpawn = T + Mathf.Lerp(2.0f, 1.05f, Ramp) * Random.Range(0.8f, 1.2f);
        }

        float ZoneX => 74f;   // 길 위 좌판 앞 자리 (좌판 그림 가운데)

        Guest InZone()
        {
            Guest best = null; float bd = float.MaxValue;
            foreach (var g in guests)
            {
                if (g.gone || g.mood != Mood.Walk || g.tried) continue;
                float d = Mathf.Abs(g.x - ZoneX);
                if (d <= ZoneHalf && d < bd) { best = g; bd = d; }
            }
            return best;
        }

        void Pick(GameManager gm, int slot)
        {
            if (slot < 0 || slot >= shelf.Count || pickFrame == Time.frameCount) return;   // 숫자 키와 클릭이 같은 프레임에 둘 다 들어와도 한 번만
            pickFrame = Time.frameCount;
            var p = gm.Player; var it = shelf[slot];
            if (p.stallStock.Count(it) <= 0) { Sfx.Play("cancel", .4f); return; }
            var g = InZone();
            if (g == null) { Break("아직 손님이 앞에 없어요"); Sfx.Play("cancel", .4f); return; }
            g.tried = true;
            if (g.wish == null) { g.mood = Mood.Shrug; g.moodAt = clock; Break("구경만 하는 손님이에요"); Sfx.Play("cancel", .5f); return; }
            if (g.wish != it) { g.mood = Mood.Sad; g.moodAt = clock; Break("그게 아닌데요…"); Sfx.Play("cancel"); return; }
            // 맞혔다: 시세 그대로 판다 (배율 1). 점수 = 10 + 딱 10 + 연속 (최대 +10)
            int price = PlayerStall.SellOne(gm, it, 1f, true);
            if (price <= 0) { g.tried = false; Sfx.Play("cancel", .4f); return; }
            bool perfect = Mathf.Abs(g.x - ZoneX) <= PerfectHalf;
            int pts = 10 + (perfect ? 10 : 0) + Mathf.Min(streak, 5) * 2;
            score += pts; earned += price; served++; streak++; bestStreak = Mathf.Max(bestStreak, streak);
            g.mood = Mood.Happy; g.moodAt = clock;
            flashAt = clock; flashX = g.x;
            Pop(perfect ? $"딱! +{pts}" : $"+{pts}", perfect ? CuteUI.Sell : CuteUI.Up);
            Sfx.Play(perfect ? "success" : "coin", perfect ? .7f : 1f);
        }

        void Break(string why) { streak = 0; Pop(why, CuteUI.Down); }
        void Pop(string s, Color32 c) { pop = s; popCol = c; popAt = clock; }

        void Finish(GameManager gm)
        {
            phase = Phase.Done;
            if (score > bestBefore) gm.SetMemo(BestKey, score);
            Sfx.Play(served > 0 ? "levelup" : "cancel", .7f);
            { var s = StallSpot.Current; if (s != null) s.RefreshDeco(); }
            gm.SaveNow();
        }

        void Update()
        {
            if (phase == Phase.Closed) return;
            clock += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            var gm = GameManager.I; if (gm == null || !StallWindow.Showing) { Shut(); return; }
            bool esc = InputReader.CancelPressed();
            if (phase == Phase.Intro) { if (esc) Shut(); else if (Go()) StartPlay(); return; }
            if (phase == Phase.Done) { if (esc || Go()) Shut(); return; }
            if (esc) { Finish(gm); return; }
            var p = gm.Player;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (T >= nextSpawn && T < Duration - 2f && guests.FindAll(x => !x.gone).Count < 5) Spawn(p);
            foreach (var g in guests)
            {
                if (g.gone) continue;
                bool stop = g.mood == Mood.Happy && clock - g.moodAt < 0.7f;   // 산 손님은 잠깐 서서 받는다
                if (!stop) g.x -= g.speed * dt * (g.mood == Mood.Happy ? 1.4f : 1f);
                if (g.x < -24f)
                {
                    g.gone = true;
                    if (g.mood == Mood.Walk && !g.tried)
                    {
                        if (g.wish != null && p.stallStock.Count(g.wish) > 0) { missed++; Break("손님을 놓쳤어요"); }
                        else if (g.wish == null) { score += 2; }   // 구경꾼을 그냥 보냈다
                    }
                }
            }
            int n = InputReader.NumberPressed(); if (n >= 1 && n <= shelf.Count) Pick(gm, n - 1);
            bool empty = shelf.TrueForAll(it => p.stallStock.Count(it) <= 0);
            if (T >= Duration || empty) Finish(gm);
        }

        static bool Go() => InputReader.InteractPressed() || InputReader.EnterPressed();   // 시작·닫기 (Space·Enter)

        // ---------------------------------------------------------------- 그리기
        void OnGUI()
        {
            if (phase == Phase.Closed || GameManager.I == null) return;
            UIStyle.Begin();
            GUI.depth = -20;   // 좌판 창 위
            var gm = GameManager.I; var p = gm.Player;
            float w = Mathf.Min(420, UIStyle.VirtualWidth - 24); LaneW = Mathf.Min(380f, w - 40f);
            float h = phase == Phase.Done ? 200 : 262;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "손님 맞이");
            if (phase == Phase.Done) { DrawDone(r); return; }

            // 위 줄: 점수 · 연속 · 남은 시간
            float top = r.y + 24;
            CuteUI.Label(r.x + 16, top, $"점수 {score}", CuteUI.Text, false, true);
            if (streak >= 2) CuteUI.Label(r.x + 92, top, $"{streak}명 연속!", CuteUI.Sell, true, true);
            float remain = phase == Phase.Play ? Mathf.Max(0f, Duration - T) : Duration;
            CuteUI.Icon(CuteUI.Pic.Clock, r.xMax - 128, top - 5);
            CuteUI.Bar(new Rect(r.xMax - 114, top - 3, 98, 7), remain / Duration, remain > 10f ? CuteUI.Interact : CuteUI.Down, remain > 10f ? CuteUI.InteractHi : CuteUI.AlertSoft);

            // 길 (장터 흙길) + 좌판 + 손님
            var lane = new Rect(r.center.x - LaneW / 2f, r.y + 40, LaneW, 116);
            DrawLane(lane, p);
            if (phase == Phase.Intro)
            {
                var card = new Rect(lane.x + 120, lane.y + 10, lane.width - 132, lane.height - 20);
                CuteUI.Box(card, 8, CuteUI.White, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
                string[] rules = { "손님이 좌판 앞 밝은 자리에 오면", "바라는 물건 번호(1~4)를 눌러 건네요", "한가운데서 건네면 \"딱!\" · 내리 맞히면 점수 더", "\"…\" 손님은 구경꾼, 부르지 마세요", "45초 · 값은 장터 시세 그대로" };
                for (int i = 0; i < rules.Length; i++) CuteUI.Label(card.x + 10, card.y + 13 + i * 17, rules[i], i == 4 ? CuteUI.Sub : CuteUI.Text, true, i < 2);
            }
            DrawShelf(gm, p, new Rect(r.x, lane.yMax + 8, r.width, 52), phase == Phase.Play);
            if (phase == Phase.Intro)
            {
                var b = new Rect(r.center.x - 60, r.yMax - 30, 120, 22);
                if (CuteUI.Button(b, "시작")) StartPlay();
                CuteUI.Keycap(b.xMax - CuteUI.KeycapWidth("Space") - 6, b.center.y - 6.5f, "Space", true);
                if (bestBefore > 0) CuteUI.LabelRight(r.xMax - 16, r.yMax - 19, $"최고 {bestBefore}점", CuteUI.Sub, true);
            }
            else CuteUI.LabelCenter(r.center.x, r.yMax - 16, "번호 키 1~4 또는 칸 클릭 · Esc 그만", CuteUI.Sub, true);

            // 떠오르는 글 (점수 · 까닭)
            float pa = 1f - Mathf.Clamp01((clock - popAt) / 1.1f);
            if (pa > 0f && pop.Length > 0)
            {
                var old = GUI.color; GUI.color = new Color(old.r, old.g, old.b, pa);
                CuteUI.LabelCenter(lane.x + ZoneX + 60, lane.y + 14 - (1f - pa) * 8f, pop, popCol, true, true);
                GUI.color = old;
            }
        }

        static readonly Color32 Dirt = new Color32(196, 160, 112, 255), DirtLo = new Color32(176, 140, 96, 255), DirtHi = new Color32(214, 182, 136, 255), Grass = new Color32(120, 164, 92, 255), GrassLo = new Color32(98, 140, 76, 255);

        void DrawLane(Rect lane, PlayerState p)
        {
            GUI.BeginGroup(lane);
            float W = lane.width, H = lane.height, road = 60;   // 길 윗선 (손님 발은 road+40 근처)
            CuteUI.Fill(new Rect(0, 0, W, road), Grass);
            for (int i = 0; i < 26; i++) { float gx = (i * 53) % W, gy = (i * 29) % (road - 6); CuteUI.Fill(new Rect(gx, gy, 2, 3), GrassLo); }
            CuteUI.Fill(new Rect(0, road, W, H - road), Dirt);
            CuteUI.Fill(new Rect(0, road, W, 2), DirtLo);
            for (int i = 0; i < 30; i++) { float dx = (i * 47 + 11) % W, dy = road + 6 + (i * 13) % (H - road - 10); CuteUI.Fill(new Rect(dx, dy, 3, 2), i % 2 == 0 ? DirtLo : DirtHi); }
            // 좌판 (내 좌판 그림 2배) + 좌판 앞 자리
            var art = stallArt != null ? stallArt : (stallArt = Resources.Load<Sprite>("Stall/stall_0"));
            if (art != null) CuteUI.Sprite(art, new Rect(ZoneX - 48, road - 70, 96, 96));
            float zx = ZoneX, feet = road + 46;
            bool live = phase == Phase.Play;
            var zone = new Rect(zx - ZoneHalf, road + 4, ZoneHalf * 2, H - road - 8);
            float glow = live ? 0.55f + Mathf.Sin(clock * 4f) * 0.15f : 0.5f;
            var oc = GUI.color; GUI.color = new Color(1f, 1f, 1f, glow * 0.6f);
            CuteUI.Box(zone, 6, CuteUI.SellSoft, CuteUI.Sell);
            GUI.color = oc;
            CuteUI.Fill(new Rect(zx - PerfectHalf, zone.yMax - 4, PerfectHalf * 2, 2), CuteUI.Sell);   // 한가운데 표시
            if (live && clock - flashAt < 0.25f) CuteUI.Box(new Rect(flashX - 14, road + 2, 28, H - road - 4), 6, CuteUI.White, CuteUI.Sell);

            // 손님 (뒤에서 앞 순서: x가 큰 쪽이 먼저)
            var order = new List<Guest>(guests); order.Sort((a, b) => b.x.CompareTo(a.x));
            foreach (var g in order)
            {
                if (g.gone) continue;
                var set = walk[g.look]; int f = g.mood == Mood.Happy && clock - g.moodAt < 0.7f ? 0 : (int)((clock - g.at) * 8f) % 4;
                var spr = set[f] ?? set[0];
                float bob = g.mood == Mood.Happy && clock - g.moodAt < 0.4f ? -Mathf.Sin((clock - g.moodAt) / 0.4f * Mathf.PI) * 4f : 0f;
                if (spr != null) CuteUI.Sprite(spr, new Rect(Mathf.Round(g.x - 16), Mathf.Round(feet - 48 + bob), 32, 48));
                DrawBubble(g, p, g.x, feet - 38 + bob);   // 그림 위쪽 빈칸만큼 내려 머리 바로 위에
            }
            GUI.EndGroup();
        }

        void DrawBubble(Guest g, PlayerState p, float x, float y)
        {
            float since = clock - g.moodAt;
            if (g.mood == Mood.Happy && since < 1.2f) { CuteUI.Icon(CuteUI.Pic.Coin7, x - 3, y - 12); return; }
            if (g.mood == Mood.Sad) { if (since < 1f) CuteUI.LabelCenter(x, y - 6, "…", CuteUI.Down, false, true); return; }
            if (g.mood == Mood.Shrug) { if (since < 1f) CuteUI.LabelCenter(x, y - 6, "?", CuteUI.Sub, false, true); return; }
            if (g.mood != Mood.Walk) return;
            var b = new Rect(Mathf.Round(x - 13), Mathf.Round(y - 24), 26, 22);
            bool soldOut = g.wish != null && p.stallStock.Count(g.wish) <= 0;
            CuteUI.Box(b, 7, soldOut ? CuteUI.ZeroSoft : CuteUI.White, CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
            CuteUI.Fill(new Rect(x - 2, b.yMax, 4, 2), CuteUI.Ink);
            if (g.wish == null) CuteUI.LabelCenter(b.center.x, b.center.y - 1, "…", CuteUI.Sub, false, true);
            else if (g.wish.icon != null) CuteUI.Sprite(g.wish.icon, new Rect(b.center.x - 8, b.center.y - 8, 16, 16));
        }

        /// <summary>진열대: 칸마다 번호 · 물건 그림 · 남은 개수. 놀이 중엔 눌러서 고른다</summary>
        void DrawShelf(GameManager gm, PlayerState p, Rect row, bool live)
        {
            float sw = 70, gap = 8, total = shelf.Count * sw + Mathf.Max(0, shelf.Count - 1) * gap;
            float sx = row.center.x - total / 2f;
            var target = live ? InZone() : null;
            for (int i = 0; i < shelf.Count; i++)
            {
                var it = shelf[i]; int n = p.stallStock.Count(it);
                var b = new Rect(sx + i * (sw + gap), row.y, sw, 44);
                bool hover = live && n > 0 && b.Contains(Event.current.mousePosition);
                CuteUI.Box(b, 7, n > 0 ? (hover ? CuteUI.Paper2 : CuteUI.White) : CuteUI.ZeroSoft, n > 0 ? CuteUI.Ink : CuteUI.Line, 1, 2, CuteUI.SoftShadow);
                CuteUI.Keycap(b.x + 4, b.y + 4, (i + 1).ToString(), true);
                if (it.icon != null) CuteUI.Sprite(it.icon, new Rect(b.center.x - 14, b.y + 6, 28, 28));
                CuteUI.LabelRight(b.xMax - 5, b.yMax - 7, $"×{n}", n > 0 ? CuteUI.Sub : CuteUI.Zero, true);
                if (live && n > 0 && CuteUI.Hit(b, GUIContent.none, GUIStyle.none)) Pick(gm, i);
            }
        }

        void DrawDone(Rect r)
        {
            bool record = score > bestBefore && score > 0;
            CuteUI.LabelCenter(r.center.x, r.y + 36, record ? "새 기록!" : "손님 맞이 끝", record ? CuteUI.Sell : CuteUI.Text, false, true);
            CuteUI.LabelCenter(r.center.x, r.y + 62, $"점수 {score}  ·  최고 {Mathf.Max(score, bestBefore)}", CuteUI.Text, false, true);
            CuteUI.LabelCenter(r.center.x, r.y + 84, $"손님 {served}명에게 팔았어요 · 가장 긴 연속 {bestStreak}명 · 놓친 손님 {missed}명", CuteUI.Sub, true);
            if (earned > 0) CuteUI.LabelCenter(r.center.x, r.y + 102, $"판 돈 {earned}냥 (장터 시세 그대로)", CuteUI.Sub, true);
            // 꾸미기: 이번에 열린 것 · 다음 것
            string unlocked = StallDeco.NewlyUnlocked(bestBefore, Mathf.Max(score, bestBefore));
            string next = StallDeco.NextGoal(Mathf.Max(score, bestBefore));
            if (unlocked != null) CuteUI.LabelCenter(r.center.x, r.y + 126, $"좌판에 {unlocked}이 달렸어요!", CuteUI.Up, false, true);
            else if (next != null) CuteUI.LabelCenter(r.center.x, r.y + 126, next, CuteUI.Sub, true);
            var b = new Rect(r.center.x - 50, r.yMax - 32, 100, 22);
            if (CuteUI.Button(b, "닫기")) Shut();
        }
    }

    /// <summary>11/19 손님 맞이 최고 점수로 열리는 좌판 꾸미기 (돈·거래와 상관없는 순수 보상). 문턱을 넘으면 맵 위 내 좌판에 달린다</summary>
    public static class StallDeco
    {
        public const int Lantern = 150, Flag = 350;
        public static bool HasLantern => StallRush.Best >= Lantern;
        public static bool HasFlag => StallRush.Best >= Flag;

        public static string NewlyUnlocked(int before, int after)
        {
            if (before < Lantern && after >= Flag) return "등롱·깃발";
            if (before < Flag && after >= Flag) return "깃발";
            if (before < Lantern && after >= Lantern) return "등롱";
            return null;
        }

        public static string NextGoal(int best) =>
            best < Lantern ? $"최고 {Lantern}점이면 좌판에 등롱이 달려요" : best < Flag ? $"최고 {Flag}점이면 좌판에 깃발이 달려요" : null;
    }
}
