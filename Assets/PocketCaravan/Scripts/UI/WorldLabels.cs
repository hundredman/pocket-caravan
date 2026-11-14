using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 위 글자와 표시: 사람 머리 위 이름표, 말 걸 수 있을 때 "Space", 퀘스트 길 안내 화살표.
    /// 11/13 기우(시연 피드백 "글이 너무 많다"): 이름표는 가까이(NearCells 칸)에서만 서서히 나타난다. 멀리 있는 퀘스트 사람은 머리 위 !·? 표시만.
    ///   [Space] 알약은 말 걸 대상 머리 위(이름표 바로 위)에, 동사는 짧게 (대화 · 보기 · 거래 · 줍기)
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class WorldLabels : MonoBehaviour
    {
        static GUIStyle nameStyle, shadowStyle;

        static readonly Color QuestColor = new Color(1f, 0.85f, 0.25f);
        static readonly Color MarketColor = new Color(0.55f, 0.95f, 0.6f);
        static readonly Color PersonColor = new Color(0.95f, 0.95f, 0.95f);
        static readonly Color BoardColor = new Color(0.62f, 0.85f, 1f);
        static readonly Color DangerColor = new Color(1f, 0.42f, 0.38f);
        static readonly Color ActivityColor = new Color(1f, 0.87f, 0.35f);   // 11/11 놀거리 (ActivityHints와 같은 노랑)

        /// <summary>11/13 이름표가 보이는 거리(칸). 모든 지역 이름표(북유럽 좌판·고려 이름표 포함)가 같이 쓴다</summary>
        public const float NearCells = 5f;
        const float FadeCells = 1f;

        /// <summary>11/13 그 자리 이름표의 진하기 (가까우면 1, NearCells 근처에서 옅어지고 그 밖은 0). 플레이어가 없으면 1</summary>
        public static float NearAlpha(Vector3 at)
        {
            var p = PlayerController.Instance;
            if (p == null) return 1f;
            float d = Vector2.Distance(p.transform.position, at);
            return Mathf.Clamp01((NearCells + FadeCells * .5f - d) / FadeCells);
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            var cam = Camera.main;
            if (gm == null || cam == null || MapRoot.Current == null || UIState.Blocking) return;
            if (UIState.PhotoMode) return;   // 11/11 기념 사진 찍는 중: 이름표·Space 안내·길 안내 화살표를 모두 감춘다
            UIStyle.Begin();
            Init();

            var player = PlayerController.Instance;
            var nav = Navigator.Compute(gm);

            var talk = player != null ? player.TalkTarget : null;
            Vector2? talkName = null;
            // 1) 이름표 (11/13 가까이에서만 · 말 걸 대상은 늘)
            foreach (var it in Interactable.All)
            {
                Vector2 p = ToGui(cam, it.transform.position + new Vector3(0f, 1.0f, 0f));
                bool target = nav.HasValue && nav.Value.target == it;
                // 퀘스트 NPC는 머리 위 !·? 표시가 있으니, 나머지(장터 등)만 화살표로 짚어 준다 (이름표가 숨어도)
                if (target && it != talk && it.Category != Interactable.Kind.Quest) GuideMarker.Down(p + new Vector2(0, -12));
                if (p.x < -50 || p.y < -20 || p.x > UIStyle.VirtualWidth + 50 || p.y > UIStyle.VirtualHeight + 20) continue;
                string label = it.DisplayName;
                if (string.IsNullOrEmpty(label)) continue;   // 이름 없는 자리(같은 모닥불의 두 번째 의자 등)는 이름표를 띄우지 않는다
                float alpha = it == talk ? 1f : NearAlpha(it.transform.position);
                if (alpha <= 0f)
                {
                    // 11/13 멀리 있으면 이름은 빼고 "!"·"?"만 (찾아갈 사람은 멀리서도 보이게)
                    if (!(label.EndsWith(" !") || label.EndsWith(" ?"))) continue;
                    label = label.Substring(label.Length - 1); alpha = 1f;
                }
                Color c = it.Category switch
                {
                    Interactable.Kind.Quest => QuestColor,
                    Interactable.Kind.Market => MarketColor,
                    Interactable.Kind.Board => BoardColor,
                    Interactable.Kind.Danger => DangerColor,
                    _ => PersonColor,
                };
                // 11/11 은샘: 오늘 할 수 있는 놀거리 자리는 이름 앞에 ★ (머리 위 놀거리 표와 이름표가 겹쳐 두 번 보이던 것을 하나로)
                if (!StoryGuide.IntroActive && ActivityHints.IsActivity(it, out bool doneToday) && !doneToday) { label = "★ " + label; c = ActivityColor; }
                if (it == talk) talkName = p;
                var oldColor = GUI.color;
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * alpha);
                Outlined(new Rect(p.x - 60, p.y - 8, 120, 12), label, c, nameStyle);
                GUI.color = oldColor;
            }

            // 2) 말 걸기 안내 (11/05 B안: 알약 = 상호작용, 안에 중립 키캡 [Space])
            //    11/13 기우: 발밑(큰 그림이면 몸을 가렸다) → 머리 위 이름표 바로 위. 이름표가 없는 자리는 이름표 자리에
            if (player != null && player.TalkTarget != null && !UIState.Blocking)
            {
                Vector2 head = talkName ?? ToGui(cam, player.TalkTarget.transform.position + new Vector3(0f, 1.0f, 0f));
                CuteUI.SpacePrompt(head.x, head.y - (talkName.HasValue ? 26f : 10f), VerbFor(player.TalkTarget));
            }

            // 3) 퀘스트 길 안내 (11/13 기우: 캐릭터 둘레의 작은 삼각형 대신 목적지 위 ▼ · 화면 밖이면 화면 끝 핀, GuideMarker)
            if (nav.HasValue && player != null && !UIState.Blocking)
            {
                var n = nav.Value;
                Vector2 from = player.transform.position;
                float dist = n.walkCells >= 0 ? n.walkCells : ((Vector2)n.position - from).magnitude;
                GuideMarker.Draw(ToGui(cam, n.position + new Vector3(0f, 0.9f, 0f)), ToGui(cam, player.transform.position), n, Mathf.RoundToInt(dist));
                string guide = n.exit ? $"{n.label}  ({Mathf.RoundToInt(dist)}칸)" : $"{n.label}  {Mathf.RoundToInt(dist)}칸";
                CuteUI.GoalPill("목표 · " + guide);   // 11/13 왼쪽 할 일 카드의 목표 줄로 (TaskCard)
            }
        }

        static Vector2 ToGui(Camera cam, Vector3 world)
        {
            var s = cam.WorldToScreenPoint(world);
            return new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
        }

        /// <summary>
        /// 11/11 Space 알약에 쓸 동사 (거래 · 보기 · 줍기 · 불 쬐기 …). 고려 이름표(GoryeoPlaceLabels)도 같이 쓴다.
        /// 11/13 기우: 사람에게 말 걸기는 누구나 예상하니 튜토리얼이 끝나면 키캡만([Space]). 무엇을 하는지 예상하기 어려운 것만 동사를 붙인다
        /// </summary>
        public static string VerbFor(Interactable t)
        {
            if (t != null && !string.IsNullOrEmpty(t.ActionVerb) && t.ActionVerb != "말 걸기" && t.ActionVerb != "대화") return t.ActionVerb;
            if (t is DroppedCargo) return "줍기";
            if (t is ContractBoard) return "보기";
            if (t is TownMarket) return "거래";
            return TalkVerb;
        }

        /// <summary>사람에게 말 걸기: 시작 튜토리얼 동안만 "대화", 끝나면 빈 글(키캡만)</summary>
        public static string TalkVerb => StoryGuide.IntroActive ? "대화" : "";

        static void Outlined(Rect r, string text, Color c, GUIStyle style)
        {
            shadowStyle.alignment = style.alignment;
            shadowStyle.fontSize = style.fontSize;
            var sr = r; sr.x += 1; sr.y += 1;
            GUI.Label(sr, text, shadowStyle);
            style.normal.textColor = c;
            GUI.Label(r, text, style);
        }

        static void Init()
        {
            if (nameStyle == null || nameStyle.font != UIStyle.SmallFont)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.SmallFont, alignment = TextAnchor.MiddleCenter, fontSize = UIStyle.SmallSize, clipping = TextClipping.Overflow };
                shadowStyle = new GUIStyle(nameStyle);
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            }
        }
    }
}
