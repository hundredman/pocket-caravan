#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class PortWaterPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/PortWater-Task6-20261104/Unity";
        [Serializable]class Patch{public string mapId;public int[][] seaCells;}
        [Serializable]class Data{public int w,h;public int[] ground;}
        [Serializable]class Result{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Result report=new Result();void Check(bool b,string s){if(!b)throw new Exception(s);report.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){report.complete=e==null;report.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();Check(im.width==960&&im.height==540,"960x540 "+name);File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");Time.timeScale=0;
            foreach(var mid in new[]{"transit_southport","transit_ferry"})foreach(var version in new[]{"before","after"})
            {
                string scene=version=="before"?"Assets/PocketCaravan/Editor/B2Preview/PortWater20261104/"+mid+"-before.unity":"Assets/PocketCaravan/Scenes/Transit/"+mid+".unity";
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scene,new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<10;i++)yield return null;
                var map=MapRoot.Current;Check(!GameManager.I.AutosaveEnabled&&map.previewOnly==(version=="before"),"correct actual/preview scene "+mid+" "+version);
                foreach(var guide in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))guide.enabled=false;
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
                var before=JsonUtility.FromJson<Data>(File.ReadAllText("ArtReview/PortWater-Task6-20261104/Before/"+mid+".json"));var after=JsonUtility.FromJson<Data>(File.ReadAllText("Assets/PocketCaravan/Maps/"+mid+".json"));int changed=0;bool blocked=true,painted=true;
                for(int i=0;i<before.ground.Length;i++)if(before.ground[i]!=after.ground[i]){var cell=new Vector3Int(i%before.w,before.h-1-i/before.w,0);changed++;blocked&=!map.IsWalkable(cell);painted&=map.ground.HasTile(cell)==(version=="after");}
                Check(changed==(mid=="transit_southport"?1128:727)&&blocked&&painted,"sea cells remain blocked and correct before/after ground "+mid+" "+version);
                int ripples=0;foreach(var anim in UnityEngine.Object.FindObjectsByType<SpriteAnim>(FindObjectsSortMode.None))
                {anim.enabled=false;if(anim.frames.Length>0)anim.GetComponent<SpriteRenderer>().sprite=anim.frames[0];if(anim.name=="Anim goryeo_ripples")ripples++;}
                if(mid=="transit_ferry")Check(ripples==(version=="after"?20:0),"existing transparent ripple references "+version);
                var market=UnityEngine.Object.FindFirstObjectByType<TownMarket>();Check(market!=null&&market.town.id==mid,"market binding preserved "+mid);
                var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;camera.transform.position=mid=="transit_southport"?new Vector3(45,12,-10):new Vector3(64,26,-10);
                yield return Photo(mid+"-"+version);
            }
        }
    }
}
#endif
