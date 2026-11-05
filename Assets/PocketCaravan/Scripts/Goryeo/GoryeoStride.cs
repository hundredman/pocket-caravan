using System;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>A4 presentation only. Approved production director or explicit review scene opt-in.</summary>
    public sealed class GoryeoStride:MonoBehaviour
    {
        public enum Surface{Other,Dirt,Grass,Stone,Wood}
        public int width,height;public string surfaces;public AudioClip[] clips=new AudioClip[5];
        public bool approvedForProduction;
        public const int MarkLimit=24;
        public int StepsPlayed{get;private set;}
        public int ActiveMarks{get{int n=0;foreach(var m in marks)if(m.renderer.enabled)n++;return n;}}
        public AudioSource Source=>audioSource;
        public Surface LastSurface{get;private set;}
        class Mark{public SpriteRenderer renderer;public float born,life;public Vector3 at;public Vector2 drift;public bool wet;}
        readonly Mark[] marks=new Mark[MarkLimit];readonly Sprite[] rings=new Sprite[3];Sprite dust;AudioSource audioSource;MapRoot map;
        Vector2 lastFeet;bool ready;float distance,lastStep=-100;int cursor;
        void Awake()
        {
            map=GetComponent<MapRoot>();audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;audioSource.loop=false;
            dust=Pixels(new[]{"....",".xx.","xxxx","..x."});
            rings[0]=Pixels(new[]{"........","...xx...","..x..x..","...xx...","........","........"});
            rings[1]=Pixels(new[]{"........","..xxxx..",".x....x.","..xxxx..","........","........"});
            rings[2]=Pixels(new[]{"..xxxx..",".x....x.","x......x",".x....x.","..xxxx..","........"});
            for(int i=0;i<MarkLimit;i++){var go=new GameObject("Goryeo stride FX");go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sortingOrder=3;r.enabled=false;marks[i]=new Mark{renderer=r};}
        }
        static Sprite Pixels(string[] rows)
        {
            int h=rows.Length,w=rows[0].Length;var t=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)pixels[(h-y-1)*w+x]=rows[y][x]=='x'?Color.white:Color.clear;t.SetPixels(pixels);t.Apply();return Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);
        }
        public Surface At(Vector3Int cell)
        {
            if(cell.x<0||cell.y<0||cell.x>=width||cell.y>=height||surfaces==null||surfaces.Length!=width*height)return Surface.Other;
            switch(surfaces[cell.y*width+cell.x]){case'D':return Surface.Dirt;case'G':return Surface.Grass;case'S':return Surface.Stone;case'W':return Surface.Wood;default:return Surface.Other;}
        }
        public bool Wet
        {
            get{var weather=map!=null?map.GetComponent<GoryeoAtmosphere>():null;return weather!=null&&weather.isActiveAndEnabled&&(weather.Shown==GoryeoAtmosphere.Kind.Rain||weather.Shown==GoryeoAtmosphere.Kind.Monsoon);}
        }
        bool Allowed=>map!=null&&MapRoot.Current==map&&GoryeoTravelTransition.RegionOf(map.mapId)!=null&&(map.previewOnly||approvedForProduction);
        void Update(){Tick(Time.time);Animate(Time.time);}
        public void Tick(float now)
        {
            var gm=GameManager.I;var player=PlayerController.Instance;var audio=gm!=null?gm.GetComponent<AudioManager>():null;
            audioSource.volume=AudioManager.Muted||audio==null?0:Mathf.Clamp01(audio.sfxVolume);
            if(!Allowed||gm==null||player==null||gm.IsTravelling||UIState.Blocking){ready=false;distance=0;if(!Allowed){ClearMarks();audioSource.Stop();}return;}
            Vector2 feet=player.Motor.Feet;if(!ready){lastFeet=feet;ready=true;return;}
            float moved=Vector2.Distance(feet,lastFeet);lastFeet=feet;if(moved>2){distance=0;return;}
            distance+=moved;
            if(moved>.001f&&distance>=1.15f&&now-lastStep>=.16f){distance=Mathf.Repeat(distance,1.15f);lastStep=now;Emit(player.Motor.Cell,player.Running,now);}
        }
        void Emit(Vector3Int cell,bool running,float now)
        {
            if(!Allowed)return;LastSurface=At(cell);if(LastSurface==Surface.Other)return;StepsPlayed++;
            int index=(int)LastSurface-1;audioSource.pitch=1+((StepsPlayed%3)-1)*.025f;
            if(!AudioManager.Muted&&index<clips.Length&&clips[index]!=null)audioSource.PlayOneShot(clips[index],.15f);
            bool wet=Wet;
            if(wet&&clips.Length>4&&clips[4]!=null&&!AudioManager.Muted)audioSource.PlayOneShot(clips[4],.10f);
            Vector3 feet=PlayerController.Instance!=null?(Vector3)PlayerController.Instance.Motor.Feet:map.CellCenter(cell);
            if(wet)MarkAt(feet,Vector2.zero,true,now);
            else if(running&&LastSurface==Surface.Dirt)for(int i=0;i<3;i++)MarkAt(feet+new Vector3((i-1)*.08f,-.08f,0),new Vector2((i-1)*.14f,.18f+i*.03f),false,now);
        }
        void MarkAt(Vector3 at,Vector2 drift,bool wet,float now)
        {
            var cam=Camera.main;if(cam==null)return;var viewport=cam.WorldToViewportPoint(at);if(viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)return;
            var m=marks[cursor];cursor=(cursor+1)%MarkLimit;m.born=now;m.life=wet?.32f:.38f;m.wet=wet;m.at=at;m.drift=drift;m.renderer.enabled=true;AnimateMark(m,now);
        }
        public void Animate(float now){foreach(var m in marks)if(m.renderer.enabled)AnimateMark(m,now);}
        void AnimateMark(Mark m,float now)
        {
            float k=(now-m.born)/m.life;if(k>=1){m.renderer.enabled=false;return;}var at=m.at+(Vector3)m.drift*Mathf.Max(0,now-m.born);at.x=Mathf.Round(at.x*16)/16;at.y=Mathf.Round(at.y*16)/16;m.renderer.transform.position=at;m.renderer.transform.localScale=Vector3.one;
            m.renderer.sprite=m.wet?rings[Mathf.Clamp((int)(k*3),0,2)]:dust;var c=m.wet?new Color(.67f,.85f,.88f,.8f):new Color(.70f,.59f,.43f,.5f);c.a*=1-Mathf.Clamp01(k);m.renderer.color=c;
        }
#if UNITY_EDITOR
        public void ReviewStep(Vector3Int cell,bool running,float now){if(map==null||!map.previewOnly)throw new InvalidOperationException("Review only");Emit(cell,running,now);}
#endif
        void ClearMarks(){foreach(var m in marks)if(m!=null&&m.renderer!=null)m.renderer.enabled=false;}
        void OnDisable(){ready=false;distance=0;ClearMarks();if(audioSource!=null)audioSource.Stop();}
        void OnDestroy(){foreach(var m in marks)if(m!=null&&m.renderer!=null)Destroy(m.renderer.gameObject);if(audioSource!=null)Destroy(audioSource);if(dust!=null){Destroy(dust.texture);Destroy(dust);}foreach(var r in rings)if(r!=null){Destroy(r.texture);Destroy(r);}}
    }
}
