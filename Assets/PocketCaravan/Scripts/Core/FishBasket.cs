using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 생선 통 (11/25, 전 지역 낚시 공용). 낚은 물고기를 짐으로 가져가는 것은 하루에 Max마리까지.
    /// 그 뒤로 낚은 것은 놓아 주고 크기 기록·도감만 남는다 (낚시가 공짜 교역품을 끝없이 주던 것. 팀 규칙: 미니게임은 교역 수입을 크게 바꾸지 않는다).
    /// 날짜와 마릿수는 GameManager 메모에 있어 저장된다.
    /// </summary>
    public static class FishBasket
    {
        public const int Max = 3;
        const string DayKey = "fishbasket:day", CountKey = "fishbasket:n";

        public static int Left
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null) return 0;
                int n = Mathf.RoundToInt(gm.GetMemo(DayKey, -1f)) == gm.Clock.day ? Mathf.RoundToInt(gm.GetMemo(CountKey)) : 0;
                return Mathf.Max(0, Max - n);
            }
        }

        /// <summary>한 마리 담는다. 통이 찼으면 false</summary>
        public static bool Take()
        {
            var gm = GameManager.I;
            if (gm == null || Left <= 0) return false;
            int n = Max - Left;
            gm.SetMemo(DayKey, gm.Clock.day);
            gm.SetMemo(CountKey, n + 1);
            return true;
        }

        public const string FullLine = "오늘은 생선 통이 가득 찼다. 이번 것은 놓아 줬다.";
    }
}
