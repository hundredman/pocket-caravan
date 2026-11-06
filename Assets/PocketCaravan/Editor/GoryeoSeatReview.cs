using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoSeatReview
    {
        [Serializable]class Approval{public string status,sourceSha256;public ApprovedArt[] art;}
        [Serializable]class ApprovedArt{public string file,rgbaSha256;}
        static string Sha(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        public static void PromoteApproved()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            const string review="ArtReview/Goryeo-Seats-20261105/AlignedV2/";const string target="Assets/PocketCaravan/Resources/Goryeo/Seats/";
            var a=JsonUtility.FromJson<Approval>(File.ReadAllText(review+"approval.json"));var motion=JsonUtility.FromJson<Approval>(File.ReadAllText("ArtReview/Goryeo-Seats-20261105/Motion/approval.json"));
            if(a.status!="approved"||motion.status!="approved"||a.sourceSha256!=Sha(File.ReadAllBytes(review+"source.json"))||a.art==null||a.art.Length!=8)throw new InvalidDataException("Seat approval missing/mismatch");
            // Pixel fingerprint uses top-to-bottom row order, like the PNG review ledger.
            foreach(var art in a.art)
            {
                var t=new Texture2D(2,2,TextureFormat.RGBA32,false);try{t.LoadImage(File.ReadAllBytes(art.file));var pixels=t.GetPixels32();var bytes=new byte[pixels.Length*4];for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++){var c=pixels[(t.height-1-y)*t.width+x];int n=(y*t.width+x)*4;bytes[n]=c.r;bytes[n+1]=c.g;bytes[n+2]=c.b;bytes[n+3]=c.a;}if(Sha(bytes)!=art.rgbaSha256)throw new InvalidDataException("Seat pixel mismatch "+art.file);}finally{UnityEngine.Object.DestroyImmediate(t);}
            }
            var config=JsonUtility.FromJson<GoryeoSeat.Config>(File.ReadAllText(review+"source.json"));Directory.CreateDirectory(target+"Art");Directory.CreateDirectory(target+"Fire");
            foreach(var p in config.poses){var path=target+"Art/"+p.originId+".png";File.Copy(p.file,path,true);Import(path,p.ppu,p.pivotY);p.file="Goryeo/Seats/Art/"+p.originId;}
            for(int y=0;y<2;y++)for(int x=0;x<2;x++){string path=target+"Fire/"+(y*2+x)+".png";File.Copy("Assets/PocketCaravan/Art/NinjaAdventure/Tiles/camp_"+(12+x)+"_"+(5+y)+".png",path,true);Import(path,16,.5f);}
            config.status="approved";config.sprite="Goryeo/Seats/Art/goryeo";File.WriteAllText(target+"Config.json",JsonUtility.ToJson(config,true)+"\n");AssetDatabase.Refresh();
        }
        static void Import(string path,float ppu,float pivot)
        {
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=ppu;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,pivot);importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
    }
}
