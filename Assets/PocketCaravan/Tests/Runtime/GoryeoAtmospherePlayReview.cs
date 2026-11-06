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
    public sealed class GoryeoAtmospherePlayReview:MonoBehaviour
    {
        const string Out="ArtReview/Goryeo-Atmosphere-A1-20261105/Unity";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();public float approximateUnscaledFrameMs;}
        readonly Report r=new Report();void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        string Fingerprint(MapRoot map)
        {
            ulong hash=14695981039346656037UL;var road=map.GetComponent<RoadGrid>();
            for(int y=0;y<road.height;y++)for(int x=0;x<road.width;x++){var c=new Vector3Int(x,y,0);hash^=(ulong)(road.GradeAt(c)+1);hash*=1099511628211UL;hash^=map.blocked.HasTile(c)?1UL:0UL;hash*=1099511628211UL;}
            return hash.ToString();
        }
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==480&&t.height==270,"480x270 "+name);File.WriteAllBytes(Out+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
        void Day(GoryeoAtmosphere.Kind kind,float hour,GoryeoAtmosphere weather)
        {
            for(int d=1;d<1000;d++)if(GoryeoAtmosphere.DayWeather(d,weather.settings)==kind){GameManager.I.Clock.day=d;GameManager.I.Clock.minuteOfDay=hour*60;weather.Refresh();return;}
            throw new Exception("No sample day");
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<8;i++)yield return null;Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");
            foreach(var id in new[]{"Naru","goryeo_jangdan"})
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Editor/B2Preview/AtmosphereA1/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<8;i++)yield return null;Time.timeScale=0;
                var map=MapRoot.Current;var weather=map.GetComponent<GoryeoAtmosphere>();Check(map.previewOnly&&!weather.approvedForProduction&&!GameManager.I.AutosaveEnabled,"review gate "+id);
                foreach(var g in UnityEngine.Object.FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
                foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){g.StopAllCoroutines();g.enabled=false;}
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
                var player=PlayerController.Instance;player.enabled=false;var road=map.GetComponent<RoadGrid>();var desired=id=="Naru"?new Vector3Int(44,road.height-30-1,0):new Vector3Int(27,road.height-35-1,0);bool placed=false;
                for(int radius=0;radius<6&&!placed;radius++)for(int dy=-radius;dy<=radius&&!placed;dy++)for(int dx=-radius;dx<=radius&&!placed;dx++){var c=desired+new Vector3Int(dx,dy,0);if(map.IsWalkable(c)){player.Motor.Teleport(map.CellCenter(c)-(Vector3)player.Motor.feetOffset,Vector2Int.down);placed=true;}}
                Check(placed,"walkable camera sample "+id);var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=map.CellCenter(desired)+new Vector3(0,1,-10);
                string terrain=Fingerprint(map);int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);
                foreach(var kind in new[]{GoryeoAtmosphere.Kind.Clear,GoryeoAtmosphere.Kind.Rain,GoryeoAtmosphere.Kind.Monsoon,GoryeoAtmosphere.Kind.Mist})
                {
                    float hour=kind==GoryeoAtmosphere.Kind.Mist?7.5f:12;Day(kind,hour,weather);yield return null;weather.Refresh();
                    Check(weather.Shown==kind,"date-driven "+kind+" "+id);Check(weather.ActiveParticles<=128&&weather.ActiveParticles+weather.ActiveMistBands<=160,"particle budget "+kind+" "+id);
                    Check(Fingerprint(map)==terrain&&GameManager.I.Player.money==money&&JsonUtility.ToJson(GameManager.I.Player.cargo)==cargo,"terrain money/cargo unchanged "+kind+" "+id);
                    yield return Photo(id+"-"+kind);
                    if(kind==GoryeoAtmosphere.Kind.Mist){Day(GoryeoAtmosphere.Kind.Clear,7.5f,weather);yield return Photo(id+"-ClearMorning");}
                }
                Day(GoryeoAtmosphere.Kind.Monsoon,12,weather);int pool=weather.CreatedParticleRenderers;for(int i=0;i<20;i++){Day(GoryeoAtmosphere.Kind.Clear,12,weather);Day(GoryeoAtmosphere.Kind.Rain,12,weather);Day(GoryeoAtmosphere.Kind.Monsoon,12,weather);}Check(pool==weather.CreatedParticleRenderers&&pool==128,"pool reuse no growth "+id);
                Time.timeScale=1;float start=Time.realtimeSinceStartup;for(int i=0;i<30;i++)yield return null;r.approximateUnscaledFrameMs=(Time.realtimeSinceStartup-start)*1000/30;Time.timeScale=0;
                string original=map.mapId;map.mapId="north_birka";weather.Refresh();Check(weather.ActiveParticles==0&&weather.ActiveMistBands==0,"foreign scope idle "+id);map.mapId=original;
                weather.enabled=false;Check(weather.ActiveParticles==0,"disable hides effect "+id);weather.enabled=true;weather.Refresh();Check(weather.ActiveParticles==128,"enable restores current weather "+id);
                Check(Fingerprint(map)==terrain,"final terrain unchanged "+id);Check(!GameManager.I.AutosaveEnabled,"saves off "+id);
            }
        }
    }
}
#endif
