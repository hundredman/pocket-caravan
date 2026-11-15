using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 11/15 은샘: 도적을 중앙 대륙 네 방향에 고르게 둔다 (방향마다 2곳). 도적이 없던 동쪽(1곳)·남쪽(0곳)에만 더한다.
    /// 북쪽·서쪽은 지역 담당이 정한 자리 그대로 둔다 (베네치아 본토: 직행로는 빠르지만 밤 도적, 습지·고갯길은 안전 — 길 고르기 설계).
    /// 씬 파일은 고치지 않고, 맵이 열릴 때 도적 출몰 자리를 더한다.
    /// 그림은 Resources/Bandit (고려·중앙 동쪽은 C1 도적, 다른 곳은 예전 도적 그림).
    /// 쫓기·잡히면 통행료·한동안 조용함은 기존 Bandit·BanditSpot 그대로다.
    /// </summary>
    public sealed class BanditPlacement : MonoBehaviour
    {
        struct Spot { public string map, art; public Vector2 at; }

        /// <summary>새로 두는 자리 (맵 가운데 쪽, 출구에서 10칸 넘게 떨어진 길)</summary>
        static readonly Spot[] Added =
        {
            new Spot { map = "transit_south_inland", at = new Vector2(41.5f, 19.5f), art = "Bandit_east" },   // 동쪽 (솔재 고개는 원래 있음)
            new Spot { map = "arabia_central_canyon", at = new Vector2(76.5f, 24.5f), art = "Bandit_north" },  // 남쪽
            new Spot { map = "arabia_central_steppe", at = new Vector2(56.5f, 28.5f), art = "Bandit_north" },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<BanditPlacement>() != null) return;
            var go = new GameObject("BanditPlacement");
            DontDestroyOnLoad(go);
            go.AddComponent<BanditPlacement>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnLoaded;

        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            MapRoot root = null;
            foreach (var go in scene.GetRootGameObjects()) { root = go.GetComponentInChildren<MapRoot>(true); if (root != null) break; }
            if (root == null) return;
            string map = root.mapId;

            foreach (var spot in Added)
            {
                if (spot.map != map) continue;
                foreach (var go in scene.GetRootGameObjects())
                    if (go.GetComponentInChildren<BanditSpot>(true) != null) return;   // 씬에 이미 있으면 그대로
                var prefab = Resources.Load<GameObject>("Bandit/" + spot.art);
                if (prefab == null) return;
                var parent = root.transform;
                var pos = new Vector3(spot.at.x, spot.at.y, 0.5f);
                var bandit = Instantiate(prefab, pos, Quaternion.identity, parent);
                bandit.name = "도적";
                bandit.SetActive(false);
                var holder = new GameObject("도적 출몰 자리");
                holder.transform.SetParent(parent, false);
                holder.transform.position = pos;
                var bs = holder.AddComponent<BanditSpot>();
                bs.prefabAnimator = bandit.GetComponent<CharacterAnimator>();
            }
        }
    }
}
