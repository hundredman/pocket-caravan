using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 길 위 돌발 사건을 고른다. 맵에 들어올 때 꺼진 사건들 중 하나를 켠다 (맵마다 게임 시간 8시간에 한 번).
    /// </summary>
    public class RoadEventDirector : MonoBehaviour
    {
        [Range(0, 1)] public float chance = 0.7f;
        public List<RoadEvent> events = new List<RoadEvent>();

        void Start()
        {
            var gm = GameManager.I;
            if (gm == null || MapRoot.Current == null || events.Count == 0) return;
            string map = MapRoot.Current.mapId;
            if (!gm.EventReady(map) || Random.value > chance) return;
            var e = events[Random.Range(0, events.Count)];
            e.gameObject.SetActive(true);
            gm.EventHappened(map);
        }
    }
}
