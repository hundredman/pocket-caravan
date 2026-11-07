#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    /// <summary>Isolated geometry review, not a replacement for production travel/save migration.</summary>
    public sealed class T3BoundaryExplorer:MonoBehaviour
    {
        public bool capture,auditForeign;
        static T3BoundaryExplorer active;
        void Awake(){if(active!=null&&active!=this){enabled=false;Destroy(gameObject);return;}active=this;}
        void OnDestroy(){if(active==this)active=null;}
        const string Root="Assets/PocketCaravan/Editor/B2Preview/T3Boundaries";
        const string Out="Docs/Validation/T3-Boundaries-2026-11-07/";
        const string Images="ArtReview/T3-Boundaries-20261107/";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
        [Serializable]class Plan{public Metric[] maps;}
        [Serializable]class Metric{public string mapId;}
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();public List<Row> samples=new List<Row>();}
        [Serializable]class Row{public string mapId,target,edge;public Vector3 viewport;public int exits;public bool passed;}
        readonly Report report=new Report();string slot;bool loading;TravelTransition fx;float fade;bool covering;float held;
        void Check(bool ok,string message){if(!ok)throw new Exception(message);report.checks.Add(message);}
        IEnumerator Start()
        {
            if(active!=this)yield break;
            DontDestroyOnLoad(gameObject);
            yield return null;
            Check(FindObjectsByType<T3BoundaryExplorer>(FindObjectsSortMode.None).Length==1,"exactly one preview controller");
            var gm=GameManager.I;
            if(gm==null||gm.AutosaveEnabled){Debug.LogError("T3 requires previewOnly SaveOFF scene");EditorApplication.isPlaying=false;yield break;}
            SaveSystem.UseSlot("codex_t3_preview_"+Guid.NewGuid().ToString("N")+".json");slot=SaveSystem.CurrentPath;
            gm.Clock.day=1;gm.Clock.minuteOfDay=12*60;
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)typeof(NetLogin).GetMethod("PlayAlone",F).Invoke(login,null);
            if(auditForeign)yield return Guarded(AuditExternal());else if(capture)yield return Guarded(CaptureAll());else{yield return Frames();Quiet();TeleportToStart();}
        }
        IEnumerator Guarded(IEnumerator task)
        {
            var stack=new Stack<IEnumerator>();stack.Push(task);Exception failure=null;
            while(stack.Count>0){bool more=false;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null)break;if(!more){stack.Pop();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            report.complete=failure==null;report.error=failure?.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+(auditForeign?"foreign-audit.json":"unity.json"),JsonUtility.ToJson(report,true));Time.timeScale=1;EditorApplication.isPlaying=false;
        }
        IEnumerator Frames(){for(int i=0;i<8;i++)yield return null;yield return new WaitForEndOfFrame();}
        void Quiet()
        {
            foreach(var s in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){s.StopAllCoroutines();s.enabled=false;}
            foreach(var s in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None))s.StopAllCoroutines();
            var dialog=FindFirstObjectByType<DialogueUI>();if(dialog!=null)typeof(DialogueUI).GetMethod("Close",F).Invoke(dialog,null);
            UIState.Reset();Notify.Clear();GameManager.I.SetMemo("goryeo:intro",0);
            var pc=PlayerController.Instance;pc.enabled=false;
            // Existing camera behaviour is used. New scenes have no CameraBackdrop.
            var follow=Camera.main.GetComponent<CameraFollow>();follow.smoothTime=0.12f;
            var labels=MapRoot.Current.GetComponent<GoryeoPlaceLabels>()??MapRoot.Current.gameObject.AddComponent<GoryeoPlaceLabels>();labels.places=Array.Empty<GoryeoPlaceLabels.Place>();labels.Refresh();
            Time.timeScale=(capture||auditForeign)?0:1;
        }
        void TeleportToStart(){var pc=PlayerController.Instance;var spawn=SpawnPoint.Find("S0");pc.Motor.Teleport(spawn.transform.position,Vector2Int.down);Camera.main.GetComponent<CameraFollow>().SnapNow();}
        IEnumerator Load(string id){string path=Root+"/Scenes/"+id+".unity";if(File.Exists(path))yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));else yield return SceneManager.LoadSceneAsync(id);yield return Frames();Quiet();Check(!GameManager.I.AutosaveEnabled&&SaveSystem.CurrentPath==slot,"SaveOFF retained "+id);}
        IEnumerator Photo(string name){yield return new WaitForEndOfFrame();Directory.CreateDirectory(Images);var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Images+name+".png",image.EncodeToPNG());Destroy(image);}
        IEnumerator CaptureAll()
        {
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Out+"geometry.json"));
            foreach(var metric in plan.maps){
                yield return Load(metric.mapId);TeleportToStart();Check(MapRoot.Current.IsWalkable(PlayerController.Instance.Motor.Cell,PlayerController.Instance.Motor),"default start clear of actual NPC blockers "+metric.mapId);yield return Photo(metric.mapId+"-game");
                var map=MapRoot.Current;var pc=PlayerController.Instance;var motor=pc.Motor;var list=new List<MapExit>(MapExit.All);list.Sort((a,b)=>Array.IndexOf(new[]{"W","E","S","N"},a.edge).CompareTo(Array.IndexOf(new[]{"W","E","S","N"},b.edge)));Check(list.Count>0,"preview has exits "+metric.mapId);
                // Exercise every doorway, not only the photographed sample, with actual footprint checks.
                Row sample=null;
                foreach(var exit in list){
                    var dir=exit.edge=="N"?Vector2.up:exit.edge=="S"?Vector2.down:exit.edge=="E"?Vector2.right:Vector2.left;
                    var r=exit.WorldRect;var feet=r.center;var face=new Vector2Int((int)dir.x,(int)dir.y);Vector3 contact;
                    var inward=feet-dir*2;motor.Teleport((Vector3)(inward-motor.feetOffset),face);
                    Check(!exit.TryGetBoundaryContact(motor,dir,out contact),"not two cells early "+metric.mapId+"/"+exit.targetSpawnId);
                    motor.Teleport((Vector3)(r.center-dir-motor.feetOffset),face);
                    bool walked=false;for(int step=0;step<100;step++){motor.Tick(dir,3.8f,1f/60);if(exit.TryGetBoundaryContact(motor,dir,out contact)){walked=true;break;}}
                    Check(walked,"continuous motor approach reaches edge "+metric.mapId+"/"+exit.targetSpawnId);
                    Check(exit.TryGetBoundaryContact(motor,dir,out contact),"actual outward contact "+metric.mapId+"/"+exit.targetSpawnId);
                    var bounds=map.WorldBounds;
                    bool outer=exit.edge=="E"?Mathf.Abs(r.xMax-bounds.max.x)<.001f:exit.edge=="W"?Mathf.Abs(r.xMin-bounds.min.x)<.001f:exit.edge=="N"?Mathf.Abs(r.yMax-bounds.max.y)<.001f:Mathf.Abs(r.yMin-bounds.min.y)<.001f;
                    Check(outer,"exit is actual scene outer face "+metric.mapId+"/"+exit.targetSpawnId);
                    // Use a representative road portal for visual captures.
                    if(sample==null){Camera.main.GetComponent<CameraFollow>().SnapNow();yield return Photo(metric.mapId+"-edge");var vp=Camera.main.WorldToViewportPoint(motor.transform.position);
                        float edge=exit.edge=="E"?1-vp.x:exit.edge=="W"?vp.x:exit.edge=="N"?1-vp.y:vp.y;
                        Check(edge<.055f&&edge>-.055f,"rendered contact within 5.5% of screen end "+metric.mapId);
                        sample=new Row{mapId=metric.mapId,target=exit.targetScene,edge=exit.edge,viewport=vp,exits=list.Count,passed=true};
                    }
                }
                report.samples.Add(sample);
            }
            // Preview navigation uses editor scene loading, not production GameManager.TravelTo.
            yield return Load("Naru");var origin=MapRoot.Current;MapExit selected=null;foreach(var exit in MapExit.All)if(exit.targetScene=="goryeo_jangdan"){selected=exit;break;}
            Check(selected!=null,"Naru to Jangdan preview port");string destination=selected.targetSpawnId;
            yield return Load("goryeo_jangdan");var spawn=SpawnPoint.Find(destination);Check(spawn!=null,"matching preview arrival exists");var p=PlayerController.Instance;
            p.Motor.Teleport(spawn.transform.position,Vector2Int.left);Check(MapExit.At(p.Motor.Cell)==null,"arrival is not another exit");Check(MapRoot.Current.IsWalkable(p.Motor.Cell,p.Motor),"arrival footprint center walkable");
            foreach(string target in new[]{"north_c_road_se","arabia_central_eastvale","arabia_central_steppe"}){
                yield return Load("transit_crossroads");MapExit port=null;foreach(var e in MapExit.All)if(e.targetScene==target){port=e;break;}
                Check(port!=null,"preserved foreign review port "+target);var motor=PlayerController.Instance.Motor;
                motor.Teleport((Vector3)port.WorldRect.center-(Vector3)motor.feetOffset,port.edge=="N"?Vector2Int.up:Vector2Int.left);
                yield return Travel(port);Check(MapRoot.Current.mapId==target,"preview port loads original foreign scene "+target);
                MapExit back=null;foreach(var e in MapExit.All)if(e.targetScene=="transit_crossroads"){back=e;break;}
                Check(back!=null,"original foreign return exit retained "+target);
                motor=PlayerController.Instance.Motor;motor.Teleport((Vector3)back.WorldRect.center-(Vector3)motor.feetOffset,Vector2Int.right);
                yield return Travel(back);Check(MapRoot.Current.mapId=="transit_crossroads","foreign return loads boundary preview "+target);
                motor=PlayerController.Instance.Motor;Check(MapExit.At(motor.Cell)==null,"foreign arrival is inside and not an exit "+target);Check(MapRoot.Current.IsWalkable(motor.Cell,motor),"foreign arrival is walkable "+target);
            }
            Check(!File.Exists(slot),"isolated preview save never written");
        }
        IEnumerator AuditExternal()
        {
            foreach(string target in new[]{"north_c_road_se","arabia_central_eastvale","arabia_central_steppe"}){
                yield return SceneManager.LoadSceneAsync("transit_crossroads");yield return Frames();Quiet();
                var map=MapRoot.Current;var motor=PlayerController.Instance.Motor;
                Check(!GameManager.I.AutosaveEnabled&&SaveSystem.CurrentPath==slot,"external audit SaveOFF "+target);
                MapExit exit=null;foreach(var e in MapExit.All)if(e.targetScene==target){exit=e;break;}
                Check(exit!=null&&exit.isActiveAndEnabled,"active serialized exit "+target);
                var market=FindFirstObjectByType<TownMarket>();var seed=map.WorldToCell(market.transform.position);
                var visited=new HashSet<Vector3Int>();var queue=new Queue<Vector3Int>();
                foreach(var dir in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}){var p=seed+dir;if(map.IsWalkable(p,motor)&&visited.Add(p))queue.Enqueue(p);}
                Vector3Int contactCell=default;bool found=false;
                while(queue.Count>0){var p=queue.Dequeue();if(exit.Contains(p)){contactCell=p;found=true;break;}
                    foreach(var dir in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}){var n=p+dir;if(!visited.Contains(n)&&map.IsWalkable(n,motor)){visited.Add(n);queue.Enqueue(n);}}
                }
                var row=new Row{mapId="transit_crossroads",target=target,edge=exit.edge,passed=false};report.samples.Add(row);
                if(!found){report.checks.Add("UNREACHABLE from market under actual collision: "+target);continue;}
                Check(true,"reachable from market under actual collision "+target);
                var direction=exit.edge=="N"?Vector2.up:exit.edge=="S"?Vector2.down:exit.edge=="W"?Vector2.left:Vector2.right;
                motor.Teleport(map.CellCenter(contactCell)-(Vector3)motor.feetOffset,new Vector2Int((int)direction.x,(int)direction.y));
                Check(exit.TryUseAtBoundary(motor,direction),"normal GameManager travel invoked "+target);
                int frames=0;while(GameManager.I.IsTravelling){if(++frames>900)throw new TimeoutException(target);yield return null;}
                yield return Frames();Quiet();Check(MapRoot.Current.mapId==target,"target actually loaded "+target);row.passed=true;
                Check(!GameManager.I.AutosaveEnabled&&SaveSystem.CurrentPath==slot,"no autosave after external travel "+target);
            }
            Check(!File.Exists(slot),"no isolated external-audit save written");
        }
        void Update()
        {
            if(capture||auditForeign||loading||PlayerController.Instance==null)return;
            var pc=PlayerController.Instance;var motor=pc.Motor;var input=UIState.Blocking?Vector2.zero:InputReader.MoveVector();
            motor.Tick(input,pc.CurrentSpeed(),Time.deltaTime);if(pc.anim!=null){pc.anim.walking=motor.Velocity.sqrMagnitude>.001f;pc.anim.facing=motor.Facing;}
            var exit=MapExit.At(motor.Cell,motor.Facing);Vector3 contact;
            if(exit!=null&&exit.TryGetBoundaryContact(motor,input,out contact))StartCoroutine(Travel(exit));
        }
        IEnumerator Travel(MapExit exit)
        {
            loading=true;string from=MapRoot.Current.mapId,to=exit.targetScene,id=exit.targetSpawnId;var facing=PlayerController.Instance.Motor.Facing;var r=exit.WorldRect;var at=PlayerController.Instance.transform.position;
            float offset=exit.size.x>1?at.x-r.xMin-.5f:at.y-r.yMin-.5f;
            fx=TravelTransition.For(from,to);fx?.Begin(from,to);covering=true;
            float seconds=fx?.CoverSeconds??.12f;
            if(seconds>0)for(float t=0;t<1;t+=Mathf.Min(Time.unscaledDeltaTime,1f/30)/seconds){fade=t;yield return null;}
            fade=seconds>0?1:0;held=Time.unscaledTime;if(seconds>0)yield return null;
            yield return Load(to);var spawn=SpawnPoint.Find(id);PlayerController.Instance.Motor.Teleport(spawn.PositionFor(offset),facing);Camera.main.GetComponent<CameraFollow>().SnapNow();
            float loaded=Time.unscaledTime;while(fx!=null&&(Time.unscaledTime-held<fx.MinHoldSeconds||Time.unscaledTime-loaded<fx.AfterLoadSeconds))yield return null;
            covering=false;seconds=fx?.RevealSeconds??.18f;if(seconds>0)for(float t=1;t>0;t-=Mathf.Min(Time.unscaledDeltaTime,1f/30)/seconds){fade=t;yield return null;}
            fade=0;fx=null;loading=false;
        }
        void OnGUI()
        {
            if(!loading||fade<=0)return;
            int depth=GUI.depth;Color color=GUI.color;
            try{GUI.depth=-1000;if(fx!=null)fx.Draw(fade,covering,MapRoot.Current?.displayName,held>0?Time.unscaledTime-held:0);
                else{GUI.color=new Color(0,0,0,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}}
            finally{GUI.color=color;GUI.depth=depth;}
        }
    }
}
#endif
