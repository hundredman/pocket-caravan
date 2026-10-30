using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>작은 논 구획의 앞줄만 플레이어 앞에 그린다. 충돌·그림·저장은 바꾸지 않는다.</summary>
    public sealed class CropDepth : MonoBehaviour
    {
        public Tilemap lower, upper;
        public RectInt[] patches = Array.Empty<RectInt>();
        struct Cell { public Vector3Int position; public TileBase tile; public bool originalUpper, currentUpper; }
        readonly List<Cell> cells = new List<Cell>();
        Vector3Int previous;
        bool hasPrevious;

        void Start()
        {
            if (lower == null || upper == null) { enabled = false; return; }
            var seen = new HashSet<Vector3Int>();
            foreach (var rect in patches)
                for (int y = rect.yMin; y < rect.yMax; y++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        var p = new Vector3Int(x, y, 0);
                        if (!seen.Add(p)) continue;
                        var high = upper.GetTile(p); var low = lower.GetTile(p);
                        if (high != null && low != null)
                            throw new InvalidOperationException("CropDepth requires an isolated crop cell: " + p);
                        if (high == null && low == null) continue;
                        cells.Add(new Cell { position = p, tile = high != null ? high : low, originalUpper = high != null, currentUpper = high != null });
                    }
        }

        void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null || player.Motor == null || MapRoot.Current == null) return;
            Apply(player.Motor.Cell);
        }

        public void Apply(Vector3Int feet)
        {
            if (hasPrevious && previous.y == feet.y) return;
            previous = feet; hasPrevious = true;
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                // Only foreground rows cover the feet; the rear rows leave the face readable.
                bool high = c.position.y <= feet.y;
                if (high == c.currentUpper) continue;
                (high ? lower : upper).SetTile(c.position, null);
                (high ? upper : lower).SetTile(c.position, c.tile);
                c.currentUpper = high; cells[i] = c;
            }
        }

        void OnDisable()
        {
            if (lower == null || upper == null) return;
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                (c.originalUpper ? lower : upper).SetTile(c.position, null);
                (c.originalUpper ? upper : lower).SetTile(c.position, c.tile);
                c.currentUpper = c.originalUpper; cells[i] = c;
            }
            hasPrevious = false;
        }
    }
}
