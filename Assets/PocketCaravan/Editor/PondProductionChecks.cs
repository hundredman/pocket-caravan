using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    // Explicit Temp request or menu only. No saved game reads/writes; Gaegyeong is not the boot scene.
    [InitializeOnLoad]
    public static class PondProductionChecks
    {
        const string Output="Docs/Validation/A4-Pond-Production-2026-10-25/";
        const string Request="Temp/pond-production-checks.request";
        const string Active="PocketCaravan.PondChecks.Active",Stage="PocketCaravan.PondChecks.Stage";
        [Serializable] class Placement {public int x,y;public string piece;}
        [Serializable] class Layout {public Placement[] placements;}
        [Serializable] public class Report {
            public string unityVersion,error,screen;
            public int q1Checks,r6Checks,saveChecks,waterCells,bankApproaches,motorTicks;
            public bool sceneSaved,autosaveDisabled,playCompleted,manualPlay;
            public List<string> passed=new List<string>();
        }
        static Report report;static int frames;
        static PondProductionChecks(){EditorApplication.delayCall+=CheckRequest;EditorApplication.playModeStateChanged+=ModeChanged;EditorApplication.update+=Update;}
        static Report Read()=>File.Exists(Output+"Unity.json")?JsonUtility.FromJson<Report>(File.ReadAllText(Output+"Unity.json")):new Report();
        static void Save(){Directory.CreateDirectory(Output);File.WriteAllText(Output+"Unity.json",JsonUtility.ToJson(report,true));}
        static void Check(bool ok,string name){if(!ok)throw new Exception(name);report.passed.Add(name);}
        static void CheckRequest(){if(File.Exists(Request)&&!EditorApplication.isCompiling&&!EditorApplication.isPlayingOrWillChangePlaymode){File.Delete(Request);Run();}}
        [MenuItem("Pocket Caravan/Tests/승인 연못 본편 검사")]
        public static void Run(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play first");
            report=new Report{unityVersion=Application.unityVersion};
            try {
                MapJsonBuilder.BuildSelected("Gaegyeong");report.sceneSaved=true;
                report.q1Checks=Q1Checks.Run().passed.Count;report.r6Checks=R6Checks.Run().passed.Count;report.saveChecks=SaveCompatibilityChecks.RunData().passed.Count;
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Goryeo/Gaegyeong.unity");
                var ground=UnityEngine.Object.FindAnyObjectByType<MapRoot>().ground;
                var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Tools/MapGen/pond_layout.json"));
                foreach(var p in layout.placements){
                    var t=ground.GetTile<Tile>(new Vector3Int(p.x,59-p.y,0));Check(t!=null&&t.sprite!=null,"sprite exists "+p.x+","+p.y);
                    string path=AssetDatabase.GetAssetPath(t.sprite);Check(path.EndsWith("/pond_"+p.piece+".png"),"correct approved module "+p.x+","+p.y);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    Check(t.sprite.pixelsPerUnit==16&&t.sprite.rect.size==new Vector2(16,16)&&importer.filterMode==FilterMode.Point&&importer.textureCompression==TextureImporterCompression.Uncompressed,"native import "+p.x+","+p.y);
                }
                Save();SessionState.SetBool(Active,true);SessionState.SetString(Stage,"waiting_play");EditorApplication.isPlaying=true;
            } catch(Exception e){report.error=e.ToString();Save();Debug.LogException(e);SessionState.SetBool(Active,false);}
        }
        static void ModeChanged(PlayModeStateChange state){
            if(!SessionState.GetBool(Active,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){report=Read();frames=0;SessionState.SetString(Stage,"setup");}
            if(state==PlayModeStateChange.EnteredEditMode){report=Read();report.playCompleted=string.IsNullOrEmpty(report.error);Save();SessionState.SetBool(Active,false);Debug.Log("[PondChecks] complete: "+report.error);}
        }
        static void SetGameSize(){
            var a=typeof(Editor).Assembly;var type=a.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);var sizes=singleton.GetProperty("instance").GetValue(null,null);
            var group=type.GetMethod("GetGroup").Invoke(sizes,new object[]{0});var sizeType=a.GetType("UnityEditor.GameViewSize");var kind=a.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(kind,1),1920,1080,"Pond production1920x1080"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);var view=EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1,null);view.Focus();view.Repaint();
        }
        static void Update(){
            if(!SessionState.GetBool(Active,false)||!Application.isPlaying)return;
            string stage=SessionState.GetString(Stage,"");
            try {
                if(stage=="setup"){
                    if(GameManager.I==null||MapRoot.Current==null||PlayerController.Instance==null){if(++frames>600)throw new Exception("Game initialization timeout");return;}
                    report=Read();Application.runInBackground=true;var gm=GameManager.I;
                    typeof(GameManager).GetField("autosave",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(gm,false);gm.enabled=false;report.autosaveDisabled=true;
                    var pc=PlayerController.Instance;pc.enabled=false;var map=MapRoot.Current;Check(map.mapId=="Gaegyeong","capital scene active");
                    var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Tools/MapGen/pond_layout.json"));var wet=new HashSet<Vector3Int>(layout.placements.Select(p=>new Vector3Int(p.x,59-p.y,0)));
                    foreach(var water in wet){
                        Check(!map.IsWalkable(water,pc.Motor),"water remains blocked "+water);report.waterCells++;
                        foreach(var dir in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right}){
                            var bank=water+dir;if(wet.Contains(bank)||!map.IsWalkable(bank,pc.Motor))continue;
                            pc.Motor.Teleport(map.CellCenter(bank)-(Vector3)pc.Motor.feetOffset,Vector2Int.down);
                            for(int tick=0;tick<45;tick++){pc.Motor.Tick(new Vector2(-dir.x,-dir.y),3.8f,1f/60f);report.motorTicks++;if(pc.Motor.Cell==water||!map.IsWalkable(pc.Motor.Cell,pc.Motor))throw new Exception("Motor entered blocked water "+water);}
                            Check(pc.Motor.Cell!=water,"motor cannot enter pond "+water+" from "+bank);report.bankApproaches++;
                        }
                    }
                    var reviewCell=new Vector3Int(35,35,0);Check(map.IsWalkable(reviewCell,pc.Motor),"review actor at legal bank-side cell");pc.Motor.Teleport(map.CellCenter(reviewCell)-(Vector3)pc.Motor.feetOffset,Vector2Int.down);pc.Motor.Tick(Vector2.zero,0,1f/60f);
                    var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=new Vector3(35.5f,42.5f,-10);SetGameSize();
                    Check(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"Play scene not saved/dirty");Save();frames=0;SessionState.SetString(Stage,"render");
                } else if(stage=="render"&&++frames>=30){
                    report.screen=Output+"game-after.png";ScreenCapture.CaptureScreenshot(Path.GetFullPath(report.screen));Save();frames=0;SessionState.SetString(Stage,"capture");
                } else if(stage=="capture"&&++frames>=30){
                    if(!File.Exists(report.screen)){if(frames>180)throw new Exception("Game capture missing");return;}
                    Save();SessionState.SetString(Stage,"stopping");EditorApplication.isPlaying=false;
                }
            }catch(Exception e){report=report??Read();report.error=e.ToString();Save();SessionState.SetString(Stage,"stopping");EditorApplication.isPlaying=false;Debug.LogException(e);}
        }
    }
}
