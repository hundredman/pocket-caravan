using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 자유 이동 (스타듀밸리식). 8방향, 짧은 가속·감속, 막힌 칸과 부딪히면 모서리를 따라 미끄러진다.
    /// 충돌은 발밑 작은 상자로만 판단하고, 막힘 여부는 MapRoot.IsWalkable(칸 단위)을 쓴다.
    /// </summary>
    public class PlayerMotor : MonoBehaviour
    {
        [Tooltip("속도 배율 1일 때 초당 칸 수")] public float walkSpeed = 3.8f;
        [Tooltip("출발할 때 가속 (칸/초²)")] public float accel = 40f;
        [Tooltip("멈출 때 감속 (칸/초²)")] public float decel = 60f;
        [Tooltip("발밑 상자 중심 (transform 기준)")] public Vector2 feetOffset = new Vector2(0f, -0.32f);
        public Vector2 feetSize = new Vector2(0.5f, 0.25f);
        [Tooltip("모서리에 걸렸을 때 옆으로 비켜 주는 최대 거리")] public float cornerSlide = 0.4f;

        public Vector2 Velocity { get; private set; }
        /// <summary>이번 Tick에서 시도한 이동. 벽에 막혀 속도가0이 되어도 경계 접촉 판정에 쓴다.</summary>
        public Vector2 AttemptedDelta { get; private set; }
        public Vector2Int Facing { get; private set; } = Vector2Int.down;
        public bool IsMoving => Velocity.sqrMagnitude > 0.04f;
        public Vector2 Feet => (Vector2)transform.position + feetOffset;
        public Vector3Int Cell => MapRoot.Current != null ? MapRoot.Current.WorldToCell(Feet) : Vector3Int.zero;

        public void Teleport(Vector3 pos, Vector2Int facing)
        {
            transform.position = pos;
            Velocity = Vector2.zero; AttemptedDelta = Vector2.zero;
            if (facing != Vector2Int.zero) Facing = facing;
        }

        public void Tick(Vector2 input, float speed, float dt)
        {
            if (input.sqrMagnitude > 1f) input.Normalize();
            if (input.sqrMagnitude > 0.01f)
                Facing = Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                    ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                    : new Vector2Int(0, input.y > 0 ? 1 : -1);

            float traction=1;Vector2 drift=Vector2.zero;
            var obstacles=MapRoot.Current!=null?MapRoot.Current.GetComponent<ObstacleField>():null;
            if(obstacles!=null)obstacles.Effects(Cell,out _,out traction,out drift,out _,out _);
            Vector2 target = input * speed;
            float rate = input.sqrMagnitude > 0.01f ? accel * Mathf.Max(1f, speed / walkSpeed) : decel;
            Velocity = Vector2.MoveTowards(Velocity, target, rate * Mathf.Clamp(traction,.05f,1) * dt);

            Vector2 delta = (Velocity + drift) * dt;
            AttemptedDelta = delta;
            MoveAxis(new Vector2(delta.x, 0f));
            MoveAxis(new Vector2(0f, delta.y));
        }

        void MoveAxis(Vector2 d)
        {
            if (d.sqrMagnitude < 1e-10f) return;
            Vector2 pos = transform.position;
            if (Free(pos + d)) { transform.position = pos + d; return; }

            // 모서리에 살짝 걸렸으면 옆으로 비켜서 지나가게
            Vector2 perp = d.x != 0f ? Vector2.up : Vector2.right;
            float step = d.magnitude;
            for (float n = 0.05f; n <= cornerSlide; n += 0.05f)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 side = perp * (n * s);
                    if (Free(pos + side) && Free(pos + side + d))
                    {
                        transform.position = pos + perp * (Mathf.Min(n, step) * s);
                        return;
                    }
                }
            }
            Velocity = d.x != 0f ? new Vector2(0f, Velocity.y) : new Vector2(Velocity.x, 0f);
        }

        bool Free(Vector2 pos)
        {
            var map = MapRoot.Current;
            if (map == null) return true;
            Vector2 c = pos + feetOffset, h = feetSize * 0.5f;
            Vector2 a=new Vector2(c.x-h.x,c.y-h.y),b=new Vector2(c.x+h.x,c.y-h.y);
            Vector2 e=new Vector2(c.x-h.x,c.y+h.y),f=new Vector2(c.x+h.x,c.y+h.y);
            if(FreeSamples(map,a,b,e,f))return true;
            var exit=MapExit.At(map.WorldToCell(c),Facing);
            return exit!=null&&exit.TryProjectFootprint(c,ref a,ref b,ref e,ref f)&&FreeSamples(map,a,b,e,f);
        }

        bool FreeSamples(MapRoot map,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            return map.IsWalkable(map.WorldToCell(a),this)&&map.IsWalkable(map.WorldToCell(b),this)
                &&map.IsWalkable(map.WorldToCell(c),this)&&map.IsWalkable(map.WorldToCell(d),this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)transform.position + feetOffset, feetSize);
        }
    }
}
