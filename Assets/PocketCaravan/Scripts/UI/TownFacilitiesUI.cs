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
            instance.scroll = Vector2.zero; instance.detail = ""; instance.message = "이 창을 닫은 뒤 Q: 성장·주변 거래처 / L: 방문 시세"; UIState.Blocking = true;
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
            else if (f.kind == FacilityKind.RouteInformation) { detail = f.description; message = "길 안내를 열었습니다. 본문을 내려 읽을 수 있습니다."; scroll = Vector2.zero; }
            else { warehouse = f; scroll.y = 10000; message = "이 마을에 맡긴 물건은 여기서 다시 찾을 수 있습니다."; }
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

        // R4: 마이너스일 때만 보인다. 세 길 모두 중립(0점)까지만 올린다.
        void DrawRecovery(GameManager gm, PlayerState p)
        {
            if (town.region == null) return;
            var scope = ReputationPricing.Scope(town); string target = ReputationPricing.Target(town);
            if (!StandingRecovery.Negative(p.reputation, scope, target)) return;
            GUILayout.Label($"평판 회복 · {ReputationAwards.TargetName(gm.Db, scope, target)} · 중립까지 {StandingRecovery.Missing(p.reputation, scope, target)}점", CuteUI.PaperWrap);
            var chore = p.tradeActivity.ActiveChore;
            GUILayout.BeginHorizontal();
            if (chore != null)
                GUILayout.Label($"마을 일 진행 중: {StandingRecovery.ChoreTitles[chore.kind % StandingRecovery.ChoreTitles.Length]} ({gm.Contracts.TownName(chore.town)}) · {StandingRecovery.ChoreReport}", CuteUI.PaperWrap);
            else
            {
                int kind = StandingRecovery.ChoreKind(town.id, gm.Clock.day);
                GUILayout.Label($"① {StandingRecovery.ChoreTitles[kind]} · 무료 · 평판 +{ReputationAwards.PointsFor(StandingRecovery.ChoreSize)}", CuteUI.PaperWrap, GUILayout.Width(Mathf.Min(530, UIStyle.VirtualWidth-16)-134));
                if (GUILayout.Button("맡기", CuteUI.PaperButtonOn, GUILayout.Width(52))) StartChore(gm, p, kind, scope, target);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label($"② 기부: {StandingRecovery.CoinsPerPoint}냥 = 1점, 필요한 만큼만 받음", CuteUI.PaperWrap);
            GUILayout.BeginHorizontal();
            foreach (int amount in DonationSteps)
            {
                int cost = Mathf.Min(StandingRecovery.DonationCost(p.reputation, scope, target, amount), p.money);
                GUI.enabled = cost >= StandingRecovery.CoinsPerPoint;
                if (GUILayout.Button($"{amount}냥", CuteUI.PaperButton, GUILayout.Width(52))) Donate(gm, p, amount, scope, target);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label("③ 속죄 의뢰: 게시판에서 · 보증금 필요 · 빠듯한 기한", CuteUI.PaperWrap);
            GUILayout.Space(8);
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

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin(); GUI.depth = -12;
            var gm = GameManager.I; var p = gm.Player;
            float w = Mathf.Min(530, UIStyle.VirtualWidth - 16), h = UIStyle.WindowHeight(326) - 6;
            var rect = new Rect(Mathf.Round((UIStyle.VirtualWidth-w)/2), Mathf.Round((UIStyle.VirtualHeight-h)/2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(rect);
            CuteUI.TitlePlate(rect.center.x, rect.y - 11, town.displayName + " 시설");
            GUILayout.BeginArea(new Rect(rect.x+14, rect.y+14, w-28, h-68));
            GUILayout.Label($"거점 {town.SettlementTier}등급 · 내 신용 {p.credit}", CuteUI.PaperRich);
            GUILayout.Label("마을 방문·기본 매매는 신용과 관계없이 가능합니다.", CuteUI.PaperSmall);
            scroll = GUILayout.BeginScrollView(scroll);
            DrawRecovery(gm, p);
            if (!string.IsNullOrEmpty(detail))
            {
                GUILayout.Label(CuteUI.PaperColors(detail), CuteUI.PaperWrap);
                GUILayout.Space(8);
            }
            foreach (var f in town.facilities)
            {
                if (f == null) continue;
                string denial = FacilityAccess.Denial(town, f, p);
                GUILayout.Label($"{f.displayName} · 거점 {f.minTier}등급 / 신용 {f.minCredit}", CuteUI.PaperRich);
                GUILayout.BeginHorizontal();
                GUILayout.Label(denial ?? (f.kind == FacilityKind.Warehouse ? $"보관 한도 {f.storageCapacity:0} · 일반 교역품" : f.kind == FacilityKind.Rest ? $"{Inn.Price}냥 · {Inn.SleepHours:0}시간 자고 체력 가득 · {Inn.BuffHours:0}시간 덜 지침" : "주변 길과 거래처 안내"), CuteUI.PaperSmall);
                GUI.enabled = denial == null;
                if (GUILayout.Button("이용", CuteUI.PaperButton, GUILayout.Width(52))) Use(f);
                GUI.enabled = true; GUILayout.EndHorizontal();
            }
            if (warehouse != null)
            {
                GUILayout.Label("창고 · 한 개씩 맡기기 / 찾기", CuteUI.PaperRich);
                foreach (var item in gm.Db.items)
                {
                    if (item == null || item.questItem || p.cargo.Count(item) == 0) continue;
                    GUILayout.BeginHorizontal(); GUILayout.Label($"{item.displayName} 보유 {p.cargo.Count(item)}개", CuteUI.PaperSmall);
                    if (GUILayout.Button("맡기기", CuteUI.PaperButton, GUILayout.Width(60))) Transfer(item, true);
                    GUILayout.EndHorizontal();
                }
                foreach (var s in new List<StoredCargo>(p.warehouse.At(town.id)))
                {
                    var item = gm.ItemById(s.itemId);
                    GUILayout.BeginHorizontal(); GUILayout.Label($"{(item != null ? item.displayName : s.itemId)} 보관 {s.count}개", CuteUI.PaperSmall);
                    GUI.enabled = item != null;
                    if (GUILayout.Button("찾기", CuteUI.PaperButton, GUILayout.Width(60))) Transfer(item, false);
                    GUI.enabled = true; GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView(); GUILayout.EndArea();
            GUI.Label(new Rect(rect.x+12, rect.yMax-46, w-118, 34), message ?? "", CuteUI.PaperWrap);
            if (GUI.Button(new Rect(rect.xMax-96, rect.yMax-26, 82, 20), "닫기 (Esc)", CuteUI.PaperButton)) Close();
        }
    }
}
