using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 왼쪽 위 상태 창: 맵, 돈·신용, 짐 무게, 체력, 노면/감속 안내. 아래쪽에 조작 안내.
    /// 10/28 UI 통일: 평소엔 돈·체력 한 줄만, Tab을 누르면 5초 동안 펼친다 (자동으로 펼치지 않음).
    /// 오른쪽 위 할 일(QuestTracker)도 Expanded를 보고 같이 펼친다.
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        static readonly Color32 LoadColor = new Color32(0xE9, 0xC4, 0x6A, 0xFF);
        static readonly Color32 StaminaColor = new Color32(0x7C, 0xD3, 0x7A, 0xFF);
        static readonly Color32 ShakeLow = new Color32(0x9E, 0xC9, 0xE8, 0xFF);
        static readonly Color32 ShakeHigh = new Color32(0xE8, 0x6A, 0x5A, 0xFF);
        const float ExpandSeconds = 5f;
        static float expandedUntil = -1f;

        /// <summary>Tab으로 펼친 동안 true (상태창·할 일 모두)</summary>
        public static bool Expanded => Time.unscaledTime < expandedUntil;

        void Update()
        {
            if (InputReader.StatusPressed() && !UIState.Blocking)
                expandedUntil = Expanded ? -1f : Time.unscaledTime + ExpandSeconds;   // 펼친 채로 다시 누르면 접기
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null) return;
            UIStyle.Begin();
            var p = gm.Player;
            var map = MapRoot.Current;

            string roadName = "";
            var player = PlayerController.Instance;
            if (RoadGrid.Current != null && player != null)
            {
                int g = RoadGrid.Current.GradeAt(player.Motor.Cell);
                var names = gm.Db.roads.gradeNames;
                roadName = g < names.Length ? names[g] : g.ToString();
            }

            if (!Expanded)
            {
                DrawCompact(gm);
                DrawBottom(gm);
                return;
            }

            var marketId=WorldIndex.Get()?.Map(map.mapId)?.town;
            var town=gm.Db.towns.Find(t=>t!=null&&t.id==marketId);
            string rep;
            if(town!=null)
            {
                string target=town.region!=null&&!town.region.townReputation?town.region.displayName:town.displayName;
                rep="평판 "+target+"\n"+ReputationState.ProgressText(ReputationPricing.Entry(p,town));
            }
            else if(map.region!=null&&!map.region.townReputation)rep="평판 "+map.region.displayName+"\n"+ReputationState.ProgressText(p.reputation.Get(ReputationScope.Civilization,map.region.id));
            else rep="마을별 평판\n지도에서 확인";
            // 10/28 평판이 두 줄로 늘어나면 아래 등짐 줄을 덮던 문제: 높이를 재서 칸을 늘린다
            float repH = Mathf.Max(24f, UIStyle.Wrap.CalcHeight(new GUIContent(rep), 170f));
            var r = new Rect(6, 6, 182, 85 + repH + (p.HasAnyCargo ? 12 : 0));
            GUI.Box(r, "", UIStyle.Box);
            float x = r.x + 8, y = r.y + 5;
            string market = gm.Clock.IsMarketDay ? " <color=#ffd84a>장날</color>" : "";
            GUI.Label(new Rect(x, y - 1, 160, 16), $"{map.displayName}{market}", UIStyle.Rich); y += 13;
            GUI.Label(new Rect(x, y - 1, 160, 16), $"<color=#ffe08a>돈 {p.money}</color>   신용 {p.credit}", UIStyle.Rich); y += 15;
            GUI.Label(new Rect(x,y-1,170,repH),rep,UIStyle.Wrap);y+=repH+2;

            string vehicle = p.vehicle != null ? p.vehicle.displayName : "등짐";
            Gauge(x, ref y, $"{vehicle} {p.TotalWeight:0.#}/{p.Capacity:0}", p.LoadRatio, LoadColor);
            Gauge(x, ref y, "체력", p.stamina, StaminaColor);
            GUI.Label(new Rect(x, y - 1, 170, 12), $"발밑: {roadName}", UIStyle.Small);
            y += 12;
            if (p.HasAnyCargo)
            {
                string advice = "거친 길·무거운 짐은 느립니다";
                GUI.Label(new Rect(x, y - 1, 170, 12), advice, UIStyle.Small);
            }

            DrawBottom(gm);
        }

        /// <summary>평소 한 줄: 돈 · 체력 막대 (Tab 자세히)</summary>
        static void DrawCompact(GameManager gm)
        {
            var p = gm.Player;
            var r = new Rect(6, 6, 168, 18);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 7, r.y + 1, 70, 16), $"<color=#ffe08a>돈 {p.money}</color>", UIStyle.Rich);
            GUI.Label(new Rect(r.x + 66, r.y + 3, 30, 12), "체력", UIStyle.Small);
            UIStyle.Bar(new Rect(r.x + 88, r.y + 6, 50, 6), p.stamina, StaminaColor);
            GUI.Label(new Rect(r.x + 142, r.y + 3, 24, 12), "<color=#9a93ad>Tab</color>", UIStyle.Small);
        }

        static void DrawBottom(GameManager gm)
        {
            if (!string.IsNullOrEmpty(gm.LoadIssue))
            {
                var status = new Rect(6, UIStyle.VirtualHeight-43, Mathf.Min(350,UIStyle.VirtualWidth-12),22);
                GUI.Box(status,"",UIStyle.Box);
                GUI.Label(new Rect(status.x+5,status.y+3,status.width-10,18),"저장 복원 중단 · 이번 세션 저장 안 됨 · Esc에서 원인 확인",UIStyle.Small);
            }
            GUI.Label(new Rect(6, UIStyle.VirtualHeight - 16, 520, 14),
                "WASD 이동 · Shift 달리기 · Space 말 걸기 · Tab 상태 · Q 퀘스트 · M 지도 · L 장부 · Esc 메뉴", UIStyle.Small);
        }

        static void Gauge(float x, ref float y, string label, float v, Color32 c)
        {
            GUI.Label(new Rect(x, y - 2, 70, 12), label, UIStyle.Small);
            UIStyle.Bar(new Rect(x + 72, y + 2, 82, 6), v, c);
            y += 11;
        }
    }
}
