using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoStormLogReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/StormLogs/";
        [Serializable]class Approval{public string status,audioSha256,sourceSha256,effectSha256,mapId,placementId;public bool oncePerWindowPerSession;}
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();}
        public static void PromoteApproved()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            const string review="ArtReview/Goryeo-StormLogs-20261105/";var a=JsonUtility.FromJson<Approval>(File.ReadAllText(review+"thunder-approval.json"));
            if(a.status!="approved"||!a.oncePerWindowPerSession||a.mapId!=GoryeoStormLogs.MapId||a.placementId!=GoryeoStormLogs.PlacementId||a.audioSha256!=Hash(review+"Native/distant-thunder.wav")||a.sourceSha256!=Hash(review+"source.json")||a.effectSha256!=Hash(review+"approved-effect-source.txt"))throw new InvalidDataException("Thunder approval mismatch");
            const string target="Assets/PocketCaravan/Resources/Goryeo/Thunder/";Directory.CreateDirectory(target);
            File.Copy(review+"Native/distant-thunder.wav",target+"distant-thunder.wav",true);
            var config=new GoryeoThunderDirector.Config{status="approved",mapId=a.mapId,placementId=a.placementId,windowHours=RoadHazardSchedule.LogWindowHours,oncePerWindowPerSession=true};
            File.WriteAllText(target+"Config.json",JsonUtility.ToJson(config,true)+"\n");AssetDatabase.Refresh();Import(target+"distant-thunder.wav");
        }
        static void Import(string path)
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        public static void BuildAudio()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");Directory.CreateDirectory(Folder);
            string path=Folder+"distant-thunder.wav";File.Copy("ArtReview/Goryeo-StormLogs-20261105/Native/distant-thunder.wav",path,true);AssetDatabase.ImportAsset(path);
            Import(path);
        }
    }
}
