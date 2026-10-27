using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Reflection;
using PocketCaravan;
using UnityEngine;

string root=Path.GetFullPath(args[0]);Resources.Folder=Path.Combine(root,"Assets/PocketCaravan/Resources");
var snapshot=JsonNode.Parse(File.ReadAllText(Path.Combine(root,"Docs/Validation/R2-2026-10-25/source-snapshot.json")));
// RegionData equality in MarketState is object identity, exactly as real shared Unity assets.
var regions=snapshot["towns"].AsArray().Select(x=>(string)x["region"]).Distinct().ToDictionary(x=>x,x=>new RegionData{id=x,displayName=x,townReputation=x=="transit"});
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
 var player=GameManager.I.Player;float now=GameManager.I.Clock.TotalHours;
 float buyHaggle=h=="none"?0:haggle[stage],sellHaggle=h=="both"?haggle[stage]:0;
 int buy=ReputationPricing.Quote(market,item,false,player,player.tradeActivity,now,buyHaggle,sellHaggle);
 int sell=ReputationPricing.Quote(market,item,true,player,player.tradeActivity,now,buyHaggle,sellHaggle);
 Check(buy>=sell+1,"Safety spread violated");return(buy,sell);
}
Cycle Run(TownData t,ItemData item,int level,float stock,bool growth,int quantity,bool sellFirst,int stage,string h,bool baseline=false,bool trace=false,MarketState previous=null,bool preserve=false){
 var player=GameManager.I.Player;if(!preserve){player.tradeActivity.Clear();player.reputation.Initialize(null);}
 if(!preserve&&t.region!=null)player.reputation.Set(ReputationPricing.Scope(t),ReputationPricing.Target(t),(ReputationStanding)stage,0);
 var m=previous??Market(t,item,level,stock,growth);int buys=0,sells=0;var log=new List<object>();
 for(int leg=0;leg<2;leg++)for(int unit=0;unit<quantity;unit++){
  bool selling=sellFirst?leg==0:leg==1;if(!selling&&!m.CanBuy(item))return new(false,0,0,0,m.Level,m.Stock(item),log,m);
  var q=baseline?(Buy:m.BuyPrice(item),Sell:m.SellPrice(item)):Quote(m,item,stage,h);
  if(trace)log.Add(new{selling,stock=m.Stock(item),level=m.Level,volume=m.TradeVolume,buyQuote=q.Buy,sellQuote=q.Sell});
  if(selling){sells+=q.Sell;ReputationPricing.Sold(player,t,item,q.Sell,GameManager.I.Clock.TotalHours,GameManager.I.Clock.day);m.RecordSell(item,q.Sell);}else{buys+=q.Buy;ReputationPricing.Bought(player,t,item,q.Buy,GameManager.I.Clock.TotalHours);m.RecordBuy(item,q.Buy);}
 }
 return new(true,sells-buys,buys,sells,m.Level,m.Stock(item),log,m);
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
var exampleItem=new ItemData{id="counterexample",basePrice=100,pricePerMap=0,weight=1};var exampleTown=new TownData{id="counterexample",defaultOptimal=3,region=regions["goryeo"]};
GameManager.I.Player.origin=origins.Single(o=>o.id=="goryeo");GameManager.I.Clock.IsMarketDay=false;
var known=Run(exampleTown,exampleItem,1,3,false,1,false,5,"buy_only",trace:true);
Check(known.Buys==81&&known.Sells==80&&known.Profit==-1&&known.FinalStock==3,"Recorded purchase prevents fixed-stock counterexample");
var groups=new List<Group>();object singleCurrent=null,singleFuture=null;long cases=0,repeatChecked=0,repeatProfitable=0;
var goods=items.Values.Where(i=>!i.questItem).OrderBy(i=>i.id).ToArray();
foreach(bool growth in new[]{false,true})foreach(bool sellFirst in new[]{false,true})foreach(int stage in Enumerable.Range(0,6))foreach(string h in new[]{"none","buy_only","both"}){
 var group=new Group{stage=stage,haggle=h,growth=growth,order=sellFirst?"sell_then_buy":"buy_then_sell"};groups.Add(group);
 foreach(var t in towns.Values.OrderBy(t=>t.id))foreach(var item in goods)foreach(var origin in origins)foreach(int level in new[]{1,(int)snapshot["maxTownLevel"]})foreach(float ratio in new[]{.25f,.5f,1f,2f,4f})foreach(bool day in new[]{false,true})foreach(int quantity in new[]{1,5,20,100}){
  GameManager.I.Player.origin=origin;GameManager.I.Clock.IsMarketDay=day;GameManager.I.Clock.day=day?5:1;GameManager.I.Clock.TotalHours=day?104:8;float initial=t.OptimalFor(item)*(1f+.5f*(level-1))*ratio;
  var r=Run(t,item,level,initial,growth,quantity,sellFirst,stage,h);cases++;
  if(!r.Valid){group.ineligible++;continue;}group.valid++;
  if(r.Profit<=0)continue;group.profitable++;
  if(sellFirst){var repeated=Run(t,item,level,initial,growth,quantity,sellFirst,stage,h,previous:r.Model,preserve:true);repeatChecked++;if(repeated.Valid&&repeated.Profit>0)repeatProfitable++;}
  object Witness(bool details)=>new{town=t.id,item=item.id,origin=origin.id,stage,haggle=h,growth,order=group.order,startLevel=level,stockRatio=ratio,marketDay=day,quantity,initialStock=initial,result=details?Run(t,item,level,initial,growth,quantity,sellFirst,stage,h,trace:true):r};
  if(r.Profit>group.maxProfit){group.maxProfit=r.Profit;group.worst=Witness(true);}
  if(!sellFirst&&quantity==1){if(h=="none"&&singleCurrent==null)singleCurrent=Witness(true);if(h!="none"&&singleFuture==null)singleFuture=Witness(true);}
 }
}
var normalTrades=new List<object>();
GameManager.I.Clock.IsMarketDay=false;GameManager.I.Clock.TotalHours=8;GameManager.I.Clock.day=1;
foreach(var from in towns.Values.Where(t=>t.region.id=="goryeo"))foreach(var to in towns.Values.Where(t=>t.region.id=="goryeo"&&t!=from))foreach(var item in goods)foreach(var origin in origins)
{
 var player=GameManager.I.Player;player.origin=origin;player.tradeActivity.Clear();player.reputation.Initialize(ReputationState.HomeOf(origin));
 var source=Market(from,item,from.startLevel,from.OptimalFor(item)*(1+.5f*(from.startLevel-1)),false);
 var target=Market(to,item,to.startLevel,to.OptimalFor(item)*(1+.5f*(to.startLevel-1)),false);
 int rawBuy=source.BaseBuyPrice(item),rawSell=target.BaseSellPrice(item),buy=source.BuyPrice(item),sellBefore=target.SellPrice(item);
 player.tradeActivity.Bought(from.id,item.id,8,buy);int sellAfter=target.SellPrice(item);
 Check(sellBefore==sellAfter,"Cross-town normal trade must not be capped");
 normalTrades.Add(new{from=from.id,to=to.id,item=item.id,origin=origin.id,rawBuy,rawSell,buy,sellBefore,sellAfter,rawProfit=rawSell-rawBuy,newProfit=sellAfter-buy,purchasable=ReputationPricing.BuyDenial(player,from,item)==null});
}
var result=new{schemaVersion=1,appliedToGame=true,sharedRegionIdentity=true,currentDistanceMode="current",oracleCases=oracle,cycleCases=cases,checks,repeatChecked,repeatProfitable,normalTrades,safetySpreadViolations=0,knownCounterexample=known,singleCurrent,singleFuture,groups,limitations=new[]{"Compiled production quote, recent-purchase, daily contribution and reputation logic","A local purchase followed by returns is protected; initial carried goods sold before a new local purchase have no prior purchase record","Growth-off/volume0 and growth-on/volume0; levels1/max; stock ratios .25/.5/1/2/4; normal/market day; quantities1/5/20/100; both orders","No time passage/restock/NPC traders/cash/capacity constraints/haggle success probability; one-unit actual-data witnesses are recorded separately","Positive finite cycles alone do not establish indefinitely repeatable arbitrage","Shared RegionData instances by ID; twelve-hour window is measured from most recent purchase in each aggregated record"}};
File.WriteAllText(args[1],JsonSerializer.Serialize(result,new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));
long positive=groups.Where(g=>g.order=="buy_then_sell").Sum(g=>g.profitable);Console.WriteLine(JsonSerializer.Serialize(new{oracle,cycleCases=cases,checks,positive,unrecordedInitialSaleThenBuyPositive=groups.Where(g=>g.order=="sell_then_buy").Sum(g=>g.profitable),repeatChecked,repeatProfitable,safetySpreadViolations=0}));
return positive==0&&repeatProfitable==0?0:2;
record Cycle(bool Valid,int Profit,int Buys,int Sells,int FinalLevel,float FinalStock,List<object> Trace,[property:JsonIgnore] MarketState Model);
class Group {public int stage;public string haggle,order;public bool growth;public long valid,ineligible,profitable;public int maxProfit;public object worst;}
