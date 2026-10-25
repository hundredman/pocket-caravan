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
        int soldThisVisit, revenueThisVisit;
        float profitThisVisit;
        int marketTab, quantityMode, customQuantity = 1;
        int RequestedCount => quantityMode==0?1:quantityMode==1?5:quantityMode==2?MarketState.MaxBatch:Mathf.Clamp(customQuantity,1,MarketState.MaxBatch);
        Vector2 vehicleScroll;

        MarketState Market => GameManager.I.GetMarket(town);

        public override string DisplayName => town != null ? $"{town.displayName} 상인" : "장터 상인";
        public override Kind Category => Kind.Market;

        public override void Interact(PlayerController player)
        {
            if (town == null) { Debug.LogWarning($"{name}: TownData가 비어 있습니다."); return; }
            open = true;
            openedFrame = Time.frameCount;
            message = "";
            soldThisVisit = revenueThisVisit = 0; profitThisVisit = 0;
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
            if (InputReader.LedgerPressed()) { OpenLedger(); return; }
            if (InputReader.CancelPressed() || InputReader.InteractPressed()) Close();
        }

        // Close before opening the next modal; its opening-frame guard consumes the same L press.
        bool OpenLedger()
        {
            var ledger=GameManager.I!=null?GameManager.I.GetComponent<TradeLedgerUI>():null;
            if(!open || ledger==null)return false;
            Close();
            ledger.Open();
            return ledger.IsOpen;
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

            float w = Mathf.Min(548, UIStyle.VirtualWidth - 16), h = 350;
            var rect = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(rect, "", UIStyle.Box);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 8, w - 24, h - 56));

            string marketDay = gm.Clock.IsMarketDay ? "  <color=#ffd84a>· 오늘은 장날 (값 상승)</color>" : "";
            int nextLevel = m.Level < gm.Db.maxTownLevel && m.Level < gm.Db.townLevelVolume.Length ? gm.Db.townLevelVolume[m.Level] : 0;
            string growth = nextLevel > 0 ? $"   다음 등급까지 거래 {Mathf.Max(0, nextLevel - Mathf.RoundToInt(m.TradeVolume))}냥" : "";
            GUILayout.Label($"<color=#FFE08A>{town.displayName} 장터</color>  ({m.LevelName}){marketDay}", UIStyle.Rich);
            GUILayout.Label($"돈 {p.money}   짐 {p.TotalWeight:0.#}/{p.Capacity:0.#}{growth}", UIStyle.Small);
            if(town.vehiclesForSale.Count>0)
            {
                GUILayout.BeginHorizontal();
                if(GUILayout.Button((marketTab==0?"▸ ":"")+"상품 거래",UIStyle.Button))marketTab=0;
                if(GUILayout.Button((marketTab==1?"▸ ":"")+"운반 수단",UIStyle.Button))marketTab=1;
                GUILayout.EndHorizontal();
            }
            else marketTab=0;
            if(marketTab==1) DrawVehicles(p,m);
            else
            {
                GUILayout.Label(ReputationPricing.Progress(p,town),UIStyle.Small);
            GUILayout.Label(LocalGoods(gm), UIStyle.Small);
                GUILayout.BeginHorizontal();
                GUILayout.Label("품목 선택 / 무게", UIStyle.Small, GUILayout.Width(144));
                GUILayout.Label("살 때", UIStyle.Small, GUILayout.Width(44));
                GUILayout.Label("팔 때", UIStyle.Small, GUILayout.Width(44));
                GUILayout.Label("재고", UIStyle.Small, GUILayout.Width(34));
                GUILayout.Label("보유", UIStyle.Small, GUILayout.Width(34));
                GUILayout.EndHorizontal();

                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(42));
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
                    string denial=ReputationPricing.BuyDenial(p,town,item);
                    GUILayout.Label(denial!=null?"신뢰 필요":m.CanBuy(item) && !item.questItem ? buy.ToString() : "-",UIStyle.Small,GUILayout.Width(44));
                    GUILayout.Label(sellText, UIStyle.Rich, GUILayout.Width(44));
                    GUILayout.Label(Mathf.FloorToInt(m.Stock(item)).ToString(), GUILayout.Width(34));
                    GUILayout.Label(p.cargo.Count(item).ToString(), GUILayout.Width(34));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                DrawTradeInfo(gm, m);

                DrawBatch(gm);
            }
            GUILayout.EndArea();
            GUI.Label(new Rect(rect.x + 12, rect.yMax - 48, w - 242, soldThisVisit>0?28:42), message, UIStyle.Wrap);
            if(soldThisVisit>0)GUI.Label(new Rect(rect.x+12,rect.yMax-18,w-242,12),$"이번 판매 {soldThisVisit}개 · 수령 {revenueThisVisit} · 손익 {Signed(profitThisVisit)}",UIStyle.Small);
            if (town.facilities.Count > 0 && GUI.Button(new Rect(rect.xMax - 213, rect.yMax - 30, 110, 20), "마을 시설", UIStyle.Button))
            { Close(); TownFacilitiesUI.Show(town); }
            if (GUI.Button(new Rect(rect.xMax - 94, rect.yMax - 30, 82, 20), "닫기 (Esc)", UIStyle.Button)) Close();
        }

        void DrawBatch(GameManager gm)
        {
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("-",UIStyle.Button,GUILayout.Width(24),GUILayout.Height(18))){customQuantity=Mathf.Max(1,RequestedCount-1);quantityMode=3;}
            GUILayout.Label($"{RequestedCount}개까지",UIStyle.Small,GUILayout.Width(58));
            if(GUILayout.Button("+",UIStyle.Button,GUILayout.Width(24),GUILayout.Height(18))){customQuantity=Mathf.Min(MarketState.MaxBatch,RequestedCount+1);quantityMode=3;}
            string[] modes={"1개","최대5개","최대100개"};
            for(int i=0;i<modes.Length;i++)if(GUILayout.Button((quantityMode==i?"▸ ":"")+modes[i],UIStyle.Button,GUILayout.Height(18)))quantityMode=i;
            GUILayout.EndHorizontal();
            if(selected==null || selected.questItem)return;
            int requested=RequestedCount;
            var buy=Market.Preview(selected,gm.Player,requested,false);
            var sell=Market.Preview(selected,gm.Player,requested,true);
            GUILayout.BeginHorizontal();
            GUILayout.Label(buy.count>0?$"{buy.count}개 총 {buy.total}냥 · 남는 돈 {gm.Player.money-buy.total} · 보상 별도":buy.blocker,UIStyle.Small);
            GUI.enabled=buy.count>0;
            if(GUILayout.Button($"{buy.count}개 사기",UIStyle.Button,GUILayout.Width(86),GUILayout.Height(18)))TradeBatch(buy);
            GUI.enabled=true;GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(sell.count>0?$"{sell.count}개 수령 {sell.total}냥 · 매매 손익 {Signed(sell.profit)}냥":sell.blocker,UIStyle.Small);
            GUI.enabled=sell.count>0;
            if(GUILayout.Button($"{sell.count}개 팔기",UIStyle.Button,GUILayout.Width(86),GUILayout.Height(18)))TradeBatch(sell);
            GUI.enabled=true;GUILayout.EndHorizontal();
        }

        void DrawVehicles(PlayerState player,MarketState market)
        {
            GUILayout.Label("수단 가격 외에 다음 상품을 살 돈도 남겨 두세요. 배달 짐도 적재 공간을 씁니다.",UIStyle.Wrap);
            vehicleScroll=GUILayout.BeginScrollView(vehicleScroll,GUILayout.Height(198));
            foreach(var v in town.vehiclesForSale)
            {
                if(v==null || v.minTownLevel>market.Level)continue;
                bool owned=player.vehicle==v;
                GUILayout.Label($"{v.displayName} · 적재 {v.capacity:0} · 가격 {v.price}냥",UIStyle.Rich);
                int remaining=player.money-v.price;
                GUILayout.Label(owned?"현재 사용 중":remaining>=0?$"구입 후 보유금 {remaining}냥 · 상품 매입비는 별도":"돈이 부족합니다.",UIStyle.Small);
                if(!owned && remaining==0)GUILayout.Label("매입 자금이 남지 않습니다. 일반 배달로 자금을 마련할 수 있습니다.",UIStyle.Wrap);
                GUI.enabled=!owned && remaining>=0 && player.TotalWeight<=v.capacity;
                if(GUILayout.Button(owned?"사용 중":"구입",UIStyle.Button,GUILayout.Height(20)))BuyVehicle(v);
                GUI.enabled=true;GUILayout.Space(6);
            }
            GUILayout.EndScrollView();
        }

        // 각 단위 거래는 기존 규칙/이벤트를 유지한다. 실행 직전 견적이 달라졌으면 시작하지 않는다.
        int TradeBatch(MarketEstimate estimate)
        {
            var p=GameManager.I.Player; var m=Market;
            if(estimate==null || estimate.town!=town || estimate.count<1 || !estimate.Matches(m.Preview(estimate.item,p,estimate.count,estimate.selling)))
            { message="시세·잔고·화물이 달라졌습니다. 수량과 총액을 다시 확인하세요.";return 0; }
            int completed=0,total=0;float basis=p.cargo.AverageCost(estimate.item);
            for(int i=0;i<estimate.count;i++)
            {
                int price=estimate.selling?m.SellPrice(estimate.item):m.BuyPrice(estimate.item);
                if(price!=estimate.PriceAt(i))break;
                if(estimate.selling)
                {
                    if(p.cargo.Count(estimate.item)<1 || (long)p.money+price>int.MaxValue)break;
                    Sell(estimate.item);
                }
                else
                {
                    if(!m.CanBuy(estimate.item) || p.money<price || !p.CanCarry(estimate.item))break;
                    Buy(estimate.item);
                }
                completed++;total+=price;
            }
            string partial=completed<estimate.count?" · 도중 조건 변경으로 중단":"";
            message=estimate.selling?$"{completed}개 판매 · {total}냥 · 매매 손익 {Signed(total-basis*completed)}냥{partial}":$"{completed}개 매입 · {total}냥 · 남은 돈 {p.money}냥{partial}";
            return completed;
        }

        void Observe()
        {
            var gm = GameManager.I;
            gm.Player.ledger.Observe(town, Market, ItemsShown(gm.Player));
        }

        static string Signed(float value) => value.ToString("+0.#;-0.#;0");

        string LocalGoods(GameManager gm)
        {
            var supply=new List<string>();var demand=new List<string>();
            foreach(var entry in town.stock)
            {
                if(entry.item==null || entry.item.questItem)continue;
                float factor=entry.localPriceMultiplier;
                if(factor>0 && factor<1 && supply.Count<2)supply.Add(entry.item.displayName);
                else if(factor>1 && !float.IsInfinity(factor) && demand.Count<2)demand.Add(entry.item.displayName);
            }
            if(supply.Count>0 || demand.Count>0)
                return (supply.Count>0?"공급: "+string.Join(" · ",supply):"")+(supply.Count>0&&demand.Count>0?" / ":"")
                    +(demand.Count>0?"수요: "+string.Join(" · ",demand):"")+" · 실제 시세는 표에서 확인";
            var local = new List<string>();
            foreach (var item in gm.Db.items)
                if (item != null && !item.questItem && item.originTown == town) local.Add(item.displayName);
            return local.Count > 0 ? "기준 공급품: " + string.Join(" · ", local) + " · 시세는 품목별로 확인"
                : "품목을 눌러 원가와 판매처 확인";
        }

        void DrawTradeInfo(GameManager gm, MarketState market)
        {
            var panel = GUILayoutUtility.GetRect(10, 50, GUILayout.ExpandWidth(true));
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
                Line($"{selected.displayName} · 기준가 지역 {origin}{owned}");
                bool any = false;
                int shown = 0;
                var observed = gm.Db.towns.FindAll(other => other != null && other != town && p.ledger.Find(other.id, selected.id) != null);
                observed.Sort((a, b) => p.ledger.Find(b.id, selected.id).sell.CompareTo(p.ledger.Find(a.id, selected.id).sell));
                foreach (var other in observed)
                {
                    var q = p.ledger.Find(other.id, selected.id);
                    any = true;
                    Line($"{other.displayName}: 확인했던 판매가 {q.sell}냥 · 현재 구입가 대비 {Signed(q.sell - market.BuyPrice(selected))}냥/개");
                    if (++shown == 1) break;
                }
                if (!any) Line("다른 장터를 방문하면 그곳에서 확인한 시세가 장부에 남습니다.");
                Line(p.tradeActivity.Restricted(town.id,selected.id,gm.Clock.TotalHours)?"최근 매입분 · 12시간 동안 되팔기 금액 제한":"장부는 과거 첫1개 시세 · L 전체 비교 · 도착 시 변동할 수 있습니다.");
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
            string denial=ReputationPricing.BuyDenial(p,town,item);
            if(denial!=null){message=denial;Sfx.Play("cancel");return;}
            int price = m.BuyPrice(item);
            if (!m.CanBuy(item)) { message = "재고가 없습니다."; Sfx.Play("cancel"); return; }
            if (p.money < price) { message = "돈이 모자랍니다."; Sfx.Play("cancel"); return; }
            if (!p.CanCarry(item)) { message = "더 실을 수 없습니다."; Sfx.Play("cancel"); return; }
            p.money -= price;
            p.cargo.Add(item, 1, price);
            m.RecordBuy(item, price);
            ReputationPricing.Bought(p,town,item,price,GameManager.I.Clock.TotalHours);
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
            if((long)p.money+price>int.MaxValue) { message="보유금 한도를 확인하세요.";return; }
            float averageCost = p.cargo.AverageCost(item);
            int profit = Mathf.RoundToInt(price - averageCost);
            p.cargo.Remove(item);
            p.money += price;
            m.RecordSell(item, price);
            ReputationPricing.Sold(p,town,item,price,GameManager.I.Clock.TotalHours,GameManager.I.Clock.day);
            selected = item;
            Observe();
            soldThisVisit++;
            revenueThisVisit += price;
            profitThisVisit += price - averageCost;
            Sfx.Play("coin");
            message = $"{Josa.Eul(item.displayName)} {price}냥에 팔았습니다. 손익 {Signed(profit)}냥.";
            GameEvents.RaiseItemSold(item, town, price, profit);
        }

        void BuyVehicle(VehicleData v)
        {
            var p = GameManager.I.Player;
            if(v==null || !town.vehiclesForSale.Contains(v) || v.minTownLevel>Market.Level) { message="이 장터에서 구입할 수 없는 수단입니다.";return; }
            if (p.vehicle == v) { message = "이미 사용 중인 이동 수단입니다."; return; }
            if (p.money < v.price) { message = "돈이 모자랍니다."; Sfx.Play("cancel"); return; }
            if (p.TotalWeight > v.capacity) { message = "지금 짐이 너무 많습니다."; Sfx.Play("cancel"); return; }
            p.money -= v.price;
            p.vehicle = v;
            message = $"{v.displayName} 구입 · 남은 돈 {p.money}냥." + (p.money==0?" 일반 배달로 매입 자금을 마련할 수 있습니다.":" 상품 매입비를 남겨 두세요.");
            Sfx.Play("levelup");
            GameEvents.RaiseVehicleBought(v);
        }
    }
}
