using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 시작 쪽 화면(로그인 · 이어하기 · 상인 고르기) 모양 (2026-11-18 윤채, 디자인 의견 "작은 동물 상인들이 함께하는 아기자기한 여행 상점").
    /// - 색: 크림 · 살구 코랄 · 꿀색 나무, 외곽선은 짙은 초콜릿, 주요 행동만 밝은 청록.
    /// - 상인 수첩 패널(크림 종이 · 초콜릿 도트 테 · 위 작은 탭 · 왼쪽 고리), 짧고 통통한 도트 단추(윗면 빛 · 아래 4px 그림자 · 누르면 2px 내려감),
    ///   키보드로 고른 단추는 색 대신 네 모서리 꺾쇠 + 글자 왼쪽 ▶.
    /// - 왼쪽 상점 장면: 코랄·크림 줄무늬 천막 + 꿀색 좌판(그림은 코드로 그린 픽셀) 둘레에
    ///   토비(손님 맞이) · 루나(좌판 뒤에서 빼꼼) · 보리(상자 정리) · 사피(보따리 옆에 앉음). 말풍선은 한 번에 하나, 돌아가며 인사.
    ///   이어하기 화면은 저장된 상인 하나 + 그 지역 특산품.
    /// 그림은 정수배로만 키운다(도트가 흐려지지 않게). 움직임은 말하는 상인 하나만 (나머지는 가만히).
    /// </summary>
    public static class TitleUI
    {
        static Color32 C(int v) => new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);

        public static readonly Color32 Choco = C(0x5B3A29), ChocoSoft = C(0x8C6650), ChocoShadow = new Color32(0x5B, 0x3A, 0x29, 70),
            Cream = C(0xFFF7EA), Ivory = C(0xFFFDF6), CreamDeep = C(0xFCEBD2),
            Coral = C(0xFF9E85), CoralLo = C(0xE9806A), Honey = C(0xEBB86A), HoneyLo = C(0xC99248), HoneyHi = C(0xF7D79E),
            Teal = C(0x36A89E), TealHi = C(0x86D9CF), TealLo = C(0x23796F), Leaf = C(0x7CC27A), LeafLo = C(0x4E9858), Pot = C(0xE08A5C);

        // ---------------- 패널 · 단추 · 칸 ----------------

        /// <summary>상인 수첩 패널: 크림 종이 + 초콜릿 도트 테 + 짧은 그림자, 왼쪽 위 작은 탭(글), 왼쪽 가장자리 고리</summary>
        public static void Panel(Rect r, string tab = null)
        {
            if (!string.IsNullOrEmpty(tab))
            {
                float tw = CuteUI.Width(tab, true, true) + 18;
                var t = new Rect(r.x + 18, r.y - 13, tw, 18);
                CuteUI.Box(t, 3, Coral, Choco, 1);
                CuteUI.LabelCenter(t.center.x, t.y + 7, tab, CuteUI.White, true, true);
            }
            CuteUI.Box(r, 4, Cream, Choco, 2, 3, ChocoShadow);
            CuteUI.Fill(new Rect(r.x + 4, r.y + 3, r.width - 8, 1), CuteUI.White);
            // 수첩 고리
            for (float y = r.y + 22; y < r.yMax - 14; y += 26)
            {
                CuteUI.Fill(new Rect(r.x - 3, y, 7, 4), Choco);
                CuteUI.Fill(new Rect(r.x - 2, y + 1, 5, 2), HoneyHi);
            }
        }

        /// <summary>얇은 크림 카드 (고르기 화면 카드 · 아래 설명칸)</summary>
        public static void Card(Rect r) => CuteUI.Box(r, 4, Cream, Choco, 2, 3, ChocoShadow);

        /// <summary>
        /// 짧고 통통한 도트 단추. primary = 밝은 청록(윗면 빛, 아래 4px 초콜릿 그림자), 아니면 크림(3px 그림자).
        /// 마우스를 누르고 있으면 2px 내려가고 그림자가 준다. selected = 키보드로 고른 것 (색은 그대로, 모서리 꺾쇠 + ▶). 누르면 true
        /// </summary>
        public static bool Button(Rect r, string label, bool primary, bool selected = false, bool small = false, bool enabled = true)
        {
            var e = Event.current;
            bool hover = enabled && r.Contains(e.mousePosition);
            bool pressed = hover && GUIUtility.hotControl != 0;
            float sh = primary ? 4 : 3;
            if (pressed) { r.y += 2; sh -= 2; }
            Color32 fill = primary ? (hover ? C(0x3FB8AD) : Teal) : (hover ? CreamDeep : C(0xFFF3DC));
            CuteUI.Box(new Rect(r.x, r.y, r.width, r.height), 3, fill, Choco, 1, (int)sh, Choco);
            CuteUI.Fill(new Rect(r.x + 3, r.y + 2, r.width - 6, 2), primary ? TealHi : CuteUI.White);
            if (primary) CuteUI.Fill(new Rect(r.x + 3, r.yMax - 3, r.width - 6, 1), TealLo);
            Color32 ink = primary ? CuteUI.White : Choco;
            float lw = CuteUI.Width(label, small, true);
            if (selected)
            {
                float lx = Mathf.Round(r.center.x - (lw + 9f) / 2f);
                CuteUI.Pointer(lx, r.center.y + 1, primary ? CuteUI.White : Teal);
                CuteUI.Label(lx + 9f, r.center.y + 1, label, ink, small, true);
                Corners(new Rect(r.x, r.y, r.width, r.height + sh));
            }
            else CuteUI.LabelCenter(r.center.x, r.center.y + 1, label, ink, small, true);
            return enabled && CuteUI.Hit(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>고른 표시: 네 모서리 바깥 꺾쇠 (초콜릿)</summary>
        public static void Corners(Rect r)
        {
            float x0 = Mathf.Round(r.x - 4), y0 = Mathf.Round(r.y - 4), x1 = Mathf.Round(r.xMax + 2), y1 = Mathf.Round(r.yMax + 2);
            const float L = 6, T = 2;
            CuteUI.Fill(new Rect(x0, y0, L, T), Choco); CuteUI.Fill(new Rect(x0, y0, T, L), Choco);
            CuteUI.Fill(new Rect(x1 + T - L, y0, L, T), Choco); CuteUI.Fill(new Rect(x1, y0, T, L), Choco);
            CuteUI.Fill(new Rect(x0, y1, L, T), Choco); CuteUI.Fill(new Rect(x0, y1 + T - L, T, L), Choco);
            CuteUI.Fill(new Rect(x1 + T - L, y1, L, T), Choco); CuteUI.Fill(new Rect(x1, y1 + T - L, T, L), Choco);
        }

        public enum FieldIcon { None, Tag, Key, Globe }

        /// <summary>입력 칸 틀 (아이보리 · 초콜릿 테, 쓰는 중이면 청록 두 줄) + 왼쪽 작은 그림. 글을 쓸 자리를 돌려준다</summary>
        public static Rect Field(Rect r, bool active, FieldIcon icon)
        {
            CuteUI.Box(r, 3, Ivory, active ? Teal : Choco, active ? 2 : 1);
            if (icon == FieldIcon.None) return new Rect(r.x + 4, r.y, r.width - 6, r.height);
            var t = icon == FieldIcon.Tag ? Tex("tag") : icon == FieldIcon.Key ? Tex("key") : Tex("globe");
            float k = 1;
            var ir = new Rect(r.x + 7, Mathf.Round(r.center.y - t.height * k / 2f), t.width * k, t.height * k);
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(ir, t);
            return new Rect(ir.xMax + 5, r.y, r.xMax - ir.xMax - 7, r.height);
        }

        // ---------------- 로고 ----------------

        /// <summary>"바리바리" 판: 주황 판 · 흰 글씨는 그대로, 테와 글씨 그림자는 초콜릿으로 얇게. scale = 정수배</summary>
        public static void Logo(float cx, float y, float scale = 2f)
        {
            var m = GUI.matrix;
            CuteUI.ScaleAround(scale, new Vector2(cx, y));
            const string label = "바리바리";
            float w = Mathf.Round(CuteUI.Width(label, false, true) + 44), h = 22, x = Mathf.Round(cx - w / 2f);
            CuteUI.Box(new Rect(x, y, w, h), 5, CuteUI.Frame, Choco, 1, 2, Choco);
            CuteUI.Fill(new Rect(x + 4, y + 2, w - 8, 1), CuteUI.FrameHi);
            CuteUI.Icon(CuteUI.Pic.Star4, x + 9, y + 7);
            CuteUI.Icon(CuteUI.Pic.Star4, x + w - 16, y + 7);
            CuteUI.LabelCenter(cx, y + 12, label, Choco, false, true);
            CuteUI.LabelCenter(cx, y + 11, label, CuteUI.White, false, true);
            GUI.matrix = m;
        }

        // ---------------- 상점 장면 ----------------

        public const float GroundH = 70f;
        public static float SceneWidth(float W) => W >= 560f ? Mathf.Round(W * .56f) : 0f;

        /// <summary>오른쪽 메뉴 패널 자리 (땅 위 가운데. 장면이 없으면 화면 가운데)</summary>
        public static Rect MenuPanel(float W, float H, float height, float width = 236f)
        {
            float sw = SceneWidth(W);
            float cx = sw > 0 ? sw + (W - sw) / 2f - 4f : W / 2f;
            float top = sw > 0 ? Mathf.Max(16f, Mathf.Round((H - GroundH - height) / 2f) + 4f) : Mathf.Max(80f, Mathf.Round((H - height) / 2f + 16f));
            return new Rect(Mathf.Round(cx - width / 2f), top, width, height);
        }

        static readonly string[] Lines = { "어서 오세요!", "오늘 물건 좋아요~", "상자 정리 끝!", "같이 떠나요!" };
        static readonly string[] Order = { "북유럽", "베네치아", "고려", "아라비아" };   // 토비 · 루나 · 보리 · 사피

        static StartChoices.Choice Who(string region)
        {
            var all = StartChoices.Get();
            return all != null ? all.choices.Find(c => c.region == region && c.origin != null && PlayerAppearance.HasFrames(c.origin)) : null;
        }

        /// <summary>
        /// 왼쪽 상점 장면. saved == null이면 로그인 장면(네 상인 · 돌아가며 인사), 아니면 이어하기 장면(그 상인 하나 · 그 지역 특산품 · 팻말 sign).
        /// 좁은 화면이면 로고만 가운데 위에
        /// </summary>
        public static void Scene(float W, float H, StartChoices.Choice saved, string sign, string say, string tagline = null)
        {
            float sw = SceneWidth(W), gy = H - GroundH;
            Ground(W, H, gy);
            float cx = sw > 0 ? sw / 2f : W / 2f;
            // 11/18 화면을 보고: 로고가 너무 위에 붙어 좌판과 사이가 비었다 → 조금 내리고 아래 한 줄
            Logo(cx, sw > 0 ? 46f : 30f);
            if (sw <= 0) return;
            if (!string.IsNullOrEmpty(tagline)) CuteUI.LabelCenter(cx, 114f, tagline, ChocoSoft, false, true);
            const float k = 3f;
            float t = Time.unscaledTime;

            // 좌판 (뒤판) 자리
            var back = Tex("stallBack");
            var stall = new Rect(Mathf.Round(cx - back.width * k / 2f - (saved != null ? 26 : 0)), gy + 6 - back.height * k, back.width * k, back.height * k);
            float counterTop = stall.y + 37 * k;
            CuteUI.Box(new Rect(stall.x + 6, gy - 2, stall.width - 12, 10), 4, ChocoShadow, ChocoShadow, 0);
            Draw(back, stall);

            if (saved == null)
            {
                int speaker = Mathf.FloorToInt(t / 3.2f) % 4;
                float since = t % 3.2f;
                var luna = Who(Order[1]);
                // 루나: 좌판 뒤에서 빼꼼 (얼굴은 계산대 위로)
                Rect lunaHead = default;
                if (luna != null) lunaHead = Hero(luna, 0, new Vector2(stall.x + 76, counterTop + 21), k, speaker == 1 ? since : 9f, false, 0f);   // 얼굴까지 계산대 위로
                Draw(Tex("stallFront"), stall);
                Goods(stall, counterTop, null);
                // 보리: 왼쪽에서 상자 정리 (오른쪽 보기)
                var crates = Tex("crates");
                var crR = new Rect(stall.x - 6, gy + 10 - crates.height * k, crates.width * k, crates.height * k);
                var bori = Who(Order[2]);
                Rect boriHead = default;
                if (bori != null) boriHead = Hero(bori, 12, new Vector2(crR.x - 22, gy + 12), k, speaker == 2 ? since : 9f, true, 0f);
                CuteUI.Box(new Rect(crR.x + 2, gy + 4, crR.width - 4, 8), 4, ChocoShadow, ChocoShadow, 0);
                Draw(crates, crR);
                // 토비: 오른쪽에서 손님 맞이
                var tobi = Who(Order[0]);
                Rect tobiHead = default;
                if (tobi != null) tobiHead = Hero(tobi, 0, new Vector2(stall.xMax + 26, gy + 14), k, speaker == 0 ? since : 9f, true, 0f);
                // 사피: 좌판 앞 보따리 옆에 앉음 (다리는 땅에 묻혀 앉은 모습)
                var sapi = Who(Order[3]);
                Rect sapiHead = default;
                if (sapi != null) sapiHead = Hero(sapi, 0, new Vector2(stall.center.x - 6, gy + 22), k, speaker == 3 ? since : 9f, true, 6f / 24f);
                var sack = Tex("sack");
                var skR = new Rect(stall.center.x + 18, gy + 22 - sack.height * k, sack.width * k, sack.height * k);
                CuteUI.Box(new Rect(skR.x + 4, gy + 16, skR.width - 8, 8), 4, ChocoShadow, ChocoShadow, 0);
                Draw(sack, skR);
                // 화분 (토비 오른쪽. 16:10처럼 장면이 좁아 토비와 겹치면 그리지 않는다)
                var pot = Tex("plant");
                if (stall.xMax + 54 + pot.width * k <= sw - 4)
                    Draw(pot, new Rect(stall.xMax + 54, gy + 8 - pot.height * k, pot.width * k, pot.height * k));
                // 말풍선: 한 번에 하나, 돌아가며
                if (since < 2.7f)
                {
                    var head = speaker == 0 ? tobiHead : speaker == 1 ? lunaHead : speaker == 2 ? boriHead : sapiHead;
                    if (head.width > 0) Bubble(Lines[speaker], head, since, sw);
                }
                return;
            }

            // 이어하기: 그 상인 하나가 좌판 오른쪽에서 맞이 · 계산대에 그 지역 특산품 · 위에 마지막에 있던 곳 팻말
            Draw(Tex("stallFront"), stall);
            Goods(stall, counterTop, saved);
            if (!string.IsNullOrEmpty(sign))
            {
                float tw = CuteUI.Width(sign, true, true) + 16;
                // 팻말은 천막 가운데에 건다 (위 한 줄 · 로고와 겹치지 않게)
                var tr = new Rect(Mathf.Round(stall.center.x - tw / 2f), stall.y + 34, tw, 17);
                CuteUI.Box(tr, 3, Cream, Choco, 1, 2, Choco);
                CuteUI.LabelCenter(tr.center.x, tr.center.y, sign, Choco, true, true);
            }
            var pot2 = Tex("plant");
            Draw(pot2, new Rect(stall.x - pot2.width * k + 2, gy + 8 - pot2.height * k, pot2.width * k, pot2.height * k));
            float open = t - openedAt;
            if (open < 0f || open > 60f) { openedAt = t; open = 0f; }
            var hd = Hero(saved, 0, new Vector2(stall.xMax + 30, gy + 14), k, open < 1.2f ? open : 9f, true, 0f);
            if (!string.IsNullOrEmpty(say) && hd.width > 0) Bubble(say, hd, Mathf.Min(open, 0.5f), sw, true);
        }

        static float openedAt = -99f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { openedAt = -99f; }

        /// <summary>모래 바닥 + 작은 점 무늬</summary>
        static void Ground(float W, float H, float gy)
        {
            var sand = C(0xF3DFBC); var speck = C(0xE2C796); var edge = C(0xD9B884);
            CuteUI.Fill(new Rect(0, gy, W, H - gy), sand);
            CuteUI.Fill(new Rect(0, gy, W, 2), edge);
            int row = 0;
            for (float yy = gy + 9; yy < H; yy += 10, row++)
                for (float xx = (row % 2) * 13 + 5; xx < W; xx += 26) { CuteUI.Fill(new Rect(xx, yy, 3, 1), speck); CuteUI.Fill(new Rect(xx + 9, yy + 4, 1, 1), speck); }
        }

        /// <summary>
        /// 상인 하나를 세운다 (feet = 발 가운데, k배). frame = 그림 번호(0 앞 · 12 오른쪽). talk = 말하기 시작한 뒤 시간(말할 때만 한 번 폴짝, 보리는 상자 손질),
        /// cutBottom = 아래를 잘라 앉은 모습(0~1). 머리 자리(말풍선용)를 돌려준다
        /// </summary>
        static Rect Hero(StartChoices.Choice who, int frame, Vector2 feet, float k, float talk, bool shadow, float cutBottom)
        {
            var frames = who.origin.playerFrames;
            if (who.region == "고려" && talk < 2.7f) frame = 12 + (Mathf.FloorToInt(talk / .35f) % 2) * 2;   // 보리: 말하는 동안 상자 손질
            var s = frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
            float w = Mathf.Round(s.rect.width / s.pixelsPerUnit * 16f * k), h = Mathf.Round(s.rect.height / s.pixelsPerUnit * 16f * k);
            float vis = Mathf.Round(h * (1f - cutBottom));
            float hop = talk < .36f ? Mathf.Round(Mathf.Sin(talk / .36f * Mathf.PI) * 2f) * 2f : 0f;
            if (shadow) CuteUI.Box(new Rect(feet.x - 18, feet.y - 5, 36, 8), 4, ChocoShadow, ChocoShadow, 0);
            var dst = new Rect(Mathf.Round(feet.x - w / 2f), feet.y - vis - hop, w, vis);
            CuteUI.Sprite(s, dst, new Rect(0, cutBottom, 1, 1 - cutBottom));
            return new Rect(dst.x, dst.y, w, 12);
        }

        /// <summary>계산대 위 물건: 로그인은 과일 상자 · 빵 바구니 · 지도 · 보따리, 이어하기는 그 지역 특산품</summary>
        static void Goods(Rect stall, float counterTop, StartChoices.Choice saved)
        {
            var db = GameManager.I != null ? GameManager.I.Db : null;
            float x = saved == null ? stall.x + 100 : stall.x + 36, y = counterTop - 28;
            if (saved == null)
            {
                // 루나 오른쪽: 과일 상자 · 빵 바구니 · 지도 (보따리는 앞 땅에)
                var fruit = db != null ? db.items.Find(i => i != null && i.id == "venice_fruit") : null;
                if (fruit != null) TradeLedgerUI.ItemIcon(fruit, x, y, 2);
                CuteUI.Icon(CuteUI.Pic.Basket, x + 36, counterTop - 14, 3);
                CuteUI.Icon(CuteUI.Pic.Scroll, x + 60, counterTop - 20, 2);
                return;
            }
            if (db == null) return;
            string civ = saved.origin.id == "viking" ? "north" : saved.origin.id;
            int n = 0;
            foreach (var item in db.items)
            {
                if (item == null || item.questItem || item.originTown == null || OriginPerks.Civ(item.originTown.region) != civ) continue;
                TradeLedgerUI.ItemIcon(item, x, y, 2);
                x += 34;
                if (++n >= 3) break;
            }
        }

        /// <summary>말풍선 (나타날 때 톡 커진다). head = 머리 자리, sw = 장면 오른쪽 끝</summary>
        static void Bubble(string text, Rect head, float since, float sw, bool right = false)
        {
            float bw = CuteUI.Width(text, true) + 16;
            float tail = head.center.x;
            var b = new Rect(Mathf.Round(Mathf.Clamp(right ? tail - 16 : tail - bw / 2f + 10, 6, sw - bw - 6)), Mathf.Round(head.y - 24), bw, 18);
            var m = GUI.matrix;
            float a = Mathf.Clamp01(since / .16f);
            if (a < 1f) CuteUI.ScaleAround(Mathf.Lerp(.6f, 1f, a), new Vector2(tail, b.yMax));
            CuteUI.Bubble(b, Mathf.Clamp(tail, b.x + 8, b.xMax - 8), Choco);
            CuteUI.LabelCenter(b.center.x, b.center.y, text, Choco, true);
            GUI.matrix = m;
        }

        static void Draw(Texture2D t, Rect r) { if (Event.current.type == EventType.Repaint && t != null) GUI.DrawTexture(r, t); }

        // ---------------- 픽셀 그림 (코드로 그린다, 점 필터) ----------------

        static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();

        static Texture2D Tex(string key)
        {
            if (tex.TryGetValue(key, out var t) && t != null) return t;
            switch (key)
            {
                case "stallBack": t = StallBack(); break;
                case "stallFront": t = StallFront(); break;
                default: t = FromRows(Art[key]); break;
            }
            return tex[key] = t;
        }

        sealed class Pix
        {
            public readonly int w, h; public readonly Color32[] px;
            public Pix(int w, int h) { this.w = w; this.h = h; px = new Color32[w * h]; }
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) px[(h - 1 - y) * w + x] = c; }
            public void Rect(int x, int y, int rw, int rh, Color32 c) { for (int j = 0; j < rh; j++) for (int i = 0; i < rw; i++) Set(x + i, y + j, c); }
            public Texture2D Tex()
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                t.SetPixels32(px); t.Apply(false, false); return t;
            }
        }

        const int SW = 60, SH = 52;

        /// <summary>좌판 뒤판: 깃발 줄 · 코랄/크림 줄무늬 천막(물결 아래 끝) · 꿀색 기둥 · 따뜻한 뒷벽</summary>
        static Texture2D StallBack()
        {
            var p = new Pix(SW, SH);
            // 깃발 줄
            for (int x = 3; x <= 56; x++) p.Set(x, 3, ChocoSoft);
            Color32[] flag = { Teal, Honey, Coral };
            for (int i = 0, x0 = 5; x0 + 4 <= 55; i++, x0 += 7)
            {
                var c = flag[i % 3];
                p.Rect(x0, 4, 5, 1, c); p.Rect(x0 + 1, 5, 3, 1, c); p.Set(x0 + 2, 6, c);
                p.Set(x0 - 1, 4, Choco); p.Set(x0 + 5, 4, Choco);
            }
            // 기둥
            foreach (int px0 in new[] { 5, 52 })
            {
                p.Rect(px0 - 1, 12, 5, SH - 12, Choco);
                p.Rect(px0, 12, 3, SH - 13, Honey);
                p.Rect(px0, 12, 1, SH - 13, HoneyHi);
            }
            // 뒷벽 + 선반
            p.Rect(9, 21, 42, 17, C(0xFBE3C2));
            p.Rect(9, 27, 42, 1, HoneyLo);
            // 천막
            p.Rect(1, 9, 58, 1, Choco);
            for (int x = 1; x <= 58; x++)
            {
                bool coral = ((x - 2) / 5) % 2 == 0;
                var c = coral ? Coral : Cream;
                for (int y = 10; y <= 18; y++) p.Set(x, y, c);
                p.Set(x, 10, coral ? C(0xFFC1AE) : CuteUI.White);   // 천막 윗줄 빛
            }
            p.Rect(0, 9, 1, 11, Choco); p.Rect(59, 9, 1, 11, Choco);
            // 물결 아래 끝 (줄무늬마다 둥글게)
            for (int s0 = 1; s0 <= 58; s0 += 5)
            {
                bool coral = ((s0 - 1) / 5) % 2 == 0;
                var c = coral ? Coral : Cream;
                for (int i = 0; i < 5 && s0 + i <= 58; i++)
                {
                    int x = s0 + i;
                    bool mid = i >= 1 && i <= 3;
                    p.Set(x, 19, c);
                    if (mid) p.Set(x, 20, c);
                    p.Set(x, mid ? 21 : 20, Choco);
                }
            }
            return p.Tex();
        }

        /// <summary>좌판 앞 계산대 (꿀색 나무, 위 판자 밝게, 판자 줄)</summary>
        static Texture2D StallFront()
        {
            var p = new Pix(SW, SH);
            p.Rect(2, 36, 56, 16, Choco);
            p.Rect(3, 37, 54, 3, HoneyHi);
            p.Rect(3, 40, 54, 1, HoneyLo);
            p.Rect(4, 41, 52, 10, Honey);
            p.Rect(4, 46, 52, 1, HoneyLo);
            foreach (int x in new[] { 4, 30, 55 }) p.Rect(x, 41, 1, 10, HoneyLo);
            p.Rect(6, 42, 22, 1, HoneyHi); p.Rect(32, 42, 22, 1, HoneyHi);
            return p.Tex();
        }

        // 작은 소품: X 초콜릿 · h 꿀 · H 밝은 꿀 · l 어두운 꿀 · c 코랄 · C 진한 코랄 · w 크림 · g 잎 · G 진한 잎 · p 화분 · t 청록 · W 흰색
        static readonly Dictionary<string, string[]> Art = new Dictionary<string, string[]>
        {
            ["crates"] = new[]
            {
                "...XXXXXXXXXX...",
                "...XHHHHHHHHX...",
                "...XhlhhlhhlX...",
                "...XhlhhlhhlX...",
                "...XhlhhlhhlX...",
                "XXXXXXXXXXXXXXXX",
                "XHHHHHHHXHHHHHHX",
                "XhlhhlhhXhlhhlhX",
                "XhlhhlhhXhlhhlhX",
                "XhlhhlhhXhlhhlhX",
                "XhlhhlhhXhlhhlhX",
                "XXXXXXXXXXXXXXXX",
            },
            ["sack"] = new[]
            {
                "......XXX.......",
                ".....XcXcX......",
                "......XcX.......",
                "....XXwwwXX.....",
                "...XwwcwwcwX....",
                "..XwcwwwwwcwX...",
                "..XwwwcwcwwwX...",
                ".XwcwwwwwwwcwX..",
                ".XwwwcwwcwwwwX..",
                ".XwcwwwwwwwcwX..",
                "..XwwwcwcwwwX...",
                "...XXXXXXXXX....",
            },
            ["plant"] = new[]
            {
                "...XX.XX...",
                "..XggXggX..",
                ".XgGgggGgX.",
                ".XggGgGggX.",
                "..XgggggX..",
                "...XXgXX...",
                "..XXXXXXX..",
                "..XpppppX..",
                "...XpppX...",
                "...XXXXX...",
            },
            ["tag"] = new[]
            {
                "..XXXXXXX",
                ".XhhhhhhX",
                "XhXhhhhhX",
                ".XhhhhhhX",
                "..XXXXXXX",
            },
            ["key"] = new[]
            {
                ".XXX......",
                "XhhhXXXXXX",
                "XhXhhhhhhX",
                "XhhhXXhXhX",
                ".XXX..X.X.",
            },
            ["globe"] = new[]
            {
                ".XXXXX.",
                "XtWtWtX",
                "XWtttWX",
                "XtWtWtX",
                ".XXXXX.",
            },
        };

        static Texture2D FromRows(string[] rows)
        {
            int w = rows[0].Length, h = rows.Length;
            var p = new Pix(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    Color32 c;
                    switch (rows[y][x])
                    {
                        case 'X': c = Choco; break;
                        case 'h': c = Honey; break;
                        case 'H': c = HoneyHi; break;
                        case 'l': c = HoneyLo; break;
                        case 'c': c = Coral; break;
                        case 'C': c = CoralLo; break;
                        case 'w': c = Cream; break;
                        case 'g': c = Leaf; break;
                        case 'G': c = LeafLo; break;
                        case 'p': c = Pot; break;
                        case 't': c = Teal; break;
                        case 'W': c = CuteUI.White; break;
                        default: continue;
                    }
                    p.Set(x, y, c);
                }
            return p.Tex();
        }
    }
}
