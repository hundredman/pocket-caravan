using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/14 은샘: 짐 싣는 짐승(3단계 운반 수단) 그림. 짐말(고려·베네치아·북유럽 장터)과 낙타(아라비아 장터, 짐말 대신)가 같이 쓴다.
    /// 내가 지나온 길을 고삐 거리만큼 뒤에서 따라오고, 짐 비율에 따라 등에 실린 짐이 0~3단계로 바뀐다.
    /// 그림: Resources/Transport/PackHorse·Camel/side_0~3 (왼쪽을 봄, 1배 크기. Tools/ArtGen/pack_animals_v1.py).
    /// 예전 낙타(ArabiaCamelFollow, 장터 그림을 0.45배로 줄여 사람만 했다)를 대신한다. 온라인 다른 사람 것은 VehicleLook이 Place로 그린다. 그림만, 충돌·속도·저장과 무관.
    /// </summary>
    public class PackAnimal : MonoBehaviour
    {
        public const string HorseId = "packhorse", CamelId = "arabia_camel";

        /// <summary>이 운반 수단이 짐승이면 그림 폴더와 고삐 거리</summary>
        public static bool Is(string vehicleId, out string folder, out float gap)
        {
            folder = null; gap = 0f;
            if (vehicleId == HorseId) { folder = "PackHorse"; gap = 1.45f; return true; }
            if (vehicleId == CamelId) { folder = "Camel"; gap = 1.5f; return true; }
            return false;
        }
        public static bool Is(string vehicleId) => Is(vehicleId, out _, out _);

        static readonly System.Collections.Generic.Dictionary<string, Sprite> art = new System.Collections.Generic.Dictionary<string, Sprite>();
        static Sprite Art(string folder, int stage)
        {
            string key = folder + "/side_" + Mathf.Clamp(stage, 0, 3);
            if (art.TryGetValue(key, out var s) && s != null) return s;
            return art[key] = Resources.Load<Sprite>("Transport/" + key);
        }

        /// <summary>trail 자리에 짐승 그림을 놓는다 (짐 단계·좌우·걸음 들썩임·앞뒤 정렬). 짐승이 아니면 false</summary>
        public static bool Place(SpriteRenderer sr, VehicleTrail trail, Transform owner, SpriteRenderer body, string vehicleId, Vector2 facing, bool moving, int stage, float phase)
        {
            if (sr == null || trail == null || owner == null || !Is(vehicleId, out var folder, out var gap)) return false;
            trail.Gap = gap;
            sr.sprite = Art(folder, stage);
            var tr = sr.transform;
            tr.localScale = Vector3.one; tr.localRotation = Quaternion.identity;
            var at = trail.Step(owner.position, facing, moving);
            float bob = moving ? Mathf.Round(Mathf.Abs(Mathf.Sin(phase))) / 16f : 0f;   // 걸을 때 1픽셀 들썩
            float lift = sr.sprite != null ? sr.sprite.rect.height / 32f - .55f : .45f;   // 발이 사람 발높이쯤 오게 (그림 가운데가 중심)
            tr.position = new Vector3(at.x, at.y + lift + bob, owner.position.z);
            sr.flipY = false;                 // 썰매에서 바꿨을 때
            sr.flipX = trail.FaceRight;       // 그림이 왼쪽을 보니 오른쪽으로 갈 때 뒤집는다
            int baseOrder = body != null ? body.sortingOrder : 10;
            if (body != null) sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = at.y < owner.position.y ? baseOrder + 1 : baseOrder - 2;   // 짐승이 나보다 아래면 앞에, 위면 뒤에
            return true;
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
            if (!show) { if (animal != null) animal.gameObject.SetActive(false); trail.Reset(); return; }
            if (animal == null)
            {
                animal = new GameObject(id == CamelId ? "내 낙타" : "내 짐말").transform;
                animal.SetParent(pc.transform, false);
                sr = animal.gameObject.AddComponent<SpriteRenderer>();
            }
            animal.gameObject.SetActive(true);
            Vector2 facing = pc.anim != null ? new Vector2(pc.anim.facing.x, pc.anim.facing.y) : (Vector2)pc.Motor.Facing;
            bool moving = pc.Motor.Velocity.sqrMagnitude > 0.01f;
            if (moving) phase += Time.deltaTime * 9f;
            var body = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            Place(sr, trail, pc.transform, body, id, facing, moving, HandcartLook.Stage(st.LoadRatio), phase);
        }
    }
}
