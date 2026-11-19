using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/14 은샘: 짐 싣는 짐승(3단계 운반 수단) 그림. 11/19 마차(4단계, Tools/ArtGen/wagon_v1.py)도 같은 방식. 짐말(고려·베네치아 장터)·낙타(아라비아, 짐말 대신)·순록(북유럽, 짐말 대신)과 순록 썰매(북유럽, 마차 대신)가 같이 쓴다.
    /// 내가 지나온 길을 고삐 거리만큼 뒤에서 따라오고, 짐 비율에 따라 등에 실린 짐이 0~3단계로 바뀐다.
    /// 그림: Resources/Transport/PackHorse·Camel/{side,front,back}_0~3 (옆은 왼쪽을 봄, 앞=아래로 올 때, 뒤=위로 갈 때. 1배 크기. Tools/ArtGen/pack_animals_v1.py).
    /// 예전 낙타(ArabiaCamelFollow, 장터 그림을 0.45배로 줄여 사람만 했다)를 대신한다. 온라인 다른 사람 것은 VehicleLook이 Place로 그린다. 그림만, 충돌·속도·저장과 무관.
    /// </summary>
    public class PackAnimal : MonoBehaviour
    {
        public const string HorseId = "packhorse", CamelId = "arabia_camel", WagonId = "wagon",
            ReindeerId = "north_reindeer", ReindeerSledId = "north_reindeer_sled";   // 11/19 은샘 "북유럽은 순록으로": 짐말 대신 순록, 마차 대신 순록 썰매 (그림 Tools/ArtGen/reindeer_v3.py)

        /// <summary>이 운반 수단이 짐승이면 그림 폴더와 고삐 거리</summary>
        public static bool Is(string vehicleId, out string folder, out float gap)
        {
            folder = null; gap = 0f;
            if (vehicleId == HorseId) { folder = "PackHorse"; gap = 1.45f; return true; }
            if (vehicleId == CamelId) { folder = "Camel"; gap = 1.5f; return true; }
            // 11/19 은샘: 마차 (짐말 다음 단계, 네 지역 공용). 옆모습이 68px로 길어 말 머리가 짐말과 같은 자리에 오게 고삐를 길게
            if (vehicleId == WagonId) { folder = "Wagon"; gap = 2.45f; return true; }
            if (vehicleId == ReindeerId) { folder = "Reindeer"; gap = 1.6f; return true; }           // 11/19 순록 3판: 옆 그림이 짐말보다 4칸 길다 (44px)
            if (vehicleId == ReindeerSledId) { folder = "ReindeerSled"; gap = 2.6f; return true; }   // 마차 틀, 옆 그림 72px (마차 68px)
            return false;
        }
        public static bool Is(string vehicleId) => Is(vehicleId, out _, out _);

        static readonly System.Collections.Generic.Dictionary<string, Sprite> art = new System.Collections.Generic.Dictionary<string, Sprite>();
        static Sprite Art(string folder, string view, int stage, string frame)
        {
            string key = folder + "/" + view + "_" + Mathf.Clamp(stage, 0, 3) + frame;
            if (art.TryGetValue(key, out var s) && s != null) return s;
            return art[key] = Resources.Load<Sprite>("Transport/" + key);
        }

        /// <summary>trail 자리에 짐승 그림을 놓는다 (짐 단계·좌우·걸음 들썩임·앞뒤 정렬). 짐승이 아니면 false</summary>
        public static bool Place(SpriteRenderer sr, VehicleTrail trail, Transform owner, SpriteRenderer body, string vehicleId, Vector2 facing, bool moving, int stage, float phase)
        {
            if (sr == null || trail == null || owner == null || !Is(vehicleId, out var folder, out var gap)) return false;
            trail.Gap = gap;
            var tr = sr.transform;
            tr.localScale = Vector3.one; tr.localRotation = Quaternion.identity;
            var at = trail.Step(owner.position, facing, moving);
            var h = trail.Heading;
            bool wagon = folder == "Wagon" || folder == "ReindeerSled";
            bool faceRight = trail.FaceRight;
            if (wagon)
            {   // 11/19 마차는 길어서(고삐 2.45칸) 모퉁이에서 지나온 길 방향을 보면 말 머리가 나와 반대쪽을 향했다 → 말이 늘 나를 향한다
                var toMe = (Vector2)owner.position - at;
                if (toMe.sqrMagnitude > .01f) { h = toMe.normalized; if (Mathf.Abs(h.x) > .05f) faceRight = h.x > 0f; }
            }
            // 11/14 위아래로 걸으면 앞·뒷모습. 옆모습은 좌우까지 한 방향으로 쳐서, 방향이 바뀔 때 뒤집힘·앞뒤 바뀜이 같이 0.1초 기다린다
            string want = Mathf.Abs(h.y) > Mathf.Abs(h.x) * 1.2f ? (h.y < 0f ? "front" : "back") : faceRight ? "side_r" : "side_l";
            string shown = SteadyView(sr, want);
            bool upDown = !shown.StartsWith("side");
            string view = upDown ? shown : "side";
            // 11/14 걸음: 기본 → a(다리 한 쌍 듦) → 기본 → b(다른 쌍). 서 있으면 기본 (Tools/ArtGen/pack_animals_v1.py lift)
            int step = moving ? Mathf.FloorToInt(phase) & 3 : 0;
            sr.sprite = Art(folder, view, stage, step == 1 ? "_a" : step == 3 ? "_b" : "");
            float bob = 0f;   // 11/14 다리 걸음 프레임으로 바꿔 들썩임은 뺐다
            float lift = sr.sprite != null ? sr.sprite.rect.height / 32f - .55f : .45f;   // 발이 사람 발높이쯤 오게 (그림 가운데가 중심)
            if (upDown && h.y < 0f) lift -= .4f;   // 앞모습(내 위에서 따라 내려올 때)은 머리가 아래 끝이라 조금 당겨 고삐 거리를 옆모습과 비슷하게
            // 11/19 마차 앞·뒷모습(30x58)은 옆모습보다 짧다: 앞(내려올 때)은 말 머리가 나에게 가깝게 당기고, 뒤(올라갈 때)는 말 귀가 내 다리를 덮지 않게 조금 뒤로
            if (wagon && upDown) lift += h.y < 0f ? -.5f : -.6f;
            tr.position = new Vector3(at.x, at.y + lift + bob, owner.position.z);
            sr.flipY = false;                 // 썰매에서 바꿨을 때
            sr.flipX = shown == "side_r";   // 옆모습 그림이 왼쪽을 보니 오른쪽으로 갈 때 뒤집는다
            bool mine = PlayerController.Instance != null && owner == PlayerController.Instance.transform;
            int baseOrder = body != null ? (mine ? BodyOrder(body) : body.sortingOrder) : 10;
            if (body != null) sr.sortingLayerID = body.sortingLayerID;
            bool ahead = at.y < owner.position.y;   // 짐승이 나보다 아래면 앞에, 위면 뒤에
            int order = ahead ? baseOrder + 1 : baseOrder - 2;
            // 11/19 은샘 "발이 땅에 있는데 기둥 윗부분에 뿔이 가려진다": 사람과 같은 규칙으로 물건 윗부분(Above)과 앞뒤를 맞춘다.
            // 발이 물건 밑동보다 아래면 그 위로 올리고, 밑동을 지나 뒤에 있으면 그대로 가려진다 (AboveDepth)
            var ab = sr.bounds;
            bool raise = AboveDepth.InFront(ab, ab.min.y);
            // 주인이 짐승 앞(아래)에서 겹치면 주인도 짐승 위에 와야 한다. 내 몸은 같이 올리고(다른 물건 뒤에 서 있으면 못 올린다),
            // 다른 사람 몸은 DepthActor가 맡고 있어 올릴 수 없으니 그때는 짐승을 올리지 않는다
            bool overlap = body != null && !ahead && body.bounds.Intersects(ab);
            bool bodyUp = false;
            if (raise && overlap)
            {
                if (mine && !AboveDepth.Behind(body.bounds, body.bounds.min.y)) bodyUp = true;
                else raise = false;
            }
            if (raise) order += AboveDepth.Raise;
            sr.sortingOrder = order;
            if (body != null && mine) SetBodyUp(body, bodyUp ? baseOrder + AboveDepth.Raise + 1 : baseOrder);
            DepthActor.AddProp(sr);   // NPC·다른 사람도 발 높이로 짐승과 앞뒤를 맞춘다 (발이 짐승 발보다 아래면 짐승을 가린다)
            return true;
        }

        // 11/19 내 몸을 잠깐 올렸다 내리기 위해 원래 순서를 기억한다 (다른 코드가 순서를 새로 정하면 그것을 원래 값으로)
        static SpriteRenderer upBody; static int upNormal, upSet = int.MinValue;
        /// <summary>내 몸의 원래 그리는 순서 (짐승 때문에 잠깐 올린 값이 아니라). 내 몸 순서를 기준으로 삼는 그림은 이 값을 쓴다</summary>
        public static int BodyOrder(SpriteRenderer body)
        {
            if (body == null) return 10;
            return body == upBody && body.sortingOrder == upSet ? upNormal : body.sortingOrder;   // 다른 사람 몸이나 다른 코드가 새로 정한 값은 그대로
        }
        static void SetBodyUp(SpriteRenderer body, int order)
        {
            int normal = BodyOrder(body);
            body.sortingOrder = order; upBody = body; upNormal = normal; upSet = order;
        }
        /// <summary>짐승을 넣거나 맵을 옮기면 내 몸 순서를 원래대로</summary>
        public static void Restore(SpriteRenderer body)
        {
            if (body == null || body != upBody) return;
            if (body.sortingOrder == upSet) body.sortingOrder = upNormal;
            upSet = upNormal;
        }

        /// <summary>모퉁이에서 그림이 한두 프레임만 바뀌었다 돌아오는 깜빡임을 막는다: 새 방향이 0.1초 이어져야 바꾼다</summary>
        static readonly System.Collections.Generic.Dictionary<int, (string shown, string pending, float since)> views = new System.Collections.Generic.Dictionary<int, (string, string, float)>();
        static string SteadyView(SpriteRenderer sr, string want)
        {
            if (views.Count > 64) views.Clear();   // 맵이 바뀌며 사라진 그림 칸이 쌓이지 않게
            int key = sr.GetInstanceID();
            if (!views.TryGetValue(key, out var v) || v.shown == null) { views[key] = (want, want, Time.time); return want; }
            if (want == v.shown) { views[key] = (v.shown, want, Time.time); return v.shown; }
            if (want != v.pending) { views[key] = (v.shown, want, Time.time); return v.shown; }
            if (Time.time - v.since >= .1f) { views[key] = (want, want, Time.time); return want; }
            return v.shown;
        }

        Transform animal; SpriteRenderer sr; PlayerController owner; string shownId; float phase;
        readonly VehicleTrail trail = new VehicleTrail();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<PackAnimal>() != null) return;
            var go = new GameObject("PackAnimal"); DontDestroyOnLoad(go); go.AddComponent<PackAnimal>();
        }

        void LateUpdate()
        {
            var pc = PlayerController.Instance;
            var st = GameManager.I != null ? GameManager.I.Player : null;
            string id = st != null && st.vehicle != null ? st.vehicle.id : null;
            bool show = pc != null && Is(id) && !GameManager.I.IsTravelling;
            if (pc != owner || id != shownId) { if (animal != null) Destroy(animal.gameObject); animal = null; owner = pc; shownId = id; trail.Reset(); }   // 맵·수단이 바뀌면 다시 붙인다
            if (!show)
            {
                if (animal != null) animal.gameObject.SetActive(false);
                trail.Reset();
                if (pc != null && pc.anim != null) Restore(pc.anim.GetComponent<SpriteRenderer>());   // 11/19 올려 둔 내 몸 순서를 원래대로
                return;
            }
            if (animal == null)
            {
                animal = new GameObject(id == CamelId ? "내 낙타" : id == WagonId ? "내 마차" : id == ReindeerId ? "내 순록" : id == ReindeerSledId ? "내 순록 썰매" : "내 짐말").transform;
                animal.SetParent(pc.transform, false);
                sr = animal.gameObject.AddComponent<SpriteRenderer>();
            }
            animal.gameObject.SetActive(true);
            Vector2 facing = pc.anim != null ? new Vector2(pc.anim.facing.x, pc.anim.facing.y) : (Vector2)pc.Motor.Facing;
            bool moving = pc.Motor.Velocity.sqrMagnitude > 0.01f;
            if (moving) phase += Time.deltaTime * 8f;   // 걸음 프레임 1초에 8장 (사람 걸음과 비슷하게)
            var body = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            Place(sr, trail, pc.transform, body, id, facing, moving, HandcartLook.Stage(st.LoadRatio), phase);
        }
    }
}
