using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoPlaceLabelsReview
    {
        [Serializable]class Approval{public string status;}
        public static void PromoteApproved()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            const string review="ArtReview/Goryeo-Atmosphere-A5-20261105/";
            if(JsonUtility.FromJson<Approval>(File.ReadAllText(review+"approval.json")).status!="approved")throw new InvalidDataException("A5 approval missing");
            var config=JsonUtility.FromJson<GoryeoPlaceLabelsDirector.Config>(File.ReadAllText(review+"source.json"));config.status="approved";
            const string path="Assets/PocketCaravan/Resources/Goryeo/PlaceLabels.json";File.WriteAllText(path,JsonUtility.ToJson(config,true)+"\n");AssetDatabase.ImportAsset(path);
        }
    }
}
