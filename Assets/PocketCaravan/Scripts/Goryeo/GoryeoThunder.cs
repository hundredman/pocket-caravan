using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Approved single soft flash and delayed thunder. Playback itself never changes events or saves.</summary>
    public sealed class GoryeoThunder:MonoBehaviour
    {
        public AudioClip clip;public bool approvedForProduction;public int Plays{get;private set;}AudioSource source;float elapsed=10;bool sounded=true;
        bool Allowed=>MapRoot.Current!=null&&MapRoot.Current.gameObject==gameObject&&(MapRoot.Current.previewOnly||(approvedForProduction&&MapRoot.Current.mapId==GoryeoStormLogs.MapId))&&GoryeoTravelTransition.RegionOf(MapRoot.Current.mapId)!=null&&GetComponent<GoryeoAtmosphere>()!=null&&GetComponent<GoryeoAtmosphere>().isActiveAndEnabled&&GetComponent<GoryeoAtmosphere>().Shown==GoryeoAtmosphere.Kind.Monsoon;
        public float Flash=>Allowed?Mathf.Max(0,1-Mathf.Abs(elapsed-.2f)/.2f)*.07f:0;
        public AudioSource Source=>source;
        void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.loop=false;source.volume=0;}
        public bool PlayOnce(){if(!Allowed||clip==null)return false;source.Stop();elapsed=0;sounded=false;Plays++;return true;}
        void Update()
        {
            if(!Allowed){Stop();return;}elapsed+=Time.unscaledDeltaTime;var gm=GameManager.I;var audio=gm!=null?gm.GetComponent<AudioManager>():null;
            source.volume=AudioManager.Muted||audio==null?0:Mathf.Clamp01(audio.sfxVolume)*.35f;
            if(!sounded&&elapsed>=.7f){sounded=true;source.clip=clip;source.Play();}
        }
        void OnGUI(){float alpha=Flash;if(alpha<=0)return;var old=GUI.color;int depth=GUI.depth;GUI.depth=100;GUI.color=new Color(.88f,.94f,1,alpha);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;GUI.depth=depth;}
        void Stop(){elapsed=10;sounded=true;if(source!=null)source.Stop();}
        void OnDisable()=>Stop();
        void OnDestroy(){Stop();if(source!=null)Destroy(source);}
    }
}
