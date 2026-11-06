using System;

namespace PocketCaravan
{
    /// <summary>
    /// R4 마이너스 평판 회복(Standing 8절). 세 길 모두 그 평판 단위가 0점(중립 시작)을 넘지 않게 올린다.
    /// ① 마을 일: 돈·짐·신용 조건 없음. 장터에서 맡고 같은 마을 게시판에 보고한다.
    /// ② 기부: 마이너스일 때만, 필요한 만큼만 받는다.
    /// ③ 속죄 의뢰: ContractManager가 보증금 의뢰로 연다.
    /// 점수는 D10 크기 표를 쓰며, 기부 환산·보증금·기한·재개방은 임시값이다.
    /// </summary>
    public static class StandingRecovery
    {
        public const int NeutralScore = 0;
        public const ReputationSize ChoreSize = ReputationSize.Medium;      // 75
        public const ReputationSize AtonementSize = ReputationSize.Large;   // 250, 마을 일 3번 남짓
        public const ReputationSize AtonementPartialSize = ReputationSize.Medium;
        public const int CoinsPerPoint = 2;            // 기부 2냥 = 1점(100냥 = 마을 일 2/3번)
        public const float ChoreHours = 1f;             // 일하는 데 드는 게임 시간
        public const float AtonementReopenHours = 24f;  // 끝난 뒤 다시 열리는 시간
        public const float AtonementHoursPerStep = 6f;  // 일반 의뢰(9)보다 빠듯함
        public const int AtonementDepositBase = 40, AtonementDepositPerStep = 15;

        public static readonly string[] ChoreTitles = { "우물 물 길어 나르기", "장터 마당 쓸기", "마을 안 짐 옮기기" };
        public const string ChoreReport = "일을 마치고 이 마을 의뢰 게시판에 알려 주세요.";

        public static int Score(ReputationState rep, ReputationScope scope, string id) => ReputationState.Score(rep.Get(scope, id));
        public static bool Negative(ReputationState rep, ReputationScope scope, string id) => Score(rep, scope, id) < NeutralScore;
        public static int Missing(ReputationState rep, ReputationScope scope, string id) => Math.Max(0, NeutralScore - Score(rep, scope, id));

        /// <summary>중립을 넘지 않게 올리고 실제로 오른 점수를 돌려준다.</summary>
        public static int Raise(ReputationState rep, ReputationScope scope, string id, int points)
        {
            if (points <= 0) return 0;
            int gain = Math.Min(points, Missing(rep, scope, id));
            if (gain > 0) rep.Add(scope, id, gain);
            return gain;
        }

        public static int ChoreKind(string townId, int day) => (int)(((uint)(townId ?? "").Length * 31u + (uint)Math.Max(0, day)) % (uint)ChoreTitles.Length);

        public static string ChoreDenial(TradeActivity activity, ReputationState rep, ReputationScope scope, string id)
        {
            if (!Negative(rep, scope, id)) return "평판이 중립 이상이라 회복할 필요가 없습니다.";
            if (activity.ActiveChore != null) return "이미 맡은 마을 일이 있습니다.";
            return null;
        }

        public static int DonationCost(ReputationState rep, ReputationScope scope, string id, int requested)
        {
            int needed = Missing(rep, scope, id) * CoinsPerPoint;
            return Math.Max(0, Math.Min(requested, needed));
        }

        /// <summary>기부. 돈을 먼저 확인하고, 필요한 만큼만 받는다. (받은 돈, 오른 점수)</summary>
        public static (int paid, int gained) Donate(ReputationState rep, ReputationScope scope, string id, int requested, int wallet)
        {
            int cost = Math.Min(DonationCost(rep, scope, id, requested), Math.Max(0, wallet));
            cost -= cost % CoinsPerPoint;
            if (cost <= 0) return (0, 0);
            int gained = Raise(rep, scope, id, cost / CoinsPerPoint);
            return (gained * CoinsPerPoint, gained);
        }

        public static int AtonementDeposit(int routeSteps) => AtonementDepositBase + AtonementDepositPerStep * Math.Max(1, routeSteps);
        public static float AtonementHours(int routeSteps) => AtonementHoursPerStep * Math.Max(1, routeSteps) + 4f;
    }
}
