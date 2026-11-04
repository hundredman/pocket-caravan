using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoAtmosphereReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA1/";
        [Serializable]class Source{public string status;public GoryeoAtmosphere.Settings settings;}
        [Serializable]class Approval{public string status;}
        public static void PromoteApproved()
        {
            const string art="ArtReview/Goryeo-Atmosphere-A1-20261105/";
            var approval=JsonUtility.FromJson<Approval>(File.ReadAllText(art+"approval.json"));if(approval.status!="approved")throw new InvalidDataException("A1 approval missing");
            var source=JsonUtility.FromJson<Source>(File.ReadAllText(art+"source.json"));source.status="approved";
            const string path="Assets/PocketCaravan/Resources/Goryeo/Atmosphere.json";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(source,true)+"\n");AssetDatabase.ImportAsset(path);
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save scene first");
            var source=JsonUtility.FromJson<Source>(File.ReadAllText("ArtReview/Goryeo-Atmosphere-A1-20261105/source.json"));if(source.status!="review")throw new InvalidDataException("Expected review-only source");
            Directory.CreateDirectory(Folder);var setup=EditorSceneManager.GetSceneManagerSetup();
            try{foreach(var id in new[]{"Naru","goryeo_jangdan"})
            {
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Goryeo/"+id+".unity");var root=UnityEngine.Object.FindFirstObjectByType<MapRoot>();root.previewOnly=true;
                foreach(var e in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                var atmosphere=root.gameObject.AddComponent<GoryeoAtmosphere>();atmosphere.settings=source.settings;atmosphere.approvedForProduction=false;
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+id+".unity");
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
