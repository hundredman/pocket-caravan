using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>장터에서 이용하는 시설. 거점 등급과 개인 신용을 함께 보여 준다.</summary>
    public class TownFacilitiesUI : MonoBehaviour
    {
        static TownFacilitiesUI instance;
        TownData town;
        TownFacility warehouse;
        bool open;
        int openedFrame;
        Vector2 scroll;
        string message = "", detail = "";
        void Awake() => instance = this;
        public static void Show(TownData value)
        {
            if (instance == null || value == null || UIState.Blocking) return;
            instance.town = value; instance.warehouse = null;
            instance.open = true; instance.openedFrame = Time.frameCount;
            instance.scroll = Vector2.zero; instance.detail = ""; instance.message = ""; UIState.Blocking = true;
        }
        void Close() { open = false; UIState.Release(); }
        void OnDisable() { if (open) Close(); if (instance == this) instance = null; }
        void Update()
        {
            if (open && Time.frameCount != openedFrame && InputReader.CancelPressed()) Close();
        }
        bool InRange()
        {
            var p = PlayerController.Instance;
            if (p == null || GameManager.I == null || GameManager.I.IsTravelling) return false;
            foreach (var m in FindObjectsByType<TownMarket>(FindObjectsSortMode.None))
                if (m.town == town && Vector2.Distance(m.transform.position, p.transform.position) <= p.talkRange + 0.1f) return true;
            return false;
        }
        void Use(TownFacility f)
        {
            var gm = GameManager.I;
            message = !InRange() ? "장터 가까이에서 이용하세요." : FacilityAccess.Denial(town, f, gm.Player);
            if (message != null) return;
            detail = "";
            if (f.kind == FacilityKind.Rest) { Close(); Inn.Offer(f.displayName); return; }   // 2026-11-03 여관 통합: 공용 Inn이 묻고 재운다
            else if (f.kind == FacilityKind.RouteInformation) { detail = f.description; message = "길 안내를 펼쳤어요."; }
            else { warehouse = f; message = "이 마을에 맡긴 물건은 여기서 다시 찾을 수 있어요."; }
            gm.SaveNow();
        }
        void Transfer(ItemData item, bool deposit)
        {
            if (!InRange()) { message = "장터 가까이에서 이용하세요."; return; }
            var gm = GameManager.I;
            string denial = gm.Player.warehouse.Transfer(town, warehouse, gm.Player, item, deposit, gm.ItemById);
            message = denial ?? $"{item.displayName} 1개를 " + (deposit ? "맡겼습니다." : "찾았습니다.");
            if (denial == null) gm.SaveNow();
        }
        static readonly int[] DonationSteps = { 50, 100, 200 };

        static readonly System.Text.RegularExpressions.Regex NeedRule = new System.Text.RegularExpressions.Regex(@"^(신용|거점) ?(\d+)(등급)? ?이상 필요");
        /// <summary>11/08 막힌 까닭을 알약에 들어갈 만큼 짧게: "신용 1 이상 필요 (현재 0)" → "신용 1 필요"</summary>
        public static string ShortDenial(string denial)
        {
            if (string.IsNullOrEmpty(denial)) return "";
            var m = NeedRule.Match(denial);
            if (m.Success) return $"{m.Groups[1].Value} {m.Groups[2].Value}{m.Groups[3].Value} 필요";
            string plain = CuteUI.Plain(denial).Trim().TrimEnd('.');
            return plain.Length <= 12 ? plain : plain.Substring(0, 11) + "…";
        }

        // R4: 마이너스일 때만 보인다. 세 길 모두 중립(0점)까지만 올린다. 11/08 카드 모양으로 (그린 높이를 돌려준다)
        float DrawRecovery(GameManager gm, PlayerState p, float y, float w)
        {
            if (town.region == null) return y;
            var scope = ReputationPricing.Scope(town); string target = ReputationPricing.Target(town);
            if (!StandingRecovery.Negative(p.reputation, scope, target)) return y;
            float top = y;
            CuteUI.Box(new Rect(0, y, w, 96), 7, CuteUI.AlertSoft, CuteUI.Alert);
            CuteUI.Label(10, y + 11, $"평판 회복 · {ReputationAwards.TargetName(gm.Db, scope, target)}", CuteUI.Down, false, true);
            CuteUI.LabelRight(w - 10, y + 11, $"중립까지 {StandingRecovery.Missing(p.reputation, scope, target)}점", CuteUI.Down, true, true);
            y += 22;
            var chore = p.tradeActivity.ActiveChore;
            if (chore != null)
                CuteUI.Label(10, y + 9, TradeLedgerUI.Fit($"① 마을 일 진행 중 · {StandingRecovery.ChoreTitles[chore.kind % StandingRecovery.ChoreTitles.Length]} ({gm.Contracts.TownName(chore.town)}) · 게시판에 알려 주세요", w - 20, true, false), CuteUI.Text, true);
            else
            {
                int kind = StandingRecovery.ChoreKind(town.id, gm.Clock.day);
                CuteUI.Label(10, y + 9, $"① 마을 일 · {StandingRecovery.ChoreTitles[kind]} · 무료 · 평판 +{ReputationAwards.PointsFor(StandingRecovery.ChoreSize)}", CuteUI.Text, true);
                if (CuteUI.Button(new Rect(w - 58, y, 50, 18), "맡기")) StartChore(gm, p, kind, scope, target);
            }
            y += 23;
            bool poor = p.money < StandingRecovery.CoinsPerPoint;
            CuteUI.Label(10, y + 9, poor ? $"② 기부 · 돈이 모자라요 ({StandingRecovery.CoinsPerPoint}냥부터)" : $"② 기부 · {StandingRecovery.CoinsPerPoint}냥 = 1점 · 필요한 만큼만 받아요", CuteUI.Text, true);
            float bx = w - 8;
            for (int i = DonationSteps.Length - 1; i >= 0; i--)
            {
                int amount = DonationSteps[i];
                int cost = Mathf.Min(StandingRecovery.DonationCost(p.reputation, scope, target, amount), p.money);
                var b = new Rect(bx - 46, y, 46, 18); bx -= 50;
                if (cost >= StandingRecovery.CoinsPerPoint) { if (CuteUI.SoftButton(b, amount + "냥")) Donate(gm, p, amount, scope, target); }
                else { CuteUI.Box(b, 8, CuteUI.ZeroSoft, CuteUI.KeyLine); CuteUI.LabelCenter(b.center.x, b.center.y, amount + "냥", CuteUI.Sub, true); }
            }
            y += 23;
            CuteUI.Label(10, y + 9, "③ 속죄 의뢰 · 의뢰 게시판에서 · 보증금 필요 · 빠듯한 기한", CuteUI.Text, true);
            return top + 96 + 8;
        }

        void StartChore(GameManager gm, PlayerState p, int kind, ReputationScope scope, string target)
        {
            if (!InRange()) { message = "장터 가까이에서 이용하세요."; return; }
            message = StandingRecovery.ChoreDenial(p.tradeActivity, p.reputation, scope, target);
            if (message != null) return;
            if (!SavedEconomyChange.Try(gm,()=>p.tradeActivity.StartChore(town.id, kind, gm.Clock.TotalHours)))
            { message=SavedEconomyChange.Failure; return; }
            message = "마을 일을 맡았습니다.\n이 마을 게시판에 보고하세요.";
            Sfx.Play("accept");
        }

        void Donate(GameManager gm, PlayerState p, int amount, ReputationScope scope, string target)
        {
            if (!InRange()) { message = "장터 가까이에서 이용하세요."; return; }
            int paid=0, gained=0;
            if (!SavedEconomyChange.Try(gm,()=>{
                (paid,gained)=StandingRecovery.Donate(p.reputation,scope,target,amount,p.money);
                p.money-=paid;
            })) { message=SavedEconomyChange.Failure; return; }
            if (paid<=0) { message="기부할 수 없습니다."; return; }
            message=$"{paid}냥을 기부했습니다. 평판 +{gained}.";
            Sfx.Play("coin");
        }

        // 11/08 은샘: 시설 창을 장부·게시판과 같은 모양으로. 위 줄 = 마을·거점 등급·신용·돈, 시설마다 그림·효과·이용 단추,
        // 못 쓰는 시설은 회색 단추에 이유(신용 1 필요)를 적는다. 길 안내·보관소는 그 시설 카드 아래에 펼친다.
        float contentH;
        Rect lastWindow;
        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin(); GUI.depth = -12;
            var gm = GameManager.I; var p = gm.Player;
            float w = Mathf.Min(500, UIStyle.VirtualWidth - 16), h = Mathf.Min(286, UIStyle.VirtualHeight - 34);
            var r = lastWindow = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 8), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, town.displayName + " 시설");
            CuteUI.Keycap(r.xMax - 32 - CuteUI.KeycapWidth("Esc") - 3, r.y - 3, "Esc");
            if (CuteUI.CloseButton(r.xMax - 32, r.y - 9)) { Close(); return; }

            // 위 줄
            float cy = r.y + 27, x = r.x + 15;
            CuteUI.Icon(CuteUI.Pic.Pin, x, cy - 5, 1, CuteUI.Sub);
            x = CuteUI.Label(x + 9, cy, town.displayName, CuteUI.Text, false, true);
            x = ContractBoard.Pill(x + 6, cy, $"거점 {town.SettlementTier}등급", CuteUI.Paper, CuteUI.Line, CuteUI.Sub);
            CuteUI.Label(x + 6, cy, "큰 마을일수록 시설이 많아요", CuteUI.Sub, true);
            float xr = r.xMax - 15;
            string money = p.money.ToString("N0") + "냥";
            CuteUI.LabelRight(xr, cy, money, CuteUI.Text, false, true);
            xr -= CuteUI.Width(money, false, true) + 11;
            CuteUI.Icon(CuteUI.Pic.Coin7, xr, cy - 4);
            ContractBoard.Pill(xr - 12, cy, "신용 " + p.credit, CuteUI.Interact, CuteUI.InteractLo, CuteUI.White, true);

            var content = new Rect(r.x + 15, r.y + 43, w - 30, h - 43 - 30);
            CuteUI.Box(content, 7, CuteUI.Paper, CuteUI.Ink);
            var area = new Rect(content.x + 6, content.y + 6, content.width - 12, content.height - 12);
            scroll.y = TradeLedgerUI.Scroll(area, scroll.y, contentH);
            GUI.BeginGroup(area);
            float cw = area.width - (contentH > area.height ? 7 : 0);
            float y = DrawRecovery(gm, p, -scroll.y, cw);
            foreach (var f in town.facilities)
            {
                if (f == null) continue;
                y = DrawFacility(gm, p, f, y, cw);
                if (f.kind == FacilityKind.RouteInformation && !string.IsNullOrEmpty(detail)) y = DrawRouteInfo(y, cw);
                if (f == warehouse) y = DrawWarehouse(gm, p, y, cw);
            }
            contentH = y + scroll.y;
            GUI.EndGroup();

            string note = string.IsNullOrEmpty(message) ? "할 일 카드: 성장·주변 거래처 · L: 방문 시세" : CuteUI.Plain(message).Replace("\n", " ");
            CuteUI.Icon(CuteUI.Pic.Bulb, content.x + 4, r.yMax - 22);
            CuteUI.Label(content.x + 15, r.yMax - 17, TradeLedgerUI.Fit(note, content.width - 20, true, false), CuteUI.Sub, true);
        }

        float DrawFacility(GameManager gm, PlayerState p, TownFacility f, float y, float w)
        {
            string denial = FacilityAccess.Denial(town, f, p);
            bool ok = denial == null;
            bool opened = (f.kind == FacilityKind.RouteInformation && !string.IsNullOrEmpty(detail)) || f == warehouse;
            CuteUI.Box(new Rect(0, y, w, 40), 7, opened ? CuteUI.Paper : CuteUI.Paper2, opened ? CuteUI.Interact : CuteUI.Line, opened ? 2 : 1);
            var pic = f.kind == FacilityKind.Rest ? CuteUI.Pic.Bolt : f.kind == FacilityKind.Warehouse ? CuteUI.Pic.Sack : CuteUI.Pic.Compass;
            var ps = CuteUI.PicSize(pic);
            CuteUI.Circle(6, y + 6, 28, CuteUI.Paper, CuteUI.Line);
            CuteUI.Icon(pic, 20 - ps.x / 2f, y + 20 - ps.y / 2f);
            string label = ok ? (opened ? "닫기" : "이용") : ShortDenial(denial);
            float bw = ok ? 52 : Mathf.Max(52, Mathf.Round(CuteUI.Width(label, true) + 14));
            var b = new Rect(w - 7 - bw, y + 10, bw, 20);
            if (ok)
            {
                if (opened ? CuteUI.SoftButton(b, label) : CuteUI.Button(b, label))
                {
                    if (opened) { if (f == warehouse) warehouse = null; else detail = ""; message = ""; }
                    else Use(f);
                }
            }
            else
            {
                CuteUI.Box(b, 8, CuteUI.ZeroSoft, CuteUI.KeyLine);
                CuteUI.LabelCenter(b.center.x, b.center.y, label, CuteUI.Sub, true);
                if (GUI.Button(b, GUIContent.none, GUIStyle.none)) { message = denial; Sfx.Play("cancel"); }
            }
            CuteUI.Label(42, y + 13, f.displayName, ok ? CuteUI.Text : CuteUI.Sub, false, true);
            string effect = f.kind == FacilityKind.Warehouse ? $"맡겨 둘 수 있는 무게 {f.storageCapacity:0} · 일반 교역품"
                : f.kind == FacilityKind.Rest ? (gm.Clock.Shared ? $"푹 쉬고 {Inn.BuffHours:0}시간 덜 지침" : $"자고 체력 가득 · {Inn.BuffHours:0}시간 덜 지침")
                : "주변 길과 거래처를 알려 줘요";
            float ex = 42;
            if (f.kind == FacilityKind.Rest)
            {
                CuteUI.Icon(CuteUI.Pic.Coin7, ex, y + 24);
                ex = CuteUI.Label(ex + 10, y + 28, Inn.Price + "냥", CuteUI.Text, true, true) + 6;
            }
            CuteUI.Label(ex, y + 28, TradeLedgerUI.Fit(effect, b.x - 8 - ex, true, false), CuteUI.Sub, true);
            return y + 44;
        }

        float DrawRouteInfo(float y, float w)
        {
            string text = CuteUI.Plain(detail);
            float th = CuteUI.WrapHeight(text, w - 28, true);
            CuteUI.Box(new Rect(10, y - 2, w - 10, th + 14), 7, CuteUI.Paper, CuteUI.Line);
            CuteUI.Wrap(18, y + 5, w - 28, text, CuteUI.Text, true);
            return y + th + 18;
        }

        float DrawWarehouse(GameManager gm, PlayerState p, float y, float w)
        {
            var stored = new List<StoredCargo>(p.warehouse.At(town.id));
            float used = 0;
            foreach (var s in stored) { var it = gm.ItemById(s.itemId); if (it != null) used += it.weight * s.count; }
            var carried = new List<ItemData>();
            foreach (var item in gm.Db.items) if (item != null && !item.questItem && p.cargo.Count(item) > 0) carried.Add(item);
            int rowsN = Mathf.Max(1, Mathf.Max(carried.Count, stored.Count));
            float bh = 34 + rowsN * 22 + 6;
            CuteUI.Box(new Rect(10, y - 2, w - 10, bh), 7, CuteUI.Paper, CuteUI.Line);
            float half = (w - 10) / 2f, lx = 18, rx = 10 + half + 6;
            CuteUI.Label(lx, y + 8, "내 짐 → 맡기기", CuteUI.Sub, true, true);
            CuteUI.Label(rx, y + 8, "보관 중 → 찾기", CuteUI.Sub, true, true);
            float cap = warehouse != null ? warehouse.storageCapacity : 0;
            CuteUI.Bar(new Rect(rx + 80, y + 4, half - 130, 8), cap > 0 ? used / cap : 0, CuteUI.Load, CuteUI.LoadHi);
            CuteUI.LabelRight(w - 8, y + 8, $"{used:0.#}/{cap:0}", CuteUI.Sub, true);
            for (float yy = y + 18; yy < y + bh - 8; yy += 3) CuteUI.Fill(new Rect(10 + half, yy, 1, 1), CuteUI.Line);
            float ry = y + 20;
            if (carried.Count == 0) CuteUI.Label(lx, ry + 10, "맡길 물건이 없어요", CuteUI.Sub, true);
            foreach (var item in carried)
            {
                if (WarehouseRow(item, item.displayName, p.cargo.Count(item), lx, ry, half - 16, "맡기기")) Transfer(item, true);
                ry += 22;
            }
            ry = y + 20;
            if (stored.Count == 0) CuteUI.Label(rx, ry + 10, "맡긴 물건이 없어요", CuteUI.Sub, true);
            foreach (var s in stored)
            {
                var item = gm.ItemById(s.itemId);
                if (WarehouseRow(item, item != null ? item.displayName : s.itemId, s.count, rx, ry, half - 16, "찾기") && item != null) Transfer(item, false);
                ry += 22;
            }
            return y + bh + 4;
        }

        static bool WarehouseRow(ItemData item, string name, int count, float x, float y, float w, string action)
        {
            CuteUI.Box(new Rect(x, y, w, 20), 6, CuteUI.Paper2, CuteUI.Paper2, 0);
            TradeLedgerUI.ItemIcon(item, x + 2, y + 2, 1);
            float nx = CuteUI.Label(x + 20, y + 10, TradeLedgerUI.Fit(name, w - 100, true, true), CuteUI.Text, true, true);
            CuteUI.Label(nx + 4, y + 10, count + "개", CuteUI.Sub, true);
            return CuteUI.SoftButton(new Rect(x + w - 46, y + 2, 44, 16), action);
        }
    }
}
