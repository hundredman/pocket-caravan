using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoStrideReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA4/";
        [Serializable]public class Map{public string id,data;public int w,h;}
        [Serializable]public class Source{public string status;public Map[] maps;}
        public static void PromoteApproved()
        {
            const string review="ArtReview/Goryeo-Atmosphere-A4-v3-20261105/";
            var approval=JsonUtility.FromJson<Source>(File.ReadAllText(review+"approval.json"));
            if(approval.status!="approved")throw new InvalidDataException("A4 approval missing");
            var source=JsonUtility.FromJson<Source>(File.ReadAllText(review+"source.json"));source.status="approved";
            const string target="Assets/PocketCaravan/Resources/Goryeo/Stride/";Directory.CreateDirectory(target);
            foreach(var name in new[]{"dirt","grass","stone","wood","splash"})File.Copy(review+"Native/"+name+".wav",target+name+".wav",true);
            File.WriteAllText(target+"Surfaces.json",JsonUtility.ToJson(source,true)+"\n");AssetDatabase.Refresh();
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save scene first");
            const string review="ArtReview/Goryeo-Atmosphere-A4-v3-20261105/";var source=JsonUtility.FromJson<Source>(File.ReadAllText(review+"source.json"));if(source.status!="review")throw new InvalidDataException("Expected review");Directory.CreateDirectory(Folder);
            foreach(var name in new[]{"dirt","grass","stone","wood","splash"}){var file=Folder+name+".wav";File.Copy(review+"Native/"+name+".wav",file,true);AssetDatabase.ImportAsset(file);}
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                const string id="Naru";EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Goryeo/"+id+".unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                foreach(var exit in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))exit.gameObject.SetActive(false);
                var data=Array.Find(source.maps,v=>v.id==id);var stride=map.gameObject.AddComponent<GoryeoStride>();stride.width=data.w;stride.height=data.h;stride.surfaces=data.data;
                int index=0;foreach(var name in new[]{"dirt","grass","stone","wood","splash"})stride.clips[index++]=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+name+".wav");
                map.gameObject.AddComponent<GoryeoAtmosphere>();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+id+".unity");
            }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
