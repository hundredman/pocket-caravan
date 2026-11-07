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
    /// <summary>Read-only production-boundary diagnosis in an isolated SaveOFF slot.</summary>
    public sealed class MapBoundaryAuditReview:MonoBehaviour
    {
        const string Out="Docs/Validation/Map-Boundary-Audit-2026-11-07/";
        const string Images="ArtReview/Map-Boundary-Audit-20261107/";
        const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
        public static Action<int,int> Resize;
        [Serializable]class Data{public Sample[] samples;}
        [Serializable]class Sample{public string mapId,target,edge;public int x,y,w,h,insetCells,probeX,probeY;}
        [Serializable]class Row{public string mapId,target,edge;public int insetCells;public bool centerTriggers,boundaryTriggers,actualTravel,proposalOnly;public Vector3 playerViewport,noBackdropViewport,camera,noBackdropCamera;public Vector2 viewCells;}
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();public List<Row> rows=new List<Row>();}
        readonly Report report=new Report();string slot;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.checks.Add(text);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Images);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool more=false;object current=null;Exception error=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!more){stack.Pop();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;}Finish(null);}
        void Finish(Exception e){report.complete=e==null;report.error=e?.ToString();File.WriteAllText(Out+"unity.json",JsonUtility.ToJson(report,true));Time.timeScale=1;EditorApplication.isPlaying=false;}
        IEnumerator Frames(){for(int i=0;i<8;i++)yield return null;yield return new WaitForEndOfFrame();}
        void Quiet(){foreach(var s in FindObjectsByType<StoryGuide>(FindObjectsSortMode.None)){s.StopAllCoroutines();s.enabled=false;}foreach(var s in FindObjectsByType<GoryeoIntro>(FindObjectsSortMode.None))s.StopAllCoroutines();var d=FindFirstObjectByType<DialogueUI>();typeof(DialogueUI).GetMethod("Close",F).Invoke(d,null);UIState.Reset();Notify.Clear();GameManager.I.SetMemo("goryeo:intro",0);Time.timeScale=0;}
        IEnumerator Photo(string name){yield return Frames();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Images+name+".png",t.EncodeToPNG());Destroy(t);}
        [Serializable]class RawMap{public int w,h,terrainX,terrainY;public int[] blocked;}
        IEnumerator RectanglePrototype(GameManager gm)
        {
            yield return SceneManager.LoadSceneAsync("Naru");yield return Frames();Quiet();
            var map=MapRoot.Current;map.previewOnly=true;var pc=PlayerController.Instance;pc.enabled=false;var motor=pc.Motor;
            var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();follow.enabled=false;follow.smoothTime=0;
            MapExit oldExit=null;foreach(var e in MapExit.All)if(e.targetScene=="goryeo_jangdan"&&e.edge=="N"&&map.WorldToCell(e.transform.position)==new Vector3Int(21,51,0)){oldExit=e;break;}
            Check(oldExit!=null,"original paired destination spawn retained for prototype");string spawn=oldExit.targetSpawnId;
            foreach(var e in new List<MapExit>(MapExit.All))e.enabled=false;
            var walk=new HashSet<Vector3Int>();
            foreach(string id in new[]{"Naru","goryeo_jangdan"}){
                var d=JsonUtility.FromJson<RawMap>(File.ReadAllText("Assets/PocketCaravan/Maps/"+id+".json"));
                for(int y=0;y<d.h;y++)for(int x=0;x<d.w;x++)if(d.blocked[y*d.w+x]==0)
                    walk.Add(new Vector3Int(d.terrainX+x-202,110-(d.terrainY+y),0));
            }
            // Bounds/collision-only temporary layers. Existing rendered art is neither edited nor saved.
            var originalGround=map.ground;var tile=originalGround.GetTile(new Vector3Int(22,51,0));Check(tile!=null,"whole existing terrain tile available");
            var g=new GameObject("Rectangle bounds preview");g.transform.SetParent(map.transform,false);var ground=g.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var b=new GameObject("Rectangle collision preview");b.transform.SetParent(map.transform,false);var blocked=b.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            for(int y=0;y<86;y++)for(int x=22;x<88;x++){
                var cell=new Vector3Int(x,y,0);ground.SetTile(cell,tile);if(!walk.Contains(cell))blocked.SetTile(cell,tile);
            }
            ground.CompressBounds();map.ground=ground;map.blocked=blocked;
            typeof(CameraFollow).GetField("backdropMap",F).SetValue(follow,map);typeof(CameraFollow).GetField("backdropBounds",F).SetValue(follow,null);
            var gate=new GameObject("Preview Naru west edge").AddComponent<MapExit>();gate.transform.SetParent(map.transform,false);gate.transform.position=new Vector3(22.01f,51.01f,0);gate.edge="W";gate.size=Vector2Int.one;gate.targetScene="goryeo_jangdan";gate.targetSpawnId=spawn;
            motor.Teleport(new Vector3(24.1f,51.5f,0)-(Vector3)motor.feetOffset,Vector2Int.left);follow.SnapNow();Vector3 contact;
            Check(!gate.TryGetBoundaryContact(motor,Vector2.left,out contact),"preview does not travel two cells before the edge");
            Check(!map.IsWalkable(new Vector3Int(21,51,0)),"preview is bounded at its outer edge");
            int steps=0;while(!gate.TryGetBoundaryContact(motor,Vector2.left,out contact)){
                if(++steps>240)throw new TimeoutException("preview approach blocked");motor.Tick(Vector2.left,3.8f,1f/60);follow.SnapNow();yield return null;
            }
            yield return Photo("Naru-rectangle-preview");
            var row=new Row{mapId="Naru-rectangle-preview",target="goryeo_jangdan",edge="W",insetCells=0,proposalOnly=true,boundaryTriggers=true,playerViewport=cam.WorldToViewportPoint(motor.transform.position),camera=cam.transform.position,viewCells=new Vector2(cam.orthographicSize*2*cam.aspect,cam.orthographicSize*2)};
            Check(row.playerViewport.x>=-.02f&&row.playerViewport.x<.02f,"rectangle prototype reaches actual left screen edge before travel");
            var fx=TravelTransition.For("Naru","goryeo_jangdan");Check(fx!=null,"same existing regional transition effect retained");
            Check(gate.TryUseAtBoundary(motor,Vector2.left),"prototype uses normal MapExit travel");int frames=0;while(gm.IsTravelling){if(++frames>600)throw new TimeoutException("prototype travel");yield return null;}
            yield return Frames();Check(MapRoot.Current.mapId=="goryeo_jangdan","prototype arrives using original paired spawn");row.actualTravel=true;report.rows.Add(row);
            Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slot,"preview never enables autosave");
        }
        IEnumerator Run()
        {
            var gm=GameManager.I;Check(gm!=null&&!gm.AutosaveEnabled,"SaveOFF bootstrap");
            var login=FindFirstObjectByType<NetLogin>();if(login!=null)typeof(NetLogin).GetMethod("PlayAlone",F).Invoke(login,null);
            typeof(GameManager).GetField("firstSceneLoaded",F).SetValue(gm,true);SaveSystem.UseSlot("codex_map_boundary_audit_"+Guid.NewGuid().ToString("N")+".json");slot=SaveSystem.CurrentPath;
            Resize(960,540);yield return Frames();
            var data=JsonUtility.FromJson<Data>(File.ReadAllText(Out+"metrics.json"));
            foreach(var sample in data.samples){
                yield return SceneManager.LoadSceneAsync(sample.mapId);yield return Frames();Quiet();
                var map=MapRoot.Current;var pc=PlayerController.Instance;pc.enabled=false;var motor=pc.Motor;
                var camera=Camera.main;var follow=camera.GetComponent<CameraFollow>();follow.enabled=false;follow.smoothTime=0;
                var probe=new Vector3Int(sample.probeX,map.ground.cellBounds.yMax-1-sample.probeY,0);MapExit exit=null;
                foreach(var item in MapExit.All)if(item.targetScene==sample.target&&item.edge==sample.edge&&item.size==new Vector2Int(sample.w,sample.h)&&item.Contains(probe)){exit=item;break;}
                Check(exit!=null,"exact registered exit "+sample.mapId+" "+sample.edge);
                Vector2 dir=sample.edge=="E"?Vector2.right:sample.edge=="W"?Vector2.left:sample.edge=="N"?Vector2.up:Vector2.down;
                var facing=new Vector2Int((int)dir.x,(int)dir.y);Vector3 contact;
                Vector2 feet=map.CellCenter(probe);motor.Teleport((Vector3)(feet-motor.feetOffset),facing);
                bool center=exit.TryGetBoundaryContact(motor,dir,out contact);
                if(!center){var box=exit.WorldRect;if(dir.x>0)feet.x=box.xMax-.00005f;else if(dir.x<0)feet.x=box.xMin+.00005f;else if(dir.y>0)feet.y=box.yMax-.00005f;else feet.y=box.yMin+.00005f;motor.Teleport((Vector3)(feet-motor.feetOffset),facing);motor.Tick(dir,3.8f,1f/60);}
                bool boundary=exit.TryGetBoundaryContact(motor,dir,out contact);Check(boundary,"actual exit contact ready "+sample.mapId);
                follow.SnapNow();var row=new Row{mapId=sample.mapId,target=sample.target,edge=sample.edge,insetCells=sample.insetCells,centerTriggers=center,boundaryTriggers=boundary,playerViewport=camera.WorldToViewportPoint(motor.transform.position),camera=camera.transform.position,viewCells=new Vector2(2*camera.orthographicSize*camera.aspect,2*camera.orthographicSize)};
                yield return Photo(sample.mapId+"-current");
                // Projection matrices are sampled after the rendered frame, never immediately after moving the camera.
                row.playerViewport=camera.WorldToViewportPoint(motor.transform.position);row.camera=camera.transform.position;
                Check(row.playerViewport.x>=-.02f&&row.playerViewport.x<=1.02f&&row.playerViewport.y>=-.02f&&row.playerViewport.y<=1.02f,"rendered transition point inside actual viewport "+sample.mapId);
                // Temporary camera-only experiment; no runtime/source/map changes are saved.
                var fm=typeof(CameraFollow).GetField("backdropMap",F);var fb=typeof(CameraFollow).GetField("backdropBounds",F);object oldMap=fm.GetValue(follow),oldBounds=fb.GetValue(follow);
                fm.SetValue(follow,map);fb.SetValue(follow,null);follow.SnapNow();row.noBackdropViewport=camera.WorldToViewportPoint(motor.transform.position);row.noBackdropCamera=camera.transform.position;
                yield return Photo(sample.mapId+"-camera-only-test");row.noBackdropViewport=camera.WorldToViewportPoint(motor.transform.position);row.noBackdropCamera=camera.transform.position;fm.SetValue(follow,oldMap);fb.SetValue(follow,oldBounds);follow.SnapNow();
                if(sample.mapId=="Naru"){Check(!center,"Goryeo cell-center threshold already delayed");Check(row.playerViewport.y>.2f&&row.playerViewport.y<.8f,"interior Naru exit switches near screen center");Check(row.noBackdropViewport.y>.2f&&row.noBackdropViewport.y<.8f,"removing backdrop camera expansion alone still switches inside frame");}
                else if(sample.mapId=="north_birka")Check(row.playerViewport.x>.95f,"North transition is visually near right edge");
                else if(sample.mapId=="venice_rialto")Check(row.playerViewport.x<.05f,"Venice transition is visually near left edge");
                Check(exit.TryUseAtBoundary(motor,dir),"actual travel invoked "+sample.mapId);int frames=0;
                while(gm.IsTravelling){if(++frames>600)throw new TimeoutException("travel "+sample.mapId);yield return null;}
                yield return Frames();Check(MapRoot.Current.mapId==sample.target,"registered target actually loaded "+sample.target);row.actualTravel=true;report.rows.Add(row);
                Check(!gm.AutosaveEnabled&&SaveSystem.CurrentPath==slot,"isolated slot retained after travel");
            }
            yield return RectanglePrototype(gm);
            Check(!File.Exists(slot),"no isolated QA slot file written");
        }
    }
}
#endif
