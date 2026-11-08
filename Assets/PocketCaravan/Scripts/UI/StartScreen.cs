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
            inst.menuIndex = 0;
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

            if (mode == Mode.Menu)
            {
                if (step.y != 0) { menuIndex = 1 - menuIndex; Sfx.Play("open"); }
                if (InputReader.ConfirmPressed())
                {
                    Sfx.Play("accept");
                    if (menuIndex == 0) { mode = Mode.Closed; GameManager.I.ContinueFromStartScreen(); }
                    else { mode = Mode.Pick; pick = 0; pickScroll = Vector2.zero; visiblePick = -1; }
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
            CuteUI.Backdrop();
            if (mode == Mode.Menu) { DrawMenu(W, H); return; }
            DrawPick(W, H);
        }

        void DrawMenu(float W, float H)
        {
            var box = new Rect(Mathf.Round((W - 240) / 2), 128, 240, 112);
            CuteUI.Window(box);
            CuteUI.TitlePlate(box.center.x, box.y - 11, "바리바리");
            string[] items = { "이어하기", "새로 시작 (지난 저장 덮어씀)" };
            for (int i = 0; i < 2; i++)
            {
                var r = new Rect(box.x + 18, box.y + 22 + i * 38, box.width - 36, 28);
                bool on = i == menuIndex;
                CuteUI.Box(r, 8, on ? CuteUI.Interact : CuteUI.Paper, on ? CuteUI.Ink : CuteUI.Line, 1, on ? 2 : 0, CuteUI.InteractLo);
                if (on) CuteUI.Fill(new Rect(r.x + 5, r.y + 2, r.width - 10, 1), CuteUI.InteractHi);
                CuteUI.LabelCenter(r.center.x, r.center.y, items[i], on ? CuteUI.OnInteract : CuteUI.Text, false, on);
                // 11/08 마우스로 눌러도 고르고 결정 (위에 접속 창이 떠 있으면 무시)
                if (!Held && GUI.Button(r, GUIContent.none, GUIStyle.none)) { menuIndex = i; InputReader.Press(' '); }
            }
            CuteUI.LabelCenter(W / 2, H - 20, "↑ ↓ 고르기 · Space 결정 · 마우스로 눌러도 돼요", CuteUI.Sub, true);
        }

        void DrawPick(float W, float H)
        {
            var all = StartChoices.Get();
            if (previews == null || previews.Length != all.choices.Count)
            {
                previews = new Texture2D[all.choices.Count];
                for (int i = 0; i < previews.Length; i++)
                    previews[i] = string.IsNullOrEmpty(all.choices[i].preview) ? null : Resources.Load<Texture2D>(all.choices[i].preview);
            }

            var sel = all.choices[Mathf.Clamp(pick, 0, all.choices.Count - 1)];
            string who = sel.origin != null ? sel.origin.displayName : "";
            string intro = $"{sel.region} · {who} ({sel.animal}) · 강점: {sel.strength}";
            string hint = $"{sel.startTown}에서 출발 · ← → ↑ ↓ 또는 카드를 눌러 고르기 · Space·출발 단추로 떠나기" + (hasSave ? " · Esc 뒤로" : "");
            float bw = Mathf.Min(W - 24, Mathf.Max(430, CuteUI.Width(intro) + 30));
            float introHeight = CuteUI.WrapHeight(intro, bw - 20);
            float hintHeight = CuteUI.WrapHeight(hint, bw - 20, true);
            float footerHeight = Mathf.Max(32, introHeight + hintHeight + 8);
            pickFooter = new Rect(Mathf.Round((W - bw) / 2), H - footerHeight - 8, bw, footerHeight);

            CuteUI.LabelCenter(W / 2, 56, "어느 문명의 상인으로 떠날까?", CuteUI.Text, false, true);
            const float cw = 190, ch = 107, gap = 22, rowGap = 14;
            int columns = PickColumns(W), count = Mathf.Min(4, all.choices.Count);
            int rows = Mathf.CeilToInt(count / (float)columns);
            float contentHeight = rows * ch + Mathf.Max(0, rows - 1) * rowGap + 8;
            pickViewport = new Rect(8, 72, W - 16, Mathf.Max(1, pickFooter.y - 4 - 72));
            float contentWidth = pickViewport.width - (contentHeight > pickViewport.height ? 18 : 0);
            float x0 = Mathf.Round((contentWidth - (columns * cw + (columns - 1) * gap)) / 2), y0 = 4;
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
            pickScroll = GUI.BeginScrollView(pickViewport, pickScroll, new Rect(0, 0, contentWidth, contentHeight));
            for (int i = 0; i < all.choices.Count && i < 4; i++)
            {
                var c = all.choices[i];
                var r = new Rect(x0 + (i % columns) * (cw + gap), y0 + (i / columns) * (ch + rowGap), cw, ch);
                pickRects.Add(r);
                bool on = i == pick;
                if (on) CuteUI.Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), 10, CuteUI.Interact, CuteUI.Ink, 1, 2, CuteUI.InteractLo);
                CuteUI.Card(r);
                // 출발 마을 그림
                var mapR = new Rect(r.x + 7, r.y + 7, 122, 93);
                if (previews[i] != null) GUI.DrawTexture(mapR, previews[i], ScaleMode.ScaleAndCrop);
                else CuteUI.Fill(mapR, CuteUI.Paper2);
                CuteUI.Box(new Rect(mapR.x - 1, mapR.y - 1, mapR.width + 2, mapR.height + 2), 2, new Color32(0, 0, 0, 0), CuteUI.Ink);
                // 주인공
                var charR = new Rect(mapR.xMax + 4, r.y + 7, r.xMax - 7 - (mapR.xMax + 4), 93);
                CuteUI.Box(charR, 5, CuteUI.Paper2, CuteUI.Line);
                DrawHero(c.origin, new Rect(charR.x, charR.y + 8, charR.width, 62));
                CuteUI.LabelCenter(charR.center.x, charR.yMax - 11, c.animal, CuteUI.Sub, true);
                // 이름표
                string name = string.IsNullOrEmpty(c.startTown) ? c.region : $"{c.region} · {c.startTown}";
                float tw = Mathf.Min(mapR.width - 8, CuteUI.Width(name, false, true) + 14);
                var tag = new Rect(mapR.x + 4, mapR.yMax - 22, tw, 17);
                CuteUI.Box(tag, 7, on ? CuteUI.Interact : CuteUI.Paper, CuteUI.Ink);
                CuteUI.LabelCenter(tag.center.x, tag.center.y, name, on ? CuteUI.OnInteract : CuteUI.Text, false, true);
                // 11/08 카드를 누르면 고르고, 고른 카드를 한 번 더 누르면 출발 (위에 접속 창이 떠 있으면 무시)
                if (!Held && GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    if (on) InputReader.Press(' ');
                    else { pick = i; visiblePick = i; Sfx.Play("open"); }
                }
            }

            GUI.EndScrollView();

            // 11/08 출발·뒤로 단추 (마우스만으로도). 카드 칸을 줄이면 스크롤이 생겨서, 질문 줄 양 끝(카드 왼쪽·오른쪽 끝에 맞춰)에 둔다
            if (!Held)
            {
                float gridL = pickViewport.x + x0, gridR = gridL + columns * cw + (columns - 1) * gap;
                if (gridR - gridL < 360) { gridL = 12; gridR = W - 12; }
                if (CuteUI.Button(new Rect(gridR - 96, 46, 96, 20), "출발 (Space)")) InputReader.Press(' ');
                if (hasSave && CuteUI.SoftButton(new Rect(gridL, 46, 80, 20), "뒤로 (Esc)")) InputReader.Press('\x1b');
            }

            // 목록과 분리한 고정 소개. 긴 강점/키 안내도 창 안에서 줄바꿈한다.
            CuteUI.Box(pickFooter, 9, CuteUI.Paper, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            CuteUI.WrapCenter(pickFooter.x + 10, pickFooter.y + 4, bw - 20, intro, CuteUI.Text);
            CuteUI.Wrap(pickFooter.x + 10, pickFooter.y + 4 + introHeight, bw - 20, hint, CuteUI.Sub, true);

        }

        static void DrawHero(OriginData origin, Rect area)
        {
            if (!PlayerAppearance.HasFrames(origin)) return;
            var s = origin.playerFrames[0];
            var tr = s.textureRect;
            // 같은 게임 속 키(유닛)로 맞춤: 16px@16 과 32px@32 가 같은 크기로 보이게
            float unitsW = tr.width / s.pixelsPerUnit, unitsH = tr.height / s.pixelsPerUnit;
            float k = Mathf.Min(area.width / unitsW, area.height / unitsH);
            k = Mathf.Floor(k / 16f) * 16f; if (k < 16f) k = 16f;   // 16px 그림 기준 정수 배율
            float w = unitsW * k, h = unitsH * k;
            var dst = new Rect(area.x + (area.width - w) / 2, area.yMax - h, w, h);
            var tex = s.texture;
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(dst, tex, uv);
        }
    }
}
