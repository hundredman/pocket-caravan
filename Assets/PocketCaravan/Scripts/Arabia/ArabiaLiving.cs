using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 씬 전용 "살아 있는 풍경" (2026-11-18). 순수 표시용: 판정·충돌·저장에 영향 없음.
    /// - 물(바다·강·오아시스): 잔물결 빛 반짝임과 짧은 물결 줄이 천천히 흘렀다 사라진다. 밤엔 어둡게.
    /// - 배(다우선): 물 위에서 1픽셀씩 천천히 출렁이고, 뱃전에 잔물결이 인다.
    /// - 낙타·당나귀·양: 제자리 근처를 가끔 몇 걸음 걷거나 몸을 돌린다. 앉은 낙타는 숨 쉬듯 천천히 움직인다.
    ///   (모두가 늘 움직이지는 않는다: 대부분은 쉬고, 가끔씩만 움직인다.)
    /// 동물·배는 맵 타일(arabia_drawn_*)로 깔려 있어서, 시작할 때 그 타일 조각을 모아 스프라이트 묶음으로 바꾼다.
    /// 막힘(Blocked) 타일은 그대로 두므로 길막 판정은 바뀌지 않는다. 그래서 동물은 집 둘레 1칸 남짓만 걷는다.
    /// ArabiaAmbience가 아라비아 씬에서 함께 붙인다. 공용 코드는 바꾸지 않는다.
    /// </summary>
    public class ArabiaLiving : MonoBehaviour
    {
        const float Px = 1f / 16f;
        const int OrderWater = 1;
        static readonly string[] Movers = { "camelRest", "camel", "donkey", "sheep2", "sheep", "dhow" };

        /// <summary>11/18 요청: 아라비아에서는 '길 잃은 길손 데려다주기'(뒤따라오는 할아버지)를 끈다.
        /// 공용 LostTraveler는 그대로 두고, 지역이 등록하는 날씨 규칙만 "궂은 날 아님"으로 둔다 (북·베·고려와 같은 등록 방식).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void NoLostTraveler() => LostTraveler.Weather[Reactions.Region.Arabia] = () => false;

        MapRoot map;
        Camera cam;
        Transform root;
        System.Random rng;

        // ---------------------------------------------------------------- 시작
        void Start() { SetupFor(MapRoot.Current, null); }

        /// <summary>시작 준비 (에디터 점검에서도 부른다)</summary>
        public void SetupFor(MapRoot m, Camera c)
        {
            map = m; cam = c;
            if (map == null || map.ground == null) { enabled = false; return; }
            rng = new System.Random(map.mapId.GetHashCode());
            root = new GameObject("arabia_living").transform;
            root.SetParent(transform, false);
            FindWater();
            CollectMovers();
        }

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x / Px) * Px, Mathf.Round(p.y / Px) * Px, p.z);
        float Daylight => GameManager.I != null ? GameManager.I.Clock.Daylight : 1f;
        static Vector2 PlayerFeet => PlayerController.Instance != null ? PlayerController.Instance.Motor.Feet : new Vector2(9999, 9999);

        Rect View(float margin)
        {
            float h = cam.orthographicSize + margin, w = cam.orthographicSize * cam.aspect + margin;
            var c = cam.transform.position;
            return new Rect(c.x - w, c.y - h, 2 * w, 2 * h);
        }

        void Update()
        {
            cam = Camera.main;
            Step(Time.deltaTime);
        }

        public void Step(float dt)
        {
            if (cam == null || root == null) return;
            UpdateWater(dt);
            UpdateMovers(dt);
        }

        // ---------------------------------------------------------------- 그림 (코드로 그린 작은 픽셀)
        static Sprite dot, dash, dashShort;
        static Sprite Bar(int w)
        {
            var t = new Texture2D(w, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var c = new Color32[w];
            for (int i = 0; i < w; i++) c[i] = new Color32(255, 255, 255, (byte)(i == 0 || i == w - 1 ? 150 : 255));   // 끝은 살짝 옅게
            t.SetPixels32(c); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 16f);
        }
        static void MakeArt()
        {
            if (dot != null) return;
            dot = Bar(1); dash = Bar(5); dashShort = Bar(3);
        }

        SpriteRenderer MakeSr(string name, Sprite s, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
            return sr;
        }

        // ---------------------------------------------------------------- 물
        readonly List<Vector3> waterCells = new List<Vector3>();   // 사방이 물인 칸 중심 (물가 테두리 위엔 안 그림)
        readonly List<Vector3> visibleWater = new List<Vector3>();
        float visibleCheck;

        class Ripple
        {
            public SpriteRenderer sr; public float life, max, alpha; public Vector3 vel, at;   // at = 실제 위치 (그릴 땐 픽셀에 맞춤)
            public void Place(Vector3 p) { at = p; sr.transform.position = Snap(p); }
        }
        readonly List<Ripple> glints = new List<Ripple>(), waves = new List<Ripple>();

        static bool IsWater(TileBase t) => t != null && t.name.StartsWith("arabia_twater");

        void FindWater()
        {
            var g = map.ground; var b = g.cellBounds;
            for (int x = b.xMin; x < b.xMax; x++)
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (!IsWater(g.GetTile(c))) continue;
                    if (!IsWater(g.GetTile(c + Vector3Int.up)) || !IsWater(g.GetTile(c + Vector3Int.down)) ||
                        !IsWater(g.GetTile(c + Vector3Int.left)) || !IsWater(g.GetTile(c + Vector3Int.right))) continue;
                    waterCells.Add(g.GetCellCenterWorld(c));
                }
            if (waterCells.Count > 0) MakeArt();
        }

        Ripple Take(List<Ripple> pool, int max, Sprite s, string name)
        {
            foreach (var r in pool) if (r.life <= 0f) { r.sr.gameObject.SetActive(true); r.sr.sprite = s; return r; }
            if (pool.Count >= max) return null;
            var n = new Ripple { sr = MakeSr(name, s, OrderWater, root) };
            pool.Add(n); return n;
        }

        int Active(List<Ripple> pool) { int n = 0; foreach (var r in pool) if (r.life > 0f) n++; return n; }

        void UpdateWater(float dt)
        {
            if (waterCells.Count > 0 && Time.time >= visibleCheck)
            {
                visibleCheck = Time.time + 0.5f;
                visibleWater.Clear();
                var v = View(1f);
                foreach (var p in waterCells) if (v.Contains(p)) visibleWater.Add(p);
            }
            float night = 0.35f + 0.65f * Daylight;
            if (visibleWater.Count > 0)
            {
                // 반짝임: 보이는 물 12칸에 하나쯤, 물결 줄: 22칸에 하나쯤
                int wantGlint = Mathf.Clamp(visibleWater.Count / 12, 1, 26), wantWave = Mathf.Clamp(visibleWater.Count / 22, 1, 16);
                if (Active(glints) < wantGlint && rng.NextDouble() < dt * 8f)
                {
                    var r = Take(glints, 26, rng.NextDouble() < 0.7 ? dot : dashShort, "glint");
                    if (r != null)
                    {
                        r.Place(visibleWater[rng.Next(visibleWater.Count)] + new Vector3(Rand(-0.45f, 0.45f), Rand(-0.45f, 0.45f), 0));
                        r.max = r.life = Rand(0.7f, 1.5f); r.alpha = Rand(0.55f, 0.85f); r.vel = Vector3.zero;
                    }
                }
                if (Active(waves) < wantWave && rng.NextDouble() < dt * 3f)
                {
                    var r = Take(waves, 16, rng.NextDouble() < 0.6 ? dash : dashShort, "wave");
                    if (r != null)
                    {
                        r.Place(visibleWater[rng.Next(visibleWater.Count)] + new Vector3(Rand(-0.4f, 0.4f), Rand(-0.4f, 0.4f), 0));
                        r.max = r.life = Rand(2.2f, 3.8f); r.alpha = Rand(0.22f, 0.34f);
                        r.vel = new Vector3(rng.NextDouble() < 0.5 ? 0.12f : -0.12f, 0, 0);
                    }
                }
            }
            Tick(glints, dt, night);
            Tick(waves, dt, night);
        }

        static void Tick(List<Ripple> pool, float dt, float night)
        {
            foreach (var r in pool)
            {
                if (r.life <= 0f) continue;
                r.life -= dt;
                float t = 1f - Mathf.Clamp01(r.life / r.max);           // 0 → 1
                float a = Mathf.Sin(t * Mathf.PI);                      // 천천히 나타났다 사라짐
                if (r.vel != Vector3.zero) r.Place(r.at + r.vel * dt);
                r.sr.color = new Color(0.92f, 0.97f, 1f, r.alpha * a * night);
                if (r.life <= 0f) r.sr.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- 동물 · 배
        class Mover
        {
            public string kind;
            public Transform t;
            public Vector3 home, pos, target;
            public readonly List<Transform> top = new List<Transform>();   // 맨 윗줄 조각 (앉은 낙타 숨쉬기)
            public float wait, phase, stepT, breathT;
            public bool moving, flipped;
            public Vector3 wake;                                            // 배: 잔물결 위치 (배 기준)
        }
        readonly List<Mover> movers = new List<Mover>();

        static bool Parse(string n, out string kind, out int cx, out int cy)
        {
            kind = null; cx = cy = 0;
            if (n == null || !n.StartsWith("arabia_drawn_")) return false;
            var parts = n.Substring(13).Split('_');
            if (parts.Length != 3 || !int.TryParse(parts[1], out cx) || !int.TryParse(parts[2], out cy)) return false;
            foreach (var k in Movers) if (parts[0] == k) { kind = k; return true; }
            return false;
        }

        void CollectMovers()
        {
            // 레이어: 바닥 장식(배가 깔린 곳)은 2, 소품 5, 위 20 (원래 TilemapRenderer 순서를 따른다)
            var groups = new Dictionary<(string, int, int), List<(Tilemap, Vector3Int, int, int)>>();
            foreach (var tm in map.GetComponentsInChildren<Tilemap>())
            {
                if (tm == map.ground || tm == map.blocked || tm.GetComponent<TilemapRenderer>() == null) continue;
                var b = tm.cellBounds;
                for (int x = b.xMin; x < b.xMax; x++)
                    for (int y = b.yMin; y < b.yMax; y++)
                    {
                        var c = new Vector3Int(x, y, 0);
                        var tile = tm.GetTile(c);
                        if (tile == null || !Parse(tile.name, out var kind, out int cx, out int cy)) continue;
                        var key = (kind, x - cx, y + cy);     // 그림 왼쪽 위 칸 (그림의 cy는 위에서 아래로)
                        if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<(Tilemap, Vector3Int, int, int)>();
                        list.Add((tm, c, cx, cy));
                    }
            }
            foreach (var kv in groups)
            {
                string kind = kv.Key.Item1;
                int w = 0, h = 0;
                foreach (var p in kv.Value) { w = Mathf.Max(w, p.Item3 + 1); h = Mathf.Max(h, p.Item4 + 1); }
                var g = map.ground;
                Vector3 topLeft = g.GetCellCenterWorld(new Vector3Int(kv.Key.Item2, kv.Key.Item3, 0)) + new Vector3(-0.5f, 0.5f, 0);
                var pivot = topLeft + new Vector3(w * 0.5f, -h, 0);          // 아래 가운데 (뒤집어도 제자리)
                var go = new GameObject("living_" + kind);
                go.transform.SetParent(root, false);
                go.transform.position = pivot;
                var m = new Mover { kind = kind, t = go.transform, home = pivot, pos = pivot, target = pivot, phase = Rand(0f, 10f), wait = Rand(1f, 8f) };
                foreach (var (tm, c, cx, cy) in kv.Value)
                {
                    var sprite = tm.GetSprite(c);
                    if (sprite == null) continue;
                    var tr = tm.GetComponent<TilemapRenderer>();
                    int order = tr.sortingOrder <= 1 ? 2 : tr.sortingOrder;
                    var sr = MakeSr(kind + "_" + cx + "_" + cy, sprite, order, go.transform);
                    sr.sortingLayerID = tr.sortingLayerID;
                    sr.color = tm.GetColor(c);
                    sr.transform.position = tm.GetCellCenterWorld(c);
                    if (cy == 0 && h > 1) m.top.Add(sr.transform);
                    tm.SetTile(c, null);                                       // 같은 그림이 두 번 보이지 않게 타일은 지운다 (막힘 판정은 Blocked에 따로 있음)
                }
                if (kind == "dhow")
                {
                    // 모래 위에 얹어 둔 배(사막 장식)는 출렁이지 않는다: 배 밑 칸이 물일 때만 움직인다
                    var under = map.ground.GetTile(map.WorldToCell(pivot + new Vector3(0, 0.5f, 0)));
                    if (!IsWater(under)) m.kind = "still";
                    else { MakeArt(); m.wake = new Vector3(w * 0.5f - 0.2f, 0.25f, 0); }
                }
                movers.Add(m);
            }
        }

        void UpdateMovers(float dt)
        {
            if (movers.Count == 0) return;
            var view = View(3f);
            Vector2 feet = PlayerFeet;
            foreach (var m in movers)
            {
                if (m.t == null || !view.Contains(m.home)) continue;           // 화면 밖은 쉰다
                switch (m.kind)
                {
                    case "dhow": Boat(m, dt); break;
                    case "camelRest": Rest(m, dt); break;
                    case "still": break;
                    default: Walker(m, dt, feet); break;
                }
            }
        }

        // 배: 물결 따라 1픽셀 위아래 (서로 다른 박자), 가끔 뱃전에 잔물결
        void Boat(Mover m, float dt)
        {
            float s = Mathf.Sin((Time.time + m.phase) * (2f * Mathf.PI / 3.2f));
            float s2 = Mathf.Sin((Time.time + m.phase) * (2f * Mathf.PI / 7.7f));
            float y = (s + 0.5f * s2) > 0.35f ? Px : 0f;
            m.t.position = Snap(m.home + new Vector3(0, y, 0));
            m.wait -= dt;
            if (m.wait <= 0f)
            {
                m.wait = Rand(0.8f, 1.8f);
                var r = Take(waves, 16 + movers.Count * 2, dashShort, "wake");
                if (r != null)
                {
                    bool left = rng.NextDouble() < 0.5;
                    r.Place(m.home + new Vector3(left ? -m.wake.x : m.wake.x, m.wake.y, 0));
                    r.max = r.life = Rand(1.4f, 2.2f); r.alpha = Rand(0.35f, 0.5f);
                    r.vel = new Vector3(left ? -0.16f : 0.16f, 0, 0);
                }
            }
        }

        // 앉은 낙타: 그림을 쪼개 움직이면 머리가 눌려 보이므로 통째로만 움직인다.
        // 가끔 고개를 반대로 돌리고(좌우 뒤집기), 가끔 몸을 1픽셀 들썩이며 자리를 고쳐 앉는다.
        void Rest(Mover m, float dt)
        {
            m.wait -= dt;
            if (m.breathT > 0f) m.breathT -= dt;
            if (m.wait <= 0f)
            {
                m.wait = Rand(4f, 9f);
                if (rng.NextDouble() < 0.5) Flip(m, !m.flipped);
                else m.breathT = 0.5f;
            }
            float y = m.breathT > 0f && m.breathT < 0.35f ? Px : 0f;
            m.t.position = Snap(m.home + new Vector3(0, y, 0));
        }

        readonly Dictionary<Transform, float> baseY = new Dictionary<Transform, float>();
        float BaseY(Transform p, Mover m)
        {
            if (!baseY.TryGetValue(p, out float y)) baseY[p] = y = p.localPosition.y;
            return y;
        }

        static void Flip(Mover m, bool on) { m.flipped = on; m.t.localScale = new Vector3(on ? -1f : 1f, 1f, 1f); }

        // 서 있는 동물: 대부분 쉬다가 가끔 몇 걸음 걷거나 몸을 돌린다. 양은 플레이어가 아주 가까이 오면 살짝 비켜선다.
        void Walker(Mover m, float dt, Vector2 feet)
        {
            bool sheep = m.kind.StartsWith("sheep");
            float speed = sheep ? 0.7f : m.kind == "donkey" ? 0.6f : 0.5f;
            float range = sheep ? 1.9f : 1.6f;
            float near = Vector2.Distance(feet, m.pos);
            if (!m.moving)
            {
                m.wait -= dt;
                if (sheep && near < 1.3f && m.wait > 0.3f) m.wait = 0.3f;     // 놀라서 곧 움직인다
                if (m.wait <= 0f)
                {
                    m.wait = Rand(2f, 6f);
                    double roll = rng.NextDouble();
                    if (sheep && near < 1.3f) Go(m, m.pos + (Vector3)((Vector2)m.pos - feet).normalized * 0.9f, range);
                    else if (roll < 0.6) Go(m, m.home + new Vector3(Rand(-range, range), Rand(-range, range) * 0.35f, 0), range);
                    else if (roll < 0.85) Flip(m, !m.flipped);
                }
            }
            if (m.moving)
            {
                if (!sheep && near < 0.9f) { m.moving = false; m.wait = Rand(1f, 2f); }   // 사람 앞에선 멈춘다
                else
                {
                    var d = m.target - m.pos;
                    float step = speed * dt;
                    if (d.magnitude <= step) { m.pos = m.target; m.moving = false; }
                    else m.pos += d.normalized * step;
                    if (Mathf.Abs(d.x) > 0.02f) Flip(m, d.x < 0f);                       // 그림이 오른쪽을 본다
                    m.stepT += dt;
                }
            }
            float bob = m.moving && ((int)(m.stepT / (sheep ? 0.14f : 0.2f)) % 2 == 1) ? Px : 0f;   // 걸을 때 한 걸음마다 1픽셀
            m.t.position = Snap(m.pos + new Vector3(0, bob, 0));
        }

        void Go(Mover m, Vector3 to, float range)
        {
            var off = to - m.home;
            if (off.magnitude > range) to = m.home + off.normalized * range;
            if ((to - m.pos).magnitude < 0.35f) return;
            // 발이 닿을 곳이 물·벽이면 가지 않는다 (집 칸은 원래 막혀 있으니 괜찮다)
            var cell = map.WorldToCell(to + new Vector3(0, 0.2f, 0));
            var homeCell = map.WorldToCell(m.home + new Vector3(0, 0.2f, 0));
            if (Mathf.Abs(cell.x - homeCell.x) > 1 || Mathf.Abs(cell.y - homeCell.y) > 1)
                if (!map.IsWalkable(cell)) return;
            if (IsWater(map.ground.GetTile(cell))) return;
            m.target = to; m.moving = true; m.stepT = 0f;
        }
    }
}
