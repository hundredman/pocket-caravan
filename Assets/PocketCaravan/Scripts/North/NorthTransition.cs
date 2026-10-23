using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 맵(north_)에서 다른 맵으로 넘어갈 때의 전환 연출: 검게 어두워지는 대신
    /// 오른쪽에서 눈보라가 휘몰아쳐 화면을 덮고, 새 맵에 도착하면 왼쪽으로 쓸려 나가며 새 맵이 드러난다.
    /// 공용 GameManager의 검은 페이드는 그대로 두고, 그 위에(GUI.depth 더 앞) 그린다. 이동·저장과 무관한 연출만.
    /// </summary>
    public class NorthTransition : MonoBehaviour
    {
        const float CoverTime = 0.28f, RevealTime = 0.7f;
        float progress = -1f;           // -1 꺼짐, 0→1 덮기, 1 유지, 1→2 걷히기
        bool wasTravelling;
        Vector3[] flakes;               // x(0~1), y(0~1), 속도
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
                f.x -= f.z * dt;
                f.y += Mathf.Sin(Time.unscaledTime * 3f + i) * 0.02f * dt;
                if (f.x < -0.05f) f.x += 1.1f;
                flakes[i] = f;
            }
        }

        void OnGUI()
        {
            if (progress < 0f) return;
            GUI.depth = -1001;   // 공용 검은 페이드(-1000)보다 앞
            float w = Screen.width, h = Screen.height;
            // 눈보라 띠: 덮을 때는 오른쪽에서 들어와 왼쪽 끝까지, 걷힐 때는 왼쪽으로 빠져나감
            float left, right;
            if (progress <= 1f) { left = w * (1f - Ease(progress)) - 40f; right = w + 40f; }
            else { left = -40f; right = w * (1f - Ease(progress - 1f)) + 40f; }
            float t = Time.unscaledTime;
            const int Row = 6;
            var snow = new Color(0.93f, 0.96f, 1f, 1f);
            for (int y = 0; y < h; y += Row)
            {
                float jag = Mathf.Sin(y * 0.11f + t * 9f) * 22f + Mathf.Sin(y * 0.031f + t * 4f) * 30f;
                float l = left + jag, r = right + jag;
                if (r <= l) continue;
                GUI.color = snow;
                GUI.DrawTexture(new Rect(l, y, r - l, Row), Texture2D.whiteTexture);
                // 가장자리는 옅게 흩날림
                GUI.color = new Color(snow.r, snow.g, snow.b, 0.45f);
                GUI.DrawTexture(new Rect(l - 26f, y, 26f, Row), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r, y, 26f, Row), Texture2D.whiteTexture);
            }
            // 바람에 날리는 눈송이 (띠 앞뒤로)
            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            float size = Mathf.Max(2f, h / 180f);
            foreach (var f in flakes)
            {
                float x = f.x * w, y = f.y * h;
                if (x < left - 140f || x > right + 140f) continue;
                GUI.DrawTexture(new Rect(x, y, size * (1f + f.z), size), Texture2D.whiteTexture);
            }
            // 띠 안쪽은 살짝 푸른 기운
            GUI.color = new Color(0.75f, 0.85f, 1f, 0.18f);
            GUI.DrawTexture(new Rect(Mathf.Max(0f, left), 0, Mathf.Max(0f, Mathf.Min(w, right) - Mathf.Max(0f, left)), h), Texture2D.whiteTexture);
            GUI.color = Color.white;
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
