using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class TransitRegionChecks
    {
        [Serializable] public class Trade { public string item; public int count, cost, revenue, profit; }
        [Serializable] public class Report { public List<string> checks = new List<string>(); public List<Trade> trades = new List<Trade>(); public string scope; }
        [MenuItem("Pocket Caravan/중간권/왕복 시세 검사")]
        public static string Run()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 검사하세요.");
            var db=AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/PocketCaravan/Resources/GameDatabase.asset");
            var report=new Report(); report.scope="실제 MarketState의 초기 재고·출신 능력 없음·시간 회복 없음. 실제 이동 시간/멀티 수익 검증은 별도.";
            foreach(var id in new[]{"transit_resin","transit_driedfish"})
            {
                var item=db.items.Find(i=>i!=null && i.id==id);
                if(item==null || item.originTown==null)throw new Exception("상품 공급지 누락: "+id);
                var dest=db.towns.Find(t=>t!=null && t.region==item.originTown.region && t!=item.originTown);
                if(dest==null || item.MapsFrom(dest)!=2)throw new Exception("중간권 시장 거리 오류");
                foreach(int count in new[]{1,3,5,10})
                {
                    var source=new MarketState(item.originTown,db); var target=new MarketState(dest,db);
                    var t=new Trade{item=id,count=count};
                    for(int n=0;n<count;n++)
                    {
                        if(!source.CanBuy(item))throw new Exception("공급 재고 부족");
                        int buy=source.BuyPrice(item);t.cost+=buy;source.RecordBuy(item,buy);
                        int sell=target.SellPrice(item);t.revenue+=sell;target.RecordSell(item,sell);
                    }
                    t.profit=t.revenue-t.cost;report.trades.Add(t);
                    if(count<=5 && t.profit<=0)throw new Exception("소량 교역의 순이익 없음: "+id+"/"+count);
                }
                report.checks.Add(id+": 공급지/거리2 및 1·3·5개 거래 수수료 후 이익 확인");
            }
            var registry=WorldRegistry.Load();
            foreach(var m in registry.maps) if(m.region.regionId=="transit" && !File.Exists(m.scenePath))throw new Exception("씬 누락: "+m.mapId);
            report.checks.Add("중간권 등록 씬 존재");
            string json=JsonUtility.ToJson(report,true);Directory.CreateDirectory("output/transit");File.WriteAllText("output/transit/economy-checks.json",json);return json;
        }
    }
}
