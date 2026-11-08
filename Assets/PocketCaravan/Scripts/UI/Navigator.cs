using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 추적 중인 퀘스트의 지금 단계가 어디인지 찾는다.
    /// 같은 맵이면 그 사람(또는 장터), 다른 맵이면 그쪽으로 가는 출구를 가리킨다 (WorldIndex로 가장 짧은 길).
    /// 화살표 방향과 칸 수는 맵 안에서 실제로 걸을 수 있는 칸(MapRoot.IsWalkable)으로 찾은 길을 따른다.
    /// 물로 나뉜 큰 맵(베네치아)에서 직선 방향이 바다를 가리키던 문제 때문. 표시만 바꾸며 진행·저장과 무관하다.
    /// </summary>
    public static class Navigator
    {
        public struct Result
        {
            public Vector3 position;
            public string label;
            public bool exit;               // 출구를 가리키는 중
            public Interactable target;     // 같은 맵에 있는 대상
            public Vector3 steer;           // 화살표가 가리킬 곳: 실제로 걸을 수 있는 길의 몇 칸 앞 (길이 없으면 position)
            public int walkCells;           // 걸어서 가는 칸 수 (길을 못 찾으면 -1)
            public string exitScene;        // 11/08: 출구로 가는 중이면 그 출구가 이어진 씬 (출구 칸 전체를 목적지로 찾는다)
        }

        /// <summary>
        /// 11/08 은샘: 지역 첫걸음 안내(Intro)처럼 퀘스트가 아닌 안내도 같은 화살표를 쓰게 한다. 퀘스트·의뢰보다 먼저 본다.
        /// target(같은 맵의 대상)을 주거나, kind/id(npc·market·board·map)를 주면 다른 맵이면 출구로 안내한다.
        /// </summary>
        public struct Hint { public string kind, id, label; public Interactable target; }
        public static readonly List<System.Func<Hint?>> Tutorials = new List<System.Func<Hint?>>();
        static Hint? TutorialHint() { foreach (var f in Tutorials) { var h = f != null ? f() : null; if (h.HasValue) return h; } return null; }

        public static Result? Compute(GameManager gm)
        {
            var r = ComputeTarget(gm);
            if (!r.HasValue) return null;
            var res = r.Value;
            Route(res.position, res.target != null, res.exitScene, out res.steer, out res.walkCells, out var end);
            if (res.exit && end.HasValue) res.position = end.Value;   // ▼ 표시는 실제로 닿을 출구 칸에
            return res;
        }

        // ---- 걸어서 가는 길 (같은 맵 안). 물·건물로 막힌 직선 대신 다리·골목을 따라 가리킨다 ----
        const int Lookahead = 4;          // 화살표는 길에서 이만큼 앞 칸을 가리킨다
        const int SightAhead = 24;        // 11/08: 이만큼 앞까지는 곧게 닿으면 그 칸을 가리킨다 (12 → 24: 계단 모양 길에서 방향이 덜 튄다)

        /// <summary>두 점 사이를 곧게 걸어도 막힌 칸이 없는지 (0.25칸마다 확인)</summary>
        static bool ClearLine(MapRoot map, Vector3 a, Vector3 b, System.Func<Vector3Int, bool> isGoal)
        {
            float len = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / 0.25f));
            for (int k = 1; k <= steps; k++)
            {
                var c = map.WorldToCell(Vector3.Lerp(a, b, k / (float)steps));
                if (!map.IsWalkable(c) && !isGoal(c)) return false;
            }
            return true;
        }
        const int MaxSearch = 20000;      // 탐색 칸 수 상한 (큰 맵에서도 한 번에 끝나는 크기)
        static Vector3Int cachedFrom, cachedGoal;
        static string cachedExit;
        static Vector3? cachedEnd;
        static MapRoot cachedMap;
        static Vector3 cachedSteer;
        static int cachedCells = -1;
        static float cachedAt = -10f;
        const float Refresh = 0.25f;      // 같은 목표면 이 간격으로만 다시 찾는다 (걷는 동안 매 칸 탐색하지 않음)

        static void Route(Vector3 goalWorld, bool goalIsObject, string exitScene, out Vector3 steer, out int cells, out Vector3? end)
        {
            steer = goalWorld; cells = -1; end = null;
            var map = MapRoot.Current;
            var player = PlayerController.Instance;
            if (map == null || player == null) return;
            var from = player.Motor.Cell;
            var goal = map.WorldToCell(goalWorld);
            // 11/08: 출구로 갈 때는 출구 칸 전체가 목적지다. 예전엔 "지금 가장 가까운 출구 칸"을 매번 새로 골라
            // 긴 출구를 따라 걸으면 목적지가 계속 바뀌어 화살표가 날뛰었다.
            if (map == cachedMap && cachedExit == exitScene && (exitScene != null || goal == cachedGoal) && (from == cachedFrom || Time.unscaledTime - cachedAt < Refresh))
            { steer = cachedSteer; cells = cachedCells; end = cachedEnd; return; }
            cachedMap = map; cachedFrom = from; cachedGoal = goal; cachedExit = exitScene; cachedSteer = goalWorld; cachedCells = -1; cachedEnd = null; cachedAt = Time.unscaledTime;
            var walk = new Dictionary<Vector3Int, bool>();
            bool Walkable(Vector3Int c) { if (!walk.TryGetValue(c, out var w)) walk[c] = w = map.IsWalkable(c); return w; }
            HashSet<Vector3Int> exitCells = null;
            if (exitScene != null)
            {
                exitCells = new HashSet<Vector3Int>();
                foreach (var e in MapExit.All)
                {
                    if (e == null || e.targetScene != exitScene) continue;
                    var o = map.WorldToCell(e.transform.position);
                    for (int y = 0; y < e.size.y; y++) for (int x = 0; x < e.size.x; x++) exitCells.Add(new Vector3Int(o.x + x, o.y + y, 0));
                }
                if (exitCells.Count == 0) exitCells = null;
            }

            // 사람·장터는 그 칸이 막혀 있으므로 바로 옆 칸에 닿으면 도착으로 본다.
            bool IsGoal(Vector3Int c) => exitCells != null ? exitCells.Contains(c) : c == goal || (goalIsObject && Mathf.Abs(c.x - goal.x) + Mathf.Abs(c.y - goal.y) == 1);
            var prev = new Dictionary<Vector3Int, Vector3Int> { [from] = from };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(from);
            Vector3Int? found = null;
            var dirs = new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
            while (queue.Count > 0 && prev.Count < MaxSearch)
            {
                var c = queue.Dequeue();
                if (IsGoal(c)) { found = c; break; }
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (prev.ContainsKey(n) || !(IsGoal(n) || Walkable(n))) continue;
                    prev[n] = c;
                    queue.Enqueue(n);
                }
            }
            if (!found.HasValue) return;     // 길을 못 찾으면 예전처럼 직선 방향
            var path = new List<Vector3Int>();
            for (var c = found.Value; c != from; c = prev[c]) path.Add(c);
            path.Reverse();
            cells = path.Count;
            Vector3 last = exitCells != null ? map.CellCenter(found.Value) : goalWorld;
            if (exitCells != null) end = last;
            if (path.Count > 0) steer = map.CellCenter(path[Mathf.Min(Lookahead, path.Count) - 1]);
            if (path.Count <= Lookahead) steer = last;
            // 11/08 기우: 칸 길은 가로·세로로만 꺾여서, 비스듬히 바로 갈 수 있어도 화살표가 옆(→)만 가리켰다.
            // 길 위 앞 칸들 중에 곧게 걸어 닿는(사이가 막히지 않은) 가장 먼 칸을 가리킨다 → 목적지(▼)와 방향이 맞는다.
            // 11/08 은샘: 목적지까지 곧게 트여 있으면 목적지를 바로 가리킨다(걸음마다 계단 칸이 바뀌어 방향이 흔들리지 않게).
            Vector3 fromW = player.transform.position;
            if (ClearLine(map, fromW, last, IsGoal)) steer = last;
            else
                for (int i = Mathf.Min(SightAhead, path.Count) - 1; i >= Lookahead; i--)
                {
                    var c = path[i];
                    Vector3 to = (i == path.Count - 1) ? last : map.CellCenter(c);
                    if (ClearLine(map, fromW, to, IsGoal)) { steer = to; break; }
                }
            cachedSteer = steer; cachedCells = cells; cachedEnd = end;
        }

        // ---- 11/08 은샘: 화살표 방향을 부드럽게 (한 걸음마다 길 칸이 바뀌어도 화살표가 날뛰지 않게) ----
        static Vector2 smoothDir; static int smoothFrame = -1; static float smoothTime; static MapRoot smoothMap;
        /// <summary>그리는 쪽(이름표 OnGUI)이 부른다. 같은 프레임 안에서는 같은 값을 돌려준다. 초당 최대 270도만 돈다.</summary>
        public static Vector2 SmoothDirection(Vector2 desired)
        {
            if (desired.sqrMagnitude < 1e-6f) return smoothDir;
            desired.Normalize();
            if (smoothFrame == Time.frameCount) return smoothDir;
            float now = Time.unscaledTime;
            if (smoothDir.sqrMagnitude < 0.5f || smoothMap != MapRoot.Current || now - smoothTime > 0.5f) smoothDir = desired;
            else
            {
                float max = 270f * Mathf.Max(0f, now - smoothTime) * Mathf.Deg2Rad;
                smoothDir = (Vector2)Vector3.RotateTowards(smoothDir, desired, max, 0f);
            }
            smoothFrame = Time.frameCount; smoothTime = now; smoothMap = MapRoot.Current;
            return smoothDir;
        }

        static Result? ComputeTarget(GameManager gm)
        {
            var map = MapRoot.Current;
            if (map == null) return null;
            var hint = TutorialHint();
            if (hint.HasValue && hint.Value.target != null && hint.Value.target.isActiveAndEnabled)
                return new Result { position = hint.Value.target.transform.position, label = hint.Value.label ?? WorldDisplayNames.For(hint.Value.target), target = hint.Value.target };
            var p = gm.Quests.Tracked;
            (string, string)? t = hint.HasValue && !string.IsNullOrEmpty(hint.Value.kind) ? (hint.Value.kind, hint.Value.id)
                : p != null && p.Current != null ? gm.Quests.TargetOf(p.Current) : null;
            // 퀘스트 안내가 없으면 맡은 의뢰의 목적지 게시판으로
            if (!t.HasValue && gm.Player.contracts.Count > 0) t = ("board", gm.Player.contracts[0].toTown);
            if (!t.HasValue) return null;
            var index = WorldIndex.Get();
            var (kind, id) = t.Value;

            string targetMap = kind == "map" ? id : index != null ? index.MapOf(kind, id) : null;
            if (targetMap == null) return null;

            if (targetMap == map.mapId)
            {
                if (kind == "map") return null; // 이미 도착
                foreach (var it in Interactable.All)
                {
                    bool match = (kind == "npc" && it is QuestNpc q && q.npcId == id)
                              || (kind == "market" && it is TownMarket m && m.town != null && m.town.id == id)
                              || (kind == "board" && it is ContractBoard b && b.town == id);
                    if (match) return new Result { position = it.transform.position, label = WorldDisplayNames.For(it), target = it };
                }
                return null;
            }

            string next = index.NextStep(map.mapId, targetMap);
            if (next == null) return null;
            // 11/08 기우: 같은 맵으로 가는 출구가 여러 토막이면(아라비아 빈 사막 남쪽 등) 목록 맨 앞 토막의 가운데가 아니라
            // 플레이어에게 가장 가까운 출구 칸을 가리킨다. 출구가 하나인 맵은 그 출구 안에서 가장 가까운 칸.
            var player = PlayerController.Instance;
            Vector3Int me = player != null ? map.WorldToCell(player.transform.position) : Vector3Int.zero;
            Vector3? best = null; int bestD = int.MaxValue;
            foreach (var e in MapExit.All)
            {
                if (e.targetScene != next) continue;
                var o = map.WorldToCell(e.transform.position);
                var c = new Vector3Int(Mathf.Clamp(me.x, o.x, o.x + e.size.x - 1), Mathf.Clamp(me.y, o.y, o.y + e.size.y - 1), 0);
                int dist = Mathf.Abs(c.x - me.x) + Mathf.Abs(c.y - me.y);
                if (player == null) { best = e.transform.position + new Vector3(e.size.x * 0.5f, e.size.y * 0.5f, 0f); break; }
                if (dist < bestD) { bestD = dist; best = map.CellCenter(c); }
            }
            if (best.HasValue)
            {
                string where = next == targetMap ? index.MapName(targetMap) : $"{index.MapName(next)} 지나 {index.MapName(targetMap)}";
                return new Result { position = best.Value, label = $"{where} 방면", exit = true, exitScene = next };
            }
            return null;
        }
    }
}
