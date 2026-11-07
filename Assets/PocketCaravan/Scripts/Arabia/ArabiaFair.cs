using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 돌아가며 서는 큰 장 — 아라비아(남쪽 조각) 장식 (2026-11-07 기우).
    /// 어디서 언제 서는지·덤·알림·화면 띠는 공용 CentralFair. 여기는 자비드·알 아흐사·타이즈 장터 둘레에
    /// 향신료 자루·양탄자·대추야자·유향 좌판과 깃발 줄을 세우는 것만. 그림: Resources/Arabia/Fair (Art/Arabia/Sprites/v1/prop 복사).
    /// </summary>
    public class ArabiaFair : MonoBehaviour
    {
        Transform deco;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (FindFirstObjectByType<ArabiaFair>() != null) return;
            var go = new GameObject("ArabiaFair");
            DontDestroyOnLoad(go);
            go.AddComponent<ArabiaFair>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Decorate(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void OnLoaded(Scene s, LoadSceneMode m) => Decorate();

        void Decorate()
        {
            if (deco != null) Destroy(deco.gameObject);
            deco = null;
            var map = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            if (string.IsNullOrEmpty(map) || !map.StartsWith("arabia_") || !CentralFair.IsFairMap(map)) return;
            var market = FindAnyObjectByType<TownMarket>();
            if (market == null) return;
            deco = new GameObject("큰 장 장식").transform;
            var at = market.transform.position;
            void Put(string sprite, float dx, float dy, int order)
            {
                var sp = Resources.Load<Sprite>("Arabia/Fair/" + sprite);
                if (sp == null) { Debug.LogWarning("[아라비아 큰 장] 그림 없음: " + sprite); return; }
                var go = new GameObject(sprite);
                go.transform.SetParent(deco, false);
                go.transform.position = at + new Vector3(dx, dy, 0f);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = sp; r.sortingOrder = order;
            }
            // 소품은 24px·가운데 기준점이라 땅에 놓이게 0.75 올림. 장터 상인 양옆 두 칸씩 비워 전시실·행상 자리(±2, ±3)와 겹치지 않게 바깥쪽
            Put("prop_carpet",       -4.5f, 0.75f, 8);
            Put("prop_spiceSack",    -4.5f, 1.9f,  9);
            Put("prop_dates",         4.5f, 0.75f, 9);
            Put("prop_frankincense",  4.5f, 1.9f,  9);
            Put("prop_pepperSack",    0f,  -1.6f,  9);
            Put("bunting",           -1f,   2.6f, 21);
            Put("bunting",            1f,   2.6f, 21);
        }
    }
}
