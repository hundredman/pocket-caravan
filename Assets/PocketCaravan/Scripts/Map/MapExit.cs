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

        /// <summary>발 중심이 출구 바깥 면까지 도달한 뒤 이동한다.</summary>
        public bool TryGetBoundaryContact(PlayerMotor player, Vector2 input, out Vector3 contact)
        {
            contact=player!=null?player.transform.position:Vector3.zero;
            var map=MapRoot.Current;
            if(player==null||map==null||!Contains(player.Cell))return false;
            if(GoryeoTravelTransition.RegionOf(map.mapId)==null||GoryeoTravelTransition.RegionOf(targetScene)==null)return true;
            Vector2 direction;
            switch(edge){case "E":direction=Vector2.right;break;case "W":direction=Vector2.left;break;case "N":direction=Vector2.up;break;case "S":direction=Vector2.down;break;default:return false;}
            if(Vector2.Dot(input,direction)<=.01f)return false;
            Rect rect=WorldRect;Vector2 feet=player.Feet;
            float gap=direction.x>0?rect.xMax-feet.x:direction.x<0?feet.x-rect.xMin:direction.y>0?rect.yMax-feet.y:feet.y-rect.yMin;
            float attempted=Mathf.Max(0,Vector2.Dot(player.AttemptedDelta,direction));
            const float epsilon=.0001f;
            if(gap>attempted+epsilon)return false;
            contact=player.transform.position+(Vector3)(direction*Mathf.Max(0,gap-epsilon));return true;
        }

        /// <summary>출구 칸 안에서 발 중심은 유지하고, 경계 밖으로 걸친 발 상자만 안쪽에 투영한다.
        /// 옆 벽·다른 충돌은 그대로 검사한다. 다른 문명은 기존 충돌을 유지한다.</summary>
        public bool TryProjectFootprint(Vector2 center, ref Vector2 a, ref Vector2 b, ref Vector2 c, ref Vector2 d)
        {
            if(MapRoot.Current==null||GoryeoTravelTransition.RegionOf(MapRoot.Current.mapId)==null||GoryeoTravelTransition.RegionOf(targetScene)==null)return false;
            Rect rect=WorldRect;if(!rect.Contains(center))return false;
            a=ProjectSample(a,rect);b=ProjectSample(b,rect);c=ProjectSample(c,rect);d=ProjectSample(d,rect);return true;
        }

        Vector2 ProjectSample(Vector2 sample, Rect rect)
        {
            const float inset=.0001f;
            switch(edge){case "E":sample.x=Mathf.Min(sample.x,rect.xMax-inset);break;case "W":sample.x=Mathf.Max(sample.x,rect.xMin+inset);break;case "N":sample.y=Mathf.Min(sample.y,rect.yMax-inset);break;case "S":sample.y=Mathf.Max(sample.y,rect.yMin+inset);break;}
            return sample;
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
