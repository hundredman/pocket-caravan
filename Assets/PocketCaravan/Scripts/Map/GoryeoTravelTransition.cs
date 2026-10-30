using System;
using System.IO;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>고려/중앙 동쪽 이동 표시. 이동·시간·저장은 기존 Travel 코루틴이 담당한다.</summary>
    public sealed class GoryeoTravelTransition : TravelTransition
    {
        static bool registered;
        bool crossing;
        string destination;
        GUIStyle title, subtitle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterOnce()
        {
            if (registered) return;
            RegisterRoute((from, to) => RegionOf(from) != null && RegionOf(to) != null, new GoryeoTravelTransition());
            registered = true;
        }

        public static string RegionOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            id = GoryeoLayout.Resolve(Path.GetFileNameWithoutExtension(id.Replace('\\', '/')));
            switch (id)
            {
                case "Naru": case "Gaegyeong": case "Dakmok":
                case "goryeo_songak": case "goryeo_jangdan": return "고려";
                case "transit_woodtown": case "transit_ferry": case "transit_southport":
                case "transit_crossroads": case "transit_wetroad": case "transit_pass":
                case "transit_central_approach": case "transit_south_inland": return "중앙 대륙";
                default: return null;
            }
        }

        public static bool IsRegionalCrossing(string from, string to)
        {
            string a = RegionOf(from), b = RegionOf(to);
            return a != null && b != null && !String.Equals(a, b, StringComparison.Ordinal);
        }

        public override void Begin(string fromMap, string toMap)
        {
            crossing = IsRegionalCrossing(fromMap, toMap);
            destination = crossing ? RegionOf(toMap) : null;
        }

        public override float CoverSeconds => crossing ? 0.22f : 0.10f;
        public override float RevealSeconds => crossing ? 0.28f : 0.14f;
        public override float MinHoldSeconds => crossing ? 0.65f : 0f;
        public override float AfterLoadSeconds => crossing ? 0.25f : 0f;
        public override bool ShowArrivalNotice => crossing;

        public override void Draw(float cover, bool covering, string nextName, float heldFor)
        {
            Color previous = GUI.color;
            try
            {
                GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(cover));
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                if (!crossing || string.IsNullOrEmpty(destination)) return;
                if (title == null || title.font == null)
                {
                    var font = Resources.Load<Font>("Fonts/Galmuri11");
                    title = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal };
                    subtitle = new GUIStyle(title);
                }
                float scale = Mathf.Clamp(Screen.height / 270f, 1f, 4f);
                title.fontSize = Mathf.RoundToInt(22f * scale);
                subtitle.fontSize = Mathf.RoundToInt(10f * scale);
                title.normal.textColor = new Color(0.96f, 0.94f, 0.85f);
                subtitle.normal.textColor = new Color(0.78f, 0.80f, 0.75f);
                GUI.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 1f, cover)));
                float y = Screen.height * 0.46f;
                GUI.Label(new Rect(0, y - 24 * scale, Screen.width, 40 * scale), destination, title);
                if (!string.IsNullOrEmpty(nextName))
                    GUI.Label(new Rect(0, y + 20 * scale, Screen.width, 22 * scale), nextName, subtitle);
            }
            finally { GUI.color = previous; }
        }
    }
}
