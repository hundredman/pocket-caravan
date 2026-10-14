using UnityEngine;

namespace PocketCaravan
{
    /// <summary>임시 상태 표시. GameManager가 자동으로 붙인다. 정식 HUD가 생기면 지운다.</summary>
    public class DebugHUD : MonoBehaviour
    {
        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            UIStyle.Begin();
            var p = gm.Player;
            var map = MapRoot.Current;

            string roadName = "";
            var player = PlayerController.Instance;
            if (RoadGrid.Current != null && player != null)
            {
                int g = RoadGrid.Current.GradeAt(player.Mover.Cell);
                var names = gm.Db.roads.gradeNames;
                roadName = g < names.Length ? names[g] : g.ToString();
            }

            string vehicle = p.vehicle != null ? p.vehicle.displayName : "걷기";
            string text =
                $"{(map != null ? map.displayName : "")}   {gm.Clock.Label}{(gm.Clock.IsMarketDay ? " 장날" : "")}\n" +
                $"돈 {p.money}   신용 {p.credit}\n" +
                $"{vehicle}  짐 {p.cargo.Weight:0.#}/{p.Capacity:0.#}   발밑: {roadName}";
            GUI.Box(new Rect(6, 6, 230, 52), "");
            GUI.Label(new Rect(12, 9, 224, 50), text);

            GUI.Label(new Rect(6, UIStyle.VirtualHeight - 20, 400, 18), "방향키/WASD 이동 · Shift 달리기 · E/Space 말 걸기 · J 퀘스트");
        }
    }
}
