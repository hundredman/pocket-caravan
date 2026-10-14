using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 씬이 바뀌어도 남는 게임 상태(플레이어, 시계, 장터, 발길)를 들고 있다.
    /// 첫 씬이 열리기 전에 자동으로 만들어지므로 씬에 따로 넣지 않는다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GameDatabase Db { get; private set; }
        public PlayerState Player { get; private set; }
        public GameClock Clock { get; private set; }

        readonly Dictionary<string, MarketState> markets = new Dictionary<string, MarketState>();
        readonly Dictionary<string, Dictionary<Vector3Int, float>> footfall = new Dictionary<string, Dictionary<Vector3Int, float>>();

        string pendingSpawnId;
        bool firstSceneLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Db = Resources.Load<GameDatabase>("GameDatabase");
            if (Db == null)
            {
                Debug.LogWarning("[PocketCaravan] Resources/GameDatabase가 없습니다. 메뉴 Pocket Caravan > 시작 세트 만들기를 먼저 실행하세요.");
                Db = ScriptableObject.CreateInstance<GameDatabase>();
            }

            Player = new PlayerState
            {
                money = Db.startMoney,
                vehicle = Db.startVehicle,
                origin = Db.startOrigin,
                credit = Db.startOrigin != null ? Db.startOrigin.startingCredit : 0,
            };

            Clock = new GameClock { realSecondsPerDay = Db.realSecondsPerDay };
            Clock.NewHour += OnNewHour;
            Clock.NewDay += OnNewDay;

            gameObject.AddComponent<DebugHUD>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Update()
        {
            if (!UIState.Blocking) Clock.Tick(Time.deltaTime);
        }

        // ---------- 맵 이동 ----------

        public void TravelTo(string sceneName, string spawnId)
        {
            pendingSpawnId = spawnId;
            SceneManager.LoadScene(sceneName);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string id = pendingSpawnId;
            if (id == null && !firstSceneLoaded) id = SpawnPoint.StartId;
            firstSceneLoaded = true;
            pendingSpawnId = null;
            if (id == null) return;

            var spawn = SpawnPoint.Find(id);
            var player = FindAnyObjectByType<PlayerController>();
            if (spawn != null && player != null)
                player.transform.position = spawn.transform.position;
        }

        // ---------- 장터 ----------

        public MarketState GetMarket(TownData town)
        {
            if (town == null) return null;
            if (!markets.TryGetValue(town.id, out var m))
            {
                m = new MarketState(town, Db);
                markets[town.id] = m;
            }
            return m;
        }

        // ---------- 발길 ----------

        /// <summary>맵별 발길 값. 처음 요청이면 fresh=true (맵에 그려 둔 초기 길로 채워야 함).</summary>
        public Dictionary<Vector3Int, float> GetFootfall(string mapId, out bool fresh)
        {
            fresh = !footfall.TryGetValue(mapId, out var d);
            if (fresh)
            {
                d = new Dictionary<Vector3Int, float>();
                footfall[mapId] = d;
            }
            return d;
        }

        // ---------- 시간 ----------

        void OnNewHour(int hour)
        {
            foreach (var m in markets.Values) m.RecoverOneHour();
        }

        void OnNewDay(int day)
        {
            // 발길은 하루마다 조금씩 줄어든다. 덜 다닌 가장자리부터 등급이 내려간다.
            float keep = 1f - Db.roads.decayPerDay;
            var remove = new List<Vector3Int>();
            foreach (var map in footfall.Values)
            {
                remove.Clear();
                var keys = new List<Vector3Int>(map.Keys);
                foreach (var k in keys)
                {
                    float f = map[k] * keep;
                    if (f < 0.5f) remove.Add(k); else map[k] = f;
                }
                foreach (var k in remove) map.Remove(k);
            }
        }
    }
}
