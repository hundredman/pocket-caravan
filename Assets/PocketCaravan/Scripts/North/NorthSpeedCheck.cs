using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 11/01 이동 속도 점검 (기우: Shift를 안 눌러도 달리는 것처럼 빨라질 때가 있음, 유저테스트에서도 나옴).
    /// F9로 켜고 끄는 작은 창: 지금 속도(칸/초)와 무엇이 곱해졌는지 보여 준다. 공용 코드는 읽기만 한다.
    /// 꺼져 있어도 "Shift를 안 눌렀는데 평소 걷기보다 1.25배 넘게 빠름"이 되면 Console에 원인을 한 줄 남긴다 (같은 원인은 맵마다 한 번).
    /// </summary>
    public class NorthSpeedCheck : MonoBehaviour
    {
        bool show;
        string text = "", lastLog = "";
        GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!InputReader.DevKeys) return;   // 11/08 배포판에서는 띄우지 않는다 (C 빠르게 키를 안내하던 개발용 창)
            if (FindFirstObjectByType<NorthSpeedCheck>() != null) return;
            var go = new GameObject("NorthSpeedCheck");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthSpeedCheck>();
        }

        static bool Key(out bool shift, out bool g)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            shift = k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
            g = k != null && k.cKey.isPressed;
            return k != null && k.f9Key.wasPressedThisFrame;
#else
            shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            g = Input.GetKey(KeyCode.C);
            return Input.GetKeyDown(KeyCode.F9);
#endif
        }

        void Update()
        {
            bool toggle = Key(out bool shift, out bool g);
            if (toggle) show = !show;
            var pc = PlayerController.Instance;
            var gm = GameManager.I;
            if (pc == null || gm == null || pc.Motor == null) { text = ""; return; }
            var p = gm.Player;
            int grade = RoadGrid.Current != null ? RoadGrid.Current.GradeAt(pc.Motor.Cell) : 0;
            float veh = p.vehicle != null ? p.vehicle.SpeedOn(grade) : 1f;
            float org = p.origin != null ? p.origin.speedMultiplier * (grade == 0 ? p.origin.offroadSpeedMultiplier : 1f) : 1f;
            float load = Mathf.Lerp(1f, pc.fullLoadSpeed, p.LoadRatio);
            float baseWalk = 3.8f;
            float world = SpeedMods.Product;          // 날씨·썰매 지역 배율 (NorthTravel), 베네치아 물웅덩이 등 (11/08 공용 SpeedMods)
            float total = pc.CurrentSpeed();
            float plain = baseWalk * (p.vehicle != null ? p.vehicle.SpeedOn(3) : 1f) * (p.origin != null ? p.origin.speedMultiplier : 1f);   // 큰길에서 그냥 걸을 때
            text = $"<b>속도 점검 (F9)</b>  {total:0.00}칸/초  (큰길 걷기 {plain:0.00})\n"
                 + $"Shift {(shift ? "누름" : "-")} · 달리기 {(pc.Running ? "<color=#ffd84a>예 ×" + pc.runMultiplier + "</color>" : "아니오")} · C {(g ? "<color=#ff8a7a>누름 ×6 (시험용 빠르게)</color>" : "-")}\n"
                 + $"땅 등급 {grade}{(grade == 0 ? " (길 없음)" : "")} · 출신 ×{org:0.00} · 수단 {(p.vehicle != null ? p.vehicle.displayName : "등짐")} ×{veh:0.00}\n"
                 + $"날씨·지역 ×{world:0.00} · 짐 무게 ×{load:0.00} · 날씨 {NorthAtmosphere.Shown}";
            // 원인 기록: Shift 없이 큰길 걷기보다 1.25배 넘게 빠를 때
            if (!pc.Running && pc.Motor.IsMoving && total > plain * 1.25f)
            {
                string why = g ? "C(시험용 6배 빠르게)를 누르고 있음" : grade == 0 ? $"길 없는 곳 보너스 (출신 ×{org:0.00}, 수단 ×{veh:0.00})" : $"기타 (등급 {grade}, 수단 ×{veh:0.00}, 지역 ×{world:0.00})";
                string key = (MapRoot.Current != null ? MapRoot.Current.mapId : "") + why;
                if (key != lastLog) { lastLog = key; Debug.Log($"[속도 점검] Shift 없이 빠름 {total:0.00}칸/초 (큰길 걷기 {plain:0.00}) — {why} · 맵 {(MapRoot.Current != null ? MapRoot.Current.mapId : "?")}"); }
            }
        }

        void OnGUI()
        {
            if (!show || string.IsNullOrEmpty(text)) return;
            UIStyle.Begin();
            if (style == null || style.font != UIStyle.RegularFont) style = new GUIStyle(UIStyle.Rich) { wordWrap = true, fontSize = UIStyle.Rich.fontSize };
            var r = new Rect(8, UIStyle.VirtualHeight - 92, 330, 72);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 6, r.y + 4, r.width - 12, r.height - 8), text, style);
        }
    }
}
