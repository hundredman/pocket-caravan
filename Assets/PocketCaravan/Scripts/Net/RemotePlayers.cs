using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 같은 맵에 있는 다른 플레이어를 화면에 그린다 (2026-10-30, feature/online 5단계).
    /// - NetClient.Peers(서버가 알려 준 위치·방향·걷기·출신)를 보고 사람마다 캐릭터 하나를 세운다.
    /// - 그림은 그 사람 출신(look)의 캐릭터. 모르는 출신이면 내 캐릭터 그림을 빌린다.
    /// - 위치는 1초에 10번 오므로, 그 사이를 부드럽게 이어 움직인다. 멀리 순간이동했으면 바로 옮긴다.
    /// - 머리 위에 하늘색 이름표(마을 사람과 구분). 부딪히지 않고 겹쳐 지나간다(충돌체 없음).
    /// - 맵을 떠나거나 끊기면, 또는 5초 넘게 소식이 없으면 지운다.
    /// - 7단계: 채팅 말풍선이 위치를 묻는다(TryGetPosition).
    /// - 8단계: 리액션(서버 "emote")을 받으면 그 사람 캐릭터가 동작하고(인사·깡총·박수·빙글·춤·하트·쉬기),
    ///   머리 위에 리액션 이름이 크게 뜨고 기호(★ ♪ ♥ z !)가 떠오른다. 동작 모양은 아이디로 고른다(NetEmote 설명 참고).
    /// - 채팅 리액션 명령(/춤 등)은 내 캐릭터에도 같은 동작과 머리 위 표시를 한다(PlaySelf).
    ///   공용 CarryVisual이 그림 자리를 정한 뒤에 덧입히고(실행 순서 1000), 다음 프레임 처음에 되돌린다. 걷기 시작하면 멈춘다.
    /// 공용 코드는 읽기만 한다(PlayerController · StartChoices · PlayerAppearance).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class RemotePlayers : MonoBehaviour
    {
        const float StaleSeconds = 5f, Snap = 4f, Smooth = 14f;
        static readonly Color NameColor = new Color(0.56f, 0.83f, 1f);

        class Avatar
        {
            public GameObject root;
            public CharacterAnimator anim;
            public SpriteRenderer sr;
            public string look, name;
            public Vector3 bodyPos, bodyScale;   // 그림의 원래 자리·크기 (리액션 뒤에 되돌린다)
            public string emote, emoteName;      // 하고 있는 리액션 (동작 모양 · 보여 줄 이름)
            public float emoteStart, emoteLength;
        }

        static readonly Vector2Int[] SpinFacings = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

        static RemotePlayers inst;

        // 내 캐릭터 리액션 (채팅 명령)
        static string selfKind, selfName;
        static float selfStart, selfLength;
        Transform selfBody;
        Vector3 selfPos, selfScale;
        Quaternion selfRot;
        bool selfApplied;

        /// <summary>내 캐릭터가 리액션을 한다 (채팅 명령 /춤 등). 다른 사람에게 알리는 건 NetEmote가 한다</summary>
        public static void PlaySelf(string id, string name)
        {
            selfKind = Kind(id);
            selfName = name;
            selfStart = Time.unscaledTime;
            selfLength = Length(selfKind);
        }

        /// <summary>지난 프레임에 덧입힌 내 그림을 되돌린다</summary>
        void RestoreSelf()
        {
            if (!selfApplied) return;
            selfApplied = false;
            if (selfBody == null) return;
            selfBody.localPosition = selfPos;
            selfBody.localScale = selfScale;
            selfBody.localRotation = selfRot;
        }

        void LateUpdate()
        {
            if (selfKind == null) return;
            var pc = PlayerController.Instance;
            float k = (Time.unscaledTime - selfStart) / selfLength;
            if (pc == null || pc.anim == null || k >= 1f || (pc.Motor != null && pc.Motor.IsMoving)) { selfKind = null; return; }
            selfBody = pc.anim.transform;
            selfPos = selfBody.localPosition; selfScale = selfBody.localScale; selfRot = selfBody.localRotation;
            Pose(selfKind, k, out var lift, out var sx, out var sy, out var tilt);
            selfBody.localPosition = selfPos + new Vector3(0f, Mathf.Round(lift * 16f) / 16f, 0f);
            selfBody.localScale = new Vector3(selfScale.x * sx, selfScale.y * sy, selfScale.z);
            selfBody.localRotation = selfRot * Quaternion.Euler(0f, 0f, tilt);
            selfApplied = true;
        }
        readonly Dictionary<string, Avatar> avatars = new Dictionary<string, Avatar>();
        readonly List<string> gone = new List<string>();
        string shownMap;
        GUIStyle nameStyle, shadowStyle, emoteStyle, symbolStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<RemotePlayers>() != null) return;
            var go = new GameObject("RemotePlayers");
            DontDestroyOnLoad(go);
            go.AddComponent<RemotePlayers>();
        }

        void Awake() => inst = this;
        void OnEnable() => NetClient.Received += OnReceived;
        void OnDisable() => NetClient.Received -= OnReceived;

        void OnReceived(NetClient.Msg m)
        {
            if (m.t != "emote" || string.IsNullOrEmpty(m.id) || !avatars.TryGetValue(m.id, out var a)) return;
            a.emote = Kind(m.e);
            a.emoteName = string.IsNullOrEmpty(m.name) ? m.e : m.name;
            a.emoteStart = Time.unscaledTime;
            a.emoteLength = Length(a.emote);
            Sfx.Play("voice", .3f);
        }

        /// <summary>리액션 아이디 → 동작 모양</summary>
        static string Kind(string e)
        {
            switch (e)
            {
                case "bow": case "wave": case "nod": case "pray": case "jeol": case "salam": return "bow";
                case "clap": return "clap";
                case "spin": case "mask": return "spin";
                case "dance": case "row": case "stretch": case "fan": return "dance";
                case "love": return "love";
                case "nap": case "think": return "rest";
                default: return "hop";   // hop · joy · surprise · 모르는 것
            }
        }

        static float Length(string kind)
        {
            switch (kind)
            {
                case "bow": return 1.3f;
                case "clap": return 1.4f;
                case "spin": return 1.4f;
                case "dance": return 2.2f;
                case "love": return 1.8f;
                case "rest": return 2.6f;
                default: return 1.1f;
            }
        }

        static string Symbol(string kind, out Color color)
        {
            switch (kind)
            {
                case "dance": color = new Color(.56f, .83f, 1f); return "♪";
                case "love": color = new Color(1f, .5f, .62f); return "♥";
                case "rest": color = new Color(.9f, .92f, 1f); return "z";
                case "hop": color = new Color(1f, .7f, .3f); return "!";
                default: color = new Color(1f, .87f, .35f); return "★";
            }
        }

        /// <summary>동작별 몸 모양. 발밑이 기준점이라 납작해져도 땅에 붙어 있다</summary>
        static void Pose(string kind, float k, out float lift, out float sx, out float sy, out float tilt)
        {
            lift = 0f; sx = sy = 1f; tilt = 0f;
            switch (kind)
            {
                case "bow":
                {
                    float d = Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI);
                    sy = 1f - .14f * d; sx = 1f + .05f * d; tilt = -8f * d;
                    break;
                }
                case "clap":
                {
                    float f = k * 4f - Mathf.Floor(k * 4f);
                    lift = Mathf.Sin(f * Mathf.PI) * .08f;
                    if (f < .2f) { sx = 1.08f; sy = .94f; }
                    break;
                }
                case "spin":
                    lift = Mathf.Sin(k * Mathf.PI) * .15f;
                    if (k > .85f) { sx = 1.12f; sy = .9f; }
                    break;
                case "dance":
                    tilt = Mathf.Sin(k * Mathf.PI * 6f) * 12f;
                    lift = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 6f)) * .1f;
                    break;
                case "love":
                {
                    float beat = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f));
                    sx = sy = 1f + beat * .08f;
                    tilt = Mathf.Sin(k * Mathf.PI * 4f) * 5f;
                    break;
                }
                case "rest":
                {
                    float breath = .5f + .5f * Mathf.Sin(k * Mathf.PI * 4f);
                    sy = 1f - .08f * breath; sx = 1f + .04f * breath;
                    break;
                }
                default:   // hop: 두 번 깡총
                {
                    float h = k * 2f, f = h - Mathf.Floor(h);
                    lift = Mathf.Sin(f * Mathf.PI) * .38f;
                    if (f < .15f || f > .9f) { sx = 1.15f; sy = .85f; } else { sx = .92f; sy = 1.08f; }
                    break;
                }
            }
        }

        /// <summary>그림을 원래대로 두거나, 리액션 중이면 동작 모양을 입힌다</summary>
        static void ApplyEmote(Avatar a)
        {
            var t = a.anim.transform;
            t.localPosition = a.bodyPos;
            t.localScale = a.bodyScale;
            t.localRotation = Quaternion.identity;
            if (a.emote == null) return;
            float k = (Time.unscaledTime - a.emoteStart) / a.emoteLength;
            if (k >= 1f) { a.emote = null; return; }
            Pose(a.emote, k, out var lift, out var sx, out var sy, out var tilt);
            t.localPosition = a.bodyPos + new Vector3(0f, Mathf.Round(lift * 16f) / 16f, 0f);
            t.localScale = new Vector3(a.bodyScale.x * sx, a.bodyScale.y * sy, a.bodyScale.z);
            t.localRotation = Quaternion.Euler(0f, 0f, tilt);
            if (a.emote == "spin") a.anim.facing = SpinFacings[Mathf.FloorToInt(k * 8f) % 4];
            a.anim.walking = false;
        }

        /// <summary>화면에 그려진 다른 플레이어의 발밑 위치 (채팅 말풍선이 따라간다)</summary>
        public static bool TryGetPosition(string id, out Vector3 pos)
        {
            pos = default;
            if (inst == null || id == null || !inst.avatars.TryGetValue(id, out var a) || a.root == null) return false;
            pos = a.root.transform.position;
            return true;
        }

        void Update()
        {
            RestoreSelf();
            var net = NetClient.I;
            var map = MapRoot.Current;
            var pc = PlayerController.Instance;
            bool on = net != null && net.Status == NetClient.State.Online && map != null && pc != null && net.CurrentMap == map.mapId;
            if (!on) { ClearAll(); return; }
            if (shownMap != map.mapId) { ClearAll(); shownMap = map.mapId; }

            gone.Clear();
            foreach (var kv in avatars)
                if (!net.Peers.TryGetValue(kv.Key, out var p) || Time.unscaledTime - p.heardAt > StaleSeconds || kv.Value.root == null)
                    gone.Add(kv.Key);
            foreach (var id in gone) Remove(id);

            foreach (var p in net.Peers.Values)
            {
                if (Time.unscaledTime - p.heardAt > StaleSeconds) continue;
                if (!avatars.TryGetValue(p.id, out var a)) avatars[p.id] = a = Create(p, pc);
                if (a.look != p.look) { a.look = p.look; a.anim.frames = FramesFor(p.look, pc); }
                a.name = string.IsNullOrEmpty(p.name) ? p.id : p.name;

                Vector3 target = new Vector3(p.x, p.y, pc.transform.position.z);
                Vector3 now = a.root.transform.position;
                float dist = Vector2.Distance(now, target);
                a.root.transform.position = dist > Snap ? target : Vector3.Lerp(now, target, 1f - Mathf.Exp(-Smooth * Time.deltaTime));
                if (p.fx != 0 || p.fy != 0) a.anim.facing = new Vector2Int(p.fx, p.fy);
                a.anim.walking = p.m || dist > 0.05f;
                if (p.m) a.emote = null;   // 걷기 시작하면 리액션은 멈춘다
                ApplyEmote(a);
            }
        }

        Avatar Create(NetClient.Peer p, PlayerController pc)
        {
            var root = new GameObject($"온라인 플레이어 {p.id}");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(p.x, p.y, pc.transform.position.z);
            var body = new GameObject("그림");
            body.transform.SetParent(root.transform, false);
            var sr = body.AddComponent<SpriteRenderer>();
            var anim = body.AddComponent<CharacterAnimator>();
            var mine = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            if (mine != null)
            {
                // 내 캐릭터와 같은 높이·그리는 순서·재질(밤 조명 등)로
                body.transform.localPosition = pc.anim.transform.localPosition;
                sr.sortingLayerID = mine.sortingLayerID;
                sr.sortingOrder = mine.sortingOrder;
                sr.sharedMaterial = mine.sharedMaterial;
                sr.spriteSortPoint = mine.spriteSortPoint;
            }
            anim.frames = FramesFor(p.look, pc);
            if (anim.frames != null && anim.frames.Length > 0) sr.sprite = anim.frames[0];
            return new Avatar { root = root, anim = anim, sr = sr, look = p.look, name = p.name, bodyPos = body.transform.localPosition, bodyScale = body.transform.localScale };
        }

        /// <summary>출신 아이디 → 그 출신의 캐릭터 그림 16장. 모르면 내 그림을 빌린다</summary>
        static Sprite[] FramesFor(string look, PlayerController pc)
        {
            var choices = StartChoices.Get();
            if (!string.IsNullOrEmpty(look) && choices != null)
                foreach (var c in choices.choices)
                    if (c.origin != null && c.origin.id == look && PlayerAppearance.HasFrames(c.origin))
                        return (Sprite[])c.origin.playerFrames.Clone();
            return pc.anim != null && pc.anim.frames != null ? (Sprite[])pc.anim.frames.Clone() : new Sprite[16];
        }

        void Remove(string id)
        {
            if (avatars.TryGetValue(id, out var a) && a.root != null) Destroy(a.root);
            avatars.Remove(id);
        }

        void ClearAll()
        {
            foreach (var a in avatars.Values) if (a.root != null) Destroy(a.root);
            avatars.Clear();
            shownMap = null;
        }

        /// <summary>머리 위 리액션 이름(톡 튀어나오며 커진다)과 떠오르는 기호 셋</summary>
        void DrawEmote(Camera cam, Vector3 feet, string kind, string label, float start, float length, float scale)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - start) / length);
            if (emoteStyle == null || emoteStyle.font != UIStyle.RegularFont)
            {
                emoteStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.RegularFont, fontSize = UIStyle.BodySize + 2, alignment = TextAnchor.MiddleCenter, richText = true, clipping = TextClipping.Overflow, fontStyle = FontStyle.Bold };
                symbolStyle = new GUIStyle(emoteStyle) { fontSize = UIStyle.BodySize + 4 };
            }
            string symbol = Symbol(kind, out var color);
            var head = cam.WorldToScreenPoint(feet + new Vector3(0f, 1.6f, 0f));
            if (head.z < 0f) return;
            float hx = head.x / scale, hy = (Screen.height - head.y) / scale;
            float pop = k < .15f ? Mathf.Lerp(6f, 0f, k / .15f) : 0f;   // 처음에 아래에서 톡 올라온다
            float alpha = k > .85f ? (1f - k) / .15f : 1f;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            // 11/05 UI B안: 리액션 이름도 말풍선과 같은 종이 알약
            string text = $"{symbol} {label}";
            float tw = CuteUI.Width(text, false, true) + 14f;
            var pill = new Rect(Mathf.Round(hx - tw / 2f), Mathf.Round(hy - 9f + pop), Mathf.Round(tw), 17f);
            CuteUI.Box(pill, 7, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            CuteUI.LabelCenter(pill.center.x, pill.center.y, text, CuteUI.Text, false, true);
            for (int i = 0; i < 3; i++)
            {
                float phase = Mathf.Repeat(k * 1.6f + i / 3f, 1f);
                float sx = hx + Mathf.Sin(i * 2.1f + k * 3f) * 16f;
                float sy = hy - 14f - phase * 26f;
                GUI.color = new Color(color.r, color.g, color.b, alpha * (1f - phase));
                symbolStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(sx - 10f, sy - 10f, 20f, 20f), symbol, symbolStyle);
            }
            GUI.color = old;
        }

        void OnGUI()
        {
            var cam = Camera.main;
            var me = PlayerController.Instance;
            bool selfShow = selfKind != null && me != null;
            if ((avatars.Count == 0 && !selfShow) || cam == null || UIState.Blocking) return;
            UIStyle.Begin();
            GUI.depth = -3;
            if (nameStyle == null || nameStyle.font != UIStyle.SmallFont)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.SmallFont, fontSize = UIStyle.SmallSize, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
                shadowStyle = new GUIStyle(nameStyle);
                shadowStyle.normal.textColor = new Color(0.07f, 0.05f, 0.1f, 0.85f);
                nameStyle.normal.textColor = NameColor;
            }
            float scale = UIStyle.Scale;
            foreach (var a in avatars.Values)
            {
                if (a.root == null) continue;
                var sp = cam.WorldToScreenPoint(a.root.transform.position + new Vector3(0f, 1.15f, 0f));
                if (sp.z < 0f) continue;
                var r = new Rect(sp.x / scale - 60f, (Screen.height - sp.y) / scale - 8f, 120f, 12f);
                foreach (var o in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
                    GUI.Label(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), a.name, shadowStyle);
                GUI.Label(r, a.name, nameStyle);
                if (a.emote != null) DrawEmote(cam, a.root.transform.position, a.emote, a.emoteName, a.emoteStart, a.emoteLength, scale);
            }
            if (selfShow) DrawEmote(cam, me.transform.position, selfKind, selfName, selfStart, selfLength, scale);
        }
    }
}
