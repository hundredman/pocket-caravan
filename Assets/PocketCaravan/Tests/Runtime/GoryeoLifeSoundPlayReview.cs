#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class GoryeoLifeSoundPlayReview:MonoBehaviour
    {
        const string Review="ArtReview/Goryeo-Atmosphere-A3-20261105/";
        [Serializable]class Map{public string id;public GoryeoLifeSound.Point[] points;}
        [Serializable]class Config{public string status;public Map[] maps;}
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();AudioManager master;float oldVolume;bool oldMute,backedUp;
        void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        void Mute(bool value)=>typeof(AudioManager).GetProperty("Muted",BindingFlags.Public|BindingFlags.Static).SetValue(null,value);
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){if(backedUp){if(master!=null)master.sfxVolume=oldVolume;Mute(oldMute);}r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Review+"Unity");File.WriteAllText(Review+"Unity/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        void Pose(Vector3 position){var p=PlayerController.Instance;p.Motor.Teleport(position-(Vector3)p.Motor.feetOffset,Vector2Int.down);}
        IEnumerator Run()
        {
            Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");Time.timeScale=0;
            master=GameManager.I.GetComponent<AudioManager>();oldVolume=master.sfxVolume;oldMute=AudioManager.Muted;backedUp=true;
            foreach(var id in new[]{"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_southport","transit_crossroads","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"})
            {
                string folder=id.StartsWith("transit_",StringComparison.Ordinal)?"Transit":"Goryeo";
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+folder+"/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<6;i++)yield return null;
                var map=MapRoot.Current;Check(map.mapId==id&&map.GetComponent<GoryeoLifeSound>()==null,"no unapproved production install "+id);
                int baseSources=map.GetComponents<AudioSource>().Length;map.previewOnly=true;var config=JsonUtility.FromJson<Config>(File.ReadAllText(Review+"source.json"));var row=Array.Find(config.maps,v=>v.id==id);var sound=map.gameObject.AddComponent<GoryeoLifeSound>();sound.points=row.points;int clipIndex=0;foreach(var kind in new[]{"crowd","chicken","cow","dog","bird"})sound.clips[clipIndex++]=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PocketCaravan/Editor/B2Preview/AtmosphereA3/"+kind+".wav");sound.enabled=false;sound.enabled=true;
                foreach(var g in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();PlayerController.Instance.enabled=false;
                master.sfxVolume=.65f;Mute(false);GameManager.I.Clock.minuteOfDay=720;
                Check(!sound.approvedForProduction&&sound.SourceCount==4&&map.GetComponents<AudioSource>().Length==baseSources+4,"review source cap4 "+id);
                foreach(var clip in sound.clips)Check(clip!=null&&clip.channels==1&&clip.length>0,"clip imported "+id+" "+clip.name);
                int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);int waters=map.GetComponentsInChildren<MapAmbience>().Length;
                sound.Tick(0,0);float now=10;bool crowdSeen=false;
                foreach(var point in sound.points)
                {
                    var at=map.CellCenter(new Vector3Int(point.x,point.y,0));Pose(at);now+=100;
                    Check(GoryeoLifeSound.Gain(point,at,at,12)>0&&GoryeoLifeSound.Gain(point,at+Vector3.right*100,at,12)==0&&GoryeoLifeSound.Gain(point,at,at,2)==0,"distance/time gain "+id+" "+point.kind);
                    if(point.kind=="crowd")
                    {
                        crowdSeen=true;sound.Tick(2,now);now+=100;Check(sound.Crowd.isPlaying&&Mathf.Abs(sound.Crowd.volume-.65f*point.level)<.001f,"crowd near follows master volume "+id);
                        Pose(at+Vector3.right*100);sound.Tick(2,now);now+=100;Check(sound.Crowd.volume==0&&!sound.Crowd.isPlaying,"crowd far silent "+id);
                        Pose(at);GameManager.I.Clock.minuteOfDay=120;sound.Tick(2,now);now+=100;Check(sound.Crowd.volume==0,"crowd night silent "+id);GameManager.I.Clock.minuteOfDay=720;
                        sound.Tick(2,now);now+=100;Mute(true);sound.Tick(.01f,now);now+=100;Check(sound.Crowd.volume==0,"mute immediate "+id);Mute(false);master.sfxVolume=0;sound.Tick(2,now);now+=100;Check(sound.Crowd.volume==0,"zero SFX volume "+id);master.sfxVolume=.65f;
                    }
                    else{sound.enabled=false;sound.enabled=true;int before=sound.CallsPlayed;sound.Tick(2,now);now+=100;Check(sound.CallsPlayed>before,"animal call from nearby anchor "+id+" "+point.kind);}
                }
                if(!crowdSeen)Check(!sound.Crowd.isPlaying,"no phantom market on road "+id);
                string random=JsonUtility.ToJson(UnityEngine.Random.state);sound.Tick(.1f,now);Check(random==JsonUtility.ToJson(UnityEngine.Random.state),"global random unchanged "+id);
                sound.enabled=false;Check(!sound.Crowd.isPlaying&&sound.Crowd.volume==0,"disable stops loop "+id);Destroy(sound);yield return null;yield return null;Check(map.GetComponents<AudioSource>().Length==baseSources,"owned sources cleaned "+id);
                Check(waters==map.GetComponentsInChildren<MapAmbience>().Length&&money==GameManager.I.Player.money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo),"existing water money cargo untouched "+id);Check(!GameManager.I.AutosaveEnabled,"saves off "+id);
            }
        }
    }
}
#endif
