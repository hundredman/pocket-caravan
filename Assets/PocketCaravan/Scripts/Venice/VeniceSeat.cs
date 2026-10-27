using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 벤치에 앉기 (2026-10-27, 제안). 벤치 바로 앞 칸에서 Space: 고양이가 벤치에 올라앉아 앞을 본다.
    /// 앉아 있는 동안 체력이 세 배로 차고, 4초쯤 쉬면 비둘기가 다가오고 "한숨 돌림"(1시간 동안 달릴 때 체력 덜 닮)을 얻는다.
    /// 방향키를 누르면 일어난다. 그림 위치·크기만 바꿨다가 되돌린다(이동·충돌·저장 형식은 그대로, 버프는 VeniceSave "breather_until").
    /// </summary>
    [DefaultExecutionOrder(1001)]
    public class VeniceSeat : Interactable
    {
        public override string DisplayName => "벤치에 앉기";
        public override Kind Category => Kind.Market;

        static VeniceSeat sitting;
        float since;
        bool rewarded;
        Vector3 rest, restScale; bool applied;

        void Awake() => blocksMovement = false;

        public static bool IsSitting => sitting != null;
        public static bool Breather => GameManager.I != null && GameManager.I.Clock.TotalHours < VeniceSave.Get("breather_until", -1f);

        public override void Interact(PlayerController player)
        {
            if (sitting == this) { Stand(); return; }
            sitting = this;
            player.Motor.Teleport(transform.position, Vector2Int.down);     // 벤치 앞 칸으로 올라앉아 앞을 본다 (아래·옆에서 눌러도)
            since = Time.time;
            rewarded = false;
            Sfx.Play("step", .6f);
            Notify.Show("벤치에 앉았다. 방향키로 일어난다.", 2f);
        }

        void Stand()
        {
            Undo();
            sitting = null;
        }

        void Update()
        {
            if (sitting != this) return;
            var pc = PlayerController.Instance;
            Undo();
            if (pc == null || (!UIState.Blocking && InputReader.MoveVector().sqrMagnitude > .01f))
            {
                sitting = null;
                return;
            }
            var st = GameManager.I.Player;
            st.stamina = Mathf.Min(1f, st.stamina + pc.staminaRegen * 2f * Time.deltaTime);
            if (!rewarded && Time.time - since > 4f)
            {
                rewarded = true;
                VeniceSave.Set("breather_until", GameManager.I.Clock.TotalHours + 1f);
                VeniceLife.Attract(transform.position, 5f);
                Notify.Show("한숨 돌렸다. 1시간 동안 덜 지친다.", 3f);
            }
        }

        void LateUpdate()
        {
            if (sitting != this) return;
            var pc = PlayerController.Instance;
            if (pc == null || pc.anim == null) return;
            var t = pc.anim.transform;
            rest = t.localPosition; restScale = t.localScale; applied = true;
            t.localPosition = rest + new Vector3(0f, .5f, 0f);                    // 벤치 위로
            t.localScale = new Vector3(restScale.x * 1.06f, restScale.y * .86f, restScale.z);
            pc.anim.facing = Vector2Int.down;
            var sr = pc.anim.GetComponent<SpriteRenderer>();
            if (sr != null && pc.anim.frames != null && pc.anim.frames.Length > 0 && pc.anim.frames[0] != null) sr.sprite = pc.anim.frames[0];
        }

        void Undo()
        {
            if (!applied) return;
            var pc = PlayerController.Instance;
            if (pc != null && pc.anim != null) { pc.anim.transform.localPosition = rest; pc.anim.transform.localScale = restScale; }
            applied = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Undo();
            if (sitting == this) sitting = null;
        }
    }
}
