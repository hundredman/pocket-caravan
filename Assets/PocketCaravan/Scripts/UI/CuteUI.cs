using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-11-05 UI B안(귀여움 + 대비) 그리기 도구. 상태 줄·할 일 줄·교역 장부·온라인 표시·말 걸기 안내가 쓴다.
    /// 다른 창(장터·퀘스트 수첩·대화 등)은 아직 UIStyle 그대로다.
    /// 색은 역할마다 하나: 상호작용(청록) = 누르거나 고를 수 있는 것, 알림(빨강) = 새 이야기·주의,
    /// 살 때(파랑)·팔 때(주황), 이득(초록)·0(회색)·손해(빨강). 단축키는 중립색 키캡.
    /// 텍스처는 sRGB로 만들고 색을 그대로 넣는다. UIStyle.Lin을 거치면 두 번 어두워진다
    /// (11/04 윈도우 캡처: 창 바탕 #2C263B가 #04020B로 나옴).
    /// OnGUI 안에서 UIStyle.Begin() 다음에 부른다. 좌표는 UIStyle 가상 좌표다.
    /// </summary>
    public static class CuteUI
    {
        // ---------------- 색 (역할) ----------------
        public static readonly Color32 Ink = Hex(0x2A1A2E), Paper = Hex(0xFFFAF2), Paper2 = Hex(0xFFF0DC), Line = Hex(0xECD3B0),
            Frame = Hex(0xFF9A4D), FrameHi = Hex(0xFFC58F), FrameLo = Hex(0xE0702A), Deco = Hex(0xFFCF33),
            Text = Hex(0x2A1A2E), Sub = Hex(0x6A5464),
            Interact = Hex(0x2E8C86), InteractHi = Hex(0x6BBDB5), InteractLo = Hex(0x1F625E), OnInteract = Hex(0xFFFFFF),   // 11/05 보라(#6B5BFF)가 너무 튀어서 운하 청록으로
            Alert = Hex(0xFF3D5F),
            Buy = Hex(0x1A98D8), BuySoft = Hex(0xD8EFFA), Sell = Hex(0xE08400), SellSoft = Hex(0xFFE9C2),
            Up = Hex(0x22A84A), Zero = Hex(0x958A98), ZeroSoft = Hex(0xECE4EC), Down = Hex(0xFF3D5F),
            KeyFace = Hex(0xF4EFE9), KeyLow = Hex(0xD6CBC0),
            Stamina = Hex(0x37C45A), StaminaHi = Hex(0xA5F0B5), Load = Hex(0xE9C46A), LoadHi = Hex(0xFBE3A0), Track = Hex(0xECD3B0),
            White = Hex(0xFFFFFF), AlertSoft = Hex(0xFFE6EA), KeyLine = Hex(0xC9B8C2);
        /// <summary>옅은 그림자 (조작 안내·알림처럼 떠 있는 줄)</summary>
        public static readonly Color32 SoftShadow = new Color32(42, 26, 46, 70);
        static readonly Color32 Shadow = new Color32(42, 26, 46, 175), DimColor = new Color32(30, 15, 35, 150);
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        static Color32 Hex(int v) => new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        static string Key(Color32 c) => $"{c.r:x2}{c.g:x2}{c.b:x2}{c.a:x2}";
        static bool Repaint => Event.current != null && Event.current.type == EventType.Repaint;

        // ---------------- 텍스처 ----------------
        static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, GUIStyle> slices = new Dictionary<string, GUIStyle>();

        static Texture2D NewTex(int w, int h, TextureWrapMode wrap = TextureWrapMode.Clamp) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false, false) { filterMode = FilterMode.Point, wrapMode = wrap, hideFlags = HideFlags.HideAndDontSave };

        /// <summary>그림(위에서 아래로 적은 줄)을 텍스처로. 텍스처는 아래가 0번 줄이라 뒤집어 넣는다</summary>
        static Texture2D Paint(int w, int h, System.Func<int, int, Color32> at, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var t = NewTex(w, h, wrap);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) px[(h - 1 - y) * w + x] = at(x, y);
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        /// <summary>가장자리에서 몇 번째 줄인지 (밖이면 -1)</summary>
        static int[,] Rings(bool[,] inside)
        {
            int w = inside.GetLength(0), h = inside.GetLength(1);
            var d = new int[w, h];
            var q = new Queue<Vector2Int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    d[x, y] = inside[x, y] ? int.MaxValue : -1;
                    if (!inside[x, y]) continue;
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1 || !inside[x - 1, y] || !inside[x + 1, y] || !inside[x, y - 1] || !inside[x, y + 1])
                    { d[x, y] = 0; q.Enqueue(new Vector2Int(x, y)); }
                }
            var steps = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var s in steps)
                {
                    var n = p + s;
                    if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h || d[n.x, n.y] != int.MaxValue) continue;
                    d[n.x, n.y] = d[p.x, p.y] + 1;
                    q.Enqueue(n);
                }
            }
            return d;
        }

        static bool[,] RoundMask(int w, int h, int r)
        {
            var m = new bool[w, h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) m[x, y] = true;
            r = Mathf.Min(r, w / 2, h / 2);
            float t = (r - 0.25f) * (r - 0.25f);
            for (int cy = 0; cy < r; cy++)
                for (int cx = 0; cx < r; cx++)
                {
                    float dx = r - cx - 0.5f, dy = r - cy - 0.5f;
                    if (dx * dx + dy * dy <= t) continue;
                    m[cx, cy] = m[w - 1 - cx, cy] = m[cx, h - 1 - cy] = m[w - 1 - cx, h - 1 - cy] = false;
                }
            return m;
        }

        static bool[,] CircleMask(int d)
        {
            var m = new bool[d, d];
            float c = (d - 1) / 2f, t = (d / 2f - 0.1f) * (d / 2f - 0.1f);
            for (int y = 0; y < d; y++) for (int x = 0; x < d; x++) m[x, y] = (x - c) * (x - c) + (y - c) * (y - c) <= t;
            return m;
        }

        /// <summary>창·카드 테: 바깥 진한 선 2 + 주황 테(위 밝게·아래 어둡게) + 안쪽 진한 선 1 + 종이</summary>
        static Color32 FrameAt(int k, int y, int h, int r, int band)
        {
            if (k < 0) return Clear;
            if (k < 2) return Ink;
            k -= 2;
            if (k < band)
            {
                if (k == 0 && y < r) return FrameHi;
                if (k == band - 1 && y >= h - r) return FrameLo;
                return Frame;
            }
            return k == band ? Ink : Paper;
        }

        static Texture2D RoundTex(int r, Color32 fill, Color32 line, int lw)
        {
            string key = $"round{r}:{Key(fill)}:{Key(line)}:{lw}";
            if (tex.TryGetValue(key, out var t) && t != null) return t;
            int s = 2 * r + 2;
            var d = Rings(RoundMask(s, s, r));
            t = Paint(s, s, (x, y) => d[x, y] < 0 ? Clear : d[x, y] < lw ? line : fill);
            return tex[key] = t;
        }

        static Texture2D FramedTex(int r, int band)
        {
            string key = $"framed{r}:{band}";
            if (tex.TryGetValue(key, out var t) && t != null) return t;
            int s = 2 * r + 2;
            var d = Rings(RoundMask(s, s, r));
            t = Paint(s, s, (x, y) => FrameAt(d[x, y], y, s, r, band));
            return tex[key] = t;
        }

        static Texture2D CircleTex(int dm, Color32 fill, Color32 line, int lw)
        {
            string key = $"circle{dm}:{Key(fill)}:{Key(line)}:{lw}";
            if (tex.TryGetValue(key, out var t) && t != null) return t;
            var d = Rings(CircleMask(dm));
            t = Paint(dm, dm, (x, y) => d[x, y] < 0 ? Clear : d[x, y] < lw ? line : fill);
            return tex[key] = t;
        }

        static Texture2D FramedCircleTex(int dm)
        {
            string key = $"framedcircle{dm}";
            if (tex.TryGetValue(key, out var t) && t != null) return t;
            var d = Rings(CircleMask(dm));
            t = Paint(dm, dm, (x, y) => FrameAt(d[x, y], y, dm, dm / 2, 2));
            return tex[key] = t;
        }

        static Texture2D SolidTex(Color32 c)
        {
            string key = "solid" + Key(c);
            if (tex.TryGetValue(key, out var t) && t != null) return t;
            return tex[key] = Paint(1, 1, (x, y) => c);
        }

        /// <summary>종이 결 (17칸마다 점 하나, 겹쳐 깔아서 반복)</summary>
        static Texture2D SpeckleTex()
        {
            if (tex.TryGetValue("speckle", out var t) && t != null) return t;
            return tex["speckle"] = Paint(17, 17, (x, y) => (x * 3 + y * 5) % 17 == 0 ? Paper2 : Clear, TextureWrapMode.Repeat);
        }

        /// <summary>제목 리본 꼬리 (V자 홈)</summary>
        static Texture2D TailTex(bool left)
        {
            string key = left ? "tailL" : "tailR";
            if (tex.TryGetValue(key, out var t) && t != null) return t;
            const int w = 16, h = 14;
            var m = new bool[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    m[x, y] = left ? x > 3 - Mathf.Abs(y - 7) / 2 : x < 12 + Mathf.Abs(y - 7) / 2;
            var d = Rings(m);
            return tex[key] = Paint(w, h, (x, y) => d[x, y] < 0 ? Clear : d[x, y] < 2 ? Ink : FrameLo);
        }

        static GUIStyle Slice(string key, Texture2D t, int border)
        {
            if (slices.TryGetValue(key, out var s) && s != null && s.normal.background != null) return s;
            s = new GUIStyle { border = new RectOffset(border, border, border, border) };
            s.normal.background = t;
#if UNITY_EDITOR
            s.normal.scaledBackgrounds = new Texture2D[0];   // 레티나용 기본 배경이 우선하지 않게
#endif
            return slices[key] = s;
        }

        static Rect Snap(Rect r) => new Rect(Mathf.Round(r.x), Mathf.Round(r.y), Mathf.Round(r.width), Mathf.Round(r.height));

        // ---------------- 그림 (픽셀 아이콘) ----------------
        public enum Pic { Coin, Coin7, Basket, Bolt, Scroll, Compass, OpenBook, People, Sack, Chart, Arrow, Pin, Bulb, XMark, Star4 }

        static readonly Dictionary<Pic, (string[] rows, Dictionary<char, Color32> pal)> pics = new Dictionary<Pic, (string[], Dictionary<char, Color32>)>
        {
            { Pic.Coin, (new[] { "..#####..", ".#hhyyy#.", "#hyyyyyd#", "#hy###yd#", "#yy#.#yd#", "#yy###dd#", "#yyyyydd#", ".#yyddd#.", "..#####.." }, CoinPal()) },
            { Pic.Coin7, (new[] { "..###..", ".#hyy#.", "#hy#yd#", "#y#.#d#", "#yd#dd#", ".#ddd#.", "..###.." }, CoinPal()) },
            { Pic.Basket, (new[] { "..###..", ".#...#.", "#######", "#hbhbh#", "#bhbhb#", ".#####." },
                Pal(('#', 0x5A3416), ('h', 0xF6CF8A), ('b', 0xD39A4C))) },
            { Pic.Bolt, (new[] { ".....###.", "....#hy#.", "...#hy#..", "..#hy####", ".#hyyyyy#", ".####yy#.", "....#y#..", "...#y#...", "..#y#....", "..##....." },
                Pal(('#', 0x2A4A1A), ('y', 0xA6EC68), ('h', 0xE8FFD0))) },
            { Pic.Scroll, (new[] { "#########", "#hhhhhhh#", "#########", ".#ppppp#.", ".#p---p#.", ".#ppppp#.", ".#p---p#.", ".#ppppp#.", ".#p--pp#.", "#########", "#hhhhhhh#", "#########" },
                Pal(('#', 0x4A2A12), ('h', 0xD9A05B), ('p', 0xFFF4DC), ('-', 0xB08A5A))) },
            { Pic.Compass, (new[] { "...#####...", ".##ppppp##.", ".#pppRppp#.", "#ppppRpppp#", "#pppRRRppp#", "#pppDoDppp#", "#pppDDDppp#", "#ppppDpppp#", ".#pppDppp#.", ".##ppppp##.", "...#####..." },
                Pal(('#', 0x2A2A3A), ('p', 0xEEF6F4), ('R', 0xE04A3A), ('D', 0x34466A), ('o', 0xF7C843))) },
            { Pic.OpenBook, (new[] { ".##.......##.", "#pp##...##pp#", "#pppp#.#pppp#", "#p--pp#pp--p#", "#ppppp#ppppp#", "#p---p#p---p#", "#ppppp#ppppp#", "#ccccc#ccccc#", ".#####.#####." },
                Pal(('#', 0x3A2016), ('p', 0xFFFAF0), ('-', 0xB09A80), ('c', 0xA8503A))) },
            { Pic.People, (new[] { "..##...##..", ".#hh#.#hh#.", ".#hh#.#hh#.", "..##...##..", ".####.####.", "#hhhh#hhhh#", "#hhhh#hhhh#" },
                Pal(('#', 0x2A1A2E), ('h', 0x6A5464))) },
            { Pic.Sack, (new[] { "..#####..", "...#t#...", "..#bbb#..", ".#bhbbb#.", "#bhbbbbb#", "#bbbbbbd#", "#bbbbbdd#", ".#######." },
                Pal(('#', 0x5A3216), ('t', 0x8A5A2B), ('b', 0xE8B06C), ('h', 0xFBDCA8), ('d', 0xC08445))) },
            { Pic.Chart, (new[] { "......##.", "......##.", "...##.##.", "...##.##.", "##.##.##.", "##.##.##.", "#########" }, Pal(('#', 0x2A1A2E))) },
            { Pic.Arrow, (new[] { "....#....", "....##...", "#######..", "########.", "#######..", "....##...", "....#...." }, Pal(('#', 0x2A1A2E))) },
            { Pic.Pin, (new[] { ".###.", "#rrr#", "#rwr#", "#rrr#", ".#r#.", "..#.." }, Pal(('#', 0x2A1A2E), ('r', 0xFF3D5F), ('w', 0xFFFFFF))) },
            { Pic.Bulb, (new[] { "..###..", ".#yyy#.", "#yywyy#", "#yyyyy#", ".#yyy#.", "..#y#..", "..###..", "..#g#..", "..###.." },
                Pal(('#', 0x2A1A2E), ('y', 0xFFCF33), ('w', 0xFFFFFF), ('g', 0x6A5464))) },
            { Pic.XMark, (new[] { "##...##", "###.###", ".#####.", "..###..", ".#####.", "###.###", "##...##" }, Pal(('#', 0x2A1A2E))) },
            { Pic.Star4, (new[] { "...y...", "...y...", "..yWy..", "yyWWWyy", "..yWy..", "...y...", "...y..." }, Pal(('y', 0xFFCF33), ('W', 0xFFFFFF))) },
        };

        static Dictionary<char, Color32> CoinPal() => Pal(('#', 0x6B3F10), ('h', 0xFFF3B0), ('y', 0xF7C843), ('d', 0xD08F22));
        static Dictionary<char, Color32> Pal(params (char c, int hex)[] list)
        {
            var d = new Dictionary<char, Color32>();
            foreach (var (c, hex) in list) d[c] = Hex(hex);
            return d;
        }

        public static Vector2 PicSize(Pic p) => new Vector2(pics[p].rows[0].Length, pics[p].rows.Length);

        /// <summary>픽셀 아이콘. mono를 주면 모든 칸을 그 색 하나로 (차트·화살표·X)</summary>
        public static void Icon(Pic p, float x, float y, int scale = 1, Color32? mono = null)
        {
            if (!Repaint) return;
            string key = "pic" + p + (mono.HasValue ? Key(mono.Value) : "");
            if (!tex.TryGetValue(key, out var t) || t == null)
            {
                var (rows, pal) = pics[p];
                t = tex[key] = Paint(rows[0].Length, rows.Length, (x2, y2) =>
                {
                    char ch = rows[y2][x2];
                    if (!pal.TryGetValue(ch, out var c)) return Clear;
                    return mono ?? c;
                });
            }
            GUI.DrawTexture(new Rect(Mathf.Round(x), Mathf.Round(y), t.width * scale, t.height * scale), t);
        }

        // ---------------- 글씨 ----------------
        static GUIStyle body, small, wrapBody, wrapSmall, wrapCenter;
        static Font styleFont;
        static int styleSize;
        static readonly Regex Tags = new Regex("<[^>]+>");

        static GUIStyle Make(Font f, int size, bool wrap) => new GUIStyle
        {
            font = f, fontSize = size, alignment = TextAnchor.MiddleLeft, wordWrap = wrap, richText = false,
            clipping = TextClipping.Overflow, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0),
        };

        static GUIStyle St(bool isSmall, bool wrap = false)
        {
            if (body == null || styleFont != UIStyle.RegularFont || styleSize != UIStyle.BodySize)
            {
                styleFont = UIStyle.RegularFont; styleSize = UIStyle.BodySize;
                body = Make(UIStyle.RegularFont, UIStyle.BodySize, false);
                small = Make(UIStyle.SmallFont, UIStyle.SmallSize, false);
                wrapBody = Make(UIStyle.RegularFont, UIStyle.BodySize, true);
                wrapSmall = Make(UIStyle.SmallFont, UIStyle.SmallSize, true);
                wrapBody.alignment = wrapSmall.alignment = TextAnchor.UpperLeft;
                wrapCenter = Make(UIStyle.RegularFont, UIStyle.BodySize, true);
                wrapCenter.alignment = TextAnchor.UpperCenter;
            }
            return wrap ? (isSmall ? wrapSmall : wrapBody) : (isSmall ? small : body);
        }

        /// <summary>색 태그를 뺀 글씨 (어두운 바탕용 색 태그가 종이 위에서 안 읽혀서)</summary>
        public static string Plain(string s) => string.IsNullOrEmpty(s) ? "" : Tags.Replace(s, "");

        public static float Width(string s, bool isSmall = false, bool bold = false) =>
            St(isSmall).CalcSize(new GUIContent(s)).x + (bold ? 1 : 0);

        /// <summary>한 줄 글씨 (cy = 가운데 높이). 오른쪽 끝 x를 돌려준다</summary>
        public static float Label(float x, float cy, string s, Color32 col, bool isSmall = false, bool bold = false)
        {
            var st = St(isSmall);
            st.normal.textColor = col;
            var size = st.CalcSize(new GUIContent(s));
            var r = new Rect(Mathf.Round(x), Mathf.Round(cy - size.y / 2f), size.x + 2, size.y);
            GUI.Label(r, s, st);
            if (bold) GUI.Label(new Rect(r.x + 1, r.y, r.width, r.height), s, st);
            return x + size.x + (bold ? 1 : 0);
        }

        public static float LabelRight(float xr, float cy, string s, Color32 col, bool isSmall = false, bool bold = false) =>
            Label(xr - Width(s, isSmall, bold), cy, s, col, isSmall, bold);

        public static float LabelCenter(float cx, float cy, string s, Color32 col, bool isSmall = false, bool bold = false) =>
            Label(cx - Width(s, isSmall, bold) / 2f, cy, s, col, isSmall, bold);

        public static float WrapHeight(string s, float w, bool isSmall = false) => St(isSmall, true).CalcHeight(new GUIContent(s), w);

        /// <summary>가운데 맞춤 여러 줄 (알림). 높이를 돌려준다</summary>
        public static float WrapCenter(float x, float y, float w, string s, Color32 col)
        {
            St(false);
            wrapCenter.normal.textColor = col;
            float h = wrapCenter.CalcHeight(new GUIContent(s), w);
            GUI.Label(new Rect(Mathf.Round(x), Mathf.Round(y), w, h), s, wrapCenter);
            return h;
        }

        /// <summary>여러 줄 글씨. 높이를 돌려준다</summary>
        public static float Wrap(float x, float y, float w, string s, Color32 col, bool isSmall = false, bool bold = false)
        {
            var st = St(isSmall, true);
            st.normal.textColor = col;
            float h = st.CalcHeight(new GUIContent(s), w);
            var r = new Rect(Mathf.Round(x), Mathf.Round(y), w, h);
            GUI.Label(r, s, st);
            if (bold) GUI.Label(new Rect(r.x + 1, r.y, w, h), s, st);
            return h;
        }

        // ---------------- 판·모양 ----------------
        public static void Fill(Rect r, Color32 c) { if (Repaint) GUI.DrawTexture(Snap(r), SolidTex(c)); }

        /// <summary>창 뒤 화면을 어둡게</summary>
        public static void Dim() => Fill(new Rect(0, 0, UIStyle.VirtualWidth, UIStyle.VirtualHeight), DimColor);

        /// <summary>둥근 상자. lw = 0이면 테두리 없음, shadow = 아래 그림자 두께</summary>
        public static void Box(Rect r, int radius, Color32 fill, Color32 line, int lw = 1, int shadow = 0, Color32? shadowColor = null)
        {
            if (!Repaint) return;
            r = Snap(r);
            radius = Mathf.Max(1, Mathf.Min(radius, (int)r.height / 2 - 1, (int)r.width / 2 - 1));
            if (shadow > 0)
            {
                var sc = shadowColor ?? Shadow;
                var st = RoundTex(radius, sc, sc, 0);
                Slice("round" + radius + Key(sc), st, radius).Draw(new Rect(r.x, r.y + shadow, r.width, r.height), false, false, false, false);
            }
            var t = RoundTex(radius, fill, line, lw);
            Slice($"round{radius}:{Key(fill)}:{Key(line)}:{lw}", t, radius).Draw(r, false, false, false, false);
        }

        static void Framed(Rect r, int radius, int band, int shadow)
        {
            if (!Repaint) return;
            r = Snap(r);
            Box(new Rect(r.x, r.y + shadow, r.width, r.height), radius, Shadow, Shadow, 0);
            Slice($"framed{radius}:{band}", FramedTex(radius, band), radius).Draw(r, false, false, false, false);
            int inset = 2 + band + 1;
            var inner = new Rect(r.x + inset, r.y + inset, r.width - 2 * inset, r.height - 2 * inset);
            GUI.DrawTextureWithTexCoords(inner, SpeckleTex(), new Rect(0, 0, inner.width / 17f, inner.height / 17f));
        }

        /// <summary>큰 창: 진한 2px 외곽선 + 주황 테 3px + 종이, 아래 그림자</summary>
        public static void Window(Rect r) => Framed(r, 9, 3, 4);

        /// <summary>작은 카드 (상태 줄 등): 테 2px</summary>
        public static void Card(Rect r) => Framed(r, 8, 2, 2);

        /// <summary>종이색 알약 (위쪽 할 일 줄·온라인 표시)</summary>
        public static void Pill(Rect r) => Box(r, (int)r.height / 2 - 1, Paper, Ink, 1, 2, SoftShadow);

        public static void Circle(float x, float y, int d, Color32 fill, Color32 line, int lw = 1, int shadow = 0)
        {
            if (!Repaint) return;
            x = Mathf.Round(x); y = Mathf.Round(y);
            if (shadow > 0) GUI.DrawTexture(new Rect(x, y + shadow, d, d), CircleTex(d, Shadow, Shadow, 0));
            GUI.DrawTexture(new Rect(x, y, d, d), CircleTex(d, fill, line, lw));
        }

        /// <summary>테 두른 동그라미 (초상·단추)</summary>
        public static void FramedCircle(float x, float y, int d, int shadow = 2)
        {
            if (!Repaint) return;
            x = Mathf.Round(x); y = Mathf.Round(y);
            if (shadow > 0) GUI.DrawTexture(new Rect(x, y + shadow, d, d), CircleTex(d, Shadow, Shadow, 0));
            GUI.DrawTexture(new Rect(x, y, d, d), FramedCircleTex(d));
        }

        /// <summary>알림 동그라미 안에 흰 "!"</summary>
        public static void Medallion(float x, float y, int d, Color32 fill)
        {
            Circle(x, y, d, fill, Ink, 1, 2);
            float cx = Mathf.Round(x + d / 2f - 1);
            Fill(new Rect(cx, y + 5, 2, d - 12), White);
            Fill(new Rect(cx, y + d - 6, 2, 2), White);
        }

        /// <summary>스프라이트 일부를 그린다 (uv는 스프라이트 안 0~1). 뒤집기 지원</summary>
        public static void Sprite(Sprite s, Rect dst, Rect part, bool flipX = false)
        {
            if (!Repaint || s == null || s.texture == null) return;
            var t = s.texture; var tr = s.textureRect;
            var uv = new Rect((tr.x + tr.width * part.x) / t.width, (tr.y + tr.height * part.y) / t.height, tr.width * part.width / t.width, tr.height * part.height / t.height);
            if (flipX) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
            GUI.DrawTextureWithTexCoords(Snap(dst), t, uv);
        }

        public static void Sprite(Sprite s, Rect dst) => Sprite(s, dst, new Rect(0, 0, 1, 1));

        /// <summary>플레이어 얼굴 동그라미 (지금 모습의 위쪽)</summary>
        public static void Portrait(float x, float y, int d)
        {
            FramedCircle(x, y, d);
            var pc = PlayerController.Instance;
            var sr = pc != null && pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            if (sr == null || sr.sprite == null) { Icon(Pic.Coin, x + d / 2f - 4, y + d / 2f - 4); return; }
            var tr = sr.sprite.rect;
            float showW = Mathf.Min(tr.width, d - 8), showH = Mathf.Min(tr.height * 0.6f, d - 8);
            var part = new Rect((1f - showW / tr.width) / 2f, 1f - showH / tr.height, showW / tr.width, showH / tr.height);
            Sprite(sr.sprite, new Rect(x + (d - showW) / 2f, y + 4, showW, showH), part, sr.flipX);
        }

        // ---------------- 조각 ----------------
        /// <summary>중립색 키캡 (단축키 표시). soft = 종이 위에 놓을 때 옅은 테. 너비를 돌려준다</summary>
        public static float Keycap(float x, float y, string label, bool soft = false)
        {
            float w = Mathf.Round(Width(label, true) + 8);
            Box(new Rect(x, y, w, 13), 3, soft ? White : KeyFace, soft ? KeyLine : Ink);
            Fill(new Rect(x + 2, y + 11, w - 4, 1), soft ? Paper2 : KeyLow);
            Label(x + 4, y + 6.5f, label, Ink, true);
            return w;
        }

        public static float KeycapWidth(string label) => Mathf.Round(Width(label, true) + 8);

        /// <summary>막대 (체력·짐). 채운 만큼 색, 나머지 바탕</summary>
        public static void Bar(Rect r, float v, Color32 fill, Color32 hi)
        {
            Box(r, 3, Track, Ink);
            float w = Mathf.Round((r.width - 2) * Mathf.Clamp01(v));
            if (w <= 0) return;
            Fill(new Rect(r.x + 1, r.y + 1, w, r.height - 2), fill);
            Fill(new Rect(r.x + 1, r.y + 1, w, 1), hi);
        }

        public static string GainText(float g) => g > 0.05f ? "+" + g.ToString("0.#") : g < -0.05f ? "-" + (-g).ToString("0.#") : "0";

        /// <summary>차익 배지: 이득 초록·0 회색·손해 빨강. 왼쪽 끝 x를 돌려준다</summary>
        public static float GainBadge(float xr, float cy, float gain, bool big = false)
        {
            string s = GainText(gain);
            float w = Mathf.Round(Width(s, !big, true) + (big ? 14 : 10)), h = big ? 17 : 13;
            bool zero = s == "0";
            Box(new Rect(xr - w, cy - h / 2f, w, h), (int)h / 2 - 1, gain > 0 && !zero ? Up : zero ? ZeroSoft : Down, Ink);
            LabelCenter(xr - w / 2f, cy, s, zero ? Zero : White, !big, true);
            return xr - w;
        }

        /// <summary>모르는 값 배지 "?"</summary>
        public static float UnknownBadge(float xr, float cy)
        {
            float w = 18, h = 13;
            Box(new Rect(xr - w, cy - h / 2f, w, h), 5, ZeroSoft, Line);
            LabelCenter(xr - w / 2f, cy, "?", Zero, true, true);
            return xr - w;
        }

        /// <summary>살 곳·팔 곳 칩: [바구니/엽전 장터 값]. 오른쪽 끝 x를 돌려준다</summary>
        public static float Chip(float x, float cy, bool buy, string town, int value)
        {
            var col = buy ? Buy : Sell;
            string v = value.ToString();
            float w = Mathf.Round(6 + 7 + 4 + Width(town, true) + 5 + Width(v, false, true) + 7);
            Box(new Rect(x, cy - 9, w, 18), 6, buy ? BuySoft : SellSoft, col);
            Icon(buy ? Pic.Basket : Pic.Coin7, x + 6, cy - (buy ? 3 : 4));
            float tx = Label(x + 17, cy, town, Text, true);
            Label(tx + 5, cy, v, col, false, true);
            return x + w;
        }

        public static float ChipWidth(string town, int value) => Mathf.Round(6 + 7 + 4 + Width(town, true) + 5 + Width(value.ToString(), false, true) + 7);

        /// <summary>경로 요약: [살 곳 값] → [팔 곳 값]. 오른쪽 끝 x를 돌려준다</summary>
        public static float Route(float x, float cy, string buyTown, int buy, string sellTown, int sell)
        {
            float xx = Chip(x, cy, true, buyTown, buy);
            Icon(Pic.Arrow, xx + 3, cy - 3, 1, Sub);
            return Chip(xx + 15, cy, false, sellTown, sell);
        }

        public static float RouteWidth(string buyTown, int buy, string sellTown, int sell) => ChipWidth(buyTown, buy) + 15 + ChipWidth(sellTown, sell);

        /// <summary>상호작용 버튼 (보라). 눌리면 true</summary>
        public static bool Button(Rect r, string label, Pic? pic = null)
        {
            Box(r, 8, Interact, Ink, 1, 2, InteractLo);
            Fill(new Rect(r.x + 5, r.y + 2, r.width - 10, 1), InteractHi);
            float w = Width(label, false, true) + (pic.HasValue ? 14 : 0);
            float x = r.center.x - w / 2f;
            if (pic.HasValue) { Icon(pic.Value, x, r.center.y - 3, 1, White); x += 14; }
            Label(x, r.center.y, label, OnInteract, false, true);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>닫기 (중립색 동그라미 X). 눌리면 true</summary>
        public static bool CloseButton(float x, float y)
        {
            Circle(x, y, 22, Paper, Ink, 2, 2);
            Icon(Pic.XMark, x + 8, y + 8, 1, Ink);
            return GUI.Button(new Rect(x, y, 22, 22), GUIContent.none, GUIStyle.none);
        }

        /// <summary>탭 (고른 탭은 아래 내용 칸과 이어진다). contentTop = 내용 칸 위 끝. 눌리면 true</summary>
        public static bool Tab(float x, float contentTop, string label, Pic pic, bool on, out float width)
        {
            width = Mathf.Round(12 + PicSize(pic).x + 5 + Width(label, false, on) + 12);
            var r = on ? new Rect(x, contentTop - 21, width, 23) : new Rect(x, contentTop - 17, width, 17);
            Box(r, 6, on ? Paper : Paper2, on ? Ink : Line);
            if (on)
            {
                Fill(new Rect(r.x + 2, contentTop - 1, r.width - 4, 3), Paper);   // 아래 내용 칸과 잇기
                Fill(new Rect(r.x + 4, r.y + 2, r.width - 8, 2), Interact);
            }
            if (pic == Pic.Chart) Icon(pic, r.x + 11, r.y + (on ? 7 : 5), 1, on ? Interact : Sub);
            else Icon(pic, r.x + 11, r.y + (on ? 6 : 4));
            Label(r.x + 12 + PicSize(pic).x + 5, r.y + (on ? 12 : 9), label, on ? Text : Sub, false, on);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>창 제목 리본 (양쪽 꼬리 + 별)</summary>
        public static void TitlePlate(float cx, float y, string label)
        {
            float w = Mathf.Round(Width(label, false, true) + 44), h = 22, x = Mathf.Round(cx - w / 2f);
            if (Repaint)
            {
                GUI.DrawTexture(new Rect(x - 11, y + 9, 16, 14), TailTex(true));
                GUI.DrawTexture(new Rect(x + w - 5, y + 9, 16, 14), TailTex(false));
            }
            Box(new Rect(x, y, w, h), 6, Frame, Ink, 2, 3);
            Fill(new Rect(x + 4, y + 3, w - 8, 1), FrameHi);
            Icon(Pic.Star4, x + 9, y + 7);
            Icon(Pic.Star4, x + w - 16, y + 7);
            LabelCenter(cx, y + 12, label, Ink, false, true);
            LabelCenter(cx, y + 11, label, White, false, true);
        }

        /// <summary>"[Space] 말 걸기" 같은 상호작용 안내 (11/05 종이 알약 + 키캡, 색은 빼서 덜 튀게)</summary>
        public static void SpacePrompt(float cx, float y, string verb)
        {
            float kw = KeycapWidth("Space");
            float w = Mathf.Round(3 + kw + 5 + Width(verb, true) + 7), x = Mathf.Round(cx - w / 2f);
            Box(new Rect(x, y, w, 17), 7, Paper, Ink, 1, 2, SoftShadow);
            Keycap(x + 3, y + 2, "Space", true);
            Label(x + 3 + kw + 5, y + 8.5f, verb, Text, true);
        }

        /// <summary>보조 버튼 (종이색 + 진한 테). 눌리면 true</summary>
        public static bool SoftButton(Rect r, string label)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Box(r, 8, hover ? Paper2 : Paper, Ink, 1, 2, SoftShadow);
            LabelCenter(r.center.x, r.center.y, label, Text, false, false);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>네모 칸 + 글: 누르면 켜고 끈다. 새 값을 돌려준다</summary>
        public static bool Check(Rect r, bool on, string label)
        {
            var b = new Rect(r.x, Mathf.Round(r.y + (r.height - 11) / 2f), 11, 11);
            Box(b, 3, White, Ink);
            if (on) Fill(new Rect(b.x + 3, b.y + 3, 5, 5), Interact);
            Label(b.xMax + 5, r.center.y, label, Text, true);
            return GUI.Button(r, GUIContent.none, GUIStyle.none) ? !on : on;
        }

        // ---------------- 위 가운데 줄 (할 일·다음 이야기) ----------------
        /// <summary>
        /// 위 가운데 알약: 왼쪽 동그라미(알림 "!" 또는 그림) + 굵은 할 일 + 덧붙임(pin이면 핀과 장소).
        /// 너무 길면 덧붙임을 빼고, 그래도 길면 할 일을 줄인다.
        /// </summary>
        public static void TopPill(float y, bool alert, Pic? pic, string main, string where, bool pin = true)
        {
            float maxW = Mathf.Max(200f, UIStyle.VirtualWidth - 330f);   // 왼쪽 위 카드·오른쪽 위 단추와 겹치지 않게
            float whereW = string.IsNullOrEmpty(where) ? 0 : 8 + (pin ? 7 : 0) + Width(where, true);
            float mainW = Width(main, false, true);
            if (22 + mainW + whereW + 10 > maxW) whereW = 0;
            while (22 + mainW + 10 > maxW && main.Length > 4) { main = main.Substring(0, main.Length - 2) + "…"; mainW = Width(main, false, true); }
            float w = Mathf.Round(22 + mainW + whereW + 10), x = Mathf.Round((UIStyle.VirtualWidth - w) / 2f + 8);
            Pill(new Rect(x, y, w, 20));
            if (alert) Medallion(x - 9, y - 2, 24, Alert);
            else
            {
                FramedCircle(x - 9, y - 2, 24);
                if (pic.HasValue) { var s = PicSize(pic.Value); Icon(pic.Value, x - 9 + (24 - s.x) / 2f, y - 2 + (24 - s.y) / 2f); }
            }
            float tx = Label(x + 21, y + 10, main, Text, false, true);
            if (whereW > 0)
            {
                if (pin) Icon(Pic.Pin, tx + 8, y + 5, 1, Sub);
                Label(tx + (pin ? 15 : 8), y + 10.5f, where, Sub, true);
            }
        }


        /// <summary>받침 "을/를" (마지막 글자에 받침이 있으면 을)</summary>
        public static string Eul(string word)
        {
            if (string.IsNullOrEmpty(word)) return "를";
            char c = word[word.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return "을(를)";
            return (c - 0xAC00) % 28 != 0 ? "을" : "를";
        }
    }
}
