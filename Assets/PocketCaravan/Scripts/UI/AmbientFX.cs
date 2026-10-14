using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면을 살아 있게 하는 작은 연출: 바람에 날리는 꽃잎·잎, 천천히 지나가는 구름 그림자.
    /// 카메라 주변에서만 움직이고, 화면 밖으로 나가면 반대편에서 다시 들어온다. GameManager가 붙인다.
    /// </summary>
    public class AmbientFX : MonoBehaviour
    {
        public int petals = 18;
        public int clouds = 3;
        public Vector2 wind = new Vector2(0.55f, -0.25f);

        class Bit { public Transform t; public SpriteRenderer sr; public float phase, speed; }
        readonly List<Bit> petalBits = new List<Bit>(), cloudBits = new List<Bit>();
        Transform root;

        static readonly Color32[] PetalColors =
        {
            new Color32(0xF7, 0xC6, 0xD4, 0xFF), new Color32(0xFF, 0xE8, 0xEE, 0xFF),
            new Color32(0xB9, 0xD8, 0x7A, 0xFF), new Color32(0xF2, 0xD7, 0x7E, 0xFF),
        };

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || MapRoot.Current == null) { if (root != null) root.gameObject.SetActive(false); return; }
            if (root == null) Build();
            root.gameObject.SetActive(true);

            float h = cam.orthographicSize + 1f, w = h * cam.aspect + 1f;
            Vector2 c = cam.transform.position;
            float dt = Time.deltaTime, time = Time.time;
            float night = GameManager.I != null ? 1f - GameManager.I.Clock.Daylight : 0f;

            foreach (var b in petalBits)
            {
                Vector3 p = b.t.position;
                p += (Vector3)(wind * b.speed * dt);
                p.x += Mathf.Sin(time * 1.7f + b.phase) * 0.35f * dt;
                p.y += Mathf.Cos(time * 1.1f + b.phase) * 0.25f * dt;
                Wrap(ref p, c, w, h);
                b.t.position = p;
                // 팔랑거림: 좌우로 뒤집히며 폭이 바뀐다
                float flip = Mathf.Sin(time * 4f + b.phase * 3f);
                b.t.localScale = new Vector3(Mathf.Abs(flip) < 0.3f ? 0.5f : 1f, 1f, 1f);
                var col = b.sr.color; col.a = 0.9f * (1f - night * 0.8f); b.sr.color = col;
            }
            foreach (var b in cloudBits)
            {
                Vector3 p = b.t.position;
                p.x += wind.x * 0.35f * b.speed * dt;
                p.y += wind.y * 0.2f * b.speed * dt;
                Wrap(ref p, c, w + 5f, h + 3f);
                b.t.position = p;
                b.sr.color = new Color(0.1f, 0.12f, 0.2f, 0.11f * (1f - night));
            }
        }

        static void Wrap(ref Vector3 p, Vector2 c, float w, float h)
        {
            if (p.x > c.x + w) p.x -= 2 * w; else if (p.x < c.x - w) p.x += 2 * w;
            if (p.y > c.y + h) p.y -= 2 * h; else if (p.y < c.y - h) p.y += 2 * h;
        }

        void Build()
        {
            root = new GameObject("Ambient FX").transform;
            DontDestroyOnLoad(root.gameObject);
            var cam = Camera.main;
            Vector2 c = cam.transform.position;
            float h = cam.orthographicSize, w = h * cam.aspect;

            for (int i = 0; i < petals; i++)
            {
                var sprite = PetalSprite(PetalColors[i % PetalColors.Length], i % 2 == 0);
                var b = MakeBit($"Petal {i}", sprite, 22, c + new Vector2(Random.Range(-w, w), Random.Range(-h, h)));
                b.speed = Random.Range(0.6f, 1.4f);
                petalBits.Add(b);
            }
            var cloud = CloudSprite();
            for (int i = 0; i < clouds; i++)
            {
                var b = MakeBit($"Cloud Shadow {i}", cloud, 15, c + new Vector2(Random.Range(-w, w) * 1.3f, Random.Range(-h, h)));
                b.speed = Random.Range(0.7f, 1.2f);
                b.t.localScale = Vector3.one * Random.Range(1f, 1.6f);
                cloudBits.Add(b);
            }
        }

        Bit MakeBit(string name, Sprite sprite, int order, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return new Bit { t = go.transform, sr = sr, phase = Random.value * 10f };
        }

        static Sprite PetalSprite(Color32 col, bool round)
        {
            var t = new Texture2D(3, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var clear = new Color32(0, 0, 0, 0);
            var dark = new Color32((byte)(col.r * 0.8f), (byte)(col.g * 0.8f), (byte)(col.b * 0.8f), 255);
            t.SetPixels32(round ? new[] { dark, col, clear, clear, col, col } : new[] { clear, dark, col, col, col, clear });
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 3, 2), new Vector2(0.5f, 0.5f), 16f);
        }

        /// <summary>가장자리가 디더링된 큰 구름 그림자 (흰색, 색은 SpriteRenderer가 정함)</summary>
        static Sprite CloudSprite()
        {
            const int W = 96, H = 48;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            Vector3[] blobs = { new Vector3(30, 22, 18), new Vector3(52, 26, 20), new Vector3(70, 20, 15), new Vector3(45, 16, 14) };
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float v = 0f;
                foreach (var b in blobs)
                {
                    float dx = (x - b.x) / b.z, dy = (y - b.y) / (b.z * 0.7f);
                    v = Mathf.Max(v, 1f - (dx * dx + dy * dy));
                }
                // 4×4 베이어 디더링으로 가장자리를 픽셀답게
                int bayer = new[] { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 }[(y % 4) * 4 + x % 4];
                bool on = v > 0.25f || (v > 0f && v * 4f * 16f > bayer);
                px[y * W + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
