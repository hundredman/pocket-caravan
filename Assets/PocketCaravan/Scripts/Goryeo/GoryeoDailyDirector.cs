using UnityEngine;

namespace PocketCaravan
{
    /// <summary>Own-map daily activity binding. Never rewrites scenes or installs in previews.</summary>
    public sealed class GoryeoDailyDirector : MonoBehaviour
    {
        MapRoot applied;
        GoryeoDailyActivity.Site config;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<GoryeoDailyDirector>() != null) return;
            var go = new GameObject("Goryeo daily activities"); DontDestroyOnLoad(go); go.AddComponent<GoryeoDailyDirector>();
        }
        void Update()
        {
            var map = MapRoot.Current; var pc = PlayerController.Instance; var gm = GameManager.I;
            if (map == null || map == applied || map.previewOnly || map.mapId != "Naru" || gm == null || gm.IsTravelling
                || pc == null || pc.gameObject.scene != map.gameObject.scene) return;
            if (config == null)
            {
                var text = Resources.Load<TextAsset>("Goryeo/Daily/Config");
                if (text == null) return;
                config = JsonUtility.FromJson<GoryeoDailyActivity.Site>(text.text);
            }
            if (GoryeoDailyActivity.InstallApproved(config) != null) applied = map;
        }
    }
}
