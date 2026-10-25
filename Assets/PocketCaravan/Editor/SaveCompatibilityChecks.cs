using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class SaveCompatibilityChecks
    {
        [Serializable] public class Report { public List<string> passed=new List<string>(); }
        static void Check(Report r,string name,bool ok) { if(!ok)throw new Exception(name);r.passed.Add(name); }
        static SaveData Legacy() => JsonUtility.FromJson<SaveData>("{\"money\":9,\"stamina\":1,\"day\":1,\"minute\":480}");
        public static Report RunData()
        {
            var r=new Report();var db=Resources.Load<GameDatabase>("GameDatabase");var item=db.items.Find(i=>i!=null && !i.questItem);var town=db.towns.Find(t=>t!=null);var quest=db.quests.Find(q=>q!=null);
            void Valid(string name,SaveData d){SaveCompatibility.Validate(d,db,s=>s=="known_map");Check(r,name,true);}
            void Reject(string name,Action<SaveData> mutate)
            {
                var d=Legacy();mutate(d);string before=JsonUtility.ToJson(d);bool rejected=false;
                try{SaveCompatibility.Validate(d,db,s=>s=="known_map");}catch(InvalidDataException){rejected=true;}
                Check(r,name,rejected && before==JsonUtility.ToJson(d));
            }
            Valid("legacy missing optional IDs remains loadable",Legacy());
            var current=Legacy();current.scene="known_map";current.vehicle=db.startVehicle.id;current.cargoItems.Add(item.id);current.cargoCounts.Add(2);current.cargoCosts.Add(13);
            current.quests.Add(new QuestManager.Save{id=quest.id});current.markets.Add(new MarketState(town,db).Export());
            Valid("current registered cargo vehicle quest market accepted",current);
            Reject("unknown carried cargo rejected without mutating payload",d=>{d.cargoItems.Add("missing_cargo");d.cargoCounts.Add(1);d.cargoCosts.Add(4);});
            Reject("duplicate carried IDs cannot merge or overflow on load",d=>{d.cargoItems.AddRange(new[]{item.id,item.id});d.cargoCounts.AddRange(new[]{1,1});d.cargoCosts.AddRange(new[]{1f,1f});});
            Reject("unknown vehicle cannot silently become starting vehicle",d=>d.vehicle="missing_vehicle");
            Reject("unavailable saved map blocked before travel",d=>d.scene="missing_map");
            Reject("unknown active quest cannot disappear",d=>d.quests.Add(new QuestManager.Save{id="missing_quest"}));
            Reject("unknown completed quest cannot reset completion",d=>d.finishedQuests.Add("missing_quest"));
            Reject("negative quest step rejected",d=>d.quests.Add(new QuestManager.Save{id=quest.id,step=-1}));
            Reject("quest step beyond authored objectives rejected",d=>d.quests.Add(new QuestManager.Save{id=quest.id,step=quest.objectives.Count+1}));
            Reject("duplicate active quest IDs rejected",d=>{d.quests.Add(new QuestManager.Save{id=quest.id});d.quests.Add(new QuestManager.Save{id=quest.id});});
            Reject("null active progress rejected",d=>d.quests.Add(null));
            Reject("unknown stored market cannot lose state",d=>d.markets.Add(new MarketState.Save{town="missing_town"}));
            Reject("unknown market stock cannot disappear",d=>d.markets.Add(new MarketState.Save{town=town.id,items=new List<string>{"missing_cargo"},stocks=new List<float>{3}}));
            Reject("mismatched market arrays rejected",d=>d.markets.Add(new MarketState.Save{town=town.id,items=new List<string>{item.id}}));
            Reject("unknown active delivery destination rejected",d=>d.contracts.Add(new Contract{id=4,fromTown=town.id,toTown="missing_town",weight=2,reward=4}));
            Reject("unknown board town cannot disappear",d=>d.boards.Add(new ContractManager.Board{town="missing_town"}));
            Reject("null collection rejected before any state import",d=>d.contracts=null);
            Reject("invalid wallet rejected",d=>d.money=-1);
            Reject("invalid clock rejected",d=>d.minute=1440);
            Reject("NaN position rejected",d=>d.px=float.NaN);
            Reject("invalid stamina rejected",d=>d.stamina=2);
            var pending=Legacy();pending.storedCargo.Add(new StoredCargo{townId="old_town",itemId="old_item",count=1,totalCost=5});pending.drops.Add(new DropRecord{itemId="old_item",unitCost=2});
            string preserved=JsonUtility.ToJson(pending);Valid("unknown warehouse and legacy drop IDs remain preserved",pending);Check(r,"pending recovery payload unchanged",preserved==JsonUtility.ToJson(pending));
            var path="output/save-compatibility/file-check.json";Directory.CreateDirectory("output/save-compatibility");
            try
            {
                var good=Legacy();good.money=42;Check(r,"valid file write succeeds",SaveSystem.WriteAt(path,good));
                good.money=55;Check(r,"valid replacement creates backup",SaveSystem.WriteAt(path,good) && File.Exists(path+".bak"));
                byte[] before=File.ReadAllBytes(path),backup=File.ReadAllBytes(path+".bak");var bad=Legacy();bad.money=-1;
                Check(r,"invalid write preserves current and backup files",!SaveSystem.WriteAt(path,bad) && Convert.ToBase64String(before)==Convert.ToBase64String(File.ReadAllBytes(path)) && Convert.ToBase64String(backup)==Convert.ToBase64String(File.ReadAllBytes(path+".bak")));
            }
            finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
            return r;
        }

        // Non-start scene only; leaves an isolated test state and never invokes an allowed production save.
        public static Report RunPlay()
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;var gm=GameManager.I;
            if(!EditorApplication.isPlaying || gm==null || MapRoot.Current==null || (bool)typeof(GameManager).GetField("autosave",flags).GetValue(gm))throw new InvalidOperationException("Initialized non-start session required");
            var r=new Report();var apply=typeof(GameManager).GetMethod("TryApplySave",flags);var p=gm.Player;var item=gm.Db.items.Find(i=>i!=null && !i.questItem);
            UIState.Blocking=true;p.money=313;p.cargo.Clear();p.cargo.Add(item,2,7);
            var bad=Legacy();bad.money=1;bad.cargoItems.Add("missing_item_for_guard");bad.cargoCounts.Add(3);bad.cargoCosts.Add(12);
            var ledgerBefore=JsonUtility.ToJson(new Quotes{rows=p.ledger.Export()});var questsBefore=new SaveData();gm.Quests.Export(questsBefore);
            bool loaded=(bool)apply.Invoke(gm,new object[]{bad});
            Check(r,"incompatible cargo blocks actual GameManager load",!loaded);
            Check(r,"rejected load preserves live cash and cargo cost",p.money==313 && p.cargo.Count(item)==2 && Mathf.Approximately(p.cargo.TotalCost(item),14));
            var questsAfter=new SaveData();gm.Quests.Export(questsAfter);
            Check(r,"rejected load preserves quest and ledger state",JsonUtility.ToJson(questsBefore)==JsonUtility.ToJson(questsAfter) && ledgerBefore==JsonUtility.ToJson(new Quotes{rows=p.ledger.Export()}));
            Check(r,"failure explains cause and blocks SaveNow",gm.LoadIssue.Contains("missing_item_for_guard") && !gm.SaveNow() && !(bool)typeof(GameManager).GetField("autosave",flags).GetValue(gm));
            var sceneBad=Legacy();sceneBad.scene="missing_scene_for_guard";
            Check(r,"missing scene rejected before attempting travel",!(bool)apply.Invoke(gm,new object[]{sceneBad}) && !gm.IsTravelling && p.money==313);
            var oldVehicle=p.vehicle;var vehicleBad=Legacy();vehicleBad.vehicle="missing_vehicle_for_guard";
            Check(r,"unknown vehicle cannot replace live vehicle",!(bool)apply.Invoke(gm,new object[]{vehicleBad}) && p.vehicle==oldVehicle && p.money==313);
            var questBad=Legacy();questBad.quests.Add(new QuestManager.Save{id="missing_quest_for_guard"});
            Check(r,"late quest reference failure preserves cash and cargo",!(bool)apply.Invoke(gm,new object[]{questBad}) && p.money==313 && p.cargo.Count(item)==2);
            var marketBad=Legacy();marketBad.markets.Add(new MarketState.Save{town=gm.Db.towns.Find(t=>t!=null).id,items=new List<string>{"missing_stock_for_guard"},stocks=new List<float>{1}});
            Check(r,"late market reference failure preserves cash and cargo",!(bool)apply.Invoke(gm,new object[]{marketBad}) && p.money==313 && p.cargo.Count(item)==2);
            var good=Legacy();good.money=42;good.scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Check(r,"existing registered scene remains loadable",(bool)apply.Invoke(gm,new object[]{good}) && p.money==42 && p.cargo.Count(item)==0 && gm.Clock.day==1);
            Check(r,"successful diagnostic apply cannot reenable saving after failure",!gm.SaveNow());
            UIState.Blocking=false;
            return r;
        }
        [Serializable] class Quotes { public List<TradeLedger.Quote> rows; }
    }
}
