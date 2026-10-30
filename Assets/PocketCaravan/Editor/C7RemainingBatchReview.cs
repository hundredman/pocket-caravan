using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PocketCaravan.EditorTools
{
    // Editor-only camera render. Never enters Play, opens a production scene,
    // loads WorldIndex/GameDatabase, saves a scene, or edits build settings.
    public static class C7RemainingBatchReview
    {
        const string Out="ArtReview/C7-RemainingBatch-20261027/";
        [Serializable] class Entry {public string name,before,after;public int page;public float x,y;}
        [Serializable] class Plan {public Entry[] assets;}
        [Serializable] class GroundCell {public int x,y;public string image;}
        [Serializable] class GroundPlan {public GroundCell[] entries;}
        static Plan plan;
        static GroundPlan ground;
        static Camera camera;
        static GameObject stage;
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        static int frame;
        static double ready;
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Run review outside Play mode");
            Directory.CreateDirectory(Out+"Screens");
            plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Out+"render-plan.json"));
            ground=JsonUtility.FromJson<GroundPlan>(File.ReadAllText("ArtReview/C7-TeamStyle-20261027/GroundOrigin-v7/original-ground.json"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("C7 independent review camera");camera=go.AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=270f/32;camera.transform.position=new Vector3(15,8.4375f,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(139,168,101,255);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var light=new GameObject("C7 review global light").AddComponent<Light2D>();
            light.lightType=Light2D.LightType.Global;light.intensity=1;light.color=Color.white;
            frame=0;Build(0,false);ready=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Update;
        }
        static Sprite Load(string path)
        {
            if(sprites.TryGetValue(path,out var sprite))return sprite;
            var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!tex.LoadImage(File.ReadAllBytes(path)))throw new IOException(path);
            tex.filterMode=FilterMode.Point;tex.wrapMode=TextureWrapMode.Clamp;
            sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.zero,16,0,SpriteMeshType.FullRect);sprites[path]=sprite;return sprite;
        }
        static void Put(string path,float x,float y,int order=1)
        {
            var go=new GameObject(Path.GetFileNameWithoutExtension(path));go.transform.SetParent(stage.transform,false);go.transform.position=new Vector3(x,y,0);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=Load(path);r.sortingOrder=order;
        }
        static void WaterBackdrop(float x,float y,int width,int height)
        {
            // A flat review color swatch, not an authored pond, tile set or map.
            var tex=new Texture2D(width*16,height*16,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Point;
            var pixels=new Color32[tex.width*tex.height];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(113,166,174,255);
            tex.SetPixels32(pixels);tex.Apply();var go=new GameObject("Water color review backdrop");go.transform.SetParent(stage.transform,false);go.transform.position=new Vector3(x,y,0);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.zero,16);r.sortingOrder=1;
        }
        static void Build(int page,bool trial)
        {
            if(stage!=null)UnityEngine.Object.DestroyImmediate(stage);stage=new GameObject("C7 art review only");
            foreach(var c in ground.entries)Put(c.image,c.x,c.y,0);
            Put("ArtReview/Approved/C7-GateProps-v1/gate.png",1,11,2);
            Put("ArtReview/Approved/C7-Buildings-v6/tile-house.png",12,11,2);
            Put("ArtReview/Approved/C7-Gaekju-v1/gaekju.png",21,11,2);
            Put("ArtReview/Approved/C7-Batch1-v1/willow.png",2,2,2);
            Put("ArtReview/Approved/C7-Shop-v1/shop.png",8,2,2);
            Put("Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png",11,6,3);
            Put("ArtReview/Approved/woman-front-v4.png",13,6,3);
            Put("Assets/PocketCaravan/Art/Characters/NPC/npc_naru_chief/npc_naru_chief_tall_d0.png",15,6,3);
            Put("ArtReview/C7-NaturalPond-20261027/pond.png",20,2,2);
            if(page==0)WaterBackdrop(1,6,7,4);
            if(page==2)WaterBackdrop(24,7,4,2);
            foreach(var e in plan.assets)if(e.page==page)Put(trial?e.after:e.before,e.x,e.y,3);
            EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();
        }
        static void Update()
        {
            if(EditorApplication.timeSinceStartup<ready)return;
            try
            {
                int page=frame/2;bool trial=(frame%2)==1;
                Capture($"page{page}_{(trial?"trial":"before")}");frame++;
                if(frame==6)
                {
                    File.WriteAllText(Out+"Screens/complete.json","{\"editorCamera\":true,\"playModeRun\":false,\"productionSceneOpened\":false,\"sceneSaved\":false,\"resolution\":[480,270],\"ppu\":16,\"pages\":3,\"waterBackground\":\"review color swatch\"}\n");
                    EditorApplication.update-=Update;EditorApplication.Exit(0);return;
                }
                Build(frame/2,(frame%2)==1);ready=EditorApplication.timeSinceStartup+2;
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText(Out+"Screens/error.txt",e.ToString());EditorApplication.update-=Update;EditorApplication.Exit(1);}
        }
        static void Capture(string name)
        {
            var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(480,270,24,RenderTextureFormat.ARGB32);var image=new Texture2D(480,270,TextureFormat.RGBA32,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,480,270),0,0);image.Apply();File.WriteAllBytes(Out+"Screens/"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
