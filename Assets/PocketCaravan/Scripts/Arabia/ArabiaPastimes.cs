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

        // ---------------- 공통 창 ----------------

        public abstract class Game : Interactable
        {
            public abstract string Id { get; }
            public abstract string Title { get; }
            public abstract string Hint { get; }
            protected bool open; protected int openedFrame; protected float t; protected string result = "";
            protected bool finished;
            public override Kind Category => Kind.Board;
            void Awake() { blocksMovement = false; }
            public override void Interact(PlayerController player)
            {
                if (open || UIState.Blocking) return;
                open = true; openedFrame = Time.frameCount; t = 0f; finished = false; result = ""; UIState.Blocking = true;
                Begin();
            }
            protected abstract void Begin();
            protected abstract void Tick(float dt);
            protected abstract void Draw(Rect r);
            protected void Close() { if (!open) return; open = false; UIState.Release(); }
            protected override void OnDisable() { Close(); base.OnDisable(); }
            void Update()
            {
                if (!open || Time.frameCount == openedFrame) return;
                if (InputReader.CancelPressed()) { Close(); return; }
                if (!finished) { float dt = Time.unscaledDeltaTime; t += dt; Tick(dt); }
                else if (InputReader.ConfirmPressed()) Close();
            }
            void OnGUI()
            {
                if (!open) return;
                UIStyle.Begin();
                float w = Mathf.Min(360f, UIStyle.VirtualWidth - 20f), h = 230f;
                var r = new Rect((UIStyle.VirtualWidth - w) * .5f, 80, w, h);
                GUI.Box(r, "", UIStyle.Box);
                GUI.Label(new Rect(r.x + 14, r.y + 8, w - 28, 22), Title, UIStyle.Center);
                GUI.Label(new Rect(r.x + 14, r.y + 30, w - 28, 30), Hint, UIStyle.Wrap);
                Draw(new Rect(r.x + 14, r.y + 64, w - 28, h - 110));
                GUI.Label(new Rect(r.x + 14, r.y + h - 46, w - 28 - 104, 30), result, UIStyle.Wrap);   // 11/07 오른쪽 단추 자리 비움
                if (GUI.Button(new Rect(r.x + w - 112, r.y + h - 28, 98, 22), finished ? "닫기 · Space" : "그만 · Esc", UIStyle.Button)) Close();   // 11/07 60px라 "닫기 · Space" 잘림 → 고려 놀거리와 같은 폭
            }
            protected static void Fill(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = o; }
        }

        // ---------------- 1) 대추야자 따기 ----------------

        public class DatePicking : Game
        {
            public override string Id => "dates";
            public override string DisplayName => "대추야자 따기";
            public override string Title => "알 아흐사 농장 · 대추야자 따기";
            public override string Hint => "←→ 로 바구니를 옮겨 떨어지는 열매를 받으세요. 20초.";
            const int Lanes = 5; const float Length = 20f;
            int lane, caught, missed; float nextDrop;
            readonly List<(int lane, float y)> drops = new List<(int, float)>();
            protected override void Begin() { lane = 2; caught = missed = 0; drops.Clear(); nextDrop = .4f; }
            protected override void Tick(float dt)
            {
                var mv = InputReader.MovePressed();
                lane = Mathf.Clamp(lane + mv.x, 0, Lanes - 1);
                nextDrop -= dt;
                if (nextDrop <= 0f) { drops.Add((UnityEngine.Random.Range(0, Lanes), 0f)); nextDrop = Mathf.Lerp(.9f, .45f, t / Length); }
                float speed = Mathf.Lerp(.55f, .9f, t / Length);
                for (int i = drops.Count - 1; i >= 0; i--)
                {
                    var d = drops[i]; d.y += speed * dt; drops[i] = d;
                    if (d.y >= 1f) { if (d.lane == lane) caught++; else missed++; drops.RemoveAt(i); }
                }
                if (t >= Length) Finish();
            }
            void Finish()
            {
                finished = true; SetBest(Id, caught);
                var gm = GameManager.I; var dates = gm.ItemById("arabia_dates");
                int n = Mathf.Min(5, caught / 3);
                if (n > 0 && !DoneToday(Id) && dates != null && gm.Player.CanCarry(dates, n)) { gm.Player.cargo.Add(dates, n, 0f); MarkToday(Id); result = $"{caught}개를 받았다! 농부가 대추야자 {n}개를 나눠 줬다."; }
                else if (n > 0 && DoneToday(Id)) result = $"{caught}개를 받았다. (오늘 몫은 이미 받음)";
                else if (n > 0) result = $"{caught}개를 받았다. 짐이 가득 차서 못 받았다.";
                else result = $"{caught}개… 내일 다시 해 보자.";
                Sfx.Play(n > 0 ? "success" : "coin", .6f);
            }
            protected override void Draw(Rect r)
            {
                Fill(r, new Color(.95f, .86f, .62f, .35f));
                float lw = r.width / Lanes;
                foreach (var d in drops) Fill(new Rect(r.x + d.lane * lw + lw / 2 - 4, r.y + d.y * (r.height - 14), 8, 8), new Color(.6f, .3f, .1f));
                Fill(new Rect(r.x + lane * lw + 4, r.y + r.height - 12, lw - 8, 10), new Color(.55f, .4f, .2f));
                GUI.Label(new Rect(r.x, r.y - 2, r.width, 18), $"남은 {Mathf.Max(0, Length - t):0}초 · 받음 {caught} · 놓침 {missed} · 최고 {Best(Id)}", UIStyle.Small);
            }
        }

        // ---------------- 2) 낙타 경주 ----------------

        public class CamelRace : Game
        {
            public override string Id => "camel";
            public override string DisplayName => "낙타 경주";
            public override string Title => "제다 성 밖 · 낙타 경주";
            public override string Hint => "바늘이 초록 구간에 올 때 Space! 맞히면 낙타가 달립니다. 틀리면 주춤.";
            const float Length = 30f, Track = 100f;
            float[] pos = new float[4]; float needle, target; int hits, miss;
            float Needle => .5f + .48f * Mathf.Sin(t * 3.2f);
            protected override void Begin() { Array.Clear(pos, 0, 4); hits = miss = 0; target = .6f; }
            protected override void Tick(float dt)
            {
                if (InputReader.ConfirmPressed())
                {
                    bool ok = Mathf.Abs(Needle - target) <= .12f;
                    if (ok) { pos[0] += 9f; hits++; target = UnityEngine.Random.Range(.2f, .8f); } else { pos[0] -= 2f; miss++; }
                }
                pos[0] = Mathf.Max(0f, pos[0] + dt * 1.2f);
                for (int i = 1; i < 4; i++) pos[i] += dt * (2.6f + i * .35f + Mathf.Sin(t * (1.1f + i) ) * .8f);
                if (pos[0] >= Track || t >= Length) Finish();
            }
            void Finish()
            {
                finished = true;
                int place = 1; for (int i = 1; i < 4; i++) if (pos[i] > pos[0]) place++;
                SetBest(Id, 5 - place);
                int prize = place == 1 ? 30 : place == 2 ? 10 : 0;
                if (prize > 0 && !DoneToday(Id)) { GameManager.I.Player.money += prize; MarkToday(Id); result = $"{place}등! 상금 {prize}냥. (맞힘 {hits} · 틀림 {miss})"; Sfx.Play("levelup", .6f); }
                else if (prize > 0) result = $"{place}등! (오늘 상금은 이미 받음)";
                else result = $"{place}등… 바늘을 더 잘 보자. (맞힘 {hits} · 틀림 {miss})";
            }
            protected override void Draw(Rect r)
            {
                Fill(r, new Color(.93f, .8f, .55f, .35f));
                float laneH = (r.height - 30f) / 4f;
                string[] names = { "나", "자이드", "라일라", "오마르" };
                for (int i = 0; i < 4; i++)
                {
                    var y = r.y + i * laneH;
                    Fill(new Rect(r.x, y + laneH - 2, r.width, 1), new Color(.5f, .35f, .2f, .5f));
                    float x = r.x + Mathf.Clamp01(pos[i] / Track) * (r.width - 20);
                    Fill(new Rect(x, y + 3, 14, laneH - 8), i == 0 ? new Color(.95f, .75f, .25f) : new Color(.6f, .45f, .3f));
                    GUI.Label(new Rect(r.x, y, 60, laneH), names[i], UIStyle.Small);
                }
                var bar = new Rect(r.x, r.y + r.height - 24, r.width, 16);
                Fill(bar, new Color(.12f, .22f, .28f));
                Fill(new Rect(bar.x + (target - .12f) * bar.width, bar.y, .24f * bar.width, bar.height), new Color(.34f, .67f, .58f));
                Fill(new Rect(bar.x + Needle * bar.width - 2, bar.y - 3, 4, bar.height + 6), new Color(.97f, .85f, .56f));
                GUI.Label(new Rect(r.x, r.y - 2, r.width, 18), $"남은 {Mathf.Max(0, Length - t):0}초 · 최고 {(Best(Id) > 0 ? (5 - Best(Id)) + "등" : "-")}", UIStyle.Small);
            }
        }

        // ---------------- 3) 모래바람 짐 지키기 ----------------

        public class SandstormRun : Game
        {
            public override string Id => "storm";
            public override string DisplayName => "모래바람 건너기";
            public override string Title => "지름길 · 모래바람 짐 지키기";
            public override string Hint => "잠잠할 땐 Space 연타로 나아가고, \"바람!\"일 땐 아무 키도 누르지 마세요. 25초.";
            const float Length = 25f, Goal = 60f;
            float dist, phase; bool gust; int lost; float calmLeft;
            protected override void Begin() { dist = 0f; gust = false; lost = 0; calmLeft = 3f; ArabiaAmbience.Current?.StartGust(4f); }
            protected override void Tick(float dt)
            {
                calmLeft -= dt;
                if (calmLeft <= 0f) { gust = !gust; calmLeft = gust ? UnityEngine.Random.Range(1.2f, 2.2f) : UnityEngine.Random.Range(2f, 3.5f); if (gust) ArabiaAmbience.Current?.StartGust(calmLeft + 1f); }
                bool pressed = InputReader.ConfirmPressed() || InputReader.MovePressed() != Vector2Int.zero;
                if (pressed) { if (gust) { lost++; dist = Mathf.Max(0f, dist - 4f); } else dist += 3f; }
                if (dist >= Goal || t >= Length) Finish();
            }
            void Finish()
            {
                finished = true;
                bool arrived = dist >= Goal;
                int score = arrived ? Mathf.Max(1, 10 - lost) : 0;
                SetBest(Id, score);
                if (arrived && !DoneToday(Id)) { GameManager.I.Player.money += 25; MarkToday(Id); result = $"건넜다! 짐 {lost}번 날아감. 대상 숙소에서 25냥을 받았다."; Sfx.Play("levelup", .6f); }
                else if (arrived) result = $"건넜다! 짐 {lost}번 날아감. (오늘 몫은 이미 받음)";
                else result = $"시간 안에 못 건넜다 ({dist:0}/{Goal:0}). 잠잠할 때 더 빨리!";
            }
            protected override void Draw(Rect r)
            {
                Fill(r, gust ? new Color(.85f, .7f, .45f, .6f) : new Color(.93f, .85f, .6f, .3f));
                var road = new Rect(r.x, r.y + r.height / 2 - 6, r.width, 12);
                Fill(road, new Color(.6f, .45f, .3f, .6f));
                Fill(new Rect(r.x + Mathf.Clamp01(dist / Goal) * (r.width - 16), road.y - 8, 16, 28), new Color(.95f, .75f, .25f));
                Fill(new Rect(r.x + r.width - 6, road.y - 10, 4, 32), new Color(.3f, .6f, .4f));
                GUI.Label(new Rect(r.x, r.y + 6, r.width, 24), gust ? "<b><color=#ffde59>바람! 멈추세요</color></b>" : "<color=#9be8a8>잠잠함 — Space 연타</color>", UIStyle.CenterRich);
                GUI.Label(new Rect(r.x, r.y - 2, r.width, 18), $"남은 {Mathf.Max(0, Length - t):0}초 · 짐 날아감 {lost} · 최고 {Best(Id)}", UIStyle.Small);
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
