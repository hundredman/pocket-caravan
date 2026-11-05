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
    public sealed class GoryeoTimeMusicPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Atmosphere-A2-20261105/";
        static readonly FieldInfo CommonA=typeof(AudioManager).GetField("musicA",BindingFlags.NonPublic|BindingFlags.Instance),CommonB=typeof(AudioManager).GetField("musicB",BindingFlags.NonPublic|BindingFlags.Instance),Sfx=typeof(AudioManager).GetField("sfx",BindingFlags.NonPublic|BindingFlags.Instance);
        [Serializable]class Report{public bool complete;public string error;public int choiceCases;public List<string> checks=new List<string>();}
        readonly Report r=new Report();AudioManager master;float oldVolume;bool oldMute,backedUp;GoryeoTimeMusic music;
        void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        void Mute(bool value)=>typeof(AudioManager).GetProperty("Muted",BindingFlags.Public|BindingFlags.Static).SetValue(null,value);
        void Preview(Scene scene,LoadSceneMode mode){var map=MapRoot.Current;if(map!=null&&GoryeoTravelTransition.RegionOf(map.mapId)!=null)map.previewOnly=true;}
        IEnumerator Load(string path){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/"+path+".unity",new LoadSceneParameters(LoadSceneMode.Single));for(int n=0;n<8;n++)yield return null;}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);SceneManager.sceneLoaded+=Preview;var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){SceneManager.sceneLoaded-=Preview;if(music!=null)music.enabled=false;if(backedUp){if(master!=null)master.musicVolume=oldVolume;Mute(oldMute);}r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out+"Unity");File.WriteAllText(Out+"Unity/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        AudioSource Selected(){for(int n=0;n<2;n++)if(music.Bus(n).clip==music.clips[music.Selected]&&music.Weight(n)>.9f)return music.Bus(n);return null;}
        IEnumerator Run()
        {
            Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");Time.timeScale=0;yield return Load("Goryeo/Naru");
            master=GameManager.I.GetComponent<AudioManager>();oldVolume=master.musicVolume;oldMute=AudioManager.Muted;backedUp=true;master.musicVolume=.4f;Mute(false);
            var a=CommonA.GetValue(master)as AudioSource;var b=CommonB.GetValue(master)as AudioSource;var sfx=Sfx.GetValue(master)as AudioSource;bool muteA=a.mute,muteB=b.mute,muteSfx=sfx.mute;int baseCount=GameManager.I.GetComponents<AudioSource>().Length;
            int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);
            Check(GameManager.I.GetComponent<GoryeoTimeMusic>()==null,"no unapproved production music hook");music=GameManager.I.gameObject.AddComponent<GoryeoTimeMusic>();music.enabled=false;
            int i=0;foreach(var name in new[]{"morning","day","evening","night"})music.clips[i++]=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PocketCaravan/Editor/B2Preview/AtmosphereA2/"+name+".wav");music.clips[4]=Resources.Load<AudioClip>("Audio/Music/road");music.clips[5]=Resources.Load<AudioClip>("Audio/Music/danger");
            for(int n=0;n<6;n++)Check(music.clips[n]!=null&&music.clips[n].length>0,"imported choice "+n);music.enabled=true;music.Tick(0);
            foreach(var id in new[]{"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_southport","transit_crossroads","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"})for(int hour=0;hour<24;hour++)foreach(bool danger in new[]{false,true})
            {
                int choice=GoryeoTimeMusic.Choice(id,hour,danger);if(choice<0||choice>5||danger&&choice!=5||!danger&&(id=="transit_wetroad"||id=="transit_pass"||id=="transit_central_approach"||id=="transit_south_inland")&&choice!=4)throw new Exception("choice "+id+" "+hour);r.choiceCases++;
            }
            Check(r.choiceCases==624,"13 maps x24 hours x2 danger states");foreach(var id in new[]{"north_birka","venice_rialto","arabia_ahsa","transit_fake","",null})Check(GoryeoTimeMusic.Choice(id,12,true)==-1,"foreign choice excluded "+id);
            Check(GoryeoTimeMusic.Slot(4)==3&&GoryeoTimeMusic.Slot(5)==0&&GoryeoTimeMusic.Slot(9)==0&&GoryeoTimeMusic.Slot(10)==1&&GoryeoTimeMusic.Slot(16)==1&&GoryeoTimeMusic.Slot(17)==2&&GoryeoTimeMusic.Slot(19)==2&&GoryeoTimeMusic.Slot(20)==3,"four exact time boundaries");
            GameManager.I.Clock.minuteOfDay=300;music.Tick(2);Check(music.Selected==0&&Selected()!=null,"morning loop active");Check(music.SourceCount==2&&GameManager.I.GetComponents<AudioSource>().Length==baseCount+2,"only two owned music sources");Check(a.mute&&b.mute&&sfx.mute==muteSfx,"only common music buses muted, SFX untouched");
            int starts=music.Starts;music.Tick(.2f);Check(music.Starts==starts,"same slot does not restart");
            var playing=Selected();int samples=playing.timeSamples;yield return new WaitForSecondsRealtime(.3f);Check(playing.isPlaying&&playing.timeSamples!=samples,"actual audio playhead advances while game time paused");
            GameManager.I.Clock.minuteOfDay=600;music.Tick(0);music.Tick(1);Check(music.Selected==1&&music.Bus(0).isPlaying&&music.Bus(1).isPlaying,"two tracks overlap at time change");Check(Mathf.Abs(music.Weight(0)*music.Weight(0)+music.Weight(1)*music.Weight(1)-1)<.001f,"equal-power midpoint");Check(music.Bus(0).volume<=.401f&&music.Bus(1).volume<=.401f,"midpoint volume bounded");music.Tick(1);Check(Selected()!=null,"fade ends on day track");
            starts=music.Starts;yield return Load("Goryeo/Gaegyeong");music.Tick(.1f);Check(music.Selected==1&&music.Starts==starts,"town transition retains same track and playhead");
            foreach(var hour in new[]{17,20,5}){GameManager.I.Clock.minuteOfDay=hour*60;music.Tick(0);music.Tick(2);Check(music.Selected==GoryeoTimeMusic.Slot(hour)&&Selected()!=null,"time slot fade "+hour);}
            master.musicVolume=.2f;music.Tick(.1f);Check(Mathf.Abs(Selected().volume-.2f)<.001f,"actual music setting followed");Mute(true);music.Tick(.1f);Check(music.Bus(0).volume==0&&music.Bus(1).volume==0,"mute immediately silences both buses");Mute(false);master.musicVolume=0;music.Tick(.1f);Check(music.Bus(0).volume==0&&music.Bus(1).volume==0,"zero music volume respected");master.musicVolume=.4f;
            yield return Load("Transit/transit_wetroad");music.Tick(2);Check(music.Selected==4&&Selected().clip==Resources.Load<AudioClip>("Audio/Music/road"),"four road maps preserve existing road audio");
            AudioManager.SetDanger(true);music.Tick(.6f);Check(music.Selected==5&&Selected().clip==Resources.Load<AudioClip>("Audio/Music/danger"),"existing danger music takes priority");AudioManager.SetDanger(false);music.Tick(2);Check(music.Selected==4,"danger exit returns to road");
            var kept=music.clips[4];music.clips[4]=null;music.Tick(.1f);Check(!music.OwnsCommon&&a.mute==muteA&&b.mute==muteB,"missing asset falls back to original audio");music.clips[4]=kept;music.Tick(2);
            foreach(var path in new[]{"Venice/venice_rialto","Arabia/arabia_ahsa","North/north_birka"}){yield return Load(path);music.Tick(.1f);Check(music.Selected==-1&&!music.OwnsCommon&&!music.Bus(0).isPlaying&&!music.Bus(1).isPlaying,"no own music in foreign map "+path);Check(a.mute==muteA&&b.mute==muteB,"foreign original music mute flags restored "+path);Check(!GameManager.I.AutosaveEnabled,"foreign saves off "+path);}
            yield return Load("Goryeo/Naru");master.musicVolume=.4f;music.Tick(2);music.enabled=false;Check(a.mute==muteA&&b.mute==muteB&&!music.Bus(0).isPlaying&&!music.Bus(1).isPlaying,"disable stops and restores original music");music.enabled=true;music.Tick(2);Check(music.OwnsCommon,"re-enable resumes bounded music");Destroy(music);yield return null;yield return null;Check(GameManager.I.GetComponents<AudioSource>().Length==baseCount&&a.mute==muteA&&b.mute==muteB,"owned sources removed and original restored on destroy");
            Check(money==GameManager.I.Player.money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo)&&sfx.mute==muteSfx,"money cargo and SFX state unchanged");Check(!GameManager.I.AutosaveEnabled,"saves remain off");
        }
    }
}
#endif
