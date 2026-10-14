using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 입력을 받아 GridMover를 움직인다. 속도 = 이동 수단의 길 등급별 속도 × 짐 무게 × 달리기.
    /// 운반 담당은 흔들림·도적 등을 여기(또는 별도 컴포넌트)에 더한다.
    /// </summary>
    [RequireComponent(typeof(GridMover))]
    public class PlayerController : MonoBehaviour
    {
        public float runMultiplier = 1.6f;
        [Tooltip("짐이 가득 찼을 때 속도 배율")] public float fullLoadSpeed = 0.65f;
        [Tooltip("그림이 붙은 자식 (걸을 때 통통 튀는 연출)")] public SpriteRenderer body;

        GridMover mover;

        public GridMover Mover => mover;
        public static PlayerController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            mover = GetComponent<GridMover>();
            mover.Stepped += OnStepped;
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (UIState.Blocking || GameManager.I == null) return;

            if (InputReader.InteractPressed()) TryInteract();

            if (!mover.IsMoving)
            {
                var dir = InputReader.Move();
                if (dir != Vector2Int.zero) mover.TryMove(dir, CurrentSpeed(dir));
            }

            UpdateBody();
        }

        public float CurrentSpeed(Vector2Int dir)
        {
            var p = GameManager.I.Player;
            var target = mover.Cell + new Vector3Int(dir.x, dir.y, 0);
            int grade = RoadGrid.Current != null ? RoadGrid.Current.GradeAt(target) : 0;
            float s = p.vehicle != null ? p.vehicle.SpeedOn(grade) : 1f;
            s *= Mathf.Lerp(1f, fullLoadSpeed, p.LoadRatio);
            if (InputReader.Run()) s *= runMultiplier;
            return s;
        }

        void OnStepped(Vector3Int cell)
        {
            if (RoadGrid.Current != null) RoadGrid.Current.AddFootfall(cell, GameManager.I.Player.FootfallPerStep);
            var exit = MapExit.At(cell);
            if (exit != null) exit.Use();
        }

        void TryInteract()
        {
            var f = mover.Facing;
            var it = Interactable.At(mover.Cell + new Vector3Int(f.x, f.y, 0));
            if (it != null) it.Interact(this);
        }

        void UpdateBody()
        {
            if (body == null) return;
            if (mover.Facing.x != 0) body.flipX = mover.Facing.x < 0;
            // 걸을 때 1픽셀 통통 (16 PPU 기준)
            float bob = mover.IsMoving && mover.StepProgress > 0.25f && mover.StepProgress < 0.75f ? 1f / 16f : 0f;
            body.transform.localPosition = new Vector3(0f, bob, 0f);
        }
    }
}
