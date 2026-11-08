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
        float phase; bool faceRight = true;

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
            if (pc != owner) { if (camel != null) Destroy(camel.gameObject); camel = null; owner = pc; }   // 맵이 바뀌면 새 플레이어에 다시 붙인다
            if (!show) { if (camel != null) camel.gameObject.SetActive(false); return; }
            if (camel == null)
            {
                camel = new GameObject("내 낙타").transform;
                camel.SetParent(pc.transform, false);
                camel.localScale = Vector3.one * 0.45f;
                sr = camel.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Arabia/Fair/animal_camel");
            }
            camel.gameObject.SetActive(true);

            Vector2 facing = pc.Motor.Facing;
            if (facing.x > 0.1f) faceRight = true; else if (facing.x < -0.1f) faceRight = false;   // 위·아래로 갈 땐 마지막 좌우 방향 유지
            bool moving = pc.Motor.Velocity.sqrMagnitude > 0.01f;
            if (moving) phase += Time.deltaTime * 10f;
            float bob = moving ? Mathf.Abs(Mathf.Sin(phase)) * .06f : 0f;
            camel.localPosition = -(Vector3)(facing * 1.1f) + new Vector3(0f, .15f + bob, 0f);
            sr.flipX = faceRight;   // 그림이 왼쪽을 보니 오른쪽으로 갈 때 뒤집는다

            var body = pc.anim != null ? pc.anim.GetComponent<SpriteRenderer>() : null;
            if (body != null) sr.sortingLayerID = body.sortingLayerID;
            // 위로 걸으면 낙타가 내 앞(화면 아래)에 오니 위에 그리고, 그 밖엔 내 뒤에
            int baseOrder = body != null ? body.sortingOrder : 10;
            sr.sortingOrder = facing.y > 0.1f ? baseOrder + 1 : baseOrder - 2;
        }
    }
}
