using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 장터 상인. Space로 열면 사고팔기와 이동 수단 구입 창이 뜬다.
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

        public override string DisplayName => town != null ? $"{town.displayName} 상인" : "장터 상인";
        public override Kind Category => Kind.Market;

        public override void Interact(PlayerController player)
        {
            if (town == null) { Debug.LogWarning($"{name}: TownData가 비어 있습니다."); return; }
            open = true;
            openedFrame = Time.frameCount;
            message = "";
            UIState.Blocking = true;
            Sfx.Play("open");
        }

        void Close()
        {
            open = false;
            UIState.Release();
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
            GUI.depth = -10;   // 다른 글자들 위에
            var gm = GameManager.I;
            var p = gm.Player;
            var m = Market;

            float w = 440, h = 300;
            var rect = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(rect, "", UIStyle.Box);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 8, w - 24, h - 16));

            string marketDay = gm.Clock.IsMarketDay ? "  <color=#ffd84a>· 오늘은 장날 (값 상승)</color>" : "";
            int nextLevel = m.Level < gm.Db.maxTownLevel && m.Level < gm.Db.townLevelVolume.Length ? gm.Db.townLevelVolume[m.Level] : 0;
            string growth = nextLevel > 0 ? $"   다음 등급까지 거래 {Mathf.Max(0, nextLevel - Mathf.RoundToInt(m.TradeVolume))}냥" : "";
            GUILayout.Label($"<color=#FFE08A>{town.displayName} 장터</color>  ({m.LevelName}){marketDay}", UIStyle.Rich);
            GUILayout.Label($"돈 {p.money}   짐 {p.TotalWeight:0.#}/{p.Capacity:0.#}{growth}", UIStyle.Small);
            if (p.credit < 4) GUILayout.Label(BeginnerTip(gm), UIStyle.Small);

            GUILayout.BeginHorizontal();
            GUILayout.Label("품목 (무게)", UIStyle.Small, GUILayout.Width(118));
            GUILayout.Label("살 때", UIStyle.Small, GUILayout.Width(44));
            GUILayout.Label("팔 때", UIStyle.Small, GUILayout.Width(44));
            GUILayout.Label("재고", UIStyle.Small, GUILayout.Width(34));
            GUILayout.Label("보유", UIStyle.Small, GUILayout.Width(34));
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(156));
            foreach (var item in ItemsShown(p))
            {
                GUILayout.BeginHorizontal(GUILayout.Height(18));
                var iconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
                if (item.icon != null) DrawSprite(iconRect, item.icon);
                GUILayout.Label($"{item.displayName} ({item.weight:0.#})", GUILayout.Width(98));
                int buy = m.BuyPrice(item), sell = m.SellPrice(item);
                float avg = p.cargo.AverageCost(item);
                string sellText = item.questItem ? "맡은 짐" : (p.cargo.Count(item) > 0 && avg > 0 ? (sell > avg ? $"<color=#a8f0a0>{sell}</color>" : $"<color=#ff9a8a>{sell}</color>") : sell.ToString());
                GUILayout.Label(m.CanBuy(item) && !item.questItem ? buy.ToString() : "-", GUILayout.Width(44));
                GUILayout.Label(sellText, UIStyle.Rich, GUILayout.Width(44));
                GUILayout.Label(Mathf.FloorToInt(m.Stock(item)).ToString(), GUILayout.Width(34));
                GUILayout.Label(p.cargo.Count(item).ToString(), GUILayout.Width(34));
                if (GUILayout.Button("사기", UIStyle.Button, GUILayout.Width(40), GUILayout.Height(16))) Buy(item);
                if (GUILayout.Button("팔기", UIStyle.Button, GUILayout.Width(40), GUILayout.Height(16))) Sell(item);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            foreach (var v in town.vehiclesForSale)
            {
                if (v == null || v.minTownLevel > m.Level) continue;
                bool owned = p.vehicle == v;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{v.displayName}  적재 {v.capacity:0}  {v.price}냥", GUILayout.Width(230));
                GUI.enabled = !owned;
                if (GUILayout.Button(owned ? "사용 중" : "구입", UIStyle.Button, GUILayout.Width(70), GUILayout.Height(18))) BuyVehicle(v);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(message, UIStyle.Wrap);
            if (GUILayout.Button("닫기 (Esc)", UIStyle.Button, GUILayout.Width(80), GUILayout.Height(18))) Close();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>초보 상인용 장사 팁: 이 마을 특산과 비싸게 받는 곳</summary>
        string BeginnerTip(GameManager gm)
        {
            var local = new List<string>();
            foreach (var it in gm.Db.items) if (it != null && !it.questItem && it.originTown == town) local.Add(it.displayName);
            string here = local.Count > 0 ? $"이 마을 특산 {Josa.Eun(string.Join("·", local))} 여기서 싸다." : "이 마을은 특산이 없어 다른 곳 물건이 비싸다.";
            return $"<color=#ffd84a>팁</color> {here} 산지에서 멀리 가져갈수록 비싸게 팔린다. 한 곳에 몰아 팔면 값이 떨어진다.";
        }

        /// <summary>스프라이트 한 장을 GUI 사각형에 그린다 (아틀라스 안의 위치를 고려)</summary>
        public static void DrawSprite(Rect r, Sprite s)
        {
            var t = s.texture;
            var tr = s.textureRect;
            GUI.DrawTextureWithTexCoords(r, t, new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height));
        }

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
            if (!m.CanBuy(item)) { message = "재고가 없습니다."; Sfx.Play("cancel"); return; }
            if (p.money < price) { message = "돈이 모자랍니다."; Sfx.Play("cancel"); return; }
            if (!p.CanCarry(item)) { message = "더 실을 수 없습니다."; Sfx.Play("cancel"); return; }
            p.money -= price;
            p.cargo.Add(item, 1, price);
            m.RecordBuy(item, price);
            message = $"{Josa.Eul(item.displayName)} {price}에 샀습니다.";
            Sfx.Play("coin");
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
            Sfx.Play("coin");
            message = profit > 0 ? $"{Josa.Eul(item.displayName)} {price}에 팔았습니다. (이익 {profit})" : $"{Josa.Eul(item.displayName)} {price}에 팔았습니다.";
            GameEvents.RaiseItemSold(item, town, price, profit);
        }

        void BuyVehicle(VehicleData v)
        {
            var p = GameManager.I.Player;
            if (p.money < v.price) { message = "돈이 모자랍니다."; Sfx.Play("cancel"); return; }
            if (p.TotalWeight > v.capacity) { message = "지금 짐이 너무 많습니다."; Sfx.Play("cancel"); return; }
            p.money -= v.price;
            p.vehicle = v;
            message = $"{Josa.Eul(v.displayName)} 샀습니다.";
            Sfx.Play("levelup");
            GameEvents.RaiseVehicleBought(v);
        }
    }
}
