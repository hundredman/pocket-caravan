using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public static class GoryeoLogLayout
    {
        public const string Source="Assets/PocketCaravan/Data/Goryeo/LogArt.json";
        [Serializable]public class Art{public string variant,image;public Vector2Int[] occupied;}
        [Serializable]public class Catalogue{public int schemaVersion;public string status;public Art[] assets;}
        [Serializable]public class Binding{public string id,variant;}
        [Serializable]class ApprovedPlacement{public string status,mapId,id,variant;public int x,y,width,height;}
        public static Catalogue Read(){var c=JsonUtility.FromJson<Catalogue>(File.ReadAllText(Source));if(c.schemaVersion!=1||c.status!="approved"||c.assets.Length!=4)throw new InvalidDataException("Log art approval missing");return c;}
        public static void ImportArt()
        {
            foreach(var a in Read().assets)
            {
                AssetDatabase.ImportAsset(a.image);var im=(TextureImporter)AssetImporter.GetAtPath(a.image);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;
                var s=new TextureImporterSettings();im.ReadTextureSettings(s);s.spritePivot=Vector2.zero;s.spriteAlignment=(int)SpriteAlignment.Custom;im.SetTextureSettings(s);im.SaveAndReimport();
            }
        }
        public static void Apply(MapRoot map,Binding[] bindings)
        {
            if(bindings==null||bindings.Length==0)return;
            ApprovedPlacement approved=null;
            if(!map.previewOnly)
            {
                approved=JsonUtility.FromJson<ApprovedPlacement>(File.ReadAllText("Assets/PocketCaravan/Data/Goryeo/LogPlacement.json"));
                if(approved.status!="approved"||approved.mapId!=map.mapId||bindings.Length!=1)throw new InvalidDataException("Native log placement approval mismatch");
            }
            if(map.mapId!="transit_south_inland"&&map.mapId!="transit_central_approach")throw new InvalidDataException("Log placement scope needs review: "+map.mapId);
            var field=map.GetComponent<ObstacleField>();if(field==null)throw new InvalidDataException("Log obstacle field missing");
            var arts=Read();var entries=new List<GoryeoLogVisuals.Entry>();var ids=new HashSet<string>();
            foreach(var b in bindings)
            {
                var p=field.placements.Find(v=>v.id==b.id);var a=Array.Find(arts.assets,v=>v.variant==b.variant);
                if(!ids.Add(b.id)||p==null||p.definitionId!="fallen_log"||!b.id.StartsWith("e3_",StringComparison.Ordinal)||a==null||p.width!=3||p.height!=3)throw new InvalidDataException("Invalid log binding");
                if(approved!=null&&(p.id!=approved.id||b.variant!=approved.variant||p.x!=approved.x||p.y!=approved.y||p.width!=approved.width||p.height!=approved.height))throw new InvalidDataException("Unapproved native log placement");
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(a.image);if(sprite==null||sprite.rect.size!=new Vector2(48,48)||sprite.pixelsPerUnit!=16||sprite.pivot!=Vector2.zero)throw new InvalidDataException("Log native import changed");
                foreach(var c in a.occupied)if(c.x<0||c.y<0||c.x>=3||c.y>=3)throw new InvalidDataException("Log collision out of range");
                entries.Add(new GoryeoLogVisuals.Entry{placementId=b.id,sprite=sprite,occupied=a.occupied});
            }
            map.gameObject.AddComponent<GoryeoLogVisuals>().entries=entries.ToArray();
        }
    }
}
