using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 위 글자와 표시: 사람 머리 위 이름표, 말 걸 수 있을 때 "Space", 퀘스트 길 안내 화살표.
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class WorldLabels : MonoBehaviour
    {
        static Texture2D arrowTex, downTex;
        static GUIStyle nameStyle, shadowStyle, guideStyle;

        static readonly Color QuestColor = new Color(1f, 0.85f, 0.25f);
        static readonly Color MarketColor = new Color(0.55f, 0.95f, 0.6f);
        static readonly Color PersonColor = new Color(0.95f, 0.95f, 0.95f);
        static readonly Color BoardColor = new Color(0.62f, 0.85f, 1f);
        static readonly Color DangerColor = new Color(1f, 0.42f, 0.38f);

        void OnGUI()
        {
            var gm = GameManager.I;
            var cam = Camera.main;
            if (gm == null || cam == null || MapRoot.Current == null || UIState.Blocking) return;
            UIStyle.Begin();
            Init();

            var player = PlayerController.Instance;
            var nav = Navigator.Compute(gm);

            // 1) 이름표
            foreach (var it in Interactable.All)
            {
                Vector2 p = ToGui(cam, it.transform.position + new Vector3(0f, 1.0f, 0f));
                if (p.x < -50 || p.y < -20 || p.x > UIStyle.VirtualWidth + 50 || p.y > UIStyle.VirtualHeight + 20) continue;
                string label = it.DisplayName;
                if (string.IsNullOrEmpty(label)) continue;   // 이름 없는 자리(같은 모닥불의 두 번째 의자 등)는 이름표를 띄우지 않는다
                Color c = it.Category switch
                {
                    Interactable.Kind.Quest => QuestColor,
                    Interactable.Kind.Market => MarketColor,
                    Interactable.Kind.Board => BoardColor,
                    Interactable.Kind.Danger => DangerColor,
                    _ => PersonColor,
                };
                bool target = nav.HasValue && nav.Value.target == it;
                Outlined(new Rect(p.x - 60, p.y - 8, 120, 12), label, c, nameStyle);
                // 퀘스트 NPC는 머리 위 !·? 표시가 있으니, 나머지(장터 등)만 화살표로 짚어 준다
                if (target && it.Category != Interactable.Kind.Quest) DrawDown(p + new Vector2(0, -12));
            }

            // 2) 말 걸기 안내 (11/05 B안: 보라 알약 = 상호작용, 안에 중립 키캡 [Space])
            if (player != null && player.TalkTarget != null && !UIState.Blocking)
            {
                Vector2 p = ToGui(cam, player.TalkTarget.transform.position + new Vector3(0f, -0.7f, 0f));
                string verb = !string.IsNullOrEmpty(player.TalkTarget.ActionVerb) ? player.TalkTarget.ActionVerb
                    : player.TalkTarget is DroppedCargo ? "줍기" : player.TalkTarget is ContractBoard ? "게시판 보기" : player.TalkTarget is TownMarket ? "사고팔기" : "말 걸기";
                CuteUI.SpacePrompt(p.x, p.y, verb);
            }

            // 3) 퀘스트 길 안내
            if (nav.HasValue && player != null && !UIState.Blocking)
            {
                var n = nav.Value;
                Vector2 from = player.transform.position;
                Vector2 d = (Vector2)n.steer - from;                 // 걸을 수 있는 길의 앞쪽을 가리킨다
                float dist = n.walkCells >= 0 ? n.walkCells : ((Vector2)n.position - from).magnitude;
                if (n.exit) DrawDown(ToGui(cam, n.position + new Vector3(0f, 0.9f, 0f)));
                if (dist > 2.2f && d.sqrMagnitude > 0.01f)
                {
                    Vector2 c = ToGui(cam, player.transform.position);
                    float ang = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;   // GUI는 y가 아래로
                    Vector2 dir = new Vector2(d.x, -d.y).normalized;
                    Vector2 a = c + dir * 26f;
                    var m = GUI.matrix;
                    GUIUtility.RotateAroundPivot(ang, a * UIStyle.Scale);
                    GUI.color = QuestColor;
                    GUI.DrawTexture(new Rect(a.x - 6, a.y - 6, 12, 12), arrowTex);
                    GUI.color = Color.white;
                    GUI.matrix = m;
                }
                string guide = n.exit ? $"{n.label}  ({Mathf.RoundToInt(dist)}칸)" : $"{n.label}  {Mathf.RoundToInt(dist)}칸";
                float gw = 220;
                Outlined(new Rect((UIStyle.VirtualWidth - gw) / 2, UIStyle.VirtualHeight - 40, gw, 14), "목표 · " + guide, QuestColor, guideStyle);
            }
        }

        static Vector2 ToGui(Camera cam, Vector3 world)
        {
            var s = cam.WorldToScreenPoint(world);
            return new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
        }

        static void Outlined(Rect r, string text, Color c, GUIStyle style)
        {
            shadowStyle.alignment = style.alignment;
            shadowStyle.fontSize = style.fontSize;
            var sr = r; sr.x += 1; sr.y += 1;
            GUI.Label(sr, text, shadowStyle);
            style.normal.textColor = c;
            GUI.Label(r, text, style);
        }

        static void DrawDown(Vector2 p)
        {
            float bob = Mathf.Floor(Mathf.Sin(Time.time * 5f) * 2f + 2f);
            GUI.color = QuestColor;
            GUI.DrawTexture(new Rect(p.x - 5, p.y - 8 + bob, 10, 8), downTex);
            GUI.color = Color.white;
        }

        static void Init()
        {
            if (arrowTex == null)
            {
                arrowTex = Triangle(12, 12, true);
                downTex = Triangle(10, 8, false);
            }
            if (nameStyle == null || nameStyle.font != UIStyle.SmallFont)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.SmallFont, alignment = TextAnchor.MiddleCenter, fontSize = UIStyle.SmallSize, clipping = TextClipping.Overflow };
                shadowStyle = new GUIStyle(nameStyle);
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                guideStyle = new GUIStyle(nameStyle) { font = GUI.skin.font, fontSize = UIStyle.BodySize };
            }
        }

        /// <summary>오른쪽(right=true) 또는 아래(false)를 가리키는 흰 삼각형</summary>
        static Texture2D Triangle(int w, int h, bool right)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inside = right
                    ? Mathf.Abs(y - (h - 1) / 2f) <= (w - 1 - x) * 0.5f
                    : Mathf.Abs(x - (w - 1) / 2f) <= y * 0.6f + 0.5f; // 텍스처 y는 아래부터: 아래가 뾰족
                t.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
            t.Apply();
            return t;
        }
    }
}
