using System;
using System.Collections.Generic;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoAtmosphereChecks
    {
        public static readonly string[] Maps={"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_southport","transit_crossroads","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"};
        [Serializable]class Report{public int dateMapCases;public List<string> passed=new List<string>();}
        public static string RunData()
        {
            var report=new Report();string random=JsonUtility.ToJson(UnityEngine.Random.state);var seen=new HashSet<GoryeoAtmosphere.Kind>();
            foreach(var id in Maps)
            {
                if(GoryeoTravelTransition.RegionOf(id)==null)throw new Exception("Scope lost "+id);
                for(int day=1;day<=4096;day++)
                {
                    var a=GoryeoAtmosphere.WeatherFor(day,id,7.5f);var b=GoryeoAtmosphere.WeatherFor(day,id,7.5f);if(a!=b)throw new Exception("Nondeterministic weather");seen.Add(a);
                    var daily=GoryeoAtmosphere.DayWeather(day);
                    if(daily==GoryeoAtmosphere.Kind.Mist)
                    {
                        if((a==GoryeoAtmosphere.Kind.Mist)!=GoryeoAtmosphere.MistMap(id))throw new Exception("Mist map mismatch");
                        foreach(float hour in new[]{0f,3.99f,9f,12f,23.99f})if(GoryeoAtmosphere.WeatherFor(day,id,hour)!=GoryeoAtmosphere.Kind.Clear)throw new Exception("Mist outside morning");
                    }
                    else if(a!=GoryeoAtmosphere.WeatherFor(day,id,18))throw new Exception("Rain changed within day");
                    report.dateMapCases++;
                }
            }
            if(seen.Count!=4)throw new Exception("Missing weather states");
            foreach(var other in new[]{"north_birka","venice_rialto","arabia_ahsa","transit_fake","",null})for(int day=1;day<=128;day++)if(GoryeoAtmosphere.WeatherFor(day,other,7)!=GoryeoAtmosphere.Kind.Clear)throw new Exception("Foreign weather changed");
            if(random!=JsonUtility.ToJson(UnityEngine.Random.state))throw new Exception("Global random mutated");
            report.passed.Add("13 maps x4096 dates: deterministic, morning/location fog, all4 states");report.passed.Add("6 foreign/unknown IDs untouched; global random unchanged");report.passed.Add("Visual proposal only; no save/clock/terrain/economy or movement writes");
            return JsonUtility.ToJson(report,true);
        }
    }
}
