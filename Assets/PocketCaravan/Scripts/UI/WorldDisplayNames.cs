namespace PocketCaravan
{
    /// <summary>Display aliases only. Full identity strings stay in social/quest/save data.</summary>
    public static class WorldDisplayNames
    {
        public static string For(Interactable target)
        {
            if(target==null)return "";
            bool own=MapRoot.Current!=null&&GoryeoTravelTransition.RegionOf(MapRoot.Current.mapId)!=null;
            if(own&&target is TownMarket)return "상인";
            if(target is GoryeoPastime)return "낚시꾼";
            return ShortName(target.DisplayName);
        }
        public static string ShortName(string text)
        {
            if(string.IsNullOrEmpty(text))return text;
            if(text.EndsWith(" !")||text.EndsWith(" ?"))text=text.Substring(0,text.Length-2);
            if(text=="나루 우물가 아낙")return "아낙";
            foreach(var prefix in new[]{"닥나무 마을 ","나루 마을 ","예성 나루 ","나루 ","장단 ","개경 ","송악 ","닥목 ","산록 "})
                if(text.StartsWith(prefix,System.StringComparison.Ordinal))return text.Substring(prefix.Length);
            // No global character cap. Dense layouts require individual placement/alias review.
            return text;
        }
    }
}
