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
    /// - 11/09 다른 플레이어 앞에서 Space: 인사·리액션·선물 창(NetMeet). 장터·NPC 같은 다른 대상이 가까이 있으면 그쪽이 먼저다
    ///   (사람이 노점 앞에 몰려 있어도 Space가 사고팔기로 간다). 만국 상인회에 가입한 사람(tag "guild")은 이름 위에 금빛 표시.
    /// 공용 코드는 읽기만 한다(PlayerController · StartChoices · PlayerAppearance · Interactable).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class RemotePlayers : MonoBehaviour
    {
        const float StaleSeconds = 5f, Snap = 4f, Smooth = 14f;
        /// <summary>내 발밑에서 이 거리 안에 장터·NPC가 있으면 다른 플레이어는 Space 대상에서 잠깐 빠진다 (PlayerController.talkRange 1.8칸보다 조금 넓게)</summary>
        const float OtherTargetRange = 2.2f;
        static readonly Color NameColor = new Color(0.56f, 0.83f, 1f);

        /// <summary>11/09 다른 플레이어 앞 Space 자리. 이름표는 RemotePlayers가 하늘색으로 그리니 이름은 비워 둔다</summary>
        internal class Target : Interactable
        {
            public string id, who;
            public override string DisplayName => "";
            public override string ActionVerb => "어울리기";
            public override void Interact(PlayerController player) => NetMeet.OpenPlayer(id, who);
        }

        class Avatar
        {
            public GameObject root;
            public CharacterAnimator anim;
            public SpriteRenderer sr;
            public Target target;
            public string look, name, tag;
            public Vector3 bodyPos, bodyScale;   // 그림의 원래 자리·크기 (리액션 뒤에 되돌린다)
            public string emote, emoteName;      // 하고 있는 리액션 (동작 모양 · 보여 줄 이름)
            public float emoteStart, emoteLength;
            public string vehicle;               // 11/08 운반 수단 (서버 veh, VehicleLook이 그린다)
            public SpriteRenderer vehicleSr;
            public VehicleTrail trail;
            public float spawnAt;                // 11/11 나타난 시각 (등장 반짝임)
        }

        // 11/11 윤채: 다른 사람이 같은 맵에 들어오면 반짝이며 나타나고 머리 위에 "입장!", 떠나면 연기가 퐁 (내가 맵에 막 들어왔을 때 이미 있던 사람은 조용히)
        float mapSince;
        readonly List<(Vector3 at, float born)> poofs = new List<(Vector3, float)>();
        const float JoinSeconds = 2.2f, PoofSeconds = .5f;

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
            // 11/11 기우: R 리액션(Reactions)이 같은 그림을 움직이는 중이면 겹쳐 덧입히지 않는다 (둘이 겹치면 그림이 눌린 채 남았다)
            if (Reactions.Animating(pc.anim)) { selfKind = null; return; }
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
        /// <summary>11/10 만국 상인회 기념 사진: at에서 range 안의 다른 플레이어 (정면 그림 · 이름)</summary>
        public static void Collect(Vector2 at, float range, List<(Sprite sprite, string name)> into)
        {
            if (inst == null || into == null) return;
            foreach (var a in inst.avatars.Values)
            {
                if (a.root == null || ((Vector2)a.root.transform.position - at).sqrMagnitude > range * range) continue;
                Sprite sp = a.anim != null && a.anim.frames != null && a.anim.frames.Length > 0 && a.anim.frames[0] != null ? a.anim.frames[0]
                          : a.sr != null ? a.sr.sprite : null;
                if (sp != null) into.Add((sp, string.IsNullOrEmpty(a.name) ? "상인" : a.name));
            }
        }

        /// <summary>11/10 그 사람 앞 Space 자리 (만국 상인회 길 안내가 가리킨다). 없으면 null</summary>
        public static Interactable TargetOf(string id) =>
            inst != null && id != null && inst.avatars.TryGetValue(id, out var a) ? a.target : null;

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
            if (shownMap != map.mapId) { ClearAll(); shownMap = map.mapId; mapSince = Time.unscaledTime; }

            gone.Clear();
            foreach (var kv in avatars)
                if (!net.Peers.TryGetValue(kv.Key, out var p) || Time.unscaledTime - p.heardAt > StaleSeconds || kv.Value.root == null)
                    gone.Add(kv.Key);
            foreach (var id in gone) Remove(id);

            bool otherNear = OtherTargetNear(pc.transform.position);
            foreach (var p in net.Peers.Values)
            {
                if (Time.unscaledTime - p.heardAt > StaleSeconds) continue;
                if (!avatars.TryGetValue(p.id, out var a)) avatars[p.id] = a = Create(p, pc);
                if (a.look != p.look) { a.look = p.look; a.anim.frames = FramesFor(p.look, pc); }
                a.vehicle = p.veh;
                a.tag = p.tag;
                a.name = string.IsNullOrEmpty(p.name) ? p.id : p.name;
                if (a.target != null) { a.target.who = a.name; if (a.target.enabled == otherNear) a.target.enabled = !otherNear; }

                Vector3 target = new Vector3(p.x, p.y, pc.transform.position.z);
                Vector3 now = a.root.transform.position;
                float dist = Vector2.Distance(now, target);
                a.root.transform.position = dist > Snap ? target : Vector3.Lerp(now, target, 1f - Mathf.Exp(-Smooth * Time.deltaTime));
                if (p.fx != 0 || p.fy != 0) a.anim.facing = new Vector2Int(p.fx, p.fy);
                a.anim.walking = p.m || dist > 0.05f;
                if (p.m) a.emote = null;   // 걷기 시작하면 리액션은 멈춘다
                ApplyEmote(a);
                if (p.s && a.emote == null)
                {   // 11/08 기우: 앉아 있으면 그 사람 화면과 같은 높이·눌림으로 앞을 보고 앉힌다
                    var bt = a.anim.transform;
                    float sq = p.sq > 0.3f && p.sq < 1.5f ? p.sq : 1f;
                    bt.localPosition = a.bodyPos + new Vector3(0f, Mathf.Clamp(p.sy, -1f, 1f), 0f);
                    bt.localScale = new Vector3(a.bodyScale.x * (sq < 0.99f ? 1.04f : 1f), a.bodyScale.y * sq, a.bodyScale.z);
                    a.anim.facing = Vector2Int.down; a.anim.walking = false;
                }
                VehicleLook.Draw(ref a.vehicleSr, ref a.trail, a.root.transform, a.sr, a.vehicle, new Vector2(a.anim.facing.x, a.anim.facing.y), a.anim.walking, p.ld);
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
            var target = root.AddComponent<Target>();
            target.blocksMovement = false;   // 겹쳐 지나간다 (칸을 막지 않는다)
            target.id = p.id;
            target.who = string.IsNullOrEmpty(p.name) ? p.id : p.name;
            return new Avatar { root = root, anim = anim, sr = sr, target = target, look = p.look, name = p.name, tag = p.tag, bodyPos = body.transform.localPosition, bodyScale = body.transform.localScale, vehicle = p.veh, spawnAt = Time.unscaledTime };
        }

        /// <summary>내 곁에 다른 플레이어가 아닌 Space 대상(장터·NPC·게시판 등)이 있는지</summary>
        static bool OtherTargetNear(Vector2 me)
        {
            foreach (var it in Interactable.All)
            {
                if (it == null || it is Target || !it.isActiveAndEnabled) continue;
                if (((Vector2)it.transform.position - me).sqrMagnitude <= OtherTargetRange * OtherTargetRange) return true;
            }
            return false;
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
            if (avatars.TryGetValue(id, out var a) && a.root != null)
            {
                poofs.Add((a.root.transform.position, Time.unscaledTime));   // 11/11 떠날 때 연기
                Destroy(a.root);
            }
            avatars.Remove(id);
        }

        void ClearAll()
        {
            foreach (var a in avatars.Values) if (a.root != null) Destroy(a.root);
            avatars.Clear();
            shownMap = null;
        }

        /// <summary>11/09 만국 상인회 표시: 이름 바로 위 금빛 알약 "★ 상인회" (리액션 이름이 뜨는 동안은 숨긴다)</summary>
        public static void DrawBadge(float cx, float bottom)
        {
            const string text = "상인회";
            float w = Mathf.Round(CuteUI.Width(text, true, true) + 18f);
            var r = new Rect(Mathf.Round(cx - w / 2f), Mathf.Round(bottom - 11f), w, 11f);
            CuteUI.Box(r, 5, CuteUI.Deco, CuteUI.Ink, 1, 1, CuteUI.SoftShadow);
            CuteUI.Icon(CuteUI.Pic.Star4, r.x + 3, r.y + 2, 1, CuteUI.Ink);
            CuteUI.Label(r.x + 12, r.center.y, text, CuteUI.Ink, true, true);
        }

        /// <summary>머리 위 리액션 이름(톡 튀어나오며 커진다)과 떠오르는 기호 셋</summary>
        /// <summary>11/11 등장: 몸 둘레로 별 여섯 개가 퍼지며 반짝이고, 이름 위에 "입장!" 알약이 톡 떴다가 사라진다</summary>
        void DrawJoin(Camera cam, Avatar a, Rect name, float scale)
        {
            float t = Time.unscaledTime - a.spawnAt;
            var old = GUI.color;
            if (t < .8f)
            {
                var c = cam.WorldToScreenPoint(a.root.transform.position + new Vector3(0f, .55f, 0f));
                Vector2 center = new Vector2(c.x / scale, (Screen.height - c.y) / scale);
                float q = t / .8f, rad = Mathf.Lerp(8f, 22f, 1f - (1f - q) * (1f - q));
                GUI.color = new Color(old.r, old.g, old.b, old.a * (1f - q));
                for (int i = 0; i < 6; i++)
                {
                    float ang = (i * 60f + q * 70f) * Mathf.Deg2Rad;
                    CuteUI.Icon(CuteUI.Pic.Star4, Mathf.Round(center.x + Mathf.Cos(ang) * rad - 3.5f), Mathf.Round(center.y + Mathf.Sin(ang) * rad * .8f - 3.5f));
                }
            }
            const string tag = "입장!";
            float k = Mathf.Clamp01(t / .18f), fade = Mathf.Clamp01((JoinSeconds - t) / .4f);
            float w = Mathf.Round(CuteUI.Width(tag, true, true) + 10f), y = name.y - (a.tag == NetGuild.BadgeId ? 20f : 10f);
            var m = GUI.matrix;
            float s = k < .6f ? Mathf.Lerp(.6f, 1.12f, k / .6f) : Mathf.Lerp(1.12f, 1f, (k - .6f) / .4f);
            GUIUtility.ScaleAroundPivot(new Vector2(s, s), new Vector2(name.center.x, y + 6f) * scale);
            GUI.color = new Color(old.r, old.g, old.b, old.a * fade);
            var pill = new Rect(Mathf.Round(name.center.x - w / 2f), Mathf.Round(y), w, 12f);
            CuteUI.Box(pill, 6, CuteUI.Interact, CuteUI.InteractLo, 1, 1);
            CuteUI.LabelCenter(pill.center.x, pill.center.y, tag, CuteUI.White, true, true);
            GUI.matrix = m; GUI.color = old;
        }

        /// <summary>11/11 떠남: 그 자리에서 작은 연기 여덟 점이 퍼지며 옅어진다</summary>
        void DrawPoofs(Camera cam, float scale)
        {
            float now = Time.unscaledTime;
            poofs.RemoveAll(x => now - x.born > PoofSeconds);
            var old = GUI.color;
            foreach (var (at, born) in poofs)
            {
                float q = (now - born) / PoofSeconds;
                var c = cam.WorldToScreenPoint(at + new Vector3(0f, .45f, 0f));
                if (c.z < 0f) continue;
                Vector2 center = new Vector2(c.x / scale, (Screen.height - c.y) / scale);
                GUI.color = new Color(.95f, .93f, .9f, old.a * .8f * (1f - q));
                float rad = Mathf.Lerp(3f, 14f, q), d = Mathf.Round(Mathf.Lerp(5f, 3f, q));
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * 45f * Mathf.Deg2Rad;
                    GUI.DrawTexture(new Rect(Mathf.Round(center.x + Mathf.Cos(ang) * rad - d / 2f), Mathf.Round(center.y + Mathf.Sin(ang) * rad * .7f - d / 2f), d, d), Texture2D.whiteTexture);
                }
            }
            GUI.color = old;
        }

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
            // 11/11 기우: 기념 사진을 찍는 동안에는 내 머리 위에도 내 닉네임을 띄운다 (사진에 접속한 사람 이름만 남기기로 해서, 나도 같이)
            var net = NetClient.I;
            string myName = UIState.PhotoMode && me != null && net != null && net.Status == NetClient.State.Online ? net.MyName : null;
            bool selfTag = !string.IsNullOrEmpty(myName);
            if ((avatars.Count == 0 && !selfShow && !selfTag && poofs.Count == 0) || cam == null || UIState.Blocking) return;
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
                if (a.tag == NetGuild.BadgeId && a.emote == null) DrawBadge(r.center.x, r.y - 6f);
                if (a.emote != null) DrawEmote(cam, a.root.transform.position, a.emote, a.emoteName, a.emoteStart, a.emoteLength, scale);
                if (Time.unscaledTime - a.spawnAt < JoinSeconds && a.spawnAt - mapSince > 1.2f) DrawJoin(cam, a, r, scale);
            }
            DrawPoofs(cam, scale);
            if (selfTag)
            {
                var sp = cam.WorldToScreenPoint(me.transform.position + new Vector3(0f, 1.15f, 0f));
                if (sp.z >= 0f)
                {
                    var r = new Rect(sp.x / scale - 60f, (Screen.height - sp.y) / scale - 8f, 120f, 12f);
                    foreach (var o in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
                        GUI.Label(new Rect(r.x + o.x, r.y + o.y, r.width, r.height), myName, shadowStyle);
                    GUI.Label(r, myName, nameStyle);
                }
            }
            if (selfShow) DrawEmote(cam, me.transform.position, selfKind, selfName, selfStart, selfLength, scale);
        }
    }
}
