using UnityEngine;

namespace PocketCaravan
{
    /// <summary>플레이어를 따라가되 맵 밖은 보이지 않게 막는다.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [Tooltip("따라붙는 데 걸리는 시간(초). 0이면 바로 붙는다")] public float smoothTime = 0.12f;
        Camera cam;
        Vector3 vel;
        bool snap = true;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target == null)
            {
                var p = FindAnyObjectByType<PlayerController>();
                if (p != null) target = p.transform;
            }
        }

        public void SnapNow() { snap = true; LateUpdate(); }

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
            if (snap || smoothTime <= 0f) { transform.position = p; vel = Vector3.zero; snap = false; }
            else transform.position = Vector3.SmoothDamp(transform.position, p, ref vel, smoothTime);
        }
    }
}
