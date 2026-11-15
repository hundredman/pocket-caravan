using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/16 은샘: 짐 싣기 퍼즐. 큰 짐(무게 5 이상)이나 신용 의뢰를 게시판에서 맡을 때 뜬다 (속죄 의뢰는 빼고).
    /// 수레 앞칸에서 의뢰 짐 조각(보자기)을 빈 칸에 맞춰 싣는다. 다 실으면 끈으로 묶고 보수 +10% (최소 1냥).
    /// "그냥 싣기"는 언제든 되고 보수만 그대로. Esc는 의뢰를 맡지 않고 닫는다.
    /// 난이도는 실은 짐 양과 상관없이 의뢰 크기로만 정한다.
    ///   무게 5~7: 조각 2~3개(2~3칸), 남는 칸 2, 판 5×3 · 무게 8 이상: 조각 3~4개(2~4칸, 4칸 하나 이상), 남는 칸 1, 판 7×4.
    /// 정답 배치를 먼저 만들고 잘라 내므로 언제나 풀 수 있다. 시간 제한 없음. 놓은 조각은 다시 들 수 있다.
    /// 조작: 방향키 옮기기 · R 돌리기 · Space 놓기 · Tab 다음 조각 · Z 마지막 조각 다시 들기. 마우스: 칸 위로 옮기고 왼쪽 클릭 놓기, 오른쪽 클릭 돌리기, 놓은 조각 클릭하면 다시 들기.
    /// 게시판 창 위에 그리는 창이라 UIState는 게시판이 그대로 쥔다 (HaggleDuel과 같은 방식).
    /// </summary>
    public class PackPuzzle : MonoBehaviour
    {
        public const float BonusRate = 0.1f;
        static PackPuzzle inst;
        public static bool IsOpen => inst != null && inst.open;
        /// <summary>창을 닫은 프레임 (게시판이 같은 Esc·Space를 또 받지 않게)</summary>
        public static int ClosedFrame = -1;

        /// <summary>맡지 않고 닫는다 (게시판이 닫히거나 장면이 바뀔 때)</summary>
        public static void Cancel() { if (inst != null) inst.Close(false, false); }

        /// <summary>이 의뢰를 맡을 때 퍼즐을 하는지</summary>
        public static bool Wants(Contract c) => c != null && !c.atonement && (c.weight >= 5f || c.Premium);
        public static int BonusFor(Contract c) => c == null ? 0 : Mathf.Max(1, Mathf.RoundToInt(c.reward * BonusRate));

        class Piece { public List<Vector2Int> cells = new List<Vector2Int>(); public bool placed; public Vector2Int at; public int look; }

        bool open; int openedFrame; float openedAt;
        Contract contract; string fromName, toName; int bonus;
        Action<bool> done;
        int cols, rows;
        bool[,] region;                       // 의뢰 짐을 실을 수 있는 칸 (나머지는 이미 실은 짐)
        readonly List<Piece> pieces = new List<Piece>();
        readonly List<(List<Vector2Int> cells, Sprite icon)> cargo = new List<(List<Vector2Int>, Sprite)>();
        int sel = -1; Vector2Int at; Vector2 lastMouse = new Vector2(-1, -1); int resetVersion;
        readonly List<int> placeOrder = new List<int>();
        float tiedAt = -1f, bumpAt = -9f, rotAt = -9f; int lastPlaced = -1; float lastPlacedAt = -9f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<PackPuzzle>() != null) return;
            var go = new GameObject("PackPuzzle"); DontDestroyOnLoad(go); inst = go.AddComponent<PackPuzzle>();
        }
        void Awake() => inst = this;

        /// <summary>퍼즐을 연다. done(다 실었는지). Esc로 닫으면 done을 부르지 않는다 (의뢰를 맡지 않음)</summary>
        public static bool Open(Contract c, string from, string to, Action<bool> done)
        {
            if (inst == null || inst.open || c == null) return false;
            return inst.Begin(c, from, to, done);
        }

        bool Begin(Contract c, string from, string to, Action<bool> cb)
        {
            contract = c; fromName = from; toName = to; done = cb; bonus = BonusFor(c);
            int w = Mathf.Clamp(Mathf.RoundToInt(c.weight), 2, 12);
            bool big = w >= 8;
            cols = big ? 7 : 5; rows = big ? 4 : 3;
            var gm = GameManager.I;
            int seed = (int)(Hash($"{c.id}|{c.cargo}|{(gm != null ? gm.Clock.day : 0)}") & 0x7fffffff);
            if (!Generate(Sizes(w, big), big ? 1 : 2, new System.Random(seed))) return false;
            BuildCargo(new System.Random(seed ^ 0x5bd1e995));
            placeOrder.Clear(); tiedAt = -1f; bumpAt = rotAt = lastPlacedAt = -9f; lastPlaced = -1;
            sel = 0; at = DefaultAt(pieces[0]);
            open = true; openedFrame = Time.frameCount; openedAt = Time.unscaledTime;
            resetVersion = UIState.ResetVersion; lastMouse = new Vector2(-1, -1);
            Sfx.Play("open");
            return true;
        }

        /// <summary>조각 크기: 작은 의뢰 2~3칸, 큰 의뢰 2~4칸 (4칸 하나 이상)</summary>
        static List<int> Sizes(int w, bool big)
        {
            int max = big ? 4 : 3;
            int n = big ? (w <= 9 ? 3 : 4) : (w <= 6 ? 2 : 3);
            while (Mathf.CeilToInt(w / (float)n) > max) n++;
            var s = new List<int>();
            for (int i = 0; i < n; i++) s.Add(w / n + (i < w % n ? 1 : 0));
            if (big && !s.Contains(4) && s.Count >= 2)
            {
                // 4칸 하나를 만든다 (가장 작은 조각에서 덜어 온다)
                s.Sort();
                int need = 4 - s[s.Count - 1];
                for (int i = 0; i < s.Count - 1 && need > 0; i++) { int give = Mathf.Min(need, s[i] - 2); s[i] -= give; need -= give; }
                if (need == 0) s[s.Count - 1] = 4;
            }
            return s;
        }

        static readonly Vector2Int[] D4 = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        bool Generate(List<int> sizes, int slack, System.Random rng)
        {
            for (int attempt = 0; attempt < 400; attempt++)
            {
                var used = new bool[cols, rows];
                pieces.Clear();
                bool fail = false;
                foreach (int k in sizes)
                {
                    Vector2Int s0;
                    if (pieces.Count == 0) s0 = new Vector2Int(rng.Next(cols), rng.Next(rows));
                    else { var fr = Frontier(used, null); if (fr.Count == 0) { fail = true; break; } s0 = fr[rng.Next(fr.Count)]; }
                    var p = new Piece(); p.cells.Add(s0); used[s0.x, s0.y] = true;
                    while (p.cells.Count < k)
                    {
                        var fr = Frontier(used, p.cells);
                        if (fr.Count == 0) { fail = true; break; }
                        var c = fr[rng.Next(fr.Count)]; p.cells.Add(c); used[c.x, c.y] = true;
                    }
                    if (fail) break;
                    pieces.Add(p);
                }
                if (fail) continue;
                for (int i = 0; i < slack; i++)
                {
                    var fr = Frontier(used, null);
                    if (fr.Count == 0) break;
                    var c = fr[rng.Next(fr.Count)]; used[c.x, c.y] = true;
                }
                region = used;
                // 조각을 정답 자리에서 떼어 내고, 모양을 0에 맞추고, 아무렇게나 돌리고 섞는다
                for (int i = 0; i < pieces.Count; i++)
                {
                    var p = pieces[i];
                    p.cells = Normalize(p.cells);
                    int r = rng.Next(4); for (int k = 0; k < r; k++) p.cells = Rotate(p.cells);
                    p.look = i % Cloth.Length;
                    p.placed = false;
                }
                for (int i = pieces.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = pieces[i]; pieces[i] = pieces[j]; pieces[j] = t; }
                for (int i = 0; i < pieces.Count; i++) pieces[i].look = i % Cloth.Length;
                return true;
            }
            return false;
        }

        List<Vector2Int> Frontier(bool[,] used, List<Vector2Int> from)
        {
            var list = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                {
                    if (!used[x, y]) continue;
                    var c = new Vector2Int(x, y);
                    if (from != null && !from.Contains(c)) continue;
                    foreach (var d in D4)
                    {
                        var n = c + d;
                        if (n.x < 0 || n.y < 0 || n.x >= cols || n.y >= rows || used[n.x, n.y] || !seen.Add(n)) continue;
                        list.Add(n);
                    }
                }
            return list;
        }

        static List<Vector2Int> Normalize(List<Vector2Int> cells)
        {
            int mx = int.MaxValue, my = int.MaxValue;
            foreach (var c in cells) { mx = Mathf.Min(mx, c.x); my = Mathf.Min(my, c.y); }
            var o = new List<Vector2Int>();
            foreach (var c in cells) o.Add(new Vector2Int(c.x - mx, c.y - my));
            return o;
        }
        static List<Vector2Int> Rotate(List<Vector2Int> cells)
        {
            var o = new List<Vector2Int>();
            foreach (var c in cells) o.Add(new Vector2Int(-c.y, c.x));
            return Normalize(o);
        }
        static Vector2Int Size(List<Vector2Int> cells)
        {
            int w = 0, h = 0;
            foreach (var c in cells) { w = Mathf.Max(w, c.x + 1); h = Mathf.Max(h, c.y + 1); }
            return new Vector2Int(w, h);
        }

        /// <summary>이미 실은 짐 칸을 1~3칸 묶음으로 나누고, 내 짐 그림(없으면 장터 물건)을 붙인다</summary>
        void BuildCargo(System.Random rng)
        {
            cargo.Clear();
            var icons = new List<Sprite>();
            var gm = GameManager.I;
            if (gm != null)
            {
                foreach (var kv in gm.Player.cargo.Items) if (kv.Key != null && kv.Key.icon != null && !icons.Contains(kv.Key.icon)) icons.Add(kv.Key.icon);
                if (icons.Count < 3)
                    foreach (var it in gm.Db.items)
                        if (it != null && it.icon != null && !it.questItem && !icons.Contains(it.icon)) { icons.Add(it.icon); if (icons.Count >= 6) break; }
            }
            var taken = new bool[cols, rows];
            int k = rng.Next(Mathf.Max(1, icons.Count));
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    if (region[x, y] || taken[x, y]) continue;
                    var cells = new List<Vector2Int> { new Vector2Int(x, y) }; taken[x, y] = true;
                    int len = 1 + rng.Next(3);
                    bool vertical = rng.Next(3) == 0;
                    for (int i = 1; i < len; i++)
                    {
                        int nx = vertical ? x : x + i, ny = vertical ? y + i : y;
                        if (nx >= cols || ny >= rows || region[nx, ny] || taken[nx, ny]) break;
                        cells.Add(new Vector2Int(nx, ny)); taken[nx, ny] = true;
                    }
                    cargo.Add((cells, icons.Count > 0 ? icons[k++ % icons.Count] : null));
                }
        }

        // ---------------- 조작 ----------------
        Vector2Int DefaultAt(Piece p)
        {
            var s = Size(p.cells);
            return new Vector2Int(Mathf.Clamp((cols - s.x) / 2, 0, cols - s.x), Mathf.Clamp((rows - s.y) / 2, 0, rows - s.y));
        }
        Vector2Int Clamp(Piece p, Vector2Int a)
        {
            var s = Size(p.cells);
            return new Vector2Int(Mathf.Clamp(a.x, 0, Mathf.Max(0, cols - s.x)), Mathf.Clamp(a.y, 0, Mathf.Max(0, rows - s.y)));
        }
        bool Occupied(Vector2Int cell, Piece except)
        {
            foreach (var p in pieces)
            {
                if (!p.placed || p == except) continue;
                foreach (var c in p.cells) if (c + p.at == cell) return true;
            }
            return false;
        }
        bool Fits(Piece p, Vector2Int a)
        {
            foreach (var c in p.cells)
            {
                var g = c + a;
                if (g.x < 0 || g.y < 0 || g.x >= cols || g.y >= rows || !region[g.x, g.y] || Occupied(g, p)) return false;
            }
            return true;
        }
        int NextUnplaced(int from)
        {
            for (int i = 1; i <= pieces.Count; i++) { int j = (from + i + pieces.Count) % pieces.Count; if (!pieces[j].placed) return j; }
            return -1;
        }
        void Select(int i, Vector2Int? where = null)
        {
            if (i < 0 || i >= pieces.Count || pieces[i].placed) return;
            sel = i; at = Clamp(pieces[i], where ?? DefaultAt(pieces[i]));
        }
        void TryPlace()
        {
            if (sel < 0) return;
            var p = pieces[sel];
            if (!Fits(p, at)) { bumpAt = Time.unscaledTime; Sfx.Play("bump", .7f); return; }
            p.placed = true; p.at = at; placeOrder.Add(sel); lastPlaced = sel; lastPlacedAt = Time.unscaledTime;
            Sfx.Play("drop");
            int n = NextUnplaced(sel);
            if (n < 0) { sel = -1; tiedAt = Time.unscaledTime; Sfx.Play("success"); Sfx.Play("coin", .7f); return; }
            Select(n, at);
        }
        void Lift(int i)
        {
            if (i < 0 || !pieces[i].placed || tiedAt >= 0f) return;
            pieces[i].placed = false; placeOrder.Remove(i);
            sel = i; at = pieces[i].at; Sfx.Play("move", .7f);
        }
        void RotateSel()
        {
            if (sel < 0) return;
            var p = pieces[sel];
            var before = Size(p.cells);
            p.cells = Rotate(p.cells);
            var after = Size(p.cells);
            at = Clamp(p, at + new Vector2Int((before.x - after.x) / 2, (before.y - after.y) / 2));
            rotAt = Time.unscaledTime; Sfx.Play("move", .6f);
        }

        void Close(bool apply, bool solved)
        {
            if (!open) return;
            open = false; ClosedFrame = Time.frameCount;
            var cb = done; done = null;
            if (apply) cb?.Invoke(solved);
        }

        void Update()
        {
            if (!open) return;
            if (UIState.ResetVersion != resetVersion || !UIState.Blocking) { Cancel(); return; }   // 장면 전환·창 초기화로 게시판이 사라졌으면 맡지 않고 닫는다
            if (Time.frameCount == openedFrame) return;
            if (tiedAt >= 0f) { if (Time.unscaledTime - tiedAt > 1.4f) Close(true, true); return; }
            if (InputReader.CancelPressed()) { Close(false, false); Sfx.Play("cancel"); return; }
            if (Time.unscaledTime - openedAt < 0.2f) return;
            var mv = InputReader.MovePressed();
            if (mv != Vector2Int.zero && sel >= 0) at = Clamp(pieces[sel], at + new Vector2Int(mv.x, -mv.y));
            if (KeyPressed('r')) RotateSel();
            if (KeyPressed('\t')) Select(NextUnplaced(sel < 0 ? -1 : sel), at);
            if (KeyPressed('z') && placeOrder.Count > 0) Lift(placeOrder[placeOrder.Count - 1]);
            if (InputReader.InteractPressed()) TryPlace();
        }

        static bool KeyPressed(char k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current; if (kb == null) return false;
            return k == 'r' ? kb.rKey.wasPressedThisFrame : k == 'z' ? kb.zKey.wasPressedThisFrame : k == '\t' && kb.tabKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(k == 'r' ? KeyCode.R : k == 'z' ? KeyCode.Z : KeyCode.Tab);
#endif
        }

        // ---------------- 그리기 ----------------
        const float W = 360f, H = 206f;
        struct ClothSet { public Color32 c, hi, lo; }
        static readonly ClothSet[] Cloth =
        {
            new ClothSet { c = Hex(0xC8524A), hi = Hex(0xE27A6E), lo = Hex(0x9A3A36) },
            new ClothSet { c = Hex(0x4F6FB5), hi = Hex(0x7D98D6), lo = Hex(0x3A5290) },
            new ClothSet { c = Hex(0x5F9A52), hi = Hex(0x86BF74), lo = Hex(0x44753B) },
            new ClothSet { c = Hex(0x9A5FA8), hi = Hex(0xBE8ACB), lo = Hex(0x74457F) },
        };
        static readonly Color32 Rope = Hex(0xF1E2B8), RopeLo = Hex(0xB8893E), RopeMid = Hex(0xE8C27A),
            Wood = Hex(0xB4793F), WoodHi = Hex(0xD69A5C), WoodLo = Hex(0x8E5A2C), WoodDk = Hex(0x6A4422),
            Floor = Hex(0xE0B27A), FloorHi = Hex(0xECC490), FloorLo = Hex(0xD3A26A), Slot = Hex(0xD9A868), SlotHi = Hex(0xE8BF84),
            Iron = Hex(0x5A5574), IronHi = Hex(0x8A86A4), Mat = Hex(0xEAD7A6), MatLine = Hex(0xB8893E),
            Gold = Hex(0xFFCF33), GoldLo = Hex(0xE0A83A), Green = Hex(0x3FB85A), Red = Hex(0xF0555A), Teal = Hex(0x2E8C86);
        static Color32 Hex(int v) => new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        static float Ease(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
        static float Back(float x) { x = Mathf.Clamp01(x); const float c = 2.2f; return 1f + (c + 1f) * Mathf.Pow(x - 1f, 3f) + c * Mathf.Pow(x - 1f, 2f); }
        static void FillA(Rect r, Color32 c, float a)
        {
            if (a <= 0f) return;
            var g = GUI.color; GUI.color = new Color(g.r, g.g, g.b, g.a * Mathf.Clamp01(a)); CuteUI.Fill(r, c); GUI.color = g;
        }

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -30;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, now = Time.unscaledTime;
            CuteUI.Fill(new Rect(0, 0, vw, vh), new Color32(30, 15, 35, 110));
            float appear = Mathf.Clamp01((now - openedAt) / .16f), ea = 1f - (1f - appear) * (1f - appear);
            var r = new Rect(Mathf.Round((vw - W) / 2f), Mathf.Round((vh - H) / 2f + (1f - ea) * 12f), W, H);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 6, "짐 싣기");
            bool tied = tiedAt >= 0f; float tt = tied ? now - tiedAt : 0f;

            // 판 자리
            float cs = Mathf.Min(20f, Mathf.Floor(140f / cols), Mathf.Floor(80f / rows));
            var g = new Vector2(Mathf.Round(r.x + 136 + (140 - cols * cs) / 2f), Mathf.Round(r.y + 40 + (80 - rows * cs) / 2f));
            var e = Event.current;

            DrawCard(new Rect(r.x + 12, r.y + 18, 104, 132), now, tied, tt);
            DrawCart(g, cs);
            foreach (var cg in cargo) DrawCargo(g, cs, cg.cells, cg.icon);

            // 돗자리 (놓을 조각)
            var mat = new Rect(r.x + 286, r.y + 30, 62, 122);
            CuteUI.Box(mat, 5, Mat, MatLine, 1);
            for (float y = mat.y + 4; y < mat.yMax - 3; y += 4) FillA(new Rect(mat.x + 3, y, mat.width - 6, 1), MatLine, .35f);
            var slots = TraySlots(mat);
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.placed || i == sel) continue;
                var s = Size(p.cells);
                var sr = slots[i];
                float ts = Mathf.Min(12f, Mathf.Floor((sr.width - 2) / s.x), Mathf.Floor((sr.height - 2) / s.y));   // 칸을 넘지 않게
                DrawPiece(p.cells, new Vector2(Mathf.Round(sr.center.x - s.x * ts / 2f), Mathf.Round(sr.center.y - s.y * ts / 2f)), ts, Cloth[p.look], 1f);
                if (e.type == EventType.MouseDown && e.button == 0 && sr.Contains(e.mousePosition) && !tied) { Select(i, at); e.Use(); }
            }
            if (sel >= 0 && !tied) Corners(slots[sel], Mathf.FloorToInt(now * 3f) % 2);

            // 놓은 조각
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (!p.placed) continue;
                var o = new Vector2(g.x + p.at.x * cs, g.y + p.at.y * cs);
                float land = i == lastPlaced ? now - lastPlacedAt : 9f;
                float sq = land < .16f ? Mathf.Sin(land / .16f * Mathf.PI) : 0f;
                DrawPiece(p.cells, o + new Vector2(0, Mathf.Round(sq * 2f)), cs, Cloth[p.look], 1f);
                if (land < .25f) foreach (var c in p.cells) FillA(new Rect(o.x + c.x * cs, o.y + c.y * cs, cs, cs), CuteUI.White, .6f * (1f - land / .25f));
                if (land < .4f)
                {
                    float k = land / .4f;
                    for (int q = 0; q < 8; q++)
                    {
                        float a = q / 8f * Mathf.PI * 2f; var c = p.cells[q % p.cells.Count]; float rad = 6 + k * 16;
                        FillA(new Rect(Mathf.Round(o.x + c.x * cs + cs / 2 + Mathf.Cos(a) * rad), Mathf.Round(o.y + c.y * cs + cs / 2 + Mathf.Sin(a) * rad * .6f), q % 3 == 0 ? 3 : 2, q % 3 == 0 ? 3 : 2), Hex(0xF5E6C8), 1f - k);
                    }
                }
            }

            // 마우스: 판 위에서 조각을 끌고 다님
            var board = new Rect(g.x, g.y, cols * cs, rows * cs);
            bool moved = e.type == EventType.Repaint && (e.mousePosition - lastMouse).sqrMagnitude > 0.01f;
            if (e.type == EventType.Repaint) lastMouse = e.mousePosition;
            if (!tied && (moved || e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && board.Contains(e.mousePosition))
            {
                var cell = new Vector2Int(Mathf.FloorToInt((e.mousePosition.x - g.x) / cs), Mathf.FloorToInt((e.mousePosition.y - g.y) / cs));
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    int hit = PlacedAt(cell);
                    if (hit >= 0) Lift(hit);   // 놓은 조각을 누르면 다시 든다 (들고 있던 조각은 돗자리로)
                    else if (sel >= 0) { at = Clamp(pieces[sel], cell - Size(pieces[sel].cells) / 2); TryPlace(); }
                    e.Use();
                }
                else if (e.type == EventType.MouseDown && e.button == 1) { RotateSel(); e.Use(); }
                else if (sel >= 0 && e.type != EventType.MouseDown) at = Clamp(pieces[sel], cell - Size(pieces[sel].cells) / 2);
            }

            // 든 조각: 놓일 자리(초록·빨강) + 살짝 떠 있는 조각
            if (sel >= 0 && !tied)
            {
                var p = pieces[sel];
                bool ok = Fits(p, at);
                float bump = now - bumpAt < .3f ? Mathf.Round(Mathf.Sin((now - bumpAt) * 60f) * 2f * (1f - (now - bumpAt) / .3f)) : 0f;
                var o = new Vector2(g.x + at.x * cs + bump, g.y + at.y * cs);
                float pulse = .55f + .25f * Mathf.Sin(now * 12f);
                foreach (var c in p.cells)
                {
                    var cr = new Rect(o.x + c.x * cs + 1, o.y + c.y * cs + 1, cs - 2, cs - 2);
                    FillA(cr, ok ? Green : Red, pulse * .55f);
                    if (!ok)
                        for (int d = -3; d <= 3; d++) { CuteUI.Fill(new Rect(cr.center.x + d, cr.center.y + d, 1, 1), CuteUI.White); CuteUI.Fill(new Rect(cr.center.x + d, cr.center.y - d, 1, 1), CuteUI.White); }
                }
                Outline(p.cells, o, cs, ok ? Green : Red, 2);
                float lift = 6 + Mathf.Round(Mathf.Sin(now * 10f));
                foreach (var c in p.cells) FillA(new Rect(o.x + c.x * cs + 3, o.y + c.y * cs + 3, cs, cs), CuteUI.Ink, .3f);
                DrawPiece(p.cells, new Vector2(o.x, o.y - lift + 4), cs, Cloth[p.look], 1f);
                if (now - rotAt < .15f) foreach (var c in p.cells) FillA(new Rect(o.x + c.x * cs, o.y - lift + 4 + c.y * cs, cs, cs), CuteUI.White, .6f);
            }

            // 다 실음: 끈 두 줄 + 매듭 + 반짝 + 글자
            if (tied) DrawTie(g, cs, tt, r);

            // 아래: 그냥 싣기 + 키 안내
            float ky = r.y + 172;
            var skip = new Rect(r.x + 12, ky - 3, 62, 18);
            CuteUI.Box(skip, 6, CuteUI.White, CuteUI.KeyLine, 1);
            CuteUI.LabelCenter(skip.center.x, skip.center.y, "그냥 싣기", CuteUI.Sub, true);
            if (!tied && e.type == EventType.MouseDown && e.button == 0 && skip.Contains(e.mousePosition)) { e.Use(); Close(true, false); Sfx.Play("accept"); return; }
            float kx = r.x + 116;
            kx += CuteUI.Keycap(kx, ky, "방향키") + 3; kx = CuteUI.Label(kx, ky + 6.5f, "옮기기", CuteUI.Sub, true) + 8;
            if (now - rotAt < .2f) CuteUI.Box(new Rect(kx - 2, ky - 2, CuteUI.KeycapWidth("R") + 4, 17), 4, Teal, Teal, 0);
            kx += CuteUI.Keycap(kx, ky, "R") + 3; kx = CuteUI.Label(kx, ky + 6.5f, "돌리기", CuteUI.Sub, true) + 8;
            kx += CuteUI.Keycap(kx, ky, "Space") + 3; CuteUI.Label(kx, ky + 6.5f, "놓기", CuteUI.Sub, true);

            if (e.isMouse || e.type == EventType.ScrollWheel) e.Use();   // 게시판이 클릭을 받지 않게
        }

        int PlacedAt(Vector2Int cell)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].placed) continue;
                foreach (var c in pieces[i].cells) if (c + pieces[i].at == cell) return i;
            }
            return -1;
        }

        Rect[] TraySlots(Rect mat)
        {
            var s = new Rect[pieces.Count];
            float h = (mat.height - 8) / Mathf.Max(1, pieces.Count);
            for (int i = 0; i < pieces.Count; i++) s[i] = new Rect(mat.x + 4, mat.y + 4 + i * h, mat.width - 8, h);
            return s;
        }

        void DrawCard(Rect L, float now, bool tied, float tt)
        {
            FillA(new Rect(L.x + 2, L.y + 3, L.width - 4, L.height), CuteUI.Ink, .18f);
            CuteUI.Box(L, 8, CuteUI.Paper2, CuteUI.Ink, 2);
            var tag = new Rect(L.x + 8, L.y + 7, 40, 13);
            CuteUI.Box(tag, 3, CuteUI.FrameLo, CuteUI.Ink, 1);
            CuteUI.LabelCenter(tag.center.x, tag.center.y, "의뢰", CuteUI.White, true);
            // 의뢰 짐 그림: 보자기 꾸러미
            float bob = Mathf.Round(Mathf.Sin(now * 3f));
            FillA(new Rect(L.center.x - 12, L.y + 50, 24, 4), CuteUI.Ink, .2f);
            var bundle = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
            DrawPiece(bundle, new Vector2(L.center.x - 12, L.y + 24 + bob), 12, Cloth[0], 1f);
            CuteUI.LabelCenter(L.center.x, L.y + 66, TradeLedgerUI.Fit(contract.cargo, L.width - 10, false, false), CuteUI.Text);
            CuteUI.LabelCenter(L.center.x, L.y + 80, TradeLedgerUI.Fit($"{fromName} ▸ {toName}", L.width - 10, true, false), CuteUI.Sub, true);
            CuteUI.Fill(new Rect(L.x + 8, L.y + 90, L.width - 16, 1), CuteUI.Line);
            // 실은 칸 (작은 네모)
            int total = 0, filled = 0;
            foreach (var p in pieces) { total += p.cells.Count; if (p.placed) filled += p.cells.Count; }
            float pw = Mathf.Min(8f, (L.width - 20) / total), px0 = L.center.x - pw * total / 2f;
            for (int i = 0; i < total; i++)
                CuteUI.Box(new Rect(Mathf.Round(px0 + i * pw), L.y + 96, pw - 1, 7), 1, i < filled ? Cloth[0].c : CuteUI.White, i < filled ? CuteUI.Ink : CuteUI.KeyLine, 1);
            // 보수
            float coinT = .75f, k = tied ? Mathf.Clamp01((tt - coinT - .35f) / .3f) : 0f;
            int pay = contract.reward + Mathf.RoundToInt(bonus * k);
            float py = L.y + 118;
            Coin(L.x + 18, py);
            float pop = tied && tt > coinT + .35f && tt < coinT + .7f ? -Mathf.Round(Mathf.Sin((tt - coinT - .35f) / .35f * Mathf.PI) * 3f) : 0f;
            CuteUI.Label(L.x + 27, py + pop, $"{pay}냥", k > 0f ? CuteUI.Up : CuteUI.Text);
            var chip = new Rect(L.xMax - 38, py - 6, 30, 13);
            if (k < 1f) CuteUI.Box(chip, 6, CuteUI.White, CuteUI.KeyLine, 1);
            CuteUI.LabelCenter(chip.center.x, chip.center.y, $"+{bonus}", CuteUI.Up, true);
            if (tied && tt > coinT && tt < coinT + .4f)
            {
                // 동전이 수레에서 카드로 날아온다
                float a = Ease((tt - coinT) / .4f);
                var from = new Vector2(L.xMax + 90, L.y + 70);
                Coin(Mathf.Lerp(from.x, L.x + 18, a), Mathf.Lerp(from.y, py, a) - Mathf.Sin(a * Mathf.PI) * 30f);
            }
        }

        void DrawCart(Vector2 g, float cs)
        {
            float bx = g.x - 10, by = g.y - 10, bw = cols * cs + 20, bh = rows * cs + 20;
            FillA(new Rect(bx + 6, by + bh + 10, bw - 12, 7), CuteUI.Ink, .22f);
            // 손잡이
            // 손잡이 (짧게: 큰 판에서도 왼쪽 의뢰 카드에 닿지 않게)
            CuteUI.Fill(new Rect(bx - 6, by + bh - 16, 10, 6), CuteUI.Ink); CuteUI.Fill(new Rect(bx - 5, by + bh - 15, 8, 3), WoodLo); CuteUI.Fill(new Rect(bx - 5, by + bh - 15, 8, 1), Hex(0xC98A4B));
            CuteUI.Fill(new Rect(bx - 8, by + bh - 18, 4, 10), CuteUI.Ink); CuteUI.Fill(new Rect(bx - 7, by + bh - 17, 2, 8), WoodDk);
            // 짐받이 테
            CuteUI.Box(new Rect(bx, by, bw, bh), 4, Wood, CuteUI.Ink, 2);
            CuteUI.Fill(new Rect(bx + 3, by + 3, bw - 6, 2), WoodHi);
            CuteUI.Fill(new Rect(bx + 3, by + bh - 6, bw - 6, 3), WoodLo);
            foreach (var c in new[] { new Vector2(bx + 3, by + 3), new Vector2(bx + bw - 9, by + 3), new Vector2(bx + 3, by + bh - 9), new Vector2(bx + bw - 9, by + bh - 9) })
            {
                CuteUI.Fill(new Rect(c.x, c.y, 6, 6), Iron); CuteUI.Fill(new Rect(c.x, c.y, 6, 1), IronHi); CuteUI.Fill(new Rect(c.x + 2, c.y + 2, 2, 2), CuteUI.Ink);
            }
            // 바닥 판자
            float gw = cols * cs, gh = rows * cs;
            CuteUI.Fill(new Rect(g.x, g.y, gw, gh), Floor);
            for (int j = 0; j < rows; j++)
            {
                CuteUI.Fill(new Rect(g.x, g.y + j * cs + cs - 3, gw, 3), FloorLo);
                CuteUI.Fill(new Rect(g.x, g.y + j * cs, gw, 1), FloorHi);
                FillA(new Rect(g.x + (j * 53) % Mathf.Max(1, gw - 30) + 8, g.y + j * cs + 8, 10, 1), WoodDk, .4f);
                FillA(new Rect(g.x + (j * 97 + 40) % Mathf.Max(1, gw - 30) + 8, g.y + j * cs + 12, 7, 1), WoodDk, .4f);
            }
            for (int i = 1; i < cols; i++) FillA(new Rect(g.x + i * cs, g.y, 1, gh), WoodDk, .28f);
            FillA(new Rect(g.x, g.y, gw, 2), CuteUI.Ink, .18f); FillA(new Rect(g.x, g.y, 2, gh), CuteUI.Ink, .18f);
            // 바퀴
            foreach (float wx in new[] { bx + 28, bx + bw - 28 })
            {
                float wy = by + bh + 2;
                CuteUI.Box(new Rect(wx - 12, wy - 12, 24, 24), 12, WoodDk, CuteUI.Ink, 2);
                CuteUI.Fill(new Rect(wx - 1, wy - 9, 2, 18), WoodLo); CuteUI.Fill(new Rect(wx - 9, wy - 1, 18, 2), WoodLo);
                CuteUI.Box(new Rect(wx - 4, wy - 4, 8, 8), 4, Hex(0xC98A4B), CuteUI.Ink, 1);
            }
        }

        void DrawCargo(Vector2 g, float cs, List<Vector2Int> cells, Sprite icon)
        {
            bool Has(int x, int y) { foreach (var c in cells) if (c.x == x && c.y == y) return true; return false; }
            foreach (var c in cells)
            {
                var r = new Rect(g.x + c.x * cs, g.y + c.y * cs, cs, cs);
                CuteUI.Fill(r, Slot);
                if (!Has(c.x, c.y - 1)) CuteUI.Fill(new Rect(r.x, r.y, cs, 1), SlotHi);
                if (!Has(c.x, c.y + 1)) FillA(new Rect(r.x, r.yMax - 2, cs, 2), WoodDk, .35f);
            }
            foreach (var c in cells)
            {
                var r = new Rect(g.x + c.x * cs, g.y + c.y * cs, cs, cs);
                if (!Has(c.x, c.y - 1)) CuteUI.Fill(new Rect(r.x, r.y, cs, 1), WoodDk);
                if (!Has(c.x, c.y + 1)) CuteUI.Fill(new Rect(r.x, r.yMax - 1, cs, 1), WoodDk);
                if (!Has(c.x - 1, c.y)) CuteUI.Fill(new Rect(r.x, r.y, 1, cs), WoodDk);
                if (!Has(c.x + 1, c.y)) CuteUI.Fill(new Rect(r.xMax - 1, r.y, 1, cs), WoodDk);
                if (icon != null)
                {
                    float isz = Mathf.Min(16f, cs - 4);
                    var gc = GUI.color; GUI.color = new Color(gc.r, gc.g, gc.b, gc.a * .92f);
                    CuteUI.Sprite(icon, new Rect(r.center.x - isz / 2f, r.center.y - isz / 2f, isz, isz));
                    GUI.color = gc;
                }
            }
        }

        /// <summary>보자기 조각: 칸마다 볼록, 칸 사이 주름, 바깥 테, 끈 열십자와 매듭</summary>
        static void DrawPiece(List<Vector2Int> cells, Vector2 o, float s, ClothSet cl, float alpha)
        {
            bool Has(int x, int y) { foreach (var c in cells) if (c.x == x && c.y == y) return true; return false; }
            o = new Vector2(Mathf.Round(o.x), Mathf.Round(o.y));
            var gc = GUI.color; GUI.color = new Color(gc.r, gc.g, gc.b, gc.a * alpha);
            foreach (var c in cells)
            {
                float px = o.x + c.x * s, py = o.y + c.y * s;
                CuteUI.Fill(new Rect(px, py, s, s), cl.c);
                if (!Has(c.x, c.y - 1)) CuteUI.Fill(new Rect(px + 1, py + 1, s - 2, 2), cl.hi);
                if (!Has(c.x - 1, c.y)) CuteUI.Fill(new Rect(px + 1, py + 1, 2, s - 2), cl.hi);
                if (!Has(c.x, c.y + 1)) CuteUI.Fill(new Rect(px + 1, py + s - 3, s - 2, 2), cl.lo);
                if (!Has(c.x + 1, c.y)) CuteUI.Fill(new Rect(px + s - 3, py + 1, 2, s - 2), cl.lo);
                if (Has(c.x + 1, c.y)) CuteUI.Fill(new Rect(px + s - 1, py + 4, 1, s - 8), cl.lo);
                if (Has(c.x, c.y + 1)) CuteUI.Fill(new Rect(px + 4, py + s - 1, s - 8, 1), cl.lo);
                if (s >= 16) { CuteUI.Fill(new Rect(px + 5, py + 6, 2, 1), cl.hi); CuteUI.Fill(new Rect(px + 12, py + 12, 2, 1), cl.hi); }
            }
            Outline(cells, o, s, CuteUI.Ink, 1);
            // 끈
            var k = cells[0];
            int half = Mathf.FloorToInt(s / 2f);
            foreach (var c in cells)
            {
                float px = o.x + c.x * s, py = o.y + c.y * s;
                if (c.y == k.y) CuteUI.Fill(new Rect(px, py + half - 1, s, 2), Rope);
                if (c.x == k.x) CuteUI.Fill(new Rect(px + half - 1, py, 2, s), Rope);
            }
            float cx = o.x + k.x * s + half, cy = o.y + k.y * s + half;
            int kn = s >= 16 ? 1 : 0;   // 작은 조각은 매듭도 작게
            CuteUI.Fill(new Rect(cx - 3 - kn, cy - 2 - kn, 6 + 2 * kn, 4 + 2 * kn), CuteUI.Ink);
            CuteUI.Fill(new Rect(cx - 2 - kn, cy - 1 - kn, 4 + 2 * kn, 2 + 2 * kn), Rope);
            if (s >= 16)
            {
                CuteUI.Fill(new Rect(cx - 7, cy - 6, 4, 4), CuteUI.Ink); CuteUI.Fill(new Rect(cx + 3, cy - 6, 4, 4), CuteUI.Ink);
                CuteUI.Fill(new Rect(cx - 6, cy - 5, 2, 2), Rope); CuteUI.Fill(new Rect(cx + 4, cy - 5, 2, 2), Rope);
            }
            GUI.color = gc;
        }

        static void Outline(List<Vector2Int> cells, Vector2 o, float s, Color32 col, int w)
        {
            bool Has(int x, int y) { foreach (var c in cells) if (c.x == x && c.y == y) return true; return false; }
            foreach (var c in cells)
            {
                float px = o.x + c.x * s, py = o.y + c.y * s;
                if (w == 1)
                {
                    if (!Has(c.x, c.y - 1)) CuteUI.Fill(new Rect(px - 1, py - 1, s + 2, 1), col);
                    if (!Has(c.x, c.y + 1)) CuteUI.Fill(new Rect(px - 1, py + s, s + 2, 1), col);
                    if (!Has(c.x - 1, c.y)) CuteUI.Fill(new Rect(px - 1, py - 1, 1, s + 2), col);
                    if (!Has(c.x + 1, c.y)) CuteUI.Fill(new Rect(px + s, py - 1, 1, s + 2), col);
                }
                else
                {
                    if (!Has(c.x, c.y - 1)) CuteUI.Fill(new Rect(px, py, s, w), col);
                    if (!Has(c.x, c.y + 1)) CuteUI.Fill(new Rect(px, py + s - w, s, w), col);
                    if (!Has(c.x - 1, c.y)) CuteUI.Fill(new Rect(px, py, w, s), col);
                    if (!Has(c.x + 1, c.y)) CuteUI.Fill(new Rect(px + s - w, py, w, s), col);
                }
            }
        }

        void DrawTie(Vector2 g, float cs, float tt, Rect win)
        {
            float gw = cols * cs, gh = rows * cs;
            float[] ys = rows >= 3 ? new[] { g.y + cs - 2, g.y + (rows - 1) * cs - 2 } : new[] { g.y + gh / 2f - 2 };
            for (int i = 0; i < ys.Length; i++)
            {
                float len = Mathf.Round((gw + 16) * Ease((tt - i * .1f) / .4f));
                if (len <= 0) continue;
                float ry = Mathf.Round(ys[i]);
                CuteUI.Fill(new Rect(g.x - 8, ry - 1, len, 5), CuteUI.Ink);
                CuteUI.Fill(new Rect(g.x - 8, ry, len, 3), RopeMid);
                CuteUI.Fill(new Rect(g.x - 8, ry + 2, len, 1), RopeLo);
                for (float q = g.x - 4; q < g.x - 8 + len; q += 6) CuteUI.Fill(new Rect(q, ry, 2, 1), RopeLo);
                if (tt > .45f + i * .1f)
                {
                    float kk = Back((tt - .45f - i * .1f) / .25f), kx = g.x + gw + 4, ky = ry + 1;
                    var m = GUI.matrix; CuteUI.ScaleAround(kk, new Vector2(kx, ky));
                    CuteUI.Box(new Rect(kx - 4, ky - 4, 9, 9), 3, RopeMid, CuteUI.Ink, 1); CuteUI.Fill(new Rect(kx - 1, ky - 1, 3, 3), CuteUI.Ink);
                    GUI.matrix = m;
                }
            }
            // 반짝이
            for (int q = 0; q < 6; q++)
            {
                float ph = tt - .4f - q * .12f; if (ph < 0 || ph > .6f) continue;
                float a = ph / .6f, sx = Mathf.Round(g.x + 10 + (q * 47) % Mathf.Max(1, gw - 20)), sy = Mathf.Round(g.y + 6 + (q * 29) % Mathf.Max(1, gh - 12));
                int rad = a < .5f ? 3 : 2; var col = Hex(0xFFF1A8);
                FillA(new Rect(sx - rad, sy, 2 * rad + 1, 1), col, 1f - a); FillA(new Rect(sx, sy - rad, 1, 2 * rad + 1), col, 1f - a); FillA(new Rect(sx - 1, sy - 1, 3, 3), col, 1f - a);
            }
            if (tt > .4f)
            {
                float s = Back((tt - .4f) / .35f);
                var c = new Vector2(g.x + gw / 2f, g.y + gh / 2f - 2);
                var m = GUI.matrix; CuteUI.ScaleAround((cols >= 7 ? 1.6f : 1.3f) * s, c);   // 작은 판에서는 글자가 수레를 넘지 않게
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) if (x != 0 || y != 0) CuteUI.LabelCenter(c.x + x, c.y + y, "꽉 묶었다!", CuteUI.Ink, false, true);
                CuteUI.LabelCenter(c.x, c.y, "꽉 묶었다!", CuteUI.White, false, true);
                GUI.matrix = m;
            }
        }

        static void Coin(float x, float y)
        {
            x = Mathf.Round(x); y = Mathf.Round(y);
            CuteUI.Box(new Rect(x - 5, y - 5, 11, 11), 5, Gold, CuteUI.Ink, 1);
            CuteUI.Fill(new Rect(x - 1, y - 3, 3, 7), GoldLo); CuteUI.Fill(new Rect(x - 3, y - 3, 2, 2), Hex(0xFFF1A8));
        }

        static void Corners(Rect r, int b)
        {
            r = new Rect(r.x - b, r.y - b, r.width + 2 * b, r.height + 2 * b);
            const float L = 5;
            CuteUI.Fill(new Rect(r.x, r.y, L, 2), Teal); CuteUI.Fill(new Rect(r.x, r.y, 2, L), Teal);
            CuteUI.Fill(new Rect(r.xMax - L, r.y, L, 2), Teal); CuteUI.Fill(new Rect(r.xMax - 2, r.y, 2, L), Teal);
            CuteUI.Fill(new Rect(r.x, r.yMax - 2, L, 2), Teal); CuteUI.Fill(new Rect(r.x, r.yMax - L, 2, L), Teal);
            CuteUI.Fill(new Rect(r.xMax - L, r.yMax - 2, L, 2), Teal); CuteUI.Fill(new Rect(r.xMax - 2, r.yMax - L, 2, L), Teal);
        }

        static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return h;
        }
    }
}
