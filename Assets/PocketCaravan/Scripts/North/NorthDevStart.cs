using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 개발용: 에디터에서 북유럽 씬(이름이 north_로 시작)을 열고 바로 Play하면 출신을 바이킹으로 바꾸고,
    /// 주인공 그림을 북유럽 순록(Art/Characters/north_reindeer_tall_*)으로 바꾼다.
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
            // 맵마다 플레이어가 새로 생기므로, 북유럽 맵에 들어올 때마다 그림을 바꾼다
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SwapPlayerSprite("north_reindeer");
            var gm = GameManager.I;
            var origin = UnityEditor.AssetDatabase.LoadAssetAtPath<OriginData>(OriginPath);
            if (gm == null || origin == null || gm.Player.origin == origin) return;
            gm.Player.origin = origin;
            gm.Player.credit = origin.startingCredit;
            Debug.Log($"[북유럽 테스트] 출신을 {origin.displayName}(으)로 바꿨습니다. 신용 {origin.startingCredit}");
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (s.name.StartsWith("north_")) SwapPlayerSprite("north_reindeer");
        }

        static void SwapPlayerSprite(string character)
        {
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc == null || pc.anim == null) return;
            var frames = new Sprite[16];
            const string dirs = "dulr";
            for (int d = 0; d < 4; d++)
                for (int f = 0; f < 4; f++)
                    frames[d * 4 + f] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/PocketCaravan/Art/Characters/{character}_tall_{dirs[d]}{f}.png");
            if (frames[0] == null) { Debug.LogWarning($"[북유럽 테스트] 캐릭터 그림 없음: {character} (Tools/ArtGen/north_chars.py)"); return; }
            pc.anim.frames = frames;
            var sr = pc.anim.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = frames[0];
        }
#endif
    }
}
