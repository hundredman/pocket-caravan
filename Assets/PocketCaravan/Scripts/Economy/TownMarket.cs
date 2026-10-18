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
        ItemData selected;
        int soldThisVisit, revenueThisVisit, profitThisVisit;

        MarketState Market => GameManager.I.GetMarket(town);

        public override string DisplayName => town != null ? $"{town.displayName} 상인" : "장터 상인";
        public override Kind Category => Kind.Market;

        public override void Interact(PlayerController player)
        {
            if (town == null) { Debug.LogWarning($"{name}: TownData가 비어 있습니다."); return; }
            open = true;
            openedFrame = Time.frameCount;
            message = "";
            soldThisVisit = revenueThisVisit = profitThisVisit = 0;
            if (selected == null) foreach (var item in ItemsShown(GameManager.I.Player)) { selected = item; break; }
            Observe();
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

            float w = Mathf.Min(548, UIStyle.VirtualWidth - 16), h = 340;
            var rect = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(rect, "", UIStyle.Box);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 8, w - 24, h - 56));

            string marketDay = gm.Clock.IsMarketDay ? "  <color=#ffd84a>· 오늘은 장날 (값 상승)</color>" : "";
            int nextLevel = m.Level < gm.Db.maxTownLevel && m.Level < gm.Db.townLevelVolume.Length ? gm.Db.townLevelVolume[m.Level] : 0;
            string growth = nextLevel > 0 ? $"   다음 등급까지 거래 {Mathf.Max(0, nextLevel - Mathf.RoundToInt(m.TradeVolume))}냥" : "";
            GUILayout.Label($"<color=#FFE08A>{town.displayName} 장터</color>  ({m.LevelName}){marketDay}", UIStyle.Rich);
            GUILayout.Label($"돈 {p.money}   짐 {p.TotalWeight:0.#}/{p.Capacity:0.#}{growth}", UIStyle.Small);
            GUILayout.Label(LocalGoods(gm), UIStyle.Small);

            GUILayout.BeginHorizontal();
            GUILayout.Label("품목 선택 / 무게", UIStyle.Small, GUILayout.Width(144));
            GUILayout.Label("살 때", UIStyle.Small, GUILayout.Width(44));
            GUILayout.Label("팔 때", UIStyle.Small, GUILayout.Width(44));
            GUILayout.Label("재고", UIStyle.Small, GUILayout.Width(34));
            GUILayout.Label("보유", UIStyle.Small, GUILayout.Width(34));
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(94));
            foreach (var item in ItemsShown(p))
            {
                GUILayout.BeginHorizontal(GUILayout.Height(18));
                var iconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
                if (item.icon != null) DrawSprite(iconRect, item.icon);
                if (GUILayout.Button((selected == item ? "▸ " : "") + $"{item.displayName} ({item.weight:0.#})",
                    UIStyle.Button, GUILayout.Width(124), GUILayout.Height(18))) selected = item;
                int buy = m.BuyPrice(item), sell = m.SellPrice(item);
                float avg = p.cargo.AverageCost(item);
                string sellText = item.questItem ? "맡은 짐" : (p.cargo.Count(item) > 0 && avg > 0 ? (sell > avg ? $"<color=#a8f0a0>{sell}</color>" : $"<color=#ff9a8a>{sell}</color>") : sell.ToString());
                GUILayout.Label(m.CanBuy(item) && !item.questItem ? buy.ToString() : "-", GUILayout.Width(44));
                GUILayout.Label(sellText, UIStyle.Rich, GUILayout.Width(44));
                GUILayout.Label(Mathf.FloorToInt(m.Stock(item)).ToString(), GUILayout.Width(34));
                GUILayout.Label(p.cargo.Count(item).ToString(), GUILayout.Width(34));
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && !item.questItem && m.CanBuy(item) && p.money >= buy && p.CanCarry(item);
                if (GUILayout.Button("사기", UIStyle.Button, GUILayout.Width(40), GUILayout.Height(16))) Buy(item);
                GUI.enabled = enabled && !item.questItem && p.cargo.Count(item) > 0;
                if (GUILayout.Button("팔기", UIStyle.Button, GUILayout.Width(40), GUILayout.Height(16))) Sell(item);
                GUI.enabled = enabled;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            DrawTradeInfo(gm, m);

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

            if (soldThisVisit > 0)
                GUILayout.Label($"이번 판매 {soldThisVisit}개 · 받은 돈 {revenueThisVisit}냥 · 손익 {Signed(profitThisVisit)}냥", UIStyle.Small);
            GUILayout.EndArea();
            GUI.Label(new Rect(rect.x + 12, rect.yMax - 36, w - 242, 30), message, UIStyle.Wrap);
            if (town.facilities.Count > 0 && GUI.Button(new Rect(rect.xMax - 213, rect.yMax - 30, 110, 20), "마을 시설", UIStyle.Button))
            { Close(); TownFacilitiesUI.Show(town); }
            if (GUI.Button(new Rect(rect.xMax - 94, rect.yMax - 30, 82, 20), "닫기 (Esc)", UIStyle.Button)) Close();
        }

        void Observe()
        {
            var gm = GameManager.I;
            gm.Player.ledger.Observe(town, Market, ItemsShown(gm.Player));
        }

        static string Signed(float value) => value.ToString("+0.#;-0.#;0");

        string LocalGoods(GameManager gm)
        {
            var local = new List<string>();
            foreach (var item in gm.Db.items)
                if (item != null && !item.questItem && item.originTown == town) local.Add(item.displayName);
            return local.Count > 0 ? "이곳 특산: " + string.Join(" · ", local) + "  |  품목을 눌러 원가와 판매처 확인"
                : "품목을 눌러 원가와 판매처 확인";
        }

        void DrawTradeInfo(GameManager gm, MarketState market)
        {
            var panel = GUILayoutUtility.GetRect(10, 62, GUILayout.ExpandWidth(true));
            GUI.Box(panel, "", UIStyle.Box);
            float y = panel.y + 5;
            void Line(string text)
            {
                GUI.Label(new Rect(panel.x + 5, y, panel.width - 10, 12), text, UIStyle.Small);
                y += 13;
            }
            if (selected == null || selected.questItem)
                Line("교역품을 선택하면 원가와 직접 확인한 시세를 볼 수 있습니다.");
            else
            {
                var p = gm.Player;
                string origin = selected.originTown != null ? selected.originTown.displayName : "여러 마을";
                string owned = p.cargo.Count(selected) > 0
                    ? $" · 내 원가 {p.cargo.AverageCost(selected):0.#} · 이곳 손익 {Signed(market.SellPrice(selected) - p.cargo.AverageCost(selected))}냥/개" : "";
                Line($"{selected.displayName} · 기준 공급지 {origin}{owned}");
                bool any = false;
                int shown = 0;
                var observed = gm.Db.towns.FindAll(other => other != null && other != town && p.ledger.Find(other.id, selected.id) != null);
                observed.Sort((a, b) => p.ledger.Find(b.id, selected.id).sell.CompareTo(p.ledger.Find(a.id, selected.id).sell));
                foreach (var other in observed)
                {
                    var q = p.ledger.Find(other.id, selected.id);
                    any = true;
                    Line($"{other.displayName}: 확인했던 판매가 {q.sell}냥 · 현재 구입가 대비 {Signed(q.sell - market.BuyPrice(selected))}냥/개");
                    if (++shown == 2) break;
                }
                if (!any) Line("다른 장터를 방문하면 그곳에서 확인한 시세가 장부에 남습니다.");
                Line("장부는 마지막 확인값 · 첫 1개 기준 참고 · 도착 시 시세와 다를 수 있습니다.");
            }
        }

        /// <summary>스프라이트 한 장을 GUI 사각형에 그린다 (아틀라스 안의 위치를 고려)</summary>
        public static void DrawSprite(Rect r, Sprite s)
        {
            var t = s.texture;
            var tr = s.textureRect;
            GUI.DrawTextureWithTexCoords(r, t, new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height));
        }

        List<ItemData> ItemsShown(PlayerState p)
        {
            // 버튼 처리 중 화물/재고가 바뀌어도 목록 순회를 깨뜨리지 않도록 스냅샷을 만든다.
            var seen = new HashSet<ItemData>();
            var result = new List<ItemData>();
            foreach (var s in town.stock) if (s.item != null && seen.Add(s.item)) result.Add(s.item);
            foreach (var item in Market.KnownItems) if (item != null && seen.Add(item)) result.Add(item);
            foreach (var kv in p.cargo.Items) if (seen.Add(kv.Key)) result.Add(kv.Key);
            return result;
        }

        void Buy(ItemData item)
        {
            if (item == null || item.questItem) { message = "맡은 짐은 교역할 수 없습니다."; return; }
            var p = GameManager.I.Player;
            var m = Market;
            int price = m.BuyPrice(item);
            if (!m.CanBuy(item)) { message = "재고가 없습니다."; Sfx.Play("cancel"); return; }
            if (p.money < price) { message = "돈이 모자랍니다."; Sfx.Play("cancel"); return; }
            if (!p.CanCarry(item)) { message = "더 실을 수 없습니다."; Sfx.Play("cancel"); return; }
            p.money -= price;
            p.cargo.Add(item, 1, price);
            m.RecordBuy(item, price);
            selected = item;
            Observe();
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
            selected = item;
            Observe();
            soldThisVisit++;
            revenueThisVisit += price;
            profitThisVisit += profit;
            Sfx.Play("coin");
            message = $"{Josa.Eul(item.displayName)} {price}냥에 팔았습니다. 손익 {Signed(profit)}냥.";
            GameEvents.RaiseItemSold(item, town, price, profit);
        }

        void BuyVehicle(VehicleData v)
        {
            var p = GameManager.I.Player;
            if (p.vehicle == v) { message = "이미 사용 중인 이동 수단입니다."; return; }
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
