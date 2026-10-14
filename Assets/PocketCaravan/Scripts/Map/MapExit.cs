using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 밟으면 다른 맵으로 넘어가는 칸들. 오브젝트 위치가 왼쪽 아래 칸, size만큼 덮는다.
    /// 도착 맵에는 targetSpawnId와 같은 이름의 SpawnPoint가 있어야 한다.
    /// </summary>
    public class MapExit : MonoBehaviour
    {
        public static readonly List<MapExit> All = new List<MapExit>();

        [Tooltip("Build Settings에 들어 있는 씬 이름")] public string targetScene;
        public string targetSpawnId;
        public Vector2Int size = Vector2Int.one;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool Contains(Vector3Int cell)
        {
            if (MapRoot.Current == null) return false;
            var o = MapRoot.Current.WorldToCell(transform.position);
            return cell.x >= o.x && cell.x < o.x + size.x && cell.y >= o.y && cell.y < o.y + size.y;
        }

        public void Use()
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.Log($"[MapExit] {name}: 아직 연결된 맵이 없습니다.");
                return;
            }
            GameManager.I.TravelTo(targetScene, targetSpawnId);
        }

        public static MapExit At(Vector3Int cell)
        {
            foreach (var e in All) if (e.Contains(cell)) return e;
            return null;
        }

        void OnDrawGizmos()
        {
            var grid = GetComponentInParent<Grid>();
            Vector3 cs = grid != null ? grid.cellSize : Vector3.one;
            var center = transform.position + new Vector3(size.x * cs.x, size.y * cs.y, 0) * 0.5f;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Gizmos.DrawCube(center, new Vector3(size.x * cs.x, size.y * cs.y, 0.1f));
        }
    }
}
