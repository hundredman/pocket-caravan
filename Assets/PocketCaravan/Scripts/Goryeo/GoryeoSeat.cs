using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>ATM-B3 opt-in review only. The motor remains on safe ground; only the body sits on the furniture.</summary>
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
            return pc!=null && pc.Motor!=null && pc.anim!=null && site!=null &&
                map!=null && map.previewOnly && map.mapId==site.mapId && gameObject.scene==map.gameObject.scene &&
                GameManager.I!=null && !GameManager.I.AutosaveEnabled && !GameManager.I.IsTravelling &&
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
            if(actor==null)return;
            if(!Safe(actor)){Stand();return;}
            actor.anim.transform.localPosition=bodyRest+Vector3.up*site.lift;
            actor.anim.GetComponent<SpriteRenderer>().sprite=activePose;
        }
        protected override void OnDisable(){Stand();base.OnDisable();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()=>current=null;
#if UNITY_EDITOR
        public static int InstallReview(Config config,Sprite pose)
            =>InstallReview(config,new[]{new Art{originId="goryeo",sprite=pose}});
        public static int InstallReview(Config config,Art[] drawings)
        {
            var map=MapRoot.Current;
            if(map==null||!map.previewOnly||GameManager.I==null||GameManager.I.AutosaveEnabled||
               config==null||config.status!="review"||config.originId!="goryeo"||drawings==null||drawings.Length==0)
                throw new InvalidOperationException("Explicit preview with saves OFF required");
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var drawing in drawings)
                if(drawing==null||drawing.sprite==null||!ids.Add(drawing.originId)||
                   (drawing.originId!="goryeo"&&drawing.originId!="venice"&&drawing.originId!="viking"&&drawing.originId!="arabia"))
                    throw new ArgumentException("Invalid or duplicate native pose");
            foreach(var old in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))DestroyImmediate(old.gameObject);
            int count=0;
            foreach(var s in config.seats)
            {
                if(s.mapId!=map.mapId)continue;
                var cell=new Vector3Int(s.x,RoadGrid.Current.height-s.y-1,0);
                if(!map.IsWalkable(cell)||MapExit.At(cell)!=null)continue;
                var go=new GameObject("Review seat "+s.id);go.transform.SetParent(map.transform,false);
                go.transform.position=map.CellCenter(cell)-(Vector3)PlayerController.Instance.Motor.feetOffset;
                var seat=go.AddComponent<GoryeoSeat>();seat.blocksMovement=false;seat.site=s;seat.art=(Art[])drawings.Clone();
                foreach(var drawing in drawings)if(drawing.originId=="goryeo")seat.seatedSprite=drawing.sprite;
                count++;
                if(s.fire)
                {
                    for(int y=0;y<2;y++)for(int x=0;x<2;x++)
                    {
                        var piece=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PocketCaravan/Art/NinjaAdventure/Tiles/camp_"+(12+x)+"_"+(5+y)+".png");
                        if(piece==null)throw new InvalidOperationException("Missing original campfire module");
                        var part=new GameObject("Whole campfire module");part.transform.SetParent(go.transform,false);
                        part.transform.position=map.CellCenter(new Vector3Int(s.propX+x,RoadGrid.Current.height-s.propY-y-1,0));
                        var renderer=part.AddComponent<SpriteRenderer>();renderer.sprite=piece;renderer.sortingOrder=6;
                    }
                    var center=map.CellCenter(new Vector3Int(s.propX,RoadGrid.Current.height-s.propY-1,0))+new Vector3(.5f,-.5f,0);
                    go.AddComponent<GoryeoCampfire>().Configure(map,center);
                }
            }
            return count;
        }
#endif
    }
}
