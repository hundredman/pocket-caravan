using UnityEngine;

namespace PocketCaravan
{
    /// <summary>손수레와 적재량을 보여 주는 표현. 충돌·속도·화물 상태는 기존 운반 코드가 담당한다.</summary>
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
        readonly SpriteRenderer[] crates = new SpriteRenderer[3];

        void Start()
        {
            player = GetComponent<PlayerController>();
            if (player.anim != null) bodyRest = player.anim.transform.localPosition;
            cart = new GameObject("Handcart visual").transform;
            cart.SetParent(transform, false);
            cartSprite = cart.gameObject.AddComponent<SpriteRenderer>();
            cartSprite.sprite = Resources.Load<Sprite>("Transport/handcart");
            var crate = Resources.Load<Sprite>("Transport/crate");
            for (int i = 0; i < crates.Length; i++)
            {
                crates[i] = new GameObject("Cargo " + i).AddComponent<SpriteRenderer>();
                crates[i].transform.SetParent(cart, false);
                crates[i].transform.localPosition = new Vector3((i - 1) * .28f, -.14f, 0);
                crates[i].transform.localScale = Vector3.one * .42f;
                crates[i].sprite = crate;
            }
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
            Vector2 facing = player.Motor.Facing;
            cart.localPosition = -(Vector3)(facing * .95f) + new Vector3(wobble * .08f, -.1f + Mathf.Abs(wobble) * .05f, 0);
            cart.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg + wobble * 4f);
            int order = facing.y > 0 ? 12 : 8;
            cartSprite.sortingOrder = order;
            for (int i = 0; i < crates.Length; i++)
            {
                crates[i].enabled = state.LoadRatio > i / 3f;
                crates[i].sortingOrder = order + 1;
            }
        }

        void OnDisable()
        {
            if (cart != null) cart.gameObject.SetActive(false);
            if (player != null && player.anim != null && player.anim.transform != transform) player.anim.transform.localPosition = bodyRest;
        }
    }
}
