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
    public sealed class GoryeoStormLogPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-StormLogs-20261105/Unity/";
        [Serializable]class Report{public bool complete;public string error;public int scheduleCases;public List<string> entries=new List<string>();public List<string> checks=new List<string>();}
        public bool thunderTrial;
        readonly Report r=new Report();void Check(bool ok,string text){if(!ok)throw new Exception(text);r.checks.Add(text);}
        IEnumerator Start(){SceneManager.sceneLoaded+=Entry;DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(thunderTrial?RunThunder():Run());while(stack.Count>0){bool next=false;object value=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!next){stack.Pop();continue;}if(value is IEnumerator)stack.Push((IEnumerator)value);else yield return value;}Finish(null);}
        void Finish(Exception e){SceneManager.sceneLoaded-=Entry;r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+(thunderTrial?"thunder-play.json":"play.json"),JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        float Roll(string key){ulong h=14695981039346656037UL;unchecked{foreach(char c in key){h^=c;h*=1099511628211UL;}}return h%1000003UL/1000003f;}
        bool Original(string map,string p,float hours)=>Roll(map+"|"+p+"|log|"+Mathf.FloorToInt(hours/8))<.5f;
        void Clock(float hours){GameManager.I.Clock.day=1+Mathf.FloorToInt(hours/24);GameManager.I.Clock.minuteOfDay=Mathf.Repeat(hours,24)*60;}
        float Window(GoryeoAtmosphere.Kind kind,bool on){for(int day=1;day<1000;day++)if(GoryeoAtmosphere.DayWeather(day)==kind){float h=(day-1)*24+12;if(Original(GoryeoStormLogs.MapId,GoryeoStormLogs.PlacementId,h)==on)return h;}throw new Exception("missing window");}
        void Entry(Scene scene,LoadSceneMode mode)
        {
            var map=MapRoot.Current;var gm=GameManager.I;if(map==null||gm==null)return;var field=map.GetComponent<ObstacleField>();if(field==null)return;
            foreach(var p in field.placements)r.entries.Add("sceneLoaded "+map.mapId+" time="+gm.Clock.TotalHours+" enabled="+p.enabled+" expected="+RoadHazardSchedule.Enabled(map.mapId,p.id,p.definitionId,gm.Clock.TotalHours));
        }
        IEnumerator Load(float hour){Clock(hour);yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/Transit/transit_central_approach.unity",new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<8;i++)yield return null;Time.timeScale=0;PlayerController.Instance.enabled=false;
            foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
            var map=MapRoot.Current;var road=map.GetComponent<RoadGrid>();var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=map.CellCenter(new Vector3Int(51,road.height-34,0))+new Vector3(0,0,-10);At(new Vector3Int(50,road.height-37,0));map.GetComponent<GoryeoAtmosphere>().Refresh();var field=map.GetComponent<ObstacleField>();r.entries.Add("afterStart requested="+hour+" actual="+GameManager.I.Clock.TotalHours+" enabled="+field.placements[0].enabled+" active="+field.States[0].active);}
        void At(Vector3Int cell){var pc=PlayerController.Instance;pc.Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)pc.Motor.feetOffset,Vector2Int.up);}
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==480&&t.height==270,"actual480x270 "+name);File.WriteAllBytes(Out+name+".png",t.EncodeToPNG());Destroy(t);}
        bool Path(Vector3Int a,Vector3Int b,RectInt area){var q=new Queue<Vector3Int>();var seen=new HashSet<Vector3Int>{a};q.Enqueue(a);while(q.Count>0){var c=q.Dequeue();if(c==b)return true;foreach(var d in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}){var n=c+d;if(area.Contains((Vector2Int)n)&&seen.Add(n)&&MapRoot.Current.IsWalkable(n))q.Enqueue(n);}}return false;}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");Time.timeScale=0;
            Check(RoadHazardSchedule.LogChance==.5f&&RoadHazardSchedule.LogWindowHours==8,"8hour50percent unchanged");
            for(int window=0;window<365*3;window++)foreach(float offset in new[]{0f,4f,7.99f})
            {
                float h=window*8+offset;bool expected=Original(GoryeoStormLogs.MapId,GoryeoStormLogs.PlacementId,h)&&GoryeoStormLogs.StormAt(h);
                if(RoadHazardSchedule.Enabled(GoryeoStormLogs.MapId,GoryeoStormLogs.PlacementId,"fallen_log",h)!=expected)throw new Exception("storm schedule mismatch");r.scheduleCases++;
                foreach(var pair in new[]{new[]{"transit_south_inland","e3_other_log"},new[]{"venice_rialto",GoryeoStormLogs.PlacementId},new[]{GoryeoStormLogs.MapId,"e3_other_log"}}){if(RoadHazardSchedule.Enabled(pair[0],pair[1],"fallen_log",h)!=Original(pair[0],pair[1],h))throw new Exception("other log changed");r.scheduleCases++;}
                bool flood=Roll(GoryeoStormLogs.MapId+"|e3_flood|flood|"+Mathf.FloorToInt(h/24))<.4f;if(RoadHazardSchedule.Enabled(GoryeoStormLogs.MapId,"e3_flood","river_flood",h)!=flood)throw new Exception("flood changed");r.scheduleCases++;
            }
            Check(r.scheduleCases==16425,"365days x3windows x3offsets x5schedule regression cases");Check(RoadHazardSchedule.Enabled(GoryeoStormLogs.MapId,"unmanaged","fallen_log",0)==null,"unmanaged unchanged");Check(!GoryeoStormLogs.StormAt(float.NaN)&&!GoryeoStormLogs.StormAt(-1),"invalid hour fails closed");
            foreach(var kind in new[]{GoryeoAtmosphere.Kind.Clear,GoryeoAtmosphere.Kind.Rain}){yield return Load(Window(kind,true));var s=MapRoot.Current.GetComponent<ObstacleField>().States[0];Check(!s.active&&!s.renderer.enabled&&!s.handle.isActiveAndEnabled,"no log on nonstorm despite positive oldroll "+kind);yield return Photo(kind+"-open");}
            yield return Load(Window(GoryeoAtmosphere.Kind.Monsoon,false));Check(!MapRoot.Current.GetComponent<ObstacleField>().States[0].active,"monsoon still requires original50percent roll");
            var initial=MapRoot.Current.GetComponent<ObstacleField>();initial.placements[0].enabled=true;
            typeof(GoryeoLogVisuals).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(MapRoot.Current.GetComponent<GoryeoLogVisuals>(),null);
            typeof(ObstacleField).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(initial,null);
            Check(!initial.States[0].active&&!initial.placements[0].enabled,"initialization corrects serialized true even if scene schedule callback was skipped");
            yield return Load(Window(GoryeoAtmosphere.Kind.Monsoon,true));var map=MapRoot.Current;var field=map.GetComponent<ObstacleField>();var state=field.States[0];var road=map.GetComponent<RoadGrid>();
            Check(!map.previewOnly&&state.active&&state.nativeLog!=null&&state.renderer.enabled,"actual storm log installed");Check(state.definition.workSeconds==3&&state.definition.money==0&&state.definition.itemCount==0,"existing free3second action");
            var a=new Vector3Int(49,road.height-32-1,0);var b=new Vector3Int(53,road.height-36-1,0);var area=new RectInt(44,road.height-41,15,15);
            // Use the exact approved layout endpoints, rather than guessing path cells from the image.
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("ArtReview/LogBinding-20261104/revised-layout.json"));var option=Array.Find(layout.options,v=>v.mapId==map.mapId);a=new Vector3Int(option.a[0],road.height-option.a[1]-1,0);b=new Vector3Int(option.b[0],road.height-option.b[1]-1,0);
            Check(!Path(a,b,area),"approved local corridor blocked during monsoon");yield return Photo("Monsoon-blocked");
            bool near=false;for(int y=state.area.yMin-2;y<state.area.yMax+2&&!near;y++)for(int x=state.area.xMin-2;x<state.area.xMax+2&&!near;x++){var c=new Vector3Int(x,y,0);if(map.IsWalkable(c)&&Path(a,c,area)){At(c);if(field.Near(state,PlayerController.Instance))near=true;}}
            Check(near,"single actor can approach from one side");int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);Check(field.TryWork(state.placement.id,PlayerController.Instance),"start clearing alone");field.Advance(1);field.Advance(1);Check(!state.resolved,"not cleared before3seconds");field.Advance(1);field.Advance(0);Check(state.resolved&&!state.renderer.enabled&&Path(a,b,area),"clear3seconds restores path");RoadHazardDirector.Apply();Check(state.resolved&&!state.active,"same visit resolved remains after schedule apply");typeof(ObstacleField).GetField("showDetails",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(field,false);yield return Photo("Monsoon-cleared");
            yield return Load(Window(GoryeoAtmosphere.Kind.Monsoon,true));Check(MapRoot.Current.GetComponent<ObstacleField>().States[0].active,"same original window can recur on new visit, existing semantics");
            Check(GameManager.I.Player.money==money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo),"money/cargo unchanged");Check(!GameManager.I.AutosaveEnabled,"final saves off");
        }
        IEnumerator RunThunder()
        {
            Directory.CreateDirectory(Out);Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");yield return Load(Window(GoryeoAtmosphere.Kind.Monsoon,true));var map=MapRoot.Current;map.previewOnly=true;var installed=map.GetComponent<GoryeoThunder>();if(installed!=null)Destroy(installed);var hook=map.GetComponent<GoryeoThunderTrigger>();if(hook!=null)Destroy(hook);yield return null;yield return null;
            int count=map.GetComponents<AudioSource>().Length;var trial=map.gameObject.AddComponent<GoryeoThunder>();trial.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PocketCaravan/Editor/B2Preview/StormLogs/distant-thunder.wav");Check(trial.clip!=null,"review imported thunder");yield return null;yield return null;Check(!trial.Source.isPlaying,"no autoplay before explicit trigger");
            Check(trial.PlayOnce(),"one storm preview trigger");Time.timeScale=1;GameManager.I.Clock.realSecondsPerDay=36000;
            float maximum=0;for(int frame=0;frame<12;frame++){maximum=Mathf.Max(maximum,trial.Flash);yield return Photo("thunder-frame-"+frame.ToString("00"));yield return new WaitForSecondsRealtime(.06f);}
            Check(maximum>0&&maximum<=.07f,"one soft fade flash max7percent");Check(trial.Source.isPlaying,"actual delayed thunder playing");
            var master=GameManager.I.GetComponent<AudioManager>();float old=master.sfxVolume;bool mute=AudioManager.Muted;master.sfxVolume=.4f;yield return null;Check(Mathf.Abs(trial.Source.volume-.14f)<.001f,"actual SFX setting followed");
            typeof(AudioManager).GetProperty("Muted",BindingFlags.Public|BindingFlags.Static).SetValue(null,true);yield return null;Check(trial.Source.volume==0,"mute silences thunder");typeof(AudioManager).GetProperty("Muted",BindingFlags.Public|BindingFlags.Static).SetValue(null,mute);master.sfxVolume=old;
            map.previewOnly=false;yield return null;Check(!trial.Source.isPlaying&&!trial.PlayOnce()&&trial.Flash==0,"hard gate no production thunder/flash");map.previewOnly=true;Clock(Window(GoryeoAtmosphere.Kind.Clear,true));map.GetComponent<GoryeoAtmosphere>().Refresh();Check(!trial.PlayOnce(),"no thunder in clear weather");Destroy(trial);yield return null;yield return null;Check(map.GetComponents<AudioSource>().Length==count,"owned source cleaned up");Check(!GameManager.I.AutosaveEnabled,"saves off after trial");
        }
        [Serializable]class Option{public string mapId;public int[] a,b;}[Serializable]class Layout{public Option[] options;}
    }
}
#endif
