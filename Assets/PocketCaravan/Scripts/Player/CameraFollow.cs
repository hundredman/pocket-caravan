using UnityEngine;

namespace PocketCaravan
{
    /// <summary>플레이어를 따라가되 맵 밖은 보이지 않게 막는다.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target == null)
            {
                var p = FindAnyObjectByType<PlayerController>();
                if (p != null) target = p.transform;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 p = target.position;
            var map = MapRoot.Current;
            if (map != null)
            {
                var b = map.WorldBounds;
                float h = cam.orthographicSize, w = h * cam.aspect;
                p.x = b.size.x > 2 * w ? Mathf.Clamp(p.x, b.min.x + w, b.max.x - w) : b.center.x;
                p.y = b.size.y > 2 * h ? Mathf.Clamp(p.y, b.min.y + h, b.max.y - h) : b.center.y;
            }
            p.z = -10f;
            transform.position = p;
        }
    }
}
