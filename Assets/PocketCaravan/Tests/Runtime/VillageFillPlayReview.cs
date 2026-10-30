#if UNITY_EDITOR
using System;using System.IO;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class VillageFillPlayReview:MonoBehaviour
    {
        [Serializable] public class Row{public string test,map;public bool passed;}
        [Serializable] public class Report{public bool complete;public string error;public List<Row> checks=new List<Row>();}
        readonly Report report=new Report();const string ReviewOut="ArtReview/VillageFill-20261030";const string Root="Assets/PocketCaravan/Editor/B2Preview/VillageFill";const string Out=ReviewOut+"/Unity";
        [Serializable] class Targets{public Target[] shots;}
        [Serializable] class Target{public string mapId,slug;public float x,y;}
        void Check(bool ok,string test,string map){report.checks.Add(new Row{passed=ok,test=test,map=map});if(!ok)throw new Exception(test+" "+map);}
        void Save(){Directory.CreateDirectory(Out);File.WriteAllText(Out+"/checks.json",JsonUtility.ToJson(report,true));}
        void OnEnable(){SceneManager.sceneLoaded+=IsolateInput;}
        void OnDisable(){SceneManager.sceneLoaded-=IsolateInput;}
        static void IsolateInput(Scene scene,LoadSceneMode mode){foreach(var pc in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))pc.enabled=false;}
        IEnumerator Start(){var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object item=null;bool moved=false;Exception error=null;try{moved=stack.Peek().MoveNext();if(moved)item=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!moved){stack.Pop();continue;}if(item is IEnumerator)stack.Push((IEnumerator)item);else yield return item;}Finish(null);}
        void Finish(Exception e){report.complete=e==null;report.error=e==null?null:e.ToString();Save();Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        string Production(string id){foreach(var s in EditorBuildSettings.scenes)if(Path.GetFileNameWithoutExtension(s.path)==id)return s.path;throw new Exception(id);}
        IEnumerator Load(string path){yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<4;i++)yield return null;Time.timeScale=0;PlayerController.Instance.enabled=false;Check(!GameManager.I.AutosaveEnabled,"user saves disabled",MapRoot.Current.mapId);}
        void PhotoCamera(Target t){GameManager.I.Clock.day=1;GameManager.I.Clock.minuteOfDay=12*60;var map=MapRoot.Current;var motor=PlayerController.Instance.Motor;var c=new Vector3Int(Mathf.RoundToInt(t.x),Mathf.RoundToInt(t.y)-2,0);Vector3 safe;var desired=map.CellCenter(c)-(Vector3)motor.feetOffset;if(!map.TryResolveSavedPosition(desired,motor.feetOffset,out safe))throw new Exception("No safe photo pose");motor.Teleport(safe,Vector2Int.down);
            var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;foreach(var component in cam.GetComponents<Component>())if(component.GetType().Name=="PixelPerfectCamera"){var so=new SerializedObject(component);so.FindProperty("m_RefResolutionX").intValue=640;so.FindProperty("m_RefResolutionY").intValue=360;so.ApplyModifiedPropertiesWithoutUndo();}cam.orthographicSize=11.25f;cam.transform.position=new Vector3(t.x+.5f,t.y+.5f,-10);
            foreach(var h in FindObjectsByType<HUD>(FindObjectsSortMode.None))h.enabled=false;foreach(var q in FindObjectsByType<QuestTracker>(FindObjectsSortMode.None))q.enabled=false;foreach(var w in FindObjectsByType<WorldLabels>(FindObjectsSortMode.None))w.enabled=false;Notify.Clear();}
        IEnumerator Capture(string path){yield return null;yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();Check(im.width==1280&&im.height==720,"native 1280x720 capture",path);File.WriteAllBytes(path,im.EncodeToPNG());Destroy(im);}
        IEnumerator Run(){Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;if(GameManager.I.AutosaveEnabled)throw new Exception("Safe preview startup required");
            var targets=JsonUtility.FromJson<Targets>(File.ReadAllText(ReviewOut+"/shots.json"));foreach(var t in targets.shots){
                yield return Load(Production(t.mapId));PhotoCamera(t);yield return Capture(Out+"/"+t.slug+"-before.png");
                yield return Load(Root+"/Scenes/"+t.mapId+".unity");var map=MapRoot.Current;var d=map.GetComponent<VillageDressing>();Check(d!=null,"default dressing exists",t.mapId);
                foreach(var s in d.spaces){Check(s.normal!=null&&s.normal.activeSelf,"normal state visible "+s.id,t.mapId);foreach(var r in s.footprints)for(int y=r.yMin;y<r.yMax;y++)for(int x=r.xMin;x<r.xMax;x++)Check(!map.IsWalkable(new Vector3Int(x,y,0)),"normal prop collision",t.mapId);}
                var yard=Array.Find(d.spaces,s=>s.id=="event-yard");var foot=yard.footprints[0];var cell=new Vector3Int(foot.xMin,foot.yMin,0);Vector3 recovered;var motor=PlayerController.Instance.Motor;Check(map.TryResolveSavedPosition(map.CellCenter(cell)-(Vector3)motor.feetOffset,motor.feetOffset,out recovered)&&map.IsWalkable(map.WorldToCell(recovered+(Vector3)motor.feetOffset)),"old position under prop recovers safely",t.mapId);var dummy=new GameObject("QA event only - not saved");dummy.SetActive(false);d.SetEvent("event-yard",dummy);Check(!yard.normal.activeSelf&&yard.eventInstance.activeSelf,"event replaces everyday appearance",t.mapId);Check(map.IsWalkable(cell),"event clears everyday collision",t.mapId);d.SetEvent("event-yard",null);Check(yard.normal.activeSelf&&!map.IsWalkable(cell),"event end restores appearance and collision",t.mapId);Destroy(dummy);yield return null;
                foreach(var anim in FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None)){Check(anim.frames!=null&&anim.frames.Length==16,"character has 16 frames",t.mapId);foreach(var frame in anim.frames)Check(frame!=null,"character frame texture exists",t.mapId);}
                PhotoCamera(t);yield return Capture(Out+"/"+t.slug+"-after.png");
                foreach(var board in FindObjectsByType<ContractBoard>(FindObjectsSortMode.None)){board.Interact(PlayerController.Instance);yield return null;Check(UIState.Blocking,"existing contract board opens",t.mapId);if(t.mapId=="goryeo_jangdan")yield return Capture(Out+"/jangdan-board-ui.png");typeof(ContractBoard).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board,null);Check(!UIState.Blocking,"contract board closes",t.mapId);}
                Save();
            }
        }
    }
}
#endif
