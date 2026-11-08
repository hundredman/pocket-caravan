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
    public sealed class GoryeoAtmosphereMotionReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Atmosphere-A1-20261105/Unity",Frames="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA1/MotionFrames";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();File.WriteAllText(Out+"/motion.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Run()
        {
            Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Editor/B2Preview/AtmosphereA1/Naru.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<8;i++)yield return null;
            var map=MapRoot.Current;var weather=map.GetComponent<GoryeoAtmosphere>();Check(map.previewOnly&&!weather.approvedForProduction&&!GameManager.I.AutosaveEnabled,"review only");
            foreach(var g in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
            foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
            var player=PlayerController.Instance;player.enabled=false;player.Motor.Teleport(map.CellCenter(new Vector3Int(44,53,0))-(Vector3)player.Motor.feetOffset,Vector2Int.down);
            var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=map.CellCenter(new Vector3Int(44,53,0))+new Vector3(0,1,-10);Time.timeScale=1;
            foreach(var kind in new[]{GoryeoAtmosphere.Kind.Rain})
            {
                for(int day=1;day<1000;day++)if(GoryeoAtmosphere.DayWeather(day,weather.settings)==kind){GameManager.I.Clock.day=day;GameManager.I.Clock.minuteOfDay=12*60;break;}
                weather.Refresh();yield return null;yield return null;
                var tracked=new List<Transform>();var before=new List<Vector3>();foreach(var sr in map.GetComponentsInChildren<SpriteRenderer>())if(sr.name=="Weather particle"){tracked.Add(sr.transform);before.Add(sr.transform.position);}
                Check(tracked.Count>0,"tracked renderers "+kind);string dir=Frames+"/"+kind;Directory.CreateDirectory(dir);int count=24;
                for(int frame=0;frame<count;frame++)
                {
                    yield return new WaitForSecondsRealtime(.125f);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==480&&t.height==270,"480x270 motion "+kind+" "+frame);File.WriteAllBytes(dir+"/"+frame.ToString("D3")+".png",t.EncodeToPNG());Destroy(t);
                }
                bool moved=false;for(int i=0;i<tracked.Count;i++)if(Vector3.Distance(tracked[i].position,before[i])>.03125f)moved=true;
                Check(moved&&weather.Shown==kind,"actual live renderer motion, not fixed overlay "+kind);Check(!GameManager.I.AutosaveEnabled,"saves off "+kind);
            }
        }
    }
}
#endif
