using UnityEngine;

namespace PocketCaravan
{
    /// <summary>창이 열려 있으면 이동과 시간을 멈춘다.</summary>
    public static class UIState
    {
        public static bool Blocking;
        /// <summary>창이 닫힌 프레임. 닫는 키(E)가 같은 프레임에 '말 걸기'로 또 쓰이지 않게 한다.</summary>
        public static int ClosedFrame = -1;

        public static void Release()
        {
            Blocking = false;
            ClosedFrame = Time.frameCount;
        }

        public static bool JustClosed => Time.frameCount == ClosedFrame;
    }

    /// <summary>
    /// 임시 UI(OnGUI)용 폰트·창 모양·화면 배율. 폰트는 갈무리(Galmuri, OFL) 픽셀 폰트.
    /// 창과 버튼은 둥근 모서리 픽셀 패널로 바꿔 그린다.
    /// </summary>
    public static class UIStyle
    {
        public const float VirtualHeight = 360f;
        static Font font, small;
        static bool skinned;
        public static Font Small => small != null ? small : font;

        public static float Scale => Screen.height / VirtualHeight;
        public static float VirtualWidth => Screen.width / Scale;

        static GUIStyle center, right, rich, wrap;
        public static GUIStyle Center => center ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        public static GUIStyle Right => right ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
        public static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
        public static GUIStyle Wrap => wrap ??= new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true };

        public static void Begin()
        {
            if (font == null)
            {
                font = Resources.Load<Font>("Fonts/Galmuri11");
                small = Resources.Load<Font>("Fonts/Galmuri9");
                if (font == null)
                    font = Font.CreateDynamicFontFromOSFont(new[] { "Apple SD Gothic Neo", "Malgun Gothic", "NanumGothic", "Noto Sans CJK KR", "Arial" }, 12);
                Font.textureRebuilt += PointFilter;
                PointFilter(font); PointFilter(small);
            }
            GUI.skin.font = font;
            GUI.skin.label.fontSize = 11;
            GUI.skin.button.fontSize = 11;
            GUI.skin.box.fontSize = 11;
            if (!skinned || box.normal.background == null) Skin();
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
        }
    
        static void PointFilter(Font f)
        {
            if (f != null && f.material != null && f.material.mainTexture != null)
                f.material.mainTexture.filterMode = FilterMode.Point;
        }

        // ---------- 창 모양 ----------
        static readonly Color32 Ink = new Color32(0x1B, 0x17, 0x24, 0xFF);
        static readonly Color32 Rim = new Color32(0xE9, 0xD8, 0xB4, 0xFF);
        static readonly Color32 Fill = new Color32(0x2C, 0x26, 0x3B, 0xEE);
        static readonly Color32 FillLight = new Color32(0x3A, 0x33, 0x50, 0xEE);

        static GUIStyle box, button;
        /// <summary>창 바탕 스타일 (GUI.Box(rect, "", UIStyle.Box))</summary>
        public static GUIStyle Box => box;
        /// <summary>버튼 스타일 (GUI.Button(rect, label, UIStyle.Button))</summary>
        public static GUIStyle Button => button;

        static void Skin()
        {
            skinned = true;
            box = new GUIStyle(GUI.skin.box) { border = new RectOffset(4, 4, 4, 4) };
            Plain(box.normal, Panel(Fill, Rim), Color.white);

            button = new GUIStyle(GUI.skin.button) { border = new RectOffset(4, 4, 4, 4), alignment = TextAnchor.MiddleCenter };
            var normal = Panel(FillLight, Rim);
            Plain(button.normal, normal, Color.white);
            Plain(button.focused, normal, Color.white);
            Plain(button.hover, Panel(new Color32(0x55, 0x4A, 0x6E, 0xF5), new Color32(0xFF, 0xE9, 0xA8, 0xFF)), new Color(1f, 0.93f, 0.66f));
            Plain(button.active, Panel(Rim, Ink), new Color32(0x2C, 0x26, 0x3B, 0xFF));
            GUI.skin.label.normal.textColor = new Color(0.97f, 0.95f, 0.9f);
        }

        static void Plain(GUIStyleState st, Texture2D bg, Color text)
        {
            st.background = bg;
            st.scaledBackgrounds = new Texture2D[0];   // 레티나용 기본 배경이 우선하지 않게
            st.textColor = text;
        }

        /// <summary>
        /// 프로젝트가 Linear 색 공간이면 OnGUI가 텍스처 색을 한 번 더 밝힌다. 미리 감마를 풀어 둔다.
        /// </summary>
        static Color Lin(Color32 c)
        {
            Color col = c;
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? new Color(col.linear.r, col.linear.g, col.linear.b, col.a) : col;
        }

        /// <summary>8×8 둥근 모서리 패널: 바깥 검은 선, 안쪽 밝은 테, 속 채움 (9분할로 늘어난다)</summary>
        static Texture2D Panel(Color32 fill, Color32 rim)
        {
            const int N = 8;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int ex = Mathf.Min(x, N - 1 - x), ey = Mathf.Min(y, N - 1 - y);
                Color32 c;
                if (ex == 0 && ey == 0) c = clear;                       // 모서리 깎기
                else if (ex == 0 || ey == 0 || (ex == 1 && ey == 1)) c = Ink;
                else if (ex == 1 || ey == 1) c = rim;
                else c = fill;
                t.SetPixel(x, y, Lin(c));
            }
            t.Apply();
            return t;
        }
    }
}
