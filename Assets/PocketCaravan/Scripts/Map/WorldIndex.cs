using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 세계 지도 색인: 어느 맵에 누가 있는지, 맵끼리 어떻게 이어지는지.
    /// 맵 씬을 만들 때(MapJsonBuilder) 자동으로 채워지고, 길 안내(Navigator)가 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Caravan/World Index", fileName = "WorldIndex")]
    public class WorldIndex : ScriptableObject
    {
        [Serializable]
        public class Place
        {
            public string kind;   // "npc", "market", "board"
            public string id;     // npc id 또는 마을 id
            public string mapId;
        }

        [Serializable]
        public class MapInfo
        {
            public string mapId;
            public string displayName;
            public int height, legacyHeight;
            public Vector2Int coord;
            public string town;   // 장터가 있는 마을 id (없으면 빈칸)
            public List<string> neighbors = new List<string>();
        }

        public List<MapInfo> maps = new List<MapInfo>();
        public List<Place> places = new List<Place>();

        static WorldIndex cached;
        public static WorldIndex Get() => cached != null ? cached : (cached = Resources.Load<WorldIndex>("WorldIndex"));

        public MapInfo Map(string id) => maps.Find(m => m.mapId == id);
        public string MapName(string id) => Map(id)?.displayName ?? id;
        public string MapOf(string kind, string id) => places.Find(p => p.kind == kind && p.id == id)?.mapId;

        /// <summary>두 마을의 장터가 출구로 이어져 있는지. 장터 위치를 모르는 마을은 기존 동작 호환을 위해 true.
        /// 접속부가 없는 다른 지역(예: 아라비아 첫 구간)과의 원격 교역·시세 소문을 막는 데 쓴다.</summary>
        public bool MarketsConnected(string townA, string townB)
        {
            string a = MapOf("market", townA), b = MapOf("market", townB);
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || Map(a) == null || Map(b) == null) return true;
            return a == b || NextStep(a, b) != null;
        }

        /// <summary>from에서 to로 갈 때 다음에 들어갈 맵 (가장 짧은 길). 없으면 null.</summary>
        public string NextStep(string from, string to)
        {
            if (from == to || Map(from) == null) return null;
            var prev = new Dictionary<string, string> { [from] = null };
            var q = new Queue<string>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == to) break;
                var info = Map(cur);
                if (info == null) continue;
                foreach (var n in info.neighbors)
                    if (!prev.ContainsKey(n)) { prev[n] = cur; q.Enqueue(n); }
            }
            if (!prev.ContainsKey(to)) return null;
            var step = to;
            while (prev[step] != from) step = prev[step];
            return step;
        }
    }
}
