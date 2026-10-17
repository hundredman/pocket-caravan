using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>카메라 범위 안의 제한된 스프라이트 풀. 서버 상태·판정과 독립적인 표시.</summary>
    public class WeatherOverlay : MonoBehaviour
    {
        readonly List<SpriteRenderer> particles=new List<SpriteRenderer>();
        readonly List<Vector2> positions=new List<Vector2>();
        SpriteRenderer haze;Sprite sprite;Texture2D texture;WeatherSkin skin;int configuredCount;
        public int ActiveParticleCount => skin!=null?Mathf.Clamp(skin.particleCount,0,160):0;
        public void Configure(WeatherSkin value)
        {
            skin=value;
            if(sprite==null)
            {
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();texture.filterMode=FilterMode.Point;
                sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);
            }
            SpriteRenderer Create(string label,int order)
            {
                var go=new GameObject(label);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;return r;
            }
            if(haze==null)haze=Create("Weather haze",39);
            int count=ActiveParticleCount;configuredCount=count;
            while(particles.Count<count){particles.Add(Create("Weather particle",40));positions.Add(Vector2.zero);}
            var rng=new System.Random(skin!=null?skin.seed:73);
            for(int i=0;i<particles.Count;i++)
            {
                var r=particles[i];r.gameObject.SetActive(i<count);
                positions[i]=new Vector2((float)rng.NextDouble(),(float)rng.NextDouble());
                if(skin!=null){r.color=skin.particleColor;r.transform.localScale=new Vector3(skin.size.x,skin.size.y,1);}
            }
            haze.gameObject.SetActive(skin!=null && skin.haze.a>0);
            if(skin!=null)haze.color=skin.haze;
        }
        void LateUpdate()
        {
            if(skin==null)return;
            if(ActiveParticleCount!=configuredCount)Configure(skin);
            var cam=Camera.main;if(cam==null || !cam.orthographic)return;
            float h=cam.orthographicSize*2,w=h*cam.aspect;
            var center=cam.transform.position;center.z=0;
            haze.transform.position=center;haze.transform.localScale=new Vector3(w,h,1);
            for(int i=0;i<ActiveParticleCount;i++)
            {
                var p=positions[i];
                p.x=Mathf.Repeat(p.x+(skin.drift.x+Mathf.Sin(Time.time*2+i)*skin.flutter)*Time.deltaTime/w,1);
                p.y=Mathf.Repeat(p.y+skin.drift.y*Time.deltaTime/h,1);positions[i]=p;
                particles[i].transform.position=center+new Vector3((p.x-.5f)*w,(p.y-.5f)*h,0);
            }
        }
        void OnDestroy(){if(sprite!=null)Destroy(sprite);if(texture!=null)Destroy(texture);}
    }
}
