using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using PocketCaravan;
using UnityEngine;

if(args.Length!=2)throw new ArgumentException("R2Probe <source-snapshot.json> <output.json>");
var snapshot=JsonNode.Parse(File.ReadAllText(args[0]));
var config=new GameDatabase {marketDayDemand=(float)snapshot["marketDayDemand"],maxTownLevel=(int)snapshot["maxTownLevel"],townLevelVolume=snapshot["townLevelVolume"].AsArray().Select(x=>(int)x).ToArray()};
var regions=snapshot["towns"].AsArray().Select(x=>(string)x["region"]).Distinct().ToDictionary(id=>id,id=>new RegionData{id=id});
Resources.Folder=Path.Combine(Directory.GetCurrentDirectory(),"Assets/PocketCaravan/Resources");
var towns=snapshot["towns"].AsArray().ToDictionary(x=>(string)x["id"],x=>new TownData {id=(string)x["id"],displayName=(string)x["displayName"],region=regions[(string)x["region"]],worldCoord=new((int)x["x"],(int)x["y"]),startLevel=(int)x["startLevel"],defaultOptimal=(float)x["defaultOptimal"]});
var items=snapshot["items"].AsArray().ToDictionary(x=>(string)x["id"],x=>new ItemData {id=(string)x["id"],displayName=(string)x["displayName"],basePrice=(float)x["basePrice"],pricePerMap=(float)x["pricePerMap"],weight=(float)x["weight"],questItem=(bool)x["questItem"],originTown=x["originTown"]!=null?towns[(string)x["originTown"]]:null});
foreach(var t in snapshot["towns"].AsArray())foreach(var s in t["stock"].AsArray())towns[(string)t["id"]].stock.Add(new(){item=items[(string)s["item"]],optimal=(float)s["optimal"],localPriceMultiplier=(float)s["localPriceMultiplier"]});
var origins=snapshot["origins"].AsArray().Select(x=>new OriginData {id=(string)x["id"],resaleDropMultiplier=(float)x["resaleDropMultiplier"],longHaulMinMaps=(int)x["longHaulMinMaps"],longHaulBonus=(float)x["longHaulBonus"]}).ToArray();
var policies=new[]{
 new Policy("baseline",new float[6],new float[6],new float[6]),
 new Policy("small",new[]{.15f,.07f,0f,-.02f,-.04f,-.06f},new float[6],new[]{0f,0f,.01f,.02f,.03f,.04f}),
 new Policy("example",new[]{.15f,.07f,0f,-.03f,-.06f,-.10f},new float[6],new[]{0f,0f,.03f,.05f,.08f,.10f}),
 new Policy("dual",new[]{.15f,.07f,0f,-.02f,-.04f,-.06f},new[]{0f,0f,0f,.01f,.02f,.03f},new[]{0f,0f,.03f,.05f,.08f,.10f})};
int checkedOracle=0;
foreach(var row in snapshot["oracle"].AsArray()){
 var t=towns[(string)row["town"]];var i=items[(string)row["item"]];float opt=t.OptimalFor(i)*(1f+.5f*((int)row["level"]-1));float stock=opt*(float)row["ratio"];float d=(float)row["demand"];
 if(MarketMath.Price(t.BasePriceFor(i),opt,stock,d)!=(int)row["buy"] || MarketMath.Buyback(MarketMath.Price(t.BasePriceFor(i),opt,stock+1,d))!=(int)row["sell"])throw new Exception("Unity oracle mismatch");checkedOracle++;
}
GameManager.I=new();
int Quote(MarketState market,ItemData i,Policy p,int stage,string haggle,bool selling){
 int raw=selling?market.SellPrice(i):market.BuyPrice(i);
 int rep=Mathf.Max(1,Mathf.RoundToInt(raw*(1+(selling?p.SellBonus[stage]:p.BuyDelta[stage]))));
 bool enabled=haggle=="both" || (haggle=="buy_only"&&!selling);
 return enabled?Mathf.Max(1,Mathf.RoundToInt(rep*(1+(selling?p.Haggle[stage]:-p.Haggle[stage])))):rep;
}
MarketState Market(TownData t,ItemData i,int level,float stock,bool growth){
 var db=new GameDatabase {marketDayDemand=config.marketDayDemand,maxTownLevel=growth?config.maxTownLevel:level,townLevelVolume=config.townLevelVolume};
 var m=(MarketState)Activator.CreateInstance(typeof(MarketState),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{t,db,false},null);
 m.Import(new MarketState.Save {town=t.id,level=level,volume=0,items=new(){i.id},stocks=new(){stock}},id=>items[id]);return m;
}
Cycle Run(TownData t,ItemData i,int level,float stock,bool growth,int quantity,bool sellFirst,Policy p,int stage,string haggle){
 var m=Market(t,i,level,stock,growth);int buys=0,sells=0;
 for(int leg=0;leg<2;leg++)for(int q=0;q<quantity;q++){
  bool selling=sellFirst?leg==0:leg==1;
  if(!selling&&!m.CanBuy(i))return new(false,0,0,0,m.Level,m.Stock(i));
  int price=Quote(m,i,p,stage,haggle,selling);
  if(selling){sells+=price;m.RecordSell(i,price);}else{buys+=price;m.RecordBuy(i,price);}
 }
 return new(true,sells-buys,buys,sells,m.Level,m.Stock(i));
}
// Known-answer platform and candidate composition checks, independently of the live snapshot.
if(Mathf.RoundToInt(2.5f)!=2||Mathf.RoundToInt(3.5f)!=4||MarketMath.Buyback(1)!=1||MarketMath.Price(10,10,0)!=20)throw new Exception("Platform semantics");
var checks=new List<string>{"Mathf midpoint-to-even, floor/min-price and stock clamp known answers","1680 live Unity quote cases equal linked production MarketMath"};
// Known answer: at 100 base/100 optimal stock, a one-unit baseline cycle loses 15.
var probeItem=new ItemData {id="probe",basePrice=100,pricePerMap=0};items.Add(probeItem.id,probeItem);
var probeTown=new TownData {id="probe",defaultOptimal=100};GameManager.I.Player.origin=origins.First(o=>o.id=="goryeo");
var plain=Run(probeTown,probeItem,1,100,false,1,false,policies[0],2,"none");
var stacked=Run(probeTown,probeItem,1,100,false,1,false,policies.First(p=>p.Id=="example"),5,"buy_only");
if(plain.Profit!=-15 || stacked.Profit!=4)throw new Exception("Known roundtrip composition mismatch");
items.Remove(probeItem.id);checks.Add("Known single-unit cycle: base buy100/sell85 => -15; 10% reputation then10% haggle => buy81/sell85 => +4");
var groups=new Dictionary<string,Group>();var quotes=new List<object>();long cycles=0;
var goods=items.Values.Where(i=>!i.questItem).OrderBy(i=>i.id).ToArray();
foreach(var t in towns.Values.OrderBy(t=>t.id))foreach(var i in goods)foreach(var origin in origins){
 GameManager.I.Player.origin=origin;
 foreach(var level in new[]{1,config.maxTownLevel})foreach(float ratio in new[]{.25f,.5f,1f,2f,4f})foreach(bool day in new[]{false,true})foreach(bool growth in new[]{false,true})foreach(int quantity in new[]{1,5,20,100})foreach(bool sellFirst in new[]{false,true}){
  GameManager.I.Clock.IsMarketDay=day;float initial=t.OptimalFor(i)*(1+.5f*(level-1))*ratio;
  var baseline=Run(t,i,level,initial,growth,quantity,sellFirst,policies[0],2,"none");
  foreach(var policy in policies)foreach(int stage in Enumerable.Range(0,6))foreach(var haggle in new[]{"none","buy_only","both"}){
   var result=policy.Id=="baseline"?baseline:Run(t,i,level,initial,growth,quantity,sellFirst,policy,stage,haggle);cycles++;
   string key=$"{policy.Id}/{stage}/{haggle}/{growth}/{sellFirst}";
   if(!groups.TryGetValue(key,out var g))groups[key]=g=new Group {policy=policy.Id,stage=stage,haggle=haggle,growth=growth,order=sellFirst?"sell_then_buy":"buy_then_sell"};
   if(!result.Valid){g.ineligible++;continue;}g.valid++;
   if(result.Profit>0)g.profitable++;
   if(result.Profit>0&&baseline.Valid&&baseline.Profit<=0)g.newProfitable++;
   if(baseline.Valid&&baseline.Profit>0)g.baselineProfitable++;
   if(g.worst==null||result.Profit>g.worst.profit)g.worst=new Example(t.id,i.id,origin.id,level,ratio,day,quantity,result.Profit,baseline.Valid?(int?)baseline.Profit:null,result.Buys,result.Sells,result.FinalLevel,initial,result.FinalStock);
  }
 }
}
var discountSweep=new List<object>();
// Freeze growth and omit haggle: isolate how even a small discount interacts with source traits.
foreach(int percent in new[]{0,1,2,3,4,5,6,8,10}){
 long eligible=0,positive=0,introduced=0;Example worst=null;
 var policy=new Policy("discount_sweep",Enumerable.Repeat(-percent/100f,6).ToArray(),new float[6],new float[6]);
 foreach(var t in towns.Values)foreach(var i in goods)foreach(var origin in origins)foreach(int level in new[]{1,config.maxTownLevel})foreach(float ratio in new[]{.25f,.5f,1f,2f,4f})foreach(bool day in new[]{false,true})foreach(int q in new[]{1,5,20,100}){
  GameManager.I.Player.origin=origin;GameManager.I.Clock.IsMarketDay=day;float initial=t.OptimalFor(i)*(1+.5f*(level-1))*ratio;
  var b=Run(t,i,level,initial,false,q,false,policies[0],2,"none");var r=Run(t,i,level,initial,false,q,false,policy,2,"none");
  if(!r.Valid)continue;eligible++;if(r.Profit>0)positive++;if(b.Valid&&b.Profit<=0&&r.Profit>0)introduced++;
  if(worst==null||r.Profit>worst.profit)worst=new Example(t.id,i.id,origin.id,level,ratio,day,q,r.Profit,b.Valid?(int?)b.Profit:null,r.Buys,r.Sells,r.FinalLevel,initial,r.FinalStock);
 }
 discountSweep.Add(new {percent=percent,eligible=eligible,positive=positive,introduced=introduced,worst=worst});
}
foreach(var t in towns.Values.OrderBy(t=>t.id))foreach(var i in goods){GameManager.I.Player.origin=origins.First(o=>o.id=="goryeo");GameManager.I.Clock.IsMarketDay=false;
 foreach(var p in policies)foreach(int stage in Enumerable.Range(0,6)){
 var m=Market(t,i,t.startLevel,t.OptimalFor(i)*(1+.5f*(t.startLevel-1)),false);int raw=m.BuyPrice(i);
 quotes.Add(new {town=t.id,item=i.id,policy=p.Id,stage=stage,buyBase=raw,sellBase=m.SellPrice(i),buyRep=Quote(m,i,p,stage,"none",false),buyMaxHaggle=Quote(m,i,p,stage,"buy_only",false),sellRep=Quote(m,i,p,stage,"none",true),sellMaxHaggle=Quote(m,i,p,stage,"both",true)});
 }
}
// Data coverage is explicit: no labels invented for rare items or facilities.
var report=new {schemaVersion=1,appliedToGame=false,oracleCases=checkedOracle,goods=goods.Select(x=>x.id),towns=towns.Keys,origins=origins.Select(x=>x.id),policies=policies,checks=checks,cycleCases=cycles,discountSweep=discountSweep,groups=groups.Values,quotes=quotes,limitations=new[]{"Historical candidate wrappers; approved D10 safety is examined separately in R2SafetyProbe","Rounding uses two stages: reputation then haggle; floor=1 each step","Stress cases assume successful maximum haggle, no capacity/cash restriction; infeasible stock is skipped","Growth-on starts at volume 0; levels1/max, 0.25/0.5/1/2/4 optimal stock, normal/market day, quantities 1/5/20/100, both transaction orders","Venice changes ending stock; growth changes level. Profitable cycles are not proof of indefinitely repeatable arbitrage","No restock/time passage/NPC trading or cooldown/success probability simulated","Current distance mode retained through the E1 unified service; layoutD switch is B5","No unlock items/facilities/reputation points/thresholds/recovery quantities chosen"}};
File.WriteAllText(args[1],JsonSerializer.Serialize(report,new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(new {oracleCases=checkedOracle,cycleCases=cycles,discountSweep=discountSweep,groups=groups.Count,quoteRows=quotes.Count,goods=goods.Length,towns=towns.Count}));
record Policy(string Id,float[] BuyDelta,float[] SellBonus,float[] Haggle);
record Cycle(bool Valid,int Profit,int Buys,int Sells,int FinalLevel,float FinalStock);
record Example(string town,string item,string origin,int startLevel,float stockRatio,bool marketDay,int quantity,int profit,int? baselineProfit,int buyTotal,int sellTotal,int finalLevel,float initialStock,float finalStock);
class Group {public string policy,haggle,order;public int stage;public bool growth;public long valid,ineligible,profitable,newProfitable,baselineProfitable;public Example worst;}
