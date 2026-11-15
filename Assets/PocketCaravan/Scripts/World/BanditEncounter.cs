using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/15 은샘: 도적에게 잡혔을 때 뜨는 창. 셋 중 하나를 고른다.
    /// 1 통행세: 그대로 낸다. 2 흥정: 장터 저울 흥정을 도적판으로 (HaggleDuel.OpenBandit, 금색 반값 · 빨강 1.5배).
    /// 3 도망: 창 안 리듬 놀이. 화살표가 판정 칸에 닿을 때 같은 방향키를 누르면 도적과 거리가 벌어지고, 놓치면 따라붙는다.
    ///   짐이 무거울수록 음표가 많고 빠르며 처음 거리가 짧다. 붙잡히면 가장 비싼 짐을 뺏긴다 (예전 "거절한다"와 같다).
    /// 참고: DDR·osu!mania(방향별 색 화살표), 태고의 달인(판정 칸 번쩍·불꽃·연속), 크립트 오브 더 네크로댄서(박자 선).
    /// 위 장면 바탕은 지금 맵을 잠깐 찍어(사람 그림은 빼고) 옆으로 흘린다.
    /// </summary>
    public class BanditEncounter : MonoBehaviour
    {
        public enum Result { Paid, Escaped, Robbed }

        static BanditEncounter inst;
        public static bool IsOpen => inst != null && inst.phase != Phase.Closed;

        enum Phase { Closed, Choose, Haggle, Run }
        Phase phase = Phase.Closed;
        int toll, sel = 1; float openedAt; int openedFrame;
        Sprite banditFace; Sprite[] banditRun = new Sprite[4], playerRun = new Sprite[4];
        string mapId;
        Action<Result> done;
        RenderTexture band;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<BanditEncounter>() != null) return;
            var go = new GameObject("BanditEncounter"); DontDestroyOnLoad(go); inst = go.AddComponent<BanditEncounter>();
        }
        void Awake() => inst = this;

        /// <summary>잡혔을 때 창을 연다. 돈·짐 처리는 여기서 하고, 끝나면 done(결과)</summary>
        public static bool Open(Bandit who, int toll, Action<Result> done)
        {
            if (inst == null || inst.phase != Phase.Closed) return false;
            if (!UIState.TryOpen(inst, () => inst.ForceClose())) return false;
            inst.Begin(who, toll, done);
            return true;
        }

        void Begin(Bandit who, int t, Action<Result> cb)
        {
            toll = t; done = cb;
            mapId = MapRoot.Current != null ? MapRoot.Current.mapId : "";
            var pa = PlayerController.Instance != null ? PlayerController.Instance.anim : null;
            var ba = who != null ? who.anim : null;
            for (int i = 0; i < 4; i++)
            {
                playerRun[i] = pa != null && pa.frames != null && pa.frames.Length >= 16 ? pa.frames[12 + i] : null;
                banditRun[i] = ba != null && ba.frames != null && ba.frames.Length >= 16 ? ba.frames[12 + i] : null;
            }
            banditFace = ba != null && ba.frames != null && ba.frames.Length > 0 ? ba.frames[0] : null;
            sel = GameManager.I.Player.money >= toll ? 0 : 1;
            phase = Phase.Choose; openedAt = Time.unscaledTime; openedFrame = Time.frameCount;
            Sfx.Play("alert");
        }

        void ForceClose()
        {
            phase = Phase.Closed; done = null;
            if (band != null) { band.Release(); Destroy(band); band = null; }
        }

        void Finish(Result r)
        {
            var cb = done;
            ForceClose();
            UIState.Release(this);
            cb?.Invoke(r);
        }

        void Pay(int price)
        {
            var p = GameManager.I.Player;
            if (p.money >= price)
            {
                p.money -= price; Sfx.Play("coin");
                Notify.Show($"통행세 {price}냥을 냈다.");
            }
            else GameManager.I.StealMostValuable();   // 돈이 모자라면 짐을 뺏긴다
            Finish(Result.Paid);
        }

        // ================= 고르기 =================
        bool CanPay => GameManager.I.Player.money >= toll;

        void Pick(int i)
        {
            if (i == 0) { if (!CanPay) { Sfx.Play("bump"); return; } Pay(toll); return; }
            if (i == 1)
            {
                if (HaggleDuel.OpenBandit(banditFace, toll, mapId, Pay)) phase = Phase.Haggle;
                return;
            }
            StartRun();
        }

        void Update()
        {
            if (phase == Phase.Closed || Time.frameCount == openedFrame) return;
            if (phase == Phase.Choose)
            {
                if (Time.unscaledTime - openedAt < 0.3f) return;   // 쫓기다 Space를 누르던 손에 바로 골라지지 않게
                var mv = InputReader.MovePressed();
                if (mv.x != 0) { sel = Mathf.Clamp(sel + mv.x, 0, 2); Sfx.Play("move", .6f); }
                int n = InputReader.NumberPressed();
                if (n >= 1 && n <= 3) { sel = n - 1; Pick(sel); return; }
                if (InputReader.InteractPressed()) Pick(sel);
                return;
            }
            if (phase == Phase.Run) RunUpdate();
        }

        // ================= 도망 (리듬) =================
        struct Note { public float t; public int dir; public int judge; }   // dir 0←1↓2↑3→ · judge 0 아직 1 딱 2 좋아 3 놓침
        readonly List<Note> notes = new List<Note>();
        float runStart, endAt = -1f, speed, gap, gapShown;
        float lastEventAt = -9f; int lastDir, lastJudge, combo, keyDir = -1; float keyAt = -9f;
        bool won, caught;
        const float Perfect = 0.07f, Good = 0.14f, Lead = 2.2f;

        float RunT => Time.unscaledTime - runStart;

        void StartRun()
        {
            var p = GameManager.I.Player;
            float load = p.LoadRatio;
            int count = 10 + Mathf.RoundToInt(6f * load);
            float beat = 60f / (112f + 40f * load);
            float halfChance = 0.2f + 0.35f * load;
            speed = 120f + 60f * load;
            gap = gapShown = 70f - 16f * load;
            var rng = new System.Random(Environment.TickCount);
            notes.Clear();
            float t = Lead; int prev = -1;
            for (int i = 0; i < count; i++)
            {
                int d = rng.Next(4);
                if (d == prev && rng.NextDouble() < 0.5) d = (d + 1 + rng.Next(3)) % 4;   // 같은 방향이 너무 이어지지 않게
                notes.Add(new Note { t = t, dir = d }); prev = d;
                t += (i > 1 && rng.NextDouble() < halfChance) ? beat * 0.5f : beat;
            }
            won = caught = false; endAt = -1f; combo = 0; lastEventAt = -9f; keyAt = -9f;
            Snapshot();
            runStart = Time.unscaledTime;
            phase = Phase.Run;
            Sfx.Play("alert");
        }

        static readonly Vector2Int[] Dirs = { Vector2Int.left, Vector2Int.down, Vector2Int.up, Vector2Int.right };

        void RunUpdate()
        {
            float t = RunT;
            gapShown = Mathf.MoveTowards(gapShown, gap, Time.unscaledDeltaTime * 140f);
            if (endAt >= 0f)
            {
                if (t - endAt > 1.4f)
                {
                    if (won) { Notify.Show("도적을 따돌렸다!"); Finish(Result.Escaped); }
                    else { GameManager.I.StealMostValuable(); Finish(Result.Robbed); }
                }
                return;
            }
            // 누르기
            var mv = InputReader.MovePressed();
            int press = mv == Vector2Int.zero ? -1 : Array.IndexOf(Dirs, mv);
            if (press < 0 && mv != Vector2Int.zero) press = mv.x < 0 ? 0 : mv.x > 0 ? 3 : mv.y < 0 ? 1 : 2;
            if (press >= 0)
            {
                keyDir = press; keyAt = t;
                for (int i = 0; i < notes.Count; i++)
                {
                    var n = notes[i];
                    if (n.judge != 0) continue;
                    float dt = Mathf.Abs(t - n.t);
                    if (dt > Good) break;   // 가장 앞 음표만 본다
                    Judge(i, n.dir != press ? 3 : dt <= Perfect ? 1 : 2, t);
                    break;
                }
            }
            // 지나간 음표는 놓침
            for (int i = 0; i < notes.Count; i++)
                if (notes[i].judge == 0 && t - notes[i].t > Good) Judge(i, 3, t);
            if (caught) return;
            if (notes.Count > 0 && notes[notes.Count - 1].judge != 0 && t > notes[notes.Count - 1].t + 0.3f)
            {
                won = true; endAt = t; Sfx.Play("success");
            }
        }

        void Judge(int i, int j, float t)
        {
            var n = notes[i]; n.judge = j; notes[i] = n;
            lastEventAt = t; lastDir = n.dir; lastJudge = j;
            if (j == 3)
            {
                combo = 0; gap -= 30f; Sfx.Play("bump");
                if (gap <= 4f) { caught = true; endAt = t; Sfx.Play("fail"); }
            }
            else
            {
                combo++; gap += j == 1 ? 9f : 6f;
                Sfx.Play(j == 1 ? "accept" : "move", j == 1 ? 1f : .7f);
            }
        }

        /// <summary>지금 맵을 찍어 장면 바탕으로 쓴다 (사람 그림은 잠깐 감춘다)</summary>
        void Snapshot()
        {
            var main = Camera.main; var pc = PlayerController.Instance;
            if (main == null || pc == null) return;
            const int w = 640, h = 176;   // 1칸 = 32px (2배)
            band = new RenderTexture(w, h, 16) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Mirror };
            var hidden = new List<Renderer>();
            foreach (var a in FindObjectsByType<CharacterAnimator>(FindObjectsSortMode.None))
                foreach (var r in a.transform.root.GetComponentsInChildren<Renderer>())
                    if (r.enabled) { r.enabled = false; hidden.Add(r); }
            foreach (var r in pc.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hidden.Add(r); }
            var go = new GameObject("BanditBandCam");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(main);
            cam.orthographic = true;
            cam.orthographicSize = h / 32f / 2f;
            cam.targetTexture = band;
            var pos = pc.transform.position;
            cam.transform.position = new Vector3(Mathf.Round(pos.x * 16f) / 16f, Mathf.Round((pos.y + 0.6f) * 16f) / 16f, main.transform.position.z);
            cam.Render();
            cam.targetTexture = null;
            Destroy(go);
            foreach (var r in hidden) if (r != null) r.enabled = true;
        }

        // ================= 그리기 =================
        static readonly Color32[] Col = { Hex(0xB07CF0), Hex(0x4FA3F7), Hex(0x3FB85A), Hex(0xF0555A) };
        static readonly Color32[] ColLo = { Hex(0x7E4FC4), Hex(0x2D6FC0), Hex(0x23853C), Hex(0xB8323A) };
        static readonly Color32[] ColHi = { Hex(0xDCC4FF), Hex(0xBFE0FF), Hex(0xBFF0C9), Hex(0xFFC2C4) };
        static readonly Color32 LaneBg = Hex(0x241A2C), LaneEdge = Hex(0x2C2136), Gold = Hex(0xFFCF33), Red = Hex(0xF0555A), Teal = Hex(0x2E8C86);
        static Color32 Hex(int v) => new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        static float Ease(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
        static float Back(float x) { x = Mathf.Clamp01(x); const float c = 2.2f; return 1f + (c + 1f) * Mathf.Pow(x - 1f, 3f) + c * Mathf.Pow(x - 1f, 2f); }

        void OnGUI()
        {
            if (phase == Phase.Closed || phase == Phase.Haggle) return;
            UIStyle.Begin();
            GUI.depth = -30;
            CuteUI.Dim();
            if (phase == Phase.Choose) DrawChoose(); else DrawRun();
            if (Event.current.isMouse || Event.current.type == EventType.ScrollWheel) Event.current.Use();
        }

        void DrawChoose()
        {
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            const float W = 340f, H = 150f;
            var r = new Rect(Mathf.Round((vw - W) / 2f), Mathf.Round((vh - H) / 2f), W, H);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 6, "도적");
            var face = new Rect(r.x + 14, r.y + 18, 66, 84);
            CuteUI.Box(face, 8, CuteUI.Paper2, CuteUI.Ink, 2);
            float bob = Mathf.Round(Mathf.Sin(Time.unscaledTime * 2.4f));
            if (banditFace != null) CuteUI.Sprite(banditFace, new Rect(face.center.x - 24, face.y + 8 + bob, 48, 72));
            float x = CuteUI.Label(r.x + 92, r.y + 30, "멈추시오! 통행세 ", CuteUI.Text);
            CuteUI.Label(x, r.y + 30, $"{toll}냥", CuteUI.Down, false, true);

            string[] title = { "통행세", "흥정", "도망" };
            string[] sub = { $"{toll}냥", "깎기", "짐 걸기" };
            for (int i = 0; i < 3; i++)
            {
                var c = new Rect(r.x + 92 + i * 80, r.y + 52, 74, 76);
                bool on = sel == i, off = i == 0 && !CanPay;
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && c.Contains(Event.current.mousePosition)) { sel = i; Pick(i); Event.current.Use(); return; }
                if (Event.current.type == EventType.MouseMove && c.Contains(Event.current.mousePosition)) sel = i;
                if (on) CuteUI.Box(new Rect(c.x - 2, c.y - 2, c.width + 4, c.height + 4), 9, Teal, Teal, 0);
                CuteUI.Box(c, 8, on ? new Color32(0xE3, 0xF4, 0xF2, 255) : CuteUI.White, CuteUI.Ink, 2);
                CuteUI.Keycap(c.x + 4, c.y + 4, (i + 1).ToString(), true);
                float icy = c.y + 22, icx = c.center.x;
                if (i == 0) CuteUI.Icon(CuteUI.Pic.Coin, icx - 9, icy - 9, 2);
                else if (i == 1) MiniScale(new Rect(icx - 14, icy - 5, 28, 10));
                else RunnerIcon(icx - 12, icy - 11);
                Color32 tc = off ? CuteUI.Zero : CuteUI.Text;
                CuteUI.LabelCenter(c.center.x, c.y + 46, title[i], tc, false, true);
                CuteUI.LabelCenter(c.center.x, c.y + 61, off ? "돈 모자람" : sub[i], off ? CuteUI.Down : CuteUI.Sub, true);
            }
        }

        static void MiniScale(Rect b)
        {
            CuteUI.Box(b, 3, CuteUI.White, CuteUI.Ink, 2);
            Color32[] cs = { Hex(0xD9D2CC), Hex(0xB7E4A8), Hex(0x3FB85A), Hex(0xFFCF33), Hex(0xF0555A) };
            float[] at = { 0f, .25f, .45f, .7f, .86f, 1f };
            float iw = b.width - 4;
            for (int k = 0; k < 5; k++) CuteUI.Fill(new Rect(b.x + 2 + Mathf.Round(iw * at[k]), b.y + 2, Mathf.Round(iw * at[k + 1]) - Mathf.Round(iw * at[k]), b.height - 4), cs[k]);
        }

        static readonly string[] Runner = { ".....###....", ".....###....", "....####....", "..##.####...", "....####.##.", "...##..##...", "..##....##..", "..#......##." };
        static void RunnerIcon(float x, float y)
        {
            for (int j = 0; j < Runner.Length; j++)
                for (int i = 0; i < Runner[j].Length; i++)
                    if (Runner[j][i] == '#') CuteUI.Fill(new Rect(x + i * 2, y + 3 + j * 2, 2, 2), CuteUI.Ink);
            CuteUI.Fill(new Rect(x - 2, y + 9, 4, 2), Hex(0x9FD0E6));
            CuteUI.Fill(new Rect(x - 4, y + 13, 6, 2), Hex(0x9FD0E6));
            CuteUI.Fill(new Rect(x - 2, y + 17, 4, 2), Hex(0x9FD0E6));
        }

        // ---- 도망 창 ----
        void DrawRun()
        {
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, t = RunT;
            const float W = 360f, H = 206f;
            var r = new Rect(Mathf.Round((vw - W) / 2f), Mathf.Round((vh - H) / 2f), W, H);
            CuteUI.Window(r);
            CuteUI.TitlePlate(r.center.x, r.y - 6, "도망");
            var scene = new Rect(r.x + 12, r.y + 18, W - 24, 92);
            var lane = new Rect(r.x + 12, r.y + 116, W - 24, 48);
            DrawScene(scene, t);
            DrawLane(lane, t);
            DrawKeys(r.center.x, lane.yMax + 8, t);
        }

        int Ev(float t, out float dt) { dt = t - lastEventAt; return dt < 9f ? lastJudge : 0; }

        void DrawScene(Rect s, float t)
        {
            CuteUI.Box(s, 6, CuteUI.Ink, CuteUI.Ink, 2);
            var inner = new Rect(s.x + 2, s.y + 2, s.width - 4, s.height - 4);
            bool end = endAt >= 0f; float et = end ? t - endAt : 0f;
            float scroll = end && won ? endAt * 70f + et * 20f : end ? endAt * 70f : t * 70f;
            if (Event.current.type == EventType.Repaint)
            {
                if (band != null)
                    GUI.DrawTextureWithTexCoords(inner, band, new Rect(scroll / band.width, (band.height - inner.height) / 2f / band.height, inner.width / band.width, inner.height / band.height));
                else CuteUI.Fill(inner, Hex(0x6E9A5C));
            }
            GUI.BeginGroup(inner);
            float w = inner.width, h = inner.height;
            // 속도선
            if (!end) for (int i = 0; i < 7; i++)
            {
                float sp = 180f + i * 23f, xx = w - Mathf.Repeat(t * sp + i * 97f, w + 40f);
                CuteUI.Fill(new Rect(Mathf.Round(xx), 12 + (i * 29) % 62, 8 + (i % 3) * 4, 1), new Color32(255, 255, 255, 140));
            }
            float ground = h - 12;
            int j = Ev(t, out float dt);
            bool missFx = j == 3 && dt < .35f;
            float shake = missFx ? Mathf.Round(Mathf.Sin(dt * 70f) * 2f * (1f - dt / .35f)) : 0f;
            float px = 188f, bx = px - gapShown;
            if (end && won) { px += Ease(et / 0.9f) * 170f; bx += Mathf.Min(30f, et * 25f); }
            if (end && caught) bx = Mathf.Lerp(bx, px - 14f, Ease(et / .25f));
            // 흙먼지
            if (!(end && caught)) for (int i = 0; i < 5; i++)
            {
                float a = Mathf.Repeat(t * 10f + i * 0.4f, 2f) / 2f;
                FillA(new Rect(Mathf.Round(px - 6 - a * 26), Mathf.Round(ground - 2 - a * 6 - (i % 2) * 3), a < .5f ? 3 : 2, a < .5f ? 3 : 2), new Color32(240, 228, 205, 255), .8f * (1f - a));
            }
            // 도적
            int bf = end ? (won && et > .5f ? 0 : Mathf.FloorToInt(t * 11f) % 4) : Mathf.FloorToInt(t * 11f) % 4;
            if (end && caught) bf = 0;
            DrawChar(banditRun[bf], bx, ground, (bf % 2 == 1) ? -2 : 0);
            if (!end)
            {
                bool near = gapShown < 45f; int pz = near && Mathf.FloorToInt(t * 8f) % 2 == 1 ? 1 : 0;
                var bub = new Rect(Mathf.Round(bx + 10) - pz, ground - 56 - pz, 12 + 2 * pz, 14 + 2 * pz);
                CuteUI.Box(bub, 3, CuteUI.White, CuteUI.Ink, 2);
                CuteUI.LabelCenter(bub.center.x, bub.center.y, "!", near ? CuteUI.Down : CuteUI.Ink, true, true);
            }
            else if (won) Outlined(bx, ground - 58, "헉헉", CuteUI.Sub, CuteUI.White, true);
            // 나
            int f = missFx || (end && caught) ? 2 : Mathf.FloorToInt(t * 12f) % 4;
            DrawChar(playerRun[f], px + shake, ground, f % 2 == 1 ? -2 : 0);
            if (missFx) FillA(new Rect(0, 0, w, h), new Color32(240, 60, 70, 255), .28f * (1f - dt / .35f));
            // 결승 점선
            CuteUI.Fill(new Rect(w - 20, 0, 20, h), new Color32(255, 250, 242, 90));
            for (float y = 0; y < h; y += 6) CuteUI.Fill(new Rect(w - 21, y, 2, 3), CuteUI.White);
            // 거리 막대
            float tx = 10, tw = w - 20, ty = 6;
            CuteUI.Box(new Rect(tx, ty, tw, 8), 4, CuteUI.Paper, CuteUI.Ink, 2);
            float last = notes.Count > 0 ? notes[notes.Count - 1].t + 0.3f : 1f;
            float p = Mathf.Clamp01((end ? endAt : t) / last);
            if (end && won) p = 1f;
            CuteUI.Fill(new Rect(tx + 2, ty + 2, Mathf.Round((tw - 4) * p), 4), Hex(0xCDEBD3));
            float bp = Mathf.Max(0f, p - gapShown / 520f);
            CuteUI.Box(new Rect(Mathf.Round(tx + 3 + bp * (tw - 6) - 5), ty - 1, 10, 10), 5, Red, CuteUI.Ink, 2);
            CuteUI.Box(new Rect(Mathf.Round(tx + 3 + p * (tw - 6) - 5), ty - 1, 10, 10), 5, Teal, CuteUI.Ink, 2);
            CuteUI.Fill(new Rect(tx + tw - 2, ty - 8, 2, 14), CuteUI.Ink); CuteUI.Fill(new Rect(tx + tw, ty - 8, 7, 5), Red); CuteUI.Fill(new Rect(tx + tw, ty - 3, 7, 1), CuteUI.Ink);
            GUI.EndGroup();
            // 끝 글자 (그룹 밖에서: 그룹 안에서 키우면 기준점이 어긋나 창 밖에 그려졌다)
            if (end && et > .15f)
            {
                float k = Back((et - .15f) / .35f);
                var c = new Vector2(inner.x + w / 2f, inner.y + h / 2f + 4);
                var m = GUI.matrix;
                CuteUI.ScaleAround(2f * k, c);
                Outlined(c.x, c.y, won ? "따돌렸다!" : "붙잡혔다!", won ? CuteUI.White : Hex(0xFFC2C4), CuteUI.Ink, false);
                GUI.matrix = m;
            }
        }

        static void DrawChar(Sprite s, float x, float ground, float lift)
        {
            if (s == null) return;
            CuteUI.Sprite(s, new Rect(Mathf.Round(x - 16), ground - 48 + lift, 32, 48));
        }

        static void Outlined(float cx, float cy, string s, Color32 col, Color32 line, bool small)
        {
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) if (x != 0 || y != 0) CuteUI.LabelCenter(cx + x, cy + y, s, line, small, true);
            CuteUI.LabelCenter(cx, cy, s, col, small, true);
        }

        void DrawLane(Rect l, float t)
        {
            bool end = endAt >= 0f;
            CuteUI.Box(l, 6, LaneBg, CuteUI.Ink, 2);
            var inner = new Rect(l.x + 2, l.y + 2, l.width - 4, l.height - 4);
            float tn = end ? endAt : t;   // 끝나면 음표·박자선은 그 자리에 멈춘다
            GUI.BeginGroup(inner);
            float w = inner.width, h = inner.height, rx = 28f, ry = h / 2f;
            CuteUI.Fill(new Rect(0, 0, w, 6), LaneEdge); CuteUI.Fill(new Rect(0, h - 6, w, 6), LaneEdge);
            CuteUI.Fill(new Rect(0, Mathf.Round(ry), w, 1), new Color32(255, 255, 255, 15));
            // 박자선 (음표와 함께 흐른다)
            if (notes.Count > 0)
            {
                float beat = 60f / (112f + 40f * GameManager.I.Player.LoadRatio);
                for (float b = Lead; b < notes[notes.Count - 1].t + 2f; b += beat)
                {
                    float x = Mathf.Round(rx + (b - tn) * speed);
                    if (x < 0 || x > w) continue;
                    bool bar = Mathf.RoundToInt((b - Lead) / beat) % 4 == 0;
                    CuteUI.Fill(new Rect(x, 1, bar ? 2 : 1, h - 2), bar ? new Color32(255, 207, 51, 56) : new Color32(255, 255, 255, 20));
                }
            }
            int j = Ev(t, out float dt);
            CuteUI.Fill(new Rect(rx - 17, 0, 34, h), new Color32(255, 255, 255, 18));
            bool hit = (j == 1 || j == 2) && dt < .3f;
            if (hit) FillA(new Rect(rx - 17, 0, 34, h), Col[lastDir], .35f * (1f - dt / .3f));
            // 판정 칸 (박자에 맞춰 숨쉰다)
            bool pulse = !end && t > 0.5f && Mathf.Repeat(t - Lead, 60f / (112f + 40f * GameManager.I.Player.LoadRatio)) < 0.07f;
            float rs = 28 + (pulse ? 2 : 0) + (hit && dt < .08f ? 4 : 0);
            var rr = new Rect(Mathf.Round(rx - rs / 2f), Mathf.Round(ry - rs / 2f), rs, rs);
            Frame(new Rect(rr.x - 1, rr.y - 1, rr.width + 2, rr.height + 2), 1, CuteUI.Ink);
            Frame(rr, 2, j == 3 && dt < .3f ? Red : hit && dt < .12f ? CuteUI.White : Hex(0xE8DDF0));
            Frame(new Rect(rr.x + 2, rr.y + 2, rr.width - 4, rr.height - 4), 1, CuteUI.Ink);
            // 음표
            if (!end || caught)
                foreach (var n in notes)
                {
                    float x = rx + (n.t - tn) * speed;
                    if (x > w + 14) break;
                    if (n.judge == 1 || n.judge == 2) continue;
                    if (n.judge == 3)
                    {
                        if (tn > n.t) Arrow(x, ry, n.dir, 2, Hex(0x8A7F90), Hex(0x5E5466), Hex(0xB4AABA), Mathf.Clamp01(1f - (tn - n.t) / .5f));
                        continue;
                    }
                    Arrow(x, ry, n.dir, 2, Col[n.dir], ColLo[n.dir], ColHi[n.dir], 1f);
                }
            // 맞힘: 퍼지는 네모 + 불꽃
            if (hit)
            {
                float k = Ease(dt / .3f);
                float s = Mathf.Round(28 + k * 24);
                FrameA(new Rect(Mathf.Round(rx - s / 2f), Mathf.Round(ry - s / 2f), s, s), 2, ColHi[lastDir], 1f - k);
                if (dt < .12f) Arrow(rx, ry, lastDir, 2, CuteUI.White, CuteUI.White, CuteUI.White, 1f - dt / .12f);
                for (int i = 0; i < 10; i++)
                {
                    float ang = i / 10f * Mathf.PI * 2f + lastEventAt, rad = 6 + k * 26;
                    float sx = rx + Mathf.Cos(ang) * rad, sy = ry + Mathf.Sin(ang) * rad * .8f + k * k * 6;
                    int z = i % 3 == 0 ? 3 : 2;
                    FillA(new Rect(Mathf.Round(sx), Mathf.Round(sy), z, z), i % 2 == 1 ? Col[lastDir] : CuteUI.White, 1f - k);
                }
            }
            // 처음 안내
            if (!end && t < Lead - 0.4f) CuteUI.LabelCenter(w / 2f - 10, ry, "칸에 닿을 때 같은 방향키", new Color32(0xC9, 0xB8, 0xD2, 255), true);
            GUI.EndGroup();
            // 오른쪽 끝 판 (음표가 나오는 곳) + 연속
            var pan = new Rect(l.xMax - 46, l.y, 46, l.height);
            CuteUI.Box(pan, 6, Hex(0x1A1220), CuteUI.Ink, 2);
            if (!end && combo >= 2)
            {
                float k = (j == 1 || j == 2) ? Back(dt / .15f) : 1f;
                var m = GUI.matrix;
                var c = new Vector2(pan.center.x, pan.center.y - 5);
                CuteUI.ScaleAround(1.6f * k, c);
                Outlined(c.x, c.y, combo.ToString(), CuteUI.White, CuteUI.Ink, false);
                GUI.matrix = m;
                CuteUI.LabelCenter(pan.center.x, pan.center.y + 12, "연속", new Color32(0xC9, 0xB8, 0xD2, 255), true);
            }
            // 판정 글자 (판정 칸 위)
            if (!end && j != 0 && dt < .55f)
            {
                string s = j == 1 ? "딱!" : j == 2 ? "좋아" : "놓침";
                Color32 c = j == 1 ? Gold : j == 2 ? Hex(0xBFF0C9) : Hex(0xFF9AA0);
                float yy = l.y - 10 - Mathf.Round(Back(dt / .18f) * 6f);
                var gc = GUI.color; if (dt > .4f) GUI.color = new Color(gc.r, gc.g, gc.b, 1f - (dt - .4f) / .15f);
                Outlined(inner.x + rx + 4, yy, s, c, CuteUI.Ink, false);
                GUI.color = gc;
            }
            if (end && won)
            {
                CuteUI.Fill(l, new Color32(36, 26, 44, 150));
                Outlined(l.center.x, l.center.y, "짐도 돈도 그대로", CuteUI.White, CuteUI.Ink, false);
            }
        }

        void DrawKeys(float cx, float y, float t)
        {
            const float kw = 22, gapK = 4;
            float x0 = Mathf.Round(cx - (4 * kw + 3 * gapK) / 2f);
            for (int i = 0; i < 4; i++)
            {
                float x = x0 + i * (kw + gapK);
                bool on = keyDir == i && t - keyAt < .12f && endAt < 0f;
                if (!on) CuteUI.Box(new Rect(x, y + 2, kw, 18), 4, CuteUI.KeyLine, CuteUI.KeyLine, 0);
                CuteUI.Box(new Rect(x, y + (on ? 2 : 0), kw, 18), 4, on ? Col[i] : CuteUI.White, CuteUI.Ink, 2);
                var g = on ? CuteUI.White : CuteUI.Sub;
                Arrow(x + kw / 2f, y + 9 + (on ? 2 : 0), i, 1, g, g, g, 1f, false);
            }
        }

        static void Frame(Rect r, float s, Color32 c)
        {
            CuteUI.Fill(new Rect(r.x, r.y, r.width, s), c); CuteUI.Fill(new Rect(r.x, r.yMax - s, r.width, s), c);
            CuteUI.Fill(new Rect(r.x, r.y, s, r.height), c); CuteUI.Fill(new Rect(r.xMax - s, r.y, s, r.height), c);
        }

        // 화살표 9×9 (위쪽). 테두리·윗빛·아랫그늘·그림자
        static readonly string[] ArrowUp = { "....#....", "...###...", "..#####..", ".#######.", "#########", "...###...", "...###...", "...###...", "...###..." };
        static readonly Dictionary<int, bool[,]> masks = new Dictionary<int, bool[,]>();
        static bool[,] Mask(int dir)
        {
            if (masks.TryGetValue(dir, out var m)) return m;
            const int n = 9; m = new bool[n, n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int sx, sy;
                    switch (dir) { case 2: sx = x; sy = y; break; case 1: sx = x; sy = n - 1 - y; break; case 0: sx = y; sy = x; break; default: sx = y; sy = n - 1 - x; break; }
                    m[x, y] = ArrowUp[sy][sx] == '#';
                }
            masks[dir] = m; return m;
        }

        static readonly Dictionary<string, Texture2D> arrowTex = new Dictionary<string, Texture2D>();
        /// <summary>화살표 한 장을 텍스처로 만들어 둔다 (테두리 1칸 · 아래 그림자 1칸 포함 11×12)</summary>
        static Texture2D ArrowTex(int dir, Color32 mid, Color32 lo, Color32 hi, bool shadow)
        {
            string key = $"{dir}{mid.r:x2}{mid.g:x2}{mid.b:x2}{lo.r:x2}{lo.g:x2}{hi.r:x2}{hi.b:x2}{(shadow ? 1 : 0)}";
            if (arrowTex.TryGetValue(key, out var tx) && tx != null) return tx;
            var m = Mask(dir); const int n = 9, TW = n + 2, TH = n + 3;
            bool At(int x, int y) => x >= 0 && y >= 0 && x < n && y < n && m[x, y];
            bool Body(int x, int y) => At(x, y) || At(x - 1, y) || At(x + 1, y) || At(x, y - 1) || At(x, y + 1);
            var px = new Color32[TW * TH];
            void Set(int x, int y, Color32 c) { px[(TH - 1 - (y + 1)) * TW + (x + 1)] = c; }
            for (int y = -1; y <= n + 1; y++) for (int x = -1; x <= n; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                if (At(x, y))
                {
                    c = mid;
                    if (!At(x, y + 1) || !At(x + 1, y)) c = lo;
                    if (!At(x, y - 1) || !At(x - 1, y)) c = hi;
                }
                else if (y <= n && Body(x, y)) c = CuteUI.Ink;
                else if (shadow && Body(x, y - 1)) c = new Color32(0, 0, 0, 115);
                Set(x, y, c);
            }
            tx = new Texture2D(TW, TH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tx.SetPixels32(px); tx.Apply();
            arrowTex[key] = tx; return tx;
        }

        static void Arrow(float cx, float cy, int dir, int s, Color32 mid, Color32 lo, Color32 hi, float alpha, bool shadow = true)
        {
            if (alpha <= 0f || Event.current.type != EventType.Repaint) return;
            const int n = 9;
            float ox = Mathf.Round(cx - n * s / 2f), oy = Mathf.Round(cy - n * s / 2f);
            var gc = GUI.color; GUI.color = new Color(gc.r, gc.g, gc.b, gc.a * alpha);
            GUI.DrawTexture(new Rect(ox - s, oy - s, (n + 2) * s, (n + 3) * s), ArrowTex(dir, mid, lo, hi, shadow));
            GUI.color = gc;
        }

        /// <summary>투명도는 GUI.color로 (색마다 텍스처가 늘지 않게)</summary>
        static void FillA(Rect r, Color32 c, float alpha)
        {
            if (alpha <= 0f) return;
            var gc = GUI.color; GUI.color = new Color(gc.r, gc.g, gc.b, gc.a * Mathf.Clamp01(alpha));
            CuteUI.Fill(r, c); GUI.color = gc;
        }
        static void FrameA(Rect r, float s, Color32 c, float alpha)
        {
            var gc = GUI.color; GUI.color = new Color(gc.r, gc.g, gc.b, gc.a * Mathf.Clamp01(alpha));
            Frame(r, s, c); GUI.color = gc;
        }
    }
}
