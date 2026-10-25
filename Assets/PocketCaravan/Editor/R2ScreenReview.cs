using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    [InitializeOnLoad]
    public static class R2ScreenReview
    {
        const string Key="PocketCaravan.R2Review",Folder="Docs/Validation/R2-Implementation-2026-10-26/";
        static int frames;
        static R2ScreenReview(){EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first");
            var scene=EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Goryeo/Gaegyeong.unity");
            UnityEngine.Object.FindAnyObjectByType<MapRoot>().previewOnly=true;
            Directory.CreateDirectory("Assets/PocketCaravan/Editor/R2Preview");
            EditorSceneManager.SaveScene(scene,"Assets/PocketCaravan/Editor/R2Preview/Review.unity");
            SessionState.SetString(Key,"start");EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredPlayMode)frames=0;
            if(change==PlayModeStateChange.EnteredEditMode&&SessionState.GetString(Key,"")=="stop")
            {SessionState.EraseString(Key);File.WriteAllText(Folder+"screen-review-complete.txt","PreviewOnly Play; HUD/market/map screenshots; stopped without persistent player writes.\n");EditorApplication.Exit(0);}
        }
        static void Update()
        {
            string stage=SessionState.GetString(Key,"");if(stage==""||!EditorApplication.isPlaying||++frames<12)return;
            try
            {
                var gm=GameManager.I;if(gm==null)return;
                if(stage=="start")
                {
                    if(!UnityEngine.Object.FindAnyObjectByType<MapRoot>().previewOnly)throw new Exception("Not preview scene");
                    Time.timeScale=0;var market=UnityEngine.Object.FindAnyObjectByType<TownMarket>();
                    var motor=UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();motor.Teleport(market.transform.position+new Vector3(0,-1,0),Vector2Int.up);
                    UnityEngine.Object.FindAnyObjectByType<CameraFollow>().SnapNow();
                    SessionState.SetString(Key,"hud");
                }
                else if(stage=="hud")
                {ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"hud-after.png"));SessionState.SetString(Key,"marketOpen");}
                else if(stage=="marketOpen")
                {
                    var market=UnityEngine.Object.FindAnyObjectByType<TownMarket>();market.Interact(PlayerController.Instance);
                    typeof(TownMarket).GetField("selected",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(market,gm.Db.items.Find(i=>i.id=="najeon"));
                    typeof(TownMarket).GetField("scroll",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(market,Vector2.zero);SessionState.SetString(Key,"market");
                }
                else if(stage=="market")
                {ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"market-lock.png"));SessionState.SetString(Key,"mapOpen");}
                else if(stage=="mapOpen")
                {
                    var market=UnityEngine.Object.FindAnyObjectByType<TownMarket>();typeof(TownMarket).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(market,null);
                    var map=gm.GetComponent<WorldMapUI>();typeof(WorldMapUI).GetField("open",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,true);UIState.Blocking=true;SessionState.SetString(Key,"map");
                }
                else if(stage=="map")
                {ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"map-after.png"));SessionState.SetString(Key,"finish");}
                else if(stage=="finish")
                {SessionState.SetString(Key,"stop");Time.timeScale=1;EditorApplication.isPlaying=false;}
                frames=0;
            }
            catch(Exception ex){Debug.LogException(ex);SessionState.EraseString(Key);Time.timeScale=1;EditorApplication.isPlaying=false;File.WriteAllText(Folder+"screen-error.txt",ex.ToString());EditorApplication.Exit(1);}
        }
    }
}
