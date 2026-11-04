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
        TravelTransition transition;   // 이번 이동의 지역별 화면 효과 (null이면 검은 페이드)
        string transitionName;
        bool transitionCovering;
        float coveredAt = -1f;

        /// <summary>맵을 넘어가는 중 (화면이 어두워졌다 밝아지는 동안 조작을 막음)</summary>
        public bool IsTravelling { get; private set; }
        bool firstSceneLoaded;
        bool skipStartScreen;   // 시작 화면에서 이어하기/새로 시작을 고른 뒤
        SaveData saveToLoad;
        bool autosave = true;
        RegionTestSession regionSession;

        /// <summary>지역 시험 세션 중이면 그 설정 (전용 저장 파일 사용). 본편이면 null.</summary>
        public RegionTestSession ActiveRegionSession => regionSession;
        /// <summary>현재 자동 저장 여부 (시험 도구 확인용)</summary>
        public bool AutosaveEnabled => autosave;

        public string LoadIssue { get; private set; }

        void BlockFailedLoad(string reason)
        {
            autosave = false;
            LoadIssue = "저장 복원을 중단했습니다. " + reason + " 기존 파일은 보존되며 이번 세션은 저장되지 않습니다.";
            Debug.LogWarning("[불러오기] " + LoadIssue);
            Notify.Show("저장 복원 중단 · 원본 보존/저장 차단. Esc 메뉴에서 원인을 확인하세요.", 8f);
        }

        bool TryApplySave(SaveData data)
        {
            try { Apply(data); return true; }
            catch (System.Exception e) { BlockFailedLoad(e.Message); return false; }
        }

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

            Player.reputation.Initialize(ReputationState.HomeOf(Player.origin));

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
            gameObject.AddComponent<InventoryUI>();
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
            Player.tradeActivity.Clear();
            Player.contracts.Clear();
            Player.origin = null;
            Player.reputation.Initialize(null);
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
            // 지역별 이동 효과(TravelTransition). 등록이 없으면 기존 검은 페이드와 시간 그대로.
            string fromMap = MapRoot.Current != null ? MapRoot.Current.mapId : null;
            transition = TravelTransition.For(fromMap, sceneName);
            transition?.Begin(fromMap, sceneName);
            var index = WorldIndex.Get();
            transitionName = index != null ? index.MapName(sceneName) : sceneName;
            transitionCovering = true; coveredAt = -1f;
            float cover = transition != null ? transition.CoverSeconds : 0.12f;
            float reveal = transition != null ? transition.RevealSeconds : 0.18f;
            // 지역 효과는 한 프레임이 길어도(씬 불러오기 직후 등) 건너뛰지 않게 프레임당 진행을 1/30초로 묶는다
            float Dt() => transition != null ? Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) : Time.unscaledDeltaTime;
            if (cover > 0f)
                for (float t = 0; t < 1f; t += Dt() / cover) { fade = t; yield return null; }
            fade = cover > 0f ? 1f : 0f;
            coveredAt = Time.unscaledTime;
            // Present a fully covered frame before scene loading can stall the main thread.
            if (cover > 0f) yield return null;
            pendingSpawnId = spawnId; pendingOffset = offset; pendingFacing = facing; pendingExact = exact;
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null;
            float loadedAt = Time.unscaledTime;
            while (transition != null && (Time.unscaledTime - coveredAt < transition.MinHoldSeconds
                   || Time.unscaledTime - loadedAt < transition.AfterLoadSeconds)) yield return null;
            transitionCovering = false;
            { var cam = FindAnyObjectByType<CameraFollow>(); if (cam != null) cam.SnapNow(); }   // 2026-10-29: 걷히기 전에 카메라를 한 번 더 붙인다
            if (reveal > 0f)
                for (float t = 1f; t > 0f; t -= Dt() / reveal) { fade = t; yield return null; }
            fade = 0f;
            transition = null;
            IsTravelling = false;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool first = !firstSceneLoaded;
            firstSceneLoaded = true;
            UIState.Blocking = false;

            // 지역 시험 세션: 그 지역 씬에서 Play를 시작하면 본편 저장 대신 전용 파일을 읽고 쓴다.
            bool startedRegionSession = first && TryBeginRegionSession();

            // 게임을 켜서 첫 씬(빌드 0번)이 열리면 시작 화면에서 이어하기/새로 시작·문명을 고를 때까지 붙잡아 둔다.
            if (first && !startedRegionSession && !skipStartScreen && scene.buildIndex == 0 && !SaveSystem.LoadBlocked
                && (MapRoot.Current == null || !MapRoot.Current.previewOnly) && StartScreen.Available)
            {
                StartScreen.Open(saveToLoad != null);
                return;
            }

            // 처음 연 씬이 시작 맵이면 저장을 불러온다. 다른 씬에서 바로 Play한 경우(개발 중)는 불러오지도, 덮어쓰지도 않는다.
            if (first && !startedRegionSession && (scene.buildIndex != 0 || (MapRoot.Current != null && MapRoot.Current.previewOnly)))
            {
                autosave = false;
                saveToLoad = null;
                Debug.Log("[저장] 비시작 씬/제작 시험장: 기존 저장 적용과 자동 저장을 끕니다.");
                ApplyDevOrigin();
            }
            else if (first && SaveSystem.LoadBlocked)
            {
                BlockFailedLoad("저장 형식이나 파일을 읽을 수 없습니다.");
            }
            else if (first && saveToLoad != null)
            {
                var d = saveToLoad;
                saveToLoad = null;
                if (!TryApplySave(d)) return;
                if (!string.IsNullOrEmpty(d.scene) && GoryeoLayout.Resolve(d.scene) != scene.name)
                {
                    StartCoroutine(Travel(GoryeoLayout.Resolve(d.scene), null, 0f, new Vector2Int(d.fx, d.fy), SaveSystem.PositionFor(d)));
                    return;
                }
                pendingExact = SaveSystem.PositionFor(d);
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
                    if(transition == null || transition.ShowArrivalNotice)
                    { if(UIStyle.CrispRegion)Notify.Location(map.displayName,3f);else Notify.Show(map.displayName,3f); }
                }
                else if(transition == null || transition.ShowArrivalNotice)
                { if(UIStyle.CrispRegion)Notify.Location(map.displayName,2.5f);else Notify.Show(map.displayName,2.5f); }
                GameEvents.RaiseMapEntered(map.mapId);
                sun = null;
                foreach (var light in FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None))
                    if (light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global) { sun = light; break; }
                // 기존 낙하물은 Apply에서 재산으로 이주하며 월드에 새로 배치하지 않는다.
                AudioManager.OnMapEntered(map.mapId);
                SaveNow();
            }
        }

        /// <summary>개발 중 지역 씬을 바로 Play하면 시작 화면을 거치지 않으므로, 그 맵 지역의 출신(주인공 그림 포함)을 넣는다.
        /// 맵 id 앞부분(north_·venice_·arabia_)이 시작 화면 문명의 출발 씬과 같으면 그 출신. 아니면 기본 출신 그대로.</summary>
        void ApplyDevOrigin()
        {
            var all = StartChoices.Get();
            var map = MapRoot.Current;
            if (all == null || map == null || string.IsNullOrEmpty(map.mapId)) return;
            int cut = map.mapId.IndexOf('_');
            if (cut <= 0) return;
            string prefix = map.mapId.Substring(0, cut + 1);
            foreach (var c in all.choices)
            {
                if (c.origin == null || string.IsNullOrEmpty(c.startScene) || !c.startScene.StartsWith(prefix)) continue;
                if (Player.origin == c.origin) return;
                Player.origin = c.origin;
                Player.credit = c.origin.startingCredit;
                Player.reputation.Initialize(ReputationState.HomeOf(Player.origin));
                Debug.Log($"[개발 시작] {map.mapId}에서 바로 Play: 출신을 {c.origin.displayName}(으)로 맞춤");
                return;
            }
        }

        /// <summary>시작 화면 "이어하기": 붙잡아 둔 첫 씬 처리를 그대로 이어간다 (저장 불러오기 포함).</summary>
        public void ContinueFromStartScreen()
        {
            skipStartScreen = true;
            firstSceneLoaded = false;
            UIState.Blocking = false;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        /// <summary>시작 화면 "새로 시작": 고른 출신으로 처음 상태를 맞추고 그 문명의 출발 마을로 간다.
        /// 시작 돈은 네 문명 모두 같다(Db.startMoney). 지난 저장은 출발 마을에 도착하면서 덮어쓴다.</summary>
        public void BeginNewGame(StartChoices.Choice choice)
        {
            skipStartScreen = true;
            saveToLoad = null;
            Player.origin = choice.origin;
            Player.credit = choice.origin != null ? choice.origin.startingCredit : 0;
            Player.money = Db.startMoney;
            Player.reputation.Initialize(ReputationState.HomeOf(Player.origin));
            UIState.Blocking = false;
            StartCoroutine(Travel(choice.startScene, string.IsNullOrEmpty(choice.startSpawn) ? SpawnPoint.StartId : choice.startSpawn, 0f, default, null));
        }

        /// <summary>Resources/RegionSessions/{지역 id}가 있는 지역 씬에서 시작하면 전용 저장·시작값으로 바꾼다.
        /// 본편 저장 파일은 읽지도 쓰지도 지우지도 않는다. 제작 시험장(previewOnly)은 제외.</summary>
        bool TryBeginRegionSession()
        {
            var map = MapRoot.Current;
            if (map == null || map.previewOnly) return false;
            var session = RegionTestSession.For(map.region);
            if (session == null) return false;
            regionSession = session;
            SaveSystem.UseSlot(session.saveFileName);
            autosave = true;
            Player.money = session.startMoney;
            if (session.startOrigin != null) { Player.origin = session.startOrigin; Player.credit = session.startOrigin.startingCredit; }
            if (session.startVehicle != null) Player.vehicle = session.startVehicle;
            saveToLoad = SaveSystem.Read();
            Debug.Log($"[저장] {session.regionId} 시험 세션: 본편 저장 대신 전용 파일 {SaveSystem.FileName} 사용"
                + (saveToLoad != null ? " (이어서 하기)" : " (새로 시작)"));
            return true;
        }

        void OnGUI()
        {
            if (fade <= 0f) return;
            GUI.depth = -1000;
            if (transition != null)
            {
                transition.Draw(fade, transitionCovering, transitionName, coveredAt < 0f ? 0f : Time.unscaledTime - coveredAt);
                return;
            }
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

        // ---------- 지역 진행 메모 ----------
        // 지역별 작은 진행(찾은 수집품, 하루 한 번 놀이 등)을 이미 저장되는 숫자 표에 "지역:이름" 키로 남긴다.
        // 저장 형식은 바꾸지 않는다. 키는 지역 접두사로 구분한다 (예: "venice:lion:3").
        public float GetMemo(string key, float fallback = 0f) => cooldowns.TryGetValue(key, out var v) ? v : fallback;
        public bool HasMemo(string key) => cooldowns.ContainsKey(key);
        public void SetMemo(string key, float value) => cooldowns[key] = value;
        public void ClearMemo(string key) => cooldowns.Remove(key);   // 2026-11-04 점검 도구(리액션 잊기)용

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
            // 시작 화면에서 고르는 중에는 저장하지 않는다 (지난 저장을 덮지 않게)
            if (!autosave || MapRoot.Current == null || StartScreen.IsOpen) return false;
            var pc = PlayerController.Instance;
            var d = new SaveData
            {
                scene = SceneManager.GetActiveScene().name,
                layoutVersion = GoryeoLayout.Current,
                mapHeight = MapRoot.Current.ground.cellBounds.size.y,
                px = pc != null ? pc.transform.position.x : 0, py = pc != null ? pc.transform.position.y : 0,
                fx = pc != null ? pc.Motor.Facing.x : 0, fy = pc != null ? pc.Motor.Facing.y : -1,
                money = Player.money, credit = Player.credit,
                origin = Player.origin != null ? Player.origin.id : null,
                vehicle = Player.vehicle != null ? Player.vehicle.id : null,
                stamina = Player.stamina, shake = Player.shake,
                day = Clock.day, minute = Clock.minuteOfDay,
                nextContractId = Contracts.nextId,
            };
            foreach (var kv in Player.cargo.Items) { d.cargoItems.Add(kv.Key.id); d.cargoCounts.Add(kv.Value); d.cargoCosts.Add(Player.cargo.TotalCost(kv.Key)); }
            d.storedCargo = Player.warehouse.Export();
            d.tradeActivity = Player.tradeActivity.Export();
            d.contracts.AddRange(Player.contracts);
            Quests.Export(d);
            foreach (var m in markets.Values) d.markets.Add(m.Export());
            d.boards.AddRange(Contracts.boards);
            d.traders.AddRange(Traders.traders);
            d.drops.AddRange(Drops);
            d.marketQuotes.AddRange(Player.ledger.Export());
            foreach (var kv in cooldowns) { d.cooldownKeys.Add(kv.Key); d.cooldownUntil.Add(kv.Value); }
            Player.reputation.ExportTo(d);
            return SaveSystem.Write(d);
        }

        void Apply(SaveData d)
        {
            SaveCompatibility.Validate(d, Db, Application.CanStreamedLevelBeLoaded);
            Player.reputation.ImportFrom(d, Db.startOrigin);
            Player.warehouse.Import(d.storedCargo);
            Player.tradeActivity.Import(d.tradeActivity);
            Player.ledger.Import(d.marketQuotes);
            Player.money = d.money;
            Player.credit = d.credit;
            // 출신은 시작 화면이 생기면서 저장에 들어감. 옛 저장(값 없음)은 기본 출신 그대로.
            var savedOrigin = StartChoices.FindOrigin(d.origin);
            if (savedOrigin != null) Player.origin = savedOrigin;
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
            GoryeoLayout.MigrateDrops(d);   // 옛 배치 저장의 떨어진 짐 → 새 맵 도착 자리 옆
            Drops.Clear(); if (d.drops != null) Drops.AddRange(d.drops);
            LegacyCargoMigration.Restore(Player, Drops, ItemById);
            cooldowns.Clear();
            for (int i = 0; i < d.cooldownKeys.Count && i < d.cooldownUntil.Count; i++) cooldowns[d.cooldownKeys[i]] = d.cooldownUntil[i];
        }

        /// <summary>저장을 지우고 처음부터</summary>
        public static void NewGame()
        {
            // 지역 시험 세션이면 전용 파일만 지우고 그 지역 시작 씬으로 돌아간다.
            string restart = I != null && I.regionSession != null ? I.regionSession.startScene : null;
            SaveSystem.Delete();
            var old = I;
            I = null;
            if (old != null) { old.autosave = false; Destroy(old.gameObject); }
            GameEvents.ClearAll();
            UIState.Blocking = false;
            Boot();
            if (!string.IsNullOrEmpty(restart)) SceneManager.LoadScene(restart);
            else SceneManager.LoadScene(0);
        }
    }
}
