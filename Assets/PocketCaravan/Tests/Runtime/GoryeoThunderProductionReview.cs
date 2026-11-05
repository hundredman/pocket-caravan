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
    public sealed class GoryeoThunderProductionReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-StormLogs-20261105/Unity/";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report report=new Report();AudioManager audio;float oldVolume;bool oldMute,backedUp;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.checks.Add(text);}
        void Mute(bool value)=>typeof(AudioManager).GetProperty("Muted",BindingFlags.Public|BindingFlags.Static).SetValue(null,value);
        void Travelling(bool value)=>typeof(GameManager).GetProperty("IsTravelling",BindingFlags.Public|BindingFlags.Instance).SetValue(GameManager.I,value);
        void FreezeEntry(Scene scene,LoadSceneMode mode){UIState.Blocking=true;}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);SceneManager.sceneLoaded+=FreezeEntry;var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool next=false;object value=null;Exception error=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!next){stack.Pop();continue;}if(value is IEnumerator)stack.Push((IEnumerator)value);else yield return value;}Finish(null);}
        void Finish(Exception e){SceneManager.sceneLoaded-=FreezeEntry;if(backedUp){audio.sfxVolume=oldVolume;Mute(oldMute);}if(GameManager.I!=null)Travelling(false);report.complete=e==null;report.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"thunder-production.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        void Clock(float hours){GameManager.I.Clock.day=1+Mathf.FloorToInt(hours/24);GameManager.I.Clock.minuteOfDay=Mathf.Repeat(hours,24)*60;}
        float Window(GoryeoAtmosphere.Kind kind,bool on,int after=-1){for(int w=after+1;w<after+1+365*3;w++){float h=w*8+4;if(GoryeoAtmosphere.DayWeather(1+Mathf.FloorToInt(h/24))==kind&&RoadHazardSchedule.Enabled(GoryeoStormLogs.MapId,GoryeoStormLogs.PlacementId,"fallen_log",h)==on)return h;}throw new Exception("missing window");}
        IEnumerator Load(string path,float hours){Clock(hours);yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+path+".unity",new LoadSceneParameters(LoadSceneMode.Single));for(int n=0;n<8;n++)yield return null;PlayerController.Instance.enabled=false;foreach(var guide in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){guide.StopAllCoroutines();guide.enabled=false;}foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,null);UIState.Blocking=true;MapRoot.Current.GetComponent<GoryeoAtmosphere>()?.Refresh();Check(!GameManager.I.AutosaveEnabled,"saves off "+path);}
        IEnumerator Target(float hours){yield return Load("Transit/transit_central_approach",hours);}
        IEnumerator Run()
        {
            Check(!GameManager.I.AutosaveEnabled,"safe bootstrap");Time.timeScale=0;audio=GameManager.I.GetComponent<AudioManager>();oldVolume=audio.sfxVolume;oldMute=AudioManager.Muted;backedUp=true;audio.sfxVolume=.4f;Mute(false);
            int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);
            Check(GoryeoThunderDirector.PlayedCount==0,"fresh actual Play session has no played windows");
            float first=Window(GoryeoAtmosphere.Kind.Monsoon,true);yield return Target(first);var map=MapRoot.Current;var trigger=map.GetComponent<GoryeoThunderTrigger>();var effect=map.GetComponent<GoryeoThunder>();
            Check(trigger!=null&&effect!=null&&effect.approvedForProduction&&!map.previewOnly,"approved automatic production install");Check(effect.clip==Resources.Load<AudioClip>("Goryeo/Thunder/distant-thunder"),"approved production clip identity");Check(effect.Plays==0&&GoryeoThunderDirector.PlayedCount==0,"blocked UI does not consume window");
            int count=map.GetComponents<AudioSource>().Length;GoryeoThunderDirector.Apply(map);GoryeoThunderDirector.Apply(map);Check(map.GetComponents<GoryeoThunder>().Length==1&&map.GetComponents<GoryeoThunderTrigger>().Length==1&&map.GetComponents<AudioSource>().Length==count,"repeat install has one effect/trigger/source");
            Check(!GoryeoThunderDirector.TryPlay(trigger),"UI blocks triggering");UIState.Blocking=false;Travelling(true);Check(!GoryeoThunderDirector.TryPlay(trigger)&&GoryeoThunderDirector.PlayedCount==0,"travel cover waits without consuming window");Travelling(false);
            Check(GoryeoThunderDirector.TryPlay(trigger)&&effect.Plays==1&&GoryeoThunderDirector.PlayedCount==1,"first eligible window triggers once");Check(!effect.Source.isPlaying,"approved delayed thunder not immediate");float max=0;bool photo=false;
            for(int frame=0;frame<28;frame++){max=Mathf.Max(max,effect.Flash);if(!photo&&effect.Flash>.04f){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();Check(image.width==480&&image.height==270,"actual480x270 production flash");File.WriteAllBytes(Out+"thunder-production.png",image.EncodeToPNG());Destroy(image);photo=true;}yield return new WaitForSecondsRealtime(.02f);}
            Check(max>0&&max<=.07f,"approved soft flash strength unchanged");yield return new WaitForSecondsRealtime(.8f);Check(effect.Source.isPlaying&&effect.Source.timeSamples>0,"actual delayed production audio playing");Check(Mathf.Abs(effect.Source.volume-.14f)<.001f,"production follows SFX volume");
            int samples=effect.Source.timeSamples;yield return new WaitForSecondsRealtime(.15f);Check(effect.Source.timeSamples!=samples,"production audio playhead advances");Mute(true);yield return null;Check(effect.Source.volume==0,"production mute immediate");Mute(false);audio.sfxVolume=0;yield return null;Check(effect.Source.volume==0,"zero SFX volume respected");audio.sfxVolume=.4f;
            Check(!GoryeoThunderDirector.TryPlay(trigger)&&effect.Plays==1,"same window cannot retrigger");effect.enabled=false;Check(!effect.Source.isPlaying&&effect.Flash==0,"disable stops and clears flash");effect.enabled=true;yield return null;Check(effect.Plays==1&&!effect.Source.isPlaying,"re-enable does not replay consumed window");
            var oldSource=effect.Source;foreach(var path in new[]{"Goryeo/Naru","Venice/venice_rialto","Arabia/arabia_ahsa","North/north_birka"}){yield return Load(path,first);GoryeoThunderDirector.Apply(MapRoot.Current);Check(MapRoot.Current.GetComponent<GoryeoThunder>()==null&&MapRoot.Current.GetComponent<GoryeoThunderTrigger>()==null,"no thunder module in other map "+path);Check(GoryeoThunderDirector.PlayedCount==1,"foreign map does not consume window "+path);}Check(oldSource==null,"owned source destroyed on map leave");
            yield return Target(first);trigger=MapRoot.Current.GetComponent<GoryeoThunderTrigger>();effect=trigger.effect;UIState.Blocking=false;yield return new WaitForSecondsRealtime(.8f);Check(effect.Plays==0&&!effect.Source.isPlaying&&GoryeoThunderDirector.PlayedCount==1,"same window reentry stays silent despite active log");Check(MapRoot.Current.GetComponent<ObstacleField>().States[0].active,"same window log state still unchanged");
            float second=Window(GoryeoAtmosphere.Kind.Monsoon,true,Mathf.FloorToInt(first/8));yield return Target(second);trigger=MapRoot.Current.GetComponent<GoryeoThunderTrigger>();effect=trigger.effect;UIState.Blocking=false;yield return null;yield return null;Check(effect.Plays==1&&GoryeoThunderDirector.PlayedCount==2,"different eligible window gets one new event automatically");
            float third=Window(GoryeoAtmosphere.Kind.Monsoon,true,Mathf.FloorToInt(second/8));yield return Target(third);trigger=MapRoot.Current.GetComponent<GoryeoThunderTrigger>();effect=trigger.effect;UIState.Blocking=false;var field=MapRoot.Current.GetComponent<ObstacleField>();var state=field.States[0];state.resolved=true;field.Advance(0);Check(!GoryeoThunderDirector.TryPlay(trigger)&&GoryeoThunderDirector.PlayedCount==2,"resolved before trigger suppresses event");state.resolved=false;field.Advance(0);
            var clip=effect.clip;effect.clip=null;Check(!GoryeoThunderDirector.TryPlay(trigger)&&GoryeoThunderDirector.PlayedCount==2,"missing clip does not consume window");effect.clip=clip;Clock(third+8);Check(!GoryeoThunderDirector.TryPlay(trigger)&&GoryeoThunderDirector.PlayedCount==2,"expired entry window does not start a new event");Clock(third);MapRoot.Current.GetComponent<GoryeoAtmosphere>().Refresh();Mute(true);Check(GoryeoThunderDirector.TryPlay(trigger),"muted entry still allows one visual event");yield return null;Check(effect.Source.volume==0&&effect.Plays==1&&GoryeoThunderDirector.PlayedCount==3,"muted entry remains silent and counted once");Mute(false);
            oldSource=effect.Source;yield return Load("Goryeo/Naru",third);Check(oldSource==null,"leaving before delayed thunder cancels owned source");
            foreach(var kind in new[]{GoryeoAtmosphere.Kind.Clear,GoryeoAtmosphere.Kind.Rain,GoryeoAtmosphere.Kind.Monsoon}){yield return Target(Window(kind,false));Check(MapRoot.Current.GetComponent<GoryeoThunderTrigger>()==null&&MapRoot.Current.GetComponent<GoryeoThunder>()==null,"no module when storm/log not eligible "+kind);}
            Check(GameManager.I.Player.money==money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo),"money cargo unchanged");Check(!GameManager.I.AutosaveEnabled,"final saves off");
        }
    }
}
#endif
