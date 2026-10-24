using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 맵(north_) 분위기 (보이기·들리기만: 이동·충돌·저장에는 영향 없음, 공용 코드 수정 없음).
    /// - 공용 AmbientFX의 분홍 꽃잎 대신 눈. 날씨는 게임 시간 4시간마다 바뀐다: 맑음 → 가랑눈 → 함박눈 → 눈보라
    /// - 맑거나 가랑눈 내리는 밤에는 하늘에 초록 오로라가 일렁인다 (밤마다 뜨는 건 아님)
    /// - 바람 소리 (날씨가 거칠수록 크게). 파일 없이 코드로 만든 소리
    /// 북유럽을 벗어나면 원래대로 되돌린다.
    /// </summary>
    public class NorthAtmosphere : MonoBehaviour
    {
        public enum Weather { Clear, Light, Heavy, Blizzard }
        public static Weather Current { get; private set; }   // 다른 북유럽 연출이 참고 (나중에 썰매·대사 등)
        public static bool AuroraOut { get; private set; }    // 지금 오로라가 떠 있는지 (얼음 낚시: 황금 연어가 잘 잡힘, 비르카 축제)
        public static Weather Shown { get; private set; }     // 이 맵에서 실제로 보이는 날씨 (중앙 구역은 약해짐) — 이동 속도·길 잃은 상인이 참고

        WeatherOverlay overlay;
        WeatherSkin[] skins;
        AmbientFX petals;
        GameManager owner;
        GameObject petalRoot;
        bool inNorth, hidPetals;
        int lastBlock = -1;
        int applied = -1;
        AudioSource wind;
        Transform aurora;
        SpriteRenderer[] ribbons;
        Texture2D ribbonTex;

        static readonly float[] WindVolume = { .04f, .08f, .16f, .38f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthAtmosphere>() != null) return;
            var go = new GameObject("NorthAtmosphere");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthAtmosphere>();
        }

        static WeatherSkin Skin(string id, int count, Vector2 drift, float size, float flutter, float haze)
        {
            var s = ScriptableObject.CreateInstance<WeatherSkin>();
            s.id = id; s.displayName = id;
            s.haze = new Color(.86f, .9f, .96f, haze);
            s.particleColor = new Color(.96f, .98f, 1f, .85f);
            s.particleCount = count; s.drift = drift; s.size = new Vector2(size, size); s.flutter = flutter; s.seed = 31;
            return s;
        }

        void Awake()
        {
            skins = new[]
            {
                Skin("north_clear", 8, new Vector2(-.3f, -.8f), .05f, .5f, 0f),        // 맑음: 가끔 흩날리는 눈송이
                Skin("north_light", 55, new Vector2(-.5f, -1.6f), .06f, .8f, .05f),    // 가랑눈
                Skin("north_heavy", 120, new Vector2(-1.2f, -2.4f), .08f, 1f, .12f),   // 함박눈
                Skin("north_blizzard", 160, new Vector2(-7f, -2.5f), .07f, .4f, .32f), // 눈보라: 옆으로 몰아침, 하얗게 흐림
            };
            overlay = gameObject.AddComponent<WeatherOverlay>();
            wind = gameObject.AddComponent<AudioSource>();
            wind.clip = MakeWind(); wind.loop = true; wind.volume = 0f; wind.playOnAwake = false;
        }

        void OnDestroy()
        {
            if (skins != null) foreach (var s in skins) if (s != null) Destroy(s);
            if (ribbonTex != null) Destroy(ribbonTex);
        }

        /// <summary>게임 시간 4시간 단위로 정해지는 날씨 (같은 시간이면 언제 봐도 같음)</summary>
        static Weather Roll(int block)
        {
            uint h = unchecked((uint)block * 2654435761u) ^ 0x9e3779b9u;
            h ^= h >> 13; h = unchecked(h * 0x5bd1e995u); h ^= h >> 15;
            int r = (int)(h % 100);
            return r < 30 ? Weather.Clear : r < 65 ? Weather.Light : r < 90 ? Weather.Heavy : Weather.Blizzard;
        }

        /// <summary>0 = 북유럽 본토, 1 = 중앙 윗줄(고갯길·갈림길), 2 = 가운데(초소·썰매·룬돌), 3 = 북문 큰길</summary>
        static int ThawLevel(string mapId)
        {
            if (!mapId.StartsWith("north_c_")) return 0;
            switch (mapId)
            {
                case "north_c_thaw": case "north_c_west": case "north_c_east": return 1;
                case "north_c_gate": return 3;
                default: return 2;
            }
        }

        static bool AuroraNight(int day)
        {
            uint h = unchecked((uint)day * 40503u + 17u); h ^= h >> 7; h = unchecked(h * 2246822519u); h ^= h >> 13;
            return h % 100 < 65;
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm != owner) { owner = gm; petals = gm != null ? gm.GetComponent<AmbientFX>() : null; petalRoot = null; hidPetals = false; }
            bool north = MapRoot.Current != null && MapRoot.Current.mapId != null && MapRoot.Current.mapId.StartsWith("north_");

            if (!north)
            {
                if (inNorth) { overlay.Configure(null); applied = -1; lastBlock = -1; }
                if (hidPetals) { if (petals != null) petals.enabled = true; hidPetals = false; }
                if (wind.isPlaying) wind.Stop();
                if (aurora != null) aurora.gameObject.SetActive(false);
                AuroraOut = false;
                inNorth = false;
                return;
            }
            inNorth = true;

            // 꽃잎 숨기기 (북유럽엔 눈)
            if (petals != null && petals.enabled) { petals.enabled = false; hidPetals = true; }
            if (petalRoot == null && Time.frameCount % 30 == 0) petalRoot = GameObject.Find("Ambient FX");
            if (petalRoot != null && petalRoot.activeSelf) petalRoot.SetActive(false);

            float hours = gm != null ? gm.Clock.TotalHours : 0f;
            int block = Mathf.FloorToInt(hours / 4f);
            if (block != lastBlock) { lastBlock = block; Current = Roll(block); }
            // 중앙 경유 구역(north_c_)은 남쪽으로 갈수록 눈이 약하다: 윗줄은 함박눈까지, 가운데는 가랑눈까지, 북문 큰길은 거의 안 옴
            int thaw = ThawLevel(MapRoot.Current.mapId);
            int shown = Mathf.Min((int)Current, thaw == 0 ? 3 : thaw == 1 ? 2 : thaw == 2 ? 1 : 0);
            Shown = thaw >= 3 && Current < Weather.Heavy ? Weather.Clear : (Weather)shown;
            int key = thaw >= 3 && Current < Weather.Heavy ? -2 : shown;     // 북문 큰길: 함박눈 이상일 때만 가끔 눈송이
            if (key != applied) { overlay.Configure(key == -2 ? null : skins[shown]); applied = key; }

            float target = AudioManager.Muted ? 0f : WindVolume[shown] * (thaw >= 3 ? 0.5f : 1f);
            wind.volume = Mathf.MoveTowards(wind.volume, target, Time.deltaTime * 0.2f);
            if (!wind.isPlaying) wind.Play();

            float night = gm != null ? 1f - gm.Clock.Daylight : 0f;
            int day = Mathf.FloorToInt(hours / 24f);
            bool show = night > 0.05f && (Weather)shown <= Weather.Light && AuroraNight(day) && thaw < 3;
            AuroraOut = show;
            UpdateAurora(show ? night : 0f);
        }

        void UpdateAurora(float strength)
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (aurora == null)
            {
                if (strength <= 0f) return;
                BuildAurora();
            }
            aurora.gameObject.SetActive(strength > 0f);
            if (strength <= 0f) return;
            float h = cam.orthographicSize * 2f, w = h * cam.aspect;
            var c = cam.transform.position;
            float t = Time.time;
            for (int i = 0; i < ribbons.Length; i++)
            {
                var r = ribbons[i];
                // 화면 위쪽에 겹친 띠 세 줄이 천천히 일렁인다
                float y = c.y + h * (0.34f - i * 0.07f) + Mathf.Sin(t * 0.35f + i * 1.7f) * 0.25f;
                float x = c.x + Mathf.Sin(t * 0.12f + i) * w * 0.08f;
                r.transform.position = new Vector3(x, y, 0f);
                r.transform.localScale = new Vector3(w * 1.3f / 8f * (1f + 0.05f * Mathf.Sin(t * 0.5f + i)), (1.6f + 0.4f * Mathf.Sin(t * 0.7f + i * 2f)) / 2f, 1f);
                float a = strength * (0.30f - i * 0.06f) * (0.75f + 0.25f * Mathf.Sin(t * 0.9f + i * 3f));
                r.color = i == 1 ? new Color(0.45f, 1f, 0.85f, a) : new Color(0.35f, 1f, 0.55f, a);
            }
        }

        void BuildAurora()
        {
            aurora = new GameObject("Aurora").transform;
            aurora.SetParent(transform, false);
            // 세로로 위·아래가 흐려지는 띠 (가로 128 x 세로 32), 가로로 커튼 같은 주름
            const int W = 128, H = 32;
            ribbonTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float v = Mathf.Sin(Mathf.PI * y / (H - 1));
                    float curtain = 0.6f + 0.4f * Mathf.Sin(x * 0.45f) * Mathf.Sin(x * 0.11f + 1f);
                    float edge = Mathf.Clamp01(Mathf.Min(x, W - 1 - x) / 20f);
                    byte a = (byte)(255 * Mathf.Clamp01(v * v * curtain * edge));
                    px[y * W + x] = new Color32(255, 255, 255, a);
                }
            ribbonTex.SetPixels32(px); ribbonTex.Apply();
            var sprite = Sprite.Create(ribbonTex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);
            ribbons = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Aurora Ribbon " + i);
                go.transform.SetParent(aurora, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite; sr.sortingOrder = 38;   // 날씨 김(39)·눈(40) 아래, 세상 위
                ribbons[i] = sr;
            }
        }

        /// <summary>바람 소리: 갈색 소음에 느린 세기 변화 (4초 반복)</summary>
        static AudioClip MakeWind()
        {
            const int rate = 22050; int n = rate * 4;
            var data = new float[n];
            var rng = new System.Random(3);
            float b = 0f;
            for (int i = 0; i < n; i++)
            {
                b = Mathf.Clamp(b + ((float)rng.NextDouble() * 2f - 1f) * 0.04f, -1f, 1f) * 0.995f;
                float gust = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * i / n) * Mathf.Sin(2f * Mathf.PI * 3f * i / n + 1f);
                data[i] = b * gust * 0.8f;
            }
            for (int i = 0; i < 2000; i++) { float k = i / 2000f; data[i] = data[i] * k + data[n - 2000 + i] * (1f - k); }   // 이음매 부드럽게
            var clip = AudioClip.Create("north_wind", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
