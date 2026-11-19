using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 좌판 미니게임 "손님 부르기" (3판). 은샘 "미니게임은 직접 거래가 아니라 좌판 버프로" → 물건을 팔지 않고, 지나가는 사람을 외쳐 불러 좌판을 북적이게 한다.
    /// - 장터 길을 사람들이 오른쪽에서 왼쪽으로 지나간다 (게임의 실제 상인 그림, 걷는 4장). 머리 위 말풍선에 찾는 물건, "…"은 구경꾼.
    /// - 좌판 앞 밝은 자리에 오면 그 물건 칸 번호(1~4)나 클릭으로 "○○ 있어요!" 하고 부른다. 한가운데면 "딱!", 내리 맞히면 점수 더.
    ///   틀린 물건 · 구경꾼 부르기 · 빈자리에 외치기 · 놓치기는 연속만 끊긴다.
    /// - 45초. 갈수록 빨라지고 자주 온다. 진열한 물건은 그대로 (안 팔림, 돈 변화 없음).
    /// - 보상: 점수 단계(PlayerStall.BuzzScore 50·120·200)만큼 12시간 "북적임" → 저절로 팔리는 빈도가 오른다 (값은 그대로 시세, PlayerStall.OnHour).
    ///   최고 기록(메모 "stall:best")으로 좌판 꾸미기 (StallDeco 등롱·깃발).
    /// - 몇 번이든 (진열한 물건이 있고 아침 7시~밤 9시). 지금 북적임보다 낮으면 그대로 둔다. 사람들은 내 화면의 NPC라 한 마을 좌판 여럿이 서로 뺏지 않는다.
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
            if (phase == Phase.Play && GameManager.I != null && GameManager.I.GameEpoch == epoch) { Reward(GameManager.I); GameManager.I.SaveNow(); }
            phase = Phase.Closed; ClosedFrame = Time.frameCount;
        }

        public const string BestKey = "stall:best";
        const float Duration = 45f, ZoneHalf = 26f, PerfectHalf = 8f;
        static readonly string[] People = { "trader_tall", "venice_merchant_tall", "arabia_npc_merchant_tall", "north_p_trader_tall", "arabia_woman_calligrapher_tall" };
        Sprite[][] walk;
        StallStyle style;

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
        int epoch;   // 이 판을 시작한 게임 (도중에 불러오기·새 게임이면 다른 게임에 보상을 쓰지 않게, GameManager.GameEpoch)
        int score, served, missed, streak, bestStreak, bestBefore, buzzBefore, buzzGot; bool buzzSet;
        float shoutAt; ItemData shoutItem;
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
            style = StallStyle.Current();   // 11/19 은샘 "지역 그림에 맞춰야지": 그 지역 좌판 · 그 지역 사람들
            walk = style.People();
            LaneColors(style.id);
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
            // 11/19 은샘: 진열한 물건이 아니라 아무 교역품 넷 (진열이 하나뿐이면 늘 같은 번호라 너무 쉬웠다)
            var pool = gm.Db.items.FindAll(it => it != null && !it.questItem && it.icon != null);
            for (int i = 0; i < 4 && pool.Count > 0; i++) { int k = Random.Range(0, pool.Count); shelf.Add(pool[k]); pool.RemoveAt(k); }
            guests.Clear();
            score = served = missed = streak = bestStreak = buzzGot = 0; buzzSet = false; pop = ""; shoutItem = null; shoutAt = -9f;
            bestBefore = Best; buzzBefore = PlayerStall.BuzzLevel(gm); epoch = gm.GameEpoch;
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
            var have = shelf;
            var g = new Guest { x = LaneW + 20f, look = Random.Range(0, walk.Length), at = clock };
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
            var it = shelf[slot];
            shoutItem = it; shoutAt = clock;   // 좌판에서 "○○ 있어요!"
            var g = InZone();
            if (g == null) { Break("아직 앞에 아무도 없어요"); Sfx.Play("cancel", .4f); return; }
            g.tried = true;
            if (g.wish == null) { g.mood = Mood.Shrug; g.moodAt = clock; Break("구경만 하는 사람이에요"); Sfx.Play("cancel", .5f); return; }
            if (g.wish != it) { g.mood = Mood.Sad; g.moodAt = clock; Break("그건 안 찾는데요…"); Sfx.Play("cancel"); return; }
            // 맞혔다: 좌판에 들렀다 간다 (물건은 안 판다). 점수 = 10 + 딱 10 + 연속 (최대 +10)
            bool perfect = Mathf.Abs(g.x - ZoneX) <= PerfectHalf;
            int pts = 10 + (perfect ? 10 : 0) + Mathf.Min(streak, 5) * 2;
            score += pts; served++; streak++; bestStreak = Mathf.Max(bestStreak, streak);
            g.mood = Mood.Happy; g.moodAt = clock;
            flashAt = clock; flashX = g.x;
            Pop(perfect ? $"딱! +{pts}" : $"+{pts}", perfect ? CuteUI.Sell : CuteUI.Up);
            Sfx.Play(perfect ? "success" : "accept", perfect ? .7f : .8f);
        }

        void Break(string why) { streak = 0; Pop(why, CuteUI.Down); }
        void Pop(string s, Color32 c) { pop = s; popCol = c; popAt = clock; }

        void Finish(GameManager gm)
        {
            Reward(gm);
            phase = Phase.Done;
            Sfx.Play(buzzGot > 0 ? "levelup" : "cancel", .7f);
            gm.SaveNow();
        }

        /// <summary>이번 점수로 북적임을 걸고 최고 기록을 남긴다 (끝날 때 · 도중에 닫힐 때)</summary>
        void Reward(GameManager gm)
        {
            buzzGot = PlayerStall.BuzzOfScore(score);
            buzzSet = PlayerStall.SetBuzz(gm, buzzGot);
            if (score > bestBefore) gm.SetMemo(BestKey, score);
            var s = StallSpot.Current; if (s != null) s.RefreshDeco();
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
                bool stop = g.mood == Mood.Happy && clock - g.moodAt < 0.9f;   // 부른 사람은 좌판 앞에 잠깐 서서 구경한다
                if (!stop) g.x -= g.speed * dt;
                if (g.x < -24f)
                {
                    g.gone = true;
                    if (g.mood == Mood.Walk && !g.tried)
                    {
                        if (g.wish != null) { missed++; Break("지나가 버렸어요"); }
                        else if (g.wish == null) { score += 2; }   // 구경꾼을 그냥 보냈다
                    }
                }
            }
            int n = InputReader.NumberPressed(); if (n >= 1 && n <= shelf.Count) Pick(gm, n - 1);
            if (T >= Duration) Finish(gm);
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
            float h = phase == Phase.Done ? 200 : 270;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "손님 부르기");
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
                string[] rules = { "지나가는 사람이 좌판 앞 밝은 자리에 오면", $"찾는 물건 번호(1~{shelf.Count})를 눌러 불러요", "한가운데서 부르면 \"딱!\" · 내리 맞히면 점수 더", "\"…\" 사람은 구경꾼, 부르지 마세요", "점수만큼 12시간 좌판이 북적여요 (놀이 중엔 물건이 나가지 않아요)" };
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
            else CuteUI.LabelCenter(r.center.x, r.yMax - 12, $"번호 키 1~{shelf.Count} 또는 칸 클릭 · Esc 그만", CuteUI.Sub, true);
            if (phase == Phase.Play) DrawBuzzBar(new Rect(r.x + 16, r.yMax - 36, r.width - 32, 8));

            // 떠오르는 글 (점수 · 까닭)
            float pa = 1f - Mathf.Clamp01((clock - popAt) / 1.1f);
            if (pa > 0f && pop.Length > 0)
            {
                var old = GUI.color; GUI.color = new Color(old.r, old.g, old.b, pa);
                CuteUI.LabelCenter(lane.x + ZoneX + 60, lane.y + 14 - (1f - pa) * 8f, pop, popCol, true, true);
                GUI.color = old;
            }
        }

        // 11/19 길 색도 지역마다: 고려 풀·흙길 · 베네치아 운하·돌바닥 · 아라비아 사암 벽·모래 · 북유럽 눈
        Color32 Dirt, DirtLo, DirtHi, Grass, GrassLo;
        void LaneColors(string id)
        {
            switch (id)
            {
                case "venice": Grass = new Color32(70, 128, 140, 255); GrassLo = new Color32(96, 156, 166, 255); Dirt = new Color32(222, 210, 186, 255); DirtLo = new Color32(196, 182, 156, 255); DirtHi = new Color32(236, 226, 206, 255); break;
                case "arabia": Grass = new Color32(214, 190, 146, 255); GrassLo = new Color32(190, 164, 120, 255); Dirt = new Color32(232, 206, 150, 255); DirtLo = new Color32(212, 184, 128, 255); DirtHi = new Color32(244, 224, 176, 255); break;
                case "north": Grass = new Color32(240, 236, 244, 255); GrassLo = new Color32(218, 210, 226, 255); Dirt = new Color32(206, 194, 212, 255); DirtLo = new Color32(182, 168, 192, 255); DirtHi = new Color32(226, 218, 232, 255); break;   // 눈밭 · 밟힌 길
                default: Grass = new Color32(120, 164, 92, 255); GrassLo = new Color32(98, 140, 76, 255); Dirt = new Color32(196, 160, 112, 255); DirtLo = new Color32(176, 140, 96, 255); DirtHi = new Color32(214, 182, 136, 255); break;
            }
        }

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
            var art = style != null ? style.Art : null;   // 내 좌판 그림 2배, 아래를 길 위에 맞춘다
            if (art != null) { float aw = art.rect.width * 2f, ah = art.rect.height * 2f; CuteUI.Sprite(art, new Rect(ZoneX - aw / 2f, road + 26 - ah, aw, ah)); }
            float zx = ZoneX, feet = road + 46;
            bool live = phase == Phase.Play;
            var zone = new Rect(zx - ZoneHalf, road + 4, ZoneHalf * 2, H - road - 8);
            float glow = live ? 0.55f + Mathf.Sin(clock * 4f) * 0.15f : 0.5f;
            var oc = GUI.color; GUI.color = new Color(1f, 1f, 1f, glow * 0.6f);
            CuteUI.Box(zone, 6, CuteUI.SellSoft, CuteUI.Sell);
            GUI.color = oc;
            CuteUI.Fill(new Rect(zx - PerfectHalf, zone.yMax - 4, PerfectHalf * 2, 2), CuteUI.Sell);   // 한가운데 표시
            if (live && clock - flashAt < 0.25f) CuteUI.Box(new Rect(flashX - 14, road + 2, 28, H - road - 4), 6, CuteUI.White, CuteUI.Sell);
            if (live && shoutItem != null && clock - shoutAt < 0.8f)   // 좌판에서 외치는 말풍선 "○○ 있어요!"
            {
                string say = $"{shoutItem.displayName} 있어요!";
                float sw = CuteUI.Width(say, true) + 30, sx = Mathf.Min(ZoneX + 40, W - sw - 4);
                var sb = new Rect(sx, 6, sw, 20);
                CuteUI.Box(sb, 8, CuteUI.White, CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
                CuteUI.Fill(new Rect(sb.x - 3, sb.center.y - 1, 4, 2), CuteUI.Ink);
                if (shoutItem.icon != null) CuteUI.Sprite(shoutItem.icon, new Rect(sb.x + 4, sb.y + 2, 16, 16));
                CuteUI.Label(sb.x + 22, sb.center.y, say, CuteUI.Text, true, true);
            }

            // 손님 (뒤에서 앞 순서: x가 큰 쪽이 먼저)
            var order = new List<Guest>(guests); order.Sort((a, b) => b.x.CompareTo(a.x));
            foreach (var g in order)
            {
                if (g.gone) continue;
                var set = walk[g.look]; int f = g.mood == Mood.Happy && clock - g.moodAt < 0.9f ? 0 : (int)((clock - g.at) * 8f) % 4;
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
            if (g.mood == Mood.Happy) { if (since < 1.2f) CuteUI.Icon(CuteUI.Pic.Star4, x - 4, y - 14); return; }
            if (g.mood == Mood.Sad) { if (since < 1f) CuteUI.LabelCenter(x, y - 6, "…", CuteUI.Down, false, true); return; }
            if (g.mood == Mood.Shrug) { if (since < 1f) CuteUI.LabelCenter(x, y - 6, "?", CuteUI.Sub, false, true); return; }
            if (g.mood != Mood.Walk) return;
            var b = new Rect(Mathf.Round(x - 13), Mathf.Round(y - 24), 26, 22);
            CuteUI.Box(b, 7, CuteUI.White, CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
            CuteUI.Fill(new Rect(x - 2, b.yMax, 4, 2), CuteUI.Ink);
            if (g.wish == null) CuteUI.LabelCenter(b.center.x, b.center.y - 1, "…", CuteUI.Sub, false, true);
            else if (g.wish.icon != null) CuteUI.Sprite(g.wish.icon, new Rect(b.center.x - 8, b.center.y - 8, 16, 16));
        }

        /// <summary>진열대: 칸마다 번호 · 물건 그림 · 남은 개수. 놀이 중엔 눌러서 고른다</summary>
        void DrawShelf(GameManager gm, PlayerState p, Rect row, bool live)
        {
            float sw = 70, gap = 8, total = shelf.Count * sw + Mathf.Max(0, shelf.Count - 1) * gap;
            float sx = row.center.x - total / 2f;
            for (int i = 0; i < shelf.Count; i++)
            {
                var it = shelf[i];
                var b = new Rect(sx + i * (sw + gap), row.y, sw, 44);
                bool hover = live && b.Contains(Event.current.mousePosition);
                bool lit = live && shoutItem == it && clock - shoutAt < 0.25f;
                CuteUI.Box(b, 7, lit ? CuteUI.SellSoft : hover ? CuteUI.Paper2 : CuteUI.White, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
                CuteUI.Keycap(b.x + 4, b.y + 4, (i + 1).ToString(), true);
                if (it.icon != null) CuteUI.Sprite(it.icon, new Rect(b.center.x - 14, b.y + 4, 28, 28));
                CuteUI.LabelCenter(b.center.x, b.yMax - 7, TradeLedgerUI.Fit(it.displayName, sw - 6, true, false), CuteUI.Sub, true);
                if (live && CuteUI.Hit(b, GUIContent.none, GUIStyle.none)) Pick(gm, i);
            }
        }

        /// <summary>북적임 막대: 점수가 단계 문턱(50·120·200)을 넘을수록 찬다</summary>
        /// <summary>점수 → 막대 위치. 11/19 은샘 "1·2·3 눈금 간격을 같게, 3 뒤에 조금 여유": 단계마다 막대의 1/4씩, 3단계 뒤 1/4은 넘친 점수</summary>
        static float BuzzFill(int score)
        {
            var t = PlayerStall.BuzzScore; int n = t.Length - 1;   // 문턱 셋
            for (int i = 1; i <= n; i++)
                if (score < t[i]) return (i - 1 + (score - t[i - 1]) / (float)(t[i] - t[i - 1])) / (n + 1);
            float over = t[n] - t[n - 1];   // 3단계 뒤: 앞 칸과 같은 폭만큼 더 차면 꽉
            return Mathf.Clamp01((n + Mathf.Clamp01((score - t[n]) / over)) / (n + 1));
        }

        /// <summary>북적임 막대: [북적임] [막대 · 눈금 1 2 3] [n단계] 를 한 덩어리로 창 가운데에</summary>
        void DrawBuzzBar(Rect area)
        {
            int lv = PlayerStall.BuzzOfScore(score), n = PlayerStall.BuzzScore.Length - 1;
            float lw = CuteUI.Width("북적임", true, true), rw = CuteUI.Width("3단계", true, true), bw = Mathf.Min(220f, area.width - lw - rw - 24);
            float x0 = Mathf.Round(area.center.x - (lw + 8 + bw + 8 + rw) / 2f);
            var bar = new Rect(x0 + lw + 8, area.y, bw, area.height);
            CuteUI.Label(x0, bar.center.y, "북적임", CuteUI.Sub, true, true);
            CuteUI.Bar(bar, BuzzFill(score), CuteUI.Sell, CuteUI.SellSoft);
            for (int i = 1; i <= n; i++)
            {
                float x = Mathf.Round(bar.x + bar.width * i / (n + 1f));
                CuteUI.Fill(new Rect(x - 1, bar.y - 2, 2, bar.height + 4), CuteUI.Ink);
                CuteUI.LabelCenter(x, bar.y - 7, i.ToString(), lv >= i ? CuteUI.Sell : CuteUI.Zero, true, true);
            }
            if (lv > 0) CuteUI.Label(bar.xMax + 8, bar.center.y, $"{lv}단계", CuteUI.Sell, true, true);
        }

        void DrawDone(Rect r)
        {
            bool record = score > bestBefore && score > 0;
            CuteUI.LabelCenter(r.center.x, r.y + 34, record ? "새 기록!" : "손님 부르기 끝", record ? CuteUI.Sell : CuteUI.Text, false, true);
            CuteUI.LabelCenter(r.center.x, r.y + 58, $"점수 {score}  ·  최고 {Mathf.Max(score, bestBefore)}", CuteUI.Text, false, true);
            CuteUI.LabelCenter(r.center.x, r.y + 78, $"부른 사람 {served}명 · 가장 긴 연속 {bestStreak}명 · 지나간 사람 {missed}명", CuteUI.Sub, true);
            // 북적임 결과
            string buzz = buzzGot <= 0 ? $"{PlayerStall.BuzzScore[1]}점부터 좌판이 북적여요"
                : buzzSet ? $"좌판 북적임 {buzzGot}단계 · {PlayerStall.BuzzHours:0}시간 동안 {PlayerStall.BuzzEffect(buzzGot)} 팔려요"
                : $"지금 북적임 {buzzBefore}단계가 더 커서 그대로 둬요";
            CuteUI.LabelCenter(r.center.x, r.y + 102, buzz, buzzGot > 0 && buzzSet ? CuteUI.Up : CuteUI.Sub, true, buzzGot > 0 && buzzSet);
            CuteUI.LabelCenter(r.center.x, r.y + 118, "(값은 그대로 장터 시세)", CuteUI.Sub, true);
            // 꾸미기: 이번에 열린 것 · 다음 것
            string unlocked = StallDeco.NewlyUnlocked(bestBefore, Mathf.Max(score, bestBefore));
            string next = StallDeco.NextGoal(Mathf.Max(score, bestBefore));
            if (unlocked != null) CuteUI.LabelCenter(r.center.x, r.y + 140, $"좌판에 {unlocked}이 달렸어요!", CuteUI.Up, false, true);
            else if (next != null) CuteUI.LabelCenter(r.center.x, r.y + 140, next, CuteUI.Sub, true);
            var b = new Rect(r.center.x - 50, r.yMax - 32, 100, 22);
            if (CuteUI.Button(b, "닫기")) Shut();
        }
    }

    /// <summary>11/19 손님 부르기 최고 점수로 열리는 좌판 꾸미기 (돈·거래와 상관없는 순수 보상). 문턱을 넘으면 맵 위 내 좌판에 달린다</summary>
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
