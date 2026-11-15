using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>Approved D10 coefficients. Haggle limits are available to R3; no random haggle implementation here.</summary>
    public static class ReputationPricing
    {
        static readonly float[] BuyDeltas={.15f,.07f,0,-.03f,-.06f,-.10f};
        static readonly float[] Haggles={0,0,.03f,.05f,.08f,.10f};
        public static ReputationScope Scope(TownData town)=>town.region.townReputation?ReputationScope.Town:ReputationScope.Civilization;
        public static string Target(TownData town)=>town.region.townReputation?town.id:town.region.id;
        public static ReputationState.Entry Entry(PlayerState p,TownData town)=>p!=null&&town!=null&&town.region!=null?p.reputation.Get(Scope(town),Target(town)):new ReputationState.Entry();
        public static float BuyDelta(ReputationStanding stage)=>BuyDeltas[(int)stage];
        public static float MaxHaggle(ReputationStanding stage)=>Haggles[(int)stage];
        public static string BuyDenial(PlayerState p,TownData town,ItemData item)=>item!=null&&item.id=="najeon"&&Entry(p,town).standing<ReputationStanding.Trusted?"평판 신뢰 필요":null;
        public static string Progress(PlayerState p,TownData town)
        {
            var e=Entry(p,town);string target=town==null?"지역":town.region!=null&&!town.region.townReputation?town.region.displayName:town.displayName;
            return target+" "+ReputationState.ProgressText(e);
        }
        public static int Quote(MarketState m,ItemData item,bool selling,PlayerState player,TradeActivity activity,float now,float buyHaggle=0,float sellHaggle=0)
        {
            var stage=Entry(player,m.town).standing;float max=MaxHaggle(stage);
            int buy=Mathf.Max(1,Mathf.RoundToInt(m.BaseBuyPrice(item)*(1+BuyDelta(stage))));
            buy=Mathf.Max(1,Mathf.RoundToInt(buy*(1-Mathf.Clamp(buyHaggle,0,max))));
            int sell=Mathf.Max(1,Mathf.RoundToInt(m.BaseSellPrice(item,player?.origin)*(1+Mathf.Clamp(sellHaggle,0,max))));
            buy=Mathf.Max(buy,sell+1); // simultaneous fully modified quote rule remains
            if(selling)return activity!=null?activity.LimitSale(m.town.id,item.id,now,sell):sell;
            return buy;
        }
        public static void Bought(PlayerState p,TownData town,ItemData item,int price,float hours)=>p.tradeActivity.Bought(town.id,item.id,hours,price);
        public static void Sold(PlayerState p,TownData town,ItemData item,int price,float hours,int day)
        {
            bool restricted=p.tradeActivity.Sold(town.id,item.id,hours);
            bool foreign=item.originTown!=null&&item.originTown.region!=null&&town.region!=null
                && (item.originTown.region.id!=town.region.id || town.region.townReputation&&item.originTown.id!=town.id);
            var size=p.tradeActivity.EligibleSale(town.id,day,price,foreign,restricted);
            if(size!=ReputationSize.None)ReputationAwards.ForTown(town,size,1,"trade:daily");
        }
    }
}
