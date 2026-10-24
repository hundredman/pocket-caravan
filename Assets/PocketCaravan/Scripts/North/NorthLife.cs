using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 마을을 살아 있게 하는 연출 (보이기만 함: 충돌·저장·경제와 무관, 공용 코드 수정 없음).
    /// - 돌아다니는 동물: 우리 안 염소·순록, 광장의 까마귀, 부두의 갈매기
    /// - 굴뚝 연기, 밤에 따뜻하게 빛나는 모닥불·등불·온천, 가까이 가면 들리는 모닥불 소리
    /// 자리 자료: Resources/North/NorthLife.json (Tools/MapGen/export_north.py가 맵과 함께 만든다).
    /// </summary>
    public class NorthLife : MonoBehaviour
    {
        [Serializable] public class Group { public string kind; public int x0, y0, x1, y1, count; }
        [Serializable] public class MapLife { public string mapId; public int h; public int[] chimneys, fires, spits, lamps, springs, glints, runes, crystals; public Group[] critters; }
        [Serializable] class Root { public MapLifeRaw[] maps; }
        // JsonUtility는 2차원 배열을 못 읽어서 [[x,y],...]를 따로 푼다
        [Serializable] class MapLifeRaw { public string mapId; public int h; public Group[] critters; }

        class Critter
        {
            public Transform t; public SpriteRenderer sr; public Sprite[] frames;
            public Vector3 target; public float wait, speed, anim; public bool bird;
            public Sprite graze; public bool grazing;   // 10/25 순록: 멈춰 있을 때 고개 숙여 풀(이끼) 뜯기
            public Rect area;
        }

        readonly Dictionary<string, MapLife> life = new Dictionary<string, MapLife>();
        readonly Dictionary<string, Sprite[]> sprites = new Dictionary<string, Sprite[]>();
        readonly List<Critter> critters = new List<Critter>();
        readonly List<(Light2D light, float phase, int mode)> lights = new List<(Light2D, float, int)>();
        readonly List<Vector3> fireSpots = new List<Vector3>();
        readonly List<(SpriteRenderer sr, float phase)> smokes = new List<(SpriteRenderer, float)>();
        Sprite[] smoke, flame, glint;
        Sprite dot, print, grazeSprite;
        readonly List<(SpriteRenderer sr, float phase, float size)> flames = new List<(SpriteRenderer, float, float)>();
        readonly List<(SpriteRenderer sr, float next)> glints = new List<(SpriteRenderer, float)>();
        class Ember { public Transform t; public SpriteRenderer sr; public Vector3 home; public float age, life; public Vector2 v; }
        readonly List<Ember> embers = new List<Ember>();
        readonly List<(SpriteRenderer sr, float born)> prints = new List<(SpriteRenderer, float)>();
        Vector3 lastStep; bool leftFoot;
        Transform root;
        string lastMap;
        AudioSource crackle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthLife>() != null) return;
            var go = new GameObject("NorthLife");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthLife>();
        }

        void Awake()
        {
            var json = Resources.Load<TextAsset>("North/NorthLife");
            if (json != null) Parse(json.text);
            smoke = LoadFrames("North/smoke_", 4, new Vector2(0.5f, 0f));
            flame = LoadFrames("North/flame_", 4, new Vector2(0.5f, 0.1f));
            glint = LoadFrames("North/glint_", 4, new Vector2(0.5f, 0.5f));
            dot = Pixel(new Color32(255, 200, 120, 255), 1, 1);
            print = Pixel(new Color32(150, 146, 170, 255), 2, 1);
            foreach (var k in new[] { "reindeer", "goat", "raven", "gull" })
                sprites[k] = LoadFrames($"North/Critters/{k}_", 4, new Vector2(0.5f, 0.1f));   // 순록은 4프레임, 나머지는 2프레임 (없는 프레임은 건너뜀)
            var g = LoadFrames("North/Critters/reindeer_graze_", 1, new Vector2(0.5f, 0.1f));
            grazeSprite = g.Length > 0 ? g[0] : null;
            crackle = gameObject.AddComponent<AudioSource>();
            crackle.clip = MakeCrackle();
            crackle.loop = true; crackle.volume = 0f; crackle.playOnAwake = false;
        }

        static Sprite Pixel(Color32 c, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[w * h]; for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite[] LoadFrames(string prefix, int n, Vector2 pivot)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < n; i++)
            {
                var tex = Resources.Load<Texture2D>(prefix + i);
                if (tex == null) continue;
                tex.filterMode = FilterMode.Point;
                list.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 16f));
            }
            return list.ToArray();
        }

        void Parse(string text)
        {
            var data = JsonUtility.FromJson<Root>(text);
            if (data == null || data.maps == null) return;
            // 좌표 목록은 직접 꺼낸다: "chimneys": [[x, y], ...]
            foreach (var raw in data.maps)
            {
                var ml = new MapLife { mapId = raw.mapId, h = raw.h, critters = raw.critters ?? new Group[0] };
                string block = Block(text, raw.mapId);
                ml.chimneys = Pairs(block, "chimneys"); ml.fires = Pairs(block, "fires");
                ml.lamps = Pairs(block, "lamps"); ml.springs = Pairs(block, "springs");
                ml.spits = Pairs(block, "spits"); ml.glints = Pairs(block, "glints");
                ml.runes = Pairs(block, "runes"); ml.crystals = Pairs(block, "crystals");
                life[raw.mapId] = ml;
            }
        }

        static string Block(string text, string mapId)
        {
            int i = text.IndexOf("\"" + mapId + "\"", StringComparison.Ordinal);
            if (i < 0) return "";
            int j = text.IndexOf("\"mapId\"", i + mapId.Length + 2, StringComparison.Ordinal);
            return j < 0 ? text.Substring(i) : text.Substring(i, j - i);
        }

        static int[] Pairs(string block, string key)
        {
            int i = block.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
            if (i < 0) return new int[0];
            int a = block.IndexOf('[', i), depth = 0, b = a;
            for (; b < block.Length; b++)
            {
                if (block[b] == '[') depth++;
                else if (block[b] == ']' && --depth == 0) break;
            }
            var nums = new List<int>();
            foreach (var part in block.Substring(a, b - a).Split(new[] { '[', ']', ',', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(part, out int v)) nums.Add(v);
            return nums.ToArray();
        }

        Vector3 Cell(MapLife ml, int x, int y) => MapRoot.Current.CellCenter(new Vector3Int(x, ml.h - 1 - y, 0));

        void Update()
        {
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            if (root == null) { if (crackle.isPlaying) crackle.Stop(); return; }

            float night = GameManager.I != null ? 1f - GameManager.I.Clock.Daylight : 0f;
            float t = Time.time;
            foreach (var (light, phase, mode) in lights)
            {
                if (light == null) continue;
                if (mode == 2) { light.intensity = night * (0.55f + 0.35f * Mathf.Sin(t * 1.3f + phase)); continue; }   // 밤에만, 숨 쉬듯
                bool fire = mode == 1;
                float flicker = fire ? 0.85f + 0.15f * Mathf.Sin(t * 9f + phase) * Mathf.Sin(t * 5.3f + phase * 2f) : 1f;
                light.intensity = Mathf.Lerp(0.15f, fire ? 1.25f : 0.9f, night) * flicker;
            }
            for (int i = 0; i < smokes.Count; i++)
            {
                var (sr, phase) = smokes[i];
                if (sr == null || smoke.Length == 0) continue;
                sr.sprite = smoke[(int)(t * 3f + phase) % smoke.Length];
            }
            MoveCritters(Time.deltaTime);
            foreach (var (sr, phase, size) in flames)
                if (sr != null && flame.Length > 0)
                {
                    sr.sprite = flame[(int)(t * 9f + phase) % flame.Length];
                    float s = size * (1f + 0.08f * Mathf.Sin(t * 13f + phase));
                    sr.transform.localScale = new Vector3(s, s * (1f + 0.1f * Mathf.Sin(t * 7f + phase)), 1f);
                }
            UpdateEmbers(Time.deltaTime);
            for (int i = 0; i < glints.Count; i++)
            {
                var (sr, next) = glints[i];
                if (sr == null || glint.Length == 0) continue;
                float k = t - next;                     // 0~0.6초 동안 반짝, 그다음 몇 초 쉼
                if (k < 0f) { sr.enabled = false; continue; }
                if (k > 0.6f) { glints[i] = (sr, t + UnityEngine.Random.Range(2f, 9f)); sr.enabled = false; continue; }
                sr.enabled = true;
                sr.sprite = glint[Mathf.Min(glint.Length - 1, (int)(k / 0.15f))];
            }
            UpdateFootprints(t);

            // 모닥불 소리: 가장 가까운 불까지 거리로 크기 (5칸 안에서 들림)
            var cam = Camera.main;
            float vol = 0f;
            if (cam != null)
                foreach (var p in fireSpots)
                    vol = Mathf.Max(vol, Mathf.Clamp01(1f - Vector2.Distance(cam.transform.position, p) / 5f));
            crackle.volume = AudioManager.Muted ? 0f : vol * 0.35f;
            if (vol > 0f && !crackle.isPlaying) crackle.Play();
            else if (vol <= 0f && crackle.isPlaying) crackle.Stop();
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null; critters.Clear(); lights.Clear(); smokes.Clear(); fireSpots.Clear();
            flames.Clear(); glints.Clear(); embers.Clear(); prints.Clear(); lastStep = Vector3.positiveInfinity;
            if (map == null || !life.TryGetValue(map, out var ml) || MapRoot.Current == null) return;
            root = new GameObject("North Life").transform;   // 씬 안에 둬서 맵을 떠나면 같이 사라진다

            for (int i = 0; i + 1 < ml.chimneys.Length; i += 2)
            {
                var go = new GameObject("Chimney Smoke");
                go.transform.SetParent(root);
                go.transform.position = Cell(ml, ml.chimneys[i], ml.chimneys[i + 1]) + new Vector3(0f, 0.3f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 21;   // 지붕(Above 20) 위
                sr.color = new Color(1f, 1f, 1f, 0.8f);
                smokes.Add((sr, UnityEngine.Random.value * 4f));
            }
            void AddLight(int x, int y, Color c, float radius, bool fire, int mode = -1)
            {
                var go = new GameObject(fire ? "Fire Light" : "Lamp Light");
                go.transform.SetParent(root);
                go.transform.position = Cell(ml, x, y) + (fire ? new Vector3(-0.5f, 0.5f, 0f) : Vector3.zero);
                var l = go.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                l.color = c;
                l.pointLightInnerRadius = radius * 0.2f;
                l.pointLightOuterRadius = radius;
                l.falloffIntensity = 0.7f;
                lights.Add((l, UnityEngine.Random.value * 10f, mode >= 0 ? mode : (fire ? 1 : 0)));
            }
            // 모닥불·고기 화덕: 2칸 그림의 가운데(칸 경계)에 흔들리는 불꽃 + 튀는 불씨 + 빛
            void AddFire(int x, int y, float size, float lift)
            {
                var p = Cell(ml, x, y) + new Vector3(-0.5f, 0.5f + lift, 0f);
                var go = new GameObject("Flame");
                go.transform.SetParent(root);
                go.transform.position = p;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 6;   // 화덕 그림(Objects 5) 위
                sr.color = new Color(1f, 1f, 1f, 0.95f);
                go.transform.localScale = Vector3.one * size;
                flames.Add((sr, UnityEngine.Random.value * 4f, size));
                fireSpots.Add(p);
                for (int e = 0; e < 4; e++)
                {
                    var eg = new GameObject("Ember");
                    eg.transform.SetParent(root);
                    var esr = eg.AddComponent<SpriteRenderer>();
                    esr.sprite = dot; esr.sortingOrder = 22;
                    embers.Add(new Ember { t = eg.transform, sr = esr, home = p + new Vector3(0f, 0.3f, 0f), age = UnityEngine.Random.value * 2f, life = 1.6f });
                }
            }
            for (int i = 0; i + 1 < ml.fires.Length; i += 2)
            {
                AddFire(ml.fires[i], ml.fires[i + 1], 1f, -0.25f);
                AddLight(ml.fires[i], ml.fires[i + 1], new Color(1f, 0.62f, 0.32f), 4.5f, true);
            }
            for (int i = 0; i + 1 < ml.spits.Length; i += 2)
            {
                AddFire(ml.spits[i], ml.spits[i + 1], 0.6f, -0.55f);   // 꼬치 밑 작은 불
                AddLight(ml.spits[i], ml.spits[i + 1], new Color(1f, 0.6f, 0.3f), 3.5f, true);
            }
            // 밤에 빛나는 룬돌(청록)과 수정(푸른빛)
            for (int i = 0; i + 1 < ml.runes.Length; i += 2)
                AddLight(ml.runes[i], ml.runes[i + 1], new Color(0.35f, 1f, 0.85f), 2.4f, false, 2);
            for (int i = 0; i + 1 < ml.crystals.Length; i += 2)
                AddLight(ml.crystals[i], ml.crystals[i + 1], new Color(0.45f, 0.75f, 1f), 1.8f, false, 2);
            // 눈 반짝임: 빈 눈밭 곳곳에서 가끔 반짝
            for (int i = 0; i + 1 < ml.glints.Length; i += 2)
            {
                var go = new GameObject("Snow Glint");
                go.transform.SetParent(root);
                go.transform.position = Cell(ml, ml.glints[i], ml.glints[i + 1]) + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(-0.3f, 0.3f), 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2; sr.enabled = false;
                glints.Add((sr, Time.time + UnityEngine.Random.Range(0f, 8f)));
            }
            for (int i = 0; i + 1 < ml.lamps.Length; i += 2)
                AddLight(ml.lamps[i], ml.lamps[i + 1], new Color(1f, 0.8f, 0.5f), 2.6f, false);
            for (int i = 0; i + 1 < ml.springs.Length; i += 2)
                AddLight(ml.springs[i], ml.springs[i + 1], new Color(0.55f, 0.9f, 1f), 2.2f, false);

            foreach (var g in ml.critters)
            {
                if (!sprites.TryGetValue(g.kind, out var frames) || frames.Length == 0) continue;
                var a = Cell(ml, g.x0, g.y1); var b = Cell(ml, g.x1, g.y0);
                var area = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                for (int i = 0; i < g.count; i++)
                {
                    var go = new GameObject("Critter " + g.kind);
                    go.transform.SetParent(root);
                    var c = new Critter { t = go.transform, sr = go.AddComponent<SpriteRenderer>(), frames = frames, area = area,
                                          bird = g.kind == "raven" || g.kind == "gull" };
                    c.sr.sprite = frames[0]; c.sr.sortingOrder = 9;
                    if (g.kind == "reindeer") c.graze = grazeSprite;
                    c.t.position = RandomIn(area);
                    c.target = c.t.position; c.wait = UnityEngine.Random.Range(0f, 3f);
                    c.speed = c.bird ? 2.2f : 0.9f;
                    critters.Add(c);
                }
            }
        }

        void UpdateEmbers(float dt)
        {
            float night = GameManager.I != null ? 1f - GameManager.I.Clock.Daylight : 0f;
            foreach (var e in embers)
            {
                e.age += dt;
                if (e.age > e.life)
                {
                    e.age = 0f; e.life = UnityEngine.Random.Range(1.0f, 2.2f);
                    e.v = new Vector2(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(0.6f, 1.1f));
                    e.t.position = e.home + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0f, 0f);
                }
                e.t.position += (Vector3)(e.v * dt) + new Vector3(Mathf.Sin(e.age * 6f) * 0.2f * dt, 0f, 0f);
                float a = Mathf.Clamp01(1f - e.age / e.life);
                e.sr.color = new Color(1f, 0.75f, 0.4f, a * Mathf.Lerp(0.6f, 1f, night));
            }
        }

        /// <summary>눈 위 발자국: 길(다져진 눈)이 아닌 눈밭을 걸으면 남고 몇 초 뒤 사라진다</summary>
        void UpdateFootprints(float t)
        {
            var player = FindAnyObjectByType<PlayerMotor>();
            var map = MapRoot.Current;
            if (player != null && map != null)
            {
                Vector3 feet = player.transform.position + new Vector3(0f, -0.4f, 0f);
                if (float.IsInfinity(lastStep.x)) lastStep = feet;
                if (Vector2.Distance(feet, lastStep) > 0.32f)
                {
                    var cell = map.WorldToCell(feet);
                    bool onRoad = map.road != null && map.road.HasTile(cell);
                    if (!onRoad)
                    {
                        var dir = ((Vector2)(feet - lastStep)).normalized;
                        var side = new Vector3(-dir.y, dir.x, 0f) * (leftFoot ? 0.09f : -0.09f);
                        var go = new GameObject("Footprint");
                        go.transform.SetParent(root);
                        go.transform.position = feet + side;
                        go.transform.rotation = Quaternion.FromToRotation(Vector3.right, new Vector3(-dir.y, dir.x, 0f));
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = print; sr.sortingOrder = 2;
                        prints.Add((sr, t));
                        leftFoot = !leftFoot;
                        if (prints.Count > 90) { if (prints[0].sr != null) Destroy(prints[0].sr.gameObject); prints.RemoveAt(0); }
                    }
                    lastStep = feet;
                }
            }
            for (int i = prints.Count - 1; i >= 0; i--)
            {
                var (sr, born) = prints[i];
                if (sr == null) { prints.RemoveAt(i); continue; }
                float age = t - born;
                if (age > 10f) { Destroy(sr.gameObject); prints.RemoveAt(i); continue; }
                sr.color = new Color(1f, 1f, 1f, 0.55f * Mathf.Clamp01(1f - age / 10f));
            }
        }

        static Vector3 RandomIn(Rect r) => new Vector3(UnityEngine.Random.Range(r.xMin, r.xMax), UnityEngine.Random.Range(r.yMin, r.yMax), 0f);

        void MoveCritters(float dt)
        {
            var player = MapRoot.Current != null ? FindAnyObjectByType<PlayerMotor>() : null;
            foreach (var c in critters)
            {
                // 새는 플레이어가 가까이 오면 날아오르듯 멀리 깡충 도망
                if (c.bird && player != null && Vector2.Distance(player.transform.position, c.t.position) < 1.6f && c.wait > 0f)
                {
                    c.target = RandomIn(c.area); c.wait = 0f;
                }
                if (c.wait > 0f) { c.wait -= dt; c.sr.sprite = c.grazing && c.graze != null ? c.graze : c.frames[0]; continue; }
                var to = c.target - c.t.position;
                if (to.magnitude < 0.05f)
                {
                    c.wait = c.bird ? UnityEngine.Random.Range(0.6f, 2.5f) : UnityEngine.Random.Range(1.5f, 5f);
                    c.grazing = c.graze != null && UnityEngine.Random.value < 0.6f;
                    c.target = RandomIn(c.area);
                    continue;
                }
                c.t.position += to.normalized * Mathf.Min(to.magnitude, c.speed * dt);
                if (Mathf.Abs(to.x) > 0.01f) c.sr.flipX = to.x < 0f;
                c.anim += dt * (c.bird ? 8f : 5f);
                c.sr.sprite = c.frames[(int)c.anim % c.frames.Length];
            }
        }

        /// <summary>모닥불 타닥 소리 (파일 없이 코드로 만든 2초 반복음)</summary>
        static AudioClip MakeCrackle()
        {
            const int rate = 22050; int n = rate * 2;
            var data = new float[n];
            var rng = new System.Random(7);
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                low += ((float)rng.NextDouble() * 2f - 1f - low) * 0.02f;   // 낮게 웅웅거리는 불길
                data[i] = low * 0.5f;
            }
            for (int k = 0; k < 40; k++)   // 탁, 탁 튀는 소리
            {
                int at = rng.Next(n - 400); float amp = 0.3f + (float)rng.NextDouble() * 0.5f;
                for (int j = 0; j < 300; j++) data[at + j] += amp * ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-j / 40f);
            }
            var clip = AudioClip.Create("north_crackle", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
