using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>저장 파일 한 칸. 맵을 넘어갈 때와 게임을 끌 때 자동 저장한다.</summary>
    [Serializable]
    public class SaveData
    {
        // Keep missing-version pre-R1 JSON compatible; ReputationState.ExportTo promotes real saves to v3.
        public int version = 2;
        public ReputationState.Save reputation;
        public TradeActivity.Save tradeActivity;
        public int layoutVersion; // 1: broad regions, 2: M4, 3: T3 exterior boundaries; old saves migrate in memory.
        public string scene;
        public float px, py;
        public int mapHeight; // 0: 맵 확장 전 저장. WorldIndex의 고정된 이전 높이를 사용한다.
        public int fx, fy;

        public int money, credit;
        public string origin; // 출신 id (viking·venice·arabia·goryeo). 없으면 기본 출신
        public string vehicle;
        public float stamina;
        public float shake; // 이전 저장에는 없으므로 0으로 읽는다.
        public List<StoredCargo> storedCargo = new List<StoredCargo>();
        public List<string> cargoItems = new List<string>();
        public List<int> cargoCounts = new List<int>();
        public List<float> cargoCosts = new List<float>();
        public List<Contract> contracts = new List<Contract>();

        public int day;
        public float minute;

        public List<QuestManager.Save> quests = new List<QuestManager.Save>();
        public List<string> finishedQuests = new List<string>();
        public string trackedQuest;
        public int q1MigrationVersion; // 0: before Q1, 1: frozen legacy history
        public List<string> legacyMainFinishedIds = new List<string>();

        public List<MarketState.Save> markets = new List<MarketState.Save>();
        public List<ContractManager.Board> boards = new List<ContractManager.Board>();
        public int nextContractId;
        public List<NpcTraders.Trader> traders = new List<NpcTraders.Trader>();
        public List<DropRecord> drops = new List<DropRecord>();
        public List<TradeLedger.Quote> marketQuotes = new List<TradeLedger.Quote>();
        public List<string> cooldownKeys = new List<string>();
        public List<float> cooldownUntil = new List<float>();
    }

    public static class SaveSystem
    {
        public const int CurrentVersion = 3;
#if CARAVAN_PREVIEW
        const string SaveFile = "pocket_caravan_preview_save.json";
#else
        const string SaveFile = "pocket_caravan_save.json";
#endif
        // 지역 시험 세션(RegionTestSession) 전용 저장 칸. null이면 본편 저장 파일을 쓴다.
        static string slotFile;
        public static string FileName => slotFile ?? SaveFile;
        public static bool UsingSeparateSlot => slotFile != null;
        public static string CurrentPath => Path;
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>본편 파일과 다른 같은 폴더의 .json 이름만 허용한다. 하위 경로·본편/미리보기 파일명은 거부.</summary>
        public static bool IsValidSlotName(string name) =>
            !string.IsNullOrWhiteSpace(name) && name.EndsWith(".json", StringComparison.Ordinal)
            && name.IndexOfAny(new[] { '/', '\\', ':' }) < 0 && !name.Contains("..")
            && name != "pocket_caravan_save.json" && name != "pocket_caravan_preview_save.json";

        /// <summary>이후 읽기/쓰기/삭제를 전용 파일로 돌린다. Play 시작마다 본편 파일로 초기화된다.</summary>
        public static void UseSlot(string fileName)
        {
            if (!IsValidSlotName(fileName)) throw new ArgumentException("전용 저장 파일 이름이 올바르지 않습니다: " + fileName);
            slotFile = fileName;
            LoadBlocked = false;
        }

        // 도메인 리로드를 끈 에디터에서도 Play마다 본편 저장 칸으로 되돌린다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSlot() { slotFile = null; LoadBlocked = false; }

        public static bool Exists => File.Exists(Path);

        /// <summary>위쪽 기준 타일은 유지하고, 맵 높이 확장으로 바뀐 Unity y만 보정한다.</summary>
        public static Vector3 PositionFor(SaveData data)
        {
            GoryeoBoundaryLayout.Migrate(data);
            if (GoryeoLayout.TryPosition(data, out var migrated)) return migrated;
            var info = WorldIndex.Get()?.Map(GoryeoLayout.Resolve(data.scene));
            int current = info != null ? info.height : 0;
            int saved = data.mapHeight > 0 ? data.mapHeight : (info != null ? info.legacyHeight : 0);
            float delta = current > 0 && saved > 0 ? current - saved : 0;
            return new Vector3(data.px, data.py + delta, 0f);
        }

        public static bool LoadBlocked { get; private set; }

        public static bool Write(SaveData d)
        {
            if (LoadBlocked) { Debug.LogWarning("[저장] 기존 파일 읽기 실패로 덮어쓰기를 막았습니다."); return false; }
            return WriteAt(Path, d);
        }

        /// <summary>임시 파일 완성 후 원자 교체. 기존 파일은 .bak으로 보존한다.</summary>
        public static bool WriteAt(string path, SaveData d)
        {
            string temp = path + ".tmp";
            try
            {
                Validate(d);
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                { writer.Write(JsonUtility.ToJson(d)); writer.Flush(); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[저장] 실패, 기존 파일 보존: " + e.Message); return false; }
        }

        public static void Validate(SaveData d)
        {
            TradeActivity.Validate(d?.tradeActivity);
            if (d == null || d.version < 1 || d.version > CurrentVersion) throw new InvalidDataException("지원하지 않는 저장 버전입니다.");
            ReputationState.ValidateSaveBlock(d);
            if (d.cargoItems == null || d.cargoCounts == null || d.cargoCosts == null
                || d.cargoItems.Count != d.cargoCounts.Count || d.cargoItems.Count != d.cargoCosts.Count)
                throw new InvalidDataException("화물 저장 목록이 맞지 않습니다.");
            for (int i = 0; i < d.cargoCounts.Count; i++)
                if (d.cargoCounts[i] <= 0 || d.cargoCosts[i] < 0 || float.IsNaN(d.cargoCosts[i]) || float.IsInfinity(d.cargoCosts[i]))
                    throw new InvalidDataException("화물 수량/원가가 올바르지 않습니다.");
            var ids = new HashSet<string>();
            foreach (string id in d.cargoItems)
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException("화물 ID가 없거나 중복됐습니다.");
            if (d.contracts == null || d.quests == null || d.finishedQuests == null || d.markets == null || d.boards == null
                || d.traders == null || d.cooldownKeys == null || d.cooldownUntil == null || d.cooldownKeys.Count != d.cooldownUntil.Count)
                throw new InvalidDataException("진행 저장 목록이 없거나 맞지 않습니다.");
            if (d.money < 0 || !Finite(d.px) || !Finite(d.py) || !Finite(d.stamina) || d.stamina < 0 || d.stamina > 1
                || d.day < 0 || !Finite(d.minute) || d.minute < 0 || d.minute >= 1440)
                throw new InvalidDataException("잔고·위치·체력·시각 저장값이 올바르지 않습니다.");
            if (d.q1MigrationVersion < 0 || d.q1MigrationVersion > 1
                || (d.q1MigrationVersion == 1 && d.legacyMainFinishedIds == null)
                || (d.q1MigrationVersion == 0 && d.legacyMainFinishedIds != null && d.legacyMainFinishedIds.Count > 0))
                throw new InvalidDataException("메인 이주 기록의 버전/목록이 올바르지 않습니다.");
            foreach (var c in d.contracts) c?.ValidateRecovery();
            foreach (var b in d.boards) if (b?.list != null) foreach (var c in b.list) c?.ValidateRecovery();
            new WarehouseStore().Import(d.storedCargo);
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static SaveData Read()
        {
            LoadBlocked = false;
            if (!Exists) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
                Validate(data);
                return data;
            }
            catch (Exception e)
            { LoadBlocked = true; Debug.LogWarning("[불러오기] 기존 파일 보존·자동저장 차단: " + e.Message); return null; }
        }

        public static void Delete()
        {
            if (Exists) File.Delete(Path);
            LoadBlocked = false;
        }
    }
}
