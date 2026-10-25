using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 맵(venice_*)을 떠나거나 들어올 때의 이동 효과: 둥근 물결이 위에서 화면을 덮고, 덮인 동안 플레이어 고양이가
    /// 곤돌라를 타고 지나가며 다음 마을 이름을 띄운 뒤, 물이 아래로 빠지며 새 맵이 드러난다.
    /// 화면 표시만 하며 이동·시간·저장은 GameManager.Travel 그대로다. 다른 지역은 등록하지 않았으므로 기존 검은 페이드를 쓴다.
    /// </summary>
    public class VeniceTravelWave : TravelTransition
    {
        public override float CoverSeconds => 0.5f;
        public override float RevealSeconds => 0.55f;
        public override float MinHoldSeconds => 0.9f;

        static readonly Color Water = new Color32(58, 150, 160, 255);
        static readonly Color WaterLight = new Color32(104, 196, 196, 255);
        static readonly Color Foam = new Color32(236, 250, 244, 255);
        static readonly Color Boat = new Color32(70, 40, 44, 255);
        static readonly Color Trim = new Color32(238, 196, 96, 255);
        static readonly Color Ink = new Color32(44, 52, 70, 255);
        static readonly Color Ribbon = new Color32(196, 70, 78, 255);

        const float Scallop = 18f;    // 둥근 물결 하나의 폭
        static float scale = 1f;
        static Sprite[] catFrames;    // 떠날 때 플레이어 그림을 잡아 둔다 (씬이 바뀌는 동안에도 같은 그림)
        GUIStyle title;
        float shownAt = -1f;
        float clock;
        int lastFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSelf() => Register(id => id.StartsWith("venice_") && !id.StartsWith("venice_c_"), new VeniceTravelWave());   // 석호 안과 키오자 배(본토 나루터) 이동만. 본토 안은 기본 페이드

        public override void Draw(float cover, bool covering, string nextName, float heldFor)
        {
            if (Event.current.type != EventType.Repaint) return;     // 그리기 이벤트에서만 (배치·입력 이벤트에서 또 그리지 않는다)
            var anim = PlayerController.Instance != null ? PlayerController.Instance.anim : null;
            if (anim != null && anim.frames != null && anim.frames.Length >= 16 && anim.frames[12] != null) catFrames = anim.frames;

            UIStyle.Begin();
            // 효과 시계: 프레임마다 한 번, 최대 1/30초씩만 간다. 씬을 불러오며 멈췄던 시간만큼 배가 순간이동하지 않게.
            if (Time.frameCount != lastFrame)
            {
                lastFrame = Time.frameCount;
                clock += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            }
            float w = UIStyle.VirtualWidth, h = UIStyle.VirtualHeight, time = clock;
            float p = Ease(cover);
            // 물은 큰 사각형 하나 + 둥근 물결 테두리 그림 한 줄로 그린다(그리기 호출 몇 번).
            // 가는 기둥 수백 개로 그리면 IMGUI가 느려져 프레임이 떨어진다.
            scale = UIStyle.Scale;
            GUI.matrix = Matrix4x4.identity;
            if (edgeDown == null) BuildEdges();
            float reach = h + 40f;
            float edgeY = covering ? p * reach - 20f : (1f - p) * reach - 20f;
            float sw = Screen.width, eh = Mathf.Round(EdgeH * scale);
            var flow = new Rect(time * 14f / Scallop, 0f, w / Scallop, 1f);     // 테두리가 옆으로 흐른다
            GUI.color = Color.white;
            if (covering)
            {
                float y = Mathf.Round((edgeY - EdgeTop) * scale);
                Fill(new Rect(0, 0, sw, Mathf.Max(0f, y)));
                GUI.DrawTextureWithTexCoords(new Rect(0, y, sw, eh), edgeDown, flow);
            }
            else
            {
                float y = Mathf.Round((edgeY - (EdgeH - EdgeTop)) * scale);
                GUI.DrawTextureWithTexCoords(new Rect(0, y, sw, eh), edgeUp, flow);
                Fill(new Rect(0, y + eh, sw, Mathf.Max(0f, Screen.height - y - eh)));
            }
            Bubbles(w, h, time, covering, edgeY);

            // 곤돌라·이름표는 물이 화면 가운데를 넘는 순간부터 함께 보인다 (다 덮일 때까지 기다리지 않는다)
            float show = covering ? Mathf.Clamp01((edgeY - (h * 0.5f + 40f)) / 30f) : Mathf.Clamp01((cover - 0.5f) / 0.4f);
            if (covering && show <= 0f) shownAt = -1f;          // 새 이동: 곤돌라를 처음 자리에서
            if (show > 0f)
            {
                if (shownAt < 0f) shownAt = time;
                Gondola(w, h, time, time - shownAt, show);
                Card(w, h, nextName, show);
            }
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
        }

        /// <summary>물속에서 떠오르는 작은 방울 (물에 덮인 곳에만)</summary>
        static void Bubbles(float w, float h, float time, bool covering, float edgeY)
        {
            for (int i = 0; i < 22; i++)
            {
                float speed = 12f + 16f * Hash(i * 7 + 2);
                float y = h - Mathf.Repeat(time * speed + h * Hash(i * 3 + 1), h + 10f);
                float x = w * Hash(i * 11 + 4) + Mathf.Sin(time * 2f + i) * 3f;
                if (covering ? y > edgeY - 8f : y < edgeY + 8f) continue;
                float size = Hash(i * 5 + 3) > 0.6f ? 3f : 2f;
                var c = Foam; c.a = 0.5f;
                Box(x, y, size, size, c);
            }
        }

        /// <summary>곤돌라를 탄 고양이가 화면을 천천히 가로지른다 (위아래로 살짝 흔들림).
        /// 배 전체를 화면 픽셀 한 점(기준점)에 맞춰 그 위에 조각을 붙인다. 조각마다 따로 반올림하면 조각끼리 1픽셀씩 어긋나 떨려 보인다.</summary>
        void Gondola(float w, float h, float time, float t, float alpha)
        {
            float total = MinHoldSeconds + RevealSeconds + CoverSeconds;
            float cx = Mathf.Lerp(w * 0.5f - 45f, w * 0.5f + 45f, Mathf.Clamp01(t / total));
            float bob = Mathf.Sin(time * 3f) * 1.5f;
            float ox = Mathf.Round(cx * scale), oy = Mathf.Round((h * 0.5f - 6f + bob) * scale);
            void Part(float dx, float dy, float pw, float ph, Color c)
            {
                c.a *= alpha;
                GUI.color = c;
                GUI.DrawTexture(new Rect(ox + Mathf.Round(dx * scale), oy + Mathf.Round(dy * scale),
                    Mathf.Max(1f, Mathf.Round(pw * scale)), Mathf.Max(1f, Mathf.Round(ph * scale))), Texture2D.whiteTexture);
            }
            // 잔물결 자국 (배 뒤에서 깜박이며 흐른다)
            var wake = Foam; wake.a = 0.6f;
            for (int i = 1; i <= 3; i++) Part(-34f - i * 9f - Mathf.Repeat(time * 6f, 9f), 9f + (i % 2), 6f, 1.5f, wake);
            // 배: 가운데 몸통 + 양 끝이 위로 휜 뱃머리
            Part(-30f, 0f, 60f, 6f, Boat);
            Part(-34f, -2f, 5f, 5f, Boat);
            Part(-37f, -6f, 4f, 5f, Boat);
            Part(29f, -2f, 5f, 5f, Boat);
            Part(33f, -7f, 4f, 6f, Boat);
            Part(35f, -9f, 3f, 3f, Trim);
            Part(-28f, 1f, 56f, 1.5f, Trim);
            // 노 (천천히 젓는다)
            float row = Mathf.Sin(time * 3f) * 2f;
            for (int i = 0; i < 9; i++) Part(6f + i * 1.5f + row * (i / 8f), -16f + i * 2.5f, 1.5f, 2.5f, Ink);
            // 고양이: 오른쪽을 보고 선 그림(2배). 배 위에서는 걷지 않는다
            if (catFrames != null && catFrames[12] != null)
            {
                var sprite = catFrames[12];
                var tex = sprite.texture;
                var tr = sprite.textureRect;
                var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                float sw = Mathf.Round(tr.width * 2f * scale), sh = Mathf.Round(tr.height * 2f * scale);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTextureWithTexCoords(new Rect(ox - Mathf.Round(sw * 0.5f), oy - sh + Mathf.Round(4f * scale), sw, sh), tex, uv);
            }
        }

        void Card(float w, float h, string name, float alpha)
        {
            if (string.IsNullOrEmpty(name)) return;
            UIStyle.Begin();
            title ??= new GUIStyle(UIStyle.CenterRich) { fontSize = 16 };
            float bw = Mathf.Max(110f, title.CalcSize(new GUIContent(name)).x + 36f), bh = 26f;
            var r = new Rect(w * 0.5f - bw * 0.5f, h * 0.5f + 22f, bw, bh);
            // 리본 꼬리 + 둥근 이름표
            GUI.matrix = Matrix4x4.identity;
            var ribbon = Ribbon; ribbon.a = alpha;
            Box(r.x - 10f, r.y + 6f, 14f, 14f, ribbon);
            Box(r.xMax - 4f, r.y + 6f, 14f, 14f, ribbon);
            var foam = Foam; foam.a = alpha;
            Box(r.x + 2f, r.y, r.width - 4f, r.height, foam);
            Box(r.x, r.y + 2f, r.width, r.height - 4f, foam);
            var trim = Trim; trim.a = alpha;
            Box(r.x + 6f, r.yMax - 4f, r.width - 12f, 1.5f, trim);
            UIStyle.Begin();
            GUI.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            GUI.Label(new Rect(r.x, r.y - 1f, r.width, r.height), name, title);
        }

        const int EdgeH = 12, EdgeTop = 2;     // 물결 테두리 그림 높이와, 그림 안에서 물 경계가 시작하는 줄
        static Texture2D edgeDown, edgeUp, waterTex;   // 물 바닥도 같은 방식의 텍스처로 칠해 테두리와 색이 꼭 같게

        /// <summary>물결 테두리 한 칸(Scallop×EdgeH, 가상 픽셀 1:1)을 만든다. 옆으로 이어 붙여 그린다.</summary>
        static void BuildEdges()
        {
            int sw = (int)Scallop;
            edgeDown = NewEdge(sw); edgeUp = NewEdge(sw);
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < sw; x++)
            {
                float u = (x + 0.5f) / sw * 2f - 1f;
                int front = EdgeTop + Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) * 7f) + 2;   // 위에서부터 물이 차는 깊이
                for (int r = 0; r < EdgeH; r++)
                {
                    Color c = r < front - 4 ? Water : r < front - 2 ? WaterLight : r < front + 1 ? Foam : clear;
                    edgeDown.SetPixel(x, EdgeH - 1 - r, c);   // 텍스처는 아래가 0
                    edgeUp.SetPixel(x, r, c);                 // 위아래를 뒤집은 것: 물이 아래에 있다
                }
            }
            edgeDown.Apply(); edgeUp.Apply();
            waterTex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
            waterTex.SetPixel(0, 0, Water);
            waterTex.Apply();
        }

        static Texture2D NewEdge(int width) => new Texture2D(width, EdgeH, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };

        static void Fill(Rect r)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            GUI.color = Color.white;
            GUI.DrawTexture(r, waterTex);
        }

        static void Box(float x, float y, float bw, float bh, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(Px(x, y, bw, bh), Texture2D.whiteTexture);
        }

        /// <summary>가상 좌표 사각형 → 화면 픽셀 사각형 (정수로 맞춤)</summary>
        static Rect Px(float x, float y, float bw, float bh)
        {
            float x0 = Mathf.Round(x * scale), y0 = Mathf.Round(y * scale);
            return new Rect(x0, y0, Mathf.Max(1f, Mathf.Round((x + bw) * scale) - x0), Mathf.Max(1f, Mathf.Round((y + bh) * scale) - y0));
        }

        static float Ease(float t) => t * t * (3f - 2f * t);
        static float Hash(int n) => Mathf.Repeat(Mathf.Sin(n * 12.9898f) * 43758.5453f, 1f);
    }
}
