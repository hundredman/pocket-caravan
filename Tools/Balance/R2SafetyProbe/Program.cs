using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using PocketCaravan;
using UnityEngine;

string root=Path.GetFullPath(args[0]);Resources.Folder=Path.Combine(root,"Assets/PocketCaravan/Resources");
var snapshot=JsonNode.Parse(File.ReadAllText(Path.Combine(root,"Docs/Validation/R2-2026-10-25/source-snapshot.json")));
// RegionData equality in MarketState is object identity, exactly as real shared Unity assets.
var regions=snapshot["towns"].AsArray().Select(x=>(string)x["region"]).Distinct().ToDictionary(x=>x,x=>new RegionData{id=x});
var towns=snapshot["towns"].AsArray().ToDictionary(x=>(string)x["id"],x=>new TownData{id=(string)x["id"],region=regions[(string)x["region"]],worldCoord=new((int)x["x"],(int)x["y"]),startLevel=(int)x["startLevel"],defaultOptimal=(float)x["defaultOptimal"]});
var items=snapshot["items"].AsArray().ToDictionary(x=>(string)x["id"],x=>new ItemData{id=(string)x["id"],basePrice=(float)x["basePrice"],pricePerMap=(float)x["pricePerMap"],weight=(float)x["weight"],questItem=(bool)x["questItem"],originTown=x["originTown"]!=null?towns[(string)x["originTown"]]:null});
foreach(var t in snapshot["towns"].AsArray())foreach(var s in t["stock"].AsArray())towns[(string)t["id"]].stock.Add(new(){item=items[(string)s["item"]],optimal=(float)s["optimal"],localPriceMultiplier=(float)s["localPriceMultiplier"]});
var origins=snapshot["origins"].AsArray().Select(x=>new OriginData{id=(string)x["id"],resaleDropMultiplier=(float)x["resaleDropMultiplier"],longHaulMinMaps=(int)x["longHaulMinMaps"],longHaulBonus=(float)x["longHaulBonus"]}).ToArray();
var buyDelta=new[]{.15f,.07f,0,-.03f,-.06f,-.10f};var haggle=new[]{0f,0,.03f,.05f,.08f,.10f};
GameManager.I=new();long checks=0;
void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
var constructor=typeof(MarketState).GetConstructors(BindingFlags.NonPublic|BindingFlags.Instance).Single();
MarketState Market(TownData t,ItemData item,int level,float stock,bool growth){
 var db=new GameDatabase{marketDayDemand=(float)snapshot["marketDayDemand"],maxTownLevel=growth?(int)snapshot["maxTownLevel"]:level,townLevelVolume=snapshot["townLevelVolume"].AsArray().Select(x=>(int)x).ToArray()};
 var market=(MarketState)constructor.Invoke(new object[]{t,db,false});market.Import(new(){town=t.id,level=level,volume=0,items=new(){item.id},stocks=new(){stock}},id=>item);return market;
}
(int Buy,int Sell) Quote(MarketState market,ItemData item,int stage,string h){
 int buy=Math.Max(1,Mathf.RoundToInt(market.BuyPrice(item)*(1+buyDelta[stage]))),sell=market.SellPrice(item);
 if(h!="none")buy=Math.Max(1,Mathf.RoundToInt(buy*(1-haggle[stage])));
 if(h=="both")sell=Math.Max(1,Mathf.RoundToInt(sell*(1+haggle[stage])));
 buy=Math.Max(buy,sell+1); // literal Decisions 3: simultaneous fully-modified quotes
 Check(buy>=sell+1,"Safety spread violated");return(buy,sell);
}
Cycle Run(TownData t,ItemData item,int level,float stock,bool growth,int quantity,bool sellFirst,int stage,string h,bool baseline=false,bool trace=false){
 var m=Market(t,item,level,stock,growth);int buys=0,sells=0;var log=new List<object>();
 for(int leg=0;leg<2;leg++)for(int unit=0;unit<quantity;unit++){
  bool selling=sellFirst?leg==0:leg==1;if(!selling&&!m.CanBuy(item))return new(false,0,0,0,m.Level,m.Stock(item),log);
  var q=baseline?(Buy:m.BuyPrice(item),Sell:m.SellPrice(item)):Quote(m,item,stage,h);
  if(trace)log.Add(new{selling,stock=m.Stock(item),level=m.Level,volume=m.TradeVolume,buyQuote=q.Buy,sellQuote=q.Sell});
  if(selling){sells+=q.Sell;m.RecordSell(item,q.Sell);}else{buys+=q.Buy;m.RecordBuy(item,q.Buy);}
 }
 return new(true,sells-buys,buys,sells,m.Level,m.Stock(item),log);
}
// Check actual platform and the shared-region bug that the older candidate probe missed.
var grain=items["ginseng"];GameManager.I.Player.origin=origins.Single(o=>o.id=="venice");
Check(Market(towns["naru"],grain,1,10,false).SellStockDelta(grain)==1,"Domestic Venice must not be foreign");
Check(Market(towns["transit_woodtown"],grain,1,10,false).SellStockDelta(grain)==.8f,"Foreign Venice delta");
int oracle=0;
foreach(var row in snapshot["oracle"].AsArray()){
 var t=towns[(string)row["town"]];var i=items[(string)row["item"]];float opt=t.OptimalFor(i)*(1f+.5f*((int)row["level"]-1)),s=opt*(float)row["ratio"],d=(float)row["demand"];
 Check(MarketMath.Price(t.BasePriceFor(i),opt,s,d)==(int)row["buy"],"Buy oracle");Check(MarketMath.Buyback(MarketMath.Price(t.BasePriceFor(i),opt,s+1,d))==(int)row["sell"],"Sell oracle");oracle++;
}
// Mathematical counterexample with a fully restored stock, enough cash and one unit.
var exampleItem=new ItemData{id="counterexample",basePrice=100,pricePerMap=0,weight=1};var exampleTown=new TownData{id="counterexample",defaultOptimal=3};
GameManager.I.Player.origin=origins.Single(o=>o.id=="goryeo");GameManager.I.Clock.IsMarketDay=false;
var known=Run(exampleTown,exampleItem,1,3,false,1,false,5,"buy_only",trace:true);
Check(known.Buys==81&&known.Sells==85&&known.Profit==4&&known.FinalStock==3,"Literal-rule counterexample");
var groups=new List<Group>();object singleCurrent=null,singleFuture=null;long cases=0;
var goods=items.Values.Where(i=>!i.questItem).OrderBy(i=>i.id).ToArray();
foreach(bool growth in new[]{false,true})foreach(bool sellFirst in new[]{false,true})foreach(int stage in Enumerable.Range(0,6))foreach(string h in new[]{"none","buy_only","both"}){
 var group=new Group{stage=stage,haggle=h,growth=growth,order=sellFirst?"sell_then_buy":"buy_then_sell"};groups.Add(group);
 foreach(var t in towns.Values.OrderBy(t=>t.id))foreach(var item in goods)foreach(var origin in origins)foreach(int level in new[]{1,(int)snapshot["maxTownLevel"]})foreach(float ratio in new[]{.25f,.5f,1f,2f,4f})foreach(bool day in new[]{false,true})foreach(int quantity in new[]{1,5,20,100}){
  GameManager.I.Player.origin=origin;GameManager.I.Clock.IsMarketDay=day;float initial=t.OptimalFor(item)*(1f+.5f*(level-1))*ratio;
  var r=Run(t,item,level,initial,growth,quantity,sellFirst,stage,h);cases++;
  if(!r.Valid){group.ineligible++;continue;}group.valid++;
  if(r.Profit<=0)continue;group.profitable++;
  object Witness(bool details)=>new{town=t.id,item=item.id,origin=origin.id,stage,haggle=h,growth,order=group.order,startLevel=level,stockRatio=ratio,marketDay=day,quantity,initialStock=initial,result=details?Run(t,item,level,initial,growth,quantity,sellFirst,stage,h,trace:true):r};
  if(r.Profit>group.maxProfit){group.maxProfit=r.Profit;group.worst=Witness(true);}
  if(!sellFirst&&quantity==1){if(h=="none"&&singleCurrent==null)singleCurrent=Witness(true);if(h!="none"&&singleFuture==null)singleFuture=Witness(true);}
 }
}
var result=new{schemaVersion=1,appliedToGame=false,sharedRegionIdentity=true,currentDistanceMode="current",oracleCases=oracle,cycleCases=cases,checks,safetySpreadViolations=0,knownCounterexample=known,singleCurrent,singleFuture,groups,limitations=new[]{"Approved coefficients simulated with linked production MarketState; quote wrapper only, not runtime integration","Price floor compares simultaneous buy/sell quotes; positive cycles obey it at every transaction","Growth-off/volume0 and growth-on/volume0; levels1/max; stock ratios .25/.5/1/2/4; normal/market day; quantities1/5/20/100; both orders","No time passage/restock/NPC traders/cash/capacity constraints/haggle success probability; one-unit actual-data witnesses are recorded separately","Positive finite cycles alone do not establish indefinitely repeatable arbitrage","Old R2 candidate probe used separate RegionData objects per town, which misclassified domestic Venice sales; its profit counts are not reused"}};
File.WriteAllText(args[1],JsonSerializer.Serialize(result,new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));
long positive=groups.Sum(g=>g.profitable);Console.WriteLine(JsonSerializer.Serialize(new{oracle,cycleCases=cases,checks,positive,positiveNoHaggle=groups.Where(g=>g.haggle=="none").Sum(g=>g.profitable),safetySpreadViolations=0}));
return positive==0?0:2;
record Cycle(bool Valid,int Profit,int Buys,int Sells,int FinalLevel,float FinalStock,List<object> Trace);
class Group {public int stage;public string haggle,order;public bool growth;public long valid,ineligible,profitable;public int maxProfit;public object worst;}
