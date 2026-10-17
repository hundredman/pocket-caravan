using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 맵 하나(씬 하나)의 기준. Grid 오브젝트에 붙인다.
    /// Ground에 타일이 있고 Blocked에 타일이 없는 칸만 걸을 수 있다.
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public class MapRoot : MonoBehaviour
    {
        public static MapRoot Current { get; private set; }

        [Tooltip("맵 고유 이름. 발길 저장 키로 쓰므로 바꾸지 말 것")] public string mapId;
        public string displayName;
        public RegionData region;
        [Tooltip("월드 지도에서의 좌표 (맵 단위)")] public Vector2Int worldCoord;
        [Tooltip("이 맵(길)에 들어서면 흐르는 게임 시간. 짧게 걸어도 하루 여정처럼 느껴지게")] public int travelHours;

        [Tooltip("제작 시험장: 빌드 0번 씬에서도 기존 저장 적용/자동 저장을 끈다")] public bool previewOnly;

        [Header("레이어")]
        public Tilemap ground;
        public Tilemap road;
        public Tilemap blocked;

        Grid grid;

        void Awake()
        {
            Current = this;
            grid = GetComponent<Grid>();
            if (ground != null) ground.CompressBounds();
        }

        public Vector3Int WorldToCell(Vector3 p) => (grid != null ? grid : GetComponent<Grid>()).WorldToCell(p);
        public Vector3 CellCenter(Vector3Int c) => (grid != null ? grid : GetComponent<Grid>()).GetCellCenterWorld(c);

        public bool IsWalkable(Vector3Int c, PlayerMotor actor = null)
        {
            if (ground == null || !ground.HasTile(c)) return false;
            if (blocked != null && blocked.HasTile(c)) return false;
            if (Interactable.BlockerAt(c) != null) return false;
            var obstacles=GetComponent<ObstacleField>();
            if(obstacles!=null && obstacles.Blocks(c,actor))return false;
            return true;
        }

        /// <summary>지형 수정 후 저장 발밑이 막혔다면 가장 가까운 통행 가능한 길로 옮긴다.
        /// 유효한 저장 위치는 그대로 유지하며, 경계 출구 위로 복구해 즉시 재이동하지 않는다.</summary>
        public bool TryResolveSavedPosition(Vector3 saved, Vector2 feetOffset, out Vector3 restored)
        {
            restored = saved;
            var feet = saved + (Vector3)feetOffset;
            if (IsWalkable(WorldToCell(feet))) return true;
            if (ground == null) return false;
            float best = float.PositiveInfinity;
            var bounds = ground.cellBounds;
            var roadGrid = GetComponent<RoadGrid>();
            // 우선 연결된 도로, 도로가 전혀 없는 제작 구역은 일반 지면을 사용한다.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        var cell = new Vector3Int(x, y, 0);
                        if (!IsWalkable(cell) || MapExit.At(cell) != null) continue;
                        bool onRoad = (roadGrid != null && roadGrid.GradeAt(cell) > 0) || (road != null && road.HasTile(cell));
                        if (pass == 0 && !onRoad) continue;
                        var center = CellCenter(cell);
                        float distance = (center - feet).sqrMagnitude;
                        if (distance >= best) continue;
                        best = distance;
                        restored = center - (Vector3)feetOffset;
                    }
                if (!float.IsPositiveInfinity(best)) return true;
            }
            return false;
        }

        public Bounds WorldBounds
        {
            get
            {
                if (ground == null) return new Bounds(transform.position, Vector3.one * 10);
                var b = ground.cellBounds;
                Vector3 min = ground.CellToWorld(b.min);
                Vector3 max = ground.CellToWorld(b.max);
                var bounds = new Bounds();
                bounds.SetMinMax(min, max);
                return bounds;
            }
        }
    }
}
