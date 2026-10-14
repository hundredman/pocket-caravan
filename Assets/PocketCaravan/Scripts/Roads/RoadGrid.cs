using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 맵에 미리 정해 둔 길의 등급 (0 길 없음, 1 오솔길, 2 길, 3 큰길). 이동 속도에 쓰인다.
    /// 플레이어가 길을 만들지는 않는다 (MMORPG에서 맵이 망가지지 않게).
    /// 등급은 맵 JSON에서 온 grades 배열을 쓰고, 없으면 Road 타일맵의 타일로 판단한다.
    /// </summary>
    public class RoadGrid : MonoBehaviour
    {
        public static RoadGrid Current { get; private set; }

        [Header("맵 JSON에서 채움 (y는 아래쪽부터)")]
        public int width;
        public int height;
        public int[] grades;

        [Header("또는 타일로 판단")]
        public Tilemap roadTilemap;
        public RoadTileSet tiles;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public int GradeAt(Vector3Int c)
        {
            if (grades != null && grades.Length == width * height && width > 0)
            {
                if (c.x < 0 || c.y < 0 || c.x >= width || c.y >= height) return 0;
                return grades[c.y * width + c.x];
            }
            if (roadTilemap == null || tiles == null) return 0;
            return tiles.GradeOf(roadTilemap.GetTile(c));
        }
    }
}
