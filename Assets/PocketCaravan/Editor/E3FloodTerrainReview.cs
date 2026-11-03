using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace PocketCaravan.EditorTools
{
    public static class E3FloodTerrainReview
    {
        public const string Root="Assets/PocketCaravan/Editor/B2Preview/E3FloodTerrain";
        [Serializable] class Data{public int w,h;public int[] ground,deco,blocked,road;public string[] palette;}
        [MenuItem("Pocket Caravan/Tests/E3 범람 지형 검토 만들기")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var file in Directory.GetFiles(Root+"/Tiles","*.png"))
            {
                var im=AssetImporter.GetAtPath(file)as TextureImporter;
                if(im.textureType!=TextureImporterType.Sprite || im.spritePixelsPerUnit!=16 || im.filterMode!=FilterMode.Point || im.mipmapEnabled || im.textureCompression!=TextureImporterCompression.Uncompressed)
                {im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;im.SaveAndReimport();}
            }
            try
            {
                Directory.CreateDirectory(Root+"/Scenes");
                foreach(var name in new[]{"flood","normal"})MapJsonBuilder.BuildSingleMap(File.ReadAllText(Root+"/Maps/"+name+".json"),Root+"/Scenes/"+name+".unity",Root);
                var dry=JsonUtility.FromJson<Data>(File.ReadAllText(Root+"/Maps/normal.json"));var wet=JsonUtility.FromJson<Data>(File.ReadAllText(Root+"/Maps/flood.json"));
                var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();var preview=map.gameObject.AddComponent<E3FloodTerrainPreview>();var cells=new List<E3FloodTerrainPreview.Cell>();
                TileBase blocker=null;foreach(var c in map.blocked.cellBounds.allPositionsWithin)if(map.blocked.HasTile(c)){blocker=map.blocked.GetTile(c);break;}
                if(blocker==null)throw new InvalidOperationException("Missing blocker");
                for(int i=0;i<dry.ground.Length;i++)
                {
                    if(dry.ground[i]==wet.ground[i] && dry.deco[i]==wet.deco[i] && dry.blocked[i]==wet.blocked[i] && dry.road[i]==wet.road[i])continue;
                    cells.Add(new E3FloodTerrainPreview.Cell{position=new Vector3Int(i%dry.w,dry.h-1-i/dry.w,0),dryGround=Tile(dry,dry.ground[i]),wetGround=Tile(wet,wet.ground[i]),dryDeco=Tile(dry,dry.deco[i]),wetDeco=Tile(wet,wet.deco[i]),dryBlocked=dry.blocked[i]!=0?blocker:null,wetBlocked=wet.blocked[i]!=0?blocker:null,dryGrade=dry.road[i],wetGrade=wet.road[i]});
                }
                preview.cells=cells.ToArray();
                // Keep this test self-contained: no walking into registered production scenes.
                foreach(var exit in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))exit.gameObject.SetActive(false);
                var spawn=new GameObject("Spawn S0 terrain review").AddComponent<SpawnPoint>();spawn.id=SpawnPoint.StartId;spawn.transform.position=map.CellCenter(new Vector3Int(43,dry.h-1-21,0));
                var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();player.transform.position=spawn.transform.position-(Vector3)player.GetComponent<PlayerMotor>().feetOffset;
                Camera.main.transform.position=new Vector3(player.transform.position.x,player.transform.position.y,-10);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/normal.unity");
                AssetDatabase.SaveAssets();Debug.Log("Flood terrain preview built: "+cells.Count+" state cells; production unchanged");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static TileBase Tile(Data data,int i){if(i==0)return null;var tile=AssetDatabase.LoadAssetAtPath<TileBase>(Root+"/TileAssets/"+data.palette[i]+".asset");if(tile==null)throw new InvalidOperationException("Missing tile "+data.palette[i]);return tile;}
        [MenuItem("Pocket Caravan/Tests/E3 범람 지형 검토 실행")]
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(480,270);EditorSceneManager.OpenScene(Root+"/Scenes/normal.unity");EditorApplication.isPlaying=true;}
    }
}
