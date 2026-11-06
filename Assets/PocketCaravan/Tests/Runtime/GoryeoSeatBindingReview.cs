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
    public sealed class GoryeoSeatBindingReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Seats-Binding-20261106/";
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();public List<int> pixelDifferences=new List<int>();}
        readonly Report report=new Report();string slotPath;Texture2D last;
        void Check(bool b,string s){if(!b)throw new Exception(s);report.checks.Add(s);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){bool more=false;object v=null;Exception error=null;try{more=stack.Peek().MoveNext();if(more)v=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!more){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);
        }
        void Finish(Exception e)
        {
            if(GameManager.I!=null)typeof(GameManager).GetField("autosave",Flags).SetValue(GameManager.I,false);
            if(slotPath!=null)foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(slotPath+suffix))File.Delete(slotPath+suffix);
            report.complete=e==null;report.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"production.json",JsonUtility.ToJson(report,true));Time.timeScale=1;EditorApplication.isPlaying=false;
        }
        void Prepare()
        {
            foreach(var g in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
            foreach(var g in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
            var d=FindFirstObjectByType<DialogueUI>();typeof(DialogueUI).GetMethod("Close",Flags).Invoke(d,null);UIState.Blocking=false;Notify.Clear();Time.timeScale=0;
            var field=MapRoot.Current.GetComponent<ObstacleField>();if(field!=null)field.enabled=false;
        }
        IEnumerator Load(string name)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<10;i++)yield return null;Prepare();
            if(slotPath!=null)Check(SaveSystem.UsingSeparateSlot&&SaveSystem.CurrentPath==slotPath,"disposable slot retained after load "+name);
        }
        GoryeoSeat Find(string id){foreach(var s in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))if(s.site.id==id)return s;throw new Exception("missing seat "+id);}
        void Frame(GoryeoSeat s)
        {
            var pc=PlayerController.Instance;pc.Motor.Teleport(s.transform.position,Vector2Int.down);
            var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=s.transform.position+Vector3.up*2+new Vector3(0,0,-10);
            GameManager.I.Player.stamina=1;GameManager.I.Clock.day=1;GameManager.I.Clock.minuteOfDay=12*60;
        }
        IEnumerator Photo(string name)
        {
            // Freeze only this test's flame at a shared phase for pixel comparisons.
            foreach(var f in FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None)){f.enabled=false;typeof(GoryeoCampfire).GetField("elapsed",Flags).SetValue(f,-Time.unscaledDeltaTime);typeof(GoryeoCampfire).GetMethod("Update",Flags).Invoke(f,null);}
            Notify.Clear();yield return null;yield return new WaitForEndOfFrame();last=ScreenCapture.CaptureScreenshotAsTexture();Check(last.width==480&&last.height==270,"actual480x270 "+name);File.WriteAllBytes(Out+name+".png",last.EncodeToPNG());
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);Check(!GameManager.I.AutosaveEnabled,"bootstrap saves OFF");
            yield return Load("Transit/transit_central_approach");var gm=GameManager.I;
            Check(FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==3&&FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None).Length==1,"automatic production installation: three seats one fire");
            string slot="codex_seat_qa_"+Guid.NewGuid().ToString("N")+".json";SaveSystem.UseSlot(slot);slotPath=SaveSystem.CurrentPath;typeof(GameManager).GetField("autosave",Flags).SetValue(gm,true);Check(gm.SaveNow(),"actual save ON in disposable slot");
            Frame(Find("north_fire"));var liveFire=FindFirstObjectByType<GoryeoCampfire>();var liveFrames=new HashSet<int>();
            for(int i=0;i<6;i++){yield return new WaitForSecondsRealtime(.12f);liveFrames.Add(liveFire.FrameIndex);}
            Check(liveFire.isActiveAndEnabled&&liveFire.Visible&&liveFrames.Count>1&&liveFire.VisibleEmbers<=3&&liveFire.LightIntensity>0,"approved production flame/embers/light animate with actual saving ON");
            var cfg=JsonUtility.FromJson<GoryeoSeat.Config>(Resources.Load<TextAsset>("Goryeo/Seats/Config").text);var art=new GoryeoSeat.Art[4];var fire=new Sprite[4];for(int i=0;i<4;i++){art[i]=new GoryeoSeat.Art{originId=cfg.poses[i].originId,sprite=Resources.Load<Sprite>(cfg.poses[i].file)};fire[i]=Resources.Load<Sprite>("Goryeo/Seats/Fire/"+i);}
            Check(GoryeoSeat.InstallApproved(cfg,art,fire)==0&&FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==3,"duplicate production install has no effect");
            foreach(string originId in new[]{"goryeo","venice","viking","arabia"})
            {
                string file=originId=="goryeo"?"Assets/PocketCaravan/Data/Goryeo/Origin_Goryeo.asset":"Assets/PocketCaravan/Data/Origins/Origin_"+(originId=="venice"?"Venice":originId=="viking"?"Viking":"Arabia")+".asset";
                gm.Player.origin=AssetDatabase.LoadAssetAtPath<OriginData>(file);for(int i=0;i<3;i++)yield return null;int money=gm.Player.money;string cargo=JsonUtility.ToJson(gm.Player.cargo);
                foreach(var seat in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))
                {
                    Frame(seat);var pc=PlayerController.Instance;Vector3 pos=pc.transform.position;Vector3 scale=pc.anim.transform.localScale;seat.Interact(pc);yield return new WaitForEndOfFrame();
                    Check(seat.approvedForProduction&&GoryeoSeat.Sitting(pc)&&pc.anim.GetComponent<SpriteRenderer>().sprite==seat.PoseFor(gm.Player.origin),"approved identity "+originId+seat.site.id);
                    Check(pc.anim.transform.localScale==scale&&MapRoot.Current.IsWalkable(pc.Motor.Cell,pc.Motor)&&pc.Motor.Velocity==Vector2.zero,"native scale safe stationary feet "+originId+seat.site.id);
                    Check(gm.SaveNow(),"actual seated save "+originId+seat.site.id);var saved=JsonUtility.FromJson<SaveData>(File.ReadAllText(slotPath));Check(saved.scene=="transit_central_approach"&&Mathf.Abs(saved.px-pos.x)<.001f&&Mathf.Abs(saved.py-pos.y)<.001f&&saved.origin==originId&&saved.money==money,"save contains safe physical root and unchanged origin/money "+originId+seat.site.id);
                    var regen=typeof(PlayerController).GetMethod("UpdateCarry",Flags);gm.Player.stamina=.4f;regen.Invoke(pc,new object[]{1f});float seated=gm.Player.stamina;GoryeoSeat.Handle(pc,Vector2.right,false,false);gm.Player.stamina=.4f;regen.Invoke(pc,new object[]{1f});
                    Check(!GoryeoSeat.Sitting(pc)&&Mathf.Abs(gm.Player.stamina-seated)<.0001f&&Vector3.Distance(pc.transform.position,pos)<.001f,"same ordinary regen and movement release "+originId+seat.site.id);
                    seat.Interact(pc);GoryeoSeat.Handle(pc,Vector2.zero,true,false);Check(!GoryeoSeat.Sitting(pc)&&!UIState.Blocking,"Space release without UI lock "+originId+seat.site.id);
                    if(originId=="goryeo"){Frame(seat);seat.Interact(pc);yield return Photo("production-"+seat.site.id);Destroy(last);seat.Stand();}
                }
                Check(gm.Player.money==money&&JsonUtility.ToJson(gm.Player.cargo)==cargo,"cargo and money retained "+originId);
                // Compare imported production pixels to the raw approved review drawing at identical settings.
                var site=Find("north_fire");Frame(site);site.Interact(PlayerController.Instance);yield return Photo("production-"+originId);var imported=last;site.Stand();
                typeof(GameManager).GetField("autosave",Flags).SetValue(gm,false);MapRoot.Current.previewOnly=true;
                var rawConfig=JsonUtility.FromJson<GoryeoSeat.Config>(File.ReadAllText("ArtReview/Goryeo-Seats-20261105/AlignedV2/source.json"));var raw=new List<GoryeoSeat.Art>();
                foreach(var p in rawConfig.poses){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(p.file));t.filterMode=FilterMode.Point;raw.Add(new GoryeoSeat.Art{originId=p.originId,sprite=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,p.pivotY),p.ppu)});}
                GoryeoSeat.InstallReview(rawConfig,raw.ToArray());site=Find("north_fire");Frame(site);site.Interact(PlayerController.Instance);yield return Photo("review-"+originId);
                var a=imported.GetPixels32();var b=last.GetPixels32();int diff=0;for(int y=91;y<126;y++)for(int x=228;x<253;x++)if(!a[y*480+x].Equals(b[y*480+x]))diff++;report.pixelDifferences.Add(diff);Check(diff==0,"approved review/production character pixels identical "+originId);Destroy(imported);Destroy(last);site.Stand();
                foreach(var s in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))DestroyImmediate(s.gameObject);MapRoot.Current.previewOnly=false;Check(GoryeoSeat.InstallApproved(cfg,art,fire)==3,"restore approved automatic content "+originId);typeof(GameManager).GetField("autosave",Flags).SetValue(gm,true);
            }
            var actor=PlayerController.Instance;Frame(Find("north_fire"));Find("north_fire").Interact(actor);actor.enabled=false;yield return null;yield return new WaitForEndOfFrame();Check(!GoryeoSeat.Sitting(actor),"disabling local controller releases sitting");actor.enabled=true;
            var field=MapRoot.Current.GetComponent<GoryeoFireFootprint>();var cell=new Vector3Int(43,51,0);Check(!MapRoot.Current.IsWalkable(cell),"fire footprint is solid");
            Frame(Find("north_fire"));actor.enabled=false;for(int i=0;i<120;i++)actor.Motor.Tick(Vector2.up,2f,1f/60);Check(actor.Motor.Cell.y<51,"real motor cannot enter fire from below");
            actor.Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)actor.Motor.feetOffset,Vector2Int.down);field.Configure(Find("north_fire"),43,51,actor.Motor);Check(MapRoot.Current.IsWalkable(cell,actor.Motor)&&!MapRoot.Current.IsWalkable(cell),"existing overlapping player may escape, other actors cannot enter");
            for(int i=0;i<120;i++)actor.Motor.Tick(Vector2.down,2f,1f/60);Check(actor.Motor.Feet.y<50&&!MapRoot.Current.IsWalkable(cell,actor.Motor),"entire foot box escapes without an edge trap and grace expires");actor.enabled=true;
            actor.Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)actor.Motor.feetOffset,Vector2Int.down);field.Configure(Find("north_fire"),43,51,actor.Motor);Check(gm.SaveNow(),"save existing overlapping physical position");
            var overlap=JsonUtility.FromJson<SaveData>(File.ReadAllText(slotPath));typeof(GameManager).GetField("pendingExact",Flags).SetValue(gm,(Vector3?)SaveSystem.PositionFor(overlap));
            yield return Load("Transit/transit_central_approach");actor=PlayerController.Instance;
            Check(actor.Motor.Cell==cell&&MapRoot.Current.IsWalkable(cell,actor.Motor)&&!MapRoot.Current.IsWalkable(cell),"actual saved-position scene restore grants escape without moving old position");
            actor.enabled=false;for(int i=0;i<120;i++)actor.Motor.Tick(Vector2.down,2f,1f/60);actor.enabled=true;Check(actor.Motor.Feet.y<50&&!MapRoot.Current.IsWalkable(cell,actor.Motor),"restored entire foot box escapes and cannot reenter");
            Frame(Find("north_fire"));Find("north_fire").Interact(actor);Check(gm.SaveNow(),"checkpoint while sitting before scene reload");var data=JsonUtility.FromJson<SaveData>(File.ReadAllText(slotPath));typeof(GameManager).GetField("pendingExact",Flags).SetValue(gm,(Vector3?)SaveSystem.PositionFor(data));
            yield return Load("Transit/transit_central_approach");Check(!GoryeoSeat.Sitting(PlayerController.Instance)&&MapRoot.Current.IsWalkable(PlayerController.Instance.Motor.Cell)&&FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==3,"saved physical position restored as standing after actual scene reload");
            foreach(string other in new[]{"Goryeo/Naru","Transit/transit_ferry","Venice/venice_rialto","Arabia/arabia_ahsa","North/north_naru"})
            {
                string path="Assets/PocketCaravan/Scenes/"+other+".unity";Check(File.Exists(path),"external fixture exists "+other);
                yield return Load(other);Check(FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==0&&FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None).Length==0&&MapRoot.Current.GetComponent<GoryeoFireFootprint>()==null,"no seat/fire/collision installed outside approved map "+other);
            }
            Check(gm.AutosaveEnabled,"disposable-slot save mode retained until teardown");
        }
    }
}
#endif
