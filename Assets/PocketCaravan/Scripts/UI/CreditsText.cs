namespace PocketCaravan
{
    /// <summary>11/28 게임 메뉴 "만든 사람들"에 나오는 글. 출처와 라이선스가 확인된 것만 적는다 (스팀 제출용 목록과 맞춘다).
    /// 새 외부 자료를 넣으면 여기에 한 줄 더한다.</summary>
    public static class CreditsText
    {
        public struct Section { public string title, body; public Section(string t, string b) { title = t; body = b; } }

        public static readonly Section[] Sections =
        {
            new Section("바리바리 (Pocket Caravan)",
                "은샘 · 고려, 중앙 대륙 동쪽\n윤채 · 베네치아, 온라인 서버, 공용 화면\n은인 · 아라비아\n기우 · 북유럽, 시작 이야기와 배우기"),
            new Section("그림",
                "땅·건물·나무·물·소품 타일: Ninja Adventure Asset Pack (Pixel-boy, AAA) · CC0\n캐릭터와 물건 그림은 팀이 직접 찍었어요.\n일부 배경 그림은 생성형 AI로 초안을 만든 뒤 팀이 도트로 다듬었어요."),
            new Section("소리",
                "기본 음악과 효과음: Ninja Adventure Asset Pack (Pixel-boy, AAA) · CC0\n화면 소리: Kenney Interface Sounds · CC0\n발소리와 물소리: Kenney RPG Audio, Impact Sounds · CC0, rubberduck 40 CC0 water/splash/slime SFX · CC0\n지역 음악 일부는 팀이 직접 만들었어요."),
            new Section("글꼴",
                "Galmuri (Lee Minseo) · SIL Open Font License 1.1"),
            new Section("만든 도구",
                "Unity 6"),
        };
    }
}
