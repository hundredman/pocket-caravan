namespace PocketCaravan
{
    /// <summary>
    /// 2026-11-04 통합: 친밀도·선물(G)·친구 수첩(F)·사진(P)은 공용 Social로 옮겼다 (베네치아 친구 8명도 거기).
    /// 베네치아 점검 도구가 부르던 이름만 남겨 넘긴다.
    /// </summary>
    public static class VeniceSocial
    {
        public static int Points(string name) => Social.Points(name);
        public static int Hearts(string name) => Social.Hearts(name);
        public static void TestGift(string name, ItemData item) => Social.TestGift(name, item);
    }
}
