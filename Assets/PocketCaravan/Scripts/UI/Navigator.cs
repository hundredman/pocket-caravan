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
            Route(res.position, res.target != null, out res.steer, out res.walkCells);
            return res;
        }

        // ---- 걸어서 가는 길 (같은 맵 안). 물·건물로 막힌 직선 대신 다리·골목을 따라 가리킨다 ----
        const int Lookahead = 4;          // 화살표는 길에서 이만큼 앞 칸을 가리킨다
        const int SightAhead = 12;        // 11/08: 이만큼 앞까지는 곧게 닿으면 그 칸을 가리킨다

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
        /// <summary>11/11 길에 서 있으면 곧게 가리킬 때도 길 위로만 (모퉁이를 풀밭으로 자르지 않게)</summary>
        static bool RoadLine(MapRoot map, RoadGrid roads, Vector3 a, Vector3 b, System.Func<Vector3Int, bool> isGoal)
        {
            float len = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / 0.25f));
            for (int k = 1; k <= steps; k++)
            {
                var c = map.WorldToCell(Vector3.Lerp(a, b, k / (float)steps));
                if (roads.GradeAt(c) <= 0 && !isGoal(c)) return false;
            }
            return true;
        }

        const int OffRoadCost = 6;        // 11/11 길 따라가기: 길 아닌 칸 값 (길 칸은 1). 3이면 장단처럼 길이 멀리 돌면 풀밭을 가로질렀다

        /// <summary>11/11 은샘 담당 맵(고려·중앙 대륙 동쪽)만 길을 따라간다</summary>
        static bool FollowsRoads(string mapId)
        {
            if (string.IsNullOrEmpty(mapId)) return false;
            string id = GoryeoLayout.Resolve(mapId);
            return id == "Naru" || id == "Gaegyeong" || id == "Dakmok" || id.StartsWith("goryeo_") || id.StartsWith("transit_");
        }

        // 11/11 은샘: 고려·중앙 대륙 동쪽에서 서쪽(교역 길목 너머: 만국 장터·다른 지역)으로 갈 때 맵마다 다음 맵을 정해 둔다.
        //   맵 건너는 수만 보면 나루는 장단→남쪽 포구(길 없는 1칸 출구), 개경은 송악(길 없는 연결)으로 제각각 갔다.
        //   길로 이어진 큰길 하나: 나루→장단→송악, 개경→닥목→송악 → 나무꾼 길목 → 중앙 진입로 → 교역 길목
        static readonly Dictionary<string, string> WestRoadNext = new Dictionary<string, string>
        {
            ["Naru"] = "goryeo_jangdan", ["goryeo_jangdan"] = "goryeo_songak",
            ["Gaegyeong"] = "Dakmok", ["Dakmok"] = "goryeo_songak",
            ["goryeo_songak"] = "transit_woodtown", ["transit_pass"] = "transit_woodtown",
            ["transit_woodtown"] = "transit_central_approach", ["transit_ferry"] = "transit_wetroad",
            ["transit_wetroad"] = "transit_central_approach", ["transit_central_approach"] = "transit_crossroads",
            ["transit_southport"] = "transit_south_inland", ["transit_south_inland"] = "transit_crossroads",
        };

        static string WestRoad(string here, string target)
        {
            if (string.IsNullOrEmpty(here) || string.IsNullOrEmpty(target)) return null;
            here = GoryeoLayout.Resolve(here);
            if (!WestRoadNext.TryGetValue(here, out var next)) return null;
            string t = GoryeoLayout.Resolve(target);
            if (t == "transit_crossroads" || WestRoadNext.ContainsKey(t)) return null;   // 고려·중앙 동쪽 안에서는 예전처럼 가장 짧은 길
            var info = WorldIndex.Get()?.Map(here);
            return info != null && info.neighbors.Contains(next) ? next : null;
        }

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
            // 11/11 은샘: 고려·중앙 대륙 동쪽 맵은 길(오솔길·길·큰길)을 따라 가리킨다. 길 아닌 칸은 OffRoadCost배로 쳐서
            //   조금 돌아가더라도 길로 간다 (팀 시연: 화살표가 풀밭을 가로질렀다). 다른 지역은 예전처럼 가장 짧은 길
            var roads = RoadGrid.Current;
            bool roadMode = roads != null && FollowsRoads(map.mapId);
            bool OnRoad(Vector3Int c) => roads.GradeAt(c) > 0;
            var prev = new Dictionary<Vector3Int, Vector3Int> { [from] = from };
            Vector3Int? found = null;
            var dirs = new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
            if (!roadMode)
            {
                var queue = new Queue<Vector3Int>();
                queue.Enqueue(from);
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
            }
            else
            {
                // 칸 값이 1 또는 OffRoadCost뿐이라 값별 줄(버킷)로 가장 싼 칸부터 꺼낸다
                var cost = new Dictionary<Vector3Int, int> { [from] = 0 };
                var buckets = new List<List<Vector3Int>> { new List<Vector3Int> { from } };
                var done = new HashSet<Vector3Int>();
                for (int k = 0; k < buckets.Count && !found.HasValue && done.Count < MaxSearch; k++)
                {
                    var list = buckets[k];
                    for (int j = 0; j < list.Count; j++)
                    {
                        var c = list[j];
                        if (cost[c] != k || !done.Add(c)) continue;
                        if (IsGoal(c)) { found = c; break; }
                        foreach (var d in dirs)
                        {
                            var n = c + d;
                            if (done.Contains(n) || !(IsGoal(n) || Walkable(n))) continue;
                            int nc = k + (OnRoad(n) || IsGoal(n) ? 1 : OffRoadCost);
                            if (cost.TryGetValue(n, out var old) && old <= nc) continue;
                            cost[n] = nc; prev[n] = c;
                            while (buckets.Count <= nc) buckets.Add(new List<Vector3Int>());
                            buckets[nc].Add(n);
                        }
                    }
                }
            }
            if (!found.HasValue) return;     // 길을 못 찾으면 예전처럼 직선 방향
            var path = new List<Vector3Int>();
            for (var c = found.Value; c != from; c = prev[c]) path.Add(c);
            path.Reverse();
            cells = path.Count;
            if (path.Count > 0) steer = map.CellCenter(path[Mathf.Min(Lookahead, path.Count) - 1]);
            if (path.Count <= Lookahead) steer = goalWorld;
            // 11/08 기우: 칸 길은 가로·세로로만 꺾여서, 비스듬히 바로 갈 수 있어도 화살표가 옆(→)만 가리켰다.
            // 길 위 12칸 앞까지 중에 곧게 걸어 닿는(사이가 막히지 않은) 가장 먼 칸을 가리킨다 → 목적지(▼)와 방향이 맞는다.
            Vector3 fromW = player.transform.position;
            // 11/11 길 밖에 있으면 곧게 가리키는 것도 길에 올라서는 첫 칸까지만 (길을 건너뛰어 가로지르지 않게)
            int firstRoad = path.Count - 1;
            if (roadMode && !OnRoad(from)) for (int k = 0; k < path.Count; k++) if (OnRoad(path[k])) { firstRoad = k; break; }
            for (int i = Mathf.Min(SightAhead, path.Count) - 1; i >= Lookahead; i--)
            {
                if (roadMode && !OnRoad(from) && i > firstRoad) continue;
                var c = path[i];
                Vector3 to = (i == path.Count - 1) ? goalWorld : map.CellCenter(c);
                if (ClearLine(map, fromW, to, IsGoal) && (!roadMode || !OnRoad(from) || RoadLine(map, roads, fromW, to, IsGoal))) { steer = to; break; }
            }
            cachedSteer = steer; cachedCells = cells;
        }

        static Result? ComputeTarget(GameManager gm)
        {
            var map = MapRoot.Current;
            if (map == null) return null;
            var hint = TutorialHint();
            if (hint.HasValue && hint.Value.target != null && hint.Value.target.isActiveAndEnabled)
                return new Result { position = hint.Value.target.transform.position, label = hint.Value.label ?? WorldDisplayNames.For(hint.Value.target), target = hint.Value.target };
            // 11/11 은샘: 길 안내 대상 하나만 (QuestFocus). 받은 퀘스트는 수첩에서 고른 것일 때만, 다음 이야기는 그 사람에게
            var focus = QuestFocus.Current;
            var p = focus == QuestFocus.Kind.Quest ? gm.Quests.Tracked : null;
            (string, string)? t = hint.HasValue && !string.IsNullOrEmpty(hint.Value.kind) ? (hint.Value.kind, hint.Value.id)
                : p != null && p.Current != null ? gm.Quests.TargetOf(p.Current) : null;
            if (!t.HasValue && focus == QuestFocus.Kind.Lead && !string.IsNullOrEmpty(StoryGuide.LeadNpc)) t = ("npc", StoryGuide.LeadNpc);
            // 퀘스트 안내가 없으면 맡은 의뢰의 목적지 게시판으로 (다음 이야기·이벤트를 안내 중이면 그쪽만)
            if (!t.HasValue && (focus == QuestFocus.Kind.Quest || focus == QuestFocus.Kind.None) && gm.Player.contracts.Count > 0) t = ("board", gm.Player.contracts[0].toTown);
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

            string west = WestRoad(map.mapId, targetMap);
            string next = west ?? index.NextStep(map.mapId, targetMap);
            if (next == null) return null;
            // 11/08 기우: 같은 맵으로 가는 출구가 여러 토막이면(아라비아 빈 사막 남쪽 등) 목록 맨 앞 토막의 가운데가 아니라
            // 플레이어에게 가장 가까운 출구 칸을 가리킨다. 출구가 하나인 맵은 그 출구 안에서 가장 가까운 칸.
            var player = PlayerController.Instance;
            Vector3Int me = player != null ? map.WorldToCell(player.transform.position) : Vector3Int.zero;
            Vector3? best = null; int bestD = int.MaxValue;
            // 11/11 은샘: 만국 장터 큰길(WestRoad)로 갈 때는 맵 끝 출구 중 길이 닿는 칸(길목)만 목적지로 한다.
            //   장단→송악처럼 맵 끝 전체가 출구면 가장 가까운 끝 칸을 골라 길을 벗어나 가로질렀다. (나루→개경 같은 지역 안 길은 그대로)
            var roads = RoadGrid.Current;
            if (west != null && player != null && roads != null)
                foreach (var e in MapExit.All)
                {
                    if (e.targetScene != next) continue;
                    var o = map.WorldToCell(e.transform.position);
                    for (int x = 0; x < e.size.x; x++)
                        for (int y = 0; y < e.size.y; y++)
                        {
                            var c = o + new Vector3Int(x, y, 0);
                            if (roads.GradeAt(c) <= 0) continue;
                            int dist = Mathf.Abs(c.x - me.x) + Mathf.Abs(c.y - me.y);
                            if (dist < bestD) { bestD = dist; best = map.CellCenter(c); }
                        }
                }
            if (!best.HasValue)
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
                return new Result { position = best.Value, label = $"{where} 방면", exit = true };
            }
            return null;
        }
    }
}
