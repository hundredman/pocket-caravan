using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>Approved seating or explicit editor review. Physical feet remain on safe ground.</summary>
    [DefaultExecutionOrder(1200)]
    public sealed class GoryeoSeat : Interactable
    {
        [Serializable] public class Site { public string id,mapId,name,source; public int x,y,propX,propY; public float lift; public bool fire; }
        [Serializable] public class PoseData { public string originId,file; public float ppu,pivotY; }
        [Serializable] public class Art { public string originId; public Sprite sprite; }
        [Serializable] public class Config { public string status,originId,sprite; public Site[] seats; public PoseData[] poses; }
        public Site site;
        public Sprite seatedSprite;
        public Art[] art;
        public bool approvedForProduction;
        static GoryeoSeat current;
        PlayerController actor;
        OriginData seatedOrigin;
        Sprite activePose;
        Vector3 standing,bodyRest;
        Vector2Int facing;
        public static bool Sitting(PlayerController pc) => current != null && current.actor == pc;
        public override string DisplayName => site == null ? "" : site.name;
        public override string ActionVerb => current==this ? "일어나기" : "앉기";
        public override Kind Category => Kind.Board;
        public Sprite PoseFor(OriginData origin)
        {
            if(origin==null)return null;
            if(art!=null)foreach(var drawing in art)if(drawing!=null&&drawing.originId==origin.id)return drawing.sprite;
            return art==null&&origin.id=="goryeo"?seatedSprite:null;
        }

        bool Safe(PlayerController pc)
        {
            var map=MapRoot.Current;
            return pc!=null && pc.isActiveAndEnabled && pc.Motor!=null && pc.anim!=null && site!=null &&
                map!=null && map.mapId==site.mapId && gameObject.scene==map.gameObject.scene &&
                GameManager.I!=null && (map.previewOnly?!GameManager.I.AutosaveEnabled:approvedForProduction&&map.mapId=="transit_central_approach") && !GameManager.I.IsTravelling &&
                PoseFor(GameManager.I.Player.origin)!=null && (actor==null||GameManager.I.Player.origin==seatedOrigin) &&
                !UIState.Blocking && map.IsWalkable(Cell,pc.Motor) && MapExit.At(Cell)==null &&
                Vector2.Distance(pc.transform.position,transform.position)<=pc.talkRange;
        }
        public override void Interact(PlayerController pc)
        {
            if(current!=null || !Safe(pc))return;
            actor=pc;current=this;standing=pc.transform.position;facing=pc.Motor.Facing;
            seatedOrigin=GameManager.I.Player.origin;activePose=PoseFor(seatedOrigin);
            bodyRest=pc.anim.transform.localPosition;
            pc.Motor.Teleport(transform.position,Vector2Int.down);
            pc.anim.facing=Vector2Int.down;pc.anim.walking=false;
            Notify.Show("이동 또는 Space로 일어나기",2f);
        }
        // Called before the ordinary motor tick. No global UI lock or player disable is acquired.
        public static bool BeforeMove(PlayerController pc,bool locked)
            => Handle(pc,InputReader.MoveVector(),InputReader.InteractPressed(),locked);
        public static bool Handle(PlayerController pc,Vector2 move,bool interact,bool locked)
        {
            if(!Sitting(pc))return false;
            if(locked || move.sqrMagnitude>.01f || interact || !current.Safe(pc))current.Stand();
            else SeatRest.Tick(pc,Time.deltaTime);   // 11/08 공용 앉기 기본 효과 (다른 지역 벤치·그늘·모닥불과 같은 3배)
            return true;
        }
        public void Stand()
        {
            if(actor!=null)
            {
                if(actor.anim!=null)actor.anim.transform.localPosition=bodyRest;
                var map=MapRoot.Current;
                if(map!=null && map.mapId==site.mapId && map.gameObject.scene==gameObject.scene &&
                   map.IsWalkable(map.WorldToCell(standing+(Vector3)actor.Motor.feetOffset),actor.Motor))
                    actor.Motor.Teleport(standing,facing);
                if(actor.anim!=null)
                {
                    actor.anim.facing=actor.Motor.Facing;actor.anim.walking=false;
                    int d=actor.Motor.Facing.y<0?0:actor.Motor.Facing.y>0?1:actor.Motor.Facing.x<0?2:3;
                    if(actor.anim.frames!=null&&actor.anim.frames.Length>d*4)
                        actor.anim.GetComponent<SpriteRenderer>().sprite=actor.anim.frames[d*4];
                }
            }
            actor=null;activePose=null;seatedOrigin=null;if(current==this)current=null;
        }
        void LateUpdate()
        {
            if(actor==null){if(current==this)Stand();return;}
            if(!Safe(actor)){Stand();return;}
            actor.anim.transform.localPosition=bodyRest+Vector3.up*site.lift;
            actor.anim.GetComponent<SpriteRenderer>().sprite=activePose;
        }
        protected override void OnDisable(){Stand();base.OnDisable();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()=>current=null;
        static bool ValidArt(Art[] drawings)
        {
            if(drawings==null||drawings.Length==0)return false;
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var drawing in drawings)
                if(drawing==null||drawing.sprite==null||!ids.Add(drawing.originId)||
                   (drawing.originId!="goryeo"&&drawing.originId!="venice"&&drawing.originId!="viking"&&drawing.originId!="arabia"))return false;
            return true;
        }
        public static int InstallApproved(Config config,Art[] drawings,Sprite[] fire)
        {
            var map=MapRoot.Current;var pc=PlayerController.Instance;var road=map!=null?map.GetComponent<RoadGrid>():null;
            if(map==null||map.previewOnly||map.mapId!="transit_central_approach"||road==null||!((road.width==71&&road.height==87)||GoryeoBoundaryLayout.Matches(map.mapId,road.width,road.height))||
               pc==null||pc.gameObject.scene!=map.gameObject.scene||config==null||config.status!="approved"||config.seats==null||config.seats.Length!=3||
               !ValidArt(drawings)||drawings.Length!=4||fire==null||fire.Length!=4)return 0;
            if(map.GetComponentsInChildren<GoryeoSeat>(true).Length>0)return 0;
            foreach(var piece in fire)if(piece==null)return 0;
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var s in config.seats)
            {
                if(s==null||s.mapId!=map.mapId||!ids.Add(s.id)||s.x<0||s.x>=road.width||s.y<0||s.y>=road.height||
                   !map.IsWalkable(new Vector3Int(s.x,road.height-s.y-1,0))||MapExit.At(new Vector3Int(s.x,road.height-s.y-1,0))!=null)return 0;
                if(s.fire)for(int y=0;y<2;y++)for(int x=0;x<2;x++)
                {var c=new Vector3Int(s.propX+x,road.height-s.propY-y-1,0);if(!map.IsWalkable(c)||MapExit.At(c)!=null)return 0;}
            }
            foreach(var s in config.seats)Create(map,s,drawings,fire,true);
            return config.seats.Length;
        }
        static GoryeoSeat Create(MapRoot map,Site site,Art[] drawings,Sprite[] fire,bool approved)
        {
            int h=map.GetComponent<RoadGrid>().height;
            var go=new GameObject("Goryeo seat "+site.id);go.transform.SetParent(map.transform,false);
            go.transform.position=map.CellCenter(new Vector3Int(site.x,h-site.y-1,0))-(Vector3)PlayerController.Instance.Motor.feetOffset;
            var seat=go.AddComponent<GoryeoSeat>();seat.blocksMovement=false;seat.site=site;seat.art=(Art[])drawings.Clone();seat.approvedForProduction=approved;
            foreach(var drawing in drawings)if(drawing.originId=="goryeo")seat.seatedSprite=drawing.sprite;
            if(site.fire)
            {
                for(int y=0;y<2;y++)for(int x=0;x<2;x++)
                {
                    var part=new GameObject("Whole campfire module");part.transform.SetParent(go.transform,false);
                    part.transform.position=map.CellCenter(new Vector3Int(site.propX+x,h-site.propY-y-1,0));
                    var sr=part.AddComponent<SpriteRenderer>();sr.sprite=fire[y*2+x];sr.sortingOrder=6;
                }
                var center=map.CellCenter(new Vector3Int(site.propX,h-site.propY-1,0))+new Vector3(.5f,-.5f,0);
                go.AddComponent<GoryeoCampfire>().Configure(map,center,approved);
                var field=map.GetComponent<GoryeoFireFootprint>();if(field==null)field=map.gameObject.AddComponent<GoryeoFireFootprint>();
                field.Configure(seat,site.propX,h-site.propY-2,PlayerController.Instance.Motor);
            }
            return seat;
        }
#if UNITY_EDITOR
        public static int InstallReview(Config config,Sprite pose)
            =>InstallReview(config,new[]{new Art{originId="goryeo",sprite=pose}});
        public static int InstallReview(Config config,Art[] drawings)
        {
            var map=MapRoot.Current;
            if(map==null||!map.previewOnly||GameManager.I==null||GameManager.I.AutosaveEnabled||
               config==null||config.status!="review"||config.originId!="goryeo"||drawings==null||drawings.Length==0)
                throw new InvalidOperationException("Explicit preview with saves OFF required");
            if(!ValidArt(drawings))throw new ArgumentException("Invalid or duplicate native pose");
            var fire=new Sprite[4];for(int y=0;y<2;y++)for(int x=0;x<2;x++)
            {fire[y*2+x]=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PocketCaravan/Art/NinjaAdventure/Tiles/camp_"+(12+x)+"_"+(5+y)+".png");if(fire[y*2+x]==null)throw new InvalidOperationException("Missing fire module");}
            foreach(var old in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))DestroyImmediate(old.gameObject);
            int count=0;
            foreach(var s in config.seats)
            {
                if(s.mapId!=map.mapId)continue;
                var cell=new Vector3Int(s.x,RoadGrid.Current.height-s.y-1,0);
                if(!map.IsWalkable(cell)||MapExit.At(cell)!=null)continue;
                Create(map,s,drawings,fire,false);count++;
            }
            return count;
        }
#endif
    }
}
