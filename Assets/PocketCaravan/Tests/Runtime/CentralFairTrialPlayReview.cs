#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class CentralFairTrialPlayReview:MonoBehaviour
    {
        const string Out="ArtReview/CentralFair-Task4-20261104/Unity";
        [Serializable]class Result{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Result report=new Result();
        void Check(bool b,string s){if(!b)throw new Exception(s);report.checks.Add(s);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){bool n=false;object v=null;Exception error=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);
        }
        void Finish(Exception e){report.complete=e==null;report.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string n){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==960&&t.height==540,"960x540 "+n);File.WriteAllBytes(Out+"/"+n+".png",t.EncodeToPNG());Destroy(t);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;Time.timeScale=0;Check(!GameManager.I.AutosaveEnabled,"preview save isolation");
            foreach(var id in new[]{"transit_woodtown","transit_ferry"})foreach(bool fair in new[]{false,true})
            {
                var gm=GameManager.I;gm.Clock.day=id=="transit_woodtown"?(fair?1:2):(fair?2:1);gm.Clock.minuteOfDay=600;
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Editor/B2Preview/CentralFair20261104/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                for(int i=0;i<10;i++)yield return null;var map=MapRoot.Current;
                Check(map.previewOnly&&!gm.AutosaveEnabled,"isolated "+id);Check(CentralFair.IsFairMap(id)==fair,"actual schedule "+id+" "+fair);
                foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))g.enabled=false;
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
                var centre=id=="transit_woodtown"?new Vector3(38,41,-10):new Vector3(39,27,-10);var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=centre;
                var player=PlayerController.Instance;var cell=id=="transit_woodtown"?new Vector3Int(38,36,0):new Vector3Int(34,24,0);if(!map.IsWalkable(cell)){var desired=cell;bool found=false;for(int r=1;r<=6&&!found;r++)for(int dy=-r;dy<=r&&!found;dy++)for(int dx=-r;dx<=r&&!found;dx++){var candidate=desired+new Vector3Int(dx,dy,0);if(map.IsWalkable(candidate)){cell=candidate;found=true;}}}Check(map.IsWalkable(cell),"view player cell walkable "+id);player.Motor.Teleport(map.CellCenter(cell)-(Vector3)player.Motor.feetOffset,Vector2Int.up);
                var dressing=map.GetComponent<CentralFairTrialDressing>();Check(dressing.Count==4,"four whole stalls "+id);
                foreach(var sr in map.GetComponentsInChildren<SpriteRenderer>())if(sr.name=="grain_stall")Check(sr.sprite.rect.width==48&&sr.sprite.rect.height==48&&sr.sprite.pixelsPerUnit==16,"native stall dimensions");
                yield return Photo(id+(fair?"-fair":"-normal"));
                if(id=="transit_woodtown"){cam.transform.position=new Vector3(34,50,-10);yield return Photo(id+"-event-yard"+(fair?"-fair":"-normal"));}
            }
        }
    }
}
#endif
