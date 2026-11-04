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
        static void Install(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;Apply();}
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
