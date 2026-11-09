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
        /// <summary>짐 상자 수 (내 손수레 CarryVisual과 같은 기준: 짐 비율이 0 넘으면 1개, 1/3 넘으면 2개, 2/3 넘으면 3개)</summary>
        public static int Crates(float loadRatio) => (loadRatio > 0f ? 1 : 0) + (loadRatio > 1f / 3f ? 1 : 0) + (loadRatio > 2f / 3f ? 1 : 0);

        public static void Draw(ref SpriteRenderer sr, ref VehicleTrail trail, Transform owner, SpriteRenderer body, string vehicle, Vector2 facing, bool moving, int crates = 0)
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
            if (vehicle != "north_sled") { var old = sr.GetComponent<RemoteSledFx>(); if (old != null) Object.Destroy(old); }   // 썰매에서 바꿨으면 방울 소리 끄기
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
                    var fx = sr.GetComponent<RemoteSledFx>();   // 11/08 방울 소리·눈가루도
                    if (fx == null) fx = sr.gameObject.AddComponent<RemoteSledFx>();
                    fx.Tick(owner.position, facing, moving);
                    break;
                }
                default:   // handcart (모르는 운반 수단도 손수레로). 11/09 내 손수레와 같은 HandcartLook 그림·자리 (짐 단계 = crates)
                    HandcartLook.Place(sr, owner, body, facing, crates);
                    break;
            }
            if (vehicle == "arabia_camel" || vehicle == "north_sled") HandcartLook.HideHandle(sr);
            // 11/08에 붙였던 축소 상자 짐은 그림 안 짐 단계로 바뀌어 쓰지 않는다 (예전 것이 남아 있으면 끈다)
            for (int i = 0; i < 3; i++)
            {
                var c = tr.Find("짐 " + i);
                if (c != null) c.gameObject.SetActive(false);
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

    /// <summary>
    /// 다른 사람 순록 썰매의 방울 소리·눈가루 (11/08 기우). 내 썰매(NorthTravel)와 같은 소리·같은 눈가루.
    /// 소리는 나와 가까울수록 크게 (8칸 밖이면 안 들림), 북유럽 땅에서 달릴 때만 눈가루.
    /// </summary>
    public sealed class RemoteSledFx : MonoBehaviour
    {
        AudioSource bells;
        Sprite puff;
        float puffAt, speed;
        Vector3 last; bool hasLast;
        readonly System.Collections.Generic.List<(SpriteRenderer sr, float born)> puffs = new System.Collections.Generic.List<(SpriteRenderer, float)>();

        public void Tick(Vector3 owner, Vector2 facing, bool moving)
        {
            if (bells == null)
            {
                bells = gameObject.AddComponent<AudioSource>();
                bells.clip = NorthTravel.BellsClip; bells.loop = true; bells.volume = 0f; bells.playOnAwake = false; bells.spatialBlend = 0f;
                puff = NorthUtil.Pixel(new Color32(250, 250, 255, 255), 2, 2);
            }
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            if (hasLast) speed = Mathf.Lerp(speed, Vector2.Distance(owner, last) / dt, 0.2f);
            last = owner; hasLast = true;
            var me = PlayerController.Instance;
            float dist = me != null ? Vector2.Distance(me.transform.position, owner) : 99f;
            float near = Mathf.Clamp01(1f - dist / 8f);
            float target = moving && speed > 0.3f && !AudioManager.Muted ? Mathf.Clamp01(speed / 5f) * 0.25f * near : 0f;
            bells.volume = Mathf.MoveTowards(bells.volume, target, Time.deltaTime * 0.8f);
            if (bells.volume > 0f && !bells.isPlaying) bells.Play();
            else if (bells.volume <= 0f && bells.isPlaying) bells.Stop();

            if (NorthUtil.InNorth && moving && speed > 1f && Time.time >= puffAt && puff != null)
            {
                puffAt = Time.time + 0.06f;
                var go = new GameObject("Sled Puff");
                go.transform.position = transform.position - (Vector3)(facing * 0.4f) + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.1f), 0f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = puff; sr.sortingOrder = 7;
                puffs.Add((sr, Time.time));
            }
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var (sr, born) = puffs[i];
                float age = Time.time - born;
                if (sr == null || age > 0.6f) { if (sr != null) Destroy(sr.gameObject); puffs.RemoveAt(i); continue; }
                sr.color = new Color(1f, 1f, 1f, 1f - age / 0.6f);
                sr.transform.position += new Vector3(0f, Time.deltaTime * 0.6f, 0f);
            }
        }

        void OnDisable() { if (bells != null) bells.Stop(); foreach (var (sr, _) in puffs) if (sr != null) Destroy(sr.gameObject); puffs.Clear(); }
        void OnDestroy() => OnDisable();
    }
}
