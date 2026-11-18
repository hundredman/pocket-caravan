namespace PocketCaravan
{
    /// <summary>11/19 착한 일(선행·친구·박물관·수집품·전설·부탁)의 평판 보상. ReputationAwards는 .NET 검사 도구가 따로 빌드하므로 지역 enum(Reactions)을 쓰는 이 함수는 여기 둔다.</summary>
    public static class GoodDeeds
    {
        /// <summary>
        /// 11/19 맡은 일이 아닌 착한 일(나그네·친구·박물관·수집품·전설·부탁 들어주기)은 신용 대신 그 문명 평판으로 (Docs/Standing.md 원칙).
        /// 지역을 모르면(Common) 아무것도 주지 않는다. 오른 점수를 돌려준다 (알림 글에 쓴다).
        /// </summary>
        public static int ForRegion(Reactions.Region region, ReputationSize size, string sourceId)
        {
            string id = region == Reactions.Region.Goryeo ? "goryeo" : region == Reactions.Region.Venice ? "venice"
                : region == Reactions.Region.Arabia ? "arabia" : region == Reactions.Region.North ? "north" : null;
            if (id == null || size == ReputationSize.None) return 0;
            ReputationAwards.Emit(ReputationScope.Civilization, id, size, 1, sourceId);
            return ReputationAwards.PointsFor(size);
        }

    }
}
