using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>A1 visual weather. Opt-in only until approved; no movement, clock, saves or economy writes.
    /// 2026-11-08 은샘: 안개 없앰. 맑음·비·장맛비만 있고, 예전 안개 날(뽑기 monsoonBelow 이상)은 맑음.</summary>
    [DefaultExecutionOrder(250)]
    public sealed class GoryeoAtmosphere:MonoBehaviour
    {
        public enum Kind{Clear,Rain,Monsoon}
        [Serializable]public class Settings
        {
            public int seed=73,rainParticles=70,monsoonParticles=128;
            public int clearBelow=55,rainBelow=80,monsoonBelow=90;
        }
        public const int ParticleLimit=128;
        public Settings settings=new Settings();
        public bool approvedForProduction;
        public Kind Shown{get;private set;}
        public int ActiveParticles=>effects!=null&&effects.activeInHierarchy&&overlay!=null?overlay.ActiveParticleCount:0;
        public int CreatedParticleRenderers{get{int count=0;if(overlay!=null)foreach(Transform t in overlay.transform)if(t.name=="Weather particle")count++;return count;}}
        GameObject effects;WeatherOverlay overlay;WeatherSkin[] skins;
        readonly Dictionary<SpriteRenderer,bool> ambientStates=new Dictionary<SpriteRenderer,bool>();
        GameObject ambientRoot;MapRoot map;Kind? applied;

        public static Kind DayWeather(int day,Settings options=null)
        {
            var s=options??new Settings();uint h=unchecked((uint)Mathf.Max(1,day)*2654435761u)^(uint)s.seed;
            h^=h>>13;h=unchecked(h*2246822519u);h^=h>>16;int roll=(int)(h%100);
            return roll<s.clearBelow?Kind.Clear:roll<s.rainBelow?Kind.Rain:roll<s.monsoonBelow?Kind.Monsoon:Kind.Clear;
        }
        public static Kind WeatherFor(int day,string mapId,float hour,Settings options=null)
        {
            if(GoryeoTravelTransition.RegionOf(mapId)==null)return Kind.Clear;
            return DayWeather(day,options);
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
                Skin("goryeo_monsoon",settings.monsoonParticles,new Vector2(-2.2f,-15),new Vector2(1f/16,7f/16),new Color(.69f,.80f,.87f,.72f),new Color(.18f,.25f,.34f,.12f))};
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
        }
        void LateUpdate()=>Refresh();
        void OnDisable(){if(effects!=null)effects.SetActive(false);RestoreAmbient();applied=null;}
        void OnDestroy()
        {
            RestoreAmbient();if(effects!=null)Destroy(effects);if(skins!=null)foreach(var s in skins)if(s!=null)Destroy(s);
        }
    }
}
