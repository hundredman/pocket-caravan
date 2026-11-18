using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 위 가운데 작은 시계 (2026-11-18 은샘 C안). 그리기만 하고 진행·저장과 상관없다.
    /// - "N일째 · 오후 2:20"(10분 단위) + 장날까지 남은 날(D-2, 당일은 "장날").
    /// - 왼쪽 반원: 해가 새벽 5시에 수평선 아래에서 떠올라 반원을 따라 지나고 저녁 7시에 진다. 밤에는 달이 같은 길로 뜨고 진다.
    ///   해·달은 수평선(가로줄) 아래로는 잘려 보이지 않는다. 달은 초승달 모양 그림 (빈 칸이 줄을 가리지 않게).
    /// - 시계를 누르면 아래로 5일 달력(이번 장 돌이: 오늘 · 장날 · 지난 날)과 오늘 큰 장 마을이 펼쳐진다. 다시 누르면 접힌다.
    /// 다른 화면 위에 그리지 않도록 깊이는 상태 카드(HUD)와 같고, 창이 열리면 누를 수 없다. 기념 사진 동안 감춘다.
    /// </summary>
    public class HudClock : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<HudClock>() != null) return;
            var go = new GameObject("HudClock"); DontDestroyOnLoad(go); go.AddComponent<HudClock>();
        }

        const float W = 132f, H = 30f, Rise = 5f, Set = 19f, ArcR = 15f;
        static Texture2D sunTex, moonTex;
        bool open; float openK;

        void Update()
        {
            if (UIState.Blocking || UIState.PhotoMode) open = false;
            openK = Mathf.MoveTowards(openK, open ? 1f : 0f, Time.unscaledDeltaTime / .18f);
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null || UIState.PhotoMode || gm.Clock == null) return;
            UIStyle.Begin();
            GUI.depth = openK > 0f ? -5 : -2;   // 달력을 펼치면 할 일 카드보다 위
            var clock = gm.Clock;
            float vw = UIStyle.VirtualWidth, cx = Mathf.Round(vw / 2f);
            float y = CuteUI.TopStack(0, H + 64f * openK) - 4f;   // 위 가운데 맨 위. 다른 위 알약(상인회·축제 등)은 이 아래로 쌓인다
            var r = new Rect(cx - W / 2f, y, W, H);
            ScreenZones.Mark("hud.clock", new Rect(r.x - 2, r.y, r.width + 4, r.height + 2 + (openK > 0f ? 64f * openK : 0f)));
            bool hover = !UIState.Blocking && r.Contains(Event.current.mousePosition);
            CuteUI.Box(r, 9, hover || open ? CuteUI.Paper2 : CuteUI.Paper, open ? CuteUI.Interact : CuteUI.Ink, 1, 1, CuteUI.SoftShadow);

            DrawSky(r, clock);

            // 날짜 · 시각
            int hh = clock.Hour, mm = (int)(clock.minuteOfDay % 60f) / 10 * 10;
            string ampm = hh < 12 ? "오전" : "오후";
            int h12 = (hh + 11) % 12 + 1;
            CuteUI.Label(r.x + 45, r.y + 10, $"{clock.day}일째", CuteUI.Text, false, true);
            CuteUI.Label(r.x + 45, r.y + 22, $"{ampm} {h12}:{mm:00}", CuteUI.Sub, true);
            // 장날까지
            int left = DaysToMarket(clock);
            string chip = left == 0 ? "장날" : $"D-{left}";
            float cw = Mathf.Round(CuteUI.Width(chip, true, true) + 10);
            var c = new Rect(r.xMax - cw - 6, r.y + 8, cw, 14);
            CuteUI.Box(c, 5, left == 0 ? CuteUI.Deco : CuteUI.SellSoft, CuteUI.Sell, 1);
            CuteUI.LabelCenter(c.center.x, c.center.y, chip, left == 0 ? CuteUI.Ink : CuteUI.Sell, true, true);

            if (!UIState.Blocking && GUI.Button(r, GUIContent.none, GUIStyle.none)) { open = !open; Sfx.Play(open ? "open" : "move", .4f); }
            if (openK > 0f) DrawCalendar(r, gm, clock);
        }

        /// <summary>장날까지 남은 날 (오늘이 장날이면 0)</summary>
        static int DaysToMarket(GameClock clock)
        {
            int n = Mathf.Max(1, clock.marketDayInterval);
            int rem = clock.day % n;
            return rem == 0 ? 0 : n - rem;
        }

        // ---------------- 해 · 달 ----------------

        void DrawSky(Rect r, GameClock clock)
        {
            float baseY = r.y + 24f, ax = r.x + 22f;   // 수평선 높이 · 반원 가운데
            float h = clock.minuteOfDay / 60f;
            bool day = h >= Rise && h < Set;
            // 0~1: 떠서 질 때까지. 밤은 19시~다음 날 5시 (10시간)
            float p = day ? (h - Rise) / (Set - Rise) : ((h - Set + 24f) % 24f) / (24f - (Set - Rise));
            // 반원 (옅은 선)
            var arcCol = new Color32(0xD2, 0xB2, 0x84, 255);
            for (int i = 0; i <= 28; i++)
            {
                float a = Mathf.PI * i / 28f;
                CuteUI.Fill(new Rect(Mathf.Round(ax - Mathf.Cos(a) * ArcR), Mathf.Round(baseY - Mathf.Sin(a) * ArcR), 1, 1), arcCol);
            }
            // 해·달: 양 끝에서는 수평선 아래(6칸)에서 올라오고 내려간다 → 줄 아래는 잘라서 뜨고 지는 모습
            float t = Mathf.Clamp01(p);
            float x = ax - Mathf.Cos(t * Mathf.PI) * ArcR;
            // 반원 위를 지나되, 처음·끝 7% 동안만 수평선 아래(5칸)에서 올라오고 내려간다 (약 1시간: 숨었다 → 반쯤 → 다 보임)
            float dip = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f - t) / .07f);
            float yy = baseY - Mathf.Sin(t * Mathf.PI) * ArcR + dip * 5f;
            var tex = day ? Sun() : Moon();
            var clip = new Rect(r.x + 2, r.y + 2, 40f, baseY - (r.y + 2));   // 수평선 위만 보인다
            GUI.BeginGroup(clip);
            GUI.DrawTexture(new Rect(Mathf.Round(x - 4.5f - clip.x), Mathf.Round(yy - 4.5f - clip.y), 9, 9), tex);
            GUI.EndGroup();
            // 수평선
            CuteUI.Fill(new Rect(ax - ArcR - 2f, baseY, ArcR * 2f + 5f, 1), CuteUI.Ink);
        }

        static Texture2D Sun()
        {
            if (sunTex != null) return sunTex;
            sunTex = Make((x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(4f, 4f));
                return d <= 2.2f ? new Color32(0xFF, 0xE2, 0x7A, 255) : d <= 4.3f ? new Color32(0xFF, 0xB0, 0x20, 255) : new Color32(0, 0, 0, 0);
            });
            return sunTex;
        }

        /// <summary>초승달: 둥근 달에서 오른쪽 위로 비킨 원을 파낸 모양 (파낸 곳은 투명이라 뒤 선이 그대로 보인다)</summary>
        static Texture2D Moon()
        {
            if (moonTex != null) return moonTex;
            moonTex = Make((x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(4f, 4f));
                float cut = Vector2.Distance(new Vector2(x, y), new Vector2(6.5f, 5.5f));
                if (d > 4.3f || cut < 3.6f) return new Color32(0, 0, 0, 0);
                return d > 3.4f ? new Color32(0xB0, 0x8A, 0x1E, 255) : new Color32(0xF5, 0xD9, 0x6B, 255);
            });
            return moonTex;
        }

        static Texture2D Make(System.Func<int, int, Color32> px)
        {
            var t = new Texture2D(9, 9, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var c = new Color32[81];
            for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++) c[y * 9 + x] = px(x, y);
            t.SetPixels32(c); t.Apply();
            return t;
        }

        // ---------------- 5일 달력 ----------------

        void DrawCalendar(Rect top, GameManager gm, GameClock clock)
        {
            float ease = 1f - (1f - openK) * (1f - openK);
            const float bw = 204f, bh = 58f;   // 왼쪽 할 일 카드(오른쪽 끝 214)와 겹치지 않는 너비
            var b = new Rect(Mathf.Round(top.center.x - bw / 2f), Mathf.Round(top.yMax + 4f - (1f - ease) * 6f), bw, bh);
            var oc = GUI.color; GUI.color = new Color(oc.r, oc.g, oc.b, oc.a * ease);
            CuteUI.Box(b, 8, CuteUI.Paper, CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
            int n = Mathf.Max(1, clock.marketDayInterval);
            // 머리줄: 오늘 큰 장 (없으면 장날 주기)
            string fair = FairToday();
            if (fair != null) CuteUI.LabelCenter(b.center.x, b.y + 9, "오늘 큰 장: " + fair, CuteUI.Interact, true, true);
            else CuteUI.LabelCenter(b.center.x, b.y + 9, $"{n}일마다 장날", CuteUI.Sub, true);
            int start = clock.day - ((clock.day - 1) % n);   // 이번 장 돌이 첫날 (장날이 끝날)
            float cw = (bw - 16f - (n - 1) * 4f) / n;
            for (int i = 0; i < n; i++)
            {
                int d = start + i;
                bool today = d == clock.day, market = d % n == 0, past = d < clock.day;
                var cell = new Rect(Mathf.Round(b.x + 8 + i * (cw + 4f)), b.y + 17, Mathf.Floor(cw), 32);
                CuteUI.Box(cell, 5, today ? new Color32(0xE6, 0xF6, 0xF3, 255) : market ? CuteUI.SellSoft : CuteUI.Paper2,
                    today ? CuteUI.Interact : market ? CuteUI.Sell : CuteUI.Line, today ? 2 : 1);
                CuteUI.LabelCenter(cell.center.x, cell.y + 9, d + "일", today ? CuteUI.Interact : past ? CuteUI.Sub : CuteUI.Text, true, true);
                string sub = market ? "장날" : today ? "오늘" : "";
                if (sub.Length > 0) CuteUI.LabelCenter(cell.center.x, cell.y + 22, sub, market ? CuteUI.Sell : CuteUI.Interact, true, market || today);
                if (past) CuteUI.Fill(new Rect(cell.x + 6, cell.y + 22, cell.width - 12, 1), CuteUI.Line);   // 지난 날
            }
            GUI.color = oc;
        }

        /// <summary>오늘 큰 장 마을 (지금 지역 쪽 중앙 조각). 고려는 동쪽, 베네치아는 서쪽, 북유럽은 북쪽, 아라비아는 남쪽</summary>
        static string FairToday()
        {
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            string chunk = CentralFair.ChunkOfMap(map);
            if (chunk == null)
                switch (Reactions.RegionOfMap(map))
                {
                    case Reactions.Region.Venice: chunk = "west"; break;
                    case Reactions.Region.North: chunk = "north"; break;
                    case Reactions.Region.Arabia: chunk = "south"; break;
                    default: chunk = "east"; break;
                }
            var t = CentralFair.Today(chunk);
            return string.IsNullOrEmpty(t.name) ? null : t.name;
        }
    }
}
