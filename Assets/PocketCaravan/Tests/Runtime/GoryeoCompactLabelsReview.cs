#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    // Isolated, editor-only captures of the real scene/UI. No user save writes.
    public sealed class GoryeoCompactLabelsReview : MonoBehaviour
    {
        public static bool Before;
        public static bool IntroOnly;
        public static Action<int,int> Resize;
        static string Out=>IntroOnly?"ArtReview/Goryeo-Intro-Card-20261107/":"ArtReview/Goryeo-Compact-Labels-20261107/";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
        [Serializable] class Scope { public ScopeMap[] maps; }
        [Serializable] class ScopeMap { public string id; }
        [Serializable] class Report { public bool complete; public string error; public List<string> checks=new List<string>(); }
        readonly Report report=new Report();
        string slot;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);report.checks.Add(name);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Out);
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){bool more=false;object current=null;Exception err=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){err=e;}
                if(err!=null){Finish(err);yield break;}if(!more){stack.Pop();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;}
            Finish(null);
        }
        void Finish(Exception error){report.complete=error==null;report.error=error?.ToString();File.WriteAllText(Out+(Before?"before":"after")+".json",JsonUtility.ToJson(report,true));Time.timeScale=1;EditorApplication.isPlaying=false;}
        IEnumerator Frames(){for(int i=0;i<8;i++)yield return null;yield return new WaitForEndOfFrame();}
        void Quiet(){foreach(var s in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){s.StopAllCoroutines();s.enabled=false;}foreach(var s in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None))s.StopAllCoroutines();var d=FindFirstObjectByType<DialogueUI>();if(d!=null)typeof(DialogueUI).GetMethod("Close",F).Invoke(d,null);UIState.Reset();Notify.Clear();Time.timeScale=0;}
        IEnumerator Photo(string name){yield return Frames();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+(Before?"before-":"after-")+name+".png",t.EncodeToPNG());Check(t.width==Screen.width&&t.height==Screen.height,"native capture "+name);Destroy(t);}
        void Focus(Interactable it){var pc=PlayerController.Instance;var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=it.transform.position+new Vector3(0,-2,-10);pc.Motor.Teleport(it.transform.position+Vector3.down*1.3f-(Vector3)pc.Motor.feetOffset,Vector2Int.up);}
        IEnumerator Run()
        {
            var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"SaveOFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)typeof(NetLogin).GetMethod("PlayAlone",F).Invoke(login,null);
            typeof(GameManager).GetField("firstSceneLoaded",F).SetValue(gm,true);
            SaveSystem.UseSlot("codex_compact_labels_"+Guid.NewGuid().ToString("N")+".json");slot=SaveSystem.CurrentPath;
            Resize(480,270);yield return Frames();yield return SceneManager.LoadSceneAsync("Naru");yield return Frames();Quiet();gm.Clock.minuteOfDay=720;
            if(IntroOnly){
                Check(gm.Player.origin!=null&&gm.Player.origin.id=="goryeo","Goryeo tutorial origin");
                var intro=FindFirstObjectByType<GoryeoIntro>();Check(intro!=null,"real Goryeo intro component");
                foreach(var size in new[]{new Vector2Int(480,270),new Vector2Int(960,540),new Vector2Int(480,360)}){
                    Resize(size.x,size.y);yield return Frames();
                    for(int step=1;step<=10;step++){
                        gm.SetMemo("goryeo:intro",step);yield return Frames();
                        var r=intro.GuideRect;
                        Check(r.height>=24&&r.yMin>=52&&r.width==168&&Mathf.Abs(r.xMax-UIStyle.VirtualWidth)<.1f,"compact tutorial tracker below Q/M/L step "+step+" "+size);
                        Check(r.xMin>=10&&r.xMax<=UIStyle.VirtualWidth&&r.yMax<UIStyle.VirtualHeight-44,"tutorial guide fits viewport step "+step+" "+size);
                        if(step==1){Check(intro.GuideText.Contains("Space")&&intro.GuideText.Contains("선착장 일꾼"),"complete first conversation instruction");yield return Photo("first-"+size.x+"x"+size.y);}
                        if(step==10)yield return Photo("return-"+size.x+"x"+size.y);
                    }
                }
                Check(!gm.AutosaveEnabled&&!File.Exists(slot),"isolated tutorial review writes no save");yield break;
            }
            Interactable woman=null;foreach(var it in Interactable.All)if(it.DisplayName=="나루 우물가 아낙")woman=it;
            Check(woman!=null,"real wellside woman found");Focus(woman);yield return Photo("well");
            if(!Before){var labels=MapRoot.Current.GetComponent<GoryeoPlaceLabels>();Check(labels.DrawnNames.Contains("아낙"),"short name rendered without changing source identity");var prior=new Dictionary<Interactable,Rect>(labels.TargetNameRects);for(int i=0;i<5;i++){PlayerController.Instance.Motor.Teleport(woman.transform.position+new Vector3((i-2)*.35f,-1.4f,0)-(Vector3)PlayerController.Instance.Motor.feetOffset,Vector2Int.up);yield return Frames();foreach(var p in prior)if(labels.TargetNameRects.TryGetValue(p.Key,out var r))Check(r==p.Value,"name remains fixed while player moves");}}
            var fish=FindFirstObjectByType<GoryeoPastime>();Check(fish!=null,"real Naru fishing found");Focus(fish);yield return Photo("fishing-place");
            foreach(var size in new[]{new Vector2Int(480,270),new Vector2Int(960,540),new Vector2Int(480,360)}){
                Resize(size.x,size.y);yield return Frames();fish.Interact(PlayerController.Instance);Check(fish.IsOpen,"fishing opens");yield return Photo("fishing-"+size.x+"x"+size.y);
                if(!Before){var r=(Rect)typeof(GoryeoPastime).GetProperty("WindowRect").GetValue(fish);Check(r.xMin>=8&&r.yMin>=8&&r.xMax<=UIStyle.VirtualWidth-8&&r.yMax<=UIStyle.VirtualHeight-8,"fishing fits actual viewport "+size);fish.Cast();yield return Frames();fish.Strike();Check(fish.Attempts==1,"fishing interaction still works");}
                fish.Close();Check(!UIState.Blocking,"fishing releases modal");}
            if(!Before){
                Resize(480,270);yield return Frames();
                var scope=JsonUtility.FromJson<Scope>(Resources.Load<TextAsset>("Goryeo/PlaceLabels").text);
                foreach(var row in scope.maps){
                    yield return SceneManager.LoadSceneAsync(row.id);yield return Frames();Quiet();
                    var items=new List<Interactable>(Interactable.All);int captures=0;
                    foreach(var it in items){if(it==null||it.gameObject.scene!=SceneManager.GetActiveScene())continue;
                        Focus(it);yield return Frames();var labels=MapRoot.Current.GetComponent<GoryeoPlaceLabels>();
                        for(int a=0;a<labels.DrawnRects.Count;a++)for(int b=a+1;b<labels.DrawnRects.Count;b++)Check(!labels.DrawnRects[a].Overlaps(labels.DrawnRects[b]),"names do not overlap "+row.id);
                        foreach(var badge in labels.QuestBadgeRects)foreach(var name in labels.TargetNameRects)Check(!badge.Value.Overlaps(name.Value),"quest badge does not cover a name "+row.id);
                        foreach(var name in labels.DrawnNames)Check(name.Length<=10,"short world label "+row.id+" "+name);
                        Check(labels.PromptRect.width<40,"Space stays compact "+row.id);
                        if(captures++==0)yield return Photo("map-"+row.id);
                    }
                }
            }
            Check(!gm.AutosaveEnabled&&!File.Exists(slot),"no writes to isolated or user saves");
        }
    }
}
#endif
