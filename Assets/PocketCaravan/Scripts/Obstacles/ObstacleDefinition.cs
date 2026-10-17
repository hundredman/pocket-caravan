using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    public enum ObstacleMode { Barrier, Slow, Threat, TimedBarrier, TimedSlow, Toll, VehicleGate, LoadGate }
    public enum ObstacleAction { None, Clear, Repair, Pay }
    [CreateAssetMenu(menuName="Pocket Caravan/Obstacles/Definition")]
    public class ObstacleDefinition : ScriptableObject
    {
        public string id,displayName,category;
        [TextArea] public string description;
        public ObstacleMode mode;
        public ObstacleAction action;
        public int width=3,height=3;
        public Sprite sprite;
        [Serializable] public class Visual { public string environmentId; public Sprite sprite; }
        public List<Visual> visuals=new List<Visual>();
        [Range(.1f,1)] public float walkSpeed=1,cartSpeed=1,traction=1;
        public Vector2 drift;
        [Range(0,1)] public float staminaDrain;
        [Range(.1f,1)] public float visibility=1;
        public float workSeconds;
        public int money;
        public string itemId;
        public int itemCount;
        public float activeSeconds=6,openSeconds=5;
        public float threatSpeed=2.7f,threatRadius=6,leash=8;
        public float weightLimit=12;
        public bool Timed => mode==ObstacleMode.TimedBarrier || mode==ObstacleMode.TimedSlow;
        public bool Active(float elapsed,float offset=0) => !Timed || Mathf.Repeat(elapsed+offset,Mathf.Max(.1f,activeSeconds+openSeconds))<Mathf.Max(0,activeSeconds);
        public bool Blocks(PlayerState player)
        {
            if(mode==ObstacleMode.VehicleGate)return player!=null && player.vehicle!=null && player.vehicle.id!="walk";
            if(mode==ObstacleMode.LoadGate)return player!=null && player.TotalWeight>weightLimit;
            return mode==ObstacleMode.Barrier || mode==ObstacleMode.TimedBarrier || mode==ObstacleMode.Toll;
        }
        public float Speed(PlayerState player) => Mathf.Clamp(player!=null && player.vehicle!=null && player.vehicle.id!="walk"?cartSpeed:walkSpeed,.1f,1);
        public Sprite SpriteFor(string environment)
        { foreach(var v in visuals)if(v!=null && v.environmentId==environment && v.sprite!=null)return v.sprite;return sprite; }
    }
    [Serializable] public class ObstaclePlacement
    {
        public string id,definitionId;
        [Tooltip("JSON 타일 좌표: 왼쪽 위에서 x/y. 0 크기는 정의의 기본 크기")]
        public int x,y,width,height;
        public bool enabled=true;
        public float phaseOffset;
        public RectInt Rect(int mapHeight,ObstacleDefinition d) => new RectInt(x,mapHeight-y-(height>0?height:d.height),width>0?width:d.width,height>0?height:d.height);
    }
}
