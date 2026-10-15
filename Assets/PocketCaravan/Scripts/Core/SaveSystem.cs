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
        public int version = 1;
        public string scene;
        public float px, py;
        public int fx, fy;

        public int money, credit;
        public string vehicle;
        public float stamina;
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

        public static void Write(SaveData d)
        {
            try { File.WriteAllText(Path, JsonUtility.ToJson(d)); }
            catch (Exception e) { Debug.LogWarning("[저장] 실패: " + e.Message); }
        }

        public static SaveData Read()
        {
            try { return Exists ? JsonUtility.FromJson<SaveData>(File.ReadAllText(Path)) : null; }
            catch (Exception e) { Debug.LogWarning("[불러오기] 실패: " + e.Message); return null; }
        }

        public static void Delete()
        {
            if (Exists) File.Delete(Path);
        }
    }
}
