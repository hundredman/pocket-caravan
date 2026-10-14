using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 맵에 들어왔을 때 서는 자리. 위치는 첫 칸의 중심이고 size만큼 이어진다.
    /// 이웃 맵 출구와 폭이 같으면, 나간 자리와 같은 위치로 들어온다. "S"는 게임 시작 자리.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        public const string StartId = "S0";
        static readonly List<SpawnPoint> all = new List<SpawnPoint>();

        public string id = StartId;
        public Vector2Int size = Vector2Int.one;

        void Awake() => all.Add(this);
        void OnDestroy() => all.Remove(this);

        public static SpawnPoint Find(string id)
        {
            foreach (var s in all) if (s != null && s.id == id) return s;
            return null;
        }

        /// <summary>출구에서 넘어온 위치(첫 칸 기준 거리)를 이 자리의 같은 위치로 바꾼다.</summary>
        public Vector3 PositionFor(float offset)
        {
            var p = transform.position;
            if (size.x > 1) p.x += Mathf.Clamp(offset, 0f, size.x - 1);
            else if (size.y > 1) p.y += Mathf.Clamp(offset, 0f, size.y - 1);
            return p;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            var end = transform.position + new Vector3(size.x - 1, size.y - 1, 0);
            Gizmos.DrawLine(transform.position, end);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
