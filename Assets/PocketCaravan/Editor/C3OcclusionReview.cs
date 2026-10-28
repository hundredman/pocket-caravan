using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan.EditorTools
{
    // Isolated Play review: navigate with the production motor, never place an actor inside a prop.
    [InitializeOnLoad]
    public static class C3OcclusionReview
    {
        const string Key="PocketCaravan.C3Review", Root="ArtReview/C3a-2026-10-26/", Preview="Assets/PocketCaravan/Editor/C3Preview/";
        [Serializable] public class Plan { public Area[] maps; }
        [Serializable] public class Area { public string map; public int w,h; public Prop[] props; }
        [Serializable] public class Prop { public string name,kind; public int x,y,w,h,beforeRows,afterRows; public Side[] sides; }
        [Serializable] public class Side { public string side; public int x,y,dx,dy; }
        [Serializable] public class Record { public string map,prop,side,result; public int steps; public float x,y; }
        [Serializable] public class Report { public List<Record> approaches=new List<Record>(); public int motorTicks; public bool previewOnly=true; }
        static int frames;
        static C3OcclusionReview(){EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=Changed;}
        static Plan Read()=>JsonUtility.FromJson<Plan>(File.ReadAllText(Root+"plan.json"));
        static bool Production=>SessionState.GetBool(Key+"production",false);
        static string WalkingPath=>Production?"Docs/Validation/C3a-Occlusion-2026-10-26/production-walking.json":Root+"walking.json";
        static string ScenePath(string map)=>Preview+(Production?"P-":"")+map+".unity";
        public static void WalkProduction()
        {
            SessionState.SetBool(Key+"production",true);
            foreach(var m in Read().maps)
            {
                var scene=EditorSceneManager.OpenScene(WorldRegistry.Load().Map(m.map).scenePath);
                UnityEngine.Object.FindAnyObjectByType<MapRoot>().previewOnly=true;
                EditorSceneManager.SaveScene(scene,ScenePath(m.map));
            }
            File.WriteAllText(WalkingPath,JsonUtility.ToJson(new Report(),true));
            SessionState.SetInt(Key+"index",0);SessionState.SetString(Key,"walk");
            EditorSceneManager.OpenScene(ScenePath(Read().maps[0].map));EditorApplication.isPlaying=true;
        }
        public static void CaptureOnly()
        {
            SessionState.SetBool(Key+"production",false);
            SessionState.SetString(Key,"pose");SessionState.SetInt(Key+"pose",0);
            EditorSceneManager.OpenScene(Preview+"Gaegyeong.unity");EditorApplication.isPlaying=true;
        }
        public static void Run()
        {
            SessionState.SetBool(Key+"production",false);
            Directory.CreateDirectory(Preview); Directory.CreateDirectory(Root+"screens");
            foreach(var m in Read().maps)
                Build(Root+"before/"+m.map+".json",Preview+m.map+".unity");
            Build(Root+"candidate/Assets/PocketCaravan/Maps/Gaegyeong.json",Preview+"Gaegyeong-after.unity");
            File.WriteAllText(Root+"walking.json",JsonUtility.ToJson(new Report(),true));
            SessionState.SetInt(Key+"index",0);SessionState.SetString(Key,"walk");
            EditorSceneManager.OpenScene(Preview+Read().maps[0].map+".unity"); EditorApplication.isPlaying=true;
        }
        static void Build(string input,string scene)
        {
            string json=File.ReadAllText(input).TrimEnd();json=json.Substring(0,json.Length-1)+",\"previewOnly\":true}";
            MapJsonBuilder.BuildSingleMap(json,scene);
        }
        static void Changed(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredPlayMode&&SessionState.GetString(Key,"")!=""){frames=0;Time.timeScale=0;}   // 10/27 통합: 검토 실행 중일 때만 시간 정지 (일반 Play가 멈춰 움직이지 못하던 문제)
            if(change==PlayModeStateChange.EnteredEditMode&&SessionState.GetString(Key,"")=="stop")
            {SessionState.EraseString(Key);File.WriteAllText(Root+"review-complete.txt","Production PlayerMotor.Tick over reachable routes in previewOnly Play. No persistent saves.\n");EditorApplication.Exit(0);}
        }
        static void Update()
        {
            string stage=SessionState.GetString(Key,"");if(stage==""||!EditorApplication.isPlaying||++frames<15)return;
            try
            {
                var gm=GameManager.I;var map=MapRoot.Current;var motor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();if(gm==null||map==null||motor==null)return;
                if(!map.previewOnly)throw new Exception("Not previewOnly");
                Time.timeScale=0;UnityEngine.Object.FindAnyObjectByType<PlayerController>().enabled=false;
                var plan=Read();int index=SessionState.GetInt(Key+"index",0);
                if(stage=="walk")
                {
                    var area=plan.maps[index];if(map.mapId!=area.map)return;
                    var report=JsonUtility.FromJson<Report>(File.ReadAllText(WalkingPath));
                    foreach(var prop in area.props)foreach(var side in prop.sides)
                    {
                        var record=new Record{map=area.map,prop=prop.name+"@"+prop.x+","+prop.y,side=side.side};
                        record.result=Approach(area,side,motor,report,out int steps);record.steps=steps;record.x=motor.Feet.x;record.y=motor.Feet.y;report.approaches.Add(record);
                    }
                    File.WriteAllText(WalkingPath,JsonUtility.ToJson(report,true));
                    index++;SessionState.SetInt(Key+"index",index);
                    if(index<plan.maps.Length)EditorSceneManager.LoadSceneInPlayMode(ScenePath(plan.maps[index].map),new LoadSceneParameters(LoadSceneMode.Single));
                    else if(Production){SessionState.SetString(Key,"stop");Time.timeScale=1;EditorApplication.isPlaying=false;}
                    else {SessionState.SetString(Key,"pose");SessionState.SetInt(Key+"pose",0);EditorSceneManager.LoadSceneInPlayMode(Preview+"Gaegyeong.unity",new LoadSceneParameters(LoadSceneMode.Single));}
                }
                else if(stage=="pose")
                {
                    int pose=SessionState.GetInt(Key+"pose",0);bool after=pose>=4;int p=pose%4;
                    var area=Array.Find(plan.maps,m=>m.map=="Gaegyeong");
                    // Adjacent legal cell, then actual movement towards the prop. Same route in both versions.
                    var prop=area.props[p<2?14:0];string sideName=p%2==0?(p<2?"west":"east"):"south";
                    var side=Array.Find(prop.sides,s=>s.side==sideName);
                    string result=Approach(area,side,motor,new Report(),out _);if(result!="reached")throw new Exception("Review pose "+result);
                    var anim=motor.GetComponentInChildren<CharacterAnimator>();anim.facing=motor.Facing;anim.walking=false;
                    UnityEngine.Object.FindAnyObjectByType<CameraFollow>().SnapNow();
                    File.WriteAllText(Root+"screens/pose-"+pose+".json",JsonUtility.ToJson(new Record{map=map.mapId,prop=prop.name,side=side.side,result=result,x=motor.Feet.x,y=motor.Feet.y},true));
                    SessionState.SetString(Key,"capture");
                }
                else if(stage=="capture")
                {
                    int pose=SessionState.GetInt(Key+"pose",0);ScreenCapture.CaptureScreenshot(Path.GetFullPath(Root+"screens/pose-"+pose+".png"));SessionState.SetString(Key,"next");
                }
                else if(stage=="next")
                {
                    int pose=SessionState.GetInt(Key+"pose",0)+1;SessionState.SetInt(Key+"pose",pose);
                    if(pose==8){SessionState.SetString(Key,"stop");Time.timeScale=1;EditorApplication.isPlaying=false;}
                    else {SessionState.SetString(Key,"pose");if(pose==4)EditorSceneManager.LoadSceneInPlayMode(Preview+"Gaegyeong-after.unity",new LoadSceneParameters(LoadSceneMode.Single));}
                }
                frames=0;
            }
            catch(Exception ex)
            {Debug.LogException(ex);File.WriteAllText(Root+"review-error.txt",ex.ToString());SessionState.EraseString(Key);Time.timeScale=1;EditorApplication.isPlaying=false;EditorApplication.Exit(1);}
        }
        static string Approach(Area area,Side side,PlayerMotor motor,Report report,out int steps)
        {
            steps=0;var map=MapRoot.Current;var goal=new Vector3Int(side.x,area.h-1-side.y,0);var start=motor.Cell;
            var previous=new Dictionary<Vector3Int,Vector3Int>{{start,start}};var queue=new Queue<Vector3Int>();queue.Enqueue(start);
            while(queue.Count>0&&!previous.ContainsKey(goal))
            {
                var c=queue.Dequeue();foreach(var d in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right})
                {var n=c+d;if(previous.ContainsKey(n)||!map.IsWalkable(n,motor))continue;previous[n]=c;queue.Enqueue(n);}
            }
            if(!previous.ContainsKey(goal))return "runtime blocker / no route";
            var path=new List<Vector3Int>();for(var c=goal;c!=start;c=previous[c])path.Add(c);path.Add(start);path.Reverse();
            foreach(var c in path)
            {
                var target=(Vector2)map.CellCenter(c);int ticks=0;
                while(Vector2.Distance(motor.Feet,target)>.025f&&ticks++<150)
                {
                    var delta=target-motor.Feet;motor.Tick(delta.normalized,Mathf.Min(motor.walkSpeed,delta.magnitude*20),1f/60);report.motorTicks++;
                    if(!FeetFree(motor))throw new Exception("Feet entered blocked cell");
                }
                if(ticks>=150)return "motor could not follow route";steps++;
            }
            var inward=new Vector2(-side.dx,side.dy);
            for(int i=0;i<40;i++){motor.Tick(inward,motor.walkSpeed,1f/60);report.motorTicks++;if(!FeetFree(motor))throw new Exception("Feet crossed prop collision");}
            for(int i=0;i<12;i++)motor.Tick(Vector2.zero,motor.walkSpeed,1f/60);
            return "reached";
        }
        static bool FeetFree(PlayerMotor m)
        {
            var h=m.feetSize*.5f;var p=m.Feet;var map=MapRoot.Current;
            return map.IsWalkable(map.WorldToCell(p+h),m)&&map.IsWalkable(map.WorldToCell(p-h),m)
                &&map.IsWalkable(map.WorldToCell(p+new Vector2(h.x,-h.y)),m)&&map.IsWalkable(map.WorldToCell(p+new Vector2(-h.x,h.y)),m);
        }
    }
}
