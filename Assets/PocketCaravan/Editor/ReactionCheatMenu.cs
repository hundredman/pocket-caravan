using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>2026-11-04 점검용: Play 중에 리액션을 전부 배우거나 처음 상태로 되돌린다 (빌드에는 들어가지 않음).</summary>
    public static class ReactionCheatMenu
    {
        [MenuItem("Pocket Caravan/통합/리액션 전부 배우기 (Play 중)")]
        static void LearnAll()
        {
            if (!EditorApplication.isPlaying || GameManager.I == null) { Debug.LogWarning("[리액션] Play 중에 눌러 주세요."); return; }
            int n = Reactions.TeachAll();
            Notify.Show($"리액션 {n}개를 새로 배웠다 (점검용). R로 열어 보세요.", 3f);
            Debug.Log($"[리액션] 전부 배움: 새로 {n}개");
        }

        [MenuItem("Pocket Caravan/통합/리액션 처음 상태로 (Play 중)")]
        static void Forget()
        {
            if (!EditorApplication.isPlaying || GameManager.I == null) { Debug.LogWarning("[리액션] Play 중에 눌러 주세요."); return; }
            Reactions.ForgetAll();
            Notify.Show("리액션을 처음 상태로 (인사·끄덕·박수만)", 3f);
        }
    }
}
