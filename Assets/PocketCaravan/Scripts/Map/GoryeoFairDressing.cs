using System;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>중앙 동쪽 기존 큰 장의 그림만 교체한다. 일정/보너스/통행/저장은 공용 규칙 그대로.</summary>
    public sealed class GoryeoFairDressing:MonoBehaviour
    {
        [Serializable]public class Stall{public SpriteRenderer renderer;public Sprite normal,fair;}
        public Stall[] stalls=Array.Empty<Stall>();
        MapRoot map;bool? previous;
        void Awake(){map=GetComponent<MapRoot>();}
        void LateUpdate()
        {
            if(map==null||GameManager.I==null)return;
            bool fair=(map.mapId=="transit_woodtown"||map.mapId=="transit_ferry")&&CentralFair.IsFairMap(map.mapId);
            if(previous==fair)return;
            foreach(var s in stalls)if(s.renderer!=null)s.renderer.sprite=fair?s.fair:s.normal;
            previous=fair;
        }
        void OnDisable(){foreach(var s in stalls)if(s.renderer!=null)s.renderer.sprite=s.normal;previous=null;}
    }
}
