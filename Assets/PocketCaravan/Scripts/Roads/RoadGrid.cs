using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 맵에 미리 깔아 둔 길의 등급을 읽는다 (0 길 없음, 1 오솔길, 2 길, 3 큰길).
    /// 길은 맵 제작자가 Road 타일맵에 칠한다. 플레이어가 길을 만들지는 않는다 (MMORPG에서 맵이 망가지지 않게).
    /// 등급은 이동 수단의 속도에 쓰인다.
    /// </summary>
    public class RoadGrid : MonoBehaviour
    {
        public static RoadGrid Current { get; private set; }

        public Tilemap roadTilemap;
        public RoadTileSet tiles;

        void Awake() => Current = this;

        void Start()
        {
            if (roadTilemap == null && MapRoot.Current != null) roadTilemap = MapRoot.Current.road;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public int GradeAt(Vector3Int c)
        {
            if (roadTilemap == null || tiles == null) return 0;
            return tiles.GradeOf(roadTilemap.GetTile(c));
        }
    }
}
