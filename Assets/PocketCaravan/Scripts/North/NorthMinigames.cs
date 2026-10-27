using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 미니게임 두 개 (10/24). 공용 코드는 고치지 않고, 공용 Interactable을 상속한 자리를 맵에 놓는다.
    /// - 얼음 낚시: 언 호수·개울가의 낚시 자리에서 Space → 얼음 깨기(3번) → 찌 기다리기 → "!"에 당기기 → 줄다리기. 잡은 물고기는 화물로.
    /// - 썰매 언덕: 순록 썰매 정류장 출발대에서 20냥 → 내리막 썰매 (나무·바위 피하기, 통나무는 Space로 점프, 눈송이·룬 모으기) → 점수만큼 상금.
    /// 자리 목록은 Resources/North/NorthLife.json (fishing, fishingWater, sledStart). 그림은 Resources/North/Minigame.
    /// 게임 중에는 UIState.Blocking으로 이동과 시간을 멈춘다.
    /// </summary>
    public class NorthMinigames : MonoBehaviour
    {
        public const int SledFee = 20;
        const int SledRewardCap = 60;
        const string BestKey = "north_sled_best";

        static NorthMinigames instance;

        class Spots { public int h; public int[] fishing, water, sled, edge; }
        readonly Dictionary<string, Spots> spots = new Dictionary<string, Spots>();
        readonly Dictionary<string, float> refreezeAt = new Dictionary<string, float>();
        readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
        Transform root;
        string lastMap;
        AudioSource sfx;
        AudioClip crack, splash, tick, jingle, ding, thud, whoosh;
        int startFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthMinigames>() != null) return;
            var go = new GameObject("NorthMinigames");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthMinigames>();
        }

        void Awake()
        {
            instance = this;
            var json = Resources.Load<TextAsset>("North/NorthLife");
            if (json != null) Parse(json.text);
            foreach (var n in new[] { "sled", "tree", "tree2", "tree3", "rock", "log", "rune", "flake", "heart", "heart_empty", "fish_cod", "fish_char", "fish_salmon", "bobber" })
            {
                var t = Resources.Load<Texture2D>("North/Minigame/" + n);
                if (t != null) { t.filterMode = FilterMode.Point; tex[n] = t; }
            }
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            crack = Noise("north_crack", 0.12f, 0.9f, 1f, 7);
            splash = Noise("north_splash", 0.45f, 0.5f, 0.97f, 8);
            thud = Noise("north_thud", 0.25f, 0.9f, 0.9f, 9);
            whoosh = Noise("north_sledwhoosh", 0.5f, 0.35f, 0.99f, 10);
            tick = Tone("north_tick", 0.04f, new[] { 1400f });
            ding = Tone("north_ding", 0.12f, new[] { 1320f, 1760f });
            jingle = Tone("north_jingle", 0.5f, new[] { 784f, 988f, 1175f, 1568f });
        }

        void Play(AudioClip c, float v = 0.6f) { if (c != null && !AudioManager.Muted) sfx.PlayOneShot(c, v); }

        // ---------------- 자리 놓기 ----------------

        void Update()
        {
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            if (fishing != FState.None) UpdateFishing(Time.unscaledDeltaTime);
            if (sled != SState.None) UpdateSled(Time.unscaledDeltaTime);
            UpdateEdge(map);
        }

        // 10/27 호수 둘레 어디서나 얼음 낚시: 둘레 칸(NorthLife "fishEdge")에 서면 발밑에 낚시 자리를 하나 띄운다.
        // 가까이에 다른 사람·자리(고정 낚시 자리 포함)가 있으면 그쪽 말 걸기를 가로채지 않게 띄우지 않는다.
        // 얼음 구멍 쿨다운은 가장 가까운 고정 낚시 자리와 같이 쓴다 (한 칸 옮겨서 바로 다시 낚는 것 막기).
        readonly HashSet<int> edgeSet = new HashSet<int>();
        NorthFishingSpot edgeSpot;

        void UpdateEdge(string map)
        {
            if (edgeSpot == null || map == null || !spots.TryGetValue(map, out var s)) return;
            bool on = false;
            var pc = PlayerController.Instance;
            if (pc != null && pc.Motor != null && fishing == FState.None && sled == SState.None && !UIState.Blocking && MapRoot.Current != null)
            {
                var c = pc.Motor.Cell;
                int jy = s.h - 1 - c.y;
                if (edgeSet.Contains(c.x * 1000 + jy))
                {
                    Vector2 p = pc.transform.position;
                    bool nearOther = false;
                    foreach (var it in Interactable.All)
                        if (it != edgeSpot && ((Vector2)it.transform.position - p).magnitude < 1.6f) { nearOther = true; break; }
                    if (!nearOther)
                    {
                        on = true;
                        edgeSpot.transform.position = MapRoot.Current.CellCenter(c);
                        int best = -1, bd = int.MaxValue;
                        for (int i = 0; i + 1 < s.fishing.Length; i += 2)
                        {
                            int d = Mathf.Abs(s.fishing[i] - c.x) + Mathf.Abs(s.fishing[i + 1] - jy);
                            if (d < bd) { bd = d; best = i; }
                        }
                        edgeSpot.key = best >= 0 ? $"{map}:{s.fishing[best]}:{s.fishing[best + 1]}" : $"{map}:edge";
                    }
                }
            }
            if (edgeSpot.gameObject.activeSelf != on) edgeSpot.gameObject.SetActive(on);
        }

        void Rebuild(string map)
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            edgeSpot = null; edgeSet.Clear();
            if (map == null || !spots.TryGetValue(map, out var s)) return;
            root = new GameObject("North Minigame Spots").transform;
            if (s.edge != null && s.edge.Length >= 2)
            {
                for (int i = 0; i + 1 < s.edge.Length; i += 2) edgeSet.Add(s.edge[i] * 1000 + s.edge[i + 1]);
                var eg = new GameObject("Ice Fishing (lake edge)");
                eg.transform.SetParent(root, false);
                eg.SetActive(false);
                edgeSpot = eg.AddComponent<NorthFishingSpot>();
                edgeSpot.blocksMovement = false;
            }
            for (int i = 0; i + 1 < s.fishing.Length; i += 2)
            {
                var go = new GameObject("Ice Fishing");
                go.transform.SetParent(root, false);
                go.transform.position = MapRoot.Current.CellCenter(new Vector3Int(s.fishing[i], s.h - 1 - s.fishing[i + 1], 0));
                var spot = go.AddComponent<NorthFishingSpot>();
                spot.key = $"{map}:{s.fishing[i]}:{s.fishing[i + 1]}";
                spot.blocksMovement = false;
            }
            if (s.sled != null && s.sled.Length >= 2)
            {
                var go = new GameObject("Sled Hill");
                go.transform.SetParent(root, false);
                go.transform.position = MapRoot.Current.CellCenter(new Vector3Int(s.sled[0], s.h - 1 - s.sled[1], 0));
                go.AddComponent<NorthSledStart>().blocksMovement = false;
            }
        }

        void Parse(string text)
        {
            int at = 0;
            while (true)
            {
                int i = text.IndexOf("\"mapId\"", at, StringComparison.Ordinal);
                if (i < 0) break;
                int q1 = text.IndexOf('"', text.IndexOf(':', i) + 1), q2 = text.IndexOf('"', q1 + 1);
                string id = text.Substring(q1 + 1, q2 - q1 - 1);
                int next = text.IndexOf("\"mapId\"", q2, StringComparison.Ordinal);
                string block = next < 0 ? text.Substring(i) : text.Substring(i, next - i);
                var s = new Spots { h = Int(block, "\"h\""), fishing = Ints(block, "fishing"), water = Ints(block, "fishingWater"), sled = Ints(block, "sledStart"), edge = Ints(block, "fishEdge") };
                if (s.fishing.Length > 0 || s.sled.Length > 0) spots[id] = s;
                at = q2;
            }
        }

        static int Int(string block, string key)
        {
            int i = block.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return 0;
            i = block.IndexOf(':', i) + 1;
            int j = i;
            while (j < block.Length && (char.IsWhiteSpace(block[j]) || char.IsDigit(block[j]) || block[j] == '-')) j++;
            int.TryParse(block.Substring(i, j - i).Trim(), out int v);
            return v;
        }

        static int[] Ints(string block, string key)
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

        static bool SpaceHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        bool Fresh => Time.frameCount <= startFrame + 1;   // 시작한 프레임의 Space는 무시 (대화 선택에 쓴 키)

        // ---------------- 얼음 낚시 ----------------

        enum FState { None, Break, Wait, Bite, Reel, Result }
        FState fishing = FState.None;
        NorthFishingSpot fishSpot;
        int cracks;
        float timer, window;
        float zone, zoneVel, fishPos, fishTarget, fishRetarget, progress, bob, reelTime;
        int fishKind;
        string result;

        struct Fish { public string id, name, sprite; public float speed, zoneSize, window; public int minCm, maxCm; }
        static readonly Fish[] Fishes =
        {
            new Fish { id = "north_cod", name = "얼음 대구", sprite = "fish_cod", speed = 0.9f, zoneSize = 0.32f, window = 0.8f, minCm = 30, maxCm = 72 },
            new Fish { id = "north_char", name = "북극 곤들매기", sprite = "fish_char", speed = 1.4f, zoneSize = 0.26f, window = 0.65f, minCm = 25, maxCm = 62 },
            new Fish { id = "north_salmon", name = "황금 연어", sprite = "fish_salmon", speed = 2.0f, zoneSize = 0.22f, window = 0.5f, minCm = 60, maxCm = 112 },
        };

        public static bool FishingReady(string key, out float wait)
        {
            wait = 0f;
            if (instance == null || GameManager.I == null) return false;
            if (instance.refreezeAt.TryGetValue(key, out var t)) wait = t - GameManager.I.Clock.TotalHours;
            return wait <= 0f;
        }

        public static void StartFishing(NorthFishingSpot spot)
        {
            var m = instance;
            if (m == null || m.fishing != FState.None || m.sled != SState.None) return;
            if (!FishingReady(spot.key, out var wait))
            {
                Notify.Show($"구멍이 다시 얼었어. 조금 있다가 ({Mathf.CeilToInt(wait * 60f)}분 뒤) 다시 깨 보자.");
                return;
            }
            m.fishSpot = spot;
            m.fishing = FState.Break;
            m.cracks = 0;
            m.startFrame = Time.frameCount;
            UIState.Blocking = true;
        }

        void BeginWait()
        {
            fishing = FState.Wait;
            timer = UnityEngine.Random.Range(1.5f, 5f);
        }

        void UpdateFishing(float dt)
        {
            bob += dt;
            if (InputReader.CancelPressed()) { EndFishing(); return; }
            bool press = !Fresh && InputReader.InteractPressed();
            switch (fishing)
            {
                case FState.Break:
                    if (press)
                    {
                        cracks++;
                        Play(crack, 0.8f);
                        if (cracks >= 3) { Play(splash, 0.5f); BeginWait(); }
                    }
                    break;
                case FState.Wait:
                    timer -= dt;
                    if (press) { result = "너무 일찍 당겼어! 물고기가 놀라서 흩어졌어."; timer += 1.5f; Notify.Show(result, 1.6f); }
                    if (timer <= 0f)
                    {
                        fishKind = PickFish();
                        window = Fishes[fishKind].window;
                        timer = window;
                        fishing = FState.Bite;
                        Play(tick, 0.9f);
                    }
                    break;
                case FState.Bite:
                    timer -= dt;
                    if (press)
                    {
                        fishing = FState.Reel;
                        zone = 0.3f; zoneVel = 0f; fishPos = 0.5f; fishTarget = 0.5f; fishRetarget = 0f; progress = 0.35f; reelTime = 0f;
                        Play(splash, 0.4f);
                    }
                    else if (timer <= 0f) { Notify.Show("놓쳤다… 찌를 다시 던졌어.", 1.6f); BeginWait(); }
                    break;
                case FState.Reel:
                {
                    var f = Fishes[fishKind];
                    reelTime += dt;
                    zoneVel += (SpaceHeld() ? 2.6f : -2.2f) * dt;
                    zone += zoneVel * dt;
                    if (zone < 0f) { zone = 0f; zoneVel = Mathf.Max(0f, zoneVel) * 0.3f; }
                    if (zone > 1f - f.zoneSize) { zone = 1f - f.zoneSize; zoneVel = Mathf.Min(0f, zoneVel) * 0.3f; }
                    fishRetarget -= dt;
                    if (fishRetarget <= 0f) { fishTarget = UnityEngine.Random.Range(0.05f, 0.95f); fishRetarget = UnityEngine.Random.Range(0.5f, 1.4f) / f.speed; }
                    fishPos = Mathf.MoveTowards(fishPos, fishTarget, f.speed * 0.45f * dt);
                    bool inside = fishPos >= zone && fishPos <= zone + f.zoneSize;
                    progress += (inside ? 0.32f : -0.22f) * dt;
                    if (progress >= 1f) Caught();
                    else if (progress <= 0f) { result = $"{f.name}가 줄을 끊고 달아났어…"; fishing = FState.Result; Play(thud, 0.4f); Refreeze(); }
                    break;
                }
                case FState.Result:
                    if (press) EndFishing();
                    break;
            }
        }

        int PickFish()
        {
            float r = UnityEngine.Random.value;
            float salmon = (NorthAtmosphere.AuroraOut ? 0.2f : 0.03f) + (NorthLegends.Blessed ? 0.05f : 0f);
            if (NorthSave.Get("bait") >= 1f)            // 떠돌이 상인의 황금 미끼: 귀한 물고기가 잘 문다
            {
                NorthSave.Add("bait", -1f);
                salmon += 0.15f;
                return UnityEngine.Random.value < salmon ? 2 : 1;
            }
            bool night = GameManager.I != null && GameManager.I.Clock.Daylight < 0.5f;
            float charr = night ? 0.35f : 0.27f;
            return r < salmon ? 2 : r < salmon + charr ? 1 : 0;
        }

        void Caught()
        {
            var f = Fishes[fishKind];
            var gm = GameManager.I;
            var item = gm != null ? gm.ItemById(f.id) : null;
            if (item == null) result = $"{f.name}를 낚았는데 물건 정보가 없어.\n(Pocket Caravan > 북유럽 > 테스트 준비를 다시 실행)";
            else if (!gm.Player.CanCarry(item, 1)) result = $"{f.name}를 낚았지만 짐이 가득 차서 놓아 줬어.";
            else
            {
                gm.Player.cargo.Add(item, 1, 0f);
                // 크기: 빨리 끌어올릴수록(줄다리기 시간이 짧을수록) 큰 놈일 확률이 조금 높다
                float luck = Mathf.Pow(UnityEngine.Random.value, 1.6f) * 0.85f + Mathf.Clamp01(1.6f / Mathf.Max(0.5f, reelTime)) * 0.15f;
                int cm = Mathf.RoundToInt(Mathf.Lerp(f.minCm, f.maxCm, Mathf.Clamp01(luck)));
                int best = NorthDaily.Best(f.id);
                NorthSave.Add("fish:" + f.id + ":count", 1f);
                if (cm > best) NorthSave.Set("fish:" + f.id + ":best", cm);
                result = $"{f.name} {cm}cm를 낚았다!  (화물 +1)" + (cm > best && best > 0 ? "\n최고 기록!" : best == 0 ? "\n도감에 새로 올랐다!" : "");
            }
            fishing = FState.Result;
            Play(jingle, 0.6f);
            Refreeze();
        }

        void Refreeze()
        {
            if (GameManager.I != null && fishSpot != null) refreezeAt[fishSpot.key] = GameManager.I.Clock.TotalHours + 1f;
        }

        void EndFishing()
        {
            fishing = FState.None;
            fishSpot = null;
            UIState.Release();
        }

        // ---------------- 썰매 언덕 ----------------

        enum SState { None, Ready, Run, Result }
        SState sled = SState.None;
        class Obj { public int kind; public float x, y; public bool taken; }   // kind: 0 나무, 1 바위, 2 통나무, 3 눈송이, 4 룬
        readonly List<Obj> objs = new List<Obj>();
        float px, dist, speed, air, airCool, invuln, spawnAt, shake, runTime;
        int hearts, flakes, runes;
        int reward;
        const float FieldW = 220f, FieldH = 300f, Goal = 4200f;

        public static void AskSled()
        {
            var m = instance;
            var gm = GameManager.I;
            if (m == null || gm == null || m.sled != SState.None || m.fishing != FState.None) return;
            int best = PlayerPrefs.GetInt(BestKey, 0);
            string rec = best > 0 ? $"\n내 최고 기록: {best}점" : "";
            if (gm.Player.money < SledFee)
            {
                DialogueUI.Show("썰매꾼", $"언덕 썰매는 한 판에 {SledFee}냥이야. 돈이 모자라는걸? 장사 좀 하고 와.{rec}");
                return;
            }
            DialogueUI.Show("썰매꾼",
                $"언덕 썰매 한 판에 {SledFee}냥! 나무랑 바위는 피하고, 통나무는 Space로 뛰어넘어.\n눈송이와 룬돌을 모아서 끝까지 내려가면 상금을 줄게 (최대 {SledRewardCap}냥).{rec}",
                new DialogueUI.Choice($"탄다 ({SledFee}냥)", () => m.StartSled()),
                new DialogueUI.Choice("다음에", null));
        }

        void StartSled()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player.money < SledFee) return;
            gm.Player.money -= SledFee;
            objs.Clear();
            px = 0f; dist = 0f; speed = 70f; air = 0f; airCool = 0f; invuln = 0f; spawnAt = 120f; shake = 0f; runTime = 0f;
            hearts = 3; flakes = 0; runes = 0; reward = 0;
            sled = SState.Ready; timer = 1.6f;
            startFrame = Time.frameCount;
            UIState.Blocking = true;
            Play(whoosh, 0.5f);
        }

        void UpdateSled(float dt)
        {
            if (sled == SState.Ready)
            {
                timer -= dt;
                if (timer <= 0f) sled = SState.Run;
                return;
            }
            if (sled == SState.Result)
            {
                if (!Fresh && (InputReader.InteractPressed() || InputReader.CancelPressed())) { sled = SState.None; UIState.Release(); }
                return;
            }
            runTime += dt;
            // 속도: 점점 빨라지고, 부딪히면 줄어든다
            speed = Mathf.MoveTowards(speed, Mathf.Min(240f, 110f + runTime * 6f), 60f * dt);
            dist += speed * dt;
            float steer = InputReader.MoveVector().x;
            px = Mathf.Clamp(px + steer * 150f * dt, -FieldW / 2 + 22f, FieldW / 2 - 22f);
            if (air > 0f) air -= dt;
            if (airCool > 0f) airCool -= dt;
            if (invuln > 0f) invuln -= dt;
            if (shake > 0f) shake -= dt;
            if (!Fresh && InputReader.InteractPressed() && air <= 0f && airCool <= 0f) { air = 0.6f; airCool = 0.8f; Play(whoosh, 0.35f); }
            if (InputReader.CancelPressed()) { EndSled(false); return; }

            // 앞쪽에 장애물·보너스 줄 만들기
            while (spawnAt < dist + FieldH + 40f && spawnAt < Goal - 60f)
            {
                SpawnRow(spawnAt);
                spawnAt += UnityEngine.Random.Range(46f, 74f) * Mathf.Lerp(1.1f, 0.8f, Mathf.Clamp01(spawnAt / Goal));
            }

            // 부딪힘: 썰매는 화면 아래쪽 (거리 dist + 40)
            float sy = dist + 40f;
            foreach (var o in objs)
            {
                if (o.taken) continue;
                float w = o.kind == 2 ? 26f : o.kind <= 1 ? 13f : 9f;
                float h = o.kind == 2 ? 7f : o.kind <= 1 ? 9f : 9f;
                if (Mathf.Abs(o.x - px) > w || Mathf.Abs(o.y - sy) > h) continue;
                if (o.kind == 3) { o.taken = true; flakes++; Play(ding, 0.35f); }
                else if (o.kind == 4) { o.taken = true; runes++; Play(jingle, 0.4f); }
                else if (o.kind == 2 && air > 0f) { }                      // 통나무: 뛰면 넘는다
                else if (o.kind == 1 && air > 0.15f) { }                   // 바위: 높이 뛰면 넘는다
                else if (invuln <= 0f)
                {
                    hearts--; invuln = 1.3f; speed *= 0.45f; shake = 0.35f; o.taken = true;
                    Play(thud, 0.8f);
                    if (hearts <= 0) { EndSled(false); return; }
                }
            }
            objs.RemoveAll(o => o.y < dist - 40f);
            if (dist >= Goal) EndSled(true);
        }

        void SpawnRow(float y)
        {
            float half = FieldW / 2 - 24f;
            float r = UnityEngine.Random.value;
            if (r < 0.12f)                                       // 통나무 한 줄 (뛰어넘기)
                objs.Add(new Obj { kind = 2, x = UnityEngine.Random.Range(-half + 20f, half - 20f), y = y });
            else if (r < 0.2f && y > 1200f)                      // 룬돌 (드물게, 나무 사이)
            {
                float x = UnityEngine.Random.Range(-half, half);
                objs.Add(new Obj { kind = 4, x = x, y = y });
                objs.Add(new Obj { kind = 0, x = Mathf.Clamp(x - 30f, -half, half), y = y + 4f });
                objs.Add(new Obj { kind = 0, x = Mathf.Clamp(x + 30f, -half, half), y = y + 4f });
            }
            else
            {
                int n = 1 + (y > 1500f ? 1 : 0) + (y > 3000f && UnityEngine.Random.value < 0.5f ? 1 : 0);
                for (int i = 0; i < n; i++)
                    objs.Add(new Obj { kind = UnityEngine.Random.value < 0.75f ? 0 : 1, x = UnityEngine.Random.Range(-half, half), y = y + UnityEngine.Random.Range(-8f, 8f) });
            }
            // 눈송이 줄 (휘어진 길을 따라)
            if (UnityEngine.Random.value < 0.55f)
            {
                float x0 = UnityEngine.Random.Range(-half, half), dir = UnityEngine.Random.Range(-1f, 1f);
                for (int i = 0; i < 4; i++)
                    objs.Add(new Obj { kind = 3, x = Mathf.Clamp(x0 + dir * i * 12f, -half, half), y = y + 22f + i * 12f });
            }
        }

        void EndSled(bool done)
        {
            int score = flakes + runes * 10 + (done ? 20 + hearts * 5 : 0);
            reward = Mathf.Min(SledRewardCap + (NorthLegends.Blessed ? 20 : 0), score);
            if (GameManager.I != null) GameManager.I.Player.money += reward;
            if (score > PlayerPrefs.GetInt(BestKey, 0)) { PlayerPrefs.SetInt(BestKey, score); PlayerPrefs.Save(); }
            result = (done ? "완주!" : hearts <= 0 ? "꽈당! 썰매가 뒤집혔어." : "중간에 내렸어.")
                     + $"\n눈송이 {flakes} · 룬돌 {runes}" + (done ? $" · 남은 하트 {hearts}" : "")
                     + $"\n점수 {score}  →  상금 {reward}냥" + (score >= PlayerPrefs.GetInt(BestKey, 0) && score > 0 ? "  (최고 기록!)" : "");
            sled = SState.Result;
            startFrame = Time.frameCount;
            Play(done ? jingle : thud, 0.6f);
        }

        // ---------------- 그리기 ----------------

        void OnGUI()
        {
            if (fishing == FState.None && sled == SState.None) return;
            UIStyle.Begin();
            GUI.depth = -20;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            Fill(new Rect(0, 0, vw, vh), new Color(0f, 0f, 0.05f, 0.45f));
            if (fishing != FState.None) DrawFishing(vw, vh);
            else DrawSled(vw, vh);
        }

        void DrawFishing(float vw, float vh)
        {
            var panel = new Rect(vw / 2 - 160, vh / 2 - 115, 320, 230);
            GUI.Box(panel, GUIContent.none, UIStyle.Box);
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, 296, 18), "<b>얼음 낚시</b>", UIStyle.Rich);
            var ice = new Rect(panel.x + 16, panel.y + 30, 200, 150);
            Fill(ice, new Color(0.78f, 0.9f, 0.96f));
            for (int i = 0; i < 9; i++) Fill(new Rect(ice.x + 10 + i * 21, ice.y + 8 + (i % 3) * 40, 12, 2), new Color(1f, 1f, 1f, 0.7f));
            var hole = new Rect(ice.center.x - 34, ice.center.y - 22, 68, 44);
            if (fishing == FState.Break)
            {
                // 금: 깬 횟수만큼 금이 뻗어 나간다
                for (int c = 0; c < cracks; c++)
                    for (int k = 0; k < 6; k++)
                    {
                        float ang = (c * 2.1f + k * 1.05f), len = 10 + c * 8;
                        var p = new Vector2(ice.center.x + Mathf.Cos(ang) * len, ice.center.y + Mathf.Sin(ang) * len * 0.6f);
                        Fill(new Rect(p.x - 1, p.y - 1, 3, 2), new Color(0.35f, 0.5f, 0.6f));
                    }
                Fill(new Rect(ice.center.x - 3, ice.center.y - 3, 6, 6), new Color(0.3f, 0.45f, 0.55f));
            }
            else
            {
                Fill(hole, new Color(0.08f, 0.2f, 0.32f));
                Fill(new Rect(hole.x + 4, hole.y + 4, hole.width - 8, 4), new Color(1f, 1f, 1f, 0.25f));
                float dip = fishing == FState.Bite ? 6f : Mathf.Sin(bob * 3f) * 1.5f;
                Draw("bobber", new Rect(hole.center.x - 6, hole.center.y - 9 + dip, 12, 18));
                if (fishing == FState.Bite)
                    GUI.Label(new Rect(hole.center.x - 20, hole.y - 30, 40, 26), "<size=22><color=#ffde59><b>!</b></color></size>", UIStyle.CenterRich);
            }
            if (fishing == FState.Reel)
            {
                var f = Fishes[fishKind];
                var bar = new Rect(panel.x + 236, panel.y + 30, 26, 150);
                Fill(bar, new Color(0.1f, 0.18f, 0.28f));
                Fill(new Rect(bar.x + 2, bar.yMax - 2 - (zone + f.zoneSize) * (bar.height - 4), bar.width - 4, f.zoneSize * (bar.height - 4)), new Color(0.4f, 0.85f, 0.45f, 0.85f));
                Draw(f.sprite, new Rect(bar.x + 1, bar.yMax - 2 - fishPos * (bar.height - 4) - 6, 24, 12));
                var pb = new Rect(panel.x + 272, panel.y + 30, 10, 150);
                Fill(pb, new Color(0.15f, 0.12f, 0.2f));
                Fill(new Rect(pb.x + 1, pb.yMax - 1 - progress * (pb.height - 2), pb.width - 2, progress * (pb.height - 2)), new Color(1f, 0.82f, 0.3f));
            }
            if (fishing == FState.Result)
            {
                var f = Fishes[fishKind];
                if (result != null && result.Contains("낚았다")) Draw(f.sprite, new Rect(ice.center.x - 32, ice.y + 6, 64, 32));
            }
            string help = "";
            if (fishing == FState.Break) help = $"Space를 눌러 얼음을 깨자 ({cracks}/3)";
            else if (fishing == FState.Wait) help = "찌를 던졌다… 입질을 기다려 (서두르면 도망가)";
            else if (fishing == FState.Bite) help = "지금이야! Space!";
            else if (fishing == FState.Reel) help = "Space를 누르고 있으면 초록 칸이 올라가. 물고기를 칸 안에 두자!";
            else if (fishing == FState.Result) help = result + "\nSpace: 닫기";
            GUI.Label(new Rect(panel.x + 12, panel.y + 184, 296, 40), help, UIStyle.Wrap);
            GUI.Label(new Rect(panel.xMax - 70, panel.y + 8, 60, 18), "Esc 그만", UIStyle.Small);
        }

        void DrawSled(float vw, float vh)
        {
            float sx = shake > 0f ? UnityEngine.Random.Range(-2f, 2f) : 0f;
            var field = new Rect(vw / 2 - FieldW / 2 + sx, vh / 2 - FieldH / 2 + 10, FieldW, FieldH);
            Fill(new Rect(field.x - 4, field.y - 4, field.width + 8, field.height + 8), new Color(0.2f, 0.18f, 0.28f));
            Fill(field, new Color(0.95f, 0.96f, 1f));
            // 눈 위 썰매 자국 줄무늬 (위로 흘러감)
            for (int i = 0; i < 14; i++)
            {
                float y = field.y + Mathf.Repeat(i * 23f - dist * 1f, field.height);
                Fill(new Rect(field.x + 18 + (i * 37) % (int)(field.width - 40), y, 14, 1), new Color(0.78f, 0.82f, 0.92f));
            }
            // 양옆 숲
            for (int i = 0; i < 10; i++)
            {
                float y = field.y + Mathf.Repeat(i * 34f - dist, field.height + 34f) - 34f;
                DrawClip(i % 2 == 0 ? "tree" : "tree3", new Rect(field.x - 14, y, 32, 32), field);
                DrawClip(i % 2 == 0 ? "tree3" : "tree", new Rect(field.xMax - 18, y + 14, 32, 32), field);
            }
            // 결승선
            float gy = field.y + (Goal - dist);
            if (gy > field.y && gy < field.yMax)
                for (int i = 0; i < 22; i++) Fill(new Rect(field.x + i * 10, gy, 10, 4), i % 2 == 0 ? new Color(0.85f, 0.2f, 0.2f) : Color.white);
            // 장애물·보너스 (화면 위가 앞쪽: 화면 y = 물체 y - dist)
            foreach (var o in objs)
            {
                if (o.taken) continue;
                float y = field.y + (o.y - dist);
                float x = field.center.x + o.x;
                switch (o.kind)
                {
                    case 0: DrawClip("tree2", new Rect(x - 16, y - 26, 32, 32), field); break;
                    case 1: DrawClip("rock", new Rect(x - 14, y - 18, 28, 28), field); break;
                    case 2: DrawClip("log", new Rect(x - 28, y - 9, 56, 28), field); break;
                    case 3: DrawClip("flake", new Rect(x - 6, y - 6, 12, 12), field); break;
                    case 4: DrawClip("rune", new Rect(x - 8, y - 22, 16, 32), field); break;
                }
            }
            // 썰매 (뛰면 그림자가 떨어지고 조금 커짐)
            float py = field.y + 40f, lift = air > 0f ? Mathf.Sin(Mathf.Clamp01(1f - air / 0.6f) * Mathf.PI) * 10f : 0f;
            var sp = new Rect(field.center.x + px - 12, py - 20 - lift, 24, 32);
            Fill(new Rect(sp.x + 4, py + 10, 16, 3), new Color(0.6f, 0.62f, 0.75f, 0.6f));
            if (invuln <= 0f || Mathf.Repeat(invuln, 0.2f) < 0.12f) Draw("sled", sp);
            // 위쪽 정보
            for (int i = 0; i < 3; i++) Draw(i < hearts ? "heart" : "heart_empty", new Rect(field.x + 6 + i * 14, field.y - 22, 12, 11));
            GUI.Label(new Rect(field.x + 52, field.y - 26, 260, 18), $"눈송이 {flakes}   룬 {runes}   {Mathf.Clamp01(dist / Goal) * 100f:0}%", UIStyle.Rich);
            GUI.Label(new Rect(field.xMax + 12, field.y, 150, 90), "← → 방향\nSpace 점프\n(통나무·바위 넘기)\nEsc 내리기", UIStyle.Small);
            if (sled == SState.Ready)
                GUI.Label(new Rect(field.x, field.center.y - 20, field.width, 40), $"<size=18><b>{Mathf.CeilToInt(timer)}</b></size>", UIStyle.CenterRich);
            if (sled == SState.Result)
            {
                var box = new Rect(field.center.x - 100, field.center.y - 50, 200, 100);
                GUI.Box(box, GUIContent.none, UIStyle.Box);
                GUI.Label(new Rect(box.x + 10, box.y + 8, 180, 70), result, UIStyle.Wrap);
                GUI.Label(new Rect(box.x + 10, box.yMax - 22, 180, 16), "Space: 닫기", UIStyle.Small);
            }
        }

        void Draw(string name, Rect r)
        {
            if (tex.TryGetValue(name, out var t)) GUI.DrawTexture(r, t, ScaleMode.StretchToFill);
        }

        /// <summary>판 밖으로 나간 부분은 잘라서 그린다 (위·아래 끝에서 튀어나오지 않게)</summary>
        void DrawClip(string name, Rect r, Rect field)
        {
            if (!tex.TryGetValue(name, out var t)) return;
            float top = Mathf.Max(r.y, field.y), bottom = Mathf.Min(r.yMax, field.yMax);
            if (bottom <= top) return;
            float v0 = 1f - (top - r.y) / r.height, v1 = 1f - (bottom - r.y) / r.height;
            GUI.DrawTextureWithTexCoords(new Rect(r.x, top, r.width, bottom - top), t, new Rect(0f, v1, 1f, v0 - v1));
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        // ---------------- 소리 (파일 없이 코드로) ----------------

        static AudioClip Noise(string name, float seconds, float bright, float decay, int seed)
        {
            const int rate = 22050; int n = (int)(rate * seconds);
            var data = new float[n]; var rng = new System.Random(seed); float b = 0f, env = 1f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)rng.NextDouble() * 2f - 1f;
                b = b + (w - b) * bright;
                env *= Mathf.Lerp(0.9995f, 0.99985f, decay);
                data[i] = b * env * 0.7f * Mathf.Clamp01(i / 60f);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        static AudioClip Tone(string name, float seconds, float[] notes)
        {
            const int rate = 22050; int n = (int)(rate * seconds);
            var data = new float[n]; int seg = n / notes.Length;
            for (int i = 0; i < n; i++)
            {
                int k = Mathf.Min(notes.Length - 1, i / Mathf.Max(1, seg));
                float t = (float)i / rate, local = (float)(i - k * seg) / Mathf.Max(1, seg);
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[k] * t) * 0.35f * (1f - local) * Mathf.Clamp01(i / 40f);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }

    /// <summary>얼음 낚시 자리: 가까이 가서 Space</summary>
    public class NorthFishingSpot : Interactable
    {
        public string key;
        public override string DisplayName => NorthMinigames.FishingReady(key, out _) ? "얼음 낚시" : "얼음 낚시 (다시 어는 중)";
        public override Kind Category => Kind.Board;
        public override void Interact(PlayerController player) => NorthMinigames.StartFishing(this);
    }

    /// <summary>썰매 언덕 출발대: 가까이 가서 Space → 20냥 내고 썰매 미니게임</summary>
    public class NorthSledStart : Interactable
    {
        public override string DisplayName => $"썰매 언덕 ({NorthMinigames.SledFee}냥)";
        public override Kind Category => Kind.Board;
        public override void Interact(PlayerController player) => NorthMinigames.AskSled();
    }
}
