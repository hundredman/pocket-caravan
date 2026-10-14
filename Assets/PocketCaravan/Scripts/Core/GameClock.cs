using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>게임 안 시간. 장날, 배달 기한, 재고 회복, 길 하락이 모두 이 시계를 따른다.</summary>
    [Serializable]
    public class GameClock
    {
        public float realSecondsPerDay = 360f;  // 현실 6분 = 게임 하루
        public int marketDayInterval = 5;       // 5일마다 장날
        public int day = 1;
        public float minuteOfDay = 6 * 60;      // 아침 6시에 시작

        public event Action<int> NewHour;
        public event Action<int> NewDay;

        public int Hour => (int)(minuteOfDay / 60f);
        public bool IsMarketDay => day % marketDayInterval == 0;
        public float TotalHours => (day - 1) * 24f + minuteOfDay / 60f;
        public string Label => $"{day}일째 {Hour:00}:{(int)(minuteOfDay % 60):00}";

        /// <summary>시간을 한 번에 건너뛴다 (먼 길을 지날 때). 지나간 시각마다 NewHour가 불린다.</summary>
        public void Advance(float hours)
        {
            int steps = Mathf.CeilToInt(hours);
            for (int i = 0; i < steps; i++) Tick(Mathf.Min(1f, hours - i) * Mathf.Max(1f, realSecondsPerDay) / 24f);
        }

        /// <summary>0~1. 밤 0, 낮 1 (새벽·저녁에 서서히)</summary>
        public float Daylight
        {
            get
            {
                float h = minuteOfDay / 60f;
                if (h < 5f || h >= 20f) return 0f;
                if (h < 7f) return (h - 5f) / 2f;
                if (h >= 18f) return 1f - (h - 18f) / 2f;
                return 1f;
            }
        }

        public void Tick(float deltaSeconds)
        {
            int prevHour = Hour;
            minuteOfDay += deltaSeconds * 1440f / Mathf.Max(1f, realSecondsPerDay);
            while (minuteOfDay >= 1440f)
            {
                minuteOfDay -= 1440f;
                day++;
                NewDay?.Invoke(day);
            }
            if (Hour != prevHour) NewHour?.Invoke(Hour);
        }
    }
}
