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
        public override Kind Category => Kind.Board;

        public override void Interact(PlayerController player)
        {
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

        string AcceptSaved(Contract c)
        {
            bool accepted=false;
            if (!SavedEconomyChange.Try(GameManager.I,()=>accepted=Cm.Accept(town,c))) return SavedEconomyChange.Failure;
            return !accepted ? "이 의뢰는 더 이상 맡을 수 없습니다. 게시판을 다시 확인해 주세요."
                : c.atonement ? $"{c.cargo} 수락 · 보증금 {c.deposit}냥\n{Cm.TownName(c.toTown)} 게시판에 납품하세요."
                : $"{Josa.Eul(c.cargo)} 맡았다. {Cm.TownName(c.toTown)} 게시판에 가져가자.";
        }

        string DeliverSaved(Contract c)
        {
            ContractResult result=default;
            return SavedEconomyChange.Try(GameManager.I,()=>result=Cm.Deliver(c,town)) ? result.message : SavedEconomyChange.Failure;
        }

        void OnGUI()
        {
            if (!open || GameManager.I == null) return;
            UIStyle.Begin();
            GUI.depth = -10;   // 다른 글자들 위에
            var gm = GameManager.I;
            var p = gm.Player;

            float w = Mathf.Min(470, UIStyle.VirtualWidth - 16), h = UIStyle.WindowHeight(310) - 6;
            var wrapRich = CuteUI.PaperWrap;
            var rect = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(rect);
            CuteUI.TitlePlate(rect.center.x, rect.y - 11, Cm.TownName(town) + " 의뢰 게시판");
            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 14, w - 28, h - 28));
            GUILayout.Label($"돈 {p.money} · 신용 {p.credit} · 짐 {p.TotalWeight:0.#}/{p.Capacity:0.#} · 의뢰 {p.contracts.Count}/{ContractManager.MaxActive}", CuteUI.PaperRich);

            GUILayout.Label($"남는 적재 공간 {Mathf.Max(0, p.Capacity-p.TotalWeight):0.#} · 배달 짐도 공간을 씁니다. 귀환 상품과 비교하세요.", CuteUI.PaperSmall);
            float footerHeight = Mathf.Clamp(CuteUI.PaperWrap.CalcHeight(new GUIContent(message ?? ""), w - 128), 18f, 60f);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Max(72f, h - 108f - (footerHeight - 18f))));

            // 0) R4 마을 일 보고: 맡은 마을의 게시판에서 한 번만 완료된다.
            var chore = p.tradeActivity.ActiveChore;
            if (chore != null && chore.town == town)
            {
                GUILayout.Label("마을 일 보고", CuteUI.PaperRich);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{StandingRecovery.ChoreTitles[chore.kind % StandingRecovery.ChoreTitles.Length]} · 보고하면 {StandingRecovery.ChoreHours:0}시간이 지납니다.", wrapRich, GUILayout.Width(w - 128));
                if (GUILayout.Button("보고", CuteUI.PaperButtonOn, GUILayout.Width(60), GUILayout.Height(18))) message = ReportChore(gm, p);
                GUILayout.EndHorizontal();
                GUILayout.Space(6);
            }

            // 1) 이 마을로 가져온 짐
            bool header = false;
            foreach (var c in p.contracts.ToArray())
            {
                if (c.toTown != town) continue;
                if (!header) { GUILayout.Label("납품할 짐", CuteUI.PaperRich); header = true; }
                GUILayout.BeginHorizontal();
                string state = c.dropped ? "<color=#ff8a7a>길에 떨어뜨림</color>" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                string pay = c.atonement ? $"보증금 {c.deposit} 반환" : $"보수 {c.reward}";
                GUILayout.Label(CuteUI.PaperColors($"{c.cargo} · {state}\n남은 시간 {Cm.TimeLeftText(c)} · {pay}"), wrapRich, GUILayout.Width(w - 128));
                GUI.enabled = !c.dropped;
                if (GUILayout.Button("납품", CuteUI.PaperButtonOn, GUILayout.Width(60), GUILayout.Height(18)))
                {
                    message = DeliverSaved(c);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (header) GUILayout.Space(6);

            // 2) 맡을 수 있는 의뢰
            GUILayout.Label("맡을 수 있는 의뢰", CuteUI.PaperRich);
            var list = Cm.Available(town);
            if (list.Count == 0) GUILayout.Label("지금은 의뢰가 없다. 몇 시간 뒤에 다시 와 보자.", CuteUI.PaperSmall);
            foreach (var c in list.ToArray())
            {
                GUILayout.BeginHorizontal();
                string route = c.quotedRouteSteps > 0 ? $" · 연결 {c.quotedRouteSteps}구간" : "";
                string premium = c.Premium ? $" <color=#ffd84a>(신용 {c.minCredit}↑)</color>" : "";
                string text = c.atonement
                    ? $"<color=#ff9a8a>속죄 의뢰</color> · {c.cargo}\n→ {Cm.TownName(c.toTown)}{route} · 짐 {c.weight:0}\n기한 {c.hours:0}시간 · 보증금 {c.deposit}냥(성공 시 반환)\n성공: 평판 회복(중립까지) · 보수 없음"
                    : $"{c.cargo}{premium}\n→ {Cm.TownName(c.toTown)}{route}\n짐 {c.weight:0} · 기한 {c.hours:0}시간 · <color=#a8f0a0>보수 {c.reward}냥</color>";
                GUILayout.Label(CuteUI.PaperColors(text), wrapRich, GUILayout.Width(w - 128));
                string why = Cm.CanAccept(c);
                if (GUILayout.Button("맡기", CuteUI.PaperButtonOn, GUILayout.Width(60), GUILayout.Height(18)))
                {
                    if (why != null) { message = why; Sfx.Play("cancel"); }
                    else message = AcceptSaved(c);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(6);
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.Label(message, CuteUI.PaperWrap, GUILayout.Width(w - 128), GUILayout.Height(footerHeight));
            if (GUILayout.Button("닫기 (Esc)", CuteUI.PaperButton, GUILayout.Width(80), GUILayout.Height(18))) Close();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
