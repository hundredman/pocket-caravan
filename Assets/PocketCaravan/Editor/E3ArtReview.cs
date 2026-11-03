using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class E3ArtReview
    {
        public const string Root="Assets/PocketCaravan/Editor/B2Preview/E3Art";
        public const string Output="ArtReview/E3-E2-Art-20261103";
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene");
            var setup=EditorSceneManager.GetSceneManagerSetup();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Directory.CreateDirectory(Root+"/Scenes");
            var images=new System.Collections.Generic.List<string>(Directory.GetFiles(Root+"/Whole","*.png"));images.AddRange(Directory.GetFiles(Root+"/Tiles","*.png"));
            foreach(var path in images)
            {
                var im=AssetImporter.GetAtPath(path) as TextureImporter;im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;
                var settings=new TextureImporterSettings();im.ReadTextureSettings(settings);settings.spritePivot=path.Contains("/Tiles/")?new Vector2(.5f,.5f):Vector2.zero;settings.spriteAlignment=(int)SpriteAlignment.Custom;im.SetTextureSettings(settings);im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;im.SaveAndReimport();
            }
            // Texture imports can replace Sprite subassets; refresh only isolated Tile references.
            foreach(var file in Directory.GetFiles(Root+"/Tiles","*.png"))
            {
                var tile=AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(Root+"/TileAssets/"+Path.GetFileNameWithoutExtension(file)+".asset");
                if(tile!=null){tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(file);EditorUtility.SetDirty(tile);}
            }
            AssetDatabase.SaveAssets();
            try{foreach(var file in Directory.GetFiles(Root+"/Maps","*.json"))
            {
                var path=Root+"/Scenes/"+Path.GetFileNameWithoutExtension(file)+".unity";MapJsonBuilder.BuildSingleMap(File.ReadAllText(file),path,Root);
                if(Path.GetFileNameWithoutExtension(file)=="trial")
                {
                    var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
                    SpriteObject(map,"fallen_log",new Vector3(13,9,0),"Log candidate",8);
                    SpriteObject(map,"river_flood_high",new Vector3(13,6,0),"Flood high candidate",2);
                    SpriteObject(map,"river_flood_low",new Vector3(13,6,0),"Flood low candidate",2);
                    GameObject.Find("Flood high candidate").SetActive(false);GameObject.Find("Flood low candidate").SetActive(false);
                    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),path);
                }
                else
                {
                    // Keep approved market dressing in both isolated port views.
                    var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
                    var json=JsonUtility.FromJson<VillageFillReview.Header>(File.ReadAllText(file));
                    map.previewOnly=false;try{GoryeoMarketLayout.Apply(map,json.h);}finally{map.previewOnly=true;}
                    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),path);
                }
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            AssetDatabase.SaveAssets();Debug.Log("E3/E2 art: five isolated scenes built; production unchanged");
        }
        static void SpriteObject(MapRoot map,string asset,Vector3 position,string name,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(map.transform,false);go.transform.position=position;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Whole/"+asset+".png");sr.sortingOrder=order;
        }
    }
}
