using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 놀거리 알림 (전 지역 공용, 2026-11-05 통합: 베네치아 VeniceActivityHints를 그대로 올림).
    /// - 머리 위 표시: 오늘 아직 할 수 있는 놀거리 자리 위에 별과 이름("★ 유리 불기"). 14칸 안에서 보이고 4칸 안이면 원래 이름표가 대신한다.
    ///   오늘 이미 했으면 흐린 "내일 다시".
    /// - 화면 가장자리 화살표: 가장 가까운 '오늘 할 수 있는' 놀거리가 화면 밖이면 그쪽 가장자리에.
    /// - 맵 알림: 맵에 들어오면(하루 한 번, 맵마다) "여기서 할 수 있는 것: …".
    /// 어떤 자리가 놀거리인지는 두 가지로 등록한다. 이름이 null이면 놀거리로 치지 않는다.
    ///   Rules[종류] = 자리 → (이름, 오늘 했는지)         — 자기 클래스가 있는 자리
    ///   NameRules[이름표] = 자리 → (이름, 오늘 했는지)   — NorthSpot처럼 한 클래스를 여럿이 쓰는 자리 (이름표로 구분)
    /// 공용 자리(떠돌이 상인·전시실·수집물·공방)는 여기서 등록하고, 지역 자리는 지역 코드가 등록한다.
    /// 저장: 공용 메모 "hint:맵" (오늘 알림했는지).
    /// </summary>
    public class ActivityHints : MonoBehaviour
    {
        public static readonly Dictionary<Type, Func<Interactable, (string name, bool done)>> Rules = new Dictionary<Type, Func<Interactable, (string, bool)>>
        {
            { typeof(Peddler.Spot), _ => ("떠돌이 상인", false) },
            { typeof(Museum.Spot), i => ("전시실 기증", Museum.Complete(((Museum.Spot)i).region)) },
            { typeof(Collectibles.Spot), i => (Collectibles.Read(((Collectibles.Spot)i).region, ((Collectibles.Spot)i).index) ? null : Collectibles.Of(((Collectibles.Spot)i).region)?.name, false) },
            { typeof(WorkshopSpot), i => (Crafting.Label(((WorkshopSpot)i).recipeId), Crafting.DoneForToday(((WorkshopSpot)i).recipeId)) },
            { typeof(GoryeoPastime), _ => ("나루 낚시", false) },
        };
        public static readonly Dictionary<string, Func<Interactable, (string name, bool done)>> NameRules = new Dictionary<string, Func<Interactable, (string, bool)>>();

        const float Far = 14f, Near = 4f;
        readonly List<(Interactable spot, string name, bool done)> spots = new List<(Interactable, string, bool)>();
        float nextScan;
        string lastMap;
        float announceAt = -1f;
        Sprite star;

        static string Map => MapRoot.Current != null ? MapRoot.Current.mapId : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<ActivityHints>() == null)
                GameManager.I.gameObject.AddComponent<ActivityHints>();
        }

        void Update()
        {
            if (Map == null) { spots.Clear(); lastMap = null; return; }
            if (Map != lastMap) { lastMap = Map; announceAt = Time.time + .8f; nextScan = 0f; }   // 자리가 다 놓인 뒤 알린다
            if (Time.time >= nextScan) { nextScan = Time.time + .5f; Scan(); }
            if (announceAt > 0f && Time.time >= announceAt) { announceAt = -1f; Announce(); }
        }

        static bool Rule(Interactable it, out (string name, bool done) r)
        {
            r = (null, false);
            if (Rules.TryGetValue(it.GetType(), out var f)) { r = f(it); return r.name != null; }
            string label = it.DisplayName;
            if (!string.IsNullOrEmpty(label) && NameRules.TryGetValue(label, out var g)) { r = g(it); return r.name != null; }
            return false;
        }

        void Scan()
        {
            spots.Clear();
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled) continue;
                if (Rule(it, out var r)) spots.Add((it, r.name, r.done));
            }
        }

        void Announce()
        {
            var gm = GameManager.I;
            string key = "hint:" + Map;
            if (gm == null || spots.Count == 0 || Mathf.RoundToInt(gm.GetMemo(key, -1f)) == gm.Clock.day) return;
            var names = new List<string>();
            foreach (var s in spots) if (!s.done && !names.Contains(s.name)) names.Add(s.name);
            if (names.Count == 0) return;
            gm.SetMemo(key, gm.Clock.day);
            Notify.Show("여기서 할 수 있는 것: " + string.Join(" · ", names) + "  (별 표시를 찾아가 보자)", 5f);
        }

        void OnGUI()
        {
            var cam = Camera.main; var pc = PlayerController.Instance;
            if (Map == null || cam == null || pc == null || UIState.Blocking || spots.Count == 0) return;
            if (GameManager.I != null && GameManager.I.IsTravelling) return;
            if (star == null) star = Collectibles.LoadSprite("Common/fx_star", new Vector2(0.5f, 0.5f));
            UIStyle.Begin();
            GUI.depth = -4;
            float scale = UIStyle.Scale, bob = Mathf.Round(Mathf.Sin(Time.time * 3f) * 2f);
            (Interactable spot, string name, bool done) nearest = (null, null, false); float best = float.MaxValue;
            foreach (var s in spots)
            {
                if (s.spot == null) continue;
                Vector2 d = s.spot.transform.position - pc.transform.position;
                float dist = d.magnitude;
                if (!s.done && dist < best) { best = dist; nearest = s; }
                if (dist > Far || dist < Near) continue;
                var sp = cam.WorldToScreenPoint(s.spot.transform.position + new Vector3(0f, 1.6f, 0f));
                if (sp.z < 0f) continue;
                Badge(new Vector2(sp.x / scale, (Screen.height - sp.y) / scale + bob), s.done ? $"<color=#9a94a8>{s.name} · 내일 다시</color>" : $"<color=#ffde59>{s.name}</color>", !s.done);
            }
            if (nearest.spot != null) EdgeArrow(cam, nearest.spot.transform.position, nearest.name, scale);
        }

        static GUIStyle line;
        static GUIStyle Line => line ?? (line = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow });

        void Badge(Vector2 at, string text, bool withStar)
        {
            float w = Line.CalcSize(new GUIContent(text)).x + (withStar ? 22f : 12f);
            var r = new Rect(at.x - w / 2f, at.y - 18f, w, 16f);
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            if (withStar && star != null) DrawSprite(star, new Rect(r.x + 3f, r.y + 2f, 12f, 12f));
            GUI.Label(new Rect(r.x + (withStar ? 10f : 0f), r.y, r.width - (withStar ? 10f : 0f), r.height), text, Line);
        }

        void EdgeArrow(Camera cam, Vector3 world, string name, float scale)
        {
            var vp = cam.WorldToViewportPoint(world);
            if (vp.z > 0f && vp.x > .03f && vp.x < .97f && vp.y > .05f && vp.y < .95f) return;
            Vector2 dir = new Vector2(vp.x - .5f, vp.y - .5f);
            if (vp.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 1e-4f) return;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, mx = vw / 2f - 50f, my = vh / 2f - 46f;
            float k = Mathf.Min(mx / Mathf.Max(1e-3f, Mathf.Abs(dir.x)), my / Mathf.Max(1e-3f, Mathf.Abs(dir.y)));
            var p = new Vector2(vw / 2f + dir.x * k, vh / 2f - dir.y * k);
            string arrow = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? (dir.x > 0 ? "▶" : "◀") : (dir.y > 0 ? "▲" : "▼");
            float a = .75f + .25f * Mathf.Sin(Time.time * 4f);
            var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, a);
            Badge(p + new Vector2(0f, 8f), $"<color=#ffde59>{arrow} {name}</color>", false);
            GUI.color = old;
        }

        static void DrawSprite(Sprite s, Rect r)
        {
            var t = s.texture; var tr = s.textureRect;
            GUI.DrawTextureWithTexCoords(r, t, new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height));
        }
    }
}
