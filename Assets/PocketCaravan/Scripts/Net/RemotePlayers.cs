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
    /// 공용 코드는 읽기만 한다(PlayerController · StartChoices · PlayerAppearance).
    /// </summary>
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
        }

        readonly Dictionary<string, Avatar> avatars = new Dictionary<string, Avatar>();
        readonly List<string> gone = new List<string>();
        string shownMap;
        GUIStyle nameStyle, shadowStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<RemotePlayers>() != null) return;
            var go = new GameObject("RemotePlayers");
            DontDestroyOnLoad(go);
            go.AddComponent<RemotePlayers>();
        }

        void Update()
        {
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
            return new Avatar { root = root, anim = anim, sr = sr, look = p.look, name = p.name };
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

        void OnGUI()
        {
            var cam = Camera.main;
            if (avatars.Count == 0 || cam == null || UIState.Blocking) return;
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
            }
        }
    }
}
