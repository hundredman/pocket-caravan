using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 길을 오가는 NPC 상인 (보이는 모습). 정해진 길목을 따라 왕복하고, 말을 걸면 NpcTraders의 소식과 시세 소문을 들려준다.
    /// </summary>
    public class Walker : Interactable
    {
        public List<Vector3> path = new List<Vector3>();
        public int traderIndex;
        public float speed = 1.6f;
        public CharacterAnimator anim;

        int target = 1, step = 1;
        float pause;

        NpcTraders.Trader Trader
        {
            get
            {
                var t = GameManager.I != null ? GameManager.I.Traders.traders : null;
                return t != null && t.Count > 0 ? t[traderIndex % t.Count] : null;
            }
        }

        public override string DisplayName => Trader != null ? $"상인 {Trader.name}" : "상인";

        void Awake() => blocksMovement = false;

        void Update()
        {
            if (path.Count < 2 || UIState.Blocking) { SetAnim(Vector2.zero); return; }
            if (pause > 0f) { pause -= Time.deltaTime; SetAnim(Vector2.zero); return; }
            // 플레이어가 바로 앞에 있으면 멈춰서 기다림
            var pl = PlayerController.Instance;
            if (pl != null && Vector2.Distance(pl.transform.position, transform.position) < 1.2f) { SetAnim(Vector2.zero); return; }

            Vector3 goal = path[target];
            Vector3 d = goal - transform.position;
            if (d.magnitude < 0.05f)
            {
                if (target == path.Count - 1 || target == 0) { step = -step; pause = Random.Range(1.5f, 4f); }
                target = Mathf.Clamp(target + step, 0, path.Count - 1);
                return;
            }
            Vector3 move = Vector3.ClampMagnitude(d, speed * Time.deltaTime);
            transform.position += move;
            SetAnim(d);
        }

        void SetAnim(Vector2 d)
        {
            if (anim == null) return;
            anim.walking = d.sqrMagnitude > 0.0001f;
            if (anim.walking)
                anim.facing = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
        }

        public override void Interact(PlayerController player)
        {
            var tr = Trader;
            string news = tr != null && !string.IsNullOrEmpty(tr.lastNews) ? tr.lastNews + "\n" : "";
            DialogueUI.Show(DisplayName, news + GameManager.I.Traders.PriceTip());
            // 플레이어 쪽을 봄
            Vector2 d = player.transform.position - transform.position;
            if (anim != null) anim.facing = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
            pause = 2f;
        }
    }
}
