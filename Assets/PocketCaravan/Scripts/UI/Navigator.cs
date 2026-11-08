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
        }

        public static Result? Compute(GameManager gm)
        {
            var r = ComputeTarget(gm);
            if (!r.HasValue) return null;
            var res = r.Value;
            Route(res.position, res.target != null, out res.steer, out res.walkCells);
            return res;
        }

        // ---- 걸어서 가는 길 (같은 맵 안). 물·건물로 막힌 직선 대신 다리·골목을 따라 가리킨다 ----
        const int Lookahead = 4;          // 화살표는 길에서 이만큼 앞 칸을 가리킨다
        const int MaxSearch = 20000;      // 탐색 칸 수 상한 (큰 맵에서도 한 번에 끝나는 크기)
        static Vector3Int cachedFrom, cachedGoal;
        static MapRoot cachedMap;
        static Vector3 cachedSteer;
        static int cachedCells = -1;
        static float cachedAt = -10f;
        const float Refresh = 0.25f;      // 같은 목표면 이 간격으로만 다시 찾는다 (걷는 동안 매 칸 탐색하지 않음)

        static void Route(Vector3 goalWorld, bool goalIsObject, out Vector3 steer, out int cells)
        {
            steer = goalWorld; cells = -1;
            var map = MapRoot.Current;
            var player = PlayerController.Instance;
            if (map == null || player == null) return;
            var from = player.Motor.Cell;
            var goal = map.WorldToCell(goalWorld);
            if (map == cachedMap && goal == cachedGoal && (from == cachedFrom || Time.unscaledTime - cachedAt < Refresh))
            { steer = cachedSteer; cells = cachedCells; return; }
            cachedMap = map; cachedFrom = from; cachedGoal = goal; cachedSteer = goalWorld; cachedCells = -1; cachedAt = Time.unscaledTime;
            var walk = new Dictionary<Vector3Int, bool>();
            bool Walkable(Vector3Int c) { if (!walk.TryGetValue(c, out var w)) walk[c] = w = map.IsWalkable(c); return w; }

            // 사람·장터는 그 칸이 막혀 있으므로 바로 옆 칸에 닿으면 도착으로 본다.
            bool IsGoal(Vector3Int c) => c == goal || (goalIsObject && Mathf.Abs(c.x - goal.x) + Mathf.Abs(c.y - goal.y) == 1);
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
            if (path.Count > 0) steer = map.CellCenter(path[Mathf.Min(Lookahead, path.Count) - 1]);
            if (path.Count <= Lookahead) steer = goalWorld;
            cachedSteer = steer; cachedCells = cells;
        }

        static Result? ComputeTarget(GameManager gm)
        {
            var map = MapRoot.Current;
            if (map == null) return null;
            var p = gm.Quests.Tracked;
            (string, string)? t = p != null && p.Current != null ? gm.Quests.TargetOf(p.Current) : null;
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
            foreach (var e in MapExit.All)
            {
                if (e.targetScene != next) continue;
                var center = e.transform.position + new Vector3(e.size.x * 0.5f, e.size.y * 0.5f, 0f);
                string where = next == targetMap ? index.MapName(targetMap) : $"{index.MapName(next)} 지나 {index.MapName(targetMap)}";
                return new Result { position = center, label = $"{where} 방면", exit = true };
            }
            return null;
        }
    }
}
