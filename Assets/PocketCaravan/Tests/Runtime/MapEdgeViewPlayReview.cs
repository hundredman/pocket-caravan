#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class MapEdgeViewPlayReview:MonoBehaviour
    {
        const string Root="Assets/PocketCaravan/Editor/B2Preview/VillageFill";const string Out="ArtReview/MapIsolation-20261030/Edges";
        [Serializable] public class Row{public bool passed;public string test,map;}
        [Serializable] public class Report{public bool complete;public string error;public List<Row> checks=new List<Row>();}
        readonly Report report=new Report();void Check(bool ok,string test,string id){report.checks.Add(new Row{passed=ok,test=test,map=id});if(!ok)throw new Exception(test+" "+id);}
        void OnEnable(){SceneManager.sceneLoaded+=Isolate;}void OnDisable(){SceneManager.sceneLoaded-=Isolate;}static void Isolate(Scene s,LoadSceneMode m){foreach(var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))p.enabled=false;}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object item=null;bool moved=false;Exception e=null;try{moved=stack.Peek().MoveNext();if(moved)item=stack.Peek().Current;}catch(Exception ex){e=ex;}if(e!=null){Finish(e);yield break;}if(!moved){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}Finish(null);}
        void Finish(Exception e){report.complete=e==null;report.error=e==null?null:e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/checks.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Load(string path){yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<3;i++)yield return null;Time.timeScale=0;Check(!GameManager.I.AutosaveEnabled,"user saves disabled",path);GameManager.I.Clock.minuteOfDay=12*60;GameManager.I.Clock.day=1;Notify.Clear();}
        string Production(string id){foreach(var s in EditorBuildSettings.scenes)if(Path.GetFileNameWithoutExtension(s.path)==id)return s.path;throw new Exception(id);}
        void Pose(Vector2 feet){var p=PlayerController.Instance;p.Motor.Teleport((Vector3)(feet-p.Motor.feetOffset),Vector2Int.down);Camera.main.GetComponent<CameraFollow>().SnapNow();Notify.Clear();}
        IEnumerator Shot(string name){yield return null;yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Out+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        IEnumerator Run(){Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;Check(!GameManager.I.AutosaveEnabled,"safe preview startup","");
            foreach(string id in new[]{"Naru","goryeo_songak","goryeo_jangdan","transit_woodtown"}){
                yield return Load(Production(id));MapExit chosen=null;var roads=MapRoot.Current.GetComponent<RoadGrid>();foreach(var e in MapExit.All)if(GoryeoTravelTransition.RegionOf(e.targetScene)!=null&&roads.GradeAt(MapRoot.Current.WorldToCell(e.transform.position))>0){chosen=e;break;}Check(chosen!=null,"road boundary available",id);Rect r=chosen.WorldRect;Vector2 feet=r.center;switch(chosen.edge){case "E":feet.x=r.xMax-.0001f;break;case "W":feet.x=r.xMin+.0001f;break;case "N":feet.y=r.yMax-.0001f;break;case "S":feet.y=r.yMin+.0001f;break;}Pose(feet);Vector3 camera=Camera.main.transform.position;yield return Shot(id+"-before");
                yield return Load(Root+"/Scenes/"+id+".unity");var view=MapRoot.Current.GetComponent<MapViewBoundary>();Check(view!=null,"map ownership view present",id);Check(MapRoot.Current.transform.Find("CameraBackdrop")==null,"extended neighboring background absent",id);Check(view.GetComponentInChildren<MeshRenderer>()!=null,"one visibility mesh exists",id);Pose(feet);Check(Vector3.Distance(camera,Camera.main.transform.position)<.001f,"approved camera framing unchanged",id);yield return Shot(id+"-after");
                var ground=MapRoot.Current.ground;int far=0;for(int y=0;y<view.height;y++)for(int x=0;x<view.width;x++)if(!MapViewBoundary.InVisibleBand(view.ownership,view.width,view.height,x,view.height-y-1)){Check(!ground.HasTile(new Vector3Int(x,y,0)),"far neighbor tile not loaded",id);far++;}Check(far>0,"far neighbor region was actually excluded",id);
            }
        }
    }
}
#endif
