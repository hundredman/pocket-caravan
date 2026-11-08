using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 씬 전용: 경비병이 정해진 순찰길(칸 경로, 처음으로 돌아오는 고리)을 천천히 걷는다 (2026-10-27).
    /// - 갈림목·끝에서 가끔 멈춰 좌우를 둘러본다.
    /// - 플레이어가 가까이 오거나 대화·메뉴가 열려 있으면 멈추고 플레이어 쪽을 본다 (말 걸기 쉽게).
    /// - 다음 칸에 플레이어가 서 있으면 비켜 줄 때까지 기다린다.
    /// ArabiaSceneDressing이 맵 원본의 arabia_guard에서 만든다. 공용 코드는 바꾸지 않는다.
    /// </summary>
    public class ArabiaPatrol : MonoBehaviour
    {
        public List<Vector3> path = new List<Vector3>();
        public float speed = 1.5f;               // 칸/초
        public float stopNearPlayer = 1.8f;      // 칸

        CharacterAnimator anim;
        int index;
        float wait, lookTimer;
        PlayerMotor player;
        System.Random rng;

        void Start()
        {
            anim = GetComponent<CharacterAnimator>();
            player = FindAnyObjectByType<PlayerMotor>();
            rng = new System.Random(name.GetHashCode());
            if (path.Count > 0)
            {
                // 가장 가까운 칸부터 이어서 걷는다
                float best = float.MaxValue;
                for (int i = 0; i < path.Count; i++)
                {
                    float d = (path[i] - transform.position).sqrMagnitude;
                    if (d < best) { best = d; index = i; }
                }
                transform.position = path[index];
            }
            wait = (float)rng.NextDouble() * 2f;
        }

        void Update()
        {
            var map = MapRoot.Current;
            if (map == null || anim == null || path.Count < 2) return;
            if (player == null) player = FindAnyObjectByType<PlayerMotor>();

            bool hold = UIState.Blocking;
            if (player != null)
            {
                Vector2 d = player.transform.position - transform.position;
                if (d.magnitude < stopNearPlayer)
                {
                    hold = true;
                    anim.facing = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
                }
            }
            if (hold) { anim.walking = false; return; }

            if (WorldClock.Active)
            {   // 11/08 기우: 온라인에선 서버 세상 시각으로 순찰길 위 자리를 정해 모두 같은 곳에 보인다 (멈춰 둘러보기 없이 일정하게).
                // 내가 다가가거나 창을 열면 내 화면에서만 잠깐 멈췄다가, 떠나면 그 자리로 따라붙는다.
                var goal = PathClock.Loop(path, speed, PathClock.Seconds + PathClock.Offset(path, 7));
                Vector3 moved = PathClock.Follow(transform, goal, speed);
                anim.walking = moved.sqrMagnitude > 1e-8f;
                if (anim.walking) anim.facing = PathClock.Facing(moved);
                return;
            }

            if (wait > 0f)
            {
                wait -= Time.deltaTime;
                anim.walking = false;
                lookTimer -= Time.deltaTime;
                if (lookTimer <= 0f)                    // 멈춰 서서 좌우를 둘러본다
                {
                    lookTimer = 0.9f;
                    anim.facing = rng.Next(2) == 0 ? Vector2Int.left : Vector2Int.right;
                }
                return;
            }

            var target = path[(index + 1) % path.Count];
            if (player != null && map.WorldToCell(player.transform.position) == map.WorldToCell(target)) { anim.walking = false; return; }
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude > 1e-6f)
                anim.facing = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? new Vector2Int(dir.x > 0 ? 1 : -1, 0) : new Vector2Int(0, dir.y > 0 ? 1 : -1);
            anim.walking = true;
            if ((transform.position - target).sqrMagnitude < 1e-6f)
            {
                index = (index + 1) % path.Count;
                // 방향이 꺾이는 칸(갈림목·모퉁이)에서 가끔 멈춘다
                var prev = path[(index - 1 + path.Count) % path.Count];
                var next = path[(index + 1) % path.Count];
                bool turn = Vector3.Dot((target - prev).normalized, (next - target).normalized) < 0.5f;
                if (turn && rng.NextDouble() < 0.5) { wait = 1.2f + (float)rng.NextDouble() * 2f; lookTimer = 0f; }
            }
        }
    }
}
