using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Only the approved 2x2 fire is solid. A player already inside may leave once without teleportation.</summary>
    public sealed class GoryeoFireFootprint:MonoBehaviour
    {
        GoryeoSeat owner;int x,y;PlayerMotor escape;MapRoot map;
        public void Configure(GoryeoSeat seat,int left,int bottom,PlayerMotor player)
        {owner=seat;x=left;y=bottom;map=GetComponent<MapRoot>();AllowExistingActor(player);}
        public void AllowExistingActor(PlayerMotor player)=>escape=player!=null&&Overlaps(player)?player:null;
        bool Contains(Vector3Int c)=>c.x>=x&&c.x<x+2&&c.y>=y&&c.y<y+2;
        bool Overlaps(PlayerMotor actor)
        {
            if(map==null||actor==null)return false;
            var half=actor.feetSize*.5f;var lo=map.WorldToCell(actor.Feet-half+Vector2.one*.001f);var hi=map.WorldToCell(actor.Feet+half-Vector2.one*.001f);
            return lo.x<x+2&&hi.x>=x&&lo.y<y+2&&hi.y>=y;
        }
        public bool Blocks(Vector3Int c,PlayerMotor actor)
        {
            if(owner==null||!owner.gameObject.activeInHierarchy)return false;
            if(escape!=null&&!Overlaps(escape))escape=null;
            return Contains(c)&&(actor==null||actor!=escape);
        }
    }
}
