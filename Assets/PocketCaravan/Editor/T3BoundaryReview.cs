using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>Regenerable, editor-only review. Never rebuilds registered scenes or build settings.</summary>
    [InitializeOnLoad] public static class T3BoundaryReview
    {
        public const string Root="Assets/PocketCaravan/Editor/B2Preview/T3Boundaries";
        const string Key="Codex.T3BoundaryReview";
        [Serializable]class Plan{public Metric[] maps;}
        [Serializable]class Metric{public string mapId;}
        static T3BoundaryReview()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
                bool capture=SessionState.GetBool(Key+".capture",false);SessionState.SetBool(Key,false);
                var runner=UnityEngine.Object.FindFirstObjectByType<T3BoundaryExplorer>()??new GameObject("T3 boundary review").AddComponent<T3BoundaryExplorer>();runner.capture=capture;runner.auditForeign=SessionState.GetBool(Key+".foreign",false);SessionState.SetBool(Key+".foreign",false);
            };
        }
        [MenuItem("Pocket Caravan/Tests/T3 경계 검토 씬 생성")]
        public static void Build()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Stop Play/save current scene first");
            var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Docs/Validation/T3-Boundaries-2026-11-07/geometry.json"));
            var setup=EditorSceneManager.GetSceneManagerSetup();Directory.CreateDirectory(Root+"/Scenes");
            try{foreach(var row in plan.maps){
                string path=Root+"/Scenes/"+row.mapId+".unity";
                MapJsonBuilder.BuildSingleMap(File.ReadAllText(Root+"/Maps/"+row.mapId+".json"),path);
                var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
                ApplyMarket(map,row.mapId);
                if(row.mapId=="transit_crossroads")ApplyExitSigns(map);
                new GameObject("Preview-only routing").AddComponent<T3BoundaryPreviewGate>();
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),path);
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            AssetDatabase.SaveAssets();Debug.Log("T3: 13 previewOnly scenes built; production scenes/build list unchanged");
        }
        static void ApplyMarket(MapRoot root,string id)
        {
            // Same approved whole sprites at the same world positions, independently of production guard.
            var baseline=JsonUtility.FromJson<OldMap>(File.ReadAllText("Assets/PocketCaravan/Maps/"+id+".json"));
            var layout=GoryeoMarketLayout.Read();var map=Array.Find(layout.maps,m=>m.mapId==id);if(map==null)return;
            foreach(var space in map.spaces)foreach(var p in space.props){
                var art=Array.Find(layout.assets,a=>a.id==p.asset);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art.image);
                int x=p.x+baseline.terrainX-root.terrainOrigin.x,y=p.y+baseline.terrainY-root.terrainOrigin.y;
                var go=new GameObject("Reviewed market - "+p.asset);go.transform.SetParent(root.transform,false);
                go.transform.position=root.ground.CellToWorld(new Vector3Int(x,root.ground.cellBounds.yMax-y-p.h,0));
                go.AddComponent<SpriteRenderer>().sprite=sprite;go.GetComponent<SpriteRenderer>().sortingOrder=4;
            }
        }
        static void ApplyExitSigns(MapRoot root)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PocketCaravan/Art/Goryeo/Markets/sign.png");
            if(sprite==null||sprite.rect.size!=Vector2.one*16||sprite.pixelsPerUnit!=16)throw new InvalidDataException("Approved native sign missing");
            foreach(var at in new[]{new Vector2Int(28,-15),new Vector2Int(7,-4),new Vector2Int(7,4)}){
                var go=new GameObject("Reviewed whole exit sign");go.transform.SetParent(root.transform,false);
                go.transform.position=root.ground.CellToWorld(new Vector3Int(at.x-root.terrainOrigin.x,root.ground.cellBounds.yMax-1-at.y+root.terrainOrigin.y,0));
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=5;
            }
        }
        [Serializable]class OldMap{public int terrainX,terrainY;}
        [MenuItem("Pocket Caravan/Tests/T3 경계 검토 직접 걷기")]
        public static void Explore()=>Run(false);
        [MenuItem("Pocket Caravan/Tests/T3 경계 검토 자동 검사·캡처")]
        public static void Capture()=>Run(true);
        public static void AuditForeign(){SessionState.SetBool(Key+".foreign",true);Run(false);}
        static void Run(bool capture)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+".capture",capture);A7dUIReview.SetSize(960,540);
            EditorSceneManager.OpenScene(Root+"/Scenes/Naru.unity");Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
    }
}
