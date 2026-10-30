using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>마을 공간의 평상시 모습. 이벤트 종료 시 같은 배치와 통행 영역을 복원한다.</summary>
    public sealed class VillageDressing : MonoBehaviour
    {
        [Serializable] public class Space
        {
            public string id;
            public GameObject normal;
            public RectInt[] footprints = Array.Empty<RectInt>();
            [NonSerialized] public GameObject eventInstance;
            [NonSerialized] public SpriteRenderer[] renderers;
        }
        public Space[] spaces = Array.Empty<Space>();

        void Awake()
        {
            foreach (var space in spaces)
                space.renderers = space.normal != null ? space.normal.GetComponentsInChildren<SpriteRenderer>() : Array.Empty<SpriteRenderer>();
        }

        public bool Blocks(Vector3Int cell)
        {
            foreach (var space in spaces)
                if (space.normal != null && space.normal.activeSelf)
                    foreach (var rect in space.footprints)
                        if (rect.Contains(new Vector2Int(cell.x, cell.y))) return true;
            return false;
        }

        /// <summary>이벤트 그림은 평상시 그림 대신 표시한다. null이면 원래 모습을 복원한다.
        /// 이벤트 자체의 충돌은 이벤트 프리팹/기존 장애물 시스템이 담당한다.</summary>
        public void SetEvent(string id, GameObject eventPrefab)
        {
            var space = Array.Find(spaces, s => s.id == id);
            if (space == null) throw new ArgumentException("Unknown village space: " + id);
            if (space.eventInstance != null)
            {
                space.eventInstance.SetActive(false);
                Destroy(space.eventInstance);
                space.eventInstance = null;
            }
            space.normal.SetActive(eventPrefab == null);
            if (eventPrefab != null)
            {
                space.eventInstance = Instantiate(eventPrefab, space.normal.transform.parent, false);
                space.eventInstance.transform.localPosition = space.normal.transform.localPosition;
                space.eventInstance.SetActive(true);
            }
        }

        void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            // Whole sprites retain their authored pixels; sort using the bottom edge.
            foreach (var space in spaces)
                if (space.normal != null && space.normal.activeInHierarchy)
                    foreach (var renderer in space.renderers)
                        renderer.sortingOrder = renderer.bounds.min.y < player.Motor.Feet.y ? 12 : 4;
        }
    }
}
