using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 맵(north_)에서 다른 맵으로 넘어갈 때의 전환 연출: 검게 어두워지는 대신
    /// 가는 방향에서 눈보라가 휘몰아쳐 화면을 덮고, 새 맵에 도착하면 같은 방향으로 계속 쓸려 나가며 새 맵이 드러난다.
    /// (오른쪽으로 가면 오른쪽에서 들어와 왼쪽으로, 위로 가면 위에서 들어와 아래로 빠짐 — 카메라가 그쪽으로 넘어가는 느낌)
    /// 공용 GameManager의 검은 페이드는 그대로 두고, 그 위에(GUI.depth 더 앞) 그린다. 이동·저장과 무관한 연출만.
    /// </summary>
    public class NorthTransition : MonoBehaviour
    {
        const float CoverTime = 0.28f, RevealTime = 0.7f;
        float progress = -1f;           // -1 꺼짐, 0→1 덮기, 1 유지, 1→2 걷히기
        bool wasTravelling;
        Vector3[] flakes;               // 들어오는 쪽에서부터 거리(0~1), 옆 위치(0~1), 속도
        Vector2Int dir = Vector2Int.right;   // 가는 방향 (화면 기준: y+ = 위)
        AudioSource whoosh;

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
        }

        static bool InNorth => MapRoot.Current != null && MapRoot.Current.mapId != null && MapRoot.Current.mapId.StartsWith("north_");

        void Update()
        {
            var gm = GameManager.I;
            bool travelling = gm != null && gm.IsTravelling;
            if (travelling && !wasTravelling && InNorth)
            {
                progress = 0f;                                   // 북유럽에서 출발할 때만
                var pc = PlayerController.Instance;
                var f = pc != null && pc.Motor != null ? pc.Motor.Facing : Vector2Int.zero;
                if (f == Vector2Int.zero) f = Vector2Int.right;
                dir = Mathf.Abs(f.x) >= Mathf.Abs(f.y) ? new Vector2Int(f.x > 0 ? 1 : -1, 0) : new Vector2Int(0, f.y > 0 ? 1 : -1);
                if (!AudioManager.Muted) whoosh.Play();
            }
            wasTravelling = travelling;
            if (progress < 0f) return;

            float dt = Time.unscaledDeltaTime;
            if (progress < 1f) progress = Mathf.Min(1f, progress + dt / CoverTime);
            else if (!travelling) progress += dt / RevealTime;   // 도착하면 걷히기 시작
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

        void OnGUI()
        {
            if (progress < 0f) return;
            GUI.depth = -1001;   // 공용 검은 페이드(-1000)보다 앞
            float w = Screen.width, h = Screen.height;
            bool horiz = dir.y == 0;
            float len = horiz ? w : h, cross = horiz ? h : w;
            // 띠 위치는 "들어오는 쪽 가장자리에서부터의 거리"(a)로 계산: 덮을 때는 0에서 끝까지 자라고, 걷힐 때는 같은 방향으로 빠져나감
            float a0, a1;
            if (progress <= 1f) { a0 = -40f; a1 = len * Ease(progress) + 40f; }
            else { a0 = len * Ease(progress - 1f) - 40f; a1 = len + 40f; }
            float t = Time.unscaledTime;
            const int Row = 6;
            var snow = new Color(0.93f, 0.96f, 1f, 1f);
            var soft = new Color(snow.r, snow.g, snow.b, 0.45f);
            for (int c = 0; c < cross; c += Row)
            {
                float jag = Mathf.Sin(c * 0.11f + t * 9f) * 22f + Mathf.Sin(c * 0.031f + t * 4f) * 30f;
                float l = a0 + jag, r = a1 + jag;
                if (r <= l) continue;
                GUI.color = snow; Strip(l, r, c, Row, w, h);
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
            GUI.color = Color.white;
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
