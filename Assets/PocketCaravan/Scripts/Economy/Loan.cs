using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 은샘 "신용으로 추가로 물건을 살 수 있는 시스템": 신용 4단계 혜택 외상.
    /// 은샘 "자연스럽게", "부담·기한을 원치 않는 유저도 있으니 거래창에 옵션": 거래창 [외상 쓰기]를 켜 두면, 살 때 가진 돈보다 많이 사면
    /// 모자란 만큼을 외상으로 단다 (한도까지, 가진 돈을 먼저). 끄면 가진 돈 안에서만 (처음엔 꺼져 있다, 고른 것은 저장).
    /// 수량 [최대]는 가진 돈까지만 채우고, [+]로 더 늘리면 외상으로 넘어간다 (모르고 크게 빚지지 않게).
    /// 현금을 빌려주지는 않는다 (떼먹고 돈만 챙기는 길을 막으려고).
    /// - 한도: 지금 점수의 단계로 4단계 300 · 5단계 500 · 6단계 이상 800냥. 이자 없음
    /// - 처음 외상을 단 때부터 사흘(게임 72시간 = 현실 18분) 안에 아무 장터에서 갚는다. 기한이 하루도 안 남으면 더 달 수 없다
    /// - 기한을 넘기면 좌판 판 돈·가진 돈에서 거두고, 모자라면 "연체": 신용 −2점, 남은 외상에 1할(2400냥까지), 하루 더 (날마다)
    /// - 연체 중에는 들어오는 돈(장터에서 판 값·의뢰 보수·좌판 판 돈)이 먼저 외상으로 가고,
    ///   귀한 짐·큰 주문을 맡을 수 없고, 좌판도 저절로 팔지 않는다 (단계는 그대로여도 "믿음을 잃었다"가 바로 느껴지게)
    /// - 제때 갚아도 점수는 주지 않는다 (달고 바로 갚아 점수 벌기 막기). 신용은 "믿고 맡김"에서만 오른다
    /// 저장: SaveData.debt · debtDue · debtLate (옛 저장 0 = 없음).
    /// </summary>
    public static class Loan
    {
        public const float DueHours = 72f, ExtendHours = 24f;
        public const int LatePoints = 2;
        public const int MaxDebt = 2400;   // 1할씩 붙어도 이 위로는 안 는다 (넘쳐 0이 되는 일 막기)

        /// <summary>잠깐 켜면 견적이 가진 돈만으로 계산된다 (장터 [최대] 계산용)</summary>
        public static bool CashOnly;

        public static int Limit(PlayerState p)
        {
            // 한도는 지금 점수로 따진다 (단계는 내려가지 않아도, 외상을 떼먹어 깎인 점수만큼 한도도 준다)
            int lv = p == null ? 0 : Credit.LevelOfPoints(p.credit);
            return lv >= 6 ? 800 : lv == 5 ? 500 : lv == 4 ? 300 : 0;
        }
        /// <summary>외상을 쓸 수 있는 신용인지 (단계 기준. 알약·설명을 보일지)</summary>
        public static bool Unlocked(PlayerState p) => p != null && (Credit.Level(p) >= 4 || p.debt > 0);

        /// <summary>더 달 수 있는 외상. 연체 중이거나 기한이 하루도 안 남았으면 0 (다 갚아야 다시)</summary>
        public static int Room(PlayerState p) => p == null || Closing(p) ? 0 : Mathf.Max(0, Limit(p) - p.debt);
        public static bool Closing(PlayerState p) { var gm = GameManager.I; return p != null && p.debt > 0 && (p.debtLate || (gm != null && p.debtDue - gm.Clock.PlayHours < ExtendHours + 0.01f)); }
        public static bool Late(PlayerState p) => p != null && p.debt > 0 && p.debtLate;

        /// <summary>거래창 [외상 쓰기]를 켰는지 (지역 메모 "pref:loan"에 저장, 처음엔 꺼짐)</summary>
        public const string PrefKey = "pref:loan";
        public static bool On(PlayerState p) { var gm = GameManager.I; return Unlocked(p) && gm != null && gm.Player == p && gm.GetMemo(PrefKey) > 0f; }
        public static void SetOn(bool on) { var gm = GameManager.I; if (gm != null) gm.SetMemo(PrefKey, on ? 1f : 0f); }

        /// <summary>살 때 쓸 수 있는 돈: 가진 돈 (+ [외상 쓰기]를 켰으면 남은 외상 한도)</summary>
        public static long BuyBudget(PlayerState p) => p == null ? 0 : (long)Mathf.Max(0, p.money) + (CashOnly || !On(p) ? 0 : Room(p));

        /// <summary>price냥을 치른다: 가진 돈을 먼저, 모자란 만큼만 외상으로. 치를 수 없으면 false</summary>
        public static bool Pay(PlayerState p, GameClock clock, int price)
        {
            if (p == null || price < 0 || BuyBudget(p) < price) return false;
            int cash = Mathf.Min(price, Mathf.Max(0, p.money)), onCredit = price - cash;
            if (onCredit > 0)
            {
                if (clock == null) return false;
                if (p.debt <= 0) p.debtDue = clock.PlayHours + DueHours;
                p.debt += onCredit;
            }
            p.money -= cash;
            return true;
        }

        /// <summary>total냥을 살 때 외상으로 달릴 몫</summary>
        public static int CreditPart(PlayerState p, long total) => p == null ? 0 : (int)Mathf.Max(0, total - Mathf.Max(0, p.money));

        /// <summary>남은 기한 (게임 시간). 외상이 없으면 0</summary>
        public static float HoursLeft(PlayerState p, GameClock clock) => p == null || p.debt <= 0 || clock == null ? 0f : p.debtDue - clock.PlayHours;

        public static string DueText(PlayerState p, GameClock clock)
        {
            float h = HoursLeft(p, clock);
            if (p == null || p.debt <= 0) return "";
            if (p.debtLate) return "연체";
            if (h <= 0f) return "기한 지남";
            return h >= 24f ? $"{Mathf.FloorToInt(h / 24f)}일 {Mathf.FloorToInt(h % 24f)}시간 남음" : $"{Mathf.CeilToInt(h)}시간 남음";
        }

        /// <summary>가진 돈으로 갚을 수 있는 만큼 갚기. 갚은 냥을 돌려준다</summary>
        public static int Repay(PlayerState p, int amount = int.MaxValue)
        {
            if (p == null || p.debt <= 0) return 0;
            int pay = Mathf.Min(p.debt, Mathf.Min(amount, p.money));
            if (pay <= 0) return 0;
            p.money -= pay; p.debt -= pay;
            if (p.debt <= 0) Clear(p);
            return pay;
        }

        static void Clear(PlayerState p) { p.debt = 0; p.debtDue = 0f; p.debtLate = false; }

        /// <summary>마지막으로 저절로 갚은 알림 (장터 창이 열려 있으면 창 알림 줄에도 띄운다)</summary>
        public static string Notice; public static int NoticeId;

        /// <summary>매 프레임 (GameManager): 연체 중이면 들어온 돈을 바로 외상으로 (장터 판 값·의뢰 보수·좌판 판 돈을 받으면)</summary>
        public static void Garnish(PlayerState p)
        {
            if (!Late(p) || p.money <= 0) return;
            int paid = Repay(p);
            if (paid <= 0) return;
            Notice = p.debt <= 0 ? $"번 돈 {paid}냥으로 밀린 외상을 다 갚았어요. 다시 믿고 맡겨 줘요!" : $"연체 중이라 번 돈 {paid}냥이 외상으로 갔어요 (남은 외상 {p.debt}냥)";
            NoticeId++;
            Notify.Show(Notice, 4f);
        }

        /// <summary>게임 한 시간마다 (GameManager). 기한이 지났으면 거두고, 모자라면 연체 벌칙</summary>
        public static void OnHour(PlayerState p, GameClock clock)
        {
            if (p == null || clock == null || p.debt <= 0 || clock.PlayHours < p.debtDue) return;
            // 좌판에 모인 판 돈도 먼저 거둔다
            int fromStall = Mathf.Min(p.stallEarned, p.debt);
            p.stallEarned -= fromStall; p.debt -= fromStall;
            int paid = fromStall + (p.debt > 0 ? Repay(p) : 0);
            if (p.debt <= 0)
            {
                Clear(p);
                Notify.Show($"외상 기한이 되어 {paid}냥을 거둬 갔다. 외상을 다 갚았어요.", 5f);
                return;
            }
            bool first = !p.debtLate;
            p.debtLate = true;
            p.credit -= LatePoints;
            p.debt = (int)Mathf.Min(MaxDebt, Mathf.Ceil(p.debt * 1.1f));
            p.debtDue = clock.PlayHours + ExtendHours;
            Sfx.Play("fail");
            Notify.Show(first
                ? $"외상을 못 갚아 연체가 됐어요! 신용 −{LatePoints}점 · 1할이 붙어 {p.debt}냥\n다 갚을 때까지 번 돈은 먼저 외상으로 가고, 귀한 짐·큰 주문을 맡을 수 없고, 좌판도 쉬어요"
                : $"연체 하루가 더 지났다. 신용 −{LatePoints}점 · 남은 외상 {p.debt}냥", 7f);
        }
    }
}
