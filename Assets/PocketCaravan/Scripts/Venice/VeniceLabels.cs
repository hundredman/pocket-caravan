using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 장소 이름표 (2026-10-29): 벤치·나루·공방 같은 장소 이름은 가까이(4칸 안) 갔을 때만 띄운다.
    /// 광장에 이름표가 한꺼번에 떠서 어지럽던 것을 줄인다. 사람 이름표(공용 WorldLabels)는 그대로다.
    /// </summary>
    public static class VeniceLabels
    {
        const float Near = 4f;

        /// <summary>점검 로봇처럼 이름으로 장소를 찾아야 할 때 잠깐 켠다</summary>
        public static bool ShowAll;

        public static string Place(Component spot, string name)
        {
            var player = PlayerController.Instance;
            if (ShowAll || player == null || spot == null) return name;
            return ((Vector2)(player.transform.position - spot.transform.position)).sqrMagnitude <= Near * Near ? name : "";
        }
    }
}
