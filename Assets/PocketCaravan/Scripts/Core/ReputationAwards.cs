using System;

namespace PocketCaravan
{
    public enum ReputationSize { None = 0, VerySmall = 1, Small = 2, Medium = 3, Large = 4 }

    /// <summary>Approved contribution points; captured quest rewards are not replayed from saves.</summary>
    public sealed class ReputationContribution
    {
        public readonly ReputationScope scope;
        public readonly string targetId, sourceId;
        public readonly ReputationSize size;
        public readonly int direction;
        public int Points => direction * ReputationAwards.PointsFor(size);
        public ReputationContribution(ReputationScope scope, string targetId, ReputationSize size, int direction, string sourceId)
        { this.scope = scope; this.targetId = targetId; this.size = size; this.direction = direction; this.sourceId = sourceId; }
    }

    public static class ReputationAwards
    {
        // Approved D10 point table. Scope/target are explicit and never inferred from the active map.
        public static int PointsFor(ReputationSize size)
        {
            switch (size)
            {
                case ReputationSize.None: return 0;
                case ReputationSize.VerySmall: return 10;
                case ReputationSize.Small: return 25;
                case ReputationSize.Medium: return 75;
                case ReputationSize.Large: return 250;
                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
        }
        public static string SizeName(ReputationSize size) => size == ReputationSize.Small ? "작음"
            : size == ReputationSize.Medium ? "중간" : size == ReputationSize.Large ? "큼"
            : size == ReputationSize.VerySmall ? "아주 작음" : "없음";

        public static string TargetName(GameDatabase db, ReputationScope scope, string id)
        {
            foreach (var town in db.towns)
            {
                if (town == null) continue;
                if (scope == ReputationScope.Town && town.id == id) return town.displayName;
                if (scope == ReputationScope.Civilization && town.region != null && town.region.id == id) return town.region.displayName;
            }
            return "해당 지역";
        }

        public static void Emit(ReputationScope scope, string targetId, ReputationSize size, int direction, string sourceId)
        {
            if (size == ReputationSize.None) return;
            PointsFor(size);
            if (string.IsNullOrWhiteSpace(targetId) || (scope != ReputationScope.Civilization && scope != ReputationScope.Town)
                || (direction != -1 && direction != 1)) throw new ArgumentException("평판 기여 대상/방향이 올바르지 않습니다.");
            var contribution=new ReputationContribution(scope,targetId,size,direction,sourceId);
            GameManager.I?.Player?.reputation.Add(scope,targetId,contribution.Points);
            GameEvents.RaiseReputationContribution(contribution);
        }
        public static void ForTown(TownData town, ReputationSize size, int direction, string sourceId)
        {
            if (town == null || town.region == null) return; // Unregistered content grants nothing.
            bool local = town.region.townReputation;
            Emit(local ? ReputationScope.Town : ReputationScope.Civilization,
                local ? town.id : town.region.id, size, direction, sourceId);
        }
    }
}
