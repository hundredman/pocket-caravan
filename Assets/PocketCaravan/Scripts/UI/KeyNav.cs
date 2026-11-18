using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/17 팀 피드백 "단추를 화살표 키로도 고를 수 있게": 창 안의 단추를 ← → ↑ ↓ 로 고르고 Enter·Space로 누른다.
    /// - 창이 OnGUI에서 Begin(this) … End()로 감싸면 그 사이에 그린 CuteUI.Button·SoftButton(과 Item을 부르는 단추)이 대상이 된다.
    ///   감싸지 않은 창은 예전 그대로다 (화살표를 따로 쓰는 창과 부딪히지 않게 창마다 켠다).
    /// - 처음 화살표를 누르면 첫 단추가 골라지고, 그다음은 누른 쪽에서 가장 가까운 단추로 옮겨 간다 (화면 자리로 찾는다).
    /// - 골라진 단추가 초록(주 단추 모양)이 되고 톡 튀어 오른다. 나머지는 흰 단추로 보인다. 마우스를 움직이면 고른 것을 지워 마우스 쪽을 따른다.
    /// - 칸에 글을 쓰는 중이면 Begin(this, false)로 끈다 (← →는 글자 커서).
    /// </summary>
    public static class KeyNav
    {
        sealed class State { public float changedAt = -9f; public int focus = -1, activate = -1, counter; public readonly List<Rect> drawn = new List<Rect>(), last = new List<Rect>(); public Vector2 mouse; }
        static readonly Dictionary<object, State> states = new Dictionary<object, State>();
        static State cur;

        /// <summary>지금 창에서 골라진 단추가 있는지 (창이 Enter를 따로 쓸 때 양보하려고)</summary>
        public static bool HasFocus(object owner) => owner != null && states.TryGetValue(owner, out var s) && s.focus >= 0;
        public static void Clear(object owner) { if (owner != null && states.TryGetValue(owner, out var s)) { s.focus = -1; s.activate = -1; } }
        /// <summary>창이 단추 하나를 골라 둔다 (예: 칸에서 ↓로 내려왔을 때 첫 단추)</summary>
        public static void FocusFirst(object owner) { var s = Get(owner); s.focus = 0; s.changedAt = Time.unscaledTime; }

        static State Get(object owner)
        {
            if (!states.TryGetValue(owner, out var s)) { s = new State(); states[owner] = s; }
            return s;
        }

        public static void Begin(object owner, bool keys = true)
        {
            cur = Get(owner);
            cur.counter = 0;
            var e = Event.current;
            if (e == null) return;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                if ((e.mousePosition - cur.mouse).sqrMagnitude > 4f) cur.focus = -1;
                cur.mouse = e.mousePosition;
            }
            if (!keys) { cur.focus = -1; return; }
            if (e.type != EventType.KeyDown || cur.last.Count == 0) return;
            Vector2Int dir = e.keyCode switch
            {
                KeyCode.LeftArrow => Vector2Int.left, KeyCode.RightArrow => Vector2Int.right,
                KeyCode.UpArrow => Vector2Int.down, KeyCode.DownArrow => Vector2Int.up,   // 화면 y는 아래로 커진다
                _ => Vector2Int.zero,
            };
            // 11/18 은샘: WASD로도 고른다 (글 칸에 글을 쓰는 중이면 빼고)
            if (dir == Vector2Int.zero && GUIUtility.keyboardControl == 0)
                dir = e.keyCode switch
                {
                    KeyCode.A => Vector2Int.left, KeyCode.D => Vector2Int.right,
                    KeyCode.W => Vector2Int.down, KeyCode.S => Vector2Int.up,
                    _ => Vector2Int.zero,
                };
            if (dir != Vector2Int.zero)
            {
                cur.focus = cur.focus < 0 || cur.focus >= cur.last.Count ? 0 : Nearest(cur.last, cur.focus, dir);
                cur.changedAt = Time.unscaledTime;
                Sfx.Play("move", .4f);
                e.Use();
                return;
            }
            if (cur.focus >= 0 && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space))
            {
                cur.activate = cur.focus;
                e.Use();
            }
        }

        public static void End()
        {
            if (cur == null) return;
            var e = Event.current;
            if (e != null && e.type == EventType.Repaint)
            {
                cur.last.Clear(); cur.last.AddRange(cur.drawn); cur.drawn.Clear();
                if (cur.focus >= cur.last.Count) cur.focus = cur.last.Count - 1;
            }
            cur = null;
        }

        /// <summary>지금 창에서 고른 단추가 있다 (다른 단추는 흰 모양으로 그린다)</summary>
        public static bool Active => cur != null && cur.focus >= 0;

        /// <summary>단추를 그리기 전에 부른다: 등록하고, 골라졌는지 알려 준다. 키로 눌렸으면 true</summary>
        public static bool Item(Rect r, out bool focused)
        {
            focused = false;
            if (cur == null) return false;
            int i = cur.counter++;
            if (Event.current != null && Event.current.type == EventType.Repaint) cur.drawn.Add(r);
            focused = i == cur.focus;
            if (cur.activate == i) { cur.activate = -1; return true; }
            return false;
        }

        /// <summary>골라진 단추를 그릴 자리: 고른 순간 2px 톡 튀어 올랐다가 0.18초에 걸쳐 내려앉는다</summary>
        public static Rect Pop(Rect r)
        {
            if (cur == null) return r;
            float t = (Time.unscaledTime - cur.changedAt) / 0.18f;
            if (t >= 1f || t < 0f) return r;
            float up = Mathf.Sin(t * Mathf.PI) * 2f;
            return new Rect(r.x, Mathf.Round(r.y - up), r.width, r.height);
        }

        static int Nearest(List<Rect> rs, int from, Vector2Int dir)
        {
            Vector2 a = rs[from].center, d = dir;
            int best = from; float bestScore = float.MaxValue;
            for (int i = 0; i < rs.Count; i++)
            {
                if (i == from) continue;
                Vector2 v = rs[i].center - a;
                float along = Vector2.Dot(v, d);
                if (along <= 2f) continue;
                float side = Mathf.Abs(d.x != 0 ? v.y : v.x);
                float score = along + side * 2.5f;
                if (score < bestScore) { bestScore = score; best = i; }
            }
            if (best != from) return best;
            // 그쪽에 없으면 끝에서 반대쪽 끝으로 (같은 줄 단추 둘이면 오가기)
            return dir.x + dir.y > 0 ? 0 : rs.Count - 1;
        }
    }
}
