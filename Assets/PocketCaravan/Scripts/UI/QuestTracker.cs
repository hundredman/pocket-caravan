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

        void DrawTracker(QuestManager qm)
        {
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
                GUI.Label(new Rect(x, y, w, titleHeight), title, titleStyle);
                y += titleHeight;
                string goal = "· " + qm.ProgressText(p);
                float goalHeight = UIStyle.Wrap.CalcHeight(new GUIContent(goal), w - 6);
                GUI.Label(new Rect(x + 6, y, w - 6, goalHeight), goal, UIStyle.Wrap);
                y += goalHeight + 4;
            }
            var gm = GameManager.I;
            foreach (var c in gm.Player.contracts)
            {
                any = true;
                string state = c.dropped ? "<color=#ff8a7a>떨어뜨림!</color>" : $"상태 {Mathf.RoundToInt(c.condition * 100)}%";
                GUI.Label(new Rect(x, y, w, 14), $"<color=#9fd8ff>■</color> {c.cargo} → {gm.Contracts.TownName(c.toTown)}", UIStyle.Rich);
                y += 13;
                GUI.Label(new Rect(x + 6, y, w - 6, 14), $"· {gm.Contracts.TimeLeftText(c)} · {state} · 보수 {c.reward}", UIStyle.Small);
                y += 14;
            }
            string hint = any ? "Q 여정 수첩 · 안내할 의뢰 선택" : "자유 교역 · ! 의뢰 · 게시판 배달\nQ 수첩에서 성장·주변 거래처 보기";
            GUI.Label(new Rect(x, y, w, 42), hint, UIStyle.Wrap);
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
                    if (p.quest.rewardMoney > 0) reward += $"돈 {p.quest.rewardMoney}  ";
                    if (p.quest.rewardCredit > 0) reward += $"신용 {p.quest.rewardCredit}";
                    if (reward != "") GUILayout.Label("보상: " + reward);
                    GUILayout.Space(6);
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
