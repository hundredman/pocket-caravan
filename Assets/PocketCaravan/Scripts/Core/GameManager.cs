using System.Collections;
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
        float pendingOffset;
        Vector2Int pendingFacing;
        float fade;   // 0 = 보임, 1 = 검은 화면

        /// <summary>맵을 넘어가는 중 (화면이 어두워졌다 밝아지는 동안 조작을 막음)</summary>
        public bool IsTravelling { get; private set; }
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
            gameObject.AddComponent<WorldLabels>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        UnityEngine.Rendering.Universal.Light2D sun;
        static readonly Color DayColor = Color.white;
        static readonly Color DuskColor = new Color(1f, 0.78f, 0.6f);
        static readonly Color NightColor = new Color(0.45f, 0.5f, 0.8f);

        void Update()
        {
            if (!UIState.Blocking) Clock.Tick(Time.deltaTime);
            ApplyDaylight();
        }

        /// <summary>낮·밤: 맵의 전역 2D 빛 색과 밝기를 시간에 맞춘다.</summary>
        void ApplyDaylight()
        {
            if (sun == null) return;
            float d = Clock.Daylight;
            Color c = d >= 1f ? DayColor : d > 0.5f ? Color.Lerp(DuskColor, DayColor, (d - 0.5f) * 2f) : Color.Lerp(NightColor, DuskColor, d * 2f);
            sun.color = c;
            sun.intensity = Mathf.Lerp(0.55f, 1f, d);
        }

        // ---------- 맵 이동 ----------

        public void TravelTo(string sceneName, string spawnId, float offset = 0f, Vector2Int facing = default)
        {
            if (IsTravelling) return;
            StartCoroutine(Travel(sceneName, spawnId, offset, facing));
        }

        IEnumerator Travel(string sceneName, string spawnId, float offset, Vector2Int facing)
        {
            IsTravelling = true;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.12f) { fade = t; yield return null; }
            fade = 1f;
            pendingSpawnId = spawnId; pendingOffset = offset; pendingFacing = facing;
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null;
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.18f) { fade = t; yield return null; }
            fade = 0f;
            IsTravelling = false;
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
                var player = FindAnyObjectByType<PlayerMotor>();
                if (spawn != null && player != null) player.Teleport(spawn.PositionFor(pendingOffset), pendingFacing);
                // 카메라도 바로 따라가게
                var cam = FindAnyObjectByType<CameraFollow>();
                if (cam != null) cam.SnapNow();
            }
            pendingOffset = 0f; pendingFacing = default;

            if (MapRoot.Current != null)
            {
                var map = MapRoot.Current;
                if (map.travelHours > 0)
                {
                    Clock.Advance(map.travelHours);
                    Notify.Show($"{map.displayName} · 걸어서 {map.travelHours}시간 길 ({Clock.Label})", 3f);
                }
                else Notify.Show(map.displayName, 2.5f);
                GameEvents.RaiseMapEntered(map.mapId);
                sun = FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();
            }
        }

        void OnGUI()
        {
            if (fade <= 0f) return;
            GUI.depth = -1000;
            GUI.color = new Color(0f, 0f, 0f, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
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
