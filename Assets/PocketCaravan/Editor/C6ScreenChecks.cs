using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan.EditorTools
{
    /// <summary>Actual Play camera captures, identical frozen scene state in each run; never reads/writes player saves.</summary>
    [InitializeOnLoad]
    public static class C6ScreenChecks
    {
        const string Key="PocketCaravan.C6Capture", Folder="Docs/Validation/C6-Production-2026-10-26/", Preview="Assets/PocketCaravan/Editor/C6Preview/";
        static int frames;
        static string[] Ids=>WorldRegistry.Load().maps.Where(e=>e.region.regionId=="goryeo"||e.region.regionId=="transit").Select(e=>e.mapId).ToArray();
        static C6ScreenChecks(){EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=Changed;SceneManager.sceneLoaded+=Loaded;}
        public static void Before()=>Run("before");
        public static void After()=>Run("after");
        public static void RecaptureBefore()=>Resume("before");
        public static void RecaptureAfter()=>Resume("after");
        static void Resume(string variant)
        {
            SessionState.SetString(Key,variant);SessionState.SetInt(Key+"index",0);
            EditorSceneManager.OpenScene(Preview+variant+"/"+Ids[0]+".unity");
            UnityEngine.Random.InitState(621);EditorApplication.isPlaying=true;
        }
        static void Run(string variant)
        {
            Directory.CreateDirectory(Preview+variant);Directory.CreateDirectory(Folder+variant);
            foreach(var id in Ids)
            {
                var scene=EditorSceneManager.OpenScene(WorldRegistry.Load().Map(id).scenePath);
                UnityEngine.Object.FindAnyObjectByType<MapRoot>().previewOnly=true;
                EditorSceneManager.SaveScene(scene,Preview+variant+"/"+id+".unity");
            }
            SessionState.SetString(Key,variant);SessionState.SetInt(Key+"index",0);
            EditorSceneManager.OpenScene(Preview+variant+"/"+Ids[0]+".unity");
            UnityEngine.Random.InitState(621);EditorApplication.isPlaying=true;
        }
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(SessionState.GetString(Key,"")==""||!EditorApplication.isPlaying)return;
            // Freeze authoring positions before Start; compare art, not random ambient simulation.
            foreach(var b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if(b.gameObject.scene==scene)b.enabled=false;
            Time.timeScale=0;frames=0;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetString(Key,"")!=""){Loaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);Time.timeScale=0;frames=0;}   // 10/27 통합: 검토 실행 중일 때만 (일반 Play 시간 정지 방지)
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+"done",false))
            {SessionState.SetBool(Key+"done",false);SessionState.EraseString(Key);EditorApplication.Exit(0);}
        }
        static void Update()
        {
            string variant=SessionState.GetString(Key,"");if(variant==""||!EditorApplication.isPlaying||++frames<12)return;
            try
            {
                var gm=GameManager.I;var map=UnityEngine.Object.FindAnyObjectByType<MapRoot>();if(gm==null||map==null)return;
                if(!map.previewOnly)throw new Exception("C6 capture must use previewOnly");
                gm.EnterTestSession();Time.timeScale=0;
                var cam=Camera.main;cam.GetComponent<CameraFollow>().SnapNow();
                Capture(cam,Folder+variant+"/"+map.mapId+".png");
                int index=SessionState.GetInt(Key+"index",0)+1;SessionState.SetInt(Key+"index",index);frames=0;
                if(index<Ids.Length)
                {UnityEngine.Random.InitState(621);EditorSceneManager.LoadSceneInPlayMode(Preview+variant+"/"+Ids[index]+".unity",new LoadSceneParameters(LoadSceneMode.Single));}
                else
                {File.WriteAllText(Folder+variant+"/complete.txt","Actual previewOnly Play camera, 480x270; authoring poses frozen before Start; no persistent saves.\n");SessionState.SetBool(Key+"done",true);Time.timeScale=1;EditorApplication.isPlaying=false;}
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText(Folder+"capture-error.txt",e.ToString());SessionState.EraseString(Key);EditorApplication.Exit(1);}
        }
        static void Capture(Camera cam,string path)
        {
            var old=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(480,270,24,RenderTextureFormat.ARGB32);
            var image=new Texture2D(480,270,TextureFormat.RGBA32,false);
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,480,270),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);}
        }
    }
}
