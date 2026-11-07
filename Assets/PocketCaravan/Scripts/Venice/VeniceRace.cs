using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 곤돌라 경주 (2026-10-27, 제안·미니게임). 리알토 곤돌라 나루에서 15냥. 위로 흘러가는 운하를 좌우(←→/AD)로 저어
    /// 말뚝·다른 배·떠다니는 상자를 피하고 유리구슬(10점)·동전(5점)을 모은다. Space로 잠깐 힘껏 저으면 빨라지지만 노 힘이 준다.
    /// 세 번 부딪히거나 40초가 지나면 끝. 점수 10점당 1냥(최대 80냥), 처음 150점을 넘기면 "명예 사공"(50냥·노 젓기 동작).
    /// 기록: VeniceSave "race_best", "race_honor". 하는 동안 UIState.Blocking으로 이동·시간을 멈춘다.
    /// </summary>
    public class VeniceRace : MonoBehaviour
    {
        enum Kind { Pole, Boat, Crate, Bead, Coin }
        struct Thing { public Kind kind; public float x, y, w, h; public bool gone; }

        static VeniceRace inst;
        bool running, done;
        float t, scroll, speed, boatX, stamina, endShown;
        int hits, score, beads, coins;
        float nextSpawn, hurt;
        readonly List<Thing> things = new List<Thing>();
        readonly System.Random rng = new System.Random();
        const float W = 220f, H = 260f, Duration = 40f;

        public const int Fee = 15;
        public static int Best => (int)VeniceSave.Get("race_best");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceRace>() == null)
                inst = GameManager.I.gameObject.AddComponent<VeniceRace>();
        }

        void Awake() => inst = this;

        public static void Begin()
        {
            var gm = GameManager.I;
            if (inst == null || gm == null) return;
            if (gm.Player.money < Fee) { Notify.Show("경주 참가비가 모자란다.", 2.5f); return; }
            gm.Player.money -= Fee;
            inst.StartRace();
        }

        /// <summary>점검용: 돈 없이 시작하고 몇 초 뒤 상태를 볼 수 있게</summary>
        public static void TestBegin() { if (inst != null) inst.StartRace(); }
        public static string TestState => inst == null ? "없음" : $"진행 {inst.running}, 시간 {inst.t:0.0}, 점수 {inst.score}, 부딪힘 {inst.hits}, 물건 {inst.things.Count}";

        public static void TestStop() { if (inst != null && inst.running) inst.Finish(); }
        public static void TestClose() { if (inst != null && inst.done) { inst.done = false; UIState.Release(); } }

        void StartRace()
        {
            running = true; done = false;
            t = 0f; scroll = 0f; speed = 70f; boatX = W / 2f; stamina = 1f; hits = 0; score = 0; beads = 0; coins = 0; nextSpawn = .6f; hurt = 0f;
            things.Clear();
            UIState.Blocking = true;
            Sfx.Play("accept");
        }

        static bool Held(int dir)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return dir < 0 ? kb.leftArrowKey.isPressed || kb.aKey.isPressed : kb.rightArrowKey.isPressed || kb.dKey.isPressed;
#else
            return false;
#endif
        }
        static bool Boost()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
            return false;
#endif
        }

        void Update()
        {
            if (done)
            {
                UIState.Blocking = true;
                if (Time.unscaledTime > endShown && (InputReader.InteractPressed() || InputReader.CancelPressed())) { done = false; UIState.Release(); }
                return;
            }
            if (!running) return;
            UIState.Blocking = true;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            t += dt;
            bool boost = Boost() && stamina > .05f;
            stamina = Mathf.Clamp01(stamina + (boost ? -.6f : .25f) * dt);
            speed = 70f + t * 2.2f + (boost ? 55f : 0f);
            float steer = (Held(-1) ? -1f : 0f) + (Held(1) ? 1f : 0f);
            boatX = Mathf.Clamp(boatX + steer * 110f * dt, 22f, W - 22f);
            scroll += speed * dt;
            hurt = Mathf.Max(0f, hurt - dt);

            nextSpawn -= dt;
            if (nextSpawn <= 0f)
            {
                nextSpawn = Mathf.Max(.28f, .75f - t * .01f);
                double r = rng.NextDouble();
                var k = r < .3 ? Kind.Pole : r < .48 ? Kind.Boat : r < .6 ? Kind.Crate : r < .8 ? Kind.Coin : Kind.Bead;
                float w = k == Kind.Pole ? 6f : k == Kind.Boat ? 40f : k == Kind.Crate ? 14f : 9f;
                float h = k == Kind.Pole ? 18f : k == Kind.Boat ? 14f : k == Kind.Crate ? 14f : 9f;
                things.Add(new Thing { kind = k, x = 22f + (float)rng.NextDouble() * (W - 44f - w), y = -h, w = w, h = h });
            }
            var boat = new Rect(boatX - 7f, H - 52f, 14f, 34f);
            for (int i = things.Count - 1; i >= 0; i--)
            {
                var th = things[i];
                th.y += speed * dt * (th.kind == Kind.Boat ? .7f : 1f);
                if (th.y > H + 20f) { things.RemoveAt(i); continue; }
                if (!th.gone && boat.Overlaps(new Rect(th.x, th.y, th.w, th.h)))
                {
                    th.gone = true;
                    if (th.kind == Kind.Bead) { score += 10; beads++; Sfx.Play("coin", .5f); }
                    else if (th.kind == Kind.Coin) { score += 5; coins++; Sfx.Play("coin", .4f); }
                    else if (hurt <= 0f) { hits++; hurt = 1f; Sfx.Play("bump", .8f); }
                }
                things[i] = th;
            }
            score = Mathf.Max(score, 0);
            if (hits >= 3 || t >= Duration) Finish();
        }

        void Finish()
        {
            running = false;
            done = true;
            endShown = Time.unscaledTime + .8f;
            int total = score + Mathf.RoundToInt(t * 2f);
            score = total;
            NetScores.Report("gondola", total);   // 11/07 온라인 순위표 (계정으로 접속했을 때만)
            var gm = GameManager.I;
            int prize = Mathf.Min(80, total / 10);
            gm.Player.money += prize;
            bool record = total > Best;
            if (record) VeniceSave.Set("race_best", total);
            string extra = "";
            if (total >= 150 && !VeniceSave.Has("race_honor"))
            {
                VeniceSave.Set("race_honor", 1f);
                gm.Player.money += 50;
                string taught = VeniceReactions.Teach("row");
                extra = $"\n'명예 사공'이 되었다! 50냥{(taught != null ? $" · 동작 '{taught}'" : "")}";
            }
            Sfx.Play(record ? "levelup" : "success");
            resultText = $"점수 {total}  (구슬 {beads} · 동전 {coins} · 거리 {Mathf.RoundToInt(t * 2f)})\n상금 {prize}냥{(record ? "  — 새 기록!" : $"  (최고 {Best})")}{extra}";
        }

        string resultText;

        void OnGUI()
        {
            if (!running && !done) return;
            UIStyle.Begin();
            GUI.depth = -25;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            var old = GUI.color;
            CuteUI.Dim();
            var o = new Vector2(Mathf.Round((vw - W) / 2f), Mathf.Round((vh - H) / 2f + 8f));
            CuteUI.Box(new Rect(o.x - 3f, o.y - 3f, W + 6f, H + 6f), 5, CuteUI.Ink, CuteUI.Ink, 1, 2);   // 11/07 UI B안: 운하 둘레 진한 테
            // 운하 물·물결
            GUI.color = new Color(.32f, .55f, .6f);
            GUI.DrawTexture(new Rect(o.x, o.y, W, H), Texture2D.whiteTexture);
            GUI.color = new Color(.5f, .72f, .76f, .8f);
            for (int i = 0; i < 14; i++)
            {
                float y = Mathf.Repeat(i * 37f + scroll, H);
                GUI.DrawTexture(new Rect(o.x + 30 + (i * 53) % (W - 70), o.y + y, 10, 1), Texture2D.whiteTexture);
            }
            // 양쪽 물가 집 (알록달록한 벽이 흘러간다)
            var walls = new[] { new Color(.91f, .72f, .39f), new Color(.9f, .57f, .45f), new Color(.88f, .63f, .63f), new Color(.93f, .85f, .69f), new Color(.77f, .38f, .29f) };
            for (int side = 0; side < 2; side++)
                for (int i = -1; i < 8; i++)
                {
                    float y = Mathf.Repeat(i * 40f + scroll * .99f, H + 40f) - 40f;
                    var r = new Rect(side == 0 ? o.x : o.x + W - 18, o.y + y, 18, 38);
                    if (r.y < o.y) { r.height -= o.y - r.y; r.y = o.y; }
                    if (r.yMax > o.y + H) r.height = o.y + H - r.y;
                    if (r.height <= 0) continue;
                    int wi = (i * 3 + side * 2 + Mathf.FloorToInt(scroll / 40f)) % walls.Length;
                    GUI.color = walls[wi < 0 ? wi + walls.Length : wi];      // i = -1이면 음수가 나온다
                    GUI.DrawTexture(r, Texture2D.whiteTexture);
                    GUI.color = new Color(.18f, .2f, .28f);
                    if (r.height > 20) GUI.DrawTexture(new Rect(r.x + 6, r.y + 8, 5, 8), Texture2D.whiteTexture);
                }
            // 물건
            foreach (var th in things)
            {
                if (th.gone && (th.kind == Kind.Bead || th.kind == Kind.Coin)) continue;
                var r = new Rect(o.x + th.x, o.y + th.y, th.w, th.h);
                if (r.yMax < o.y || r.y > o.y + H) continue;
                switch (th.kind)
                {
                    case Kind.Pole:
                        for (int k = 0; k < 4; k++) { GUI.color = k % 2 == 0 ? new Color(.85f, .2f, .22f) : Color.white; GUI.DrawTexture(new Rect(r.x, r.y + k * th.h / 4f, th.w, th.h / 4f), Texture2D.whiteTexture); }
                        break;
                    case Kind.Boat:
                        GUI.color = new Color(.36f, .22f, .14f); GUI.DrawTexture(r, Texture2D.whiteTexture);
                        GUI.color = new Color(.62f, .42f, .26f); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), Texture2D.whiteTexture);
                        break;
                    case Kind.Crate:
                        GUI.color = new Color(.55f, .38f, .2f); GUI.DrawTexture(r, Texture2D.whiteTexture);
                        GUI.color = new Color(.75f, .56f, .32f); GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), Texture2D.whiteTexture);
                        break;
                    case Kind.Bead:
                        GUI.color = new Color(.35f, .75f, 1f); GUI.DrawTexture(r, Texture2D.whiteTexture);
                        GUI.color = Color.white; GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, 2, 2), Texture2D.whiteTexture);
                        break;
                    case Kind.Coin:
                        GUI.color = new Color(1f, .84f, .3f); GUI.DrawTexture(r, Texture2D.whiteTexture);
                        GUI.color = new Color(.8f, .6f, .15f); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, 3, 3), Texture2D.whiteTexture);
                        break;
                }
            }
            // 내 곤돌라 (검은 배 + 노 젓는 고양이)
            bool blink = hurt > 0f && Mathf.Repeat(Time.unscaledTime * 10f, 1f) < .5f;
            if (!blink)
            {
                float bx = o.x + boatX, by = o.y + H - 52f;
                GUI.color = new Color(.12f, .1f, .14f); GUI.DrawTexture(new Rect(bx - 6, by, 12, 34), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(bx - 3, by - 4, 6, 4), Texture2D.whiteTexture);
                GUI.color = new Color(.9f, .75f, .3f); GUI.DrawTexture(new Rect(bx - 6, by + 10, 12, 2), Texture2D.whiteTexture);
                GUI.color = Color.white; GUI.DrawTexture(new Rect(bx - 4, by + 18, 8, 8), Texture2D.whiteTexture);           // 하얀 고양이
                GUI.color = new Color(.85f, .2f, .25f); GUI.DrawTexture(new Rect(bx - 4, by + 17, 8, 2), Texture2D.whiteTexture);
                float oar = Mathf.Sin(Time.unscaledTime * 8f) * 4f;
                GUI.color = new Color(.55f, .38f, .2f); GUI.DrawTexture(new Rect(bx + 4, by + 16 + oar, 10, 2), Texture2D.whiteTexture);
            }
            GUI.color = old;
            // 11/07 UI B안 머리말: 종이 알약 하나에 점수 · 남은 시간 · 목숨 세 칸(빨강 = 남음)
            string scoreText = $"점수 {score}", timeText = $"남은 {Mathf.Max(0, Duration - t):0}초";
            float hw = Mathf.Round(10f + CuteUI.Width(scoreText, false, true) + 10f + CuteUI.Width(timeText, true) + 10f + 3 * 11f + 6f);
            var head = new Rect(Mathf.Round(o.x + (W - hw) / 2f), o.y - 27f, hw, 19f);
            CuteUI.Pill(head);
            float hx = CuteUI.Label(head.x + 10f, head.center.y, scoreText, CuteUI.Text, false, true) + 10f;
            hx = CuteUI.Label(hx, head.center.y + .5f, timeText, CuteUI.Sub, true) + 10f;
            for (int i = 0; i < 3; i++) CuteUI.Circle(hx + i * 11f, head.y + 5f, 9, i < 3 - hits ? CuteUI.Alert : CuteUI.Paper2, i < 3 - hits ? CuteUI.Ink : CuteUI.Line);
            // 노 힘 막대 + 키캡 안내
            CuteUI.Bar(new Rect(o.x, o.y + H + 6f, W, 8f), stamina, CuteUI.Stamina, CuteUI.StaminaHi);
            float kw = CuteUI.KeycapWidth("← →") + 3f + CuteUI.Width("젓기", true) + 12f + CuteUI.KeycapWidth("Space") + 3f + CuteUI.Width("힘껏 젓기", true);
            float kx = Mathf.Round(o.x + (W - kw) / 2f), ky = o.y + H + 18f;
            kx += CuteUI.Keycap(kx, ky, "← →") + 3f;
            kx = CuteUI.Label(kx, ky + 6.5f, "젓기", CuteUI.White, true) + 12f;
            kx += CuteUI.Keycap(kx, ky, "Space") + 3f;
            CuteUI.Label(kx, ky + 6.5f, "힘껏 젓기", CuteUI.White, true);
            if (done)
            {
                var r = new Rect(o.x + 6f, Mathf.Round(o.y + H / 2f - 46f), W - 12f, 92f);
                CuteUI.Window(r);
                CuteUI.TitlePlate(r.center.x, r.y - 11f, "경주 끝!");
                CuteUI.WrapCenter(r.x + 10f, r.y + 16f, r.width - 20f, resultText ?? "", CuteUI.Text);
                float bx = Mathf.Round(r.center.x - (CuteUI.KeycapWidth("Space") + 3f + CuteUI.Width("닫기", true)) / 2f);
                bx += CuteUI.Keycap(bx, r.yMax - 20f, "Space", true) + 3f;
                CuteUI.Label(bx, r.yMax - 13.5f, "닫기", CuteUI.Text, true);
            }
        }
    }
}
