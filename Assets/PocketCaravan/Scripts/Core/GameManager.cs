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
        public QuestManager Quests { get; private set; }

        readonly Dictionary<string, MarketState> markets = new Dictionary<string, MarketState>();

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

            Quests = new QuestManager(Db.quests);

            gameObject.AddComponent<DebugHUD>();
            gameObject.AddComponent<QuestTracker>();
            gameObject.AddComponent<DialogueUI>();
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
            UIState.Blocking = false;

            if (id != null)
            {
                var spawn = SpawnPoint.Find(id);
                var player = FindAnyObjectByType<PlayerController>();
                if (spawn != null && player != null)
                    player.transform.position = spawn.transform.position;
            }

            if (MapRoot.Current != null)
            {
                Notify.Show(MapRoot.Current.displayName, 2.5f);
                GameEvents.RaiseMapEntered(MapRoot.Current.mapId);
            }
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

        // ---------- 시간 ----------

        void OnNewHour(int hour)
        {
            foreach (var m in markets.Values) m.RecoverOneHour();
        }

    }
}
