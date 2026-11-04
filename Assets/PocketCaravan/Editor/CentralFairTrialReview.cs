using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class CentralFairTrialReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/CentralFair20261104/";
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save edits first");
            Directory.CreateDirectory(Folder);var sprites=new Sprite[2];int index=0;
            foreach(var n in new[]{"fair_indigo","fair_teal"})
            {
                File.Copy("ArtReview/CentralFair-Task4-20261104/Native/"+n+".png",Folder+n+".png",true);AssetDatabase.ImportAsset(Folder+n+".png");
                var im=(TextureImporter)AssetImporter.GetAtPath(Folder+n+".png");im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();im.ReadTextureSettings(settings);settings.spritePivot=Vector2.zero;settings.spriteAlignment=(int)SpriteAlignment.Custom;im.SetTextureSettings(settings);im.SaveAndReimport();sprites[index++]=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+n+".png");
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try{foreach(var id in new[]{"transit_woodtown","transit_ferry"})
            {
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Transit/"+id+".unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                foreach(var e in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                map.gameObject.AddComponent<CentralFairTrialDressing>().variants=sprites;
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+id+".unity");
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(960,540);EditorSceneManager.OpenScene(Folder+"transit_woodtown.unity");Application.runInBackground=true;EditorApplication.isPlaying=true;}
    }
}
