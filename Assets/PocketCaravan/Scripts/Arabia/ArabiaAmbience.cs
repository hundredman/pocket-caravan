using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 씬 전용 분위기 연출 (2026-10-27). 순수 표시용: 판정·이동·저장에 영향 없음, 충돌체 없음.
    /// - 모래·모래 언덕을 걸으면 발자국이 남고 천천히 지워진다. 달리면 발밑에 모래 먼지.
    /// - 가끔 모래바람: 화면이 모래빛으로 흐려지고 모래 줄기가 휩쓸고 지나가며, 사막에선 회전초가 굴러간다. 바람 소리.
    /// - 가끔 회오리 모래기둥(사막).
    /// - 만국 장터 대광장: 밤엔 불꽃놀이, 낮엔 색종이 꽃가루.
    /// - 밤 불빛: 등불·가로등·창문·천막·노점에 따뜻한 불빛(Light2D)이 켜지고 깜빡인다. 관람차는 천천히 돌고, 분수엔 물이 떨어진다.
    /// - 동물: 독수리(가까이 가면 날아올라 도망) · 하늘을 맴도는 독수리(땅에 그림자) · 도마뱀(가까이 가면 쏜살같이 숨음)
    ///   · 쇠똥구리 · 마을 비둘기 떼(가까이 가면 한꺼번에 날아감) · 골목 고양이 · 밤의 날쥐(저보아) · 해안 갈매기.
    /// 그림은 코드로 그린 작은 픽셀 그림이라 에셋이 필요 없다. 공용 코드는 바꾸지 않는다.
    /// ArabiaSceneDressing이 아라비아 씬마다 하나씩 붙인다. 맵 성격은 mapId로 정한다.
    /// </summary>
    public class ArabiaAmbience : MonoBehaviour
    {
        public static ArabiaAmbience Current { get; private set; }

        // 맵 성격
        bool desert, town, coast, green, canyon, festive;
        // 정렬 순서: 바닥 0 · 장식 1 · 발자국 2 · 소품 5 · 동물 8 · 그림자 9 · 사람 10 · 나무 위 20 · 나는 새 24 · 모래 줄기 37~39
        const int OrderPrint = 2, OrderCritter = 8, OrderFlying = 24, OrderDevil = 21, OrderHaze = 37, OrderStreak = 38;

        Transform root;
        MapRoot map;
        Camera cam;

        // ---------------------------------------------------------------- 시작
        void Awake() { Current = this; }

        void Start()
        {
            map = MapRoot.Current;
            string id = map != null ? map.mapId : "";
            desert = id == "arabia_shortcut" || id == "arabia_farmroad" || id == "arabia_central_dunes";
            canyon = id == "arabia_central_canyon";
            coast = id == "arabia_jeddah" || id == "arabia_zabid";
            green = id == "arabia_central_steppe" || id == "arabia_central_westvale" || id == "arabia_central_eastvale";
            town = !desert && !canyon && !green;
            festive = id == "arabia_central_bazaar";          // 만국 장터 대광장: 밤 불꽃놀이 · 낮 꽃가루
            root = new GameObject("arabia_ambience_fx").transform;
            root.SetParent(transform, false);
            nextGust = Time.time + (desert ? Random.Range(8f, 20f) : Random.Range(30f, 70f));
            nextDevil = Time.time + Random.Range(15f, 35f);
            nextFlyover = Time.time + Random.Range(10f, 25f);
            BuildWind();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            SetPetals(true);
        }

        void Update()
        {
            if (map == null) map = MapRoot.Current;
            cam = Camera.main;
            if (map == null || cam == null || root == null) return;
            float dt = Time.deltaTime;
            UpdatePetals();
            UpdateFootprints(dt);
            UpdateGust(dt);
            UpdateDevil(dt);
            UpdateCritters(dt);
            UpdateFireworks(dt);
            UpdateLights(dt);
        }

        // ---------------------------------------------------------------- 공용 도움
        Rect View(float margin = 0f)
        {
            float h = cam.orthographicSize + margin, w = cam.orthographicSize * cam.aspect + margin;
            var c = cam.transform.position;
            return new Rect(c.x - w, c.y - h, 2 * w, 2 * h);
        }

        static Vector2 PlayerFeet => PlayerController.Instance != null ? PlayerController.Instance.Motor.Feet : new Vector2(9999, 9999);
        static bool PlayerRunning => PlayerController.Instance != null && PlayerController.Instance.Running;
        float Daylight => GameManager.I != null ? GameManager.I.Clock.Daylight : 1f;

        /// <summary>발밑 바닥 종류 (아라비아 바닥 타일 이름 arabia_t{종류}{아래}{모양}_x_y에서)</summary>
        static readonly string[] Kinds = { "sand", "dune", "dry", "grass", "red", "rock", "gravel", "road", "paving", "cobble", "boards", "bridge", "water" };
        string GroundAt(Vector2 p)
        {
            if (map == null || map.ground == null) return "";
            var t = map.ground.GetTile(map.WorldToCell(p));
            if (t == null) return "";
            string n = t.name;
            if (!n.StartsWith("arabia_t")) return "";
            foreach (var k in Kinds) if (string.CompareOrdinal(n, 8, k, 0, k.Length) == 0) return k;
            return "";
        }
        static bool Soft(string k) => k == "sand" || k == "dune";

        SpriteRenderer MakeSr(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        // ---------------------------------------------------------------- 꽃잎 끄기 (사막에 분홍 꽃잎은 어울리지 않음)
        readonly List<GameObject> petals = new List<GameObject>();
        float petalCheck;
        void UpdatePetals()
        {
            if (green || petals.Count > 0 || Time.time < petalCheck) return;
            petalCheck = Time.time + 1f;
            var fx = GameObject.Find("Ambient FX");   // 공용 AmbientFX가 만든 묶음 (코드는 건드리지 않고 이 씬에서만 꽃잎을 숨긴다)
            if (fx == null) return;
            foreach (Transform c in fx.transform)
                if (c.name.StartsWith("Petal") && c.gameObject.activeSelf) { petals.Add(c.gameObject); c.gameObject.SetActive(false); }
        }
        void SetPetals(bool on) { foreach (var p in petals) if (p != null) p.SetActive(on); petals.Clear(); }

        // ---------------------------------------------------------------- 발자국 · 달리기 먼지
        class Fade { public SpriteRenderer sr; public float life, max, a; public Vector2 vel; }
        readonly List<Fade> prints = new List<Fade>(), puffs = new List<Fade>();
        Vector2 lastPrint; bool printLeft; float puffTimer;

        void UpdateFootprints(float dt)
        {
            var pc = PlayerController.Instance;
            if (pc != null && pc.Motor.IsMoving)
            {
                Vector2 feet = pc.Motor.Feet, v = pc.Motor.Velocity;
                string g = GroundAt(feet);
                if (Soft(g) && (feet - lastPrint).sqrMagnitude > 0.42f * 0.42f)
                {
                    lastPrint = feet;
                    printLeft = !printLeft;
                    var dir = v.normalized;
                    var side = new Vector2(-dir.y, dir.x) * (printLeft ? 0.09f : -0.09f);
                    var f = Take(prints, 48, () => Art.Footprint, OrderPrint);
                    f.sr.transform.position = feet + side;
                    f.sr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
                    f.max = f.life = g == "dune" ? 9f : 7f; f.a = 0.55f; f.vel = Vector2.zero;
                }
                else if (!Soft(g)) lastPrint = feet;
                puffTimer -= dt;
                if (Soft(g) && pc.Running && puffTimer <= 0f)
                {
                    puffTimer = 0.09f;
                    for (int i = 0; i < 2; i++)
                    {
                        var p = Take(puffs, 30, () => Art.Grain, OrderCritter + 1);
                        p.sr.transform.position = feet - v.normalized * 0.2f + Random.insideUnitCircle * 0.15f;
                        p.sr.transform.localScale = Vector3.one * Random.Range(1f, 2f);
                        p.sr.transform.rotation = Quaternion.identity;
                        p.max = p.life = Random.Range(0.4f, 0.7f); p.a = 0.7f;
                        p.vel = -v.normalized * 0.4f + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.3f, 0.7f));
                    }
                }
            }
            float erase = gustLevel > 0.3f ? 4f : 1f;   // 모래바람이 불면 발자국이 빨리 지워진다
            Tick(prints, dt * erase, new Color(0.42f, 0.30f, 0.17f));
            Tick(puffs, dt, new Color(0.93f, 0.83f, 0.62f));
        }

        Fade Take(List<Fade> pool, int max, System.Func<Sprite> sprite, int order)
        {
            foreach (var f in pool) if (f.life <= 0f) { f.sr.gameObject.SetActive(true); return f; }
            if (pool.Count < max) { var n = new Fade { sr = MakeSr("fx", sprite(), order) }; pool.Add(n); return n; }
            Fade old = pool[0];
            foreach (var f in pool) if (f.life < old.life) old = f;
            return old;
        }

        static void Tick(List<Fade> pool, float dt, Color col)
        {
            foreach (var f in pool)
            {
                if (f.life <= 0f) continue;
                f.life -= dt;
                if (f.vel != Vector2.zero) f.sr.transform.position += (Vector3)(f.vel * dt);
                col.a = f.a * Mathf.Clamp01(f.life / (f.max * 0.6f));
                f.sr.color = col;
                if (f.life <= 0f) f.sr.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- 모래바람
        float nextGust, gustTime, gustLen, gustLevel;
        bool gusting;
        Vector2 wind = new Vector2(1f, -0.12f);
        SpriteRenderer haze;
        readonly List<SpriteRenderer> streaks = new List<SpriteRenderer>();
        readonly List<Vector3> streakState = new List<Vector3>();   // x,y = 화면 안 비율, z = 속도
        readonly List<SpriteRenderer> grains = new List<SpriteRenderer>();
        AudioSource windSrc;

        public void StartGust(float seconds = 9f)
        {
            if (root == null) return;
            gusting = true; gustTime = 0f; gustLen = seconds;
            wind = new Vector2(Random.value < 0.5f ? 1f : -1f, Random.Range(-0.25f, 0.1f));
            if (desert || canyon) for (int i = 0; i < (desert ? 2 : 1); i++) SpawnTumbleweed();
        }

        void UpdateGust(float dt)
        {
            if (!gusting && Time.time >= nextGust && !green)
            {
                StartGust(Random.Range(7f, 11f));
                nextGust = Time.time + (desert ? Random.Range(35f, 70f) : canyon ? Random.Range(50f, 90f) : Random.Range(80f, 150f));
            }
            float target = 0f;
            if (gusting)
            {
                gustTime += dt;
                float a = Mathf.Clamp01(gustTime / 1.8f), b = Mathf.Clamp01((gustLen - gustTime) / 2.2f);
                target = Mathf.SmoothStep(0, 1, Mathf.Min(a, b)) * (desert ? 1f : canyon ? 0.8f : 0.55f);
                if (gustTime >= gustLen) gusting = false;
            }
            gustLevel = target;
            var view = View(1f);
            // 화면을 덮는 모래빛
            if (haze == null) { haze = MakeSr("sand_haze", Art.White, OrderHaze); }
            haze.gameObject.SetActive(gustLevel > 0.01f);
            if (gustLevel > 0.01f)
            {
                haze.transform.position = view.center;
                haze.transform.localScale = new Vector3(view.width * 2f, view.height * 2f, 1f);   // 흰 그림 8px = 0.5칸
                haze.color = new Color(0.86f, 0.70f, 0.45f, 0.20f * gustLevel);
            }
            // 모래 줄기
            int want = gustLevel > 0.01f ? 70 : 0;
            while (streaks.Count < want)
            {
                streaks.Add(MakeSr("sand_streak", Art.Streak, OrderStreak));
                streakState.Add(new Vector3(Random.value, Random.value, Random.Range(0.7f, 1.4f)));
            }
            for (int i = 0; i < streaks.Count; i++)
            {
                bool on = i < want;
                streaks[i].gameObject.SetActive(on);
                if (!on) continue;
                var s = streakState[i];
                s.x = Mathf.Repeat(s.x + wind.x * 9f * s.z * dt / view.width, 1f);
                s.y = Mathf.Repeat(s.y + (wind.y * 9f * s.z + Mathf.Sin(Time.time * 3f + i) * 0.4f) * dt / view.height, 1f);
                streakState[i] = s;
                streaks[i].transform.position = new Vector3(view.xMin + s.x * view.width, view.yMin + s.y * view.height, 0);
                streaks[i].transform.localScale = new Vector3((wind.x > 0 ? 1 : -1) * (0.8f + s.z), 1, 1);
                streaks[i].color = new Color(0.95f, 0.85f, 0.62f, 0.75f * gustLevel * (0.5f + 0.5f * Mathf.Sin(i * 1.7f + Time.time * 2f)));
            }
            // 땅 위를 기는 모래 알갱이 (사막은 늘 조금, 바람 불면 많이)
            int g = desert ? 14 + (int)(40 * gustLevel) : (int)(30 * gustLevel);
            while (grains.Count < g) { var sr = MakeSr("sand_grain", Art.Grain, OrderCritter - 1); sr.transform.position = RandomIn(view); grains.Add(sr); }
            for (int i = 0; i < grains.Count; i++)
            {
                bool on = i < g;
                grains[i].gameObject.SetActive(on);
                if (!on) continue;
                var p = grains[i].transform.position;
                float sp = 0.6f + 6f * gustLevel;
                p += (Vector3)(new Vector2(wind.x, wind.y * 0.3f) * sp * dt * (0.6f + (i % 5) * 0.15f));
                p.y += Mathf.Sin(Time.time * 6f + i) * 0.05f * dt;
                if (!view.Contains(p)) p = wind.x > 0 ? new Vector3(view.xMin, Random.Range(view.yMin, view.yMax)) : new Vector3(view.xMax, Random.Range(view.yMin, view.yMax));
                grains[i].transform.position = p;
                grains[i].color = new Color(0.97f, 0.88f, 0.66f, 0.45f + 0.4f * gustLevel);
            }
            // 바람 소리
            if (windSrc != null)
            {
                float vol = AudioManager.Muted ? 0f : 0.22f * gustLevel;
                windSrc.volume = Mathf.MoveTowards(windSrc.volume, vol, dt * 0.3f);
                if (windSrc.volume > 0.001f && !windSrc.isPlaying) windSrc.Play();
                if (windSrc.volume <= 0.001f && windSrc.isPlaying) windSrc.Stop();
            }
        }

        void BuildWind()
        {
            windSrc = gameObject.AddComponent<AudioSource>();
            windSrc.clip = Art.WindClip;
            windSrc.loop = true; windSrc.playOnAwake = false; windSrc.spatialBlend = 0f; windSrc.volume = 0f;
        }

        static Vector3 RandomIn(Rect r) => new Vector3(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax), 0);

        // ---------------------------------------------------------------- 회오리 모래기둥
        float nextDevil, devilTime;
        bool devilOn;
        Vector2 devilPos, devilVel;
        readonly List<SpriteRenderer> devilBits = new List<SpriteRenderer>();

        public void StartDevil(Vector2 at)
        {
            devilOn = true; devilTime = 0f; devilPos = at;
            devilVel = new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(-0.3f, 0.3f));
            while (devilBits.Count < 42) devilBits.Add(MakeSr("dust_devil", Art.Grain, OrderDevil));
        }

        void UpdateDevil(float dt)
        {
            if (!devilOn && desert && Time.time >= nextDevil)
            {
                nextDevil = Time.time + Random.Range(45f, 95f);
                var v = View(-2f);
                var at = (Vector2)RandomIn(v);
                if (Soft(GroundAt(at)) && Daylight > 0.4f) StartDevil(at);
            }
            if (!devilOn) { foreach (var b in devilBits) b.gameObject.SetActive(false); return; }
            devilTime += dt;
            devilPos += (devilVel + new Vector2(Mathf.Sin(devilTime * 0.9f), Mathf.Cos(devilTime * 0.7f)) * 0.3f) * dt;
            float life = 12f, fade = Mathf.Clamp01(devilTime / 1.5f) * Mathf.Clamp01((life - devilTime) / 2f);
            for (int i = 0; i < devilBits.Count; i++)
            {
                var b = devilBits[i];
                b.gameObject.SetActive(true);
                float hgt = (i / (float)devilBits.Count) * 2.6f;                      // 아래는 좁고 위로 갈수록 넓다
                float r = 0.12f + hgt * 0.32f;
                float ang = devilTime * (7f - hgt) + i * 2.39f;
                b.transform.position = devilPos + new Vector2(Mathf.Cos(ang) * r + Mathf.Sin(devilTime * 2f + hgt) * 0.1f * hgt, hgt + Mathf.Sin(ang) * r * 0.25f);
                b.transform.localScale = Vector3.one * (1f + (i % 3) * 0.5f);
                b.color = new Color(0.90f, 0.78f, 0.55f, 0.75f * fade * (1f - hgt / 3.4f));
            }
            if (devilTime > life) devilOn = false;
        }

        // ---------------------------------------------------------------- 밤 불빛 · 관람차 · 분수 (2026-10-27)
        class Glow { public ArabiaFxMarker m; public Light2D light; public float phase; }
        readonly List<Glow> glows = new List<Glow>();
        bool markersRead;
        Transform ferris, ferrisWheel, ferrisBulbs; SpriteRenderer bulbsSr;
        readonly List<Transform> gondolas = new List<Transform>();
        Vector3 ferrisCenter, fountainTop;
        bool hasFountain;
        class Drop { public SpriteRenderer sr; public float u, speed; public int arc, side; }
        readonly List<Drop> drops = new List<Drop>();
        readonly List<SpriteRenderer> glints = new List<SpriteRenderer>();
        static int[] allLayers;

        void ReadMarkers()
        {
            markersRead = true;
            foreach (var m in Object.FindObjectsByType<ArabiaFxMarker>(FindObjectsSortMode.None))
            {
                if (m.kind == "light") glows.Add(new Glow { m = m, phase = Random.value * 10f });
                else if (m.kind == "ferris") BuildFerris(m.transform.position);
                else if (m.kind == "fountain") { hasFountain = true; fountainTop = m.transform.position; }
            }
        }

        static Color ColorOf(string n, float t)
        {
            switch (n)
            {
                case "amber": return new Color(1f, .64f, .32f);
                case "gold": return new Color(1f, .84f, .46f);
                case "white": return new Color(1f, .94f, .84f);
                case "violet": return new Color(.76f, .56f, 1f);
                case "pink": return new Color(1f, .56f, .76f);
                case "aqua": return new Color(.56f, .86f, 1f);
                case "fire": return new Color(1f, .56f, .26f);
                case "festive": return Color.HSVToRGB(Mathf.Repeat(t * 0.05f, 1f), .45f, 1f);
                default: return new Color(1f, .8f, .5f);
            }
        }

        /// <summary>해 질 녘부터 불이 켜진다 (0 = 낮, 1 = 한밤)</summary>
        float Night => Mathf.Clamp01((0.8f - Daylight) / 0.65f);

        void UpdateLights(float dt)
        {
            if (!markersRead) ReadMarkers();
            float night = Night, t = Time.time;
            var view = View(6f);
            if (allLayers == null)
            {
                var ls = SortingLayer.layers;
                allLayers = new int[ls.Length];
                for (int i = 0; i < ls.Length; i++) allLayers[i] = ls[i].id;
            }
            foreach (var g in glows)
            {
                if (g.m == null) continue;
                var p = g.m.transform.position;
                bool on = night > 0.02f && view.Contains(p);
                if (on && g.light == null)
                {
                    var go = new GameObject("night_light");
                    go.transform.SetParent(root, false);
                    go.transform.position = p;
                    var l = go.AddComponent<Light2D>();
                    l.lightType = Light2D.LightType.Point;
                    l.pointLightOuterRadius = g.m.radius;
                    l.pointLightInnerRadius = g.m.radius * 0.15f;
                    l.falloffIntensity = 0.65f;
                    l.shadowsEnabled = false;
                    l.targetSortingLayers = allLayers;
                    g.light = l;
                }
                if (g.light == null) continue;
                g.light.enabled = on;
                if (!on) continue;
                float f = g.m.flicker ? 0.82f + 0.18f * Mathf.PerlinNoise(t * 3f, g.phase) : 1f;
                g.light.intensity = night * 0.9f * f;
                g.light.color = ColorOf(g.m.colorName, t + g.phase);
            }
            UpdateFerris(dt, night);
            UpdateFountain(dt, night);
        }

        // 관람차: 바퀴(테·바퀴살·전구)를 천천히 돌리고, 곤돌라 8개는 늘 아래로 매달린 채 따라 돈다
        const float WheelR = 52f / 16f;

        void BuildFerris(Vector3 topLeft)
        {
            ferrisCenter = topLeft + new Vector3(64f / 16f, -60f / 16f, 0f);
            ferris = new GameObject("ferris_wheel_fx").transform;
            ferris.SetParent(root, false);
            ferris.position = ferrisCenter;
            var wheel = MakeSr("wheel", Art.Wheel, 18);
            ferrisWheel = wheel.transform; ferrisWheel.SetParent(ferris, false); ferrisWheel.localPosition = Vector3.zero;
            bulbsSr = MakeSr("bulbs", Art.WheelBulbs, 18);
            ferrisBulbs = bulbsSr.transform; ferrisBulbs.SetParent(ferris, false); ferrisBulbs.localPosition = Vector3.zero;
            for (int k = 0; k < 8; k++)
            {
                var g = MakeSr("gondola", Art.Gondola(k), 19);
                g.transform.SetParent(ferris, false);
                gondolas.Add(g.transform);
            }
            var hub = MakeSr("hub", Art.Hub, 19);
            hub.transform.SetParent(ferris, false); hub.transform.localPosition = Vector3.zero;
        }

        void UpdateFerris(float dt, float night)
        {
            if (ferris == null) return;
            float rot = -Time.time * 7.2f;                                         // 50초에 한 바퀴
            ferrisWheel.localRotation = Quaternion.Euler(0, 0, rot);
            ferrisBulbs.localRotation = ferrisWheel.localRotation;
            for (int k = 0; k < gondolas.Count; k++)
            {
                float a = (rot + k * 45f) * Mathf.Deg2Rad;
                gondolas[k].localPosition = new Vector3(Mathf.Cos(a) * WheelR, Mathf.Sin(a) * WheelR, 0);
                gondolas[k].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.3f + k) * 3f);   // 살짝 흔들림
            }
            // 전구: 낮엔 은은하게, 밤엔 번갈아 반짝인다
            float blink = (int)(Time.time * 2f) % 2 == 0 ? 1f : 0.55f;
            bulbsSr.color = new Color(1, 1, 1, Mathf.Lerp(0.6f, blink, night));
        }

        // 분수: 위 그릇 → 둘째 그릇 → 큰 수반으로 떨어지는 물방울 + 수면 반짝임
        Vector3 FountainPx(float px, float py) => fountainTop + new Vector3(px / 16f, -py / 16f, 0f);

        void UpdateFountain(float dt, float night)
        {
            if (!hasFountain) return;
            if (!View(4f).Contains(fountainTop)) return;
            while (drops.Count < 40)
            {
                var d = new Drop { sr = MakeSr("drop", Art.Grain, 21), u = Random.value, speed = Random.Range(1.1f, 1.6f), arc = drops.Count % 2, side = (drops.Count / 2) % 2 == 0 ? -1 : 1 };
                drops.Add(d);
            }
            while (glints.Count < 16) glints.Add(MakeSr("glint", Art.Grain, 21));
            const float cx = 64f;
            foreach (var d in drops)
            {
                d.u += dt * d.speed;
                if (d.u > 1f) { d.u -= 1f; d.speed = Random.Range(1.1f, 1.6f); }
                float u = d.u, px, py;
                if (d.arc == 0) { px = cx + d.side * (11 + 13 * u); py = 20 + 21 * u * u; }
                else { px = cx + d.side * (25 + 24 * u); py = 42 + 31 * u * u; }
                d.sr.transform.position = FountainPx(px, py);
                d.sr.transform.localScale = Vector3.one * (u > 0.9f ? 2f : 1.2f);               // 떨어질 때 물보라
                d.sr.color = new Color(0.82f, 0.95f, 1f, u > 0.9f ? (1f - u) * 6f : 0.9f);
            }
            for (int i = 0; i < glints.Count; i++)
            {
                float a = i * 2.39f, r = 0.25f + (i % 4) * 0.22f;
                glints[i].transform.position = FountainPx(cx + Mathf.Cos(a) * 50f * r * 1.6f, 74f + Mathf.Sin(a) * 8f * r * 1.4f);
                float tw = Mathf.Max(0f, Mathf.Sin(Time.time * 3.1f + i * 1.7f));
                glints[i].color = new Color(1f, 1f, 1f, tw * tw * (0.6f + 0.4f * night));
            }
        }

        // ---------------------------------------------------------------- 대광장: 밤 불꽃놀이 · 낮 꽃가루
        class Spark { public SpriteRenderer sr; public Vector2 p, v; public float life, max; public Color c; public bool rocket; public float burstAt; }
        readonly List<Spark> sparks = new List<Spark>();
        float nextFirework, nextConfetti;
        static readonly Color[] FireColors = { new Color(1f, .35f, .35f), new Color(1f, .85f, .3f), new Color(.4f, .9f, 1f), new Color(1f, .5f, .9f), new Color(.6f, 1f, .5f), new Color(.8f, .6f, 1f) };

        Spark NewSpark()
        {
            foreach (var s in sparks) if (s.life <= 0f) { s.sr.gameObject.SetActive(true); return s; }
            var n = new Spark { sr = MakeSr("spark", Art.Grain, OrderFlying + 2) };
            sparks.Add(n);
            return n;
        }

        /// <summary>불꽃 하나 쏘기: 꼬리를 끌며 올라가 터진다 (발표·시험용으로도 부른다)</summary>
        public void LaunchFirework(Vector2 from)
        {
            var s = NewSpark();
            s.rocket = true; s.p = from; s.v = new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(7f, 9f));
            s.max = s.life = Random.Range(0.8f, 1.1f); s.c = new Color(1f, .9f, .7f); s.burstAt = 0f;
            s.sr.transform.localScale = Vector3.one * 1.5f;
        }

        void Burst(Vector2 at)
        {
            var c = FireColors[Random.Range(0, FireColors.Length)];
            var c2 = FireColors[Random.Range(0, FireColors.Length)];
            int n = Random.Range(26, 38);
            float speed = Random.Range(3.2f, 4.6f);
            for (int i = 0; i < n; i++)
            {
                var s = NewSpark();
                float a = i / (float)n * Mathf.PI * 2f + Random.Range(-0.05f, 0.05f);
                s.rocket = false; s.p = at; s.v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.85f, 1.05f);
                s.max = s.life = Random.Range(1.2f, 1.7f); s.c = i % 3 == 0 ? c2 : c; s.burstAt = 0f;
                s.sr.transform.localScale = Vector3.one * 1.5f;
            }
        }

        void UpdateFireworks(float dt)
        {
            if (!festive) return;
            var v = View(-1f);
            bool night = Daylight < 0.45f;
            if (night && Time.time >= nextFirework)
            {
                nextFirework = Time.time + Random.Range(1.2f, 4f);
                int salvo = Random.value < 0.25f ? 3 : 1;
                for (int i = 0; i < salvo; i++) LaunchFirework(new Vector2(Random.Range(v.xMin + 2, v.xMax - 2), v.yMin + Random.Range(0f, 3f)));
            }
            if (!night && Time.time >= nextConfetti)
            {
                nextConfetti = Time.time + Random.Range(0.08f, 0.25f);                  // 낮엔 색종이 꽃가루가 흩날린다
                var s = NewSpark();
                s.rocket = false; s.p = new Vector2(Random.Range(v.xMin, v.xMax), v.yMax + 0.5f);
                s.v = new Vector2(Random.Range(-0.4f, 0.6f), Random.Range(-1.2f, -0.7f));
                s.max = s.life = Random.Range(5f, 8f); s.c = FireColors[Random.Range(0, FireColors.Length)];
                s.sr.transform.localScale = new Vector3(2f, 1f, 1f);
                s.burstAt = -1f;
            }
            for (int i = 0, count = sparks.Count; i < count; i++)                    // 도중에 불꽃이 늘어나도 안전하게 (새 것은 다음 프레임부터)
            {
                var s = sparks[i];
                if (s.life <= 0f) continue;
                s.life -= dt;
                if (s.rocket)
                {
                    s.v.y -= 4f * dt;
                    s.p += s.v * dt;
                    var tail = NewSpark();                                            // 올라가는 꼬리
                    tail.rocket = false; tail.p = s.p; tail.v = Random.insideUnitCircle * 0.2f; tail.max = tail.life = 0.35f; tail.c = new Color(1f, .8f, .5f); tail.burstAt = 0f;
                    tail.sr.transform.localScale = Vector3.one;
                    if (s.life <= 0f) Burst(s.p);
                }
                else if (s.burstAt < 0f)                                              // 꽃가루: 팔랑팔랑
                {
                    s.p += (s.v + new Vector2(Mathf.Sin(Time.time * 3f + s.max * 7f) * 0.6f, 0)) * dt;
                    s.sr.transform.localScale = new Vector3(Mathf.Abs(Mathf.Sin(Time.time * 6f + s.max * 9f)) * 2f + 0.4f, 1f, 1f);
                }
                else
                {
                    s.v *= 1f - 1.6f * dt; s.v.y -= 1.6f * dt;                         // 불꽃: 퍼지며 천천히 떨어진다
                    s.p += s.v * dt;
                }
                var col = s.c;
                col.a = s.burstAt < 0f && !s.rocket ? 0.9f * Mathf.Clamp01(s.life / 1.5f) : Mathf.Clamp01(s.life / (s.max * 0.5f));
                if (!s.rocket && s.burstAt >= 0f && s.life < s.max * 0.4f && Random.value < 0.3f) col.a *= 0.3f;   // 반짝임
                s.sr.color = col;
                s.sr.transform.position = s.p;
                if (s.life <= 0f) { s.sr.gameObject.SetActive(false); s.burstAt = 0f; }
            }
        }

        // ---------------------------------------------------------------- 동물
        enum Kind { Vulture, Circler, Dove, Lizard, Scarab, Cat, Jerboa, Gull, Tumbleweed }
        enum State { Idle, Move, Flee, Leave }
        class Critter
        {
            public Kind kind; public State state;
            public SpriteRenderer sr, shadow;
            public Vector2 pos, vel, home; public float alt, timer, anim, age, alpha, spin, flock;
            public Sprite[] idle, move;
            public bool faceRight = true;
        }
        readonly List<Critter> critters = new List<Critter>();
        float spawnTimer, nextFlyover;

        void UpdateCritters(float dt)
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0f) { spawnTimer = 1.3f; TrySpawn(); }
            if (Time.time >= nextFlyover)
            {
                nextFlyover = Time.time + Random.Range(25f, 50f);
                if (coast && Daylight > 0.3f) FlyOver(Kind.Gull);
            }
            var feet = PlayerFeet;
            var far = View(7f);
            for (int i = critters.Count - 1; i >= 0; i--)
            {
                var c = critters[i];
                c.age += dt; c.anim += dt;
                bool gone = Step(c, dt, feet) || (!far.Contains(c.pos) && c.state != State.Idle) || !View(14f).Contains(c.pos);
                if (gone) { Destroy(c.sr.gameObject); if (c.shadow != null) Destroy(c.shadow.gameObject); critters.RemoveAt(i); continue; }
                Draw(c);
            }
        }

        int Count(Kind k) { int n = 0; foreach (var c in critters) if (c.kind == k) n++; return n; }

        void TrySpawn()
        {
            bool day = Daylight > 0.35f;
            if (day && (desert || canyon) && Count(Kind.Vulture) < 2 && Random.value < 0.35f) SpawnGround(Kind.Vulture, k => k == "sand" || k == "dune" || k == "dry" || k == "red" || k == "rock");
            if (day && (desert || canyon) && Count(Kind.Circler) < (canyon ? 3 : 2) && Random.value < 0.2f) SpawnCircler();
            if (day && !green && Count(Kind.Lizard) < (desert || canyon ? 3 : 1) && Random.value < 0.4f) SpawnGround(Kind.Lizard, k => k == "sand" || k == "dune" || k == "red" || k == "rock" || k == "dry");
            if (day && desert && Count(Kind.Scarab) < 2 && Random.value < 0.3f) SpawnGround(Kind.Scarab, Soft);
            if (day && (town || green) && Count(Kind.Dove) < 9 && Random.value < 0.3f) SpawnFlock();
            if (town && Count(Kind.Cat) < 2 && Random.value < 0.25f) SpawnGround(Kind.Cat, k => k == "paving" || k == "cobble" || k == "sand" || k == "road");
            if (!day && (desert || town) && Count(Kind.Jerboa) < 2 && Random.value < 0.35f) SpawnGround(Kind.Jerboa, Soft);
        }

        /// <summary>화면 안, 플레이어에게서 떨어진 걸을 수 있는 칸 (갑자기 눈앞에 나타나지 않게)</summary>
        bool FindSpot(System.Func<string, bool> ok, out Vector2 at)
        {
            var v = View(-0.5f);
            var feet = PlayerFeet;
            for (int t = 0; t < 14; t++)
            {
                at = RandomIn(v);
                var cell = map.WorldToCell(at);
                at = map.CellCenter(cell);
                if ((at - feet).sqrMagnitude < 6f * 6f) continue;
                if (!map.IsWalkable(cell) || MapExit.At(cell) != null) continue;
                if (map.road != null && map.road.GetTile(cell) != null && !ok("road")) continue;
                if (!ok(GroundAt(at))) continue;
                bool crowded = false;
                foreach (var c in critters) if ((c.pos - at).sqrMagnitude < 4f) { crowded = true; break; }
                if (crowded) continue;
                return true;
            }
            at = default;
            return false;
        }

        Critter Make(Kind k, Vector2 at, Sprite[] idle, Sprite[] move, bool shadow)
        {
            var c = new Critter { kind = k, pos = at, home = at, idle = idle, move = move, alpha = 0f, timer = Random.Range(1f, 3f) };
            c.sr = MakeSr(k.ToString(), idle[0], OrderCritter);
            if (shadow) { c.shadow = MakeSr("shadow", Art.Shadow, OrderPrint); }
            c.faceRight = Random.value < 0.5f;
            critters.Add(c);
            return c;
        }

        void SpawnGround(Kind k, System.Func<string, bool> ok)
        {
            if (!FindSpot(ok, out var at)) return;
            switch (k)
            {
                case Kind.Vulture: Make(k, at, Art.VulturePerch, Art.VultureFly, true); break;
                case Kind.Lizard: Make(k, at, Art.Lizard, Art.Lizard, false); break;
                case Kind.Scarab: Make(k, at, Art.Scarab, Art.Scarab, false); break;
                case Kind.Cat: Make(k, at, Art.Cat, Art.Cat, false); break;
                case Kind.Jerboa: Make(k, at, Art.Jerboa, Art.Jerboa, true); break;
            }
        }

        void SpawnFlock()
        {
            if (!FindSpot(k => k != "water" && k != "", out var at)) return;
            int n = Random.Range(3, 6);
            float id = Random.value;
            for (int i = 0; i < n; i++)
            {
                var p = at + Random.insideUnitCircle * 0.9f;
                if (!map.IsWalkable(map.WorldToCell(p))) continue;
                var c = Make(Kind.Dove, p, Art.DoveStand, Art.DoveFly, true);
                c.flock = id;
            }
        }

        void SpawnCircler()
        {
            var v = View(-1f);
            var c = Make(Kind.Circler, RandomIn(v), Art.VultureFly, Art.VultureFly, true);
            c.home = c.pos; c.alt = Random.Range(5.5f, 7.5f); c.spin = Random.Range(0f, 6.28f);
            c.timer = Random.Range(18f, 30f);
            c.sr.sortingOrder = OrderFlying;
        }

        void FlyOver(Kind k)
        {
            var v = View(2f);
            bool ltr = Random.value < 0.5f;
            var c = Make(k, new Vector2(ltr ? v.xMin : v.xMax, Random.Range(v.yMin + 2, v.yMax - 2)), Art.Gull, Art.Gull, true);
            c.state = State.Leave; c.alt = 5f; c.vel = new Vector2(ltr ? 3.2f : -3.2f, Random.Range(-0.4f, 0.4f)); c.sr.sortingOrder = OrderFlying;
        }

        void SpawnTumbleweed()
        {
            var v = View(1f);
            var c = Make(Kind.Tumbleweed, new Vector2(wind.x > 0 ? v.xMin : v.xMax, Random.Range(v.yMin + 1, v.yMax - 1)), Art.Tumbleweed, Art.Tumbleweed, true);
            c.state = State.Leave; c.vel = new Vector2(wind.x * Random.Range(3.5f, 5f), Random.Range(-0.3f, 0.3f));
        }

        /// <summary>시험·발표용: 지금 화면에 모래바람·모래기둥·독수리·도마뱀을 한꺼번에 보여 준다.</summary>
        public void Showcase()
        {
            if (map == null) return;
            StartGust(10f);
            var feet = PlayerFeet;
            StartDevil(feet + new Vector2(-4f, 2f));
            foreach (var (k, off) in new[] { (Kind.Vulture, new Vector2(3.5f, 1.5f)), (Kind.Vulture, new Vector2(5f, -1f)), (Kind.Lizard, new Vector2(-2.5f, -2f)), (Kind.Scarab, new Vector2(2f, -2.5f)) })
            {
                var at = feet + off;
                if (!map.IsWalkable(map.WorldToCell(at))) continue;
                if (k == Kind.Vulture) Make(k, at, Art.VulturePerch, Art.VultureFly, true).alpha = 1f;
                else if (k == Kind.Lizard) Make(k, at, Art.Lizard, Art.Lizard, false).alpha = 1f;
                else Make(k, at, Art.Scarab, Art.Scarab, false).alpha = 1f;
            }
            SpawnCircler();
        }

        public int CritterCount => critters.Count;
        public int LitCount { get { int n = 0; foreach (var g in glows) if (g.light != null && g.light.enabled) n++; return n; } }
        public bool FerrisTurning => ferris != null;
        public bool Gusting => gustLevel > 0.05f;

        /// <summary>한 마리 움직임. true를 돌려주면 사라진다.</summary>
        bool Step(Critter c, float dt, Vector2 feet)
        {
            Vector2 d = c.pos - feet;
            float dist = d.magnitude;
            float scare = PlayerRunning ? 1.6f : 1f;
            c.alpha = c.state == State.Leave && c.age > 0.5f && c.kind != Kind.Gull && c.kind != Kind.Tumbleweed && c.kind != Kind.Circler
                ? Mathf.MoveTowards(c.alpha, 0f, dt * 0.6f) : Mathf.MoveTowards(c.alpha, 1f, dt * 1.6f);
            switch (c.kind)
            {
                case Kind.Vulture:
                    if (c.state == State.Idle)
                    {
                        c.faceRight = d.x < 0;                                  // 다가오는 사람을 바라본다
                        if (dist < 3.6f * scare) { c.state = State.Flee; c.vel = (d.normalized + Vector2.up * 0.3f).normalized * 3.4f; c.timer = 0; }
                        return false;
                    }
                    c.timer += dt;
                    c.alt = Mathf.Min(7f, c.alt + dt * (c.timer < 0.4f ? 1.2f : 2.6f));
                    c.pos += c.vel * dt * Mathf.Clamp01(0.3f + c.timer);
                    if (c.alt > 0.8f) c.sr.sortingOrder = OrderFlying;
                    if (c.timer > 2.5f) c.state = State.Leave;
                    return c.timer > 7f || (c.state == State.Leave && c.alpha <= 0.01f);
                case Kind.Circler:
                    c.spin += dt * 0.45f;
                    c.timer -= dt;
                    if (c.timer < 0) c.home += new Vector2(2.5f, 0.8f) * dt;                // 맴돌다가 다른 곳으로
                    c.pos = c.home + new Vector2(Mathf.Cos(c.spin) * 3.2f, Mathf.Sin(c.spin) * 1.8f);
                    c.vel = new Vector2(-Mathf.Sin(c.spin), Mathf.Cos(c.spin) * 0.56f);
                    return c.timer < -12f;
                case Kind.Dove:
                    if (c.state == State.Idle || c.state == State.Move)
                    {
                        if (dist < 2.6f * scare) { Scatter(c.flock, feet); return false; }
                        c.timer -= dt;
                        if (c.state == State.Move)
                        {
                            c.pos += c.vel * dt; c.alt = Mathf.Sin(Mathf.Clamp01(c.timer / 0.25f) * Mathf.PI) * 0.15f;
                            if (c.timer <= 0) { c.state = State.Idle; c.alt = 0; c.timer = Random.Range(0.6f, 2.5f); }
                        }
                        else if (c.timer <= 0)
                        {
                            var hop = Random.insideUnitCircle.normalized * 0.35f;
                            if (map.IsWalkable(map.WorldToCell(c.pos + hop)) && (c.pos + hop - c.home).sqrMagnitude < 2.5f)
                            { c.state = State.Move; c.vel = hop / 0.25f; c.timer = 0.25f; c.faceRight = hop.x > 0; }
                            else c.timer = Random.Range(0.4f, 1.5f);
                        }
                        return false;
                    }
                    c.timer += dt;
                    c.alt += dt * 2.4f; c.pos += c.vel * dt;
                    c.sr.sortingOrder = OrderFlying;
                    if (c.timer > 1.6f) c.state = State.Leave;
                    return c.timer > 5f || (c.state == State.Leave && c.alpha <= 0.01f);
                case Kind.Lizard:
                case Kind.Jerboa:
                    {
                        bool liz = c.kind == Kind.Lizard;
                        if (c.state != State.Flee && c.state != State.Leave && dist < (liz ? 2.8f : 3.2f) * scare)
                        {
                            var away = d.normalized; away = Rotate(away, Random.Range(-35f, 35f));
                            c.state = State.Flee; c.vel = away * (liz ? 8.5f : 6f); c.timer = liz ? 0.55f : 0.9f;
                            return false;
                        }
                        c.timer -= dt;
                        if (c.state == State.Flee || c.state == State.Move)
                        {
                            var next = c.pos + c.vel * dt;
                            if (map.IsWalkable(map.WorldToCell(next))) c.pos = next; else c.timer = 0;
                            if (!liz) c.alt = Mathf.Abs(Mathf.Sin(c.timer * 14f)) * 0.25f;   // 날쥐는 깡충깡충
                            if (c.timer <= 0) { c.alt = 0; if (c.state == State.Flee) { c.state = State.Leave; c.timer = 0; } else { c.state = State.Idle; c.timer = Random.Range(1.5f, 5f); } }
                        }
                        else if (c.state == State.Idle && c.timer <= 0)
                        {
                            var dir = Random.insideUnitCircle.normalized;
                            c.state = State.Move; c.vel = dir * (liz ? 5f : 3f); c.timer = Random.Range(0.15f, 0.4f);
                        }
                        return c.state == State.Leave && c.alpha <= 0.01f;                 // 굴로 숨는다
                    }
                case Kind.Scarab:
                    c.timer -= dt;
                    if (c.timer <= 0) { c.vel = Random.insideUnitCircle.normalized * 0.35f; c.timer = Random.Range(1.5f, 4f); }
                    {
                        var next = c.pos + c.vel * dt;
                        if (map.IsWalkable(map.WorldToCell(next)) && Soft(GroundAt(next))) c.pos = next; else c.vel = -c.vel;
                    }
                    return false;
                case Kind.Cat:
                    if (dist < 4f) c.faceRight = d.x < 0;
                    return false;
                case Kind.Gull:
                case Kind.Tumbleweed:
                    c.pos += c.vel * dt;
                    if (c.kind == Kind.Tumbleweed)
                    {
                        c.spin -= c.vel.x * dt * 160f;
                        c.alt = Mathf.Abs(Mathf.Sin(c.age * 4.2f)) * 0.45f;
                        c.vel.y += Mathf.Sin(c.age * 1.3f) * 0.3f * dt;
                    }
                    return c.age > 25f;
            }
            return false;
        }

        void Scatter(float flock, Vector2 feet)
        {
            foreach (var o in critters)
                if (o.kind == Kind.Dove && Mathf.Approximately(o.flock, flock) && (o.state == State.Idle || o.state == State.Move))
                {
                    var away = (o.pos - feet).normalized;
                    o.vel = Rotate(away, Random.Range(-30f, 30f)) * Random.Range(3f, 4.2f);
                    o.state = State.Flee; o.timer = Random.Range(-0.15f, 0f);
                }
        }

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        void Draw(Critter c)
        {
            bool flying = c.alt > 0.3f && (c.kind == Kind.Vulture || c.kind == Kind.Dove || c.kind == Kind.Circler || c.kind == Kind.Gull);
            Sprite[] set = flying || c.kind == Kind.Circler || c.kind == Kind.Gull ? c.move : c.idle;
            int f = 0;
            switch (c.kind)
            {
                case Kind.Vulture: f = flying ? (int)(c.anim * (c.alt < 3f ? 10f : 5f)) % 2 : ((int)(c.anim / 1.6f) % 3 == 0 ? 1 : 0); break;   // 앉아서 가끔 고개를 숙인다
                case Kind.Circler: f = (int)(c.anim * 3f) % 7 == 0 ? 0 : 1; break;              // 대부분 활공, 가끔 날갯짓
                case Kind.Dove: f = flying ? (int)(c.anim * 12f) % 2 : (c.state == State.Idle && (int)(c.anim * 2.5f) % 4 == 0 ? 1 : 0); break;   // 모이 쪼기
                case Kind.Gull: f = (int)(c.anim * 4f) % 2; break;
                case Kind.Lizard: case Kind.Jerboa: f = c.state == State.Idle ? 0 : (int)(c.anim * 14f) % 2; break;
                case Kind.Scarab: f = (int)(c.anim * 6f) % 2; break;
                case Kind.Cat: f = (int)(c.anim / 0.9f) % 4 == 0 ? 1 : 0; break;                 // 꼬리 살랑
            }
            c.sr.sprite = set[Mathf.Clamp(f, 0, set.Length - 1)];
            c.sr.transform.position = c.pos + new Vector2(0, c.alt);
            // 방향: 위에서 본 그림(나는 새·도마뱀·쇠똥구리·회전초)은 진행 방향으로 돌리고, 옆모습은 좌우만 뒤집는다
            float rot = 0f;
            bool topDown = flying || c.kind == Kind.Circler || c.kind == Kind.Gull || c.kind == Kind.Lizard || c.kind == Kind.Scarab;
            if (c.kind == Kind.Tumbleweed) rot = c.spin;
            else if (topDown && c.vel.sqrMagnitude > 0.001f)
                rot = Mathf.Atan2(c.vel.y, c.vel.x) * Mathf.Rad2Deg - (c.kind == Kind.Lizard ? 0f : 90f);
            else if (topDown && c.kind == Kind.Lizard) rot = c.faceRight ? 0 : 180;
            c.sr.transform.rotation = Quaternion.Euler(0, 0, rot);
            if (!topDown && c.kind != Kind.Tumbleweed)
            {
                if (c.vel.sqrMagnitude > 0.01f && c.kind != Kind.Vulture) c.faceRight = c.vel.x > 0;
                c.sr.flipX = c.kind == Kind.Jerboa ? c.faceRight : !c.faceRight;   // 날쥐 그림만 왼쪽을 본다
            }
            else c.sr.flipX = false;
            float scale = flying ? 1f + Mathf.Min(c.alt, 7f) * 0.04f : 1f;
            c.sr.transform.localScale = Vector3.one * scale;
            var col = Color.white;
            if (Daylight < 0.5f) col = Color.Lerp(new Color(0.55f, 0.6f, 0.85f), Color.white, Daylight * 2f);
            col.a = c.alpha;
            c.sr.color = col;
            if (c.shadow != null)
            {
                c.shadow.transform.position = c.pos + new Vector2(0, -0.05f);
                float s = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(c.alt / 7f)) * (c.kind == Kind.Dove || c.kind == Kind.Jerboa ? 0.5f : c.kind == Kind.Tumbleweed ? 0.8f : 1.1f);
                c.shadow.transform.localScale = new Vector3(s, s, 1);
                c.shadow.color = new Color(0, 0, 0, (c.alt > 0.3f ? 0.16f : 0.22f) * c.alpha * Daylight);
            }
        }

        // ================================================================ 코드로 그린 픽셀 그림 (16px = 1칸)
        static class Art
        {
            static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

            static Sprite Make(string key, string[] rows, Dictionary<char, Color32> pal, Vector2 pivot)
            {
                if (cache.TryGetValue(key, out var s) && s != null) return s;
                int h = rows.Length, w = rows[0].Length;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[w * h];
                for (int r = 0; r < h; r++)
                    for (int x = 0; x < w; x++)
                    {
                        char ch = x < rows[r].Length ? rows[r][x] : '.';
                        px[(h - 1 - r) * w + x] = pal.TryGetValue(ch, out var c) ? c : new Color32(0, 0, 0, 0);
                    }
                t.SetPixels32(px); t.Apply();
                s = Sprite.Create(t, new Rect(0, 0, w, h), pivot, 16f);
                s.name = "arabia_fx_" + key;
                cache[key] = s;
                return s;
            }

            static readonly Vector2 Bottom = new Vector2(0.5f, 0.1f), Mid = new Vector2(0.5f, 0.5f);

            static Dictionary<char, Color32> P(params object[] kv)
            {
                var d = new Dictionary<char, Color32>();
                for (int i = 0; i < kv.Length; i += 2) d[(char)kv[i]] = (Color32)kv[i + 1];
                return d;
            }
            static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

            static readonly Dictionary<char, Color32> Bird = P('K', C(40, 28, 22), 'B', C(74, 52, 38), 'b', C(116, 84, 58), 'W', C(232, 220, 200),
                                                                 'P', C(214, 150, 130), 'Y', C(230, 190, 90));
            public static Sprite[] VulturePerch => new[]
            {
                Make("vulture_a", new[] {
                    "................", "..........KKK...", ".........KPPPK..", ".........KPKPYY.", "..........KPPK..",
                    "......WWWWWWK...", ".....KBBBBBBBK..", "....KBbbbbbbBBK.", "...KBbbbbbbbBBK.", "..KBBbbbbbbBBK..",
                    ".KBBBBBBBBBBK...", "KBBK.KBBBBBK....", ".K.....Y..Y.....", "......YY.YY....." }, Bird, Bottom),
                Make("vulture_b", new[] {
                    "................", "................", "..........KKK...", ".........KPPPK..", ".........KPKPYY.",
                    "......WWWWKPPK..", ".....KBBBBBBBK..", "....KBbbbbbbBBK.", "...KBbbbbbbbBBK.", "..KBBbbbbbbBBK..",
                    ".KBBBBBBBBBBK...", "KBBK.KBBBBBK....", ".K.....Y..Y.....", "......YY.YY....." }, Bird, Bottom),
            };
            public static Sprite[] VultureFly => new[]
            {
                Make("vulture_up", new[] {
                    "BB................BB", ".BBB............BBB.", "..BBBB...KK...BBBB..", "...BBBBBBPPBBBBBB...", "....BBBBBPYBBBBB....",
                    "......BBBBBBBB......", ".......BBBBBB.......", "........BbbB........", ".........bb........." }, Bird, Mid),
                Make("vulture_down", new[] {
                    "....................", "....................", ".........KK.........", ".....BBBBPPBBBB.....", "...BBBBBBPYBBBBBB...",
                    ".BBBBB..BBBB..BBBBB.", "BBB.....BBBB.....BBB", "BB......BbbB......BB", ".........bb........." }, Bird, Mid),
            };

            static readonly Dictionary<char, Color32> DoveP = P('G', C(168, 174, 184), 'g', C(118, 126, 138), 'K', C(30, 30, 36), 'Y', C(220, 170, 90), 'R', C(200, 90, 80));
            public static Sprite[] DoveStand => new[]
            {
                Make("dove_a", new[] { "....GG..", "...GKGY.", "..gGGG..", ".gggGGG.", "ggGGGGG.", "..GGGG..", "...R.R.." }, DoveP, Bottom),
                Make("dove_b", new[] { "........", "........", "..gGGGG.", ".gggGGGG", "ggGGGKGY", "..GGGG..", "...R.R.." }, DoveP, Bottom),
            };
            public static Sprite[] DoveFly => new[]
            {
                Make("dove_up", new[] { "g......g", "gg....gg", ".gGGGGg.", "...GG...", "...gg..." }, DoveP, Mid),
                Make("dove_down", new[] { "........", "..gGGg..", "ggGGGGgg", "g..GG..g", "...gg..." }, DoveP, Mid),
            };

            static readonly Dictionary<char, Color32> LizP = P('L', C(186, 146, 90), 'l', C(124, 96, 58), 'H', C(200, 164, 104), 'K', C(40, 30, 20));
            public static Sprite[] Lizard => new[]
            {
                Make("lizard_a", new[] { "...l..l...", "LLLLLLLLHK", "...l..l..." }, LizP, Mid),
                Make("lizard_b", new[] { "..l....l..", "LLLLLLLLHK", "....ll...." }, LizP, Mid),
            };

            static readonly Dictionary<char, Color32> BugP = P('K', C(20, 24, 26), 'G', C(36, 104, 100), 'g', C(96, 176, 156));
            public static Sprite[] Scarab => new[]
            {
                Make("scarab_a", new[] { "..K..", ".KGK.", "KGgGK", "KGgGK", ".K.K." }, BugP, Mid),
                Make("scarab_b", new[] { "..K..", ".KGK.", "KGgGK", "KGgGK", "K...K" }, BugP, Mid),
            };

            static readonly Dictionary<char, Color32> CatP = P('O', C(214, 140, 60), 'o', C(166, 98, 40), 'K', C(40, 30, 24), 'P', C(232, 150, 150));
            public static Sprite[] Cat => new[]
            {
                Make("cat_a", new[] { "O...O...", "OO.OO...", "OOOOO...", "OKOKO...", ".OPO....", ".OOO...O", "OOOOO..O", "OoOoO.O.", "OOOOOO.." }, CatP, Bottom),
                Make("cat_b", new[] { "O...O...", "OO.OO...", "OOOOO...", "OKOKO...", ".OPO....", ".OOO....", "OOOOO...", "OoOoO.OO", "OOOOOOO." }, CatP, Bottom),
            };

            static readonly Dictionary<char, Color32> JerP = P('J', C(212, 176, 126), 'E', C(236, 196, 176), 'K', C(30, 24, 20), 'w', C(245, 236, 220));
            public static Sprite[] Jerboa => new[]
            {
                Make("jerboa_a", new[] { "..EE.....", "..EJ.....", ".JJJJ....", "JKJJJJ...", ".JwJJJ...", "...JJ..JJ", "..JJ.....", }, JerP, Bottom),
                Make("jerboa_b", new[] { "..EE.....", "..EJ.....", ".JJJJ....", "JKJJJJ...", ".JwJJJJJJ", "...JJ....", "....JJ...", }, JerP, Bottom),
            };

            static readonly Dictionary<char, Color32> GullP = P('W', C(244, 244, 240), 'g', C(150, 156, 164), 'K', C(232, 170, 60));
            public static Sprite[] Gull => new[]
            {
                Make("gull_a", new[] { "....KK....", "....WW....", ".gWW..WWg.", "gW......Wg" }, GullP, Mid),
                Make("gull_b", new[] { "....KK....", "....WW....", "gWWWWWWWWg", ".........." }, GullP, Mid),
            };

            public static Sprite[] Tumbleweed
            {
                get
                {
                    if (cache.TryGetValue("tumble", out var s) && s != null) return new[] { s };
                    var rows = new string[10];
                    var rng = new System.Random(7);
                    for (int y = 0; y < 10; y++)
                    {
                        var ch = new char[10];
                        for (int x = 0; x < 10; x++)
                        {
                            float dx = x - 4.5f, dy = y - 4.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                            ch[x] = r > 4.8f ? '.' : r > 3.6f ? (rng.NextDouble() < 0.75 ? 'T' : '.') : (rng.NextDouble() < 0.35 ? (rng.NextDouble() < 0.5 ? 't' : 'T') : '.');
                        }
                        rows[y] = new string(ch);
                    }
                    return new[] { Make("tumble", rows, P('T', C(150, 110, 62), 't', C(196, 156, 96)), Mid) };
                }
            }

            public static Sprite Footprint => Make("print", new[] { ".KK", "KKK", "KK.", ".K." }, P('K', C(255, 255, 255)), Mid);
            public static Sprite Grain => Make("grain", new[] { "K" }, P('K', C(255, 255, 255)), Mid);
            public static Sprite Streak => Make("streak", new[] { "KKKKKK" }, P('K', C(255, 255, 255)), Mid);
            public static Sprite White => Make("white", new[] { "KKKKKKKK", "KKKKKKKK", "KKKKKKKK", "KKKKKKKK", "KKKKKKKK", "KKKKKKKK", "KKKKKKKK", "KKKKKKKK" }, P('K', C(255, 255, 255)), Mid);

            // ---------------- 관람차 바퀴 (테·바퀴살 / 전구 / 곤돌라 / 축) ----------------
            static Sprite Disc(string key, System.Func<int, int, Color32> f, int size)
            {
                if (cache.TryGetValue(key, out var s) && s != null) return s;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++) px[y * size + x] = f(x, y);
                t.SetPixels32(px); t.Apply();
                s = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
                s.name = "arabia_fx_" + key;
                cache[key] = s;
                return s;
            }

            static readonly Color32 Clear = new Color32(0, 0, 0, 0), Out = new Color32(52, 40, 28, 255);
            static readonly Color32 Gold1 = new Color32(200, 150, 44, 255), Gold2 = new Color32(236, 196, 80, 255), Gold3 = new Color32(252, 230, 150, 255);
            static readonly Color32[] Fest = { new Color32(212, 58, 66, 255), new Color32(240, 196, 70, 255), new Color32(54, 150, 160, 255),
                                               new Color32(230, 120, 160, 255), new Color32(96, 170, 80, 255), new Color32(120, 90, 190, 255) };

            public static Sprite Wheel => Disc("wheel", (x, y) =>
            {
                float dx = x + .5f - 56f, dy = y + .5f - 56f, r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 53f) return Clear;
                if (r > 52f) return Out;
                if (r > 50.5f) return Gold2;
                if (r > 49.5f) return Gold3;
                if (r > 48.5f) return Out;
                if (r > 44f && r < 45f) return Gold1;                                   // 안쪽 테
                float ang = Mathf.Atan2(dy, dx) * 16f / (2f * Mathf.PI);
                float near = Mathf.Abs(ang - Mathf.Round(ang)) * r * 2f * Mathf.PI / 16f;  // 가장 가까운 바퀴살까지 거리(px)
                if (near < 0.6f && r > 5f) return ((int)r % 6 == 0) ? Gold2 : new Color32(232, 222, 202, 255);
                return Clear;
            }, 112);

            public static Sprite WheelBulbs => Disc("wheel_bulbs", (x, y) =>
            {
                float dx = x + .5f - 56f, dy = y + .5f - 56f, r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < 49.6f || r > 51.4f) return Clear;
                float a = Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI), 1f) * 32f;
                if (Mathf.Abs(a - Mathf.Round(a)) * r * 2f * Mathf.PI / 32f > 1.1f) return Clear;
                return Fest[(int)Mathf.Round(a) % Fest.Length];
            }, 112);

            public static Sprite Hub => Disc("wheel_hub", (x, y) =>
            {
                float dx = x + .5f - 6f, dy = y + .5f - 6f, r = Mathf.Sqrt(dx * dx + dy * dy);
                return r > 5.5f ? Clear : r > 4.5f ? Out : (dx < 0 && dy > 0) ? Gold3 : Gold2;
            }, 12);

            /// <summary>곤돌라 (13×14, 위 가운데가 매달리는 점): 지붕 · 창 · 몸통</summary>
            public static Sprite Gondola(int k)
            {
                string key = "gondola_" + k;
                if (cache.TryGetValue(key, out var s) && s != null) return s;
                var rows = new[] {
                    "......K......", "......K......", ".....KKK.....", "....KCCCK....", "...KCCCCCK...", "..KCCCCCCCK..",
                    ".KKKKKKKKKKK.", ".KCWWWWWWWCK.", ".KCWWWWWWWCK.", ".KCWWWWWWWCK.", ".KCCCCCCCCCK.", ".KCCCCCCCCCK.", ".KKKKKKKKKKK.", "............." };
                var c = Fest[k % Fest.Length];
                return Make(key, rows, P('K', Out, 'C', c, 'W', new Color32(190, 226, 242, 255)), new Vector2(0.5f, 1f));
            }

            public static Sprite Shadow => Make("shadow", new[] { "..KKKK..", ".KKKKKK.", "KKKKKKKK", ".KKKKKK.", "..KKKK.." }, P('K', C(255, 255, 255)), Mid);

            static AudioClip wind;
            /// <summary>바람 소리: 낮게 거른 잡음 + 천천히 오르내리는 세기 (4초 반복, 에셋 없이 만든다)</summary>
            public static AudioClip WindClip
            {
                get
                {
                    if (wind != null) return wind;
                    const int rate = 22050, len = rate * 4;
                    var data = new float[len];
                    var rng = new System.Random(11);
                    float lp = 0f, lp2 = 0f;
                    for (int i = 0; i < len; i++)
                    {
                        float n = (float)(rng.NextDouble() * 2 - 1);
                        float t = i / (float)len;
                        float cut = 0.02f + 0.035f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 2f));   // 휘이잉 오르내림
                        lp += (n - lp) * cut; lp2 += (lp - lp2) * cut;
                        float env = 0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 2f);
                        data[i] = lp2 * 6f * env;
                    }
                    // 이음매가 튀지 않게 앞뒤를 겹친다
                    int x = rate / 4;
                    for (int i = 0; i < x; i++) { float a = i / (float)x; data[i] = data[i] * a + data[len - x + i] * (1 - a); }
                    wind = AudioClip.Create("arabia_wind", len - x, 1, rate, false);
                    var trimmed = new float[len - x];
                    System.Array.Copy(data, trimmed, len - x);
                    wind.SetData(trimmed, 0);
                    return wind;
                }
            }
        }
    }
}
