using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 11/11 은샘: 지도 마을 카드(MapTownCard)에 쓸 맵별 놀거리 목록을 만든다.
    /// 놀거리 자리는 지역 코드가 맵에 들어갈 때 만들기 때문에, 플레이 중에 빌드 목록의 맵을 하나씩 열어 실제 자리를 센다.
    /// 쓰는 법: 시작 맵(Naru)을 Play → 메뉴 "Pocket Caravan/지도/놀거리 목록 만들기". 끝나면 Resources/MapActivities.json을 쓰고 Play를 멈춘다.
    /// 맵에 놀거리를 새로 넣었으면 다시 돌린다.
    /// </summary>
    public static class MapActivityScan
    {
        const string Output = "Assets/PocketCaravan/Resources/MapActivities.json";
        static readonly List<string> scenes = new List<string>();
        static readonly MapTownCard.ListFile result = new MapTownCard.ListFile();
        static int index;
        static double waitUntil;
        static bool loading;

        [MenuItem("Pocket Caravan/지도/놀거리 목록 만들기")]
        static void Run()
        {
            if (!EditorApplication.isPlaying) { EditorUtility.DisplayDialog("놀거리 목록", "시작 맵을 Play한 뒤 다시 누르세요.", "확인"); return; }
            scenes.Clear(); result.maps.Clear(); index = 0; loading = false;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.enabled || !s.path.StartsWith("Assets/PocketCaravan/Scenes/")) continue;
                if (s.path.Contains("/ModuleLab/") || s.path.Contains("/ObstacleLab/")) continue;
                scenes.Add(Path.GetFileNameWithoutExtension(s.path));
            }
            EditorApplication.update -= Step;
            EditorApplication.update += Step;
        }

        static void Step()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Step; return; }
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            if (loading)
            {
                // 다 열렸다: 지금 맵의 놀거리를 센다
                loading = false;
                string map = MapRoot.Current != null ? MapRoot.Current.mapId : scenes[index];
                var names = new List<string>();
                foreach (var it in Interactable.All)
                    if (it != null && it.isActiveAndEnabled && MapTownCard.Listed(it, out var n) && !names.Contains(n)) names.Add(n);
                if (names.Count > 0) result.maps.Add(new MapTownCard.Entry { map = map, names = names });
                Debug.Log($"[놀거리 목록] {map}: {string.Join(", ", names)}");
                index++;
            }
            if (index >= scenes.Count)
            {
                EditorApplication.update -= Step;
                result.maps.Sort((a, b) => string.CompareOrdinal(a.map, b.map));
                File.WriteAllText(Output, JsonUtility.ToJson(result, true) + "\n");
                Debug.Log($"[놀거리 목록] {result.maps.Count}개 맵을 {Output}에 썼습니다.");
                EditorApplication.isPlaying = false;
                AssetDatabase.Refresh();
                return;
            }
            UIState.Reset();
            SceneManager.LoadScene(scenes[index]);
            loading = true;
            waitUntil = EditorApplication.timeSinceStartup + 1.5;
        }
    }
}
