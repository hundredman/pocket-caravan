using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>A1 visual weather. Opt-in only until approved; no movement, clock, saves or economy writes.</summary>
    [DefaultExecutionOrder(250)]
    public sealed class GoryeoAtmosphere:MonoBehaviour
    {
        public enum Kind{Clear,Rain,Monsoon,Mist}
        [Serializable]public class Settings
        {
            public int seed=73,rainParticles=70,monsoonParticles=128;
            public int clearBelow=55,rainBelow=80,monsoonBelow=90;
            public float mistStart=4,mistEnd=9;
        }
        public const int ParticleLimit=128;
        public Settings settings=new Settings();
        public bool approvedForProduction;
        public Kind Shown{get;private set;}
        public int ActiveParticles=>effects!=null&&effects.activeInHierarchy&&overlay!=null?overlay.ActiveParticleCount:0;
        public int ActiveMistBands=>effects!=null&&effects.activeInHierarchy&&Shown==Kind.Mist?3:0;
        public int CreatedParticleRenderers{get{int count=0;if(overlay!=null)foreach(Transform t in overlay.transform)if(t.name=="Weather particle")count++;return count;}}
        GameObject effects;WeatherOverlay overlay;WeatherSkin[] skins;
        readonly List<SpriteRenderer> bands=new List<SpriteRenderer>();Sprite mistSprite;Texture2D mistTexture;
        readonly Dictionary<SpriteRenderer,bool> ambientStates=new Dictionary<SpriteRenderer,bool>();
        GameObject ambientRoot;MapRoot map;Kind? applied;

        public static Kind DayWeather(int day,Settings options=null)
        {
            var s=options??new Settings();uint h=unchecked((uint)Mathf.Max(1,day)*2654435761u)^(uint)s.seed;
            h^=h>>13;h=unchecked(h*2246822519u);h^=h>>16;int roll=(int)(h%100);
            return roll<s.clearBelow?Kind.Clear:roll<s.rainBelow?Kind.Rain:roll<s.monsoonBelow?Kind.Monsoon:Kind.Mist;
        }
        public static bool MistMap(string id)
        {
            switch(GoryeoLayout.Resolve(id??""))
            {
                case "Naru":case "Gaegyeong":case "Dakmok":case "goryeo_jangdan":
                case "transit_ferry":case "transit_southport":case "transit_wetroad":case "transit_south_inland":return true;
                default:return false;
            }
        }
        public static Kind WeatherFor(int day,string mapId,float hour,Settings options=null)
        {
            if(GoryeoTravelTransition.RegionOf(mapId)==null)return Kind.Clear;
            var s=options??new Settings();var k=DayWeather(day,s);
            return k==Kind.Mist&&(!MistMap(mapId)||hour<s.mistStart||hour>=s.mistEnd)?Kind.Clear:k;
        }
        WeatherSkin Skin(string id,int count,Vector2 drift,Vector2 size,Color color,Color haze)
        {
            var s=ScriptableObject.CreateInstance<WeatherSkin>();s.hideFlags=HideFlags.DontSave;s.id=id;s.displayName=id;
            s.particleCount=Mathf.Clamp(count,0,ParticleLimit);s.drift=drift;s.size=size;s.particleColor=color;s.haze=haze;s.seed=settings.seed;s.flutter=0;
            return s;
        }
        void Build()
        {
            effects=new GameObject("Goryeo weather review");effects.transform.SetParent(transform,false);overlay=effects.AddComponent<WeatherOverlay>();
            skins=new[]{
                Skin("goryeo_clear",0,Vector2.zero,Vector2.zero,Color.clear,Color.clear),
                Skin("goryeo_spring_rain",settings.rainParticles,new Vector2(-1.3f,-11),new Vector2(1f/16,5f/16),new Color(.72f,.83f,.88f,.65f),new Color(.20f,.27f,.35f,.065f)),
                Skin("goryeo_monsoon",settings.monsoonParticles,new Vector2(-2.2f,-15),new Vector2(1f/16,7f/16),new Color(.69f,.80f,.87f,.72f),new Color(.18f,.25f,.34f,.12f)),
                Skin("goryeo_river_mist",0,Vector2.zero,Vector2.zero,Color.clear,new Color(.82f,.88f,.86f,.085f))};
            // Procedural presentation like the existing regional fog; no map-art pixels are edited.
            const int w=64,h=16;mistTexture=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
            var pixels=new Color[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {float v=Mathf.Sin(Mathf.PI*y/(h-1));float u=Mathf.Sin(Mathf.PI*x/(w-1));pixels[y*w+x]=new Color(1,1,1,v*v*u*.21f);}
            mistTexture.SetPixels(pixels);mistTexture.Apply();mistSprite=Sprite.Create(mistTexture,new Rect(0,0,w,h),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);
            for(int i=0;i<3;i++){var go=new GameObject("River mist band");go.transform.SetParent(effects.transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=mistSprite;r.sortingOrder=40;r.color=new Color(.89f,.93f,.90f,1);r.enabled=false;bands.Add(r);}
        }
        void RestoreAmbient(){foreach(var pair in ambientStates)if(pair.Key!=null)pair.Key.enabled=pair.Value;ambientStates.Clear();}
        void HideAmbient()
        {
            if(ambientRoot==null)ambientRoot=GameObject.Find("Ambient FX");
            if(ambientRoot==null||ambientStates.Count>0)return;
            foreach(var r in ambientRoot.GetComponentsInChildren<SpriteRenderer>(true)){ambientStates[r]=r.enabled;r.enabled=false;}
        }
        public void Refresh()
        {
            if(map==null)map=GetComponent<MapRoot>();var gm=GameManager.I;
            if(map==null||map!=MapRoot.Current||gm==null||GoryeoTravelTransition.RegionOf(map.mapId)==null||(!map.previewOnly&&!approvedForProduction))
            {if(effects!=null)effects.SetActive(false);RestoreAmbient();Shown=Kind.Clear;applied=null;return;}
            if(effects==null)Build();effects.SetActive(true);
            Shown=WeatherFor(gm.Clock.day,map.mapId,gm.Clock.minuteOfDay/60f,settings);
            if(applied!=Shown){overlay.Configure(skins[(int)Shown]);applied=Shown;}
            if(Shown==Kind.Clear)RestoreAmbient();else HideAmbient();
            var cam=Camera.main;
            for(int i=0;i<bands.Count;i++)
            {
                var r=bands[i];r.enabled=Shown==Kind.Mist&&cam!=null&&cam.orthographic;if(!r.enabled)continue;
                float h=cam.orthographicSize*2,w=h*cam.aspect;
                r.transform.position=new Vector3(cam.transform.position.x+Mathf.Sin(Time.time*.08f+i*2)*w*.07f,cam.transform.position.y+(i-1)*h*.24f+Mathf.Sin(Time.time*.055f+i)*.35f,0);
                r.transform.localScale=new Vector3(w*1.2f/4f,2.4f,1);
            }
        }
        void LateUpdate()=>Refresh();
        void OnDisable(){if(effects!=null)effects.SetActive(false);RestoreAmbient();applied=null;}
        void OnDestroy()
        {
            RestoreAmbient();if(effects!=null)Destroy(effects);if(skins!=null)foreach(var s in skins)if(s!=null)Destroy(s);
            if(mistSprite!=null)Destroy(mistSprite);if(mistTexture!=null)Destroy(mistTexture);
        }
    }
}
