#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class GoryeoSeatPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Seats-20261105/Unity/";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report report=new Report();
        void Check(bool ok,string s){if(!ok)throw new Exception(s);report.checks.Add(s);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool more=false;object value=null;Exception error=null;
                try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){Finish(error);yield break;}if(!more){stack.Pop();continue;}
                if(value is IEnumerator)stack.Push((IEnumerator)value);else yield return value;
            }
            Finish(null);
        }
        void Finish(Exception e)
        {
            foreach(var seat in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))seat.Stand();
            report.complete=e==null;report.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);
            File.WriteAllText(Out+"play.json",JsonUtility.ToJson(report,true));Time.timeScale=1;EditorApplication.isPlaying=false;
        }
        void Prepare()
        {
            foreach(var g in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
            foreach(var g in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
            var ui=FindFirstObjectByType<DialogueUI>();typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
            UIState.Blocking=false;Notify.Clear();Time.timeScale=0;
        }
        IEnumerator Photo(string name)
        {
            Notify.Clear();yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();
            Check(t.width==480&&t.height==270,"actual 480x270 "+name);File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());Destroy(t);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);Check(!GameManager.I.AutosaveEnabled,"safe preview bootstrap saves OFF");
            Check(FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==0,"no automatic production seats");
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/Transit/transit_central_approach.unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<8;i++)yield return null;Prepare();Check(!GameManager.I.AutosaveEnabled,"saves remain OFF after scene load");
            var map=MapRoot.Current;var pc=PlayerController.Instance;var gm=GameManager.I;
            // Only the unrelated obstacle debug panel is hidden for both comparison columns.
            var obstaclePanel=map.GetComponent<ObstacleField>();if(obstaclePanel!=null)obstaclePanel.enabled=false;
            gm.Player.origin=AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Goryeo/Origin_Goryeo.asset");
            var config=JsonUtility.FromJson<GoryeoSeat.Config>(File.ReadAllText("ArtReview/Goryeo-Seats-20261105/source.json"));
            var t=new Texture2D(16,24,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes("ArtReview/Goryeo-Seats-20261105/tanuki-seated-front.png"));t.filterMode=FilterMode.Point;
            var pose=Sprite.Create(t,new Rect(0,0,16,24),new Vector2(.5f,.25f),16);
            bool rejected=false;try{GoryeoSeat.InstallReview(config,pose);}catch(InvalidOperationException){rejected=true;}Check(rejected,"production map refuses explicit review install");map.previewOnly=true;
            Check(GoryeoSeat.InstallReview(config,pose)==3,"three furniture candidates walkable/no exits");
            Check(GoryeoSeat.InstallReview(config,pose)==3&&FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==3,"idempotent reinstall");
            var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;
            foreach(var seat in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))
            {
                pc.Motor.Teleport(seat.transform.position+Vector3.down*.5f,Vector2Int.up);cam.transform.position=seat.transform.position+Vector3.up*2+new Vector3(0,0,-10);
                Vector3 standing=pc.transform.position;yield return null;
                var scale=pc.anim.transform.localScale;int money=gm.Player.money;string cargo=JsonUtility.ToJson(gm.Player.cargo);var vehicle=gm.Player.vehicle;
                foreach(var candidate in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))foreach(var prop in candidate.GetComponentsInChildren<SpriteRenderer>(true))prop.enabled=false;
                yield return Photo(seat.site.id+"-before");
                foreach(var candidate in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))foreach(var prop in candidate.GetComponentsInChildren<SpriteRenderer>(true))prop.enabled=true;
                seat.Interact(pc);yield return new WaitForEndOfFrame();Check(GoryeoSeat.Sitting(pc)&&!UIState.Blocking,"seat without global lock "+seat.site.id);
                Check(seat.ActionVerb=="일어나기","context action becomes stand "+seat.site.id);
                Check(pc.Motor.Velocity==Vector2.zero&&map.IsWalkable(pc.Motor.Cell)&&MapExit.At(pc.Motor.Cell)==null,"safe stationary feet "+seat.site.id);
                Check(pc.anim.GetComponent<SpriteRenderer>().sprite==pose&&pc.anim.transform.localScale==scale,"native pose / no scaling "+seat.site.id);
                yield return Photo(seat.site.id+"-after");
                var regen=typeof(PlayerController).GetMethod("UpdateCarry",BindingFlags.Instance|BindingFlags.NonPublic);gm.Player.stamina=.4f;regen.Invoke(pc,new object[]{1f});float seatedStamina=gm.Player.stamina;
                Check(GoryeoSeat.Handle(pc,Vector2.right,false,false)&&!GoryeoSeat.Sitting(pc),"movement stands "+seat.site.id);
                gm.Player.stamina=.4f;regen.Invoke(pc,new object[]{1f});Check(Mathf.Abs(gm.Player.stamina-seatedStamina)<.0001f,"ordinary idle stamina unchanged "+seat.site.id);
                Check(Vector3.Distance(standing,pc.transform.position)<.001f,"return original safe position "+seat.site.id);
                seat.Interact(pc);Check(GoryeoSeat.Handle(pc,Vector2.zero,true,false)&&!GoryeoSeat.Sitting(pc),"Space stands "+seat.site.id);
                seat.Interact(pc);Check(GoryeoSeat.Handle(pc,Vector2.zero,false,true)&&!GoryeoSeat.Sitting(pc),"UI/travel lock stands "+seat.site.id);
                seat.Interact(pc);seat.enabled=false;Check(!GoryeoSeat.Sitting(pc)&&pc.anim.transform.localScale==scale,"disable releases visual and seat "+seat.site.id);seat.enabled=true;
                pc.Motor.Teleport(standing+Vector3.left*5,Vector2Int.right);seat.Interact(pc);Check(!GoryeoSeat.Sitting(pc),"remote sitting rejected "+seat.site.id);
                pc.Motor.Teleport(standing,Vector2Int.up);var origin=gm.Player.origin;gm.Player.origin=AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Origins/Origin_Venice.asset");seat.Interact(pc);Check(!GoryeoSeat.Sitting(pc),"unsupported origin retains original art "+seat.site.id);gm.Player.origin=origin;
                Check(gm.Player.money==money&&JsonUtility.ToJson(gm.Player.cargo)==cargo&&gm.Player.vehicle==vehicle&&!gm.AutosaveEnabled,"money cargo vehicle saving unchanged "+seat.site.id);
            }
            var last=FindFirstObjectByType<GoryeoSeat>();pc.Motor.Teleport(last.transform.position,Vector2Int.down);last.Interact(pc);Check(GoryeoSeat.Sitting(pc),"sitting before real scene unload");
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/Goryeo/Naru.unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<8;i++)yield return null;Prepare();Check(!GoryeoSeat.Sitting(PlayerController.Instance)&&FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==0,"scene unload releases without leaked seats");
            Check(!gm.AutosaveEnabled,"saves OFF at end");
        }
    }
}
#endif
