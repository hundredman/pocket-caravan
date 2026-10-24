using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public static class C1NpcChecks
    {
        [Serializable] public class SceneResult { public string map; public int characters; public int interactions; public int motorTicks; public int workerTicks; public string note; }
        [Serializable] public class Report { public bool complete; public string error; public List<SceneResult> maps=new List<SceneResult>(); public List<string> conversations=new List<string>(); }
        public static Report Current;
        const string ReportPath="Docs/Validation/C1-Production-Unity-2026-10-24.json";
        public static void CheckScenes()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
            string original=SceneManager.GetActiveScene().path;int scenes=0,actors=0;
            try { foreach(var entry in WorldRegistry.Load().maps) {
                EditorSceneManager.OpenScene(entry.scenePath);scenes++;
                foreach(var a in UnityEngine.Object.FindObjectsByType<CharacterAnimator>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                    if(a.GetComponentInParent<PlayerController>()!=null)continue;
                    if(a.frames==null || a.frames.Length!=16)throw new Exception(entry.mapId+" frame count "+a.name);
                    foreach(var sp in a.frames) {
                        if(sp==null)throw new Exception(entry.mapId+" missing sprite "+a.name);
                        string path=AssetDatabase.GetAssetPath(sp);
                        if(!path.Contains("/Characters/NPC/") || sp.pixelsPerUnit!=16 || sp.pivot!=new Vector2(8,6))throw new Exception(entry.mapId+" non-human/PPU/pivot "+path);
                    } actors++;
                }
            }} finally { EditorSceneManager.OpenScene(original); }
            File.WriteAllText("Docs/Validation/C1-Production-Scenes-2026-10-24.json","{\"maps\":"+scenes+",\"npcAnimators\":"+actors+",\"all16FramesHuman\":true,\"ppu\":16,\"pivot\":[8,6]}");
        }
        public static void Begin()
        {
            var gm=GameManager.I;
            if(!EditorApplication.isPlaying || gm==null || (bool)typeof(GameManager).GetField("autosave",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(gm))throw new Exception("Initialized non-start Play with autosave off required");
            Current=new Report();Application.runInBackground=true;gm.enabled=false;gm.StartCoroutine(Drive(Run()));
        }
        static IEnumerator Drive(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0 && Current.error==null) {
                bool more=false;object value=null;
                try { more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current; } catch(Exception e){Current.error=e.ToString();}
                if(Current.error!=null)break;
                if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Current.complete=true;
            File.WriteAllText(ReportPath,JsonUtility.ToJson(Current,true));
        }
        static List<Vector3Int> Route(PlayerController pc,Vector3Int start,Func<Vector3Int,bool> goal)
        {
            var map=MapRoot.Current;var queue=new Queue<Vector3Int>();var parents=new Dictionary<Vector3Int,Vector3Int>();queue.Enqueue(start);parents[start]=start;
            while(queue.Count>0) {
                var p=queue.Dequeue();if(goal(p)){var result=new List<Vector3Int>();while(p!=start){result.Add(p);p=parents[p];}result.Reverse();return result;}
                foreach(var dir in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}) {
                    var n=p+dir;if(!parents.ContainsKey(n) && map.IsWalkable(n,pc.Motor)){parents[n]=p;queue.Enqueue(n);}
                }
                if(parents.Count>20000)break;
            }return null;
        }
        static Vector3 Position(PlayerController pc,Vector3Int cell)=>MapRoot.Current.CellCenter(cell)-(Vector3)pc.Motor.feetOffset;
        static void Move(PlayerController pc,List<Vector3Int> path,SceneResult result)
        {
            foreach(var cell in path) {
                var target=Position(pc,cell);int guard=0;
                while(Vector2.Distance(pc.transform.position,target)>.08f && guard++<240){pc.Motor.Tick(((Vector2)(target-pc.transform.position)).normalized,3.8f,1f/60f);result.motorTicks++;}
                if(guard>=240)throw new Exception(result.map+" motor blocked approaching "+cell);
            }pc.Motor.Tick(Vector2.zero,0,1f/60f);
        }
        static void CloseDialogue()
        {
            var d=UnityEngine.Object.FindAnyObjectByType<DialogueUI>();
            if(d!=null)typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);
            UIState.Blocking=false;
        }
        static IEnumerator Run()
        {
            foreach(var entry in WorldRegistry.Load().maps) {
                yield return SceneManager.LoadSceneAsync(entry.mapId);yield return null;yield return null;
                var pc=PlayerController.Instance;if(pc==null)throw new Exception("Player missing "+entry.mapId);pc.enabled=false;
                foreach(var w in UnityEngine.Object.FindObjectsByType<WorkingNpc>(FindObjectsSortMode.None))w.speed=0;
                foreach(var w in UnityEngine.Object.FindObjectsByType<Walker>(FindObjectsSortMode.None))w.enabled=false;
                var map=MapRoot.Current;var r=new SceneResult{map=entry.mapId};Current.maps.Add(r);
                var people=Interactable.All.Where(i=>(i is TalkNpc || i is QuestNpc) && i.GetComponent<CharacterAnimator>()!=null).OrderBy(i=>i.name).ToArray();
                r.characters=people.Length;
                // Exercise motor movement in every map, including transit maps with no speakers.
                var first=Route(pc,pc.Motor.Cell,c=>Vector3Int.Distance(c,pc.Motor.Cell)>=2f);
                if(first==null)throw new Exception(r.map+" no motor route from spawn");Move(pc,first,r);
                foreach(var person in people) {
                    bool Near(Vector3Int cell) {
                        var pos=Position(pc,cell);Vector2 delta=person.transform.position-pos;
                        return delta.magnitude<pc.talkRange-.03f && Interactable.Nearest(pos,delta.normalized,pc.talkRange)==person;
                    }
                    var route=Route(pc,pc.Motor.Cell,Near);
                    if(route==null && person is WorkingNpc worker) {
                        worker.speed=1.1f;
                        for(int step=0;step<600 && route==null;step+=10) {
                            for(int tick=0;tick<10;tick++){worker.Tick(.1f);r.workerTicks++;}
                            route=Route(pc,pc.Motor.Cell,Near);
                        }
                        worker.speed=0;
                    }
                    if(route==null)throw new Exception(r.map+" unreachable conversation "+person.DisplayName);
                    Move(pc,route,r);var facing=((Vector2)(person.transform.position-pc.transform.position)).normalized;
                    if(Interactable.Nearest(pc.transform.position,facing,pc.talkRange)!=person)throw new Exception(r.map+" wrong talk target "+person.DisplayName);
                    person.Interact(pc);yield return null;
                    var ui=UnityEngine.Object.FindAnyObjectByType<DialogueUI>();
                    if(ui==null || !UIState.Blocking || (string)typeof(DialogueUI).GetField("speaker",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui)!=person.DisplayName)throw new Exception(r.map+" dialogue did not open "+person.DisplayName);
                    Current.conversations.Add(r.map+":"+person.DisplayName);r.interactions++;
                    if(r.interactions==1 || person.DisplayName=="나루 우물가 아낙"){var follow=UnityEngine.Object.FindAnyObjectByType<CameraFollow>();if(follow!=null)follow.SnapNow();UnityEngine.ScreenCapture.CaptureScreenshot("Docs/Validation/C1-Game-"+entry.mapId+(person.DisplayName=="나루 우물가 아낙"?"-well":"")+".png");yield return null;yield return null;}
                    CloseDialogue();
                }
                if(people.Length==0)r.note="No speaking NPC in existing map; motor movement verified only";
                yield return null;
            }
        }
    }
}
