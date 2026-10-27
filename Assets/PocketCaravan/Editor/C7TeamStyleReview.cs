using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    // Isolated actual Play camera. Staged art assessment, not a village or walk test.
    [InitializeOnLoad]
    public static class C7TeamStyleReview
    {
        const string Key="PocketCaravan.C7TeamStyle", Root="ArtReview/C7-TeamStyle-20261027/";
        const string Preview="Assets/PocketCaravan/Editor/C6Preview/C7-team-style.unity";
        static double ready;
        static int phase;
        static GameObject stage;
        static C7TeamStyleReview(){EditorApplication.update+=Update;EditorApplication.playModeStateChanged+=Changed;}
        public static void Run()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Preview));Directory.CreateDirectory(Root+"Screens-v5");
            var scene=EditorSceneManager.OpenScene(WorldRegistry.Load().Map("Gaegyeong").scenePath);
            UnityEngine.Object.FindAnyObjectByType<MapRoot>().previewOnly=true;
            EditorSceneManager.SaveScene(scene,Preview);
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+"done",false);EditorApplication.isPlaying=true;
        }
        static void Changed(PlayModeStateChange s)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(s==PlayModeStateChange.EnteredPlayMode){phase=0;ready=EditorApplication.timeSinceStartup+3;}
            if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+"done",false))
            {SessionState.EraseBool(Key);SessionState.EraseBool(Key+"done");EditorApplication.Exit(0);}
        }
        static Sprite Load(string path)
        {
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!t.LoadImage(File.ReadAllBytes(path)))throw new Exception(path);
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;
            return Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.zero,16,0,SpriteMeshType.FullRect);
        }
        static void Put(string path,float x,float y,int order=1)
        {
            var go=new GameObject(Path.GetFileNameWithoutExtension(path));go.transform.SetParent(stage.transform,false);go.transform.localPosition=new Vector3(x,y,0);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=Load(path);r.sortingOrder=order;
        }
        static int MainRoadY(int x)=>7+(int)Math.Round(Math.Sin((x-5)*.22)*1.3);
        static bool Grass(int x,int y)
        {
            bool main=Math.Abs(y-MainRoadY(x))<=1;
            double branchX=11.5+y*.32+Math.Sin(y*.55)*.55;
            bool branch=y<9&&Math.Abs(x-branchX)<1.3;
            return !main&&!branch;
        }
        static int Mask(int x,int y)
        {
            int m=0;if(Grass(x,y+1))m|=1;if(Grass(x+1,y))m|=2;if(Grass(x,y-1))m|=4;if(Grass(x-1,y))m|=8;
            if((m&3)==3&&Grass(x+1,y+1))m|=16;if((m&6)==6&&Grass(x+1,y-1))m|=32;
            if((m&12)==12&&Grass(x-1,y-1))m|=64;if((m&9)==9&&Grass(x-1,y+1))m|=128;return m;
        }
        static void Build(bool after)
        {
            if(stage!=null)UnityEngine.Object.DestroyImmediate(stage);
            stage=new GameObject("C7 isolated art stage");
            for(int y=0;y<17;y++)for(int x=0;x<30;x++)
            {
                if(after)
                {
                    int variant=(x*11+y*7)%4,mask=Mask(x,y);
                    string piece=Grass(x,y)?(mask==255&&variant>0?"grass-center-"+variant:"grass-"+mask):(variant>0?"dirt-"+variant:"dirt");
                    Put(Root+"Native/"+piece+".png",x,y,0);
                }
                else Put("ArtReview/C7-Rebuild-20261027/Native/"+(Grass(x,y)?"grass-"+Mask(x,y):"dirt")+".png",x,y,0);
            }
            string b="ArtReview/C7-Rebuild-20261027/Native/";
            Put(after?Root+"Native/thatch.png":b+"thatch.png",5,10,2);
            Put(after?Root+"Native/tile-house.png":b+"tile-house.png",11,10,2);
            Put(after?Root+"Native/pine.png":b+"pine.png",20,10,2);
            // Both variants use the exact approved player/NPC frames and all 22 pond cells.
            Put("Assets/PocketCaravan/Art/Characters/tanuki_tall_d0.png",11,6,3);
            Put("ArtReview/Approved/woman-front-v4.png",13,6,3);
            Put("Assets/PocketCaravan/Art/Characters/NPC/npc_naru_chief/npc_naru_chief_tall_d0.png",15,6,3);
            foreach(var line in File.ReadAllLines("ArtReview/Approved/A4-Pond/unity-pond-placement.csv"))
            {
                if(string.IsNullOrWhiteSpace(line))continue;var p=line.Split(',');int x=int.Parse(p[0])-24+20,y=int.Parse(p[1])-36+2;
                Put("Assets/PocketCaravan/Art/NinjaAdventure/Tiles/"+p[3]+".png",x,y,1);
                Put("ArtReview/Approved/A4-Pond/pieces/"+p[2]+".png",x,y,2);
            }
        }
        static void Update()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<ready)return;
            try
            {
                var map=MapRoot.Current;if(map==null||GameManager.I==null)return;
                if(!map.previewOnly)throw new Exception("C7 must be previewOnly");
                var cam=Camera.main;
                if(phase==0)
                {
                    GameManager.I.EnterTestSession();Time.timeScale=0;
                    foreach(var b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))b.enabled=false;
                    foreach(var t in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))t.ClearAllTiles();
                    foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))r.enabled=false;
                    cam.orthographic=true;cam.orthographicSize=270f/32;cam.transform.position=new Vector3(15,8.4375f,-10);cam.backgroundColor=new Color32(139,168,101,255);
                    Build(false);phase=1;ready=EditorApplication.timeSinceStartup+2;
                }
                else if(phase==1){Capture(cam,"before");Build(true);phase=2;ready=EditorApplication.timeSinceStartup+2;}
                else
                {
                    Capture(cam,"trial");File.WriteAllText(Root+"Screens-v5/complete.txt","Actual Unity Play render; previous C7 rebuild vs teammate-style revision, same curved staging route; staged art review, previewOnly true; PPU16, Point, 480x270; existing approved characters and all 22 pond cells; no navigation or manual-play claim.\n");
                    SessionState.SetBool(Key+"done",true);Time.timeScale=1;EditorApplication.isPlaying=false;
                }
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText(Root+"Screens-v5/error.txt",e.ToString());SessionState.EraseBool(Key);EditorApplication.Exit(1);}
        }
        static void Capture(Camera cam,string name)
        {
            var old=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(480,270,24,RenderTextureFormat.ARGB32);var image=new Texture2D(480,270,TextureFormat.RGBA32,false);
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,480,270),0,0);image.Apply();File.WriteAllBytes(Root+"Screens-v5/"+name+".png",image.EncodeToPNG());}
            finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
