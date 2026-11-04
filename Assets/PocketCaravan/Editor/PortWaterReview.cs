using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public static class PortWaterReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/PortWater20261104/";
        public static void SaveBefore()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save scene first");
            var setup=EditorSceneManager.GetSceneManagerSetup();Directory.CreateDirectory(Folder);
            try{foreach(var mid in new[]{"transit_southport","transit_ferry"})
            {
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Transit/"+mid+".unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                foreach(var exit in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))exit.gameObject.SetActive(false);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+mid+"-before.unity");
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
