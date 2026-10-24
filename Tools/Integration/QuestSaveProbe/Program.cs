using PocketCaravan;
using System.Text.Json;
var results=new List<object>();
void Observe(string name,bool observed,string detail) { if(!observed)throw new Exception(name); results.Add(new {name,observed,detail}); }
QuestData Quest(params QuestObjective[] goals)=>new(){id="probe_quest",rewardMoney=50,rewardCredit=2,objectives=goals.ToList()};
QuestObjective Goal(ObjectiveType type,string target,int count=1)=>new(){type=type,targetId=target,count=count};
SaveData Saved(int step,int amount=0)=>new(){money=100,stamina=1,day=1,quests=new(){new(){id="probe_quest",step=step,amount=amount,rewardCaptured=true,acceptedRewardMoney=30,acceptedRewardCredit=1}}};
QuestManager Load(QuestData q,SaveData d){GameEvents.ClearAll();SaveCompatibility.Validate(d,new(){quests=new(){q}},null);var qm=new QuestManager(new(){q});qm.Import(d);return qm;}
var old=Quest(Goal(ObjectiveType.VisitMap,"Gaegyeong"),Goal(ObjectiveType.Talk,"gaekju"));
var data=Saved(1);var manager=Load(old,data);var progress=manager.Active.Single();
Observe("unchanged_definition_restores_goal",progress.Current.targetId=="gaekju" && progress.step==1,"Same quest definition restores step 1 to gaekju.");
Observe("accepted_rewards_remain_promised",progress.RewardMoney==30 && progress.RewardCredit==1,"Current asset reward 50/2 does not replace accepted reward 30/1.");
var complete=new SaveData{money=100,stamina=1,day=1,finishedQuests=new(){old.id}};
manager=Load(old,complete);Observe("completed_quest_remains_completed",manager.IsFinished(old)&&!manager.CanStart(old),"Finished ID survives import and blocks reacceptance.");
var reorder=Quest(Goal(ObjectiveType.Talk,"gaekju"),Goal(ObjectiveType.VisitMap,"goryeo_jangdan"));
manager=Load(reorder,data);progress=manager.Active.Single();
Observe("reordered_goal_silently_changes_meaning",progress.Current.targetId=="goryeo_jangdan" && progress.Current.type==ObjectiveType.VisitMap,"Same saved step 1 now means visit Jangdan, not talk to gaekju; validator accepts.");
var newCounter=Quest(Goal(ObjectiveType.VisitMap,"Gaegyeong"),Goal(ObjectiveType.Sell,"naru",3));
manager=Load(newCounter,Saved(1,2));progress=manager.Active.Single();
Observe("old_amount_moves_to_new_goal",progress.amount==2 && progress.Current.type==ObjectiveType.Sell,"Saved amount 2 is applied to a different new objective without semantic validation.");
var shortened=Quest(Goal(ObjectiveType.VisitMap,"Gaegyeong"));
manager=Load(shortened,Saved(1));manager.Tick();progress=manager.Active.Single();
Observe("equal_to_count_becomes_stuck_active",progress.Done && progress.Current==null && !manager.IsFinished(shortened),"step == objective count passes validation but remains active with no objective after Tick.");
bool blocked=false;try{Load(shortened,Saved(2));}catch(System.IO.InvalidDataException){blocked=true;}
Observe("beyond_count_is_blocked",blocked,"step greater than new count is rejected instead of silently losing progress.");
var legacy=Saved(0);legacy.quests[0].rewardCaptured=false;old.hasLegacyReward=true;old.legacyRewardMoney=12;old.legacyRewardCredit=3;
manager=Load(old,legacy);progress=manager.Active.Single();
Observe("uncaptured_reward_uses_legacy_fields",progress.RewardMoney==12 && progress.RewardCredit==3,"Old records without a captured reward use authored legacy reward fallback.");
MapRoot.Current=new(){mapId="Gaegyeong"};manager=Load(old,Saved(0));manager.Tick();progress=manager.Active.Single();
Observe("current_map_visit_is_already_passive",progress.step==1&&progress.Current.targetId=="gaekju","Already standing in target map advances VisitMap on Tick; no forced reentry needed.");
MapRoot.Current=null;GameEvents.ClearAll();
// Check every in-range active step using the current repository's real asset layouts.
int actualAssets=0, actualSteps=0;
foreach(var file in Directory.GetFiles("Assets/PocketCaravan/Data/Goryeo/Quests","*.asset")) {
 string yaml=File.ReadAllText(file);
 string Field(string block,string key)=>System.Text.RegularExpressions.Regex.Match(block,@"(?m)^\s+"+key+@": ([^\r\n]*)$").Groups[1].Value.Trim();
 var q=new QuestData{id=Field(yaml,"id"),rewardMoney=int.Parse(Field(yaml,"rewardMoney")),rewardCredit=int.Parse(Field(yaml,"rewardCredit"))};
 foreach(System.Text.RegularExpressions.Match block in System.Text.RegularExpressions.Regex.Matches(yaml,@"(?ms)^  - type: (\d+)\r?\n(.*?)(?=^  - type: |^  rewardMoney:)"))
  q.objectives.Add(Goal((ObjectiveType)int.Parse(block.Groups[1].Value),Field(block.Groups[2].Value,"targetId"),int.Parse(Field(block.Groups[2].Value,"count"))));
 if(q.objectives.Count==0)throw new Exception("No parsed goals: "+file);
 for(int i=0;i<q.objectives.Count;i++) {
  var d=Saved(i);d.quests[0].id=q.id;
  manager=Load(q,d);var before=manager.Active.Single();var exported=new SaveData();manager.Export(exported);
  manager=Load(q,exported);var after=manager.Active.Single();
  if(after.step!=i||after.Current.targetId!=q.objectives[i].targetId||after.Current.type!=q.objectives[i].type||after.Current.count!=q.objectives[i].count||after.RewardMoney!=30||after.RewardCredit!=1)throw new Exception("Current asset roundtrip failed: "+q.id+"/"+i);
  actualSteps++;
 }
 actualAssets++;
}
Observe("current_asset_goal_layout_roundtrips",actualAssets==17 && actualSteps>0,$"{actualAssets} actual quest asset layouts / {actualSteps} active steps restored with unchanged goal type, target and count. Item/vehicle GUID resolution and Unity serialization are outside this probe.");
GameEvents.ClearAll();
results.AddRange(Q1Tests.Run());
Console.WriteLine(JsonSerializer.Serialize(new {runtime=".NET 10; linked production C#; Unity platform stubs",unitySerializationTested=false,productionSavesTouched=false,observations=results},new JsonSerializerOptions{WriteIndented=true}));
