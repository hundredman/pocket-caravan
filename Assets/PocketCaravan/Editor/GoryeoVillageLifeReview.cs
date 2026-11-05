using System;
using System.IO;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoVillageLifeReview
    {
        public const string Review="ArtReview/Goryeo-Atmosphere-A7-20261105/",Folder="Assets/PocketCaravan/Editor/B2Preview/AtmosphereA7/";
        [Serializable]public class Map{public string id;public int w,h;public GoryeoVillageLife.Point[] points;}
        [Serializable]public class Asset{public string key,path;}
        [Serializable]public class Config{public string status;public Map[] maps;public Asset[] assets;}
        public static void PromoteApproved()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            var approval=JsonUtility.FromJson<Config>(File.ReadAllText(Review+"approval.json"));if(approval.status!="approved")throw new InvalidDataException("A7 approval missing");
            var config=JsonUtility.FromJson<Config>(File.ReadAllText(Review+"source.json"));config.status="approved";
            const string folder="Assets/PocketCaravan/Resources/Goryeo/VillageLife/";Directory.CreateDirectory(folder+"Art/");Directory.CreateDirectory(folder+"Trader/");
            foreach(var asset in config.assets){var path=folder+"Art/"+asset.key+".png";File.Copy(asset.path,path,true);Import(path,asset.key);}
            string dirs="dulr";for(int d=0;d<4;d++)for(int f=0;f<4;f++){var path=folder+"Trader/"+(d*4+f)+".png";File.Copy("Assets/PocketCaravan/Art/Characters/NPC/npc_goryeo_songak_merchant/npc_goryeo_songak_merchant_tall_"+dirs[d]+f+".png",path,true);Import(path,"trader");}
            File.WriteAllText(folder+"Anchors.json",JsonUtility.ToJson(config,true)+"\n");AssetDatabase.Refresh();
        }
        static void Import(string path,string key)
        {
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,key.StartsWith("chicken",StringComparison.Ordinal)||key.StartsWith("dog",StringComparison.Ordinal)||key=="cart"?.0625f:key=="lamp"?0:key=="trader"?.25f:.5f);importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
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
