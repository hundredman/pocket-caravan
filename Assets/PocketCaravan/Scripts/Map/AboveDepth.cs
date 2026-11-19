using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>
    /// 11/19 은샘 "순록 발이 땅에 있는데도 기둥 윗부분에 뿔이 가려진다": 맵의 Above 타일(기둥·나무·지붕 윗부분, 순서 20)은
    /// 사람(10·11)보다 늘 위에 그려진다. 사람은 키가 작아 앞에 서도 윗부분과 겹치지 않지만, 짐승·마차는 커서 겹친다.
    /// 규칙은 사람과 같다: 발이 그 물건 밑동보다 아래(앞)면 물건 앞에, 밑동을 지나 위(뒤)면 물건에 가려진다.
    /// 물건 = Above·Objects 타일이 이어진 덩어리, 밑동 = 그 덩어리의 가장 아래 줄. 밑동 없이 떠 있는 윗부분은 늘 위에 (전과 같다).
    /// </summary>
    public static class AboveDepth
    {
        /// <summary>앞에 설 때 더하는 순서. 사람 10·11 + 12 → 22·23 (Above 20 위)</summary>
        public const int Raise = 12;

        static readonly List<Tilemap> above = new List<Tilemap>();
        static readonly List<Tilemap> objects = new List<Tilemap>();
        static MapRoot cachedFor;
        static float recache;

        static void Cache()
        {
            var root = MapRoot.Current;
            if (root == cachedFor && Time.unscaledTime < recache && above.TrueForAll(t => t != null)) return;
            cachedFor = root; recache = Time.unscaledTime + 2f;
            above.Clear(); objects.Clear(); bases.Clear();
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<Tilemap>())
            {
                if (t.name == "Above") above.Add(t);
                else if (t.name == "Objects") objects.Add(t);
            }
        }

        /// <summary>그림(bounds, 발 높이 feetY)이 겹친 물건 윗부분과 어떻게 놓이는지.
        /// front = 앞에 선 물건과 겹침, behind = 뒤에 선 물건과 겹침</summary>
        public static void Check(Bounds b, float feetY, out bool front, out bool behind)
        {
            front = behind = false;
            Cache();
            for (int m = 0; m < above.Count; m++)
            {
                var a = above[m]; if (a == null) continue;
                Tilemap o = null;
                foreach (var t in objects) if (t != null && t.layoutGrid == a.layoutGrid) { o = t; break; }
                var c0 = a.WorldToCell(b.min); var c1 = a.WorldToCell(b.max);
                if ((c1.x - c0.x + 1) * (c1.y - c0.y + 1) > 64) continue;   // 그림이 이상하게 크면 건너뛴다
                for (int y = c0.y; y <= c1.y; y++)
                    for (int x = c0.x; x <= c1.x; x++)
                    {
                        var c = new Vector3Int(x, y, 0);
                        if (!a.HasTile(c)) continue;
                        int row = BaseRow(a, o, c);
                        if (row == Floating) { behind = true; continue; }   // 밑동(Objects) 없이 떠 있는 윗부분(처마·아치 등)은 사람처럼 늘 그 밑으로
                        float baseY = a.CellToWorld(new Vector3Int(x, row, 0)).y;
                        // 밑동 칸 아래 끝에서 1/4칸 위까지는 앞 (짐승은 길을 따라와 밑동 칸에 발이 살짝 걸칠 수 있다)
                        if (feetY < baseY + .25f) front = true; else behind = true;
                        if (front && behind) return;
                    }
            }
        }

        /// <summary>겹친 물건이 있으면 모두 앞에 섰을 때만 true (하나라도 뒤면 그 물건에 가려지는 게 맞다)</summary>
        public static bool InFront(Bounds b, float feetY)
        {
            Check(b, feetY, out var front, out var behind);
            return front && !behind;
        }

        /// <summary>뒤에 선 물건 윗부분과 겹치면 true</summary>
        public static bool Behind(Bounds b, float feetY)
        {
            Check(b, feetY, out _, out var behind);
            return behind;
        }

        const int Floating = int.MinValue;
        static readonly Queue<Vector3Int> open = new Queue<Vector3Int>();
        static readonly HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
        static readonly Dictionary<Vector3Int, int> bases = new Dictionary<Vector3Int, int>();   // 칸 → 물건 밑동 줄 (맵이 바뀌면 비운다)
        static bool Has(Tilemap a, Tilemap o, Vector3Int c) => a.HasTile(c) || (o != null && o.HasTile(c));

        /// <summary>이 칸이 속한 물건(Above·Objects 타일이 상하좌우로 이어진 덩어리)의 가장 아래 줄. 한 번 찾은 덩어리는 모든 칸에 기억한다.
        /// 큰 덩어리는 200칸까지만 본다. Objects 타일이 하나도 없으면 Floating</summary>
        static int BaseRow(Tilemap a, Tilemap o, Vector3Int start)
        {
            if (bases.TryGetValue(start, out int known)) return known;
            open.Clear(); seen.Clear();
            open.Enqueue(start); seen.Add(start);
            int low = start.y; bool grounded = false;
            while (open.Count > 0 && seen.Count < 200)
            {
                var c = open.Dequeue();
                if (c.y < low) low = c.y;
                if (!grounded && o != null && o.HasTile(c)) grounded = true;
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (seen.Contains(n) || !Has(a, o, n)) continue;
                    seen.Add(n); open.Enqueue(n);
                }
            }
            int row = grounded ? low : Floating;
            if (bases.Count > 20000) bases.Clear();
            foreach (var c in seen) bases[c] = row;
            return row;
        }
        static readonly Vector3Int[] Dirs = { Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.up };
    }
}
