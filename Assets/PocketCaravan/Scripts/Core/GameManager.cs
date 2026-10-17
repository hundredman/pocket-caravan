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
            gameObject.AddComponent<TradeLedgerUI>();
            gameObject.AddComponent<TownFacilitiesUI>();
            gameObject.AddComponent<CommerceGuideUI>();
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

        /// <summary>분리된 시스템 테스트 씬 전용. 본편 저장/튜토리얼/시간 진행을 연결하지 않는다.</summary>
        public void EnterTestSession()
        {
            autosave = false;
            saveToLoad = null;
            firstSceneLoaded = true;
            fade = 0f;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Player.cargo.Clear();
            Player.ledger.Clear();
            Player.contracts.Clear();
            Player.origin = null;
            Player.money = Player.credit = 0;
            Player.stamina = 1f;
            Player.shake = 0f;
            Drops.Clear();
            Quests = new QuestManager(new List<QuestData>());
            Clock.minuteOfDay = 12 * 60;
            // HUD·대화·메뉴·NPC 시뮬레이션은 테스트 씬 자체 UI로 대체한다.
            foreach (var component in GetComponents<MonoBehaviour>()) component.enabled = false;
            foreach (var source in GetComponents<AudioSource>()) source.Stop();
            UIState.Blocking = false;
        }

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
            if (first && (scene.buildIndex != 0 || (MapRoot.Current != null && MapRoot.Current.previewOnly)))
            {
                autosave = false;
                saveToLoad = null;
                Debug.Log("[저장] 비시작 씬/제작 시험장: 기존 저장 적용과 자동 저장을 끕니다.");
            }
            else if (first && saveToLoad != null)
            {
                var d = saveToLoad;
                saveToLoad = null;
                try { Apply(d); }
                catch (System.Exception e) { autosave = false; Debug.LogError("기존 저장 복원 실패: 원본을 덮어쓰지 않습니다. " + e.Message); return; }
                if (!string.IsNullOrEmpty(d.scene) && d.scene != scene.name)
                {
                    StartCoroutine(Travel(d.scene, null, 0f, new Vector2Int(d.fx, d.fy), new Vector3(d.px, d.py, 0f)));
                    return;
                }
                pendingExact = new Vector3(d.px, d.py, 0f);
                pendingFacing = new Vector2Int(d.fx, d.fy);
                Notify.Show("이어서 한다 (Esc 메뉴에서 처음부터 시작 가능)", 4f);
            }

            string id = pendingSpawnId;
            if (id == null && first && pendingExact == null) id = SpawnPoint.StartId;
            pendingSpawnId = null;

            var player = FindAnyObjectByType<PlayerMotor>();
            var pc = player != null ? player.GetComponent<PlayerController>() : null;
            if (pendingExact.HasValue && pc != null)
            {
                var savedPosition = pendingExact.Value;
                if (MapRoot.Current != null && MapRoot.Current.TryResolveSavedPosition(savedPosition, player.feetOffset, out var restored))
                    savedPosition = restored;
                player.Teleport(savedPosition, pendingFacing);
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
                    Notify.Show(map.displayName, 3f);
                }
                else Notify.Show(map.displayName, 2.5f);
                GameEvents.RaiseMapEntered(map.mapId);
                sun = FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();
                // 기존 낙하물은 Apply에서 재산으로 이주하며 월드에 새로 배치하지 않는다.
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

        /// <summary>이전 호출 호환용. 새 주행 낙하는 만들지 않는다.</summary>
        public void DropRandomCargo(Vector3 pos)
        {
            // 이전 외부 호출과의 API 호환. 신규 주행 낙하를 생성하지 않는다.
            Player.shake = 0f;
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

        public bool SaveNow()
        {
            if (!autosave || MapRoot.Current == null) return false;
            var pc = PlayerController.Instance;
            var d = new SaveData
            {
                scene = SceneManager.GetActiveScene().name,
                px = pc != null ? pc.transform.position.x : 0, py = pc != null ? pc.transform.position.y : 0,
                fx = pc != null ? pc.Motor.Facing.x : 0, fy = pc != null ? pc.Motor.Facing.y : -1,
                money = Player.money, credit = Player.credit,
                vehicle = Player.vehicle != null ? Player.vehicle.id : null,
                stamina = Player.stamina, shake = Player.shake,
                day = Clock.day, minute = Clock.minuteOfDay,
                nextContractId = Contracts.nextId,
            };
            foreach (var kv in Player.cargo.Items) { d.cargoItems.Add(kv.Key.id); d.cargoCounts.Add(kv.Value); d.cargoCosts.Add(Player.cargo.TotalCost(kv.Key)); }
            d.storedCargo = Player.warehouse.Export();
            d.contracts.AddRange(Player.contracts);
            Quests.Export(d);
            foreach (var m in markets.Values) d.markets.Add(m.Export());
            d.boards.AddRange(Contracts.boards);
            d.traders.AddRange(Traders.traders);
            d.drops.AddRange(Drops);
            d.marketQuotes.AddRange(Player.ledger.Export());
            foreach (var kv in cooldowns) { d.cooldownKeys.Add(kv.Key); d.cooldownUntil.Add(kv.Value); }
            return SaveSystem.Write(d);
        }

        void Apply(SaveData d)
        {
            Player.warehouse.Import(d.storedCargo);
            Player.ledger.Import(d.marketQuotes);
            Player.money = d.money;
            Player.credit = d.credit;
            Player.vehicle = Db.vehicles.Find(v => v != null && v.id == d.vehicle) ?? Db.startVehicle;
            Player.stamina = d.stamina;
            Player.shake = 0f;
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
            Contracts.nextId = Mathf.Max(Contracts.nextId, d.nextContractId);
            foreach (var c in Player.contracts) Contracts.nextId = Mathf.Max(Contracts.nextId, c.id + 1);
            foreach (var b in Contracts.boards)
                foreach (var c in b.list) Contracts.nextId = Mathf.Max(Contracts.nextId, c.id + 1);
            Contracts.InitBoards();
            if (d.traders.Count > 0) Traders.traders = new List<NpcTraders.Trader>(d.traders);
            Drops.Clear(); if (d.drops != null) Drops.AddRange(d.drops);
            LegacyCargoMigration.Restore(Player, Drops, ItemById);
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
