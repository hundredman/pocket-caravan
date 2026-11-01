using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 오른쪽 위에 진행 중 퀘스트와 지금 할 일을 보여 준다. Q로 퀘스트 목록을 연다.
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
            if (InputReader.QuestLogPressed() && (logOpen || !UIState.Blocking))
            {
                logOpen = !logOpen;
                if (logOpen) UIState.Blocking = true; else UIState.Release();
            }
            else if (logOpen && InputReader.CancelPressed())
            {
                logOpen = false;
                UIState.Release();
            }
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UIStyle.Begin();
            GUI.depth = logOpen ? -10 : 0;
            Notify.Draw();
            if (logOpen) DrawLog(gm.Quests); else DrawTracker(gm.Quests);
        }

        static GUIStyle compactStyle;

        /// <summary>10/28 평소엔 할 일 한 줄 (테두리 글씨). Tab(HUD.Expanded)으로 펼치면 아래 자세히</summary>
        void DrawCompact(QuestManager qm)
        {
            var gm = GameManager.I;
            string line = null;
            if (qm.Tracked != null)
            {
                int others = 0; foreach (var ap in qm.Active) if (ap != qm.Tracked) others++;
                string more = others > 0 ? $"  <color=#9a8fb0>외 {others}개</color>" : "";   // 11/01 진행 중인 다른 퀘스트 수 (Q 수첩에서 안내 바꾸기)
                line = $"<color=#ffd84a>▸</color> {qm.Tracked.quest.title}  <color=#d8d2e6>· {qm.ProgressText(qm.Tracked)}</color>{more}";
            }
            else if (gm.Player.contracts.Count > 0)
            {
                var c = gm.Player.contracts[0];
                line = $"<color=#9fd8ff>■</color> {c.cargo} → {gm.Contracts.TownName(c.toTown)}  <color=#d8d2e6>· {gm.Contracts.TimeLeftText(c)}</color>"
                     + (gm.Player.contracts.Count > 1 ? $" 외 {gm.Player.contracts.Count - 1}건" : "");
            }
            if (line == null) return;
            if(compactStyle==null||compactStyle.font!=UIStyle.RegularFont)compactStyle = new GUIStyle(UIStyle.Rich) { wordWrap = false, clipping = TextClipping.Clip, alignment = TextAnchor.UpperRight };
            float w = Mathf.Min(300, UIStyle.VirtualWidth * 0.45f);
            // 11/01 길면 오른쪽 맞춤 때문에 앞(제목)이 잘려 "썰매 · …"처럼 보였다: 넘치면 제목을 빼고 할 일만 보인다
            if (qm.Tracked != null && compactStyle.CalcSize(new GUIContent(line)).x > w)
            {
                int others = 0; foreach (var ap in qm.Active) if (ap != qm.Tracked) others++;
                line = $"<color=#ffd84a>▸</color> <color=#d8d2e6>{qm.ProgressText(qm.Tracked)}</color>" + (others > 0 ? $"  <color=#9a8fb0>외 {others}개</color>" : "");
            }
            UIStyle.Outlined(new Rect(UIStyle.VirtualWidth - w - 8, 8, w, 16), line, compactStyle);
        }

        void DrawTracker(QuestManager qm)
        {
            if (!HUD.Expanded) { DrawCompact(qm); return; }
            float w = 200, x = UIStyle.VirtualWidth - w - 6, y = 6;
            bool any = false;
            foreach (var p in qm.Active)
            {
                if (p != qm.Tracked) continue;
                any = true;
                string mark = p == qm.Tracked ? "<color=#ffd84a>▸</color> " : "";
                var titleStyle = new GUIStyle(UIStyle.Rich) { wordWrap = true };
                string title = mark + p.quest.title;
                float titleHeight = titleStyle.CalcHeight(new GUIContent(title), w);
                UIStyle.Outlined(new Rect(x, y, w, titleHeight), title, titleStyle);
                y += titleHeight;
                string goal = "· " + qm.ProgressText(p);
                float goalHeight = UIStyle.Wrap.CalcHeight(new GUIContent(goal), w - 6);
                UIStyle.Outlined(new Rect(x + 6, y, w - 6, goalHeight), goal, UIStyle.Wrap);
                y += goalHeight + 4;
            }
            var gm = GameManager.I;
            foreach (var c in gm.Player.contracts)
            {
                any = true;
                string state = c.dropped ? "<color=#ff8a7a>떨어뜨림!</color>" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                UIStyle.Outlined(new Rect(x, y, w, 14), $"<color=#9fd8ff>■</color> {c.cargo} → {gm.Contracts.TownName(c.toTown)}", UIStyle.Rich);
                y += 13;
                UIStyle.Outlined(new Rect(x + 6, y, w - 6, 14), $"· {gm.Contracts.TimeLeftText(c)} · {state} · 보수 {c.reward}", UIStyle.Small);
                y += 14;
            }
            string hint = any ? "Q 여정 수첩 · 안내할 의뢰 선택" : "자유 교역 · ! 의뢰 · 게시판 배달\nQ 수첩에서 성장·주변 거래처 보기";
            UIStyle.Outlined(new Rect(x, y, w, 42), hint, UIStyle.Wrap);
        }

        void DrawLog(QuestManager qm)
        {
            float w = 380, h = 260;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 8, w - 20, h - 16));
            GUILayout.Label("여정 수첩 · 의뢰와 성장   (Q 닫기)", UIStyle.Rich);
            GUILayout.BeginHorizontal();
            string[] tabs = { "의뢰", "성장·시설", "주변 거래처" };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUILayout.Button((logTab == i ? "▸ " : "") + tabs[i], UIStyle.Button) && logTab != i)
                { logTab = i; scroll = Vector2.zero; }
            }
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(h - 72));
            if (logTab == 0)
            {
                bool any = false;
                foreach (var p in qm.Active)
                {
                    any = true;
                    string chain = string.IsNullOrEmpty(p.quest.chain) ? "" : $"  <color=#9a8>[{p.quest.chain}]</color>";
                    GUILayout.Label($"{p.quest.title}{chain}", new GUIStyle(UIStyle.Rich) { wordWrap = true });
                    if (GUILayout.Button(p == qm.Tracked ? "안내 중" : "이 의뢰 길 안내", UIStyle.Button)) qm.Track(p);
                    if (!string.IsNullOrEmpty(p.quest.summary)) GUILayout.Label(p.quest.summary, UIStyle.Wrap);
                    GUILayout.Label("지금 할 일: " + qm.ProgressText(p), UIStyle.Wrap);
                    string reward = "";
                    if (p.RewardMoney > 0) reward += $"돈 {p.RewardMoney}  ";
                    if (p.RewardCredit > 0) reward += $"신용 {p.RewardCredit}";
                    if (reward != "") GUILayout.Label("보상: " + reward);
                    GUILayout.Space(6);
                }
                // 11/01 게시판에서 받은 배달 의뢰도 수첩에서 보이게 (전엔 Tab으로 펼쳐야만 보였다)
                var gmc = GameManager.I;
                if (gmc.Player.contracts.Count > 0)
                {
                    GUILayout.Label("<color=#9fd8ff>게시판 배달 의뢰</color>", UIStyle.Rich);
                    foreach (var c in gmc.Player.contracts)
                    {
                        any = true;
                        string state = c.dropped ? "<color=#ff8a7a>떨어뜨림!</color>" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                        GUILayout.Label($"<color=#9fd8ff>■</color> {c.cargo} → {gmc.Contracts.TownName(c.toTown)}", new GUIStyle(UIStyle.Rich) { wordWrap = true });
                        GUILayout.Label($"{gmc.Contracts.TimeLeftText(c)} · {state} · 보수 {c.reward}냥", UIStyle.Wrap);
                        GUILayout.Space(4);
                    }
                }
                if (!any) GUILayout.Label("진행 중인 퀘스트가 없다. 자유 거래와 게시판 배달도 선택할 수 있다.", UIStyle.Wrap);
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
                GUILayout.Label($"내 성장 · 신용 {gm.Player.credit} · 적재 {gm.Player.TotalWeight:0.#}/{gm.Player.Capacity:0.#}", UIStyle.Rich);
                GUILayout.Label("마을 방문·기본 매매는 자유입니다. 거점 등급은 마을의 시설 규모, 신용은 내 이용 조건입니다.", UIStyle.Wrap);
                GUILayout.Label(JourneyGuide.NextUnlock(towns, gm.Player), UIStyle.Wrap);
                foreach (var town in towns)
                    foreach (var f in town.facilities)
                        if (f != null && f.kind != FacilityKind.Rest && town.SettlementTier >= f.minTier)
                        {
                            string denial = FacilityAccess.Denial(town, f, gm.Player);
                            GUILayout.Label($"{town.displayName} · {f.displayName}: {(denial ?? "이용 가능")}\n{JourneyGuide.FacilityEffect(f)}", UIStyle.Wrap);
                        }
                GUILayout.Label("수레를 사기 전에는 다음 매입 자금도 남기세요. 의뢰 화물도 적재 공간을 씁니다. 창고에 맡긴 물건은 매각된 것이 아닙니다.", UIStyle.Wrap);
            }
            else
            {
                GUILayout.Label("주변 거래처 · 역할과 귀환 장사", UIStyle.Rich);
                foreach (var town in towns)
                {
                    bool recorded = JourneyGuide.MarketRecorded(gm.Player.ledger, town.id);
                    GUILayout.Label($"{town.displayName} · {(recorded ? "장부 기록 있음" : "시세 미확인")}", UIStyle.Rich);
                    if (!string.IsNullOrEmpty(town.journeyNote)) GUILayout.Label(town.journeyNote, UIStyle.Wrap);
                }
                if (towns.Count == 0) GUILayout.Label("현재 맵에 연결된 지역 거래처 정보가 없습니다.", UIStyle.Wrap);
                GUILayout.Label("L 장부는 직접 확인했던 첫 1개 시세입니다. 이동 후 시세·전량 판매 수익은 달라질 수 있습니다. 짧은 길과 수레 큰길을 화물에 맞춰 비교하세요.", UIStyle.Wrap);
            }
        }
    }
}
