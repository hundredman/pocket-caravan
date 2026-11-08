using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 장터 상인. Space로 열면 사고팔기와 이동 수단 구입 창이 뜬다.
    /// 2026-11-08 은샘: 장부(L)와 같은 CuteUI 모양으로 새로 짰다. 사고파는 규칙(MarketState·흥정·평판)은 그대로다.
    /// - 탭: 사기(상인이 파는 것) · 팔기(내 짐) · 운반 수단. ←→로 탭, ↑↓로 물건.
    /// - 왼쪽 목록 + 오른쪽 고른 물건(값·재고·원가·다른 장터 최고 값, 수량 −/+/최대, 큰 사기·팔기 버튼, 흥정).
    /// </summary>
    public class TownMarket : Interactable
    {
        public TownData town;

        bool open;
        int openedFrame;
        float listScroll, listHeight = 150f, vehicleListScroll;
        string message = "";
        ItemData selected;
        int soldThisVisit, revenueThisVisit;
        float profitThisVisit;
        int marketTab, quantityMode, customQuantity = 1;
        int RequestedCount => quantityMode==0?1:quantityMode==1?5:quantityMode==2?MarketState.MaxBatch:Mathf.Clamp(customQuantity,1,MarketState.MaxBatch);
        const float RowH = 26f;   // 목록 한 줄 높이 (↑↓로 고를 때 스크롤 맞추기)
        float lastMoveY, lastMoveX;
        HaggleDeal deal;
        bool haggleSelling => marketTab == 1;

        MarketState Market => GameManager.I.GetMarket(town);

        public override string DisplayName => town != null ? $"{town.displayName} 상인" : "장터 상인";
        public override Kind Category => Kind.Market;

        public override void Interact(PlayerController player)
        {
            if (town == null) { Debug.LogWarning($"{name}: TownData가 비어 있습니다."); return; }
            if (!UIState.TryOpen(this, Close)) return;
            open = true;
            openedFrame = Time.frameCount;
            message = "";
            soldThisVisit = revenueThisVisit = 0; profitThisVisit = 0;
            deal = null;
            // 팔 물건이 있으면 팔기부터 (상인에게 가면 먼저 팔고 그 돈으로 산다)
            bool canSell = false;
            foreach (var item in TabItems(GameManager.I.Player, true)) if (!item.questItem) { canSell = true; break; }
            SetTab(canSell ? 1 : 0);
            Observe();
            Sfx.Play("open");
        }

        void Close()
        {
            open = false;
            deal = null;
            UIState.Release(this);
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            if (InputReader.LedgerPressed()) { OpenLedger(); return; }
            if (InputReader.CancelPressed() || InputReader.InteractPressed()) { Close(); return; }
            if (GameManager.I == null) return;
            // 11/08 ←→ 사기·팔기(·운반 수단) 탭, ↑↓ 물건 고르기
            var mv = InputReader.MoveVector();
            int stepY = Mathf.Abs(mv.y) > 0.5f && Mathf.Abs(lastMoveY) <= 0.5f ? (mv.y > 0 ? -1 : 1) : 0;
            int stepX = Mathf.Abs(mv.x) > 0.5f && Mathf.Abs(lastMoveX) <= 0.5f ? (mv.x > 0 ? 1 : -1) : 0;
            lastMoveY = mv.y; lastMoveX = mv.x;
            if (stepX != 0)
            {
                int tabs = town.vehiclesForSale.Count > 0 ? 3 : 2;
                int t = Mathf.Clamp(marketTab + stepX, 0, tabs - 1);
                if (t != marketTab) { SetTab(t); Sfx.Play("move"); }
                return;
            }
            if (stepY == 0 || marketTab > 1) return;
            var items = TabItems(GameManager.I.Player, marketTab == 1);
            if (items.Count == 0) return;
            int i = Mathf.Clamp(items.IndexOf(selected) + stepY, 0, items.Count - 1);
            if (items[i] == selected) return;
            selected = items[i];
            Sfx.Play("move");
            float top = i * RowH, bottom = top + RowH;
            if (top < listScroll) listScroll = top;
            else if (bottom > listScroll + listHeight) listScroll = bottom - listHeight;
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

        bool haggleOpen;

        void SetTab(int t)
        {
            marketTab = t; listScroll = 0; haggleOpen = false;
            if (t > 1 || GameManager.I == null) return;
            var items = TabItems(GameManager.I.Player, t == 1);
            if (!items.Contains(selected)) selected = items.Count > 0 ? items[0] : null;
        }

        /// <summary>사기 탭: 이 장터가 다루는 물건. 팔기 탭: 내 짐 (맡은 짐은 맨 아래)</summary>
        List<ItemData> TabItems(PlayerState p, bool selling)
        {
            var result = new List<ItemData>();
            if (selling)
            {
                foreach (var kv in p.cargo.Items) if (kv.Key != null && !kv.Key.questItem && kv.Value > 0) result.Add(kv.Key);
                foreach (var kv in p.cargo.Items) if (kv.Key != null && kv.Key.questItem && kv.Value > 0) result.Add(kv.Key);
                return result;
            }
            var seen = new HashSet<ItemData>();
            foreach (var st in town.stock) if (st.item != null && !st.item.questItem && seen.Add(st.item)) result.Add(st.item);
            foreach (var item in Market.KnownItems) if (item != null && !item.questItem && seen.Add(item)) result.Add(item);
            return result;
        }

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin();
            GUI.depth = -10;
            var gm = GameManager.I;
            var p = gm.Player;
            var m = Market;

            CuteUI.Dim();
            float w = Mathf.Min(520, UIStyle.VirtualWidth - 16), h = 288;
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, town.displayName.EndsWith("장터") ? town.displayName : town.displayName + " 장터");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            // 위: 돈 · 짐 (+ 장날)
            float hy = r.y + 24;
            CuteUI.Icon(CuteUI.Pic.Coin, r.x + 18, hy - 5);
            float x = CuteUI.Label(r.x + 31, hy, $"{p.money}냥", CuteUI.Text, false, true) + 12;
            CuteUI.Icon(CuteUI.Pic.Sack, x, hy - 4);
            CuteUI.Bar(new Rect(x + 13, hy - 4, 56, 8), p.Capacity > 0 ? p.TotalWeight / p.Capacity : 0, CuteUI.Load, CuteUI.LoadHi);
            x = CuteUI.Label(x + 73, hy, $"{p.TotalWeight:0.#}/{p.Capacity:0.#}", CuteUI.Sub, true) + 10;
            if (gm.Clock.IsMarketDay)
            {
                float bw = CuteUI.Width("장날", true, true) + 12;
                CuteUI.Box(new Rect(x, hy - 7, bw, 14), 6, CuteUI.SellSoft, CuteUI.Sell);
                CuteUI.LabelCenter(x + bw / 2, hy, "장날", CuteUI.Sell, true, true);
            }

            var c = new Rect(r.x + 15, r.y + 60, r.width - 30, r.height - 92);
            CuteUI.Box(c, 7, CuteUI.Paper, CuteUI.Ink);
            float tx = c.x + 6;
            if (CuteUI.Tab(tx, c.y, "사기", CuteUI.Pic.Basket, marketTab == 0, out float w0) && marketTab != 0) SetTab(0);
            tx += w0 + 3;
            if (CuteUI.Tab(tx, c.y, "팔기", CuteUI.Pic.Coin7, marketTab == 1, out float w1) && marketTab != 1) SetTab(1);
            tx += w1 + 3;
            if (town.vehiclesForSale.Count > 0 && CuteUI.Tab(tx, c.y, "운반 수단", CuteUI.Pic.Sack, marketTab == 2, out _) && marketTab != 2) SetTab(2);
            if (marketTab > 1 && town.vehiclesForSale.Count == 0) SetTab(0);

            if (marketTab == 2) DrawVehicles(p, m, c);
            else DrawGoods(gm, m, c);

            // 아래: 결과 한 줄 + 마을 시설
            float fy = r.yMax - 17;
            bool recover = town.region != null && StandingRecovery.Negative(p.reputation, ReputationPricing.Scope(town), ReputationPricing.Target(town));
            float right = r.xMax - 15;
            if (town.facilities.Count > 0 || recover)
            {
                string fl = recover ? "평판 회복·시설" : "마을 시설";
                float fw = CuteUI.Width(fl, true) + 18;
                if (CuteUI.SoftButton(new Rect(right - fw, fy - 9, fw, 18), fl)) { Close(); TownFacilitiesUI.Show(town); return; }
                right -= fw + 8;
            }
            right -= CuteUI.KeycapWidth("L") + CuteUI.Width("장부", true) + 4;
            float kx = CuteUI.Keycap(right, fy - 6, "L", true);
            CuteUI.Label(right + kx + 3, fy, "장부", CuteUI.Sub, true);
            if (!string.IsNullOrEmpty(message)) CuteUI.Label(r.x + 18, fy, Fit(CuteUI.Plain(message), right - r.x - 30, true, false), CuteUI.Text, true);
        }

        void DrawGoods(GameManager gm, MarketState m, Rect c)
        {
            var p = gm.Player;
            bool selling = marketTab == 1;
            var items = TabItems(p, selling);
            const float lw = 196f;
            var area = new Rect(c.x + 6, c.y + 8, lw + 4, c.height - 12);
            listHeight = area.height;
            if (items.Count == 0)
            {
                CuteUI.LabelCenter(c.center.x, c.center.y - 6, selling ? "팔 짐이 없어요" : "지금 파는 물건이 없어요", CuteUI.Text, false, true);
                if (selling) CuteUI.LabelCenter(c.center.x, c.center.y + 10, "사기 탭에서 물건을 사 보세요", CuteUI.Sub, true);
                return;
            }
            if (!items.Contains(selected)) selected = items[0];
            listScroll = Scroll(area, listScroll, items.Count * RowH);
            GUI.BeginGroup(area);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                float y = i * RowH - listScroll;
                if (y + RowH < 0 || y > area.height) continue;
                bool on = item == selected;
                var row = new Rect(0, y, lw, RowH - 2);
                if (on)
                {
                    CuteUI.Box(row, 8, CuteUI.Interact, CuteUI.Ink, 1, 2, CuteUI.InteractLo);
                    CuteUI.Fill(new Rect(5, y + 2, lw - 10, 1), CuteUI.InteractHi);
                }
                ItemIcon(item, 5, y + 4, 1);
                float valueLeft;
                bool dim = false;
                if (selling)
                {
                    int n = p.cargo.Count(item);
                    if (item.questItem) { valueLeft = lw - 6 - CuteUI.Width("맡은 짐", true); CuteUI.Label(valueLeft, y + 12, "맡은 짐", on ? CuteUI.OnInteract : CuteUI.Zero, true); dim = true; }
                    else
                    {
                        int sell = m.SellPrice(item); float avg = p.cargo.AverageCost(item);
                        valueLeft = avg > 0 ? CuteUI.GainBadge(lw - 5, y + 12, sell - avg) - 4 : lw - 5;
                        valueLeft = CuteUI.LabelRight(valueLeft, y + 12, sell.ToString(), on ? CuteUI.OnInteract : CuteUI.Sell, false, true) - 4;
                    }
                    CuteUI.Label(26, y + 12, Fit(item.displayName + (n > 1 ? $" ×{n}" : ""), valueLeft - 30, false, on), on ? CuteUI.OnInteract : dim ? CuteUI.Zero : CuteUI.Text, false, on);
                }
                else
                {
                    bool can = m.CanBuy(item) && ReputationPricing.BuyDenial(p, town, item) == null;
                    string v = !m.CanBuy(item) ? "품절" : ReputationPricing.BuyDenial(p, town, item) != null ? "신뢰 필요" : m.BuyPrice(item).ToString();
                    valueLeft = CuteUI.LabelRight(lw - 7, y + 12, v, on ? CuteUI.OnInteract : can ? CuteUI.Buy : CuteUI.Zero, !can, can) - 4;
                    CuteUI.Label(26, y + 12, Fit(item.displayName, valueLeft - 30, false, on), on ? CuteUI.OnInteract : can ? CuteUI.Text : CuteUI.Zero, false, on);
                }
                if (GUI.Button(row, GUIContent.none, GUIStyle.none) && selected != item) { selected = item; haggleOpen = false; }
            }
            GUI.EndGroup();
            for (float yy = c.y + 8; yy < c.yMax - 8; yy += 3) CuteUI.Fill(new Rect(c.x + lw + 14, yy, 1, 1), CuteUI.Line);
            DrawDetail(gm, m, new Rect(c.x + lw + 24, c.y + 8, c.xMax - 10 - (c.x + lw + 24), c.height - 14));
        }

        void DrawDetail(GameManager gm, MarketState m, Rect d)
        {
            var p = gm.Player;
            var item = selected;
            if (item == null) return;
            bool selling = marketTab == 1;
            // 물건: 큰 그림 · 이름 · 한 개 값
            CuteUI.FramedCircle(d.x, d.y, 40);
            ItemIcon(item, d.x + 4, d.y + 4, 2);
            CuteUI.Label(d.x + 48, d.y + 11, Fit(item.displayName, d.width - 50, false, true), CuteUI.Text, false, true);
            if (item.questItem) { CuteUI.Label(d.x + 48, d.y + 28, "맡은 짐이라 팔 수 없어요", CuteUI.Sub, true); return; }
            float haggle = DealFor(selling);
            int unit = selling ? m.SellPrice(item, haggle) : m.BuyPrice(item, haggle);
            float vx = d.x + 48;
            if (haggle > 0) vx = Struck(vx, d.y + 29, (selling ? m.SellPrice(item) : m.BuyPrice(item)).ToString()) + 5;   // 흥정 전 값 취소선
            vx = CuteUI.Label(vx, d.y + 29, $"{unit}냥", selling ? CuteUI.Sell : CuteUI.Buy, false, true);
            if (selling)
            {
                float avg = p.cargo.AverageCost(item);
                if (avg > 0) CuteUI.GainBadge(vx + 6 + CuteUI.Width(CuteUI.GainText(unit - avg), true, true) + 10, d.y + 29, unit - avg);
            }
            else CuteUI.Label(vx + 8, d.y + 29, $"남은 {Mathf.FloorToInt(m.Stock(item))}개", CuteUI.Sub, true);

            float banLeft = p.tradeActivity.BanRemaining(town.id, gm.Clock.TotalHours);
            if (banLeft > 0)
            {
                CuteUI.Wrap(d.x, d.y + 62, d.width, $"상인이 거래를 거절해요. {Mathf.CeilToInt(banLeft)}시간 뒤에 다시 와요.", CuteUI.Down, true);
                return;
            }
            string denial = selling ? null : ReputationPricing.BuyDenial(p, town, item);
            if (denial != null) { CuteUI.Wrap(d.x, d.y + 62, d.width, "이 상인과 더 친해져야 살 수 있어요.", CuteUI.Sub, true); return; }

            // 수량: − n + 최대
            int max = m.Preview(item, p, MarketState.MaxBatch, selling, haggle).count;
            if (haggle > 0) max = Mathf.Min(max, deal.count);
            int want = Mathf.Clamp(RequestedCount, 1, Mathf.Max(1, max));
            float qy = d.y + 60;
            CuteUI.Label(d.x, qy + 9, "수량", CuteUI.Sub, true);
            var minus = new Rect(d.x + 30, qy, 20, 18);
            var plus = new Rect(d.x + 92, qy, 20, 18);
            if (CuteUI.SoftButton(minus, "−")) { customQuantity = Mathf.Max(1, want - 1); quantityMode = 3; }
            CuteUI.Box(new Rect(d.x + 53, qy, 36, 18), 5, CuteUI.White, CuteUI.Ink);
            CuteUI.LabelCenter(d.x + 71, qy + 9, max > 0 ? want.ToString() : "0", CuteUI.Text, false, true);
            if (CuteUI.SoftButton(plus, "+")) { customQuantity = Mathf.Min(Mathf.Max(1, max), want + 1); quantityMode = 3; }
            if (CuteUI.SoftButton(new Rect(d.x + 116, qy, 38, 18), "최대")) quantityMode = 2;

            var est = m.Preview(item, p, haggle > 0 ? deal.count : want, selling, haggle);
            // 흥정을 열면 합계·거래 버튼 자리에 3단계를 띄운다 (겹친 버튼이 눌리지 않게 거래 버튼은 그리지 않는다)
            if (haggleOpen) { DrawHaggle(gm, new Rect(d.xMax - 58, qy, 58, 18), new Rect(d.x, qy + 26, d.width, d.yMax - qy - 26), est.count); return; }
            float sy = qy + 30;
            if (est.count > 0)
            {
                CuteUI.Label(d.x, sy, "합계", CuteUI.Sub, true);
                CuteUI.Icon(CuteUI.Pic.Coin7, d.x + 30, sy - 4);
                float ex = d.x + 41;
                if (haggle > 0) ex = Struck(ex, sy, m.Preview(item, p, est.count, selling).total.ToString()) + 5;
                ex = CuteUI.Label(ex, sy, $"{est.total}냥", CuteUI.Text, false, true);
                if (selling && est.profit != 0) CuteUI.GainBadge(ex + 8 + CuteUI.Width(CuteUI.GainText(est.profit), true, true) + 10, sy, est.profit);
            }
            else CuteUI.Wrap(d.x, sy - 6, d.width, est.blocker ?? "", CuteUI.Down, true);

            // 큰 버튼
            var btn = new Rect(d.x, sy + 16, d.width, 26);
            string label = est.count > 0 ? $"{est.count}개 {(selling ? "팔기" : "사기")}" : selling ? "팔 수 없어요" : "살 수 없어요";
            if (est.count > 0)
            {
                if (CuteUI.Button(btn, label, selling ? CuteUI.Pic.Coin7 : CuteUI.Pic.Basket)) TradeBatch(est);
            }
            else
            {
                CuteUI.Box(btn, 8, CuteUI.ZeroSoft, CuteUI.Line);
                CuteUI.LabelCenter(btn.center.x, btn.center.y, label, CuteUI.Zero, false, true);
            }

            DrawHaggle(gm, new Rect(d.xMax - 58, qy, 58, 18), new Rect(d.x, btn.yMax + 6, d.width, d.yMax - btn.yMax - 6), est.count);

        }

        // 흥정: 수량 줄 오른쪽 "흥정" 버튼. 누르면 아래에 3단계(살짝·적당히·세게: 깎는 폭 · 성공 확률)가 열린다.
        // 결과는 다음 한 번의 거래에만 적용된다.
        void DrawHaggle(GameManager gm, Rect button, Rect panel, int count)
        {
            if (count <= 0) { haggleOpen = false; return; }
            var p = gm.Player; var entry = ReputationPricing.Entry(p, town); float max = ReputationPricing.MaxHaggle(entry.standing);
            string denial = Haggle.Denial(p.tradeActivity, town.id, gm.Clock.day, gm.Clock.TotalHours, max);
            bool done = DealFor(haggleSelling) > 0;
            if (done || denial != null)
            {
                haggleOpen = false;
                CuteUI.Box(button, 8, CuteUI.ZeroSoft, CuteUI.Line);
                CuteUI.LabelCenter(button.center.x, button.center.y, "흥정", CuteUI.Zero, false, true);
                if (denial != null && !done) CuteUI.Label(panel.x, panel.y + 8, Fit(denial, panel.width, true, false), CuteUI.Zero, true);
                return;
            }
            if (haggleOpen) { CuteUI.Box(button, 8, CuteUI.Interact, CuteUI.Ink, 1, 2, CuteUI.InteractLo); CuteUI.LabelCenter(button.center.x, button.center.y, "흥정", CuteUI.OnInteract, false, true); if (GUI.Button(button, GUIContent.none, GUIStyle.none)) haggleOpen = false; }
            else if (CuteUI.SoftButton(button, "흥정")) haggleOpen = true;
            if (!haggleOpen) return;

            float bh = 22, gap = 3;
            var box = new Rect(panel.x, panel.y, panel.width, Mathf.Min(panel.height, 3 * bh + 2 * gap + 8));
            CuteUI.Box(box, 8, CuteUI.Paper2, CuteUI.Line);
            int i = 0;
            foreach (HaggleStrength k in new[] { HaggleStrength.Light, HaggleStrength.Fair, HaggleStrength.Hard })
            {
                float chance = HaggleRules.Chance(max, entry.standing, k, town.id, count);
                var b = new Rect(box.x + 4, box.y + 4 + i * (bh + gap), box.width - 8, bh);
                if (b.yMax > box.yMax) break;
                bool hover = b.Contains(Event.current.mousePosition);
                CuteUI.Box(b, 7, hover ? CuteUI.White : CuteUI.Paper, CuteUI.Ink);
                CuteUI.Label(b.x + 8, b.center.y, HaggleRules.Name(k), CuteUI.Text, false, true);
                CuteUI.Label(b.x + 64, b.center.y, $"{HaggleRules.Discount(max, k) * 100:0.#}% {(haggleSelling ? "더 받기" : "깎기")}", CuteUI.Sub, true);
                CuteUI.LabelRight(b.xMax - 8, b.center.y, $"성공 {chance * 100:0}%", chance >= .6f ? CuteUI.Up : chance >= .4f ? CuteUI.Sell : CuteUI.Down, true, true);
                if (GUI.Button(b, GUIContent.none, GUIStyle.none)) { TryHaggle(k, max, entry.standing, count); haggleOpen = false; }
                i++;
            }
        }

        float DealFor(bool selling,ItemData item=null)
        {
            var it=item??selected;var gm=GameManager.I;
            return deal!=null && it!=null && deal.AppliesTo(town.id,it.id,selling,gm.Clock.day,Market.Preview(it,gm.Player,RequestedCount,selling).count)?deal.discount:0;
        }

        void TryHaggle(HaggleStrength k,float max,ReputationStanding standing,int count)
        {
            var gm=GameManager.I;
            // Recompute the real, undiscounted executable quantity at the click. Never trust the requested maximum.
            count=Market.Preview(selected,gm.Player,RequestedCount,haggleSelling).count;
            HaggleResult r=default;
            if(!SavedEconomyChange.Try(gm,()=>r=Haggle.Try(gm.Player.tradeActivity,town.id,selected.id,haggleSelling,count,
                standing,max,k,gm.Clock.day,gm.Clock.TotalHours)))
            { message=SavedEconomyChange.Failure; return; }
            // 11/08 아래 한 줄에 들어가게 짧게 (자세한 까닭은 흥정 버튼 아래·거래 칸에 나온다)
            message=r.outcome==HaggleOutcome.Refused?"흥정 실패 · 원래 값으로는 거래할 수 있어요"
                :r.outcome==HaggleOutcome.Cancelled?$"흥정 실패 · 상인이 화가 나서 {HaggleRules.BanHours:0}시간 동안 거래하지 않아요"
                :r.message;
            if(r.outcome==HaggleOutcome.Success){deal=r.deal;Sfx.Play("coin");}
            else{deal=null;Sfx.Play("cancel");}
        }

        void DrawVehicles(PlayerState player, MarketState market, Rect c)
        {
            var list = new List<VehicleData>();
            foreach (var v in town.vehiclesForSale) if (v != null && v.minTownLevel <= market.Level) list.Add(v);
            var area = new Rect(c.x + 6, c.y + 8, c.width - 10, c.height - 12);
            const float rowH = 40f;
            vehicleListScroll = Scroll(area, vehicleListScroll, list.Count * rowH);
            GUI.BeginGroup(area);
            for (int i = 0; i < list.Count; i++)
            {
                var v = list[i];
                float y = i * rowH - vehicleListScroll;
                if (y + rowH < 0 || y > area.height) continue;
                bool owned = player.vehicle == v;
                bool can = !owned && player.money >= v.price && player.TotalWeight <= v.capacity;
                CuteUI.Box(new Rect(0, y, area.width - 6, rowH - 4), 8, i % 2 == 0 ? CuteUI.Paper2 : CuteUI.Paper, CuteUI.Line);
                CuteUI.Label(10, y + 12, v.displayName, CuteUI.Text, false, true);
                CuteUI.Icon(CuteUI.Pic.Sack, 10, y + 21);
                CuteUI.Label(22, y + 25, $"짐 {v.capacity:0}", CuteUI.Sub, true);
                var b = new Rect(area.width - 98, y + 7, 86, 22);
                if (owned) { CuteUI.Box(b, 8, CuteUI.ZeroSoft, CuteUI.Line); CuteUI.LabelCenter(b.center.x, b.center.y, "쓰는 중", CuteUI.Zero, false, true); }
                else if (can) { if (CuteUI.Button(b, $"{v.price}냥")) BuyVehicle(v); }
                else { CuteUI.Box(b, 8, CuteUI.ZeroSoft, CuteUI.Line); CuteUI.LabelCenter(b.center.x, b.center.y, $"{v.price}냥", CuteUI.Zero, false, true); }
            }
            GUI.EndGroup();
        }

        // 각 단위 거래는 기존 규칙/이벤트를 유지한다. 실행 직전 견적이 달라졌으면 시작하지 않는다.
        int TradeBatch(MarketEstimate estimate)
        {
            var p=GameManager.I.Player; var m=Market;
            if(estimate!=null && p.tradeActivity.BanRemaining(town.id,GameManager.I.Clock.TotalHours)>0){message="상인이 지금은 거래하지 않겠다고 합니다.";return 0;}
            float haggle=estimate!=null?DealFor(estimate.selling,estimate.item):0;
            if((haggle>0 && estimate.count!=deal.count) || estimate==null || estimate.town!=town || estimate.count<1 || !estimate.Matches(m.Preview(estimate.item,p,estimate.count,estimate.selling,haggle)))
            { message="시세·잔고·화물이 달라졌습니다. 수량과 총액을 다시 확인하세요.";return 0; }
            int completed=0,total=0;float basis=p.cargo.AverageCost(estimate.item);
            for(int i=0;i<estimate.count;i++)
            {
                int price=estimate.selling?m.SellPrice(estimate.item,haggle):m.BuyPrice(estimate.item,haggle);
                if(price!=estimate.PriceAt(i))break;
                if(estimate.selling)
                {
                    if(p.cargo.Count(estimate.item)<1 || (long)p.money+price>int.MaxValue)break;
                    SellHaggled(estimate.item,haggle);
                }
                else
                {
                    if(!m.CanBuy(estimate.item) || p.money<price || !p.CanCarry(estimate.item))break;
                    BuyHaggled(estimate.item,haggle);
                }
                completed++;total+=price;
            }
            if(completed>0 && haggle>0)deal=null; // 흥정은 이 거래 한 번에만 쓴다.
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

        /// <summary>회색 글씨 + 가운데 취소선. 오른쪽 끝 x를 돌려준다</summary>
        static float Struck(float x, float cy, string s)
        {
            float xr = CuteUI.Label(x, cy, s, CuteUI.Zero, true);
            CuteUI.Fill(new Rect(x - 1, Mathf.Round(cy), xr - x + 2, 1), CuteUI.Zero);
            return xr;
        }

        static void ItemIcon(ItemData item, float x, float y, int scale)
        {
            if (item != null && item.icon != null) CuteUI.Sprite(item.icon, new Rect(x, y, 16 * scale, 16 * scale));
            else CuteUI.Icon(CuteUI.Pic.Sack, x + 3 * scale, y + 4 * scale, scale);
        }

        static string Fit(string s, float width, bool small, bool bold)
        {
            if (string.IsNullOrEmpty(s) || width <= 0 || CuteUI.Width(s, small, bold) <= width) return s;
            while (s.Length > 1 && CuteUI.Width(s + "…", small, bold) > width) s = s.Substring(0, s.Length - 1);
            return s + "…";
        }

        static float Scroll(Rect area, float offset, float contentH)
        {
            var e = Event.current;
            float max = Mathf.Max(0f, contentH - area.height);
            if (e.type == EventType.ScrollWheel && area.Contains(e.mousePosition)) { offset += e.delta.y * 10f; e.Use(); }
            offset = Mathf.Clamp(offset, 0f, max);
            if (max > 0f)
            {
                float bh = Mathf.Max(12f, area.height * area.height / contentH);
                float by = area.y + (area.height - bh) * (offset / max);
                CuteUI.Fill(new Rect(area.xMax - 3, area.y, 2, area.height), CuteUI.Paper2);
                CuteUI.Fill(new Rect(area.xMax - 3, by, 2, bh), CuteUI.Line);
            }
            return offset;
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

        void Buy(ItemData item) => BuyHaggled(item,0);

        void BuyHaggled(ItemData item,float haggle)
        {
            if (item == null || item.questItem) { message = "맡은 짐은 교역할 수 없습니다."; return; }
            var p = GameManager.I.Player;
            var m = Market;
            string denial=ReputationPricing.BuyDenial(p,town,item);
            if(denial!=null){message=denial;Sfx.Play("cancel");return;}
            int price = m.BuyPrice(item,haggle);
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
            p.ledger.Record(false, town, item, price, 0, GameManager.I.Clock.day);   // 11/08 장부 최근 거래
            GameEvents.RaiseItemBought(item, town, price);
        }

        void Sell(ItemData item) => SellHaggled(item,0);

        void SellHaggled(ItemData item,float haggle)
        {
            var p = GameManager.I.Player;
            var m = Market;
            if (p.cargo.Count(item) <= 0) { message = "가진 것이 없습니다."; return; }
            if (item.questItem) { message = "맡은 물건은 팔 수 없습니다."; return; }
            int price = m.SellPrice(item,haggle);
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
            p.ledger.Record(true, town, item, price, profit, GameManager.I.Clock.day);   // 11/08 장부 최근 거래
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
