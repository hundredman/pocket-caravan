using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 입력을 받아 PlayerMotor를 움직인다. 속도 = 걷기 속도 × 이동 수단의 길 등급별 속도 × 짐 무게 × 달리기.
    /// Space로 바라보는 쪽 사람에게 말을 건다. 출구 칸에 들어가면 다음 맵으로 이어진다.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerController : MonoBehaviour
    {
        public float runMultiplier = 1.6f;
        [Tooltip("짐이 가득 찼을 때 속도 배율")] public float fullLoadSpeed = 0.7f;
        [Tooltip("걷기 애니메이션 (그림이 붙은 자식)")] public CharacterAnimator anim;

        [Tooltip("말을 걸 수 있는 거리 (칸)")] public float talkRange = 1.8f;

        PlayerMotor motor;

        /// <summary>지금 Space를 누르면 말을 걸 대상 (머리 위 안내 표시용)</summary>
        public Interactable TalkTarget { get; private set; }

        public PlayerMotor Motor => motor;
        public static PlayerController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            motor = GetComponent<PlayerMotor>();
            if (anim == null) anim = GetComponentInChildren<CharacterAnimator>();
        }

        void Update()
        {
            if (GameManager.I == null) return;
            bool locked = UIState.Blocking || GameManager.I.IsTravelling;
            Vector2 input = locked ? Vector2.zero : InputReader.MoveVector();
            motor.Tick(input, CurrentSpeed(), Time.deltaTime);

            TalkTarget = locked ? null : Interactable.Nearest(transform.position, motor.Facing, talkRange);
            if (!locked)
            {
                if (InputReader.InteractPressed() && !UIState.JustClosed && TalkTarget != null) TalkTarget.Interact(this);
                var exit = MapExit.At(motor.Cell);
                if (exit != null) exit.Use(motor);
            }
            UpdateBody();
        }

        public float CurrentSpeed()
        {
            var p = GameManager.I.Player;
            int grade = RoadGrid.Current != null ? RoadGrid.Current.GradeAt(motor.Cell) : 0;
            float s = p.vehicle != null ? p.vehicle.SpeedOn(grade) : 1f;
            if (p.origin != null)
            {
                s *= p.origin.speedMultiplier;
                if (grade == 0) s *= p.origin.offroadSpeedMultiplier; // 길 없는 곳
            }
            s *= Mathf.Lerp(1f, fullLoadSpeed, p.LoadRatio);
            if (InputReader.Run()) s *= runMultiplier;
            return s * motor.walkSpeed;
        }

        void UpdateBody()
        {
            if (anim == null) return;
            anim.facing = motor.Facing;
            anim.walking = motor.IsMoving;
            // 달리면 발걸음도 빨라지게
            anim.framesPerSecond = 6f + motor.Velocity.magnitude * 1.2f;
        }
    }
}
