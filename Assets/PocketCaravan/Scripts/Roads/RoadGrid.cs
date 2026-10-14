using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 발길로 자라는 길. 칸마다 발길 값을 쌓고, 기준을 넘으면 등급이 오른다.
    /// 한 걸음은 옆 칸에도 조금 남아서 많이 다닌 곳일수록 길이 넓어진다.
    /// 발길은 하루마다 조금씩 줄어 덜 다닌 가장자리부터 풀밭으로 돌아간다 (GameManager.OnNewDay).
    /// 맵에 미리 그려 둔 길 타일은 처음 들어올 때 발길 값으로 바뀐다.
    /// </summary>
    public class RoadGrid : MonoBehaviour
    {
        public static RoadGrid Current { get; private set; }

        public Tilemap roadTilemap;
        public RoadTileSet tiles;

        Dictionary<Vector3Int, float> footfall;
        readonly HashSet<Vector3Int> invested = new HashSet<Vector3Int>(); // 큰길 (추후: 투자 시스템)
        RoadSettings S => GameManager.I.Db.roads;

        static readonly Vector3Int[] Neighbors = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        void Awake() => Current = this;

        void Start()
        {
            var map = MapRoot.Current;
            if (roadTilemap == null && map != null) roadTilemap = map.road;
            footfall = GameManager.I.GetFootfall(map != null ? map.mapId : gameObject.scene.name, out bool fresh);
            if (fresh) SeedFromTiles();
            RepaintAll();
            GameManager.I.Clock.NewDay += OnNewDay;
        }

        void OnDestroy()
        {
            if (GameManager.I != null) GameManager.I.Clock.NewDay -= OnNewDay;
            if (Current == this) Current = null;
        }

        void OnNewDay(int day) => RepaintAll(); // 발길 감소는 GameManager가 먼저 처리

        void SeedFromTiles()
        {
            if (roadTilemap == null || tiles == null) return;
            foreach (var pos in roadTilemap.cellBounds.allPositionsWithin)
            {
                int g = tiles.GradeOf(roadTilemap.GetTile(pos));
                if (g > 0) footfall[pos] = S.thresholds[g] * S.seededMultiplier;
            }
        }

        public float FootfallAt(Vector3Int c) => footfall != null && footfall.TryGetValue(c, out var f) ? f : 0f;

        public int GradeAt(Vector3Int c) => GradeFor(c, FootfallAt(c));

        int GradeFor(Vector3Int c, float f)
        {
            int g = 0;
            for (int i = 1; i < S.thresholds.Length; i++) if (f >= S.thresholds[i]) g = i;
            int cap = invested.Contains(c) ? 3 : S.maxNaturalGrade;
            return Mathf.Min(g, cap);
        }

        public void AddFootfall(Vector3Int c, float amount)
        {
            if (footfall == null) return;
            Add(c, amount);
            foreach (var n in Neighbors)
            {
                var nc = c + n;
                if (MapRoot.Current != null && MapRoot.Current.IsWalkable(nc)) Add(nc, amount * S.neighborShare);
            }
        }

        void Add(Vector3Int c, float amount)
        {
            int before = GradeAt(c);
            footfall[c] = FootfallAt(c) + amount;
            int after = GradeAt(c);
            if (after != before) Paint(c, after);
        }

        void Paint(Vector3Int c, int grade)
        {
            if (roadTilemap == null || tiles == null) return;
            roadTilemap.SetTile(c, tiles.ForGrade(grade));
        }

        public void RepaintAll()
        {
            if (roadTilemap == null || tiles == null || footfall == null) return;
            roadTilemap.ClearAllTiles();
            foreach (var kv in footfall)
            {
                int g = GradeFor(kv.Key, kv.Value);
                if (g > 0) roadTilemap.SetTile(kv.Key, tiles.ForGrade(g));
            }
        }
    }
}
