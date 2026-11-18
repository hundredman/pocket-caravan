using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 게임 시작 연출. 시작 화면에서 "출발"(새로 시작) · "이어하기"를 누르면 바로 넘어가지 않고:
    ///   1) 화면이 한 번 반짝이고 가운데에 "출발!" 띠가 통통 튀어나온다 (문명·출발 마을 한 줄).
    ///   2) 고른 카드(이어하기는 그 단추)를 가운데로 둥근 구멍이 오므라들며 화면을 닫는다 (픽셀 아이리스).
    ///   3) 다 닫히면 실제 시작(GameManager.BeginNewGame / ContinueFromStartScreen)을 부르고, 맵을 불러오는 동안 검은 화면을 지킨다.
    ///   4) 도착하면 화면 가운데(플레이어)에서 구멍이 열리고, 아래에 도착한 곳 이름이 잠깐 떴다 사라진다.
    /// 11/19 2차: 이어하기는 띠·구멍 없이 부드럽게 어두워졌다 밝아진다. 음악도 닫힐 때 줄고 열릴 때 올라온다 (AudioManager.Silence).
    /// 저장·이동 규칙은 그대로 두고 그리기만 한다. 맵 이동 페이드(GUI.depth -1000)보다 위(-1100)에 그린다.
    /// </summary>
    public class StartLaunch : MonoBehaviour
    {
        enum Phase { Idle, Out, Hold, In }

        static StartLaunch inst;
        Phase phase = Phase.Idle;
        float t, holdSince;
        int stable;
        Vector2 center;
        string title, sub, place;
        Action go;
        bool soft;   // 이어하기: 띠·구멍 없이 부드럽게 어두워졌다 밝아짐
        static Texture2D hole;
        static readonly object MusicKey = new object();

        const float FlashLen = 0.18f, StampLen = 0.3f, CloseAt = 0.12f, CloseLen = 0.8f, SoftLen = 0.7f, OpenLen = 1.1f, NameLen = 2.0f;
        float OutLen => soft ? SoftLen : CloseAt + CloseLen;

        /// <summary>닫히는 동안·불러오는 동안 true (시작 화면이 키를 받지 않게)</summary>
        /// <summary>연출이 화면에 걸려 있는 동안 true (열리는 중 포함). 소문·알림을 미룰 때 쓴다</summary>
        public static bool Active => inst != null && inst.phase != Phase.Idle;
        public static bool Busy => inst != null && (inst.phase == Phase.Out || inst.phase == Phase.Hold);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var g = new GameObject("StartLaunch");
            DontDestroyOnLoad(g);
            inst = g.AddComponent<StartLaunch>();
        }

        /// <summary>연출을 시작한다. centerVirtual = 구멍이 오므라들 곳(가상 해상도 좌표), go = 다 닫혔을 때 할 일</summary>
        public static void Play(Vector2 centerVirtual, bool iris, Action go)
        {
            if (inst == null) Boot();
            if (Busy) return;
            inst.center = centerVirtual; inst.title = null; inst.sub = null; inst.go = go;   // 11/19 3차: "출발!" 같은 문구 띠는 넣지 않는다
            inst.soft = !iris;
            inst.phase = Phase.Out; inst.t = 0f; inst.stable = 0;
            AudioManager.Silence(MusicKey, true);   // 음악은 화면이 닫히는 동안 천천히 줄어든다 (AudioManager가 부드럽게 낮춤)
            if (!inst.soft) Sfx.Play("success", .7f);
        }

        void Update()
        {
            if (phase == Phase.Idle) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);   // 불러오기로 한 프레임이 길어도 건너뛰지 않게
            t += dt;
            switch (phase)
            {
                case Phase.Out:
                    if (t >= OutLen)
                    {
                        phase = Phase.Hold; holdSince = Time.unscaledTime; t = 0f;
                        var g = go; go = null;
                        try { g?.Invoke(); }
                        catch (Exception e) { Debug.LogException(e); phase = Phase.In; t = 0f; }
                    }
                    break;
                case Phase.Hold:
                    var gm = GameManager.I;
                    bool ready = gm != null && !gm.IsTravelling && MapRoot.Current != null && PlayerController.Instance != null
                                 && Time.unscaledTime - holdSince > 0.35f;
                    stable = ready ? stable + 1 : 0;
                    if (stable >= 3 || Time.unscaledTime - holdSince > 10f)   // 한참 걸리면 그래도 연다
                    {
                        phase = Phase.In; t = 0f;
                        place = MapRoot.Current != null ? MapRoot.Current.displayName : "";
                        AudioManager.Silence(MusicKey, false);   // 도착한 곳 음악이 천천히 올라온다
                        Sfx.Play("open", .5f);
                    }
                    break;
                case Phase.In:
                    if (t >= Mathf.Max(OpenLen, NameLen)) phase = Phase.Idle;
                    break;
            }
        }

        void OnGUI()
        {
            if (phase == Phase.Idle) return;
            UIStyle.Begin();
            GUI.depth = -1100;
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            float maxR = Mathf.Sqrt(W * W + H * H);
            switch (phase)
            {
                case Phase.Out:
                {
                    if (soft)
                    {
                        // 이어하기: 띠 없이 천천히 어두워진다
                        CuteUI.Fill(new Rect(0, 0, W, H), WithA(Dark, Ease(t / SoftLen)));
                        break;
                    }
                    // 3) 구멍이 고른 곳으로 오므라든다 (끝무렵엔 남은 구멍도 같이 어두워져 뚝 끊기지 않게)
                    float k = Ease((t - CloseAt) / CloseLen);
                    if (t > CloseAt)
                    {
                        Iris(center, Mathf.Lerp(maxR, 0f, k), W, H);
                        if (k > 0.6f) CuteUI.Fill(new Rect(0, 0, W, H), WithA(Dark, (k - 0.6f) / 0.4f));
                    }
                    // 1) 반짝
                    if (t < FlashLen) CuteUI.Fill(new Rect(0, 0, W, H), new Color32(255, 250, 236, (byte)(150 * (1f - t / FlashLen))));
                    break;
                }
                case Phase.Hold:
                {
                    CuteUI.Fill(new Rect(0, 0, W, H), Dark);
                    int dots = 1 + (int)(Time.unscaledTime * 3f) % 3;   // 불러오는 중 · · ·
                    for (int i = 0; i < dots; i++) CuteUI.Fill(new Rect(W / 2f - 9 + i * 7, H / 2f - 2, 4, 4), new Color32(0xF7, 0xD7, 0x9E, 200));
                    break;
                }
                case Phase.In:
                {
                    if (t < OpenLen)
                    {
                        float k = OutCubic(t / OpenLen);
                        if (soft) CuteUI.Fill(new Rect(0, 0, W, H), WithA(Dark, 1f - k));
                        else
                        {
                            Iris(new Vector2(W / 2f, H / 2f), Mathf.Lerp(0f, maxR, k), W, H);
                            CuteUI.Fill(new Rect(0, 0, W, H), WithA(Dark, Mathf.Clamp01(1f - k * 1.6f)));   // 처음엔 구멍 안도 어둑하게 → 서서히 밝아짐
                        }
                    }
                    // 도착한 곳 이름표 (아래 가운데, 천천히 떴다 사라짐)
                    if (!string.IsNullOrEmpty(place) && t > 0.25f)
                    {
                        float a = Mathf.Clamp01((t - 0.25f) / 0.25f) * Mathf.Clamp01((NameLen - t) / 0.35f);
                        if (a > 0.01f)
                        {
                            var gc = GUI.color; GUI.color = new Color(1f, 1f, 1f, a);
                            float lift = Mathf.Round((1f - Ease((t - 0.25f) / 0.4f)) * 6f);
                            float wv = Mathf.Max(96f, CuteUI.Width(place, false, true) + 36f);
                            var r = new Rect(Mathf.Round(W / 2f - wv / 2f), Mathf.Round(H * 0.7f + lift), wv, 26);
                            CuteUI.Box(r, 4, TitleUI.Cream, TitleUI.Choco, 2, 3, TitleUI.ChocoShadow);
                            CuteUI.LabelCenter(r.center.x, r.center.y, place, TitleUI.Choco, false, true);
                            GUI.color = gc;
                        }
                    }
                    break;
                }
            }
        }

        static readonly Color32 Dark = new Color32(0x1E, 0x14, 0x12, 255);

        /// <summary>"출발!" 띠 (코랄 바탕 · 초코 테) + 아래 한 줄</summary>
        void Stamp(Vector2 c, float s)
        {
            var m = GUI.matrix;
            CuteUI.ScaleAround(s, c);
            float tw = Mathf.Max(CuteUI.Width(title, false, true) * 2f, CuteUI.Width(sub ?? "", true, false)) + 40f;
            var r = new Rect(Mathf.Round(c.x - tw / 2f), Mathf.Round(c.y - 22), tw, 44);
            CuteUI.Box(r, 4, TitleUI.Coral, TitleUI.Choco, 2, 3, TitleUI.ChocoShadow);
            var big = GUI.matrix;
            CuteUI.ScaleAround(2f, new Vector2(c.x, r.y + 15));
            CuteUI.LabelCenter(c.x, r.y + 15 + 1, title, TitleUI.Choco, false, true);
            CuteUI.LabelCenter(c.x, r.y + 15, title, CuteUI.White, false, true);
            GUI.matrix = big;
            if (!string.IsNullOrEmpty(sub)) CuteUI.LabelCenter(c.x, r.yMax - 9, sub, TitleUI.Cream, true, true);
            GUI.matrix = m;
        }

        /// <summary>가운데 c, 반지름 r인 둥근 구멍만 남기고 화면을 가린다 (구멍 그림 + 둘레 네모 넷)</summary>
        static void Iris(Vector2 c, float r, float W, float H)
        {
            if (r <= 0.75f) { CuteUI.Fill(new Rect(0, 0, W, H), Dark); return; }
            if (hole == null) hole = MakeHole(48);
            var q = new Rect(Mathf.Round(c.x - r), Mathf.Round(c.y - r), Mathf.Round(2f * r), Mathf.Round(2f * r));
            if (Event.current.type == EventType.Repaint)
            {
                var gc = GUI.color; GUI.color = new Color(Dark.r / 255f, Dark.g / 255f, Dark.b / 255f, 1f);
                GUI.DrawTexture(q, hole);
                GUI.color = gc;
            }
            CuteUI.Fill(new Rect(0, 0, W, Mathf.Max(0, q.y)), Dark);
            CuteUI.Fill(new Rect(0, q.yMax, W, Mathf.Max(0, H - q.yMax)), Dark);
            CuteUI.Fill(new Rect(0, q.y, Mathf.Max(0, q.x), q.height), Dark);
            CuteUI.Fill(new Rect(q.xMax, q.y, Mathf.Max(0, W - q.xMax), q.height), Dark);
        }

        /// <summary>가운데가 비고 둘레가 흰 작은 그림 (점 단위로 또렷하게 늘어나 픽셀 아이리스가 된다)</summary>
        static Texture2D MakeHole(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            float rr = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - rr, dy = y + 0.5f - rr;
                    px[y * n + x] = dx * dx + dy * dy <= rr * rr ? new Color32(255, 255, 255, 0) : new Color32(255, 255, 255, 255);
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float OutCubic(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
        static Color32 WithA(Color32 c, float a) { c.a = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(a)); return c; }
        static float Back(float x) { x = Mathf.Clamp01(x); const float c = 2.2f; return 1f + (c + 1f) * Mathf.Pow(x - 1f, 3f) + c * Mathf.Pow(x - 1f, 2f); }
    }

    /// <summary>11/19 화면이 처음 뜰 때 요소를 하나씩 나타내는 도움 (투명도 + 살짝 밀려 있다가 제자리로). 로그인 화면에서 쓴다</summary>
    public static class UIAppear
    {
        static readonly System.Collections.Generic.Stack<(Color, Matrix4x4)> st = new System.Collections.Generic.Stack<(Color, Matrix4x4)>();
        public static float K(float since, float delay, float dur = .36f)
        {
            float x = Mathf.Clamp01((Time.unscaledTime - since - delay) / dur);
            return 1f - (1f - x) * (1f - x) * (1f - x);
        }
        public static void Push(float k, float dx = 0f, float dy = 10f)
        {
            st.Push((GUI.color, GUI.matrix));
            var c = GUI.color; c.a *= k; GUI.color = c;
            GUI.matrix = GUI.matrix * Matrix4x4.Translate(new Vector3(Mathf.Round((1f - k) * dx), Mathf.Round((1f - k) * dy), 0f));
        }
        public static void Pop() { if (st.Count == 0) return; var (c, m) = st.Pop(); GUI.color = c; GUI.matrix = m; }
        public static void Reset() => st.Clear();   // 지난 프레임에 남은 것만 비운다 (GUI 상태는 UIStyle.Begin이 새로 맞춤)
    }
}
