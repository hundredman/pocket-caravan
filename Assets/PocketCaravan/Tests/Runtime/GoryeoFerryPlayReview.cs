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
    public sealed class GoryeoFerryPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Ferry-20261105/Unity/";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        public bool production;readonly List<string> temporarySaves=new List<string>();
        readonly Report r=new Report();GoryeoFerry.Config config;
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        DialogueUI Dialogue=>UnityEngine.Object.FindFirstObjectByType<DialogueUI>();
        List<DialogueUI.Choice> Choices=>(List<DialogueUI.Choice>)typeof(DialogueUI).GetField("choices",Flags).GetValue(Dialogue);
        void Check(bool ok,string text){if(!ok)throw new Exception(text);r.checks.Add(text);}
        void Close(){typeof(DialogueUI).GetMethod("Close",Flags).Invoke(Dialogue,null);UIState.Blocking=false;Notify.Clear();}
        void Pick(int index){typeof(DialogueUI).GetMethod("Pick",Flags).Invoke(Dialogue,new object[]{Choices[index]});}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object value=null;Exception error=null;try{n=stack.Peek().MoveNext();if(n)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!n){stack.Pop();continue;}if(value is IEnumerator)stack.Push((IEnumerator)value);else yield return value;}Finish(null);}
        void Finish(Exception e){if(production&&GameManager.I!=null)typeof(GameManager).GetField("autosave",Flags).SetValue(GameManager.I,false);foreach(var path in temporarySaves)foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+(production?"production.json":"play.json"),JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        void Prepare()
        {
            foreach(var guide in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){guide.StopAllCoroutines();guide.enabled=false;}foreach(var guide in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){guide.StopAllCoroutines();guide.enabled=false;}
            Close();PlayerController.Instance.enabled=false;Time.timeScale=0;
        }
        GoryeoFerry.Port Port=>Array.Find(config.ports,p=>p.mapId==MapRoot.Current.mapId);
        TalkNpc Boatman(){var port=Port;foreach(var npc in UnityEngine.Object.FindObjectsByType<TalkNpc>(FindObjectsSortMode.None))if(MapRoot.Current.WorldToCell(npc.transform.position)==new Vector3Int(port.npcX,port.height-port.npcY-1,0))return npc;throw new Exception("missing original boatman");}
        void Frame()
        {
            var port=Port;var map=MapRoot.Current;var pc=PlayerController.Instance;var cell=new Vector3Int(port.arrivalX,port.height-port.arrivalY-1,0);pc.Motor.Teleport(map.CellCenter(cell)-(Vector3)pc.Motor.feetOffset,Vector2Int.right);
            var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=map.CellCenter(new Vector3Int(port.npcX+2,port.height-port.npcY-2,0))+new Vector3(0,0,-10);
        }
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==480&&t.height==270,"actual480x270 "+name);File.WriteAllBytes(Out+(production?"production-":"")+name+".png",t.EncodeToPNG());Destroy(t);}
        IEnumerator DirectLoad(string path){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+path+".unity",new LoadSceneParameters(LoadSceneMode.Single));for(int n=0;n<8;n++)yield return null;Prepare();}
        IEnumerator Travel(string target)
        {
            var gm=GameManager.I;var before=MapRoot.Current.mapId;var source=Boatman();var pc=PlayerController.Instance;Frame();Close();int money=gm.Player.money;float hours=gm.Clock.TotalHours;string cargo=JsonUtility.ToJson(gm.Player.cargo);string checkpoint=null;if(production){Check(gm.SaveNow(),"safe separate-slot source checkpoint "+before);checkpoint=Convert.ToBase64String(File.ReadAllBytes(SaveSystem.CurrentPath));}var vehicle=gm.Player.vehicle;var origin=gm.Player.origin;int count=GoryeoFerry.CompletedCount;
            string error=GoryeoFerry.TryStart(source,pc,target);Check(error==null,"real transition request "+before+" -> "+target);Check(gm.Player.money==money&&Mathf.Abs(gm.Clock.TotalHours-hours)<.0001f,"no payment/time before arrival "+target);if(production)Check(checkpoint==Convert.ToBase64String(File.ReadAllBytes(SaveSystem.CurrentPath)),"source save unchanged at departure "+target);Check(GoryeoFerry.TryStart(source,pc,target)!=null,"duplicate transition rejected "+target);
            float until=Time.realtimeSinceStartup+45;while(gm.IsTravelling&&Time.realtimeSinceStartup<until)yield return null;Check(!gm.IsTravelling,"real GameManager transition completed "+target);for(int n=0;n<4;n++)yield return null;
            Check(MapRoot.Current.mapId==target&&MapRoot.Current.previewOnly==!production&&gm.AutosaveEnabled==production,"correct destination and saving mode "+target);Check(GoryeoFerry.LastError==""&&GoryeoFerry.CompletedCount==count+1,"one arrival commit "+target);Check(gm.Player.money==money-config.fare&&Mathf.Abs(gm.Clock.TotalHours-hours-config.hours)<.001f,"exact one fare/hour after arrival "+target);
            var port=Port;var cell=new Vector3Int(port.arrivalX,port.height-port.arrivalY-1,0);Check(pc==null||PlayerController.Instance!=null,"new scene player exists");Check(PlayerController.Instance.Motor.Cell==cell&&MapRoot.Current.IsWalkable(cell),"spawn registered before shared arrival callback "+target);Check(SpawnPoint.Find(GoryeoFerry.SpawnId(target))!=null,"new unique arrival point "+target);
            if(production)
            {
                var saved=SaveSystem.Read();Check(saved!=null&&saved.scene==target&&saved.money==gm.Player.money&&Mathf.Abs((saved.day-1)*24+saved.minute/60-hours-config.hours)<.001f,"real saved destination fare and clock "+target);
                Check(Vector2.Distance(new Vector2(saved.px,saved.py),PlayerController.Instance.transform.position)<.001f&&saved.vehicle==gm.Player.vehicle.id,"saved arrival position and vehicle "+target);
            }
            Check(cargo==JsonUtility.ToJson(gm.Player.cargo)&&vehicle==gm.Player.vehicle&&origin==gm.Player.origin,"cargo vehicle origin retained "+target);Prepare();Frame();yield return Photo(target+"-arrival-"+count);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);Check(!GameManager.I.AutosaveEnabled,"safe bootstrap saves off");Time.timeScale=0;Check(!GoryeoFerry.ReviewEnabled,"no explicit review mode");config=JsonUtility.FromJson<GoryeoFerry.Config>(File.ReadAllText("ArtReview/Goryeo-Ferry-20261105/source.json"));
            yield return DirectLoad("Goryeo/Naru");Frame();var source=Boatman();int people=UnityEngine.Object.FindObjectsByType<TalkNpc>(FindObjectsSortMode.None).Length;
            if(production)
            {
                Check(GoryeoFerry.Approved&&!MapRoot.Current.previewOnly,"approved automatic production installation");
                string slot="codex_ferry_qa_"+Guid.NewGuid().ToString("N")+".json";SaveSystem.UseSlot(slot);Check(SaveSystem.UsingSeparateSlot&&SaveSystem.FileName==slot,"separate disposable save selected");temporarySaves.Add(SaveSystem.CurrentPath);typeof(GameManager).GetField("autosave",Flags).SetValue(GameManager.I,true);Check(GameManager.I.SaveNow(),"initial actual save-on checkpoint");
            }
            else
            {
                MapRoot.Current.previewOnly=true;source.Interact(PlayerController.Instance);yield return null;Check(Choices.Count==0,"original conversation before opt-in");yield return Photo("Naru-original");Close();GoryeoFerry.EnableReview(config);
            }
            source.Interact(PlayerController.Instance);yield return null;Check(Choices.Count==4&&((string)typeof(DialogueUI).GetField("text",Flags).GetValue(Dialogue)).Contains("8냥")&&((string)typeof(DialogueUI).GetField("text",Flags).GetValue(Dialogue)).Contains("1시간"),"proposed route/price/time visible");yield return Photo("Naru-menu");Check(UnityEngine.Object.FindObjectsByType<TalkNpc>(FindObjectsSortMode.None).Length==people&&source.isActiveAndEnabled,"no duplicate/disabled original NPC");
            int money=GameManager.I.Player.money;float time=GameManager.I.Clock.TotalHours;Pick(3);Check(GameManager.I.Player.money==money&&GameManager.I.Clock.TotalHours==time&&!GameManager.I.IsTravelling,"cancel unchanged");source.Interact(PlayerController.Instance);Pick(2);yield return null;Check(Choices.Count==0,"original story still reachable");Close();
            GameManager.I.Player.money=0;Check(GoryeoFerry.TryStart(source,PlayerController.Instance,"transit_ferry")!=null&&!GoryeoFerry.Pending&&GameManager.I.Clock.TotalHours==time,"insufficient money no charge/no time/no pending");GameManager.I.Player.money=500;
            Check(GoryeoFerry.TryStart(source,PlayerController.Instance,"no_such_map")!=null&&!GoryeoFerry.Pending,"unknown destination rejected");var pos=PlayerController.Instance.transform.position;PlayerController.Instance.Motor.Teleport(pos+Vector3.left*5,Vector2Int.right);Check(GoryeoFerry.TryStart(source,PlayerController.Instance,"transit_ferry")!=null,"remote boarding rejected");Frame();
            if(production)
            {
                typeof(SaveSystem).GetProperty("LoadBlocked",BindingFlags.Public|BindingFlags.Static).SetValue(null,true);Check(GoryeoFerry.TryStart(source,PlayerController.Instance,"transit_ferry")!=null&&!GoryeoFerry.Pending,"failed source save rejects departure");typeof(SaveSystem).GetProperty("LoadBlocked",BindingFlags.Public|BindingFlags.Static).SetValue(null,false);
                var target=GoryeoFerry.Find("transit_ferry");int height=target.height;target.height++;Check(GoryeoFerry.TryStart(source,PlayerController.Instance,"transit_ferry")!=null&&!GoryeoFerry.Pending,"mismatched destination rejected before travel");target.height=height;
            }
            var cart=AssetDatabase.LoadAssetAtPath<VehicleData>("Assets/PocketCaravan/Data/Goryeo/Vehicle_Handcart.asset");var goods=GameManager.I.ItemById("paper");Check(cart!=null&&goods!=null&&goods.weight>0,"existing handcart and paper fixture");GameManager.I.Player.vehicle=cart;GameManager.I.Player.cargo.Clear();GameManager.I.Player.cargo.Add(goods,Mathf.FloorToInt(cart.capacity/goods.weight),20);Check(GameManager.I.Player.LoadRatio>=.9f,"loaded handcart >=90percent for all six trips");
            foreach(var target in new[]{"transit_ferry","transit_southport","Naru","transit_southport","transit_ferry","Naru"}){yield return Travel(target);source=Boatman();source.Interact(PlayerController.Instance);yield return Photo(MapRoot.Current.mapId+"-menu-"+GoryeoFerry.CompletedCount);Close();}
            Check(GoryeoFerry.CompletedCount==6,"all six directed trips using shared transition");Check(GameManager.I.AutosaveEnabled==production,"correct saving mode at end of trips");
            if(production)
            {
                foreach(var fieldName in new[]{"owner","slot"})
                {
                    Frame();Close();int balance=GameManager.I.Player.money;float clock=GameManager.I.Clock.TotalHours;int count=GoryeoFerry.CompletedCount;var destination=MapRoot.Current.mapId=="Naru"?"transit_ferry":"Naru";
                    Check(GoryeoFerry.TryStart(Boatman(),PlayerController.Instance,destination)==null,"guard injection started real travel "+fieldName);
                    var voyage=typeof(GoryeoFerry).GetField("pending",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);voyage.GetType().GetField(fieldName).SetValue(voyage,fieldName=="slot"?(object)"codex_other_profile.json":null);
                    float end=Time.realtimeSinceStartup+45;while(GameManager.I.IsTravelling&&Time.realtimeSinceStartup<end)yield return null;
                    Check(!GameManager.I.IsTravelling&&!GoryeoFerry.Pending&&GoryeoFerry.CompletedCount==count&&GoryeoFerry.LastError!="","stale request discarded "+fieldName);
                    Check(GameManager.I.Player.money==balance&&Mathf.Abs(GameManager.I.Clock.TotalHours-clock)<.001f,"stale request no money/time charge "+fieldName);var saved=SaveSystem.Read();Check(saved.money==balance,"stale request saved without charge "+fieldName);Prepare();
                }
                var map=MapRoot.Current;var port=Port;var primary=new Vector3Int(port.arrivalX,port.height-port.arrivalY-1,0);var oldTile=map.blocked.GetTile(primary);var testTile=ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                try{map.blocked.SetTile(primary,testTile);typeof(GoryeoFerry).GetMethod("Install",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{map});var spawn=SpawnPoint.Find(GoryeoFerry.SpawnId(map.mapId));var alternative=map.WorldToCell(spawn.transform.position+(Vector3)PlayerController.Instance.Motor.feetOffset);Check(alternative!=primary&&map.IsWalkable(alternative)&&MapExit.At(alternative)==null,"blocked primary uses safe adjacent landing");}
                finally{map.blocked.SetTile(primary,oldTile);Destroy(testTile);typeof(GoryeoFerry).GetMethod("Install",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{map});}
            }
            foreach(var path in new[]{"Goryeo/Gaegyeong","Venice/venice_rialto","Arabia/arabia_ahsa","North/north_birka"}){yield return DirectLoad(path);foreach(var npc in UnityEngine.Object.FindObjectsByType<TalkNpc>(FindObjectsSortMode.None)){Check(!GoryeoFerry.TryInteract(npc,PlayerController.Instance),"unrelated NPC untouched "+path);break;}Check(SpawnPoint.Find(GoryeoFerry.SpawnId(MapRoot.Current.mapId))==null,"no ferry arrival in unrelated map "+path);}
        }
    }
}
#endif
