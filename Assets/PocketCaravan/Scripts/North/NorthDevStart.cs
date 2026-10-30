using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 개발용: 에디터에서 북유럽 씬(이름이 north_로 시작)을 열고 바로 Play하면 출신을 바이킹으로 바꾼다.
    /// 주인공 그림(순록)은 이제 출신 데이터(playerFrames)를 따라 PlayerAppearance가 입힌다.
    /// 공용 빌더가 씬을 만들기 때문에 씬에 붙이지 않고 저절로 실행된다. 빌드·정식 시작 맵에는 영향 없음.
    /// 출신 고르기가 생기면 지운다.
    /// </summary>
    public static class NorthDevStart
    {
#if UNITY_EDITOR
        const string OriginPath = "Assets/PocketCaravan/Data/Origins/Origin_Viking.asset";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex == 0 || !scene.name.StartsWith("north_")) return;
            var gm = GameManager.I;
            var origin = UnityEditor.AssetDatabase.LoadAssetAtPath<OriginData>(OriginPath);
            if (gm == null || origin == null || gm.Player.origin == origin) return;
            gm.Player.origin = origin;
            gm.Player.credit = origin.startingCredit;
            Debug.Log($"[북유럽 테스트] 출신을 {origin.displayName}(으)로 바꿨습니다. 신용 {origin.startingCredit}");
        }
#endif
    }
}
