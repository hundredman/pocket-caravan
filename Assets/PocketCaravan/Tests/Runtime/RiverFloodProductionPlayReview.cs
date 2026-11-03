#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class RiverFloodProductionPlayReview:MonoBehaviour
    {
        [Serializable] public class Result{public bool complete;public string error;public List<string> checks=new List<string>();}
        const string Out="ArtReview/E3-FloodProduction-20261103/Unity";
        readonly Result result=new Result();
        void Check(bool value,string text){if(!value)throw new Exception(text);result.checks.Add(text);}
        void Save(){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/checks.json",JsonUtility.ToJson(result,true));}
        IEnumerator Start()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){bool next=false;object item=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){error=e;}
                if(error!=null){Finish(error);yield break;}if(!next){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}
            Finish(null);
        }
        void Finish(Exception error){result.complete=error==null;result.error=error==null?"":error.ToString();Save();Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string name)
        {
            Texture2D im=null;for(int n=0;n<20;n++){yield return null;yield return new WaitForEndOfFrame();im=ScreenCapture.CaptureScreenshotAsTexture();if(new HashSet<Color32>(im.GetPixels32()).Count>128)break;Destroy(im);im=null;}
            Check(im!=null,"render "+name);Check(im.width==480&&im.height==270,"480x270 "+name);File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Destroy(im);
        }
        void Day(bool active,RiverFloodTerrain flood)
        {
            for(int day=1;day<100;day++)if((RoadHazardSchedule.Enabled(MapRoot.Current.mapId,flood.placementId,"river_flood",(day-1)*24f)??false)==active)
            {GameManager.I.Clock.day=day;GameManager.I.Clock.minuteOfDay=720;return;}
            throw new Exception("No matching schedule day");
        }
        void Pose()
        {
            var map=MapRoot.Current;var player=PlayerController.Instance;player.enabled=false;Time.timeScale=0;
            player.Motor.Teleport(map.CellCenter(new Vector3Int(44,51,0))-(Vector3)player.Motor.feetOffset,Vector2Int.down);
            foreach(var i in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){i.StopAllCoroutines();i.enabled=false;}
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None)){typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);d.enabled=false;}
            foreach(var h in UnityEngine.Object.FindObjectsByType<HUD>(FindObjectsSortMode.None))h.enabled=false;foreach(var q in UnityEngine.Object.FindObjectsByType<QuestTracker>(FindObjectsSortMode.None))q.enabled=false;foreach(var l in UnityEngine.Object.FindObjectsByType<WorldLabels>(FindObjectsSortMode.None))l.enabled=false;Notify.Clear();
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.orthographicSize=8.4375f;camera.transform.position=new Vector3(47.5f,45.5f,-10);
        }
        void State(RiverFloodTerrain flood,bool wet)
        {
            Check(flood.Ready&&flood.Flooded==wet,"terrain ready/state "+wet);var map=MapRoot.Current;var road=map.GetComponent<RoadGrid>();var deco=map.transform.Find("Deco").GetComponent<Tilemap>();int submerged=0;
            foreach(var c in flood.cells)
            {
                Check(map.ground.GetTile(c.position)==(wet?c.wetGround:c.dryGround),"ground state "+c.position);
                Check(deco.GetTile(c.position)==(wet?c.wetDeco:c.dryDeco),"deco state "+c.position);
                Check(map.blocked.HasTile(c.position)==(wet?c.wetBlocked:c.dryBlocked),"collision state "+c.position);
                Check(road.GradeAt(c.position)==(wet?c.wetGrade:c.dryGrade),"road grade "+c.position);
                var tile=map.ground.GetTile<Tile>(c.position);Check(tile!=null&&tile.sprite!=null&&tile.sprite.pixelsPerUnit==16,"native sprite "+c.position);
                if(c.dryGrade>0&&c.wetGrade==0){submerged++;if(wet)Check(!map.IsWalkable(c.position),"submerged road blocks "+c.position);}
            }
            Check(submerged==22,"22 submerged road cells");
        }
        IEnumerator Reload(string name)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode((name=="runtime"?"Assets/PocketCaravan/Scenes/Transit/transit_wetroad.unity":"Assets/PocketCaravan/Editor/B2Preview/E3FloodTerrain/Scenes/normal.unity"),new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            for(int i=0;i<8;i++)yield return null;Pose();Check(!GameManager.I.AutosaveEnabled,"reentry autosave off");
        }
        IEnumerator WaitTravel()
        {
            float end=Time.realtimeSinceStartup+20;while(GameManager.I.IsTravelling&&Time.realtimeSinceStartup<end)yield return null;
            Check(!GameManager.I.IsTravelling,"travel completed");for(int i=0;i<8;i++)yield return null;Pose();Check(!GameManager.I.AutosaveEnabled,"travel saves remain isolated");
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);UnityEngine.Object.DontDestroyOnLoad(gameObject);
            for(int i=0;i<10;i++)yield return null;Check(MapRoot.Current.previewOnly&&!GameManager.I.AutosaveEnabled,"safe preview bootstrap");yield return Reload("runtime");Pose();var map=MapRoot.Current;var flood=map.GetComponent<RiverFloodTerrain>();
            Check(!map.previewOnly&&!GameManager.I.AutosaveEnabled,"actual production scene and isolated save session");Check(flood!=null&&flood.Ready,"runtime initialized by builder");
            Check(flood.definition.activeSeconds==9&&flood.definition.openSeconds==6,"existing 9/6 timing unchanged");Check(RoadHazardSchedule.FloodDayChance==.4f,"existing daily 40 percent unchanged");
            Day(false,flood);flood.Advance(0);State(flood,false);yield return Photo("normal");
            Day(true,flood);flood.Advance(0);State(flood,true);yield return Photo("flood");
            var motor=PlayerController.Instance.Motor;motor.Teleport(map.CellCenter(new Vector3Int(44,49,0))-(Vector3)motor.feetOffset,Vector2Int.down);
            for(int i=0;i<60;i++)motor.Tick(Vector2.down,3.8f,1f/60);Check(motor.Cell.y>=49,"PlayerMotor blocked on flooded road");Pose();
            while(flood.Elapsed<flood.definition.activeSeconds+.1f)flood.Advance(1);State(flood,false);yield return Photo("receded");
            motor.Teleport(map.CellCenter(new Vector3Int(44,49,0))-(Vector3)motor.feetOffset,Vector2Int.down);
            for(int i=0;i<60;i++)motor.Tick(Vector2.down,3.8f,1f/60);Check(motor.Cell.y<=47,"PlayerMotor crosses receded road");Pose();
            while(flood.Elapsed<flood.definition.activeSeconds+flood.definition.openSeconds+.1f)flood.Advance(1);State(flood,true);
            Day(false,flood);flood.Advance(0);State(flood,false);
            motor.Teleport(map.CellCenter(new Vector3Int(44,48,0))-(Vector3)motor.feetOffset,Vector2Int.down);var pos=motor.transform.position;
            Day(true,flood);flood.Advance(0);Check(flood.Pending&&!flood.Flooded&&motor.transform.position==pos,"activation deferred without trapping/teleport");
            Pose();flood.Advance(0);Check(!flood.Pending&&flood.Flooded,"activation retries after actor leaves");
            flood.enabled=false;State(flood,false);flood.enabled=true;flood.Advance(0);State(flood,true);
            var day=GameManager.I.Clock.day;yield return Reload("runtime");flood=MapRoot.Current.GetComponent<RiverFloodTerrain>();Check(GameManager.I.Clock.day==day,"reentry preserves game day");State(flood,true);
            Day(false,flood);flood.Advance(0);yield return Reload("runtime");flood=MapRoot.Current.GetComponent<RiverFloodTerrain>();State(flood,false);
            Day(true,flood);
            var exit=MapExit.All.Find(e=>e.targetScene=="transit_ferry");Check(exit!=null,"ferry exit retained");
            PlayerController.Instance.Motor.Teleport(exit.transform.position-(Vector3)PlayerController.Instance.Motor.feetOffset,Vector2Int.up);exit.Use(PlayerController.Instance.Motor);yield return WaitTravel();
            Check(MapRoot.Current.mapId=="transit_ferry","actual exit travel to ferry");Check(MapRoot.Current.GetComponents<RiverFloodTerrain>().Length==0,"ferry has no flood component");
            exit=MapExit.All.Find(e=>e.targetScene=="transit_wetroad");Check(exit!=null,"return exit retained");PlayerController.Instance.Motor.Teleport(exit.transform.position-(Vector3)PlayerController.Instance.Motor.feetOffset,Vector2Int.down);exit.Use(PlayerController.Instance.Motor);yield return WaitTravel();
            Check(MapRoot.Current.mapId=="transit_wetroad","actual return exit travel");flood=MapRoot.Current.GetComponent<RiverFloodTerrain>();Check(flood.Ready,"production flood ready after travel");
            // Old v2 save shape: no terrain-state field. In-memory restoration only; no user file writes.
            var gm=GameManager.I;Day(true,flood);var item=gm.Db.items.Find(i=>i!=null&&!i.questItem);
            var legacy=new SaveData{version=2,layoutVersion=2,scene="transit_wetroad",mapHeight=64,px=44.5f,py=48.5f-PlayerController.Instance.Motor.feetOffset.y,fx=0,fy=-1,money=137,stamina=.75f,day=gm.Clock.day,minute=720,vehicle=gm.Player.vehicle!=null?gm.Player.vehicle.id:null,origin=gm.Player.origin!=null?gm.Player.origin.id:null};
            legacy.cargoItems.Add(item.id);legacy.cargoCounts.Add(2);legacy.cargoCosts.Add(3);
            legacy=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(legacy));SaveCompatibility.Validate(legacy,gm.Db,Application.CanStreamedLevelBeLoaded);
            Check((bool)typeof(GameManager).GetMethod("TryApplySave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gm,new object[]{legacy}),"legacy v2 save applied in memory");
            var travel=(IEnumerator)typeof(GameManager).GetMethod("Travel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gm,new object[]{"transit_wetroad",null,0f,Vector2Int.down,(Vector3?)SaveSystem.PositionFor(legacy)});
            gm.StartCoroutine(travel);float until=Time.realtimeSinceStartup+20;while(gm.IsTravelling&&Time.realtimeSinceStartup<until)yield return null;Check(!gm.IsTravelling,"saved position travel completed");for(int i=0;i<8;i++)yield return null;
            Check(gm.Player.money==137&&gm.Player.cargo.Count(item)==2,"legacy money/cargo preserved");
            Check(MapRoot.Current.IsWalkable(PlayerController.Instance.Motor.Cell),"restored actor is not trapped by flood");Check(!gm.AutosaveEnabled,"production test never enables autosave");
        }
    }
}
#endif
