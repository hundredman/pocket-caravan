using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoVillageLifeReview
    {
        public const string Review="ArtReview/Goryeo-Atmosphere-A7-20261105/",Folder="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA7/";
        [Serializable]public class Map{public string id;public GoryeoVillageLife.Point[] points;}
        [Serializable]public class Asset{public string key,path;}
        [Serializable]public class Config{public string status;public Map[] maps;public Asset[] assets;}
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");Directory.CreateDirectory(Folder);
            var config=JsonUtility.FromJson<Config>(File.ReadAllText(Review+"source.json"));if(config.status!="review")throw new InvalidDataException("Review only");
            foreach(var asset in config.assets)
            {
                var path=Folder+asset.key+".png";File.Copy(asset.path,path,true);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                if(asset.key.StartsWith("chicken",StringComparison.Ordinal)||asset.key.StartsWith("dog",StringComparison.Ordinal)||asset.key=="lamp"||asset.key=="cart"){var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,asset.key=="lamp"?0:.0625f);importer.SetTextureSettings(settings);}
                importer.SaveAndReimport();
            }
        }
    }
}
