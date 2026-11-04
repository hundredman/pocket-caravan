namespace PocketCaravan
{
    /// <summary>
    /// 2026-11-04 통합: 리액션 바퀴·동작은 공용 Reactions로 옮겼다. 베네치아 코드(친구·떠돌이 상인·곤돌라 경주·가면 장수)가
    /// 부르던 이름은 그대로 두고 공용으로 넘긴다. 옛 동작 이름(hop·spin·stretch)은 공용에서 기쁨·춤·낮잠으로 바뀐다.
    /// </summary>
    public static class VeniceReactions
    {
        /// <summary>점검 도구가 순서대로 도는 옛 10개 (Knows(i)용)</summary>
        static readonly string[] Old = { "bow", "hop", "clap", "spin", "stretch", "dance", "love", "nap", "row", "mask" };

        public static string NameOf(string id) => Reactions.NameOf(id);
        public static bool Knows(int i) => i >= 0 && i < Old.Length && Reactions.Knows(Old[i]);
        public static string Teach(string id) => Reactions.Teach(id);
        public static string TeachRandom() => Reactions.TeachRandom();
        public static void TestPlay(string id) => Reactions.TestPlay(id);
        public static void NpcCheer(string bit) => Reactions.NpcCheer(bit);
    }
}
