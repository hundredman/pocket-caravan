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
        }
        void CloseLog() { logOpen = false; UIState.Release(this); }
        void OnDisable() { if (logOpen) CloseLog(); }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UIStyle.Begin();
            GUI.depth = logOpen ? -10 : 0;
            Notify.Draw();
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
            // 11/05 UI B안: 종이 창 + 제목 리본, 글씨·버튼은 CuteUI 종이 모양 (내용·기능은 그대로)
            float w = 380, h = UIStyle.WindowHeight(260);   // 11/08 고려 화면(높이 270)에서 아래가 잘렸다
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "여정 수첩");
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 14, w - 28, h - 24));
            GUILayout.Label("의뢰와 성장 · Q 닫기", CuteUI.PaperSmall);
            GUILayout.BeginHorizontal();
            string[] tabs = { "의뢰", "성장·시설", "주변 거래처" };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUILayout.Button(tabs[i], CuteUI.PaperButtonIf(logTab == i)) && logTab != i)
                { logTab = i; scroll = Vector2.zero; }
            }
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(h - 80));
            if (logTab == 0)
            {
                bool any = false;
                foreach (var p in qm.Active)
                {
                    any = true;
                    string chain = string.IsNullOrEmpty(p.quest.chain) ? "" : $"  <color=#6a5464>[{p.quest.chain}]</color>";
                    GUILayout.Label($"{p.quest.title}{chain}", new GUIStyle(CuteUI.PaperRich) { wordWrap = true });
                    if (GUILayout.Button(p == qm.Tracked ? "안내 중" : "이 의뢰 길 안내", CuteUI.PaperButtonIf(p == qm.Tracked))) qm.Track(p);
                    if (!string.IsNullOrEmpty(p.quest.summary)) GUILayout.Label(p.quest.summary, CuteUI.PaperWrap);
                    GUILayout.Label("지금 할 일: " + qm.ProgressText(p), CuteUI.PaperWrap);
                    string reward = "";
                    if (p.RewardMoney > 0) reward += $"돈 {p.RewardMoney}  ";
                    if (p.RewardCredit > 0) reward += $"신용 {p.RewardCredit}";
                    if (reward != "") GUILayout.Label("보상: " + reward, CuteUI.PaperRich);
                    GUILayout.Space(6);
                }
                // 11/01 게시판에서 받은 배달 의뢰도 수첩에서 보이게 (전엔 Tab으로 펼쳐야만 보였다)
                var gmc = GameManager.I;
                if (gmc.Player.contracts.Count > 0)
                {
                    GUILayout.Label("<color=#1a78b8>게시판 배달 의뢰</color>", CuteUI.PaperRich);
                    foreach (var c in gmc.Player.contracts)
                    {
                        any = true;
                        string state = c.dropped ? "<color=#d6334f>떨어뜨림!</color>" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                        GUILayout.Label($"<color=#1a78b8>■</color> {c.cargo} → {gmc.Contracts.TownName(c.toTown)}", new GUIStyle(CuteUI.PaperRich) { wordWrap = true });
                        GUILayout.Label($"{gmc.Contracts.TimeLeftText(c)} · {state} · 보수 {c.reward}냥", CuteUI.PaperWrap);
                        GUILayout.Space(4);
                    }
                }
                if (!any) GUILayout.Label("진행 중인 퀘스트가 없다. 자유 거래와 게시판 배달도 선택할 수 있다.", CuteUI.PaperWrap);
            }
            else DrawJourney();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        void DrawJourney()
        {
            var gm = GameManager.I;
            var map = MapRoot.Current;
            var towns = JourneyGuide.Destinations(gm.Db, map != null ? map.region : null,
                WorldIndex.Get(), map != null ? map.mapId : null);
            if (logTab == 1)
            {
                GUILayout.Label($"내 성장 · 신용 {gm.Player.credit} · 적재 {gm.Player.TotalWeight:0.#}/{gm.Player.Capacity:0.#}", CuteUI.PaperRich);
                GUILayout.Label("마을 방문·기본 매매는 자유입니다. 거점 등급은 마을의 시설 규모, 신용은 내 이용 조건입니다.", CuteUI.PaperWrap);
                GUILayout.Label(JourneyGuide.NextUnlock(towns, gm.Player), CuteUI.PaperWrap);
                foreach (var town in towns)
                    foreach (var f in town.facilities)
                        if (f != null && f.kind != FacilityKind.Rest && town.SettlementTier >= f.minTier)
                        {
                            string denial = FacilityAccess.Denial(town, f, gm.Player);
                            GUILayout.Label($"{town.displayName} · {f.displayName}: {(denial ?? "이용 가능")}\n{JourneyGuide.FacilityEffect(f)}", CuteUI.PaperWrap);
                        }
                GUILayout.Label("수레를 사기 전에는 다음 매입 자금도 남기세요. 의뢰 화물도 적재 공간을 씁니다. 창고에 맡긴 물건은 매각된 것이 아닙니다.", CuteUI.PaperWrap);
            }
            else
            {
                GUILayout.Label("주변 거래처 · 역할과 귀환 장사", CuteUI.PaperRich);
                foreach (var town in towns)
                {
                    bool recorded = JourneyGuide.MarketRecorded(gm.Player.ledger, town.id);
                    GUILayout.Label($"{town.displayName} · {(recorded ? "장부 기록 있음" : "시세 미확인")}", CuteUI.PaperRich);
                    if (!string.IsNullOrEmpty(town.journeyNote)) GUILayout.Label(town.journeyNote, CuteUI.PaperWrap);
                }
                if (towns.Count == 0) GUILayout.Label("현재 맵에 연결된 지역 거래처 정보가 없습니다.", CuteUI.PaperWrap);
                GUILayout.Label("L 장부는 직접 확인했던 첫 1개 시세입니다. 이동 후 시세·전량 판매 수익은 달라질 수 있습니다. 짧은 길과 수레 큰길을 화물에 맞춰 비교하세요.", CuteUI.PaperWrap);
            }
        }
    }
}
