using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 씬이 바뀌어도 남는 게임 상태(플레이어, 시계, 장터, 의뢰, NPC 상인, 떨어진 짐)를 들고 있다.
    /// 첫 씬이 열리기 전에 자동으로 만들어지므로 씬에 따로 넣지 않는다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GameDatabase Db { get; private set; }
        public PlayerState Player { get; private set; }
        public GameClock Clock { get; private set; }
        public QuestManager Quests { get; private set; }
        public ContractManager Contracts { get; private set; }
        public NpcTraders Traders { get; private set; }
        public readonly List<DropRecord> Drops = new List<DropRecord>();

        readonly Dictionary<string, MarketState> markets = new Dictionary<string, MarketState>();
        readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>();   // "bandit:맵", "event:맵" → 다시 가능한 시각

        string pendingSpawnId;
        float pendingOffset;
        Vector2Int pendingFacing;
        Vector3? pendingExact;
        float fade;   // 0 = 보임, 1 = 검은 화면

        /// <summary>맵을 넘어가는 중 (화면이 어두워졌다 밝아지는 동안 조작을 막음)</summary>
        public bool IsTravelling { get; private set; }
        bool firstSceneLoaded;
        SaveData saveToLoad;
        bool autosave = true;

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
            Contracts = new ContractManager(Db, Clock, Player);
            Contracts.InitBoards();
            Traders = new NpcTraders(Db, Clock, GetMarket);
            Traders.Init();

            GameEvents.TownLeveled += (town, level) =>
            {
                Notify.Show($"{Josa.Ga(town.displayName)} {Josa.Ro(GetMarket(town).LevelName)} 커졌다! 거래가 늘어 값이 덜 흔들린다.", 5f);
                Sfx.Play("levelup");
            };

            gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<HUD>();
            gameObject.AddComponent<QuestTracker>();
            gameObject.AddComponent<DialogueUI>();
            gameObject.AddComponent<WorldLabels>();
            gameObject.AddComponent<AmbientFX>();
            gameObject.AddComponent<WorldMapUI>();
            gameObject.AddComponent<PauseMenu>();
            SceneManager.sceneLoaded += OnSceneLoaded;

            saveToLoad = SaveSystem.Read();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (I == this) I = null;
        }

        void OnApplicationQuit() => SaveNow();

        UnityEngine.Rendering.Universal.Light2D sun;
        static readonly Color DayColor = new Color(1f, 0.98f, 0.93f);
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
            StartCoroutine(Travel(sceneName, spawnId, offset, facing, null));
        }

        IEnumerator Travel(string sceneName, string spawnId, float offset, Vector2Int facing, Vector3? exact)
        {
            IsTravelling = true;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.12f) { fade = t; yield return null; }
            fade = 1f;
            pendingSpawnId = spawnId; pendingOffset = offset; pendingFacing = facing; pendingExact = exact;
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null;
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.18f) { fade = t; yield return null; }
            fade = 0f;
            IsTravelling = false;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool first = !firstSceneLoaded;
            firstSceneLoaded = true;
            UIState.Blocking = false;

            // 처음 연 씬이 시작 맵이면 저장을 불러온다. 다른 씬에서 바로 Play한 경우(개발 중)는 불러오지도, 덮어쓰지도 않는다.
            if (first && saveToLoad != null)
            {
                if (scene.buildIndex == 0)
                {
                    var d = saveToLoad;
                    saveToLoad = null;
                    Apply(d);
                    if (!string.IsNullOrEmpty(d.scene) && d.scene != scene.name)
                    {
                        StartCoroutine(Travel(d.scene, null, 0f, new Vector2Int(d.fx, d.fy), new Vector3(d.px, d.py, 0f)));
                        return;
                    }
                    pendingExact = new Vector3(d.px, d.py, 0f);
                    pendingFacing = new Vector2Int(d.fx, d.fy);
                    Notify.Show("이어서 한다 (Esc 메뉴에서 처음부터 시작 가능)", 4f);
                }
                else
                {
                    autosave = false;
                    saveToLoad = null;
                    Debug.Log("[저장] 시작 맵이 아닌 씬에서 Play: 저장을 불러오지 않고 자동 저장도 끕니다.");
                }
            }

            string id = pendingSpawnId;
            if (id == null && first && pendingExact == null) id = SpawnPoint.StartId;
            pendingSpawnId = null;

            var player = FindAnyObjectByType<PlayerMotor>();
            var pc = player != null ? player.GetComponent<PlayerController>() : null;
            if (pendingExact.HasValue && pc != null)
            {
                player.Teleport(pendingExact.Value, pendingFacing);
            }
            else if (id != null && player != null)
            {
                var spawn = SpawnPoint.Find(id);
                if (spawn != null) player.Teleport(spawn.PositionFor(pendingOffset), pendingFacing);
            }
            var cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.SnapNow();
            bool loadedFromSave = pendingExact.HasValue;
            pendingOffset = 0f; pendingFacing = default; pendingExact = null;

            if (MapRoot.Current != null)
            {
                var map = MapRoot.Current;
                if (map.travelHours > 0 && !loadedFromSave)
                {
                    Clock.Advance(map.travelHours);
                    Notify.Show($"{map.displayName} · 걸어서 {map.travelHours}시간 길 ({Clock.Label})", 3f);
                }
                else Notify.Show(map.displayName, 2.5f);
                GameEvents.RaiseMapEntered(map.mapId);
                sun = FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();
                SpawnDrops(map.mapId);
                AudioManager.OnMapEntered(map.mapId);
                SaveNow();
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

        public ItemData ItemById(string id) => Db.items.Find(i => i != null && i.id == id);

        // ---------- 시간 ----------

        void OnNewHour(int hour)
        {
            foreach (var t in Db.towns) if (t != null) GetMarket(t);   // NPC 상인도 거래하므로 모든 장터를 깨운다
            foreach (var m in markets.Values) m.RecoverOneHour();
            Contracts.OnHour();
            Traders.OnHour();
            if (hour == 6 && Clock.IsMarketDay) Notify.Show("오늘은 장날! 장터 값이 오른다.", 4f);
        }

        // ---------- 떨어진 짐 ----------

        /// <summary>흔들림이 가득 차면 짐 하나(물건 1개 또는 의뢰 짐)를 pos에 떨어뜨린다.</summary>
        public void DropRandomCargo(Vector3 pos)
        {
            var map = MapRoot.Current;
            if (map == null) return;
            if (!map.IsWalkable(map.WorldToCell(pos))) pos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : pos;
            var choices = new List<object>();
            foreach (var kv in Player.cargo.Items) if (!kv.Key.questItem) choices.Add(kv.Key);
            foreach (var c in Player.contracts) if (!c.dropped) choices.Add(c);
            if (choices.Count == 0) return;
            var pick = choices[Random.Range(0, choices.Count)];
            var r = new DropRecord { mapId = map.mapId, x = pos.x, y = pos.y };
            if (pick is ItemData item)
            {
                r.itemId = item.id; r.label = item.displayName; r.unitCost = Player.cargo.AverageCost(item);
                Player.cargo.Remove(item, 1);
            }
            else if (pick is Contract c)
            {
                c.dropped = true;
                c.condition = Mathf.Max(0f, c.condition - 0.2f);
                r.contractId = c.id; r.label = c.cargo;
            }
            Drops.Add(r);
            DroppedCargo.Spawn(r, IconFor(r));
            Sfx.Play("drop");
            Notify.Show($"짐이 흔들려 {Josa.Ga(r.label)} 떨어졌다! 다가가 Space로 줍자.", 4f);
        }

        Sprite IconFor(DropRecord r)
        {
            if (r.contractId == 0) { var it = ItemById(r.itemId); if (it != null && it.icon != null) return it.icon; }
            return Db.crateIcon;
        }

        void SpawnDrops(string mapId)
        {
            foreach (var r in Drops) if (r.mapId == mapId) DroppedCargo.Spawn(r, IconFor(r));
        }

        // ---------- 도적·돌발 사건 ----------

        public bool BanditReady(string map) => Ready("bandit:" + map);
        public void BanditMet(string map) => cooldowns["bandit:" + map] = Clock.TotalHours + 12f;
        public bool EventReady(string map) => Ready("event:" + map);
        public void EventHappened(string map) => cooldowns["event:" + map] = Clock.TotalHours + 8f;
        bool Ready(string key) => !cooldowns.TryGetValue(key, out var until) || Clock.TotalHours >= until;

        /// <summary>가장 비싼 짐 (물건은 한 묶음 기준가, 의뢰는 보수)</summary>
        object MostValuable(out string name)
        {
            object best = null; float bestValue = 0; name = null;
            foreach (var kv in Player.cargo.Items)
            {
                if (kv.Key.questItem) continue;
                float v = kv.Key.basePrice * Mathf.Min(kv.Value, 3);
                if (v > bestValue) { bestValue = v; best = kv.Key; name = kv.Key.displayName; }
            }
            foreach (var c in Player.contracts)
                if (!c.dropped && c.reward > bestValue) { bestValue = c.reward; best = c; name = c.cargo; }
            return best;
        }

        public string MostValuableCargoName() { MostValuable(out var n); return n; }

        public void StealMostValuable()
        {
            var best = MostValuable(out var name);
            if (best is ItemData item)
            {
                int n = Mathf.Min(Player.cargo.Count(item), 3);
                Player.cargo.Remove(item, n);
                Notify.Show($"도적이 {item.displayName} {n}개를 빼앗아 갔다.", 4f);
            }
            else if (best is Contract c) Contracts.Lose(c);
            Sfx.Play("fail");
        }

        // ---------- 저장 ----------

        public void SaveNow()
        {
            if (!autosave || MapRoot.Current == null) return;
            var pc = PlayerController.Instance;
            var d = new SaveData
            {
                scene = SceneManager.GetActiveScene().name,
                px = pc != null ? pc.transform.position.x : 0, py = pc != null ? pc.transform.position.y : 0,
                fx = pc != null ? pc.Motor.Facing.x : 0, fy = pc != null ? pc.Motor.Facing.y : -1,
                money = Player.money, credit = Player.credit,
                vehicle = Player.vehicle != null ? Player.vehicle.id : null,
                stamina = Player.stamina,
                day = Clock.day, minute = Clock.minuteOfDay,
                nextContractId = Contracts.nextId,
            };
            foreach (var kv in Player.cargo.Items) { d.cargoItems.Add(kv.Key.id); d.cargoCounts.Add(kv.Value); d.cargoCosts.Add(Player.cargo.TotalCost(kv.Key)); }
            d.contracts.AddRange(Player.contracts);
            Quests.Export(d);
            foreach (var m in markets.Values) d.markets.Add(m.Export());
            d.boards.AddRange(Contracts.boards);
            d.traders.AddRange(Traders.traders);
            d.drops.AddRange(Drops);
            foreach (var kv in cooldowns) { d.cooldownKeys.Add(kv.Key); d.cooldownUntil.Add(kv.Value); }
            SaveSystem.Write(d);
        }

        void Apply(SaveData d)
        {
            Player.money = d.money;
            Player.credit = d.credit;
            Player.vehicle = Db.vehicles.Find(v => v != null && v.id == d.vehicle) ?? Db.startVehicle;
            Player.stamina = d.stamina;
            Player.cargo.Clear();
            for (int i = 0; i < d.cargoItems.Count; i++)
            {
                var it = ItemById(d.cargoItems[i]);
                if (it != null && d.cargoCounts[i] > 0) Player.cargo.Add(it, d.cargoCounts[i], d.cargoCosts[i] / d.cargoCounts[i]);
            }
            Player.contracts.Clear();
            Player.contracts.AddRange(d.contracts);
            Clock.day = Mathf.Max(1, d.day);
            Clock.minuteOfDay = d.minute;
            Quests.Import(d);
            markets.Clear();
            foreach (var s in d.markets)
            {
                var t = Db.towns.Find(x => x != null && x.id == s.town);
                if (t != null) GetMarket(t).Import(s, ItemById);
            }
            if (d.boards.Count > 0) Contracts.boards = new List<ContractManager.Board>(d.boards);
            Contracts.InitBoards();
            Contracts.nextId = Mathf.Max(Contracts.nextId, d.nextContractId);
            if (d.traders.Count > 0) Traders.traders = new List<NpcTraders.Trader>(d.traders);
            Drops.Clear(); Drops.AddRange(d.drops);
            cooldowns.Clear();
            for (int i = 0; i < d.cooldownKeys.Count && i < d.cooldownUntil.Count; i++) cooldowns[d.cooldownKeys[i]] = d.cooldownUntil[i];
        }

        /// <summary>저장을 지우고 처음부터</summary>
        public static void NewGame()
        {
            SaveSystem.Delete();
            var old = I;
            I = null;
            if (old != null) { old.autosave = false; Destroy(old.gameObject); }
            GameEvents.ClearAll();
            UIState.Blocking = false;
            Boot();
            SceneManager.LoadScene(0);
        }
    }
}
