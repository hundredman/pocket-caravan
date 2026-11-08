using System;
using System.Reflection;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Approved A2 playback; six clip choices, two bounded crossfading sources, no settings/save writes.</summary>
    public sealed class GoryeoTimeMusic:MonoBehaviour
    {
        public AudioClip[] clips=new AudioClip[6];public bool approvedForProduction;
        readonly AudioSource[] buses=new AudioSource[2];readonly float[] weights=new float[2];
        AudioSource commonA,commonB;bool oldMuteA,oldMuteB,ownsCommon;AudioManager master;
        int wanted=-1,to;float elapsed=2,fromWeight;
        static readonly FieldInfo A=typeof(AudioManager).GetField("musicA",BindingFlags.NonPublic|BindingFlags.Instance),B=typeof(AudioManager).GetField("musicB",BindingFlags.NonPublic|BindingFlags.Instance),Current=typeof(AudioManager).GetField("currentClip",BindingFlags.NonPublic|BindingFlags.Instance);
        public int Selected=>wanted;public int Starts{get;private set;}public bool OwnsCommon=>ownsCommon;public int SourceCount=>buses.Length;
        public AudioSource Bus(int index)=>buses[index];public float Weight(int index)=>weights[index];
        void Awake(){for(int i=0;i<2;i++){buses[i]=gameObject.AddComponent<AudioSource>();buses[i].loop=true;buses[i].playOnAwake=false;buses[i].spatialBlend=0;buses[i].volume=0;}}
        public static int Slot(int hour)=>hour>=5&&hour<10?0:hour>=10&&hour<17?1:hour>=17&&hour<20?2:3;
        public static int Choice(string id,int hour,bool danger)
        {
            if(GoryeoTravelTransition.RegionOf(id)==null)return -1;
            if(danger)return 5;
            if(id=="transit_wetroad"||id=="transit_pass"||id=="transit_central_approach"||id=="transit_south_inland")return 4;
            return Slot(hour);
        }
        void Update()=>Tick(Time.unscaledDeltaTime);
        public void Tick(float dt)
        {
            var gm=GameManager.I;var map=MapRoot.Current;bool allowed=gm!=null&&map!=null&&GoryeoTravelTransition.RegionOf(map.mapId)!=null&&(map.previewOnly||approvedForProduction);
            if(!allowed||A==null||B==null||Current==null){Release();return;}
            master=gm.GetComponent<AudioManager>();if(master==null){Release();return;}
            var a=A.GetValue(master)as AudioSource;var b=B.GetValue(master)as AudioSource;
            int next=Choice(map.mapId,gm.Clock.Hour,(Current.GetValue(master)as string)=="danger");
            if(a==null||b==null||next<0||next>=clips.Length||clips[next]==null){Release();return;}
            if(commonA!=a||commonB!=b){Restore();commonA=a;commonB=b;}
            if(!ownsCommon){oldMuteA=commonA.mute;oldMuteB=commonB.mute;ownsCommon=true;}commonA.mute=commonB.mute=true;
            if(next!=wanted)
            {
                wanted=next;to=weights[0]<=weights[1]?0:1;fromWeight=weights[1-to];elapsed=0;
                buses[to].Stop();buses[to].clip=clips[next];buses[to].Play();weights[to]=0;Starts++;
            }
            float duration=wanted==5?.6f:2f;elapsed=Mathf.Min(duration,elapsed+Mathf.Max(0,dt));float angle=elapsed/duration*Mathf.PI*.5f;
            weights[to]=Mathf.Sin(angle);weights[1-to]=fromWeight*Mathf.Cos(angle);
            float volume=Mathf.Clamp01(AudioManager.MusicLevel);   // 11/08 공용 음량 (다른 지역이 musicVolume을 0으로 두던 일 없음)
            for(int i=0;i<2;i++){buses[i].volume=weights[i]*volume;if(i!=to&&elapsed>=duration){buses[i].volume=0;weights[i]=0;if(buses[i].isPlaying)buses[i].Stop();}}
        }
        void Restore(){if(!ownsCommon)return;if(commonA!=null)commonA.mute=oldMuteA;if(commonB!=null)commonB.mute=oldMuteB;ownsCommon=false;}
        void Release(){Restore();wanted=-1;elapsed=2;for(int i=0;i<2;i++)if(buses[i]!=null){buses[i].Stop();buses[i].volume=0;weights[i]=0;}}
        void OnDisable()=>Release();
        void OnDestroy(){Release();foreach(var bus in buses)if(bus!=null)Destroy(bus);}
    }
}
