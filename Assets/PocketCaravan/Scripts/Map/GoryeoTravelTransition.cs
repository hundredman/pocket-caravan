using System;
using System.IO;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>고려/중앙 동쪽 이동 표시(창호지 문). 이동·시간·저장은 기존 Travel 코루틴이 담당한다.</summary>
    public sealed class GoryeoTravelTransition : TravelTransition
    {
        static bool registered;
        bool crossing;
        string destination;

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
            showCard = crossing || IsTown(toMap);
        }

        static readonly string[] TownMaps =
        {
            "Naru", "Gaegyeong", "Dakmok", "goryeo_songak", "goryeo_jangdan",
            "transit_woodtown", "transit_ferry", "transit_southport", "transit_crossroads",
        };
        public static bool IsTown(string id) => Array.IndexOf(TownMaps, GoryeoLayout.Resolve(id ?? "")) >= 0;

        // 2026-11-04 은샘 결정(A안): 창호지 미닫이 두 짝이 양옆에서 닫혔다가 열린다. 덮인 동안 맵을 불러와 멈칫하는 순간을 가린다.
        // 마을에 들어설 때·지역이 바뀔 때만 가운데 나무 현판에 이름을 보여 준다. 길 맵끼리는 문만 짧게 닫혔다 열린다.
        bool showCard;
        public override float CoverSeconds => 0.26f;
        public override float RevealSeconds => 0.32f;
        public override float MinHoldSeconds => showCard ? 0.6f : 0.05f;
        public override float AfterLoadSeconds => 0.05f;          // 새 맵 첫 화면(카메라 붙이기)이 문 뒤에서 그려지게
        public override bool ShowArrivalNotice => false;          // 이름은 현판이 보여 준다

        static readonly Color Hanji = new Color32(238, 229, 204, 255);
        static readonly Color HanjiShade = new Color32(226, 214, 184, 255);
        static readonly Color Lattice = new Color32(128, 92, 60, 255);
        static readonly Color Wood = new Color32(92, 62, 40, 255);
        static readonly Color Board = new Color32(58, 40, 28, 255);
        static readonly Color Gold = new Color32(196, 160, 92, 255);
        static readonly Color Letter = new Color32(240, 226, 180, 255);

        public override void Draw(float cover, bool covering, string nextName, float heldFor)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color previous = GUI.color;
            try
            {
                float sw = Screen.width, sh = Screen.height;
                int u = Mathf.Max(1, Mathf.FloorToInt(sh / 270f));          // 화면 1픽셀(480×270 기준)의 실제 크기
                float k = Ease(Mathf.Clamp01(cover));
                float half = Mathf.Ceil(sw / 2f * k);
                if (half > 0f)
                {
                    Panel(0f, half, half, u, sh, true);                        // 왼쪽 문: 안쪽 끝이 half
                    Panel(sw - half, sw, sw - half, u, sh, false);             // 오른쪽 문: 안쪽 끝이 sw-half
                }
                if (!showCard || string.IsNullOrEmpty(nextName)) return;
                float a = covering ? Mathf.Clamp01((cover - 0.85f) / 0.15f) : Mathf.Clamp01((cover - 0.6f) / 0.3f);
                if (a > 0f) Card(nextName, destination, a, u, sw, sh);
            }
            finally { GUI.color = previous; }
        }

        static float Ease(float t) => t * t * (3f - 2f * t);

        static void Panel(float x0, float x1, float inner, int u, float sh, bool left)
        {
            Fill(new Rect(x0, 0, x1 - x0, sh), Hanji, 1f);
            float cell = 24 * u;
            // 창살: 문이 다 닫혔을 때와 같은 자리(안쪽 끝 기준)에 고정해 문이 미끄러져 들어오는 것처럼 보인다
            for (float x = inner; left ? x >= x0 : x <= x1; x += left ? -cell : cell)
                Fill(new Rect(left ? x - 2 * u : x, 0, 2 * u, sh), Lattice, 1f);
            for (float y = 0; y <= sh; y += cell)
                Fill(new Rect(x0, y, x1 - x0, 2 * u), Lattice, 1f);
            Fill(new Rect(x0, 0, x1 - x0, 5 * u), Wood, 1f);                   // 위 문틀
            Fill(new Rect(x0, sh - 6 * u, x1 - x0, 6 * u), Wood, 1f);          // 아래 문틀
            float edge = left ? inner - 4 * u : inner;
            Fill(new Rect(edge, 0, 4 * u, sh), Wood, 1f);                      // 맞닿는 문살
            Fill(new Rect(left ? edge - u : edge + 4 * u, 0, u, sh), HanjiShade, 1f);
        }

        GUIStyle nameStyle, regionStyle;
        void Card(string name, string region, float a, int u, float sw, float sh)
        {
            if (nameStyle == null || nameStyle.font == null)
            {
                var font = Resources.Load<Font>("Fonts/GoryeoGalmuri11") ?? Resources.Load<Font>("Fonts/Galmuri11");
                nameStyle = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, wordWrap = false };
                regionStyle = new GUIStyle(nameStyle);
            }
            nameStyle.fontSize = 16 * u; regionStyle.fontSize = 9 * u;
            nameStyle.normal.textColor = Letter; regionStyle.normal.textColor = Gold;
            bool two = !string.IsNullOrEmpty(region);
            float tw = nameStyle.CalcSize(new GUIContent(name)).x;
            float w = Mathf.Round(tw + 32 * u), h = (two ? 40 : 30) * u;
            var r = new Rect(Mathf.Round(sw / 2f - w / 2f), Mathf.Round(sh / 2f - h / 2f), w, h);
            Fill(new Rect(r.x - 3 * u, r.y - 3 * u, r.width + 6 * u, r.height + 6 * u), Wood, a);
            Fill(r, Board, a);
            Outline(new Rect(r.x + 2 * u, r.y + 2 * u, r.width - 4 * u, r.height - 4 * u), u, Gold, a);
            GUI.color = new Color(1f, 1f, 1f, a);
            if (two)
            {
                GUI.Label(new Rect(r.x, r.y + 4 * u, r.width, 12 * u), region, regionStyle);
                GUI.Label(new Rect(r.x, r.y + 14 * u, r.width, 22 * u), name, nameStyle);
            }
            else GUI.Label(r, name, nameStyle);
        }

        static void Outline(Rect r, int u, Color c, float a)
        {
            Fill(new Rect(r.x, r.y, r.width, u), c, a); Fill(new Rect(r.x, r.yMax - u, r.width, u), c, a);
            Fill(new Rect(r.x, r.y, u, r.height), c, a); Fill(new Rect(r.xMax - u, r.y, u, r.height), c, a);
        }

        static void Fill(Rect r, Color c, float a)
        {
            c.a *= a; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }
    }
}
