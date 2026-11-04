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
    public sealed class NativeLogBindingPlayReview:MonoBehaviour
    {
        const string Folder="Assets/PocketCaravan/Editor/B2Preview/LogBinding20261104/",Out="ArtReview/LogBinding-20261104/Unity";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report r=new Report();void Check(bool b,string s){if(!b)throw new Exception(s);r.checks.Add(s);}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        void Finish(Exception e){r.complete=e==null;r.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(r,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Photo(string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==960&&t.height==540,"960x540 "+name);File.WriteAllBytes(Out+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
        IEnumerator Load(string name)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Folder+name+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            for(int i=0;i<8;i++)yield return null;Time.timeScale=0;Check(MapRoot.Current.previewOnly&&!GameManager.I.AutosaveEnabled,"preview and saves off "+name);
            foreach(var s in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))s.enabled=false;
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
            PlayerController.Instance.enabled=false;var cam=Camera.main;cam.GetComponent<CameraFollow>().enabled=false;cam.orthographicSize=8.4375f;cam.transform.position=new Vector3(29.5f,42,-10);
            Teleport(new Vector3Int(29,39,0));
        }
        void Teleport(Vector3Int cell){var p=PlayerController.Instance;p.Motor.Teleport(MapRoot.Current.CellCenter(cell)-(Vector3)p.Motor.feetOffset,Vector2Int.up);}
        void Window(bool on,ObstacleField field)
        {
            var clock=GameManager.I.Clock;var p=field.placements[0];
            for(int window=0;window<100;window++)if(RoadHazardSchedule.Enabled(MapRoot.Current.mapId,p.id,p.definitionId,window*8f)==on)
            {clock.day=1+window/3;clock.minuteOfDay=(window%3)*480;RoadHazardDirector.Apply();field.Advance(0);Check(field.States[0].active==on,"existing schedule "+on);return;}
            throw new Exception("No event window");
        }
        bool Path(Vector3Int from,Vector3Int to,RectInt region)
        {
            var q=new Queue<Vector3Int>();var seen=new HashSet<Vector3Int>{from};q.Enqueue(from);
            while(q.Count>0){var c=q.Dequeue();if(c==to)return true;foreach(var dir in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}){var n=c+dir;if(region.Contains((Vector2Int)n)&&seen.Add(n)&&MapRoot.Current.IsWalkable(n))q.Enqueue(n);}}
            return false;
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<8;i++)yield return null;Check(!GameManager.I.AutosaveEnabled,"bootstrap saves off");
            int money=GameManager.I.Player.money;string cargo=JsonUtility.ToJson(GameManager.I.Player.cargo);float slide=PlayerController.Instance.Motor.cornerSlide;
            Check(RoadHazardSchedule.LogWindowHours==8&&RoadHazardSchedule.LogChance==.5f,"existing 8h/50 percent unchanged");
            foreach(var name in new[]{"horizontal","diagonal_ne","diagonal_nw","vertical"})
            {
                yield return Load(name);var map=MapRoot.Current;var field=map.GetComponent<ObstacleField>();var s=field.States[0];var p=s.placement;var motor=PlayerController.Instance.Motor;
                Check(s.nativeLog!=null&&s.definition.id=="fallen_log"&&s.definition.workSeconds==3&&s.definition.money==0&&s.definition.itemCount==0,"native binding / existing free3s "+name);
                Check(s.renderer.sprite.pixelsPerUnit==16&&s.renderer.transform.localScale==Vector3.one&&s.renderer.transform.rotation==Quaternion.identity,"no sprite rotation/stretch "+name);
                Check(!s.footprint.enabled,"no placeholder rectangle "+name);
                Window(false,field);var road=map.GetComponent<RoadGrid>();var baseline=new bool[road.width*road.height];
                for(int y=0;y<road.height;y++)for(int x=0;x<road.width;x++)baseline[y*road.width+x]=map.IsWalkable(new Vector3Int(x,y,0));
                Check(!s.renderer.enabled,"inactive sprite hidden "+name);yield return Photo(name+"-before");
                Window(true,field);int count=0;
                for(int y=0;y<road.height;y++)for(int x=0;x<road.width;x++)
                {
                    var c=new Vector3Int(x,y,0);bool expected=s.area.Contains(new Vector2Int(x,y))&&s.nativeLog.Contains(x-s.area.x,y-s.area.y);
                    if(field.Blocks(c)!=expected)throw new Exception("Collision mismatch "+name+" "+c);
                    if(expected){count++;Check(baseline[y*road.width+x],"blocked tile was open land "+name);}else if(map.IsWalkable(c)!=baseline[y*road.width+x])throw new Exception("Unrelated walkability changed "+name+" "+c);
                }
                Check(count>0&&count<9,"all map cells compared; body only and empty corners stay open "+name);Check(s.renderer.enabled,"active sprite visible "+name);yield return Photo(name+"-blocked");
                Check(Path(new Vector3Int(29,39,0),new Vector3Int(29,45,0),new RectInt(24,37,12,12)),"local bypass available "+name);
                Vector3Int target=Vector3Int.zero,approach=Vector3Int.zero,direction=Vector3Int.zero;bool found=false;
                foreach(var cell in s.nativeLog.occupied)
                {
                    var c=new Vector3Int(s.area.x+cell.x,s.area.y+cell.y,0);
                    foreach(var dir in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right})if(map.IsWalkable(c-dir)&&map.IsWalkable(c-dir*2)){target=c;approach=c-dir;direction=dir;found=true;break;}
                    if(found)break;
                }
                Check(found,"exposed approach exists "+name);Teleport(approach);field.Advance(0);motor.cornerSlide=0;
                for(int i=0;i<60;i++)motor.Tick((Vector2Int)direction,2,1f/60);
                Check(motor.Cell!=target,"PlayerMotor cannot enter log "+name);
                Teleport(approach);Check(field.Near(s,PlayerController.Instance),"approach within work range "+name);
                s.handle.Interact(PlayerController.Instance);Check(field.Progress==0&&field.Message.Contains("작업"),"interaction starts clearing "+name);
                field.Advance(1);Check(!s.resolved,"not cleared before3s "+name);
                motor.Tick(-(Vector2)(Vector2Int)direction,2,.1f);field.Advance(.1f);Check(field.Message.Contains("취소")&&!s.resolved,"moving cancels without charge "+name);
                Teleport(approach);s.handle.Interact(PlayerController.Instance);field.Advance(1);field.Advance(1);field.Advance(1);field.Advance(0);
                Check(s.resolved&&!s.active&&!s.renderer.enabled&&!field.Blocks(target),"clear restores passage and hides sprite "+name);
                Teleport(approach);for(int i=0;i<40;i++)motor.Tick((Vector2Int)direction,2,1f/60);Check(motor.Cell!=approach,"PlayerMotor crosses cleared cells "+name);motor.cornerSlide=slide;
                Teleport(new Vector3Int(29,39,0));yield return Photo(name+"-cleared");
                Check(GameManager.I.Player.money==money&&JsonUtility.ToJson(GameManager.I.Player.cargo)==cargo,"money/cargo unchanged "+name);
                // Existing visit-session semantics: resolution is not a save-format addition.
                yield return Load(name);field=MapRoot.Current.GetComponent<ObstacleField>();Window(true,field);s=field.States[0];Check(!s.resolved&&s.active,"reentry follows existing window "+name);
                field.SetEnabled(s.placement.id,false);var occupied=s.nativeLog.occupied[0];target=new Vector3Int(s.area.x+occupied.x,s.area.y+occupied.y,0);Teleport(target);var position=PlayerController.Instance.transform.position;field.SetEnabled(s.placement.id,true);
                Check(s.escapeGrace&&PlayerController.Instance.transform.position==position&&!field.Blocks(target,PlayerController.Instance.Motor),"activation over actor permits escape without teleport "+name);
                Teleport(new Vector3Int(29,39,0));field.Advance(0);Check(!s.escapeGrace&&field.Blocks(target),"block restored after actor leaves "+name);
            }
            // Opt-in must not change the shared obstacle library or legacy full-rectangle behavior.
            var f=MapRoot.Current.GetComponent<ObstacleField>();var binding=MapRoot.Current.GetComponent<GoryeoLogVisuals>();binding.entries=Array.Empty<GoryeoLogVisuals.Entry>();f.Rebuild();Window(true,f);var legacy=f.States[0];
            Check(legacy.nativeLog==null&&legacy.renderer.sprite==legacy.definition.SpriteFor(""),"legacy sprite fallback unchanged");int blocks=0;for(int y=legacy.area.yMin;y<legacy.area.yMax;y++)for(int x=legacy.area.xMin;x<legacy.area.xMax;x++)if(f.Blocks(new Vector3Int(x,y,0)))blocks++;Check(blocks==9,"legacy rectangular collision unchanged");
            Check(!GameManager.I.AutosaveEnabled,"no saves enabled");
        }
    }
}
#endif
