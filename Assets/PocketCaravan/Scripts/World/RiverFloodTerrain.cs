using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>Paired terrain states for a managed river_flood. Does not alter saves or create sprites.</summary>
    public sealed class RiverFloodTerrain : MonoBehaviour
    {
        [Serializable] public class Cell
        {
            public Vector3Int position;
            public TileBase dryGround,wetGround,dryDeco,wetDeco;
            public bool dryBlocked,wetBlocked;
            public int dryGrade,wetGrade;
        }
        public string placementId;
        public ObstacleDefinition definition;
        public float phaseOffset;
        public Cell[] cells;
        public TileBase blocker;
        public bool Flooded {get;private set;}
        public bool Pending {get;private set;}
        public float Elapsed {get;private set;}
        public bool Ready {get;private set;}
        MapRoot map;RoadGrid roads;Tilemap deco;

        void Start(){Initialize();Advance(0);}
        public bool Initialize()
        {
            if(Ready)return true;
            map=GetComponent<MapRoot>();roads=GetComponent<RoadGrid>();
            var layer=transform.Find("Deco");deco=layer!=null?layer.GetComponent<Tilemap>():null;
            bool valid=map!=null&&map.ground!=null&&map.blocked!=null&&roads!=null&&deco!=null&&blocker!=null&&
                definition!=null&&definition.id=="river_flood"&&RoadHazardSchedule.Managed(placementId)&&cells!=null&&cells.Length>0;
            var seen=new System.Collections.Generic.HashSet<Vector3Int>();
            if(valid)foreach(var c in cells)
            {
                if(c==null||!seen.Add(c.position)||c.position.x<0||c.position.y<0||c.position.x>=roads.width||c.position.y>=roads.height||
                    c.dryGround==null||c.wetGround==null||map.ground.GetTile(c.position)!=c.dryGround||deco.GetTile(c.position)!=c.dryDeco||
                    map.blocked.HasTile(c.position)!=c.dryBlocked||roads.GradeAt(c.position)!=c.dryGrade)
                {valid=false;break;}
            }
            if(!valid){Debug.LogError("범람 지형 기준 불일치: "+placementId+" (지형/그림/통행을 변경하지 않았습니다.)",this);enabled=false;return false;}
            Ready=true;return true;
        }
        void Update()
        {
            if(map==null||MapRoot.Current!=map||UIState.Blocking)return;
            Advance(Time.deltaTime);
        }
        public void Advance(float deltaTime)
        {
            if(!enabled||!Initialize())return;
            var gm=GameManager.I;if(gm==null||gm.Clock==null)return;
            Elapsed+=Mathf.Clamp(deltaTime,0,1); // Same per-visit cycle as ObstacleField.
            bool scheduled=RoadHazardSchedule.Enabled(map.mapId,placementId,definition.id,gm.Clock.TotalHours)??false;
            bool desired=scheduled&&definition.Active(Elapsed,phaseOffset);
            Pending=false;
            if(desired&&!Flooded&&OverlapsNewWater()){Pending=true;return;}
            Apply(desired);
        }
        bool OverlapsNewWater()
        {
            var player=PlayerController.Instance;if(player==null||MapRoot.Current!=map)return false;
            var motor=player.Motor;var feet=motor.Feet;var half=motor.feetSize*.5f;
            foreach(var c in cells)if(!c.dryBlocked&&c.wetBlocked)
            {
                var center=map.CellCenter(c.position);
                if(Mathf.Abs(feet.x-center.x)<.5f+half.x&&Mathf.Abs(feet.y-center.y)<.5f+half.y)return true;
            }
            return false;
        }
        void Apply(bool wet)
        {
            if(Flooded==wet)return;
            foreach(var c in cells)
            {
                map.ground.SetTile(c.position,wet?c.wetGround:c.dryGround);
                deco.SetTile(c.position,wet?c.wetDeco:c.dryDeco);
                map.blocked.SetTile(c.position,(wet?c.wetBlocked:c.dryBlocked)?blocker:null);
                roads.grades[c.position.y*roads.width+c.position.x]=wet?c.wetGrade:c.dryGrade;
            }
            Flooded=wet;
        }
        void OnDisable()
        {
            if(Ready&&map!=null&&map.ground!=null&&map.blocked!=null&&deco!=null&&roads!=null)Apply(false);
            Pending=false;
        }
    }
}
