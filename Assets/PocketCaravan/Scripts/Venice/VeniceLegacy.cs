using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아가 공용 기능(전시실 Museum · 숨은 사자 문장 Collectibles · 놀거리 알림 ActivityHints)에 자기 자료를 등록하는 곳.
    /// 2026-11-05 통합: VeniceMuseum·VeniceLion·VeniceActivityHints를 지우고 규칙·자료만 남김. 옛 저장 키(venice:museum:*, venice:lion:*)는 그대로 이어진다.
    /// </summary>
    public static class VeniceLegacy
    {
        public static readonly string[] LionMaps = { "venice_rialto", "venice_santerasmo", "venice_murano", "venice_burano", "venice_chioggia", "venice_c_vineyard", "venice_c_marsh" };
        public static bool LionBlessed => VeniceSave.Get("lion_blessing") > 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var V = Reactions.Region.Venice;

            // ---- 전시실 (리알토 상인회관) ----
            Museum.Shelf[V] = new[]
            {
                new Museum.Entry { key = "venice_salt", name = "석호 소금", item = "venice_salt" },
                new Museum.Entry { key = "venice_fruit", name = "과일 상자", item = "venice_fruit" },
                new Museum.Entry { key = "venice_fish", name = "절인 생선", item = "venice_fish" },
                new Museum.Entry { key = "venice_wine", name = "포도주", item = "venice_wine" },
                new Museum.Entry { key = "venice_glass_art", name = "유리 장식", item = "venice_glass_art" },
                new Museum.Entry { key = "venice_lace", name = "레이스 손수건", item = "venice_lace" },
            };
            Museum.KeyFor[V] = key => "venice:museum:" + key;

            // ---- 숨은 사자 문장 7개 ----
            Collectibles.Sets[V] = new Collectibles.Set
            {
                name = "이끼 낀 사자 문장", readName = "이끼 낀 사자 문장", maps = LionMaps, autoPlace = false,
                lore = new[]
                {
                    "옛날 석호의 배 한 척이 폭풍을 만났다.\n뱃사람들은 구름 사이로 날개 달린 사자를 보았다고 했다.",
                    "사자는 펼친 책을 앞발로 누르고 있었다.\n책에는 '평화'라는 한 낱말이 적혀 있었다.",
                    "상인들은 배와 창고에 사자를 새겼다.\n사자 아래에서는 속이지 않는다는 약속이었다.",
                    "유리장이들은 가마 불빛 속에서\n사자의 날개가 반짝이는 것을 보았다고 한다.",
                    "어부들은 그물을 던지기 전에\n문장의 사자를 한 번씩 쓰다듬었다.",
                    "본토로 건너간 상인들도 길목마다 작은 사자를 숨겼다.\n돌아오는 길을 잊지 않으려고.",
                    "일곱 사자를 다 찾은 상인은\n어느 항구에서든 믿을 사람으로 통했다고 한다.",
                },
                completeText = "일곱 사자를 모두 찾았다! 상금 300냥, 신용 +2, '사자의 축복'(큰 장 덤 2할 5푼)",
            };
            Collectibles.KeyFor[V] = i => i == 99 ? "venice:lion_blessing" : "venice:lion:" + i;
            Collectibles.OnComplete[V] = () => VeniceSave.Set("lion_blessing", 1f);

            // ---- 놀거리 알림 ----
            ActivityHints.Rules[typeof(VeniceGlassSpot)] = _ => ("유리 불기", VeniceSave.DoneToday("glass"));
            ActivityHints.Rules[typeof(VeniceNetSpot)] = _ => ("그물 던지기", VeniceSave.DoneToday("net"));
            ActivityHints.Rules[typeof(VeniceFeed)] = _ => ("비둘기 모이 주기", VeniceSave.DoneToday("feed"));
            ActivityHints.Rules[typeof(VeniceTraghetto)] = _ => (VeniceUtil.Map == "venice_rialto" ? "곤돌라 경주" : null, false);
            ActivityHints.Rules[typeof(VeniceWorkshop)] = i => ((VeniceWorkshop)i).recipe == VeniceWorkshop.Recipe.GlassArt
                ? ("유리 장식 공방", false)
                : ("레이스 공방", Crafting.DoneForToday("venice_lace"));
        }
    }
}
