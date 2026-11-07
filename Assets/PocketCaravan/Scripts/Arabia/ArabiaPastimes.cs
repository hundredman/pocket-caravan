using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 미니게임 3개 (2026-11-05, 기우·통합. 아라비아 담당이 다듬어도 됨). 전부 창(OnGUI) 안에서 하고, 자리는 맵을 열 때 세운다.
    /// 1) 대추야자 따기 (알 아흐사 농장): ←→로 바구니를 옮겨 떨어지는 열매 받기, 20초. 3개마다 대추야자 1개(최대 5개), 하루 한 번 보상.
    /// 2) 낙타 경주 (제다 성 밖): 바늘이 초록 구간에 올 때 Space, 세 마리와 30초 경주. 1등 30냥·2등 10냥, 하루 한 번 보상.
    /// 3) 모래바람 짐 지키기 (지름길): 바람이 잦아들면 Space 연타로 나아가고, "바람!"일 땐 아무것도 누르지 않기. 25초 안에 도착하면 25냥, 누르면 짐이 날아감(점수만).
    /// 저장: 공용 메모 "arabia:pastime:이름:day"(오늘 보상 받았는지), "arabia:pastime:이름:best".
    /// </summary>
    public static class ArabiaPastimes
    {
        static int Day => GameManager.I != null ? GameManager.I.Clock.day : 0;
        public static bool DoneToday(string id) => GameManager.I != null && Mathf.RoundToInt(GameManager.I.GetMemo($"arabia:pastime:{id}:day", -1f)) == Day;
        static void MarkToday(string id) => GameManager.I?.SetMemo($"arabia:pastime:{id}:day", Day);
        public static int Best(string id) => GameManager.I == null ? 0 : Mathf.RoundToInt(GameManager.I.GetMemo($"arabia:pastime:{id}:best", 0f));
        static void SetBest(string id, int score) { if (GameManager.I != null && score > Best(id)) GameManager.I.SetMemo($"arabia:pastime:{id}:best", score); }

        // ---------------- 공통 창 (11/07 기우: UI B안 종이 창 + 은인 그림으로 다시 그림) ----------------

        static readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        /// <summary>Resources/Arabia/Pastimes/* (Art/Arabia/Sprites/v1 복사본)</summary>
        public static Sprite Art(string name)
        {
            if (!art.TryGetValue(name, out var sp) || sp == null) art[name] = sp = Resources.Load<Sprite>("Arabia/Pastimes/" + name);
            return sp;
        }
        /// <summary>원본 픽셀 크기 그대로(scale배) 그리기. 기준점은 왼쪽 아래</summary>
        static void Pic(string name, float x, float bottom, float scale = 1f, bool flipX = false)
        {
            var sp = Art(name); if (sp == null) return;
            float w = sp.rect.width * scale, h = sp.rect.height * scale;
            CuteUI.Sprite(sp, new Rect(x, bottom - h, w, h), new Rect(0, 0, 1, 1), flipX);
        }

        public abstract class Game : Interactable
        {
            public abstract string Id { get; }
            public abstract string Title { get; }
            public abstract string Where { get; }
            public abstract string Hint { get; }
            protected bool open; protected int openedFrame; protected float t; protected string result = "";
            protected bool finished; protected float finishedAt;
            // 떠오르는 글씨 (+1 같은 것)
            protected readonly List<(string text, Vector2 at, float born, Color32 col)> pops = new List<(string, Vector2, float, Color32)>();
            public override Kind Category => Kind.Board;
            void Awake() { blocksMovement = false; }
            public override void Interact(PlayerController player)
            {
                if (open || !UIState.TryOpen(this, Close)) return;   // 11/07 새 창 잠금 API (다른 창이 열려 있으면 안 열림)
                open = true; openedFrame = Time.frameCount; t = 0f; finished = false; result = ""; pops.Clear();
                Begin();
            }
            protected abstract void Begin();
            protected abstract void Tick(float dt);
            protected abstract void Draw(Rect r);
            protected virtual string Status => "";
            protected void Pop(string text, Vector2 at, Color32 col) => pops.Add((text, at, Time.unscaledTime, col));
            protected void Close() { if (!open) return; open = false; UIState.Release(this); }
            protected override void OnDisable() { Close(); base.OnDisable(); }
            protected void Finish() { finished = true; finishedAt = Time.unscaledTime; }
            void Update()
            {
                if (!open || Time.frameCount == openedFrame) return;
                if (InputReader.CancelPressed()) { Close(); return; }
                if (!finished) { float dt = Time.unscaledDeltaTime; t += dt; Tick(dt); }
                else if (InputReader.ConfirmPressed() && Time.unscaledTime - finishedAt > .3f) Close();
            }
            void OnGUI()
            {
                if (!open) return;
                UIStyle.Begin();
                float w = Mathf.Min(380f, UIStyle.VirtualWidth - 20f), h = 250f;
                var r = new Rect(Mathf.Round((UIStyle.VirtualWidth - w) * .5f), Mathf.Round((UIStyle.VirtualHeight - h) * .5f), w, h);
                CuteUI.Dim();
                CuteUI.Window(r);
                CuteUI.TitlePlate(r.center.x, r.y - 11, Title);
                CuteUI.LabelCenter(r.center.x, r.y + 22, Where, CuteUI.Sub, true);
                GUI.Label(new Rect(r.x + 14, r.y + 32, w - 28, 30), Hint, CuteUI.PaperWrap);
                var play = new Rect(r.x + 14, r.y + 64, w - 28, h - 118);
                CuteUI.Box(play, 4, CuteUI.Paper2, CuteUI.Line, 1);
                GUI.BeginGroup(play);
                Draw(new Rect(0, 0, play.width, play.height));
                // 떠오르는 글씨
                for (int i = pops.Count - 1; i >= 0; i--)
                {
                    float age = Time.unscaledTime - pops[i].born;
                    if (age > .8f) { pops.RemoveAt(i); continue; }
                    CuteUI.LabelCenter(pops[i].at.x, pops[i].at.y - age * 22f, pops[i].text, pops[i].col, true, true);
                }
                GUI.EndGroup();
                CuteUI.Label(r.x + 16, r.y + h - 36, Status, CuteUI.Sub, true);
                if (finished)
                {
                    // 결과 띠
                    var band = new Rect(r.x + 14, r.y + 64 + (h - 118) / 2f - 18f, w - 28, 36);
                    CuteUI.Box(band, 6, CuteUI.Paper, CuteUI.Ink, 1, 2);
                    CuteUI.WrapCenter(band.x + 8, band.y + 4, band.width - 16, result, CuteUI.Text);
                }
                if (CuteUI.Button(new Rect(r.x + w - 112, r.y + h - 30, 98, 22), finished ? "닫기 · Space" : "그만 · Esc")) Close();
            }
        }

        // ---------------- 1) 대추야자 따기 ----------------

        public class DatePicking : Game
        {
            public override string Id => "dates";
            public override string DisplayName => "대추야자 따기";
            public override string Title => "대추야자 따기";
            public override string Where => "알 아흐사 농장";
            public override string Hint => "← → 로 항아리를 옮겨 떨어지는 열매를 받으세요. 20초, 3개마다 대추야자 하나.";
            const int Lanes = 5; const float Length = 20f;
            int lane, caught, missed, streak; float nextDrop, shake;
            readonly List<(int lane, float y, float spin)> drops = new List<(int, float, float)>();
            readonly List<(int lane, float born)> splats = new List<(int, float)>();
            protected override string Status => $"남은 {Mathf.Max(0, Length - t):0}초 · 받음 {caught} · 놓침 {missed} · 최고 {Best(Id)}";
            protected override void Begin() { lane = 2; caught = missed = streak = 0; drops.Clear(); splats.Clear(); nextDrop = .4f; }
            protected override void Tick(float dt)
            {
                var mv = InputReader.MovePressed();
                lane = Mathf.Clamp(lane + mv.x, 0, Lanes - 1);
                nextDrop -= dt; shake = Mathf.Max(0f, shake - dt * 4f);
                if (nextDrop <= 0f) { drops.Add((UnityEngine.Random.Range(0, Lanes), 0f, UnityEngine.Random.value)); nextDrop = Mathf.Lerp(.9f, .45f, t / Length); }
                float speed = Mathf.Lerp(.55f, .9f, t / Length);
                for (int i = drops.Count - 1; i >= 0; i--)
                {
                    var d = drops[i]; d.y += speed * dt; drops[i] = d;
                    if (d.y < 1f) continue;
                    drops.RemoveAt(i);
                    if (d.lane == lane) { caught++; streak++; Sfx.Play("coin", .35f); Pop(streak >= 3 ? $"+1  ×{streak}" : "+1", LanePos(d.lane, 0.78f), CuteUI.Up); }
                    else { missed++; streak = 0; shake = 1f; splats.Add((d.lane, Time.unscaledTime)); Sfx.Play("step", .4f); }
                }
                if (t >= Length) End();
            }
            Vector2 LanePos(int l, float v) => new Vector2((l + .5f) * (lastW / Lanes), v * lastH);
            float lastW = 300f, lastH = 120f;
            void End()
            {
                Finish(); SetBest(Id, caught);
                var gm = GameManager.I; var dates = gm.ItemById("arabia_dates");
                int n = Mathf.Min(5, caught / 3);
                if (n > 0 && !DoneToday(Id) && dates != null && gm.Player.CanCarry(dates, n)) { gm.Player.cargo.Add(dates, n, 0f); MarkToday(Id); result = $"{caught}개를 받았다! 농부가 대추야자 {n}개를 나눠 줬다."; Sfx.Play("success", .6f); }
                else if (n > 0 && DoneToday(Id)) result = $"{caught}개를 받았다. (오늘 몫은 이미 받음)";
                else if (n > 0) result = $"{caught}개를 받았다. 짐이 가득 차서 못 받았다.";
                else result = $"{caught}개… 내일 다시 해 보자.";
            }
            protected override void Draw(Rect r)
            {
                lastW = r.width; lastH = r.height;
                float lw = r.width / Lanes;
                // 하늘·모래 바닥
                CuteUI.Fill(new Rect(0, 0, r.width, r.height), new Color32(246, 232, 200, 255));
                CuteUI.Fill(new Rect(0, r.height - 16, r.width, 16), new Color32(226, 196, 140, 255));
                // 야자나무 다섯 그루 (줄마다 하나, 열매가 떨어지는 곳)
                for (int l = 0; l < Lanes; l++) Pic("nature_datePalm", (l + .5f) * lw - 20, 48, 1f);
                for (int l = 1; l < Lanes; l++) CuteUI.Fill(new Rect(Mathf.Round(l * lw), 40, 1, r.height - 56), new Color32(236, 211, 176, 120));
                // 떨어진 열매 (놓친 것)
                for (int i = splats.Count - 1; i >= 0; i--)
                {
                    float age = Time.unscaledTime - splats[i].born;
                    if (age > 1.2f) { splats.RemoveAt(i); continue; }
                    Pic("prop_dates", (splats[i].lane + .5f) * lw - 7, r.height - 6, .6f);
                }
                // 떨어지는 열매
                foreach (var d in drops)
                {
                    float y = 26 + d.y * (r.height - 26 - 22);
                    float wob = Mathf.Sin((t + d.spin) * 9f) * 2f;
                    Pic("prop_dates", (d.lane + .5f) * lw - 8 + wob, y + 16, .66f);
                }
                // 항아리 (흔들림 = 놓쳤을 때)
                float bx = (lane + .5f) * lw - 12 + Mathf.Sin(Time.unscaledTime * 40f) * shake * 2f;
                Pic("prop_zir", bx, r.height - 4, 1f);
            }
        }

        // ---------------- 2) 낙타 경주 ----------------

        public class CamelRace : Game
        {
            public override string Id => "camel";
            public override string DisplayName => "낙타 경주";
            public override string Title => "낙타 경주";
            public override string Where => "제다 성 밖 모래밭";
            public override string Hint => "바늘이 초록 구간에 올 때 Space! 맞히면 낙타가 달리고, 틀리면 주춤합니다. 30초.";
            const float Length = 30f, Track = 100f;
            readonly float[] pos = new float[4]; readonly float[] gallop = new float[4];
            float target, window, stumble; int hits, miss;
            float Needle => .5f + .48f * Mathf.Sin(t * 3.2f);
            static readonly string[] Names = { "나", "자이드", "라일라", "오마르" };
            protected override string Status => $"남은 {Mathf.Max(0, Length - t):0}초 · 맞힘 {hits} · 틀림 {miss} · 최고 {(Best(Id) > 0 ? (5 - Best(Id)) + "등" : "-")}";
            protected override void Begin() { Array.Clear(pos, 0, 4); Array.Clear(gallop, 0, 4); hits = miss = 0; target = .6f; window = .12f; stumble = 0f; }
            protected override void Tick(float dt)
            {
                if (InputReader.ConfirmPressed())
                {
                    bool ok = Mathf.Abs(Needle - target) <= window;
                    if (ok) { pos[0] += 9f; gallop[0] = 1f; hits++; target = UnityEngine.Random.Range(.2f, .8f); window = Mathf.Max(.08f, window - .004f); Sfx.Play("step", .5f); Pop("달려!", new Vector2(60 + Mathf.Clamp01(pos[0] / Track) * (lastW - 100), 18), CuteUI.Up); }
                    else { pos[0] = Mathf.Max(0f, pos[0] - 2f); miss++; stumble = .5f; Sfx.Play("coin", .3f); Pop("주춤…", new Vector2(60 + Mathf.Clamp01(pos[0] / Track) * (lastW - 100), 18), CuteUI.Alert); }
                }
                stumble = Mathf.Max(0f, stumble - dt);
                pos[0] = Mathf.Max(0f, pos[0] + dt * 1.2f); gallop[0] = Mathf.Max(0f, gallop[0] - dt * 1.5f);
                for (int i = 1; i < 4; i++) { pos[i] += dt * (2.6f + i * .35f + Mathf.Sin(t * (1.1f + i)) * .8f); gallop[i] = 1f; }
                if (pos[0] >= Track || t >= Length) End();
            }
            float lastW = 300f;
            void End()
            {
                Finish();
                int place = 1; for (int i = 1; i < 4; i++) if (pos[i] > pos[0]) place++;
                SetBest(Id, 5 - place);
                int prize = place == 1 ? 30 : place == 2 ? 10 : 0;
                if (prize > 0 && !DoneToday(Id)) { GameManager.I.Player.money += prize; MarkToday(Id); result = $"{place}등! 상금 {prize}냥."; Sfx.Play("levelup", .6f); }
                else if (prize > 0) result = $"{place}등! (오늘 상금은 이미 받음)";
                else result = $"{place}등… 바늘을 더 잘 보자.";
            }
            protected override void Draw(Rect r)
            {
                lastW = r.width;
                float laneH = (r.height - 34f) / 4f;
                CuteUI.Fill(new Rect(0, 0, r.width, r.height), new Color32(243, 224, 184, 255));
                // 뒤 모래언덕
                for (float x = -10; x < r.width; x += 60) Pic("nature_dune", x, 14, 1f);
                // 결승선
                float finishX = 44 + (r.width - 100);
                for (int k = 0; k < (int)((r.height - 34f) / 6f); k++) CuteUI.Fill(new Rect(finishX + 28, 2 + k * 6, 4, 3), k % 2 == 0 ? CuteUI.Ink : CuteUI.Paper);
                for (int i = 0; i < 4; i++)
                {
                    float y = i * laneH;
                    CuteUI.Fill(new Rect(0, y + laneH - 1, r.width, 1), new Color32(226, 196, 140, 255));
                    CuteUI.Label(2, y + laneH / 2f, Names[i], i == 0 ? CuteUI.Ink : CuteUI.Sub, true, i == 0);
                    float x = 44 + Mathf.Clamp01(pos[i] / Track) * (r.width - 100);
                    float bob = gallop[i] > 0f ? Mathf.Abs(Mathf.Sin((t + i) * 14f)) * 3f : 0f;
                    bool tripping = i == 0 && stumble > 0f;
                    Pic(tripping ? "animal_camelRest" : "animal_camel", x, y + laneH - 2 - bob, laneH / 44f * 1.15f);
                    if (gallop[i] > 0f && !tripping) for (int d = 0; d < 3; d++) CuteUI.Fill(new Rect(x - 4 - d * 5 - (t * 30f % 5f), y + laneH - 6 - d, 3, 2), new Color32(226, 196, 140, 200));
                }
                // 바늘 띠
                var bar = new Rect(40, r.height - 24, r.width - 48, 16);
                CuteUI.Box(bar, 3, CuteUI.Paper, CuteUI.Line, 1);
                CuteUI.Fill(new Rect(bar.x + (target - window) * bar.width, bar.y + 2, 2 * window * bar.width, bar.height - 4), CuteUI.Interact);
                CuteUI.Fill(new Rect(bar.x + Needle * bar.width - 2, bar.y - 3, 4, bar.height + 6), CuteUI.Ink);
                CuteUI.Keycap(4, r.height - 22, "Space");
            }
        }

        // ---------------- 3) 모래바람 짐 지키기 ----------------

        public class SandstormRun : Game
        {
            public override string Id => "storm";
            public override string DisplayName => "모래바람 건너기";
            public override string Title => "모래바람 짐 지키기";
            public override string Where => "룹알할리 지름길";
            public override string Hint => "잠잠할 땐 Space 연타로 나아가고, \"바람!\"일 땐 아무 키도 누르지 마세요. 25초.";
            const float Length = 25f, Goal = 60f;
            float dist, gustAge; bool gust; int lost; float calmLeft, step;
            readonly List<(float x, float born)> flying = new List<(float, float)>();
            protected override string Status => $"남은 {Mathf.Max(0, Length - t):0}초 · 짐 날아감 {lost} · 최고 {Best(Id)}";
            protected override void Begin() { dist = 0f; gust = false; lost = 0; calmLeft = 3f; step = 0f; flying.Clear(); ArabiaAmbience.Current?.StartGust(4f); }
            protected override void Tick(float dt)
            {
                calmLeft -= dt; gustAge += dt;
                if (calmLeft <= 0f)
                {
                    gust = !gust; gustAge = 0f;
                    calmLeft = gust ? UnityEngine.Random.Range(1.2f, 2.2f) : UnityEngine.Random.Range(2f, 3.5f);
                    if (gust) { ArabiaAmbience.Current?.StartGust(calmLeft + 1f); Sfx.Play("step", .3f); }
                }
                bool pressed = InputReader.ConfirmPressed() || InputReader.MovePressed() != Vector2Int.zero;
                if (pressed)
                {
                    if (gust) { lost++; dist = Mathf.Max(0f, dist - 4f); flying.Add((CamelX(), Time.unscaledTime)); Sfx.Play("coin", .3f); Pop("짐이!", new Vector2(CamelX() + 20, 30), CuteUI.Alert); }
                    else { dist += 3f; step = .25f; }
                }
                step = Mathf.Max(0f, step - dt);
                if (dist >= Goal || t >= Length) End();
            }
            float lastW = 300f;
            float CamelX() => 8 + Mathf.Clamp01(dist / Goal) * (lastW - 110);
            void End()
            {
                Finish();
                bool arrived = dist >= Goal;
                int score = arrived ? Mathf.Max(1, 10 - lost) : 0;
                SetBest(Id, score);
                if (arrived && !DoneToday(Id)) { GameManager.I.Player.money += 25; MarkToday(Id); result = $"건넜다! 짐 {lost}번 날아감. 대상 숙소에서 25냥을 받았다."; Sfx.Play("levelup", .6f); }
                else if (arrived) result = $"건넜다! 짐 {lost}번 날아감. (오늘 몫은 이미 받음)";
                else result = $"시간 안에 못 건넜다 ({dist:0}/{Goal:0}). 잠잠할 때 더 빨리!";
            }
            protected override void Draw(Rect r)
            {
                lastW = r.width;
                CuteUI.Fill(new Rect(0, 0, r.width, r.height), gust ? new Color32(222, 190, 130, 255) : new Color32(243, 226, 186, 255));
                for (float x = -20; x < r.width; x += 70) Pic("nature_dune", x + (gust ? Mathf.Sin(Time.unscaledTime * 30f) : 0f), r.height - 28, 1f);
                Pic("nature_deadwood", r.width * .35f, r.height - 20, 1f);
                Pic("building_tent", r.width - 70, r.height - 10, 1f);   // 목적지 대상 숙소
                // 길
                CuteUI.Fill(new Rect(0, r.height - 18, r.width, 6), new Color32(214, 180, 120, 255));
                // 낙타 (걸을 때 흔들)
                float bob = step > 0f ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 16f)) * 3f : 0f;
                Pic("animal_camel", CamelX(), r.height - 12 - bob, .9f);
                // 날아가는 짐
                for (int i = flying.Count - 1; i >= 0; i--)
                {
                    float age = Time.unscaledTime - flying[i].born;
                    if (age > .9f) { flying.RemoveAt(i); continue; }
                    Pic("prop_spiceSack", flying[i].x + 20 + age * 90f, r.height - 40 - age * 70f + age * age * 40f, .6f);
                }
                // 바람 줄기
                if (gust)
                {
                    for (int i = 0; i < 14; i++)
                    {
                        float yy = 6 + (i * 37) % (int)(r.height - 24);
                        float xx = r.width - ((Time.unscaledTime * (160f + i * 23f) + i * 53f) % (r.width + 60f)) ;
                        CuteUI.Fill(new Rect(xx, yy, 18 + i % 3 * 8, 2), new Color32(255, 244, 214, 200));
                    }
                }
                CuteUI.LabelCenter(r.width / 2f, 12, gust ? "바람!  멈추세요" : "잠잠함 — Space 연타", gust ? CuteUI.Alert : CuteUI.Interact, false, true);
            }
        }

        // ---------------- 자리 ----------------

        struct Place { public string map; public int x, y; public Type game; public string label; }
        static readonly Place[] Places =
        {
            new Place { map = "arabia_ahsa", x = 20, y = 38, game = typeof(DatePicking) },   // 대추야자 농부 옆
            new Place { map = "arabia_jeddah", x = 44, y = 21, game = typeof(CamelRace) },   // 수레 장인 유수프 옆
            new Place { map = "arabia_shortcut", x = -1, y = -1, game = typeof(SandstormRun) }, // 길 가운데 (자동)
        };

        static Vector3? Cell(MapRoot mr, string map, int x, int y)
        {
            var info = WorldIndex.Get()?.Map(map);
            if (info == null) return null;
            int h = info.height > 0 ? info.height : 44;
            var c = new Vector3Int(x, h - 1 - y, 0);
            int[] dx = { 0, 1, -1, 0, 0, 1, -1, 2, -2 }, dy = { 0, 0, 0, 1, -1, 1, -1, 0, 0 };
            for (int i = 0; i < dx.Length; i++)
            {
                var cell = new Vector3Int(c.x + dx[i], c.y + dy[i], 0);
                if (mr.IsWalkable(cell) && Interactable.At(cell) == null) return mr.CellCenter(cell);
            }
            return null;
        }

        static Vector3? RoadCenter(MapRoot mr)
        {
            if (mr.ground == null) return null;
            var b = mr.ground.cellBounds; Vector2 center = new Vector2(b.center.x, b.center.y);
            float best = float.MaxValue; Vector3Int bc = default; bool any = false;
            var tm = mr.road != null ? mr.road : mr.ground;
            foreach (var p in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(p) || !mr.IsWalkable(p) || Interactable.At(p) != null) continue;
                float d = (center - new Vector2(p.x, p.y)).sqrMagnitude;
                if (d < best) { best = d; bc = p; any = true; }
            }
            return any ? mr.CellCenter(bc) : (Vector3?)null;
        }

        class Runner : MonoBehaviour
        {
            void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
            void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
            void OnLoaded(Scene s, LoadSceneMode m)
            {
                var mr = MapRoot.Current;
                if (mr == null || GameManager.I == null) return;
                foreach (var p in Places)
                {
                    if (p.map != mr.mapId) continue;
                    var pos = p.x >= 0 ? Cell(mr, p.map, p.x, p.y) : RoadCenter(mr);
                    if (pos == null) { Debug.LogWarning($"[아라비아 놀거리] {p.map}: 자리가 없다"); continue; }
                    var go = new GameObject("아라비아 놀거리");
                    go.transform.position = pos.Value;
                    go.AddComponent(p.game);
                    var t = Resources.Load<Texture2D>("Common/signpost");
                    if (t != null) { t.filterMode = FilterMode.Point; var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.1f), 16f); sr.sortingOrder = 9; }
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            ActivityHints.Rules[typeof(DatePicking)] = _ => ("대추야자 따기", DoneToday("dates"));
            ActivityHints.Rules[typeof(CamelRace)] = _ => ("낙타 경주", DoneToday("camel"));
            ActivityHints.Rules[typeof(SandstormRun)] = _ => ("모래바람 건너기", DoneToday("storm"));
            if (UnityEngine.Object.FindFirstObjectByType<Runner>() != null) return;
            var go = new GameObject("ArabiaPastimes");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }
    }
}
