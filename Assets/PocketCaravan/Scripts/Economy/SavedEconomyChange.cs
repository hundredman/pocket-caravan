using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 흥정·평판 회복·게시판 작업의 저장 경계. 쓰기가 실패하면 그 작업의 경제 상태를 함께 되돌린다.
    /// 마을 일의 한 시간 진행은 시장·NPC 상인·의뢰도 갱신하므로 이들도 포함한다.
    /// 기존 객체 참조는 유지한다. 파일/저장 형식·지도·플레이어 위치는 바꾸지 않는다.
    /// </summary>
    public static class SavedEconomyChange
    {
        public const string Failure = "저장하지 못해 처리하지 않았습니다. 저장 상태를 확인한 뒤 다시 시도해 주세요.";

        public static bool Try(GameManager gm, Action change)
        {
            if (gm == null || !gm.AutosaveEnabled) return false;
            // A failed preflight never changes gameplay state.
            if (!Write(gm)) return false;
            var p=gm.Player;
            int money=p.money, credit=p.credit, day=gm.Clock.day, nextId=gm.Contracts.nextId;
            float minute=gm.Clock.minuteOfDay; float playHours=gm.Clock.playHours;
            var reputation=p.reputation.Export(); var activity=p.tradeActivity.Export();
            var quests=new SaveData(); gm.Quests.Export(quests);
            var contracts=new List<Contract>(p.contracts);
            var boards=new List<ContractManager.Board>(gm.Contracts.boards);
            var boardLists=new Dictionary<ContractManager.Board,List<Contract>>();
            var added=new Dictionary<ContractManager.Board,float>();
            var originals=new Dictionary<Contract,string>();
            foreach(var c in contracts) originals[c]=JsonUtility.ToJson(c);
            foreach(var b in boards)
            {
                boardLists[b]=new List<Contract>(b.list); added[b]=b.lastAdded;
                foreach(var c in b.list) if(c!=null) originals[c]=JsonUtility.ToJson(c);
            }
            var markets=new Dictionary<MarketState,MarketState.Save>();
            foreach(var t in gm.Db.towns) if(t!=null) { var m=gm.GetMarket(t); markets[m]=m.Export(); }
            var traders=new List<NpcTraders.Trader>(gm.Traders.traders);
            var traderStates=new Dictionary<NpcTraders.Trader,string>();
            foreach(var t in traders) traderStates[t]=JsonUtility.ToJson(t);
            try
            {
                change();
                if(Write(gm)) return true;
            }
            catch(Exception e) { Debug.LogWarning("[경제 작업] 처리하지 못했습니다: "+e.GetType().Name); }
            p.money=money; p.credit=credit; p.reputation.Import(reputation); p.tradeActivity.Import(activity);
            p.contracts.Clear(); p.contracts.AddRange(contracts);
            gm.Contracts.boards.Clear(); gm.Contracts.boards.AddRange(boards); gm.Contracts.nextId=nextId;
            foreach(var c in originals) JsonUtility.FromJsonOverwrite(c.Value,c.Key);
            foreach(var b in boards) { b.list.Clear(); b.list.AddRange(boardLists[b]); b.lastAdded=added[b]; }
            gm.Clock.day=day; gm.Clock.minuteOfDay=minute; gm.Clock.playHours=playHours;
            foreach(var m in markets) m.Key.Import(m.Value,gm.ItemById);
            gm.Traders.traders.Clear(); gm.Traders.traders.AddRange(traders);
            foreach(var t in traderStates) JsonUtility.FromJsonOverwrite(t.Value,t.Key);
            gm.Quests.Import(quests);
            Notify.Clear();
            return false;
        }

        static bool Write(GameManager gm)
        {
            try { return gm.SaveNow(); }
            catch(Exception e) { Debug.LogWarning("[경제 작업] 저장하지 못했습니다: "+e.GetType().Name); return false; }
        }
    }
}
