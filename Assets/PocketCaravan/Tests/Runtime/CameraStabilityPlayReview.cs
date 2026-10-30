#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class CameraStabilityPlayReview:MonoBehaviour
    {
        public const string Output="ArtReview/CameraStability-20261030";
        [Serializable] public class Row {public string map;public int probes;public float maxBeforeStep,maxAfterStep,maxBeforeTurn,maxAfterTurn,maxLiveFrameStep;}
        [Serializable] public class Report {public bool complete;public string error;public List<Row> maps=new List<Row>();}
        float previousCaptureDelta;readonly Report report=new Report();PlayerMotor Motor=>PlayerController.Instance.Motor;
        void OnEnable(){SceneManager.sceneLoaded+=IsolateInput;}
        void OnDisable(){SceneManager.sceneLoaded-=IsolateInput;}
        static void IsolateInput(Scene scene,LoadSceneMode mode){foreach(var pc in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))pc.enabled=false;}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object item=null;bool moved=false;Exception error=null;try{moved=stack.Peek().MoveNext();if(moved)item=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!moved){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}Finish(null);}
        void Finish(Exception e){report.complete=e==null;report.error=e==null?null:e.ToString();Save();Time.timeScale=1;Time.captureDeltaTime=previousCaptureDelta;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        void Save(){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/checks.json",JsonUtility.ToJson(report,true));}
        void Pose(Vector2 feet,Vector2Int face){Motor.Teleport((Vector3)(feet-Motor.feetOffset),face);UnityEngine.Object.FindFirstObjectByType<CameraFollow>().SnapNow();}
        Vector3 Before(){var cam=Camera.main;var map=MapRoot.Current;var p=Motor.transform.position;var b=map.WorldBounds;float h=cam.orthographicSize,w=h*cam.aspect;p.x=b.size.x>2*w?Mathf.Clamp(p.x,b.min.x+w,b.max.x-w):b.center.x;p.y=b.size.y>2*h?Mathf.Clamp(p.y,b.min.y+h,b.max.y-h):b.center.y;
            var best=new MapExit[4];var score=new[]{float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity};
            foreach(var e in MapExit.All){if(GoryeoTravelTransition.RegionOf(e.targetScene)==null)continue;Rect r=e.WorldRect;int a=e.edge=="W"?0:e.edge=="E"?1:e.edge=="S"?2:e.edge=="N"?3:-1;if(a<0)continue;var f=Motor.Feet;float l=a<2?Mathf.Max(Mathf.Max(r.yMin-f.y,f.y-r.yMax),0):Mathf.Max(Mathf.Max(r.xMin-f.x,f.x-r.xMax),0);float d=a==0?f.x-r.xMin:a==1?r.xMax-f.x:a==2?f.y-r.yMin:r.yMax-f.y;if(l>.75f||d<-.001f||d>(a<2?w:h)+1)continue;float s=l*1000+d;if(s<score[a]){score[a]=s;best[a]=e;}}
            float left=best[0]!=null?best[0].WorldRect.xMin+w:float.NegativeInfinity,right=best[1]!=null?best[1].WorldRect.xMax-w:float.PositiveInfinity,bottom=best[2]!=null?best[2].WorldRect.yMin+h:float.NegativeInfinity,top=best[3]!=null?best[3].WorldRect.yMax-h:float.PositiveInfinity;
            p.x=left<=right?Mathf.Clamp(p.x,left,right):Motor.Facing.x>0?right:left;p.y=bottom<=top?Mathf.Clamp(p.y,bottom,top):Motor.Facing.y>0?top:bottom;p.z=-10;return p;}
        bool Free(Vector2 f){var m=MapRoot.Current;var half=Motor.feetSize*.5f;foreach(var d in new[]{new Vector2(-half.x,-half.y),new Vector2(half.x,-half.y),new Vector2(-half.x,half.y),half})if(!m.IsWalkable(m.WorldToCell(f+d)))return false;return true;}
        IEnumerator Run(){previousCaptureDelta=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;Directory.CreateDirectory(Output);for(int i=0;i<10;i++)yield return null;if(GameManager.I.AutosaveEnabled)throw new Exception("Autosave must be disabled by preview startup");
            foreach(string id in new[]{"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_crossroads","transit_southport","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"}){
                string path=null;foreach(var scene in EditorBuildSettings.scenes)if(Path.GetFileNameWithoutExtension(scene.path)==id)path=scene.path;if(path==null)throw new Exception(id);
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<3;i++)yield return null;Time.timeScale=0;PlayerController.Instance.enabled=false;if(GameManager.I.AutosaveEnabled)throw new Exception("Autosave enabled");
                var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.smoothTime=0;var row=new Row{map=id};report.maps.Add(row);var candidates=new List<Vector2>();var bounds=MapRoot.Current.ground.cellBounds;
                for(int y=bounds.yMin;y<bounds.yMax;y+=2)for(int x=bounds.xMin;x<bounds.xMax;x+=2)candidates.Add(MapRoot.Current.CellCenter(new Vector3Int(x,y,0)));
                foreach(var e in MapExit.All){Rect r=e.WorldRect;foreach(float shift in new[]{-.76f,-.75f,-.74f,.74f,.75f,.76f})candidates.Add(e.edge=="E"||e.edge=="W"?new Vector2(r.center.x,r.yMax+shift):new Vector2(r.xMax+shift,r.center.y));}
                int count=0;foreach(var feet in candidates){if(!Free(feet))continue;Pose(feet,Vector2Int.right);Vector3 after=Camera.main.transform.position,before=Before();
                    foreach(var delta in new[]{new Vector2(.02f,0),new Vector2(0,.02f)}){if(!Free(feet+delta))continue;Pose(feet+delta,Vector2Int.right);row.maxAfterStep=Mathf.Max(row.maxAfterStep,Vector3.Distance(after,Camera.main.transform.position));row.maxBeforeStep=Mathf.Max(row.maxBeforeStep,Vector3.Distance(before,Before()));row.probes++;}
                    Pose(feet,Vector2Int.left);row.maxAfterTurn=Mathf.Max(row.maxAfterTurn,Vector3.Distance(after,Camera.main.transform.position));row.maxBeforeTurn=Mathf.Max(row.maxBeforeTurn,Vector3.Distance(before,Before()));
                    if(row.maxAfterStep>.12f||row.maxAfterTurn>.0001f)throw new Exception(id+" camera discontinuity "+JsonUtility.ToJson(row));if(++count%120==0)yield return null;
                }Save();
                // Rendered updates exercise SmoothDamp and actual motor collision, not only target math.
                var start=candidates.Find(p=>Free(p));Pose(start,Vector2Int.right);follow.smoothTime=.12f;Time.timeScale=1;Vector3 previous=Camera.main.transform.position;
                foreach(var direction in new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down})for(int frame=0;frame<30;frame++){Motor.Tick(direction,3.8f,1f/60);yield return null;float step=Vector3.Distance(previous,Camera.main.transform.position);row.maxLiveFrameStep=Mathf.Max(row.maxLiveFrameStep,step);if(step>1f)throw new Exception(id+" live camera step "+step);previous=Camera.main.transform.position;}
                Time.timeScale=0;Save();yield return null;
            }
        }
    }
}
#endif
