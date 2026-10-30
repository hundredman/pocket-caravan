#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class LocalTravelPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/LocalTravel-20261030";
        [Serializable] public class Row{public bool passed;public string test,detail;}
        [Serializable] public class Report{public bool complete;public string error;public List<Row> checks=new List<Row>();}
        readonly Report report=new Report();
        void Check(bool ok,string test,string detail=""){report.checks.Add(new Row{passed=ok,test=test,detail=detail});if(!ok)throw new Exception(test+" "+detail);}
        void OnEnable(){SceneManager.sceneLoaded+=Isolate;}
        void OnDisable(){SceneManager.sceneLoaded-=Isolate;}
        static void Isolate(Scene s,LoadSceneMode m){foreach(var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))pc.enabled=false;}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object item=null;bool moved=false;Exception e=null;try{moved=stack.Peek().MoveNext();if(moved)item=stack.Peek().Current;}catch(Exception ex){e=ex;}if(e!=null){Finish(e);yield break;}if(!moved){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}Finish(null);}
        void Finish(Exception e){report.complete=e==null;report.error=e==null?null:e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/checks.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        IEnumerator Run(){Directory.CreateDirectory(Out);for(int i=0;i<12;i++)yield return null;Check(!GameManager.I.AutosaveEnabled,"safe preview startup");Time.timeScale=0;
            string[] ids={"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_crossroads","transit_southport","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"};
            foreach(string a in ids)foreach(string b in ids){var fx=TravelTransition.For(a,b);Check(fx is GoryeoTravelTransition,"regional policy registered",a+" -> "+b);fx.Begin(a,b);bool crossing=GoryeoTravelTransition.IsRegionalCrossing(a,b);Check(fx.ShowArrivalNotice==crossing,"arrival notice only across regions",a+" -> "+b);Check(crossing?fx.CoverSeconds==.22f&&fx.RevealSeconds==.28f&&fx.MinHoldSeconds==.65f&&fx.AfterLoadSeconds==.25f:fx.CoverSeconds==0&&fx.RevealSeconds==0&&fx.MinHoldSeconds==0&&fx.AfterLoadSeconds==0,"regional-only visual timing",a+" -> "+b);if(!crossing)fx.Draw(1,true,"",0);}
            Check(!(TravelTransition.For("venice_rialto","arabia_central_steppe") is GoryeoTravelTransition),"foreign policies unchanged");
            foreach(var scene in EditorBuildSettings.scenes)if(Path.GetFileNameWithoutExtension(scene.path)=="goryeo_jangdan"){yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scene.path,new LoadSceneParameters(LoadSceneMode.Single));break;}for(int i=0;i<3;i++)yield return null;
            foreach(string target in new[]{"goryeo_songak","Dakmok","transit_woodtown","transit_ferry","transit_crossroads","goryeo_songak"}){
                string from=MapRoot.Current.mapId;bool cross=GoryeoTravelTransition.IsRegionalCrossing(from,target);var gm=GameManager.I;Notify.Clear();gm.TravelTo(target,"S0");bool sawCover=false;float began=Time.realtimeSinceStartup;int samples=0;
                while(gm.IsTravelling){float fade=(float)typeof(GameManager).GetField("fade",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(gm);if(!cross)Check(fade==0,"same-region travel never covers the screen",from+" -> "+target+" "+fade);if(cross&&fade>=.999f&&!sawCover){sawCover=true;yield return Capture("regional-"+target);}samples++;if(Time.realtimeSinceStartup-began>45)throw new Exception("Travel timeout");yield return null;}
                Check(MapRoot.Current.mapId==target,"actual travel reaches destination",from+" -> "+target);Check(!gm.AutosaveEnabled,"autosave remains disabled");Check(cross==sawCover,"cover occurs only at regional crossings",target);Check(samples>0,"actual scene-load frames sampled",target);if(!cross){bool location=false;var entries=(IEnumerable)typeof(Notify).GetField("entries",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);foreach(var entry in entries)if((string)entry.GetType().GetField("key").GetValue(entry)=="location")location=true;Check(!location,"no automatic location overlay on local arrival",target);}if(!cross)yield return Capture("local-"+target);
            }
        }
    }
}
#endif
