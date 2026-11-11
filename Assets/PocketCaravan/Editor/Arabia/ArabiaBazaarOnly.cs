using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>
    /// 만국 장터(arabia_central_bazaar) 씬 하나만 JSON에서 다시 만든다 (2026-11-11 기우).
    /// "2. 아라비아 씬만 생성"은 아라비아 씬 13개를 모두 다시 써서, 장터만 고쳤을 때도 다른 씬 파일이 바뀐 것으로 잡혔다.
    /// 순서: Tools/MapGen/arabia_export.py --only arabia_central_bazaar 로 JSON을 만든 뒤 이 메뉴.
    /// 빌드 목록은 그대로 둔다. WorldIndex.asset은 공용 생성기가 다시 쓰므로, 바뀌었으면 올리기 전에 확인한다.
    /// </summary>
    public static class ArabiaBazaarOnly
    {
        const string MapId = "arabia_central_bazaar";

        [MenuItem("Pocket Caravan/아라비아/2b. 만국 장터 씬만 다시 만들기", priority = 201)]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[아라비아] Play를 끄고 실행하세요."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var buildBefore = EditorBuildSettings.scenes.Select(s => new EditorBuildSettingsScene(s.path, s.enabled)).ToArray();

            AssetDatabase.Refresh();   // 맵 생성기가 새로 쓴 타일 그림을 먼저 가져온다
            ArabiaSceneDressing.EnsureCharacterImport();
            MapJsonBuilder.BuildSelected(MapId);
            string path = WorldRegistry.Load().Map(MapId).scenePath;
            ArabiaSceneDressing.Apply(path);

            EditorBuildSettings.scenes = buildBefore;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(path);
            Debug.Log("[아라비아] 만국 장터 씬만 다시 만들었습니다: " + path);
        }
    }
}
