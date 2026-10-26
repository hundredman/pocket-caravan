using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아·본토 서부 맵(venice_*)을 오갈 때의 이동 효과 (2026-10-26 개편: 짧고 귀엽게).
    /// 내 고양이를 가운데 두고 '고양이 얼굴 모양' 구멍이 좁혀지며 화면이 닫힌다 → 덮인 동안 다음 마을 이름표와 발자국 →
    /// 새 맵에서 같은 모양 구멍이 고양이 자리부터 넓어지며 열린다. 석호·배 이동은 청록, 본토 이동은 풀빛.
    /// 덮인 동안에는 움직이는 연출이 없어 씬을 불러오며 화면이 잠깐 멈춰도 끊겨 보이지 않는다. 그리기 호출 몇 번뿐.
    /// 화면 표시만 하며 이동·시간·저장은 GameManager.Travel 그대로다. 다른 지역은 등록하지 않았으므로 기존 검은 페이드를 쓴다.
    /// </summary>
    public class VeniceTravelWave : TravelTransition
    {
        public override float CoverSeconds => 0.32f;
        public override float RevealSeconds => 0.38f;
        public override float MinHoldSeconds => 0.25f;

        class Theme { public Color fill, rim, paper, ink; public Texture2D iris, solid; }
        static readonly Theme Water = new Theme
        {
            fill = new Color32(46, 120, 132, 255), rim = new Color32(236, 250, 244, 255),
            paper = new Color32(244, 236, 214, 255), ink = new Color32(44, 52, 70, 255)
        };
        static readonly Theme Land = new Theme
        {
            fill = new Color32(104, 140, 80, 255), rim = new Color32(240, 228, 170, 255),
            paper = new Color32(244, 236, 214, 255), ink = new Color32(52, 60, 40, 255)
        };
        static readonly Color Ribbon = new Color32(196, 70, 78, 255);

        const int IrisSize = 128;
        GUIStyle title;
        bool land;
        Vector2 focus;        // 구멍 가운데 (화면 픽셀, 위가 0)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSelf() => Register(id => id.StartsWith("venice_"), new VeniceTravelWave());

        static bool Mainland(string id) => id != null && id.StartsWith("venice_c_");

        public override void Begin(string fromMap, string toMap)
        {
            bool ferry = (fromMap == "venice_chioggia" && toMap == "venice_c_landing") || (fromMap == "venice_c_landing" && toMap == "venice_chioggia");
            land = !ferry && (Mainland(fromMap) || Mainland(toMap));
        }

        public override void Draw(float cover, bool covering, string nextName, float heldFor)
        {
            if (Event.current.type != EventType.Repaint) return;
            var t = land ? Land : Water;
            if (t.iris == null) Build(t);
            GUI.matrix = Matrix4x4.identity;
            float sw = Screen.width, sh = Screen.height;
            // 구멍은 내 고양이 머리 쪽을 따라간다 (덮는 동안·여는 동안 모두)
            var player = PlayerController.Instance;
            if (player != null && Camera.main != null && cover < .98f)
            {
                var p = Camera.main.WorldToScreenPoint(player.transform.position + new Vector3(0f, .9f, 0f));
                focus = new Vector2(p.x, sh - p.y);
            }
            else if (focus == Vector2.zero) focus = new Vector2(sw / 2f, sh / 2f);

            // 덮인 정도 → 구멍 크기 (부드럽게 들어갔다 나온다)
            float k = 1f - EaseInOut(Mathf.Clamp01(cover));
            float size = Mathf.Lerp(0f, Mathf.Max(sw, sh) * 2.8f, k * k);
            GUI.color = Color.white;
            if (size < 2f) GUI.DrawTexture(new Rect(0, 0, sw, sh), t.solid);
            else
            {
                var r = new Rect(Mathf.Round(focus.x - size / 2f), Mathf.Round(focus.y - size / 2f), Mathf.Round(size), Mathf.Round(size));
                GUI.DrawTexture(r, t.iris);
                Fill(t, new Rect(0, 0, sw, r.y));                       // 위
                Fill(t, new Rect(0, r.yMax, sw, sh - r.yMax));          // 아래
                Fill(t, new Rect(0, r.y, r.x, r.height));               // 왼쪽
                Fill(t, new Rect(r.xMax, r.y, sw - r.xMax, r.height));  // 오른쪽
            }
            float show = covering ? Mathf.Clamp01((cover - .85f) / .15f) : Mathf.Clamp01((cover - .7f) / .3f);
            if (show > 0f) Card(t, nextName, show, covering ? heldFor : 1f);
            GUI.color = Color.white;
        }

        /// <summary>가운데 이름표 + 발자국 (덮인 동안 고정 그림, 발자국만 하나씩 찍힌다)</summary>
        void Card(Theme t, string name, float alpha, float held)
        {
            if (string.IsNullOrEmpty(name)) return;
            UIStyle.Begin();
            float w = UIStyle.VirtualWidth, h = UIStyle.VirtualHeight;
            title ??= new GUIStyle(UIStyle.CenterRich) { fontSize = 16 };
            float bw = Mathf.Max(120f, title.CalcSize(new GUIContent(name)).x + 40f), bh = 28f;
            var r = new Rect(Mathf.Round(w / 2f - bw / 2f), Mathf.Round(h / 2f - bh / 2f), Mathf.Round(bw), bh);
            Box(new Rect(r.x - 10f, r.y + 7f, 14f, 14f), Ribbon, alpha);
            Box(new Rect(r.xMax - 4f, r.y + 7f, 14f, 14f), Ribbon, alpha);
            Box(new Rect(r.x + 2f, r.y, r.width - 4f, r.height), t.paper, alpha);
            Box(new Rect(r.x, r.y + 2f, r.width, r.height - 4f), t.paper, alpha);
            GUI.color = new Color(t.ink.r, t.ink.g, t.ink.b, alpha);
            GUI.Label(r, name, title);
            // 고양이 발자국 네 개가 이름표 아래로 차례로 찍힌다
            int steps = Mathf.Clamp(Mathf.FloorToInt(held / .08f) + 1, 1, 4);
            for (int i = 0; i < steps; i++)
                Paw(w / 2f - 30f + i * 20f, r.yMax + 10f + (i % 2) * 6f, t.rim, alpha);
            GUI.matrix = Matrix4x4.identity;
        }

        static void Paw(float x, float y, Color c, float alpha)
        {
            Box(new Rect(x, y + 3f, 6f, 5f), c, alpha);
            Box(new Rect(x - 2f, y, 2f, 2f), c, alpha);
            Box(new Rect(x + 1f, y - 1f, 2f, 2f), c, alpha);
            Box(new Rect(x + 4f, y - 1f, 2f, 2f), c, alpha);
            Box(new Rect(x + 7f, y, 2f, 2f), c, alpha);
        }

        static void Box(Rect r, Color c, float alpha)
        {
            c.a *= alpha;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }

        static void Fill(Theme t, Rect r)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            GUI.DrawTexture(r, t.solid);
        }

        /// <summary>고양이 얼굴 모양 구멍 그림: 바깥은 덮개 색, 얼굴(둥근 머리 + 두 귀)은 투명, 테두리는 밝은 선.</summary>
        static void Build(Theme t)
        {
            int n = IrisSize;
            t.iris = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Inside(x + .5f, y + .5f, n);
                    t.iris.SetPixel(x, n - 1 - y, d > 1.5f ? clear : d > -1.5f ? t.rim : t.fill);
                }
            t.iris.Apply();
            t.solid = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            t.solid.SetPixel(0, 0, t.fill);
            t.solid.Apply();
        }

        /// <summary>얼굴 안쪽이면 양수(경계에서 몇 픽셀 안쪽인지), 바깥이면 음수. 좌표는 위가 0.</summary>
        static float Inside(float x, float y, int n)
        {
            float cx = n / 2f, cy = n * .56f, r = n * .30f;
            float head = r - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            float ear = Mathf.Max(Ear(x, y, cx - r * .58f, cy - r * .45f, n), Ear(x, y, cx + r * .58f, cy - r * .45f, n));
            return Mathf.Max(head, ear);
        }

        static float Ear(float x, float y, float bx, float by, int n)
        {
            // 귀: 아래가 넓고 위가 뾰족한 삼각형 (밑변 가운데 bx,by, 높이 n*.2)
            float hgt = n * .27f, half = n * .14f;
            float ty = (by - y) / hgt;                    // 0 = 밑변, 1 = 꼭대기
            if (ty < -.3f || ty > 1f) return -99f;
            float width = half * (1f - Mathf.Clamp01(ty));
            return Mathf.Min(width - Mathf.Abs(x - bx), Mathf.Min((1f - ty) * hgt, (ty + .3f) * hgt));
        }

        static float EaseInOut(float v) => v * v * (3f - 2f * v);
    }
}
