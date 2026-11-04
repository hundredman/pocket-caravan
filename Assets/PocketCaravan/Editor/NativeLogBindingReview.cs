using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public static class NativeLogBindingReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/LogBinding20261104/";
        [Serializable]class Candidate{public bool previewOnly;public string id;public ObstaclePlacement[] obstacles;public GoryeoLogLayout.Binding[] nativeLogs;}
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save scene first");
            GoryeoLogLayout.ImportArt();Directory.CreateDirectory(Folder);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var name in new[]{"horizontal","diagonal_ne","diagonal_nw","vertical","forest-horizontal","forest-diagonal"})
                {
                    var c=JsonUtility.FromJson<Candidate>(File.ReadAllText("ArtReview/LogBinding-20261104/"+name+".json"));if(!c.previewOnly||(c.id!="transit_south_inland"&&c.id!="transit_central_approach"))throw new InvalidDataException("Expected isolated log candidate");
                    EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Transit/"+c.id+".unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                    if(map.GetComponent<ObstacleField>()!=null)throw new InvalidOperationException("Existing obstacles require review");
                    foreach(var e in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                    var field=map.gameObject.AddComponent<ObstacleField>();field.library=Resources.Load<ObstacleLibrary>("Obstacles/ObstacleLibrary");field.placements=new List<ObstaclePlacement>(c.obstacles);GoryeoLogLayout.Apply(map,c.nativeLogs);
                    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+name+".unity");
                }
            }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
