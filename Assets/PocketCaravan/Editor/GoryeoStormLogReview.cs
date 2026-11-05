using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoStormLogReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/StormLogs/";
        public static void BuildAudio()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");Directory.CreateDirectory(Folder);
            string path=Folder+"distant-thunder.wav";File.Copy("ArtReview/Goryeo-StormLogs-20261105/Native/distant-thunder.wav",path,true);AssetDatabase.ImportAsset(path);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
    }
}
