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
    public sealed class SocialGoryeoPlayReview:MonoBehaviour
    {
        [Serializable] class Result{public bool complete;public string error;public List<string> checks=new List<string>();}
        const string Out="Docs/Validation/Social-Task1-2026-11-04";
        readonly Result result=new Result();Keyboard keyboard,previous;
        void Check(bool value,string name){if(!value)throw new Exception(name);result.checks.Add(name);}
        void Finish(Exception error)
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(previous!=null&&previous.added)previous.MakeCurrent();
            result.complete=error==null;result.error=error==null?"":error.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(result,true));
            Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;
        }
        IEnumerator Start()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){bool next=false;object item=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){Finish(error);yield break;}if(!next){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}
            Finish(null);
        }
        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator Photo(string name)
        {
            yield return null;yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            Check(image.width==960&&image.height==540,"960x540 capture "+name);File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());Destroy(image);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<12;i++)yield return null;
            var gm=GameManager.I;var map=MapRoot.Current;var player=PlayerController.Instance;
            Check(map.previewOnly&&!gm.AutosaveEnabled,"save-isolated preview");gm.Clock.day=1;gm.Clock.minuteOfDay=720;Time.timeScale=0;
            foreach(var guide in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))guide.enabled=false;
            foreach(var intro in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){intro.StopAllCoroutines();intro.enabled=false;}
            foreach(var dialogue in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(dialogue,null);
            UIState.Blocking=false;Notify.Clear();
            var social=UnityEngine.Object.FindFirstObjectByType<Social>();Check(social!=null,"Social installed");
            var target=new List<Interactable>(Interactable.All).Find(i=>i.DisplayName=="땔감 장수");Check(target!=null,"real talker display name matches friend");
            bool found=false;
            foreach(var dir in new[]{Vector2Int.down,Vector2Int.up,Vector2Int.left,Vector2Int.right})
            {
                Vector3 p=target.transform.position+(Vector3)(Vector2)dir*1.35f;
                if(!map.IsWalkable(map.WorldToCell(p+(Vector3)player.Motor.feetOffset)))continue;
                player.Motor.Teleport(p,-dir);
                if(Interactable.Nearest(p,-dir,player.talkRange)==target){found=true;break;}
            }
            Check(found,"walkable interaction position");
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;camera.transform.position=new Vector3(player.transform.position.x,player.transform.position.y,-10);
            previous=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            gm.Player.cargo.Clear();var salt=gm.ItemById("salt");Check(salt!=null,"salt registered");gm.Player.cargo.Add(salt,1,10);
            foreach(var key in new[]{"pts","lv","talk","gift","keepsake"})gm.ClearMemo("social:Goryeo:땔감 장수:"+key);
            for(int i=0;i<4;i++)yield return null;Check(player.TalkTarget==target,"PlayerController selects real friend");
            yield return Press(Key.Space);Check(Social.Points("땔감 장수")==1,"Space talk awards one point");yield return Photo("talk");
            yield return Press(Key.Escape);for(int i=0;i<3;i++)yield return null;
            Check(!UIState.Blocking,"dialogue closed");yield return Press(Key.G);
            Check((bool)typeof(Social).GetField("picking",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(social),"real G key opens gift picker");yield return Photo("gift-picker");
            yield return Press(Key.Space);Check(gm.Player.cargo.Count(salt)==0,"one salt consumed by gift confirmation");Check(Social.Points("땔감 장수")==6,"approved salt is liked gift plus five points");Check(Social.Hearts("땔감 장수")==1,"first-heart dialogue reached");yield return Photo("gift-result");
            Check(!gm.AutosaveEnabled,"autosave remains off");
        }
    }
}
#endif
