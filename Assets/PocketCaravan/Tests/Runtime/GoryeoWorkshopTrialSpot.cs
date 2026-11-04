#if UNITY_EDITOR
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    /// <summary>Preview-only candidate; installs/removes only its own temporary recipe.</summary>
    public sealed class GoryeoWorkshopTrialSpot:WorkshopSpot
    {
        Crafting.Recipe owned;
        public const string Id="goryeo_hanji";
        public override string ActionVerb=>"공방 이용";
        public int LastQuotedFee {get;private set;}
        void Start()
        {
            if(MapRoot.Current==null||!MapRoot.Current.previewOnly){enabled=false;return;}
            recipeId=Id;blocksMovement=false;
            if(Crafting.Find(Id)!=null)return;
            owned=new Crafting.Recipe{id=Id,shop="닥목 한지 공방",worker="한지 장인",product="paper",productName="종이",fee=18,hours=6,maxBatch=1,dailyMax=2,
                hello="재료는 공방에서 마련합니다. 삯을 맡기면 종이 한 묶음을 떠서 말려 드리지요."};
            Crafting.Recipes.Add(owned);
        }
        public override void Interact(PlayerController player)
        {
            if(MapRoot.Current==null||!MapRoot.Current.previewOnly)return;
            var gm=GameManager.I;var r=Crafting.Find(Id);if(gm==null||r==null)return;
            if(Mathf.RoundToInt(gm.GetMemo("craft:"+Id+":day"))!=gm.Clock.day)
            {gm.SetMemo("craft:"+Id+":cnt",0);gm.SetMemo("craft:"+Id+":day",gm.Clock.day);}
            if(Crafting.State(Id)==0)
            {
                var town=gm.Db.towns.Find(t=>t.id=="dakmok");var item=gm.ItemById("paper");
                LastQuotedFee=Mathf.Max(18,gm.GetMarket(town).BuyPrice(item)+5);r.fee=LastQuotedFee;
            }
            base.Interact(player);
        }
        void OnDestroy(){if(owned!=null)Crafting.Recipes.Remove(owned);}
    }
}
#endif
