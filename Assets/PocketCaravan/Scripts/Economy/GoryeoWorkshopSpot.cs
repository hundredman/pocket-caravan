using UnityEngine;
namespace PocketCaravan
{
    /// <summary>고려 공방 접수. 공용 Crafting의 주문/저장 규칙을 그대로 쓴다.</summary>
    public sealed class GoryeoWorkshopSpot:WorkshopSpot
    {
        public const string Id="goryeo_hanji";
        public string townId="dakmok";
        public int LastQuotedFee {get;private set;}
        public override string ActionVerb=>"공방 이용";
        public override void Interact(PlayerController player)
        {
            var gm=GameManager.I;var r=Crafting.Find(recipeId);
            if(gm==null||r==null||!recipeId.StartsWith("goryeo_")){Notify.Show("지금은 주문을 받지 않습니다.",2f);return;}
            var town=gm.Db.towns.Find(t=>t.id==townId);var item=gm.ItemById(r.product);
            if(town==null||item==null||item.questItem){Notify.Show("지금은 주문을 받지 않습니다.",2f);return;}
            // 고려 공방의 날짜가 바뀌면 전날 횟수를 넘기지 않는다. 공용/타 지역 메모는 건드리지 않는다.
            if(Mathf.RoundToInt(gm.GetMemo("craft:"+recipeId+":day"))!=gm.Clock.day)
            {gm.SetMemo("craft:"+recipeId+":cnt",0);gm.SetMemo("craft:"+recipeId+":day",gm.Clock.day);}
            if(Crafting.State(recipeId)==0)
            {LastQuotedFee=Mathf.Max(18,gm.GetMarket(town).BuyPrice(item)+5);r.fee=LastQuotedFee;}
            base.Interact(player);
        }
    }
}
