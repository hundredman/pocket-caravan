using System;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Opt-in approved whole sprites and occupied cells. No weather or event-rule changes.</summary>
    public sealed class GoryeoLogVisuals:MonoBehaviour
    {
        [Serializable]public class Entry
        {
            public string placementId;public Sprite sprite;public Vector2Int[] occupied;
            public bool Contains(int x,int y){foreach(var c in occupied)if(c.x==x&&c.y==y)return true;return false;}
        }
        public Entry[] entries=Array.Empty<Entry>();
        public Entry Find(ObstaclePlacement p,ObstacleDefinition d)
        {
            if(d.id!="fallen_log")return null;
            foreach(var e in entries)if(e.placementId==p.id)return e;
            return null;
        }
    }
}
