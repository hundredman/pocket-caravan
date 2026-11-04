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
            // 도착 알림(화면을 덮지 않음): 지역이 바뀌면 "지역 · 맵", 장터 마을에 들어서면 마을 이름.
            // 다른 지역(북유럽 순록·베네치아 고양이)은 마을 도착 때 이름표를 보여 준다. 고려·중앙 동쪽은 땅이 이어져 있어 덮지 않고 알림만.
            pendingRegion = destination;
            pendingTown = crossing || IsTown(toMap);
            pendingMap = toMap;
            // GameEvents는 Play 시작·새 게임 때 구독을 모두 지우므로 이동할 때마다 다시 건다(중복 없이).
            GameEvents.MapEntered -= Announce; GameEvents.MapEntered += Announce;
        }

        static string pendingRegion, pendingMap;
        static bool pendingTown;
        static readonly string[] TownMaps =
        {
            "Naru", "Gaegyeong", "Dakmok", "goryeo_songak", "goryeo_jangdan",
            "transit_woodtown", "transit_ferry", "transit_southport", "transit_crossroads",
        };
        static bool IsTown(string id) => Array.IndexOf(TownMaps, GoryeoLayout.Resolve(id ?? "")) >= 0;

        static void Announce(string mapId)
        {
            if (!pendingTown || pendingMap != mapId || MapRoot.Current == null) { pendingTown = false; return; }
            string name = MapRoot.Current.displayName;
            if (!string.IsNullOrEmpty(pendingRegion)) Notify.Location(pendingRegion + " · " + name, 3.5f);
            else Notify.Location(name, 2.5f);
            pendingTown = false; pendingRegion = null;
        }

        // 2026-11-04(기우 요청·은샘 지시): 고려·중앙 동쪽 맵은 땅이 이어져 있어 출구가 아니라 걸어서 넘어간다.
        // 그래서 맵을 넘을 때 검은 화면을 띄우지 않는다(맵 이동처럼 보이지 않게). 이름 알림은 Announce(지역이 바뀔 때·장터 마을 도착).
        public override float CoverSeconds => 0f;
        public override float RevealSeconds => 0f;
        public override float MinHoldSeconds => 0f;
        public override float AfterLoadSeconds => 0f;
        public override bool ShowArrivalNotice => false;   // 알림은 Announce가 띄운다

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
