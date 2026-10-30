using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 놀거리 알림 (2026-10-30, 제안). 미니게임·놀거리가 어디 있는지 몰라 지나치던 것을 줄인다.
    /// - 머리 위 표시: 오늘 아직 할 수 있는 놀거리 자리 위에 별과 이름("★ 유리 불기")이 떠서 까딱인다. 14칸 안에서 보이고,
    ///   4칸 안으로 들어오면 원래 장소 이름표가 뜨므로 사라진다. 오늘 이미 했으면 흐린 "내일 다시"만.
    /// - 화면 가장자리 화살표: 가장 가까운 '오늘 할 수 있는' 놀거리가 화면 밖이면 그쪽 가장자리에 화살표와 이름.
    /// - 맵 알림: 맵에 들어오면(하루 한 번, 맵마다) "여기서 할 수 있는 것: 유리 불기 · 장식 공방".
    /// 공용 코드는 고치지 않는다. 새 놀거리는 Rules에 한 줄 더하면 된다(이름이 null이면 표시하지 않음).
    /// </summary>
    public class VeniceActivityHints : MonoBehaviour
    {
        /// <summary>놀거리 종류 → (이름, 오늘 이미 했는지). 이름이 null이면 이 자리는 놀거리로 치지 않는다</summary>
        public static readonly Dictionary<Type, Func<Interactable, (string name, bool done)>> Rules = new Dictionary<Type, Func<Interactable, (string, bool)>>
        {
            { typeof(VeniceGlassSpot), _ => ("유리 불기", VeniceSave.DoneToday("glass")) },
            { typeof(VeniceNetSpot), _ => ("그물 던지기", VeniceSave.DoneToday("net")) },
            { typeof(VeniceFeed), _ => ("비둘기 모이 주기", VeniceSave.DoneToday("feed")) },
            { typeof(VeniceTraghetto), _ => (VeniceUtil.Map == "venice_rialto" ? "곤돌라 경주" : null, false) },
            { typeof(VeniceWorkshop), i => ((VeniceWorkshop)i).recipe == VeniceWorkshop.Recipe.GlassArt
                ? ("유리 장식 공방", false)
                : ("레이스 공방", VeniceSave.DoneToday("lace") && VeniceSave.Get("lace_count") >= 2) },
            { typeof(VeniceMuseum), _ => ("전시실 기증", VeniceMuseum.Complete) },
            { typeof(VeniceMerchant), _ => ("떠돌이 상인", false) },
        };

        const float Far = 14f, Near = 4f;
        readonly List<(Interactable spot, string name, bool done)> spots = new List<(Interactable, string, bool)>();
        float nextScan;
        string lastMap;
        float announceAt = -1f;
        Sprite star;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceActivityHints>() == null)
                GameManager.I.gameObject.AddComponent<VeniceActivityHints>();
        }

        void Update()
        {
            if (!VeniceUtil.InVenice) { spots.Clear(); lastMap = null; return; }
            if (VeniceUtil.Map != lastMap) { lastMap = VeniceUtil.Map; announceAt = Time.time + .8f; nextScan = 0f; }   // 생활 자리가 다 놓인 뒤 알린다
            if (Time.time >= nextScan) { nextScan = Time.time + .5f; Scan(); }
            if (announceAt > 0f && Time.time >= announceAt) { announceAt = -1f; Announce(); }
        }

        void Scan()
        {
            spots.Clear();
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || !Rules.TryGetValue(it.GetType(), out var rule)) continue;
                var (name, done) = rule(it);
                if (name != null) spots.Add((it, name, done));
            }
        }

        void Announce()
        {
            string key = "hint_" + VeniceUtil.Map;
            if (spots.Count == 0 || VeniceSave.DoneToday(key)) return;
            var names = new List<string>();
            foreach (var s in spots) if (!s.done && !names.Contains(s.name)) names.Add(s.name);
            if (names.Count == 0) return;
            VeniceSave.MarkToday(key);
            Notify.Show("여기서 할 수 있는 것: " + string.Join(" · ", names) + "  (별 표시를 찾아가 보자)", 5f);
        }

        void OnGUI()
        {
            var cam = Camera.main; var pc = PlayerController.Instance;
            if (!VeniceUtil.InVenice || cam == null || pc == null || UIState.Blocking || spots.Count == 0) return;
            if (GameManager.I != null && GameManager.I.IsTravelling) return;
            if (star == null) star = VeniceUtil.Sprite("fx_star");
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

        /// <summary>가장 가까운 놀거리가 화면 밖이면 그쪽 가장자리에 화살표</summary>
        void EdgeArrow(Camera cam, Vector3 world, string name, float scale)
        {
            var vp = cam.WorldToViewportPoint(world);
            if (vp.z > 0f && vp.x > .03f && vp.x < .97f && vp.y > .05f && vp.y < .95f) return;   // 화면 안이면 머리 위 표시로 충분
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
