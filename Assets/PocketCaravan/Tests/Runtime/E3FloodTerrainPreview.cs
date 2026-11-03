#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Tilemaps;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace PocketCaravan.EditorTools
{
    /// <summary>Editor-only terrain experiment. No scheduling, saves or production hook.</summary>
    public sealed class E3FloodTerrainPreview : MonoBehaviour
    {
        [Serializable] public class Cell
        {
            public Vector3Int position;
            public TileBase dryGround,wetGround,dryDeco,wetDeco,dryBlocked,wetBlocked;
            public int dryGrade,wetGrade;
        }
        public Cell[] cells;
        public bool Flooded {get;private set;}
        public bool showControls=true;
        public string message="";
        MapRoot Map=>GetComponent<MapRoot>();
        public bool SetFlooded(bool value)
        {
            if(!Application.isPlaying || !Map.previewOnly)throw new InvalidOperationException("Preview Play session only");
            if(value==Flooded)return true;
            // Preview policy: defer instead of teleporting an actor or trapping them in water.
            var actor=PlayerController.Instance;
            if(value && actor!=null)
            {
                var motor=actor.Motor;var half=motor.feetSize*.5f;
                foreach(var c in cells)if(c.dryBlocked==null && c.wetBlocked!=null)
                {
                    var b=new Bounds(Map.CellCenter(c.position),new Vector3(1,1,100));
                    var p=new Vector3(motor.Feet.x,motor.Feet.y,0);
                    if(Mathf.Abs(p.x-b.center.x)<.5f+half.x && Mathf.Abs(p.y-b.center.y)<.5f+half.y)
                    {message="잠길 길 밖으로 이동한 뒤 전환하세요.";return false;}
                }
            }
            var deco=transform.Find("Deco").GetComponent<Tilemap>();var roads=GetComponent<RoadGrid>();
            foreach(var c in cells)
            {
                Map.ground.SetTile(c.position,value?c.wetGround:c.dryGround);
                deco.SetTile(c.position,value?c.wetDeco:c.dryDeco);
                Map.blocked.SetTile(c.position,value?c.wetBlocked:c.dryBlocked);
                roads.grades[c.position.y*roads.width+c.position.x]=value?c.wetGrade:c.dryGrade;
            }
            Flooded=value;message="";return true;
        }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current!=null && Keyboard.current.f6Key.wasPressedThisFrame)SetFlooded(!Flooded);
#else
            if(Input.GetKeyDown(KeyCode.F6))SetFlooded(!Flooded);
#endif
        }
        void OnGUI()
        {
            if(!showControls)return;
            if(GUI.Button(new Rect(8,8,215,28),(Flooded?"범람 → 평상시":"평상시 → 범람")+" (F6)"))SetFlooded(!Flooded);
            if(message.Length>0)GUI.Label(new Rect(8,40,460,28),message);
        }
    }
}
#endif
