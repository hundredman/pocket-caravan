using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PocketCaravan.EditorTools
{
    /// <summary>Approved market courts. Editor source is reapplied on every production scene rebuild.</summary>
    public static class GoryeoMarketLayout
    {
        public const string Source = "Assets/PocketCaravan/Data/Goryeo/MarketLayout.json";
        [Serializable] public class Art { public string id, image; public int[] size; }
        [Serializable] public class Prop { public string asset; public int x,y,w,h; }
        [Serializable] public class Space { public string id; public Prop[] props; }
        [Serializable] public class Map { public string mapId; public int width,height; public Space[] spaces; }
        [Serializable] public class Layout { public int schemaVersion; public string status; public Art[] assets; public Map[] maps; }
        public static Layout Read()
        {
            var data = JsonUtility.FromJson<Layout>(File.ReadAllText(Source));
            if (data.schemaVersion != 1 || data.status != "approved") throw new InvalidDataException("Market layout is not approved");
            return data;
        }
        public static void Apply(MapRoot root, int height)
        {
            var data = Read();
            var map = Array.Find(data.maps, m => m.mapId == root.mapId);
            if (map == null) return; // Other regions and undecided settlement functions are untouched.
            if (root.previewOnly || map.height != height) throw new InvalidDataException("Market map mismatch: " + root.mapId);
            if (root.GetComponent<VillageDressing>() != null) throw new InvalidDataException("Duplicate market layout: " + root.mapId);
            var dressing = root.gameObject.AddComponent<VillageDressing>();
            var groups = new List<VillageDressing.Space>();
            foreach (var space in map.spaces)
            {
                var normal = new GameObject("Everyday - " + space.id); normal.transform.SetParent(root.transform, false);
                var footprints = new List<RectInt>();
                foreach (var p in space.props)
                {
                    var art = Array.Find(data.assets, a => a.id == p.asset);
                    if (art == null || art.size[0] != p.w || art.size[1] != p.h || p.x < 0 || p.y < 0 || p.x+p.w > map.width || p.y+p.h > height)
                        throw new InvalidDataException("Invalid whole market object: " + root.mapId + "/" + p.asset);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art.image);
                    if (sprite == null || sprite.pixelsPerUnit != 16 || sprite.rect.width != p.w*16 || sprite.rect.height != p.h*16)
                        throw new InvalidDataException("Native market sprite missing: " + art.image);
                    var obj = new GameObject(p.asset); obj.transform.SetParent(normal.transform, false);
                    obj.transform.position = root.ground.CellToWorld(new Vector3Int(p.x, height-p.y-p.h, 0));
                    var sr = obj.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 4;
                    footprints.Add(new RectInt(p.x, height-p.y-p.h, p.w, 1));
                }
                groups.Add(new VillageDressing.Space { id=space.id, normal=normal, footprints=footprints.ToArray() });
            }
            dressing.spaces = groups.ToArray();
        }
        [MenuItem("Pocket Caravan/Art/승인 장터 본편 씬 생성")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first");
            for (int i=0; i<UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene");
            var data = Read(); var setup = EditorSceneManager.GetSceneManagerSetup();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var art in data.assets)
            {
                var im = AssetImporter.GetAtPath(art.image) as TextureImporter;
                if (im == null) throw new InvalidDataException(art.image);
                im.textureType=TextureImporterType.Sprite; im.spriteImportMode=SpriteImportMode.Single; im.spritePixelsPerUnit=16;
                var settings=new TextureImporterSettings(); im.ReadTextureSettings(settings);
                settings.spritePivot=Vector2.zero; settings.spriteAlignment=(int)SpriteAlignment.Custom; im.SetTextureSettings(settings);
                im.filterMode=FilterMode.Point; im.mipmapEnabled=false; im.textureCompression=TextureImporterCompression.Uncompressed; im.SaveAndReimport();
            }
            var registry=WorldRegistry.Load();
            try { foreach (var map in data.maps)
            {
                var entry=registry.Map(map.mapId);
                if (entry==null || (entry.region.regionId!="goryeo" && entry.region.regionId!="transit")) throw new InvalidDataException("Foreign map: "+map.mapId);
                MapJsonBuilder.BuildSingleMap(File.ReadAllText(entry.jsonPath),entry.scenePath);
            }} finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
            AssetDatabase.SaveAssets();
            Debug.Log("Approved market layouts linked to " + data.maps.Length + " production scenes");
        }
    }
}
