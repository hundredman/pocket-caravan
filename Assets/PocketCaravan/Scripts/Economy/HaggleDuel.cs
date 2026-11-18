using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 흥정 미니게임 "저울 흥정" (2026-11-15 은샘 4판, Fable 설계 A안). 살 때마다 꼭 한다 (TownMarket).
    /// 흔들리는 저울 바늘을 Space(또는 클릭)로 멈춘다. 눈금은 왼쪽부터 회색(정가) → 초록(점점 싸짐) → 좁은 금색(최대) → 빨강(+2%).
    /// 금색 바로 옆이 빨강이라 "안전한 초록에서 멈출까, 금색 끝을 노릴까"를 매번 고른다. 참고: Gears of War Active Reload.
    /// - 금색: 신용 최대치(ReputationPricing.MaxHaggle) 그대로. 초록: 최대치 × 위치^1.5 × 0.85. 회색 0. 빨강 −2%(살 때 +2%), 거래는 그대로.
    /// - 바늘 빠르기 · 가감속 · 시작 자리 · 빨강 시작(78~90%)은 날짜·마을·물건·거래 횟수로 정해 판마다 다르다. 결과는 손 실력이라 저장을 되돌려도 소용없다.
    /// - 상인 성격: 깐깐함 빠르고 금색 좁음 · 너그러움 느리고 넓음. 3왕복 동안 안 누르면 정가.
    /// - 빨강을 두 번 잇달아 밟으면 다음 한 판은 바늘이 10% 느리다.
    /// - 11/18 은샘: 처음 한 번은 바늘이 서 있고 아래에 색깔별 뜻을 보여 준다. Space를 누르면 그때 바늘이 움직인다 (메모 haggle:intro)
    /// 장터 창 위에 그리는 창이라 UIState는 장터가 그대로 쥔다.
    /// </summary>
    public class HaggleDuel : MonoBehaviour
    {
        public const float FailPenalty = 0.02f;
        const float GreyEnd = 0.25f, AutoTrips = 3f;

        static HaggleDuel inst;
        public static bool IsOpen => inst != null && inst.open;
        /// <summary>창을 닫은 프레임 (장터가 같은 Esc·Space를 또 받지 않게)</summary>
        public static int ClosedFrame = -1;
        static int redStreak;

        bool open; int openedFrame; float openedAt;
        bool intro;   // 11/18 처음 한 번: 설명을 보여 주고 바늘은 Space를 누를 때까지 멈춰 있다
        const string IntroMemo = "haggle:intro";
        TownData town; ItemData item; bool selling; int count; float max; int baseTotal; int temperament;
        bool bandit;   // 11/15 은샘: 도적 흥정 (통행세를 깎는다. 금색 반값 · 빨강 ×1.5)
        Action<float, bool> done;
        // 판마다 다른 값
        float period, phase0, redStart, goldW, ease;
        // 결과
        bool stopped; float stopX, stopAt; float result; bool failed; int zone;   // zone: 0 회색 1 초록 2 금색 3 빨강
        Sprite merchant;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<HaggleDuel>() != null) return;
            var go = new GameObject("HaggleDuel"); DontDestroyOnLoad(go); inst = go.AddComponent<HaggleDuel>();
        }
        void Awake() => inst = this;

        /// <summary>흥정 시작. baseTotal = 흥정 전 총액. done(할인율, 빨강이었는지): 할인율은 −0.02~max. seedSalt로 같은 날 같은 물건도 거래마다 다르게</summary>
        public static bool Open(TownData town, ItemData item, bool selling, int count, float max, int baseTotal, int seedSalt, Action<float, bool> done)
        {
            if (inst == null || inst.open || town == null || item == null || count < 1 || max <= 0f) return false;
            inst.Begin(town, item, selling, count, max, baseTotal, seedSalt, done);
            return true;
        }

        /// <summary>11/15 은샘: 도적 흥정. 같은 저울로 통행세를 깎는다. 초록 최대 −42% · 금색 반값 · 빨강 1.5배. done(낼 통행세)</summary>
        public static bool OpenBandit(Sprite face, int toll, string mapId, Action<int> done)
        {
            if (inst == null || inst.open || toll < 1) return false;
            var gm = GameManager.I; int day = gm != null ? gm.Clock.day : 0;
            var rng = new System.Random((int)(Hash($"bandit|{day}|{mapId}|{toll}") & 0x7fffffff));
            var h = inst;
            h.town = null; h.item = null; h.selling = false; h.count = 1; h.max = 0.5f; h.baseTotal = toll; h.bandit = true;
            h.done = (d, f) => done?.Invoke(h.PriceFor(d));
            h.temperament = 0;
            h.period = 1.1f * (0.92f + (float)rng.NextDouble() * 0.16f);
            h.goldW = 0.08f;
            h.redStart = 0.78f + (float)rng.NextDouble() * 0.12f;
            h.phase0 = (float)rng.NextDouble() * 0.3f;
            h.ease = (float)rng.NextDouble();
            h.stopped = false; h.failed = false; h.result = 0f; h.zone = 0;
            h.merchant = face;
            h.open = true; h.openedFrame = Time.frameCount; h.openedAt = Time.unscaledTime; h.intro = !IntroSeen();
            Sfx.Play("open");
            return true;
        }

        void Begin(TownData t, ItemData it, bool sell, int n, float mx, int total, int salt, Action<float, bool> cb)
        {
            town = t; item = it; selling = sell; count = n; max = mx; baseTotal = total; done = cb; bandit = false;
            var gm = GameManager.I; int day = gm != null ? gm.Clock.day : 0;
            var rng = new System.Random((int)(Hash($"{day}|{t.id}|{it.id}|{(sell ? 1 : 0)}|{salt}") & 0x7fffffff));
            temperament = HaggleRules.Temperament(t.id);
            period = temperament < 0 ? 0.9f : temperament > 0 ? 1.4f : 1.1f;
            period *= 0.92f + (float)rng.NextDouble() * 0.16f;
            if (redStreak >= 2) period *= 1.1f;
            goldW = temperament < 0 ? 0.06f : temperament > 0 ? 0.11f : 0.08f;
            redStart = 0.78f + (float)rng.NextDouble() * 0.12f;
            phase0 = (float)rng.NextDouble() * 0.3f;            // 왼쪽 근처에서 출발
            ease = (float)rng.NextDouble();                     // 0 고르게 · 1 양 끝에서 머뭇
            stopped = false; failed = false; result = 0f; zone = 0;
            merchant = Resources.Load<Sprite>("Haggle/merchant_" + OriginPerks.Civ(t.region)) ?? Resources.Load<Sprite>("Haggle/merchant_center");
            open = true; openedFrame = Time.frameCount; openedAt = Time.unscaledTime; intro = !IntroSeen();
            Sfx.Play("open");
        }

        static bool IntroSeen() { var gm = GameManager.I; return gm == null || gm.GetMemo(IntroMemo, 0f) >= 1f; }

        /// <summary>설명을 닫고 바늘을 출발시킨다</summary>
        void StartNeedle()
        {
            intro = false;
            openedAt = Time.unscaledTime - 0.1f;   // 0.15초 뒤 출발
            var gm = GameManager.I; if (gm != null) gm.SetMemo(IntroMemo, 1f);
            Sfx.Play("accept");
        }

        float GoldStart => redStart - goldW;

        /// <summary>바늘 자리 0~1 (시간 함수, 프레임과 무관)</summary>
        float NeedleAt(float t)
        {
            float u = Mathf.Repeat(t / period + phase0, 2f);
            float lin = u <= 1f ? u : 2f - u;
            float smooth = lin * lin * (3f - 2f * lin);
            return Mathf.Lerp(lin, smooth, ease * 0.6f);
        }

        int ZoneOf(float x) => x < GreyEnd ? 0 : x < GoldStart ? 1 : x < redStart ? 2 : 3;

        float DiscountAt(float x)
        {
            switch (ZoneOf(x))
            {
                case 0: return 0f;
                case 1: return max * Mathf.Pow(Mathf.InverseLerp(GreyEnd, GoldStart, x), 1.5f) * 0.85f;
                case 2: return max;
                default: return bandit ? -0.5f : -FailPenalty;
            }
        }

        int PriceFor(float d) => Mathf.Max(1, Mathf.RoundToInt(baseTotal * (selling ? 1f + d : 1f - d)));

        void Stop(float x)
        {
            if (stopped) return;
            stopped = true; stopX = x; stopAt = Time.unscaledTime;
            zone = ZoneOf(x);
            result = Mathf.Round(DiscountAt(x) * 200f) / 200f;   // 0.5% 단위
            failed = zone == 3;
            if (!bandit) redStreak = failed ? redStreak + 1 : 0;
            Sfx.Play(zone == 2 ? "coin" : zone == 3 ? "bump" : zone == 1 ? "accept" : "move", zone == 0 ? .6f : 1f);
            if (zone == 2) Sfx.Play("success", .6f);
        }

        void Close(bool apply)
        {
            if (!open) return;
            open = false; ClosedFrame = Time.frameCount;
            var cb = done; done = null;
            if (apply) cb?.Invoke(result, failed);
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            float t = Time.unscaledTime - openedAt - 0.25f;   // 패널이 열린 뒤 바늘 출발
            if (intro)
            {
                if (!bandit && InputReader.CancelPressed()) { Close(false); Sfx.Play("cancel"); return; }
                if (t > 0f && InputReader.InteractPressed()) StartNeedle();
                return;
            }
            if (!stopped)
            {
                if (!bandit && InputReader.CancelPressed()) { Close(false); Sfx.Play("cancel"); return; }   // 도적 앞에서는 물러설 수 없다
                if (t > 0f && InputReader.InteractPressed()) { Stop(NeedleAt(t)); return; }
                if (t > period * 2f * AutoTrips) { Stop(0f); }   // 안 누르면 정가
                return;
            }
            if (Time.unscaledTime - stopAt > 0.75f) Close(true);   // 결과를 잠깐 보여 주고 닫힌다
        }

        // ---------- 그리기 ----------
        // 요소는 셋: 상인(왼쪽) · 저울 눈금과 바늘(가운데) · 큰 값(오른쪽 위). 글자는 값 숫자뿐.
        const float W = 340f, H = 132f;
        static readonly Color32 Grey = new Color32(0xD9, 0xD2, 0xCC, 255), GreenLo = new Color32(0xB7, 0xE4, 0xA8, 255), GreenHi = new Color32(0x3F, 0xB8, 0x5A, 255),
            Gold = new Color32(0xFF, 0xCF, 0x33, 255), GoldHi = new Color32(0xFF, 0xF1, 0xA8, 255), Red = new Color32(0xF0, 0x55, 0x5A, 255),
            Wood = new Color32(0xC9, 0x8A, 0x4B, 255), WoodLo = new Color32(0x8E, 0x5A, 0x2C, 255), Brass = new Color32(0xE0, 0xA8, 0x3A, 255),
            GoldEdge = new Color32(0xB0, 0x7D, 0x00, 255);

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -30;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, now = Time.unscaledTime;
            CuteUI.Fill(new Rect(0, 0, vw, vh), new Color32(30, 15, 35, 110));
            float appear = Mathf.Clamp01((now - openedAt) / .16f), ea = 1f - (1f - appear) * (1f - appear);
            float since = stopped ? now - stopAt : 0f;
            float shake = stopped && zone == 3 && since < .3f ? Mathf.Round(Mathf.Sin(since * 70f) * 3f) : 0f;
            var r = new Rect(Mathf.Round((vw - W) / 2f) + shake, Mathf.Round((vh - H) / 2f + (1f - ea) * 12f), W, H);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 6, "흥정");

            // 상인
            var face = new Rect(r.x + 14, r.y + 16, 66, 84);
            CuteUI.Box(face, 8, CuteUI.Paper2, CuteUI.Ink, 2);   // 11/15 은샘: 상인 그림 둘레 선
            float hop = stopped && zone == 2 && since < .3f ? -Mathf.Round(Mathf.Sin(since / .3f * Mathf.PI) * 5f) : 0f;
            float bob = Mathf.Round(Mathf.Sin(now * 2.4f));
            if (merchant != null) CuteUI.Sprite(merchant, new Rect(face.center.x - 24, face.y + 10 + bob + hop, 48, 72));
            CuteUI.Fill(new Rect(face.x + 1, face.yMax - 14, face.width - 2, 13), Wood);
            CuteUI.Fill(new Rect(face.x + 1, face.yMax - 14, face.width - 2, 1), new Color32(0xE6, 0xB0, 0x78, 255));
            if (stopped)
            {
                string emo = bandit ? (zone == 2 ? "에잇!" : zone == 3 ? "뭐라고?!" : zone == 1 ? "쳇" : "…")
                    : zone == 2 ? "졌소!" : zone == 3 ? "!!" : zone == 1 ? "흠" : "…";
                Color32 ec = zone == 2 ? CuteUI.Up : zone == 3 ? CuteUI.Down : CuteUI.Sub;
                float bw = CuteUI.Width(emo, true, true) + 12, by = face.y - 2 - Mathf.Min(since, .2f) * 20f;
                var b = new Rect(face.xMax - bw + 6, by, bw, 15);
                CuteUI.Box(b, 6, CuteUI.White, CuteUI.Ink, 1, 1);
                CuteUI.LabelCenter(b.center.x, b.center.y, emo, ec, true, true);
            }

            // 값 (오른쪽 위, 크게) — 바늘이 가리키는 값이 실시간으로 보인다
            float t = intro ? 0f : now - openedAt - 0.25f;
            float x = stopped ? stopX : Mathf.Max(0f, t) > 0f ? NeedleAt(t) : NeedleAt(0f);
            float d = stopped ? result : DiscountAt(x);
            int price = PriceFor(d);
            int diff = price - baseTotal; bool good = selling ? diff > 0 : diff < 0;
            Color32 pc = diff == 0 ? CuteUI.Text : good ? CuteUI.Up : CuteUI.Down;
            float sx = r.x + 92, sw = r.xMax - 16 - sx;
            float pop = stopped && since < .25f ? -Mathf.Round(Mathf.Sin(since / .25f * Mathf.PI) * 4f) : 0f;   // 멈추면 값이 통 튄다
            float px = CuteUI.Label(sx, r.y + 30 + pop, $"{price}냥", pc, false, true);
            // 11/15 은샘: 취소선 대신 정가와의 차이를 적는다
            if (diff != 0) CuteUI.Label(px + 8, r.y + 31, $"{(bandit ? "통행세" : "정가")} {baseTotal}냥 · {(diff > 0 ? "+" : "−")}{Mathf.Abs(diff)}냥", CuteUI.Sub, true);
            string what = bandit ? "통행세" : $"{item.displayName} ×{count}";
            CuteUI.LabelRight(r.xMax - 16, r.y + 30, what, CuteUI.Sub, true);

            // 저울 눈금
            var bar = new Rect(sx, r.y + 62, sw, 16);
            DrawScale(bar, now);
            // 바늘
            float nx = Mathf.Round(bar.x + bar.width * x);
            float settle = stopped ? Mathf.Sin(since * 30f) * Mathf.Max(0f, 1f - since * 4f) * 2f : 0f;   // 멈출 때 덜컥
            nx += Mathf.Round(settle);
            CuteUI.Fill(new Rect(nx - 1, bar.y - 9, 3, bar.height + 14), CuteUI.Ink);
            bool onGold = stopped ? zone == 2 : ZoneOf(x) == 2;   // 11/17 기우: 금색 위를 지나는 동안에도 추가 빛난다 (지금 누르면 된다)
            CuteUI.Fill(new Rect(nx, bar.y - 8, 1, bar.height + 12), onGold ? Gold : CuteUI.White);
            // 바늘 머리 (작은 추)
            CuteUI.Circle(nx - 5, bar.y - 16, 11, onGold ? Gold : Brass, CuteUI.Ink, 1, 1);
            // 받침 (ㅗ): 11/15 은샘 — 다른 그림처럼 테두리를 두르고 저울 막대 테두리에 붙인다
            float cx = Mathf.Round(bar.x + bar.width / 2f), top = bar.yMax + 2;   // 저울 막대 테두리 아래 끝
            CuteUI.Fill(new Rect(cx - 3, top - 1, 6, 8), CuteUI.Ink);            // 기둥 테두리 (막대 테두리와 이어짐)
            CuteUI.Fill(new Rect(cx - 15, top + 6, 30, 6), CuteUI.Ink);          // 바닥 테두리
            CuteUI.Fill(new Rect(cx - 14, top + 7, 28, 4), WoodLo);              // 바닥
            CuteUI.Fill(new Rect(cx - 14, top + 7, 28, 1), Wood);                // 바닥 윗면 빛
            CuteUI.Fill(new Rect(cx - 2, top, 4, 8), WoodLo);                    // 기둥 (바닥과 한 덩어리)

            // 11/17 기우: 양 끝에 정가 ↔ 욕심 (어디로 갈수록 무엇인지)
            CuteUI.Label(bar.x, bar.yMax + 12, bandit ? "그대로" : "정가", CuteUI.Sub, true);
            CuteUI.LabelRight(bar.xMax, bar.yMax + 12, "욕심", CuteUI.Down, true);

            // 결과 한 줄 또는 첫 판 안내
            float ly = bar.yMax + 26;
            if (stopped)
            {
                string res = bandit ? (zone == 2 ? "반값!" : zone == 3 ? "화났다 ×1.5" : zone == 1 ? $"−{result * 100f:0.#}%" : "그대로")
                    : zone == 2 ? $"최고! {(selling ? "+" : "−")}{max * 100f:0.#}%" : zone == 3 ? $"욕심이 지나쳤다 {(selling ? "−" : "+")}2%"
                    : zone == 1 ? $"{(selling ? "+" : "−")}{result * 100f:0.#}%" : "정가";
                Color32 rc = zone == 2 ? new Color32(0xC9, 0x8A, 0x00, 255) : zone == 3 ? CuteUI.Down : zone == 1 ? CuteUI.Up : CuteUI.Sub;
                CuteUI.LabelCenter(bar.center.x, ly, res, rc, false, true);
            }
            else   // 11/15 은샘: 멈추기 전까지 늘 보인다
            {
                string kl = intro ? "흥정 시작" : "멈추기";
                float kx = bar.center.x - (CuteUI.KeycapWidth("Space") + 4 + CuteUI.Width(kl, true)) / 2f;
                if (intro) { float pulse = .5f + .5f * Mathf.Sin(now * 5f); var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * (.55f + .45f * pulse)); kx += CuteUI.Keycap(kx, ly - 6.5f, "Space") + 4; GUI.color = oc; }
                else kx += CuteUI.Keycap(kx, ly - 6.5f, "Space") + 4;
                CuteUI.Label(kx, ly, kl, intro ? CuteUI.Text : CuteUI.Sub, true, intro);
            }
            if (intro) DrawIntro(r, now);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && intro && now - openedAt > .25f) StartNeedle();
            else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !stopped && t > 0f) Stop(NeedleAt(t));
            // 장터 창이 클릭을 받지 않게
            if (Event.current.isMouse || Event.current.type == EventType.ScrollWheel) Event.current.Use();
        }

        /// <summary>11/18 처음 한 번: 창 아래에 붙는 설명 (색깔마다 무슨 뜻인지)</summary>
        void DrawIntro(Rect win, float now)
        {
            string good = bandit ? "통행세가 줄어요" : selling ? "값을 더 받아요" : "값이 깎여요";
            var rows = new (Color32 c, string s)[]
            {
                (GreenHi, $"초록에서 멈추면 {good}. 오른쪽일수록 더"),
                (Gold, bandit ? "금색에서 멈추면 반값!" : "금색에서 멈추면 가장 크게"),
                (Red, bandit ? "빨강은 화를 내서 1.5배" : $"빨강은 욕심이 지나쳐 2% {(selling ? "덜 받아요" : "비싸져요")}"),
            };
            const float rowH = 15f;
            var p = new Rect(win.x + 18, win.yMax + 6, win.width - 36, 22 + rows.Length * rowH + 6);
            float k = Mathf.Clamp01((now - openedAt) / .25f);
            var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * k);
            CuteUI.Box(p, 8, CuteUI.White, CuteUI.Ink, 1, 2);
            // 꼬리 (창을 가리킨다)
            CuteUI.Fill(new Rect(p.center.x - 4, p.y - 3, 8, 3), CuteUI.Ink);
            CuteUI.Fill(new Rect(p.center.x - 3, p.y - 2, 6, 3), CuteUI.White);
            CuteUI.LabelCenter(p.center.x, p.y + 12, "흔들리는 바늘을 Space로 멈춰요", CuteUI.Text, true, true);
            float y = p.y + 26;
            foreach (var row in rows)
            {
                CuteUI.Box(new Rect(p.x + 14, y - 4, 16, 9), 3, row.c, CuteUI.Ink);
                CuteUI.Label(p.x + 36, y + .5f, row.s, CuteUI.Text, true);
                y += rowH;
            }
            GUI.color = oc;
        }

        void DrawScale(Rect bar, float now)
        {
            CuteUI.Box(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), 4, Wood, CuteUI.Ink);
            var inner = new Rect(bar.x, bar.y, bar.width, bar.height);
            // 11/15 은샘: 칸 사이 선 없이 회색 → 초록 → 금색 → 빨강을 그라데이션으로. 판정 경계는 그대로(ZoneOf)
            float shine = 0.5f + 0.5f * Mathf.Sin(now * 8f);
            var gold = Color32.Lerp(Gold, GoldHi, shine * 0.5f);
            float blend = 0.03f;
            for (float px = 0; px < inner.width; px += 1f)
            {
                float x = (px + 0.5f) / inner.width;
                Color32 c;
                float gm = GoldStart + goldW * 0.5f;
                if (x < GreyEnd - blend) c = Grey;
                else if (x < GreyEnd + blend) c = Color32.Lerp(Grey, GreenLo, Mathf.InverseLerp(GreyEnd - blend, GreyEnd + blend, x));
                else if (x < GoldStart - blend) c = Color32.Lerp(GreenLo, GreenHi, Mathf.InverseLerp(GreyEnd + blend, GoldStart - blend, x));
                else if (x < gm) c = Color32.Lerp(GreenHi, gold, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(GoldStart - blend, gm, x)));
                else if (x < redStart + blend) c = Color32.Lerp(gold, Red, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(gm, redStart + blend, x)));
                else c = Red;
                CuteUI.Fill(new Rect(inner.x + px, inner.y, 1, inner.height), c);
            }
            // 11/17 기우: 금색 칸은 번지지 않게 또렷이 (테두리) — 빨강도 금색 끝에서 바로 시작
            float g0 = Mathf.Round(inner.x + inner.width * GoldStart), g1 = Mathf.Round(inner.x + inner.width * redStart);
            CuteUI.Fill(new Rect(g1, inner.y, inner.xMax - g1, inner.height), Red);
            CuteUI.Fill(new Rect(g0, inner.y, g1 - g0, inner.height), gold);
            CuteUI.Fill(new Rect(g0 - 1, inner.y, 1, inner.height), GoldEdge);
            CuteUI.Fill(new Rect(g1, inner.y, 1, inner.height), GoldEdge);
            CuteUI.Fill(new Rect(inner.x, inner.y, inner.width, 2), new Color32(255, 255, 255, 70));
            // 눈금
            for (int i = 1; i < 10; i++) CuteUI.Fill(new Rect(Mathf.Round(inner.x + inner.width * i / 10f), inner.yMax - 4, 1, 4), new Color32(42, 26, 46, 70));
        }

        static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return h;
        }
    }
}
