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
    public sealed class GoryeoAtmosphereProductionReview:MonoBehaviour
    {
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();File.WriteAllText("ArtReview/Goryeo-Atmosphere-A1-20261105/Unity/production.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Run()
        {
            Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");Time.timeScale=0;
            foreach(var id in new[]{"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_southport","transit_crossroads","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"})
            {
                string folder=id.StartsWith("transit_",StringComparison.Ordinal)?"Transit":"Goryeo";
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+folder+"/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<8;i++)yield return null;
                var map=MapRoot.Current;var weather=map.GetComponent<GoryeoAtmosphere>();Check(map.mapId==id&&!map.previewOnly&&weather!=null&&weather.approvedForProduction,"actual owned map installed "+id);
                GoryeoAtmosphereDirector.Apply();Check(map.GetComponents<GoryeoAtmosphere>().Length==1,"no duplicate install "+id);
                for(int d=1;d<1000;d++)if(GoryeoAtmosphere.DayWeather(d)==GoryeoAtmosphere.Kind.Monsoon){GameManager.I.Clock.day=d;GameManager.I.Clock.minuteOfDay=720;break;}
                weather.Refresh();yield return null;Check(weather.Shown==GoryeoAtmosphere.Kind.Monsoon&&weather.ActiveParticles==128,"actual rain128 "+id);
                for(int d=1;d<1000;d++)if(GoryeoAtmosphere.DayWeather(d)==GoryeoAtmosphere.Kind.Mist){GameManager.I.Clock.day=d;GameManager.I.Clock.minuteOfDay=450;break;}
                weather.Refresh();Check((weather.Shown==GoryeoAtmosphere.Kind.Mist)==GoryeoAtmosphere.MistMap(id),"actual mist location "+id);
                if(id=="Naru")
                {
                    foreach(var guide in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){guide.StopAllCoroutines();guide.enabled=false;}
                    foreach(var guide in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){guide.StopAllCoroutines();guide.enabled=false;}
                    foreach(var dialogue in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(dialogue,null);
                    UIState.Blocking=false;Notify.Clear();var player=PlayerController.Instance;player.enabled=false;var cell=new Vector3Int(44,53,0);player.Motor.Teleport(map.CellCenter(cell)-(Vector3)player.Motor.feetOffset,Vector2Int.down);
                    var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;camera.transform.position=map.CellCenter(cell)+new Vector3(0,1,-10);yield return null;
                    yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();Check(image.width==480&&image.height==270,"actual480x270");File.WriteAllBytes("ArtReview/Goryeo-Atmosphere-A1-20261105/Unity/Naru-production-mist.png",image.EncodeToPNG());Destroy(image);
                }
                Check(!GameManager.I.AutosaveEnabled,"saves off "+id);
            }
        }
    }
}
#endif
