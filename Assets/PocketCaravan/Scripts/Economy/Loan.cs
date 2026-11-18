using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 은샘 "신용은 돈을 빌릴 수 있는 능력 같은 무형 자산": 신용 4단계 혜택 외상.
    /// 장터에서 상인에게 외상으로 돈을 받고(한도 안), 사흘(게임 72시간 = 현실 18분) 안에 아무 장터에서 갚는다.
    /// - 한도: 4단계 300 · 5단계 500 · 6단계 이상 800냥. 이자 없음 (처음 하는 사람에게 겁주지 않게)
    /// - 한 번에 하나의 외상: 이미 있으면 한도 안에서 더 받을 수 있고, 기한은 처음 받은 때 그대로
    /// - 기한을 넘기면: 가진 돈에서 저절로 거둔다. 모자라면 신용 −2점, 남은 외상에 1할을 더하고 하루 더 준다 (날마다 반복)
    /// - 제때 갚아도 점수는 주지 않는다 (받고 바로 갚아 점수를 버는 일이 없게). 신용은 "믿고 맡김"에서만 오른다
    /// 저장: SaveData.debt · debtDue (옛 저장 0 = 없음).
    /// </summary>
    public static class Loan
    {
        public const float DueHours = 72f, ExtendHours = 24f;
        public const int LatePoints = 2;
        public const int Step = 50;   // 장터 창에서 한 번에 받는 단위

        public static int Limit(PlayerState p)
        {
            int lv = Credit.Level(p);
            return lv >= 6 ? 800 : lv == 5 ? 500 : lv == 4 ? 300 : 0;
        }
        public static bool Unlocked(PlayerState p) => Limit(p) > 0;
        public static int Room(PlayerState p) => p == null ? 0 : Mathf.Max(0, Limit(p) - p.debt);

        /// <summary>남은 기한 (게임 시간). 외상이 없으면 0</summary>
        public static float HoursLeft(PlayerState p, GameClock clock) => p == null || p.debt <= 0 || clock == null ? 0f : p.debtDue - clock.PlayHours;

        public static string DueText(PlayerState p, GameClock clock)
        {
            float h = HoursLeft(p, clock);
            if (p == null || p.debt <= 0) return "";
            if (h <= 0f) return "기한 지남";
            return h >= 24f ? $"{Mathf.FloorToInt(h / 24f)}일 {Mathf.FloorToInt(h % 24f)}시간 남음" : $"{Mathf.CeilToInt(h)}시간 남음";
        }

        /// <summary>외상으로 amount냥 받기. 실제로 받은 냥을 돌려준다</summary>
        public static int Borrow(PlayerState p, GameClock clock, int amount)
        {
            if (p == null || clock == null) return 0;
            amount = Mathf.Min(amount, Room(p));
            if (amount <= 0 || (long)p.money + amount > int.MaxValue) return 0;
            if (p.debt <= 0) p.debtDue = clock.PlayHours + DueHours;
            p.debt += amount; p.money += amount;
            return amount;
        }

        /// <summary>가진 돈으로 갚을 수 있는 만큼 갚기. 갚은 냥을 돌려준다</summary>
        public static int Repay(PlayerState p, int amount = int.MaxValue)
        {
            if (p == null || p.debt <= 0) return 0;
            int pay = Mathf.Min(p.debt, Mathf.Min(amount, p.money));
            if (pay <= 0) return 0;
            p.money -= pay; p.debt -= pay;
            if (p.debt <= 0) { p.debt = 0; p.debtDue = 0f; }
            return pay;
        }

        /// <summary>게임 한 시간마다 (GameManager). 기한이 지났으면 거두고, 모자라면 벌칙</summary>
        public static void OnHour(PlayerState p, GameClock clock)
        {
            if (p == null || clock == null || p.debt <= 0 || clock.PlayHours < p.debtDue) return;
            int paid = Repay(p);
            if (p.debt <= 0)
            {
                Notify.Show($"외상 기한이 되어 가진 돈에서 {paid}냥을 거둬 갔다. 외상을 다 갚았어요.", 5f);
                return;
            }
            p.credit -= LatePoints;
            p.debt = Mathf.CeilToInt(p.debt * 1.1f);
            p.debtDue = clock.PlayHours + ExtendHours;
            Sfx.Play("fail");
            Notify.Show($"외상을 다 못 갚았다! 신용 −{LatePoints}점 · 남은 외상에 1할이 붙어 {p.debt}냥 · 하루 안에 갚아요", 6f);
        }
    }
}
