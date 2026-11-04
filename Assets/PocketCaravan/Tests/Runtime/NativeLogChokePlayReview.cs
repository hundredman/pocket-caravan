#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class NativeLogChokePlayReview:MonoBehaviour
    {
        const string Root="ArtReview/LogBinding-20261104/";
        public bool production;
        string Out=>Root+(production?"ProductionUnity":"ChokeUnity");
        [Serializable]class Option{public string name,mapId,variant;public int x,y;public int[] a,b;}
        [Serializable]class Layout{public Option[] options;}
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==960&&t.height==540,"960x540 "+name);File.WriteAllBytes(Out+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
        void Teleport(Vector3Int c){var p=PlayerController.Instance;p.Motor.Teleport(MapRoot.Current.CellCenter(c)-(Vector3)p.Motor.feetOffset,Vector2Int.up);}
        bool Path(Vector3Int a,Vector3Int b,RectInt area)
        {
            var q=new Queue<Vector3Int>();var seen=new HashSet<Vector3Int>{a};q.Enqueue(a);
            while(q.Count>0){var c=q.Dequeue();if(c==b)return true;foreach(var dir in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}){var n=c+dir;if(area.Contains((Vector2Int)n)&&seen.Add(n)&&MapRoot.Current.IsWalkable(n))q.Enqueue(n);}}
            return false;
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<8;i++)yield return null;Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");
            var options=JsonUtility.FromJson<Layout>(File.ReadAllText(Root+"revised-layout.json")).options;
            foreach(var o in options)
            {
                string scene=production?"Assets/PocketCaravan/Scenes/Transit/"+o.mapId+".unity":"Assets/PocketCaravan/Editor/B2Preview/LogBinding20261104/"+o.name+".unity";
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene,new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<8;i++)yield return null;
                Time.timeScale=0;var map=MapRoot.Current;var field=map.GetComponent<ObstacleField>();var s=field.States[0];var road=map.GetComponent<RoadGrid>();
                Check(map.previewOnly==!production&&!GameManager.I.AutosaveEnabled,"correct actual/preview scene; saves off "+o.name);
                foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))g.enabled=false;
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();PlayerController.Instance.enabled=false;
                var a=new Vector3Int(o.a[0],road.height-o.a[1]-1,0);var b=new Vector3Int(o.b[0],road.height-o.b[1]-1,0);
                var area=new RectInt(o.x-6,road.height-o.y-9,15,15);Teleport(a);
                var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=map.CellCenter(new Vector3Int(o.x+1,road.height-o.y-2,0))+new Vector3(0,0,-10);
                for(int w=1;w<100;w+=3)if(RoadHazardSchedule.Enabled(map.mapId,s.placement.id,"fallen_log",w*8f)==true){GameManager.I.Clock.day=1+w/3;GameManager.I.Clock.minuteOfDay=720;break;}
                field.SetEnabled(s.placement.id,false);field.Advance(0);Check(map.IsWalkable(a)&&map.IsWalkable(b)&&Path(a,b,area)&&!s.handle.isActiveAndEnabled,"route open; no ghost marker before "+o.name);yield return Photo(o.name+"-before");
                field.SetEnabled(s.placement.id,true);field.Advance(0);Check(!Path(a,b,area),"route blocked through 15x15 corridor "+o.name);Check(s.renderer.enabled&&s.renderer.transform.localScale==Vector3.one,"native sprite active "+o.name);yield return Photo(o.name+"-blocked");
                r.checks.Add("full-map alternate route "+Path(a,b,new RectInt(0,0,road.width,road.height))+" "+o.name);
                // Approach from both sides must reach the same interaction marker without crossing the tree.
                foreach(var start in new[]{a,b})
                {
                    bool reachable=false;var center=map.WorldToCell(s.handle.transform.position);
                    for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                    {
                        var c=center+new Vector3Int(dx,dy,0);if(!map.IsWalkable(c)||!Path(start,c,area))continue;Teleport(c);field.Advance(0);
                        if(Vector2.Distance(PlayerController.Instance.transform.position,s.handle.transform.position)<=PlayerController.Instance.talkRange&&field.Near(s,PlayerController.Instance)&&Interactable.Nearest(PlayerController.Instance.transform.position,((Vector2)s.handle.transform.position-(Vector2)PlayerController.Instance.transform.position).normalized,PlayerController.Instance.talkRange)==s.handle)reachable=true;
                    }
                    Check(reachable,"clear interaction reachable from "+start+" "+o.name);
                }
                var handleCell=map.WorldToCell(s.handle.transform.position);bool near=false;
                for(int dy=-2;dy<=2&&!near;dy++)for(int dx=-2;dx<=2&&!near;dx++){var c=handleCell+new Vector3Int(dx,dy,0);if(map.IsWalkable(c)&&Path(a,c,area)){Teleport(c);if(field.Near(s,PlayerController.Instance))near=true;}}
                Check(near,"clearing approach "+o.name);int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);s.handle.Interact(PlayerController.Instance);field.Advance(1);field.Advance(1);field.Advance(1);field.Advance(0);
                Check(s.resolved&&!s.renderer.enabled&&!s.handle.isActiveAndEnabled&&Path(a,b,area),"route restored; no ghost marker after3s clear "+o.name);Check(GameManager.I.Player.money==money&&JsonUtility.ToJson(GameManager.I.Player.cargo)==cargo,"no money/cargo charge "+o.name);Teleport(a);yield return Photo(o.name+"-cleared");
            }
            Check(!GameManager.I.AutosaveEnabled,"no saves enabled");
        }
    }
}
#endif
