#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PocketCaravan.EditorTools
{
    /// <summary>Editor-only text palette and real UI comparison. No server connection or user save writes.</summary>
    public sealed class GoryeoFixedLabelsReview : MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Fixed-Labels-20261107/";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] class LayoutSnapshot { public string[] names; public Rect[] rectangles, protectedArt; public Vector2 origin; }
        [Serializable] class Scope { public ScopeMap[] maps; }
        [Serializable] class ScopeMap { public string id; }
        [Serializable] class Report { public bool complete; public string error; public List<string> checks = new List<string>(); }
        readonly Report report = new Report();
        public static Action<int,int> Resize;
        Texture2D lastCapture;
        string slotPath;
        RegionTestSession redirectedSession;
        string originalSessionSlot;
        void Check(bool value, string text) { if (!value) throw new Exception(text); report.checks.Add(text); }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                bool more = false; object value = null; Exception error = null;
                try { more = stack.Peek().MoveNext(); if (more) value = stack.Peek().Current; } catch (Exception e) { error = e; }
                if (error != null) { Finish(error); yield break; }
                if (!more) { stack.Pop(); continue; }
                if (value is IEnumerator) stack.Push((IEnumerator)value); else yield return value;
            }
            Finish(null);
        }
        void Finish(Exception error)
        {
            if(redirectedSession!=null)redirectedSession.saveFileName=originalSessionSlot;
            if (GameManager.I != null) typeof(GameManager).GetField("autosave", Flags).SetValue(GameManager.I, false);
            if(slotPath!=null && Directory.Exists(slotPath+".tmp"))Directory.Delete(slotPath+".tmp");
            if (slotPath != null) foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(slotPath + suffix)) File.Delete(slotPath + suffix);
            report.complete = error == null; report.error = error == null ? "" : error.ToString();
            Directory.CreateDirectory(Out); File.WriteAllText(Out + "play.json", JsonUtility.ToJson(report, true));
            if(lastCapture!=null)Destroy(lastCapture);
            Time.timeScale = 1; EditorApplication.isPlaying = false;
        }
        void Quiet()
        {
            foreach (var s in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)) { s.StopAllCoroutines(); s.enabled = false; }
            foreach (var s in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)) s.StopAllCoroutines();
            var d = FindFirstObjectByType<DialogueUI>(); typeof(DialogueUI).GetMethod("Close", Flags).Invoke(d, null);
            UIState.Reset(); Notify.Clear(); Time.timeScale = 0;
        }
        IEnumerator Load(string path)
        {
            yield return SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(path), LoadSceneMode.Single);
            for (int i = 0; i < 12; i++) yield return null;
            Quiet(); Check(SaveSystem.CurrentPath == slotPath, "isolated slot retained " + path+" actual="+SaveSystem.CurrentPath+" expected="+slotPath);
            Check(SceneManager.GetActiveScene().buildIndex >= 0, "normal registered scene index " + path);
        }
        IEnumerator Photo(string name)
        {
            Notify.Clear(); yield return null; yield return new WaitForEndOfFrame();
            var t = ScreenCapture.CaptureScreenshotAsTexture(); Check(t.width == Screen.width && t.height == Screen.height, "native capture " + name+" "+t.width+"x"+t.height);
            File.WriteAllBytes(Out + name + ".png", t.EncodeToPNG()); lastCapture=t;
        }
        IEnumerator Size(int w,int h)
        {
            if(Resize==null)throw new Exception("Resize delegate missing");Resize(w,h);
            for(int i=0;i<10;i++)yield return null;
            Check(Screen.width==w&&Screen.height==h,"actual viewport "+w+"x"+h);
        }
        void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Flags).SetValue(obj,value);
        object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Flags).Invoke(obj,args);
        IEnumerator Frames(){for(int i=0;i<5;i++)yield return null;yield return new WaitForEndOfFrame();}
        void CheckFixed(GoryeoPlaceLabels labels,string where)
        {
            foreach(var pair in labels.TargetNameRects)
            {
                var body=labels.TargetBodyRects[pair.Key];
                Check(pair.Value.yMax<=body.yMin-1,"name fixed above own sprite "+where+" "+pair.Key.DisplayName);
                Check(Mathf.Abs(pair.Value.center.x-body.center.x)<=.6f,"name horizontally centered on own target "+where);
            }
            foreach(var pair in labels.QuestBadgeRects)
            {
                Check(labels.QuestBadgeStates[pair.Key]==GoryeoPlaceLabels.BadgeFor(pair.Key),"badge matches real quest/suffix state "+where);
                if(labels.TargetBodyRects.TryGetValue(pair.Key,out var b))Check(pair.Value.yMax<=b.yMin-2,"quest badge separated above head "+where);
                if(labels.TargetNameRects.TryGetValue(pair.Key,out var n))Check(pair.Value.xMin>=n.xMax+2 && Mathf.Abs(pair.Value.yMax-n.yMax)<.1f,"small quest icon on right of name without moving name "+where);
            }
            if(labels.PromptTarget!=null && labels.PromptRect.height>0 && labels.TargetBodyRects.TryGetValue(labels.PromptTarget,out var promptBody))
                Check(labels.PromptRect.yMin>=promptBody.yMax+2,"Space fixed below own sprite "+where);
            foreach(var name in labels.DrawnNames)Check(!name.EndsWith(" !")&&!name.EndsWith(" ?"),"no inline quest suffix in name "+where);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"SaveOFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)Call(login,"PlayAlone");Set(gm,"firstSceneLoaded",true);
            SaveSystem.UseSlot("codex_fixed_labels_"+Guid.NewGuid().ToString("N")+".json");slotPath=SaveSystem.CurrentPath;
            yield return Size(480,270);yield return Load("Naru");
            Check(GoryeoPlaceLabels.PlainName("선착장 일꾼 !")=="선착장 일꾼","intro suffix removed for display only");
            var scope=JsonUtility.FromJson<Scope>(Resources.Load<TextAsset>("Goryeo/PlaceLabels").text);
            int promptComparisons=0, changedTargets=0;
            foreach(var row in scope.maps)
            {
                yield return Load(row.id);gm.Clock.minuteOfDay=720;
                var pc=PlayerController.Instance;var focus=(Interactable)FindFirstObjectByType<ContractBoard>();
                if(focus==null)foreach(var it in Interactable.All)if(it.gameObject.scene==SceneManager.GetActiveScene()&&!string.IsNullOrEmpty(it.DisplayName)){focus=it;break;}
                if(focus==null)continue;
                pc.enabled=true;var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;
                var center=focus.transform.position;cam.transform.position=center+Vector3.down*2+new Vector3(0,0,-10);
                pc.Motor.Teleport(center+Vector3.down*2-(Vector3)pc.Motor.feetOffset,Vector2Int.up);yield return Frames();
                var labels=MapRoot.Current.GetComponent<GoryeoPlaceLabels>();Check(labels!=null&&labels.OwnsOriginal,"scoped fixed labels "+row.id);
                CheckFixed(labels,row.id);
                var baseline=new Dictionary<Interactable,Rect>(labels.TargetNameRects);
                var badges=new Dictionary<Interactable,Rect>(labels.QuestBadgeRects);
                var prompts=new Dictionary<Interactable,Rect>();Interactable last=null;
                for(int step=0;step<16;step++)
                {
                    float angle=step*Mathf.PI/8;
                    var feet=center+new Vector3(Mathf.Sin(angle)*2.1f,Mathf.Cos(angle)*2.1f,0);
                    var facing=Vector2Int.RoundToInt((Vector2)(center-feet).normalized);
                    pc.Motor.Teleport(feet-(Vector3)pc.Motor.feetOffset,facing);yield return Frames();
                    foreach(var pair in baseline)
                        if(pair.Key!=null&&labels.TargetNameRects.TryGetValue(pair.Key,out var rect))Check(rect==pair.Value,"player movement cannot relocate name "+row.id+" step "+step);
                    foreach(var pair in badges)
                        if(pair.Key!=null&&labels.QuestBadgeRects.TryGetValue(pair.Key,out var rect))Check(rect==pair.Value,"player movement cannot relocate quest badge "+row.id);
                    if(labels.PromptTarget!=null && labels.PromptRect.height>0)
                    {
                        var target=labels.PromptTarget;var rect=labels.PromptRect;
                        Check(rect.height==12&&rect.width<40,"Space only stays compact "+row.id);
                        if(prompts.TryGetValue(target,out var previous)){Check(rect==previous,"same-target Space unaffected by player position "+row.id);promptComparisons++;}
                        else prompts.Add(target,rect);
                        if(last!=null&&last!=target)changedTargets++;last=target;
                    }
                    CheckFixed(labels,row.id);
                    if(row.id=="Naru")yield return Photo("movement-"+step.ToString("00"));
                }
                Check(labels.TargetNameRects.Count>0,"fixed target names visible "+row.id);
                pc.Motor.Teleport(center+Vector3.down*2-(Vector3)pc.Motor.feetOffset,Vector2Int.up);yield return Frames();yield return Photo("map-"+row.id);
                if(row.id=="Naru")
                {
                    Check(labels.QuestBadgeRects.Count>0,"actual Naru quest/intro badge visible");
                    var old=(Dictionary<SpriteRenderer,bool>)labels.GetType().GetField("oldMarkers",Flags).GetValue(labels);
                    var restored=new Dictionary<SpriteRenderer,bool>(old);Check(restored.Count>0,"actual legacy quest markers registered");
                    foreach(var pair in restored)if(pair.Key!=null)Check(!pair.Key.enabled,"no duplicate legacy world quest sprite");
                    labels.enabled=false;foreach(var pair in restored)if(pair.Key!=null)Check(pair.Key.enabled==pair.Value,"legacy markers restored on scoped label disable");
                    labels.enabled=true;yield return Frames();
                    var stable=new Dictionary<Interactable,Rect>(labels.TargetNameRects);
                    var badgeStable=new Dictionary<Interactable,Rect>(labels.QuestBadgeRects);
                    var origin=(Vector2)labels.GetType().GetField("guiOrigin",Flags).GetValue(labels);
                    var prompt=labels.PromptRect;var target=labels.PromptTarget;
                    for(int i=0;i<12;i++)
                    {
                        cam.transform.position+=new Vector3(.04f,.03f,0);yield return Frames();
                        var now=(Vector2)labels.GetType().GetField("guiOrigin",Flags).GetValue(labels);var delta=now-origin;
                        foreach(var pair in stable)if(labels.TargetNameRects.TryGetValue(pair.Key,out var rect))Check(Vector2.Distance(rect.position,pair.Value.position+delta)<.1f,"camera pan keeps target-relative name offset");
                        foreach(var pair in badgeStable)if(labels.QuestBadgeRects.TryGetValue(pair.Key,out var rect))Check(Vector2.Distance(rect.position,pair.Value.position+delta)<.1f,"camera pan keeps quest badge offset");
                        if(target!=null && labels.PromptTarget==target && prompt.height>0 && labels.PromptRect.height>0)Check(Vector2.Distance(labels.PromptRect.position,prompt.position+delta)<.1f,"camera pan keeps Space offset without rounding jumps");
                    }
                }
            }
            yield return Load("Naru");
            var delivery=gm.Db.quests.Find(q=>q.id=="q1_naru");Check(delivery!=null,"existing first delivery quest present");
            gm.Quests.Accept(delivery);bool accepted=false;foreach(var progress in gm.Quests.Active)if(progress.quest==delivery)accepted=true;
            Check(accepted,"real first delivery accepted in SaveOFF slot for question-mark render");
            yield return Load("goryeo_jangdan");gm.Clock.minuteOfDay=720;
            QuestNpc recipient=null;foreach(var npc in FindObjectsByType<QuestNpc>(FindObjectsSortMode.None))if(npc.npcId=="goryeo_jangdan_farmer")recipient=npc;
            Check(recipient!=null && gm.Quests.MarkerFor(recipient.npcId)==QuestMarker.TurnIn,"actual delivery recipient ready to turn in");
            var player=PlayerController.Instance;var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;
            camera.transform.position=recipient.transform.position+Vector3.down*2+new Vector3(0,0,-10);
            player.Motor.Teleport(recipient.transform.position+Vector3.down*2-(Vector3)player.Motor.feetOffset,Vector2Int.up);
            yield return Frames();var readyLabels=MapRoot.Current.GetComponent<GoryeoPlaceLabels>();
            Check(readyLabels.QuestBadgeStates.TryGetValue(recipient,out var readyState)&&readyState==QuestMarker.TurnIn,"actual ready question mark rendered next to recipient name");
            CheckFixed(readyLabels,"actual delivery ready");yield return Photo("quest-ready-question");
            Check(promptComparisons>10,"many same-target Space comparisons executed count="+promptComparisons);
            Check(changedTargets>0,"interaction target changes exercised count="+changedTargets);
            Check(!gm.AutosaveEnabled&&!File.Exists(slotPath),"no save writes in fixed labels review");
        }
    }
}
#endif
