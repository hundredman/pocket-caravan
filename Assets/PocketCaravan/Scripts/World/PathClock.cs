using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 온라인에서 길을 걷는 NPC(공용 Walker 상인 · 아라비아 순찰 경비병)를 모두 같은 자리에 보이게 (2026-11-08 기우).
    /// 서버 세상 시각(WorldClock.NowMs)으로 "지금 길 위의 어디"를 계산한다. 같은 맵이면 늦게 들어와도 같은 자리.
    /// 혼자 할 때는 쓰지 않는다 (예전 움직임 그대로). 표시만: 충돌·저장·경제 무관.
    /// </summary>
    public static class PathClock
    {
        public static double Seconds => WorldClock.NowMs / 1000.0;

        /// <summary>NPC마다 다른 출발 시각 (길 첫 칸과 번호로 정함, 모든 컴퓨터에서 같은 값)</summary>
        public static double Offset(List<Vector3> path, int salt)
        {
            if (path == null || path.Count == 0) return 0;
            uint h = 2166136261u;
            unchecked
            {
                h = (h ^ (uint)Mathf.RoundToInt(path[0].x * 4f)) * 16777619u;
                h = (h ^ (uint)Mathf.RoundToInt(path[0].y * 4f)) * 16777619u;
                h = (h ^ (uint)path.Count) * 16777619u;
                h = (h ^ (uint)salt) * 16777619u;
            }
            return h % 100000u;
        }

        static float Length(List<Vector3> p, bool loop)
        {
            float L = 0f;
            for (int i = 1; i < p.Count; i++) L += Vector3.Distance(p[i - 1], p[i]);
            if (loop && p.Count > 1) L += Vector3.Distance(p[p.Count - 1], p[0]);
            return L;
        }

        static Vector3 At(List<Vector3> p, bool loop, float dist)
        {
            int n = loop ? p.Count : p.Count - 1;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = p[i], b = p[(i + 1) % p.Count];
                float d = Vector3.Distance(a, b);
                if (dist <= d) return d > 1e-5f ? Vector3.Lerp(a, b, dist / d) : a;
                dist -= d;
            }
            return loop ? p[0] : p[p.Count - 1];
        }

        /// <summary>왕복 길: 끝에서 pause초 쉬고 되돌아온다. moving = 지금 걷는 중</summary>
        public static Vector3 PingPong(List<Vector3> p, float speed, float pause, double t, out bool moving)
        {
            moving = false;
            if (p == null || p.Count == 0) return Vector3.zero;
            float L = Length(p, false);
            if (p.Count < 2 || L < 1e-3f || speed <= 0f) return p[0];
            float walk = L / speed, cycle = 2f * (walk + pause);
            float u = (float)(t % cycle);
            if (u < walk) { moving = true; return At(p, false, u * speed); }
            u -= walk;
            if (u < pause) return p[p.Count - 1];
            u -= pause;
            if (u < walk) { moving = true; return At(p, false, L - u * speed); }
            return p[0];
        }

        /// <summary>고리 길: 끝에서 처음으로 이어 계속 돈다</summary>
        public static Vector3 Loop(List<Vector3> p, float speed, double t)
        {
            if (p == null || p.Count == 0) return Vector3.zero;
            float L = Length(p, true);
            if (p.Count < 2 || L < 1e-3f || speed <= 0f) return p[0];
            return At(p, true, (float)((t * speed) % L));
        }

        /// <summary>계산한 자리로 따라붙기: 멀면(4칸 넘게) 바로 옮기고, 아니면 조금 빠르게 걸어 따라간다. 움직인 방향을 돌려준다</summary>
        public static Vector3 Follow(Transform tr, Vector3 goal, float speed)
        {
            Vector3 d = goal - tr.position; d.z = 0f;
            if (d.sqrMagnitude > 16f) { tr.position = new Vector3(goal.x, goal.y, tr.position.z); return Vector3.zero; }
            Vector3 next = Vector3.MoveTowards(tr.position, new Vector3(goal.x, goal.y, tr.position.z), speed * 2f * Time.deltaTime);
            Vector3 moved = next - tr.position;
            tr.position = next;
            return moved;
        }

        public static Vector2Int Facing(Vector3 d) =>
            Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
    }
}
