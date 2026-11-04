using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class SocialGoryeoReview
    {
        public const string Scene="Assets/PocketCaravan/Editor/B2Preview/Social20261104/woodtown.unity";
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save edits first");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Transit/transit_woodtown.unity");
                var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                foreach(var e in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Scene));EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(960,540);EditorSceneManager.OpenScene(Scene);Application.runInBackground=true;EditorApplication.isPlaying=true;}
    }
}
