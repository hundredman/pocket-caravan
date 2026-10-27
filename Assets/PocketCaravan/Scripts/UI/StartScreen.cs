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

        public static bool IsOpen => inst != null && inst.mode != Mode.Closed;
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
            inst.hasSave = saveExists;
            inst.menuIndex = 0;
            inst.pick = 0;
            inst.mode = saveExists ? Mode.Menu : Mode.Pick;
            inst.lastMove = Vector2.one;   // 첫 프레임에 눌려 있던 키는 무시
            UIState.Blocking = true;
        }

        void Update()
        {
            if (mode == Mode.Closed || GameManager.I == null) return;
            UIState.Blocking = true;
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
                    else { mode = Mode.Pick; pick = 0; }
                }
                return;
            }

            int n = choices != null ? choices.choices.Count : 0;
            if (n == 0) { mode = Mode.Closed; GameManager.I.ContinueFromStartScreen(); return; }
            // 2열 격자: 좌우는 같은 줄, 위아래는 두 칸씩
            if (step.x != 0) { int col = pick % 2, row = pick / 2; col = Mathf.Clamp(col + (int)step.x, 0, 1); int p = row * 2 + col; if (p < n && p != pick) { pick = p; Sfx.Play("open"); } }
            if (step.y != 0) { int p = pick - 2 * (int)step.y; if (p >= 0 && p < n) { pick = p; Sfx.Play("open"); } }
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
        static readonly Color32 Bg = new Color32(0xEF, 0xE6, 0xCF, 0xFF);
        static readonly Color32 BgCheck = new Color32(0xE8, 0xDE, 0xC4, 0xFF);
        static readonly Color32 Green = new Color32(0x3A, 0x71, 0x55, 0xFF);
        static readonly Color32 Brown = new Color32(0x4A, 0x35, 0x24, 0xFF);
        static readonly Color32 BrownLight = new Color32(0x8A, 0x6A, 0x46, 0xFF);
        static readonly Color32 Title = new Color32(0x6B, 0x4A, 0x2B, 0xFF);
        static readonly Color32 Shadow = new Color32(0xD6, 0xC8, 0xA6, 0xFF);
        static readonly Color32 CharBg = new Color32(0xF6, 0xEF, 0xDC, 0xFF);
        static readonly Color32 Pick = new Color32(0xE0, 0x57, 0x3A, 0xFF);
        static readonly Color32 White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        static readonly Color32 Cream = new Color32(0xFB, 0xF5, 0xE6, 0xFF);

        GUIStyle text, textSmall, textBig;

        static void Fill(Rect r, Color32 c)
        {
            var old = GUI.color; GUI.color = UIStyle.Lin(c);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Frame(Rect r, Color32 c, float t)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c); Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c); Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        GUIStyle Style(ref GUIStyle s, Font f, int size, Color32 c, TextAnchor a)
        {
            if (s == null) s = new GUIStyle(GUI.skin.label) { font = f, fontSize = size, alignment = a, clipping = TextClipping.Overflow, wordWrap = false };
            s.normal.textColor = UIStyle.Lin(c);
            return s;
        }

        void Label(Rect r, string s, Color32 c, bool small = false, TextAnchor a = TextAnchor.MiddleCenter)
        {
            var st = small ? Style(ref textSmall, UIStyle.SmallFont, 9, c, a) : Style(ref text, GUI.skin.font, 11, c, a);
            st.alignment = a;
            GUI.Label(r, s, st);
        }

        void OnGUI()
        {
            if (mode == Mode.Closed) return;
            UIStyle.Begin();
            GUI.depth = -500;   // HUD·대화창 위, 맵 이동 페이드(-1000) 아래
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            Fill(new Rect(0, 0, W, H), Bg);
            for (int y = 0; y < H; y += 20)
                for (int x = 0; x < W; x += 20)
                    if (((x / 20) + (y / 20)) % 2 == 1) Fill(new Rect(x, y, 20, 20), BgCheck);

            // 머리띠
            var head = new Rect(20, 12, W - 40, 34);
            Fill(head, Green);
            Label(new Rect(head.x + 14, head.y, 200, head.height), "Pocket Caravan", White, false, TextAnchor.MiddleLeft);

            if (mode == Mode.Menu) { DrawMenu(W, H); return; }
            DrawPick(W, H);
        }

        void DrawMenu(float W, float H)
        {
            var box = new Rect((W - 220) / 2, 130, 220, 110);
            Fill(new Rect(box.x + 3, box.y + 4, box.width, box.height), Shadow);
            Fill(box, White);
            string[] items = { "이어하기", "새로 시작 (지난 저장 덮어씀)" };
            for (int i = 0; i < 2; i++)
            {
                var r = new Rect(box.x + 16, box.y + 18 + i * 40, box.width - 32, 30);
                Fill(r, i == menuIndex ? Cream : White);
                if (i == menuIndex) { Frame(r, Pick, 2); Label(new Rect(r.x - 14, r.y, 12, r.height), "▶", Pick); }
                Label(r, items[i], Brown);
            }
            Label(new Rect(0, H - 26, W, 14), "↑ ↓ 고르기 · Space 결정", BrownLight, true);
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

            Label(new Rect(0, 52, W, 26), "어느 문명의 상인으로 떠날까?", Title);
            const float cw = 190, ch = 107, gap = 20;
            float x0 = (W - (cw * 2 + gap)) / 2, y0 = 83;
            for (int i = 0; i < all.choices.Count && i < 4; i++)
            {
                var c = all.choices[i];
                var r = new Rect(x0 + (i % 2) * (cw + gap), y0 + (i / 2) * (ch + 13), cw, ch);
                Fill(new Rect(r.x + 3, r.y + 4, r.width, r.height), Shadow);
                Fill(r, White);
                if (i == pick)
                {
                    Frame(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), Pick, 3);
                    Label(new Rect(r.x - 18, r.y, 12, r.height), "▶", Pick);
                }
                // 출발 마을 그림
                var mapR = new Rect(r.x + 6, r.y + 6, 125, 95);
                if (previews[i] != null) GUI.DrawTexture(mapR, previews[i], ScaleMode.ScaleAndCrop);
                else Fill(mapR, CharBg);
                // 주인공
                var charR = new Rect(mapR.xMax + 4, r.y + 6, r.xMax - 6 - (mapR.xMax + 4), 95);
                Fill(charR, CharBg);
                DrawHero(c.origin, new Rect(charR.x, charR.y + 8, charR.width, 62));
                Label(new Rect(charR.x, charR.yMax - 18, charR.width, 14), c.animal, BrownLight, true);
                // 이름표
                string name = string.IsNullOrEmpty(c.startTown) ? c.region : $"{c.region} · {c.startTown}";
                float tw = Style(ref text, GUI.skin.font, 11, Brown, TextAnchor.MiddleCenter).CalcSize(new GUIContent(name)).x + 12;
                var tag = new Rect(mapR.x + 4, mapR.yMax - 22, Mathf.Min(mapR.width - 8, tw), 18);
                Fill(tag, White);
                Label(tag, name, Brown);
            }

            // 고른 문명 한 줄 소개
            var sel = all.choices[Mathf.Clamp(pick, 0, all.choices.Count - 1)];
            var bar = new Rect((W - 420) / 2, H - 39, 420, 30);
            Fill(bar, Cream);
            Frame(bar, Shadow, 2);
            string who = sel.origin != null ? sel.origin.displayName : "";
            Label(new Rect(bar.x, bar.y + 1, bar.width, 16), $"{sel.region} · {who} ({sel.animal}) · 강점: {sel.strength}", Brown);
            Label(new Rect(bar.x, bar.y + 16, bar.width, 12),
                $"{sel.startTown}에서 출발 · ← → ↑ ↓ 고르기 · Space 출발" + (hasSave ? " · Esc 뒤로" : ""), BrownLight, true);
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
