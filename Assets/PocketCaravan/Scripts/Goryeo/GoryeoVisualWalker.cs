using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Reuse Walker path/animation fields without its interaction registry or global Random draws.</summary>
    public sealed class GoryeoVisualWalker:Walker
    {
        int target=1;
        protected override void OnEnable(){}
        protected override void OnDisable(){}
        public override void Interact(PlayerController player){}
        void Update()
        {
            if(path==null||path.Count<2||UIState.Blocking){if(anim!=null)anim.walking=false;return;}
            var player=PlayerController.Instance;if(player!=null&&Vector2.Distance(player.Motor.Feet,transform.position)<1.2f){if(anim!=null)anim.walking=false;return;}
            var delta=path[target]-transform.position;if(delta.magnitude<.05f){target=target==0?path.Count-1:0;if(anim!=null)anim.walking=false;return;}
            transform.position+=Vector3.ClampMagnitude(delta,speed*Time.deltaTime);
            if(anim!=null){anim.walking=true;anim.facing=Mathf.Abs(delta.x)>=Mathf.Abs(delta.y)?new Vector2Int(delta.x>0?1:-1,0):new Vector2Int(0,delta.y>0?1:-1);}
        }
    }
}
