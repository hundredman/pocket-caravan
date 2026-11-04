using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class ItemIconsV2Review
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/ItemIconsV220261104/";
        public static void Prepare()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");Directory.CreateDirectory(Folder);
            foreach(var n in new[]{"grain","transit_spice","transit_wool"})foreach(var version in new[]{"before","after"})
            {
                string source=version=="after"?"ArtReview/ItemIcons-v2-20261104/Native/"+n+".png":(n=="grain"?"Assets/PocketCaravan/Art/Items/grain.png":"ArtReview/E3-E2-Art-20261103/Native/"+n+".png");string file=Folder+n+"-"+version+".png";File.Copy(source,file,true);AssetDatabase.ImportAsset(file);
                var im=(TextureImporter)AssetImporter.GetAtPath(file);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;im.SaveAndReimport();
            }
        }
    }
}
