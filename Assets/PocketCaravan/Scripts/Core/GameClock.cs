using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 게임 안 시간. 장날, 재고 회복, 날씨, 길 하락이 이 시계(세상 시간)를 따른다.
    /// 2026-11-08 공용 세상 시계 (팀 승인 규칙):
    /// - 온라인(WorldClock.Active): 세상 시간은 모두 같고 늘 흐른다. 서버 기준 시각으로 계산하며, 아무도 건너뛸 수 없다.
    ///   길 이동·나룻배·여관 같은 Advance는 세상 시간을 넘기지 않고 내 의뢰 시간(PlayHours)만 쓴다.
    /// - 혼자 하기(로그인 안 함): 예전처럼 이 컴퓨터에서 흐르고, Advance가 세상 시간도 넘긴다.
    /// - 의뢰 기한·푹 쉼은 PlayHours(게임이 켜져 있던 시간 + 내가 쓴 시간)로 잰다. 게임을 끄면 멈춘다.
    /// </summary>
    [Serializable]
    public class GameClock
    {
        public float realSecondsPerDay = 360f;  // 현실 6분 = 게임 하루
        public int marketDayInterval = 5;       // 5일마다 장날
        public int day = 1;
        public float minuteOfDay = 8 * 60;      // 아침 8시에 시작
        /// <summary>내 의뢰 시간(시간 단위). 새 게임은 세상 시간과 같은 8에서 시작. 옛 저장은 불러올 때 TotalHours로 이어받는다.</summary>
        public float playHours = 8f;

        public event Action<int> NewHour;
        public event Action<int> NewDay;
        /// <summary>온라인 세상 시계에 맞추며 한 번에 앞으로 건너뛴 시간(접속·다시 접속). 놓친 시세 회복 등에 쓴다.</summary>
        public event Action<float> Jumped;

        public int Hour => (int)(minuteOfDay / 60f);
        public bool IsMarketDay => day % marketDayInterval == 0;
        /// <summary>세상 시간 (온라인이면 모두 같다)</summary>
        public float TotalHours => (day - 1) * 24f + minuteOfDay / 60f;
        /// <summary>내 의뢰 시간: 의뢰 기한·푹 쉼처럼 나에게만 흐르는 시간</summary>
        public float PlayHours => playHours;
        public bool Shared => WorldClock.Active;
        public string Label => $"{day}일째 {Hour:00}:{(int)(minuteOfDay % 60):00}";

        /// <summary>
        /// 시간을 한 번에 쓴다 (먼 길·나룻배·여관 등). 의뢰 시간은 늘 그만큼 흐른다.
        /// 혼자 할 때만 세상 시간도 넘어가며, 지나간 시각마다 NewHour가 불린다.
        /// </summary>
        public void Advance(float hours)
        {
            if (!(hours > 0f)) return;
            playHours += hours;
            if (WorldClock.Active) return;   // 온라인: 세상 시계는 아무도 건너뛸 수 없다
            int steps = Mathf.CeilToInt(hours);
            for (int i = 0; i < steps; i++) Step(Mathf.Min(1f, hours - i) * Mathf.Max(1f, realSecondsPerDay) / 24f);
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

        /// <summary>
        /// 매 프레임. deltaSeconds는 의뢰 시간이 흐를 양(창이 열려 멈춘 동안은 0).
        /// 온라인이면 세상 시간은 deltaSeconds와 상관없이 서버 기준 시각에 맞춘다.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds > 0f) playHours += deltaSeconds * 24f / Mathf.Max(1f, realSecondsPerDay);
            if (WorldClock.Active) SetWorld(WorldClock.NowHours);
            else Step(deltaSeconds);
        }

        /// <summary>혼자 할 때 세상 시간을 흘린다</summary>
        void Step(float deltaSeconds)
        {
            if (!(deltaSeconds > 0f)) return;
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

        /// <summary>
        /// 온라인 세상 시간으로 맞춘다. 앞으로 크게 건너뛰면(처음 접속 등) Jumped를 한 번 부르고, 날·시 사건도 한 번씩만 부른다.
        /// 서버 시각을 다시 받아 조금 뒤로 가는 경우는 사건 없이 맞추기만 한다.
        /// </summary>
        public void SetWorld(double totalHours)
        {
            if (double.IsNaN(totalHours) || double.IsInfinity(totalHours)) return;
            if (totalHours < 0) totalHours = 0;
            double prev = TotalHours;
            int prevDay = day, prevHour = Hour;
            int newDay = (int)Math.Floor(totalHours / 24.0) + 1;
            day = newDay;
            minuteOfDay = Mathf.Clamp((float)((totalHours - (newDay - 1) * 24.0) * 60.0), 0f, 1439.999f);
            if (totalHours <= prev) return;
            if (totalHours - prev > 1.5) Jumped?.Invoke((float)(totalHours - prev));
            if (day != prevDay) NewDay?.Invoke(day);
            if (Hour != prevHour || day != prevDay) NewHour?.Invoke(Hour);
        }
    }

    /// <summary>
    /// 공용 세상 시계 (2026-11-08). 온라인에 접속하면 서버가 알려 준 기준 시각으로 모두 같은 게임 시간을 계산한다.
    /// 게임 시간 = 1일째 아침 8시 + (지금 − 기준 시각) × 하루/현실 secondsPerDay초. 서버 Server/src/world.ts의 WORLD_EPOCH_MS와 같은 값.
    /// 서버가 clock을 보내지 않는 예전 서버여도 이 컴퓨터의 세계 표준시(UTC)로 같은 계산을 한다.
    /// </summary>
    public static class WorldClock
    {
        public static readonly long DefaultEpochMs = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        public const float DefaultSecondsPerDay = 360f;

        static long epochMs = DefaultEpochMs, offsetMs;
        static float secondsPerDay = DefaultSecondsPerDay;

        /// <summary>온라인 세상 시계를 쓰는 중 (접속 중·잠시 끊겨 다시 붙는 중)</summary>
        public static bool Active { get; private set; }
        /// <summary>서버 시각 − 이 컴퓨터 시각 (ms)</summary>
        public static long OffsetMs => offsetMs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Active = false; epochMs = DefaultEpochMs; offsetMs = 0; secondsPerDay = DefaultSecondsPerDay; }

        static long LocalNowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public static long NowMs => LocalNowMs + offsetMs;

        /// <summary>세상 시간(시간 단위). 1일째 0시 = 0</summary>
        public static double NowHours => HoursAt(NowMs);
        public static double HoursAt(long ms) => (ms - epochMs) / 1000.0 / secondsPerDay * 24.0 + 8.0;

        /// <summary>접속(welcome). serverNowMs가 0이면 이 컴퓨터 시각을 믿는다.</summary>
        public static void Join(long serverNowMs, long serverEpochMs, float serverSecondsPerDay)
        {
            offsetMs = serverNowMs > 0 ? serverNowMs - LocalNowMs : 0;
            epochMs = serverEpochMs > 0 ? serverEpochMs : DefaultEpochMs;
            secondsPerDay = serverSecondsPerDay >= 1f ? serverSecondsPerDay : DefaultSecondsPerDay;
            Active = true;
        }

        /// <summary>끊기·혼자 하기로 돌아감. 세상 시간은 지금 값에서 이 컴퓨터로 이어 흐른다.</summary>
        public static void Leave() { Active = false; }
    }
}
