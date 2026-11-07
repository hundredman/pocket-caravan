#if UNITY_EDITOR
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    /// <summary>Opening an isolated scene and pressing Play also uses safe preview routing.</summary>
    public sealed class T3BoundaryPreviewGate:MonoBehaviour
    {
        void Start()
        {
            var map=MapRoot.Current;
            if(map==null||!map.previewOnly||!gameObject.scene.path.StartsWith("Assets/PocketCaravan/Editor/B2Preview/T3Boundaries/Scenes/"))return;
            if(FindFirstObjectByType<T3BoundaryExplorer>()!=null)return;
            var runner=new GameObject("T3 boundary explorer").AddComponent<T3BoundaryExplorer>();
            if(UnityEditor.SessionState.GetBool("Codex.T3BoundaryReview",false)){
                runner.capture=UnityEditor.SessionState.GetBool("Codex.T3BoundaryReview.capture",false);
                runner.auditForeign=UnityEditor.SessionState.GetBool("Codex.T3BoundaryReview.foreign",false);
            }
        }
    }
}
#endif
