using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 위 가운데에 진행 중 퀘스트와 지금 할 일을 보여 준다 (Tab이면 오른쪽 위 단추 아래 자세히). Q로 퀘스트 목록을 연다.
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class QuestTracker : MonoBehaviour
    {
        bool logOpen;
        int logTab;
        Vector2 scroll;

        void Update()
        {
            if (GameManager.I == null) return;
            GameManager.I.Quests.Tick();
            if (logOpen && (InputReader.QuestLogPressed() || InputReader.CancelPressed())) CloseLog();
            else if (InputReader.QuestLogPressed() && !UIState.Blocking && !UIState.JustClosed && !GameManager.I.IsTravelling
                && UIState.TryOpen(this, CloseLog)) logOpen = true;
            else if (logOpen)
            {
                // 11/08 ←→ 탭 바꾸기 · ↑↓ 넘기기 (마우스는 탭 단추 · 휠 · 막대)
                var mv = InputReader.MovePressed();
                if (mv.x != 0) { logTab = (logTab + mv.x + LogTabs.Length) % LogTabs.Length; scroll = Vector2.zero; Sfx.Play("open", .25f); }
                if (mv.y != 0) scroll.y = Mathf.Max(0f, scroll.y - mv.y * 40f);
            }
        }
        static readonly string[] LogTabs = { "의뢰", "성장·시설", "주변 거래처" };
        void CloseLog() { logOpen = false; UIState.Release(this); }
        void OnDisable() { if (logOpen) CloseLog(); }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UIStyle.Begin();
            GUI.depth = logOpen ? -10 : 0;
            Notify.Draw(GoryeoIntro.ReservedGuideRect);
            if (logOpen) DrawLog(gm.Quests); else DrawTracker(gm.Quests);
        }

        /// <summary>위 가운데 할 일 줄이 떠 있는지 (다음 이야기 줄·알림이 그 아래로 비킨다)</summary>
        public static bool LineVisible
        {
            get
            {
                var gm = GameManager.I;
                return gm != null && gm.Quests != null && MapRoot.Current != null && !HUD.Expanded
                    && (gm.Quests.Tracked != null || gm.Player.contracts.Count > 0);
            }
        }

        /// <summary>
        /// 10/28 평소엔 할 일 한 줄. Tab(HUD.Expanded)으로 펼치면 아래 자세히.
        /// 11/05 B안: 화면 위 가운데 알약 (두루마리 + 지금 할 일 + 퀘스트 이름). 오른쪽 위는 단추 자리.
        /// </summary>
        void DrawCompact(QuestManager qm)
        {
            var gm = GameManager.I;
            if (qm.Tracked != null)
            {
                int others = 0; foreach (var ap in qm.Active) if (ap != qm.Tracked) others++;
                string tail = qm.Tracked.quest.title + (others > 0 ? $" 외 {others}개" : "");   // 11/01 진행 중인 다른 퀘스트 수 (Q 수첩에서 안내 바꾸기)
                CuteUI.TopPill(6, false, CuteUI.Pic.Scroll, CuteUI.Plain(qm.ProgressText(qm.Tracked)), tail, false);
            }
            else if (gm.Player.contracts.Count > 0)
            {
                var c = gm.Player.contracts[0];
                string tail = CuteUI.Plain(gm.Contracts.TimeLeftText(c)) + (gm.Player.contracts.Count > 1 ? $" 외 {gm.Player.contracts.Count - 1}건" : "");
                CuteUI.TopPill(6, false, CuteUI.Pic.Sack, $"{c.cargo} → {gm.Contracts.TownName(c.toTown)}", tail, false);
            }
        }

        /// <summary>Tab으로 펼쳤을 때: 오른쪽 위 단추 아래 카드에 퀘스트·배달 자세히</summary>
        void DrawTracker(QuestManager qm)
        {
            if (!HUD.Expanded) { DrawCompact(qm); return; }
            var gm = GameManager.I;
            const float w = 212f;
            float x = UIStyle.VirtualWidth - w - 6, inner = w - 20;
            var lines = new List<(string text, bool small, bool bold, Color32 col, float indent)>();
            bool any = false;
            if (qm.Tracked != null)
            {
                any = true;
                lines.Add((qm.Tracked.quest.title, false, true, CuteUI.Text, 0));
                lines.Add(("· " + CuteUI.Plain(qm.ProgressText(qm.Tracked)), true, false, CuteUI.Text, 6));
            }
            foreach (var c in gm.Player.contracts)
            {
                any = true;
                string state = c.dropped ? "떨어뜨림!" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                lines.Add(($"{c.cargo} → {gm.Contracts.TownName(c.toTown)}", false, true, CuteUI.Text, 0));
                lines.Add(($"· {CuteUI.Plain(gm.Contracts.TimeLeftText(c))} · {state} · 보수 {c.reward}냥", true, false, c.dropped ? CuteUI.Alert : CuteUI.Text, 6));
            }
            var next = StoryGuide.Next;
            if (next.HasValue)
            {
                any = true;
                lines.Add(("다음 이야기", true, false, CuteUI.Alert, 0));
                lines.Add((next.Value.action + (string.IsNullOrEmpty(next.Value.where) ? "" : " · " + next.Value.where), false, true, CuteUI.Text, 0));
            }
            if (!any) return;   // 11/05 보일 게 없으면 카드를 띄우지 않는다 (예전 "자유 교역 · ! 의뢰…" 안내는 뺐다)

            float y = 44 + ObstacleField.TopRightUsed;   // 길 장애 알약이 있으면 그 아래
            // 11/08 아래 조작 안내 위까지만 (좁은 고려 화면에서 카드가 화면 밖으로 나갔다). 제목·자세히 두 줄씩 통째로 넣고 남는 것은 뺀다
            float room = UIStyle.VirtualHeight - 6 - HUD.BottomReserved - y, h = 14;
            int shown = 0;
            for (int i = 0; i < lines.Count; i += 2)
            {
                float pair = 0;
                for (int k = i; k < Mathf.Min(i + 2, lines.Count); k++) pair += CuteUI.WrapHeight(lines[k].text, inner - lines[k].indent, lines[k].small) + 2;
                if (shown > 0 && h + pair > room) break;
                h += pair;
                shown = Mathf.Min(i + 2, lines.Count);
            }
            CuteUI.Card(new Rect(x, y, w, h));
            y += 8;
            for (int i = 0; i < shown; i++) { var l = lines[i]; y += CuteUI.Wrap(x + 10 + l.indent, y, inner - l.indent, l.text, l.col, l.small, l.bold) + 2; }
        }

        void DrawLog(QuestManager qm)
        {
            // 11/08 읽기 쉽게: 항목마다 종이 카드(굵은 제목 + 상태 알약 + 작은 설명), 오른쪽 위 X, 얇은 스크롤 막대, ←→ 탭 · 마우스 휠
            float w = Mathf.Min(430, UIStyle.VirtualWidth - 16), h = UIStyle.WindowHeight(290);
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "여정 수첩");
            if (CuteUI.CloseButton(r.xMax - 16f, r.y - 8f)) { CloseLog(); return; }
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 18, w - 28, h - 30));
            GUILayout.BeginHorizontal();
            for (int i = 0; i < LogTabs.Length; i++)
                if (GUILayout.Button(LogTabs[i], CuteUI.PaperButtonIf(logTab == i), GUILayout.Height(20)) && logTab != i)
                { logTab = i; scroll = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(h - 30 - 26 - 24));   // 11/08 아래 조작 안내 줄이 잘려 번져 보였다: 줄 높이만큼 비움
            if (logTab == 0) DrawQuests(qm); else DrawJourney();
            GUILayout.EndScrollView();
            GUILayout.Label("←→ 탭 · ↑↓·휠 넘기기 · Q·Esc 닫기", new GUIStyle(CuteUI.PaperSmall) { alignment = TextAnchor.MiddleCenter }, GUILayout.Height(14));
            GUILayout.EndArea();
        }

        void DrawQuests(QuestManager qm)
        {
            bool any = false;
            foreach (var p in qm.Active)
            {
                any = true;
                bool on = p == qm.Tracked;
                GUILayout.BeginVertical(on ? CuteUI.PaperCardOn : CuteUI.PaperCard);
                GUILayout.BeginHorizontal();
                GUILayout.Label(p.quest.title, CuteUI.PaperTitle);
                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(p.quest.chain)) GUILayout.Label(p.quest.chain, CuteUI.Badge(CuteUI.BadgeKind.Note));
                if (on) GUILayout.Label("안내 중", CuteUI.Badge(CuteUI.BadgeKind.On));
                else if (GUILayout.Button("길 안내", CuteUI.PaperButton, GUILayout.Width(52), GUILayout.Height(16))) qm.Track(p);
                GUILayout.EndHorizontal();
                GUILayout.Label("▸ " + CuteUI.Plain(qm.ProgressText(p)), CuteUI.PaperWrap);
                if (!string.IsNullOrEmpty(p.quest.summary)) GUILayout.Label(CuteUI.Plain(p.quest.summary), CuteUI.PaperNote);
                string reward = (p.RewardMoney > 0 ? $"돈 {p.RewardMoney}냥" : "") + (p.RewardMoney > 0 && p.RewardCredit > 0 ? " · " : "") + (p.RewardCredit > 0 ? $"신용 +{p.RewardCredit}" : "");
                if (reward != "") GUILayout.Label("보상 · " + reward, CuteUI.PaperNote);
                GUILayout.EndVertical();
            }
            // 11/01 게시판에서 받은 배달 의뢰도 수첩에서 보이게
            var gm = GameManager.I;
            if (gm.Player.contracts.Count > 0)
            {
                GUILayout.Label("게시판 배달 의뢰", CuteUI.PaperSmall);
                foreach (var c in gm.Player.contracts)
                {
                    any = true;
                    GUILayout.BeginVertical(CuteUI.PaperCard);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{c.cargo} → {gm.Contracts.TownName(c.toTown)}", CuteUI.PaperTitle);
                    GUILayout.FlexibleSpace();
                    if (c.dropped) GUILayout.Label("떨어뜨림!", CuteUI.Badge(CuteUI.BadgeKind.Alert));
                    else GUILayout.Label($"상태 {Mathf.RoundToInt(c.condition * 100)}%", CuteUI.Badge(CuteUI.BadgeKind.Ok));
                    GUILayout.EndHorizontal();
                    GUILayout.Label($"{CuteUI.Plain(gm.Contracts.TimeLeftText(c))} · 보수 {c.reward}냥", CuteUI.PaperNote);
                    GUILayout.EndVertical();
                }
            }
            if (!any)
            {
                GUILayout.BeginVertical(CuteUI.PaperCard);
                GUILayout.Label("진행 중인 의뢰가 없어요", CuteUI.PaperTitle);
                GUILayout.Label("마을 사람에게 말을 걸거나 의뢰 게시판에서 배달을 받아 보세요. 자유롭게 사고팔아도 돼요.", CuteUI.PaperNote);
                GUILayout.EndVertical();
            }
        }

        void DrawJourney()
        {
            var gm = GameManager.I;
            var map = MapRoot.Current;
            var towns = JourneyGuide.Destinations(gm.Db, map != null ? map.region : null,
                WorldIndex.Get(), map != null ? map.mapId : null);
            if (logTab == 1)
            {
                GUILayout.BeginVertical(CuteUI.PaperCardOn);
                GUILayout.BeginHorizontal();
                GUILayout.Label("내 성장", CuteUI.PaperTitle);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"신용 {gm.Player.credit}", CuteUI.Badge(CuteUI.BadgeKind.On));
                GUILayout.Label($"짐 {gm.Player.TotalWeight:0.#}/{gm.Player.Capacity:0.#}", CuteUI.Badge(CuteUI.BadgeKind.Note));
                GUILayout.EndHorizontal();
                GUILayout.Label(CuteUI.Plain(JourneyGuide.NextUnlock(towns, gm.Player)), CuteUI.PaperWrap);
                GUILayout.Label("마을 방문과 기본 매매는 언제나 자유예요. 시설은 마을 등급과 내 신용에 따라 열려요.", CuteUI.PaperNote);
                GUILayout.EndVertical();
                foreach (var town in towns)
                {
                    bool header = false;
                    foreach (var f in town.facilities)
                    {
                        if (f == null || f.kind == FacilityKind.Rest || town.SettlementTier < f.minTier) continue;
                        if (!header) { GUILayout.BeginVertical(CuteUI.PaperCard); GUILayout.Label(town.displayName, CuteUI.PaperTitle); header = true; }
                        string denial = FacilityAccess.Denial(town, f, gm.Player);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("· " + f.displayName, CuteUI.PaperRich);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(denial == null ? "이용 가능" : TownFacilitiesUI.ShortDenial(denial), CuteUI.Badge(denial == null ? CuteUI.BadgeKind.Ok : CuteUI.BadgeKind.Locked));
                        GUILayout.EndHorizontal();
                        GUILayout.Label(CuteUI.Plain(JourneyGuide.FacilityEffect(f)), CuteUI.PaperNote);
                    }
                    if (header) GUILayout.EndVertical();
                }
                GUILayout.Label("수레를 사기 전에 다음 매입 자금을 남기세요. 의뢰 화물도 짐칸을 써요. 창고에 맡긴 물건은 판 것이 아니에요.", CuteUI.PaperNote);
            }
            else
            {
                foreach (var town in towns)
                {
                    bool recorded = JourneyGuide.MarketRecorded(gm.Player.ledger, town.id);
                    GUILayout.BeginVertical(CuteUI.PaperCard);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(town.displayName, CuteUI.PaperTitle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(recorded ? "장부에 시세 있음" : "시세 모름", CuteUI.Badge(recorded ? CuteUI.BadgeKind.On : CuteUI.BadgeKind.Locked));
                    GUILayout.EndHorizontal();
                    if (!string.IsNullOrEmpty(town.journeyNote)) GUILayout.Label(CuteUI.Plain(town.journeyNote), CuteUI.PaperWrap);
                    GUILayout.EndVertical();
                }
                if (towns.Count == 0) GUILayout.Label("이 맵과 이어진 거래처 정보가 없어요.", CuteUI.PaperNote);
                GUILayout.Label("장부(L)의 시세는 직접 가서 본 값이에요. 가는 동안 값이 바뀔 수 있으니 짧은 길과 큰길을 짐에 맞춰 골라 보세요.", CuteUI.PaperNote);
            }
        }
    }
}
