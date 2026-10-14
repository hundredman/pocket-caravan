using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Road Tile Set", fileName = "RoadTiles")]
    public class RoadTileSet : ScriptableObject
    {
        [Tooltip("0: 비움, 1: 오솔길, 2: 길, 3: 큰길")]
        public UnityEngine.Tilemaps.TileBase[] byGrade = new UnityEngine.Tilemaps.TileBase[4];

        public UnityEngine.Tilemaps.TileBase ForGrade(int grade) =>
            grade <= 0 || byGrade == null || grade >= byGrade.Length ? null : byGrade[grade];

        public int GradeOf(UnityEngine.Tilemaps.TileBase tile)
        {
            if (tile == null || byGrade == null) return 0;
            for (int i = 1; i < byGrade.Length; i++) if (byGrade[i] == tile) return i;
            return 0;
        }
    }
}
