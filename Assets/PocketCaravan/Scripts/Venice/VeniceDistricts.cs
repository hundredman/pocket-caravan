using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 한 맵 안의 구역(수도·네 마을·석호 길) 이름. 플레이어가 구역을 넘으면 HUD 지역명(MapRoot.displayName)을
    /// 바꾸고 화면 위에 작은 이름을 잠깐 띄운다. 씬 전환·암전·카메라 이동은 하지 않는다.
    /// 시장·의뢰·저장 규칙은 바꾸지 않는다. VeniceRegionSetup이 맵 JSON의 districts로 채워 씬에 붙인다.
    /// </summary>
    public class VeniceDistricts : MonoBehaviour
    {
        [Serializable]
        public class District
        {
            public string name;
            [Tooltip("타일 칸 범위(Unity 셀 좌표, 양끝 포함)")] public RectInt cells;
        }

        public List<District> districts = new List<District>();
        public string defaultName = "석호 길";
        [Min(0.5f)] public float labelSeconds = 2.2f;

        string current;
        float shownAt = -10f;

        void Update()
        {
            var player = PlayerController.Instance;
            var map = MapRoot.Current;
            if (player == null || map == null) return;
            string next = NameAt(player.Motor.Cell);
            if (next == current) return;
            bool first = current == null;
            current = next;
            map.displayName = next;
            if (!first) shownAt = Time.unscaledTime;
        }

        public string NameAt(Vector3Int cell)
        {
            foreach (var d in districts)
                if (cell.x >= d.cells.xMin && cell.x <= d.cells.xMax && cell.y >= d.cells.yMin && cell.y <= d.cells.yMax)
                    return d.name;
            return defaultName;
        }

        void OnGUI()
        {
            float t = Time.unscaledTime - shownAt;
            if (t > labelSeconds || string.IsNullOrEmpty(current) || HUD.Expanded) return;   // 11/08 Tab 펼친 동안은 숨긴다
            UIStyle.Begin();
            float a = Mathf.Clamp01(Mathf.Min(t / 0.25f, (labelSeconds - t) / 0.5f));
            var c = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            // 11/07 UI B안: 위 가운데 알약들 아래에 핀 + 구역 이름 종이 알약 (살짝 내려오며 나타난다)
            float w = Mathf.Round(CuteUI.Width(current, false, true) + 30f);
            float y = Mathf.Round(CuteUI.TopStack(20) - (1f - Mathf.Clamp01(t / 0.25f)) * 4f);   // 11/08 첫 안내·큰 장·축제 알약과 같이 쌓는다
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2f), y, w, 20f);
            CuteUI.Pill(r);
            CuteUI.Icon(CuteUI.Pic.Pin, r.x + 9f, r.y + 6f, 1, CuteUI.Frame);
            CuteUI.Label(r.x + 19f, r.center.y, current, CuteUI.Text, false, true);
            GUI.color = c;
        }
    }
}
