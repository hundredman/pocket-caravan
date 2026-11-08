#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace PocketCaravan.EditorTools
{
    public sealed class TradeGoodsPlayReview:MonoBehaviour
    {
        const string Out="Docs/Validation/TradeGoods-Task6-2026-11-04";
        [Serializable]class Cycle{public string town,item;public int buy,sell,profit;}
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();public List<Cycle> cycles=new List<Cycle>();public List<Cycle> routes=new List<Cycle>();}
        readonly Report report=new Report();const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        void Check(bool b,string s){if(!b)throw new Exception(s);report.checks.Add(s);}
        void Finish(Exception e){report.complete=e==null;report.error=e==null?"":e.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;}
        IEnumerator Start(){DontDestroyOnLoad(gameObject);var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception e=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception x){e=x;}if(e!=null){Finish(e);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);}
        IEnumerator Photo(string n){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==960&&t.height==540,"960x540 "+n);File.WriteAllBytes(Out+"/"+n+".png",t.EncodeToPNG());Destroy(t);}
        void Trade(TownMarket market,string action,ItemData item){typeof(TownMarket).GetMethod(action,Private).Invoke(market,new object[]{item});}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;var gm=GameManager.I;Check(MapRoot.Current.previewOnly&&!gm.AutosaveEnabled,"save-isolated bootstrap");Time.timeScale=0;
            Check(gm.ItemById("grain")!=null&&gm.ItemById("transit_spice")!=null&&gm.ItemById("transit_wool")!=null,"three registered production icons");
            foreach(var id in new[]{"transit_southport","transit_crossroads"})
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/PocketCaravan/Scenes/Transit/"+id+".unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));for(int i=0;i<10;i++)yield return null;
                Check(!MapRoot.Current.previewOnly&&!gm.AutosaveEnabled,"actual production scene with saves off "+id);gm.Clock.day=2;gm.Clock.minuteOfDay=600;gm.Player.origin=null;gm.Player.money=10000;gm.Player.cargo.Clear();
                foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))g.enabled=false;
                foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",Private).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
                var market=UnityEngine.Object.FindFirstObjectByType<TownMarket>();Check(market.town.id==id,"production market bound "+id);var item=gm.ItemById(id=="transit_southport"?"transit_spice":"transit_wool");
                market.Interact(PlayerController.Instance);typeof(TownMarket).GetField("selected",Private).SetValue(market,item);typeof(TownMarket).GetField("listScroll",Private).SetValue(market,48f);yield return Photo(id+"-market");
                int routeStart=gm.Player.money;Trade(market,"Buy",item);Check(gm.Player.cargo.Count(item)==1,"supplier purchase "+item.id);int routeBuy=routeStart-gm.Player.money;
                market.town=gm.Db.towns.Find(t=>t.id=="gaegyeong");Trade(market,"Sell",item);int routeProfit=gm.Player.money-routeStart;
                Check(gm.Player.cargo.Count(item)==0&&routeProfit>0,"new supply route to capital profitable "+item.id);report.routes.Add(new Cycle{town=id+" -> gaegyeong",item=item.id,buy=routeBuy,sell=routeBuy+routeProfit,profit=routeProfit});market.town=gm.Db.towns.Find(t=>t.id==id);
                foreach(var townId in new[]{id,"gaegyeong"})
                {
                    market.town=gm.Db.towns.Find(t=>t.id==townId);
                    foreach(var goodId in new[]{"transit_spice","transit_wool"})
                    {
                        var good=gm.ItemById(goodId);gm.Player.cargo.Clear();int money=gm.Player.money;int buy=gm.GetMarket(market.town).BuyPrice(good);Trade(market,"Buy",good);Check(gm.Player.cargo.Count(good)==1,"real buy "+townId+" "+goodId);
                        int sell=gm.GetMarket(market.town).SellPrice(good);Trade(market,"Sell",good);Check(gm.Player.cargo.Count(good)==0&&gm.Player.money<=money,"recalculated same-town resale no profit "+townId+" "+goodId);
                        report.cycles.Add(new Cycle{town=townId,item=goodId,buy=buy,sell=sell,profit=gm.Player.money-money});
                    }
                }
                market.town=gm.Db.towns.Find(t=>t.id==id);typeof(TownMarket).GetMethod("Close",Private).Invoke(market,null);
            }
            var map=MapRoot.Current;var player=PlayerController.Instance;
            var old=new SaveData{version=2,layoutVersion=GoryeoLayout.Current,scene=map.mapId,mapHeight=WorldIndex.Get().Map(map.mapId).height,px=player.transform.position.x,py=player.transform.position.y,money=1234,stamina=1,day=2,minute=600,origin=gm.Db.startOrigin.id,vehicle=gm.Db.startVehicle.id};
            old.cargoItems.Add("grain");old.cargoCounts.Add(2);old.cargoCosts.Add(16);
            bool Restore(SaveData d)=>(bool)typeof(GameManager).GetMethod("TryApplySave",Private).Invoke(gm,new object[]{JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d))});
            Check(Restore(old)&&gm.Player.money==1234&&gm.Player.cargo.Count(gm.ItemById("grain"))==2,"legacy v2 save without new goods still restores");
            var spice=gm.ItemById("transit_spice");var wool=gm.ItemById("transit_wool");old.cargoItems.AddRange(new[]{spice.id,wool.id});old.cargoCounts.AddRange(new[]{1,2});old.cargoCosts.AddRange(new[]{14f,20f});
            var south=gm.Db.towns.Find(t=>t.id=="transit_southport");var state=new MarketState(south,gm.Db);float stock=state.Stock(spice);state.RecordBuy(spice,14);old.markets.Add(state.Export());
            Check(Restore(old)&&gm.Player.cargo.Count(spice)==1&&gm.Player.cargo.Count(wool)==2&&gm.GetMarket(south).Stock(spice)==stock-1,"new goods and market stock use existing v2 save format");
            var inv=UnityEngine.Object.FindFirstObjectByType<InventoryUI>();UIState.Blocking=false;inv.Open();yield return Photo("registered-inventory");inv.Close();
            Check(!gm.AutosaveEnabled,"saves remain disabled");
        }
    }
}
#endif
