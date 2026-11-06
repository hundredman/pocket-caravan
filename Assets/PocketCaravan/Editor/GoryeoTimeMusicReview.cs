using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoTimeMusicReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA2/";
        [Serializable]class Approval{public string status,sourceSha256;public Tracks tracks;}
        [Serializable]class Tracks{public string morning,day,evening,night;}
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();}
        public static void PromoteApproved()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            const string review="ArtReview/Goryeo-Atmosphere-A2-20261105/";
            var a=JsonUtility.FromJson<Approval>(File.ReadAllText(review+"approval.json"));
            if(a.status!="approved"||a.sourceSha256!=Hash(review+"source.json")||a.tracks==null)throw new InvalidDataException("A2 approval/source mismatch");
            var names=new[]{"morning","day","evening","night"};var hashes=new[]{a.tracks.morning,a.tracks.day,a.tracks.evening,a.tracks.night};
            for(int i=0;i<4;i++)if(hashes[i]!=Hash(review+"Native/"+names[i]+".wav"))throw new InvalidDataException("A2 approved audio mismatch: "+names[i]);
            const string target="Assets/PocketCaravan/Resources/Goryeo/TimeMusic/";Directory.CreateDirectory(target);
            foreach(var name in names)File.Copy(review+"Native/"+name+".wav",target+name+".wav",true);
            var config=new GoryeoTimeMusicDirector.Config{status="approved",maps=new[]{"Naru","Gaegyeong","Dakmok","goryeo_songak","goryeo_jangdan","transit_woodtown","transit_ferry","transit_southport","transit_crossroads","transit_wetroad","transit_pass","transit_central_approach","transit_south_inland"}};
            File.WriteAllText(target+"Config.json",JsonUtility.ToJson(config,true)+"\n");AssetDatabase.Refresh();
            foreach(var name in names)
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(target+name+".wav");var settings=importer.defaultSampleSettings;
                settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");Directory.CreateDirectory(Folder);
            foreach(var name in new[]{"morning","day","evening","night"})File.Copy("ArtReview/Goryeo-Atmosphere-A2-20261105/Native/"+name+".wav",Folder+name+".wav",true);AssetDatabase.Refresh();
        }
    }
}
