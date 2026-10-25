using System;
using System.Collections.Generic;

namespace PocketCaravan
{
    /// <summary>
    /// M 지도를 지역별로 맡기는 자리. 어떤 지역 지도가 현재 맵을 맡겠다고 등록하면 기본 지도(WorldMapUI, 고려·중간권)는 그 맵에서 열리지 않는다.
    /// 등록이 없는 맵은 지금과 같다. 표시만 다루며 이동·시세·저장과 무관하다.
    /// </summary>
    public static class RegionMapOverride
    {
        static readonly List<Func<string, bool>> claims = new List<Func<string, bool>>();

        public static void Register(Func<string, bool> claim)
        {
            if (claim != null) claims.Add(claim);
        }

        public static bool Claims(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return false;
            foreach (var claim in claims)
                if (claim(mapId)) return true;
            return false;
        }
    }
}
