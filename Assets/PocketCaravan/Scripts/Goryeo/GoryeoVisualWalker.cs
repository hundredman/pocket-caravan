using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Reuse Walker path/animation fields without its interaction registry or global Random draws.
    /// 11/08 은샘: 꺾인 길(여러 점)을 차례로 갔다가 되돌아오고, 끝점마다 잠깐 서서 둘러본다 (일자로 왕복만 하면 어색해서).</summary>
    public sealed class GoryeoVisualWalker:Walker
    {
        int target=1,dir=1;float wait;
        protected override void OnEnable(){}
        protected override void OnDisable(){}
        public override void Interact(PlayerController player){}
        void Update()
        {
            if(path==null||path.Count<2||UIState.Blocking){if(anim!=null)anim.walking=false;return;}
            var player=PlayerController.Instance;if(player!=null&&Vector2.Distance(player.Motor.Feet,transform.position)<1.2f){if(anim!=null)anim.walking=false;return;}
            if(wait>0){wait-=Time.deltaTime;if(anim!=null){anim.walking=false;if(wait<1f)anim.facing=new Vector2Int(dir,0);}return;}
            var delta=path[target]-transform.position;
            if(delta.magnitude<.05f)
            {
                if(target==path.Count-1||target==0){dir=-dir;wait=2.2f;if(anim!=null){anim.walking=false;anim.facing=Vector2Int.down;}}
                target=Mathf.Clamp(target+(target==path.Count-1?-1:target==0?1:dir),0,path.Count-1);
                return;
            }
            transform.position+=Vector3.ClampMagnitude(delta,speed*Time.deltaTime);
            if(anim!=null){anim.walking=true;anim.facing=Mathf.Abs(delta.x)>=Mathf.Abs(delta.y)?new Vector2Int(delta.x>0?1:-1,0):new Vector2Int(0,delta.y>0?1:-1);}
        }
    }
}
