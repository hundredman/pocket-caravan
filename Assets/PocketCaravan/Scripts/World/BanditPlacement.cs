using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 11/15 은샘: 도적이 없던 중앙 대륙 남쪽(아라비아 쪽)에 1곳을 더한다. 동쪽은 솔바람 고개 1곳 그대로.
    /// 외길 맵(들어가는 곳·나가는 곳 하나씩)과 대륙 한가운데(모두 지나는 곳)는 피한다.
    /// 북쪽·서쪽은 지역 담당이 정한 자리 그대로 둔다 (베네치아 본토: 직행로는 빠르지만 밤 도적, 습지·고갯길은 안전 — 길 고르기 설계).
    /// 씬 파일은 고치지 않고, 맵이 열릴 때 도적 출몰 자리를 더한다.
    /// 도적 몸은 Resources/Bandit 프리팹, 모습은 깨어날 때 BanditLook이 사람 도적 세 가지 중 하나로 바꾼다.
    /// 쫓기·잡히면 통행료·한동안 조용함은 기존 Bandit·BanditSpot 그대로다.
    /// </summary>
    public sealed class BanditPlacement : MonoBehaviour
    {
        struct Spot { public string map, art; public Vector2 at; }

        /// <summary>새로 두는 자리 (맵 가운데 쪽, 출구에서 10칸 넘게 떨어진 길)</summary>
        static readonly Spot[] Added =
        {
            new Spot { map = "arabia_central_canyon", at = new Vector2(76.5f, 24.5f), art = "Bandit_north" },  // 남쪽 붉은 바위 협곡 (자비드에서 대륙으로 가는 길, 오아시스로 돌아갈 수 있음)
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
