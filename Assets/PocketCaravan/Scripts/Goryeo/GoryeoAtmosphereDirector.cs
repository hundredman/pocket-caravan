using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    /// <summary>Install approved A1 visuals only on the 13 owned production maps.</summary>
    public static class GoryeoAtmosphereDirector
    {
        [Serializable]public class Config{public string status;public GoryeoAtmosphere.Settings settings;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;Apply();
            // 공용 날씨 사건(LostTraveler, 기우 11/05): "고려는 날씨를 넣으면 Weather[Goryeo]에 한 줄 등록". 비·장맛비·안개를 궂은 날씨로 본다.
            LostTraveler.Weather[Reactions.Region.Goryeo]=BadWeather;
        }
        static bool BadWeather()
        {
            var root=MapRoot.Current;var gm=GameManager.I;
            if(root==null||gm==null||GoryeoTravelTransition.RegionOf(root.mapId)==null)return false;
            var weather=root.GetComponent<GoryeoAtmosphere>();
            if(weather==null||!weather.approvedForProduction)return false;
            return GoryeoAtmosphere.WeatherFor(gm.Clock.day,root.mapId,gm.Clock.minuteOfDay/60f,weather.settings)!=GoryeoAtmosphere.Kind.Clear;
        }
        static void OnLoaded(Scene scene,LoadSceneMode mode)=>Apply();
        public static void Apply()
        {
            var root=MapRoot.Current;
            if(root==null||root.previewOnly||GoryeoTravelTransition.RegionOf(root.mapId)==null)return;
            var asset=Resources.Load<TextAsset>("Goryeo/Atmosphere");if(asset==null)return;
            var config=JsonUtility.FromJson<Config>(asset.text);if(config==null||config.status!="approved"||config.settings==null)return;
            var weather=root.GetComponent<GoryeoAtmosphere>()??root.gameObject.AddComponent<GoryeoAtmosphere>();
            weather.settings=config.settings;weather.approvedForProduction=true;
        }
    }
}
