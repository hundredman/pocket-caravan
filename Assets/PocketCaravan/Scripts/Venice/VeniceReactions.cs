using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 리액션 동작 (2026-10-26, 제안). 베네치아·본토 서부 맵에서 R을 누를 때마다
    /// 깡총깡총 → 빙그르르 → 기지개 → 살랑살랑 춤 → 하트 퐁퐁 → 꾸벅 낮잠 순서로 고양이가 몸을 움직인다.
    /// 그림(자식 CharacterAnimator)의 위치·크기·기울기만 잠깐 바꾸고 다음 프레임 전에 되돌린다. 움직이면 바로 멈춘다.
    /// 가까운 주민은 돌아보며 한 번 깡총 뛰고 같은 조각(하트·음표 등)을 띄운다. 광장 비둘기가 다가온다.
    /// 보이기만 한다(저장·경제와 무관). 공용 InputReader에 키를 더하지 않으려고 이 파일에서 R만 직접 읽는다.
    /// </summary>
    [DefaultExecutionOrder(1000)]   // CharacterAnimator·CarryVisual이 그림을 정한 뒤에 덧씌운다
    public class VeniceReactions : MonoBehaviour
    {
        enum Move { Hop, Spin, Stretch, Dance, Love, Nap }
        static readonly Move[] Order = { Move.Hop, Move.Spin, Move.Stretch, Move.Dance, Move.Love, Move.Nap };
        static readonly float[] Length = { .9f, 1.1f, 1.4f, 2.2f, 1.8f, 3.5f };
        static readonly string[] Bits = { "dust", "star", "note", "note", "heart", "z" };
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

        int next;
        static bool testPress;

        /// <summary>점검용: 다음 프레임에 R을 누른 것처럼 동작한다</summary>
        public static void TestPress() => testPress = true;
        readonly List<Body> bodies = new List<Body>();
        readonly List<Bit> bits = new List<Bit>();
        float nextBit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceReactions>() == null)
                GameManager.I.gameObject.AddComponent<VeniceReactions>();
        }

        static bool Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        void Update()
        {
            Restore();
            var player = PlayerController.Instance;
            if (!VeniceUtil.InVenice || player == null || player.anim == null) { bodies.Clear(); bits.Clear(); return; }
            if (player.Motor.IsMoving) bodies.RemoveAll(b => !b.npc);
            bool press = Pressed() || testPress;
            testPress = false;
            if (UIState.Blocking || !press) return;

            var move = Order[next];
            next = (next + 1) % Order.Length;
            bodies.RemoveAll(b => b.anim == player.anim);
            bodies.Add(new Body { anim = player.anim, start = Time.time, length = Length[(int)move], move = move });
            nextBit = Time.time;
            Sfx.Play(move == Move.Nap ? "cancel" : move == Move.Love ? "accept" : "voice", .5f);
            VeniceLife.Attract(player.transform.position, 2.5f);

            // 가까운 주민이 돌아보고 깡총
            foreach (var a in FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None))
            {
                if (a == player.anim || a.walking || bodies.Exists(b => b.anim == a)) continue;
                Vector2 d = player.transform.position - a.transform.position;
                if (d.sqrMagnitude > 3.2f * 3.2f) continue;
                a.facing = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
                bodies.Add(new Body { anim = a, start = Time.time + .25f + Random.value * .2f, length = .5f, move = Move.Hop, npc = true });
                if (move != Move.Hop && move != Move.Spin) Emit(Bits[(int)move], a.transform.position, 2, .4f);
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
                if (b.move == Move.Spin && b.anim.frames != null && b.anim.frames.Length >= 16)
                {
                    var sr = b.anim.GetComponent<SpriteRenderer>();
                    int dir = SpinDirs[Mathf.FloorToInt(k * 8f) % 4];
                    if (sr != null && b.anim.frames[dir * 4] != null) sr.sprite = b.anim.frames[dir * 4];
                }
            }

            // 내 고양이 둘레에 떠오르는 조각
            var player = PlayerController.Instance;
            var mine = bodies.Find(b => !b.npc);
            if (mine != null && player != null && Time.time >= nextBit && Time.time >= mine.start)
            {
                float k = (Time.time - mine.start) / mine.length;
                switch (mine.move)
                {
                    case Move.Hop: if (k > .35f) { Emit("dust", player.transform.position, 2, .2f); nextBit = Time.time + .45f; } break;
                    case Move.Spin: if (k > .8f) { Emit("star", player.transform.position + Vector3.up * .3f, 3, .5f); nextBit = float.MaxValue; } break;
                    case Move.Stretch: if (k > .45f) { Emit("note", player.transform.position, 1, .2f); nextBit = float.MaxValue; } break;
                    case Move.Dance: Emit("note", player.transform.position, 1, .4f); nextBit = Time.time + .45f; break;
                    case Move.Love: Emit("heart", player.transform.position, 1, .4f); nextBit = Time.time + .3f; break;
                    case Move.Nap: if (k > .15f) { Emit("z", player.transform.position + new Vector3(.25f, -.3f, 0f), 1, .1f); nextBit = Time.time + .8f; } break;
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
                case Move.Hop:
                {
                    int hops = b.npc ? 1 : 2;
                    float h = k * hops, f = h - Mathf.Floor(h);
                    lift = Mathf.Sin(f * Mathf.PI) * (b.npc ? .22f : .38f);
                    if (f < .15f || f > .9f) { sx = 1.15f; sy = .85f; }          // 뛰기 전·내려앉을 때 납작
                    else { sx = .92f; sy = 1.08f; }
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
            }
        }

        /// <summary>16px 격자에 맞춰 픽셀이 흔들려 보이지 않게</summary>
        static float Snap(float v) => Mathf.Round(v * 16f) / 16f;

        void Emit(string kind, Vector3 at, int count, float spread)
        {
            var sprite = VeniceUtil.Sprite("fx_" + kind);
            if (sprite == null) return;
            for (int i = 0; i < count; i++)
            {
                bool dust = kind == "dust";
                bits.Add(new Bit
                {
                    sprite = sprite,
                    from = at + new Vector3(Random.Range(-spread, spread), dust ? 0f : 1.4f, 0f),
                    drift = dust ? new Vector2((i % 2 == 0 ? -1 : 1) * .5f, .15f) : new Vector2(Random.Range(-.25f, .25f), .9f),
                    born = Time.time,
                    life = dust ? .4f : kind == "z" ? 1.6f : 1.1f,
                });
            }
        }

        void OnGUI()
        {
            if (bits.Count == 0 || Camera.main == null) return;
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
        }

        void OnDisable() => Restore();
    }
}
