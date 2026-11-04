using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class SpecialtyMarketTrialReview
    {
        public const string Folder="Assets/PocketCaravan/Editor/B2Preview/SpecialtyMarkets20261104/";
        [Serializable]class Art{public string id,image;public int w,h;}
        [Serializable]class Proposal{public string status;public GoryeoMarketLayout.Map[] maps;public Art[] assets;}
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save edits first");
            var data=JsonUtility.FromJson<Proposal>(File.ReadAllText("ArtReview/SpecialtyMarkets-Task5-20261104/layout.json"));
            if(data.status!="review"||data.maps.Length!=2)throw new InvalidDataException("Wrong review scope");
            Directory.CreateDirectory(Folder);var sprites=new Dictionary<string,Sprite>();
            foreach(var a in data.assets)
            {
                string file=Folder+a.id+".png";File.Copy(a.image,file,true);AssetDatabase.ImportAsset(file);
                var im=(TextureImporter)AssetImporter.GetAtPath(file);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;
                var settings=new TextureImporterSettings();im.ReadTextureSettings(settings);settings.spritePivot=Vector2.zero;settings.spriteAlignment=(int)SpriteAlignment.Custom;im.SetTextureSettings(settings);im.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(file);if(sprite.rect.width!=16*a.w||sprite.rect.height!=16*a.h)throw new InvalidDataException("Wrong native size");sprites.Add(a.id,sprite);
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try{foreach(var m in data.maps)
            {
                if(m.mapId!="transit_southport"&&m.mapId!="transit_crossroads")throw new InvalidDataException("Other region");
                EditorSceneManager.OpenScene("Assets/PocketCaravan/Scenes/Transit/"+m.mapId+".unity");var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
                foreach(var e in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                var targets=new List<SpriteRenderer>();var originals=new List<Sprite>();var variants=new List<Sprite>();
                int height=WorldIndex.Get().Map(m.mapId).height;
                foreach(var space in m.spaces)foreach(var p in space.props)
                {
                    if(!sprites.ContainsKey(p.asset))continue;
                    var parent=map.transform.Find("Everyday - "+space.id);if(parent==null)throw new InvalidDataException("Space missing");
                    var expected=map.ground.CellToWorld(new Vector3Int(p.x,height-p.y-p.h,0));SpriteRenderer target=null;
                    foreach(var sr in parent.GetComponentsInChildren<SpriteRenderer>())if(Vector3.Distance(sr.transform.position,expected)<.01f)target=sr;
                    if(target==null||target.sprite.rect.size!=sprites[p.asset].rect.size)throw new InvalidDataException("Target/footprint mismatch");
                    targets.Add(target);originals.Add(target.sprite);variants.Add(sprites[p.asset]);
                }
                var dressing=map.gameObject.AddComponent<SpecialtyMarketTrialDressing>();dressing.targets=targets.ToArray();dressing.originals=originals.ToArray();dressing.variants=variants.ToArray();
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Folder+m.mapId+".unity");
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(960,540);EditorSceneManager.OpenScene(Folder+"transit_southport.unity");Application.runInBackground=true;EditorApplication.isPlaying=true;}
    }
}
