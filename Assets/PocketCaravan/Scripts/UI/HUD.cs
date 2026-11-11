using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 왼쪽 위 상태 창: 맵, 돈·신용, 짐 무게, 체력, 노면/감속 안내. 오른쪽 위 퀘스트·지도·장부 단추.
    /// 10/28 UI 통일: 평소엔 돈·체력 한 줄만, Tab을 누르면 5초 동안 펼친다 (자동으로 펼치지 않음).
    /// 오른쪽 위 할 일(QuestTracker)도 Expanded를 보고 같이 펼친다.
    /// 11/05 B안(CuteUI): 초상 + 돈·체력 카드, 오른쪽 위 모양이 다른 단추 3개(누르면 Q·M·L과 같다)와 중립색 키캡.
    ///   아래 조작 안내 줄은 Tab으로 펼쳤을 때만 보인다 (평소 화면 글씨 줄이기).
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        const float ExpandSeconds = 5f;
        static float expandedUntil = -1f;
        const int ButtonSize = 22, ButtonGap = 5;   // 11/11 윤채: 26·7 → 22·5 (오른쪽 위 단추 다섯 개가 화면을 많이 차지했다)

        /// <summary>Tab으로 펼친 동안 true (상태창·할 일 모두)</summary>
        public static bool Expanded => Time.unscaledTime < expandedUntil;

        /// <summary>
        /// 화면 아래에서 HUD가 차지한 높이 (조작 안내 · 저장 문제 줄 · 아래 가운데 한 줄 안내). 알림·채팅·고려 배우기 카드는 그 위에 뜬다.
        /// 11/08 아래에서부터 조작 안내(1~2줄) → 저장 문제 줄 → 한 줄 안내(BottomLine) 차례로 쌓아 서로 덮지 않게 했다
        /// </summary>
        public static float BottomReserved => BaseReserved + LinesHeight();

        /// <summary>조작 안내 · 저장 문제 줄이 차지한 높이 (한 줄 안내는 이 위에 쌓는다)</summary>
        static float BaseReserved
        {
            get
            {
                var gm = GameManager.I;
                bool issue = gm != null && !string.IsNullOrEmpty(gm.LoadIssue);
                float strip = KeyStripHeight();
                return strip + (issue ? (strip > 0 ? 22f : 24f) : 0f);
            }
        }

        /// <summary>펼친 조작 안내의 줄 수에 따른 높이 (아래 끝에서 윗변까지). 펼치지 않았으면 0</summary>
        static float KeyStripHeight() => !Expanded || MapRoot.Current == null ? 0f : KeySplit() < Keys.Length ? 45f : 24f;

        /// <summary>조작 안내를 한 줄에 몇 개까지 놓는지 (화면보다 길면 나머지는 둘째 줄)</summary>
        static int KeySplit()
        {
            float maxW = UIStyle.VirtualWidth - 20, rowW = 0;
            for (int i = 0; i < Keys.Length; i++)
            {
                rowW += CuteUI.KeycapWidth(Keys[i].key) + 3 + CuteUI.Width(Keys[i].what, true) + 8;
                if (rowW > maxW) return i;
            }
            return Keys.Length;
        }

        static readonly System.Collections.Generic.Dictionary<int, (int frame, float h)> lines = new System.Collections.Generic.Dictionary<int, (int frame, float h)>();

        /// <summary>
        /// 11/08 아래 가운데 한 줄 안내(목표 길잡이 · 고려 친구 선물 안내 등)의 y. order가 작을수록 아래 (0 친구 안내 · 10 목표 길잡이).
        /// OnGUI마다 부른다. 부른 줄만큼 BottomReserved가 늘어 알림·채팅이 그 위로 비킨다
        /// </summary>
        public static float BottomLine(int order, float height)
        {
            lines[order] = (Time.frameCount, height);
            float y = UIStyle.VirtualHeight - BaseReserved - 6f - height;
            foreach (var kv in lines)
                if (kv.Key < order && Time.frameCount - kv.Value.frame <= 1) y -= kv.Value.h + 2f;
            return y;
        }

        static float LinesHeight()
        {
            float h = 0f;
            foreach (var kv in lines) if (Time.frameCount - kv.Value.frame <= 1) h += kv.Value.h + 2f;
            return h > 0f ? h + 4f : 0f;
        }

        /// <summary>오른쪽 위 단추 줄의 왼쪽 끝 (온라인 표시가 그 왼쪽에 붙는다)</summary>
        public static float ButtonsLeft => UIStyle.VirtualWidth - 6 - (Buttons.Length * ButtonSize + (Buttons.Length - 1) * ButtonGap);

        /// <summary>11/08 소지품(I)·메뉴(Esc)도 단추로 (마우스만으로도 할 수 있게)</summary>
        static readonly (CuteUI.Pic pic, char key, string label)[] Buttons =
        {
            (CuteUI.Pic.Sack, 'i', "I"), (CuteUI.Pic.Scroll, 'q', "Q"), (CuteUI.Pic.Compass, 'm', "M"), (CuteUI.Pic.OpenBook, 'l', "L"), (CuteUI.Pic.Menu, '\x1b', "Esc"),
        };

        static readonly (string key, string what)[] Keys =
        {
            ("WASD", "이동"), ("Shift", "달리기"), ("Space", "말 걸기"), ("Tab", "상태"),
            ("Q", "퀘스트"), ("I", "소지품"), ("M", "지도"), ("L", "장부"), ("Esc", "메뉴"),
        };

        void Update()
        {
            if (InputReader.StatusPressed() && !UIState.Blocking)
                expandedUntil = Expanded ? -1f : Time.unscaledTime + ExpandSeconds;   // 펼친 채로 다시 누르면 접기
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null) return;
            if (UIState.PhotoMode) return;   // 11/11 은샘: 기념 사진 찍는 동안 상태 카드·단추·아래 목표 줄을 감춘다
            UIStyle.Begin();

            string roadName = "";
            var player = PlayerController.Instance;
            if (RoadGrid.Current != null && player != null)
            {
                int g = RoadGrid.Current.GradeAt(player.Motor.Cell);
                var names = gm.Db.roads.gradeNames;
                roadName = g < names.Length ? names[g] : g.ToString();
            }

            if (Expanded) DrawExpanded(gm, roadName); else DrawCompact(gm);
            DrawButtons();
            DrawBottom(gm);
        }

        /// <summary>평소: 초상 + 돈 · 체력 막대 (Tab 키캡)</summary>
        static void DrawCompact(GameManager gm)
        {
            var p = gm.Player;
            string money = p.money.ToString();
            float tabW = CuteUI.KeycapWidth("Tab");
            float w = Mathf.Max(108f, 32f + CuteUI.Width(money, false, true) + 3f + CuteUI.Width("냥", true) + 8f + tabW + 8f);
            var card = new Rect(22, 5, w, 40);
            ScreenZones.Mark("hud.card", Rect.MinMaxRect(4, 4, card.xMax, card.yMax));   // 초상까지
            CuteUI.Card(card);
            if (!UIState.Blocking && GUI.Button(Rect.MinMaxRect(4, 4, card.xMax, card.yMax), GUIContent.none, GUIStyle.none)) InputReader.Press('\t');   // 11/08 눌러도 Tab처럼 펼치기
            CuteUI.Icon(CuteUI.Pic.Coin, 42, 14);
            float x = CuteUI.Label(54, 18.5f, money, CuteUI.Text, false, true);
            CuteUI.Label(x + 3, 19, "냥", CuteUI.Sub, true);
            CuteUI.Keycap(card.xMax - 8 - tabW, 12, "Tab");
            CuteUI.Icon(CuteUI.Pic.Bolt, 40, 27);
            CuteUI.Label(51, 32, "체력", CuteUI.Sub, true);
            CuteUI.Bar(new Rect(75, 28, card.xMax - 8 - 75, 8), p.stamina, CuteUI.Stamina, CuteUI.StaminaHi);
            CuteUI.Portrait(4, 5, 36);
        }

        /// <summary>Tab: 맵 · 장날 · 돈·신용 · 평판 · 짐 · 체력 · 발밑</summary>
        static void DrawExpanded(GameManager gm, string roadName)
        {
            var p = gm.Player;
            var map = MapRoot.Current;
            string repTitle, repText;
            var marketId = WorldIndex.Get()?.Map(map.mapId)?.town;
            var town = gm.Db.towns.Find(t => t != null && t.id == marketId);
            if (town != null)
            {
                repTitle = "평판 · " + (town.region != null && !town.region.townReputation ? town.region.displayName : town.displayName);
                repText = ReputationState.ProgressText(ReputationPricing.Entry(p, town));
            }
            else if (map.region != null && !map.region.townReputation)
            {
                repTitle = "평판 · " + map.region.displayName;
                repText = ReputationState.ProgressText(p.reputation.Get(ReputationScope.Civilization, map.region.id));
            }
            else { repTitle = "마을별 평판"; repText = "지도에서 확인"; }

            const float w = 210f;
            float x = 22, y = 5, inner = w - 20;
            // 10/28 평판이 두 줄로 늘어나면 아래 줄을 덮던 문제: 높이를 재서 칸을 늘린다
            float repH = CuteUI.WrapHeight(repText, inner, true);
            float h = 30 + 16 + 6 + 12 + repH + 4 + 15 + 15 + 7 + 12 + (p.HasAnyCargo ? 12 : 0) + 8;
            ScreenZones.Mark("hud.card", Rect.MinMaxRect(4, 4, x + w, y + h));
            CuteUI.Card(new Rect(x, y, w, h));
            if (!UIState.Blocking && GUI.Button(Rect.MinMaxRect(4, 4, x + w, y + h), GUIContent.none, GUIStyle.none)) InputReader.Press('\t');   // 11/08 눌러도 접기

            // 맵 이름 + 장날 (초상 오른쪽)
            float tx = CuteUI.Label(46, y + 13, map.displayName, CuteUI.Text, false, true);
            if (gm.Clock.IsMarketDay)
            {
                float bw = CuteUI.Width("장날", true, true) + 10;
                CuteUI.Box(new Rect(tx + 6, y + 6, bw, 13), 5, CuteUI.Deco, CuteUI.Ink);
                CuteUI.LabelCenter(tx + 6 + bw / 2f, y + 12.5f, "장날", CuteUI.Ink, true, true);
            }
            // 돈 · 신용
            float cy = y + 30;
            CuteUI.Icon(CuteUI.Pic.Coin, 46, cy - 4);
            float mx = CuteUI.Label(58, cy, p.money.ToString(), CuteUI.Text, false, true);
            CuteUI.Label(mx + 3, cy + 0.5f, "냥", CuteUI.Sub, true);
            CuteUI.LabelRight(x + w - 10, cy, $"신용 {p.credit}", CuteUI.Sub, true);
            cy += 16;
            CuteUI.Fill(new Rect(x + 10, cy, w - 20, 1), CuteUI.Line);
            cy += 6;
            // 평판
            CuteUI.Label(x + 10, cy + 5, repTitle, CuteUI.Text, true, true);
            cy += 12;
            cy += CuteUI.Wrap(x + 10, cy, inner, repText, CuteUI.Sub, true) + 4;
            // 짐 · 체력
            string vehicle = p.vehicle != null ? p.vehicle.displayName : "등짐";
            float barX = x + w - 10 - 76;
            CuteUI.Icon(CuteUI.Pic.Sack, x + 10, cy + 3);
            CuteUI.Label(x + 23, cy + 7, $"{vehicle} {p.TotalWeight:0.#}/{p.Capacity:0}", CuteUI.Text, true);
            CuteUI.Bar(new Rect(barX, cy + 3, 76, 8), p.LoadRatio, CuteUI.Load, CuteUI.LoadHi);
            cy += 15;
            CuteUI.Icon(CuteUI.Pic.Bolt, x + 10, cy + 2);
            CuteUI.Label(x + 23, cy + 7, "체력", CuteUI.Text, true);
            CuteUI.Bar(new Rect(barX, cy + 3, 76, 8), p.stamina, CuteUI.Stamina, CuteUI.StaminaHi);
            cy += 15;
            CuteUI.Fill(new Rect(x + 10, cy + 2, w - 20, 1), CuteUI.Line);
            cy += 7;
            CuteUI.Label(x + 10, cy + 6, $"발밑: {roadName}", CuteUI.Sub, true);
            cy += 12;
            if (p.HasAnyCargo) CuteUI.Label(x + 10, cy + 6, "거친 길·무거운 짐은 느려요", CuteUI.Sub, true);

            CuteUI.Portrait(4, 5, 36);
        }

        /// <summary>오른쪽 위 단추: 세로 두루마리(퀘스트)·나침반(지도)·펼친 책(장부). 아래에 중립 키캡</summary>
        static void DrawButtons()
        {
            float x = ButtonsLeft;
            ScreenZones.Mark("hud.buttons", new Rect(x, 4, Buttons.Length * ButtonSize + (Buttons.Length - 1) * ButtonGap, ButtonSize + 8));   // 11/11 단추 다섯 개 전부 (전에는 셋만 쳤다)
            foreach (var b in Buttons)
            {
                CuteUI.FramedCircle(x, 4, ButtonSize);
                var s = CuteUI.PicSize(b.pic);
                CuteUI.Icon(b.pic, x + Mathf.Floor((ButtonSize - s.x + 1) / 2f), 4 + Mathf.Floor((ButtonSize - s.y) / 2f));
                float kw = CuteUI.KeycapWidth(b.label);
                CuteUI.Keycap(x + Mathf.Round((ButtonSize - kw) / 2f), 4 + ButtonSize - 5, b.label);
                if (!UIState.Blocking && GUI.Button(new Rect(x, 4, ButtonSize, ButtonSize + 8), GUIContent.none, GUIStyle.none)) InputReader.Press(b.key);
                x += ButtonSize + ButtonGap;
            }
        }

        static void DrawBottom(GameManager gm)
        {
            float vh = UIStyle.VirtualHeight, vw = UIStyle.VirtualWidth;
            if (!string.IsNullOrEmpty(gm.LoadIssue))
            {
                // 11/08 조작 안내가 펼쳐져 있으면 그 위, 아니면 맨 아래 (예전엔 늘 vh-44라 알림·채팅이 이 줄 안에 떴다)
                const string issue = "저장 복원 중단 · 이번 세션 저장 안 됨 · Esc에서 원인 확인";
                float iw = Mathf.Min(vw - 12, 30 + CuteUI.Width(issue, true) + 8);
                float strip = KeyStripHeight(), iy = strip > 0 ? vh - strip - 22 : vh - 24;
                CuteUI.Box(new Rect(6, iy, iw - 10, 18), 8, CuteUI.AlertSoft, CuteUI.Alert, 1, 2, CuteUI.SoftShadow);
                CuteUI.Circle(13, iy + 5, 8, CuteUI.Alert, CuteUI.Alert, 0);
                CuteUI.Label(26, iy + 9, issue, CuteUI.Text, true);
            }
            if (!Expanded) return;

            // 조작 안내 (Tab으로 펼쳤을 때만): 키캡 + 설명, 화면보다 길면 두 줄
            int split = KeySplit();
            DrawKeyRow(0, split, split < Keys.Length ? vh - 42 : vh - 21);
            if (split < Keys.Length) DrawKeyRow(split, Keys.Length, vh - 21);
        }

        static void DrawKeyRow(int from, int to, float y)
        {
            float w = 6;
            for (int i = from; i < to; i++) w += CuteUI.KeycapWidth(Keys[i].key) + 3 + CuteUI.Width(Keys[i].what, true) + 8;
            CuteUI.Box(new Rect(6, y - 3, w + 2, 19), 8, CuteUI.Paper, CuteUI.Paper, 0, 2, CuteUI.SoftShadow);
            float x = 12;
            for (int i = from; i < to; i++)
            {
                x += CuteUI.Keycap(x, y, Keys[i].key, true) + 3;
                x = CuteUI.Label(x, y + 6.5f, Keys[i].what, CuteUI.Sub, true) + 8;
            }
        }
    }
}
