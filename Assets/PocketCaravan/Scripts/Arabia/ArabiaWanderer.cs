using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 씬 전용: 마을 사람이 제자리 근처를 천천히 거닐고, 걷는 방향으로 4방향 걷기 애니메이션을 한다.
    /// - 집(처음 칸)에서 radius칸 안에서만 한 칸씩 움직이고, 막힌 칸·다른 사람·플레이어 칸은 피한다.
    /// - 플레이어가 가까이 오거나 대화·메뉴가 열려 있으면 멈추고 플레이어 쪽을 본다 (말 걸기 쉽게).
    /// 공용 코드는 바꾸지 않는다. ArabiaSceneDressing이 아라비아 씬의 TalkNpc(주민·이야기꾼)에만 붙인다.
    /// </summary>
    public class ArabiaWanderer : MonoBehaviour
    {
        public int radius = 2;
        public float speed = 1.6f;              // 칸/초
        public Vector2 pauseRange = new Vector2(1.5f, 4f);
        public float stopNearPlayer = 2.2f;     // 칸

        CharacterAnimator anim;
        Vector3Int home;
        Vector3 target;
        bool moving;
        float wait;
        PlayerMotor player;
        System.Random rng;

        static readonly Vector3Int[] Dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        void Start()
        {
            anim = GetComponent<CharacterAnimator>();
            var map = MapRoot.Current;
            home = map != null ? map.WorldToCell(transform.position) : Vector3Int.zero;
            target = transform.position;
            rng = new System.Random(home.x * 73856093 ^ home.y * 19349663);
            wait = (float)(pauseRange.x + rng.NextDouble() * (pauseRange.y - pauseRange.x));
            player = FindAnyObjectByType<PlayerMotor>();
        }

        void Update()
        {
            var map = MapRoot.Current;
            if (map == null || anim == null) return;
            if (player == null) player = FindAnyObjectByType<PlayerMotor>();

            bool hold = UIState.Blocking;
            if (player != null)
            {
                Vector2 d = player.transform.position - transform.position;
                if (d.magnitude < stopNearPlayer)
                {
                    hold = true;
                    if (!moving)
                        anim.facing = Mathf.Abs(d.x) >= Mathf.Abs(d.y)
                            ? new Vector2Int(d.x > 0 ? 1 : -1, 0) : new Vector2Int(0, d.y > 0 ? 1 : -1);
                }
            }

            if (moving)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                if ((transform.position - target).sqrMagnitude < 1e-6f)
                {
                    transform.position = target;
                    moving = false;
                    anim.walking = false;
                    wait = (float)(pauseRange.x + rng.NextDouble() * (pauseRange.y - pauseRange.x));
                }
                return;
            }
            anim.walking = false;
            if (hold) return;
            wait -= Time.deltaTime;
            if (wait > 0f) return;

            var here = map.WorldToCell(transform.position);
            var dir = Dirs[rng.Next(Dirs.Length)];
            var next = here + dir;
            wait = 0.6f;   // 못 가면 잠깐 뒤 다시
            if (Mathf.Abs(next.x - home.x) > radius || Mathf.Abs(next.y - home.y) > radius) return;
            if (!map.IsWalkable(next)) return;
            if (MapExit.At(next) != null) return;
            if (player != null && (player.Cell == next || map.WorldToCell(player.transform.position) == next)) return;
            target = map.CellCenter(next);
            moving = true;
            anim.facing = new Vector2Int(dir.x, dir.y);
            anim.walking = true;
        }
    }
}
