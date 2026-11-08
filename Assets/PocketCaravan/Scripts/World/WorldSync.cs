using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 온라인에서 꾸미기 연출도 모두 같게 보이게 하는 도우미 (2026-11-08 기우).
    /// 혼자 할 때는 예전처럼 UnityEngine.Random, 온라인(WorldClock.Active)에선 서버 세상 시각으로 정한 값.
    /// - Rand: 둘 중 하나로 뽑는 난수
    /// - WorldBeat.Due: "가끔 한 번" (불꽃 터뜨리기 등). 온라인이면 세상 시각 칸마다 한 번, 칸 번호로 정한 난수
    /// - WorldWander.At: 영역 안을 돌아다니는 동물 자리 (세상 시각만으로 정해지는 자리)
    /// </summary>
    public sealed class Rand
    {
        readonly System.Random r;
        public Rand(System.Random r) { this.r = r; }
        public static readonly Rand Unity = new Rand(null);
        public float value => r == null ? UnityEngine.Random.value : (float)r.NextDouble();
        public float Range(float a, float b) => a + (b - a) * value;
        public int Range(int a, int b) => r == null ? UnityEngine.Random.Range(a, b) : r.Next(a, b);
    }

    public static class WorldBeat
    {
        public static bool Online => WorldClock.Active;
        public static double Seconds => WorldClock.NowMs / 1000.0;

        /// <summary>
        /// 다음 차례가 왔으면 true. 혼자: now가 next를 넘으면 next = now + (min~max). 온라인: (min+max)/2초 칸마다 한 번.
        /// next는 그 상태를 담는 칸 (처음 0).
        /// </summary>
        public static bool Due(ref double next, float now, float min, float max, int salt, out Rand rand)
        {
            rand = Rand.Unity;
            if (!Online)
            {
                if (next > 1e8) next = 0;            // 온라인에서 쓰던 칸 번호였으면 새로
                if (now < next) return false;
                next = now + UnityEngine.Random.Range(min, max);
                return true;
            }
            float len = Mathf.Max(0.1f, (min + max) * 0.5f);
            long slot = (long)System.Math.Floor(Seconds / len);
            double key = 1e9 + slot;                 // 온라인 칸 번호 (혼자 쓰는 시각과 겹치지 않게 크게)
            if (next == key) return false;
            next = key;
            rand = new Rand(new System.Random(unchecked((int)(slot * 7919L + salt * 104729L))));
            return true;
        }
    }

    public static class WorldWander
    {
        static float H(int id, long k, int c)
        {
            uint h = 2166136261u;
            unchecked
            {
                h = (h ^ (uint)id) * 16777619u; h = (h ^ (uint)k) * 16777619u; h = (h ^ (uint)(k >> 32)) * 16777619u; h = (h ^ (uint)c) * 16777619u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            }
            return (h % 10000u) / 10000f;
        }

        static Vector3 Pt(Rect a, int id, long k) => new Vector3(a.xMin + a.width * H(id, k, 1), a.yMin + a.height * H(id, k, 2), 0f);

        /// <summary>
        /// 세상 시각 t에 영역 안 동물의 자리. period초마다 새 목적지로 걸어가고(speed) 남은 시간은 쉰다.
        /// moving: 걷는 중, dx: 걷는 좌우 방향, rest: 쉬는 동안 할 몸짓(풀 뜯기 등)을 할지
        /// </summary>
        public static Vector3 At(Rect area, int id, float speed, float period, double t, out bool moving, out float dx, out bool rest)
        {
            double tt = t + H(id, 0, 9) * period;
            long k = (long)System.Math.Floor(tt / period);
            float u = (float)(tt - k * (double)period);
            Vector3 from = Pt(area, id, k - 1), to = Pt(area, id, k);
            float travel = Mathf.Min(Vector3.Distance(from, to) / Mathf.Max(0.01f, speed), period * 0.75f);
            dx = to.x - from.x;
            rest = H(id, k, 3) < 0.6f;
            moving = u < travel;
            return moving ? Vector3.Lerp(from, to, travel > 0f ? u / travel : 1f) : to;
        }
    }
}
