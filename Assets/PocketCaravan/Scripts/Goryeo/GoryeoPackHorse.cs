using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/14 은샘: 고려 3단계 운반 수단 "짐말". 등짐(기본 속도·짐 10) → 손수레(짐 30) → 짐말(더 빠르고 짐 45).
    /// 고려는 산이 많아 먼 길 짐은 수레보다 말 등에 실어 날랐다(과하마). 그래서 길 없는 곳에서도 크게 느려지지 않는다.
    /// 그림: Resources/Transport/PackHorse/side_0~3 (왼쪽을 봄, 짐 단계 0~3. Tools/ArtGen/packhorse_v1.py).
    /// 내가 지나온 길을 뒤에서 따라온다 (낙타와 같은 VehicleTrail). 온라인 다른 사람 짐말은 VehicleLook이 Place로 그린다. 그림만, 충돌·속도·저장과 무관.
    /// </summary>
    public class GoryeoPackHorse : MonoBehaviour
    {
        public const string Id = "goryeo_packhorse";
        public const float Gap = 1.45f;   // 말 몸이 길어서 낙타(1.1칸)보다 조금 더 뒤에서 (고삐 거리)

        static readonly Sprite[] art = new Sprite[4];
        public static Sprite Art(int stage)
        {
            stage = Mathf.Clamp(stage, 0, 3);
            if (art[stage] == null) art[stage] = Resources.Load<Sprite>("Transport/PackHorse/side_" + stage);
            return art[stage];
        }

        /// <summary>trail 자리에 말 그림을 놓는다 (짐 단계·좌우·걸음 들썩임·앞뒤 정렬)</summary>
        public static void Place(SpriteRenderer sr, VehicleTrail trail, Transform owner, SpriteRenderer body, Vector2 facing, bool moving, int stage, float phase)
        {
            if (sr == null || trail == null || owner == null) return;
            trail.Gap = Gap;
            sr.sprite = Art(stage);
            var tr = sr.transform;
            tr.localScale = Vector3.one; tr.localRotation = Quaternion.identity;
            var at = trail.Step(owner.position, facing, moving);
            float bob = moving ? Mathf.Round(Mathf.Abs(Mathf.Sin(phase)) * 1f) / 16f : 0f;   // 걸을 때 1픽셀 들썩
            tr.position = new Vector3(at.x, at.y + .45f + bob, owner.position.z);
            sr.flipY = false;   // 썰매에서 바꿨을 때
            sr.flipX = trail.FaceRight;   // 그림이 왼쪽을 보니 오른쪽으로 갈 때 뒤집는다
            int baseOrder = body != null ? body.sortingOrder : 10;
            if (body != null) sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = at.y < owner.position.y ? baseOrder + 1 : baseOrder - 2;   // 말이 나보다 아래면 앞에, 위면 뒤에
        }

        Transform horse; SpriteRenderer sr; PlayerController owner; float phase;
        readonly VehicleTrail trail = new VehicleTrail();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<GoryeoPackHorse>() != null) return;
            var go = new GameObject("GoryeoPackHorse"); DontDestroyOnLoad(go); go.AddComponent<GoryeoPackHorse>();
        }

        void LateUpdate()
        {
            var pc = PlayerController.Instance;
            var st = GameManager.I != null ? GameManager.I.Player : null;
            bool show = pc != null && st != null && st.vehicle != null && st.vehicle.id == Id && !GameManager.I.IsTravelling;
            if (pc != owner) { if (horse != null) Destroy(horse.gameObject); horse = null; owner = pc; trail.Reset(); }   // 맵이 바뀌면 새 플레이어에 다시 붙인다
            if (!show) { if (horse != null) horse.gameObject.SetActive(false); trail.Reset(); return; }
            if (horse == null)
            {
                horse = new GameObject("내 짐말").transform;
                horse.SetParent(pc.transform, false);
                sr = horse.gameObject.AddComponent<SpriteRenderer>();
            }
            horse.gameObject.SetActive(true);
            Vector2 facing = pc.anim != null ? new Vector2(pc.anim.facing.x, pc.anim.facing.y) : (Vector2)pc.Motor.Facing;
            bool moving = pc.Motor.Velocity.sqrMagnitude > 0.01f;
            if (moving) phase += Time.deltaTime * 9f;
            var body = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            Place(sr, trail, pc.transform, body, facing, moving, HandcartLook.Stage(st.LoadRatio), phase);
        }
    }
}
