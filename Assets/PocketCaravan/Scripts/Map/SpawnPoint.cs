using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>맵에 들어왔을 때 서는 자리. "S"는 게임 시작 자리.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public const string StartId = "S";
        static readonly List<SpawnPoint> all = new List<SpawnPoint>();

        public string id = StartId;

        void Awake() => all.Add(this);
        void OnDestroy() => all.Remove(this);

        public static SpawnPoint Find(string id)
        {
            foreach (var s in all) if (s != null && s.id == id) return s;
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
