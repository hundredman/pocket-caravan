using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    public static class GoryeoVillageLifeDirector
    {
        [Serializable]public class Map{public string id;public int w,h;public GoryeoVillageLife.Point[] points;}
        [Serializable]public class Asset{public string key,path;}
        [Serializable]public class Config{public string status;public Map[] maps;public Asset[] assets;}
        static Config config;static GoryeoVillageLife.Art[] art;static Sprite[] trader;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;art=null;trader=null;SceneManager.sceneLoaded-=OnLoaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;Apply();}
        static void OnLoaded(Scene scene,LoadSceneMode mode)=>Apply();
        public static void Apply()
        {
            var map=MapRoot.Current;if(map==null||map.previewOnly||GoryeoTravelTransition.RegionOf(map.mapId)==null)return;
            if(config==null){var text=Resources.Load<TextAsset>("Goryeo/VillageLife/Anchors");if(text==null)return;config=JsonUtility.FromJson<Config>(text.text);}
            if(config==null||config.status!="approved"||config.maps==null||config.assets==null)return;
            var row=Array.Find(config.maps,v=>v.id==map.mapId);var road=map.GetComponent<RoadGrid>();
            if(row==null||row.points==null||road==null||row.w!=road.width||row.h!=road.height)return;
            if(art==null){var list=new List<GoryeoVillageLife.Art>();foreach(var a in config.assets){var sprite=Resources.Load<Sprite>("Goryeo/VillageLife/Art/"+a.key);if(sprite==null)return;list.Add(new GoryeoVillageLife.Art{key=a.key,sprite=sprite});}art=list.ToArray();}
            if(trader==null){var frames=new Sprite[16];for(int i=0;i<16;i++){frames[i]=Resources.Load<Sprite>("Goryeo/VillageLife/Trader/"+i);if(frames[i]==null)return;}trader=frames;}
            var life=map.GetComponent<GoryeoVillageLife>();if(life!=null)return;
            life=map.gameObject.AddComponent<GoryeoVillageLife>();life.approvedForProduction=true;life.Configure(row.points,art,trader);
        }
    }
}
