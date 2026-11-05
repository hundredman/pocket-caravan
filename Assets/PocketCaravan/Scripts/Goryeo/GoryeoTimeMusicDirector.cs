using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    /// <summary>Install one approved music controller on the persistent manager, only from owned production maps.</summary>
    public static class GoryeoTimeMusicDirector
    {
        [Serializable]public class Config{public string status;public string[] maps;}
        static Config config;static AudioClip[] clips;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;clips=null;SceneManager.sceneLoaded-=OnLoaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;Apply();}
        static void OnLoaded(Scene scene,LoadSceneMode mode)=>Apply();
        public static void Apply()
        {
            var map=MapRoot.Current;var gm=GameManager.I;
            if(gm==null||map==null||map.previewOnly||GoryeoTravelTransition.RegionOf(map.mapId)==null)return;
            if(config==null){var asset=Resources.Load<TextAsset>("Goryeo/TimeMusic/Config");if(asset==null)return;config=JsonUtility.FromJson<Config>(asset.text);}
            if(config==null||config.status!="approved"||config.maps==null||Array.IndexOf(config.maps,map.mapId)<0)return;
            if(clips==null){clips=new AudioClip[6];int i=0;foreach(var name in new[]{"morning","day","evening","night"})clips[i++]=Resources.Load<AudioClip>("Goryeo/TimeMusic/"+name);clips[4]=Resources.Load<AudioClip>("Audio/Music/road");clips[5]=Resources.Load<AudioClip>("Audio/Music/danger");}
            foreach(var clip in clips)if(clip==null)return;
            var music=gm.GetComponent<GoryeoTimeMusic>()??gm.gameObject.AddComponent<GoryeoTimeMusic>();
            music.clips=clips;music.approvedForProduction=true;
        }
    }
}
