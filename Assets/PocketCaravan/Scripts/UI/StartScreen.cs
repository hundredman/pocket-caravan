using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 게임을 켜면(빌드 첫 씬) 나오는 시작 화면. 저장이 있으면 "이어하기 · 새로 시작"을 먼저 묻고,
    /// 새로 시작하면 네 문명 카드(출발 마을 그림 + 주인공)에서 골라 그 출발 마을로 간다.
    /// GameManager가 첫 씬을 붙잡아 두고 Open을 부른다. 다른 씬에서 바로 Play한 개발 시험은 그대로.
    /// </summary>
    public class StartScreen : MonoBehaviour
    {
        enum Mode { Closed, Menu, Pick }
        static StartScreen inst;
        Mode mode = Mode.Closed;
        bool hasSave;
        int menuIndex, pick;
        bool confirmNew; int confirmIndex;   // 11/18 새로 시작 확인창 (0 새로 시작 · 1 취소)
        Vector2 lastMove;
        Texture2D[] previews;
        Vector2 pickScroll, lastPickViewSize;
        int visiblePick = -1;
        Rect pickViewport, pickFooter;
        readonly System.Collections.Generic.List<Rect> pickRects = new System.Collections.Generic.List<Rect>();
        static int PickColumns(float width) => width >= 444f ? 2 : 1;

        public static bool IsOpen => inst != null && inst.mode != Mode.Closed;
        /// <summary>true인 동안 시작 화면이 키를 받지 않는다. 그 위에 다른 화면(온라인 로그인 NetLogin)이 먼저 떠 있을 때 쓴다 (2026-10-31)</summary>
        public static bool Held;
        public static bool Available { get { var c = StartChoices.Get(); return c != null && c.choices.Count > 0; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("StartScreen");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<StartScreen>();
        }

        public static void Open(bool saveExists)
        {
            if (inst == null) Boot();
            UIState.Reset();
            inst.hasSave = saveExists;
            inst.menuIndex = 0; inst.confirmNew = false;
            inst.pick = 0; inst.pickScroll = Vector2.zero; inst.visiblePick = -1;
            inst.mode = saveExists ? Mode.Menu : Mode.Pick;
            inst.lastMove = Vector2.one;   // 첫 프레임에 눌려 있던 키는 무시
            UIState.Blocking = true;
        }

        void Update()
        {
            if (mode == Mode.Closed || GameManager.I == null) return;
            UIState.Blocking = true;
            if (Held) { lastMove = Vector2.one; return; }   // 위에 다른 화면이 떠 있다. 돌아올 때 눌려 있던 키는 무시
            var choices = StartChoices.Get();
            Vector2 mv = InputReader.MoveVector();
            Vector2 step = Vector2.zero;
            if (mv.sqrMagnitude > 0.25f && lastMove.sqrMagnitude <= 0.25f)
                step = Mathf.Abs(mv.x) > Mathf.Abs(mv.y) ? new Vector2(Mathf.Sign(mv.x), 0) : new Vector2(0, Mathf.Sign(mv.y));
            lastMove = mv;

            if (mode == Mode.Menu && confirmNew)
            {
                // 11/18 "지금 기록을 덮어쓰고 새로 시작할까요?" ← → 고르기 · Space 결정 · Esc 취소 (처음엔 취소에)
                if (step.x != 0 || step.y != 0) { confirmIndex = 1 - confirmIndex; Sfx.Play("open"); }
                if (InputReader.CancelPressed()) { confirmNew = false; Sfx.Play("cancel"); return; }
                if (InputReader.ConfirmPressed())
                {
                    confirmNew = false;
                    if (confirmIndex == 0) { Sfx.Play("accept"); mode = Mode.Pick; pick = 0; pickScroll = Vector2.zero; visiblePick = -1; }
                    else Sfx.Play("cancel");
                }
                return;
            }
            if (mode == Mode.Menu)
            {
                if (step.y != 0) { menuIndex = 1 - menuIndex; Sfx.Play("open"); }
                if (InputReader.ConfirmPressed())
                {
                    if (menuIndex == 0) { Sfx.Play("accept"); mode = Mode.Closed; GameManager.I.ContinueFromStartScreen(); }
                    else { Sfx.Play("open"); confirmNew = true; confirmIndex = 1; }   // 11/18 바로 넘어가지 않고 한 번 묻는다
                }
                return;
            }

            int n = choices != null ? choices.choices.Count : 0;
            if (n == 0) { mode = Mode.Closed; GameManager.I.ContinueFromStartScreen(); return; }
            // 평소 2열, 좁은 창 1열. 선택된 카드는 DrawPick에서 보이는 범위로 스크롤한다.
            int columns = PickColumns(UIStyle.VirtualWidth);
            if (step.x != 0) { int col = pick % columns, row = pick / columns; col = Mathf.Clamp(col + (int)step.x, 0, columns - 1); int p = row * columns + col; if (p < n && p != pick) { pick = p; Sfx.Play("open"); } }
            if (step.y != 0) { int p = pick - columns * (int)step.y; if (p >= 0 && p < n) { pick = p; Sfx.Play("open"); } }
            if (hasSave && InputReader.CancelPressed()) { mode = Mode.Menu; Sfx.Play("cancel"); return; }
            if (InputReader.ConfirmPressed())
            {
                var c = choices.choices[pick];
                if (c.origin == null || string.IsNullOrEmpty(c.startScene)) { Sfx.Play("cancel"); return; }
                Sfx.Play("accept");
                mode = Mode.Closed;
                GameManager.I.BeginNewGame(c);
            }
        }

        // ---------- 그리기 (가상 해상도 640×360 기준) ----------
        // 11/05 UI B안: 종이 바둑판 바탕 + 주황 머리띠(CuteUI.Backdrop), 메뉴는 종이 창, 문명 카드는 테 두른 카드.
        // 고른 것은 청록(상호작용). 고르는 법·출발 규칙은 그대로.

        void OnGUI()
        {
            if (mode == Mode.Closed) return;
            UIStyle.Begin();
            GUI.depth = -500;   // HUD·대화창 위, 맵 이동 페이드(-1000) 아래
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            var net = NetClient.I;
            bool online = net != null && net.Status == NetClient.State.Online;
            CuteUI.Backdrop(null, false);
            if (mode == Mode.Menu)
            {
                // 11/18 이어하기: 왼쪽 상점 장면(로고) + 오른쪽 상인 수첩. 접속 중이면 오른쪽 위 작은 표시
                if (online)
                {
                    string chip = $"온라인 · {net.MyName}";
                    float cwid = CuteUI.Width(chip, true, true) + 22;
                    var cr = new Rect(W - 12 - cwid, 10, cwid, 16);
                    CuteUI.Box(cr, 3, TitleUI.Cream, TitleUI.Choco, 1, 2, TitleUI.ChocoShadow);
                    CuteUI.Fill(new Rect(cr.x + 6, cr.center.y - 2, 4, 4), CuteUI.Up);
                    CuteUI.Label(cr.x + 14, cr.center.y, chip, TitleUI.Choco, true, true);
                }
                DrawMenu(W, H);
                return;
            }
            // 11/18 상인 고르기: 낮고 덜 꾸민 코랄 띠 안에 질문 (카드보다 덜 눈에 띄게)
            var bar = new Rect(16, 8, W - 32, 22);
            CuteUI.Box(bar, 3, TitleUI.Coral, TitleUI.Choco, 1, 2, TitleUI.ChocoShadow);
            CuteUI.Label(bar.x + 10, bar.center.y, "바리바리", CuteUI.White, true, true);
            CuteUI.LabelCenter(bar.center.x, bar.center.y + 1, "어느 문명의 상인으로 떠날까?", TitleUI.Choco, false, true);
            CuteUI.LabelCenter(bar.center.x, bar.center.y, "어느 문명의 상인으로 떠날까?", CuteUI.White, false, true);
            if (online) CuteUI.LabelRight(bar.xMax - 10, bar.center.y, $"온라인 · {net.MyName}", CuteUI.White, true);
            DrawPick(W, H);
        }

        /// <summary>
        /// 11/18 이어하기 (디자인 의견 "아기자기한 여행 상점"): 왼쪽은 저장된 상인이 그 지역 특산품을 올린 좌판 옆에서 맞이하는 장면
        /// (팻말 = 마지막에 있던 곳), 오른쪽은 "내 상인" 수첩 — 이름 옆 문명 배지 · 두 칸(보유 금액 · 플레이) · 작은 상태 문구 ·
        /// 이어하기(청록 = 주요 행동) · 새로 시작(크림 보조). 키로 고른 단추는 모서리 꺾쇠 + ▶. 새로 시작은 한 번 묻는다
        /// </summary>
        void DrawMenu(float W, float H)
        {
            var save = GameManager.I != null ? GameManager.I.SaveToContinue : null;
            var all = StartChoices.Get();
            StartChoices.Choice mine = null;
            if (save != null && all != null) mine = all.choices.Find(c => c.origin != null && c.origin.id == save.origin && PlayerAppearance.HasFrames(c.origin));
            var map = save != null ? WorldIndex.Get()?.Map(save.scene) : null;
            string place = map != null && !string.IsNullOrEmpty(map.displayName) ? map.displayName : null;
            if (mine != null) TitleUI.Scene(W, H, mine, place, "오늘도 장사하러 가요!", "지난 장사를 이어서 할까요?");
            else TitleUI.Scene(W, H, null, null, null, "지난 장사를 이어서 할까요?");

            var p = TitleUI.MenuPanel(W, H, 222);
            TitleUI.Panel(p, "내 상인");
            float x = p.x + 20, y = p.y + 18, iw = p.width - 40;
            string name = mine != null && !string.IsNullOrEmpty(mine.animal) ? mine.animal : "지난 상인";
            var m = GUI.matrix;
            CuteUI.ScaleAround(2f, new Vector2(x, y));
            CuteUI.Label(x, y + 7, name, TitleUI.Choco);
            GUI.matrix = m;
            if (mine != null)
            {
                string tag = $"{mine.region} 상인";
                float tw = CuteUI.Width(tag, true, true) + 14;
                var tr = new Rect(Mathf.Round(x + CuteUI.Width(name) * 2f + 10), y + 7, tw, 15);
                CuteUI.Box(tr, 3, TitleUI.Coral, TitleUI.Choco, 1);
                CuteUI.LabelCenter(tr.center.x, tr.center.y, tag, CuteUI.White, true, true);
            }
            float cellW = Mathf.Floor((iw - 6) / 2f);
            var c1 = new Rect(x, y + 34, cellW, 32);
            var c2 = new Rect(x + cellW + 6, y + 34, cellW, 32);
            foreach (var (cell, cap, val) in new[] { (c1, "보유 금액", save != null ? $"{save.money:N0}냥" : "-"), (c2, "플레이", save != null && save.playHours > 0f ? PlayTime(save.playHours).Replace(" 플레이", "") : "-") })
            {
                CuteUI.Box(cell, 3, TitleUI.Ivory, TitleUI.HoneyLo, 1);
                CuteUI.Label(cell.x + 7, cell.y + 9, cap, TitleUI.ChocoSoft, true);
                CuteUI.Label(cell.x + 7, cell.y + 22, val, TitleUI.Choco, false, true);
            }
            if (place != null)
            {
                CuteUI.Fill(new Rect(x + 1, y + 77, 4, 4), CuteUI.Up);
                CuteUI.Label(x + 10, y + 79, $"{place}에서 쉬는 중", TitleUI.ChocoSoft, true);
            }
            CuteUI.Fill(new Rect(x, y + 92, iw, 1), TitleUI.HoneyHi);

            bool canClick = !Held && !confirmNew;
            var go = new Rect(x + 8, y + 104, iw - 16, 32);
            if (TitleUI.Button(go, "이어하기", true, menuIndex == 0, false, canClick)) { menuIndex = 0; InputReader.Press(' '); }
            var nr = new Rect(x + 28, go.yMax + 14, iw - 56, 24);
            if (TitleUI.Button(nr, "새로 시작", false, menuIndex == 1, true, canClick)) { menuIndex = 1; InputReader.Press(' '); }
            CuteUI.LabelCenter(p.center.x, nr.yMax + 15, "↑ ↓ 고르기 · Space 결정", TitleUI.ChocoSoft, true);

            if (confirmNew) DrawConfirmNew(W, H, save);
        }

        /// <summary>11/18 새로 시작 확인창 (기록을 덮어쓰기 전에 한 번 묻는다. 처음엔 취소에 · 취소 = 청록)</summary>
        void DrawConfirmNew(float W, float H, SaveData save)
        {
            CuteUI.Fill(new Rect(0, 0, W, H), new Color32(0x5B, 0x3A, 0x29, 90));
            var r = new Rect(Mathf.Round(W / 2 - 142), Mathf.Round(H / 2 - 56), 284, 112);
            TitleUI.Panel(r, "새로 시작");
            CuteUI.LabelCenter(r.center.x, r.y + 28, "지금 기록을 덮어쓰고 새로 시작할까요?", TitleUI.Choco, false, true);
            string detail = save != null ? $"{save.money:N0}냥{(save.playHours > 0f ? " · " + PlayTime(save.playHours) : "")} 기록이 지워져요" : "지난 기록이 지워져요";
            CuteUI.LabelCenter(r.center.x, r.y + 46, detail, CuteUI.Alert, true);
            var yes = new Rect(r.x + 26, r.yMax - 44, 106, 28);
            var no = new Rect(r.xMax - 26 - 106, r.yMax - 44, 106, 28);
            if (TitleUI.Button(yes, "새로 시작", false, confirmIndex == 0, false, !Held)) { confirmIndex = 0; InputReader.Press(' '); }
            if (TitleUI.Button(no, "취소", true, confirmIndex == 1, false, !Held)) { confirmIndex = 1; InputReader.Press(' '); }
        }

        static string PlayTime(float hours)
        {
            int min = Mathf.Max(1, Mathf.RoundToInt(hours * 60f));
            return min < 60 ? $"{min}분 플레이" : $"{min / 60}시간 플레이";   // 11/18 분까지는 빼서 짧게
        }

        int shownPick = -1; float pickAt = -9f;

        void DrawPick(float W, float H)
        {
            // 11/18 상인 고르기 (2×2 카드 + 아래 설명·출발은 그대로, 디자인 의견 반영):
            //   카드 = 크림 카드 · 초콜릿 테, 출발 마을 지도에 옅은 막 + 앞에 상인 크게(3배, 진한 테두리로 지도와 분리, 네 상인 같은 크기).
            //   고른 카드만 청록 테 + "선택됨" + 고를 때 한 번 폴짝 (카드 크기·배열은 그대로)
            var all = StartChoices.Get();
            if (previews == null || previews.Length != all.choices.Count)
            {
                previews = new Texture2D[all.choices.Count];
                for (int i = 0; i < previews.Length; i++)
                    previews[i] = string.IsNullOrEmpty(all.choices[i].preview) ? null : Resources.Load<Texture2D>(all.choices[i].preview);
            }
            var sel = all.choices[Mathf.Clamp(pick, 0, all.choices.Count - 1)];
            if (pick != shownPick) { shownPick = pick; pickAt = Time.unscaledTime; }

            const float cw = 196, ch = 106, gap = 16, rowGap = 12;
            int columns = PickColumns(W), count = Mathf.Min(4, all.choices.Count);
            int rows = Mathf.CeilToInt(count / (float)columns);
            float gridW = columns * cw + (columns - 1) * gap;
            float contentHeight = rows * ch + Mathf.Max(0, rows - 1) * rowGap + 8;
            float fw = Mathf.Min(W - 24, Mathf.Max(gridW, 380));
            pickFooter = new Rect(Mathf.Round((W - fw) / 2), H - 26 - 44, fw, 44);
            pickViewport = new Rect(8, 40, W - 16, Mathf.Max(1, pickFooter.y - 8 - 40));
            float contentWidth = pickViewport.width - (contentHeight > pickViewport.height ? 18 : 0);
            float x0 = Mathf.Round((contentWidth - gridW) / 2), y0 = contentHeight < pickViewport.height ? Mathf.Round((pickViewport.height - contentHeight) / 2) + 4 : 4;
            if (visiblePick != pick || lastPickViewSize != pickViewport.size)
            {
                float top = (pick / columns) * (ch + rowGap), bottom = top + ch + 8;
                if (top < pickScroll.y) pickScroll.y = top;
                else if (bottom > pickScroll.y + pickViewport.height) pickScroll.y = bottom - pickViewport.height;
                pickScroll.y = Mathf.Clamp(pickScroll.y, 0, Mathf.Max(0, contentHeight - pickViewport.height));
                visiblePick = pick; lastPickViewSize = pickViewport.size;
            }
            pickScroll.x = 0; pickScroll.y = Mathf.Round(pickScroll.y);
            pickRects.Clear();
            pickScroll = GUI.BeginScrollView(pickViewport, pickScroll, new Rect(0, 0, contentWidth, Mathf.Max(contentHeight, y0 + contentHeight - 4)));
            for (int i = 0; i < all.choices.Count && i < 4; i++)
            {
                var c = all.choices[i];
                var r = new Rect(x0 + (i % columns) * (cw + gap), y0 + (i / columns) * (ch + rowGap), cw, ch);
                pickRects.Add(r);
                bool on = i == pick, hover = !on && r.Contains(Event.current.mousePosition);
                TitleUI.Card(r);
                var inner = new Rect(r.x + 5, r.y + 5, r.width - 10, r.height - 10);
                var mapR = new Rect(inner.x, inner.y, inner.width, inner.height - 20);
                if (previews[i] != null) GUI.DrawTexture(mapR, previews[i], ScaleMode.ScaleAndCrop);
                else CuteUI.Fill(mapR, TitleUI.CreamDeep);
                CuteUI.Fill(mapR, new Color32(0xFF, 0xF6, 0xEA, (byte)(on ? 120 : hover ? 135 : 150)));   // 지도에만 옅은 단색 막 (상인은 밝게 그대로)
                if (PlayerAppearance.HasFrames(c.origin))
                {
                    var s = c.origin.playerFrames[0];
                    float w = Mathf.Round(s.rect.width / s.pixelsPerUnit * 48f), h = Mathf.Round(s.rect.height / s.pixelsPerUnit * 48f);
                    float hk = (Time.unscaledTime - pickAt) / .36f;
                    float hop = on && hk >= 0f && hk < 1f ? Mathf.Round(Mathf.Sin(hk * Mathf.PI) * 2f) * 2f : 0f;   // 고를 때 한 번 폴짝
                    float feet = mapR.yMax - 3;
                    CuteUI.Box(new Rect(mapR.center.x - 18, feet - 4, 36, 8), 4, TitleUI.ChocoShadow, TitleUI.ChocoShadow, 0);
                    var body = new Rect(Mathf.Round(mapR.center.x - w / 2f), feet - h - hop, w, h);
                    // 진한 테두리: 같은 그림을 초콜릿색으로 사방 2칸씩 먼저 (뿔·모자가 지도에 묻히지 않게)
                    var oldColor = GUI.color;
                    GUI.color = new Color(.36f, .23f, .16f, oldColor.a);
                    foreach (var o in new[] { new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, -2), new Vector2(0, 2) })
                        CuteUI.Sprite(s, new Rect(body.x + o.x, body.y + o.y, w, h));
                    GUI.color = oldColor;
                    CuteUI.Sprite(s, body);
                }
                CuteUI.Box(new Rect(mapR.x - 1, mapR.y - 1, mapR.width + 2, mapR.height + 2), 2, new Color32(0, 0, 0, 0), TitleUI.Choco);
                // 아래 줄: 이름 / 문명 · 출발 마을
                var strip = new Rect(inner.x, mapR.yMax + 1, inner.width, 19);
                CuteUI.Label(strip.x + 6, strip.center.y, string.IsNullOrEmpty(c.animal) ? c.region : c.animal, TitleUI.Choco, false, true);
                string where = string.IsNullOrEmpty(c.startTown) ? c.region : $"{c.region} · {c.startTown}";
                CuteUI.LabelRight(strip.xMax - 6, strip.center.y, where, TitleUI.ChocoSoft, true);
                if (on)
                {
                    CuteUI.Box(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), 6, new Color32(0, 0, 0, 0), TitleUI.Teal, 2);
                    const string tag = "선택됨";
                    float tw = CuteUI.Width(tag, true, true) + 12;
                    var tr = new Rect(mapR.xMax - tw - 4, mapR.y + 4, tw, 14);
                    CuteUI.Box(tr, 3, TitleUI.Teal, TitleUI.Choco, 1);
                    CuteUI.LabelCenter(tr.center.x, tr.center.y, tag, CuteUI.White, true, true);
                }
                // 11/08 카드를 누르면 고르고, 고른 카드를 한 번 더 누르면 출발 (위에 접속 창이 떠 있으면 무시)
                if (!Held && CuteUI.Hit(r, GUIContent.none, GUIStyle.none))
                {
                    if (on) InputReader.Press(' ');
                    else { pick = i; visiblePick = i; Sfx.Play("open"); }
                }
            }
            GUI.EndScrollView();

            // 아래 설명칸: 고른 상인 이름 · 문명 · 강점 + 오른쪽 출발(·뒤로)
            TitleUI.Card(pickFooter);
            var go = new Rect(pickFooter.xMax - 12 - 88, pickFooter.y + 6, 88, 28);
            if (TitleUI.Button(go, "출발", true, false, false, !Held)) InputReader.Press(' ');
            float textRight = go.x - 10;
            if (hasSave)
            {
                var back = new Rect(go.x - 10 - 56, go.y + 3, 56, 22);
                if (TitleUI.Button(back, "뒤로", false, false, true, !Held)) InputReader.Press('\x1b');
                textRight = back.x - 10;
            }
            float fx = pickFooter.x + 12;
            string nm = string.IsNullOrEmpty(sel.animal) ? sel.region : sel.animal;
            float nx = CuteUI.Label(fx, pickFooter.y + 14, nm, TitleUI.Choco, false, true);
            string sub = string.IsNullOrEmpty(sel.startTown) ? $"{sel.region} 상인" : $"{sel.region} 상인 · {sel.startTown}에서 출발";
            CuteUI.Label(nx + 8, pickFooter.y + 14.5f, TradeLedgerUI.Fit(sub, textRight - nx - 8, true, false), TitleUI.ChocoSoft, true);
            float sx = CuteUI.Label(fx, pickFooter.y + 31, "강점", TitleUI.Teal, true, true);
            CuteUI.Label(sx + 6, pickFooter.y + 31, TradeLedgerUI.Fit(sel.strength, textRight - sx - 6, true, false), TitleUI.Choco, true);

            // 맨 아래 조작 한 줄
            CuteUI.LabelCenter(W / 2, H - 13, "방향키 / 클릭으로 선택 · Space 출발" + (hasSave ? " · Esc 뒤로" : ""), TitleUI.ChocoSoft, true);
        }

    }
}
