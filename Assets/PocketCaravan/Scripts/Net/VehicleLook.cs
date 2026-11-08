using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 온라인에서 다른 사람의 운반 수단(손수레·순록 썰매·낙타)도 보이게 (2026-11-08 기우).
    /// 게임이 enter에 "veh"(운반 수단 아이디, 걷기면 "")를 싣고, 서버가 room · join으로 같은 맵 사람들에게 전한다 (Server/src/world.ts).
    /// 그림은 표시만 (충돌·속도·저장 무관). 내 낙타도 같은 VehicleTrail로 내 지나온 길을 따라온다.
    /// </summary>
    public static class VehicleLook
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public static Sprite Tex(string path, Vector2 pivot)
        {
            if (cache.TryGetValue(path, out var s) && s != null) return s;
            var t = Resources.Load<Texture2D>(path);
            if (t == null) return cache[path] = null;
            t.filterMode = FilterMode.Point;
            return cache[path] = Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, 16f);
        }

        /// <summary>
        /// 다른 사람의 운반 수단 그림 하나를 그 사람 그림 옆에 둔다. facing은 그 사람이 보는 쪽.
        /// 손수레·썰매는 내 것과 같은 자리 규칙, 낙타는 지나온 길을 따라온다.
        /// </summary>
        public static void Draw(ref SpriteRenderer sr, ref VehicleTrail trail, Transform owner, SpriteRenderer body, string vehicle, Vector2 facing, bool moving)
        {
            if (string.IsNullOrEmpty(vehicle) || owner == null)
            {
                if (sr != null) sr.gameObject.SetActive(false);
                return;
            }
            if (sr == null)
            {
                var go = new GameObject("운반 수단");
                go.transform.SetParent(owner, false);
                sr = go.AddComponent<SpriteRenderer>();
                if (body != null) { sr.sortingLayerID = body.sortingLayerID; sr.sharedMaterial = body.sharedMaterial; }
            }
            sr.gameObject.SetActive(true);
            if (facing.sqrMagnitude < 0.01f) facing = Vector2.down;
            int baseOrder = body != null ? body.sortingOrder : 10;
            var tr = sr.transform;
            switch (vehicle)
            {
                case "arabia_camel":
                    sr.sprite = Resources.Load<Sprite>("Arabia/Fair/animal_camel");
                    tr.localScale = Vector3.one * 0.45f;
                    tr.localRotation = Quaternion.identity;
                    if (trail == null) trail = new VehicleTrail();
                    var at = trail.Step(owner.position, facing, moving);
                    tr.position = new Vector3(at.x, at.y + .15f, owner.position.z);
                    sr.flipX = trail.FaceRight;
                    sr.sortingOrder = at.y < owner.position.y ? baseOrder + 1 : baseOrder - 2;
                    break;
                case "north_sled":
                {
                    bool side = Mathf.Abs(facing.x) > Mathf.Abs(facing.y);
                    sr.sprite = side ? Tex("North/Minigame/cargo_h", new Vector2(.5f, .2f)) : Tex("North/Minigame/cargo_v", new Vector2(.5f, .5f));
                    sr.flipX = side && facing.x < 0; sr.flipY = !side && facing.y < 0;
                    tr.localScale = Vector3.one; tr.localRotation = Quaternion.identity;
                    tr.localPosition = side ? new Vector3(-facing.x * 1.15f, -0.45f, 0f) : -(Vector3)(facing * 0.95f) + new Vector3(0f, -0.15f, 0f);
                    sr.sortingOrder = facing.y > 0 ? baseOrder + 1 : baseOrder - 2;
                    break;
                }
                default:   // handcart (모르는 운반 수단도 손수레로)
                    sr.sprite = Resources.Load<Sprite>("Transport/handcart");
                    sr.flipX = false; sr.flipY = false;
                    tr.localScale = Vector3.one;
                    tr.localPosition = -(Vector3)(facing * .95f) + new Vector3(0f, -.1f, 0f);
                    tr.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg);
                    sr.sortingOrder = baseOrder - 2;
                    break;
            }
        }
    }

    /// <summary>앞사람이 지나온 길을 일정 거리 뒤에서 따라가는 자리 (낙타). 서 있으면 그 자리에 머문다.</summary>
    public sealed class VehicleTrail
    {
        public float Gap = 1.1f;
        readonly List<Vector2> pts = new List<Vector2>();
        Vector2 pos; bool has;
        public bool FaceRight { get; private set; } = true;

        public void Reset() { pts.Clear(); has = false; }

        public Vector2 Step(Vector3 leader3, Vector2 facing, bool moving)
        {
            Vector2 leader = leader3;
            if (!has || (pts.Count > 0 && (leader - pts[pts.Count - 1]).sqrMagnitude > 9f))
            {   // 처음이거나 순간이동(맵 이동·먼 거리): 바라보는 쪽 뒤에 바로 놓는다
                pts.Clear();
                pos = leader - facing.normalized * Gap;
                pts.Add(pos); pts.Add(leader);
                has = true;
                if (Mathf.Abs(facing.x) > .1f) FaceRight = facing.x > 0;
                return pos;
            }
            if ((leader - pts[pts.Count - 1]).sqrMagnitude > 0.0025f) pts.Add(leader);
            // 앞사람 자리에서 길을 거슬러 Gap만큼 뒤
            float left = Gap; Vector2 target = pts[0];
            for (int i = pts.Count - 1; i > 0; i--)
            {
                float d = Vector2.Distance(pts[i], pts[i - 1]);
                if (d >= left) { target = Vector2.Lerp(pts[i], pts[i - 1], left / Mathf.Max(d, 1e-4f)); pts.RemoveRange(0, i - 1); break; }
                left -= d;
            }
            if (pts.Count > 200) pts.RemoveRange(0, pts.Count - 200);
            Vector2 before = pos;
            pos = Vector2.Lerp(pos, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
            float dx = pos.x - before.x;
            if (Mathf.Abs(dx) > 0.002f) FaceRight = dx > 0;   // 위·아래로만 가거나 서 있으면 마지막 좌우 방향 유지
            return pos;
        }
    }
}
