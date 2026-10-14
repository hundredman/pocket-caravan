using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 맵 가장자리의 출구 칸들. 오브젝트 위치가 왼쪽 아래 칸, size만큼 덮는다.
    /// 나갈 때의 위치(출구 안에서 몇 칸째인지)를 그대로 넘겨, 다음 맵의 같은 폭 도착 자리에서 이어서 나온다.
    /// </summary>
    public class MapExit : MonoBehaviour
    {
        public static readonly List<MapExit> All = new List<MapExit>();

        [Tooltip("Build Settings에 들어 있는 씬 이름")] public string targetScene;
        public string targetSpawnId;
        public Vector2Int size = Vector2Int.one;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        Vector3Int Origin => MapRoot.Current.WorldToCell(transform.position);

        public bool Contains(Vector3Int cell)
        {
            if (MapRoot.Current == null) return false;
            var o = Origin;
            return cell.x >= o.x && cell.x < o.x + size.x && cell.y >= o.y && cell.y < o.y + size.y;
        }

        public void Use(PlayerMotor player)
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.Log($"[MapExit] {name}: 아직 연결된 맵이 없습니다.");
                return;
            }
            // 출구 첫 칸 중심에서 몇 칸 떨어져 있는지 (출구가 긴 방향으로)
            Vector3 first = MapRoot.Current.CellCenter(Origin);
            float offset = size.x > 1 ? player.transform.position.x - first.x : player.transform.position.y - first.y;
            GameManager.I.TravelTo(targetScene, targetSpawnId, offset, player.Facing);
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
