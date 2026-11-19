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
    /// - 갚기: 장터 [−][50냥 단위 갚기][+] · [다 갚기]. 갚고 나서 밑천 50냥(Reserve)은 꼭 남는다 (파산 방지)
    /// - 기한을 넘기면 좌판 판 돈·가진 돈의 절반까지만 거두고(밑천은 남김), 모자라면 "연체": 날마다 신용 −(남은 외상 100냥당 1점, 1~5점), 남은 외상에 1할(2400냥까지), 하루 더 (날마다)
    /// - 연체 중에는 이익(판 값 − 산 값, 의뢰 보수)의 절반이 외상으로 가고 (손해 본 거래는 안 가져감),
    ///   귀한 짐·큰 주문을 맡을 수 없고, 좌판도 저절로 팔지 않는다 (단계는 그대로여도 "믿음을 잃었다"가 바로 느껴지게)
    /// - 제때 갚아도 점수는 주지 않는다 (달고 바로 갚아 점수 벌기 막기). 신용은 "믿고 맡김"에서만 오른다
    /// 저장: SaveData.debt · debtDue · debtLate (옛 저장 0 = 없음).
    /// </summary>
    public static class Loan
    {
        public const float DueHours = 72f, ExtendHours = 24f;
        /// <summary>11/19 은샘 "못 갚는 동안 빌린 금액에 따라": 연체 하루마다 남은 외상 100냥당 1점 (1~5점)</summary>
        public static int LatePoints(int debt) => Mathf.Clamp(Mathf.CeilToInt(debt / 100f), 1, 5);
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

        /// <summary>11/19 은샘 "파산 방지": 장터에서 갚을 때 손에 꼭 남기는 장사 밑천</summary>
        public const int Reserve = 50, PayStep = 50;
        /// <summary>밑천을 남기고 갚을 수 있는 가장 큰 돈</summary>
        public static int MaxPay(PlayerState p) => p == null || p.debt <= 0 ? 0 : Mathf.Max(0, Mathf.Min(p.debt, p.money - Reserve));
        /// <summary>[다 갚기]를 쓸 수 있는지 (다 갚아도 밑천이 남는지)</summary>
        public static bool CanPayAll(PlayerState p) => p != null && p.debt > 0 && p.money - p.debt >= Reserve;
        /// <summary>[−][n냥 갚기][+]에서 고를 수 있는 가장 큰 50냥 단위 (다 갚기보다 적게). 0이면 못 고름</summary>
        public static int MaxStep(PlayerState p) => p == null ? 0 : Mathf.Min(MaxPay(p), p.debt - 1) / PayStep * PayStep;

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

        /// <summary>11/19 은샘 "이득 볼 때만, 이익만으로": 연체 중 이익(판 값 − 산 값, 의뢰 보수)의 절반을 외상으로. 손해 본 거래는 안 가져간다</summary>
        public static void FromProfit(PlayerState p, int profit, string what)
        {
            if (!Late(p) || profit <= 0) return;
            int take = Mathf.Min(p.debt, Mathf.Min(p.money, (profit + 1) / 2));
            if (take <= 0) return;
            Repay(p, take);
            Notice = p.debt <= 0 ? $"이익 {take}냥으로 밀린 외상을 다 갚았어요!" : $"연체: 이익의 절반 {take}냥이 외상으로 (남은 {p.debt}냥)";
            NoticeId++;
            Notify.Show(Notice, 4f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            GameEvents.ItemSold -= OnSold; GameEvents.ItemSold += OnSold;   // 장터·손님 맞이에서 판 것
        }
        static void OnSold(ItemData item, TownData town, int price, int profit) { var gm = GameManager.I; if (gm != null) FromProfit(gm.Player, profit, "판"); }

        /// <summary>게임 한 시간마다 (GameManager). 기한이 지났으면 거두고, 모자라면 연체 벌칙</summary>
        public static void OnHour(PlayerState p, GameClock clock)
        {
            if (p == null || clock == null || p.debt <= 0 || clock.PlayHours < p.debtDue) return;
            // 11/19 은샘 "파산 방지": 좌판 판 돈·가진 돈에서 절반까지만 거둔다 (가진 돈은 밑천 Reserve를 남긴다)
            int fromStall = Mathf.Min(p.debt, (p.stallEarned + 1) / 2);
            p.stallEarned -= fromStall; p.debt -= fromStall;
            int cash = p.debt > 0 ? Mathf.Min(p.debt, Mathf.Max(0, Mathf.Min(p.money / 2, p.money - Reserve))) : 0;
            int paid = fromStall + (cash > 0 ? Repay(p, cash) : 0);
            if (p.debt <= 0)
            {
                Clear(p);
                Notify.Show($"외상 기한: {paid}냥을 거둬 다 갚았어요", 4f);
                return;
            }
            bool first = !p.debtLate;
            p.debtLate = true;
            int lose = LatePoints(p.debt);
            p.credit -= lose;
            p.debt = (int)Mathf.Min(MaxDebt, Mathf.Ceil(p.debt * 1.1f));
            p.debtDue = clock.PlayHours + ExtendHours;
            Sfx.Play("fail");
            Notify.Show(first
                ? $"외상 연체! 신용 −{lose}점 · 남은 외상 {p.debt}냥\n자세한 건 장터 [외상] 탭에서"
                : $"연체 하루 더: 신용 −{lose}점 · 남은 외상 {p.debt}냥", 7f);
        }
    }
}
