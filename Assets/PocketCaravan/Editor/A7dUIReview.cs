using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    [InitializeOnLoad] public static class A7dUIReview
    {
        const string Active="Codex.A7dUIReview";
        static A7dUIReview()
        {
            EditorApplication.playModeStateChanged+=s=>
            {
                if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Active,false))return;
                A7dUIPlayRunner.Output="ArtReview/A7d-20261030/UI/"+SessionState.GetString(Active+".stage","Before");
                A7dUIPlayRunner.Resize=SetSize;
                A7dUIPlayRunner.Complete=error=>{Directory.CreateDirectory(A7dUIPlayRunner.Output);File.WriteAllText(A7dUIPlayRunner.Output+"/result.txt",error==null?"Completed; previewOnly session; autosave disabled":error.ToString());SessionState.SetBool(Active,false);Time.timeScale=1;EditorApplication.isPlaying=false;};
                UnityEngine.Object.DontDestroyOnLoad(new GameObject("A7d UI review").AddComponent<A7dUIPlayRunner>().gameObject);
            };
        }
        public static void Run(string stage)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
            SessionState.SetString(Active+".stage",stage);SessionState.SetBool(Active,true);SetSize(480,270);
            EditorSceneManager.OpenScene(NatureIntegrationReview.Root+"/Scenes/Naru.unity");EditorApplication.isPlaying=true;
        }
        public static void SetSize(int w,int h)
        {
            var a=typeof(Editor).Assembly;var type=a.GetType("UnityEditor.GameViewSizes");
            var sizes=typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null,null);
            var group=type.GetMethod("GetGroup").Invoke(sizes,new object[]{0});
            var size=Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"),1),w,h,"A7d "+w+"x"+h});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            var view=EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(view,count-1,null);view.Focus();view.Repaint();
        }
    }
}
