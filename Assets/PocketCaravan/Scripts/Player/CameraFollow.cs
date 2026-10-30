using UnityEngine;
using System.Collections.Generic;

namespace PocketCaravan
{
    /// <summary>플레이어를 따라가며 담당 지역 출구에서는 화면 가장자리에 경계 면을 맞춘다.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        [Tooltip("따라붙는 데 걸리는 시간(초). 0이면 바로 붙는다")] public float smoothTime = 0.12f;
        Camera cam;
        Vector3 vel;
        bool snap = true;
        MapRoot cachedMap;
        MapRoot framedExitMap;
        int cachedExitCount = -1;
        readonly List<MapExit> boundaries = new List<MapExit>();

        void CacheBoundaries(MapRoot map)
        {
            if(cachedMap==map&&cachedExitCount==MapExit.All.Count)return;
            cachedMap=map;cachedExitCount=MapExit.All.Count;boundaries.Clear();
            if(map==null||GoryeoTravelTransition.RegionOf(map.mapId)==null)return;
            foreach(var exit in MapExit.All)
                if(GoryeoTravelTransition.RegionOf(exit.targetScene)!=null)boundaries.Add(exit);
        }

        Vector3 FrameNearbyBoundaries(Vector3 position)
        {
            var map=MapRoot.Current;CacheBoundaries(map);
            var player=PlayerController.Instance;
            if(boundaries.Count==0||player==null)return position;
            Vector2 feet=player.Motor.Feet;float h=cam.orthographicSize,w=h*cam.aspect;
            // Each boundary contributes a continuous limit, including across stair-step seams.
            // Hard row cutoffs selected a completely different plane after a tiny sideways step.
            float left=float.NegativeInfinity,right=float.PositiveInfinity;
            float bottom=float.NegativeInfinity,top=float.PositiveInfinity;
            foreach(var exit in boundaries)
            {
                Rect r=exit.WorldRect;int axis=exit.edge=="W"?0:exit.edge=="E"?1:exit.edge=="S"?2:exit.edge=="N"?3:-1;
                if(axis<0)continue;
                float lateral=axis<2?Mathf.Max(Mathf.Max(r.yMin-feet.y,feet.y-r.yMax),0):Mathf.Max(Mathf.Max(r.xMin-feet.x,feet.x-r.xMax),0);
                float falloff=lateral*2f;
                switch(axis)
                {
                    case 0:left=Mathf.Max(left,r.xMin+w-falloff);break;
                    case 1:right=Mathf.Min(right,r.xMax-w+falloff);break;
                    case 2:bottom=Mathf.Max(bottom,r.yMin+h-falloff);break;
                    case 3:top=Mathf.Min(top,r.yMax-h+falloff);break;
                }
            }
            // The viewport cannot fit both sides of a narrow neck. Keep the nearest centre
            // in the interval instead of jumping to the opposite side when facing changes.
            left=Mathf.Min(left,feet.x+w);right=Mathf.Max(right,feet.x-w);
            bottom=Mathf.Min(bottom,feet.y+h);top=Mathf.Max(top,feet.y-h);
            position.x=Mathf.Clamp(position.x,Mathf.Min(left,right),Mathf.Max(left,right));
            position.y=Mathf.Clamp(position.y,Mathf.Min(bottom,top),Mathf.Max(bottom,top));
            return position;
        }

        /// <summary>마지막 발밑 접촉 시 해당 출구 면을 화면 가장자리에 정확히 맞춘다.</summary>
        public void FrameExit(MapExit exit)
        {
            if(cam==null)cam=GetComponent<Camera>();
            Vector3 p=transform.position;Rect r=exit.WorldRect;float h=cam.orthographicSize,w=h*cam.aspect;
            switch(exit.edge){case "E":p.x=r.xMax-w;break;case "W":p.x=r.xMin+w;break;case "N":p.y=r.yMax-h;break;case "S":p.y=r.yMin+h;break;}
            transform.position=p;vel=Vector3.zero;
            framedExitMap=MapRoot.Current;
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (target == null)
            {
                var p = FindAnyObjectByType<PlayerController>();
                if (p != null) target = p.transform;
            }
        }

        public void SnapNow() { framedExitMap=null; snap = true; LateUpdate(); }

        void LateUpdate()
        {
            if (target == null) return;
            // Keep the final contact frame steady throughout the outgoing fade.
            if (framedExitMap != null && framedExitMap == MapRoot.Current) return;
            Vector3 p = target.position;
            var map = MapRoot.Current;
            if (map != null)
            {
                var b = map.WorldBounds;
                float h = cam.orthographicSize, w = h * cam.aspect;
                p.x = b.size.x > 2 * w ? Mathf.Clamp(p.x, b.min.x + w, b.max.x - w) : b.center.x;
                p.y = b.size.y > 2 * h ? Mathf.Clamp(p.y, b.min.y + h, b.max.y - h) : b.center.y;
            }
            p=FrameNearbyBoundaries(p);
            p.z = -10f;
            if (snap || smoothTime <= 0f) { transform.position = p; vel = Vector3.zero; snap = false; }
            else transform.position = Vector3.SmoothDamp(transform.position, p, ref vel, smoothTime);
        }
    }
}
