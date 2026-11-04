#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace PocketCaravan.EditorTools
{
    public sealed class JourneyNotesPlayReview:MonoBehaviour
    {
        [Serializable] class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report report=new Report();const string Out="Docs/Validation/JourneyNotes-Task2-2026-11-04";Keyboard keyboard,previous;
        void Check(bool value,string name){if(!value)throw new Exception(name);report.checks.Add(name);}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object item=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)item=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}Finish(null);}
        void Finish(Exception error){if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(previous!=null&&previous.added)previous.MakeCurrent();report.complete=error==null;report.error=error==null?"":error.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Press(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;}
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();Check(im.width==960&&im.height==540,"960x540 "+name);File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;Check(MapRoot.Current.previewOnly&&!GameManager.I.AutosaveEnabled,"save isolation");Time.timeScale=0;
            foreach(var i in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){i.StopAllCoroutines();i.enabled=false;}foreach(var i in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))i.enabled=false;
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
            var gm=GameManager.I;var destinations=JourneyGuide.Destinations(gm.Db,MapRoot.Current.region,WorldIndex.Get(),MapRoot.Current.mapId);
            foreach(var id in new[]{"transit_woodtown","transit_ferry","transit_southport","transit_crossroads"})Check(destinations.Exists(t=>t.id==id&&!string.IsNullOrWhiteSpace(t.journeyNote)),"visible journey note "+id);
            previous=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();yield return Press(Key.Q);
            var tracker=UnityEngine.Object.FindFirstObjectByType<QuestTracker>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;Check((bool)typeof(QuestTracker).GetField("logOpen",flags).GetValue(tracker),"Q opens journey notebook");
            typeof(QuestTracker).GetField("logTab",flags).SetValue(tracker,2);typeof(QuestTracker).GetField("scroll",flags).SetValue(tracker,Vector2.zero);yield return Photo("journey-top");
            typeof(QuestTracker).GetField("scroll",flags).SetValue(tracker,new Vector2(0,10000));yield return Photo("journey-bottom");
            yield return Press(Key.Escape);Check(!GameManager.I.AutosaveEnabled,"no save enabled");
        }
    }
}
#endif
