using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>적치장·작업장을 오가는 생활 연출. 공유 재고를 생산하거나 화물을 복제하지 않는다.</summary>
    public sealed class WorkingNpc : TalkNpc
    {
        public List<Vector3> path = new List<Vector3>();
        public CharacterAnimator anim;
        public float speed = 1.1f;
        int target;
        float pause = 2f;
        SpriteRenderer bundle;
        bool carrying;

        void Awake() => blocksMovement = false;

        void Start()
        {
            bundle = new GameObject("Work bundle").AddComponent<SpriteRenderer>();
            bundle.transform.SetParent(transform, false);
            bundle.transform.localPosition = new Vector3(0, .2f, 0);
            bundle.transform.localScale = Vector3.one * .45f;
            bundle.sprite = Resources.Load<Sprite>("Transport/crate");
            bundle.sortingOrder = 12;
            bundle.enabled = carrying;
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (anim == null || path.Count < 2) return;
            anim.walking = false;
            if (UIState.Blocking) return;
            if (pause > 0)
            {
                pause -= dt;
                if (pause <= 0) { carrying = !carrying; if (bundle != null) bundle.enabled = carrying; target = (target + 1) % path.Count; }
                return;
            }
            var player = PlayerController.Instance;
            if (player != null && Vector2.Distance(player.transform.position, transform.position) < 1f) return;
            var delta = path[target] - transform.position;
            if (delta.magnitude < .04f) { pause = 2f; return; }
            transform.position = Vector3.MoveTowards(transform.position, path[target], speed * dt);
            anim.walking = true;
            anim.facing = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? new Vector2Int(delta.x > 0 ? 1 : -1, 0) : new Vector2Int(0, delta.y > 0 ? 1 : -1);
        }
    }
}
