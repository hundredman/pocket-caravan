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
        public int version = 2;
        public string scene;
        public float px, py;
        public int fx, fy;

        public int money, credit;
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
#if CARAVAN_PREVIEW
        const string SaveFile = "pocket_caravan_preview_save.json";
#else
        const string SaveFile = "pocket_caravan_save.json";
#endif
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, SaveFile);

        public static bool Exists => File.Exists(Path);

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
            if (d == null || d.version < 1 || d.version > 2) throw new InvalidDataException("지원하지 않는 저장 버전입니다.");
            if (d.cargoItems == null || d.cargoCounts == null || d.cargoCosts == null
                || d.cargoItems.Count != d.cargoCounts.Count || d.cargoItems.Count != d.cargoCosts.Count)
                throw new InvalidDataException("화물 저장 목록이 맞지 않습니다.");
            for (int i = 0; i < d.cargoCounts.Count; i++)
                if (d.cargoCounts[i] <= 0 || d.cargoCosts[i] < 0 || float.IsNaN(d.cargoCosts[i]) || float.IsInfinity(d.cargoCosts[i]))
                    throw new InvalidDataException("화물 수량/원가가 올바르지 않습니다.");
            new WarehouseStore().Import(d.storedCargo);
        }

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
