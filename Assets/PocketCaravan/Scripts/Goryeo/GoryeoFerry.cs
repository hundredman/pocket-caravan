using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    /// <summary>ATM-B2 review opt-in. No production data/installer. Existing NPCs, transition, player state and saves remain shared.</summary>
    public static class GoryeoFerry
    {
        [Serializable]public class Port{public string mapId,name,townId;public int width,height,npcX,npcY,arrivalX,arrivalY;}
        [Serializable]public class Config{public string status;public int fare;public float hours;public Port[] ports;}
        sealed class Voyage{public string from,to;public int fare;public float hours;}
        static Config config;static Voyage pending;static TalkNpc telling;
        public static int CompletedCount{get;private set;}public static string LastError{get;private set;}
        public static bool Pending=>pending!=null;public static bool ReviewEnabled=>config!=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;pending=null;telling=null;CompletedCount=0;LastError="";SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;}
        static bool Owned(string id)=>id=="Naru"||id=="transit_ferry"||id=="transit_southport";
        public static Port Find(string id)=>config?.ports==null?null:Array.Find(config.ports,p=>p.mapId==id);
        public static string SpawnId(string id)=>"goryeo_ferry_arrival_"+id;
        static bool SafeReview=>config!=null&&GameManager.I!=null&&!GameManager.I.AutosaveEnabled;
#if UNITY_EDITOR
        public static void EnableReview(Config value)
        {
            if(GameManager.I==null||GameManager.I.AutosaveEnabled||MapRoot.Current==null||!MapRoot.Current.previewOnly)throw new InvalidOperationException("Safe preview session required");
            if(value==null||value.status!="review"||value.fare<0||value.hours<=0||float.IsNaN(value.hours)||float.IsInfinity(value.hours)||value.ports==null||value.ports.Length!=3)throw new ArgumentException("Invalid review policy");
            var ids=new HashSet<string>();foreach(var p in value.ports)if(p==null||!Owned(p.mapId)||!ids.Add(p.mapId)||p.width<=0||p.height<=0)throw new ArgumentException("Invalid review ports");
            config=value;Install(MapRoot.Current);
        }
#endif
        static bool Matches(TalkNpc npc,MapRoot map,Port port)=>npc!=null&&npc.isActiveAndEnabled&&npc.gameObject.scene==map.gameObject.scene&&map.WorldToCell(npc.transform.position)==new Vector3Int(port.npcX,port.height-port.npcY-1,0);
        public static bool TryInteract(TalkNpc npc,PlayerController player)
        {
            var map=MapRoot.Current;if(!SafeReview||map==null||!map.previewOnly||npc==telling)return false;var port=Find(map.mapId);if(port==null||!Matches(npc,map,port))return false;
            var choices=new List<DialogueUI.Choice>();foreach(var target in config.ports)if(target.mapId!=port.mapId){string id=target.mapId;choices.Add(new DialogueUI.Choice(target.name+"로",()=>{var error=TryStart(npc,player,id);if(error!=null)DialogueUI.Show(npc.DisplayName,error);}));}
            choices.Add(new DialogueUI.Choice("이야기",()=>{try{telling=npc;npc.Interact(player);}finally{telling=null;}}));choices.Add(new DialogueUI.Choice("다음에",null));
            DialogueUI.Show(npc.DisplayName,"뱃삯 "+config.fare+"냥 · "+config.hours.ToString("0.#")+"시간 걸리오. 짐과 수레도 함께 싣소.\n어느 나루로 가겠소?",choices.ToArray());return true;
        }
        public static string TryStart(TalkNpc npc,PlayerController actor,string destination)
        {
            var gm=GameManager.I;var map=MapRoot.Current;if(!SafeReview||map==null||!map.previewOnly)return "지금은 배를 탈 수 없소.";
            var from=Find(map.mapId);var to=Find(destination);
            if(from==null||to==null||from==to||!Matches(npc,map,from)||actor==null||actor!=PlayerController.Instance||Vector2.Distance(actor.transform.position,npc.transform.position)>actor.talkRange+.01f)return "사공 가까이에서 목적지를 골라 주시오.";
            if(pending!=null||gm.IsTravelling||UIState.Blocking)return "지금은 이동할 수 없소.";
            if(gm.Player.money<config.fare)return "뱃삯 "+config.fare+"냥이 필요하오.";
            if(!Application.CanStreamedLevelBeLoaded(to.mapId))return "그 나루로는 지금 출항할 수 없소.";
            pending=new Voyage{from=from.mapId,to=to.mapId,fare=config.fare,hours=config.hours};LastError="";
            try{gm.TravelTo(to.mapId,SpawnId(to.mapId),0,Vector2Int.right);}catch(Exception){pending=null;return "출항하지 못했소. 뱃삯은 받지 않았소.";}
            return null;
        }
        // Registered at SubsystemRegistration, before GameManager.Awake subscribes: spawn and payment exist before its arrival/save callback.
        static void OnLoaded(Scene scene,LoadSceneMode mode)
        {
            if(!SafeReview)return;var map=MapRoot.Current;var port=map!=null?Find(map.mapId):null;
            bool ready=port!=null&&Install(map);
            if(pending==null)return;var voyage=pending;pending=null;
            if(!ready||map.mapId!=voyage.to||GameManager.I.Player.money<voyage.fare){LastError="나루 도착을 확인하지 못해 뱃삯을 받지 않았소.";return;}
            GameManager.I.Player.money-=voyage.fare;GameManager.I.Clock.Advance(voyage.hours);CompletedCount++;
        }
        static bool Install(MapRoot map)
        {
            if(!SafeReview||map==null)return false;var port=Find(map.mapId);var road=map.GetComponent<RoadGrid>();if(port==null||road==null||road.width!=port.width||road.height!=port.height)return false;
            var cell=new Vector3Int(port.arrivalX,port.height-port.arrivalY-1,0);if(!map.IsWalkable(cell)||MapExit.At(cell)!=null)return false;
            var existing=SpawnPoint.Find(SpawnId(port.mapId));if(existing!=null&&existing.gameObject.scene!=map.gameObject.scene)return false;
            map.previewOnly=true;
            if(existing==null){var go=new GameObject("Ferry arrival review");go.transform.SetParent(map.transform,false);existing=go.AddComponent<SpawnPoint>();existing.id=SpawnId(port.mapId);}
            var player=PlayerController.Instance;existing.transform.position=map.CellCenter(cell)-(player!=null?(Vector3)player.Motor.feetOffset:Vector3.zero);return true;
        }
    }
}
