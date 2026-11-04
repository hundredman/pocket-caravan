using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public static class LongLogTrialReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/LongLog20261104/";
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save scene first");
            Directory.CreateDirectory(Folder);
            foreach(var v in new[]{"before","after"})
            {
                string src=v=="before"?"ArtReview/E3-E2-Art-20261103/Native/fallen_log.png":"ArtReview/LongLog-v2-20261104/Native/fallen_log.png";string file=Folder+v+".png";File.Copy(src,file,true);AssetDatabase.ImportAsset(file);
                var im=(TextureImporter)AssetImporter.GetAtPath(file);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();im.ReadTextureSettings(settings);settings.spritePivot=Vector2.zero;settings.spriteAlignment=(int)SpriteAlignment.Custom;im.SetTextureSettings(settings);im.SaveAndReimport();
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Transit/transit_south_inland.unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                foreach(var exit in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))exit.gameObject.SetActive(false);
                var go=new GameObject("Long log art trial");go.transform.SetParent(map.transform,false);go.transform.position=new Vector3(28,41,0);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"before.png");sr.sortingOrder=8;
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+"trial.unity");
            }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(960,540);EditorSceneManager.OpenScene(Folder+"trial.unity");Application.runInBackground=true;EditorApplication.isPlaying=true;}
    }
}
