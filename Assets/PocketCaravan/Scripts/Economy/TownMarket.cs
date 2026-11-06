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
        Vector2 vehicleScroll, contentScroll;
        // 10/28 품목 목록: 한 줄 높이와 보이는 칸 높이 (방향키로 고를 때 스크롤 맞추기)
        const float RowH = 18f, ListH = 128f;   // 보이는 줄 약 7개 (예전 42 = 2줄). 아래 빈자리까지 씀
        float lastMoveY;
        static GUIStyle rowButton, rowOn;   // 11/05 고른 품목은 청록 버튼 (전엔 ▸ 표시)

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
            if (InputReader.CancelPressed() || InputReader.InteractPressed()) { Close(); return; }
            // 10/28 ↑↓(W/S)로 품목 고르기. 고른 줄이 목록 칸 안에 보이게 스크롤을 맞춘다
            float my = InputReader.MoveVector().y;
            int step = Mathf.Abs(my) > 0.5f && Mathf.Abs(lastMoveY) <= 0.5f ? (my > 0 ? -1 : 1) : 0;
            lastMoveY = my;
            if (step == 0 || marketTab != 0 || GameManager.I == null) return;
            var items = ItemsShown(GameManager.I.Player);
            if (items.Count == 0) return;
            int i = Mathf.Clamp(items.IndexOf(selected) + step, 0, items.Count - 1);
            if (items[i] == selected) return;
            selected = items[i];
            Sfx.Play("move");
            float top = i * RowH, bottom = top + RowH;
            if (top < scroll.y) scroll.y = top;
            else if (bottom > scroll.y + ListH) scroll.y = bottom - ListH;
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

            // 11/05 UI B안: 종이 창 + 제목 리본, 글씨·버튼은 CuteUI 종이 모양 (배치·기능은 그대로)
            float w = Mathf.Min(548, UIStyle.VirtualWidth - 16), h = UIStyle.WindowHeight(350) - 6;
            var rect = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(rect);
            CuteUI.TitlePlate(rect.center.x, rect.y - 11, town.displayName + " 장터");
            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 14, w - 28, h - 62));
            contentScroll=GUILayout.BeginScrollView(contentScroll);

            string marketDay = gm.Clock.IsMarketDay ? "  <color=#c0611a>· 오늘은 장날 (값 상승)</color>" : "";
            int nextLevel = m.Level < gm.Db.maxTownLevel && m.Level < gm.Db.townLevelVolume.Length ? gm.Db.townLevelVolume[m.Level] : 0;
            string growth = nextLevel > 0 ? $"   다음 등급까지 거래 {Mathf.Max(0, nextLevel - Mathf.RoundToInt(m.TradeVolume))}냥" : "";
            // 10/28 제목과 돈·짐을 한 줄로 (품목 목록을 넓히려고)
            if(UIStyle.CrispRegion)
            {
                GUILayout.Label($"<b>{m.LevelName}</b>{marketDay}",CuteUI.PaperRich);
                GUILayout.Label($"돈 {p.money} · 짐 {p.TotalWeight:0.#}/{p.Capacity:0.#}{growth}",CuteUI.PaperSmall);
            }
            else
            GUILayout.Label($"<b>{m.LevelName}</b>{marketDay}   <size=9>돈 {p.money} · 짐 {p.TotalWeight:0.#}/{p.Capacity:0.#}{growth}</size>", CuteUI.PaperRich);
            if(town.vehiclesForSale.Count>0)
            {
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("상품 거래",CuteUI.PaperButtonIf(marketTab==0)))marketTab=0;
                if(GUILayout.Button("운반 수단",CuteUI.PaperButtonIf(marketTab==1)))marketTab=1;
                GUILayout.EndHorizontal();
            }
            else marketTab=0;
            if(marketTab==1) DrawVehicles(p,m);
            else
            {
                // 10/28 평판과 기준 공급품을 한 줄로 (목록 칸을 넓히려고)
                GUILayout.Label(CuteUI.PaperColors(ReputationPricing.Progress(p,town) + "   · " + LocalGoods(gm)), CuteUI.PaperSmall);
                GUILayout.BeginHorizontal();
                GUILayout.Label("품목 선택 / 무게  <color=#6a5464>↑↓</color>", CuteUI.PaperSmall, GUILayout.Width(144));
                GUILayout.Label("<color=#1a78b8>살 때</color>", CuteUI.PaperSmall, GUILayout.Width(44));   // 살 때 파랑·팔 때 주황 (장부와 같은 색)
                GUILayout.Label("<color=#c06c00>팔 때</color>", CuteUI.PaperSmall, GUILayout.Width(44));
                GUILayout.Label("재고", CuteUI.PaperSmall, GUILayout.Width(34));
                GUILayout.Label("보유", CuteUI.PaperSmall, GUILayout.Width(34));
                GUILayout.EndHorizontal();

                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(ListH));
                if(rowButton==null||rowButton.fontSize!=UIStyle.BodySize||rowButton.normal.background==null)
                {
                    rowButton = new GUIStyle(CuteUI.PaperButton) { margin = new RectOffset(2, 2, 1, 1) };
                    rowOn = new GUIStyle(CuteUI.PaperButtonOn) { margin = new RectOffset(2, 2, 1, 1) };
                }
                foreach (var item in ItemsShown(p))
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(RowH));
                    var iconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
                    if (item.icon != null) DrawSprite(iconRect, item.icon);
                    if (GUILayout.Button($"{item.displayName} ({item.weight:0.#})",
                        selected == item ? rowOn : rowButton, GUILayout.Width(124), GUILayout.Height(RowH - 2))) selected = item;
                    int buy = m.BuyPrice(item), sell = m.SellPrice(item);
                    float avg = p.cargo.AverageCost(item);
                    string sellText = item.questItem ? "맡은 짐" : (p.cargo.Count(item) > 0 && avg > 0 ? (sell > avg ? $"<color=#1e8a3e>{sell}</color>" : $"<color=#d6334f>{sell}</color>") : sell.ToString());
                    string denial=ReputationPricing.BuyDenial(p,town,item);
                    GUILayout.Label(denial!=null?"신뢰 필요":m.CanBuy(item) && !item.questItem ? buy.ToString() : "-",CuteUI.PaperSmall,GUILayout.Width(44));
                    GUILayout.Label(sellText, CuteUI.PaperRich, GUILayout.Width(44));
                    GUILayout.Label(Mathf.FloorToInt(m.Stock(item)).ToString(), CuteUI.PaperRich, GUILayout.Width(34));
                    GUILayout.Label(p.cargo.Count(item).ToString(), CuteUI.PaperRich, GUILayout.Width(34));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                DrawTradeInfo(gm, m);

                DrawBatch(gm);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.Label(new Rect(rect.x + 14, rect.yMax - 48, w - 244, soldThisVisit>0?28:40), CuteUI.PaperColors(message), CuteUI.PaperWrap);
            if(soldThisVisit>0)GUI.Label(new Rect(rect.x+14,rect.yMax-20,w-244,12),$"이번 판매 {soldThisVisit}개 · 수령 {revenueThisVisit} · 손익 {Signed(profitThisVisit)}",CuteUI.PaperSmall);
            if (town.facilities.Count > 0 && GUI.Button(new Rect(rect.xMax - 215, rect.yMax - 32, 110, 20), "마을 시설", CuteUI.PaperButton))
            { Close(); TownFacilitiesUI.Show(town); }
            if (GUI.Button(new Rect(rect.xMax - 96, rect.yMax - 32, 82, 20), "닫기 (Esc)", CuteUI.PaperButton)) Close();
        }

        void DrawBatch(GameManager gm)
        {
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("-",CuteUI.PaperButton,GUILayout.Width(24),GUILayout.Height(18))){customQuantity=Mathf.Max(1,RequestedCount-1);quantityMode=3;}
            GUILayout.Label($"{RequestedCount}개까지",CuteUI.PaperSmall,GUILayout.Width(58));
            if(GUILayout.Button("+",CuteUI.PaperButton,GUILayout.Width(24),GUILayout.Height(18))){customQuantity=Mathf.Min(MarketState.MaxBatch,RequestedCount+1);quantityMode=3;}
            string[] modes={"1개","최대5개","최대100개"};
            for(int i=0;i<modes.Length;i++)if(GUILayout.Button(modes[i],CuteUI.PaperButtonIf(quantityMode==i),GUILayout.Height(18)))quantityMode=i;
            GUILayout.EndHorizontal();
            if(selected==null || selected.questItem)return;
            int requested=RequestedCount;
            var buy=Market.Preview(selected,gm.Player,requested,false);
            var sell=Market.Preview(selected,gm.Player,requested,true);
            GUILayout.BeginHorizontal();
            GUILayout.Label(buy.count>0?$"{buy.count}개 총 {buy.total}냥 · 남는 돈 {gm.Player.money-buy.total} · 보상 별도":buy.blocker,CuteUI.PaperSmall);
            GUI.enabled=buy.count>0;
            if(GUILayout.Button($"{buy.count}개 사기",CuteUI.PaperButtonOn,GUILayout.Width(86),GUILayout.Height(18)))TradeBatch(buy);
            GUI.enabled=true;GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(sell.count>0?$"{sell.count}개 수령 {sell.total}냥 · 매매 손익 {Signed(sell.profit)}냥":sell.blocker,CuteUI.PaperSmall);
            GUI.enabled=sell.count>0;
            if(GUILayout.Button($"{sell.count}개 팔기",CuteUI.PaperButtonOn,GUILayout.Width(86),GUILayout.Height(18)))TradeBatch(sell);
            GUI.enabled=true;GUILayout.EndHorizontal();
        }

        void DrawVehicles(PlayerState player,MarketState market)
        {
            GUILayout.Label("수단 가격 외에 다음 상품을 살 돈도 남겨 두세요. 배달 짐도 적재 공간을 씁니다.",CuteUI.PaperWrap);
            vehicleScroll=GUILayout.BeginScrollView(vehicleScroll,GUILayout.Height(198));
            foreach(var v in town.vehiclesForSale)
            {
                if(v==null || v.minTownLevel>market.Level)continue;
                bool owned=player.vehicle==v;
                GUILayout.Label($"<b>{v.displayName}</b> · 적재 {v.capacity:0} · 가격 {v.price}냥",CuteUI.PaperRich);
                int remaining=player.money-v.price;
                GUILayout.Label(owned?"현재 사용 중":remaining>=0?$"구입 후 보유금 {remaining}냥 · 상품 매입비는 별도":"돈이 부족합니다.",CuteUI.PaperSmall);
                if(!owned && remaining==0)GUILayout.Label("매입 자금이 남지 않습니다. 일반 배달로 자금을 마련할 수 있습니다.",CuteUI.PaperWrap);
                GUI.enabled=!owned && remaining>=0 && player.TotalWeight<=v.capacity;
                if(GUILayout.Button(owned?"사용 중":"구입",CuteUI.PaperButtonIf(owned),GUILayout.Height(20)))BuyVehicle(v);
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
            gm.Player.ledger.Observe(town, Market, ItemsShown(gm.Player), gm.Clock != null ? gm.Clock.day : 0);
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
            // 10/28 한 줄로 줄임 (품목 목록을 넓히려고): 품목 · 기준가 지역 · 내 원가·손익 · 다른 곳 최고 확인가 · 되팔기 제한
            var panel = GUILayoutUtility.GetRect(10, 20, GUILayout.ExpandWidth(true));
            CuteUI.Box(panel, 5, CuteUI.Paper2, CuteUI.Line);
            string text;
            if (selected == null || selected.questItem)
                text = "교역품을 고르면 원가와 직접 확인한 시세가 보입니다 · L 장부";
            else
            {
                var p = gm.Player;
                string origin = selected.originTown != null ? selected.originTown.displayName : "여러 마을";
                text = $"{selected.displayName} · 기준 {origin}";
                if (p.cargo.Count(selected) > 0)
                    text += $" · 내 원가 {p.cargo.AverageCost(selected):0.#} · 이곳 손익 {Signed(market.SellPrice(selected) - p.cargo.AverageCost(selected))}";
                TownData best = null; int bestSell = int.MinValue;
                foreach (var other in gm.Db.towns)
                {
                    if (other == null || other == town) continue;
                    var q = p.ledger.Find(other.id, selected.id);
                    if (q != null && q.sell > bestSell) { bestSell = q.sell; best = other; }
                }
                text += best != null ? $" · 최고 확인 {best.displayName} {bestSell}냥({Signed(bestSell - market.BuyPrice(selected))})" : " · 다른 장터 시세는 가 보면 장부에";
                if (p.tradeActivity.Restricted(town.id, selected.id, gm.Clock.TotalHours)) text += " · <color=#d6334f>12시간 되팔기 제한</color>";
            }
            var st = new GUIStyle(CuteUI.PaperSmall) { richText = true, wordWrap = false, clipping = TextClipping.Clip, alignment = TextAnchor.MiddleLeft };
            GUI.Label(new Rect(panel.x + 5, panel.y + 1, panel.width - 10, panel.height - 2), text, st);   // 10/28 글자 아래가 잘리던 것: 칸 높이 전체를 씀
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
