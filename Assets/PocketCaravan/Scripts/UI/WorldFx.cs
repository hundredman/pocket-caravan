using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 땅 위 작은 움직임 (2026-11-11 윤채). 그리기만 하고 진행·저장과 상관없다.
    /// - 말 걸 대상 고리: Space로 말 걸 사람·장터·자리 발밑에 노란 고리가 은은하게 숨쉰다.
    ///   땅 위 그림이라 사람 그림보다 아래에 깔린다 (2026-11-18, 화면 위 그리기에서 바꿈).
    ///   새 대상으로 바뀌면 고리가 크게 퍼졌다가 자리 잡고, 물결 하나가 바깥으로 퍼진다 (가까이 왔다는 신호)
    /// - 달리기 흙먼지: Shift로 달리면 발밑에서 흙먼지가 퐁퐁 일었다가 옅어진다
    /// 창이 열려 있거나 기념 사진을 찍는 동안은 감춘다. 공용 코드는 읽기만 한다 (PlayerController · Interactable · UIState)
    /// </summary>
    public class WorldFx : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<WorldFx>() != null) return;
            var go = new GameObject("WorldFx");
            DontDestroyOnLoad(go);
            go.AddComponent<WorldFx>();
        }

        static readonly Color RingColor = new Color(1f, .81f, .2f), DustColor = new Color(.86f, .77f, .62f);
        static Texture2D ring;

        Interactable lastTarget;
        float targetAt = -9f, nextPuff;
        struct Puff { public Vector2 p; public float born, drift; }
        readonly List<Puff> puffs = new List<Puff>();

        void Update()
        {
            var pc = PlayerController.Instance;
            float now = Time.unscaledTime;
            var t = pc != null ? pc.TalkTarget : null;
            if (t != lastTarget) { lastTarget = t; targetAt = now; }
            if (pc != null && pc.Running && pc.Motor != null && pc.Motor.IsMoving && !UIState.Blocking && now >= nextPuff)
            {
                nextPuff = now + .09f;
                puffs.Add(new Puff { p = pc.Motor.Feet + new Vector2(Random.Range(-.18f, .18f), Random.Range(-.06f, .02f)), born = now, drift = Random.Range(-.3f, .3f) });
                if (puffs.Count > 24) puffs.RemoveAt(0);
            }
            puffs.RemoveAll(x => now - x.born > .45f);
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            var cam = Camera.main;
            if (cam == null || MapRoot.Current == null || UIState.Blocking || UIState.PhotoMode) return;
            UIStyle.Begin();
            GUI.depth = 8;   // 이름표 · Space 알약 · 알림보다 아래
            float now = Time.unscaledTime;
            DrawPuffs(cam, now);
        }

        static Vector2 ToGui(Camera cam, Vector3 world)
        {
            var s = cam.WorldToScreenPoint(world);
            return new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
        }

        void DrawPuffs(Camera cam, float now)
        {
            var old = GUI.color;
            foreach (var x in puffs)
            {
                float t = (now - x.born) / .45f;
                var p = ToGui(cam, new Vector3(x.p.x + x.drift * t * .3f, x.p.y + t * .22f, 0f));
                float d = Mathf.Round(Mathf.Lerp(2f, 6f, t));
                GUI.color = new Color(DustColor.r, DustColor.g, DustColor.b, old.a * .7f * (1f - t));
                GUI.DrawTexture(new Rect(Mathf.Round(p.x - d / 2f), Mathf.Round(p.y - d / 2f), d, d), Texture2D.whiteTexture);   // 픽셀 네모 흙먼지
            }
            GUI.color = old;
        }

        // 말 걸 대상 고리는 땅 위 그림으로 그린다 (2026-11-18). 화면 위에 그리면 사람 그림을 가려서,
        // 대상 몸 그림 · 내 몸 그림보다 낮은 순서로 발밑에 깐다 (내 고리 SelfMarker와 같은 방식)
        SpriteRenderer ringSr, waveSr;

        SpriteRenderer MakeRingRenderer(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = RingSprite();
            r.enabled = false;
            return r;
        }

        void LateUpdate()
        {
            if (ringSr == null) ringSr = MakeRingRenderer("말 걸 고리");
            if (waveSr == null) waveSr = MakeRingRenderer("말 걸 물결");
            var t = lastTarget;
            var pc = PlayerController.Instance;
            bool show = t != null && t.isActiveAndEnabled && pc != null && MapRoot.Current != null && !UIState.Blocking && !UIState.PhotoMode;
            ringSr.enabled = waveSr.enabled = false;
            if (!show) return;
            float now = Time.unscaledTime;
            // 발밑: 그림이 있으면 그림 아래 끝 가운데, 없으면 자리 조금 아래. 너비는 그림 너비에 맞추되 너무 넓지 않게
            var sr = t.GetComponentInChildren<SpriteRenderer>();
            Vector3 feet; float wUnits;
            if (sr != null && sr.sprite != null && sr.bounds.size.x < 4f) { var b = sr.bounds; feet = new Vector3(b.center.x, b.min.y + .1f, 0f); wUnits = Mathf.Clamp(b.size.x * .9f, .8f, 2.2f); }
            else { feet = t.transform.position + new Vector3(0f, -.42f, 0f); wUnits = 1f; }
            feet = new Vector3(Mathf.Round(feet.x * 16f) / 16f, Mathf.Round(feet.y * 16f) / 16f, t.transform.position.z);
            // 순서: 대상 그림과 내 몸 그림 중 낮은 쪽보다 3 아래 (그림자 9보다도 아래)
            var body = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            // 대상의 첫 그림이 바닥 자국·배처럼 낮은 순서(8 미만)면 무시한다. 땅 그림(타일 2~5) 밑으로 숨지 않게 6 이상
            int bo = body != null ? body.sortingOrder : 11;
            int order = Mathf.Max(6, (sr != null && sr.sortingOrder >= 8 ? Mathf.Min(sr.sortingOrder, bo) : bo) - 3);
            var mat = body != null ? body.sharedMaterial : null;   // 밤 조명을 같이 받게
            float age = now - targetAt, k = Mathf.Clamp01(age / .22f), ease = 1f - (1f - k) * (1f - k);
            // 처음: 크게 퍼졌다가 자리 잡는다. 그 뒤로는 은은하게 숨쉰다
            float sc = Mathf.Lerp(1.6f, 1f, ease) * wUnits / 2f;   // 고리 그림은 32칸 = 2칸 너비
            float a = k < 1f ? ease : .7f + .25f * Mathf.Sin(now * 4f);
            Place(ringSr, feet, sc, order, mat, new Color(RingColor.r, RingColor.g, RingColor.b, a));
            // 물결 하나 (가까이 왔다는 신호, 0.5초)
            if (age < .5f)
            {
                float q = age / .5f;
                Place(waveSr, feet, Mathf.Lerp(1f, 2f, q) * wUnits / 2f, order, mat, new Color(1f, 1f, 1f, .7f * (1f - q)));
            }
        }

        static void Place(SpriteRenderer r, Vector3 at, float scale, int order, Material mat, Color c)
        {
            r.transform.position = at;
            r.transform.localScale = new Vector3(scale, scale, 1f);
            r.sortingOrder = order;
            if (mat != null) r.sharedMaterial = mat;
            r.color = c;
            r.enabled = true;
        }

        static Sprite ringSprite;
        static Sprite RingSprite()
        {
            if (ringSprite != null) return ringSprite;
            var tex = Ring();
            ringSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 16f);
            return ringSprite;
        }

        /// <summary>타원 고리 그림 (32×12, 두께 2칸, 픽셀 그대로)</summary>
        static Texture2D Ring()
        {
            if (ring != null) return ring;
            const int W = 32, H = 12;
            ring = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            float cx = (W - 1) / 2f, cy = (H - 1) / 2f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float ox = (x - cx) / (W / 2f), oy = (y - cy) / (H / 2f);
                    float ix = (x - cx) / (W / 2f - 2f), iy = (y - cy) / (H / 2f - 2f);
                    bool outside = ox * ox + oy * oy > 1f, inside = ix * ix + iy * iy < 1f;
                    px[y * W + x] = !outside && !inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            ring.SetPixels32(px);
            ring.Apply();
            return ring;
        }
    }
}
