using System;using System.IO;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class VillageFillReview
    {
        public const string Root="Assets/PocketCaravan/Editor/B2Preview/VillageFill";
        public const string Output="ArtReview/VillageFill-20261030";
        [Serializable] public class Asset {public string id,image;public int[] size;}
        [Serializable] public class Prop {public string asset;public int x,y,w,h;}
        [Serializable] public class Space {public string id;public Prop[] props;}
        [Serializable] public class Map {public string mapId,village;public Space[] spaces;}
        [Serializable] public class Manifest {public Asset[] assets;public Map[] maps;}
        [Serializable] public class Header {public int h;}
        [MenuItem("Pocket Caravan/Review/마을 공터 검토 (저장 안 함)")]
        public static void OpenVillageManual()
        {
            SessionState.SetString("Codex.NatureIntegrationReview.Root",Root);
            SessionState.SetString("Codex.NatureIntegrationReview.Output","ArtReview/VillageFill-20261030/Manual");
            Directory.CreateDirectory(NatureIntegrationReview.Output);
            NatureIntegrationReview.OpenManual();
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before building review scenes");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene");
            var setup=EditorSceneManager.GetSceneManagerSetup();var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Output+"/manifest.json"));
            Directory.CreateDirectory(Root+"/Scenes");AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var a in data.assets){var im=(TextureImporter)AssetImporter.GetAtPath(a.image);im.textureType=TextureImporterType.Sprite;im.spriteImportMode=SpriteImportMode.Single;im.spritePixelsPerUnit=16;var settings=new TextureImporterSettings();im.ReadTextureSettings(settings);settings.spritePivot=Vector2.zero;settings.spriteAlignment=(int)SpriteAlignment.Custom;im.SetTextureSettings(settings);im.filterMode=FilterMode.Point;im.mipmapEnabled=false;im.textureCompression=TextureImporterCompression.Uncompressed;im.SaveAndReimport();}
            // Existing map importer handles all generated review tiles; common database/build list untouched.
            SessionState.SetString("Codex.NatureIntegrationReview.Root",Root);SessionState.SetString("Codex.NatureIntegrationReview.Output",Output+"/Build");NatureIntegrationReview.Build();
            try {foreach(var map in data.maps){string path=Root+"/Scenes/"+map.mapId+".unity";var scene=EditorSceneManager.OpenScene(path);var root=UnityEngine.Object.FindFirstObjectByType<MapRoot>();var dressing=root.gameObject.AddComponent<VillageDressing>();var groups=new List<VillageDressing.Space>();var h=JsonUtility.FromJson<Header>(File.ReadAllText(Root+"/Maps/"+map.mapId+".json")).h;
                foreach(var space in map.spaces){var go=new GameObject("Everyday - "+space.id);go.transform.SetParent(root.transform,false);var rects=new List<RectInt>();
                    foreach(var prop in space.props){var a=Array.Find(data.assets,item=>item.id==prop.asset);var obj=new GameObject(prop.asset);obj.transform.SetParent(go.transform,false);obj.transform.position=root.ground.CellToWorld(new Vector3Int(prop.x,h-prop.y-prop.h,0));var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(a.image);sr.sortingOrder=4;int rows=prop.asset=="thatch"?Math.Max(1,prop.h-1):1;rects.Add(new RectInt(prop.x,h-prop.y-prop.h,prop.w,rows));}
                    groups.Add(new VillageDressing.Space{id=space.id,normal=go,footprints=rects.ToArray()});
                }dressing.spaces=groups.ToArray();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,path);
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            AssetDatabase.SaveAssets();Debug.Log("Village dressing preview built; production maps unchanged");
        }
    }
}
