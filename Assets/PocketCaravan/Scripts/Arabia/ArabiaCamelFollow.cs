using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 낙타를 타고(산 뒤) 다닐 때 내 뒤를 따라오는 낙타 그림 (2026-11-08 기우).
    /// 공용 CarryVisual은 손수레(handcart)만, 북유럽은 NorthTravel이 썰매를 그린다 — 아라비아 낙타는 여기서.
    /// 그림만 (충돌·이동 규칙은 그대로). 낙타 그림(Resources/Arabia/Fair/animal_camel, 원래 왼쪽을 봄)을 0.45배로, 걸을 때 들썩인다.
    /// </summary>
    public class ArabiaCamelFollow : MonoBehaviour
    {
        const string CamelId = "arabia_camel";
        Transform camel; SpriteRenderer sr; PlayerController owner;
        float phase;
        readonly VehicleTrail trail = new VehicleTrail();   // 11/08: 내가 지나온 길을 1.1칸 뒤에서 따라온다 (공용 VehicleLook과 같은 방식)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<ArabiaCamelFollow>() != null) return;
            var go = new GameObject("ArabiaCamelFollow"); DontDestroyOnLoad(go); go.AddComponent<ArabiaCamelFollow>();
        }

        void LateUpdate()
        {
            var pc = PlayerController.Instance;
            var st = GameManager.I != null ? GameManager.I.Player : null;
            bool show = pc != null && st != null && st.vehicle != null && st.vehicle.id == CamelId && !GameManager.I.IsTravelling;
            if (pc != owner) { if (camel != null) Destroy(camel.gameObject); camel = null; owner = pc; trail.Reset(); }   // 맵이 바뀌면 새 플레이어에 다시 붙인다
            if (!show) { if (camel != null) camel.gameObject.SetActive(false); trail.Reset(); return; }
            if (camel == null)
            {
                camel = new GameObject("내 낙타").transform;
                camel.SetParent(pc.transform, false);
                camel.localScale = Vector3.one * 0.45f;
                sr = camel.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Arabia/Fair/animal_camel");
            }
            camel.gameObject.SetActive(true);

            // 보이는 캐릭터 방향(anim.facing) 기준. 처음엔 그 뒤에 서고, 걸으면 지나온 길을 따라온다
            Vector2 facing = pc.anim != null ? new Vector2(pc.anim.facing.x, pc.anim.facing.y) : (Vector2)pc.Motor.Facing;
            bool moving = pc.Motor.Velocity.sqrMagnitude > 0.01f;
            if (moving) phase += Time.deltaTime * 10f;
            float bob = moving ? Mathf.Abs(Mathf.Sin(phase)) * .06f : 0f;
            var at = trail.Step(pc.transform.position, facing, moving);
            camel.position = new Vector3(at.x, at.y + .15f + bob, pc.transform.position.z);
            sr.flipX = trail.FaceRight;   // 그림이 왼쪽을 보니 오른쪽으로 갈 때 뒤집는다

            var body = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            if (body != null) sr.sortingLayerID = body.sortingLayerID;
            // 낙타가 나보다 화면 아래에 있으면 내 앞에, 위에 있으면 내 뒤에 그린다
            int baseOrder = body != null ? body.sortingOrder : 10;
            sr.sortingOrder = at.y < pc.transform.position.y ? baseOrder + 1 : baseOrder - 2;
        }
    }
}
