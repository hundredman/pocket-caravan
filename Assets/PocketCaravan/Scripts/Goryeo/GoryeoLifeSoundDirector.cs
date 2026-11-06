using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan
{
    /// <summary>Install approved A3 living ambience on owned production maps only.</summary>
    public static class GoryeoLifeSoundDirector
    {
        [Serializable]public class Map{public string id;public int w,h;public GoryeoLifeSound.Point[] points;}
        [Serializable]public class Config{public string status;public Map[] maps;}
        static Config config;static AudioClip[] clips;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){config=null;clips=null;SceneManager.sceneLoaded-=OnLoaded;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=OnLoaded;SceneManager.sceneLoaded+=OnLoaded;Apply();}
        static void OnLoaded(Scene scene,LoadSceneMode mode)=>Apply();
        public static void Apply()
        {
            var root=MapRoot.Current;
            if(root==null||root.previewOnly||GoryeoTravelTransition.RegionOf(root.mapId)==null)return;
            if(config==null){var asset=Resources.Load<TextAsset>("Goryeo/Life/Anchors");if(asset==null)return;config=JsonUtility.FromJson<Config>(asset.text);}
            if(config==null||config.status!="approved"||config.maps==null)return;
            var data=Array.Find(config.maps,v=>v.id==root.mapId);var road=root.GetComponent<RoadGrid>();
            if(data==null||data.w<=0||data.h<=0||data.points==null||road==null||road.width!=data.w||road.height!=data.h)return;
            if(clips==null){clips=new AudioClip[5];int i=0;foreach(var name in new[]{"crowd","chicken","cow","dog","bird"})clips[i++]=Resources.Load<AudioClip>("Goryeo/Life/"+name);}
            foreach(var clip in clips)if(clip==null)return;
            var stride=root.GetComponent<GoryeoLifeSound>()??root.gameObject.AddComponent<GoryeoLifeSound>();
            stride.points=data.points;stride.clips=clips;stride.approvedForProduction=true;
        }
    }
}
