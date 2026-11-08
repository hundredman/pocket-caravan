using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    public static class GoryeoPlaceLabelsDirector
    {
        [Serializable]public class Map{public string id;public int w,h;public GoryeoPlaceLabels.Place[] places;}
        [Serializable]public class Config{public string status;public float near;public Map[] maps;}
        static Config config;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;SceneManager.sceneLoaded-=OnLoaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;Apply();}
        static void OnLoaded(Scene scene,LoadSceneMode mode)=>Apply();
        public static void Apply()
        {
            var map=MapRoot.Current;if(map==null||map.previewOnly)return;
            var places=Array.Empty<GoryeoPlaceLabels.Place>();
            if(GoryeoTravelTransition.RegionOf(map.mapId)!=null)
            {
                if(config==null){var asset=Resources.Load<TextAsset>("Goryeo/PlaceLabels");if(asset==null)return;config=JsonUtility.FromJson<Config>(asset.text);}
                if(config==null||config.status!="approved"||config.near!=GoryeoPlaceLabels.Near||config.maps==null)return;
                var row=Array.Find(config.maps,v=>v.id==map.mapId);var road=map.GetComponent<RoadGrid>();
                if(row==null||row.places==null||road==null||road.width!=row.w||road.height!=row.h)return;
                places=row.places;
            }
            // Foreign regions retain their authoring data; only the common presentation is shared.
            var labels=map.GetComponent<GoryeoPlaceLabels>()??map.gameObject.AddComponent<GoryeoPlaceLabels>();
            labels.places=places;labels.approvedForProduction=true;labels.Refresh();
        }
    }
}
