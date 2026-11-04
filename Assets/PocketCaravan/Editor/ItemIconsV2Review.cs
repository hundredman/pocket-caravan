using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class ItemIconsV2Review
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/ItemIconsV220261104/";
        public const string V3Folder="Assets/PocketCaravan/Editor/B2Preview/ItemIconsV320261104/";
        public static void Prepare(){PrepareAt("ArtReview/ItemIcons-v2-20261104/Native/",Folder,null);}
        public static void PrepareV3(){PrepareAt("ArtReview/ItemIcons-v3-20261104/Native/",V3Folder,"ArtReview/ItemIcons-v2-20261104/Native/");}
        public static void PrepareV4(){PrepareAt("ArtReview/WoolWidth-v4-20261104/Native/","Assets/PocketCaravan/Editor/B2Preview/ItemIconsV420261104/","ArtReview/ItemIcons-v3-20261104/Native/");}
        static void PrepareAt(string after,string folder,string before)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");Directory.CreateDirectory(folder);
            foreach(var n in new[]{"grain","transit_spice","transit_wool"})foreach(var version in new[]{"before","after"})
            {
                string source=version=="after"?after+n+".png":before!=null?before+n+".png":(n=="grain"?"Assets/PocketCaravan/Art/Items/grain.png":"ArtReview/E3-E2-Art-20261103/Native/"+n+".png");string file=folder+n+"-"+version+".png";File.Copy(source,file,true);AssetDatabase.ImportAsset(file);
                var im=(TextureImporter)AssetImporter.GetAtPath(file);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;im.SaveAndReimport();
            }
        }
    }
}
