using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 떠돌이 상인 (2026-10-27, 제안). 날마다 다른 장터 마을에 선다(VeniceDaily.MerchantMap).
    /// - 오늘의 특가: 베네치아 물건 하나를 기준가의 6할에. 하루 5개까지(VeniceSave "deal_n", 날짜별).
    ///   산 값이 원가로 기록되어 장부·이익 계산이 그대로 맞는다. 장터 시세(공용 MarketState)는 건드리지 않는다.
    /// - 동작 비법서(60냥): 아직 모르는 R 동작 하나. 가면 춤은 카니발 밤에만 판다.
    /// - 사자 문장 소문(30냥): 아직 못 찾은 사자 문장 하나가 어느 맵에 있는지.
    /// </summary>
    public class VeniceMerchant : Interactable
    {
        public override string DisplayName => "떠돌이 상인";
        public override Kind Category => Kind.Market;
        const int MaxDeals = 5;

        SpriteRenderer body;

        void Start()
        {
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sprite = VeniceUtil.Sprite("wanderer");
            body.sortingOrder = 10;
            transform.position += new Vector3(0f, -.5f, 0f);
            var stall = new GameObject("상인 보따리").AddComponent<SpriteRenderer>();
            stall.transform.SetParent(transform, false);
            stall.transform.localPosition = new Vector3(.8f, .1f, 0f);
            stall.sprite = VeniceUtil.Sprite("seed_sack");
            stall.sortingOrder = 9;
        }

        void Update()
        {
            if (body != null) body.transform.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 2.4f) * .02f, 1f);   // 숨쉬기
        }

        static int DealsToday => (int)VeniceSave.Get("deal_n_" + GameManager.I.Clock.day);
        public static int DealPrice(ItemData item) => Mathf.Max(1, Mathf.RoundToInt(item.basePrice * .6f));

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            var item = VeniceUtil.Item(VeniceDaily.DealItem);
            int left = MaxDeals - DealsToday;
            var choices = new System.Collections.Generic.List<VeniceMenu.Option>();
            if (item != null && left > 0)
                choices.Add(new VeniceMenu.Option($"특가 {item.displayName} 1개 ({DealPrice(item)}냥, 남은 {left})", () => BuyDeal(item)));
            bool carnival = VeniceAtmosphere.CarnivalNight;
            choices.Add(new VeniceMenu.Option("동작 비법서 (60냥)", () => BuyBook(carnival)));
            if (VeniceLion.Found < VeniceLion.Maps.Length) choices.Add(new VeniceMenu.Option("사자 문장 소문 (30냥)", BuyRumor));
            choices.Add(new VeniceMenu.Option("그냥 구경만", () => { }));
            string hello = carnival ? "카니발 밤이라 가면 춤 비법서도 가져왔지!" : "오늘은 이 마을에 짐을 풀었소. 내일은 또 다른 데로 가지.";
            VeniceMenu.Show("떠돌이 상인", $"{hello}\n오늘의 특가는 {(item != null ? item.displayName : "없음")}, 기준가의 6할이오.", choices.ToArray());
        }

        public static void BuyDeal(ItemData item)
        {
            var gm = GameManager.I;
            int price = DealPrice(item);
            if (gm.Player.money < price) { Notify.Show("돈이 모자란다.", 2.5f); return; }
            if (!gm.Player.CanCarry(item)) { Notify.Show("짐이 가득 찼다.", 2.5f); return; }
            gm.Player.money -= price;
            gm.Player.cargo.Add(item, 1, price);
            VeniceSave.Set("deal_n_" + gm.Clock.day, DealsToday + 1);
            Sfx.Play("coin");
            Notify.Show($"특가로 {item.displayName}을(를) {price}냥에 샀다 (남은 {MaxDeals - DealsToday})", 3f);
        }

        static void BuyBook(bool carnival)
        {
            var gm = GameManager.I;
            if (gm.Player.money < 60) { Notify.Show("돈이 모자란다.", 2.5f); return; }
            string learned = carnival ? VeniceReactions.Teach("mask") : null;
            if (learned == null) learned = VeniceReactions.TeachRandom();
            if (learned == null) { Notify.Show("이미 모든 동작을 안다.", 2.5f); return; }
            gm.Player.money -= 60;
            Sfx.Play("levelup", .7f);
            Notify.Show($"비법서를 읽고 동작 '{learned}'을(를) 배웠다! (R)", 4f);
        }

        static void BuyRumor()
        {
            var gm = GameManager.I;
            if (gm.Player.money < 30) { Notify.Show("돈이 모자란다.", 2.5f); return; }
            for (int i = 0; i < VeniceLion.Maps.Length; i++)
            {
                if (VeniceSave.Has("lion:" + i)) continue;
                gm.Player.money -= 30;
                Sfx.Play("coin");
                DialogueUI.Show("떠돌이 상인", $"{VeniceDaily.MapName(VeniceLion.Maps[i])} 어딘가, 길에서 가장 먼 구석에 이끼 낀 사자가 있다더군.");
                return;
            }
        }
    }
}
