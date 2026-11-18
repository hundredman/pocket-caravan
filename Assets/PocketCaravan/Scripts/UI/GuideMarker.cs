using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/13 기우(시연 피드백 "화살표가 정삼각형이라 방향을 모르겠다"): 퀘스트 길 안내 표시.
    /// 예전엔 캐릭터 둘레를 도는 작은 노란 삼각형이었다 → 다른 게임처럼
    /// - 목적지가 화면 안: 목적지 위 ▼ (진한 테두리, 통통 튐)
    /// - 화면 밖: 그쪽 화면 끝에 [핀 · N칸] + 바깥을 가리키는 작은 화살촉. 붙은 쪽이 곧 방향이라 모양을 읽지 않아도 된다
    /// - 처음 뜨거나 목적지가 바뀌면 한 번 톡 튀어나온다. 이벤트 안내 중이면 금빛 (할 일 카드와 같은 색)
    /// - 핀을 누르면 지도(M). 왼쪽 위 카드·오른쪽 위 단추·아래 알림과 겹치면 가장자리를 따라 비킨다 (ScreenZones)
    /// - 11/18 은샘(첫 플레이 피드백 "NPC·다른 마을 가는 길을 못 찾겠다"): 캐릭터 옆 화살표를 다시 넣는다.
    ///   예전 작은 정삼각형은 방향이 안 읽혀서, 이번엔 몸통 달린 화살표(➜)에 진한 테두리. 걸을 수 있는 길 앞쪽(steer)을 가리키고
    ///   다른 마을이면 다음 출구 쪽 길을 따라간다. 목적지 2칸 안이면 감춘다
    /// 공용 이름표(WorldLabels)와 고려 이름표(GoryeoPlaceLabels)가 같이 쓴다.
    /// </summary>
    public static class GuideMarker
    {
        const float Margin = 12f, Top = 34f, Dot = 16f;
        static Texture2D tri, triInk, arrow;
        static Vector3 lastPos; static bool lastOff; static int lastFrame = -10; static float popAt = -9f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { lastFrame = -10; popAt = -9f; }

        /// <summary>목적지 위 ▼ (p = 꼭짓점 바로 위 자리). 진한 테두리를 둘러 눈밭·모래에서도 보이게</summary>
        public static void Down(Vector2 p)
        {
            Init();
            float bob = Mathf.Floor(Mathf.Sin(Time.time * 5f) * 2f + 2f);
            var r = new Rect(Mathf.Round(p.x - 6f), Mathf.Round(p.y - 10f + bob), 12f, 10f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, old.a);
            GUI.DrawTexture(r, triInk);
            GUI.color = new Color(1f, 0.85f, 0.25f, old.a);   // 노랑 (예전 길 안내 색)
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 1f, 8f, 7f), tri);
            GUI.color = old;
        }

        /// <summary>
        /// 길 안내 하나 (OnGUI마다). target = 목적지 자리(GUI 좌표), player = 캐릭터 자리(GUI 좌표).
        /// 화면 안이면 출구만 ▼ (사람·장터 위 ▼는 이름표 쪽이 그린다), 화면 밖이면 화면 끝 핀
        /// </summary>
        public static void Draw(Vector2 target, Vector2 player, Navigator.Result n, int cells)
        {
            Init();
            NearArrow(player, n, cells);
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            bool off = target.x < 4f || target.x > vw - 4f || target.y < 4f || target.y > vh - 4f;
            int f = Time.frameCount;
            if (f - lastFrame > 2 || off != lastOff || (n.position - lastPos).sqrMagnitude > 1f) popAt = Time.unscaledTime;
            lastFrame = f; lastOff = off; lastPos = n.position;
            if (!off) { ScreenZones.Clear("guide"); if (n.exit) Down(target); return; }

            // ---- 화면 끝 핀 ----
            Vector2 dir = target - player;
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            var inset = Rect.MinMaxRect(Margin, Top, vw - Margin, vh - Margin);
            float tx = dir.x > 0 ? (inset.xMax - player.x) / dir.x : dir.x < 0 ? (inset.x - player.x) / dir.x : float.MaxValue;
            float ty = dir.y > 0 ? (inset.yMax - player.y) / dir.y : dir.y < 0 ? (inset.y - player.y) / dir.y : float.MaxValue;
            bool sideHit = tx < ty;
            Vector2 at = player + dir * Mathf.Max(0f, Mathf.Min(tx, ty));

            string label = cells + "칸";
            float chipW = Mathf.Round(CuteUI.Width(label, true, true) + 10f);
            float w = Dot + 3f + chipW, h = Dot;
            var r = new Rect(Mathf.Round(at.x - w / 2f), Mathf.Round(at.y - h / 2f), w, h);
            r.x = Mathf.Clamp(r.x, Margin, vw - Margin - w);
            r.y = Mathf.Clamp(r.y, Top, vh - Margin - h);
            ScreenZones.Clear("guide");   // 지난 프레임 내 자리와 부딪히지 않게
            r = sideHit ? ScreenZones.Dodge(r) : ScreenZones.DodgeSideways(r);
            ScreenZones.MarkCenter("guide", r);   // 놀거리 가장자리 화살표(ActivityHints)가 이 자리를 피한다

            bool evt = QuestFocus.Current == QuestFocus.Kind.Event;
            Color32 fill = evt ? CuteUI.Deco : CuteUI.Interact, line = evt ? CuteUI.EventLine : CuteUI.InteractLo;

            // 톡 튀어나오기 (0.7 → 1.1 → 1, 0.22초)
            var m = GUI.matrix;
            float k = Mathf.Clamp01((Time.unscaledTime - popAt) / .22f);
            if (k < 1f)
            {
                float s = k < .6f ? Mathf.Lerp(.7f, 1.1f, k / .6f) : Mathf.Lerp(1.1f, 1f, (k - .6f) / .4f);
                CuteUI.ScaleAround(s, r.center);
            }
            // 바깥을 가리키는 화살촉 (동그라미 밖, 목적지 쪽)
            var dc = new Vector2(r.x + Dot / 2f, r.y + Dot / 2f);
            var tip = dc + dir * (Dot / 2f + 4f);
            var mm = GUI.matrix;
            CuteUI.RotateAround(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f, tip);
            var old = GUI.color;
            GUI.DrawTexture(new Rect(Mathf.Round(tip.x - 5f), Mathf.Round(tip.y - 4f), 10f, 8f), triInk);
            GUI.color = new Color(fill.r / 255f, fill.g / 255f, fill.b / 255f, old.a);
            GUI.DrawTexture(new Rect(Mathf.Round(tip.x - 3f), Mathf.Round(tip.y - 3f), 6f, 4f), tri);
            GUI.color = old;
            GUI.matrix = mm;

            bool hover = !UIState.Blocking && r.Contains(Event.current.mousePosition);
            var chip = new Rect(r.x + Dot - 4f, r.y + 2f, chipW + 7f, 12f);
            CuteUI.Box(chip, 6, hover ? CuteUI.Paper2 : CuteUI.Paper, hover ? CuteUI.Interact : CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
            CuteUI.LabelCenter(chip.x + 4f + chipW / 2f, chip.center.y, label, CuteUI.Text, true, true);
            CuteUI.Circle(r.x, r.y, (int)Dot, fill, line, 1, 1);
            var pic = n.exit ? CuteUI.Pic.Arrow : CuteUI.Pic.Pin; var ps = CuteUI.PicSize(pic);
            CuteUI.Icon(pic, r.x + (Dot - ps.x) / 2f, r.y + (Dot - ps.y) / 2f, 1, CuteUI.White);
            GUI.matrix = m;

            if (!UIState.Blocking && GUI.Button(r, GUIContent.none, GUIStyle.none)) InputReader.Press('m');   // 누르면 지도
        }

        /// <summary>11/18 캐릭터 옆 화살표 (몸 가운데에서 22칸 떨어져 길 쪽을 가리키고, 그쪽으로 살짝 들락날락)</summary>
        static void NearArrow(Vector2 player, Navigator.Result n, int cells)
        {
            var cam = Camera.main;
            var pc = PlayerController.Instance;
            if (cam == null || pc == null || UIState.PhotoMode) return;
            float dist = n.walkCells >= 0 ? n.walkCells : cells;
            if (dist <= 2f) return;
            Vector2 d = (Vector2)n.steer - (Vector2)pc.transform.position;
            if (d.sqrMagnitude < 0.01f) return;
            var dir = new Vector2(d.x, -d.y).normalized;   // GUI는 y가 아래로
            var body = player + new Vector2(0f, -8f);
            float bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * 5f) * 1.5f + 1.5f);
            var c = body + dir * (22f + bob);
            var m = GUI.matrix;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, old.a * Mathf.Clamp01(dist - 2f));   // 가까워지면 옅어진다
            CuteUI.RotateAround(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, c);
            GUI.DrawTexture(new Rect(Mathf.Round(c.x - 8f), Mathf.Round(c.y - 6f), 16f, 12f), arrow);
            GUI.matrix = m;
            GUI.color = old;
        }

        static void Init()
        {
            if (tri != null) return;
            arrow = Arrow();
            tri = Tri(12, 10); triInk = Tri(12, 10);   // tri는 흰색 (GUI.color로 물들인다), triInk는 테두리용 잉크색
            var ink = (Color)CuteUI.Ink;
            for (int y = 0; y < 10; y++) for (int x = 0; x < 12; x++) if (triInk.GetPixel(x, y).a > 0) triInk.SetPixel(x, y, ink);
            triInk.Apply();
        }

        /// <summary>오른쪽을 가리키는 몸통 달린 화살표 16×12. 노란 속 + 잉크 테두리를 구워 넣는다</summary>
        static Texture2D Arrow()
        {
            const int W = 16, H = 12;
            bool In(int x, int y)
            {
                float cy = (H - 1) / 2f, dy = Mathf.Abs(y - cy);
                if (x >= 8 && x <= 14) return dy <= (14 - x) * 5f / 6f + .3f;   // 머리
                return x >= 2 && x < 8 && dy <= 1.6f;                          // 몸통
            }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color fill = new Color(1f, 0.85f, 0.25f), hi = new Color(1f, 0.95f, 0.65f), ink = CuteUI.Ink;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    bool inside = In(x, y);
                    bool edge = !inside && (In(x - 1, y) || In(x + 1, y) || In(x, y - 1) || In(x, y + 1));
                    t.SetPixel(x, y, inside ? (y >= H / 2 + 1 && x < 12 ? hi : fill) : edge ? ink : Color.clear);
                }
            t.Apply();
            return t;
        }

        /// <summary>아래를 가리키는 삼각형 (텍스처 y는 아래부터: y=0이 꼭짓점)</summary>
        static Texture2D Tri(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                t.SetPixel(x, y, Mathf.Abs(x - (w - 1) / 2f) <= y * (w / 2f) / (h - 1) + 0.5f ? Color.white : Color.clear);
            t.Apply();
            return t;
        }
    }
}
