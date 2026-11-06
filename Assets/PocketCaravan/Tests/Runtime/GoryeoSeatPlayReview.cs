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
        public bool motion,expanded,alignment;
        string Out=>alignment?"ArtReview/Goryeo-Seats-20261105/AlignedV2/Unity/":expanded?"ArtReview/Goryeo-Seats-20261105/Expanded/Unity/":motion?"ArtReview/Goryeo-Seats-20261105/Motion/":"ArtReview/Goryeo-Seats-20261105/Unity/";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        [Serializable]class MotionSample{public string phase;public int frame,embers;public float light;public Vector3 ember;}
        [Serializable]class MotionReport{public string framesPath;public List<MotionSample> samples=new List<MotionSample>();}
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
            var t=new Texture2D(16,24,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(alignment?"ArtReview/Goryeo-Seats-20261105/AlignedV2/goryeo-seated-front.png":"ArtReview/Goryeo-Seats-20261105/tanuki-seated-front.png"));t.filterMode=FilterMode.Point;
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
                foreach(var fx in FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None))fx.enabled=false;
                foreach(var candidate in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))foreach(var prop in candidate.GetComponentsInChildren<SpriteRenderer>(true))prop.enabled=false;
                yield return Photo(seat.site.id+"-before");
                foreach(var candidate in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))foreach(var prop in candidate.GetComponentsInChildren<SpriteRenderer>(true))prop.enabled=true;
                foreach(var fx in FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None))fx.enabled=true;
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
            if(motion)yield return CaptureMotion();
            if(expanded)yield return ExpandedPoses();
            foreach(var s in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))s.Stand();
            var last=FindFirstObjectByType<GoryeoSeat>();pc.Motor.Teleport(last.transform.position,Vector2Int.down);last.Interact(pc);Check(GoryeoSeat.Sitting(pc),"sitting before real scene unload");
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/Goryeo/Naru.unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<8;i++)yield return null;Prepare();Check(!GoryeoSeat.Sitting(PlayerController.Instance)&&FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==0,"scene unload releases without leaked seats");
            if(motion||expanded)Check(FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None).Length==0,"scene unload removes campfire motion");
            Check(!gm.AutosaveEnabled,"saves OFF at end");
        }
        IEnumerator CaptureMotion()
        {
            var fx=FindFirstObjectByType<GoryeoCampfire>();Check(fx!=null&&FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None).Length==1,"exactly one explicitly installed campfire");
            var seat=fx.GetComponent<GoryeoSeat>();var pc=PlayerController.Instance;var gm=GameManager.I;var cam=Camera.main;
            pc.Motor.Teleport(seat.transform.position+Vector3.down*.5f,Vector2Int.up);seat.Interact(pc);
            cam.transform.position=seat.transform.position+Vector3.up*2+new Vector3(0,0,-10);
            var r=new MotionReport{framesPath=Path.Combine(Path.GetTempPath(),"pocket-campfire-motion-20261105")};Directory.CreateDirectory(r.framesPath);
            int money=gm.Player.money;string cargo=JsonUtility.ToJson(gm.Player.cargo);float dayMax=0,nightMin=1;
            foreach(string phase in new[]{"day","night"})
            {
                gm.Clock.minuteOfDay=phase=="day"?12*60:21*60;
                fx.enabled=false;yield return Photo(phase+"-static");fx.enabled=true;
                var frames=new HashSet<int>();var positions=new HashSet<Vector3>();float min=1,max=0;
                for(int i=0;i<20;i++)
                {
                    yield return new WaitForSecondsRealtime(.12f);Notify.Clear();yield return new WaitForEndOfFrame();
                    Check(fx.Visible&&fx.VisibleEmbers<=GoryeoCampfire.EmberLimit,"visible bounded motion "+phase+i);
                    var image=ScreenCapture.CaptureScreenshotAsTexture();Check(image.width==480&&image.height==270,"motion actual480x270 "+phase+i);
                    File.WriteAllBytes(Path.Combine(r.framesPath,phase+"-"+i.ToString("00")+".png"),image.EncodeToPNG());Destroy(image);
                    var ember=fx.Root.Find("Pooled ember 0").position;
                    r.samples.Add(new MotionSample{phase=phase,frame=fx.FrameIndex,light=fx.LightIntensity,embers=fx.VisibleEmbers,ember=ember});
                    frames.Add(fx.FrameIndex);positions.Add(ember);min=Mathf.Min(min,fx.LightIntensity);max=Mathf.Max(max,fx.LightIntensity);
                }
                Check(frames.Count==4&&positions.Count>4&&max-min>.0001f,"four flame frames / moving embers / varying light "+phase);
                Check(fx.Root.GetComponentsInChildren<SpriteRenderer>().Length==4&&fx.Root.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>().Length==1,"four renderers one light fixed "+phase);
                yield return Photo(phase+"-animated");if(phase=="day")dayMax=max;else nightMin=min;
            }
            Check(dayMax<.09f&&nightMin>.30f&&nightMin<.37f,"gentle day/night brightness limits");
            var at=cam.transform.position;cam.transform.position+=Vector3.right*1000;
            for(int i=0;i<3;i++){yield return null;yield return new WaitForEndOfFrame();}
            File.WriteAllText(Out+"culling-diagnostic.txt","camera="+cam.transform.position+" main="+Camera.main.transform.position+" viewport="+Camera.main.WorldToViewportPoint(fx.Center)+" visible="+fx.Visible+" embers="+fx.VisibleEmbers+" light="+fx.LightIntensity);
            Check(!fx.Visible&&fx.VisibleEmbers==0&&fx.LightIntensity==0,"offscreen renderers/light culled");cam.transform.position=at;
            for(int i=0;i<3;i++){yield return null;yield return new WaitForEndOfFrame();}Check(fx.Visible,"onscreen animation resumes");
            fx.enabled=false;Check(!fx.Visible&&fx.VisibleEmbers==0&&fx.LightIntensity==0,"disable hides every effect");fx.enabled=true;
            MapRoot.Current.previewOnly=false;yield return null;yield return new WaitForEndOfFrame();Check(!fx.Visible&&fx.LightIntensity==0,"production scene guard keeps effect off");MapRoot.Current.previewOnly=true;
            Check(gm.Player.money==money&&JsonUtility.ToJson(gm.Player.cargo)==cargo&&!gm.AutosaveEnabled,"motion has no economy/save effects");
            File.WriteAllText(Out+"samples.json",JsonUtility.ToJson(r,true));
        }
        IEnumerator ExpandedPoses()
        {
            var config=JsonUtility.FromJson<GoryeoSeat.Config>(File.ReadAllText(alignment?"ArtReview/Goryeo-Seats-20261105/AlignedV2/source.json":"ArtReview/Goryeo-Seats-20261105/Expanded/source.json"));
            var art=new List<GoryeoSeat.Art>();
            foreach(var data in config.poses)
            {
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(data.file));texture.filterMode=FilterMode.Point;
                var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,data.pivotY),data.ppu);
                art.Add(new GoryeoSeat.Art{originId=data.originId,sprite=sprite});
            }
            Check(GoryeoSeat.InstallReview(config,art.ToArray())==3,"all-origin native art explicitly installed at three review seats");
            yield return null;yield return new WaitForEndOfFrame();
            Check(FindObjectsByType<GoryeoCampfire>(FindObjectsSortMode.None).Length==1,"reinstall cleans previous fire effect");
            bool rejected=false;try{GoryeoSeat.InstallReview(config,new[]{art[0],art[0]});}catch(ArgumentException){rejected=true;}
            Check(rejected&&FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None).Length==3,"duplicate art rejects without deleting valid seats");
            var gm=GameManager.I;var pc=PlayerController.Instance;var cam=Camera.main;
            gm.Clock.minuteOfDay=12*60;
            foreach(var data in config.poses)
            {
                string file=data.originId=="goryeo"?"Assets/PocketCaravan/Data/Goryeo/Origin_Goryeo.asset":
                    "Assets/PocketCaravan/Data/Origins/Origin_"+(data.originId=="venice"?"Venice":data.originId=="viking"?"Viking":"Arabia")+".asset";
                var origin=AssetDatabase.LoadAssetAtPath<OriginData>(file);Check(origin!=null,"existing origin "+data.originId);gm.Player.origin=origin;
                for(int n=0;n<3;n++)yield return null;
                int money=gm.Player.money;string cargo=JsonUtility.ToJson(gm.Player.cargo);var vehicle=gm.Player.vehicle;
                foreach(var seat in FindObjectsByType<GoryeoSeat>(FindObjectsSortMode.None))
                {
                    pc.Motor.Teleport(seat.transform.position,Vector2Int.down);cam.transform.position=seat.transform.position+Vector3.up*2+new Vector3(0,0,-10);
                    var standing=pc.transform.position;var scale=pc.anim.transform.localScale;
                    if(seat.site.fire)yield return Photo(data.originId+"-before");
                    seat.Interact(pc);yield return new WaitForEndOfFrame();
                    Check(GoryeoSeat.Sitting(pc)&&pc.anim.GetComponent<SpriteRenderer>().sprite==seat.PoseFor(origin),"correct native seated identity "+data.originId+seat.site.id);
                    var pose=seat.PoseFor(origin);var idle=origin.playerFrames[0];
                    Check(pose.rect.size==idle.rect.size&&pose.pixelsPerUnit==idle.pixelsPerUnit&&Vector2.Distance(pose.pivot,idle.pivot)<.01f&&pc.anim.transform.localScale==scale,"canvas PPU pivot scale retained "+data.originId+seat.site.id);
                    Check(MapRoot.Current.IsWalkable(pc.Motor.Cell)&&pc.Motor.Velocity==Vector2.zero&&!UIState.Blocking,"safe feet and ordinary UI "+data.originId+seat.site.id);
                    if(seat.site.fire)yield return Photo(data.originId+"-after");
                    float seatedSpeed=pc.CurrentSpeed();GoryeoSeat.Handle(pc,Vector2.right,false,false);
                    Check(!GoryeoSeat.Sitting(pc)&&Vector3.Distance(pc.transform.position,standing)<.001f&&Mathf.Abs(pc.CurrentSpeed()-seatedSpeed)<.001f,"movement stands at original speed/position "+data.originId+seat.site.id);
                    seat.Interact(pc);GoryeoSeat.Handle(pc,Vector2.zero,true,false);Check(!GoryeoSeat.Sitting(pc),"Space stands "+data.originId+seat.site.id);
                }
                Check(gm.Player.origin==origin&&gm.Player.money==money&&JsonUtility.ToJson(gm.Player.cargo)==cargo&&gm.Player.vehicle==vehicle&&!gm.AutosaveEnabled,"origin cargo vehicle money saves retained "+data.originId);
            }
            var last=FindFirstObjectByType<GoryeoSeat>();pc.Motor.Teleport(last.transform.position,Vector2Int.down);last.Interact(pc);
            Check(GoryeoSeat.Sitting(pc),"seated before origin switch");
            gm.Player.origin=AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Goryeo/Origin_Goryeo.asset");
            yield return null;yield return new WaitForEndOfFrame();Check(!GoryeoSeat.Sitting(pc),"changing origin stands without wrong-race pose");
            var unknown=ScriptableObject.CreateInstance<OriginData>();unknown.id="qa_unknown_origin";gm.Player.origin=unknown;
            last.Interact(pc);Check(!GoryeoSeat.Sitting(pc),"unknown/costume without pose never becomes another animal");
            gm.Player.origin=AssetDatabase.LoadAssetAtPath<OriginData>("Assets/PocketCaravan/Data/Goryeo/Origin_Goryeo.asset");Destroy(unknown);
            for(int n=0;n<3;n++)yield return null;
        }
    }
}
#endif
