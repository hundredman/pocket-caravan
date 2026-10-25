using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 왼쪽 위 상태 창: 맵, 돈·신용, 짐 무게, 체력, 노면/감속 안내. 아래쪽에 조작 안내.
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        static readonly Color32 LoadColor = new Color32(0xE9, 0xC4, 0x6A, 0xFF);
        static readonly Color32 StaminaColor = new Color32(0x7C, 0xD3, 0x7A, 0xFF);
        static readonly Color32 ShakeLow = new Color32(0x9E, 0xC9, 0xE8, 0xFF);
        static readonly Color32 ShakeHigh = new Color32(0xE8, 0x6A, 0x5A, 0xFF);

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null) return;
            UIStyle.Begin();
            var p = gm.Player;
            var map = MapRoot.Current;

            string roadName = "";
            var player = PlayerController.Instance;
            if (RoadGrid.Current != null && player != null)
            {
                int g = RoadGrid.Current.GradeAt(player.Motor.Cell);
                var names = gm.Db.roads.gradeNames;
                roadName = g < names.Length ? names[g] : g.ToString();
            }

            var r = new Rect(6, 6, 182, 83);
            GUI.Box(r, "", UIStyle.Box);
            float x = r.x + 8, y = r.y + 5;
            string market = gm.Clock.IsMarketDay ? " <color=#ffd84a>장날</color>" : "";
            GUI.Label(new Rect(x, y - 1, 160, 16), $"{map.displayName}{market}", UIStyle.Rich); y += 13;
            GUI.Label(new Rect(x, y - 1, 160, 16), $"<color=#ffe08a>돈 {p.money}</color>   신용 {p.credit}", UIStyle.Rich); y += 15;

            string vehicle = p.vehicle != null ? p.vehicle.displayName : "등짐";
            Gauge(x, ref y, $"{vehicle} {p.TotalWeight:0.#}/{p.Capacity:0}", p.LoadRatio, LoadColor);
            Gauge(x, ref y, "체력", p.stamina, StaminaColor);
            GUI.Label(new Rect(x, y - 1, 170, 12), $"발밑: {roadName}", UIStyle.Small);
            y += 12;
            if (p.HasAnyCargo)
            {
                string advice = "거친 길·무거운 짐은 느립니다";
                GUI.Label(new Rect(x, y - 1, 170, 12), advice, UIStyle.Small);
            }

            if (!string.IsNullOrEmpty(gm.LoadIssue))
            {
                var status = new Rect(6, UIStyle.VirtualHeight-43, Mathf.Min(350,UIStyle.VirtualWidth-12),22);
                GUI.Box(status,"",UIStyle.Box);
                GUI.Label(new Rect(status.x+5,status.y+3,status.width-10,18),"저장 복원 중단 · 이번 세션 저장 안 됨 · Esc에서 원인 확인",UIStyle.Small);
            }
            GUI.Label(new Rect(6, UIStyle.VirtualHeight - 16, 520, 14),
                "WASD 이동 · Shift 달리기 · Space 말 걸기 · Q 퀘스트 · M 지도 · L 장부 · Esc 메뉴", UIStyle.Small);
        }

        static void Gauge(float x, ref float y, string label, float v, Color32 c)
        {
            GUI.Label(new Rect(x, y - 2, 70, 12), label, UIStyle.Small);
            UIStyle.Bar(new Rect(x + 72, y + 2, 82, 6), v, c);
            y += 11;
        }
    }
}
