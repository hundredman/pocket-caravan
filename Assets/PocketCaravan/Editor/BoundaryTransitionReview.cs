using System;using System.IO;using System.Collections.Generic;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;
namespace PocketCaravan.EditorTools
{
    [InitializeOnLoad] public static class BoundaryTransitionReview
    {
        const string Key="Codex.BoundaryTransitionReview";
        public const string Output="ArtReview/TransitionBoundary-20261030";
        [Serializable] public class CheckRow{public bool passed;public string test,detail;}
        [Serializable] public class Report{public bool completed;public string error;public List<CheckRow> checks=new List<CheckRow>();}
        static Report report;
        static BoundaryTransitionReview(){EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
            report=new Report();BoundaryTransitionPlayReview.Output=Output;
            BoundaryTransitionPlayReview.Record=(ok,test,detail)=>{report.checks.Add(new CheckRow{passed=ok,test=test,detail=detail});if(!ok||report.checks.Count%100==0)Save();if(!ok)throw new InvalidOperationException(test+": "+detail);};
            BoundaryTransitionPlayReview.Complete=e=>{report.completed=e==null;report.error=e==null?null:e.ToString();Save();SessionState.SetBool(Key,false);Time.timeScale=1;EditorApplication.isPlaying=false;};
            var go=new GameObject("Boundary timing QA - temporary");UnityEngine.Object.DontDestroyOnLoad(go);go.AddComponent<BoundaryTransitionPlayReview>();};}
        static void Save(){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/checks.json",JsonUtility.ToJson(report,true));}
        public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");A7dUIReview.SetSize(480,270);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/PocketCaravan/Editor/B2Preview/A7d/Scenes/Naru.unity");EditorApplication.isPlaying=true;}
    }
}
