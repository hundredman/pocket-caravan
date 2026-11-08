using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 고려 나루 낚시 (2026-11-08 은샘). 예성 나루·버들나루·남동 포구 물가의 낚싯대에서 Space.
    /// 1) 던지기: 물속 그림자(작은·중간·큰 = 흔함·가끔·드묾)를 보고 힘 막대가 그림자 위에 올 때 Space.
    /// 2) 찌 보기: 톡톡 건드리는 속임 사이에 찌가 쑥 잠기면 Space. 너무 일찍 채면 그 물고기는 달아난다.
    /// 3) 줄다리기: Space를 누르고 있으면 감기(긴장 ↑), 떼면 풀기(긴장 ↓). 긴장을 초록 칸에 두면 잡기 막대가 찬다.
    ///    긴장이 끝까지 차면 줄이 끊기고, 잡기 막대가 비면 물고기가 빠진다.
    /// 잡은 물고기는 짐(장터 품목)으로 받는다. 낚거나 줄이 끊기면 그 자리 물고기가 흩어져 게임 1시간 뒤 다시 모인다
    /// (북유럽 얼음 구멍과 같은 기준). 입질만 놓치면 바로 다시 던진다.
    /// 물때: 하구·바다는 밀물(6~9시·18~21시), 강은 새벽·해질녘(5~8시·17~20시)에 큰 물고기가 잦다. 세상 시간이라 온라인에서 모두 같다.
    /// </summary>
    public static class GoryeoFishingConfig
    {
        [Serializable] public class Spot { public string mapId, name, water, kind; public int x, y; public string[] fish; }
        [Serializable] public class Species { public string id, name; public int tier; public int minCm, maxCm; }
        [Serializable] public class Data { public string status; public float scatterHours = 1f; public Spot[] spots; public Species[] species; }
        static Data data; static bool tried;
        public static Data Get()
        {
            if (data == null && !tried)
            {
                tried = true;
                var asset = Resources.Load<TextAsset>("Goryeo/Fishing/Config");
                if (asset != null) { var d = JsonUtility.FromJson<Data>(asset.text); if (d != null && d.status == "approved" && d.spots != null && d.species != null) data = d; }
            }
            return data;
        }
        public static Species Find(string id) { var d = Get(); return d == null ? null : Array.Find(d.species, s => s.id == id); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { data = null; tried = false; }
    }

    /// <summary>낚싯대 자리를 승인된 맵에만 놓는다. 장면 파일은 바꾸지 않는다.</summary>
    public static class GoryeoFishingDirector
    {
        static Sprite rod, shadow;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Apply();
            ActivityHints.Rules[typeof(GoryeoFishingSpot)] = i => ("물가 낚시", !((GoryeoFishingSpot)i).Ready);
        }
        static void OnLoaded(Scene scene, LoadSceneMode mode) => Apply();
        public static void Apply()
        {
            var map = MapRoot.Current; var cfg = GoryeoFishingConfig.Get();
            if (map == null || cfg == null || map.GetComponentInChildren<GoryeoFishingSpot>() != null) return;
            foreach (var s in cfg.spots)
            {
                if (s == null || s.mapId != map.mapId) continue;
                var cell = new Vector3Int(s.x, s.y, 0);
                var go = new GameObject("물가 낚싯대 (" + s.name + ")");
                go.transform.SetParent(map.transform, false);
                go.transform.position = map.CellCenter(cell);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = RodSprite(); sr.sortingOrder = 5;
                if (s.water == "left") sr.flipX = true;
                var spot = go.AddComponent<GoryeoFishingSpot>();
                spot.config = s; spot.waterDir = Dir(s.water); spot.shadowSprite = ShadowSprite();
            }
        }
        static Vector2Int Dir(string w) => w == "left" ? Vector2Int.left : w == "up" ? Vector2Int.up : w == "down" ? Vector2Int.down : Vector2Int.right;
        static Sprite RodSprite()
        {
            if (rod != null) return rod;
            var tex = Resources.Load<Texture2D>("Goryeo/Fishing/rod");
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point;
            // 받침대 발이 칸 가운데, 낚싯대 끝은 물 위로
            return rod = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(8.5f / tex.width, 1.5f / tex.height), 16f);
        }
        static Sprite ShadowSprite()
        {
            if (shadow != null) return shadow;
            var tex = new Texture2D(12, 6, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[12 * 6];
            for (int y = 0; y < 6; y++) for (int x = 0; x < 12; x++)
            {
                float dx = (x + .5f - 5.5f) / 5.5f, dy = (y + .5f - 3f) / 2.6f;
                bool tail = x >= 10 && Mathf.Abs(y + .5f - 3f) <= (x - 9) * .9f;
                px[y * 12 + x] = dx * dx + dy * dy <= 1f || tail ? new Color32(18, 40, 52, 255) : new Color32(0, 0, 0, 0);
            }
            tex.SetPixels32(px); tex.Apply();
            return shadow = Sprite.Create(tex, new Rect(0, 0, 12, 6), new Vector2(.5f, .5f), 16f);
        }
    }

    /// <summary>물가 낚싯대. 물속에 보이는 그림자가 곧 "지금 낚을 수 있음"이다.</summary>
    public sealed class GoryeoFishingSpot : Interactable
    {
        public GoryeoFishingConfig.Spot config;
        public Vector2Int waterDir = Vector2Int.right;
        public Sprite shadowSprite;
        static readonly Dictionary<string, float> scatteredUntil = new Dictionary<string, float>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll() => scatteredUntil.Clear();

        public override string DisplayName => "낚싯대";
        public override string ActionVerb => "낚시하기";
        public string Key => config != null ? config.mapId : "";
        float Now => GameManager.I != null ? GameManager.I.Clock.TotalHours : 0f;
        public float WaitHours => scatteredUntil.TryGetValue(Key, out var t) ? Mathf.Max(0f, t - Now) : 0f;
        public bool Ready => WaitHours <= 0f;
        public void Scatter() { var cfg = GoryeoFishingConfig.Get(); scatteredUntil[Key] = Now + (cfg != null ? Mathf.Max(0.1f, cfg.scatterHours) : 1f); }

        readonly List<SpriteRenderer> worldShadows = new List<SpriteRenderer>();
        void Awake() => blocksMovement = true;
        void Start()
        {
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("물고기 그림자"); go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = shadowSprite; sr.sortingOrder = 1; sr.color = new Color(1, 1, 1, 0);
                worldShadows.Add(sr);
            }
        }
        void Update()
        {
            // 물속 그림자: 모여 있으면 천천히 맴돌고, 흩어졌으면 사라졌다가 다시 하나씩 나타난다
            float w = WaitHours, cfgH = GoryeoFishingConfig.Get() != null ? GoryeoFishingConfig.Get().scatterHours : 1f;
            float back = Mathf.Clamp01(1f - w / Mathf.Max(.1f, cfgH));
            for (int i = 0; i < worldShadows.Count; i++)
            {
                var sr = worldShadows[i]; if (sr == null) continue;
                float t = Time.time * (.35f + i * .07f) + i * 2.1f;
                var basePos = new Vector2(waterDir.x, waterDir.y) * (2.2f + i * .9f);
                var side = new Vector2(-waterDir.y, waterDir.x);
                sr.transform.localPosition = (Vector3)(basePos + side * Mathf.Sin(t) * .9f + new Vector2(waterDir.x, waterDir.y) * Mathf.Cos(t * .7f) * .35f);
                sr.flipX = Mathf.Cos(t) * side.x + Mathf.Cos(t) * side.y > 0;
                bool shown = Ready || back >= (i + 1) / 3f;
                float a = Mathf.MoveTowards(sr.color.a, shown ? .45f : 0f, Time.deltaTime * .6f);
                sr.color = new Color(1, 1, 1, a);
            }
        }

        public override void Interact(PlayerController player)
        {
            if (UIState.Blocking || GoryeoFishingGame.Open) return;
            if (!Ready)
            {
                int minutes = Mathf.CeilToInt(WaitHours * 60f);
                DialogueUI.Show("낚싯대", $"물이 아직 일렁여서 물고기가 흩어져 있다.\n그림자가 다시 모이면 낚아 보자. (게임 시간 {minutes}분쯤)");
                return;
            }
            GoryeoFishingGame.Begin(this);
        }
    }

    /// <summary>낚시 한 판 (IMGUI 종이 창). 게임 중에는 UIState.Blocking으로 이동과 의뢰 시간을 멈춘다.</summary>
    public sealed class GoryeoFishingGame : MonoBehaviour
    {
        enum Phase { Aim, Wait, Bite, Reel, Result }
        class Shadow { public GoryeoFishingConfig.Species fish; public float x, y, vx, size; public bool gone; }

        static GoryeoFishingGame instance;
        public static bool Open => instance != null && instance.spot != null;

        GoryeoFishingSpot spot;
        Phase phase;
        readonly List<Shadow> shadows = new List<Shadow>();
        Shadow hooked;
        float power, powerDir = 1f, castX = -1f, timer, nibbleAt, nibbleLeft, dip, tension, progress, surge, surgeAt, reelTime;
        int nibbles, openedFrame;
        string status = "", resultTitle = "", resultLine = "";
        Sprite resultIcon;
        bool lockedUntilRelease;

        public static void Begin(GoryeoFishingSpot spot)
        {
            if (instance == null) { var go = new GameObject("고려 낚시"); instance = go.AddComponent<GoryeoFishingGame>(); }
            instance.Session(spot);
        }

        void Session(GoryeoFishingSpot s)
        {
            if (s == null || s.config == null) return;
            spot = s; openedFrame = Time.frameCount; UIState.Blocking = true;
            shadows.Clear();
            var tide = TideBonus(s.config.kind);
            for (int i = 0; i < 3; i++) shadows.Add(NewShadow(s.config, tide, i));
            ToAim("그림자를 보고 힘 막대가 그 위에 올 때 Space로 던지세요.");
            Sfx.Play("open");
        }

        static float TideBonus(string kind)
        {
            var gm = GameManager.I; if (gm == null) return 0f;
            float h = gm.Clock.minuteOfDay / 60f;
            bool good = kind == "river" ? (h >= 5f && h < 8f) || (h >= 17f && h < 20f) : (h >= 6f && h < 9f) || (h >= 18f && h < 21f);
            return good ? 1f : 0f;
        }
        public static string TideText(string kind)
        {
            bool good = TideBonus(kind) > 0f;
            if (kind == "river") return good ? "물고기가 잘 무는 때" : "보통 때";
            return good ? "밀물 · 큰 물고기가 들어오는 때" : "썰물 무렵";
        }

        Shadow NewShadow(GoryeoFishingConfig.Spot s, float tide, int i)
        {
            float r = UnityEngine.Random.value;
            float rare = .10f + .10f * tide, mid = .30f + .05f * tide;
            int tier = r < rare ? 2 : r < rare + mid ? 1 : 0;
            GoryeoFishingConfig.Species fish = null;
            foreach (var id in s.fish) { var f = GoryeoFishingConfig.Find(id); if (f != null && f.tier == tier) { fish = f; break; } }
            if (fish == null) fish = GoryeoFishingConfig.Find(s.fish[0]);
            // 큰 물고기는 멀리, 작은 물고기는 가까이에 있는 편이다
            float x = Mathf.Clamp01(.28f + .22f * tier + UnityEngine.Random.Range(-.1f, .12f) + i * .04f);
            return new Shadow { fish = fish, x = Mathf.Clamp(x, .2f, .92f), y = .25f + .22f * i + UnityEngine.Random.Range(-.05f, .05f), vx = UnityEngine.Random.Range(.02f, .05f) * (UnityEngine.Random.value < .5f ? -1 : 1), size = 1f + .45f * tier };
        }

        void ToAim(string msg) { phase = Phase.Aim; power = 0f; powerDir = 1f; castX = -1f; hooked = null; status = msg; lockedUntilRelease = true; }

        static bool SpaceHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current; return k != null && k.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        void Update()
        {
            if (spot == null) return;
            if (Time.frameCount == openedFrame) return;
            float dt = Time.unscaledDeltaTime;
            bool press = InputReader.InteractPressed();
            bool held = SpaceHeld();
            if (InputReader.CancelPressed()) { Close(); return; }
            foreach (var sh in shadows)
            {
                if (sh.gone || sh == hooked) continue;
                sh.x += sh.vx * dt; if (sh.x < .2f || sh.x > .94f) { sh.vx = -sh.vx; sh.x = Mathf.Clamp(sh.x, .2f, .94f); }
            }
            switch (phase)
            {
                case Phase.Aim:
                    power += powerDir * dt * .85f; if (power >= 1f) { power = 1f; powerDir = -1f; } else if (power <= 0f) { power = 0f; powerDir = 1f; }
                    if (press) Cast();
                    break;
                case Phase.Wait:
                    timer -= dt;
                    if (hooked != null) hooked.x = Mathf.MoveTowards(hooked.x, castX, dt * .12f);
                    if (nibbleLeft > 0f) { nibbleLeft -= dt; dip = .35f; } else dip = Mathf.MoveTowards(dip, 0f, dt * 3f);
                    if (nibbles > 0 && timer <= nibbleAt) { nibbles--; nibbleLeft = .22f; nibbleAt = timer - UnityEngine.Random.Range(.7f, 1.4f); Sfx.Play("step", .35f); }
                    if (press) { EarlyStrike(); break; }
                    if (timer <= 0f) { phase = Phase.Bite; timer = BiteWindow(hooked.fish.tier); dip = 1f; status = "지금!"; Sfx.Play("accept", .7f); }
                    break;
                case Phase.Bite:
                    timer -= dt;
                    if (press) { StartReel(); break; }
                    if (timer <= 0f) { status = "찌가 떠올랐다. 놓쳤어요. 다시 던져 보세요."; ToAim(status); }
                    break;
                case Phase.Reel:
                    if (lockedUntilRelease && !held) lockedUntilRelease = false;
                    reelTime += dt;
                    float tier = hooked.fish.tier;
                    surgeAt -= dt;
                    if (surgeAt <= 0f) { surge = .35f; surgeAt = UnityEngine.Random.Range(.6f, 1.4f) - tier * .12f; }
                    if (surge > 0f) { tension += dt * (.55f + tier * .3f); surge -= dt; }
                    tension += (held && !lockedUntilRelease ? .85f : -.6f) * dt;
                    tension = Mathf.Clamp01(tension);
                    var (lo, hi) = Zone(hooked.fish.tier);
                    bool inZone = tension >= lo && tension <= hi;
                    progress += (inZone ? .30f - tier * .05f : -.16f - tier * .03f) * dt;
                    if (tension >= 1f) { Lost("줄이 팽팽하다가 툭 끊어졌다…"); break; }
                    if (progress <= 0f) { Lost($"{hooked.fish.name}가 바늘을 털고 달아났다…"); break; }
                    if (progress >= 1f) Caught();
                    break;
                case Phase.Result:
                    if (press) Close();
                    break;
            }
        }

        static float BiteWindow(int tier) => tier == 2 ? .45f : tier == 1 ? .6f : .75f;
        static (float lo, float hi) Zone(int tier) => tier == 2 ? (.4f, .68f) : tier == 1 ? (.34f, .7f) : (.3f, .74f);

        void Cast()
        {
            castX = .12f + power * .84f;
            Shadow best = null; float bestD = 1f;
            foreach (var sh in shadows) { if (sh.gone) continue; float d = Mathf.Abs(sh.x - castX); if (d < .07f + sh.size * .025f && d < bestD) { best = sh; bestD = d; } }
            Sfx.Play("cancel", .5f);
            if (best == null) { status = "물고기가 없는 곳에 떨어졌다. 그림자 위로 다시 던져 보세요."; power = 0f; castX = -1f; return; }
            hooked = best; phase = Phase.Wait;
            timer = UnityEngine.Random.Range(2.2f, 3.8f); nibbles = UnityEngine.Random.Range(best.fish.tier == 2 ? 1 : 0, 3); nibbleAt = timer - UnityEngine.Random.Range(.5f, 1f);
            status = "찌를 보세요. 톡톡 건드리는 건 속임수, 쑥 잠길 때 Space!";
        }

        void EarlyStrike()
        {
            if (hooked != null) hooked.gone = true;
            bool any = shadows.Exists(s => !s.gone);
            if (!any) { spot.Scatter(); ShowResult("너무 일찍 챘다", "놀란 물고기들이 모두 흩어졌다. 잠시 뒤 다시 모인다.", null); return; }
            ToAim("너무 일찍 챘다! 그 물고기는 달아났어요. 다른 그림자를 노려 보세요.");
        }

        void StartReel() { phase = Phase.Reel; tension = .45f; progress = .25f; surge = 0f; surgeAt = .5f; reelTime = 0f; lockedUntilRelease = SpaceHeld(); status = "Space를 누르면 감고, 떼면 풀려요. 긴장을 초록 칸에!"; Sfx.Play("open", .6f); }

        void Lost(string why) { spot.Scatter(); Sfx.Play("fail", .6f); ShowResult("놓쳤다", why + "\n물이 일렁여 물고기들이 흩어졌다.", null); }

        void Caught()
        {
            spot.Scatter();
            var f = hooked.fish; var gm = GameManager.I;
            float luck = Mathf.Pow(UnityEngine.Random.value, 1.5f) * .85f + Mathf.Clamp01(3f / Mathf.Max(1f, reelTime)) * .15f;
            int cm = Mathf.RoundToInt(Mathf.Lerp(f.minCm, f.maxCm, Mathf.Clamp01(luck)));
            var item = gm != null ? gm.ItemById(f.id) : null;
            string bestKey = "goryeo:fish:" + f.id + ":best", countKey = "goryeo:fish:" + f.id + ":count";
            int best = gm != null ? Mathf.RoundToInt(gm.GetMemo(bestKey, 0f)) : 0;
            string line;
            if (item == null) line = "(물건 정보가 없어 놓아 줬다)";
            else if (!gm.Player.CanCarry(item, 1)) line = "짐이 가득 차서 놓아 줬다.";
            else { gm.Player.cargo.Add(item, 1, 0f); line = "짐 +1 · 장터에서 팔 수 있다."; }
            if (gm != null) { gm.SetMemo(countKey, gm.GetMemo(countKey, 0f) + 1f); if (cm > best) gm.SetMemo(bestKey, cm); }
            if (best == 0) line += "\n처음 낚은 물고기!"; else if (cm > best) line += $"\n최고 기록! (전 {best}cm)";
            Sfx.Play("success", .8f);
            ShowResult($"{f.name} {cm}cm", line, item != null ? item.icon : null);
            if (gm != null) gm.SaveNow();
        }

        void ShowResult(string title, string line, Sprite icon) { phase = Phase.Result; resultTitle = title; resultLine = line; resultIcon = icon; status = ""; }

        void Close()
        {
            spot = null; shadows.Clear(); hooked = null;
            UIState.Release();
        }

        void OnDisable() { if (spot != null) Close(); }

        // ---------------- 그리기 ----------------
        public Rect WindowRect { get; private set; }
        static readonly Color32 Water = new Color32(78, 140, 160, 255), WaterDeep = new Color32(58, 112, 138, 255), Shore = new Color32(98, 140, 74, 255),
            ShadowCol = new Color32(20, 44, 58, 150), LineCol = new Color32(250, 248, 236, 220), FloatRed = new Color32(214, 64, 52, 255),
            ZoneCol = new Color32(55, 196, 90, 255), TensionCol = new Color32(42, 26, 46, 255), CastCol = new Color32(255, 207, 51, 255);

        void OnGUI()
        {
            if (spot == null) return;
            UIStyle.Begin(); GUI.depth = -10;
            float w = Mathf.Min(372f, UIStyle.VirtualWidth - 20f), h = 222f;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            WindowRect = r;
            CuteUI.Dim(); CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, spot.config.name + " 낚시");
            CuteUI.Label(r.x + 14, r.y + 18, TideText(spot.config.kind), CuteUI.Sub, true);
            CuteUI.LabelRight(r.xMax - 14, r.y + 18, "Esc 그만", CuteUI.Sub, true);

            var water = new Rect(r.x + 14, r.y + 28, w - 28, 92);
            if (phase != Phase.Result) DrawWater(water); else DrawResult(water);

            var bar = new Rect(r.x + 24, water.yMax + 10, w - 48, 12);
            if (phase == Phase.Aim)
            {
                CuteUI.Box(bar, 3, CuteUI.Paper2, CuteUI.Line, 1);
                CuteUI.Fill(new Rect(bar.x + 1, bar.y + 1, (bar.width - 2) * power, bar.height - 2), CastCol);
                CuteUI.Label(bar.x, bar.yMax + 9, "가까이", CuteUI.Sub, true); CuteUI.LabelRight(bar.xMax, bar.yMax + 9, "멀리", CuteUI.Sub, true);
            }
            else if (phase == Phase.Reel)
            {
                var (lo, hi) = Zone(hooked.fish.tier);
                CuteUI.Box(bar, 3, CuteUI.Paper2, CuteUI.Line, 1);
                CuteUI.Fill(new Rect(bar.x + 1 + (bar.width - 2) * lo, bar.y + 1, (bar.width - 2) * (hi - lo), bar.height - 2), ZoneCol);
                CuteUI.Fill(new Rect(bar.x + (bar.width - 2) * tension - 1, bar.y - 3, 3, bar.height + 6), TensionCol);
                CuteUI.Label(bar.x, bar.yMax + 9, "느슨", CuteUI.Sub, true); CuteUI.LabelRight(bar.xMax, bar.yMax + 9, "끊어짐", CuteUI.Sub, true);
                var pb = new Rect(bar.x, bar.yMax + 18, bar.width, 7);
                CuteUI.Box(pb, 2, CuteUI.Paper2, CuteUI.Line, 1);
                CuteUI.Fill(new Rect(pb.x + 1, pb.y + 1, (pb.width - 2) * Mathf.Clamp01(progress), pb.height - 2), CuteUI.Interact);
            }
            if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(r.x + 14, r.yMax - 52, w - 28, 30), status, CuteUI.PaperWrap);
            string key = phase == Phase.Aim ? "던지기" : phase == Phase.Reel ? "누르고 있기 = 감기" : phase == Phase.Result ? "닫기" : "채기";
            CuteUI.LabelCenter(r.center.x, r.yMax - 14, "Space " + key, CuteUI.Text, true, true);
        }

        void DrawWater(Rect a)
        {
            CuteUI.Fill(a, Water);
            CuteUI.Fill(new Rect(a.x + a.width * .55f, a.y, a.width * .45f, a.height), WaterDeep);
            CuteUI.Fill(new Rect(a.x, a.y, 10, a.height), Shore);
            // 잔물결
            float t = Time.unscaledTime;
            for (int i = 0; i < 7; i++)
            {
                float x = a.x + 20 + ((i * 47 + t * 9f) % (a.width - 30)), y = a.y + 8 + (i * 13) % (a.height - 14);
                CuteUI.Fill(new Rect(Mathf.Round(x), Mathf.Round(y), 6, 1), new Color32(150, 200, 214, 160));
            }
            foreach (var sh in shadows)
            {
                if (sh.gone) continue;
                float sw = Mathf.Round(14 * sh.size), shh = Mathf.Round(6 * sh.size);
                float x = a.x + a.width * sh.x - sw / 2, y = a.y + a.height * sh.y - shh / 2;
                CuteUI.Box(new Rect(x, y, sw, shh), (int)(shh / 2), ShadowCol, ShadowCol, 0);
                float tx = sh.vx > 0 ? x - 4 : x + sw;   // 꼬리는 헤엄치는 반대쪽
                CuteUI.Fill(new Rect(tx, y + shh / 2 - 2, 4, 4), ShadowCol);
            }
            // 찌·낚싯줄
            float tipX = a.x + 8, tipY = a.y + 4;
            if (phase == Phase.Aim)
            {
                float cx = a.x + a.width * (.12f + power * .84f);
                for (float y = a.y + 2; y < a.yMax - 2; y += 4) CuteUI.Fill(new Rect(Mathf.Round(cx), y, 1, 2), CastCol);
            }
            else if (castX >= 0f)
            {
                float fx = a.x + a.width * castX, fy = a.y + a.height * (hooked != null ? hooked.y : .5f) - 6 + dip * 5f;
                if (phase == Phase.Reel) fx += Mathf.Sin(Time.unscaledTime * 18f) * 2f;
                int steps = 24;
                for (int i = 0; i <= steps; i++)
                {
                    float k = i / (float)steps; float x = Mathf.Lerp(tipX, fx, k), y = Mathf.Lerp(tipY, fy, k) + Mathf.Sin(k * Mathf.PI) * 6f;
                    CuteUI.Fill(new Rect(Mathf.Round(x), Mathf.Round(y), 1, 1), LineCol);
                }
                bool under = phase == Phase.Bite || phase == Phase.Reel;
                if (!under) { CuteUI.Fill(new Rect(fx - 2, fy - 3, 4, 3), FloatRed); CuteUI.Fill(new Rect(fx - 2, fy, 4, 2), CuteUI.White); }
                else { CuteUI.Fill(new Rect(fx - 4, fy + 1, 8, 1), LineCol); CuteUI.Fill(new Rect(fx - 6, fy + 3, 12, 1), new Color32(250, 248, 236, 120)); }
                if (phase == Phase.Bite) CuteUI.LabelCenter(fx, fy - 12, "!", CuteUI.Alert, false, true);
            }
        }

        void DrawResult(Rect a)
        {
            CuteUI.Box(a, 4, CuteUI.Paper2, CuteUI.Line, 1);
            if (resultIcon != null && Event.current.type == EventType.Repaint)
            {
                var tr = resultIcon.textureRect; var tex = resultIcon.texture;
                var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                GUI.DrawTextureWithTexCoords(new Rect(a.x + 18, a.center.y - 24, 48, 48), tex, uv);
            }
            float tx = resultIcon != null ? a.x + 80 : a.x + 18;
            CuteUI.Label(tx, a.y + 26, resultTitle, CuteUI.Text, false, true);
            GUI.Label(new Rect(tx, a.y + 40, a.xMax - tx - 10, 46), resultLine, CuteUI.PaperWrap);
        }
    }
}
