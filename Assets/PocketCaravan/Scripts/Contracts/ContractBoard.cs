using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 마을 의뢰 게시판. Space로 열어 의뢰를 맡거나, 이 마을로 가는 의뢰를 납품한다.
    /// </summary>
    public class ContractBoard : Interactable
    {
        public string town;

        bool open;
        int openedFrame;
        string message = "";
        Vector2 scroll;

        ContractManager Cm => GameManager.I.Contracts;

        public override string DisplayName => "의뢰 게시판";

        /// <summary>11/19 게시판 창이 열려 있다 (TutorialSpot이 새 의뢰 종류를 처음 보여 줄 때)</summary>
        public static bool Showing { get; private set; }
        static readonly string[] KindKeys = { "express", "fragile", ContractKind.PremiumKey, "bulk" };
        public static System.Collections.Generic.IEnumerable<string> TourKeys => KindKeys;
        string hoverHelp; Vector2 groupOrigin;
        /// <summary>화면 확인 도구용: 이 자리(화면 좌표)에 마우스가 있는 것처럼 (null이면 실제 마우스)</summary>
        public static Vector2? TestMouse;
        static Vector2 MouseAt(Vector2 groupOffset) => TestMouse.HasValue ? TestMouse.Value - groupOffset : Event.current.mousePosition;
        public override Kind Category => Kind.Board;

        public override void Interact(PlayerController player)
        {
            if (!UIState.TryOpen(this, Close)) return;
            open = true; Showing = true;
            openedFrame = Time.frameCount;
            message = "";
            Sfx.Play("open");
        }

        void Close()
        {
            open = false; Showing = false;
            PackPuzzle.Cancel();   // 11/16 게시판이 닫히면 짐 싣기 퍼즐도 맡지 않고 닫는다
            UIState.Release(this);
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            if (PackPuzzle.IsOpen || Time.frameCount == PackPuzzle.ClosedFrame) return;   // 11/16 짐 싣기 퍼즐 중
            if (TutorialSpot.HoldsWindow) return;   // 11/19 새 의뢰 설명 중엔 Space로 닫지 않는다 (아무 키나 = 설명 넘기기)
            if (InputReader.CancelPressed() || InputReader.InteractPressed()) Close();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (open) Close();
        }

        string ReportChore(GameManager gm, PlayerState p)
        {
            var townData = gm.Db.towns.Find(t => t != null && t.id == town);
            if (townData == null || townData.region == null || p.tradeActivity.ActiveChore?.town != town) return "보고할 마을 일이 없습니다.";
            int gained = 0;
            if (!SavedEconomyChange.Try(gm, () => {
                p.tradeActivity.FinishChore(town);
                gm.Clock.Advance(StandingRecovery.ChoreHours);
                gained = StandingRecovery.Raise(p.reputation, ReputationPricing.Scope(townData), ReputationPricing.Target(townData),
                    ReputationAwards.PointsFor(StandingRecovery.ChoreSize));
            })) return SavedEconomyChange.Failure;
            Sfx.Play("success");
            return gained > 0 ? $"마을 일을 마쳤습니다. 평판 +{gained}." : "마을 일을 마쳤습니다. 평판은 이미 중립입니다.";
        }

        /// <summary>bonus: 11/16 짐 싣기 퍼즐을 다 풀면 보수에 더하는 몫 (맡기 전에 더한다. 저장 실패면 함께 되돌려진다)</summary>
        string AcceptSaved(Contract c, int bonus = 0)
        {
            bool accepted=false;
            if (!SavedEconomyChange.Try(GameManager.I,()=>{ if (bonus > 0) c.reward += bonus; accepted=Cm.Accept(town,c); if (!accepted && bonus > 0) c.reward -= bonus; })) return SavedEconomyChange.Failure;
            return !accepted ? "이 의뢰는 더 이상 맡을 수 없습니다. 게시판을 다시 확인해 주세요."
                : c.atonement ? $"{c.cargo} 수락 · 보증금 {c.deposit}냥\n{Cm.TownName(c.toTown)} 게시판에 납품하세요."
                : $"{Josa.Eul(c.cargo)} 맡았다. {Cm.TownName(c.toTown)} 게시판에 가져가자." + (ContractKind.Reminder(c) != null ? $" ({ContractKind.Reminder(c)})" : "");
        }

        string DeliverSaved(Contract c)
        {
            ContractResult result=default;
            return SavedEconomyChange.Try(GameManager.I,()=>result=Cm.Deliver(c,town)) ? result.message : SavedEconomyChange.Failure;
        }

        // 11/08 은샘: 글 줄 나열이던 게시판을 카드로. 맨 위 줄에 짐·맡은 의뢰·신용·돈, 의뢰마다 목적지·짐·기한·보수를 한 장에 두고
        // 못 맡는 의뢰는 버튼이 회색이 되고 이유(신용 3 필요·짐 자리 부족 등)를 버튼에 적는다.
        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin();
            GUI.depth = -10;   // 다른 글자들 위에
            var gm = GameManager.I;
            var p = gm.Player;

            float w = Mathf.Min(500, UIStyle.VirtualWidth - 16), h = Mathf.Min(286, UIStyle.VirtualHeight - 34);
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, Cm.TownName(town) + " 의뢰 게시판");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9) && !PackPuzzle.IsOpen) { Close(); return; }

            DrawStatus(p, new Rect(r.x + 15, r.y + 17, w - 30, 20));

            var content = new Rect(r.x + 15, r.y + 43, w - 30, h - 43 - 30);
            CuteUI.Box(content, 7, CuteUI.Paper, CuteUI.Ink);

            // 카드 목록 (높이 먼저 셈)
            var chore = p.tradeActivity.ActiveChore;
            bool hasChore = chore != null && chore.town == town;
            var deliver = p.contracts.FindAll(c => c.toTown == town);
            var list = Cm.Available(town);
            const float head = 18f, card = 46f;
            float total = (hasChore ? head + card : 0) + (deliver.Count > 0 ? head + deliver.Count * card : 0) + head + Mathf.Max(1, list.Count) * card;

            var area = new Rect(content.x + 6, content.y + 4, content.width - 12, content.height - 8);
            scroll.y = TradeLedgerUI.Scroll(area, scroll.y, total);
            GUI.BeginGroup(area);
            groupOrigin = area.position; hoverHelp = null;
            float y = -scroll.y, cw = area.width - (total > area.height ? 7 : 0);
            if (hasChore)
            {
                Header(y, "마을 일 보고", null); y += head;
                string title = StandingRecovery.ChoreTitles[chore.kind % StandingRecovery.ChoreTitles.Length];
                if (DrawCard(new Rect(0, y, cw, card - 4), CuteUI.Pic.Scroll, title, null, $"보고하면 {StandingRecovery.ChoreHours:0}시간이 지나요", null, null, "보고", null))
                    message = ReportChore(gm, p);
                y += card;
            }
            if (deliver.Count > 0)
            {
                Header(y, "이 마을에 납품할 짐", deliver.Count + "개"); y += head;
                foreach (var c in deliver)
                {
                    string badge = c.dropped ? "길에 떨어뜨림" : ContractKind.Name(c.kind);
                    string sub = $"상태 {Mathf.RoundToInt(c.condition * 100)}% · 남은 시간 {Cm.TimeLeftText(c)}";
                    string pay = c.atonement ? $"보증금 {c.deposit}" : c.reward.ToString();
                    if (DrawCard(new Rect(0, y, cw, card - 4), CuteUI.Pic.Sack, c.cargo, badge, sub, pay, null, "납품", c.dropped ? "주워 오기" : null, true))
                        message = c.dropped ? "길에 떨어뜨린 짐을 먼저 주워 오세요." : DeliverSaved(c);
                    y += card;
                }
            }
            Header(y, "맡을 수 있는 의뢰", list.Count > 0 ? list.Count + "개" : null); y += head;
            if (list.Count == 0)
            {
                CuteUI.LabelCenter(cw / 2f, y + 14, "지금은 의뢰가 없어요", CuteUI.Text, true, true);
                CuteUI.LabelCenter(cw / 2f, y + 29, "몇 시간 뒤에 다시 와 보세요", CuteUI.Sub, true);
            }
            foreach (var c in list.ToArray())
            {
                string why = Cm.CanAccept(c);
                string sub = $"{Cm.TownName(c.toTown)}" + (c.quotedRouteSteps > 0 ? $" · {c.quotedRouteSteps}구간" : "") + $" · 기한 {c.hours:0}시간";
                string badge = c.atonement ? "속죄 의뢰" : c.Premium ? $"신용 {c.minCredit}" : ContractKind.Name(c.kind);   // 11/19 종류 배지
                string pay = c.atonement ? $"보증금 {c.deposit}" : c.reward.ToString();
                var cardRect = new Rect(0, y, cw, card - 4);
                string kindKey = ContractKind.Key(c);
                if (kindKey != null)
                {
                    // 11/19 마우스를 올리면 자세히 · 열린 종류는 처음 볼 때 튜토리얼처럼 밝힌다 (TutorialSpot "board.kind.*")
                    var mouse = MouseAt(area.position);
                    if (cardRect.Contains(mouse) && area.Contains(mouse + area.position)) hoverHelp = ContractKind.Help(kindKey);
                    bool usable = kindKey != ContractKind.PremiumKey || Credit.Level(p) >= ContractManager.PremiumCredit;
                    if (usable && y >= 0 && y + card <= area.height) ScreenZones.MarkSpot("board.kind." + kindKey, new Rect(area.x + cardRect.x, area.y + cardRect.y, cardRect.width, cardRect.height));
                }
                if (DrawCard(cardRect, CuteUI.Pic.Scroll, c.cargo, badge, sub, pay, c.weight, "맡기", why != null ? ShortWhy(c, p) : null, false, true, c.atonement, c.atonement ? 0 : Cm.CreditPoints(c, false)))
                {
                    if (why != null) { message = why; Sfx.Play("deny", .55f); }
                    else if (PackPuzzle.Wants(c))
                    {
                        // 11/16 은샘: 큰 짐·신용 의뢰는 짐 싣기 퍼즐을 거쳐 맡는다 (Esc로 닫으면 맡지 않음)
                        var cc = c; int bonus = PackPuzzle.BonusFor(cc);
                        if (!PackPuzzle.Open(cc, Cm.TownName(cc.fromTown), Cm.TownName(cc.toTown), solved =>
                        {
                            message = AcceptSaved(cc, solved ? bonus : 0);
                            if (solved && message.Contains("맡았다")) message = $"꽉 묶었다! 보수 +{bonus}냥. " + message;
                        }) && !PackPuzzle.IsOpen) message = AcceptSaved(c);
                    }
                    else message = AcceptSaved(c);
                }
                y += card;
            }
            GUI.EndGroup();

            // 아래 줄: 방금 한 일 알림, 없으면 도움말
            string tip = p.TotalWeight > p.Capacity + 0.001f ? "짐이 넘쳐 의뢰를 더 실을 수 없어요 · 장터에서 팔고 다시 오세요" : "배달 짐도 짐 칸을 차지해요 · 팔 물건 자리와 비교해 보세요";
            string note = string.IsNullOrEmpty(message) ? tip : message.Replace("\n", " ");
            CuteUI.Icon(CuteUI.Pic.Bulb, content.x + 4, r.yMax - 22);
            CuteUI.Label(content.x + 15, r.yMax - 17, TradeLedgerUI.Fit(note, content.width - 20, true, false), string.IsNullOrEmpty(message) ? CuteUI.Sub : CuteUI.Text, true);
            if (hoverHelp != null && !PackPuzzle.IsOpen) HelpBox(MouseAt(Vector2.zero), hoverHelp);
        }

        /// <summary>11/19 의뢰 종류 설명 상자 (마우스 오른쪽 아래, 창 밖으로 안 나가게). 첫 줄은 굵게</summary>
        internal static void HelpBox(Vector2 at, string text)
        {
            var lines = text.Split('\n');
            float w = 0; for (int i = 0; i < lines.Length; i++) w = Mathf.Max(w, CuteUI.Width(lines[i], true, i == 0));
            w = Mathf.Round(w + 16); float h = 8 + lines.Length * 12 + 4;
            float x = Mathf.Clamp(at.x + 12, 4, UIStyle.VirtualWidth - w - 4), y = Mathf.Clamp(at.y + 12, 4, UIStyle.VirtualHeight - h - 4);
            if (at.y + 12 + h > UIStyle.VirtualHeight - 4) y = Mathf.Max(4, at.y - h - 6);
            CuteUI.Box(new Rect(x, y, w, h), 6, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            for (int i = 0; i < lines.Length; i++) CuteUI.Label(x + 8, y + 10 + i * 12, lines[i], i == 0 ? CuteUI.Text : CuteUI.Sub, true, i == 0);
        }

        internal static void DrawStatus(PlayerState p, Rect s, bool showVehicle = false)
        {
            float cy = s.center.y, x = s.x;
            // 짐
            bool over = p.TotalWeight > p.Capacity + 0.001f;
            CuteUI.Icon(CuteUI.Pic.Sack, x, cy - 5);
            x = CuteUI.Label(x + 13, cy, "짐", CuteUI.Text, true, true) + 5;
            CuteUI.Bar(new Rect(x, cy - 4, 70, 8), p.TotalWeight / Mathf.Max(0.01f, p.Capacity), over ? CuteUI.Alert : CuteUI.Load, over ? CuteUI.AlertSoft : CuteUI.LoadHi);
            x = CuteUI.Label(x + 75, cy, $"{p.TotalWeight:0.#}/{p.Capacity:0.#}", over ? CuteUI.Down : CuteUI.Text, true, over);
            if (over) x = CuteUI.Label(x + 4, cy, "넘침", CuteUI.Down, true);
            if (showVehicle) x = CuteUI.Label(x + 6, cy, "· " + (p.vehicle != null ? p.vehicle.displayName : "등짐"), CuteUI.Sub, true);
            x += 14;
            // 맡은 의뢰 칸
            CuteUI.Icon(CuteUI.Pic.Scroll, x, cy - 6);
            x = CuteUI.Label(x + 13, cy, "맡은 의뢰", CuteUI.Text, true, true) + 5;
            for (int i = 0; i < ContractManager.MaxActiveFor(p); i++)
            {
                bool on = i < p.contracts.Count;
                CuteUI.Circle(x, cy - 4, 9, on ? CuteUI.Interact : CuteUI.Paper2, on ? CuteUI.InteractLo : CuteUI.Line);
                x += 11;
            }
            CuteUI.Label(x + 2, cy, $"{p.contracts.Count}/{ContractManager.MaxActiveFor(p)}", CuteUI.Sub, true);
            // 오른쪽: 신용 · 돈
            float xr = s.xMax;
            string money = p.money.ToString("N0") + "냥";
            CuteUI.LabelRight(xr, cy, money, CuteUI.Text, false, true);
            xr -= CuteUI.Width(money, false, true) + 11;
            CuteUI.Icon(CuteUI.Pic.Coin7, xr, cy - 4);
            xr -= 12;
            Pill(xr, cy, "신용 " + Credit.Level(p), CuteUI.Interact, CuteUI.InteractLo, CuteUI.White, true);
        }

        static void Header(float y, string label, string count)
        {
            float x = CuteUI.Label(4, y + 10, label, CuteUI.Sub, true, true);
            if (count != null) CuteUI.Label(x + 5, y + 10, count, CuteUI.Sub, true);
        }

        /// <summary>작은 알약 글씨. right = true면 x가 오른쪽 끝. 반대쪽 끝 x를 돌려준다</summary>
        internal static float Pill(float x, float cy, string text, Color32 fill, Color32 line, Color32 ink, bool right = false)
        {
            float pw = Mathf.Round(CuteUI.Width(text, true) + 10);
            float px = right ? x - pw : x;
            CuteUI.Box(new Rect(px, cy - 7, pw, 14), 6, fill, line);
            CuteUI.LabelCenter(px + pw / 2f, cy, text, ink, true);
            return right ? px : px + pw;
        }

        /// <summary>의뢰 한 장. locked가 있으면 버튼이 회색이고 locked 글을 적는다. 버튼을 누르면 true</summary>
        bool DrawCard(Rect r, CuteUI.Pic pic, string title, string badge, string sub, string pay, float? weight, string action, string locked,
            bool deliver = false, bool pin = false, bool warn = false, int creditPts = 0)
        {
            var p = GameManager.I.Player;
            CuteUI.Box(r, 7, CuteUI.Paper2, warn ? CuteUI.Alert : CuteUI.Line);
            CuteUI.Circle(r.x + 6, r.y + 7, 28, CuteUI.Paper, CuteUI.Line);
            var ps = CuteUI.PicSize(pic);
            CuteUI.Icon(pic, r.x + 20 - ps.x / 2f, r.y + 21 - ps.y / 2f);

            // 오른쪽: 버튼, 그 왼쪽 보수
            float bw = locked != null ? Mathf.Max(56, Mathf.Round(CuteUI.Width(locked, true) + 14)) : 56;
            var b = new Rect(r.xMax - 7 - bw, r.y + 11, bw, 20);
            bool pressed;
            if (locked == null) pressed = CuteUI.Button(b, action) && !PackPuzzle.IsOpen;   // 11/16 짐 싣기 퍼즐 중에는 게시판 단추를 막는다
            else
            {
                CuteUI.Box(b, 8, CuteUI.ZeroSoft, CuteUI.KeyLine);
                CuteUI.LabelCenter(b.center.x, b.center.y, locked, CuteUI.Sub, true);
                pressed = GUI.Button(b, GUIContent.none, GUIStyle.none) && !PackPuzzle.IsOpen;
            }
            float payR = b.x - 10, payL = payR;
            if (pay != null)
            {
                bool deposit = pay.StartsWith("보증금");
                if (deposit)
                {
                    CuteUI.LabelRight(payR, r.y + 14, pay + "냥", CuteUI.Text, true, true);
                    CuteUI.LabelRight(payR, r.y + 28, deliver ? "돌려받음" : "성공하면 돌려받음", CuteUI.Sub, true);
                    payL = payR - Mathf.Max(CuteUI.Width(pay + "냥", true, true), CuteUI.Width(deliver ? "돌려받음" : "성공하면 돌려받음", true));
                }
                else
                {
                    CuteUI.LabelRight(payR - CuteUI.Width("냥", true) - 2, r.y + 17, pay, CuteUI.Up, false, true);
                    CuteUI.Label(payR - CuteUI.Width("냥", true), r.y + 18, "냥", CuteUI.Up, true);
                    float cx = payR - CuteUI.Width("냥", true) - 2 - CuteUI.Width(pay, false, true) - 11;
                    CuteUI.Icon(CuteUI.Pic.Coin7, cx, r.y + 13);
                    // 11/19 맡기 전에 신용 점수를 보여 준다 (빨리 오면 +1은 납품 때)
                    if (creditPts > 0) CuteUI.LabelRight(payR, r.y + 31, $"신용 +{creditPts}", CuteUI.Interact, true, true);
                    else CuteUI.LabelRight(payR, r.y + 31, "보수", CuteUI.Sub, true);
                    payL = cx;
                }
            }

            // 가운데: 제목 + 배지, 아래 줄
            float tx = r.x + 42, room = payL - 8 - tx;
            float ex = CuteUI.Label(tx, r.y + 13, TradeLedgerUI.Fit(title, room, false, true), CuteUI.Text, false, true);
            if (badge != null)
            {
                bool lackCredit = badge.StartsWith("신용") && locked != null && Credit.Level(p) < CreditOf(badge);
                if (badge == "속죄 의뢰" || badge == "길에 떨어뜨림") Pill(ex + 5, r.y + 13, badge, CuteUI.AlertSoft, CuteUI.Alert, CuteUI.Down);
                else if (badge == "급한 짐") Pill(ex + 5, r.y + 13, badge, CuteUI.AlertSoft, CuteUI.Down, CuteUI.Down);
                else if (badge == "깨지기 쉬움") Pill(ex + 5, r.y + 13, badge, CuteUI.BuySoft, CuteUI.Buy, CuteUI.Buy);
                else if (badge == "큰 주문") Pill(ex + 5, r.y + 13, badge, CuteUI.SellSoft, CuteUI.Sell, CuteUI.Sell);
                else Pill(ex + 5, r.y + 13, lackCredit ? badge + " 필요" : "귀한 의뢰", lackCredit ? CuteUI.ZeroSoft : CuteUI.SellSoft, lackCredit ? CuteUI.KeyLine : CuteUI.Sell, lackCredit ? CuteUI.Sub : CuteUI.Sell);
            }
            float sx = tx;
            if (pin) { CuteUI.Icon(CuteUI.Pic.Pin, sx, r.y + 26, 1, CuteUI.Sub); sx += 8; }
            if (weight.HasValue)
            {
                bool fits = p.CanCarryWeight(weight.Value);
                string sw = $"짐 {weight.Value:0}";
                float wx = CuteUI.Label(sx, r.y + 30, TradeLedgerUI.Fit(sub, room - CuteUI.Width(" · " + sw, true) - (pin ? 8 : 0), true, false), CuteUI.Sub, true);
                wx = CuteUI.Label(wx, r.y + 30, " · ", CuteUI.Sub, true);
                CuteUI.Label(wx, r.y + 30, sw, fits ? CuteUI.Sub : CuteUI.Down, true, !fits);
            }
            else CuteUI.Label(sx, r.y + 30, TradeLedgerUI.Fit(sub, room - (pin ? 8 : 0), true, false), CuteUI.Sub, true);
            return pressed;
        }

        static int CreditOf(string badge) { int.TryParse(badge.Replace("신용", "").Trim(), out int v); return v; }

        /// <summary>못 맡는 이유를 버튼에 들어갈 만큼 짧게</summary>
        string ShortWhy(Contract c, PlayerState p)
        {
            if (p.contracts.Count >= ContractManager.MaxActiveFor(p)) return "의뢰 가득";
            if (c.Premium && Credit.Level(p) < c.minCredit) return $"신용 {c.minCredit} 필요";
            if ((c.Premium || c.Bulk) && Loan.Late(p)) return "외상 연체 중";
            if (c.atonement && p.money < c.deposit) return "보증금 부족";
            if (!p.CanCarryWeight(c.weight)) return "짐 자리 부족";
            return "못 맡아요";
        }
    }
}
