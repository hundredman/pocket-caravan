using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoLifeSoundReview
    {
        public const string Review="ArtReview/Goryeo-Atmosphere-A3-20261105/";
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA3/";
        [Serializable]public class Map{public string id;public int w,h;public GoryeoLifeSound.Point[] points;}
        [Serializable]public class Config{public string status;public Map[] maps;}
        public static void PromoteApproved()
        {
            var approval=JsonUtility.FromJson<Config>(File.ReadAllText(Review+"approval.json"));if(approval.status!="approved")throw new InvalidDataException("A3 approval missing");
            var config=JsonUtility.FromJson<Config>(File.ReadAllText(Review+"source.json"));config.status="approved";
            const string folder="Assets/PocketCaravan/Resources/Goryeo/Life/";Directory.CreateDirectory(folder);
            foreach(var kind in new[]{"crowd","chicken","cow","dog","bird"})File.Copy(Review+"Native/"+kind+".wav",folder+kind+".wav",true);
            File.WriteAllText(folder+"Anchors.json",JsonUtility.ToJson(config,true)+"\n");AssetDatabase.Refresh();
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            Directory.CreateDirectory(Folder);foreach(var kind in new[]{"crowd","chicken","cow","dog","bird"})File.Copy(Review+"Native/"+kind+".wav",Folder+kind+".wav",true);AssetDatabase.Refresh();
        }
        public static GoryeoLifeSound AttachReview(MapRoot map)
        {
            if(!EditorApplication.isPlaying||!map.previewOnly)throw new InvalidOperationException("Preview Play only");
            var config=JsonUtility.FromJson<Config>(File.ReadAllText(Review+"source.json"));if(config.status!="review")throw new InvalidDataException("Review config expected");
            var row=Array.Find(config.maps,v=>v.id==map.mapId);if(row==null)throw new InvalidDataException("Unknown map");
            var sound=map.gameObject.AddComponent<GoryeoLifeSound>();sound.points=row.points;int i=0;foreach(var kind in new[]{"crowd","chicken","cow","dog","bird"})sound.clips[i++]=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+kind+".wav");return sound;
        }
    }
}
