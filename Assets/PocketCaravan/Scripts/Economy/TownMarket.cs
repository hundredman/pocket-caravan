using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 장터 상인. E로 열면 사고팔기와 이동 수단 구입 창이 뜬다.
    /// 창은 임시 UI(OnGUI)다. 정식 UI가 생기면 MarketState만 그대로 쓰고 화면만 바꾸면 된다.
    /// </summary>
    public class TownMarket : Interactable
    {
        public TownData town;

        bool open;
        int openedFrame;
        Vector2 scroll;
        string message = "";

        MarketState Market => GameManager.I.GetMarket(town);

        public override void Interact(PlayerController player)
        {
            if (town == null) { Debug.LogWarning($"{name}: TownData가 비어 있습니다."); return; }
            open = true;
            openedFrame = Time.frameCount;
            message = "";
            UIState.Blocking = true;
        }

        void Close()
        {
            open = false;
            UIState.Blocking = false;
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            if (InputReader.CancelPressed() || InputReader.InteractPressed()) Close();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (open) Close();
        }

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin();
            var gm = GameManager.I;
            var p = gm.Player;
            var m = Market;

            float w = 420, h = 300;
            var rect = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(rect, "");
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 8, w - 20, h - 16));

            string marketDay = gm.Clock.IsMarketDay ? "  · 오늘은 장날 (값 상승)" : "";
            GUILayout.Label($"<b>{town.displayName} 장터</b>  ({m.LevelName}){marketDay}", Rich);
            GUILayout.Label($"돈 {p.money}   짐 {p.cargo.Weight:0.#}/{p.Capacity:0.#}   누적 거래액 {m.TradeVolume:0}");

            GUILayout.BeginHorizontal();
            GUILayout.Label("품목", GUILayout.Width(90));
            GUILayout.Label("살 때", GUILayout.Width(50));
            GUILayout.Label("팔 때", GUILayout.Width(50));
            GUILayout.Label("재고", GUILayout.Width(40));
            GUILayout.Label("보유", GUILayout.Width(40));
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(150));
            foreach (var item in ItemsShown(p))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.displayName} ({item.weight:0.#})", GUILayout.Width(90));
                int buy = m.BuyPrice(item), sell = m.SellPrice(item);
                GUILayout.Label(m.CanBuy(item) && !item.questItem ? buy.ToString() : "-", GUILayout.Width(50));
                GUILayout.Label(item.questItem ? "맡은 짐" : sell.ToString(), GUILayout.Width(50));
                GUILayout.Label(Mathf.FloorToInt(m.Stock(item)).ToString(), GUILayout.Width(40));
                GUILayout.Label(p.cargo.Count(item).ToString(), GUILayout.Width(40));
                if (GUILayout.Button("사기", GUILayout.Width(44))) Buy(item);
                if (GUILayout.Button("팔기", GUILayout.Width(44))) Sell(item);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            foreach (var v in town.vehiclesForSale)
            {
                if (v == null || v.minTownLevel > m.Level) continue;
                bool owned = p.vehicle == v;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{v.displayName}  적재 {v.capacity:0}  {v.price}", GUILayout.Width(230));
                GUI.enabled = !owned;
                if (GUILayout.Button(owned ? "사용 중" : "구입", GUILayout.Width(70))) BuyVehicle(v);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(message);
            if (GUILayout.Button("닫기 (E)", GUILayout.Width(80))) Close();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 };

        IEnumerable<ItemData> ItemsShown(PlayerState p)
        {
            var seen = new HashSet<ItemData>();
            foreach (var s in town.stock) if (s.item != null && seen.Add(s.item)) yield return s.item;
            foreach (var kv in p.cargo.Items) if (seen.Add(kv.Key)) yield return kv.Key;
        }

        void Buy(ItemData item)
        {
            var p = GameManager.I.Player;
            var m = Market;
            int price = m.BuyPrice(item);
            if (!m.CanBuy(item)) { message = "재고가 없습니다."; return; }
            if (p.money < price) { message = "돈이 모자랍니다."; return; }
            if (!p.CanCarry(item)) { message = "더 실을 수 없습니다."; return; }
            p.money -= price;
            p.cargo.Add(item, 1, price);
            m.RecordBuy(item, price);
            message = $"{item.displayName}을(를) {price}에 샀습니다.";
            GameEvents.RaiseItemBought(item, town, price);
        }

        void Sell(ItemData item)
        {
            var p = GameManager.I.Player;
            var m = Market;
            if (p.cargo.Count(item) <= 0) { message = "가진 것이 없습니다."; return; }
            if (item.questItem) { message = "맡은 물건은 팔 수 없습니다."; return; }
            int price = m.SellPrice(item);
            int profit = Mathf.RoundToInt(price - p.cargo.AverageCost(item));
            p.cargo.Remove(item);
            p.money += price;
            m.RecordSell(item, price);
            message = profit > 0 ? $"{item.displayName}을(를) {price}에 팔았습니다. (이익 {profit})" : $"{item.displayName}을(를) {price}에 팔았습니다.";
            GameEvents.RaiseItemSold(item, town, price, profit);
        }

        void BuyVehicle(VehicleData v)
        {
            var p = GameManager.I.Player;
            if (p.money < v.price) { message = "돈이 모자랍니다."; return; }
            if (p.cargo.Weight > v.capacity) { message = "지금 짐이 너무 많습니다."; return; }
            p.money -= v.price;
            p.vehicle = v;
            message = $"{v.displayName}을(를) 샀습니다.";
            GameEvents.RaiseVehicleBought(v);
        }
    }
}
