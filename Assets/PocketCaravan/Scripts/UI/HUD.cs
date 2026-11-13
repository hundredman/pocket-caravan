using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 화면 왼쪽 위 상태 창: 맵, 돈·신용, 짐 무게, 체력, 노면/감속 안내. 오른쪽 위 퀘스트·지도·장부 단추.
    /// 10/28 UI 통일: 평소엔 돈·체력 한 줄만, Tab을 누르면 5초 동안 펼친다 (자동으로 펼치지 않음).
    /// 11/13 상태 카드 바로 밑에 할 일 카드(TaskCard)를 그린다. Tab이면 할 일 카드도 같이 펼친다.
    /// 11/05 B안(CuteUI): 초상 + 돈·체력 카드, 오른쪽 위 모양이 다른 단추 3개(누르면 Q·M·L과 같다)와 중립색 키캡.
    ///   아래 조작 안내 줄은 Tab으로 펼쳤을 때만 보인다 (평소 화면 글씨 줄이기).
    /// GameManager가 자동으로 붙인다. 임시 UI(OnGUI)다.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        const float ExpandSeconds = 5f;
        static float expandedUntil = -1f;
        const int ButtonSize = 22, ButtonGap = 5;
        /// <summary>11/13 왼쪽 위 줄(상태 카드 · 작은 표 · 할 일 카드)의 오른쪽 끝. 셋 다 이 너비에 맞춘다</summary>
        public const float ColumnRight = 214f;   // 11/11 윤채: 26·7 → 22·5 (오른쪽 위 단추 다섯 개가 화면을 많이 차지했다)

        /// <summary>Tab으로 펼친 동안 true (상태창·할 일 모두)</summary>
        public static bool Expanded => Time.unscaledTime < expandedUntil;
        /// <summary>11/13 마지막으로 펼친 프레임 (할 일 카드가 펼치기 전에 올라온 것을 펼친 동안 그대로 보여 준다)</summary>
        public static int ExpandedFrame { get; private set; } = -10;

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

        /// <summary>11/08 소지품(I)·메뉴(Esc)도 단추로 (마우스만으로도 할 수 있게). 11/13 Q(여정 수첩) 단추는 뺐다: 왼쪽 할 일 카드를 누르면 열린다</summary>
        static readonly (CuteUI.Pic pic, char key, string label)[] Buttons =
        {
            (CuteUI.Pic.Sack, 'i', "I"), (CuteUI.Pic.Compass, 'm', "M"), (CuteUI.Pic.OpenBook, 'l', "L"), (CuteUI.Pic.Menu, '\x1b', "Esc"),
        };

        static readonly (string key, string what)[] Keys =
        {
            ("WASD", "이동"), ("Shift", "달리기"), ("Space", "대화·행동"), ("Tab", "상태·할 일"),
            ("I", "소지품"), ("M", "지도"), ("L", "장부"), ("Esc", "메뉴"),
        };

        void Update()
        {
            if (InputReader.StatusPressed() && !UIState.Blocking)
            {
                expandedUntil = Expanded ? -1f : Time.unscaledTime + ExpandSeconds;   // 펼친 채로 다시 누르면 접기
                if (Expanded) ExpandedFrame = Time.frameCount;
            }
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null) return;
            if (UIState.PhotoMode) return;   // 11/11 은샘: 기념 사진 찍는 동안 상태 카드·단추·아래 목표 줄을 감춘다
            UIStyle.Begin();
            GUI.depth = -2;   // 11/13 머리 위 이름표·놀거리 표(깊이 0 이상)보다 앞에 (왼쪽 위 카드 위로 이름표가 겹쳐 보였다)

            string roadName = "";
            var player = PlayerController.Instance;
            if (RoadGrid.Current != null && player != null)
            {
                int g = RoadGrid.Current.GradeAt(player.Motor.Cell);
                var names = gm.Db.roads.gradeNames;
                roadName = g < names.Length ? names[g] : g.ToString();
            }

            // 11/13 캐릭터가 상태 카드 뒤로 지나가면 옅게
            var oldColor = GUI.color;
            if (TaskCard.PlayerBehind(new Rect(4, 4, ColumnRight - 4, Expanded ? 150 : 42))) GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * .4f);
            float bottom = Expanded ? DrawExpanded(gm, roadName) : DrawCompact(gm);
            GUI.color = oldColor;
            bottom = TaskCard.DrawBadges(bottom + 4f);   // 11/13 작은 표 (푹 쉼 등)
            TaskCard.Draw(bottom + 4f);   // 11/13 할 일 카드: 상태 카드 바로 밑 (예전 위 가운데 알약들)
            DrawButtons();
            DrawBottom(gm);
        }

        /// <summary>평소: 초상 + 돈 · 체력 막대 (Tab 키캡)</summary>
        // ---------------- 11/11 윤채: 돈이 오갈 때 ----------------
        // 숫자는 0.4초쯤 걸려 새 값까지 올라가거나 내려가고, 동전이 한 번 통 튀고, 숫자가 잠깐 초록(+)·빨강(−)으로 물든다.
        // 바뀐 만큼은 "+35냥" 칩이 돈 칸 아래에서 떠올라 돈 칸으로 들어가며 사라진다
        static float shownMoney = float.NaN, moneyAt = -9f, moneyCalled = -9f;
        static int lastMoney = int.MinValue, lastDelta;
        struct MoneyFloat { public int delta; public float at; }
        static readonly List<MoneyFloat> moneyFloats = new List<MoneyFloat>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMoneyFx() { shownMoney = float.NaN; lastMoney = int.MinValue; moneyAt = -9f; moneyFloats.Clear(); }

        /// <summary>그릴 돈 (새 값으로 다가가는 중인 숫자). 그리기(Repaint) 때만 움직인다</summary>
        static int MoneyShown(int actual)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return float.IsNaN(shownMoney) ? actual : Mathf.RoundToInt(shownMoney);
            float now = Time.unscaledTime;
            // 상태 창이 한동안 안 그려졌으면(시작 화면 · 불러오기 · 맵 이동) 그 사이 바뀐 돈은 칩 없이 그대로 맞춘다
            bool fresh = now - moneyCalled > .5f;
            moneyCalled = now;
            if (fresh || lastMoney == int.MinValue || float.IsNaN(shownMoney)) { lastMoney = actual; shownMoney = actual; return actual; }
            if (actual != lastMoney)
            {
                lastDelta = actual - lastMoney; lastMoney = actual; moneyAt = now;
                moneyFloats.Add(new MoneyFloat { delta = lastDelta, at = now });
                if (moneyFloats.Count > 3) moneyFloats.RemoveAt(0);
            }
            float diff = actual - shownMoney;
            shownMoney = Mathf.Abs(diff) < .5f ? actual : shownMoney + diff * Mathf.Clamp01(Time.unscaledDeltaTime * 9f);
            return Mathf.RoundToInt(shownMoney);
        }

        static Color32 MoneyColor => Time.unscaledTime - moneyAt < .5f ? (lastDelta >= 0 ? CuteUI.Up : CuteUI.Down) : CuteUI.Text;

        /// <summary>동전 그림 (돈이 바뀐 직후 통 튄다)</summary>
        static void MoneyCoin(float x, float y)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - moneyAt) / .28f);
            var m = GUI.matrix;
            if (k < 1f) { float s = 1f + .45f * Mathf.Sin(k * Mathf.PI); GUIUtility.ScaleAroundPivot(new Vector2(s, s), new Vector2(x + 4.5f, y + 4.5f) * UIStyle.Scale); }
            CuteUI.Icon(CuteUI.Pic.Coin, x, y);
            GUI.matrix = m;
        }

        /// <summary>"+35냥" 칩: (x, y) 아래에서 떠올라 위로 들어가며 옅어진다 (1.1초)</summary>
        static void MoneyFloats(float x, float y)
        {
            float now = Time.unscaledTime;
            moneyFloats.RemoveAll(f => now - f.at > 1.1f);
            var old = GUI.color;
            foreach (var f in moneyFloats)
            {
                float t = (now - f.at) / 1.1f, ease = 1f - (1f - t) * (1f - t);
                string s = (f.delta > 0 ? "+" : "−") + Mathf.Abs(f.delta) + "냥";
                float w = Mathf.Round(CuteUI.Width(s, true, true) + 10f), cy = Mathf.Round(y + 18f - ease * 16f);
                GUI.color = new Color(old.r, old.g, old.b, old.a * Mathf.Clamp01((1f - t) / .35f));
                bool up = f.delta > 0;
                CuteUI.Box(new Rect(x, cy, w, 13f), 6, up ? CuteUI.SellSoft : CuteUI.AlertSoft, up ? CuteUI.Up : CuteUI.Down, 1, 1);
                CuteUI.LabelCenter(x + w / 2f, cy + 6.5f, s, up ? CuteUI.Up : CuteUI.Down, true, true);
            }
            GUI.color = old;
        }

        static float DrawCompact(GameManager gm)
        {
            var p = gm.Player;
            string money = MoneyShown(p.money).ToString();
            float tabW = CuteUI.KeycapWidth("Tab");
            float w = ColumnRight - 22f;   // 11/13 아래 할 일 카드와 같은 오른쪽 끝
            var card = new Rect(22, 5, w, 40);
            ScreenZones.Mark("hud.card", Rect.MinMaxRect(4, 4, card.xMax, card.yMax));   // 초상까지
            CuteUI.Card(card);
            if (!UIState.Blocking && GUI.Button(Rect.MinMaxRect(4, 4, card.xMax, card.yMax), GUIContent.none, GUIStyle.none)) InputReader.Press('\t');   // 11/08 눌러도 Tab처럼 펼치기
            MoneyCoin(42, 14);
            float x = CuteUI.Label(54, 18.5f, money, MoneyColor, false, true);
            CuteUI.Label(x + 3, 19, "냥", CuteUI.Sub, true);
            CuteUI.Keycap(card.xMax - 8 - tabW, 12, "Tab");
            CuteUI.Icon(CuteUI.Pic.Bolt, 40, 27);
            CuteUI.Label(51, 32, "체력", CuteUI.Sub, true);
            CuteUI.Bar(new Rect(75, 28, card.xMax - 8 - 75, 8), p.stamina, CuteUI.Stamina, CuteUI.StaminaHi);
            CuteUI.Portrait(4, 5, 36);
            MoneyFloats(54, card.yMax - 2f);
            return card.yMax;
        }

        /// <summary>Tab: 맵 · 장날 · 돈·신용 · 평판 · 짐 · 체력 · 발밑</summary>
        static float DrawExpanded(GameManager gm, string roadName)
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

            const float w = ColumnRight - 22f;   // 11/13 210 → 192 (아래 할 일 카드와 같은 오른쪽 끝)
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
            MoneyCoin(46, cy - 4);
            float mx = CuteUI.Label(58, cy, MoneyShown(p.money).ToString(), MoneyColor, false, true);
            MoneyFloats(mx + 22f, cy - 18f);
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
            return y + h;
        }

        /// <summary>오른쪽 위 단추: 자루(소지품)·나침반(지도)·펼친 책(장부)·메뉴. 아래에 중립 키캡</summary>
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
