using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// E3 길 위 사건 일정. 맵 JSON의 장애 배치 중 id가 "e3_"로 시작하는 것만 게임 시간으로 켜고 끈다.
    /// 고려 맵에는 e3_ 배치를 두지 않으므로 고려 안에서는 생기지 않는다.
    /// 통나무(fallen_log): 8시간 창마다 확률로 쓰러져 있다. 치우면 그 방문 동안 사라진다(치우기 3초, 비용 없음).
    /// 강 범람(river_flood): 범람한 날에만 켜진다. 켜진 동안 물이 불었다 빠지기를 반복하므로 기다리거나 돌아간다.
    /// 산적은 기존 BanditSpot을 그대로 쓴다. 결과는 맵·시간 창으로 정해져 다시 들어와도 같다.
    /// </summary>
    public static class RoadHazardSchedule
    {
        public const string Prefix = "e3_";
        public const float LogWindowHours = 8f;
        public const float LogChance = .5f;
        public const float FloodDayChance = .4f;

        public static bool Managed(string placementId) => placementId != null && placementId.StartsWith(Prefix, System.StringComparison.Ordinal);

        /// <summary>이 배치를 지금 켤지. 모르는 종류는 그대로 둔다(null).</summary>
        public static bool? Enabled(string mapId, string placementId, string definitionId, float totalHours)
        {
            if (!Managed(placementId)) return null;
            if (definitionId == "fallen_log")
                return Roll(mapId + "|" + placementId + "|log|" + Mathf.FloorToInt(totalHours / LogWindowHours)) < LogChance;
            if (definitionId == "river_flood")
                return Roll(mapId + "|" + placementId + "|flood|" + Mathf.FloorToInt(totalHours / 24f)) < FloodDayChance;
            return null;
        }

        static float Roll(string key)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in key) { h ^= c; h *= 1099511628211UL; }
            return (h % 1000003UL) / 1000003f;
        }
    }

    /// <summary>씬이 열릴 때(ObstacleField.Start 전에) 일정에 따라 e3_ 배치를 켜고 끈다. 빌더·씬 수정 없이 동작한다.</summary>
    public static class RoadHazardDirector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Apply();
        }

        static void OnLoaded(Scene scene, LoadSceneMode mode) => Apply();

        public static void Apply()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Clock == null) return;
            foreach (var field in Object.FindObjectsByType<ObstacleField>(FindObjectsSortMode.None))
            {
                var map = field.GetComponent<MapRoot>();
                string mapId = map != null ? map.mapId : field.gameObject.scene.name;
                foreach (var p in field.placements)
                {
                    if (p == null) continue;
                    bool? on = RoadHazardSchedule.Enabled(mapId, p.id, p.definitionId, gm.Clock.TotalHours);
                    if (!on.HasValue) continue;
                    p.enabled = on.Value;              // Start 전이면 Rebuild가 이 값을 쓴다
                    field.SetEnabled(p.id, on.Value);  // 이미 만들어졌으면 바로 반영
                }
            }
        }
    }
}
