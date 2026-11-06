using System;

namespace PocketCaravan
{
    public enum HaggleStrength { Light = 0, Fair = 1, Hard = 2 }

    /// <summary>
    /// R3 흥정 규칙(Standing 6절). 깎는 폭 상한은 평판 단계(D10), 성공률은 단계·상인 성격·거래량으로 정한다.
    /// 결과는 날짜·마을·품목·방향·세기로 정해지므로 저장을 되돌려도 같은 시도는 같은 결과가 나온다.
    /// 수치는 D10 임시값이며 플레이로 조정한다.
    /// </summary>
    public static class HaggleRules
    {
        public const float BanHours = 3f;
        public const int BulkCount = 20;
        static readonly float[] BaseChance = { .85f, .60f, .35f };
        static readonly float[] StandingBonus = { 0, 0, 0, .05f, .10f, .15f };
        static readonly string[] StrengthNames = { "살짝", "적당히", "세게" };
        static readonly string[] TemperamentNames = { "깐깐함", "보통", "너그러움" };

        public static string Name(HaggleStrength k) => StrengthNames[(int)k];

        /// <summary>최대 폭에 대한 비율: 살짝 1/3, 적당히 2/3, 세게 전부.</summary>
        public static float Fraction(HaggleStrength k) => ((int)k + 1) / 3f;

        public static float Discount(float maxHaggle, HaggleStrength k) => maxHaggle <= 0 ? 0 : maxHaggle * Fraction(k);

        /// <summary>-1 깐깐함, 0 보통, +1 너그러움. 마을 ID로 고정한다(상인 데이터가 생기면 교체).</summary>
        public static int Temperament(string townId) => (int)(Hash(townId ?? "") % 3) - 1;
        public static string TemperamentName(string townId) => TemperamentNames[Temperament(townId) + 1];

        public static float Chance(float maxHaggle, ReputationStanding standing, HaggleStrength k, string townId, int count)
        {
            if (maxHaggle <= 0) return 0;
            float chance = BaseChance[(int)k] + StandingBonus[(int)standing] + Temperament(townId) * .10f + (count >= BulkCount ? .05f : 0);
            return Math.Max(.05f, Math.Min(.95f, chance));
        }

        public static bool Roll(float chance, int day, string townId, string itemId, bool selling, HaggleStrength k)
        {
            if (chance <= 0) return false;
            ulong h = Hash($"{day}|{townId}|{itemId}|{(selling ? 1 : 0)}|{(int)k}");
            double r = (h % 1000003UL) / 1000003.0;
            return r < chance;
        }

        // FNV-1a 64: Unity·.NET 어디서나 같은 값(string.GetHashCode는 실행마다 달라질 수 있다).
        static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return h;
        }
    }

    /// <summary>성공한 흥정 하나. 다음 한 번의 거래(같은 품목·방향)에만 쓰이고 사라진다.</summary>
    public sealed class HaggleDeal
    {
        public readonly string town, item;
        public readonly bool selling;
        public readonly int day, count;
        public readonly float discount;
        public readonly HaggleStrength strength;
        public HaggleDeal(string town, string item, bool selling, int day, float discount, HaggleStrength strength, int count)
        { this.town = town; this.item = item; this.selling = selling; this.day = day; this.discount = discount; this.strength = strength; this.count = count; }
        public bool AppliesTo(string townId, string itemId, bool sell, int today, int quantity) => town == townId && item == itemId && selling == sell && day == today && count == quantity && count > 0;
    }

    public enum HaggleOutcome { Unavailable, Success, Refused, Cancelled }

    public struct HaggleResult
    {
        public HaggleOutcome outcome;
        public HaggleDeal deal;
        public float chance;
        public string message;
    }

    /// <summary>시도 처리. 평판은 바꾸지 않는다(무리한 흥정도 평판 하락 없음).</summary>
    public static class Haggle
    {
        public static string Denial(TradeActivity activity, string townId, int day, float now, float maxHaggle)
        {
            if (maxHaggle <= 0) return "평판 경계 이하에서는 흥정할 수 없습니다.";
            float ban = activity.BanRemaining(townId, now);
            if (ban > 0) return $"상인이 거래를 거절합니다. {Math.Ceiling(ban):0}시간 뒤에 다시 오세요.";
            if (activity.HaggleLocked(townId, day)) return "오늘은 이 상인과 더 흥정할 수 없습니다.";
            return null;
        }

        public static HaggleResult Try(TradeActivity activity, string townId, string itemId, bool selling, int count,
            ReputationStanding standing, float maxHaggle, HaggleStrength k, int day, float now)
        {
            string denial = Denial(activity, townId, day, now, maxHaggle);
            if (count < 1) denial = "거래 가능한 수량이 없습니다.";
            if (denial != null) return new HaggleResult { outcome = HaggleOutcome.Unavailable, message = denial };
            float chance = HaggleRules.Chance(maxHaggle, standing, k, townId, count);
            if (HaggleRules.Roll(chance, day, townId, itemId, selling, k))
            {
                float discount = HaggleRules.Discount(maxHaggle, k);
                return new HaggleResult { outcome = HaggleOutcome.Success, chance = chance,
                    deal = new HaggleDeal(townId, itemId, selling, day, discount, k, count),
                    message = $"흥정 성공 · 이번 거래만 {(selling ? "더 받기" : "깎기")} {discount * 100:0.#}%" };
            }
            activity.LockHaggle(townId, day);
            if (k == HaggleStrength.Hard)
            {
                activity.Ban(townId, now + HaggleRules.BanHours);
                return new HaggleResult { outcome = HaggleOutcome.Cancelled, chance = chance,
                    message = $"너무 깎으려 해서 거래가 취소됐습니다. {HaggleRules.BanHours:0}시간 동안 이 상인과 거래할 수 없습니다. (평판 변화 없음)" };
            }
            return new HaggleResult { outcome = HaggleOutcome.Refused, chance = chance,
                message = "흥정 실패 · 오늘은 이 상인과 더 흥정할 수 없습니다. 원래 값으로는 거래할 수 있습니다." };
        }
    }
}
