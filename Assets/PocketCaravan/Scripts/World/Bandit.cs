using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 도적. 가까이 오면 쫓아오고, 잡히면 통행료를 내거나 가장 비싼 짐을 빼앗긴다.
    /// 짐을 실은 걸음보다 조금 빠르고 달리기보다 느리다: 달려서 따돌릴 수 있다.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class Bandit : Interactable
    {
        public enum State { Lurk, Chase, Leave }

        public Vector3 home;
        public float sightRange = 6f;
        [Tooltip("쫓는 속도 (칸/초)")] public float chaseSpeed = 3.4f;
        public float giveUpRange = 10f;
        public CharacterAnimator anim;
        public SpriteRenderer alert;

        PlayerMotor motor;
        State state = State.Lurk;
        float stateTime, wanderTimer;
        Vector2 wanderDir;
        bool met;

        public override string DisplayName => "도적";
        public override Kind Category => Kind.Danger;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            blocksMovement = false;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            var gm = GameManager.I;
            if (player == null || gm == null) return;
            stateTime += Time.deltaTime;
            bool locked = UIState.Blocking || gm.IsTravelling;
            Vector2 to = player.transform.position - transform.position;
            float dist = to.magnitude;
            Vector2 dir = Vector2.zero;
            float speed = 2f;

            switch (state)
            {
                case State.Lurk:
                    wanderTimer -= Time.deltaTime;
                    if (wanderTimer <= 0f)
                    {
                        wanderTimer = Random.Range(1f, 2.5f);
                        Vector2 back = home - transform.position;
                        wanderDir = back.magnitude > 2f ? back.normalized : (Random.value < 0.4f ? Vector2.zero : Random.insideUnitCircle.normalized);
                    }
                    dir = wanderDir;
                    speed = 1.2f;
                    if (dist < sightRange && !locked && gm.Player.HasAnyCargo) StartChase();
                    break;
                case State.Chase:
                    dir = to.normalized;
                    speed = chaseSpeed;
                    if (alert != null) alert.enabled = stateTime < 1.2f;
                    if (dist < 0.75f && !locked) Catch();
                    else if (dist > giveUpRange || stateTime > 14f)
                    {
                        Notify.Show("도적을 따돌렸다!");
                        Sfx.Play("success");
                        Leave();
                    }
                    break;
                case State.Leave:
                    dir = (Vector2)(transform.position - player.transform.position).normalized;
                    speed = 3f;
                    if (stateTime > 3f) Destroy(gameObject);
                    break;
            }
            if (locked) dir = Vector2.zero;
            motor.Tick(dir, speed, Time.deltaTime);
            if (anim != null)
            {
                anim.facing = motor.Facing;
                anim.walking = motor.IsMoving;
                anim.framesPerSecond = 6f + motor.Velocity.magnitude * 1.2f;
            }
        }

        void StartChase()
        {
            state = State.Chase;
            stateTime = 0f;
            Sfx.Play("alert");
            AudioManager.SetDanger(true);
            Notify.Show("도적이다! Shift로 달려서 따돌리자.", 3f);
        }

        void Leave()
        {
            state = State.Leave;
            stateTime = 0f;
            if (alert != null) alert.enabled = false;
            AudioManager.SetDanger(false);
            GameManager.I.BanditMet(MapRoot.Current != null ? MapRoot.Current.mapId : "");
        }

        public override void Interact(PlayerController player)
        {
            if (state != State.Leave) Catch();
        }

        void Catch()
        {
            if (met) return;
            met = true;
            var gm = GameManager.I;
            var p = gm.Player;
            int toll = Mathf.Max(10, Mathf.RoundToInt(p.money * 0.15f));
            string loot = gm.MostValuableCargoName();
            if (loot == null) { DialogueUI.Show("도적", "쳇, 빈털터리잖아. 썩 꺼져!", new DialogueUI.Choice("확인", Leave)); return; }
            var choices = p.money >= toll
                ? new[] { new DialogueUI.Choice($"통행료 {toll}냥", () => { p.money -= toll; Sfx.Play("coin"); Notify.Show($"통행료 {toll}냥을 냈다."); Leave(); }),
                          new DialogueUI.Choice("거절한다", () => { gm.StealMostValuable(); Leave(); }) }
                : new[] { new DialogueUI.Choice("거절한다", () => { gm.StealMostValuable(); Leave(); }) };
            DialogueUI.Show("도적", $"거기 짐 실은 양반, 멈추시오! 이 길을 지나려면 통행료 {toll}냥을 내야지. 싫으면 그 {Josa.Eul(loot)} 두고 가시오.", choices);
        }

        void OnDestroy()
        {
            if (state == State.Chase) AudioManager.SetDanger(false);
        }
    }
}
