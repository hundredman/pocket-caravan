using PocketCaravan;
using System.Text.RegularExpressions;

static class Q1Tests
{
    public static List<object> Run()
    {
        var results = new List<object>();
        void Check(string name, bool condition) { if (!condition) throw new Exception(name); results.Add(new { name = "Q1:" + name, observed = true, detail = "Linked production QuestManager; platform stubs. See Q1 Unity report for market/contract checks." }); }
        string Field(string text,string key) => Regex.Match(text,@"(?m)^\s+"+key+@":[^\S\r\n]*([^\r\n]*)$").Groups[1].Value.Trim();
        string Guid(string text) => Regex.Match(text,"guid: (\\w+)").Groups[1].Value;
        int Int(string text,string key) => int.TryParse(Field(text,key),out int n)?n:0;
        var itemRefs = new Dictionary<string,ItemData>();
        ItemData Item(string value) { string guid=Guid(value);if(guid=="")return null;if(!itemRefs.ContainsKey(guid))itemRefs[guid]=new(){id=guid,displayName=guid};return itemRefs[guid]; }
        var all=new List<QuestData>();var guidRefs=new Dictionary<string,QuestData>();var source=new Dictionary<QuestData,string>();
        foreach(var path in Directory.GetFiles("Assets/PocketCaravan/Data/Goryeo/Quests","*.asset"))
        {
            string s=File.ReadAllText(path);var q=new QuestData{id=Field(s,"id"),giverNpc=Field(s,"giverNpc"),retired=Int(s,"retired")==1,
                rewardMoney=Int(s,"rewardMoney"),rewardCredit=Int(s,"rewardCredit"),hasLegacyReward=Int(s,"hasLegacyReward")==1,
                legacyRewardMoney=Int(s,"legacyRewardMoney"),legacyRewardCredit=Int(s,"legacyRewardCredit"),
                rewardReputationScope=(ReputationScope)Int(s,"rewardReputationScope"),rewardReputationTarget=Field(s,"rewardReputationTarget"),rewardReputationSize=(ReputationSize)Int(s,"rewardReputationSize"),
                giveItem=Item(Field(s,"giveItem")),giveCount=Int(s,"giveCount"),offerItem=Item(Field(s,"offerItem")),offerCount=Int(s,"offerCount"),offerPrice=Int(s,"offerPrice")};
            foreach(Match m in Regex.Matches(s,@"(?ms)^  - type: (\d+)\r?\n(.*?)(?=^  - type: |^  rewardMoney:)"))
                q.objectives.Add(new(){type=(ObjectiveType)int.Parse(m.Groups[1].Value),targetId=Field(m.Groups[2].Value,"targetId"),count=Int(m.Groups[2].Value,"count"),item=Item(Field(m.Groups[2].Value,"item"))});
            all.Add(q);source[q]=s;guidRefs[Guid(File.ReadAllText(path+".meta"))]=q;
        }
        foreach(var q in all)
        {
            var block=Regex.Match(source[q],@"(?ms)^  requires:(.*?)(?=^  \w)").Groups[1].Value;
            foreach(Match m in Regex.Matches(block,"guid: (\\w+)")) q.requires.Add(guidRefs[m.Groups[1].Value]);
        }
        QuestData Q(string id)=>all.Single(q=>q.id==id);
        var ids=new[]{"q1_naru","q1_jangdan","q1_songak","q1_dakmok","q1_gaegyeong"};
        QuestManager Fresh()
        { GameEvents.ClearAll(); GameManager.I=new();GameManager.I.Player.money=100;GameManager.I.Db.quests=all;MapRoot.Current=null;return new(all); }
        var qm=Fresh();var player=GameManager.I.Player;
        foreach(string id in new[]{"tutorial_1","tutorial_2","naru_board","dakmok_hanji"}) Check("retired offer hidden "+id,!qm.CanStart(Q(id)));
        Check("first and only first main offered",qm.CanStart(Q(ids[0]))&&ids.Skip(1).All(id=>!qm.CanStart(Q(id))));
        var current=Q(ids[0]);qm.Accept(current);Check("salt parcel received",player.cargo.Count(current.giveItem)==1);qm.TryCompleteAt("goryeo_jangdan_farmer",out _);
        Check("first delivery earns credit once",qm.IsFinished(current)&&player.credit==1&&player.cargo.Count(current.giveItem)==0);
        current=Q(ids[1]);qm.Accept(current);var grain=current.objectives[0].item;var jangdan=new TownData{id="goryeo_jangdan"};var songak=new TownData{id="goryeo_songak"};
        GameEvents.RaiseItemBought(grain,songak,1);Check("wrong buy town ignored",qm.Active.Single().amount==0);
        for(int i=0;i<3;i++){player.cargo.Add(grain,1);GameEvents.RaiseItemBought(grain,jangdan,1);}
        for(int i=0;i<3;i++){player.cargo.Remove(grain,1);GameEvents.RaiseItemSold(grain,songak,2,1);}
        Check("trade tutorial no extra credit",qm.IsFinished(current)&&player.credit==1);
        current=Q(ids[2]);qm.Accept(current);GameEvents.RaiseContractFinished(new(){fromTown="naru"},true);
        Check("board must be Songak",qm.Active.Single().step==0);
        GameEvents.RaiseContractFinished(new(){fromTown="goryeo_songak"},true);qm.TryCompleteAt("goryeo_songak_merchant",out _);
        Check("board tutorial no duplicate credit",qm.IsFinished(current)&&player.credit==1);
        current=Q(ids[3]);qm.Accept(current);var paper=current.objectives[0].item;
        for(int i=0;i<2;i++){player.cargo.Add(paper,1);GameEvents.RaiseItemBought(paper,new(){id="dakmok"},1);}
        qm.TryCompleteAt("dakmok_chief",out _);Check("specialty tutorial completed",qm.IsFinished(current));
        current=Q(ids[4]);MapRoot.Current=new(){mapId="Gaegyeong"};qm.Accept(current);
        Check("previsited capital advances passively",qm.Active.Single().step==1);
        qm.TryCompleteAt("gaekju",out _);
        for(int i=0;i<2;i++){player.cargo.Remove(paper,1);GameEvents.RaiseItemSold(paper,new(){id="gaegyeong"},2,1);}
        Check("all five main quests complete",ids.All(id=>qm.IsFinished(Q(id))));
        Check("new main total quest rewards",player.money==225&&player.credit==2);
        var oldFull=new SaveData{credit=-9,money=321,finishedQuests=new(){"tutorial_2","tutorial_5"}};
        qm=Fresh();int money=GameManager.I.Player.money,credit=GameManager.I.Player.credit;qm.Import(oldFull);
        Check("legacy full exception all five mapped",ids.All(id=>qm.IsFinished(Q(id))));
        Check("completion migration pays no rewards",GameManager.I.Player.money==money&&GameManager.I.Player.credit==credit&&oldFull.credit==-9&&oldFull.money==321);
        qm.MigrateMainProgress();var exported=new SaveData();qm.Export(exported);qm.Import(exported);qm.MigrateMainProgress();var twice=new SaveData();qm.Export(twice);
        Check("migration and reload idempotent",exported.finishedQuests.Order().SequenceEqual(twice.finishedQuests.Order())&&GameManager.I.Player.money==money&&GameManager.I.Player.credit==credit);
        qm=Fresh();var partial=new SaveData{finishedQuests=new(){"tutorial_1"},quests=new(){new(){id="tutorial_2",step=1,rewardCaptured=true,acceptedRewardMoney=45,acceptedRewardCredit=1}}};qm.Import(partial);
        Check("legacy partial prefix only first mapped",qm.IsFinished(Q(ids[0]))&&ids.Skip(1).All(id=>!qm.IsFinished(Q(id))));
        Check("retired active keeps original step",qm.Active.Single().quest.id=="tutorial_2"&&qm.Active.Single().step==1);
        var oldPending=new SaveData();qm.Export(oldPending);qm.Import(oldPending);
        Check("legacy pending eligibility survives new-format reload",qm.Active.Single().legacyMainMigrationEligible);
        GameManager.I.Player.cargo.Add(Q("tutorial_2").objectives[1].item,2);qm.TryCompleteAt("gaekju",out _);
        Check("legacy completion keeps promised credit",GameManager.I.Player.credit==1&&qm.IsFinished(Q("tutorial_2")));
        Check("legacy later record retained across gap",qm.IsFinished(Q("q1_dakmok"))&&!qm.IsFinished(Q("q1_jangdan")));
        Check("earliest gap offered and later main blocked",qm.CanStart(Q("q1_jangdan"))&&!qm.CanStart(Q("q1_gaegyeong")));
        var sideRequirements=new Dictionary<string,string>{{"tutorial_3","q1_gaegyeong"},{"tutorial_4","q1_songak"},{"tutorial_5","q1_dakmok"},{"goryeo_jangdan_trade","q1_jangdan"},{"goryeo_songak_trade","q1_songak"}};
        foreach(var row in sideRequirements)
        { qm=Fresh();Check("side locked before prerequisite "+row.Key,!qm.CanStart(Q(row.Key)));qm.Import(new(){finishedQuests=new(){row.Value}});Check("side unlocked by prerequisite "+row.Key,qm.CanStart(Q(row.Key))); }
        qm=Fresh();qm.Import(new(){finishedQuests=new(){"q1_naru","q1_jangdan","q1_songak","q1_dakmok"}});
        qm.Accept(Q("tutorial_5"));GameEvents.RaiseVehicleBought(new(){id="handcart"});qm.TryCompleteAt("gaekju",out _);
        Check("new side completion never skips capital main",!qm.IsFinished(Q("q1_gaegyeong"))&&qm.CanStart(Q("q1_gaegyeong")));
        var modern=new SaveData();qm.Export(modern);qm.Import(modern);
        Check("modern side record cannot become legacy on reload",!qm.IsFinished(Q("q1_gaegyeong")));
        qm=Fresh();qm.Import(new(){q1MigrationVersion=1,legacyMainFinishedIds=new(){"tutorial_1","tutorial_2"},finishedQuests=new(){"tutorial_1","tutorial_2","q1_naru","q1_dakmok"}});
        qm.Accept(Q("tutorial_5"));GameEvents.RaiseVehicleBought(new(){id="handcart"});qm.TryCompleteAt("gaekju",out _);
        var mixed=new SaveData();qm.Export(mixed);qm.Import(mixed);
        Check("new side cannot combine with old paper to trigger legacy full exception",!qm.IsFinished(Q("q1_gaegyeong"))&&!qm.IsFinished(Q("q1_jangdan")));
        foreach(int badVersion in new[]{-1,2})
        { bool blocked=false;try{SaveSystem.Validate(new(){q1MigrationVersion=badVersion});}catch(System.IO.InvalidDataException){blocked=true;}Check("unsupported migration version rejected "+badVersion,blocked); }
        bool invalidHistory=false;try{SaveCompatibility.Validate(new(){q1MigrationVersion=1,legacyMainFinishedIds=new(){"tutorial_5"}},new(){quests=all},null);}catch(System.IO.InvalidDataException){invalidHistory=true;}
        Check("contradictory legacy history rejected",invalidHistory);
        // Accepted new quests are preserved if a legacy mapping also covers them.
        qm=Fresh();qm.Accept(Q("q1_naru"));var overlap=new SaveData();qm.Export(overlap);overlap.finishedQuests.Add("tutorial_1");qm.Import(overlap);
        Check("mapping preserves accepted new quest promise",qm.Active.Single().quest.id=="q1_naru"&&!qm.IsFinished(Q("q1_naru")));
        GameEvents.ClearAll();MapRoot.Current=null;
        return results;
    }
}
