using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 10/27 나룻배 노 젓기 미니게임 (피오르 나루 ↔ 개울 나루 마을 나룻배).
    /// 사공에게 뱃삯을 내면 시작: 피오르 물길을 거슬러 올라간다 (화면 위가 앞).
    /// Space를 톡톡 눌러 노를 젓고(누를 때마다 앞으로 밀림), ←→로 방향을 잡아 떠내려오는 얼음 덩어리를 피한다.
    /// 부딪히면 잠깐 느려질 뿐 실패는 없다. 15초 안에 닿으면 사공이 3냥을 돌려준다. Esc는 건너뛰기(바로 도착).
    /// 끝나면 원래 나룻배처럼 GameManager.TravelTo로 건너편에 도착한다 (이동·짐·저장은 공용 그대로).
    /// </summary>
    public class NorthFerryGame : MonoBehaviour
    {
        static NorthFerryGame instance;
        enum S { None, Ready, Row, Result }
        S state = S.None;
        string dest; Vector2Int facing;
        class Ice { public float x, y; public bool big, hit; }
        readonly List<Ice> ices = new List<Ice>();
        float bx, dist, speed, timer, runTime, bump, stroke, spawnAt;
        int hits;
        string result;
        int startFrame;
        readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
        const float FieldW = 200f, FieldH = 300f, Goal = 2600f, FastTime = 15f;   // 10/27 15초 안에 닿으면 3냥
        const int Refund = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthFerryGame>() != null) return;
            var go = new GameObject("NorthFerryGame");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<NorthFerryGame>();
        }

        void Awake()
        {
            instance = this;
            foreach (var n in new[] { "ferry_boat_0", "ferry_boat_1", "ferry_ice_0", "ferry_ice_1", "tree", "tree3", "rock" })
            {
                var t = Resources.Load<Texture2D>("North/Minigame/" + n);
                if (t != null) { t.filterMode = FilterMode.Point; tex[n] = t; }
            }
        }

        /// <summary>뱃삯을 낸 뒤 부른다. 미니게임이 없으면(그림 없음 등) 바로 건너간다.</summary>
        public static void Begin(string destMap, Vector2Int face)
        {
            var m = instance;
            if (m == null || !m.tex.ContainsKey("ferry_boat_0")) { Arrive(destMap, face); return; }
            m.dest = destMap; m.facing = face;
            m.ices.Clear();
            m.bx = 0f; m.dist = 0f; m.speed = 40f; m.runTime = 0f; m.bump = 0f; m.stroke = 0f; m.spawnAt = 160f;
            m.hits = 0; m.result = null;
            m.state = S.Ready; m.timer = 1.5f; m.startFrame = Time.frameCount;
            UIState.Blocking = true;
        }

        static void Arrive(string destMap, Vector2Int face)
        {
            if (GameManager.I != null) GameManager.I.TravelTo(destMap, "ferry", 0f, face);
        }

        bool Fresh => Time.frameCount - startFrame < 2;   // 시작한 Space가 바로 노 젓기로 먹지 않게

        void Update()
        {
            if (state == S.None) return;
            float dt = Time.unscaledDeltaTime;
            if (state == S.Ready)
            {
                timer -= dt;
                if (InputReader.CancelPressed()) { Finish(false); return; }
                if (timer <= 0f) state = S.Row;
                return;
            }
            if (state == S.Result)
            {
                if (!Fresh && (InputReader.InteractPressed() || InputReader.CancelPressed()))
                {
                    state = S.None;
                    UIState.Release();
                    Arrive(dest, facing);
                }
                return;
            }
            if (InputReader.CancelPressed()) { Finish(false); return; }
            runTime += dt;
            if (bump > 0f) bump -= dt;
            if (stroke > 0f) stroke -= dt;
            // 노 젓기: Space 한 번에 앞으로 확 밀리고, 물살 때문에 서서히 느려진다
            if (!Fresh && InputReader.InteractPressed()) { speed = Mathf.Min(190f, speed + 34f); stroke = 0.22f; }
            speed = Mathf.MoveTowards(speed, 35f, 26f * dt);
            dist += speed * dt;
            bx = Mathf.Clamp(bx + InputReader.MoveVector().x * 95f * dt, -FieldW / 2 + 26f, FieldW / 2 - 26f);

            while (spawnAt < dist + FieldH + 30f && spawnAt < Goal - 80f)
            {
                float half = FieldW / 2 - 30f;
                int n = spawnAt > 1300f && Random.value < 0.4f ? 2 : 1;
                for (int i = 0; i < n; i++) ices.Add(new Ice { x = Random.Range(-half, half), y = spawnAt + Random.Range(-10f, 10f), big = Random.value < 0.45f });
                spawnAt += Random.Range(70f, 110f);
            }
            // 얼음은 물살에 천천히 떠내려온다 (배 쪽으로)
            foreach (var c in ices) c.y -= 22f * dt;
            float by = dist - 18f;   // 배 몸통 가운데 (그림에서 뱃머리보다 18만큼 아래)
            foreach (var c in ices)
            {
                if (c.hit) continue;
                float hw = c.big ? 28f : 20f, hh = c.big ? 26f : 22f;   // 얼음 반폭 + 배 반폭 / 얼음 반높이 + 배 반길이
                if (Mathf.Abs(c.x - bx) < hw && Mathf.Abs(c.y - by) < hh && bump <= 0f)
                {
                    c.hit = true; hits++; speed *= 0.3f; bump = 0.9f;
                }
            }
            ices.RemoveAll(c => c.y < dist - 60f);
            if (dist >= Goal) Finish(true);
        }

        void Finish(bool rowed)
        {
            if (!rowed)
            {
                state = S.None; UIState.Release(); Arrive(dest, facing);   // 건너뛰기: 바로 도착
                return;
            }
            bool fast = runTime <= FastTime;
            if (fast && GameManager.I != null) { GameManager.I.Player.money += Refund; }
            result = $"건넜다! {runTime:0.0}초 · 얼음에 {hits}번 쿵"
                     + (fast ? $"\n사공: \"솜씨 좋네! 노 젓는 값으로 {Refund}냥 돌려줄게.\"" : "\n사공: \"수고했어. 천천히 와도 괜찮아.\"");
            state = S.Result; startFrame = Time.frameCount;
        }

        void OnGUI()
        {
            if (state == S.None) return;
            UIStyle.Begin();
            GUI.depth = -20;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight;
            Fill(new Rect(0, 0, vw, vh), new Color(0f, 0f, 0.05f, 0.45f));
            float sx = bump > 0.6f ? Random.Range(-2f, 2f) : 0f;
            var field = new Rect(vw / 2 - FieldW / 2 + sx, vh / 2 - FieldH / 2 + 10, FieldW, FieldH);
            Fill(new Rect(field.x - 4, field.y - 4, field.width + 8, field.height + 8), new Color(0.2f, 0.18f, 0.28f));
            Fill(field, new Color(0.24f, 0.47f, 0.66f));
            // 물결 (아래로 흘러감 = 배가 앞으로 감)
            for (int i = 0; i < 16; i++)
            {
                float y = field.y + Mathf.Repeat(i * 19f + dist * 0.9f, field.height);
                Fill(new Rect(field.x + 24 + (i * 41) % (int)(field.width - 56), y, 12, 1), new Color(0.6f, 0.8f, 0.92f, 0.8f));
            }
            // 양쪽 눈 덮인 기슭과 전나무
            Fill(new Rect(field.x, field.y, 18, field.height), new Color(0.93f, 0.95f, 1f));
            Fill(new Rect(field.xMax - 18, field.y, 18, field.height), new Color(0.93f, 0.95f, 1f));
            for (int i = 0; i < 10; i++)
            {
                float y = field.y + Mathf.Repeat(i * 34f + dist, field.height + 34f) - 34f;
                DrawClip(i % 2 == 0 ? "tree" : "tree3", new Rect(field.x - 14, y, 32, 32), field);
                DrawClip(i % 3 == 0 ? "rock" : i % 2 == 0 ? "tree3" : "tree", new Rect(field.xMax - 18, y + 14, 32, 32), field);
            }
            // 건너편 부두 (결승)
            float gy = field.y + field.height - 50f - (Goal - dist);
            if (gy > field.y - 20f && gy < field.yMax)
            {
                Fill(new Rect(field.x + 18, Mathf.Max(field.y, gy - 14f), field.width - 36, Mathf.Min(14f, gy - field.y + 1)), new Color(0.55f, 0.38f, 0.24f));
                if (gy - 14f > field.y) GUI.Label(new Rect(field.x, gy - 30f, field.width, 16f), "<b>건너편 부두</b>", UIStyle.CenterRich);
            }
            // 얼음 (화면 y: 아래가 배, 위가 앞쪽)
            foreach (var c in ices)
            {
                float y = field.yMax - 50f - (c.y - dist);
                float x = field.center.x + c.x;
                var t = c.big ? "ferry_ice_0" : "ferry_ice_1";
                float w = c.big ? 44f : 28f, h = c.big ? 26f : 20f;
                var old = GUI.color; if (c.hit) GUI.color = new Color(1f, 1f, 1f, 0.5f);
                DrawClip(t, new Rect(x - w / 2, y - h / 2, w, h), field);
                GUI.color = old;
            }
            // 배
            float boatY = field.yMax - 50f;
            var bp = new Rect(field.center.x + bx - 22f, boatY - 6f, 44f, 50f);
            Draw(stroke > 0f ? "ferry_boat_1" : "ferry_boat_0", bp);
            // 위 정보
            float prog = Mathf.Clamp01(dist / Goal);
            Fill(new Rect(field.x, field.y - 12, field.width, 6), new Color(0.15f, 0.12f, 0.2f));
            Fill(new Rect(field.x, field.y - 12, field.width * prog, 6), new Color(1f, 0.82f, 0.3f));
            GUI.Label(new Rect(field.x, field.y - 30, field.width, 16), $"<b>나룻배</b>  {runTime:0.0}초" + (runTime <= FastTime ? $"  <color=#9fe0a0>({FastTime:0}초 안에 닿으면 {Refund}냥!)</color>" : ""), UIStyle.Rich);
            GUI.Label(new Rect(field.xMax + 12, field.y, 150, 90), "Space 노 젓기 (톡톡!)\n← → 방향\n얼음 덩어리 피하기\nEsc 건너뛰기", UIStyle.Small);
            if (state == S.Ready)
                GUI.Label(new Rect(field.x, field.center.y - 20, field.width, 40), $"<size=18><b>{Mathf.CeilToInt(timer)}</b></size>", UIStyle.CenterRich);
            if (state == S.Result)
            {
                var box = new Rect(field.center.x - 100, field.center.y - 50, 200, 100);
                GUI.Box(box, GUIContent.none, UIStyle.Box);
                GUI.Label(new Rect(box.x + 10, box.y + 8, 180, 70), result, UIStyle.Wrap);
                GUI.Label(new Rect(box.x + 10, box.yMax - 22, 180, 16), "Space: 내리기", UIStyle.Small);
            }
        }

        void Draw(string name, Rect r)
        {
            if (tex.TryGetValue(name, out var t)) GUI.DrawTexture(r, t, ScaleMode.StretchToFill);
        }

        void DrawClip(string name, Rect r, Rect field)
        {
            if (!tex.TryGetValue(name, out var t)) return;
            float top = Mathf.Max(r.y, field.y), bottom = Mathf.Min(r.yMax, field.yMax);
            if (bottom <= top) return;
            float v0 = 1f - (top - r.y) / r.height, v1 = 1f - (bottom - r.y) / r.height;
            GUI.DrawTextureWithTexCoords(new Rect(r.x, top, r.width, bottom - top), t, new Rect(0f, v1, 1f, v0 - v1));
        }

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
