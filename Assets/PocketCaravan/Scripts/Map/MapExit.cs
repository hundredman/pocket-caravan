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
        public string edge;
        public Vector2Int size = Vector2Int.one;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        Vector3Int Origin => MapRoot.Current.WorldToCell(transform.position);

        public Rect WorldRect
        {
            get
            {
                var map = MapRoot.Current;
                var a = map.ground.CellToWorld(Origin);
                var b = map.ground.CellToWorld(Origin + new Vector3Int(size.x,size.y,0));
                return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
            }
        }

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

        /// <summary>담당 지역은 출구 칸 진입이 아니라 발밑 상자가 실제 경계에 닿을 때 이동한다.</summary>
        public bool TryGetBoundaryContact(PlayerMotor player, Vector2 input, out Vector3 contact)
        {
            contact=player!=null?player.transform.position:Vector3.zero;
            var map=MapRoot.Current;
            if(player==null||map==null||!Contains(player.Cell))return false;
            if(GoryeoTravelTransition.RegionOf(map.mapId)==null||GoryeoTravelTransition.RegionOf(targetScene)==null)return true;
            Vector2 direction;
            switch(edge){case "E":direction=Vector2.right;break;case "W":direction=Vector2.left;break;case "N":direction=Vector2.up;break;case "S":direction=Vector2.down;break;default:return false;}
            if(Vector2.Dot(input,direction)<=.01f)return false;
            Rect rect=WorldRect;Vector2 half=player.feetSize*.5f,feet=player.Feet;
            float gap=direction.x>0?rect.xMax-feet.x-half.x:direction.x<0?feet.x-half.x-rect.xMin:direction.y>0?rect.yMax-feet.y-half.y:feet.y-half.y-rect.yMin;
            float attempted=Mathf.Max(0,Vector2.Dot(player.AttemptedDelta,direction));
            const float epsilon=.0001f;
            if(gap>attempted+epsilon)return false;
            contact=player.transform.position+(Vector3)(direction*Mathf.Max(0,gap-epsilon));return true;
        }

        public bool TryUseAtBoundary(PlayerMotor player, Vector2 input)
        {
            Vector3 contact;if(!TryGetBoundaryContact(player,input,out contact))return false;
            bool ownBoundary=GoryeoTravelTransition.RegionOf(MapRoot.Current.mapId)!=null&&GoryeoTravelTransition.RegionOf(targetScene)!=null;
            if(ownBoundary)
            {
                player.Teleport(contact,player.Facing);
            }
            Use(player);return true;
        }

        public bool IsLeaving(Vector2Int facing)
        {
            switch(edge) { case "E":return facing.x>0;case "W":return facing.x<0;case "N":return facing.y>0;case "S":return facing.y<0;default:return true; }
        }

        public static MapExit At(Vector3Int cell,Vector2Int facing=default)
        {
            foreach (var e in All) if (e.Contains(cell) && (facing==Vector2Int.zero || e.IsLeaving(facing))) return e;
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
