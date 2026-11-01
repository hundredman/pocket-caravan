using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 리액션 동작 (2026-10-26~27, 제안). 베네치아·본토 서부 맵에서 R을 누르면 동작 바퀴가 열린다(방향키로 고르고 Space, R·Esc로 닫기. 10-30 북유럽 리액션 바퀴와 모양·조작 통일).
    /// 처음엔 인사·깡총·박수, 나머지(빙글·기지개·춤·하트·낮잠·노 젓기·가면 춤)는 친구 하트 2(VeniceSocial)나 떠돌이 상인에게서 배운다.
    /// 그림(자식 CharacterAnimator)의 위치·크기·기울기만 잠깐 바꾸고 다음 프레임 전에 되돌린다. 움직이면 바로 멈춘다.
    /// 가까운 주민은 돌아보며 깡총 뛰고 한마디 하며 같은 조각(하트·음표 등)을 띄운다. 광장 비둘기가 다가온다.
    /// 배운 동작만 저장한다(VeniceSave "react:id"). 공용 InputReader에 키를 더하지 않으려고 이 파일에서 R만 직접 읽는다.
    /// </summary>
    [DefaultExecutionOrder(1000)]   // CharacterAnimator·CarryVisual이 그림을 정한 뒤에 덧씌운다
    public class VeniceReactions : MonoBehaviour
    {
        enum Move { Bow, Hop, Clap, Spin, Stretch, Dance, Love, Nap, Row, Mask }
        struct Info { public Move move; public string id, name, teacher; public bool basic; public float length; public string bit; public string[] cheers; }

        static readonly Info[] Moves =
        {
            new Info { move = Move.Bow, id = "bow", name = "인사", basic = true, length = 1.0f, bit = null, cheers = new[] { "안녕!", "어서 와.", "반가워!" } },
            new Info { move = Move.Hop, id = "hop", name = "깡총", basic = true, length = .9f, bit = "dust", cheers = new[] { "하하!", "기운 넘치네!" } },
            new Info { move = Move.Clap, id = "clap", name = "박수", basic = true, length = 1.1f, bit = "star", cheers = new[] { "브라보!", "짝짝짝!" } },
            new Info { move = Move.Spin, id = "spin", teacher = "유리 장인(무라노)과 친해지면(하트 2)", name = "빙글", length = 1.1f, bit = "star", cheers = new[] { "어지럽겠다!", "유리처럼 둥글게!" } },
            new Info { move = Move.Stretch, id = "stretch", teacher = "상인회 서기(리알토)와 친해지면(하트 2)", name = "기지개", length = 1.4f, bit = "note", cheers = new[] { "개운하지?", "나도 한번!" } },
            new Info { move = Move.Dance, id = "dance", teacher = "여관 주인(리알토)과 친해지면(하트 2)", name = "춤", length = 2.2f, bit = "note", cheers = new[] { "멋진 춤이야!", "카니발 같아!" } },
            new Info { move = Move.Love, id = "love", teacher = "과수원 일꾼(산테라스모)과 친해지면(하트 2)", name = "하트", length = 1.8f, bit = "heart", cheers = new[] { "나도 좋아!", "어머나!" } },
            new Info { move = Move.Nap, id = "nap", teacher = "창고지기(키오자)와 친해지면(하트 2)", name = "낮잠", length = 3.5f, bit = "z", cheers = new[] { "쉿, 자는구나.", "꾸벅꾸벅…" } },
            new Info { move = Move.Row, id = "row", teacher = "부라노 어부와 친해지거나(하트 2) 곤돌라 경주 150점", name = "노 젓기", length = 2.0f, bit = "dust", cheers = new[] { "사공 다 됐네!", "영차, 영차!" } },
            new Info { move = Move.Mask, id = "mask", teacher = "카니발 밤 리알토 가면 장수에게", name = "가면 춤", length = 2.4f, bit = "confetti", cheers = new[] { "카니발이다!", "누구시더라?" } },
        };
        static readonly int[] SpinDirs = { 0, 3, 1, 2 };   // 아래 → 오른쪽 → 위 → 왼쪽

        class Body
        {
            public CharacterAnimator anim;
            public Vector3 pos, scale;
            public Quaternion rot;
            public bool applied;
            public float start, length;
            public Move move;
            public bool npc;
        }

        struct Bit { public Sprite sprite; public Vector3 from; public Vector2 drift; public float born, life; }
        struct Say { public Transform who; public string text; public float until; }

        static VeniceReactions inst;
        static int testMove = -1;
        bool wheel;
        int sel;
        readonly List<Body> bodies = new List<Body>();
        readonly List<Bit> bits = new List<Bit>();
        readonly List<Say> says = new List<Say>();
        float nextBit;

        public static string NameOf(string id) { foreach (var m in Moves) if (m.id == id) return m.name; return id; }

        public static bool Knows(int i) => Moves[i].basic || VeniceSave.Has("react:" + Moves[i].id);

        /// <summary>동작을 가르친다. 새로 배웠으면 이름, 이미 알면 null</summary>
        public static string Teach(string id)
        {
            for (int i = 0; i < Moves.Length; i++)
                if (Moves[i].id == id && !Knows(i)) { VeniceSave.Set("react:" + id, 1f); return Moves[i].name; }
            return null;
        }

        /// <summary>아직 모르는 동작 하나를 가르친다 (떠돌이 상인의 동작 책). 다 알면 null</summary>
        public static string TeachRandom()
        {
            var left = new List<string>();
            for (int i = 0; i < Moves.Length; i++) if (!Knows(i)) left.Add(Moves[i].id);
            return left.Count == 0 ? null : Teach(left[Random.Range(0, left.Count)]);
        }

        /// <summary>점검용: 배웠는지와 상관없이 이 동작을 바로 한다 (id: hop, spin, …)</summary>
        public static void TestPlay(string id) { for (int i = 0; i < Moves.Length; i++) if (Moves[i].id == id) testMove = i; }

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
            inst.bodies.Add(new Body { anim = best, start = Time.time + .1f, length = .6f, move = Move.Hop, npc = true });
            inst.Emit(bit, best.transform.position, 3, .4f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceReactions>() == null)
                inst = GameManager.I.gameObject.AddComponent<VeniceReactions>();
        }

        void Awake() => inst = this;

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
            return k == 'R' && Input.GetKeyDown(KeyCode.R);
#endif
        }

        void Update()
        {
            Restore();
            var player = PlayerController.Instance;
            if (!VeniceUtil.InVenice || player == null || player.anim == null)
            {
                bodies.Clear(); bits.Clear(); says.Clear();
                if (wheel) { wheel = false; UIState.Release(); }
                return;
            }
            if (player.Motor.IsMoving) bodies.RemoveAll(b => !b.npc);
            says.RemoveAll(s => s.who == null || Time.time > s.until);

            if (testMove >= 0) { int m = testMove; testMove = -1; Perform(m); return; }

            if (wheel)
            {
                // 2026-10-30 북유럽 리액션 바퀴와 같은 조작: ←↑ 이전 · →↓ 다음 · Space 하기 · R/Esc 닫기
                if (Key('<') || Key('^')) sel = Step(sel, -1);
                if (Key('>') || Key('v')) sel = Step(sel, 1);
                if (InputReader.CancelPressed() || Key('R')) { wheel = false; UIState.Release(); }
                else if (InputReader.InteractPressed())
                {
                    if (!Knows(sel))                                             // 아직 못 배운 동작: 배우는 곳을 알려 준다
                    {
                        Sfx.Play("cancel", .5f);
                        Notify.Show($"아직 모르는 리액션이야. {Moves[sel].teacher} 배울 수 있어.", 2.5f);
                        return;
                    }
                    wheel = false; UIState.Release(); Perform(sel);
                }
                return;
            }
            if (UIState.Blocking || VeniceSeat.IsSitting || !Key('R')) return;   // 앉아 있을 땐 동작 안 함 (그림 덧입힘이 겹치지 않게)
            wheel = true;
            UIState.Blocking = true;
            Sfx.Play("open", .4f);
        }

        static int Step(int from, int dir) => (from + dir + Moves.Length) % Moves.Length;   // 잠긴 칸도 고를 수 있다 (배우는 곳을 보여 준다)

        void Perform(int i)
        {
            var player = PlayerController.Instance;
            var info = Moves[i];
            bodies.RemoveAll(b => b.anim == player.anim);
            bodies.Add(new Body { anim = player.anim, start = Time.time, length = info.length, move = info.move });
            nextBit = Time.time;
            Sfx.Play(info.move == Move.Nap ? "cancel" : info.move == Move.Love ? "accept" : "voice", .5f);
            VeniceLife.Attract(player.transform.position, 2.5f);
            NetEmote.Send(info.id, info.name);   // 온라인이면 같은 맵 사람들에게도 보인다 (feature/online)

            // 가까운 주민이 돌아보고 깡총, 한마디
            int said = 0;
            foreach (var a in FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None))
            {
                if (a == player.anim || a.walking || bodies.Exists(b => b.anim == a)) continue;
                Vector2 d = player.transform.position - a.transform.position;
                if (d.sqrMagnitude > 3.2f * 3.2f) continue;
                a.facing = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
                bool copy = info.move == Move.Dance || info.move == Move.Mask || info.move == Move.Clap || info.move == Move.Bow;
                bodies.Add(new Body { anim = a, start = Time.time + .25f + Random.value * .2f, length = copy ? info.length * .7f : .5f, move = copy ? info.move : Move.Hop, npc = true });
                if (info.bit != null && info.move != Move.Hop) Emit(info.bit, a.transform.position, 2, .4f);
                if (said < 2)
                {
                    says.Add(new Say { who = a.transform, text = info.cheers[Random.Range(0, info.cheers.Length)], until = Time.time + .6f + info.length });
                    said++;
                }
            }
        }

        /// <summary>지난 프레임에 바꾼 그림 위치·크기·기울기를 되돌린다 (다른 스크립트가 덮어쓰지 않아도 쌓이지 않게)</summary>
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
                float k = (Time.time - b.start) / b.length;        // 0..1
                Pose(b, k, out var lift, out var sx, out var sy, out var tilt);
                t.localPosition = b.pos + new Vector3(0f, Snap(lift), 0f);
                t.localScale = new Vector3(b.scale.x * sx, b.scale.y * sy, b.scale.z);
                t.localRotation = b.rot * Quaternion.Euler(0f, 0f, tilt);
                int dir = b.move == Move.Spin || b.move == Move.Mask ? SpinDirs[Mathf.FloorToInt(k * (b.move == Move.Spin ? 8f : 4f)) % 4]
                        : b.move == Move.Row ? 3 : -1;                  // 노 젓기는 옆모습
                if (dir >= 0 && b.anim.frames != null && b.anim.frames.Length >= 16)
                {
                    var sr = b.anim.GetComponent<SpriteRenderer>();
                    if (sr != null && b.anim.frames[dir * 4] != null) sr.sprite = b.anim.frames[dir * 4];
                }
            }

            // 내 고양이 둘레에 떠오르는 조각
            var player = PlayerController.Instance;
            var mine = bodies.Find(b => !b.npc);
            if (mine != null && player != null && Time.time >= nextBit && Time.time >= mine.start)
            {
                float k = (Time.time - mine.start) / mine.length;
                var at = player.transform.position;
                switch (mine.move)
                {
                    case Move.Bow: if (k > .4f) { Emit("star", at, 1, .1f); nextBit = float.MaxValue; } break;
                    case Move.Hop: if (k > .35f) { Emit("dust", at, 2, .2f); nextBit = Time.time + .45f; } break;
                    case Move.Clap: Emit("star", at + Vector3.up * .2f, 1, .3f); nextBit = Time.time + .3f; break;
                    case Move.Spin: if (k > .8f) { Emit("star", at + Vector3.up * .3f, 3, .5f); nextBit = float.MaxValue; } break;
                    case Move.Stretch: if (k > .45f) { Emit("note", at, 1, .2f); nextBit = float.MaxValue; } break;
                    case Move.Dance: Emit("note", at, 1, .4f); nextBit = Time.time + .45f; break;
                    case Move.Love: Emit("heart", at, 1, .4f); nextBit = Time.time + .3f; break;
                    case Move.Nap: if (k > .15f) { Emit("z", at + new Vector3(.25f, -.3f, 0f), 1, .1f); nextBit = Time.time + .8f; } break;
                    case Move.Row: Emit("dust", at + new Vector3(.5f, -.2f, 0f), 1, .1f); nextBit = Time.time + .5f; break;
                    case Move.Mask: Emit("confetti", at, 2, .6f); nextBit = Time.time + .25f; break;
                }
            }
            bits.RemoveAll(p => Time.time > p.born + p.life);
        }

        /// <summary>동작별 몸 모양. lift 위로 뜬 높이(칸), sx·sy 가로·세로 배율, tilt 기울기(도). 발밑이 기준점이라 납작해져도 땅에 붙어 있다.</summary>
        static void Pose(Body b, float k, out float lift, out float sx, out float sy, out float tilt)
        {
            lift = 0f; sx = sy = 1f; tilt = 0f;
            switch (b.move)
            {
                case Move.Bow:
                {
                    float d = Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI);   // 숙였다가 펴기
                    sy = 1f - .14f * d; sx = 1f + .05f * d; tilt = -8f * d;
                    break;
                }
                case Move.Hop:
                {
                    int hops = b.npc ? 1 : 2;
                    float h = k * hops, f = h - Mathf.Floor(h);
                    lift = Mathf.Sin(f * Mathf.PI) * (b.npc ? .22f : .38f);
                    if (f < .15f || f > .9f) { sx = 1.15f; sy = .85f; }          // 뛰기 전·내려앉을 때 납작
                    else { sx = .92f; sy = 1.08f; }
                    break;
                }
                case Move.Clap:
                {
                    float f = k * 4f - Mathf.Floor(k * 4f);
                    lift = Mathf.Sin(f * Mathf.PI) * .08f;
                    sx = f < .2f ? 1.08f : 1f; sy = f < .2f ? .94f : 1f;
                    break;
                }
                case Move.Spin:
                    lift = Mathf.Sin(k * Mathf.PI) * .12f;
                    if (k > .85f) { sx = 1.12f; sy = .9f; }
                    break;
                case Move.Stretch:
                    if (k < .3f) { float e = k / .3f; sx = 1f + .2f * e; sy = 1f - .2f * e; }      // 웅크렸다가
                    else if (k < .75f) { sx = .9f; sy = 1.15f; lift = .05f; }                 // 쭈욱
                    else { float e = (k - .75f) / .25f; sx = Mathf.Lerp(.9f, 1f, e); sy = Mathf.Lerp(1.15f, 1f, e); }
                    break;
                case Move.Dance:
                    tilt = Mathf.Sin(k * Mathf.PI * 6f) * 12f;
                    lift = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 6f)) * .1f;
                    break;
                case Move.Love:
                    tilt = Mathf.Sin(k * Mathf.PI * 4f) * 5f;
                    float beat = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f));
                    sx = 1f + beat * .06f; sy = 1f + beat * .06f;
                    break;
                case Move.Nap:
                    float down = Mathf.Clamp01(k / .12f) * Mathf.Clamp01((1f - k) / .1f);
                    float breath = Mathf.Sin(Time.time * 3f) * .03f * down;
                    sx = 1f + .12f * down + breath; sy = 1f - .2f * down - breath;
                    tilt = 6f * down;
                    break;
                case Move.Row:                                   // 앞으로 숙여 당기고 뒤로 젖히기
                    float stroke = Mathf.Sin(k * Mathf.PI * 4f);
                    tilt = -10f * stroke; sy = 1f - .05f * Mathf.Abs(stroke); lift = Mathf.Max(0f, stroke) * .04f;
                    break;
                case Move.Mask:
                    tilt = Mathf.Sin(k * Mathf.PI * 8f) * 8f;
                    lift = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)) * .16f;
                    break;
            }
        }

        /// <summary>16px 격자에 맞춰 픽셀이 흔들려 보이지 않게</summary>
        static float Snap(float v) => Mathf.Round(v * 16f) / 16f;

        void Emit(string kind, Vector3 at, int count, float spread)
        {
            var sprite = VeniceUtil.Sprite(kind == "confetti" ? "confetti" : "fx_" + kind);
            if (sprite == null) return;
            for (int i = 0; i < count; i++)
            {
                bool dust = kind == "dust";
                bits.Add(new Bit
                {
                    sprite = sprite,
                    from = at + new Vector3(Random.Range(-spread, spread), dust ? 0f : 1.4f, 0f),
                    drift = dust ? new Vector2((i % 2 == 0 ? -1 : 1) * .5f, .15f) : new Vector2(Random.Range(-.25f, .25f), kind == "confetti" ? -.4f : .9f),
                    born = Time.time,
                    life = dust ? .4f : kind == "z" ? 1.6f : 1.1f,
                });
            }
        }

        void OnGUI()
        {
            if (Camera.main == null) return;
            float scale = Mathf.Max(2f, Mathf.Round(Screen.height / 270f));   // 고양이 머리보다 작게
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
            foreach (var s in says)                                 // 주민 한마디 말풍선
            {
                if (s.who == null) continue;
                var sp = Camera.main.WorldToScreenPoint(s.who.position + new Vector3(0, 2.6f, 0));
                var p = new Vector2(sp.x / UIStyle.Scale, (Screen.height - sp.y) / UIStyle.Scale);
                float w = Mathf.Max(40, s.text.Length * 9 + 12);
                var r = new Rect(p.x - w / 2f, p.y - 16, w, 16);
                GUI.color = new Color(1f, .98f, .92f, .95f); GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
                GUI.Label(r, $"<color=#3A2E3A><size=9>{s.text}</size></color>", UIStyle.CenterRich);
            }
            if (wheel) DrawWheel();
        }

        /// <summary>2026-10-30 북유럽 리액션 바퀴(NorthSocial.DrawWheel)와 같은 모양: 화면을 어둡게 깔고 칸을 둥글게 놓는다.
        /// 베네치아는 동작이 10개라 북유럽(8개, 반지름 92)보다 가로로 넓힌 타원에 놓아 칸이 겹치지 않게 한다.</summary>
        void DrawWheel()
        {
            float cx = UIStyle.VirtualWidth / 2f, cy = UIStyle.VirtualHeight / 2f, rx = 128f, ry = 96f;
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0.05f, 0.4f);
            GUI.DrawTexture(new Rect(0, 0, UIStyle.VirtualWidth, UIStyle.VirtualHeight), Texture2D.whiteTexture);
            GUI.color = old;
            for (int i = 0; i < Moves.Length; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI * 2f / Moves.Length;
                var r = new Rect(cx + Mathf.Cos(a) * rx - 38, cy + Mathf.Sin(a) * ry - 13, 76, 26);
                bool known = Knows(i);
                GUI.Box(r, GUIContent.none, UIStyle.Box);
                string label = known ? Moves[i].name : "???";
                if (i == sel) label = $"<color=#ffde59><b>▶ {label}</b></color>";
                else if (!known) label = $"<color=#8a8296>{label}</color>";
                GUI.Label(r, label, OneLine);
            }
            GUI.Label(new Rect(cx - 80, cy - 10, 160, 20), "<b>리액션</b>", UIStyle.CenterRich);
            GUI.Label(new Rect(cx - 120, cy + ry + 22, 240, 16), "← → 고르기 · Space 하기 · R/Esc 닫기", UIStyle.CenterRich);
        }

        static GUIStyle oneLine;
        static GUIStyle OneLine => oneLine ?? (oneLine = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow });

        void OnDisable() => Restore();
    }
}
