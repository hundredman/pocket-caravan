using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 리액션 동작 (전 지역 공용, 2026-11-04 통합: 북유럽 NorthSocial 바퀴 + 베네치아 VeniceReactions를 합침).
    /// R로 바퀴를 연다 (←↑ 이전 · →↓ 다음 · Space 하기 · R/Esc 닫기). 어느 지역에서든 10칸:
    /// - 공통 8: 인사·끄덕·박수(처음부터) / 춤·기쁨·놀람·하트·낮잠(배움)
    /// - 지역 고유 2 (지금 있는 지역 것만 보임): 북 날씨 기도·덜덜 / 베 노 젓기·가면 춤 / 고려 큰절·부채질 / 아라비아 살람·별 보기
    /// 한 번 배우면 어디서나 쓴다 (공용 메모 "react:id"). 통합 전 저장(north:react:*, venice:react:*)도 읽는다.
    /// 그림(자식 CharacterAnimator)의 위치·크기·기울기만 잠깐 바꾸고 다음 프레임 전에 되돌린다. 움직이면 바로 멈춘다.
    /// 가까운 주민은 돌아보고 따라 하거나 깡총 뛰며 한마디 한다.
    /// 지역 효과(날씨 기도로 눈보라 누그러뜨리기 등)는 Performed 이벤트를 듣는 지역 코드가 맡는다.
    /// </summary>
    [DefaultExecutionOrder(1000)]   // CharacterAnimator·CarryVisual이 그림을 정한 뒤에 덧씌운다
    public class Reactions : MonoBehaviour
    {
        public enum Region { Common, North, Venice, Goryeo, Arabia }
        enum Pose { Bow, Nod, Clap, Dance, Hop, Surprise, Love, Nap, Pray, Shiver, Row, Mask, Jeol, Fan, Salam, Stars }

        class Info
        {
            public string id, name, teacher, bubble; public Region region; public bool basic;
            public Pose pose; public float length; public string bit; public string[] cheers; public bool copy;
        }

        static readonly Info[] Moves =
        {
            // 공통 8
            new Info { id = "bow", name = "인사", basic = true, pose = Pose.Bow, length = 1.0f, bubble = "안녕!", cheers = new[] { "안녕!", "어서 와.", "반가워!" }, copy = true },
            new Info { id = "nod", name = "끄덕", basic = true, pose = Pose.Nod, length = 1.0f, bubble = "응응", cheers = new[] { "그렇지, 그렇지.", "맞아 맞아." } },
            new Info { id = "clap", name = "박수", basic = true, pose = Pose.Clap, length = 1.1f, bit = "star", bubble = "짝짝짝", cheers = new[] { "브라보!", "짝짝짝!", "하하, 좋구먼!" }, copy = true },
            new Info { id = "dance", name = "춤", pose = Pose.Dance, length = 2.2f, bit = "note", bubble = "♪♪", cheers = new[] { "멋진 춤이야!", "나도 출래!", "흥겹구먼!" }, copy = true,
                teacher = "여관 사람과 친해지거나 축제에서" },
            new Info { id = "joy", name = "기쁨", pose = Pose.Hop, length = .9f, bit = "dust", bubble = "야호!", cheers = new[] { "좋은 일 있나 봐?", "기운 넘치네!" },
                teacher = "목동·과수원 사람과 친해지면" },
            new Info { id = "surprise", name = "놀람", pose = Pose.Surprise, length = .9f, bit = "star", bubble = "!!", cheers = new[] { "뭐, 뭐야?!", "깜짝이야!" },
                teacher = "장인들과 친해지면" },
            new Info { id = "love", name = "하트", pose = Pose.Love, length = 1.8f, bit = "heart", bubble = "♥", cheers = new[] { "나도 좋아!", "어머나!" },
                teacher = "친구와 친해지거나 떠돌이 상인에게서" },
            new Info { id = "nap", name = "낮잠", pose = Pose.Nap, length = 3.5f, bit = "z", bubble = "쿨…", cheers = new[] { "쉿, 자는구나.", "꾸벅꾸벅…" },
                teacher = "창고지기·룬 지기와 친해지면" },
            // 북유럽
            new Info { id = "pray", region = Region.North, name = "날씨 기도", pose = Pose.Pray, length = 1.8f, bit = "star", bubble = "맑아져라…", cheers = new[] { "날이 개면 좋겠네." },
                teacher = "룬돌 일곱 개를 다 읽고 고리 한가운데에서" },
            new Info { id = "shiver", region = Region.North, name = "덜덜", pose = Pose.Shiver, length = 1.4f, bubble = "으 추워!", cheers = new[] { "불가로 와!", "꿀술 한잔해." },
                teacher = "눈보라 속을 걸어 보면" },
            // 베네치아
            new Info { id = "row", region = Region.Venice, name = "노 젓기", pose = Pose.Row, length = 2.0f, bit = "dust", bubble = "영차!", cheers = new[] { "사공 다 됐네!", "영차, 영차!" },
                teacher = "부라노 어부와 친해지거나 곤돌라 경주 150점으로" },
            new Info { id = "mask", region = Region.Venice, name = "가면 춤", pose = Pose.Mask, length = 2.4f, bit = "confetti", bubble = "♪", cheers = new[] { "카니발이다!", "누구시더라?" }, copy = true,
                teacher = "카니발 밤 리알토 가면 장수에게서" },
            // 고려
            new Info { id = "jeol", region = Region.Goryeo, name = "큰절", pose = Pose.Jeol, length = 1.8f, bubble = "고맙습니다", cheers = new[] { "예의 바르구먼.", "어이쿠, 일어나게." }, copy = true,
                teacher = "개경에 처음 들어가면" },
            new Info { id = "fan", region = Region.Goryeo, name = "부채질", pose = Pose.Fan, length = 1.8f, bit = "dust", bubble = "살랑살랑", cheers = new[] { "시원하겠다!", "나도 부채 하나." },
                teacher = "고려 객주에서 처음 쉬면" },
            // 아라비아
            new Info { id = "salam", region = Region.Arabia, name = "살람", pose = Pose.Salam, length = 1.4f, bubble = "살람!", cheers = new[] { "와 알라이쿰 살람!", "평안하길." }, copy = true,
                teacher = "사나에 처음 들어가면" },
            new Info { id = "stars", region = Region.Arabia, name = "별 보기", pose = Pose.Stars, length = 2.2f, bit = "star", bubble = "…", cheers = new[] { "저 별 따라가면 돼.", "길잡이 별이네." },
                teacher = "아라비아 여관에서 처음 쉬면" },
        };

        /// <summary>통합 전 아이디 → 지금 아이디 (옛 저장·옛 코드가 부르는 이름)</summary>
        static readonly Dictionary<string, string> Alias = new Dictionary<string, string>
        {
            { "wave", "bow" }, { "hop", "joy" }, { "spin", "dance" }, { "stretch", "nap" }, { "think", "nap" },
        };
        static string Norm(string id) => id != null && Alias.TryGetValue(id, out var a) ? a : id;

        /// <summary>지역 효과를 붙이는 자리 (예: 날씨 기도). id는 지금 아이디</summary>
        public static event Action<string> Performed;

        static readonly int[] SpinDirs = { 0, 3, 1, 2 };   // 아래 → 오른쪽 → 위 → 왼쪽 (프레임 줄: 0 아래, 1 위, 2 왼쪽, 3 오른쪽)

        class Body
        {
            public CharacterAnimator anim;
            public Vector3 pos, scale;
            public Quaternion rot;
            public bool applied;
            public float start, length;
            public Pose pose;
            public bool npc;
        }
        struct Bit { public Sprite sprite; public Vector3 from; public Vector2 drift; public float born, life; }
        struct Say { public Transform who; public string text; public float until; }

        static Reactions inst;
        static string testMove;
        bool wheel;
        int sel;
        Vector2 lastMouse;   // 11/08 마우스를 움직였을 때만 칸을 고른다
        readonly List<Body> bodies = new List<Body>();
        readonly List<Bit> bits = new List<Bit>();
        readonly List<Say> says = new List<Say>();
        float nextBit;
        string lastMap;

        // ---------------- 지역 ----------------
        public static Region RegionOfMap(string map)
        {
            if (string.IsNullOrEmpty(map)) return Region.Goryeo;
            if (map.StartsWith("north_")) return Region.North;
            if (map.StartsWith("venice_")) return Region.Venice;
            if (map.StartsWith("arabia_")) return Region.Arabia;
            return Region.Goryeo;   // 고려 다섯 마을 + 중앙 동쪽(transit_)
        }
        public static Region Here => RegionOfMap(MapRoot.Current != null ? MapRoot.Current.mapId : null);

        /// <summary>지금 바퀴에 보이는 동작 (공통 8 + 이 지역 2)</summary>
        static List<Info> Wheel()
        {
            var list = new List<Info>();
            var here = Here;
            foreach (var m in Moves) if (m.region == Region.Common || m.region == here) list.Add(m);
            return list;
        }
        static Info Find(string id) { id = Norm(id); foreach (var m in Moves) if (m.id == id) return m; return null; }

        // ---------------- 배우기 ----------------
        static bool Legacy(string prefix, string id)
        {
            var gm = GameManager.I;
            if (gm == null) return false;
            if (gm.HasMemo(prefix + id)) return true;
            foreach (var kv in Alias) if (kv.Value == id && gm.HasMemo(prefix + kv.Key)) return true;
            return false;
        }
        /// <summary>
        /// 이 그림이 지금 리액션 동작 중인가 (2026-11-11 기우). 온라인 쪽 내 동작(RemotePlayers.PlaySelf)이 같은 그림에 겹쳐 덧입히면
        /// 서로 상대가 바꾼 크기를 '원래 크기'로 기억해 버려서 그림이 납작하게 눌린 채 남았다. 한쪽이 하는 동안 다른 쪽은 쉰다.
        /// </summary>
        public static bool Animating(CharacterAnimator anim)
        {
            if (inst == null || anim == null) return false;
            foreach (var b in inst.bodies) if (b.anim == anim) return true;
            return false;
        }

        public static bool Knows(string id)
        {
            var m = Find(id);
            if (m == null) return false;
            if (m.basic) return true;
            var gm = GameManager.I;
            if (gm == null) return false;
            return gm.HasMemo("react:" + m.id) || Legacy("north:react:", m.id) || Legacy("venice:react:", m.id);
        }
        public static string NameOf(string id) { var m = Find(id); return m != null ? m.name : id; }

        /// <summary>동작을 가르친다. 새로 배웠으면 이름, 이미 알면 null. notify면 "배웠다" 알림도 띄운다</summary>
        public static string Teach(string id, bool notify = false)
        {
            var m = Find(id);
            if (m == null || Knows(m.id) || GameManager.I == null) return null;
            GameManager.I.SetMemo("react:" + m.id, 1f);
            if (notify) Notify.Show($"리액션 '{m.name}'을(를) 배웠다! (R)", 3.5f);
            return m.name;
        }

        /// <summary>아직 모르는 공통 동작 하나 (떠돌이 상인의 동작 책 등). 다 알면 null</summary>
        public static string TeachRandom(bool notify = false)
        {
            var left = new List<string>();
            foreach (var m in Moves) if (m.region == Region.Common && !Knows(m.id)) left.Add(m.id);
            return left.Count == 0 ? null : Teach(left[UnityEngine.Random.Range(0, left.Count)], notify);
        }

        /// <summary>점검용: 모든 동작(네 지역 고유 포함)을 배운 것으로. 몇 개 새로 배웠는지</summary>
        public static int TeachAll()
        {
            int n = 0;
            foreach (var m in Moves) if (Teach(m.id) != null) n++;
            return n;
        }

        /// <summary>점검용: 배운 것 지우기 (처음 아는 3개만 남음). 통합 전 저장 키도 지운다</summary>
        public static void ForgetAll()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            var keys = new List<string>();
            foreach (var m in Moves) { keys.Add("react:" + m.id); keys.Add("north:react:" + m.id); keys.Add("venice:react:" + m.id); }
            foreach (var a in Alias.Keys) { keys.Add("north:react:" + a); keys.Add("venice:react:" + a); }
            foreach (var k in keys) gm.ClearMemo(k);
        }

        /// <summary>점검용: 배웠는지와 상관없이 이 동작을 바로 한다</summary>
        public static void TestPlay(string id) => testMove = Norm(id);

        /// <summary>선물 받은 사람처럼 가장 가까운 주민이 깡총 뛰며 조각을 띄운다</summary>
        public static void NpcCheer(string bit)
        {
            var player = PlayerController.Instance;
            if (inst == null || player == null) return;
            CharacterAnimator best = null; float bd = 9f;
            foreach (var a in FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None))
            {
                if (a == player.anim) continue;
                float d = (a.transform.position - player.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = a; }
            }
            if (best == null) return;
            inst.bodies.Add(new Body { anim = best, start = Time.time + .1f, length = .6f, pose = Pose.Hop, npc = true });
            inst.Emit(bit, best.transform.position, 3, .4f);
        }

        // ---------------- 시작 ----------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null || FindFirstObjectByType<Reactions>() != null) return;
            var go = new GameObject("Reactions");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<Reactions>();
            Inn.Rested += OnInnRested;
        }
        void Awake() { if (inst == null) inst = this; }

        /// <summary>고려·아라비아: 그 지역 여관에서 처음 쉬면 고유 동작 하나</summary>
        static void OnInnRested()
        {
            var r = Here;
            if (r == Region.Goryeo) Teach("fan", true);
            else if (r == Region.Arabia) Teach("stars", true);
        }

        /// <summary>고려·아라비아: 수도에 처음 들어가면 고유 동작 하나 / 북유럽: 눈보라 속을 걸으면 덜덜</summary>
        void CheckRegionLessons(string map)
        {
            if (map == "Gaegyeong") Teach("jeol", true);
            else if (map == "arabia_sanaa") Teach("salam", true);
        }

        static bool Key(char k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case 'R': return kb.rKey.wasPressedThisFrame;
                case '<': return kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
                case '>': return kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
                case '^': return kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
                case 'v': return kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
            }
            return false;
#else
            switch (k)
            {
                case 'R': return Input.GetKeyDown(KeyCode.R);
                case '<': return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
                case '>': return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
                case '^': return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
                case 'v': return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            }
            return false;
#endif
        }

        // ---------------- 매 프레임 ----------------
        void Update()
        {
            Restore();
            var player = PlayerController.Instance;
            var map = MapRoot.Current;
            if (map == null || player == null || player.anim == null || GameManager.I == null || GameManager.I.IsTravelling)
            {
                if (wheel) { wheel = false; UIState.Release(); }
                return;
            }
            if (map.mapId != lastMap)
            {
                lastMap = map.mapId;
                bodies.Clear(); bits.Clear(); says.Clear();
                CheckRegionLessons(map.mapId);
                if (!GameManager.I.HasMemo("react:tips") && !StoryGuide.IntroActive && !StartScreen.IsOpen) {   // 11/11 은샘: 시작 화면·시작 튜토리얼 중에는 미룬다 (끝난 뒤 다음 맵에서)
                    GameManager.I.SetMemo("react:tips", 1f); Notify.Show("R: 리액션 바퀴 (인사·끄덕·박수부터, 나머지는 배워서)", 5f); }
            }
            if (Here == Region.North && NorthAtmosphere.Shown == NorthAtmosphere.Weather.Blizzard && player.Motor.IsMoving) Teach("shiver", true);

            if (player.Motor.IsMoving) bodies.RemoveAll(b => !b.npc);
            says.RemoveAll(s => s.who == null || Time.time > s.until);

            if (testMove != null) { var id = testMove; testMove = null; var m = Find(id); if (m != null) Perform(m); return; }

            var list = Wheel();
            if (wheel)
            {
                if (Key('<') || Key('^')) sel = (sel - 1 + list.Count) % list.Count;
                if (Key('>') || Key('v')) sel = (sel + 1) % list.Count;
                if (InputReader.CancelPressed() || Key('R')) { wheel = false; UIState.Release(); }
                else if (InputReader.InteractPressed())
                {
                    var m = list[sel];
                    if (!Knows(m.id))
                    {
                        Sfx.Play("cancel", .5f);
                        Notify.Show($"아직 모르는 리액션이야. {m.teacher} 배울 수 있어.", 2.5f);
                        return;
                    }
                    wheel = false; UIState.Release(); Perform(m);
                }
                return;
            }
            if (UIState.Blocking || VeniceSeat.IsSitting || !Key('R')) return;   // 앉아 있을 땐 동작 안 함 (그림 덧입힘이 겹치지 않게)
            if (sel >= list.Count) sel = 0;
            wheel = true;
            UIState.Blocking = true;
            Sfx.Play("open", .4f);
        }

        void Perform(Info info)
        {
            var player = PlayerController.Instance;
            bodies.RemoveAll(b => b.anim == player.anim);
            bodies.Add(new Body { anim = player.anim, start = Time.time, length = info.length, pose = info.pose });
            nextBit = Time.time;
            Sfx.Play(info.pose == Pose.Nap ? "cancel" : info.pose == Pose.Love ? "accept" : "voice", .5f);
            if (Here == Region.Venice) VeniceLife.Attract(player.transform.position, 2.5f);   // 광장 비둘기가 다가온다
            NetEmote.Send(info.id, info.name);   // 온라인이면 같은 맵 사람들에게도 보인다

            // 내 머리 위 한마디 (별 보기는 밤이면 길잡이 별)
            string bubble = info.bubble;
            if (info.pose == Pose.Stars) { int h = GameManager.I.Clock.Hour; bubble = h >= 19 || h < 5 ? "길잡이 별!" : "낮엔 별이 안 보이네…"; }
            if (!string.IsNullOrEmpty(bubble)) says.Add(new Say { who = player.transform, text = bubble, until = Time.time + info.length + .3f });

            // 가까운 주민이 돌아보고 따라 하거나 깡총, 한마디
            int said = 0;
            foreach (var a in FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None))
            {
                if (a == player.anim || a.walking || bodies.Exists(b => b.anim == a)) continue;
                Vector2 d = player.transform.position - a.transform.position;
                if (d.sqrMagnitude > 3.2f * 3.2f) continue;
                a.facing = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
                bodies.Add(new Body { anim = a, start = Time.time + .25f + UnityEngine.Random.value * .2f, length = info.copy ? info.length * .7f : .5f, pose = info.copy ? info.pose : Pose.Hop, npc = true });
                if (info.bit != null && info.pose != Pose.Hop) Emit(info.bit, a.transform.position, 2, .4f);
                if (said < 2)
                {
                    says.Add(new Say { who = a.transform, text = info.cheers[UnityEngine.Random.Range(0, info.cheers.Length)], until = Time.time + .6f + info.length });
                    said++;
                }
            }
            Performed?.Invoke(info.id);
        }

        /// <summary>지난 프레임에 바꾼 그림 위치·크기·기울기를 되돌린다</summary>
        void Restore()
        {
            foreach (var b in bodies)
            {
                if (!b.applied || b.anim == null) continue;
                var t = b.anim.transform;
                t.localPosition = b.pos; t.localScale = b.scale; t.localRotation = b.rot;
                b.applied = false;
            }
            bodies.RemoveAll(b => b.anim == null || Time.time > b.start + b.length);
        }

        void LateUpdate()
        {
            foreach (var b in bodies)
            {
                if (b.anim == null || Time.time < b.start) continue;
                var t = b.anim.transform;
                b.pos = t.localPosition; b.scale = t.localScale; b.rot = t.localRotation; b.applied = true;
                float k = (Time.time - b.start) / b.length;
                Shape(b, k, out var lift, out var sx, out var sy, out var tilt, out var dx);
                t.localPosition = b.pos + new Vector3(Snap(dx), Snap(lift), 0f);
                t.localScale = new Vector3(b.scale.x * sx, b.scale.y * sy, b.scale.z);
                t.localRotation = b.rot * Quaternion.Euler(0f, 0f, tilt);
                int dir = b.pose == Pose.Dance && !b.npc ? SpinDirs[Mathf.FloorToInt(k * 8f) % 4]
                        : b.pose == Pose.Mask ? SpinDirs[Mathf.FloorToInt(k * 4f) % 4]
                        : b.pose == Pose.Row ? 3 : b.pose == Pose.Stars ? 1 : -1;   // 노 젓기는 옆모습, 별 보기는 뒤돌아 하늘
                if (dir >= 0 && b.anim.frames != null && b.anim.frames.Length >= 16)
                {
                    var sr = b.anim.GetComponent<SpriteRenderer>();
                    if (sr != null && b.anim.frames[dir * 4] != null) sr.sprite = b.anim.frames[dir * 4];
                }
            }

            // 내 캐릭터 둘레에 떠오르는 조각
            var player = PlayerController.Instance;
            var mine = bodies.Find(b => !b.npc);
            if (mine != null && player != null && Time.time >= nextBit && Time.time >= mine.start)
            {
                float k = (Time.time - mine.start) / mine.length;
                var at = player.transform.position;
                switch (mine.pose)
                {
                    case Pose.Bow: case Pose.Salam: if (k > .4f) { Emit("star", at, 1, .1f); nextBit = float.MaxValue; } break;
                    case Pose.Jeol: if (k > .5f) { Emit("star", at, 2, .3f); nextBit = float.MaxValue; } break;
                    case Pose.Hop: if (k > .35f) { Emit("dust", at, 2, .2f); nextBit = Time.time + .45f; } break;
                    case Pose.Surprise: if (k > .2f) { Emit("star", at + Vector3.up * .3f, 2, .3f); nextBit = float.MaxValue; } break;
                    case Pose.Clap: Emit("star", at + Vector3.up * .2f, 1, .3f); nextBit = Time.time + .3f; break;
                    case Pose.Dance: Emit("note", at, 1, .4f); nextBit = Time.time + .45f; break;
                    case Pose.Love: Emit("heart", at, 1, .4f); nextBit = Time.time + .3f; break;
                    case Pose.Nap: if (k > .15f) { Emit("z", at + new Vector3(.25f, -.3f, 0f), 1, .1f); nextBit = Time.time + .8f; } break;
                    case Pose.Pray: if (k > .3f) { Emit("star", at + Vector3.up * .5f, 1, .4f); nextBit = Time.time + .5f; } break;
                    case Pose.Row: Emit("dust", at + new Vector3(.5f, -.2f, 0f), 1, .1f); nextBit = Time.time + .5f; break;
                    case Pose.Mask: Emit("confetti", at, 2, .6f); nextBit = Time.time + .25f; break;
                    case Pose.Fan: Emit("dust", at + new Vector3(.4f, .6f, 0f), 1, .1f); nextBit = Time.time + .35f; break;
                    case Pose.Stars: if (k > .3f) { Emit("star", at + Vector3.up * 1.2f, 1, .8f); nextBit = Time.time + .5f; } break;
                }
            }
            bits.RemoveAll(p => Time.time > p.born + p.life);
        }

        /// <summary>동작별 몸 모양. lift 위로 뜬 높이(칸), sx·sy 배율, tilt 기울기(도), dx 옆으로 흔들림. 발밑이 기준점</summary>
        static void Shape(Body b, float k, out float lift, out float sx, out float sy, out float tilt, out float dx)
        {
            lift = 0f; sx = sy = 1f; tilt = 0f; dx = 0f;
            switch (b.pose)
            {
                case Pose.Bow:
                {
                    float d = Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI);
                    sy = 1f - .14f * d; sx = 1f + .05f * d; tilt = -8f * d;
                    break;
                }
                case Pose.Nod:
                    lift = -Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)) * .06f; sy = 1f - Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)) * .05f;
                    break;
                case Pose.Hop:
                {
                    int hops = b.npc ? 1 : 2;
                    float h = k * hops, f = h - Mathf.Floor(h);
                    lift = Mathf.Sin(f * Mathf.PI) * (b.npc ? .22f : .38f);
                    if (f < .15f || f > .9f) { sx = 1.15f; sy = .85f; } else { sx = .92f; sy = 1.08f; }
                    break;
                }
                case Pose.Surprise:
                    if (k < .4f) { lift = Mathf.Sin(k / .4f * Mathf.PI) * .4f; sx = .9f; sy = 1.12f; }
                    else if (k < .5f) { sx = 1.12f; sy = .88f; }
                    break;
                case Pose.Clap:
                {
                    float f = k * 4f - Mathf.Floor(k * 4f);
                    lift = Mathf.Sin(f * Mathf.PI) * .08f;
                    sx = f < .2f ? 1.08f : 1f; sy = f < .2f ? .94f : 1f;
                    break;
                }
                case Pose.Dance:
                    tilt = Mathf.Sin(k * Mathf.PI * 6f) * 12f;
                    lift = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 6f)) * .1f;
                    break;
                case Pose.Love:
                {
                    tilt = Mathf.Sin(k * Mathf.PI * 4f) * 5f;
                    float beat = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f));
                    sx = 1f + beat * .06f; sy = 1f + beat * .06f;
                    break;
                }
                case Pose.Nap:
                {
                    float down = Mathf.Clamp01(k / .12f) * Mathf.Clamp01((1f - k) / .1f);
                    float breath = Mathf.Sin(Time.time * 3f) * .03f * down;
                    sx = 1f + .12f * down + breath; sy = 1f - .2f * down - breath;
                    tilt = 6f * down;
                    break;
                }
                case Pose.Pray:
                    lift = Mathf.Sin(k * Mathf.PI * 2f) * .05f + .03f; tilt = Mathf.Sin(k * Mathf.PI * 1.5f) * 4f;
                    break;
                case Pose.Shiver:
                    dx = Mathf.Sin(Time.time * 60f) * .05f; sx = .94f; sy = .96f;
                    tilt = Mathf.Sin(Time.time * 45f) * 3f;
                    break;
                case Pose.Row:
                {
                    float stroke = Mathf.Sin(k * Mathf.PI * 4f);
                    tilt = -10f * stroke; sy = 1f - .05f * Mathf.Abs(stroke); lift = Mathf.Max(0f, stroke) * .04f;
                    break;
                }
                case Pose.Mask:
                    tilt = Mathf.Sin(k * Mathf.PI * 8f) * 8f;
                    lift = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)) * .16f;
                    break;
                case Pose.Jeol:   // 깊이 엎드렸다가 천천히 일어나기
                {
                    float d = k < .35f ? k / .35f : k < .7f ? 1f : 1f - (k - .7f) / .3f;
                    sy = 1f - .32f * d; sx = 1f + .1f * d; tilt = -4f * d;
                    break;
                }
                case Pose.Fan:
                    tilt = Mathf.Sin(k * Mathf.PI * 8f) * 6f; sx = 1f + Mathf.Sin(k * Mathf.PI * 8f) * .03f;
                    break;
                case Pose.Salam:   // 가슴에 손 얹고 살짝 숙이기
                {
                    float d = Mathf.Sin(Mathf.Clamp01(k * 1.2f) * Mathf.PI);
                    sy = 1f - .08f * d; tilt = -5f * d;
                    break;
                }
                case Pose.Stars:
                    lift = Mathf.Sin(k * Mathf.PI) * .04f; sy = 1f + Mathf.Sin(k * Mathf.PI) * .04f;
                    break;
            }
        }

        static float Snap(float v) => Mathf.Round(v * 16f) / 16f;

        void Emit(string kind, Vector3 at, int count, float spread)
        {
            var sprite = VeniceUtil.Sprite(kind == "confetti" ? "confetti" : "fx_" + kind);   // 조각 그림은 Resources/Venice에 있다 (어느 지역에서나 읽힘)
            if (sprite == null) return;
            for (int i = 0; i < count; i++)
            {
                bool dust = kind == "dust";
                bits.Add(new Bit
                {
                    sprite = sprite,
                    from = at + new Vector3(UnityEngine.Random.Range(-spread, spread), dust ? 0f : 1.4f, 0f),
                    drift = dust ? new Vector2((i % 2 == 0 ? -1 : 1) * .5f, .15f) : new Vector2(UnityEngine.Random.Range(-.25f, .25f), kind == "confetti" ? -.4f : .9f),
                    born = Time.time,
                    life = dust ? .4f : kind == "z" ? 1.6f : 1.1f,
                });
            }
        }

        // ---------------- 그리기 ----------------
        void OnGUI()
        {
            if (Camera.main == null) return;
            float scale = Mathf.Max(2f, Mathf.Round(Screen.height / 270f));
            var old = GUI.color;
            foreach (var p in bits)
            {
                float age = (Time.time - p.born) / p.life;
                var world = p.from + (Vector3)(p.drift * (Time.time - p.born)) + Vector3.right * Mathf.Sin((Time.time - p.born) * 5f) * .08f;
                var sp = Camera.main.WorldToScreenPoint(world);
                var tex = p.sprite.texture;
                var tr = p.sprite.textureRect;
                var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                float w = tr.width * scale, h = tr.height * scale;
                GUI.color = new Color(1f, 1f, 1f, age < .7f ? 1f : 1f - (age - .7f) / .3f);
                GUI.DrawTextureWithTexCoords(new Rect(Mathf.Round(sp.x - w / 2f), Mathf.Round(Screen.height - sp.y - h / 2f), w, h), tex, uv);
            }
            GUI.color = old;
            if (says.Count == 0 && !wheel) return;

            UIStyle.Begin();
            GUI.depth = -12;
            foreach (var s in says)
            {
                if (s.who == null) continue;
                var sp = Camera.main.WorldToScreenPoint(s.who.position + new Vector3(0, 2.6f, 0));
                if (sp.z < 0f) continue;
                // 11/07 UI B안: 채팅 말풍선과 같은 종이 말풍선
                float px = Mathf.Round(sp.x / UIStyle.Scale), py = Mathf.Round((Screen.height - sp.y) / UIStyle.Scale);
                float w = Mathf.Max(34f, Mathf.Round(CuteUI.Width(s.text, true) + 12f));
                var r = new Rect(Mathf.Round(px - w / 2f), py - 18f, w, 15f);
                CuteUI.Bubble(r, px, CuteUI.Ink);
                CuteUI.LabelCenter(px, r.center.y, s.text, CuteUI.Text, true);
            }
            if (wheel) DrawWheel();
        }

        /// <summary>
        /// 화면을 어둡게 깔고 10칸을 가로로 넓힌 타원에 놓는다 (칸이 겹치지 않게).
        /// 11/07 UI B안: 칸은 종이 알약, 고른 칸은 청록(상호작용), 아직 못 배운 칸은 옅게 "???", 지역 고유는 노란 별.
        /// 가운데는 "리액션"과 배운 수, 아래는 키캡 안내.
        /// </summary>
        void DrawWheel()
        {
            var list = Wheel();
            float cx = Mathf.Round(UIStyle.VirtualWidth / 2f), cy = Mathf.Round(UIStyle.VirtualHeight / 2f), rx = 128f, ry = 96f;
            bool mouseMoved = false;
            if (Event.current.type == EventType.Repaint) { mouseMoved = Event.current.mousePosition != lastMouse; lastMouse = Event.current.mousePosition; }
            CuteUI.Dim();
            int knownCount = 0;
            foreach (var m in list) if (Knows(m.id)) knownCount++;
            CuteUI.FramedCircle(cx - 29f, cy - 29f, 58);
            CuteUI.LabelCenter(cx, cy - 5f, "리액션", CuteUI.Text, false, true);
            CuteUI.LabelCenter(cx, cy + 9f, $"{knownCount}/{list.Count}", CuteUI.Sub, true);
            for (int i = 0; i < list.Count; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI * 2f / list.Count;
                var r = new Rect(Mathf.Round(cx + Mathf.Cos(a) * rx - 36f), Mathf.Round(cy + Mathf.Sin(a) * ry - 11f), 72f, 22f);
                bool known = Knows(list[i].id), on = i == sel, local = known && list[i].region != Region.Common;
                if (on)
                {
                    CuteUI.Box(r, 10, CuteUI.Interact, CuteUI.Ink, 1, 2, CuteUI.InteractLo);
                    CuteUI.Fill(new Rect(r.x + 6f, r.y + 2f, r.width - 12f, 1f), CuteUI.InteractHi);
                }
                else if (known) CuteUI.Box(r, 10, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
                else CuteUI.Box(r, 10, CuteUI.Paper2, CuteUI.Line, 1);
                string label = known ? list[i].name : "???";
                float lw = CuteUI.Width(label, false, on) + (local ? 11f : 0f);
                float lx = Mathf.Round(r.center.x - lw / 2f);
                if (local) { CuteUI.Icon(CuteUI.Pic.Star4, lx, r.center.y - 3f, 1, on ? CuteUI.White : CuteUI.Deco); lx += 11f; }
                CuteUI.Label(lx, r.center.y, label, on ? CuteUI.OnInteract : known ? CuteUI.Text : CuteUI.Sub, false, on);
                if (mouseMoved && r.Contains(Event.current.mousePosition)) sel = i;   // 11/08 마우스를 올린 칸이 골라진다
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { sel = i; InputReader.Press(' '); }   // 누르면 Space와 같다
            }
            // 아래 키캡 안내: ← → 고르기 · Space 하기 · R 닫기 (마우스: 칸 누르기 = 하기, 빈 곳 누르기 = 닫기)
            string[] keys = { "← →", "Space", "R" }, says = { "고르기", "하기", "닫기" };
            float total = 0f;
            for (int i = 0; i < keys.Length; i++) total += CuteUI.KeycapWidth(keys[i]) + 3f + CuteUI.Width(says[i], true) + (i < keys.Length - 1 ? 10f : 0f);
            var strip = new Rect(Mathf.Round(cx - total / 2f - 8f), Mathf.Round(cy + ry + 18f), Mathf.Round(total + 16f), 19f);
            CuteUI.Pill(strip);
            float x = strip.x + 8f;
            for (int i = 0; i < keys.Length; i++)
            {
                x += CuteUI.Keycap(x, strip.y + 3f, keys[i], true) + 3f;
                x = CuteUI.Label(x, strip.center.y, says[i], CuteUI.Text, true) + 10f;   // Label은 글 끝 x를 돌려준다
            }
            // 11/08 칸 밖(가운데·빈 곳)을 누르면 닫힌다 (Esc와 같다). 칸 버튼이 먼저 그려져서 칸 클릭은 여기까지 오지 않는다
            if (GUI.Button(new Rect(0f, 0f, UIStyle.VirtualWidth, UIStyle.VirtualHeight), GUIContent.none, GUIStyle.none)) InputReader.Press('\x1b');
        }

        void OnDisable() => Restore();
    }
}
