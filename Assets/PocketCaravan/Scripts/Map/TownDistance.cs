using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// All price/contract distance queries enter here. current preserves the two legacy
    /// metrics; layoutD uses one rounded walking-time distance. Switch with the maps at B5.
    /// </summary>
    public static class TownDistance
    {
        [Serializable] public class Node { public string id; }
        [Serializable] public class Edge { public string from, to; public float seconds; }
        [Serializable] public class Settings
        {
            public string mode = "current";
            public float ratio;
            public Node[] towns;
            public Edge[] roads;
            public Edge[] walkingDistances;
        }

        public enum LegacyMetric { EconomicCoordinates, MapConnections }
        static Settings cached;
        static Settings Configuration
        {
            get
            {
                if (cached == null)
                {
                    var source = Resources.Load<TextAsset>("TownRoutes");
                    if (source == null) throw new InvalidOperationException("TownRoutes 설정이 없습니다.");
                    var loaded = JsonUtility.FromJson<Settings>(source.text);
                    Validate(loaded);
                    cached = loaded;
                }
                return cached;
            }
        }

        public static void Validate(Settings settings)
        {
            if (settings == null || (settings.mode != "current" && settings.mode != "layoutD"))
                throw new InvalidOperationException("지원하지 않는 거리 설정입니다.");
            if (settings.ratio <= 0 || float.IsNaN(settings.ratio) || float.IsInfinity(settings.ratio)
                || settings.towns == null || settings.roads == null)
                throw new InvalidOperationException("길 연결망 설정이 올바르지 않습니다.");
            var ids = new HashSet<string>();
            foreach (var node in settings.towns)
                if (node == null || string.IsNullOrEmpty(node.id) || !ids.Add(node.id))
                    throw new InvalidOperationException("마을 ID가 중복되거나 비었습니다.");
            foreach (var edge in settings.roads)
                if (edge == null || !ids.Contains(edge.from) || !ids.Contains(edge.to) || edge.from == edge.to
                    || edge.seconds <= 0 || float.IsNaN(edge.seconds) || float.IsInfinity(edge.seconds))
                    throw new InvalidOperationException("길 구간이 올바르지 않습니다.");
            if (settings.walkingDistances != null)
            {
                var pairs = new HashSet<string>();
                foreach (var pair in settings.walkingDistances)
                {
                    if (pair == null || !ids.Contains(pair.from) || !ids.Contains(pair.to) || pair.from == pair.to
                        || pair.seconds <= 0 || float.IsNaN(pair.seconds) || float.IsInfinity(pair.seconds))
                        throw new InvalidOperationException("실제 보행 거리 표가 올바르지 않습니다.");
                    string key = string.CompareOrdinal(pair.from, pair.to) < 0 ? pair.from + "\n" + pair.to : pair.to + "\n" + pair.from;
                    if (!pairs.Add(key)) throw new InvalidOperationException("실제 보행 거리 쌍이 중복됐습니다.");
                }
                if (pairs.Count != ids.Count * (ids.Count - 1) / 2)
                    throw new InvalidOperationException("실제 보행 거리 쌍이 누락됐습니다.");
            }
        }

        public static int Between(string from, string to, Vector2Int fromCoord = default,
            Vector2Int toCoord = default, WorldIndex world = null,
            LegacyMetric legacy = LegacyMetric.EconomicCoordinates, Settings settings = null)
        {
            var config = settings ?? Configuration;
            if (settings != null) Validate(config);
            // layoutD 표는 고려·중앙 동쪽 마을만 있다. 다른 지역 마을(통합 브랜치의 베네치아·아라비아·북유럽)이나
            // 그 마을과의 쌍은 예전 방식 그대로 계산한다(M4 2026-10-28: 표에 없는 마을에서 예외가 나지 않게).
            if (config.mode == "layoutD" && Covers(config, from) && Covers(config, to))
            {
                float seconds = WalkingSeconds(config, from, to);
                // Self-distance remains zero, like both legacy metrics; distinct towns clamp to one.
                return from == to ? 0 : Mathf.Max(1, Mathf.RoundToInt(seconds * config.ratio));
            }
            if (legacy == LegacyMetric.MapConnections) return CurrentSteps(world, from, to);
            var d = fromCoord - toCoord;
            return Mathf.Abs(d.x) + Mathf.Abs(d.y);
        }

        static bool Covers(Settings config, string town) =>
            config.towns != null && Array.Exists(config.towns, n => n.id == town);

        public static float WalkingSeconds(Settings config, string from, string to)
        {
            if (config.walkingDistances != null)
            {
                if (from == to && Array.Exists(config.towns, n => n.id == from)) return 0;
                foreach (var pair in config.walkingDistances)
                    if ((pair.from == from && pair.to == to) || (pair.from == to && pair.to == from)) return pair.seconds;
                throw new InvalidOperationException("실제 보행 거리 표에 없는 마을입니다.");
            }
            var costs = new Dictionary<string, float>();
            foreach (var node in config.towns) costs[node.id] = float.PositiveInfinity;
            if (!costs.ContainsKey(from) || !costs.ContainsKey(to))
                throw new InvalidOperationException("새 배치에 등록되지 않은 마을입니다.");
            costs[from] = 0;
            var visited = new HashSet<string>();
            while (visited.Count < costs.Count)
            {
                string next = null; float best = float.PositiveInfinity;
                foreach (var pair in costs)
                    if (!visited.Contains(pair.Key) && pair.Value < best) { next = pair.Key; best = pair.Value; }
                if (next == null) break;
                if (next == to) return best;
                visited.Add(next);
                foreach (var edge in config.roads)
                {
                    string neighbor = edge.from == next ? edge.to : edge.to == next ? edge.from : null;
                    if (neighbor != null) costs[neighbor] = Mathf.Min(costs[neighbor], best + edge.seconds);
                }
            }
            throw new InvalidOperationException("새 배치의 마을 사이에 길이 없습니다.");
        }

        static int CurrentSteps(WorldIndex world, string fromTown, string toTown)
        {
            if (world == null) return -1;
            string from = world.MapOf("market", fromTown), to = world.MapOf("market", toTown);
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || world.Map(from) == null || world.Map(to) == null) return -1;
            var queue = new Queue<string>(); var seen = new Dictionary<string, int> { [from] = 0 };
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (id == to) return seen[id];
                var map = world.Map(id);
                if (map == null) continue;
                foreach (string neighbor in map.neighbors)
                    if (!seen.ContainsKey(neighbor)) { seen[neighbor] = seen[id] + 1; queue.Enqueue(neighbor); }
            }
            return -1;
        }
    }
}
