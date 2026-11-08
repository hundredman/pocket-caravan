using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    /// <summary>ATM-B2 approved ferries, with separate explicit editor review mode. Existing NPCs and shared travel/save flow are retained.</summary>
    public static class GoryeoFerry
    {
        [Serializable]public class Port{public string mapId,name,townId;public int width,height,npcX,npcY,arrivalX,arrivalY;}
        [Serializable]public class Config{public string status;public int fare;public float hours;public Port[] ports;}
        sealed class Voyage{public string from,to;public int fare;public float hours;public GameManager owner;public string slot;}
        static Config config;static Voyage pending;static TalkNpc telling;static bool review,attempted;
        public static int CompletedCount{get;private set;}public static string LastError{get;private set;}
        public static bool Pending=>pending!=null;public static bool ReviewEnabled=>review;public static bool Approved=>!review&&config!=null&&config.status=="approved";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;pending=null;telling=null;review=false;attempted=false;CompletedCount=0;LastError="";SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;}
        static bool Owned(string id)=>id=="Naru"||id=="transit_ferry"||id=="transit_southport";
        /// <summary>2026-11-08 은샘: 예성 나루가 중심. 버들나루·남동 포구는 예성 나루로만, 예성 나루에서는 둘 다.</summary>
        public const string Hub="Naru";
        public static bool Route(string from,string to)=>from!=to&&(from==Hub||to==Hub);
        public static Port Find(string id){EnsureConfig();return config?.ports==null?null:Array.Find(config.ports,p=>p.mapId==id);}
        public static string SpawnId(string id)=>"goryeo_ferry_arrival_"+id;
        static void EnsureConfig()
        {
            if(config!=null||attempted)return;attempted=true;var asset=Resources.Load<TextAsset>("Goryeo/Ferry/Config");if(asset==null)return;
            var value=JsonUtility.FromJson<Config>(asset.text);if(Valid(value,"approved"))config=value;
        }
        static bool Valid(Config value,string status)
        {
            if(value==null||value.status!=status||value.fare<0||value.hours<=0||float.IsNaN(value.hours)||float.IsInfinity(value.hours)||value.ports==null||value.ports.Length!=3)return false;
            var ids=new HashSet<string>();foreach(var p in value.ports)if(p==null||!Owned(p.mapId)||!ids.Add(p.mapId)||p.width<=0||p.height<=0||p.npcX<0||p.npcX>=p.width||p.npcY<0||p.npcY>=p.height||p.arrivalX<0||p.arrivalX>=p.width||p.arrivalY<0||p.arrivalY>=p.height)return false;
            return true;
        }
        static bool Active{get{EnsureConfig();return config!=null&&GameManager.I!=null&&(!review||!GameManager.I.AutosaveEnabled);}}
        static bool Allowed(MapRoot map)=>map!=null&&(review?map.previewOnly:!map.previewOnly);
#if UNITY_EDITOR
        public static void EnableReview(Config value)
        {
            if(GameManager.I==null||GameManager.I.AutosaveEnabled||MapRoot.Current==null||!MapRoot.Current.previewOnly)throw new InvalidOperationException("Safe preview session required");
            if(!Valid(value,"review"))throw new ArgumentException("Invalid review policy");
            config=value;review=true;Install(MapRoot.Current);
        }
#endif
        static bool Matches(TalkNpc npc,MapRoot map,Port port)=>npc!=null&&npc.isActiveAndEnabled&&npc.gameObject.scene==map.gameObject.scene&&map.WorldToCell(npc.transform.position)==new Vector3Int(port.npcX,port.height-port.npcY-1,0);
        public static bool TryInteract(TalkNpc npc,PlayerController player)
        {
            var map=MapRoot.Current;if(!Active||!Allowed(map)||npc==telling)return false;var port=Find(map.mapId);if(port==null||!Matches(npc,map,port))return false;
            // 이야기하기가 첫 선택지(Space). 그다음 갈 수 있는 나루, 마지막은 다음에(Esc)
            var choices=new List<DialogueUI.Choice>{new DialogueUI.Choice("이야기하기",()=>{try{telling=npc;npc.Interact(player);}finally{telling=null;}})};
            foreach(var target in config.ports)if(Route(port.mapId,target.mapId)){string id=target.mapId;choices.Add(new DialogueUI.Choice(target.name+"로",()=>{var error=TryStart(npc,player,id);if(error!=null)DialogueUI.Show(npc.DisplayName,error);}));}
            choices.Add(new DialogueUI.Choice("다음에",null));
            DialogueUI.Show(npc.DisplayName,"뱃삯 "+config.fare+"냥 · "+config.hours.ToString("0.#")+"시간 걸리오. 짐과 수레도 함께 싣소.\n물길 이야기를 들려줄까, 아니면 배를 타겠소?",choices.ToArray());return true;
        }
        public static string TryStart(TalkNpc npc,PlayerController actor,string destination)
        {
            var gm=GameManager.I;var map=MapRoot.Current;if(!Active||!Allowed(map))return "지금은 배를 탈 수 없소.";
            var from=Find(map.mapId);var to=Find(destination);
            if(from!=null&&to!=null&&!Route(from.mapId,to.mapId))return "그 나루로 가는 배는 없소. 예성 나루를 거쳐 가시오.";
            if(from==null||to==null||from==to||!Matches(npc,map,from)||actor==null||actor!=PlayerController.Instance||Vector2.Distance(actor.transform.position,npc.transform.position)>actor.talkRange+.01f)return "사공 가까이에서 목적지를 골라 주시오.";
            if(pending!=null||gm.IsTravelling||UIState.Blocking)return "지금은 이동할 수 없소.";
            if(gm.Player.money<config.fare)return "뱃삯 "+config.fare+"냥이 필요하오.";
            var info=WorldIndex.Get()?.Map(to.mapId);if(info==null||info.height!=to.height||!Application.CanStreamedLevelBeLoaded(to.mapId))return "그 나루로는 지금 출항할 수 없소.";
            if(!review&&gm.AutosaveEnabled&&!gm.SaveNow())return "진행 상황을 저장하지 못해 출항하지 않았소. 잠시 뒤 다시 부탁하오.";
            pending=new Voyage{from=from.mapId,to=to.mapId,fare=config.fare,hours=config.hours,owner=gm,slot=SaveSystem.FileName};LastError="";
            try{gm.TravelTo(to.mapId,SpawnId(to.mapId),0,Vector2Int.right);}catch(Exception){pending=null;return "출항하지 못했소. 뱃삯은 받지 않았소.";}
            return null;
        }
        // Registered at SubsystemRegistration, before GameManager.Awake subscribes: spawn and payment exist before its arrival/save callback.
        static void OnLoaded(Scene scene,LoadSceneMode mode)
        {
            if(!Active)return;var map=MapRoot.Current;var port=map!=null?Find(map.mapId):null;
            bool ready=port!=null&&Install(map);
            if(pending==null)return;var voyage=pending;pending=null;
            if(voyage.owner!=GameManager.I||voyage.slot!=SaveSystem.FileName||!ready||map.mapId!=voyage.to||GameManager.I.Player.money<voyage.fare){LastError="나루 도착을 확인하지 못해 뱃삯을 받지 않았소.";return;}
            GameManager.I.Player.money-=voyage.fare;GameManager.I.Clock.Advance(voyage.hours);CompletedCount++;
        }
        static bool Install(MapRoot map)
        {
            if(!Active||map==null||(!review&&map.previewOnly))return false;var port=Find(map.mapId);var road=map.GetComponent<RoadGrid>();if(port==null||road==null||road.width!=port.width||road.height!=port.height)return false;
            var cell=new Vector3Int(port.arrivalX,port.height-port.arrivalY-1,0);
            if(!map.IsWalkable(cell)||MapExit.At(cell)!=null)
            {
                bool found=false;var center=new Vector3Int(port.npcX,port.height-port.npcY-1,0);
                foreach(var direction in new[]{Vector3Int.down,Vector3Int.up,Vector3Int.right,Vector3Int.left,new Vector3Int(-1,-1,0),new Vector3Int(-1,1,0),new Vector3Int(1,-1,0),new Vector3Int(1,1,0)})
                {var candidate=center+direction;if(map.IsWalkable(candidate)&&MapExit.At(candidate)==null){cell=candidate;found=true;break;}}
                if(!found)return false;
            }
            var existing=SpawnPoint.Find(SpawnId(port.mapId));if(existing!=null&&existing.gameObject.scene!=map.gameObject.scene)return false;
            if(review)map.previewOnly=true;
            if(existing==null){var go=new GameObject(review?"Ferry arrival review":"Ferry arrival");go.transform.SetParent(map.transform,false);existing=go.AddComponent<SpawnPoint>();existing.id=SpawnId(port.mapId);}
            var player=PlayerController.Instance;existing.transform.position=map.CellCenter(cell)-(player!=null?(Vector3)player.Motor.feetOffset:Vector3.zero);return true;
        }
    }
}
