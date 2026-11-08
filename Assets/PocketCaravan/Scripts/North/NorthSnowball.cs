using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/30 눈숲 마을 눈싸움 (기우 기획). 공용 코드는 고치지 않는다.
    /// - 놀이터 자리: Resources/North/NorthLife.json의 "snowArena" [x0,y0,x1,y1], "snowWalls", "snowKids" (n4_build.py가 만든다. 위가 0인 칸)
    /// - 평소: 아이 셋이 눈벽 뒤에 숨었다 나왔다 하며 서로 눈덩이를 던진다. 아무에게나 말을 걸면 "같이 할래?"
    /// - 한 판: 실제 맵에서 40초. 평소처럼 WASD로 걷고, Space = 바라보는 쪽으로 던지기(가까운 아이 쪽으로 살짝 맞춰 줌).
    ///   아이 맞히기 +1, 막 고개 내민 아이를 바로 맞히면 +2, 내가 맞으면 −1(뒤로 살짝 밀림). 놀이터 밖으로 나가면 기권.
    ///   걷기를 멈추지 않으려고 UIState.Blocking은 쓰지 않는다. 대신 놀이터 둘레의 말 걸기 자리를 잠깐 끈다(Space가 말 걸기로 새지 않게).
    /// - 보상(돈 없음): 5점↑ 시세 소문 / 처음 10점↑ 칭호 '눈싸움 대장' + 눈사람 장식 / 그 뒤 10점↑ 하루 한 번 눈사람 장식
    /// - 저장: NorthSave "snowball:best", "snowball:champ", "snowball:day"
    /// </summary>
    public class NorthSnowball : MonoBehaviour
    {
        const string MapId = "north_c_snowwood";
        const float GameTime = 40f, ReadyTime = 2.4f;
        const float PlayerBallSpeed = 9f, KidBallSpeed = 5.5f, BallRange = 9f, HitRadius = 0.5f;
        const string ItemId = "north_snowman";

        static NorthSnowball instance;
        enum S { None, Ready, Play, Result }
        S state = S.None;

        class Kid
        {
            public int idx;
            public Transform tr; public SpriteRenderer sr; public Sprite[] frames;
            public NorthSpot spot;
            public Vector2 pos, target; public Vector2Int facing = Vector2Int.down;
            public float stateT, throwAt, flash, immune, peekSince = -9f, walkT;
            public bool peeking; public int home;
            public string say; public float sayUntil;
        }
        class Ball { public Vector2 p, v; public float left; public bool fromPlayer; public int owner; }
        class Splat { public Vector2 p; public float t; }

        readonly List<Kid> kids = new List<Kid>();
        readonly List<Ball> balls = new List<Ball>();
        readonly List<Splat> splats = new List<Splat>();
        readonly List<Vector2> hides = new List<Vector2>();   // 눈벽 바로 아래·위 칸 (월드)
        readonly List<Vector2> hideWall = new List<Vector2>(); // 그 숨는 자리를 가려 주는 눈벽 칸 (월드)
        readonly List<Interactable> muted = new List<Interactable>();
        RectInt arena;                 // 위가 0인 칸 (x0, y0, w, h)
        int mapH;
        Transform root;
        string lastMap;
        Sprite ballSprite, splatSprite;
        float timer, throwCool, playerFlash;
        int score, hitsGiven, hitsTaken;
        AudioSource sfx;
        AudioClip whoosh, hitClip, ouch, go, endClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthSnowball>() != null) return;
            var obj = new GameObject("NorthSnowball");
            DontDestroyOnLoad(obj);
            obj.AddComponent<NorthSnowball>();
        }

        void Awake()
        {
            instance = this;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            whoosh = NorthUtil.Tone("north_sb_throw", 0.08f, new[] { 900f, 600f }, 0.18f);
            hitClip = NorthUtil.Tone("north_sb_hit", 0.12f, new[] { 1320f, 1760f }, 0.3f);
            ouch = NorthUtil.Tone("north_sb_ouch", 0.15f, new[] { 300f, 220f }, 0.3f);
            go = NorthUtil.Tone("north_sb_go", 0.3f, new[] { 784f, 1046f }, 0.3f);
            endClip = NorthUtil.Tone("north_sb_end", 0.5f, new[] { 1046f, 784f, 1318f }, 0.3f);
        }

        void Play(AudioClip c, float v = 0.6f) { if (c != null && !AudioManager.Muted) sfx.PlayOneShot(c, v); }

        // ---------------- 자리 ----------------

        void Rebuild(string map)
        {
            if (state != S.None) Finish(true);
            if (root != null) Destroy(root.gameObject);
            root = null; kids.Clear(); balls.Clear(); splats.Clear(); hides.Clear(); hideWall.Clear();
            if (map != MapId || MapRoot.Current == null) return;
            var a = NorthData.Get(map, "snowArena");
            var k = NorthData.Get(map, "snowKids");
            var w = NorthData.Get(map, "snowWalls");
            if (a.Length < 4 || k.Length < 2) return;
            mapH = NorthData.Height(map);
            arena = new RectInt(a[0], a[1], a[2] - a[0] + 1, a[3] - a[1] + 1);
            if (ballSprite == null) ballSprite = NorthUtil.Load("snowball", new Vector2(0.5f, 0.5f));
            if (splatSprite == null) splatSprite = NorthUtil.Load("snowsplat", new Vector2(0.5f, 0.5f));
            root = new GameObject("North Snowball").transform;
            // 숨는 자리: 눈벽 칸 바로 아래·위 (놀이터 안, 걸을 수 있는 칸)
            for (int i = 0; i + 1 < w.Length; i += 2)
                foreach (int dy in new[] { 1, -1 })
                {
                    int x = w[i], y = w[i + 1] + dy;
                    if (!arena.Contains(new Vector2Int(x, y)) || !Walkable(Cell(x, y))) continue;
                    hides.Add(Cell(x, y)); hideWall.Add(Cell(w[i], w[i + 1]));
                }
            for (int i = 0; i + 1 < k.Length && kids.Count < 3; i += 2)
            {
                var kid = new Kid { idx = kids.Count, frames = new Sprite[16] };
                string[] dirs = { "d", "u", "l", "r" };
                for (int d = 0; d < 4; d++)
                    for (int f = 0; f < 4; f++)
                        kid.frames[d * 4 + f] = NorthUtil.Load($"snowkid{kid.idx}_{dirs[d]}{f}", new Vector2(0.5f, 0.1f));
                kid.pos = kid.target = Cell(k[i], k[i + 1]);
                kid.home = NearestHide(kid.pos);
                int ix = kid.idx;
                kid.spot = NorthSpot.Make(root, kid.pos, () => ix == 0 ? "눈싸움하는 아이들" : "", Ask, Interactable.Kind.Person);
                kid.spot.verb = "같이 놀기";
                kid.tr = kid.spot.transform;
                kid.sr = kid.spot.gameObject.AddComponent<SpriteRenderer>();
                kid.sr.sprite = kid.frames[0]; kid.sr.sortingOrder = 10;
                kid.stateT = UnityEngine.Random.Range(0.5f, 2.5f);
                kids.Add(kid);
            }
        }

        Vector2 Cell(int x, int y) => NorthUtil.Cell(MapId, x, y);
        Vector2Int JsonCell(Vector2 world)
        {
            var c = MapRoot.Current.WorldToCell(world);
            return new Vector2Int(c.x, mapH - 1 - c.y);
        }
        static bool Walkable(Vector2 world) => MapRoot.Current != null && MapRoot.Current.IsWalkable(MapRoot.Current.WorldToCell(world));
        bool InArena(Vector2 world, int pad = 0)
        {
            var c = JsonCell(world);
            return c.x >= arena.xMin - pad && c.x < arena.xMax + pad && c.y >= arena.yMin - pad && c.y < arena.yMax + pad;
        }
        int NearestHide(Vector2 p)
        {
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < hides.Count; i++) { float d = (hides[i] - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }

        // ---------------- 시작·끝 ----------------

        static void Ask()
        {
            var m = instance;
            if (m == null || m.state != S.None) return;
            int best = (int)NorthSave.Get("snowball:best", 0f);
            string rec = best > 0 ? $"\n(내 최고 점수: {best}점)" : "";
            m.Say(0, "같이 할래?", 2f);
            DialogueUI.Show("잉가 (눈싸움 대장)",
                "같이 할래? 우리 편이 한 명 모자라!\n40초 동안 눈덩이를 많이 맞히는 쪽이 이기는 거야. Space로 던지고, 눈벽 뒤에 숨으면 안 맞아.\n막 고개를 내민 애를 바로 맞히면 2점! 놀이터 밖으로 도망가면 지는 거야." + rec,
                new DialogueUI.Choice("좋아, 하자!", () => m.StartGame()),
                new DialogueUI.Choice("다음에", null));
        }

        void StartGame()
        {
            var pc = PlayerController.Instance;
            if (pc == null || MapRoot.Current == null || !InArena(pc.transform.position, 3))
            {
                Notify.Show("놀이터(눈벽이 있는 눈밭) 가까이에서 시작해요.", 3f);
                return;
            }
            score = hitsGiven = hitsTaken = 0; balls.Clear(); splats.Clear();
            timer = ReadyTime; throwCool = 0.3f; playerFlash = 0f;
            state = S.Ready;
            // Space가 말 걸기로 새지 않게: 놀이터 둘레(4칸) 말 걸기 자리를 잠깐 끈다
            muted.Clear();
            foreach (var it in new List<Interactable>(Interactable.All))
                if (it != null && it.enabled && InArena(it.transform.position, 4)) { it.enabled = false; muted.Add(it); }
            // 아이들은 각자 숨는 자리로
            for (int i = 0; i < kids.Count; i++)
            {
                var kid = kids[i];
                kid.home = -1;
                kid.home = PickHide(kid, pc.transform.position);
                if (kid.home >= 0) kid.target = hides[kid.home];
                kid.peeking = false; kid.stateT = UnityEngine.Random.Range(1.0f, 2.0f); kid.throwAt = -1f; kid.immune = 0f;
            }
            Say(0, "준비…", 1.2f);
        }

        void Finish(bool silent, bool forfeit = false)
        {
            foreach (var it in muted) if (it != null) it.enabled = true;
            muted.Clear();
            balls.Clear();
            var pc = PlayerController.Instance;
            if (pc != null && pc.anim != null) { var psr = pc.anim.GetComponent<SpriteRenderer>(); if (psr != null) psr.color = Color.white; }
            if (silent) { state = S.None; return; }
            state = S.Result;
            Play(endClip);
            string text;
            var rewards = new List<string>();
            if (forfeit) text = "어? 어디 가! …놀이터 밖으로 나가면 지는 거라고 했잖아. 다음엔 끝까지 하자!";
            else
            {
                int best = (int)NorthSave.Get("snowball:best", 0f);
                if (score > best) NorthSave.Set("snowball:best", score);
                text = $"맞힌 눈덩이 {hitsGiven} · 맞은 눈덩이 {hitsTaken}\n점수 {score}점" + (score > best && best > 0 ? " (최고 기록!)" : "") + "\n\n";
                text += score >= 10 ? "우와… 형 진짜 잘한다! 우리 셋이 덤벼도 못 이기겠어!"
                      : score >= 5 ? "칫, 졌다! 다음엔 우리가 이길 거야!"
                      : score >= 0 ? "헤헤, 우리가 이겼다! 눈벽 뒤에 숨는 게 요령이야."
                      : "형 눈사람 됐네! 하하하!";
                if (score >= 5)
                {
                    string tip = Tip();
                    if (tip != null) rewards.Add("토르비: 이거 비밀인데… " + tip);
                }
                if (score >= 10) rewards.Add(Prize());
            }
            rewards.RemoveAll(string.IsNullOrEmpty);
            if (rewards.Count > 0) text += "\n\n" + string.Join("\n", rewards);
            DialogueUI.Show("눈싸움 끝!", text);
            state = S.None;
            for (int i = 0; i < kids.Count; i++) kids[i].stateT = UnityEngine.Random.Range(1.5f, 3f);
        }

        static string Tip()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Traders == null) return null;
            string t = gm.Traders.PriceTip();
            if (string.IsNullOrEmpty(t) || t.Contains("고만고만")) return "요즘은 어디나 값이 비슷하대. 우리 엄마가 그랬어!";
            return "우리 엄마가 그러는데, " + t.Replace("준다더군.", "준대!").Replace("쳐 준다더군", "쳐 준대");
        }

        static string Prize()
        {
            var gm = GameManager.I;
            if (gm == null) return null;
            var item = gm.ItemById(ItemId);
            int today = gm.Clock != null ? gm.Clock.day : 0;
            bool first = !NorthSave.Has("snowball:champ");
            if (!first && (int)NorthSave.Get("snowball:day", -1f) == today) return "(눈사람 장식은 오늘 이미 받았다)";
            string got = "";
            if (item != null && gm.Player.CanCarry(item)) { gm.Player.cargo.Add(item, 1, 0f); got = "눈사람 장식 1개를 받았다"; NorthSave.Set("snowball:day", today); }
            else if (item != null) got = "짐이 가득 차서 눈사람 장식을 못 받았다";
            if (first)
            {
                NorthSave.Set("snowball:champ", 1f);
                return "잉가: 오늘부터 형이 '눈싸움 대장'이야! 이건 우리가 만든 거야." + (got.Length > 0 ? $"\n(칭호 '눈싸움 대장' · {got})" : "\n(칭호 '눈싸움 대장')");
            }
            return got.Length > 0 ? $"프레야: 이거 가져가! ({got})" : null;
        }

        /// <summary>칭호를 받았는지 (다른 북유럽 스크립트·대사에서 쓸 수 있게)</summary>
        public static bool Champion => NorthSave.Has("snowball:champ");

        // ---------------- 매 프레임 ----------------

        void Update()
        {
            string map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (map != lastMap) { lastMap = map; Rebuild(map); }
            if (root == null || kids.Count == 0) return;
            float dt = Time.deltaTime;
            if (state == S.Ready)
            {
                timer -= dt;
                if (timer <= 0f) { state = S.Play; timer = GameTime; Play(go); Say(0, "시작!", 1f); }
            }
            else if (state == S.Play)
            {
                timer -= dt;
                UpdatePlayer(dt);
                if (state == S.Play && timer <= 0f) Finish(false);
            }
            UpdateKids(dt);
            UpdateBalls(dt);
            for (int i = splats.Count - 1; i >= 0; i--) { splats[i].t -= dt; if (splats[i].t <= 0f) splats.RemoveAt(i); }
            if (playerFlash > 0f)
            {
                playerFlash -= dt;
                var pc = PlayerController.Instance;
                if (pc != null && pc.anim != null)
                {
                    var psr = pc.anim.GetComponent<SpriteRenderer>();
                    if (psr != null) psr.color = playerFlash > 0f && Mathf.Repeat(playerFlash, 0.12f) < 0.06f ? new Color(1f, 0.6f, 0.6f) : Color.white;
                }
            }
        }

        void UpdatePlayer(float dt)
        {
            var pc = PlayerController.Instance;
            if (pc == null || pc.Motor == null) return;
            if (GameManager.I != null && GameManager.I.IsTravelling) { Finish(true); return; }
            if (!InArena(pc.Motor.Feet, 1)) { Finish(false, true); return; }
            throwCool -= dt;
            if (throwCool <= 0f && !UIState.Blocking && !UIState.JustClosed && InputReader.InteractPressed())
            {
                throwCool = 0.45f;
                Vector2 from = (Vector2)pc.transform.position + new Vector2(0f, 0.2f);
                Vector2 dir = pc.Motor.Facing;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
                // 바라보는 쪽 35° 안의 가장 가까운 아이에게 살짝 맞춰 준다
                Kid aim = null; float ad = BallRange;
                foreach (var k in kids)
                {
                    Vector2 d = k.pos + new Vector2(0f, 0.6f) - from;
                    float m = d.magnitude;
                    if (m < 0.1f || m > ad || Vector2.Angle(dir, d) > 35f) continue;
                    aim = k; ad = m;
                }
                if (aim != null) dir = (aim.pos + new Vector2(0f, 0.6f) - from).normalized;
                balls.Add(new Ball { p = from, v = dir * PlayerBallSpeed, left = BallRange / PlayerBallSpeed, fromPlayer = true, owner = -1 });
                Play(whoosh, 0.5f);
            }
        }

        void UpdateKids(float dt)
        {
            var pc = PlayerController.Instance;
            bool playing = state == S.Play || state == S.Ready;
            foreach (var k in kids)
            {
                k.flash -= dt; k.immune -= dt; k.stateT -= dt;
                // 움직임: target으로 걷기 (막힌 칸은 건너뛰지 않고 멈춤)
                Vector2 d = k.target - k.pos;
                bool walking = d.magnitude > 0.05f;
                if (walking)
                {
                    float sp = (playing ? 3.2f : 2.2f) * dt;
                    Vector2 step = d.magnitude <= sp ? d : d.normalized * sp;
                    Vector2 np = k.pos + step;
                    if (Walkable(np) && InArena(np)) k.pos = np; else k.target = k.pos;
                    k.facing = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
                    k.walkT += dt * 8f;
                }
                else k.walkT = 0f;

                if (state == S.Play && pc != null) KidPlay(k, pc, dt);
                else if (state == S.None) KidIdle(k);

                // 그리기
                k.tr.position = k.pos;
                int dir = k.facing.y < 0 ? 0 : k.facing.y > 0 ? 1 : k.facing.x < 0 ? 2 : 3;
                var f = k.frames[dir * 4 + (walking ? Mathf.FloorToInt(k.walkT) % 4 : 0)];
                if (f != null) k.sr.sprite = f;
                k.sr.color = k.flash > 0f && Mathf.Repeat(k.flash, 0.12f) < 0.06f ? new Color(0.7f, 0.85f, 1f) : Color.white;
                k.sr.sortingOrder = 10;
            }
        }

        void KidIdle(Kid k)
        {
            if (k.stateT > 0f) return;
            // 숨는 자리를 옮기거나, 다른 아이에게 던지기
            if (UnityEngine.Random.value < 0.5f && hides.Count > 0)
            {
                k.home = UnityEngine.Random.Range(0, hides.Count);
                k.target = hides[k.home];
                k.stateT = UnityEngine.Random.Range(2f, 3.5f);
            }
            else
            {
                var other = kids[(k.idx + 1 + UnityEngine.Random.Range(0, Mathf.Max(1, kids.Count - 1))) % kids.Count];
                if (other != k) Throw(k, other.pos + new Vector2(0f, 0.6f) + UnityEngine.Random.insideUnitCircle * 0.6f);
                k.stateT = UnityEngine.Random.Range(1.5f, 3f);
            }
        }

        void KidPlay(Kid k, PlayerController pc, float dt)
        {
            Vector2 me = pc.transform.position;
            if (k.peeking)
            {
                // 고개 내밀고 있는 동안 한 번 던지고 다시 숨는다
                if (k.throwAt > 0f && Time.time >= k.throwAt)
                {
                    k.throwAt = -1f;
                    Throw(k, me + new Vector2(0f, 0.3f) + UnityEngine.Random.insideUnitCircle * 0.5f);
                }
                if (k.stateT <= 0f)
                {
                    k.peeking = false;
                    if (hides.Count > 0)
                    {
                        // 나에게서 눈벽으로 가려지는 숨는 자리로 (내가 돌아 들어오면 반대편으로 옮긴다)
                        k.home = PickHide(k, me);
                        if (k.home >= 0) k.target = hides[k.home];
                    }
                    k.stateT = UnityEngine.Random.Range(0.8f, 1.8f);
                }
                return;
            }
            if ((k.target - k.pos).magnitude > 0.1f) return;
            if (k.stateT > 0f)
            {
                if (hides.Count > 0 && !Covered(k.home, me) && UnityEngine.Random.value < dt * 1.5f)   // 들켰으면 다른 벽 뒤로
                {
                    k.home = PickHide(k, me); if (k.home >= 0) k.target = hides[k.home];
                }
                return;
            }
            // 고개 내밀기: 숨는 자리에서 옆(왼쪽·오른쪽)으로 한 칸
            Vector2 side = new Vector2(UnityEngine.Random.value < 0.5f ? -1f : 1f, 0f);
            Vector2 peek = k.pos + side;
            if (!Walkable(peek) || !InArena(peek)) peek = k.pos - side;
            if (Walkable(peek) && InArena(peek)) k.target = peek;
            k.peeking = true; k.peekSince = Time.time;
            k.throwAt = Time.time + UnityEngine.Random.Range(0.45f, 0.8f);
            k.stateT = UnityEngine.Random.Range(1.1f, 1.8f);
            Vector2 toMe = me - k.pos;
            k.facing = Mathf.Abs(toMe.x) > Mathf.Abs(toMe.y) ? new Vector2Int(toMe.x > 0 ? 1 : -1, 0) : new Vector2Int(0, toMe.y > 0 ? 1 : -1);
        }

        /// <summary>눈벽이 나와 그 숨는 자리 사이에 있는지 (벽 쪽에 내가 있으면 가려진다)</summary>
        bool Covered(int hide, Vector2 me) => hide >= 0 && hide < hides.Count && Vector2.Dot(hideWall[hide] - hides[hide], me - hides[hide]) > 0.2f;

        int PickHide(Kid k, Vector2 me)
        {
            int pick = -1; float score = float.MinValue;
            for (int c = 0; c < hides.Count; c++)
            {
                if (Occupied(c, k)) continue;
                float s = (Covered(c, me) ? 10f : 0f) - (hides[c] - k.pos).magnitude * 0.4f + UnityEngine.Random.value * 2f;
                if (s > score) { score = s; pick = c; }
            }
            return pick >= 0 ? pick : k.home;
        }

        bool Occupied(int hide, Kid self)
        {
            foreach (var o in kids) if (o != self && o.home == hide) return true;
            return false;
        }

        void Throw(Kid k, Vector2 at)
        {
            Vector2 from = k.pos + new Vector2(0f, 0.5f);
            Vector2 d = at - from;
            if (d.magnitude < 0.2f) return;
            balls.Add(new Ball { p = from, v = d.normalized * KidBallSpeed, left = Mathf.Min(BallRange, d.magnitude + 1.5f) / KidBallSpeed, fromPlayer = false, owner = k.idx });
            k.facing = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
            if (state == S.Play) Play(whoosh, 0.25f);
        }

        void UpdateBalls(float dt)
        {
            var pc = PlayerController.Instance;
            for (int i = balls.Count - 1; i >= 0; i--)
            {
                var b = balls[i];
                b.p += b.v * dt; b.left -= dt;
                bool gone = b.left <= 0f;
                // 눈벽·나무 같은 막힌 칸에 부딪힘 (공 그림자 높이를 빼고 발밑 칸으로 판단)
                if (!gone && !Walkable(b.p - new Vector2(0f, 0.3f))) gone = true;
                if (!gone)
                {
                    if (b.fromPlayer)
                    {
                        foreach (var k in kids)
                        {
                            if (k.immune > 0f || (k.pos + new Vector2(0f, 0.6f) - b.p).magnitude > HitRadius) continue;
                            bool timing = k.peeking && Time.time - k.peekSince < 0.6f;
                            int pts = timing ? 2 : 1;
                            if (state == S.Play) { score += pts; hitsGiven++; }
                            k.flash = 0.5f; k.immune = 0.8f;
                            Say(k.idx, timing ? "으악! 타이밍!" : "으앗!", 1f);
                            if (k.peeking) k.stateT = 0f;   // 바로 다시 숨으러
                            Play(hitClip, 0.6f);
                            gone = true; break;
                        }
                    }
                    else if (state == S.Play && pc != null && ((Vector2)pc.transform.position + new Vector2(0f, 0.3f) - b.p).magnitude < HitRadius)
                    {
                        score -= 1; hitsTaken++; playerFlash = 0.5f;
                        Play(ouch, 0.6f);
                        Say(b.owner, "맞았다!", 1f);
                        var motor = pc.Motor;
                        if (motor != null)
                        {
                            Vector2 push = (Vector2)pc.transform.position + b.v.normalized * 0.3f;
                            if (Walkable(push + motor.feetOffset)) motor.Teleport(push, motor.Facing);
                        }
                        gone = true;
                    }
                    else if (!b.fromPlayer && state != S.Play)
                    {
                        foreach (var k in kids)
                        {
                            if (k.idx == b.owner || (k.pos + new Vector2(0f, 0.6f) - b.p).magnitude > HitRadius) continue;
                            k.flash = 0.4f; Say(k.idx, UnityEngine.Random.value < 0.5f ? "으앗, 차가워!" : "맞았다!", 0.9f);
                            gone = true; break;
                        }
                    }
                }
                if (gone) { splats.Add(new Splat { p = b.p, t = 0.5f }); balls.RemoveAt(i); }
            }
        }

        void Say(int idx, string text, float sec)
        {
            if (idx < 0 || idx >= kids.Count) return;
            kids[idx].say = text; kids[idx].sayUntil = Time.time + sec;
        }

        // ---------------- 화면 ----------------

        void OnGUI()
        {
            if (root == null || kids.Count == 0 || UIState.Blocking) return;
            var cam = Camera.main;
            if (cam == null) return;
            UIStyle.Begin();
            // 눈덩이·튄 눈
            foreach (var b in balls) DrawSprite(cam, ballSprite, b.p, 8f);
            foreach (var s in splats) DrawSprite(cam, splatSprite, s.p, 10f, s.t / 0.5f);
            // 아이 말풍선
            foreach (var k in kids)
                if (!string.IsNullOrEmpty(k.say) && Time.time < k.sayUntil)
                {
                    var p = ToGui(cam, (Vector3)k.pos + new Vector3(0f, 1.9f, 0f));
                    UIStyle.Outlined(new Rect(p.x - 50, p.y - 8, 100, 14), k.say, UIStyle.Center);
                }
            if (state != S.Ready && state != S.Play) return;
            // 놀이터 둘레 (점선)
            Vector2 a = ToGui(cam, CellCorner(arena.xMin, arena.yMin)), c = ToGui(cam, CellCorner(arena.xMax, arena.yMax));
            var r = Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
            var line = new Color(0.85f, 0.3f, 0.3f, 0.55f);
            for (float x = r.xMin; x < r.xMax; x += 8f) { Fill(new Rect(x, r.yMin, 4, 1), line); Fill(new Rect(x, r.yMax, 4, 1), line); }
            for (float y = r.yMin; y < r.yMax; y += 8f) { Fill(new Rect(r.xMin, y, 1, 4), line); Fill(new Rect(r.xMax, y, 1, 4), line); }
            // 위 가운데 점수판
            // 11/08 할 일·이야기 알약 아래에 쌓는다 (y 6 고정이라 할 일 줄과 겹쳤다). 너비는 아래 도움말이 한 줄에 들어가게
            float vw = UIStyle.VirtualWidth;
            const string help = "Space 던지기 · 눈벽 뒤에 숨기 · 밖으로 나가면 기권";
            float bw = Mathf.Min(vw - 12, Mathf.Max(180f, Mathf.Ceil(UIStyle.Small.CalcSize(new GUIContent(help)).x) + 16f));
            var box = new Rect(Mathf.Round((vw - bw) / 2), CuteUI.TopStack(50, 34), bw, 34);
            Fill(box, new Color(0.08f, 0.07f, 0.14f, 0.82f));
            string head = state == S.Ready ? $"<b>눈싸움</b>  준비… {Mathf.CeilToInt(timer)}" : $"<b>눈싸움</b>  남은 시간 <b>{Mathf.CeilToInt(Mathf.Max(0f, timer))}</b>   점수 <b>{score}</b>";
            GUI.Label(new Rect(box.x, box.y + 2, box.width, 16), head, UIStyle.CenterRich);
            GUI.Label(new Rect(box.x, box.y + 18, box.width, 14), help, UIStyle.Small);
        }

        Vector3 CellCorner(int jx, int jy)
        {
            // 위가 0인 칸의 왼쪽 위 모서리 → 월드
            var center = MapRoot.Current.CellCenter(new Vector3Int(jx, mapH - 1 - jy, 0));
            return center + new Vector3(-0.5f, 0.5f, 0f);
        }

        static Vector2 ToGui(Camera cam, Vector3 world)
        {
            var s = cam.WorldToScreenPoint(world);
            return new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
        }

        static void DrawSprite(Camera cam, Sprite sp, Vector2 world, float size, float alpha = 1f)
        {
            if (sp == null) return;
            var p = ToGui(cam, world);
            float ppu = cam.pixelHeight / (cam.orthographicSize * 2f) / UIStyle.Scale;   // 1칸 = GUI 몇 점
            float w = sp.rect.width / 16f * ppu, h = sp.rect.height / 16f * ppu;
            var t = sp.texture;
            var uv = new Rect(sp.rect.x / t.width, sp.rect.y / t.height, sp.rect.width / t.width, sp.rect.height / t.height);
            var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(new Rect(p.x - w / 2, p.y - h / 2, w, h), t, uv);
            GUI.color = old;
        }

        static Texture2D white;
        static void Fill(Rect r, Color c)
        {
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old;
        }
    }
}
