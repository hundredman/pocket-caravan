using System;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>A3 approved director or review opt-in. Four bounded sources, no gameplay/save writes.</summary>
    public sealed class GoryeoLifeSound:MonoBehaviour
    {
        [Serializable]public class Point{public string kind,reason;public int x,y,startHour,endHour;public float radius,level,interval;}
        public Point[] points=new Point[0];public AudioClip[] clips=new AudioClip[5];public bool approvedForProduction;
        public const int SourceLimit=4;
        public int CallsPlayed{get;private set;}
        public AudioSource Crowd=>sources[0];
        public int SourceCount=>sources.Length;
        readonly AudioSource[] sources=new AudioSource[SourceLimit];readonly Point[] active=new Point[SourceLimit];
        float[] next;MapRoot map;int cursor=1;uint sequence=73;
        void Awake(){map=GetComponent<MapRoot>();for(int i=0;i<sources.Length;i++){sources[i]=gameObject.AddComponent<AudioSource>();sources[i].playOnAwake=false;sources[i].spatialBlend=0;sources[i].volume=0;}sources[0].loop=true;}
        public static float Gain(Point point,Vector2 feet,Vector2 at,float hour)
        {
            if(point==null||hour<point.startHour||hour>=point.endHour||point.radius<=0)return 0;
            return Mathf.Clamp01(1-Vector2.Distance(feet,at)/point.radius)*Mathf.Clamp01(point.level);
        }
        bool Allowed=>map!=null&&MapRoot.Current==map&&GoryeoTravelTransition.RegionOf(map.mapId)!=null&&(map.previewOnly||approvedForProduction);
        static int ClipIndex(string kind){switch(kind){case"crowd":return 0;case"chicken":return 1;case"cow":return 2;case"dog":return 3;case"bird":return 4;default:return -1;}}
        void Update()=>Tick(Time.unscaledDeltaTime,Time.unscaledTime);
        public void Tick(float dt,float now)
        {
            var gm=GameManager.I;var player=PlayerController.Instance;
            if(!Allowed||gm==null||player==null||gm.IsTravelling){Silence();return;}
            if(next==null||next.Length!=points.Length){next=new float[points.Length];for(int i=0;i<next.Length;i++)next[i]=now+3+i*5;}
            var master=gm.GetComponent<AudioManager>();float volume=AudioManager.Muted||master==null?0:Mathf.Clamp01(master.sfxVolume);
            float hour=gm.Clock.Hour;Vector2 feet=player.Motor.Feet;Point crowd=null;
            for(int i=0;i<points.Length;i++)
            {
                var point=points[i];int kind=ClipIndex(point.kind);if(kind<0||kind>=clips.Length||clips[kind]==null)continue;
                if(kind==0){crowd=point;continue;}
                if(now<next[i])continue;sequence=sequence*1664525+1013904223;next[i]=now+Mathf.Max(8,point.interval)*(1+(sequence%100)*.004f);
                float gain=volume*Gain(point,feet,map.CellCenter(new Vector3Int(point.x,point.y,0)),hour);
                if(gain<=.001f)continue;
                int slot=cursor;cursor=cursor==SourceLimit-1?1:cursor+1;
                // Never steal an active call: crowded places remain quiet and bounded.
                if(sources[slot].isPlaying)continue;
                var source=sources[slot];active[slot]=point;source.clip=clips[kind];source.volume=gain;source.pitch=1;source.Play();CallsPlayed++;
            }
            active[0]=crowd;
            for(int i=0;i<sources.Length;i++)
            {
                var source=sources[i];var point=active[i];float want=volume*Gain(point,feet,point==null?Vector2.zero:(Vector2)map.CellCenter(new Vector3Int(point.x,point.y,0)),hour);
                source.volume=volume==0?0:Mathf.MoveTowards(source.volume,want,Mathf.Max(0,dt)*.3f);
                if(i==0){if(clips.Length>0&&clips[0]!=null)source.clip=clips[0];if(source.volume>.001f&&!source.isPlaying&&source.clip!=null)source.Play();if(source.volume<=.001f&&source.isPlaying)source.Stop();}
                else if(source.volume<=.001f&&source.isPlaying)source.Stop();
            }
        }
        void Silence(){foreach(var source in sources)if(source!=null){source.Stop();source.volume=0;}}
        void OnDisable()=>Silence();
        void OnDestroy(){foreach(var source in sources)if(source!=null)Destroy(source);}
    }
}
