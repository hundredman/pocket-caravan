using System;
using System.Collections.Generic;
using UnityEngine;
namespace PocketCaravan
{
    public static class GoryeoThunderDirector
    {
        [Serializable]public class Config{public string status,mapId,placementId;public float windowHours;public bool oncePerWindowPerSession;}
        static readonly HashSet<int> played=new HashSet<int>();static Config config;static AudioClip clip;
        public static int PlayedCount=>played.Count;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){played.Clear();config=null;clip=null;}
        public static void Apply(MapRoot map)
        {
            var gm=GameManager.I;
            if(map==null||map.previewOnly||map.mapId!=GoryeoStormLogs.MapId||gm==null)return;
            var field=map.GetComponent<ObstacleField>();if(field==null)return;
            if(!field.placements.Exists(p=>p!=null&&p.id==GoryeoStormLogs.PlacementId&&p.definitionId=="fallen_log"&&p.enabled))return;
            if(config==null){var asset=Resources.Load<TextAsset>("Goryeo/Thunder/Config");if(asset==null)return;config=JsonUtility.FromJson<Config>(asset.text);}
            if(config==null||config.status!="approved"||config.mapId!=map.mapId||config.placementId!=GoryeoStormLogs.PlacementId||config.windowHours!=RoadHazardSchedule.LogWindowHours||!config.oncePerWindowPerSession)return;
            if(clip==null)clip=Resources.Load<AudioClip>("Goryeo/Thunder/distant-thunder");if(clip==null)return;
            var existing=map.GetComponent<GoryeoThunderTrigger>();if(existing!=null)return;
            var effect=map.GetComponent<GoryeoThunder>()??map.gameObject.AddComponent<GoryeoThunder>();effect.clip=clip;effect.approvedForProduction=true;
            var trigger=map.gameObject.AddComponent<GoryeoThunderTrigger>();trigger.effect=effect;trigger.entryWindow=Mathf.FloorToInt(gm.Clock.TotalHours/RoadHazardSchedule.LogWindowHours);
        }
        public static bool TryPlay(GoryeoThunderTrigger trigger)
        {
            if(trigger==null||!trigger.isActiveAndEnabled||trigger.effect==null||!trigger.effect.isActiveAndEnabled||!trigger.effect.approvedForProduction||played.Contains(trigger.entryWindow))return false;
            var map=trigger.GetComponent<MapRoot>();var gm=GameManager.I;
            if(map==null||map!=MapRoot.Current||map.previewOnly||map.mapId!=GoryeoStormLogs.MapId||gm==null||gm.IsTravelling||UIState.Blocking||Mathf.FloorToInt(gm.Clock.TotalHours/RoadHazardSchedule.LogWindowHours)!=trigger.entryWindow)return false;
            var field=map.GetComponent<ObstacleField>();if(field==null||!field.isActiveAndEnabled)return false;
            bool active=false;foreach(var state in field.States)if(state.placement.id==GoryeoStormLogs.PlacementId&&state.definition.id=="fallen_log"&&state.active&&!state.resolved&&state.nativeLog!=null){active=true;break;}
            if(!active||!trigger.effect.PlayOnce())return false;
            played.Add(trigger.entryWindow);return true;
        }
    }
}
