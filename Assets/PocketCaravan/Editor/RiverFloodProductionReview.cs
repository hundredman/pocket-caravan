using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class RiverFloodProductionReview
    {
        [MenuItem("Pocket Caravan/Tests/E3 승인 범람 본편 씬 생성")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try{MapJsonBuilder.BuildSingleMap(File.ReadAllText("Assets/PocketCaravan/Maps/transit_wetroad.json"),"Assets/PocketCaravan/Scenes/Transit/transit_wetroad.unity");}
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
