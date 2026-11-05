using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Approved ATM-B5: weather gate for the one approved forest log. Existing window/roll/removal remain unchanged.</summary>
    public static class GoryeoStormLogs
    {
        public const string MapId="transit_central_approach",PlacementId="e3_central_approach_log";
        static bool loaded;static GoryeoAtmosphere.Settings settings;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){loaded=false;settings=null;}
        public static bool Target(string mapId,string placementId)=>mapId==MapId&&placementId==PlacementId;
        public static bool StormAt(float totalHours,GoryeoAtmosphere.Settings options=null)
        {
            if(float.IsNaN(totalHours)||float.IsInfinity(totalHours)||totalHours<0)return false;
            return GoryeoAtmosphere.DayWeather(Mathf.FloorToInt(totalHours/24)+1,options)==GoryeoAtmosphere.Kind.Monsoon;
        }
        public static bool Allows(string mapId,string placementId,float totalHours)
        {
            if(!Target(mapId,placementId))return true;
            if(!loaded)
            {
                loaded=true;var asset=Resources.Load<TextAsset>("Goryeo/Atmosphere");
                var config=asset!=null?JsonUtility.FromJson<GoryeoAtmosphereDirector.Config>(asset.text):null;
                if(config!=null&&config.status=="approved")settings=config.settings;
            }
            return settings!=null&&StormAt(totalHours,settings);
        }
        // Final arrival-time check before ObstacleField.Start builds collision/visual state.
        // Do this only once on native-log initialization; never respawn a resolved log mid-visit.
        public static void Initialize(ObstacleField field)
        {
            var map=field!=null?field.GetComponent<MapRoot>():null;var gm=GameManager.I;
            if(map==null||map.previewOnly||gm==null||gm.Clock==null)return;
            foreach(var p in field.placements)
                if(p!=null&&p.definitionId=="fallen_log"&&Target(map.mapId,p.id))
                    p.enabled=RoadHazardSchedule.Enabled(map.mapId,p.id,p.definitionId,gm.Clock.TotalHours)==true;
        }
    }
}
