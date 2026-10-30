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
        /// <summary>임시 시험용: G를 누르는 동안 속도 배율 (나중에 제거). 씬마다 저장된 값이 달라지지 않게 코드 상수로 둔다.</summary>
        public const float DebugFastMultiplier = 6f;
        [Tooltip("짐이 가득 찼을 때 속도 배율")] public float fullLoadSpeed = 0.7f;
        [Tooltip("걷기 애니메이션 (그림이 붙은 자식)")] public CharacterAnimator anim;

        [Tooltip("말을 걸 수 있는 거리 (칸)")] public float talkRange = 1.8f;

        [Header("체력")]
        [Tooltip("달릴 때 초당 체력 감소")] public float staminaDrain = 0.2f;
        [Tooltip("쉬거나 걸을 때 초당 회복")] public float staminaRegen = 0.14f;

        [Header("이전 낙하 설정 (호환 전용·미사용)")]
        public float shakeOffroad = 0.06f;
        public float shakePath = 0.02f;
        [Tooltip("달릴 때 배율")] public float shakeRunMultiplier = 3f;
        [Tooltip("멈추거나 좋은 길에서 초당 가라앉는 양")] public float shakeDecay = 0.15f;
        [Tooltip("손수레가 길 없는 곳을 지날 때 배율")] public float cartOffroadShake = 1.8f;

        bool exhausted;
        /// <summary>지금 달리는 중</summary>
        public bool Running { get; private set; }
        public bool StaminaEnabled { get; set; } = true;
        public bool CargoShakeEnabled { get => false; set { } } // 이전 도구 API 호환

        public void ResetCarryState()
        {
            exhausted = false;
            if (GameManager.I == null) return;
            GameManager.I.Player.stamina = 1f;
            GameManager.I.Player.shake = 0f;
        }

        PlayerMotor motor;

        /// <summary>지금 Space를 누르면 말을 걸 대상 (머리 위 안내 표시용)</summary>
        public Interactable TalkTarget { get; private set; }

        public PlayerMotor Motor => motor;
        public static PlayerController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            motor = GetComponent<PlayerMotor>();
            if (GetComponent<CarryVisual>() == null) gameObject.AddComponent<CarryVisual>();
            if (anim == null) anim = GetComponentInChildren<CharacterAnimator>();
        }

        void Update()
        {
            if (GameManager.I == null) return;
            bool locked = UIState.Blocking || GameManager.I.IsTravelling;
            Vector2 input = locked ? Vector2.zero : InputReader.MoveVector();
            Running = !locked && !exhausted && InputReader.Run() && input.sqrMagnitude > 0.01f;
            motor.Tick(input, CurrentSpeed(), Time.deltaTime);
            if (!locked) UpdateCarry(Time.deltaTime);

            TalkTarget = locked ? null : Interactable.Nearest(transform.position, motor.Facing, talkRange);
            if (!locked)
            {
                if (InputReader.InteractPressed() && !UIState.JustClosed && TalkTarget != null) TalkTarget.Interact(this);
                var exit = MapExit.At(motor.Cell,motor.Facing);
                if (exit != null) exit.TryUseAtBoundary(motor, input);
            }
            UpdateBody();
        }

        public float CurrentSpeed()
        {
            var p = GameManager.I.Player;
            int grade = Grade;
            float s = p.vehicle != null ? p.vehicle.SpeedOn(grade) : 1f;
            if (p.origin != null)
            {
                s *= p.origin.speedMultiplier;
                if (grade == 0) s *= p.origin.offroadSpeedMultiplier; // 길 없는 곳
            }
            s *= Mathf.Lerp(1f, fullLoadSpeed, p.LoadRatio);
            if (Running) s *= runMultiplier;
            if (InputReader.DebugFast()) s *= DebugFastMultiplier;   // 임시 시험용 (G, 6배)
            var obstacles=MapRoot.Current!=null?MapRoot.Current.GetComponent<ObstacleField>():null;
            if(obstacles!=null){obstacles.Effects(motor.Cell,out float multiplier,out _,out _,out _,out _);s*=multiplier;}
            return s * motor.walkSpeed;
        }

        int Grade => RoadGrid.Current != null ? RoadGrid.Current.GradeAt(motor.Cell) : 0;

        /// <summary>달리기 체력만 계산한다. 시각 흔들림은 CarryVisual, 낙하는 사용하지 않는다.</summary>
        void UpdateCarry(float dt)
        {
            var p = GameManager.I.Player;
            // 체력
            if (!StaminaEnabled) { p.stamina = 1f; exhausted = false; }
            else if (Running)
            {
                p.stamina -= staminaDrain * dt;
                if (p.stamina <= 0f) { p.stamina = 0f; exhausted = true; Notify.Show("숨이 차서 더 달릴 수 없다."); }
            }
            else
            {
                p.stamina = Mathf.Min(1f, p.stamina + staminaRegen * dt * (motor.IsMoving ? 0.6f : 1f));
                if (exhausted && p.stamina >= 0.35f) exhausted = false;
            }

            // 이전 저장/시험 도구와 필드 호환만 유지한다. 주행 낙하·정차 안정화는 사용하지 않는다.
            p.shake = 0f;
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
