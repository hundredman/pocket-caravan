using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class RiverFloodRuntimeReview
    {
        public const string Root="Assets/PocketCaravan/Editor/B2Preview/E3FloodTerrain";
        [MenuItem("Pocket Caravan/Tests/E3 범람 자동 연결 검토 만들기")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                MapJsonBuilder.BuildSingleMap(File.ReadAllText(Root+"/Maps/runtime.json"),Root+"/Scenes/runtime.unity",Root);
                var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
                foreach(var e in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                var spawn=new GameObject("Spawn S0 runtime review").AddComponent<SpawnPoint>();spawn.id=SpawnPoint.StartId;spawn.transform.position=map.CellCenter(new Vector3Int(43,42,0));
                var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();player.transform.position=spawn.transform.position-(Vector3)player.GetComponent<PlayerMotor>().feetOffset;
                Camera.main.transform.position=new Vector3(player.transform.position.x,player.transform.position.y,-10);
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/runtime.unity");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [MenuItem("Pocket Caravan/Tests/E3 범람 자동 연결 검토 실행")]
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(480,270);EditorSceneManager.OpenScene(Root+"/Scenes/runtime.unity");EditorApplication.isPlaying=true;}
    }
}
