using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 게임 마우스 커서 (2026-11-18 윤채, "커서를 귀엽게 · 누를 수 있는 곳에서는 손가락으로").
    /// - 평소: 주황 픽셀 화살표. 누를 수 있는 단추 위: 손가락. 글 쓰는 칸 위: 글 막대.
    /// - 단추를 그리는 쪽이 OnGUI(Repaint)에서 Hover(rect)를 부르면 그 프레임 마우스가 그 안에 있을 때 손가락이 된다.
    ///   CuteUI.Hit · CuteUI.LayoutButton · CuteUI 단추들이 저절로 부른다. 직접 그린 클릭 자리는 Hover를 따로 부르면 된다.
    /// - 그림은 16칸 픽셀을 정수배로 키운다 (보통 2배 32px, 4K쯤 3배). 윈도가 늘리지 않게 소프트웨어 커서로 그린다.
    /// </summary>
    public static class GameCursor
    {
        enum Kind { Arrow, Hand, Beam }

        static int handFrame = -10, beamFrame = -10;
        static Texture2D[] tex;
        static int builtScale, shown = -1;

        /// <summary>누를 수 있는 자리 (OnGUI 안에서). 마우스가 그 안이면 이번 프레임 커서는 손가락</summary>
        public static void Hover(Rect r)
        {
            var e = Event.current;
            if (e != null && e.type == EventType.Repaint && r.Contains(e.mousePosition)) handFrame = Time.frameCount;
        }

        /// <summary>글 쓰는 칸 (OnGUI 안에서). 마우스가 그 안이면 글 막대</summary>
        public static void HoverText(Rect r)
        {
            var e = Event.current;
            if (e != null && e.type == EventType.Repaint && r.Contains(e.mousePosition)) beamFrame = Time.frameCount;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isBatchMode) return;
            var go = new GameObject("GameCursor");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<Runner>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { handFrame = beamFrame = -10; shown = -1; builtScale = 0; tex = null; }

        sealed class Runner : MonoBehaviour
        {
            // OnGUI(Repaint)는 LateUpdate 뒤에 온다 → 지난 프레임 표시를 보고 고른다
            void LateUpdate()
            {
                int f = Time.frameCount;
                var want = handFrame >= f - 1 ? Kind.Hand : beamFrame >= f - 1 ? Kind.Beam : Kind.Arrow;   // 칸 안 단추(가/A)는 손가락
                int k = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 720f), 2, 3);   // 11/18 1배(16px)는 너무 작았다 → 보통 2배, 4K쯤에서 3배
                if (k != builtScale || tex == null) { Build(k); shown = -1; }
                if ((int)want == shown) return;
                shown = (int)want;
                Cursor.SetCursor(tex[shown], Hot[shown] * k, CursorMode.ForceSoftware);   // 11/18 윈도가 하드웨어 커서를 시스템 크기(고배율 화면이면 64px)로 늘려서 너무 컸다 → 게임이 정한 크기 그대로 그린다
            }

            void OnDestroy() { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }   // 편집기에서 Play를 멈추면 원래 커서로
        }

        // ---------------- 그림 ----------------
        // X 잉크 테두리 · o 주황 · h 밝은 주황 · w 크림
        static readonly Vector2[] Hot = { new Vector2(1, 1), new Vector2(5, 1), new Vector2(3, 7) };
        static readonly string[][] Rows =
        {
            new[]
            {
                "X...........",
                "XX..........",
                "XhX.........",
                "XhoX........",
                "XhooX.......",
                "XhoooX......",
                "XhooooX.....",
                "XhoooooX....",
                "XhooooooX...",
                "XhoooooooX..",
                "XhooooXXXXX.",
                "XhoXooX.....",
                "XoX.XooX....",
                "XX..XooX....",
                "X....XooX...",
                ".....XXX....",
            },
            new[]
            {
                "....XX..........",
                "...XwwX.........",
                "...XwwX.........",
                "...XwwX.........",
                "...XwwXXX.......",
                "...XwwXwwXXX....",
                "...XwwXwwXwwXX..",
                ".XXXwwXwwXwwXwX.",
                "XwwXwwwwwwwwXwwX",
                "XwwwwwwwwwwwwwwX",
                ".XwwwwwwwwwwwwwX",
                "..XwwwwwwwwwwwX.",
                "..XwwwwwwwwwwwX.",
                "...XoooooooooX..",
                "...XoooooooooX..",
                "...XXXXXXXXXXX..",
            },
            new[]
            {
                "XXX.XXX",
                "XwwXwwX",
                "XXXwXXX",
                "..XwX..",
                "..XwX..",
                "..XwX..",
                "..XwX..",
                "..XwX..",
                "..XwX..",
                "..XwX..",
                "..XwX..",
                "XXXwXXX",
                "XwwXwwX",
                "XXX.XXX",
            },
        };

        static void Build(int k)
        {
            builtScale = k;
            tex = new Texture2D[Rows.Length];
            for (int i = 0; i < Rows.Length; i++) tex[i] = Make(Rows[i], k);
        }

        static Texture2D Make(string[] rows, int k)
        {
            // 커서 그림은 16칸 틀(정사각) 안 왼쪽 위에 둔다
            int n = 16 * k;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    Color32 c;
                    switch (rows[y][x])
                    {
                        case 'X': c = CuteUI.Ink; break;
                        case 'o': c = CuteUI.Frame; break;
                        case 'h': c = CuteUI.FrameHi; break;
                        case 'w': c = CuteUI.Paper; break;
                        default: continue;
                    }
                    for (int dy = 0; dy < k; dy++)
                        for (int dx = 0; dx < k; dx++)
                            px[(n - 1 - (y * k + dy)) * n + x * k + dx] = c;   // 텍스처는 아래 줄부터
                }
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }
    }
}
