using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>Only the 13 approved maps. Preserve foreign index entries and the existing build list.</summary>
    [InitializeOnLoad] public static class T3Production
    {
        [Serializable]class Layout{public Row[] maps;}
        [Serializable]class Row{public string id;}
        [Serializable]class FileMap{public string id,name;public int h,legacyHeight,boundaryLayoutVersion;public bool previewOnly;public Obj[] objects;}
        [Serializable]class Obj{public string kind,id,npc,town,target;}
        static T3Production(){EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Codex.T3Production",false))return;
            SessionState.SetBool("Codex.T3Production",false);var runner=UnityEngine.Object.FindFirstObjectByType<T3BoundaryExplorer>()??new GameObject("T3 production audit").AddComponent<T3BoundaryExplorer>();runner.production=true;runner.capture=true;runner.routesOnly=SessionState.GetBool("Codex.T3Production.routes",false);
        };}
        public static void Review(bool routes=false){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            SessionState.SetBool("Codex.T3Production",true);SessionState.SetBool("Codex.T3Production.routes",routes);A7dUIReview.SetSize(960,540);
            EditorSceneManager.OpenScene(T3BoundaryReview.Root+"/Scenes/Naru.unity");Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
        public static void Build()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Stop Play/compile first");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene first");
            var ids=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/PocketCaravan/Resources/Goryeo/BoundaryLayout.json")).maps.Select(m=>m.id).ToArray();
            var registry=WorldRegistry.Load();var setup=EditorSceneManager.GetSceneManagerSetup();
            var files=new System.Collections.Generic.List<FileMap>();
            if(ids.Length!=13)throw new InvalidDataException("Missing production inputs");
            try{foreach(var id in ids){
                var entry=registry.Map(id);if(entry==null)throw new InvalidDataException(id);
                var json=File.ReadAllText(entry.jsonPath);var f=JsonUtility.FromJson<FileMap>(json);
                if(f.previewOnly||f.boundaryLayoutVersion!=3)throw new InvalidDataException("Not approved production: "+id);
                MapJsonBuilder.BuildSingleMap(json,entry.scenePath);
                var root=UnityEngine.Object.FindFirstObjectByType<MapRoot>();
                if(root.previewOnly||root.boundaryLayoutVersion!=3||root.transform.Find("CameraBackdrop")!=null)throw new InvalidDataException("Wrong boundary scene "+id);
                files.Add(f);
            }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            var index=AssetDatabase.LoadAssetAtPath<WorldIndex>("Assets/PocketCaravan/Resources/WorldIndex.asset");
            if(index==null)throw new InvalidDataException("Missing WorldIndex");
            foreach(var f in files){var id=f.id;
                var info=index.maps.Find(m=>m.mapId==id);if(info==null)throw new InvalidDataException("Index entry missing "+id);
                info.height=f.h;info.legacyHeight=f.legacyHeight;info.displayName=f.name;info.town="";
                info.neighbors=f.objects.Where(o=>o.kind=="exit").Select(o=>o.target).Distinct().ToList();
                index.places.RemoveAll(p=>p.mapId==id);
                foreach(var o in f.objects){
                    if(o.kind=="market")info.town=o.town;
                    if(o.kind=="npc"||o.kind=="market"||o.kind=="board")index.places.Add(new WorldIndex.Place{kind=o.kind,id=o.kind=="npc"?o.npc:o.town,mapId=id});
                }
            }
            EditorUtility.SetDirty(index);AssetDatabase.SaveAssets();
            Debug.Log("T3 production: 13 scenes/index entries rebuilt; other regions/build settings preserved");
        }
    }
}
