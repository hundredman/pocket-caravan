using UnityEngine;

namespace PocketCaravan
{
    /// <summary>주인공을 중심으로 따라가며 맵 가장자리와 코너에서는 화면을 고정한다.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [Tooltip("따라붙는 데 걸리는 시간(초). 0이면 바로 붙는다")] public float smoothTime = 0.12f;
        Camera cam;
        Vector3 vel;
        bool snap = true;
        Vector3 lastTarget; bool hasLast;
        [Tooltip("대상이 한 프레임에 이만큼(칸) 넘게 움직이면 순간이동으로 보고 바로 붙는다")] public float warpDistance = 3f;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
        }

        Vector3 Confine(Vector3 position)
        {
            var map = MapRoot.Current;
            if (map != null)
            {
                var bounds = map.WorldBounds;
                // 2026-11-04(고려·중앙 동쪽): 이웃 맵 그림(CameraBackdrop)이 깔린 맵은 그 범위까지 카메라를 허용한다.
                // 땅이 이어진 맵 경계를 걸어서 넘을 때 카메라가 맵 끝에 걸렸다가 튀지 않게 한다. 배경이 없는 다른 지역은 그대로.
                var backdrop = BackdropOf(map);
                if (backdrop != null) bounds.Encapsulate(backdrop.Value);
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;
                position.x = bounds.size.x > 2 * halfWidth
                    ? Mathf.Clamp(position.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth)
                    : bounds.center.x;
                position.y = bounds.size.y > 2 * halfHeight
                    ? Mathf.Clamp(position.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight)
                    : bounds.center.y;
            }
            position.z = -10f;
            return position;
        }

        MapRoot backdropMap; Bounds? backdropBounds;
        Bounds? BackdropOf(MapRoot map)
        {
            if (backdropMap == map) return backdropBounds;
            backdropMap = map; backdropBounds = null;
            foreach (var layer in map.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>())
            {
                if (layer.name != "CameraBackdrop") continue;
                layer.CompressBounds();
                var b = layer.cellBounds;
                if (b.size.x <= 0 || b.size.y <= 0) break;
                var bounds = new Bounds();
                bounds.SetMinMax(layer.CellToWorld(b.min), layer.CellToWorld(b.max));
                backdropBounds = bounds;
                break;
            }
            return backdropBounds;
        }

        public void SnapNow() { snap = true; LateUpdate(); }

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 p = target.position;
            // 2026-10-29(북유럽): 순간이동(맵 도착·저장 복원) 뒤 카메라가 미끄러져 따라오지 않게
            if (hasLast && (p - lastTarget).sqrMagnitude > warpDistance * warpDistance) snap = true;
            lastTarget = p; hasLast = true;
            if (cam == null) cam = GetComponent<Camera>();
            Vector3 desired = Confine(target.position);
            if (snap || smoothTime <= 0f)
            {
                transform.position = desired;
                vel = Vector3.zero;
                snap = false;
            }
            else
            {
                Vector3 position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
                // Viewport/zoom changes must not leave the interpolated camera outside the map.
                transform.position = Confine(position);
            }
        }
    }
}
