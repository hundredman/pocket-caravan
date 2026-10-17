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

        [Header("체력")]
        [Tooltip("달릴 때 초당 체력 감소")] public float staminaDrain = 0.2f;
        [Tooltip("쉬거나 걸을 때 초당 회복")] public float staminaRegen = 0.14f;

        [Header("흔들림 (초당 차오르는 양)")]
        public float shakeOffroad = 0.06f;
        public float shakePath = 0.02f;
        [Tooltip("달릴 때 배율")] public float shakeRunMultiplier = 3f;
        [Tooltip("멈추거나 좋은 길에서 초당 가라앉는 양")] public float shakeDecay = 0.15f;
        [Tooltip("손수레가 길 없는 곳을 지날 때 배율")] public float cartOffroadShake = 1.8f;

        bool exhausted;
        float bumpCooldown;
        /// <summary>지금 달리는 중</summary>
        public bool Running { get; private set; }
        public bool StaminaEnabled { get; set; } = true;
        public bool CargoShakeEnabled { get; set; } = true;

        public void ResetCarryState()
        {
            exhausted = false;
            bumpCooldown = 0f;
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
                var exit = MapExit.At(motor.Cell);
                if (exit != null) exit.Use(motor);
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
            return s * motor.walkSpeed;
        }

        int Grade => RoadGrid.Current != null ? RoadGrid.Current.GradeAt(motor.Cell) : 0;

        /// <summary>체력과 짐 흔들림. 흔들림이 가득 차면 짐 하나가 길에 떨어진다.</summary>
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

            // 흔들림
            if (!CargoShakeEnabled) { p.shake = 0f; return; }
            if (!p.HasAnyCargo) { p.shake = Mathf.Max(0f, p.shake - shakeDecay * dt); return; }
            int g = Grade;
            float rate = 0f;
            if (motor.IsMoving)
            {
                rate = g == 0 ? shakeOffroad : g == 1 ? shakePath : 0f;
                if (g == 0 && p.vehicle != null && p.vehicle.id == "handcart") rate *= cartOffroadShake;
                if (Running) rate = rate * shakeRunMultiplier + 0.04f;
                rate *= 0.5f + p.LoadRatio;
            }
            if (rate > 0f) p.shake += rate * dt;
            else p.shake = Mathf.Max(0f, p.shake - shakeDecay * dt);

            // 돌부리: 길 없는 곳을 달리면 가끔 덜컹
            bumpCooldown -= dt;
            if (g == 0 && Running && bumpCooldown <= 0f && Random.value < dt * 0.5f)
            {
                p.shake += 0.18f;
                bumpCooldown = 1.5f;
                Sfx.Play("bump");
                Notify.Show("덜컹! 돌부리에 걸렸다.", 1.5f);
            }

            if (p.shake >= 1f)
            {
                p.shake = 0.3f;
                GameManager.I.DropRandomCargo(transform.position - (Vector3)(Vector2)motor.Facing * 0.8f);
            }
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
