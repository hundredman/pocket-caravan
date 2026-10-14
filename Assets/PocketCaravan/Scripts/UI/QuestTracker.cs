using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 오른쪽 위에 진행 중 퀘스트와 지금 할 일을 보여 준다. J로 퀘스트 목록을 연다.
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class QuestTracker : MonoBehaviour
    {
        bool logOpen;

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
                any = true;
                string mark = p == qm.Tracked ? "<color=#ffd84a>▸</color> " : "";
                GUI.Label(new Rect(x, y, w, 16), $"{mark}{p.quest.title}", UIStyle.Rich);
                y += 14;
                GUI.Label(new Rect(x + 6, y, w - 6, 30), "· " + qm.ProgressText(p), UIStyle.Wrap);
                y += 28;
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
            if (!any) GUI.Label(new Rect(x, y, w, 30), "머리 위에 !가 뜬 사람에게 다가가 Space로 말을 걸어 보자", UIStyle.Wrap);
        }

        void DrawLog(QuestManager qm)
        {
            float w = 380, h = 260;
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, (UIStyle.VirtualHeight - h) / 2, w, h);
            GUI.Box(r, "", UIStyle.Box);
            GUILayout.BeginArea(new Rect(r.x + 10, r.y + 8, w - 20, h - 16));
            GUILayout.Label("퀘스트 목록   (Q 닫기)", UIStyle.Rich);
            bool any = false;
            foreach (var p in qm.Active)
            {
                any = true;
                string chain = string.IsNullOrEmpty(p.quest.chain) ? "" : $"  <color=#9a8>[{p.quest.chain}]</color>";
                GUILayout.Label($"{p.quest.title}{chain}", UIStyle.Rich);
                if (!string.IsNullOrEmpty(p.quest.summary)) GUILayout.Label(p.quest.summary, UIStyle.Wrap);
                GUILayout.Label("지금 할 일: " + qm.ProgressText(p), UIStyle.Wrap);
                string reward = "";
                if (p.quest.rewardMoney > 0) reward += $"돈 {p.quest.rewardMoney}  ";
                if (p.quest.rewardCredit > 0) reward += $"신용 {p.quest.rewardCredit}";
                if (reward != "") GUILayout.Label("보상: " + reward);
                GUILayout.Space(6);
            }
            if (!any) GUILayout.Label("진행 중인 퀘스트가 없다.");
            GUILayout.EndArea();
        }
    }
}
