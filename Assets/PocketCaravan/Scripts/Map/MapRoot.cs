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

        public bool IsWalkable(Vector3Int c)
        {
            if (ground == null || !ground.HasTile(c)) return false;
            if (blocked != null && blocked.HasTile(c)) return false;
            if (Interactable.BlockerAt(c) != null) return false;
            return true;
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
