#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class GoryeoStridePlayReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Atmosphere-A4-v3-20261105/Unity";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();AudioManager master;float oldVolume;bool oldMute,backedUp;
        void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        void Mute(bool value)=>typeof(AudioManager).GetProperty("Muted",BindingFlags.Public|BindingFlags.Static).SetValue(null,value);
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){if(backedUp){if(master!=null)master.sfxVolume=oldVolume;Mute(oldMute);}r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        void Day(GoryeoAtmosphere weather,GoryeoAtmosphere.Kind kind){for(int day=1;day<1000;day++)if(GoryeoAtmosphere.DayWeather(day)==kind){GameManager.I.Clock.day=day;GameManager.I.Clock.minuteOfDay=720;weather.Refresh();return;}}
        void Pose(Vector3Int cell){var p=PlayerController.Instance;p.Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)p.Motor.feetOffset,Vector2Int.down);var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=MapRoot.Current.CellCenter(cell)+new Vector3(0,1,-10);}
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==480&&t.height==270,"480x270 "+name);File.WriteAllBytes(Out+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
        bool OpenDirt(MapRoot map,GoryeoStride stride,Vector3Int c)
        {
            if(stride.At(c)!=GoryeoStride.Surface.Dirt||!map.IsWalkable(c))return false;
            foreach(var name in new[]{"Objects","Above"})if(map.transform.Find(name).GetComponent<Tilemap>().HasTile(c))return false;
            var point=map.CellCenter(c);
            foreach(var sr in map.GetComponentsInChildren<SpriteRenderer>())if(sr.enabled&&sr.gameObject.activeInHierarchy&&sr.sortingOrder>=5&&sr.bounds.Contains(point))return false;
            return true;
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Editor/B2Preview/AtmosphereA4/Naru.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<8;i++)yield return null;Time.timeScale=0;
            var map=MapRoot.Current;var stride=map.GetComponent<GoryeoStride>();var weather=map.GetComponent<GoryeoAtmosphere>();var player=PlayerController.Instance;player.enabled=false;
            Check(map.previewOnly&&!stride.approvedForProduction&&!GameManager.I.AutosaveEnabled,"review-only gate");
            foreach(var g in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
            master=GameManager.I.GetComponent<AudioManager>();Check(master!=null,"master audio found");oldVolume=master.sfxVolume;oldMute=AudioManager.Muted;backedUp=true;master.sfxVolume=0;Mute(false);
            int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);var road=map.GetComponent<RoadGrid>();int roadHash=0;foreach(int g in road.grades)roadHash=unchecked(roadHash*31+g);
            Vector3Int dry=Vector3Int.zero,wood=Vector3Int.zero;bool found=false,woodFound=false;Day(weather,GoryeoAtmosphere.Kind.Clear);float best=float.PositiveInfinity;
            for(int y=5;y<stride.height-5;y++)for(int x=5;x<stride.width-8;x++)
            {
                bool ok=true;for(int k=0;k<5;k++)if(!OpenDirt(map,stride,new Vector3Int(x+k,y,0)))ok=false;
                float score=(new Vector2(x+2,y)-new Vector2(30,54)).sqrMagnitude;
                if(ok&&score<best){best=score;dry=new Vector3Int(x,y,0);found=true;}
            }
            for(int y=5;y<stride.height-5&&!woodFound;y++)for(int x=5;x<stride.width-5&&!woodFound;x++){var c=new Vector3Int(x,y,0);if(stride.At(c)==GoryeoStride.Surface.Wood&&map.IsWalkable(c)){wood=c;woodFound=true;}}
            Check(found&&woodFound,"clear dirt route "+dry+" and certified wood bridge "+wood);Day(weather,GoryeoAtmosphere.Kind.Clear);Pose(dry);stride.Tick(0);int before=stride.StepsPlayed;
            for(int i=1;i<=50;i++){player.Motor.Tick(Vector2.right,3.8f,1f/60);stride.Tick(i/60f);}Check(stride.StepsPlayed>before,"real PlayerMotor distance emits steps");
            player.Motor.Teleport(player.transform.position,Vector2Int.down);before=stride.StepsPlayed;stride.Tick(2);stride.Tick(3);Check(stride.StepsPlayed==before,"standing still has no footsteps");
            Pose(dry);stride.Tick(4);Check(stride.StepsPlayed==before,"teleport has no spurious footsteps");
            UIState.Blocking=true;stride.Tick(5);Check(stride.StepsPlayed==before,"UI lock has no footsteps");UIState.Blocking=false;
            Pose(dry);stride.Tick(6);master.sfxVolume=.4f;stride.Tick(7);Check(Mathf.Abs(stride.Source.volume-.4f)<.001f,"master SFX volume followed");Mute(true);stride.Tick(8);Check(stride.Source.volume==0,"mute silences active source");Mute(false);master.sfxVolume=0;stride.Tick(9);Check(stride.Source.volume==0,"zero SFX volume followed");
            for(int i=0;i<5;i++)Check(stride.clips[i]!=null&&stride.clips[i].channels==1&&stride.clips[i].length>0,"imported clip "+i);
            stride.Animate(Time.time+1);Day(weather,GoryeoAtmosphere.Kind.Clear);Pose(dry+Vector3Int.right*2);yield return Photo("dry-before");stride.ReviewStep(player.Motor.Cell,true,Time.time);Time.timeScale=1;yield return new WaitForSeconds(.10f);Time.timeScale=0;Check(stride.ActiveMarks>0,"dirt running dust visible");yield return Photo("dry-dust");
            stride.Animate(Time.time+1);Day(weather,GoryeoAtmosphere.Kind.Rain);weather.Refresh();Check(stride.Wet,"approved A1 rain drives wet step");stride.ReviewStep(player.Motor.Cell,false,Time.time);Time.timeScale=1;yield return new WaitForSeconds(.12f);Time.timeScale=0;Check(stride.ActiveMarks>0,"rain splash visible");yield return Photo("rain-splash");
            stride.Animate(Time.time+1);Pose(wood);Day(weather,GoryeoAtmosphere.Kind.Clear);stride.ReviewStep(wood,false,Time.time);Check(stride.LastSurface==GoryeoStride.Surface.Wood,"certified bridge uses wood sound");yield return Photo("wood-bridge");
            Pose(dry);for(int i=0;i<50;i++)stride.ReviewStep(dry,true,Time.time);Check(stride.ActiveMarks<=24,"FX pool capped at24");stride.Animate(Time.time+1);Check(stride.ActiveMarks==0,"FX expires and pool reuses");
            var textureCount=map.GetComponentsInChildren<SpriteRenderer>(true).Length;for(int i=0;i<50;i++)stride.ReviewStep(dry,true,Time.time);Check(textureCount==map.GetComponentsInChildren<SpriteRenderer>(true).Length,"no renderer growth after50steps");
            stride.enabled=false;Check(stride.ActiveMarks==0&&!stride.Source.isPlaying,"disable clears sound and marks");
            int afterHash=0;foreach(int g in road.grades)afterHash=unchecked(afterHash*31+g);Check(roadHash==afterHash&&money==GameManager.I.Player.money&&cargo==JsonUtility.ToJson(GameManager.I.Player.cargo),"road money/cargo unchanged");Check(!GameManager.I.AutosaveEnabled,"saves off");
        }
    }
}
#endif
