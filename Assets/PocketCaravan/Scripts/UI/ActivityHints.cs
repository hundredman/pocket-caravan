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
        };
        public static readonly Dictionary<string, Func<Interactable, (string name, bool done)>> NameRules = new Dictionary<string, Func<Interactable, (string, bool)>>();

        const float Far = 9f, Near = 4f, EdgeFar = 18f;   // 11/11 은샘: 가까이 있을 때만 (머리 위 ★ 9칸, 화면 밖 화살표 18칸)
        readonly List<(Interactable spot, string name, bool done)> spots = new List<(Interactable, string, bool)>();
        float nextScan;
        string lastMap;
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
            if (Map != lastMap) { lastMap = Map; nextScan = 0f; }
            if (Time.time >= nextScan) { nextScan = Time.time + .5f; Scan(); }
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

        // 11/11 은샘: 맵에 들어설 때 "여기서 할 수 있는 것" 알림은 뺐다 (★ 놀거리 표시가 같은 것을 알려 준다. 알림이 화면을 채웠다)

        /// <summary>11/11 은샘: 이 자리가 놀거리인지 (WorldLabels가 이름표 앞에 ★를 붙일 때 쓴다)</summary>
        public static bool IsActivity(Interactable it, out bool done)
        {
            done = false;
            if (it == null || !Rule(it, out var r)) return false;
            done = r.done;
            return true;
        }

        public Rect EdgeRect { get; private set; }
        void OnGUI()
        {
            EdgeRect=default;
            var cam = Camera.main; var pc = PlayerController.Instance;
            if (Map == null || cam == null || pc == null || UIState.Blocking || spots.Count == 0) return;
            if (GameManager.I != null && GameManager.I.IsTravelling) return;
            if (UIState.PhotoMode) return;        // 11/11 기념 사진 찍는 중
            if (StoryGuide.IntroActive) return;   // 11/11 튜토리얼 중에는 ★ 놀거리 표시·가장자리 화살표도 쉰다 (길 안내 화살표와 헷갈린다)
            if (star == null) star = Collectibles.LoadSprite("Common/fx_star", new Vector2(0.5f, 0.5f));
            UIStyle.Begin();
            GUI.depth = -4;
            float scale = UIStyle.Scale, bob = Mathf.Round(Mathf.Sin(Time.time * 3f) * 2f);
            (Interactable spot, string name, bool done) nearest = (null, null, false); float best = float.MaxValue;
            var labels = GameManager.I != null ? GameManager.I.GetComponent<WorldLabels>() : null;
            bool namesStarred = labels != null && labels.isActiveAndEnabled;   // 고려 맵은 GoryeoPlaceLabels가 공용 이름표를 끄니 그때는 ★ 표를 그대로
            foreach (var s in spots)
            {
                if (s.spot == null) continue;
                Vector2 d = s.spot.transform.position - pc.transform.position;
                float dist = d.magnitude;
                if (!s.done && dist < best) { best = dist; nearest = s; }
                if (dist > Far || dist < Near) continue;
                if (namesStarred && !string.IsNullOrEmpty(s.spot.DisplayName)) continue;   // 11/11 이름표가 있는 자리는 WorldLabels가 이름 앞에 ★를 붙인다 (표가 둘 겹치지 않게)
                var sp = cam.WorldToScreenPoint(s.spot.transform.position + new Vector3(0f, 1.6f, 0f));
                if (sp.z < 0f) continue;
                Badge(new Vector2(sp.x / scale, (Screen.height - sp.y) / scale + bob), s.done ? $"<color=#9a94a8>{s.name} · 내일 다시</color>" : $"<color=#ffde59>{s.name}</color>", !s.done);
            }
            if (nearest.spot != null && best <= EdgeFar) EdgeArrow(cam, nearest.spot.transform.position, nearest.name, scale);
        }

        static GUIStyle line;
        static GUIStyle Line
        {
            get
            {
                if(line==null||line.font!=UIStyle.RegularFont||line.fontSize!=UIStyle.BodySize)
                    line=new GUIStyle(UIStyle.CenterRich) { wordWrap=false, clipping=TextClipping.Overflow };
                return line;
            }
        }

        Rect Badge(Vector2 at, string text, bool withStar, bool screenEdge=false, char side=' ')
        {
            float w = Line.CalcSize(new GUIContent(text)).x + (withStar ? 22f : 12f);
            var r = new Rect(at.x - w / 2f, at.y - 18f, w, 16f);
            if(screenEdge)
            {
                r.x=Mathf.Clamp(r.x,4,Mathf.Max(4,UIStyle.VirtualWidth-r.width-4));
                r.y=Mathf.Clamp(r.y,4,Mathf.Max(4,UIStyle.VirtualHeight-r.height-4));
                var guide=GoryeoIntro.ReservedGuideRect;
                if(guide.width>0&&r.Overlaps(guide))r.y=Mathf.Max(4,guide.yMin-r.height-4);
                // 11/11 은샘: 다른 UI와 겹치지 않게 옆으로 뺀다. 아래(▼)면 목표 줄 옆, 위(▲)면 옆으로 비키고, 양옆(◀▶)이면 위아래로 비킨다
                var goal=ScreenZones.Center("goal");
                if(side=='v'&&goal.HasValue)
                {
                    var g=goal.Value;
                    r.y=Mathf.Round(g.center.y-r.height/2f);
                    r.x=at.x>=UIStyle.VirtualWidth/2f?g.xMax+6f:g.x-r.width-6f;
                }
                r=side=='^'||side=='v'?ScreenZones.DodgeSideways(r):ScreenZones.Dodge(r);
            }
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            if (withStar && star != null) DrawSprite(star, new Rect(r.x + 3f, r.y + 2f, 12f, 12f));
            GUI.Label(new Rect(r.x + (withStar ? 10f : 0f), r.y, r.width - (withStar ? 10f : 0f), r.height), text, Line);
            return r;
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
            char side = arrow == "▼" ? 'v' : arrow == "▲" ? '^' : '>';
            EdgeRect=Badge(p + new Vector2(0f, 8f), $"<color=#ffde59>{arrow} {name}</color>", false, true, side);
            GUI.color = old;
        }

        static void DrawSprite(Sprite s, Rect r)
        {
            var t = s.texture; var tr = s.textureRect;
            GUI.DrawTextureWithTexCoords(r, t, new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height));
        }
    }
}
