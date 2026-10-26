using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/27 북유럽 이야기 길잡이: 진행 중인 북유럽 퀘스트가 없을 때, 다음에 받을 수 있는 북유럽 퀘스트를 누가 주는지
    /// 화면 위 가운데에 한 줄로 알려 준다 ("다음 이야기: 집 보는 남편 — 비르카 서쪽 집 앞").
    /// 공용 퀘스트 목록(Q)은 진행 중인 것만 보여 줘서 처음 시작할 때 누구에게 가야 할지 몰랐다.
    /// 공용 QuestManager는 읽기만 한다 (CanStart). 저장하는 것 없음.
    /// </summary>
    public class NorthGuide : MonoBehaviour
    {
        // 퀘스트를 주는 사람 id → (이름, 어디 있는지)
        static readonly (string id, string name, string where)[] Givers =
        {
            ("north_husband", "집 보는 남편", "비르카 서쪽 첫 집 앞 (광장에서 왼쪽 위로)"),
            ("north_wife", "들일 하는 아내", "모피 사냥꾼 마을 남쪽 건초 들판"),
            ("north_innkeeper", "여관 할머니", "비르카 서쪽 여관 앞 (광장 왼쪽 위)"),
            ("north_bold", "욕심쟁이 볼드", "비르카 광장 왼쪽"),
            ("north_har", "목수 하르", "비르카 남서쪽, 장터 거리 왼쪽 끝 집 앞"),
            ("north_warehouse", "창고지기", "피오르 나루 서쪽 창고 앞"),
            ("north_goats", "염소지기 맏형", "모피 사냥꾼 마을 동쪽 염소 목장 안"),
        };

        GUIStyle style;
        string text;
        float nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthGuide>() != null) return;
            var go = new GameObject("NorthGuide");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthGuide>();
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;
            text = null;
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null || !NorthUtil.InNorth) return;
            foreach (var p in gm.Quests.Active)
                if (p != null && p.quest != null && p.quest.id != null && p.quest.id.StartsWith("north_")) return;   // 진행 중이면 공용 안내가 맡음
            foreach (var q in gm.Db.quests)
            {
                if (q == null || q.id == null || !q.id.StartsWith("north_") || string.IsNullOrEmpty(q.giverNpc)) continue;
                if (!gm.Quests.CanStart(q)) continue;
                string name = q.giverNpc, where = "";
                foreach (var g in Givers) if (g.id == q.giverNpc) { name = g.name; where = g.where; }
                text = $"<color=#ffde59>다음 이야기</color>  <b>{name}</b>" + (where != "" ? $"  <color=#c9c3d6>· {where}</color>" : "") + "  <color=#ffde59>!</color>";
                return;
            }
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(text) || UIState.Blocking) return;
            UIStyle.Begin();
            if (style == null) style = new GUIStyle(UIStyle.CenterRich) { wordWrap = false, clipping = TextClipping.Overflow };
            float w = Mathf.Min(UIStyle.VirtualWidth - 20f, style.CalcSize(new GUIContent(text)).x + 20f);
            var r = new Rect((UIStyle.VirtualWidth - w) / 2f, UIStyle.VirtualHeight - 38f, w, 18f);   // 아래쪽 조작 안내 바로 위
            GUI.Box(r, GUIContent.none, UIStyle.Box);
            GUI.Label(r, text, style);
        }
    }
}
