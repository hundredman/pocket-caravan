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
    public sealed class LongLogTrialPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/LongLog-v2-20261104/Unity";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string n){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==960&&t.height==540,"960x540 "+n);File.WriteAllBytes(Out+"/"+n+".png",t.EncodeToPNG());Destroy(t);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;var map=MapRoot.Current;Check(map.previewOnly&&!GameManager.I.AutosaveEnabled,"preview-only and saves off");Time.timeScale=0;
            foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))g.enabled=false;
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
            var player=PlayerController.Instance;var cell=new Vector3Int(29,39,0);Check(map.IsWalkable(cell),"walkable trial approach");player.Motor.Teleport(map.CellCenter(cell)-(Vector3)player.Motor.feetOffset,Vector2Int.up);
            var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=new Vector3(29.5f,42,-10);
            var sr=map.transform.Find("Long log art trial").GetComponent<SpriteRenderer>();Check(sr.sprite.rect.size==new Vector2(48,48)&&sr.sprite.pixelsPerUnit==16,"original48 whole native");yield return Photo("before");
            sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PocketCaravan/Editor/B2Preview/LongLog20261104/after.png");sr.transform.position=new Vector3(27.5f,41,0);Check(sr.sprite.rect.size==new Vector2(64,48)&&sr.sprite.pixelsPerUnit==16&&sr.transform.localScale==Vector3.one,"long64 whole native no scaling");yield return Photo("after");
            Check(!GameManager.I.AutosaveEnabled,"no saves enabled");
        }
    }
}
#endif
