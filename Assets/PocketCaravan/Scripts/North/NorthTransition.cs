using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 맵(north_)에서 다른 맵으로 넘어갈 때의 전환 연출: 검게 어두워지는 대신
    /// 가는 방향에서 눈보라가 휘몰아쳐 화면을 덮고, 새 맵에 도착하면 같은 방향으로 계속 쓸려 나가며 새 맵이 드러난다.
    /// (오른쪽으로 가면 오른쪽에서 들어와 왼쪽으로, 위로 가면 위에서 들어와 아래로 빠짐 — 카메라가 그쪽으로 넘어가는 느낌)
    /// 10/27: 마을에 도착할 때는 눈보라가 덮인 채 순록 얼굴 이름표("○○ 마을")를 잠깐 보여 주고,
    /// 내 캐릭터 자리에서 순록 얼굴 모양 구멍이 넓어지며 마을이 드러난다 (베네치아 고양이 얼굴 전환을 북유럽판으로).
    /// 길 맵끼리는 지금처럼 방향 눈보라만 (자주 지나가니 짧게).
    /// 공용 GameManager의 검은 페이드는 그대로 두고, 그 위에(GUI.depth 더 앞) 그린다. 이동·저장과 무관한 연출만.
    /// </summary>
    public class NorthTransition : MonoBehaviour
    {
        const float CoverTime = 0.28f, RevealTime = 0.7f, IrisTime = 0.8f, CardHold = 0.55f;
        float progress = -1f;           // -1 꺼짐, 0→1 덮기, 1 유지, 1→2 걷히기
        bool wasTravelling, arrived, iris;
        float hold;                     // 마을 도착 이름표를 보여 주는 남은 시간
        string arriveName = "";
        Vector2 irisCenter;             // 화면 좌표 (GUI 기준, y 아래로)
        Vector3[] flakes;               // 들어오는 쪽에서부터 거리(0~1), 옆 위치(0~1), 속도
        Vector2Int dir = Vector2Int.right;   // 가는 방향 (화면 기준: y+ = 위)
        AudioSource whoosh;
        Texture2D mask, icon;
        GUIStyle cardStyle;

        static readonly Color Snow = new Color(0.93f, 0.96f, 1f, 1f);

        // 장터가 있는 마을 맵: 여기 도착할 때 순록 얼굴 전환
        static readonly string[] TownMaps =
        {
            "north_birka", "north_hunter", "north_naru", "north_laugar", "north_herd",
            "north_c_gatetown", "north_c_snowwood", "north_c_creek", "north_c_crossing",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthTransition>() != null) return;
            var go = new GameObject("NorthTransition");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthTransition>();
        }

        void Awake()
        {
            var rng = new System.Random(5);
            flakes = new Vector3[140];
            for (int i = 0; i < flakes.Length; i++)
                flakes[i] = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), 1.2f + (float)rng.NextDouble() * 1.8f);
            whoosh = gameObject.AddComponent<AudioSource>();
            whoosh.clip = MakeWhoosh(); whoosh.playOnAwake = false; whoosh.loop = false;
            mask = MakeReindeer(true);
            icon = MakeReindeer(false);
        }

        static bool InNorth => MapRoot.Current != null && MapRoot.Current.mapId != null && MapRoot.Current.mapId.StartsWith("north_");

        /// <summary>플레이어가 맵 가장자리(2칸 안)에 있으면 그쪽 방향. 아니면 0</summary>
        static Vector2Int EdgeDir(PlayerController pc)
        {
            if (pc == null || pc.Motor == null || MapRoot.Current == null) return Vector2Int.zero;
            int h = NorthData.Height(MapRoot.Current.mapId);
            if (h <= 0) return Vector2Int.zero;
            var c = pc.Motor.Cell;                 // 월드 칸 (y 위로 증가, 0이 맨 아래)
            if (c.y <= 1) return Vector2Int.down;
            if (c.y >= h - 2) return Vector2Int.up;
            if (c.x <= 1) return Vector2Int.left;
            var f = pc.Motor.Facing;
            if (f.x > 0) return Vector2Int.right;   // 오른쪽 끝은 맵 너비를 몰라서 바라보는 방향으로
            return Vector2Int.zero;
        }

        static bool IsTown(string id)
        {
            if (id == null) return false;
            foreach (var t in TownMaps) if (t == id) return true;
            return false;
        }

        void Update()
        {
            var gm = GameManager.I;
            bool travelling = gm != null && gm.IsTravelling;
            if (travelling && !wasTravelling && InNorth)
            {
                progress = 0f;                                   // 북유럽에서 출발할 때만
                arrived = false; iris = false; hold = 0f;
                var pc = PlayerController.Instance;
                var f = pc != null && pc.Motor != null ? pc.Motor.Facing : Vector2Int.zero;
                if (f == Vector2Int.zero) f = Vector2Int.right;
                dir = Mathf.Abs(f.x) >= Mathf.Abs(f.y) ? new Vector2Int(f.x > 0 ? 1 : -1, 0) : new Vector2Int(0, f.y > 0 ? 1 : -1);
                // 10/27 맵 가장자리 출구를 밟았으면 그 가장자리 쪽이 가는 방향 (처음 이동 때 바라보는 방향이 늦게 바뀌어 엉뚱한 쪽으로 덮이던 문제)
                var edge = EdgeDir(pc);
                if (edge != Vector2Int.zero) dir = edge;
                if (!AudioManager.Muted) whoosh.Play();
            }
            wasTravelling = travelling;
            if (progress < 0f) return;

            float dt = Time.unscaledDeltaTime;
            if (progress < 1f) progress = Mathf.Min(1f, progress + dt / CoverTime);
            else if (!travelling)
            {
                if (!arrived) Arrive();
                if (hold > 0f) hold -= dt;                       // 이름표 보여 주는 동안 덮인 채로
                else progress += dt / (iris ? IrisTime : RevealTime);   // 도착하면 걷히기 시작
            }
            if (progress >= 2f) progress = -1f;

            for (int i = 0; i < flakes.Length; i++)
            {
                var f = flakes[i];
                f.x += f.z * dt;
                f.y += Mathf.Sin(Time.unscaledTime * 3f + i) * 0.02f * dt;
                if (f.x > 1.05f) f.x -= 1.1f;
                flakes[i] = f;
            }
        }

        void TrackPlayer()
        {
            var pc = PlayerController.Instance;
            var cam = Camera.main;
            if (pc == null || cam == null) return;
            var p = cam.WorldToScreenPoint(pc.transform.position + new Vector3(0f, 0.5f, 0f));
            if (p.z < 0f) return;
            irisCenter = new Vector2(Mathf.Clamp(p.x, 0f, Screen.width), Mathf.Clamp(Screen.height - p.y, 0f, Screen.height));
        }

        /// <summary>새 맵에 막 도착: 마을이면 이름표 + 순록 구멍, 아니면 방향 눈보라 그대로</summary>
        void Arrive()
        {
            arrived = true;
            var root = MapRoot.Current;
            iris = root != null && IsTown(root.mapId);
            if (!iris) return;
            arriveName = !string.IsNullOrEmpty(root.displayName) ? root.displayName : root.mapId;
            hold = CardHold;
            irisCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var pc = PlayerController.Instance;
            var cam = Camera.main;
            if (pc != null && cam != null)
            {
                var p = cam.WorldToScreenPoint(pc.transform.position + new Vector3(0f, 0.5f, 0f));
                irisCenter = new Vector2(p.x, Screen.height - p.y);
            }
        }

        void OnGUI()
        {
            if (progress < 0f) return;
            GUI.depth = -1001;   // 공용 검은 페이드(-1000)보다 앞
            if (iris && progress > 1f) DrawIris(progress - 1f);
            else DrawBlizzard();
            if (iris && arrived && progress < 1.3f) DrawCard(progress <= 1f ? 1f : 1f - (progress - 1f) / 0.3f);
            GUI.color = Color.white;
        }

        void DrawBlizzard()
        {
            float w = Screen.width, h = Screen.height;
            bool horiz = dir.y == 0;
            float len = horiz ? w : h, cross = horiz ? h : w;
            // 띠 위치는 "들어오는 쪽 가장자리에서부터의 거리"(a)로 계산: 덮을 때는 0에서 끝까지 자라고, 걷힐 때는 같은 방향으로 빠져나감
            float a0, a1;
            if (progress <= 1f) { a0 = -40f; a1 = len * Ease(progress) + 40f; }
            else { a0 = len * Ease(progress - 1f) - 40f; a1 = len + 40f; }
            float t = Time.unscaledTime;
            const int Row = 6;
            var soft = new Color(Snow.r, Snow.g, Snow.b, 0.45f);
            for (int c = 0; c < cross; c += Row)
            {
                float jag = Mathf.Sin(c * 0.11f + t * 9f) * 22f + Mathf.Sin(c * 0.031f + t * 4f) * 30f;
                float l = a0 + jag, r = a1 + jag;
                if (r <= l) continue;
                GUI.color = Snow; Strip(l, r, c, Row, w, h);
                GUI.color = soft; Strip(l - 26f, l, c, Row, w, h); Strip(r, r + 26f, c, Row, w, h);   // 가장자리는 옅게 흩날림
            }
            // 바람에 날리는 눈송이 (띠 앞뒤로)
            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            float size = Mathf.Max(2f, h / 180f);
            foreach (var f in flakes)
            {
                float along = f.x * len;
                if (along < a0 - 140f || along > a1 + 140f) continue;
                var p = ToScreen(along, f.y * cross, w, h);
                float lng = size * (1f + f.z);   // 바람 방향으로 길쭉하게
                GUI.DrawTexture(horiz ? new Rect(p.x, p.y, lng, size) : new Rect(p.x, p.y, size, lng), Texture2D.whiteTexture);
            }
            // 띠 안쪽은 살짝 푸른 기운
            GUI.color = new Color(0.75f, 0.85f, 1f, 0.18f);
            Strip(Mathf.Max(0f, a0), Mathf.Min(len, a1), 0f, cross, w, h);
        }

        /// <summary>순록 얼굴 모양 구멍이 내 캐릭터 자리에서 넓어지며 새 마을이 드러난다. t: 0 → 1</summary>
        void DrawIris(float t)
        {
            TrackPlayer();   // 10/27 처음 들어온 맵은 카메라가 늦게 따라와서, 구멍 자리를 매번 다시 잼
            float w = Screen.width, h = Screen.height;
            float diag = Mathf.Sqrt(w * w + h * h);
            // 처음엔 작은 순록 얼굴이 톡 열려 잠깐 보이고, 그다음 화면 밖까지 쑥 넓어짐
            float s = t < 0.4f
                ? diag * Mathf.SmoothStep(0f, 0.32f, t / 0.4f)
                : diag * (0.32f + 5.7f * Mathf.Pow((t - 0.4f) / 0.6f, 2f));
            var r = new Rect(irisCenter.x - s * 0.5f, irisCenter.y - s * 0.5f, s, s);
            GUI.color = Snow;
            var white = Texture2D.whiteTexture;
            if (r.yMin > 0f) GUI.DrawTexture(new Rect(0f, 0f, w, r.yMin), white);
            if (r.yMax < h) GUI.DrawTexture(new Rect(0f, r.yMax, w, h - r.yMax), white);
            float y0 = Mathf.Max(0f, r.yMin), y1 = Mathf.Min(h, r.yMax);
            if (r.xMin > 0f) GUI.DrawTexture(new Rect(0f, y0, r.xMin, y1 - y0), white);
            if (r.xMax < w) GUI.DrawTexture(new Rect(r.xMax, y0, w - r.xMax, y1 - y0), white);
            GUI.color = Color.white;
            GUI.DrawTexture(r, mask);
        }

        /// <summary>덮인 화면 가운데 나무 이름표: 순록 얼굴 + 마을 이름. 10/27: 이름 길이에 맞춰 이름표를 늘림 (줄바꿈 없이 한 줄)</summary>
        void DrawCard(float alpha)
        {
            if (alpha <= 0f || string.IsNullOrEmpty(arriveName)) return;
            UIStyle.Begin();
            if (cardStyle == null) cardStyle = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            string text = $"<size=17><b><color=#3a2a1e>{arriveName}</color></b></size>";
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            float tw = cardStyle.CalcSize(new GUIContent(text)).x;
            float cw = Mathf.Clamp(tw + 66f, 150f, vw - 20f), ch = 46f;
            var card = new Rect((vw - cw) * 0.5f, (vh - ch) * 0.5f, cw, ch);
            GUI.color = new Color(0.36f, 0.25f, 0.18f, alpha);
            GUI.DrawTexture(new Rect(card.x - 2f, card.y - 2f, card.width + 4f, card.height + 4f), Texture2D.whiteTexture);
            GUI.color = new Color(0.97f, 0.94f, 0.86f, alpha);
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(card.x + 6f, card.y + 5f, 36f, 36f), icon);
            GUI.Label(new Rect(card.x + 46f, card.y, cw - 52f, ch), text, cardStyle);
        }

        /// <summary>들어오는 쪽에서부터 거리(along)·옆 위치(c) → 화면 좌표</summary>
        Vector2 ToScreen(float along, float c, float w, float h)
        {
            if (dir.x > 0) return new Vector2(w - along, c);   // 오른쪽으로 감: 오른쪽에서 들어옴
            if (dir.x < 0) return new Vector2(along, c);
            if (dir.y > 0) return new Vector2(c, along);       // 위로 감: 위에서 들어옴 (GUI y는 아래로 증가)
            return new Vector2(c, h - along);
        }

        /// <summary>along 구간 [a, b], 옆 구간 [c, c+thick] 사각형</summary>
        void Strip(float a, float b, float c, float thick, float w, float h)
        {
            if (b <= a) return;
            var p = ToScreen(a, c, w, h); var q = ToScreen(b, c, w, h);
            float x0 = Mathf.Min(p.x, q.x), y0 = Mathf.Min(p.y, q.y);
            var rect = dir.y == 0 ? new Rect(x0, c, Mathf.Abs(q.x - p.x), thick) : new Rect(c, y0, thick, Mathf.Abs(q.y - p.y));
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        static float Ease(float x) => 1f - (1f - x) * (1f - x);

        // ---------- 순록 얼굴 (코드로 그린 64×64 도트) ----------
        static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float k = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float ex = px - (ax + dx * k), ey = py - (ay + dy * k);
            return Mathf.Sqrt(ex * ex + ey * ey);
        }

        static bool Ell(float u, float v, float cx, float cy, float rx, float ry)
        {
            float a = (u - cx) / rx, b = (v - cy) / ry;
            return a * a + b * b <= 1f;
        }

        /// <summary>u, v: -1~1 (v 위쪽 +). 얼굴·주둥이·귀·뿔</summary>
        static bool Inside(float u, float v)
        {
            float x = Mathf.Abs(u);   // 좌우 대칭
            if (Ell(x, v, 0f, -0.08f, 0.40f, 0.42f)) return true;            // 얼굴
            if (Ell(x, v, 0f, -0.48f, 0.27f, 0.22f)) return true;            // 주둥이
            if (Ell(x, v, 0.47f, 0.14f, 0.19f, 0.09f)) return true;          // 귀
            const float T = 0.06f;                                           // 뿔 굵기
            if (Seg(x, v, 0.18f, 0.26f, 0.36f, 0.60f) < T) return true;      // 뿔 줄기
            if (Seg(x, v, 0.36f, 0.60f, 0.30f, 0.92f) < T) return true;      // 위 가지
            if (Seg(x, v, 0.36f, 0.60f, 0.68f, 0.76f) < T) return true;      // 바깥 가지
            if (Seg(x, v, 0.68f, 0.76f, 0.76f, 0.96f) < T) return true;
            if (Seg(x, v, 0.52f, 0.68f, 0.54f, 0.90f) < T) return true;      // 가운데 가지
            return false;
        }

        /// <summary>hole=true: 전환용 가림막 (바깥 눈빛, 순록 모양은 투명, 테두리 남색).
        /// hole=false: 이름표 아이콘 (갈색 순록, 까만 눈, 빨간 코)</summary>
        static Texture2D MakeReindeer(bool hole)
        {
            const int N = 64;
            var inside = new bool[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    inside[y * N + x] = Inside((x + 0.5f) / N * 2f - 1f, (y + 0.5f) / N * 2f - 1f);
            var px = new Color32[N * N];
            Color32 snow = Snow;
            var rim = new Color32(78, 98, 140, 255);
            var fur = new Color32(150, 98, 60, 255);
            var furDark = new Color32(70, 44, 28, 255);
            var antler = new Color32(196, 160, 112, 255);
            var clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int i = y * N + x;
                    bool edge = false;
                    for (int d = 0; d < 4 && !edge; d++)
                    {
                        int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        bool o = nx >= 0 && ny >= 0 && nx < N && ny < N && inside[ny * N + nx];
                        if (o != inside[i]) edge = true;
                    }
                    if (hole) { px[i] = inside[i] ? clear : edge ? rim : snow; continue; }
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f, au = Mathf.Abs(u);
                    bool head = Ell(au, v, 0f, -0.08f, 0.40f, 0.42f) || Ell(au, v, 0f, -0.48f, 0.27f, 0.22f) || Ell(au, v, 0.47f, 0.14f, 0.19f, 0.09f);
                    if (!inside[i]) px[i] = clear;
                    else if (edge) px[i] = furDark;
                    else if (Ell(u, v, 0f, -0.62f, 0.11f, 0.08f)) px[i] = new Color32(214, 64, 58, 255);   // 빨간 코
                    else if (Ell(au, v, 0.17f, -0.04f, 0.05f, 0.06f)) px[i] = furDark;                  // 눈
                    else if (Ell(u, v, 0f, -0.46f, 0.22f, 0.17f)) px[i] = new Color32(214, 176, 130, 255); // 밝은 주둥이
                    else px[i] = head ? fur : antler;                                                    // 뿔은 밝게
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>휘이잉 바람 소리 (0.9초, 코드로 만든 소리)</summary>
        static AudioClip MakeWhoosh()
        {
            const int rate = 22050; int n = (int)(rate * 0.9f);
            var data = new float[n];
            var rng = new System.Random(9);
            float b = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                b = Mathf.Clamp(b + ((float)rng.NextDouble() * 2f - 1f) * 0.08f, -1f, 1f) * 0.985f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.6f));   // 빠르게 커졌다 천천히 잦아듦
                data[i] = b * env * 0.6f;
            }
            var clip = AudioClip.Create("north_whoosh", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
