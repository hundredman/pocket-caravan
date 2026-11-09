using UnityEngine;

namespace PocketCaravan
{
    /// <summary>손수레와 적재량을 보여 주는 표현. 충돌·속도·화물 상태는 기존 운반 코드가 담당한다.
    /// 11/09 손수레 그림은 HandcartLook(방향별 그림 + 짐 단계, 옆모습 손잡이 앞 겹)으로. 수레·짐은 흔들지 않는다(사람 몸 흔들림만 남김).</summary>
    public sealed class CarryVisual : MonoBehaviour
    {
        PlayerController player;
        Transform cart;
        Vector3 bodyRest;
        float phase;
        public float CurrentSway { get; private set; }

        // 시각 연출만 계산한다. 위치/충돌/수량/정차 안정화에 영향을 주지 않는다.
        public static float SwayStrength(int grade, float load, float speed)
            => Mathf.Clamp01(speed / 3.8f) * Mathf.Clamp01(load) * (grade == 0 ? 1f : grade == 1 ? .55f : .12f);
        SpriteRenderer cartSprite;

        void Start()
        {
            player = GetComponent<PlayerController>();
            if (player.anim != null) bodyRest = player.anim.transform.localPosition;
            cart = new GameObject("Handcart visual").transform;
            cart.SetParent(transform, false);
            cartSprite = cart.gameObject.AddComponent<SpriteRenderer>();
            UpdateVisual();
        }

        void LateUpdate() => UpdateVisual();

        void UpdateVisual()
        {
            if (cart == null || player == null) return;
            var state = GameManager.I != null ? GameManager.I.Player : null;
            int grade = RoadGrid.Current != null ? RoadGrid.Current.GradeAt(player.Motor.Cell) : 3;
            CurrentSway = state != null && !UIState.Blocking ? SwayStrength(grade, state.LoadRatio, player.Motor.Velocity.magnitude) : 0;
            phase += Time.deltaTime * 13f;
            float wobble = Mathf.Sin(phase) * CurrentSway;
            if (player.anim != null && player.anim.transform != transform)
                player.anim.transform.localPosition = bodyRest + new Vector3(wobble * .035f, Mathf.Abs(wobble) * .025f, 0);
            bool show = state != null && state.vehicle != null && state.vehicle.id == "handcart";
            cart.gameObject.SetActive(show);
            if (!show) return;
            var body = player.anim != null ? player.anim.GetComponent<SpriteRenderer>() : null;
            HandcartLook.Place(cartSprite, transform, body, player.Motor.Facing, HandcartLook.Stage(state.LoadRatio));
        }

        void OnDisable()
        {
            if (cart != null) cart.gameObject.SetActive(false);
            if (player != null && player.anim != null && player.anim.transform != transform) player.anim.transform.localPosition = bodyRest;
        }
    }
}
