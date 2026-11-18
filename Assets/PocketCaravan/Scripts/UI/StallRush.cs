using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 좌판 미니게임 "손님 맞이" (교역:미니게임 5:5 — 좌판에 자연스럽게 붙는 놀이).
    /// 흥정(타이밍) · 짐 싣기(공간) · 도적 도망(리듬)과 다른 "주문 맞추기": 손님이 말풍선으로 원하는 물건을 보이면
    /// 진열대 칸 번호(1~4) 키나 클릭으로 골라 준다. 맞히면 시세의 1.2배로 바로 팔고(내리 맞히면 조금씩 더, 최대 1.4배), 틀리거나 늦으면 손님이 그냥 간다.
    /// "두 개 주세요" 손님은 같은 칸을 두 번. 손님 6명, 갈수록 조금 급해진다. 하루 한 번 (PlayerState.stallRushDay). 손해는 없다 (안 팔 뿐).
    /// 처음 화면에 규칙 세 줄 + [Space 시작], 끝나면 몇 명에게 얼마 팔았는지.
    /// </summary>
    public sealed class StallRush : MonoBehaviour
    {
        static StallRush inst;
        public static bool IsOpen => inst != null && inst.phase != Phase.Closed;

        enum Phase { Closed, Intro, Play, Done }
        Phase phase;
        readonly List<ItemData> shelf = new List<ItemData>();
        int customer, served, earned, streak, need, got;
        ItemData wish; float patience, left, mood, moodAt, startedAt;
        string reaction = "";
        const int Customers = 6;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StallRush>() != null) return;
            var go = new GameObject("StallRush"); DontDestroyOnLoad(go); inst = go.AddComponent<StallRush>();
        }

        public static void Open()
        {
            var gm = GameManager.I;
            if (inst == null || gm == null || StallWindow.RushDenial(gm) != null) return;
            inst.Begin(gm);
        }

        void Begin(GameManager gm)
        {
            var p = gm.Player;
            shelf.Clear();
            var rows = new List<KeyValuePair<ItemData, int>>(p.stallStock.Items);
            rows.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < rows.Count && shelf.Count < 4; i++) shelf.Add(rows[i].Key);
            customer = served = earned = streak = 0; reaction = "";
            phase = Phase.Intro;
            Sfx.Play("open");
        }

        /// <summary>[시작]을 누르면 오늘 몫을 쓴다 (처음 화면에서 Esc로 닫으면 쓰지 않는다. 시작한 뒤 그만두면 쓴 것)</summary>
        void StartPlay(GameManager gm)
        {
            gm.Player.stallRushDay = gm.Clock.day;
            phase = Phase.Play; startedAt = Time.unscaledTime; NextCustomer(gm);
        }

        void NextCustomer(GameManager gm)
        {
            var p = gm.Player;
            var have = shelf.FindAll(it => p.stallStock.Count(it) > 0);
            if (customer >= Customers || have.Count == 0) { phase = Phase.Done; Sfx.Play(served > 0 ? "levelup" : "cancel", .7f); gm.SaveNow(); return; }
            customer++;
            wish = have[Random.Range(0, have.Count)];
            need = p.stallStock.Count(wish) >= 2 && Random.value < 0.25f ? 2 : 1; got = 0;
            patience = left = Mathf.Max(3.2f, 5.6f - customer * 0.4f);
        }

        void Pick(GameManager gm, int slot)
        {
            if (slot < 0 || slot >= shelf.Count) return;
            var p = gm.Player; var it = shelf[slot];
            if (p.stallStock.Count(it) <= 0) { Sfx.Play("cancel", .5f); return; }
            if (it != wish)
            {
                streak = 0; React("그게 아닌데요…", -1); Sfx.Play("cancel");
                NextCustomer(gm); return;
            }
            float mult = PlayerStall.RushBonus + Mathf.Min(4, streak) * 0.05f;
            int price = PlayerStall.SellOne(gm, it, mult, true);
            earned += price; got++;
            Sfx.Play("coin");
            if (got < need) { React("하나 더요!", 0); return; }
            served++; streak++;
            React(streak >= 3 ? $"단골 할게요! (+{price}냥)" : $"고마워요! (+{price}냥)", 1);
            NextCustomer(gm);
        }

        void React(string s, float m) { reaction = s; mood = m; moodAt = Time.unscaledTime; }

        void Update()
        {
            if (phase == Phase.Closed) return;
            var gm = GameManager.I; if (gm == null) { phase = Phase.Closed; return; }
            bool esc = InputReader.CancelPressed();
            if (phase == Phase.Intro)
            {
                if (esc) { phase = Phase.Closed; return; }
                if (Pressed(-1)) StartPlay(gm);
                return;
            }
            if (phase == Phase.Done) { if (esc || Pressed(-1)) phase = Phase.Closed; return; }
            if (esc) { phase = Phase.Done; return; }
            left -= Time.unscaledDeltaTime;
            if (left <= 0f) { streak = 0; React("너무 오래 걸리네요…", -1); Sfx.Play("cancel", .6f); NextCustomer(gm); return; }
            for (int i = 0; i < shelf.Count; i++) if (Pressed(i)) { Pick(gm, i); break; }
        }

        /// <summary>slot −1 = 시작·닫기 (Space·Enter), 0~3 = 숫자 1~4</summary>
        static bool Pressed(int slot)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; if (k == null) return false;
            if (slot < 0) return k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame;
            var keys = new[] { k.digit1Key, k.digit2Key, k.digit3Key, k.digit4Key };
            var pads = new[] { k.numpad1Key, k.numpad2Key, k.numpad3Key, k.numpad4Key };
            return keys[slot].wasPressedThisFrame || pads[slot].wasPressedThisFrame;
#else
            if (slot < 0) return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
            return Input.GetKeyDown(KeyCode.Alpha1 + slot) || Input.GetKeyDown(KeyCode.Keypad1 + slot);
#endif
        }

        void OnGUI()
        {
            if (phase == Phase.Closed || GameManager.I == null) return;
            UIStyle.Begin();
            GUI.depth = -20;   // 좌판 창 위
            var gm = GameManager.I; var p = gm.Player;
            float w = Mathf.Min(420, UIStyle.VirtualWidth - 24), h = phase == Phase.Intro ? 250 : 230;   // 처음 화면은 규칙 아래 진열대·시작 단추가 겹치지 않게 조금 더 높게
            var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) / 2), Mathf.Round((UIStyle.VirtualHeight - h) / 2 + 6), w, h);
            CuteUI.Dim();
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 11, "손님 맞이");

            if (phase == Phase.Intro)
            {
                float y = r.y + 34;
                CuteUI.LabelCenter(r.center.x, y, "좌판 앞에 손님이 몰려와요!", CuteUI.Text, false, true);
                string[] rules = { "손님 말풍선에 원하는 물건이 떠요", "진열대 칸 번호 1~4 키나 클릭으로 골라 줘요", "맞히면 시세보다 2할 더 받아요 · 내리 맞히면 조금씩 더", "\"두 개 주세요\" 손님은 같은 칸을 두 번 · 늦거나 틀리면 그냥 가요" };
                for (int i = 0; i < rules.Length; i++) CuteUI.LabelCenter(r.center.x, y + 26 + i * 16, rules[i], CuteUI.Sub, true);
                DrawShelf(gm, p, r, false);
                var b = new Rect(r.center.x - 60, r.yMax - 34, 120, 22);
                if (CuteUI.Button(b, "시작")) StartPlay(gm);
                CuteUI.Keycap(b.xMax - CuteUI.KeycapWidth("Space") - 6, b.center.y - 6.5f, "Space", true);
                return;
            }
            if (phase == Phase.Done)
            {
                CuteUI.LabelCenter(r.center.x, r.y + 60, served > 0 ? $"손님 {customer}명 중 {served}명에게 팔았어요" : "오늘은 손님을 놓쳤어요", CuteUI.Text, false, true);
                if (earned > 0) { CuteUI.Icon(CuteUI.Pic.Coin, r.center.x - 34, r.y + 84); CuteUI.Label(r.center.x - 20, r.y + 89, $"{earned}냥 받음", CuteUI.Up, false, true); }
                CuteUI.LabelCenter(r.center.x, r.y + 116, "내일 또 손님이 와요. 좌판 물건은 그동안에도 천천히 팔려요", CuteUI.Sub, true);
                var b = new Rect(r.center.x - 50, r.yMax - 34, 100, 22);
                if (CuteUI.Button(b, "닫기")) phase = Phase.Closed;
                return;
            }

            // 손님 (얼굴 · 말풍선 · 기다림 막대)
            CuteUI.Label(r.x + 16, r.y + 22, $"손님 {customer}/{Customers}", CuteUI.Sub, true, true);
            if (streak >= 2) CuteUI.LabelRight(r.xMax - 16, r.y + 22, $"내리 {streak}명!", CuteUI.Sell, true, true);
            CuteUI.LabelRight(r.xMax - 16, r.y + 36, $"{earned}냥", CuteUI.Up, true, true);
            float cx = r.center.x - 70, cy = r.y + 78;
            DrawFace(cx, cy, customer);
            var bub = new Rect(cx + 34, cy - 30, 140, 44);
            CuteUI.Box(bub, 8, CuteUI.White, CuteUI.Ink, 1, 2, CuteUI.SoftShadow);
            if (wish != null)
            {
                TradeLedgerUI.ItemIcon(wish, bub.x + 8, bub.y + 6, 2);
                CuteUI.Label(bub.x + 44, bub.y + 15, TradeLedgerUI.Fit(wish.displayName, 90, true, true), CuteUI.Text, true, true);
                CuteUI.Label(bub.x + 44, bub.y + 30, need == 2 ? (got == 1 ? "하나 더요!" : "두 개 주세요") : "하나 주세요", need == 2 ? CuteUI.Sell : CuteUI.Sub, true, need == 2);
            }
            float k = Mathf.Clamp01(left / Mathf.Max(0.1f, patience));
            CuteUI.Bar(new Rect(cx - 16, cy + 30, 210, 7), k, k > .35f ? CuteUI.Interact : CuteUI.Down, k > .35f ? CuteUI.InteractHi : CuteUI.AlertSoft);
            // 뒤에 기다리는 손님 줄 (작은 얼굴)
            for (int i = 0; i < Customers - customer; i++)
            {
                float qx = cx - 16 + i * 20, qy = cy + 50;
                CuteUI.Circle(qx, qy, 14, new Color32(244, 206, 160, 255), CuteUI.Ink);
                CuteUI.Box(new Rect(qx - 1, qy - 3, 16, 5), 2, Hats[(customer + 1 + i) % Hats.Length], CuteUI.Ink);
            }
            if (Customers - customer > 0) CuteUI.Label(cx - 16 + (Customers - customer) * 20 + 2, cy + 57, "기다리는 손님", CuteUI.Sub, true);
            float ra = 1f - Mathf.Clamp01((Time.unscaledTime - moodAt) / 1.2f);
            if (ra > 0f && reaction.Length > 0)
            {
                var old = GUI.color; GUI.color = new Color(old.r, old.g, old.b, ra);
                CuteUI.LabelCenter(r.center.x + 70, r.y + 52, reaction, mood > 0 ? CuteUI.Up : mood < 0 ? CuteUI.Down : CuteUI.Sell, true, true);
                GUI.color = old;
            }
            DrawShelf(gm, p, r, true);
        }

        /// <summary>진열대: 칸마다 번호 · 물건 그림 · 남은 개수. 놀이 중엔 눌러서 고른다</summary>
        void DrawShelf(GameManager gm, PlayerState p, Rect r, bool live)
        {
            float sw = 72, gap = 8, total = shelf.Count * sw + (shelf.Count - 1) * gap;
            float sx = r.center.x - total / 2f, sy = r.yMax - (live ? 84 : 92);
            for (int i = 0; i < shelf.Count; i++)
            {
                var it = shelf[i]; int n = p.stallStock.Count(it);
                var b = new Rect(sx + i * (sw + gap), sy, sw, 46);
                bool hint = live && it == wish && Time.unscaledTime - startedAt < 6f;   // 처음 몇 초만 살짝 힌트
                CuteUI.Box(b, 7, n > 0 ? (hint ? CuteUI.SellSoft : CuteUI.Paper2) : CuteUI.ZeroSoft, n > 0 ? CuteUI.Ink : CuteUI.Line);
                CuteUI.Keycap(b.x + 4, b.y + 4, (i + 1).ToString(), true);
                TradeLedgerUI.ItemIcon(it, b.center.x - 16, b.y + 8, 2);
                CuteUI.LabelRight(b.xMax - 5, b.yMax - 8, $"×{n}", n > 0 ? CuteUI.Sub : CuteUI.Zero, true);
                if (live && n > 0 && CuteUI.Hit(b, GUIContent.none, GUIStyle.none)) Pick(gm, i);
            }
            if (live) CuteUI.LabelCenter(r.center.x, r.yMax - 20, "번호 키 1~4 또는 클릭 · Esc 그만", CuteUI.Sub, true);
        }

        static readonly Color32[] Hats = { new Color32(70, 110, 160, 255), new Color32(160, 70, 60, 255), new Color32(80, 130, 80, 255), new Color32(180, 140, 60, 255), new Color32(110, 80, 140, 255), new Color32(60, 60, 60, 255) };
        void DrawFace(float x, float y, int seed)
        {
            var skin = new Color32(244, 206, 160, 255);
            CuteUI.Circle(x - 14, y - 14, 28, skin, CuteUI.Ink);
            var hat = Hats[seed % Hats.Length];
            CuteUI.Box(new Rect(x - 16, y - 20, 32, 9), 4, hat, CuteUI.Ink);
            float m = Time.unscaledTime - moodAt < 1.2f ? mood : 0f;
            CuteUI.Fill(new Rect(x - 7, y - 3, 3, 3), CuteUI.Ink); CuteUI.Fill(new Rect(x + 4, y - 3, 3, 3), CuteUI.Ink);
            if (m > 0) { CuteUI.Fill(new Rect(x - 5, y + 5, 10, 2), CuteUI.Ink); CuteUI.Fill(new Rect(x - 6, y + 4, 2, 1), CuteUI.Ink); CuteUI.Fill(new Rect(x + 4, y + 4, 2, 1), CuteUI.Ink); }
            else if (m < 0) { CuteUI.Fill(new Rect(x - 5, y + 6, 10, 2), CuteUI.Ink); CuteUI.Fill(new Rect(x - 6, y + 7, 2, 1), CuteUI.Ink); CuteUI.Fill(new Rect(x + 4, y + 7, 2, 1), CuteUI.Ink); }
            else CuteUI.Fill(new Rect(x - 3, y + 5, 6, 2), CuteUI.Ink);
        }
    }
}
