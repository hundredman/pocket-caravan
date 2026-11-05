using System;
using System.IO;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoTimeMusicReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA2/";
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");Directory.CreateDirectory(Folder);
            foreach(var name in new[]{"morning","day","evening","night"})File.Copy("ArtReview/Goryeo-Atmosphere-A2-20261105/Native/"+name+".wav",Folder+name+".wav",true);AssetDatabase.Refresh();
        }
    }
}
