using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 땅 위 작은 움직임 (2026-11-11 윤채). 그리기만 하고 진행·저장과 상관없다.
    /// - 말 걸 대상 고리: Space로 말 걸 사람·장터·자리 발밑에 노란 고리가 은은하게 숨쉰다.
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
            DrawRing(cam, now);
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

        void DrawRing(Camera cam, float now)
        {
            var t = lastTarget;
            if (t == null || !t.isActiveAndEnabled) return;
            // 발밑: 그림이 있으면 그림 아래 끝 가운데, 없으면 자리 조금 아래. 너비는 그림 너비에 맞추되 너무 넓지 않게
            var sr = t.GetComponentInChildren<SpriteRenderer>();
            Vector3 feet; float wUnits;
            if (sr != null && sr.sprite != null && sr.bounds.size.x < 4f) { var b = sr.bounds; feet = new Vector3(b.center.x, b.min.y + .1f, 0f); wUnits = Mathf.Clamp(b.size.x * .9f, .8f, 2.2f); }
            else { feet = t.transform.position + new Vector3(0f, -.42f, 0f); wUnits = 1f; }
            var l = ToGui(cam, feet - new Vector3(wUnits / 2f, 0f, 0f));
            var r = ToGui(cam, feet + new Vector3(wUnits / 2f, 0f, 0f));
            float w = Mathf.Max(10f, r.x - l.x), h = Mathf.Round(w * .38f);
            var c = new Vector2((l.x + r.x) / 2f, l.y);
            float age = now - targetAt, k = Mathf.Clamp01(age / .22f), ease = 1f - (1f - k) * (1f - k);
            var old = GUI.color;
            // 처음: 크게 퍼졌다가 자리 잡는다. 그 뒤로는 은은하게 숨쉰다
            float s = Mathf.Lerp(1.6f, 1f, ease);
            float a = k < 1f ? ease : .55f + .25f * Mathf.Sin(now * 4f);
            GUI.color = new Color(RingColor.r, RingColor.g, RingColor.b, old.a * a);
            GUI.DrawTexture(new Rect(Mathf.Round(c.x - w * s / 2f), Mathf.Round(c.y - h * s / 2f), Mathf.Round(w * s), Mathf.Round(h * s)), Ring());
            // 물결 하나 (가까이 왔다는 신호, 0.5초)
            if (age < .5f)
            {
                float q = age / .5f, s2 = Mathf.Lerp(1f, 2f, q);
                GUI.color = new Color(1f, 1f, 1f, old.a * .7f * (1f - q));
                GUI.DrawTexture(new Rect(Mathf.Round(c.x - w * s2 / 2f), Mathf.Round(c.y - h * s2 / 2f), Mathf.Round(w * s2), Mathf.Round(h * s2)), Ring());
            }
            GUI.color = old;
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
