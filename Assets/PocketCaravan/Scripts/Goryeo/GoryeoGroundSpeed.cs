using UnityEngine;
namespace PocketCaravan
{
    /// <summary>A6: preserve offroad traits, but make dirt roads at least 5% faster in the owned region.</summary>
    public static class GoryeoGroundSpeed
    {
        public const float RoadAdvantage=1.05f;
        public static float WeatherFactor(GoryeoAtmosphere.Kind weather)=>weather==GoryeoAtmosphere.Kind.Rain?.97f:weather==GoryeoAtmosphere.Kind.Monsoon?.94f:1f;
        // Input includes origin/vehicle. Shared load/run/event factors are still applied once by PlayerController.
        public static float Adjust(float current,float offroad,GoryeoStride.Surface surface,GoryeoAtmosphere.Kind weather)
        {
            if(surface==GoryeoStride.Surface.Dirt)return Mathf.Max(current,offroad*RoadAdvantage)*WeatherFactor(weather);
            if(surface==GoryeoStride.Surface.Grass)return current*WeatherFactor(weather);
            return current;
        }
        public static float Apply(float current,PlayerState player,Vector3Int cell)
        {
            var map=MapRoot.Current;var gm=GameManager.I;
            if(map==null||gm==null||player==null||map.previewOnly||GoryeoTravelTransition.RegionOf(map.mapId)==null)return current;
            var stride=map.GetComponent<GoryeoStride>();var weather=map.GetComponent<GoryeoAtmosphere>();
            if(stride==null||!stride.isActiveAndEnabled||!stride.approvedForProduction||weather==null||!weather.isActiveAndEnabled||!weather.approvedForProduction)return current;
            var surface=stride.At(cell);
            if(surface!=GoryeoStride.Surface.Dirt&&surface!=GoryeoStride.Surface.Grass)return current;
            var roads=map.GetComponent<RoadGrid>();
            if(roads==null||stride.width!=roads.width||stride.height!=roads.height)return current;
            // Grass is genuinely offroad. Do not reinterpret inconsistent surface/road data as a balance change.
            if(surface==GoryeoStride.Surface.Grass&&roads.GradeAt(cell)!=0)return current;
            float origin=player.origin!=null?player.origin.speedMultiplier:1f;
            float offroad=(player.vehicle!=null?player.vehicle.SpeedOn(0):1f)*origin*(player.origin!=null?player.origin.offroadSpeedMultiplier:1f);
            var kind=GoryeoAtmosphere.WeatherFor(gm.Clock.day,map.mapId,gm.Clock.minuteOfDay/60f,weather.settings);
            return Adjust(current,offroad,surface,kind);
        }
    }
}
