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
    public sealed class GoryeoWorkshopTrialPlayReview:MonoBehaviour
    {
        [Serializable] class Result{public bool complete;public string error;public List<string> checks=new List<string>();}
        const string Out="ArtReview/GoryeoWorkshop-Task3-20261104/Unity";
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
        IEnumerator CloseDialog()
        {
            for(int i=0;i<12&&UIState.Blocking;i++)yield return Press(Key.Escape);
            for(int i=0;i<3;i++)yield return null;
        }
        IEnumerator Choices()
        {
            var d=UnityEngine.Object.FindFirstObjectByType<DialogueUI>();
            for(int i=0;i<8&&!d.ChoicesVisible;i++)yield return Press(Key.Space);
            Check(d.ChoicesVisible,"dialogue choices reached");
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<12;i++)yield return null;
            var gm=GameManager.I;var map=MapRoot.Current;var player=PlayerController.Instance;
            Check(map.previewOnly&&!gm.AutosaveEnabled,"save-isolated workshop preview");Time.timeScale=0;gm.Clock.day=1;gm.Clock.minuteOfDay=360;gm.Player.money=1000;gm.Player.cargo.Clear();
            foreach(var guide in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))guide.enabled=false;
            foreach(var intro in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){intro.StopAllCoroutines();intro.enabled=false;}
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);
            UIState.Blocking=false;Notify.Clear();
            var spot=UnityEngine.Object.FindFirstObjectByType<GoryeoWorkshopTrialSpot>();var recipe=Crafting.Find(GoryeoWorkshopTrialSpot.Id);Check(spot!=null&&recipe!=null,"preview recipe installed");
            Check(gm.ItemById(recipe.product)!=null&&recipe.inputs.Length==0,"registered paper; workshop supplies materials");Check(!spot.blocksMovement&&map.IsWalkable(map.WorldToCell(spot.transform.position)),"workshop entrance remains walkable");
            var paper=gm.ItemById("paper");var town=gm.Db.towns.Find(t=>t.id=="dakmok");
            player.Motor.Teleport(spot.transform.position+Vector3.down-(Vector3)player.Motor.feetOffset,Vector2Int.up);
            Check(map.IsWalkable(player.Motor.Cell),"player approach remains walkable");
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;camera.transform.position=new Vector3(41.5f,27.5f,-10);
            previous=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            spot.enabled=false;for(int i=0;i<3;i++)yield return null;yield return Photo("placement-before");spot.enabled=true;for(int i=0;i<3;i++)yield return null;
            Check(player.TalkTarget==spot,"real PlayerController targets workshop entrance");yield return Photo("placement-after");
            yield return Press(Key.Space);yield return Choices();int buy=gm.GetMarket(town).BuyPrice(paper),fee=spot.LastQuotedFee;
            Check(fee>=buy+5&&fee>=18,"commission more expensive than direct local purchase");yield return Photo("order-dialogue");
            int money=gm.Player.money;yield return Press(Key.Space);Check(Crafting.State(recipe.id)==1&&Crafting.Batches(recipe.id)==1&&gm.Player.money==money-fee,"order charges quote and waits six hours");
            yield return Press(Key.Space);yield return Photo("working-dialogue");yield return CloseDialog();gm.Clock.Advance(6);Check(Crafting.State(recipe.id)==2,"ready after six game hours");
            yield return Press(Key.Space);yield return Choices();yield return Photo("collect-dialogue");yield return Press(Key.Space);
            Check(gm.Player.cargo.Count(paper)==1&&Mathf.Abs(gm.Player.cargo.AverageCost(paper)-fee)<.001f&&Crafting.State(recipe.id)==0,"paper collected with paid cost recorded");
            yield return Press(Key.Space);yield return Choices();yield return Press(Key.Space);gm.Clock.Advance(6);yield return Press(Key.Space);yield return Choices();yield return Press(Key.Space);
            Check(Crafting.DoneForToday(recipe.id),"two orders exhaust daily limit");money=gm.Player.money;yield return Press(Key.Space);Check(gm.Player.money==money&&Crafting.Batches(recipe.id)==0,"third order blocked without charge");yield return CloseDialog();
            gm.Clock.day++;gm.Clock.minuteOfDay=360;yield return Press(Key.Space);yield return Choices();yield return Press(Key.Space);
            Check(Crafting.Batches(recipe.id)==1&&gm.GetMemo("craft:"+recipe.id+":cnt")==1,"new day starts at one order instead of carrying yesterday count");
            gm.Clock.Advance(6);gm.Player.cargo.Clear();gm.Player.cargo.Add(gm.ItemById("firewood"),1000,4);yield return Press(Key.Space);yield return Choices();yield return Press(Key.Space);
            Check(Crafting.Batches(recipe.id)==1,"full cargo leaves finished paper at workshop");gm.Player.cargo.Clear();yield return Press(Key.Space);yield return Choices();yield return Press(Key.Space);Check(gm.Player.cargo.Count(paper)==1&&Crafting.Batches(recipe.id)==0,"paper recoverable after clearing cargo");
            Check(!gm.AutosaveEnabled,"autosave stays off");
        }
    }
}
#endif
