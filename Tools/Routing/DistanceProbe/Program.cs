using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using PocketCaravan;
using UnityEngine;

string root=Path.GetFullPath(args[0]), output=args[1];
Resources.Folder=Path.Combine(root,"Assets/PocketCaravan/Resources");
var options=new JsonSerializerOptions {IncludeFields=true,WriteIndented=true};
var snapshot=JsonNode.Parse(File.ReadAllText(Path.Combine(root,"Docs/Validation/R2-2026-10-25/source-snapshot.json")));
var region=new RegionData {id="goryeo",deliveryRewards=new(){enabled=true}};
var towns=snapshot["towns"].AsArray().ToDictionary(x=>(string)x["id"],x=>new TownData {id=(string)x["id"],displayName=(string)x["displayName"],region=(string)x["region"]=="goryeo"?region:new(){id=(string)x["region"]},worldCoord=new((int)x["x"],(int)x["y"]),startLevel=(int)x["startLevel"],defaultOptimal=(float)x["defaultOptimal"]});
var items=snapshot["items"].AsArray().ToDictionary(x=>(string)x["id"],x=>new ItemData {id=(string)x["id"],basePrice=(float)x["basePrice"],pricePerMap=(float)x["pricePerMap"],originTown=x["originTown"]!=null?towns[(string)x["originTown"]]:null});
foreach(var t in snapshot["towns"].AsArray())foreach(var s in t["stock"].AsArray())towns[(string)t["id"]].stock.Add(new(){item=items[(string)s["item"]],optimal=(float)s["optimal"],localPriceMultiplier=(float)s["localPriceMultiplier"]});
var world=new WorldIndex();
foreach(string file in Directory.GetFiles(Path.Combine(root,"Assets/PocketCaravan/Maps"),"*.json")){
 var m=JsonNode.Parse(File.ReadAllText(file));string id=(string)m["id"];
 var objects=m["objects"].AsArray();
 world.maps.Add(new(){mapId=id,neighbors=objects.Where(x=>(string)x["kind"]=="exit").Select(x=>(string)x["target"]).Distinct().ToList()});
 foreach(var o in objects.Where(x=>(string)x["kind"]=="market"))world.places.Add(new(){kind="market",id=(string)o["town"],mapId=id});
}
int checks=0;
void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
Check(JsonUtility.FromJson<TownDistance.Settings>(File.ReadAllText(Path.Combine(Resources.Folder,"TownRoutes.json"))).mode=="current","Production current");
var prices=new List<object>();
foreach(var t in towns.Values)foreach(var i in items.Values)prices.Add(new {town=t.id,item=i.id,distance=i.MapsFrom(t),price=t.BasePriceFor(i)});
int oracleCases=0;
foreach(var row in snapshot["oracle"].AsArray()){
 var t=towns[(string)row["town"]];var i=items[(string)row["item"]];float opt=t.OptimalFor(i)*(1f+.5f*((int)row["level"]-1));float stock=opt*(float)row["ratio"];float demand=(float)row["demand"];
 Check(MarketMath.Price(t.BasePriceFor(i),opt,stock,demand)==(int)row["buy"],"Buy oracle");
 Check(MarketMath.Buyback(MarketMath.Price(t.BasePriceFor(i),opt,stock+1,demand))==(int)row["sell"],"Sell oracle");oracleCases++;
}
var quotes=new List<object>();
foreach(var a in towns.Values)foreach(var b in towns.Values){
 int d=ContractManager.Distance(a,b),steps=DeliveryReward.RouteSteps(world,a.id,b.id);
 foreach(int weight in Enumerable.Range(2,11))foreach(bool premium in new[]{false,true})foreach(float variation in new[]{.85f,1f,1.15f})foreach(bool enabled in new[]{false,true}){
  quotes.Add(new{from=a.id,to=b.id,distance=d,steps,weight,premium,variation,enabled,hours=Math.Max(1,d)*9+8,reward=DeliveryReward.Quote(enabled?region.deliveryRewards:null,d,steps,weight,premium,variation)});
 }
}
Check(DeliveryReward.RouteSteps(null,"naru","gaegyeong")==-1,"Null world");
Check(DeliveryReward.RouteSteps(world,"missing","gaegyeong")==-1,"Missing market");
var boards=new List<object>();int generated=0;
var db=new GameDatabase {towns=towns.Values.ToList()};
for(int seed=0;seed<100;seed++){
 var manager=new ContractManager(db,new(){TotalHours=7},new(),world,seed);manager.InitBoards();
 boards.Add(new {seed,manager.boards});generated+=manager.boards.Sum(b=>b.list.Count);
}
var active=new Contract {id=9000,fromTown="naru",toTown="gaegyeong",reward=777,rewardRuleVersion=0,quotedRouteSteps=73,hours=33,acceptedAt=5,deadline=38,finalDeadline=54.5f,condition=.8f};
var player=new PlayerState {contracts=new(){active}};
var cm=new ContractManager(db,new(){TotalHours=7},player,world,43);
cm.boards.Add(new(){town="naru",list=new(){active}}); // adversarial old save retains a board alias
string saved=JsonSerializer.Serialize(active,options);cm.InitBoards();
Check(saved==JsonSerializer.Serialize(active,options),"Accepted old contract changed");
// Serialization here is System.Text.Json, not Unity SaveSystem.
var restored=JsonSerializer.Deserialize<Contract>(saved,options);
Check(restored.reward==777&&restored.deadline==38&&restored.finalDeadline==54.5f,"Old contract fields");
File.WriteAllText(output,JsonSerializer.Serialize(new {prices,quotes,boards,oracleCases,generated,acceptedContract=saved},options));
#if !LEGACY
var config=JsonUtility.FromJson<TownDistance.Settings>(File.ReadAllText(args.Length>2?args[2]:Path.Combine(Resources.Folder,"TownRoutes.json")));config.mode="layoutD";
var routes=new List<object>();
foreach(var a in config.towns)foreach(var b in config.towns){
 int d=TownDistance.Between(a.id,b.id,settings:config),rewardD=TownDistance.Between(a.id,b.id,world:world,legacy:TownDistance.LegacyMetric.MapConnections,settings:config);
 Check(d==rewardD,"layoutD metrics diverged");Check(d==TownDistance.Between(b.id,a.id,settings:config),"Route symmetry");
 Check(a.id==b.id?d==0:d>=1,"Distance floor");
 routes.Add(new {from=a.id,to=b.id,seconds=TownDistance.WalkingSeconds(config,a.id,b.id),distance=d,hours=Math.Max(1,d)*9+8,rewardAtWeight4=DeliveryReward.Quote(region.deliveryRewards,d,d,4,false)});
}
foreach(var c in new[]{"bad",null}){var bad=new TownDistance.Settings{mode=c};bool threw=false;try{TownDistance.Validate(bad);}catch(InvalidOperationException){threw=true;}Check(threw,"Bad mode accepted");}
var cache=typeof(TownDistance).GetField("cached",BindingFlags.NonPublic|BindingFlags.Static);
cache.SetValue(null,config);cm.InitBoards();Check(saved==JsonSerializer.Serialize(active,options),"Mode switch changed accepted contract");
Check(items["ginseng"].MapsFrom(towns["naru"])==TownDistance.Between("gaegyeong","naru",settings:config),"Item wrapper");
Check(ContractManager.Distance(towns["naru"],towns["gaegyeong"])==DeliveryReward.RouteSteps(world,"naru","gaegyeong"),"Contract wrappers");
var layoutPrices=new List<object>();
foreach(var town in towns.Values)foreach(var item in items.Values)layoutPrices.Add(new{town=town.id,item=item.id,distance=item.MapsFrom(town),basePrice=town.BasePriceFor(item)});
config.mode="current";cache.SetValue(null,config);
File.WriteAllText(Path.ChangeExtension(output,"layoutD.json"),JsonSerializer.Serialize(new{routes,layoutPrices,checks},options));
#endif
Console.WriteLine($"{checks} checks; {oracleCases} oracle quotes; {quotes.Count} reward cases; {generated} seeded offers. No Unity serializer or engine execution.");
